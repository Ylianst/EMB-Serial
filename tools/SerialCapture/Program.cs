using System.IO.Ports;
using System.Text;

class SerialCapture
{
    private static SerialPort? port1;
    private static SerialPort? port2;
    private static StreamWriter? logWriter;
    private static StreamWriter? hLogWriter;
    private static readonly object logLock = new object();
    private static readonly object dataProcessingLock = new object();
    private static string? softwarePort;
    private static string? machinePort;
    private static bool logHex = false;
    
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
            const int baudRate = 57600;  // Hard-coded baud rate
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

            // Initialize log file
            logWriter = new StreamWriter(logFile, false, Encoding.UTF8);
            logWriter.AutoFlush = true;
            WriteLog($"Serial Capture Session Started - {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            WriteLog($"Software: {softwarePort}, Machine: {machinePort}, Baud: {baudRate}");
            WriteLog("=".PadRight(80, '='));

            // Initialize high-level log file
            hLogWriter = new StreamWriter(hLogFile, false, Encoding.UTF8);
            hLogWriter.AutoFlush = true;
            WriteHLog($"High-Level Protocol Analysis - {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            WriteHLog($"Software: {softwarePort}, Machine: {machinePort}, Baud: {baudRate}");
            WriteHLog("=".PadRight(80, '='));

            // Initialize serial ports
            port1 = new SerialPort(softwarePort, baudRate)
            {
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                ReadTimeout = 500,
                WriteTimeout = 500
            };

            port2 = new SerialPort(machinePort, baudRate)
            {
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                ReadTimeout = 500,
                WriteTimeout = 500
            };

            // Set up data received event handlers
            port1.DataReceived += (sender, e) => OnDataReceived(port1, port2, "Software", "Machine");
            port2.DataReceived += (sender, e) => OnDataReceived(port2, port1, "Machine", "Software");

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
            Cleanup();
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

    private static void OnDataReceived(SerialPort sourcePort, SerialPort destinationPort, string sourceName, string destName)
    {
        // Use lock to ensure only one thread processes data at a time
        // This prevents race conditions in the protocol state machine
        lock (dataProcessingLock)
        {
            try
            {
                int bytesToRead = sourcePort.BytesToRead;
                if (bytesToRead == 0)
                    return;

                byte[] buffer = new byte[bytesToRead];
                int bytesRead = sourcePort.Read(buffer, 0, bytesToRead);

                if (bytesRead > 0)
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
                                Console.ForegroundColor = ConsoleColor.Magenta;
                                Console.WriteLine($"*** Forwarding ENABLED - 'R' received from Software ***");
                                Console.ResetColor();
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

                            // Process each byte for high-level protocol analysis
                            for (int i = 0; i < lengthToForward; i++)
                            {
                                ProcessHighLevelProtocol(dataToForward[i], sourceName);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog($"ERROR in data transfer: {ex.Message}");
                Console.WriteLine($"ERROR: {ex.Message}");
            }
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
            
            WriteHLog(logEntry);
            
            accumulatedData.Clear();
        }
        
        currentDataType = newDataType;
        accumulationStartTime = null;
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

            // Flush any remaining high-level protocol data
            lock (logLock)
            {
                FlushAccumulatedData();
            }

            if (hLogWriter != null)
            {
                WriteHLog("=".PadRight(80, '='));
                WriteHLog($"Session Ended - {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                hLogWriter.Close();
                hLogWriter.Dispose();
                Console.WriteLine("High-level log file closed.");
            }

            if (logWriter != null)
            {
                WriteLog("=".PadRight(80, '='));
                WriteLog($"Session Ended - {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                logWriter.Close();
                logWriter.Dispose();
                Console.WriteLine("Log file closed.");
            }

            Console.WriteLine("Cleanup complete.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during cleanup: {ex.Message}");
        }
    }
}
