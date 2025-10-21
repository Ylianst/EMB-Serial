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
        private DateTime _lastCharTime = DateTime.Now;
        private System.Threading.Timer? _responseTimeoutTimer;
        private readonly int _responseTimeoutMs = 2000;
        private readonly int _charTimeoutMs = 500;
        
        // Background processing
        private CancellationTokenSource? _processingCts;
        private Task? _processingTask;
        
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
                // Stop command processing
                _processingCts?.Cancel();
                _processingTask?.Wait(1000);

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
        /// Sends an L command (L + 12 hex chars)
        /// </summary>
        /// <param name="parameters">The 12 hex characters to send (6 bytes)</param>
        public async Task<CommandResult> LCommandAsync(string parameters)
        {
            if (string.IsNullOrWhiteSpace(parameters) || parameters.Length != 12)
            {
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = "L command requires exactly 12 hex characters"
                };
            }

            string command = $"L{parameters}";
            return await EnqueueCommandAsync(command);
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

                // Stop command processing temporarily
                _processingCts?.Cancel();
                await Task.WhenAny(_processingTask ?? Task.CompletedTask, Task.Delay(1000));

                // Close and reopen port at new baud rate immediately (no delay)
                if (_serialPort?.IsOpen == true)
                {
                    _serialPort.DataReceived -= OnDataReceived;
                    _serialPort.Close();
                }
                _serialPort?.Dispose();

                // Create new serial port at target baud rate
                _serialPort = new SerialPort(_portName, targetBaudRate)
                {
                    DataBits = 8,
                    Parity = Parity.None,
                    StopBits = StopBits.One,
                    Handshake = Handshake.None,
                    ReadTimeout = 500,
                    WriteTimeout = 500
                };

                _serialPort.DataReceived += OnDataReceived;
                _serialPort.Open();
                _serialPort.DiscardInBuffer();
                _serialPort.DiscardOutBuffer();

                _currentBaudRate = targetBaudRate;

                ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs 
                { 
                    OldState = State, 
                    NewState = State, 
                    Message = $"Port opened at {targetBaudRate} baud, listening for BOS..." 
                });

                // Wait for machine to send "BOS" at new baud rate (no delay, start listening immediately)
                _responseBuffer.Clear();
                bool bosReceived = await WaitForStringAsync("BOS", 2000);
                
                if (!bosReceived)
                {
                    SetConnectionState(ConnectionState.Error, $"Did not receive BOS from machine at {targetBaudRate} baud");
                    return false;
                }

                ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs 
                { 
                    OldState = State, 
                    NewState = State, 
                    Message = "Received BOS from machine, sending EBYQ confirmation..." 
                });

                // Send EBYQ to confirm new baud rate
                // Each character will be echoed by the machine, plus an extra 'O' at the end
                _responseBuffer.Clear();
                
                // Send 'E' and wait for echo
                if (!await SendAndWaitForEchoAsync('E', 500))
                {
                    SetConnectionState(ConnectionState.Error, "EBYQ failed - no echo for 'E'");
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
                _processingCts = new CancellationTokenSource();
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

                // Create and configure serial port
                _serialPort = new SerialPort(_portName, baudRate)
                {
                    DataBits = 8,
                    Parity = Parity.None,
                    StopBits = StopBits.One,
                    Handshake = Handshake.None,
                    ReadTimeout = 500,
                    WriteTimeout = 500
                };

                _serialPort.DataReceived += OnDataReceived;
                _serialPort.Open();

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
                    await Task.Delay(50, cts.Token);
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

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                return;
            }

            try
            {
                int bytesToRead = _serialPort.BytesToRead;
                if (bytesToRead == 0)
                {
                    return;
                }

                byte[] buffer = new byte[bytesToRead];
                int bytesRead = _serialPort.Read(buffer, 0, bytesToRead);

                // Raise event for received data
                if (bytesRead > 0)
                {
                    byte[] actualData = new byte[bytesRead];
                    Array.Copy(buffer, actualData, bytesRead);
                    SerialTraffic?.Invoke(this, new SerialTrafficEventArgs { IsSent = false, Data = actualData });
                }

                for (int i = 0; i < bytesRead; i++)
                {
                    char c = (char)buffer[i];
                    _responseBuffer.Append(c);
                    _lastCharTime = DateTime.Now;
                    
                    // Check for immediate error responses
                    if (_currentCommand != null && (c == 'Q' || c == '?' || c == '!'))
                    {
                        // Possible error - but we need to wait a bit to see if it's part of a valid response
                        ResetResponseTimeout();
                    }
                    else
                    {
                        ResetResponseTimeout();
                    }
                    
                    // Check if we have a complete response after each character
                    if (_currentCommand != null)
                    {
                        string currentResponse = _responseBuffer.ToString();
                        if (IsResponseCompleteAsync(_currentCommand.Command, currentResponse).Result)
                        {
                            // Cancel the timeout timer since we're complete
                            _responseTimeoutTimer?.Dispose();
                            _responseTimeoutTimer = null;
                            
                            // Complete the command immediately
                            CompleteCurrentCommand(currentResponse);
                            _responseBuffer.Clear();
                            break; // Exit the loop since command is complete
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle read errors
                if (_currentCommand != null)
                {
                    _currentCommand.CompletionSource.TrySetResult(new CommandResult
                    {
                        Success = false,
                        ErrorMessage = $"Read error: {ex.Message}"
                    });
                    _currentCommand = null;
                }
            }
        }

        private void ResetResponseTimeout()
        {
            _responseTimeoutTimer?.Dispose();
            
            // Use longer character timeout for Read and Large Read commands
            int charTimeout = _charTimeoutMs;
            if (_currentCommand != null && 
                (_currentCommand.Command.StartsWith("R") || _currentCommand.Command.StartsWith("N")))
            {
                charTimeout = 2000; // 2 seconds between characters for read commands
            }
            
            _responseTimeoutTimer = new System.Threading.Timer(_ => OnResponseTimeout(), null, charTimeout, Timeout.Infinite);
        }

        private async void OnResponseTimeout()
        {
            if (_currentCommand == null)
            {
                return;
            }

            string response = _responseBuffer.ToString();
            _responseBuffer.Clear();

            // Check if this is a complete response
            if (await IsResponseCompleteAsync(_currentCommand.Command, response))
            {
                CompleteCurrentCommand(response);
            }
        }

        private Task<bool> IsResponseCompleteAsync(string command, string response)
        {
            if (string.IsNullOrEmpty(response))
            {
                return Task.FromResult(false);
            }

            // Check for error responses
            if (response == "Q" || response == "?" || response == "!")
            {
                return Task.FromResult(true);
            }

            // For Read commands (R + 6 hex) - expect command echo + 64 hex chars + O
            if (command.StartsWith("R") && command.Length == 7)
            {
                int expectedLength = command.Length + 64 + 1;
                return Task.FromResult(response.Length >= expectedLength && response.EndsWith("O"));
            }

            // For Large Read commands (N + 6 hex) - expect command echo + 256 raw bytes + O
            if (command.StartsWith("N") && command.Length == 7)
            {
                int expectedLength = command.Length + 256 + 1;
                return Task.FromResult(response.Length >= expectedLength && response.EndsWith("O"));
            }

            // For Write commands - just echo
            if (command.StartsWith("W") && command.Contains("?"))
            {
                return Task.FromResult(response.Length >= command.Length);
            }

            // For L commands - variable length ending in O
            if (command.StartsWith("L"))
            {
                return Task.FromResult(response.Length > command.Length && response.EndsWith("O"));
            }

            // For other commands - just echo
            return Task.FromResult(response.Length >= command.Length);
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

                // Send command character by character, waiting for echo
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

                // Start response timeout
                ResetResponseTimeout();

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
            finally
            {
                _responseTimeoutTimer?.Dispose();
                _responseTimeoutTimer = null;
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
            _responseTimeoutTimer?.Dispose();
        }
    }
}
