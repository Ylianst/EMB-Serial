using System.IO.Ports;
using System.Text;

class SerialCapture
{
    private static SerialPort? port1;
    private static SerialPort? port2;
    private static StreamWriter? logWriter;
    private static readonly object logLock = new object();
    private static readonly object dataProcessingLock = new object();
    private static string? softwarePort;
    private static string? machinePort;
    private static int currentBaudRate = 19200;
    
    // Command parsing state
    private static StringBuilder softwareCommandBuffer = new StringBuilder();
    private static StringBuilder machineResponseBuffer = new StringBuilder();
    private static string? currentCommand = null;
    private static int expectedResponseBytes = 0;
    private static bool waitingForResponse = false;
    private static DateTime commandStartTime;
    private static CommandType currentCommandType = CommandType.Unknown;
    private static DateTime lastSoftwareCharTime = DateTime.Now;
    private static System.Threading.Timer? commandTimeoutTimer = null;
    
    enum CommandType
    {
        Unknown,
        Read,           // R + 6 hex chars -> 65 chars response (64 hex + O)
        LargeRead,      // N + 6 hex chars -> 257 chars response (256 data + O)
        Write,          // W + 6 hex chars + data + ? -> echo only
        Session,        // TrME, TrMEYQ, TrMEJ05 -> echo or special handling
        Reset,          // RF? -> echo
        LCommand,       // L + 12 hex chars -> variable response ending in O
        Other           // EBYQ, etc -> echo or special response
    }
    
    // Forwarding control - wait for 'R' from Software before forwarding
    private static bool forwardingEnabled = false;
    
    // Filter NULL characters (0x00) from Software to Machine
    private static bool filterNullCharacters = true;
    
    // Debug mode - show individual character forwarding
    private static bool debugMode = false;
    
    // Show protocol errors on console
    private static bool showErrors = false;

    static async Task Main(string[] args)
    {
        Console.WriteLine("Serial Port High-Level Command Monitor");
        Console.WriteLine("======================================");
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
            
            // Read baud rate from config
            if (config.ContainsKey("Baud"))
            {
                if (int.TryParse(config["Baud"], out int configBaud))
                {
                    currentBaudRate = configBaud;
                }
            }
            
            // Read debug mode from config
            if (config.ContainsKey("Debug"))
            {
                if (int.TryParse(config["Debug"], out int debugValue))
                {
                    debugMode = (debugValue == 1);
                }
            }
            
            // Read errors mode from config
            if (config.ContainsKey("Errors"))
            {
                if (int.TryParse(config["Errors"], out int errorsValue))
                {
                    showErrors = (errorsValue == 1);
                }
            }
            
            string logFile = config["file"];

            Console.WriteLine($"Configuration:");
            Console.WriteLine($"  Software: {softwarePort}");
            Console.WriteLine($"  Machine: {machinePort}");
            Console.WriteLine($"  Initial Baud Rate: {currentBaudRate}");
            Console.WriteLine($"  Debug Mode: {(debugMode ? "Enabled" : "Disabled")}");
            Console.WriteLine($"  Show Errors: {(showErrors ? "Enabled" : "Disabled")}");
            Console.WriteLine($"  Log File: {logFile}");
            Console.WriteLine();

            // Initialize log file
            logWriter = new StreamWriter(logFile, false, Encoding.UTF8);
            logWriter.AutoFlush = true;
            WriteLog($"Serial Capture Session - {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            WriteLog($"Software: {softwarePort}, Machine: {machinePort}, Initial Baud: {currentBaudRate}");
            WriteLog("");

            // Initialize serial ports
            port1 = new SerialPort(softwarePort, currentBaudRate)
            {
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                ReadTimeout = 500,
                WriteTimeout = 500
            };

            port2 = new SerialPort(machinePort, currentBaudRate)
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
            Console.WriteLine("Monitoring high-level commands... (CTRL+C to exit)\n");

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
            string[] requiredKeys = { "Software", "Machine", "file" };
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
                                LogMessage("*** Forwarding ENABLED - 'R' received from Software ***", ConsoleColor.Magenta);
                                break;
                            }
                        }
                        
                        // If 'R' was found, forward 'R' and everything after it
                        if (rIndex >= 0)
                        {
                            byte[] dataToForward = PrepareDataForForwarding(buffer, rIndex, bytesRead, sourceName);
                            if (dataToForward.Length > 0)
                            {
                                destinationPort.Write(dataToForward, 0, dataToForward.Length);
                                ProcessBytes(dataToForward, sourceName);
                            }
                        }
                        return;
                    }
                    
                    // Don't forward Machine data until forwarding is enabled
                    if (!forwardingEnabled && sourceName == "Machine")
                    {
                        return;
                    }
                    
                    // Normal forwarding
                    if (forwardingEnabled)
                    {
                        byte[] dataToForward = PrepareDataForForwarding(buffer, 0, bytesRead, sourceName);
                        if (dataToForward.Length > 0)
                        {
                            destinationPort.Write(dataToForward, 0, dataToForward.Length);
                            ProcessBytes(dataToForward, sourceName);
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

    private static byte[] PrepareDataForForwarding(byte[] buffer, int startIndex, int totalBytes, string sourceName)
    {
        int lengthToForward = totalBytes - startIndex;
        
        if (filterNullCharacters && sourceName == "Software")
        {
            List<byte> filtered = new List<byte>();
            for (int i = startIndex; i < totalBytes; i++)
            {
                if (buffer[i] != 0x00)
                {
                    filtered.Add(buffer[i]);
                }
            }
            return filtered.ToArray();
        }
        else
        {
            byte[] result = new byte[lengthToForward];
            Array.Copy(buffer, startIndex, result, 0, lengthToForward);
            return result;
        }
    }

    private static void ProcessBytes(byte[] data, string sourceName)
    {
        foreach (byte b in data)
        {
            ProcessByte(b, sourceName);
        }
    }

    private static void ProcessByte(byte dataByte, string sourceName)
    {
        char c = (char)dataByte;
        
        // Debug output - print each character with timestamp (only if debug mode is enabled)
        if (debugMode)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string charDisplay = (dataByte >= 32 && dataByte <= 126) ? c.ToString() : $"[0x{dataByte:X2}]";
            Console.ForegroundColor = sourceName == "Software" ? ConsoleColor.DarkCyan : ConsoleColor.DarkYellow;
            Console.Write($"[{timestamp}] {sourceName[0]}: {charDisplay} ");
            Console.ResetColor();
            WriteLog($"[{timestamp}] {sourceName}: {dataByte:X2} ({charDisplay})");
        }
        
        if (sourceName == "Software")
        {
            // Accumulate software command
            softwareCommandBuffer.Append(c);
            lastSoftwareCharTime = DateTime.Now;
            
            // Cancel any existing timeout timer
            commandTimeoutTimer?.Dispose();
            
            // Check if command is complete
            string cmd = softwareCommandBuffer.ToString();
            
            // Detect complete commands
            if (IsCommandComplete(cmd))
            {
                // For TrME or TrMEY, wait a bit to see if more chars follow
                if (cmd == "TrME" || cmd == "TrMEY")
                {
                    // Set up a timeout - if no more chars come in 300ms, treat as complete
                    commandTimeoutTimer = new System.Threading.Timer(_ =>
                    {
                        lock (dataProcessingLock)
                        {
                            string currentCmd = softwareCommandBuffer.ToString();
                            // Check if still just "TrME" or "TrMEY" and enough time has passed
                            if ((currentCmd == "TrME" || currentCmd == "TrMEY") && 
                                (DateTime.Now - lastSoftwareCharTime).TotalMilliseconds >= 300)
                            {
                                CompleteCommand(currentCmd);
                            }
                        }
                    }, null, 300, Timeout.Infinite);
                }
                else
                {
                    CompleteCommand(cmd);
                }
            }
        }
        else // Machine
        {
            // Check for error responses that can come at any time
            // BUT: Don't treat '?' as error if we're waiting for a Write command response (which ends with ?)
            bool isErrorChar = (c == 'Q' || c == '?' || c == '!');
            bool isWriteCommandEcho = waitingForResponse && currentCommandType == CommandType.Write && c == '?';
            
            if (isErrorChar && !isWriteCommandEcho && !waitingForResponse)
            {
                // Error from machine - reset everything
                string timestamp2 = DateTime.Now.ToString("HH:mm:ss.fff");
                string errorMsg = $"\n[{timestamp2}] << ERROR: Machine sent '{c}' - resetting state";
                
                // Only log and show errors if showErrors is enabled
                if (showErrors)
                {
                    WriteLog(errorMsg);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(errorMsg);
                    Console.ResetColor();
                    Console.WriteLine();
                    WriteLog("");
                }
                
                // Reset software command buffer (software will retry)
                softwareCommandBuffer.Clear();
                
                // Reset response waiting state
                machineResponseBuffer.Clear();
                currentCommand = null;
                waitingForResponse = false;
                expectedResponseBytes = 0;
                currentCommandType = CommandType.Unknown;
                
                // Cancel any pending timeout
                commandTimeoutTimer?.Dispose();
                commandTimeoutTimer = null;
                
                return;
            }
            
            if (waitingForResponse)
            {
                machineResponseBuffer.Append(c);
                
                // Check if we have a complete response
                if (IsResponseComplete(machineResponseBuffer.ToString(), currentCommandType, expectedResponseBytes))
                {
                    string response = machineResponseBuffer.ToString();
                    DisplayCommand(currentCommand!, response, currentCommandType);
                    
                    // Handle baud rate change after TrMEJ05
                    if (currentCommand == "TrMEJ05")
                    {
                        Task.Run(() => ChangeBaudRate(57600));
                    }
                    
                    // Reset state
                    machineResponseBuffer.Clear();
                    currentCommand = null;
                    waitingForResponse = false;
                    expectedResponseBytes = 0;
                    currentCommandType = CommandType.Unknown;
                }
            }
            else
            {
                // Unsolicited machine data (like "BOSN", "BOS", etc.)
                machineResponseBuffer.Append(c);
                
                // Check for known unsolicited messages
                string buffer = machineResponseBuffer.ToString();
                if (buffer.EndsWith("BOSN") || buffer.EndsWith("BOS"))
                {
                    LogMessage($"<< Machine: {buffer}", ConsoleColor.Yellow);
                    machineResponseBuffer.Clear();
                }
            }
        }
    }

    private static void CompleteCommand(string cmd)
    {
        // Cancel any timeout timer
        commandTimeoutTimer?.Dispose();
        commandTimeoutTimer = null;
        
        currentCommand = cmd;
        commandStartTime = DateTime.Now;
        currentCommandType = DetectCommandType(cmd);
        expectedResponseBytes = GetExpectedResponseLength(cmd, currentCommandType);
        waitingForResponse = true;
        softwareCommandBuffer.Clear();
        
        // Check for baud rate change command
        if (cmd == "TrMEJ05")
        {
            // This command will trigger a baud rate change
            // We'll handle it after receiving the echo
        }
    }

    private static bool IsCommandComplete(string cmd)
    {
        if (string.IsNullOrEmpty(cmd))
            return false;
            
        // Read command: R + 6 hex digits
        if (cmd.StartsWith("R") && cmd.Length == 7)
        {
            return IsHexString(cmd.Substring(1, 6));
        }
        
        // Large Read command: N + 6 hex digits
        if (cmd.StartsWith("N") && cmd.Length == 7)
        {
            return IsHexString(cmd.Substring(1, 6));
        }
        
        // Write command: W + 6 hex + data + ?
        if (cmd.StartsWith("W") && cmd.Contains("?"))
        {
            return true;
        }
        
        // L command: L + 12 hex digits
        if (cmd.StartsWith("L") && cmd.Length == 13)
        {
            return IsHexString(cmd.Substring(1, 12));
        }
        
        // Session commands - check longer commands first
        if (cmd == "TrMEJ05" || cmd == "TrMEYQ")
        {
            return true;
        }
        
        // TrMEY - wait for Q to see if it's TrMEYQ
        if (cmd == "TrMEY")
        {
            return true;
        }
        
        // TrME by itself - needs timeout to confirm it's not TrMEJ05 or TrMEYQ
        if (cmd == "TrME")
        {
            return true;
        }
        
        // Other session commands
        if (cmd == "RF?" || cmd == "EBYQ")
        {
            return true;
        }
        
        return false;
    }

    private static CommandType DetectCommandType(string cmd)
    {
        if (cmd.StartsWith("R") && cmd.Length == 7)
            return CommandType.Read;
        if (cmd.StartsWith("N") && cmd.Length == 7)
            return CommandType.LargeRead;
        if (cmd.StartsWith("W"))
            return CommandType.Write;
        if (cmd.StartsWith("L"))
            return CommandType.LCommand;
        if (cmd == "RF?" || cmd.StartsWith("TrME"))
            return CommandType.Session;
        return CommandType.Other;
    }

    private static int GetExpectedResponseLength(string cmd, CommandType type)
    {
        switch (type)
        {
            case CommandType.Read:
                return cmd.Length + 64 + 1; // command echo + 64 hex chars + O
            case CommandType.LargeRead:
                return cmd.Length + 256 + 1; // command echo + 256 data bytes + O
            case CommandType.Write:
                return cmd.Length; // Echo only
            case CommandType.Session:
                if (cmd == "TrMEYQ")
                    return cmd.Length + 1; // Echo + O
                return cmd.Length; // Echo only
            case CommandType.Other:
                if (cmd == "EBYQ")
                    return cmd.Length + 1; // Echo + O
                return cmd.Length;
            case CommandType.LCommand:
                return -1; // Variable length, ends with O
            default:
                return cmd.Length;
        }
    }

    private static bool IsResponseComplete(string response, CommandType type, int expectedLength)
    {
        if (string.IsNullOrEmpty(response))
            return false;
            
        // Check for error responses
        if (response == "Q" || response == "?" || response == "!")
            return true;
            
        // For variable length responses (L command), check for 'O' terminator
        if (type == CommandType.LCommand)
        {
            return response.EndsWith("O") && response.Length > 5;
        }
        
        // For fixed length responses
        if (expectedLength > 0)
        {
            return response.Length >= expectedLength;
        }
        
        return false;
    }

    private static void DisplayCommand(string command, string response, CommandType type)
    {
        // Check for errors
        if (response == "Q" || response == "?" || response == "!")
        {
            // Only log and show errors if showErrors is enabled
            if (showErrors)
            {
                string errorMsg = $"{command} --> ERROR: Machine responded with '{response}'";
                
                WriteLog(errorMsg);
                
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(errorMsg);
                Console.ResetColor();
            }
            return;
        }
        
        // Format output based on command type
        if (type == CommandType.Read || type == CommandType.LargeRead)
        {
            // Response format: [command echo][data][O]
            // Extract only the data part
            string data = "";
            
            if (response.Length > command.Length)
            {
                // Remove command echo from the beginning
                data = response.Substring(command.Length);
                
                // Remove 'O' terminator from the end
                if (data.EndsWith("O"))
                {
                    data = data.Substring(0, data.Length - 1);
                }
            }
            
            if (type == CommandType.LargeRead)
            {
                // For N command, show both ASCII and HEX on separate indented lines
                string ascii = GetPrintableAscii(data);
                string hex = BytesToHex(Encoding.ASCII.GetBytes(data));
                
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"{command} -->");
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"   ASCII: {ascii}");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"   HEX: {hex}");
                Console.ResetColor();
                
                WriteLog($"{command} -->");
                WriteLog($"   ASCII: {ascii}");
                WriteLog($"   HEX: {hex}");
            }
            else
            {
                // For R command, data is hex-encoded, so decode it
                // HEX: show the raw hex characters with spaces
                // ASCII: decode the hex to show actual values
                string hexDisplay = FormatHexWithSpaces(data); // Format hex with spaces
                string asciiDisplay = DecodeHexString(data); // Decode hex to ASCII
                
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"{command} -->");
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"   ASCII: {asciiDisplay}");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"   HEX: {hexDisplay}");
                Console.ResetColor();
                
                WriteLog($"{command} -->");
                WriteLog($"   ASCII: {asciiDisplay}");
                WriteLog($"   HEX: {hexDisplay}");
            }
        }
        else if (type == CommandType.Write)
        {
            // Parse write command to extract address and data
            // Format: W + 6 hex (address) + data + ?
            string writeInfo = "Acknowledged";
            if (command.Length > 7)
            {
                string address = command.Substring(1, 6);
                string dataStr = command.Substring(7, command.Length - 8); // Remove W, address, and ?
                writeInfo = $"Write {dataStr} to {address}";
            }
            
            string msg = $"{command} --> {writeInfo}";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(command);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($" --> {writeInfo}");
            Console.ResetColor();
            WriteLog(msg);
        }
        else if (type == CommandType.Session)
        {
            // Session commands
            string msg = $"{command} --> Acknowledged";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(command);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(" --> Acknowledged");
            Console.ResetColor();
            WriteLog(msg);
        }
        else if (type == CommandType.LCommand)
        {
            // L command response
            string data = response.Substring(command.Length);
            if (data.EndsWith("O"))
            {
                data = data.Substring(0, data.Length - 1);
            }
            
            string msg = $"{command} --> {data}";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(command);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($" --> {data}");
            Console.ResetColor();
            WriteLog(msg);
        }
        else
        {
            // Other commands
            string extra = "";
            if (response.Length > command.Length)
            {
                extra = response.Substring(command.Length);
            }
            
            string responseText = string.IsNullOrEmpty(extra) ? "Acknowledged" : extra;
            string msg = $"{command} --> {responseText}";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(command);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($" --> {responseText}");
            Console.ResetColor();
            WriteLog(msg);
        }
    }

    private static string GetPrintableAscii(string data)
    {
        StringBuilder sb = new StringBuilder();
        foreach (char c in data)
        {
            sb.Append(c >= 32 && c <= 126 ? c : '.');
        }
        return sb.ToString();
    }

    private static string BytesToHex(byte[] bytes)
    {
        return BitConverter.ToString(bytes).Replace("-", " ");
    }

    private static string DecodeHexString(string hexString)
    {
        // Decode hex-encoded string (e.g., "41424344" -> "ABCD")
        // Each pair of hex characters represents one byte
        if (hexString.Length % 2 != 0)
        {
            return "[Invalid hex string - odd length]";
        }

        StringBuilder result = new StringBuilder();
        for (int i = 0; i < hexString.Length; i += 2)
        {
            try
            {
                string hexByte = hexString.Substring(i, 2);
                byte byteValue = Convert.ToByte(hexByte, 16);
                // Show printable characters, or '.' for non-printable
                char c = (char)byteValue;
                result.Append(c >= 32 && c <= 126 ? c : '.');
            }
            catch
            {
                result.Append('?');
            }
        }
        return result.ToString();
    }

    private static string FormatHexWithSpaces(string hexString)
    {
        // Format hex string with spaces between bytes (e.g., "01020304" -> "01 02 03 04")
        if (hexString.Length % 2 != 0)
        {
            return hexString; // Return as-is if odd length
        }

        StringBuilder result = new StringBuilder();
        for (int i = 0; i < hexString.Length; i += 2)
        {
            if (i > 0)
            {
                result.Append(' ');
            }
            result.Append(hexString.Substring(i, 2));
        }
        return result.ToString();
    }

    private static bool IsHexString(string str)
    {
        foreach (char c in str)
        {
            if (!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
            {
                return false;
            }
        }
        return true;
    }

    private static void ChangeBaudRate(int newBaudRate)
    {
        lock (dataProcessingLock)
        {
            try
            {
                // Small delay to allow the command to complete
                Thread.Sleep(100);
                
                LogMessage($"*** Changing baud rate from {currentBaudRate} to {newBaudRate} ***", ConsoleColor.Magenta);
                
                // Close ports
                if (port1 != null && port1.IsOpen)
                {
                    port1.Close();
                }
                if (port2 != null && port2.IsOpen)
                {
                    port2.Close();
                }
                
                // Update baud rate
                currentBaudRate = newBaudRate;
                
                // Reopen ports with new baud rate
                if (port1 != null)
                {
                    port1.BaudRate = newBaudRate;
                    port1.Open();
                }
                if (port2 != null)
                {
                    port2.BaudRate = newBaudRate;
                    port2.Open();
                }
                
                LogMessage($"*** Baud rate changed successfully to {newBaudRate} ***", ConsoleColor.Magenta);
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                WriteLog($"ERROR changing baud rate: {ex.Message}");
                Console.WriteLine($"ERROR changing baud rate: {ex.Message}");
            }
        }
    }

    private static void LogMessage(string message, ConsoleColor color)
    {
        lock (logLock)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
            WriteLog(message);
        }
    }

    private static void WriteLog(string message)
    {
        logWriter?.WriteLine(message);
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

            if (logWriter != null)
            {
                WriteLog("");
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
