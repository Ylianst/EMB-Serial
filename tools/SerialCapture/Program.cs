using System.IO.Ports;
using System.Text;

class SerialCapture
{
    private static SerialPort? port1;
    private static SerialPort? port2;
    private static StreamWriter? logWriter;
    private static StreamWriter? hLogWriter;
    private static readonly object logLock = new object();
    private static string? softwarePort;
    private static string? machinePort;
    private static bool logHex = false;
    private static int currentBaudRate = 57600;
    
    // Per-port buffers for blocking reads
    private static byte[] port1Buffer = new byte[4096];
    private static byte[] port2Buffer = new byte[4096];
    
    // Thread control
    private static bool running = true;
    private static Thread? port1Thread;
    private static Thread? port2Thread;
    
    // Baud rate switch detection
    private static readonly byte[] baudSwitchSequence = Encoding.ASCII.GetBytes("TrMEJ05");
    private static List<byte> machineDataBuffer = new List<byte>();
    private static readonly int maxBufferSize = 100; // Keep buffer reasonable size
    
    // High-level protocol analysis state
    private static byte? commandBuffer = null;
    private static List<byte> accumulatedData = new List<byte>();
    private static string? currentDataType = null;
    private static DateTime? accumulationStartTime = null;
    
    // Forwarding control - wait for 'R' from Software before forwarding
    private static bool forwardingEnabled = false;
    
    // Filter NULL characters (0x00) from Software to Machine
    // Set to false to disable NULL filtering
    private static bool filterNullCharacters = true;

    static async Task Main(string[] args)
    {
        Console.WriteLine("Serial Port Man-in-the-Middle Capture Tool");
        Console.WriteLine("===========================================");
        Console.WriteLine("Press CTRL+C to exit\n");

        // Set up CTRL+C handler
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("\n\nShutting down...");
            running = false;
            Cleanup();
            Environment.Exit(0);
        };

        try
        {
            // Read configuration
            var config = ReadConfig("config.ini");
            if (config == null)
            {
                Console.WriteLine("ERROR: Failed to read config.ini");
                return;
            }

            softwarePort = config["Software"];
            machinePort = config["Machine"];
            
            // Read baud rate from config, default to 57600 if not specified
            int baudRate = 57600;
            if (config.ContainsKey("Baud") && int.TryParse(config["Baud"], out int configBaudRate))
            {
                baudRate = configBaudRate;
            }
            
            // Set the current baud rate
            currentBaudRate = baudRate;
            
            string logFile = config["file"];
            string hLogFile = config["hfile"];
            
            // Read loghex setting (optional, defaults to false)
            if (config.ContainsKey("loghex"))
            {
                logHex = config["loghex"].ToLower() == "true";
            }

            Console.WriteLine($"Configuration:");
            Console.WriteLine($"  Software: {softwarePort}");
            Console.WriteLine($"  Machine: {machinePort}");
            Console.WriteLine($"  Baud Rate: {baudRate}");
            Console.WriteLine($"  Log File: {logFile}");
            Console.WriteLine($"  High-Level Log File: {hLogFile}");
            Console.WriteLine($"  Log Hex: {logHex}");
            Console.WriteLine();

            // Initialize log files with thread-safe access
            lock (logLock)
            {
                logWriter = new StreamWriter(logFile, false, Encoding.UTF8);
                logWriter.AutoFlush = true;
                logWriter.WriteLine($"Serial Capture Session Started - {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                logWriter.WriteLine($"Software: {softwarePort}, Machine: {machinePort}, Baud: {baudRate}");
                logWriter.WriteLine("=".PadRight(80, '='));

                hLogWriter = new StreamWriter(hLogFile, false, Encoding.UTF8);
                hLogWriter.AutoFlush = true;
                hLogWriter.WriteLine($"High-Level Protocol Analysis - {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                hLogWriter.WriteLine($"Software: {softwarePort}, Machine: {machinePort}, Baud: {baudRate}");
                hLogWriter.WriteLine("=".PadRight(80, '='));
            }

            // Initialize serial ports with infinite read timeout
            port1 = new SerialPort(softwarePort, baudRate)
            {
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                ReadTimeout = Timeout.Infinite,  // Infinite timeout for blocking reads
                WriteTimeout = 500
            };

            port2 = new SerialPort(machinePort, baudRate)
            {
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                ReadTimeout = Timeout.Infinite,  // Infinite timeout for blocking reads
                WriteTimeout = 500
            };

            // Open ports
            Console.WriteLine($"Opening Software port ({softwarePort})...");
            port1.Open();
            Console.WriteLine($"Opening Machine port ({machinePort})...");
            port2.Open();

            // Flush any existing data in the serial port buffers before starting
            Console.WriteLine("\nFlushing existing data from serial ports...");
            FlushSerialPort(port1, softwarePort);
            FlushSerialPort(port2, machinePort);

            Console.WriteLine("\nBoth ports opened successfully!");
            Console.WriteLine("Starting reader threads with blocking reads...\n");

            // Start dedicated reader threads for each port
            port1Thread = new Thread(() => ReadPortThread(port1, port2, port1Buffer, "Software", "Machine"));
            port1Thread.Name = "Software Port Reader";
            port1Thread.IsBackground = false;
            port1Thread.Start();

            port2Thread = new Thread(() => ReadPortThread(port2, port1, port2Buffer, "Machine", "Software"));
            port2Thread.Name = "Machine Port Reader";
            port2Thread.IsBackground = false;
            port2Thread.Start();

            Console.WriteLine("Listening for serial data... (CTRL+C to exit)\n");

            // Keep the application running
            await Task.Delay(Timeout.Infinite);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nERROR: {ex.Message}");
            if (ex is UnauthorizedAccessException)
            {
                Console.WriteLine("  - Check if another application is using the serial ports");
                Console.WriteLine("  - Ensure you have permission to access the serial ports");
            }
            else if (ex is IOException)
            {
                Console.WriteLine("  - Verify the serial port names in config.ini are correct");
                Console.WriteLine("  - Check if the ports exist on your system");
            }
        }
        finally
        {
            running = false;
            Cleanup();
        }
    }

    private static void ReadPortThread(SerialPort sourcePort, SerialPort destinationPort, byte[] buffer, string sourceName, string destName)
    {
        Console.WriteLine($"[{sourceName}] Reader thread started");
        
        try
        {
            while (running && sourcePort.IsOpen)
            {
                try
                {
                    // Blocking read - will wait indefinitely until data arrives
                    int bytesRead = sourcePort.Read(buffer, 0, buffer.Length);
                    
                    if (bytesRead > 0 && running)
                    {
                        ProcessData(sourcePort, destinationPort, buffer, bytesRead, sourceName, destName);
                    }
                }
                catch (InvalidOperationException)
                {
                    // Port was closed
                    break;
                }
                catch (IOException ex)
                {
                    if (running)
                    {
                        WriteLog($"ERROR in {sourceName} reader: {ex.Message}");
                        Console.WriteLine($"ERROR in {sourceName} reader: {ex.Message}");
                    }
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            if (running)
            {
                WriteLog($"FATAL ERROR in {sourceName} reader thread: {ex.Message}");
                Console.WriteLine($"FATAL ERROR in {sourceName} reader thread: {ex.Message}");
            }
        }
        finally
        {
            Console.WriteLine($"[{sourceName}] Reader thread exiting");
        }
    }

    private static void ProcessData(SerialPort sourcePort, SerialPort destinationPort, byte[] buffer, int bytesRead, string sourceName, string destName)
    {
        // Check if we need to enable forwarding
        if (!forwardingEnabled && sourceName == "Software")
        {
            // Look for 'R' (0x52) in the buffer from Software
            int rIndex = -1;
            for (int i = 0; i < bytesRead; i++)
            {
                if (buffer[i] == 'R')
                {
                    rIndex = i;
                    forwardingEnabled = true;
                    lock (logLock)
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine($"*** Forwarding ENABLED - 'R' received from Software ***");
                        Console.ResetColor();
                    }
                    WriteLog($"*** Forwarding ENABLED - 'R' received from Software ***");
                    WriteHLog($"*** Forwarding ENABLED - 'R' received from Software ***");
                    break;
                }
            }
            
            // If 'R' was found, forward 'R' and everything after it
            if (rIndex >= 0)
            {
                // Filter NULL characters from Software to Machine if enabled
                byte[] dataToForward;
                int startIndex = rIndex;
                int lengthToForward = bytesRead - rIndex;
                
                if (filterNullCharacters)
                {
                    // Remove NULL (0x00) characters from the data
                    List<byte> filtered = new List<byte>();
                    for (int i = rIndex; i < bytesRead; i++)
                    {
                        if (buffer[i] != 0x00)
                        {
                            filtered.Add(buffer[i]);
                        }
                    }
                    dataToForward = filtered.ToArray();
                    lengthToForward = dataToForward.Length;
                }
                else
                {
                    dataToForward = buffer;
                    startIndex = rIndex;
                }
                
                // Forward the data
                if (lengthToForward > 0)
                {
                    if (filterNullCharacters)
                    {
                        destinationPort.Write(dataToForward, 0, lengthToForward);
                    }
                    else
                    {
                        destinationPort.Write(dataToForward, startIndex, lengthToForward);
                    }
                }
                
                // Log the forwarded data (what was actually sent)
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                if (lengthToForward > 0)
                {
                    byte[] logData = filterNullCharacters ? dataToForward : buffer.Skip(rIndex).Take(lengthToForward).ToArray();
                    string fwdHexData = BitConverter.ToString(logData).Replace("-", " ");
                    string fwdAsciiData = GetAsciiRepresentation(logData, logData.Length);
                    string fwdLogEntry = $"[{timestamp}] {sourceName} → {destName}: {fwdHexData} ({fwdAsciiData})";
                    WriteLog(fwdLogEntry);
                    
                    lock (logLock)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write($"[{timestamp}] ");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write($"{sourceName} → {destName}: ");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"{fwdHexData} ({fwdAsciiData})");
                        Console.ResetColor();
                    }
                    
                    // Process forwarded bytes for protocol analysis
                    for (int i = 0; i < logData.Length; i++)
                    {
                        ProcessHighLevelProtocol(logData[i], sourceName);
                    }
                }
            }
            
            return; // Done processing this batch from Software
        }
        
        // Don't forward Machine data until forwarding is enabled
        if (!forwardingEnabled && sourceName == "Machine")
        {
            return; // Silently ignore - don't forward or log
        }
        
        // Normal forwarding (after 'R' has been received)
        if (forwardingEnabled)
        {
            // Prepare data to forward
            byte[] dataToForward = buffer;
            int lengthToForward = bytesRead;
            
            // Filter NULL characters from Software to Machine if enabled
            if (filterNullCharacters && sourceName == "Software")
            {
                // Remove NULL (0x00) characters from the data
                List<byte> filtered = new List<byte>();
                for (int i = 0; i < bytesRead; i++)
                {
                    if (buffer[i] != 0x00)
                    {
                        filtered.Add(buffer[i]);
                    }
                }
                dataToForward = filtered.ToArray();
                lengthToForward = dataToForward.Length;
            }
            
            // Forward data to the other port
            if (lengthToForward > 0)
            {
                destinationPort.Write(dataToForward, 0, lengthToForward);
            }

            // Log the data (what was actually forwarded)
            if (lengthToForward > 0)
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string hexData = BitConverter.ToString(dataToForward, 0, lengthToForward).Replace("-", " ");
                string asciiData = GetAsciiRepresentation(dataToForward, lengthToForward);

                string logEntry = $"[{timestamp}] {sourceName} → {destName}: {hexData} ({asciiData})";
                WriteLog(logEntry);

                // Also display on console with color coding
                lock (logLock)
                {
                    Console.ForegroundColor = sourceName == "Software" ? ConsoleColor.Cyan : ConsoleColor.Yellow;
                    Console.Write($"[{timestamp}] ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write($"{sourceName} → {destName}: ");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"{hexData} ({asciiData})");
                    Console.ResetColor();
                }

                // Check for baud rate switch sequence if data is from Machine
                if (sourceName == "Machine")
                {
                    CheckForBaudRateSwitch(dataToForward, lengthToForward);
                }

                // Process each byte for high-level protocol analysis
                for (int i = 0; i < lengthToForward; i++)
                {
                    ProcessHighLevelProtocol(dataToForward[i], sourceName);
                }
            }
        }
    }

    private static Dictionary<string, string>? ReadConfig(string filename)
    {
        try
        {
            if (!File.Exists(filename))
            {
                Console.WriteLine($"ERROR: Configuration file '{filename}' not found");
                return null;
            }

            var config = new Dictionary<string, string>();
            var lines = File.ReadAllLines(filename);

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#") || trimmedLine.StartsWith(";"))
                    continue;

                var parts = trimmedLine.Split('=', 2);
                if (parts.Length == 2)
                {
                    config[parts[0].Trim()] = parts[1].Trim();
                }
            }

            // Validate required keys
            string[] requiredKeys = { "Software", "Machine", "file", "hfile" };
            foreach (var key in requiredKeys)
            {
                if (!config.ContainsKey(key))
                {
                    Console.WriteLine($"ERROR: Missing required configuration key: {key}");
                    return null;
                }
            }

            return config;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR reading config file: {ex.Message}");
            return null;
        }
    }

    private static string GetAsciiRepresentation(byte[] buffer, int length)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < length; i++)
        {
            char c = (char)buffer[i];
            // Show printable ASCII characters, otherwise use '.'
            sb.Append(c >= 32 && c <= 126 ? c : '.');
        }
        return sb.ToString();
    }

    private static void WriteLog(string message)
    {
        lock (logLock)
        {
            logWriter?.WriteLine(message);
        }
    }

    private static void WriteHLog(string message)
    {
        lock (logLock)
        {
            hLogWriter?.WriteLine(message);
        }
    }

    private static void ProcessHighLevelProtocol(byte dataByte, string sourceName)
    {
        lock (logLock)
        {
            DateTime now = DateTime.Now;
            
            if (sourceName == "Software")
            {
                // Data from Software
                if (commandBuffer == null)
                {
                    // Buffer is empty, put character in buffer
                    commandBuffer = dataByte;
                    accumulationStartTime = now;
                }
                else
                {
                    // Buffer has a character, classify it as "Software Data"
                    FlushAccumulatedData("Software Data");
                    
                    // Put new character in buffer
                    commandBuffer = dataByte;
                    accumulationStartTime = now;
                }
            }
            else // sourceName == "Machine"
            {
                // Data from Machine
                if (commandBuffer == null)
                {
                    // Buffer is empty, this is "Machine Data"
                    if (currentDataType != "Machine Data")
                    {
                        FlushAccumulatedData("Machine Data");
                    }
                    if (accumulationStartTime == null)
                    {
                        accumulationStartTime = now;
                    }
                    accumulatedData.Add(dataByte);
                    currentDataType = "Machine Data";
                }
                else
                {
                    // Buffer has a character
                    if (dataByte == commandBuffer.Value)
                    {
                        // Machine echoes the buffer, this is "Software Command"
                        if (currentDataType != "Software Command")
                        {
                            FlushAccumulatedData("Software Command");
                        }
                        if (accumulationStartTime == null)
                        {
                            accumulationStartTime = now;
                        }
                        accumulatedData.Add(commandBuffer.Value);
                        currentDataType = "Software Command";
                        
                        // Empty the buffer
                        commandBuffer = null;
                    }
                    else
                    {
                        // Machine sends non-matching data, buffer content is "Error Data"
                        if (currentDataType != "Error Data")
                        {
                            FlushAccumulatedData("Error Data");
                        }
                        if (accumulationStartTime == null)
                        {
                            accumulationStartTime = now;
                        }
                        accumulatedData.Add(commandBuffer.Value);
                        currentDataType = "Error Data";
                        
                        // Empty the buffer
                        commandBuffer = null;
                    }
                }
            }
        }
    }

    private static void FlushAccumulatedData(string? newDataType = null)
    {
        // Must be called within logLock
        if (accumulatedData.Count > 0 && currentDataType != null && accumulationStartTime != null)
        {
            string timestamp = accumulationStartTime.Value.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string asciiData = GetAsciiRepresentation(accumulatedData.ToArray(), accumulatedData.Count);
            
            // Pad data type to 17 characters for alignment ("Software Command:" is longest at 17 chars)
            string paddedDataType = currentDataType.PadRight(17) + ":";
            
            string logEntry;
            if (logHex)
            {
                string hexData = BitConverter.ToString(accumulatedData.ToArray()).Replace("-", " ");
                logEntry = $"[{timestamp}] {paddedDataType} {hexData} ({asciiData})";
            }
            else
            {
                logEntry = $"[{timestamp}] {paddedDataType} {asciiData}";
            }
            
            hLogWriter?.WriteLine(logEntry);
            
            accumulatedData.Clear();
        }
        
        currentDataType = newDataType;
        accumulationStartTime = null;
    }

    private static void CheckForBaudRateSwitch(byte[] data, int length)
    {
        // Add new bytes to the buffer
        for (int i = 0; i < length; i++)
        {
            machineDataBuffer.Add(data[i]);
            
            // Keep buffer size reasonable
            if (machineDataBuffer.Count > maxBufferSize)
            {
                machineDataBuffer.RemoveAt(0);
            }
        }
        
        // Check if the buffer contains the baud rate switch sequence
        if (machineDataBuffer.Count >= baudSwitchSequence.Length)
        {
            // Check for the sequence in the buffer
            for (int i = 0; i <= machineDataBuffer.Count - baudSwitchSequence.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < baudSwitchSequence.Length; j++)
                {
                    if (machineDataBuffer[i + j] != baudSwitchSequence[j])
                    {
                        found = false;
                        break;
                    }
                }
                
                if (found)
                {
                    // Sequence detected!
                    string sequenceStr = Encoding.ASCII.GetString(baudSwitchSequence);
                    lock (logLock)
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine($"\n*** Baud rate switch sequence detected: \"{sequenceStr}\" ***");
                        Console.ResetColor();
                    }
                    WriteLog($"*** Baud rate switch sequence detected: \"{sequenceStr}\" ***");
                    WriteHLog($"*** Baud rate switch sequence detected: \"{sequenceStr}\" ***");
                    
                    // Switch to 57600 baud if not already at that rate
                    if (currentBaudRate != 57600)
                    {
                        SwitchBaudRate(57600);
                    }
                    else
                    {
                        lock (logLock)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("*** Already at 57600 baud - no action needed ***\n");
                            Console.ResetColor();
                        }
                        WriteLog("*** Already at 57600 baud - no action needed ***");
                        WriteHLog("*** Already at 57600 baud - no action needed ***");
                    }
                    
                    // Clear the buffer after detection
                    machineDataBuffer.Clear();
                    return;
                }
            }
        }
    }
    
    private static void SwitchBaudRate(int newBaudRate)
    {
        try
        {
            lock (logLock)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"*** Switching baud rate from {currentBaudRate} to {newBaudRate} ***");
                Console.ResetColor();
            }
            WriteLog($"*** Switching baud rate from {currentBaudRate} to {newBaudRate} ***");
            WriteHLog($"*** Switching baud rate from {currentBaudRate} to {newBaudRate} ***");
            
            // Close both ports (this will cause the reader threads to exit their loops)
            if (port1 != null && port1.IsOpen)
            {
                Console.WriteLine($"  Closing Software port ({softwarePort})...");
                port1.Close();
            }
            
            if (port2 != null && port2.IsOpen)
            {
                Console.WriteLine($"  Closing Machine port ({machinePort})...");
                port2.Close();
            }
            
            // Wait for reader threads to exit
            Console.WriteLine("  Waiting for reader threads to exit...");
            port1Thread?.Join(1000);
            port2Thread?.Join(1000);
            
            // Wait a moment for ports to fully close
            Thread.Sleep(100);
            
            // Reopen ports with new baud rate
            Console.WriteLine($"  Reopening Software port ({softwarePort}) at {newBaudRate} baud...");
            port1 = new SerialPort(softwarePort!, newBaudRate)
            {
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                ReadTimeout = Timeout.Infinite,
                WriteTimeout = 500
            };
            port1.Open();
            
            Console.WriteLine($"  Reopening Machine port ({machinePort}) at {newBaudRate} baud...");
            port2 = new SerialPort(machinePort!, newBaudRate)
            {
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                ReadTimeout = Timeout.Infinite,
                WriteTimeout = 500
            };
            port2.Open();
            
            // Restart reader threads
            Console.WriteLine("  Restarting reader threads...");
            port1Thread = new Thread(() => ReadPortThread(port1, port2, port1Buffer, "Software", "Machine"));
            port1Thread.Name = "Software Port Reader";
            port1Thread.IsBackground = false;
            port1Thread.Start();

            port2Thread = new Thread(() => ReadPortThread(port2, port1, port2Buffer, "Machine", "Software"));
            port2Thread.Name = "Machine Port Reader";
            port2Thread.IsBackground = false;
            port2Thread.Start();
            
            // Update current baud rate
            currentBaudRate = newBaudRate;
            
            lock (logLock)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"*** Baud rate switch completed successfully - now at {newBaudRate} ***\n");
                Console.ResetColor();
            }
            WriteLog($"*** Baud rate switch completed successfully - now at {newBaudRate} ***");
            WriteHLog($"*** Baud rate switch completed successfully - now at {newBaudRate} ***");
        }
        catch (Exception ex)
        {
            lock (logLock)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"*** ERROR during baud rate switch: {ex.Message} ***\n");
                Console.ResetColor();
            }
            WriteLog($"*** ERROR during baud rate switch: {ex.Message} ***");
            WriteHLog($"*** ERROR during baud rate switch: {ex.Message} ***");
        }
    }

    private static void FlushSerialPort(SerialPort port, string portName)
    {
        try
        {
            int bytesToRead = port.BytesToRead;
            if (bytesToRead > 0)
            {
                byte[] buffer = new byte[bytesToRead];
                int bytesRead = port.Read(buffer, 0, bytesToRead);
                Console.WriteLine($"  Flushed {bytesRead} bytes from {portName}");
            }
            else
            {
                Console.WriteLine($"  No data to flush from {portName}");
            }
            
            // Also discard any data in input/output buffers
            port.DiscardInBuffer();
            port.DiscardOutBuffer();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Warning: Error flushing {portName}: {ex.Message}");
        }
    }

    private static void Cleanup()
    {
        try
        {
            running = false;
            
            if (port1 != null && port1.IsOpen)
            {
                Console.WriteLine($"Closing Software port ({softwarePort})...");
                port1.Close();
                port1.Dispose();
            }

            if (port2 != null && port2.IsOpen)
            {
                Console.WriteLine($"Closing Machine port ({machinePort})...");
                port2.Close();
                port2.Dispose();
            }

            // Wait for threads to exit
            Console.WriteLine("Waiting for reader threads to exit...");
            port1Thread?.Join(2000);
            port2Thread?.Join(2000);

            // Flush any remaining high-level protocol data
            lock (logLock)
            {
                FlushAccumulatedData();

                if (hLogWriter != null)
                {
                    hLogWriter.WriteLine("=".PadRight(80, '='));
                    hLogWriter.WriteLine($"Session Ended - {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                    hLogWriter.Close();
                    hLogWriter.Dispose();
                    Console.WriteLine("High-level log file closed.");
                }

                if (logWriter != null)
                {
                    logWriter.WriteLine("=".PadRight(80, '='));
                    logWriter.WriteLine($"Session Ended - {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                    logWriter.Close();
                    logWriter.Dispose();
                    Console.WriteLine("Log file closed.");
                }
            }

            Console.WriteLine("Cleanup complete.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during cleanup: {ex.Message}");
        }
    }
}
