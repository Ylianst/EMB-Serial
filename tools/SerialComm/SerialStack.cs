using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
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
    /// Session mode of the machine
    /// </summary>
    public enum SessionMode
    {
        SewingMachine,
        EmbroideryModule
    }

    /// <summary>
    /// Storage location for embroidery files
    /// </summary>
    public enum StorageLocation
    {
        EmbroideryModuleMemory,
        PCCard
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
    /// Event arguments for debug messages
    /// </summary>
    public class DebugMessageEventArgs : EventArgs
    {
        public string Message { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
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
    /// Firmware information read from the machine
    /// </summary>
    public class FirmwareInfo
    {
        public SessionMode Mode { get; set; }
        public string Version { get; set; } = "";
        public string? Language { get; set; } = "";
        public string Manufacturer { get; set; } = "";
        public string Date { get; set; } = "";
        public bool PcCardInserted { get; set; }
    }

    /// <summary>
    /// Represents an embroidery file stored in the machine
    /// </summary>
    public class EmbroideryFile
    {
        public int FileId { get; set; }
        public string FileName { get; set; } = "";
        public byte FileAttributes { get; set; }
        public byte[]? PreviewImageData { get; set; } = null;
        public byte[]? FileData { get; set; } = null;
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
        private readonly object _currentCommandLock = new();
        private volatile bool _transmissionComplete = false;
        private volatile bool _commandInProgress = false;
        
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

        // Preview cache: key is {ChecksumHex}~{Attributes:X2}~{FileName}
        private readonly Dictionary<string, byte[]> _previewCache = new();

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
        /// Event raised when debug messages are generated
        /// </summary>
        public event EventHandler<DebugMessageEventArgs>? DebugMessage;

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
        /// Reads a block of memory and verifies it with a checksum comparison.
        /// First reads the data using ReadMemoryBlockAsync, then computes a local checksum.
        /// Then calls SumCommandAsync to get the remote checksum from the machine.
        /// Compares both checksums to verify data integrity.
        /// </summary>
        /// <param name="address">The starting address to read from</param>
        /// <param name="length">The number of bytes to read</param>
        /// <param name="progress">Optional progress callback (current bytes read, total bytes)</param>
        /// <returns>CommandResult with the verified data in BinaryData property, or error if checksums don't match</returns>
        public async Task<CommandResult> ReadMemoryBlockCheckedAsync(int address, int length, Action<int, int>? progress = null)
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
                // Step 1: Read the memory block
                var readResult = await ReadMemoryBlockAsync(address, length, progress);
                
                if (!readResult.Success || readResult.BinaryData == null)
                {
                    return new CommandResult
                    {
                        Success = false,
                        ErrorMessage = $"Failed to read memory block: {readResult.ErrorMessage}"
                    };
                }

                // Step 2: Calculate local checksum (sum of all bytes)
                long localSum = 0;
                foreach (byte b in readResult.BinaryData)
                {
                    localSum += b;
                }

                // Step 3: Get remote checksum from machine
                var sumResult = await SumCommandAsync(address, length);
                
                if (!sumResult.Success || string.IsNullOrEmpty(sumResult.Response))
                {
                    return new CommandResult
                    {
                        Success = false,
                        ErrorMessage = $"Failed to get remote checksum: {sumResult.ErrorMessage}"
                    };
                }

                // Step 4: Parse remote checksum (hex string)
                if (!long.TryParse(sumResult.Response, System.Globalization.NumberStyles.HexNumber, null, out long remoteSum))
                {
                    return new CommandResult
                    {
                        Success = false,
                        ErrorMessage = $"Failed to parse remote checksum: {sumResult.Response}"
                    };
                }

                // Step 5: Compare checksums
                if (localSum != remoteSum)
                {
                    return new CommandResult
                    {
                        Success = false,
                        ErrorMessage = $"Checksum mismatch! Local: 0x{localSum:X}, Remote: 0x{remoteSum:X}"
                    };
                }

                // Success - checksums match
                return new CommandResult
                {
                    Success = true,
                    BinaryData = readResult.BinaryData,
                    Response = $"Read and verified {length} bytes from 0x{address:X6} (Checksum: 0x{localSum:X})"
                };
            }
            catch (Exception ex)
            {
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = $"Memory block checked read error: {ex.Message}"
                };
            }
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
        /// Sends the Protocol Reset command "RF?" to reset the machine's protocol state.
        /// The 'R' character is sent up to 30 times at 50ms intervals until an echo is received,
        /// then 'F' and '?' are sent with echo confirmation.
        /// </summary>
        public async Task<CommandResult> ProtocolResetAsync()
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
                // Clear response buffer
                _responseBuffer.Clear();
                
                // Send 'R' up to 30 times at 50ms intervals until we get an echo
                bool rEchoed = false;
                for (int attempt = 0; attempt < 30; attempt++)
                {
                    if (_serialPort == null || !_serialPort.IsOpen)
                    {
                        return new CommandResult
                        {
                            Success = false,
                            ErrorMessage = "Serial port not open"
                        };
                    }
                    
                    // Send 'R'
                    byte[] rData = new byte[] { (byte)'R' };
                    _serialPort.Write(rData, 0, 1);
                    _serialPort.BaseStream.Flush();
                    SerialTraffic?.Invoke(this, new SerialTrafficEventArgs { IsSent = true, Data = rData });
                    
                    // Wait 50ms and check for echo
                    await Task.Delay(50);
                    
                    string currentBuffer = _responseBuffer.ToString();
                    if (currentBuffer.Contains('R'))
                    {
                        rEchoed = true;
                        _responseBuffer.Clear();
                        break;
                    }
                }
                
                if (!rEchoed)
                {
                    return new CommandResult
                    {
                        Success = false,
                        ErrorMessage = "Protocol Reset failed - no echo for 'R' after 30 attempts"
                    };
                }
                
                // Send 'F' and wait for echo
                if (!await SendAndWaitForEchoAsync('F', 500))
                {
                    return new CommandResult
                    {
                        Success = false,
                        ErrorMessage = "Protocol Reset failed - no echo for 'F'"
                    };
                }
                
                // Send '?' and wait for echo
                if (!await SendAndWaitForEchoAsync('?', 500))
                {
                    return new CommandResult
                    {
                        Success = false,
                        ErrorMessage = "Protocol Reset failed - no echo for '?'"
                    };
                }

                // Clear the buffer after successful completion
                _responseBuffer.Clear();

                return new CommandResult
                {
                    Success = true,
                    Response = "Protocol Reset completed successfully"
                };
            }
            catch (Exception ex)
            {
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = $"Protocol Reset error: {ex.Message}"
                };
            }
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
        /// Sends the SessionEnd command "TrME" character by character.
        /// Each character is sent and its echo is awaited before sending the next one.
        /// If SessionEnd fails, attempts protocol reset (RF?) and retries up to 3 times
        /// with a 100ms pause between each attempt to ensure proper session closure.
        /// </summary>
        public async Task<CommandResult> SessionEndAsync()
        {
            if (State != ConnectionState.Connected)
            {
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = "Not connected"
                };
            }

            const string command = "TrME";
            const int maxRetries = 3;
            const int retryDelayMs = 100;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    // Clear response buffer
                    _responseBuffer.Clear();
                    
                    // Send each character and wait for echo
                    for (int i = 0; i < command.Length; i++)
                    {
                        char c = command[i];
                        if (!await SendAndWaitForEchoAsync(c, 500))
                        {
                            // Character echo failed, break to next retry
                            break;
                        }
                        
                        // Check if we successfully sent all characters
                        if (i == command.Length - 1)
                        {
                            // All characters echoed successfully
                            _responseBuffer.Clear();
                            return new CommandResult
                            {
                                Success = true,
                                Response = "SessionEnd completed successfully"
                            };
                        }
                    }

                    // If we reach here, SessionEnd failed on this attempt
                    if (attempt < maxRetries - 1)
                    {
                        RaiseDebugMessage($"SessionEnd: Attempt {attempt + 1} failed, attempting protocol reset");
                        
                        // Try to reset protocol using RF?
                        try
                        {
                            await ProtocolResetAsync();
                        }
                        catch (Exception ex)
                        {
                            RaiseDebugMessage($"SessionEnd: Protocol reset failed: {ex.Message}");
                        }
                        
                        // Pause before retry
                        await Task.Delay(retryDelayMs);
                    }
                }
                catch (Exception ex)
                {
                    RaiseDebugMessage($"SessionEnd: Exception on attempt {attempt + 1}: {ex.Message}");
                    
                    if (attempt < maxRetries - 1)
                    {
                        // Try to reset protocol and retry
                        try
                        {
                            await ProtocolResetAsync();
                        }
                        catch (Exception resetEx)
                        {
                            RaiseDebugMessage($"SessionEnd: Protocol reset failed: {resetEx.Message}");
                        }
                        
                        // Pause before retry
                        await Task.Delay(retryDelayMs);
                    }
                }
            }

            // All retries exhausted
            return new CommandResult
            {
                Success = false,
                ErrorMessage = $"SessionEnd failed after {maxRetries} attempts"
            };
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
                            // Only check after transmission is complete to avoid race condition
                            // Use lock to safely access _currentCommand
                            bool shouldCheckCompletion = false;
                            string? commandToCheck = null;
                            
                            lock (_currentCommandLock)
                            {
                                if (_currentCommand != null && _transmissionComplete)
                                {
                                    shouldCheckCompletion = true;
                                    commandToCheck = _currentCommand.Command;
                                }
                            }
                            
                            if (shouldCheckCompletion && commandToCheck != null)
                            {
                                string currentResponse = _responseBuffer.ToString();
                                if (IsResponseComplete(commandToCheck, currentResponse))
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
            // Debug: Log the call to IsResponseComplete
            //($"IsResponseComplete: Command='{command}' (len={command.Length}), Response='{response}' (len={response.Length})");
            
            if (string.IsNullOrEmpty(response))
            {
                //RaiseDebugMessage("IsResponseComplete: Response is null or empty, returning false");
                return false;
            }

            // Check for error responses
            if (response == "Q" || response == "?" || response == "!")
            {
                //RaiseDebugMessage($"IsResponseComplete: Error response detected: '{response}', returning true");
                return true;
            }

            // For Read commands (R + 6 hex) - expect command echo + 64 hex chars + O
            // Must be EXACT length to avoid false completion with trailing data
            if (command.StartsWith("R") && command.Length == 7)
            {
                int expectedLength = command.Length + 64 + 1; // Command (7) + Data (64) + 'O' (1) = 72
                bool isComplete = response.Length == expectedLength && response.EndsWith("O");
                //RaiseDebugMessage($"IsResponseComplete: Read command - expected={expectedLength}, actual={response.Length}, endsWithO={response.EndsWith("O")}, result={isComplete}");
                return isComplete;
            }

            // For Large Read commands (N + 6 hex) - expect command echo + 256 raw bytes + O
            // Must be EXACT length to avoid false completion with trailing data
            if (command.StartsWith("N") && command.Length == 7)
            {
                int expectedLength = command.Length + 256 + 1; // Command (7) + Data (256) + 'O' (1) = 264
                bool isComplete = response.Length == expectedLength && response.EndsWith("O");
                //RaiseDebugMessage($"IsResponseComplete: Large Read command - expected={expectedLength}, actual={response.Length}, endsWithO={response.EndsWith("O")}, result={isComplete}");
                return isComplete;
            }

            // For Upload commands (PS + 4 hex) - expect command echo + "OE"
            if (command.StartsWith("PS") && command.Length == 6)
            {
                int expectedLength = command.Length + 2; // Command echo + "OE"
                bool isComplete = response.Length >= expectedLength && response.Contains("OE");
                //RaiseDebugMessage($"IsResponseComplete: Upload command - expected>={expectedLength}, actual={response.Length}, containsOE={response.Contains("OE")}, result={isComplete}");
                return isComplete;
            }

            // For Write commands (W + 6 hex + data + ?) - just need full echo, no additional confirmation
            // The Write command has no response beyond echoing the entire command including the '?'
            // Must be EXACT length to avoid waiting for more data
            if (command.StartsWith("W") && command.EndsWith("?"))
            {
                bool isComplete = string.Compare(response, command) == 0;
                //RaiseDebugMessage($"IsResponseComplete: Write command - exact match={isComplete}, result={isComplete}");
                return isComplete;
            }

            // For L commands - variable length ending in O
            if (command.StartsWith("L"))
            {
                bool isComplete = response.Length > command.Length && response.EndsWith("O");
                //RaiseDebugMessage($"IsResponseComplete: L command - len>{command.Length}={response.Length > command.Length}, endsWithO={response.EndsWith("O")}, result={isComplete}");
                return isComplete;
            }

            // For other commands - just echo
            bool defaultComplete = response.Length >= command.Length;
            //RaiseDebugMessage($"IsResponseComplete: Other command - len>={command.Length}, result={defaultComplete}");
            return defaultComplete;
        }

        private void CompleteCurrentCommand(string response)
        {
            lock (_currentCommandLock)
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

                // Clear command state atomically
                _currentCommand = null;
                _commandInProgress = false;
                _transmissionComplete = false;
            }
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
                
                // Use atomic method to set current command
                SetCurrentCommand(phaseOneCommand);
                
                // Check if command was rejected (already in progress)
                if (_currentCommand != phaseOneCommand)
                {
                    return new CommandResult
                    {
                        Success = false,
                        ErrorMessage = "Another command is already in progress"
                    };
                }

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
                var timeoutTask = Task.Delay(5000);
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
            // Use atomic method to set current command
            SetCurrentCommand(queuedCommand);
            
            // Check if command was rejected (already in progress)
            if (_currentCommand != queuedCommand)
            {
                queuedCommand.CompletionSource.TrySetResult(new CommandResult
                {
                    Success = false,
                    ErrorMessage = "Another command is already in progress"
                });
                return;
            }

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
                    
                    // Small delay between characters
                    await Task.Delay(10, cancellationToken);
                }

                // CRITICAL: Mark transmission as complete BEFORE starting to wait
                // This ensures that any data received while we're setting up the wait
                // can be processed by IsResponseComplete
                _transmissionComplete = true;
                
                // Give a tiny moment for any buffered data to be processed
                await Task.Delay(5, cancellationToken);

                // Check if response is already complete (data may have arrived during transmission)
                string currentResponse = _responseBuffer.ToString();
                if (!string.IsNullOrEmpty(currentResponse) && IsResponseComplete(queuedCommand.Command, currentResponse))
                {
                    RaiseDebugMessage($"ExecuteCommand: Response already complete after transmission: '{currentResponse}'");
                    CompleteCurrentCommand(currentResponse);
                    _responseBuffer.Clear();
                    return;
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
            finally
            {
                _transmissionComplete = false; // Reset flag after command completes
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

        /// <summary>
        /// Atomically sets the current command and resets related state
        /// </summary>
        private void SetCurrentCommand(QueuedCommand? command)
        {
            lock (_currentCommandLock)
            {
                // Wait if a command is still in progress
                if (_commandInProgress && command != null)
                {
                    // Don't set a new command while one is in progress
                    return;
                }
                
                _currentCommand = command;
                if (command != null)
                {
                    _commandInProgress = true;
                    _transmissionComplete = false;
                    lock (_responseBuffer)
                    {
                        _responseBuffer.Clear();
                    }
                }
                else
                {
                    _commandInProgress = false;
                }
            }
        }

        /// <summary>
        /// Raises a debug message event
        /// </summary>
        private void RaiseDebugMessage(string message)
        {
            DebugMessage?.Invoke(this, new DebugMessageEventArgs
            {
                Message = message,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// Sets argument 1 for function invocation by writing a byte to 0x0201E1.
        /// </summary>
        /// <param name="value">The byte value to set as argument 1</param>
        /// <returns>CommandResult indicating success or failure</returns>
        public async Task<CommandResult> SetArgument1Async(byte value)
        {
            // Check connection status
            if (State != ConnectionState.Connected)
            {
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = "Not connected to machine"
                };
            }

            try
            {
                byte[] data = new byte[] { value };
                var writeResult = await WriteAsync(0x0201E1, data);
                
                if (!writeResult.Success)
                {
                    return new CommandResult
                    {
                        Success = false,
                        ErrorMessage = $"Failed to write argument 1 to 0x0201E1: {writeResult.ErrorMessage}"
                    };
                }

                return new CommandResult
                {
                    Success = true,
                    Response = $"Argument 1 set to 0x{value:X2} at 0x0201E1"
                };
            }
            catch (Exception ex)
            {
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = $"SetArgument1 error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Sets argument 2 for function invocation by writing a byte to 0x0201DC.
        /// </summary>
        /// <param name="value">The byte value to set as argument 2</param>
        /// <returns>CommandResult indicating success or failure</returns>
        public async Task<CommandResult> SetArgument2Async(byte value)
        {
            // Check connection status
            if (State != ConnectionState.Connected)
            {
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = "Not connected to machine"
                };
            }

            try
            {
                byte[] data = new byte[] { value };
                var writeResult = await WriteAsync(0x0201DC, data);
                
                if (!writeResult.Success)
                {
                    return new CommandResult
                    {
                        Success = false,
                        ErrorMessage = $"Failed to write argument 2 to 0x0201DC: {writeResult.ErrorMessage}"
                    };
                }

                return new CommandResult
                {
                    Success = true,
                    Response = $"Argument 2 set to 0x{value:X2} at 0x0201DC"
                };
            }
            catch (Exception ex)
            {
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = $"SetArgument2 error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Gets the current session mode by reading address 0x57FF80.
        /// If the first two bytes are 0xB4A5, the machine is in Sewing Machine mode.
        /// Otherwise, it's in Embroidery Module mode.
        /// </summary>
        /// <returns>SessionMode enum value, or null if read fails</returns>
        public async Task<SessionMode?> GetCurrentSessionModeAsync()
        {
            // Check connection status
            if (State != ConnectionState.Connected)
            {
                return null;
            }

            try
            {
                var readResult = await ReadAsync(0x57FF80);
                
                if (!readResult.Success || readResult.BinaryData == null || readResult.BinaryData.Length < 2)
                {
                    return null;
                }

                // Check if first two bytes are 0xB4A5 (Sewing Machine mode)
                if (readResult.BinaryData[0] == 0xB4 && readResult.BinaryData[1] == 0xA5)
                {
                    return SessionMode.SewingMachine;
                }
                else
                {
                    return SessionMode.EmbroideryModule;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Reads firmware information from address 0x200100.
        /// First detects the mode (Sewing Machine or Embroidery Module) by calling GetCurrentSessionModeAsync().
        /// Parses version, language (if in Sewing Machine mode), manufacturer, and date.
        /// </summary>
        /// <returns>FirmwareInfo object containing mode, version, language, manufacturer, and date, or null if read fails</returns>
        public async Task<FirmwareInfo?> ReadFirmwareInfoAsync()
        {
            // Check connection status
            if (State != ConnectionState.Connected)
            {
                return null;
            }

            try
            {
                // Detect the current session mode
                var sessionMode = await GetCurrentSessionModeAsync();
                
                if (sessionMode == null)
                {
                    return null;
                }

                bool isSewingMachineMode = (sessionMode == SessionMode.SewingMachine);

                // Read 256 bytes from address 0x200100
                var readResult = await LargeReadAsync(0x200100);
                
                if (!readResult.Success || readResult.BinaryData == null)
                {
                    return null;
                }

                byte[] data = readResult.BinaryData;
                
                string version = "";
                string? language = null;
                string manufacturer = "";
                string date = "";
                
                int index = 0;
                
                // Read version string (until first null byte)
                StringBuilder versionBuilder = new StringBuilder();
                while (index < data.Length && data[index] != 0x00)
                {
                    // Only include printable ASCII characters
                    if (data[index] >= 0x20 && data[index] <= 0x7E)
                    {
                        versionBuilder.Append((char)data[index]);
                    }
                    index++;
                }
                version = versionBuilder.ToString().Trim();
                
                // Skip the null terminator
                if (index < data.Length && data[index] == 0x00)
                {
                    index++;
                }
                
                // In Sewing Machine mode, read language string
                // In Embroidery Module mode, skip language (set to null)
                if (isSewingMachineMode)
                {
                    // Read language string (until next null byte)
                    StringBuilder languageBuilder = new StringBuilder();
                    while (index < data.Length && data[index] != 0x00)
                    {
                        // Only include printable ASCII characters
                        if (data[index] >= 0x20 && data[index] <= 0x7E)
                        {
                            languageBuilder.Append((char)data[index]);
                        }
                        index++;
                    }
                    language = languageBuilder.ToString().Trim();
                    
                    // Skip the null terminator
                    if (index < data.Length && data[index] == 0x00)
                    {
                        index++;
                    }
                }
                
                // Read manufacturer string (until next null byte)
                StringBuilder manufacturerBuilder = new StringBuilder();
                while (index < data.Length && data[index] != 0x00)
                {
                    // Only include printable ASCII characters
                    if (data[index] >= 0x20 && data[index] <= 0x7E)
                    {
                        manufacturerBuilder.Append((char)data[index]);
                    }
                    index++;
                }
                manufacturer = manufacturerBuilder.ToString().Trim();
                
                // Skip the null terminator
                if (index < data.Length && data[index] == 0x00)
                {
                    index++;
                }
                
                // Read date string (until next null byte)
                StringBuilder dateBuilder = new StringBuilder();
                while (index < data.Length && data[index] != 0x00)
                {
                    // Only include printable ASCII characters
                    if (data[index] >= 0x20 && data[index] <= 0x7E)
                    {
                        dateBuilder.Append((char)data[index]);
                    }
                    index++;
                }
                date = dateBuilder.ToString().Trim();
                
                // Check for PC card insertion (only in Embroidery Module mode)
                bool pcCardInserted = false;
                if (!isSewingMachineMode)
                {
                    // Read 0xFFFED9 to check PC card status
                    var pcCardReadResult = await ReadAsync(0xFFFED9);
                    
                    if (pcCardReadResult.Success && pcCardReadResult.BinaryData != null && pcCardReadResult.BinaryData.Length > 0)
                    {
                        // Check the least significant bit of the first byte
                        // 0x83 (bit 0 = 1) = PC card present
                        // 0x82 (bit 0 = 0) = No PC card
                        pcCardInserted = (pcCardReadResult.BinaryData[0] & 0x01) == 0x01;
                    }
                }
                
                // Return the parsed firmware info
                return new FirmwareInfo
                {
                    Mode = sessionMode.Value,
                    Version = version,
                    Language = language,
                    Manufacturer = manufacturer,
                    Date = date,
                    PcCardInserted = pcCardInserted
                };
            }
            catch (Exception)
            {
                // Return null on any parsing errors
                return null;
            }
        }

        /// <summary>
        /// Invokes a machine function by writing the function ID to 0xFFFED0 and verifying completion.
        /// The machine responds with 0x0002 or 0x0000 in the first two bytes when the function completes.
        /// </summary>
        /// <param name="functionId">The function ID to invoke (16-bit value)</param>
        /// <returns>CommandResult indicating success or failure</returns>
        public async Task<CommandResult> InvokeFunctionAsync(ushort functionId)
        {
            // Check connection status
            if (State != ConnectionState.Connected)
            {
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = "Not connected to machine"
                };
            }

            try
            {
                // Write the function ID to 0xFFFED0 as a 16-bit value
                byte[] functionBytes = new byte[] 
                { 
                    (byte)(functionId >> 8),   // High byte
                    (byte)(functionId & 0xFF)  // Low byte
                };
                
                var writeResult = await WriteAsync(0xFFFED0, functionBytes);
                if (!writeResult.Success)
                {
                    return new CommandResult
                    {
                        Success = false,
                        ErrorMessage = $"Failed to write function ID to 0xFFFED0: {writeResult.ErrorMessage}"
                    };
                }

                // Read back from 0xFFFED0 and check the first two bytes
                const int maxRetries = 5;
                const int retryDelayMs = 100;

                for (int attempt = 0; attempt <= maxRetries; attempt++)
                {
                    var readResult = await ReadAsync(0xFFFED0);
                    
                    if (!readResult.Success)
                    {
                        return new CommandResult
                        {
                            Success = false,
                            ErrorMessage = $"Failed to read from 0xFFFED0: {readResult.ErrorMessage}"
                        };
                    }

                    if (readResult.BinaryData == null || readResult.BinaryData.Length < 2)
                    {
                        return new CommandResult
                        {
                            Success = false,
                            ErrorMessage = "Invalid response: expected at least 2 bytes"
                        };
                    }

                    // Check if first two bytes are 0x0002 or 0x0000
                    ushort responseValue = (ushort)((readResult.BinaryData[0] << 8) | readResult.BinaryData[1]);
                    
                    if (responseValue == 0x0002 || responseValue == 0x0000)
                    {
                        return new CommandResult
                        {
                            Success = true,
                            Response = $"Function 0x{functionId:X4} invoked successfully, response: 0x{responseValue:X4}",
                            BinaryData = readResult.BinaryData
                        };
                    }

                    // If we haven't exhausted retries, wait and try again
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(retryDelayMs);
                    }
                    else
                    {
                        // Final attempt failed
                        return new CommandResult
                        {
                            Success = false,
                            ErrorMessage = $"Function invocation failed after {maxRetries + 1} attempts. " +
                                         $"Expected 0x0002 or 0x0000, but got 0x{responseValue:X4}"
                        };
                    }
                }

                // Should not reach here, but provide a fallback
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = "Function invocation failed: unexpected error"
                };
            }
            catch (Exception ex)
            {
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = $"Function invocation error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Reads embroidery files from the specified storage location (Embroidery Module Memory or PC Card).
        /// Returns a list of EmbroideryFile objects with FileId, FileName, and FileAttributes populated.
        /// PreviewImageData is populated if loadPreviews is true, otherwise null.
        /// FileData is never populated by this method (set to null).
        /// Ensures cleanup (function 0x0101 and session end) occurs even if errors happen.
        /// </summary>
        /// <param name="location">Storage location to read from</param>
        /// <param name="loadPreviews">If true, also loads preview image data for each file; defaults to false</param>
        /// <returns>List of EmbroideryFile objects, or null if operation fails</returns>
        public async Task<List<EmbroideryFile>?> ReadEmbroideryFilesAsync(StorageLocation location, bool loadPreviews = false)
        {
            RaiseDebugMessage($"ReadEmbroideryFiles: Starting read from {location}");
            
            // Step 1: Check connection
            if (State != ConnectionState.Connected)
            {
                RaiseDebugMessage("ReadEmbroideryFiles: Not connected");
                return null;
            }

            List<EmbroideryFile>? fileList = null;
            bool sessionStarted = false;

            try
            {
                // Step 2: Ensure we're in Embroidery Mode
                RaiseDebugMessage("ReadEmbroideryFiles: Checking current session mode");
                var currentMode = await GetCurrentSessionModeAsync();
                if (currentMode == null)
                {
                    RaiseDebugMessage("ReadEmbroideryFiles: Failed to get session mode");
                    return null;
                }
                RaiseDebugMessage($"ReadEmbroideryFiles: Current mode is {currentMode}");

                // If in Sewing Machine mode, start embroidery session
                if (currentMode == SessionMode.SewingMachine)
                {
                    RaiseDebugMessage("ReadEmbroideryFiles: In Sewing Machine mode, starting embroidery session");
                    var sessionStartResult = await SessionStartAsync();
                    if (!sessionStartResult.Success)
                    {
                        RaiseDebugMessage($"ReadEmbroideryFiles: Session start failed: {sessionStartResult.ErrorMessage}");
                        return null;
                    }
                    sessionStarted = true;
                    RaiseDebugMessage("ReadEmbroideryFiles: Session start successful");
                }
                else
                {
                    RaiseDebugMessage("ReadEmbroideryFiles: Already in Embroidery Module mode");
                    sessionStarted = true;
                }

                // Step 3: Check PC Card if reading from PC Card
                if (location == StorageLocation.PCCard)
                {
                    RaiseDebugMessage("ReadEmbroideryFiles: Checking PC card status");
                    var pcCardReadResult = await ReadAsync(0xFFFED9);
                    
                    if (pcCardReadResult.Success && pcCardReadResult.BinaryData != null && pcCardReadResult.BinaryData.Length > 0)
                    {
                        bool pcCardInserted = (pcCardReadResult.BinaryData[0] & 0x01) == 0x01;
                        RaiseDebugMessage($"ReadEmbroideryFiles: PC card inserted: {pcCardInserted}");
                        if (!pcCardInserted)
                        {
                            RaiseDebugMessage("ReadEmbroideryFiles: No PC card present, returning empty list");
                            return new List<EmbroideryFile>();
                        }
                    }
                    else
                    {
                        RaiseDebugMessage("ReadEmbroideryFiles: Failed to read PC card status");
                        return null;
                    }
                }

                // Step 4: Select storage source
                ushort storageFunction = location == StorageLocation.EmbroideryModuleMemory ? (ushort)0x00A1 : (ushort)0x0051;
                RaiseDebugMessage($"ReadEmbroideryFiles: Selecting storage source with function 0x{storageFunction:X4}");
                var selectStorageResult = await InvokeFunctionAsync(storageFunction);
                if (!selectStorageResult.Success)
                {
                    RaiseDebugMessage($"ReadEmbroideryFiles: Failed to select storage source: {selectStorageResult.ErrorMessage}");
                    return null;
                }
                RaiseDebugMessage("ReadEmbroideryFiles: Storage source selected successfully");

                // Step 5: Initialize reading
                RaiseDebugMessage("ReadEmbroideryFiles: Initializing file reading (function 0x0031 with args 0x01, 0x00)");
                var setArg2Result = await SetArgument2Async(0x01);
                if (!setArg2Result.Success)
                {
                    RaiseDebugMessage($"ReadEmbroideryFiles: Failed to set argument 2: {setArg2Result.ErrorMessage}");
                    return null;
                }

                var setArg1Result = await SetArgument1Async(0x00);
                if (!setArg1Result.Success)
                {
                    RaiseDebugMessage($"ReadEmbroideryFiles: Failed to set argument 1: {setArg1Result.ErrorMessage}");
                    return null;
                }

                var invokeFunc31Result = await InvokeFunctionAsync(0x0031);
                if (!invokeFunc31Result.Success)
                {
                    RaiseDebugMessage($"ReadEmbroideryFiles: Failed to invoke function 0x0031: {invokeFunc31Result.ErrorMessage}");
                    return null;
                }
                RaiseDebugMessage("ReadEmbroideryFiles: Function 0x0031 invoked successfully");

                RaiseDebugMessage("ReadEmbroideryFiles: Invoking function 0x0021");
                var invokeFunc21Result = await InvokeFunctionAsync(0x0021);
                if (!invokeFunc21Result.Success)
                {
                    RaiseDebugMessage($"ReadEmbroideryFiles: Failed to invoke function 0x0021: {invokeFunc21Result.ErrorMessage}");
                    return null;
                }
                RaiseDebugMessage("ReadEmbroideryFiles: Function 0x0021 invoked successfully");

                // Step 6: Read file count
                RaiseDebugMessage("ReadEmbroideryFiles: Reading file count from 0x024080");
                var fileCountResult = await ReadAsync(0x024080);
                if (!fileCountResult.Success || fileCountResult.BinaryData == null || fileCountResult.BinaryData.Length == 0)
                {
                    RaiseDebugMessage("ReadEmbroideryFiles: Failed to read file count");
                    return null;
                }

                int totalFileCount = fileCountResult.BinaryData[0];
                RaiseDebugMessage($"ReadEmbroideryFiles: Total file count: {totalFileCount}");
                fileList = new List<EmbroideryFile>(totalFileCount);

                // Step 7: Set initial page
                RaiseDebugMessage("ReadEmbroideryFiles: Setting initial page to 0");
                var setPageResult = await SetArgument1Async(0);
                if (!setPageResult.Success)
                {
                    RaiseDebugMessage($"ReadEmbroideryFiles: Failed to set initial page: {setPageResult.ErrorMessage}");
                    return null;
                }

                // Step 8: Loop through pages (27 files per page)
                int fileIndex = 0;
                int pageIndex = 0;

                while (fileIndex < totalFileCount)
                {
                    int filesOnThisPage = Math.Min(27, totalFileCount - fileIndex);
                    RaiseDebugMessage($"ReadEmbroideryFiles: Reading page {pageIndex} ({filesOnThisPage} files, {fileIndex}/{totalFileCount} total)");

                    // Read file attributes
                    var attributesResult = await ReadAsync(0x0240B9);
                    if (!attributesResult.Success || attributesResult.BinaryData == null)
                    {
                        RaiseDebugMessage("ReadEmbroideryFiles: Failed to read file attributes");
                        return null;
                    }

                    // Read file names
                    int nameLength = filesOnThisPage * 32;
                    RaiseDebugMessage($"ReadEmbroideryFiles: Reading {nameLength} bytes of file names from 0x0240D5");
                    var namesResult = await ReadMemoryBlockCheckedAsync(0x0240D5, nameLength);
                    if (!namesResult.Success || namesResult.BinaryData == null)
                    {
                        RaiseDebugMessage($"ReadEmbroideryFiles: Failed to read file names: {namesResult.ErrorMessage}");
                        return null;
                    }

                    // Parse each file on this page
                    for (int i = 0; i < filesOnThisPage; i++)
                    {
                        var file = new EmbroideryFile
                        {
                            FileId = fileIndex,
                            FileAttributes = attributesResult.BinaryData[i]
                        };

                        // Extract filename
                        int nameOffset = i * 32;
                        var nameBytes = new byte[32];
                        Array.Copy(namesResult.BinaryData, nameOffset, nameBytes, 0, 32);

                        StringBuilder nameBuilder = new StringBuilder();
                        for (int j = 0; j < nameBytes.Length; j++)
                        {
                            if (nameBytes[j] == 0x00)
                                break;
                            
                            if (nameBytes[j] >= 0x20 && nameBytes[j] <= 0x7E)
                            {
                                nameBuilder.Append((char)nameBytes[j]);
                            }
                        }
                        file.FileName = nameBuilder.ToString().Trim();

                        // Load preview data if requested
                        if (loadPreviews)
                        {
                            int previewAddress = 0x02452E0 + (0x22E * (pageIndex * 27 + i));
                            const int previewSize = 0x22E; // 558 bytes
                            
                            // First, get the sum of the preview data to check cache
                            RaiseDebugMessage($"ReadEmbroideryFiles: Getting checksum for preview of file {fileIndex} ({file.FileName})");
                            var sumResult = await SumCommandAsync(previewAddress, previewSize);
                            
                            if (sumResult.Success && sumResult.Response != null)
                            {
                                // Parse the checksum value
                                if (long.TryParse(sumResult.Response, System.Globalization.NumberStyles.HexNumber, null, out long checksumValue))
                                {
                                    // Build cache key: {ChecksumHex}~{Attributes:X2}~{FileName}
                                    string cacheKey = $"{checksumValue:X}~{file.FileAttributes:X2}~{file.FileName}";
                                    
                                    // Check if preview is in cache
                                    if (_previewCache.ContainsKey(cacheKey))
                                    {
                                        RaiseDebugMessage($"ReadEmbroideryFiles: Preview cache hit for file {fileIndex} ({file.FileName})");
                                        file.PreviewImageData = _previewCache[cacheKey];
                                    }
                                    else
                                    {
                                        // Not in cache - load the preview data
                                        RaiseDebugMessage($"ReadEmbroideryFiles: Loading preview data for file {fileIndex} ({file.FileName}) from 0x{previewAddress:X}");
                                        var previewResult = await ReadMemoryBlockAsync(previewAddress, previewSize);
                                        
                                        if (previewResult.Success && previewResult.BinaryData != null && previewResult.BinaryData.Length == previewSize)
                                        {
                                            // Cache the preview data
                                            _previewCache[cacheKey] = previewResult.BinaryData;
                                            file.PreviewImageData = previewResult.BinaryData;
                                            RaiseDebugMessage($"ReadEmbroideryFiles: Preview cached for file {fileIndex} ({file.FileName})");
                                        }
                                        else
                                        {
                                            RaiseDebugMessage($"ReadEmbroideryFiles: Failed to load preview data for file {fileIndex}: {previewResult.ErrorMessage}");
                                        }
                                    }
                                }
                                else
                                {
                                    RaiseDebugMessage($"ReadEmbroideryFiles: Failed to parse checksum for file {fileIndex}");
                                }
                            }
                            else
                            {
                                RaiseDebugMessage($"ReadEmbroideryFiles: Failed to get checksum for file {fileIndex}: {sumResult.ErrorMessage}");
                            }
                        }

                        RaiseDebugMessage($"ReadEmbroideryFiles: File {fileIndex}: {file.FileName} (attr: 0x{file.FileAttributes:X2})");

                        fileList.Add(file);
                        fileIndex++;
                    }

                    // Move to next page if needed
                    if (fileIndex < totalFileCount)
                    {
                        pageIndex++;
                        RaiseDebugMessage($"ReadEmbroideryFiles: Moving to page {pageIndex}");
                        var setNextPageResult = await SetArgument1Async((byte)pageIndex);
                        if (!setNextPageResult.Success)
                        {
                            RaiseDebugMessage($"ReadEmbroideryFiles: Failed to set next page: {setNextPageResult.ErrorMessage}");
                            return null;
                        }

                        var invokeFunc61Result = await InvokeFunctionAsync(0x0061);
                        if (!invokeFunc61Result.Success)
                        {
                            RaiseDebugMessage($"ReadEmbroideryFiles: Failed to invoke function 0x0061: {invokeFunc61Result.ErrorMessage}");
                            return null;
                        }
                    }
                }

                return fileList;
            }
            catch (Exception ex)
            {
                RaiseDebugMessage($"ReadEmbroideryFiles: Exception occurred: {ex.Message}");
                return null;
            }
            finally
            {
                // Always attempt cleanup, even if errors occurred
                if (sessionStarted)
                {
                    // Step 9: Invoke function 0x0101 for cleanup
                    RaiseDebugMessage("ReadEmbroideryFiles: Invoking cleanup function 0x0101");
                    try
                    {
                        var invokeFunc101Result = await InvokeFunctionAsync(0x0101);
                        if (!invokeFunc101Result.Success)
                        {
                            RaiseDebugMessage($"ReadEmbroideryFiles: Warning - Failed to invoke cleanup function 0x0101: {invokeFunc101Result.ErrorMessage}");
                        }
                    }
                    catch (Exception ex)
                    {
                        RaiseDebugMessage($"ReadEmbroideryFiles: Warning - Exception during cleanup function 0x0101: {ex.Message}");
                    }

                    // Step 10: Close embroidery session
                    RaiseDebugMessage("ReadEmbroideryFiles: Ending session");
                    try
                    {
                        var sessionEndResult = await SessionEndAsync();
                        if (!sessionEndResult.Success)
                        {
                            RaiseDebugMessage($"ReadEmbroideryFiles: Warning - Session end failed: {sessionEndResult.ErrorMessage}");
                        }
                    }
                    catch (Exception ex)
                    {
                        RaiseDebugMessage($"ReadEmbroideryFiles: Warning - Exception during session end: {ex.Message}");
                    }
                }

                if (fileList != null)
                {
                    RaiseDebugMessage($"ReadEmbroideryFiles: Complete - returning {fileList.Count} files");
                }
            }
        }

        /// <summary>
        /// Reads the embroidery file preview image from the specified storage location.
        /// Returns a 72x64 pixel black & white bitmap image (558 bytes, 0x22E bytes).
        /// Ensures cleanup (function 0x0101 and session end) occurs even if errors happen.
        /// </summary>
        /// <param name="location">Storage location to read from</param>
        /// <param name="FileId">The file ID (0-based) to get the preview for</param>
        /// <returns>Byte array containing the preview image data (558 bytes), or null if operation fails</returns>
        public async Task<byte[]?> ReadEmbroideryFilePreviewAsync(StorageLocation location, int FileId)
        {
            RaiseDebugMessage($"ReadEmbroideryFilePreview: Starting read for FileId {FileId} from {location}");
            
            // Step 1: Check connection
            if (State != ConnectionState.Connected)
            {
                RaiseDebugMessage("ReadEmbroideryFilePreview: Not connected");
                return null;
            }

            byte[]? previewData = null;
            bool sessionStarted = false;

            try
            {
                // Step 2: Ensure we're in Embroidery Mode
                RaiseDebugMessage("ReadEmbroideryFilePreview: Checking current session mode");
                var currentMode = await GetCurrentSessionModeAsync();
                if (currentMode == null)
                {
                    RaiseDebugMessage("ReadEmbroideryFilePreview: Failed to get session mode");
                    return null;
                }
                RaiseDebugMessage($"ReadEmbroideryFilePreview: Current mode is {currentMode}");

                // If in Sewing Machine mode, start embroidery session
                if (currentMode == SessionMode.SewingMachine)
                {
                    RaiseDebugMessage("ReadEmbroideryFilePreview: In Sewing Machine mode, starting embroidery session");
                    var sessionStartResult = await SessionStartAsync();
                    if (!sessionStartResult.Success)
                    {
                        RaiseDebugMessage($"ReadEmbroideryFilePreview: Session start failed: {sessionStartResult.ErrorMessage}");
                        return null;
                    }
                    sessionStarted = true;
                    RaiseDebugMessage("ReadEmbroideryFilePreview: Session start successful");
                }
                else
                {
                    RaiseDebugMessage("ReadEmbroideryFilePreview: Already in Embroidery Module mode");
                    sessionStarted = true;
                }

                // Step 3: Check PC Card if reading from PC Card
                if (location == StorageLocation.PCCard)
                {
                    RaiseDebugMessage("ReadEmbroideryFilePreview: Checking PC card status");
                    var pcCardReadResult = await ReadAsync(0xFFFED9);
                    
                    if (pcCardReadResult.Success && pcCardReadResult.BinaryData != null && pcCardReadResult.BinaryData.Length > 0)
                    {
                        bool pcCardInserted = (pcCardReadResult.BinaryData[0] & 0x01) == 0x01;
                        RaiseDebugMessage($"ReadEmbroideryFilePreview: PC card inserted: {pcCardInserted}");
                        if (!pcCardInserted)
                        {
                            RaiseDebugMessage("ReadEmbroideryFilePreview: No PC card present");
                            return null;
                        }
                    }
                    else
                    {
                        RaiseDebugMessage("ReadEmbroideryFilePreview: Failed to read PC card status");
                        return null;
                    }
                }

                // Step 4: Select storage source
                ushort storageFunction = location == StorageLocation.EmbroideryModuleMemory ? (ushort)0x00A1 : (ushort)0x0051;
                RaiseDebugMessage($"ReadEmbroideryFilePreview: Selecting storage source with function 0x{storageFunction:X4}");
                var selectStorageResult = await InvokeFunctionAsync(storageFunction);
                if (!selectStorageResult.Success)
                {
                    RaiseDebugMessage($"ReadEmbroideryFilePreview: Failed to select storage source: {selectStorageResult.ErrorMessage}");
                    return null;
                }
                RaiseDebugMessage("ReadEmbroideryFilePreview: Storage source selected successfully");

                // Step 5: Initialize reading
                RaiseDebugMessage("ReadEmbroideryFilePreview: Initializing file reading (function 0x0031 with args 0x01, 0x00)");
                var setArg2Result = await SetArgument2Async((byte)(FileId + 1));
                if (!setArg2Result.Success)
                {
                    RaiseDebugMessage($"ReadEmbroideryFilePreview: Failed to set argument 2: {setArg2Result.ErrorMessage}");
                    return null;
                }

                var setArg1Result = await SetArgument1Async(0x00);
                if (!setArg1Result.Success)
                {
                    RaiseDebugMessage($"ReadEmbroideryFilePreview: Failed to set argument 1: {setArg1Result.ErrorMessage}");
                    return null;
                }

                var invokeFunc31Result = await InvokeFunctionAsync((ushort)((FileId < 27) ? 0x0031 : 0x0061));
                if (!invokeFunc31Result.Success)
                {
                    RaiseDebugMessage($"ReadEmbroideryFilePreview: Failed to invoke function 0x0031: {invokeFunc31Result.ErrorMessage}");
                    return null;
                }
                RaiseDebugMessage("ReadEmbroideryFilePreview: Function 0x0031 invoked successfully");

                RaiseDebugMessage("ReadEmbroideryFilePreview: Invoking function 0x0021");
                var invokeFunc21Result = await InvokeFunctionAsync(0x0021);
                if (!invokeFunc21Result.Success)
                {
                    RaiseDebugMessage($"ReadEmbroideryFilePreview: Failed to invoke function 0x0021: {invokeFunc21Result.ErrorMessage}");
                    return null;
                }
                RaiseDebugMessage("ReadEmbroideryFilePreview: Function 0x0021 invoked successfully");

                // Step 6: Read file count and validate FileId
                RaiseDebugMessage("ReadEmbroideryFilePreview: Reading file count from 0x024080");
                var fileCountResult = await ReadAsync(0x024080);
                if (!fileCountResult.Success || fileCountResult.BinaryData == null || fileCountResult.BinaryData.Length == 0)
                {
                    RaiseDebugMessage("ReadEmbroideryFilePreview: Failed to read file count");
                    return null;
                }

                int totalFileCount = fileCountResult.BinaryData[0];
                RaiseDebugMessage($"ReadEmbroideryFilePreview: Total file count: {totalFileCount}");

                // Validate FileId is within range
                if (FileId < 0 || FileId >= totalFileCount)
                {
                    RaiseDebugMessage($"ReadEmbroideryFilePreview: FileId {FileId} is out of range [0, {totalFileCount - 1}]");
                    return null;
                }
                RaiseDebugMessage($"ReadEmbroideryFilePreview: FileId {FileId} is valid");

                // Step 7: Set initial page (required by protocol, even though we won't use the page data)
                RaiseDebugMessage("ReadEmbroideryFilePreview: Setting initial page to 0");
                var setPageResult = await SetArgument1Async(0);
                if (!setPageResult.Success)
                {
                    RaiseDebugMessage($"ReadEmbroideryFilePreview: Failed to set initial page: {setPageResult.ErrorMessage}");
                    return null;
                }

                // Step 8: Compute preview image location and read preview data
                int previewAddress = 0x02452E + (0x22E * (FileId % 27));
                RaiseDebugMessage($"ReadEmbroideryFilePreview: Computing preview address - base: 0x02452E0, offset: 0x{(0x22E * FileId):X}, address: 0x{previewAddress:X}");

                const int previewSize = 0x22E; // 558 bytes
                RaiseDebugMessage($"ReadEmbroideryFilePreview: Reading {previewSize} bytes from 0x{previewAddress:X}");
                
                var previewResult = await ReadMemoryBlockCheckedAsync(previewAddress, previewSize);
                if (!previewResult.Success || previewResult.BinaryData == null)
                {
                    RaiseDebugMessage($"ReadEmbroideryFilePreview: Failed to read preview data: {previewResult.ErrorMessage}");
                    return null;
                }
                RaiseDebugMessage($"ReadEmbroideryFilePreview: Successfully read {previewResult.BinaryData.Length} bytes of preview data");

                previewData = previewResult.BinaryData;
                return previewData;
            }
            catch (Exception ex)
            {
                RaiseDebugMessage($"ReadEmbroideryFilePreview: Exception occurred: {ex.Message}");
                return null;
            }
            finally
            {
                // Always attempt cleanup, even if errors occurred
                if (sessionStarted)
                {
                    // Step 9: Invoke function 0x0101 for cleanup
                    RaiseDebugMessage("ReadEmbroideryFilePreview: Invoking cleanup function 0x0101");
                    try
                    {
                        var invokeFunc101Result = await InvokeFunctionAsync(0x0101);
                        if (!invokeFunc101Result.Success)
                        {
                            RaiseDebugMessage($"ReadEmbroideryFilePreview: Warning - Failed to invoke cleanup function 0x0101: {invokeFunc101Result.ErrorMessage}");
                        }
                    }
                    catch (Exception ex)
                    {
                        RaiseDebugMessage($"ReadEmbroideryFilePreview: Warning - Exception during cleanup function 0x0101: {ex.Message}");
                    }

                    // Step 10: Close embroidery session
                    RaiseDebugMessage("ReadEmbroideryFilePreview: Ending session");
                    try
                    {
                        var sessionEndResult = await SessionEndAsync();
                        if (!sessionEndResult.Success)
                        {
                            RaiseDebugMessage($"ReadEmbroideryFilePreview: Warning - Session end failed: {sessionEndResult.ErrorMessage}");
                        }
                    }
                    catch (Exception ex)
                    {
                        RaiseDebugMessage($"ReadEmbroideryFilePreview: Warning - Exception during session end: {ex.Message}");
                    }
                }

                if (previewData != null)
                {
                    RaiseDebugMessage($"ReadEmbroideryFilePreview: Complete - returning {previewData.Length} bytes of preview image data");
                }
            }
        }

        /// <summary>
        /// Serializes the preview cache to a compressed byte array.
        /// Format: [int: entry count][repeated: int keyLength, string key, byte[] value (558 bytes)]
        /// The serialized data is then compressed using GZipStream.
        /// </summary>
        /// <returns>Compressed byte array containing the serialized cache, or empty array if cache is empty</returns>
        public async Task<byte[]> SerializeCacheAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Use MemoryStream to build uncompressed data
                    using (var uncompressedStream = new MemoryStream())
                    using (var writer = new BinaryWriter(uncompressedStream, Encoding.UTF8))
                    {
                        // Write entry count
                        writer.Write(_previewCache.Count);
                        
                        // Write each cache entry
                        foreach (var entry in _previewCache)
                        {
                            // Write key length and key
                            byte[] keyBytes = Encoding.UTF8.GetBytes(entry.Key);
                            writer.Write(keyBytes.Length);
                            writer.Write(keyBytes);
                            
                            // Write value (preview data - should always be 558 bytes)
                            writer.Write(entry.Value.Length);
                            writer.Write(entry.Value);
                        }

                        // Get uncompressed data
                        byte[] uncompressedData = uncompressedStream.ToArray();
                        
                        // Compress the data using GZipStream
                        using (var compressedStream = new MemoryStream())
                        {
                            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Compress, leaveOpen: true))
                            {
                                gzipStream.Write(uncompressedData, 0, uncompressedData.Length);
                            }
                            
                            return compressedStream.ToArray();
                        }
                    }
                }
                catch (Exception ex)
                {
                    RaiseDebugMessage($"SerializeCache: Error during serialization: {ex.Message}");
                    return Array.Empty<byte>();
                }
            });
        }

        /// <summary>
        /// Deserializes a compressed byte array into the preview cache.
        /// The byte array is decompressed using GZipStream, then parsed back into the cache dictionary.
        /// Validates that each preview is exactly 558 bytes (0x22E).
        /// </summary>
        /// <param name="compressedData">Compressed byte array containing serialized cache data</param>
        /// <returns>True if deserialization succeeded, false otherwise</returns>
        public async Task<bool> DeserializeCacheAsync(byte[] compressedData)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (compressedData == null || compressedData.Length == 0)
                    {
                        RaiseDebugMessage("DeserializeCache: No data to deserialize");
                        return false;
                    }

                    // Decompress the data using GZipStream
                    using (var compressedStream = new MemoryStream(compressedData))
                    using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                    using (var uncompressedStream = new MemoryStream())
                    {
                        gzipStream.CopyTo(uncompressedStream);
                        byte[] uncompressedData = uncompressedStream.ToArray();

                        // Parse the uncompressed data
                        using (var reader = new BinaryReader(new MemoryStream(uncompressedData), Encoding.UTF8))
                        {
                            // Read entry count
                            int entryCount = reader.ReadInt32();
                            RaiseDebugMessage($"DeserializeCache: Reading {entryCount} cache entries");

                            // Clear existing cache
                            _previewCache.Clear();

                            // Read each cache entry
                            for (int i = 0; i < entryCount; i++)
                            {
                                // Read key
                                int keyLength = reader.ReadInt32();
                                byte[] keyBytes = reader.ReadBytes(keyLength);
                                string key = Encoding.UTF8.GetString(keyBytes);

                                // Read value
                                int valueLength = reader.ReadInt32();
                                if (valueLength != 0x22E) // 558 bytes
                                {
                                    RaiseDebugMessage($"DeserializeCache: Warning - Entry {i} has invalid preview size {valueLength}, expected 558");
                                    continue;
                                }

                                byte[] value = reader.ReadBytes(valueLength);
                                _previewCache[key] = value;
                            }

                            RaiseDebugMessage($"DeserializeCache: Successfully loaded {_previewCache.Count} cache entries");
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    RaiseDebugMessage($"DeserializeCache: Error during deserialization: {ex.Message}");
                    _previewCache.Clear();
                    return false;
                }
            });
        }

        public void Dispose()
        {
            Close();
            _processingCts?.Dispose();
            _commandSemaphore?.Dispose();
        }
    }
}
