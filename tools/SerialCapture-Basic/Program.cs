using System.IO.Ports;
using System.Text;

class SerialCapture
{
    private static SerialPort? port1;
    private static SerialPort? port2;
    private static StreamWriter? logWriter;
    private static readonly object logLock = new object();
    private static string? serial1Name;
    private static string? serial2Name;

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

            serial1Name = config["serial1"];
            serial2Name = config["serial2"];
            string baudRate = config["baud"];
            string logFile = config["file"];

            Console.WriteLine($"Configuration:");
            Console.WriteLine($"  Serial Port 1: {serial1Name}");
            Console.WriteLine($"  Serial Port 2: {serial2Name}");
            Console.WriteLine($"  Baud Rate: {baudRate}");
            Console.WriteLine($"  Log File: {logFile}");
            Console.WriteLine();

            // Initialize log file
            logWriter = new StreamWriter(logFile, false, Encoding.UTF8);
            logWriter.AutoFlush = true;
            WriteLog($"Serial Capture Session Started - {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            WriteLog($"Port 1: {serial1Name}, Port 2: {serial2Name}, Baud: {baudRate}");
            WriteLog("=".PadRight(80, '='));

            // Initialize serial ports
            port1 = new SerialPort(serial1Name, int.Parse(baudRate))
            {
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                ReadTimeout = 500,
                WriteTimeout = 500
            };

            port2 = new SerialPort(serial2Name, int.Parse(baudRate))
            {
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                ReadTimeout = 500,
                WriteTimeout = 500
            };

            // Set up data received event handlers
            port1.DataReceived += (sender, e) => OnDataReceived(port1, port2, serial1Name, serial2Name);
            port2.DataReceived += (sender, e) => OnDataReceived(port2, port1, serial2Name, serial1Name);

            // Open ports
            Console.WriteLine($"Opening {serial1Name}...");
            port1.Open();
            Console.WriteLine($"Opening {serial2Name}...");
            port2.Open();

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
            string[] requiredKeys = { "serial1", "serial2", "baud", "file" };
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
        try
        {
            int bytesToRead = sourcePort.BytesToRead;
            if (bytesToRead == 0)
                return;

            byte[] buffer = new byte[bytesToRead];
            int bytesRead = sourcePort.Read(buffer, 0, bytesToRead);

            if (bytesRead > 0)
            {
                // Forward data to the other port
                destinationPort.Write(buffer, 0, bytesRead);

                // Log the data
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string hexData = BitConverter.ToString(buffer, 0, bytesRead).Replace("-", " ");
                string asciiData = GetAsciiRepresentation(buffer, bytesRead);

                string logEntry = $"[{timestamp}] {sourceName} → {destName}: {hexData} ({asciiData})";
                WriteLog(logEntry);

                // Also display on console with color coding
                lock (logLock)
                {
                    Console.ForegroundColor = sourceName == serial1Name ? ConsoleColor.Cyan : ConsoleColor.Yellow;
                    Console.Write($"[{timestamp}] ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write($"{sourceName} → {destName}: ");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"{hexData} ({asciiData})");
                    Console.ResetColor();
                }
            }
        }
        catch (Exception ex)
        {
            WriteLog($"ERROR in data transfer: {ex.Message}");
            Console.WriteLine($"ERROR: {ex.Message}");
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

    private static void Cleanup()
    {
        try
        {
            if (port1 != null && port1.IsOpen)
            {
                Console.WriteLine($"Closing {serial1Name}...");
                port1.Close();
                port1.Dispose();
            }

            if (port2 != null && port2.IsOpen)
            {
                Console.WriteLine($"Closing {serial2Name}...");
                port2.Close();
                port2.Dispose();
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
