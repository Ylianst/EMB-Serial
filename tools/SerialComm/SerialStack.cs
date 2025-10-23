using System;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bernina.SerialStack
{
    /// <summary>
    /// Connection state of the serial stack
    /// </summary>
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Error
    }

    /// <summary>
    /// Event arguments for connection state changes
    /// </summary>
    public class ConnectionStateChangedEventArgs : EventArgs
    {
        public ConnectionState OldState { get; set; }
        public ConnectionState NewState { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>
    /// Event arguments for command completion
    /// </summary>
    public class CommandCompletedEventArgs : EventArgs
    {
        public string Command { get; set; } = "";
        public bool Success { get; set; }
        public string? Response { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Event arguments for serial traffic debug
    /// </summary>
    public class SerialTrafficEventArgs : EventArgs
    {
        public bool IsSent { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Represents a queued command
    /// </summary>
    internal class QueuedCommand
    {
        public string Command { get; set; } = "";
        public TaskCompletionSource<CommandResult> CompletionSource { get; set; } = new();
        public DateTime EnqueuedTime { get; set; }
    }

    /// <summary>
    /// Result of a command execution
    /// </summary>
    public class CommandResult
    {
        public bool Success { get; set; }
        public string? Response { get; set; }
        public string? ErrorMessage { get; set; }
        public byte[]? BinaryData { get; set; }
    }

    /// <summary>
    /// High-level serial stack for communicating with Bernina machines
    /// </summary>
    public class SerialStack : IDisposable
    {
        private readonly string _portName;
        private SerialPort? _serialPort;
        private ConnectionState _connectionState = ConnectionState.Disconnected;
        private int _currentBaudRate = 19200;
        
        // Command processing
        private readonly ConcurrentQueue<QueuedCommand> _commandQueue = new();
        private QueuedCommand? _currentCommand;
        private readonly SemaphoreSlim _commandSemaphore = new(1, 1);
        private readonly object _stateLock = new();
        
        // Response parsing
        private StringBuilder _responseBuffer = new();
        private readonly int _responseTimeoutMs = 2000;
        
        // Background processing
        private CancellationTokenSource? _processingCts;
        private Task? _processingTask;
        private Task? _readTask;
        
        // Single buffer for blocking reads
        private readonly byte[] _readBuffer = new byte[4096];
        
        // Thread lock for log file writing (SerialTraffic event)
        private readonly object _logLock = new();
        
        // Baud rates to try during connection
        private readonly int[] _baudRatesToTry = { 19200, 57600, 115200, 4800 };

        /// <summary>
        /// Event raised when connection state changes
        /// </summary>
        public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

        /// <summary>
        /// Event raised when a command completes
        /// </summary>
        public event EventHandler<CommandCompletedEventArgs>? CommandCompleted;

        /// <summary>
        /// Event raised when serial traffic occurs (for debugging)
        /// </summary>
        public event EventHandler<SerialTrafficEventArgs>? SerialTraffic;

        /// <summary>
        /// Gets the current connection state
        /// </summary>
        public ConnectionState State
        {
            get
            {
                lock (_stateLock)
                {
                    return _connectionState;
                }
            }
        }

        /// <summary>
        /// Gets the current baud rate
        /// </summary>
        public int BaudRate => _currentBaudRate;

        /// <summary>
        /// Gets whether the stack is currently connected
        /// </summary>
        public bool IsConnected => State == ConnectionState.Connected;

        /// <summary>
        /// Creates a new SerialStack instance
        /// </summary>
        /// <param name="portName">The COM port name (e.g., "COM3")</param>
        public SerialStack(string portName)
        {
            if (string.IsNullOrWhiteSpace(portName))
            {
                throw new ArgumentException("Port name cannot be null or empty", nameof(portName));
            }
            
            _portName = portName;
        }

        /// <summary>
        /// Opens the connection and auto-detects the baud rate
        /// </summary>
        public async Task<bool> OpenAsync()
        {
            if (State != ConnectionState.Disconnected)
            {
                return false;
            }

            SetConnectionState(ConnectionState.Connecting, "Starting connection...");

            try
            {
                // Try each baud rate
                foreach (var baudRate in _baudRatesToTry)
                {
                    if (await TryConnectAtBaudRateAsync(baudRate))
                    {
                        _currentBaudRate = baudRate;
                        SetConnectionState(ConnectionState.Connected, $"Connected at {baudRate} baud");
                        
                        // Start command processing task
                        _processingCts = new CancellationTokenSource();
                        _processingTask = Task.Run(() => ProcessCommandQueueAsync(_processingCts.Token));
                        
                        return true;
                    }
                }

                SetConnectionState(ConnectionState.Error, "Failed to connect at any baud rate");
                return false;
            }
            catch (Exception ex)
            {
                SetConnectionState(ConnectionState.Error, $"Connection error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Closes the connection
        /// </summary>
        public void Close()
        {
            if (State == ConnectionState.Disconnected)
            {
                return;
            }

            try
            {
                // Stop processing tasks
                _processingCts?.Cancel();
                _processingTask?.Wait(1000);
                _readTask?.Wait(1000);

                // Close serial port
                if (_serialPort?.IsOpen == true)
                {
                    _serialPort.Close();
                }

                _serialPort?.Dispose();
                _serialPort = null;

                SetConnectionState(ConnectionState.Disconnected, "Disconnected");
            }
            catch (Exception ex)
            {
                SetConnectionState(ConnectionState.Error, $"Error during disconnect: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends a Read command (R + 6 hex chars) and returns 32 bytes of data
        /// </summary>
        /// <param name="address">The address to read from (will be formatted as 6 hex digits)</param>
        public async Task<CommandResult> ReadAsync(int address)
        {
            string command = $"R{address:X6}";
            return await EnqueueCommandAsync(command);
        }

        /// <summary>
        /// Sends a Large Read command (N + 6 hex chars) and returns 256 bytes of data
        /// </summary>
        /// <param name="address">The address to read from (will be formatted as 6 hex digits)</param>
        public async Task<CommandResult> LargeReadAsync(int address)
        {
            string command = $"N{address:X6}";
            return await EnqueueCommandAsync(command);
        }

        /// <summary>
        /// Reads a block of memory by breaking it down into multiple Read (32 bytes) and LargeRead (256 bytes) commands
        /// For efficiency, always uses LargeRead when reading more than 32 bytes to minimize command count
        /// </summary>
        /// <param name="address">The starting address to read from</param>
        /// <param name="length">The number of bytes to read</param>
        /// <param name="progress">Optional progress callback (current bytes read, total bytes)</param>
        /// <returns>CommandResult with the complete data in BinaryData property</returns>
        public async Task<CommandResult> ReadMemoryBlockAsync(int address, int length, Action<int, int>? progress = null)
        {
            if (length <= 0)
            {
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = "Length must be greater than 0"
                };
            }

            if (State != ConnectionState.Connected)
            {
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = "Not connected"
                };
            }

            try
            {
                byte[] completeData = new byte[length];
                int bytesRead = 0;
                int currentAddress = address;

                while (bytesRead < length)
                {
                    int remainingBytes = length - bytesRead;
                    CommandResult result;

                    // Optimization: Always use LargeRead (256 bytes) when more than 32 bytes remain
                    // This minimizes the number of commands sent to the machine
                    if (remainingBytes > 32)
                    {
                        result = await LargeReadAsync(currentAddress);
                        if (result.Success && result.BinaryData != null)
                        {
                            int bytesToCopy = Math.Min(256, remainingBytes);
                            Array.Copy(result.BinaryData, 0, completeData, bytesRead, bytesToCopy);
                            bytesRead += bytesToCopy;
                            currentAddress += bytesToCopy;
                        }
                        else
                        {
                            return new CommandResult
                            {
                                Success = false,
                                ErrorMessage = $"Large read failed at address 0x{currentAddress:X6}: {result.ErrorMessage}"
                            };
                        }
                    }
                    else
                    {
                        // Only use Read (32 bytes) for the final chunk of 32 bytes or less
                        result = await ReadAsync(currentAddress);
                        if (result.Success && result.BinaryData != null)
                        {
                            int bytesToCopy = Math.Min(32, remainingBytes);
                            Array.Copy(result.BinaryData, 0, completeData, bytesRead, bytesToCopy);
                            bytesRead += bytesToCopy;
                            currentAddress += bytesToCopy;
                        }
                        else
                        {
                            return new CommandResult
                            {
                                Success = false,
                                ErrorMessage = $"Read failed at address 0x{currentAddress:X6}: {result.ErrorMessage}"
                            };
                        }
                    }

                    // Report progress
                    progress?.Invoke(bytesRead, length);
                }

                return new CommandResult
                {
                    Success = true,
                    BinaryData = completeData,
                    Response = $"Read {length} bytes from 0x{address:X6}"
                };
            }
            catch (Exception ex)
            {
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = $"Memory block read error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Sends a Write command (W + 6 hex chars + data + ?)
        /// </summary>
        /// <param name="address">The address to write to</param>
        /// <param name="data">The data bytes to write (will be hex encoded)</param>
        public async Task<CommandResult> WriteAsync(int address, byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = "Data cannot be null or empty"
                };
            }

            StringBuilder hexData = new StringBuilder();
            foreach (byte b in data)
            {
                hexData.Append(b.ToString("X2"));
            }

            string command = $"W{address:X6}{hexData}?";
            return await EnqueueCommandAsync(command);
        }

        /// <summary>
        /// Sends a Sum command (L + 12 hex chars: 6 for address + 6 for length)
        /// Returns the sum of all bytes starting at the specified address for the given length
        /// </summary>
        /// <param name="address">The starting address to sum from</param>
        /// <param name="length">The number of bytes to sum</param>
        public async Task<CommandResult> SumCommandAsync(int address, int length)
        {
            if (length <= 0)
            {
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = "Length must be greater than 0"
                };
            }

            string command = $"L{address:X6}{length:X6}";
            return await EnqueueCommandAsync(command);
        }

        /// <summary>
        /// Sends an Upload command (PS + 4 hex chars) to upload 256 bytes of binary data
        /// The address must be on a 256-byte boundary (last 2 hex digits are 00)
        /// </summary>
        /// <param name="address">The starting address (must be 256-byte aligned, e.g., 0x028F00)</param>
        /// <param name="data">Exactly 256 bytes of data to upload</param>
        public async Task<CommandResult> UploadAsync(int address, byte[] data)
        {
            if (data == null || data.Length != 256)
            {
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = "Data must be exactly 256 bytes"
                };
            }

            // Verify address is on 256-byte boundary (last 2 hex digits must be 00)
            if ((address & 0xFF) != 0)
            {
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = $"Address 0x{address:X6} is not on a 256-byte boundary (must end in 00)"
                };
            }

            // PS command uses only the upper 4 hex digits (implicitly adding 00 at the end)
            string command = $"PS{(address >> 8):X4}";
            return await EnqueueUploadCommandAsync(command, data);
        }

        /// <summary>
        /// Writes a block of memory by efficiently combining Write and Upload commands
        /// Uses Write command for unaligned portions and Upload command for 256-byte aligned blocks
        /// </summary>
        /// <param name="address">The starting address to write to</param>
        /// <param name="data">The data bytes to write</param>
        /// <param name="progress">Optional progress callback (current bytes written, total bytes)</param>
        /// <returns>CommandResult indicating success or failure</returns>
        public async Task<CommandResult> WriteMemoryBlockAsync(int address, byte[] data, Action<int, int>? progress = null)
        {
            if (data == null || data.Length == 0)
            {
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = "Data cannot be null or empty"
                };
            }

            if (State != ConnectionState.Connected)
            {
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = "Not connected"
                };
            }

            try
            {
                int totalLength = data.Length;
                int bytesWritten = 0;
                int currentAddress = address;

                while (bytesWritten < totalLength)
                {
                    int remainingBytes = totalLength - bytesWritten;

                    // Calculate bytes until next 256-byte boundary
                    int bytesToBoundary = 256 - (currentAddress & 0xFF);
                    
                    // If we're at a 256-byte boundary and have at least 256 bytes remaining, use Upload command
                    if ((currentAddress & 0xFF) == 0 && remainingBytes >= 256)
                    {
                        // Use Upload command for 256 bytes
                        byte[] uploadData = new byte[256];
                        Array.Copy(data, bytesWritten, uploadData, 0, 256);
                        
                        var result = await UploadAsync(currentAddress, uploadData);
                        if (!result.Success)
                        {
                            return new CommandResult
                            {
                                Success = false,
                                ErrorMessage = $"Upload failed at address 0x{currentAddress:X6}: {result.ErrorMessage}"
                            };
                        }
                        
                        bytesWritten += 256;
                        currentAddress += 256;
                    }
                    else
                    {
                        // Use Write command for bytes until boundary or remaining bytes (whichever is smaller)
                        int bytesToWrite = Math.Min(bytesToBoundary, remainingBytes);
                        
                        byte[] writeData = new byte[bytesToWrite];
                        Array.Copy(data, bytesWritten, writeData, 0, bytesToWrite);
                        
                        var result = await WriteAsync(currentAddress, writeData);
                        if (!result.Success)
                        {
                            return new CommandResult
                            {
                                Success = false,
                                ErrorMessage = $"Write failed at address 0x{currentAddress:X6}: {result.ErrorMessage}"
                            };
                        }
                        
                        bytesWritten += bytesToWrite;
                        currentAddress += bytesToWrite;
                    }

                    // Report progress
                    progress?.Invoke(bytesWritten, totalLength);
                }

                return new CommandResult
                {
                    Success = true,
                    Response = $"Wrote {totalLength} bytes to 0x{address:X6}"
                };
            }
            catch (Exception ex)
            {
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = $"Memory block write error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Sends a custom command
        /// </summary>
        public async Task<CommandResult> SendCommandAsync(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = "Command cannot be null or empty"
                };
            }

            return await EnqueueCommandAsync(command);
        }

        /// <summary>
        /// Changes the baud rate to 19200. If already at 19200, does nothing.
        /// If at a different baud rate, sends TrMEJ04 command, switches to 19200, and re-establishes connection.
        /// </summary>
        public async Task<bool> ChangeTo19200BaudAsync()
        {
            return await ChangeBaudRateAsync(19200, "TrMEJ04");
        }

        /// <summary>
        /// Changes the baud rate to 57600. If already at 57600, does nothing.
        /// If at a different baud rate, sends TrMEJ05 command, switches to 57600, and re-establishes connection.
        /// </summary>
        public async Task<bool> ChangeTo57600BaudAsync()
        {
            return await ChangeBaudRateAsync(57600, "TrMEJ05");
        }

        /// <summary>
        /// Sends the SessionStart command "TrMEYQ" character by character.
        /// Each character is sent and its echo is awaited before sending the next one.
        /// After all characters are echoed, waits for an additional "O" confirmation from the machine.
        /// </summary>
        public async Task<CommandResult> SessionStartAsync()
        {
            if (State != ConnectionState.Connected)
            {
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = "Not connected"
                };
            }

            try
            {
                const string command = "TrMEYQ";
                
                // Clear response buffer
                _responseBuffer.Clear();
                
                // Send each character and wait for echo
                for (int i = 0; i < command.Length; i++)
                {
                    char c = command[i];
                    if (!await SendAndWaitForEchoAsync(c, 500))
                    {
                        return new CommandResult
                        {
                            Success = false,
                            ErrorMessage = $"SessionStart failed - no echo for '{c}'"
                        };
                    }
                }

                // Wait for the confirmation 'O' character
                bool oReceived = await WaitForCharAsync('O', 1000);
                if (!oReceived)
                {
                    return new CommandResult
                    {
                        Success = false,
                        ErrorMessage = "Did not receive confirmation 'O' after TrMEYQ"
                    };
                }

                // Clear the buffer after successful completion
                _responseBuffer.Clear();

                return new CommandResult
                {
                    Success = true,
                    Response = "SessionStart completed successfully"
                };
            }
            catch (Exception ex)
            {
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = $"SessionStart error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Generic method to change baud rate
        /// </summary>
        private async Task<bool> ChangeBaudRateAsync(int targetBaudRate, string command)
        {
            if (State != ConnectionState.Connected)
            {
                SetConnectionState(ConnectionState.Error, "Cannot change baud rate - not connected");
                return false;
            }

            // Already at target baud rate, do nothing
            if (_currentBaudRate == targetBaudRate)
            {
                SetConnectionState(ConnectionState.Connected, $"Already at {targetBaudRate} baud");
                return true;
            }

            try
            {
                ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs 
                { 
                    OldState = State, 
                    NewState = State, 
                    Message = $"Switching from {_currentBaudRate} to {targetBaudRate} baud..." 
                });
                
                // Send command directly (not through queue) to tell machine to switch
                ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs 
                { 
                    OldState = State, 
                    NewState = State, 
                    Message = $"Sending {command} command to machine..." 
                });
                
                // Send command character by character and wait for echo
                if (_serialPort == null || !_serialPort.IsOpen)
                {
                    SetConnectionState(ConnectionState.Error, "Serial port not open");
                    return false;
                }

                _responseBuffer.Clear();
                
                // Send each character and wait for echo - as soon as last char is echoed, we switch baud rate
                for (int i = 0; i < command.Length; i++)
                {
                    char c = command[i];
                    if (!await SendAndWaitForEchoAsync(c, 500))
                    {
                        SetConnectionState(ConnectionState.Error, $"{command} command failed - no echo for '{c}'");
                        return false;
                    }
                    
                    // As soon as the last character is echoed back, switch to new baud rate immediately
                    if (i == command.Length - 1)
                    {
                        ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs 
                        { 
                            OldState = State, 
                            NewState = State, 
                            Message = $"Last character '{c}' echoed, switching to {targetBaudRate} baud immediately..." 
                        });
                        break;
                    }
                }

                // Stop processing tasks temporarily
                _processingCts?.Cancel();
                await Task.WhenAny(_processingTask ?? Task.CompletedTask, Task.Delay(1000));
                await Task.WhenAny(_readTask ?? Task.CompletedTask, Task.Delay(1000));

                // CRITICAL: Clear the buffer BEFORE closing the port to prepare for new data
                _responseBuffer.Clear();

                // Close and reopen port at new baud rate with minimal delay
                if (_serialPort?.IsOpen == true)
                {
                    _serialPort.Close();
                }
                _serialPort?.Dispose();

                // Create new serial port at target baud rate with infinite read timeout
                _serialPort = new SerialPort(_portName, targetBaudRate)
                {
                    DataBits = 8,
                    Parity = Parity.None,
                    StopBits = StopBits.One,
                    Handshake = Handshake.None,
                    ReadTimeout = Timeout.Infinite,
                    WriteTimeout = 500
                };

                _serialPort.Open();
                
                // DON'T discard buffers - we need any data that arrives!
                // The machine may have already sent BOS while we were opening
                
                _currentBaudRate = targetBaudRate;

                // Start blocking read thread IMMEDIATELY before any delays
                _processingCts = new CancellationTokenSource();
                _readTask = Task.Run(() => BlockingReadLoopAsync(_processingCts.Token));

                // Give the read thread a moment to start
                await Task.Delay(50);

                ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs 
                { 
                    OldState = State, 
                    NewState = State, 
                    Message = $"Port opened at {targetBaudRate} baud, sending EBYQ confirmation..." 
                });

                // Send EBYQ immediately to confirm new baud rate (no need to wait for BOS)
                // Each character will be echoed by the machine, plus an extra 'O' at the end
                _responseBuffer.Clear();
                
                // Send 'E' up to 20 times at 50ms intervals until we get an echo
                bool eEchoed = false;
                for (int attempt = 0; attempt < 30; attempt++)
                {
                    if (_serialPort == null || !_serialPort.IsOpen)
                    {
                        SetConnectionState(ConnectionState.Error, "Serial port not open");
                        return false;
                    }
                    
                    // Send 'E'
                    byte[] eData = new byte[] { (byte)'E' };
                    _serialPort.Write(eData, 0, 1);
                    _serialPort.BaseStream.Flush();
                    SerialTraffic?.Invoke(this, new SerialTrafficEventArgs { IsSent = true, Data = eData });
                    
                    // Wait 50ms and check for echo
                    await Task.Delay(50);
                    
                    string currentBuffer = _responseBuffer.ToString();
                    if (currentBuffer.Contains('E'))
                    {
                        eEchoed = true;
                        _responseBuffer.Clear();
                        break;
                    }
                }
                
                if (!eEchoed)
                {
                    SetConnectionState(ConnectionState.Error, "EBYQ failed - no echo for 'E' after 20 attempts");
                    return false;
                }
                
                // Send 'B' and wait for echo
                if (!await SendAndWaitForEchoAsync('B', 500))
                {
                    SetConnectionState(ConnectionState.Error, "EBYQ failed - no echo for 'B'");
                    return false;
                }
                
                // Send 'Y' and wait for echo
                if (!await SendAndWaitForEchoAsync('Y', 500))
                {
                    SetConnectionState(ConnectionState.Error, "EBYQ failed - no echo for 'Y'");
                    return false;
                }
                
                // Send 'Q' and wait for echo
                if (!await SendAndWaitForEchoAsync('Q', 500))
                {
                    SetConnectionState(ConnectionState.Error, "EBYQ failed - no echo for 'Q'");
                    return false;
                }

                ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs 
                { 
                    OldState = State, 
                    NewState = State, 
                    Message = "EBYQ echoed, waiting for confirmation 'O'..." 
                });

                // Wait for the extra 'O' confirmation character
                bool oReceived = await WaitForCharAsync('O', 1000);
                if (!oReceived)
                {
                    SetConnectionState(ConnectionState.Error, "Did not receive confirmation 'O' after EBYQ");
                    return false;
                }

                ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs 
                { 
                    OldState = State, 
                    NewState = State, 
                    Message = "Received confirmation 'O', baud rate switch complete" 
                });

                // Clear the buffer before restarting command processing
                _responseBuffer.Clear();

                // Restart command processing
                _processingTask = Task.Run(() => ProcessCommandQueueAsync(_processingCts.Token));

                SetConnectionState(ConnectionState.Connected, $"Successfully switched to {targetBaudRate} baud");
                return true;
            }
            catch (Exception ex)
            {
                SetConnectionState(ConnectionState.Error, $"Error changing baud rate: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> TryConnectAtBaudRateAsync(int baudRate)
        {
            try
            {
                // Close existing port if open
                if (_serialPort?.IsOpen == true)
                {
                    _serialPort.Close();
                }
                _serialPort?.Dispose();

                // Create and configure serial port with infinite read timeout
                _serialPort = new SerialPort(_portName, baudRate)
                {
                    DataBits = 8,
                    Parity = Parity.None,
                    StopBits = StopBits.One,
                    Handshake = Handshake.None,
                    ReadTimeout = Timeout.Infinite,
                    WriteTimeout = 500
                };

                _serialPort.Open();
                
                // Start blocking read thread
                _processingCts = new CancellationTokenSource();
                _readTask = Task.Run(() => BlockingReadLoopAsync(_processingCts.Token));

                // Clear any existing data
                _serialPort.DiscardInBuffer();
                _serialPort.DiscardOutBuffer();

                // Try sending RF? command - send R, F, ? separately and check for echoes
                if (await SendAndWaitForEchoAsync('R', 500) &&
                    await SendAndWaitForEchoAsync('F', 500) &&
                    await SendAndWaitForEchoAsync('?', 500))
                {
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Blocking read loop that continuously reads from the serial port
        /// </summary>
        private async Task BlockingReadLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _serialPort != null && _serialPort.IsOpen)
            {
                try
                {
                    // Block on read - this will wait indefinitely due to Timeout.Infinite
                    // Don't pass cancellationToken to Task.Run to avoid OperationCanceledException
                    int bytesRead = await Task.Run(() => 
                    {
                        try
                        {
                            return _serialPort?.Read(_readBuffer, 0, _readBuffer.Length) ?? 0;
                        }
                        catch (OperationCanceledException)
                        {
                            // Port was closed, return 0
                            return 0;
                        }
                        catch (InvalidOperationException)
                        {
                            // Port was closed, return 0
                            return 0;
                        }
                        catch (TimeoutException)
                        {
                            // Should not happen with Timeout.Infinite, but handle it
                            return 0;
                        }
                    });
                    
                    if (bytesRead > 0)
                    {
                        // Raise event for received data
                        byte[] actualData = new byte[bytesRead];
                        Array.Copy(_readBuffer, actualData, bytesRead);
                        
                        SerialTraffic?.Invoke(this, new SerialTrafficEventArgs { IsSent = false, Data = actualData });

                        // Process received data
                        for (int i = 0; i < bytesRead; i++)
                        {
                            char c = (char)_readBuffer[i];
                            _responseBuffer.Append(c);
                            
                            // Check if we have a complete response after each character
                            if (_currentCommand != null)
                            {
                                string currentResponse = _responseBuffer.ToString();
                                if (IsResponseComplete(_currentCommand.Command, currentResponse))
                                {
                                    // Complete the command immediately
                                    CompleteCurrentCommand(currentResponse);
                                    _responseBuffer.Clear();
                                    break; // Exit the loop since command is complete
                                }
                            }
                        }
                    }
                    
                    // Check cancellation after processing data
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    // Clean exit on cancellation
                    break;
                }
                catch (InvalidOperationException)
                {
                    // Port was closed, exit gracefully
                    break;
                }
                catch (Exception ex)
                {
                    // Handle other read errors
                    if (_currentCommand != null)
                    {
                        _currentCommand.CompletionSource.TrySetResult(new CommandResult
                        {
                            Success = false,
                            ErrorMessage = $"Read error: {ex.Message}"
                        });
                        _currentCommand = null;
                    }
                    
                    // If port is closed or not open, exit the loop
                    if (_serialPort == null || !_serialPort.IsOpen)
                    {
                        break;
                    }
                }
            }
        }

        private async Task<bool> SendAndWaitForEchoAsync(char c, int timeoutMs)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                return false;
            }

            try
            {
                _responseBuffer.Clear();
                byte[] data = new byte[] { (byte)c };
                _serialPort.Write(data, 0, 1);
                
                // Flush the output buffer to ensure data is sent immediately
                _serialPort.BaseStream.Flush();
                
                // Raise event for sent data
                SerialTraffic?.Invoke(this, new SerialTrafficEventArgs { IsSent = true, Data = data });

                // Wait for echo
                var cts = new CancellationTokenSource(timeoutMs);
                while (!cts.Token.IsCancellationRequested)
                {
                    if (_responseBuffer.Length > 0 && _responseBuffer[0] == c)
                    {
                        _responseBuffer.Clear();
                        return true;
                    }
                    await Task.Delay(10, cts.Token);
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> WaitForStringAsync(string expectedString, int timeoutMs)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                return false;
            }

            try
            {
                var cts = new CancellationTokenSource(timeoutMs);
                while (!cts.Token.IsCancellationRequested)
                {
                    string currentBuffer = _responseBuffer.ToString();
                    if (currentBuffer.Contains(expectedString))
                    {
                        return true;
                    }
                    // Check more frequently (every 5ms instead of 50ms) to catch BOS quickly
                    await Task.Delay(5, cts.Token);
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> WaitForCharAsync(char expectedChar, int timeoutMs)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                return false;
            }

            try
            {
                var cts = new CancellationTokenSource(timeoutMs);
                while (!cts.Token.IsCancellationRequested)
                {
                    string currentBuffer = _responseBuffer.ToString();
                    if (currentBuffer.Contains(expectedChar))
                    {
                        return true;
                    }
                    await Task.Delay(10, cts.Token);
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private bool IsResponseComplete(string command, string response)
        {
            if (string.IsNullOrEmpty(response))
            {
                return false;
            }

            // Check for error responses
            if (response == "Q" || response == "?" || response == "!")
            {
                return true;
            }

            // For Read commands (R + 6 hex) - expect command echo + 64 hex chars + O
            if (command.StartsWith("R") && command.Length == 7)
            {
                int expectedLength = command.Length + 64 + 1;
                return response.Length >= expectedLength && response.EndsWith("O");
            }

            // For Large Read commands (N + 6 hex) - expect command echo + 256 raw bytes + O
            if (command.StartsWith("N") && command.Length == 7)
            {
                int expectedLength = command.Length + 256 + 1;
                return response.Length >= expectedLength && response.EndsWith("O");
            }

            // For Upload commands (PS + 4 hex) - expect command echo + "OE"
            if (command.StartsWith("PS") && command.Length == 6)
            {
                int expectedLength = command.Length + 2; // Command echo + "OE"
                return response.Length >= expectedLength && response.Contains("OE");
            }

            // For Write commands - just echo
            if (command.StartsWith("W") && command.Contains("?"))
            {
                return response.Length >= command.Length;
            }

            // For L commands - variable length ending in O
            if (command.StartsWith("L"))
            {
                return response.Length > command.Length && response.EndsWith("O");
            }

            // For other commands - just echo
            return response.Length >= command.Length;
        }

        private void CompleteCurrentCommand(string response)
        {
            if (_currentCommand == null)
            {
                return;
            }

            var command = _currentCommand.Command;
            var result = ParseResponse(command, response);

            _currentCommand.CompletionSource.TrySetResult(result);
            
            // Raise event
            CommandCompleted?.Invoke(this, new CommandCompletedEventArgs
            {
                Command = command,
                Success = result.Success,
                Response = result.Response,
                ErrorMessage = result.ErrorMessage
            });

            _currentCommand = null;
        }

        private CommandResult ParseResponse(string command, string response)
        {
            // Check for error responses
            if (response == "Q" || response == "?" || response == "!")
            {
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = $"Machine error response: {response}"
                };
            }

            // Extract data based on command type
            if (command.StartsWith("R") && command.Length == 7)
            {
                // Read command - extract hex data
                if (response.Length > command.Length)
                {
                    string data = response.Substring(command.Length);
                    if (data.EndsWith("O"))
                    {
                        data = data.Substring(0, data.Length - 1);
                    }
                    
                    return new CommandResult
                    {
                        Success = true,
                        Response = data,
                        BinaryData = HexStringToBytes(data)
                    };
                }
            }
            else if (command.StartsWith("N") && command.Length == 7)
            {
                // Large Read command - extract raw binary data (256 bytes sent as raw bytes, not hex)
                if (response.Length > command.Length)
                {
                    string data = response.Substring(command.Length);
                    if (data.EndsWith("O"))
                    {
                        data = data.Substring(0, data.Length - 1);
                    }
                    
                    // Convert the string (which contains raw bytes) to byte array
                    byte[] binaryData = new byte[data.Length];
                    for (int i = 0; i < data.Length; i++)
                    {
                        binaryData[i] = (byte)data[i];
                    }
                    
                    return new CommandResult
                    {
                        Success = true,
                        Response = BitConverter.ToString(binaryData).Replace("-", ""),
                        BinaryData = binaryData
                    };
                }
            }
            else if (command.StartsWith("L"))
            {
                // L command - extract response
                if (response.Length > command.Length)
                {
                    string data = response.Substring(command.Length);
                    if (data.EndsWith("O"))
                    {
                        data = data.Substring(0, data.Length - 1);
                    }
                    
                    return new CommandResult
                    {
                        Success = true,
                        Response = data
                    };
                }
            }

            // For other commands, just return success
            return new CommandResult
            {
                Success = true,
                Response = response
            };
        }

        private byte[] HexStringToBytes(string hex)
        {
            if (hex.Length % 2 != 0)
            {
                return Array.Empty<byte>();
            }

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                try
                {
                    bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                }
                catch
                {
                    return Array.Empty<byte>();
                }
            }
            return bytes;
        }

        private async Task<CommandResult> EnqueueCommandAsync(string command)
        {
            if (State != ConnectionState.Connected)
            {
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = "Not connected"
                };
            }

            var queuedCommand = new QueuedCommand
            {
                Command = command,
                EnqueuedTime = DateTime.Now
            };

            _commandQueue.Enqueue(queuedCommand);
            return await queuedCommand.CompletionSource.Task;
        }

        private async Task<CommandResult> EnqueueUploadCommandAsync(string command, byte[] data)
        {
            if (State != ConnectionState.Connected)
            {
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = "Not connected"
                };
            }

            // Execute upload command directly (not through queue) due to two-phase protocol
            await _commandSemaphore.WaitAsync();
            try
            {
                return await ExecuteUploadCommandAsync(command, data);
            }
            finally
            {
                _commandSemaphore.Release();
            }
        }

        private async Task<CommandResult> ExecuteUploadCommandAsync(string command, byte[] data)
        {
            _responseBuffer.Clear();

            try
            {
                if (_serialPort == null || !_serialPort.IsOpen)
                {
                    return new CommandResult
                    {
                        Success = false,
                        ErrorMessage = "Serial port not open"
                    };
                }

                // Phase 1: Send PS command and wait for "OE" response
                // Set up a temporary command to track the PS command response
                var phaseOneCommand = new QueuedCommand
                {
                    Command = command,
                    EnqueuedTime = DateTime.Now
                };
                _currentCommand = phaseOneCommand;

                // Send PS command character by character
                foreach (char c in command)
                {
                    byte[] charData = new byte[] { (byte)c };
                    _serialPort.Write(charData, 0, 1);
                    _serialPort.BaseStream.Flush();
                    SerialTraffic?.Invoke(this, new SerialTrafficEventArgs { IsSent = true, Data = charData });
                    await Task.Delay(20);
                }

                // Wait for "OE" response (command echo + "OE")
                var timeoutTask = Task.Delay(2000);
                var completedTask = await Task.WhenAny(phaseOneCommand.CompletionSource.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    _currentCommand = null;
                    await RecoverFromErrorAsync();
                    return new CommandResult
                    {
                        Success = false,
                        ErrorMessage = "Upload command timeout waiting for OE"
                    };
                }

                var phaseOneResult = await phaseOneCommand.CompletionSource.Task;
                _currentCommand = null;

                if (!phaseOneResult.Success)
                {
                    await RecoverFromErrorAsync();
                    return phaseOneResult;
                }

                // Verify we got "OE" response
                if (phaseOneResult.Response == null || !phaseOneResult.Response.Contains("OE"))
                {
                    await RecoverFromErrorAsync();
                    return new CommandResult
                    {
                        Success = false,
                        ErrorMessage = $"Expected OE response, got: {phaseOneResult.Response}"
                    };
                }

                // Phase 2: Send 256 bytes of binary data and wait for "O"
                _responseBuffer.Clear();
                
                // Create a completion source for phase 2
                var phaseTwoCompletion = new TaskCompletionSource<bool>();
                
                // Send all 256 bytes
                _serialPort.Write(data, 0, data.Length);
                _serialPort.BaseStream.Flush();
                SerialTraffic?.Invoke(this, new SerialTrafficEventArgs { IsSent = true, Data = data });

                // Wait for "O" confirmation
                var waitStart = DateTime.Now;
                while ((DateTime.Now - waitStart).TotalMilliseconds < 3000)
                {
                    string currentBuffer = _responseBuffer.ToString();
                    if (currentBuffer.Contains("O"))
                    {
                        return new CommandResult
                        {
                            Success = true,
                            Response = $"Successfully uploaded 256 bytes to {command}"
                        };
                    }
                    else if (currentBuffer.Contains("Q"))
                    {
                        // Error - try to recover
                        await RecoverFromErrorAsync();
                        return new CommandResult
                        {
                            Success = false,
                            ErrorMessage = "Machine reported error (Q) during data upload"
                        };
                    }
                    await Task.Delay(50);
                }

                // Timeout waiting for confirmation
                await RecoverFromErrorAsync();
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = "Timeout waiting for O confirmation after sending data"
                };
            }
            catch (Exception ex)
            {
                _currentCommand = null;
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = $"Upload command error: {ex.Message}"
                };
            }
        }

        private async Task ProcessCommandQueueAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_commandQueue.TryDequeue(out var queuedCommand))
                    {
                        await _commandSemaphore.WaitAsync(cancellationToken);
                        try
                        {
                            await ExecuteCommandAsync(queuedCommand, cancellationToken);
                        }
                        finally
                        {
                            _commandSemaphore.Release();
                        }
                    }
                    else
                    {
                        await Task.Delay(10, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Log error but continue processing
                    Console.WriteLine($"Error in command queue: {ex.Message}");
                }
            }
        }

        private async Task ExecuteCommandAsync(QueuedCommand queuedCommand, CancellationToken cancellationToken)
        {
            _currentCommand = queuedCommand;
            _responseBuffer.Clear();

            try
            {
                if (_serialPort == null || !_serialPort.IsOpen)
                {
                    queuedCommand.CompletionSource.TrySetResult(new CommandResult
                    {
                        Success = false,
                        ErrorMessage = "Serial port not open"
                    });
                    return;
                }

                // Determine timeout based on command type
                // Read (R) and Large Read (N) commands can be slow, give them longer timeout
                int commandTimeout = _responseTimeoutMs;
                if (queuedCommand.Command.StartsWith("R") || queuedCommand.Command.StartsWith("N"))
                {
                    commandTimeout = 5000; // 5 seconds for read commands
                }

                // Send command character by character
                foreach (char c in queuedCommand.Command)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    byte[] data = new byte[] { (byte)c };
                    _serialPort.Write(data, 0, 1);
                    
                    // Flush the output buffer to ensure data is sent immediately
                    _serialPort.BaseStream.Flush();
                    
                    // Raise event for sent data
                    SerialTraffic?.Invoke(this, new SerialTrafficEventArgs { IsSent = true, Data = data });
                    
                    // Wait a bit for echo
                    await Task.Delay(20, cancellationToken);
                }

                // Wait for completion or timeout (using command-specific timeout)
                var timeoutTask = Task.Delay(commandTimeout, cancellationToken);
                var completedTask = await Task.WhenAny(queuedCommand.CompletionSource.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    // Timeout - try to recover
                    await RecoverFromErrorAsync();
                    
                    queuedCommand.CompletionSource.TrySetResult(new CommandResult
                    {
                        Success = false,
                        ErrorMessage = "Command timeout"
                    });
                }
            }
            catch (Exception ex)
            {
                queuedCommand.CompletionSource.TrySetResult(new CommandResult
                {
                    Success = false,
                    ErrorMessage = $"Command execution error: {ex.Message}"
                });
            }
        }

        private async Task RecoverFromErrorAsync()
        {
            try
            {
                // Send RF? to reset protocol state
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _responseBuffer.Clear();
                    await SendAndWaitForEchoAsync('R', 500);
                    await SendAndWaitForEchoAsync('F', 500);
                    await SendAndWaitForEchoAsync('?', 500);
                }
            }
            catch
            {
                // Recovery failed
            }
        }

        private void SetConnectionState(ConnectionState newState, string? message = null)
        {
            ConnectionState oldState;
            
            lock (_stateLock)
            {
                oldState = _connectionState;
                _connectionState = newState;
            }

            if (oldState != newState)
            {
                ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
                {
                    OldState = oldState,
                    NewState = newState,
                    Message = message
                });
            }
        }

        public void Dispose()
        {
            Close();
            _processingCts?.Dispose();
            _commandSemaphore?.Dispose();
        }
    }
}
