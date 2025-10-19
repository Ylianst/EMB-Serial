using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;

class MemoryDumper
{
    private SerialPort? serialPort;
    private string outputFile = "";
    private int startAddress = 0;

    static void Main(string[] args)
    {
        var dumper = new MemoryDumper();
        dumper.Run();
    }

    private void Run()
    {
        try
        {
            // Read configuration
            if (!ReadConfig())
            {
                Console.WriteLine("Failed to read config.ini");
                return;
            }

            Console.WriteLine($"Configuration loaded:");
            Console.WriteLine($"  Serial Port: {serialPort!.PortName}");
            Console.WriteLine($"  Baud Rate: {serialPort.BaudRate}");
            Console.WriteLine($"  Output File: {outputFile}");
            Console.WriteLine($"  Start Address: 0x{startAddress:X6}");
            Console.WriteLine();

            // Open serial port
            serialPort.Open();
            Console.WriteLine("Serial port opened successfully");

            // Start memory dump
            DumpMemory();

            Console.WriteLine("\nMemory dump completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            if (serialPort?.IsOpen == true)
            {
                serialPort.Close();
            }
        }
    }

    private bool ReadConfig()
    {
        try
        {
            string configPath = "config.ini";
            if (!File.Exists(configPath))
            {
                Console.WriteLine($"Config file not found: {configPath}");
                return false;
            }

            string[] lines = File.ReadAllLines(configPath);
            string portName = "COM1";
            int baudRate = 57600;

            foreach (string line in lines)
            {
                string[] parts = line.Split('=');
                if (parts.Length != 2) continue;

                string key = parts[0].Trim().ToLower();
                string value = parts[1].Trim();

                switch (key)
                {
                    case "serial":
                        portName = value;
                        break;
                    case "baud":
                        baudRate = int.Parse(value);
                        break;
                    case "file":
                        outputFile = value;
                        break;
                    case "start":
                        startAddress = Convert.ToInt32(value, 16);
                        break;
                }
            }

            serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
            serialPort.Handshake = Handshake.None;
            serialPort.ReadTimeout = 5000;
            serialPort.WriteTimeout = 5000;

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading config: {ex.Message}");
            return false;
        }
    }

    private void DumpMemory()
    {
        using (FileStream fs = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
        {
            int currentAddress = startAddress;
            int maxAddress = 0xFFFFFF;
            int blockSize = 256;
            int totalBytesRead = 0;

            while (currentAddress <= maxAddress)
            {
                Console.Write($"\rReading address 0x{currentAddress:X6}...");

                try
                {
                    byte[] data = ReadLargeBlock(currentAddress);
                    
                    if (data == null || data.Length == 0)
                    {
                        Console.WriteLine($"\nFailed to read at address 0x{currentAddress:X6}. Stopping.");
                        break;
                    }

                    fs.Write(data, 0, data.Length);
                    totalBytesRead += data.Length;

                    currentAddress += blockSize;

                    // Check if we've reached the end
                    if (currentAddress > maxAddress)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nError at address 0x{currentAddress:X6}: {ex.Message}");
                    break;
                }
            }

            Console.WriteLine($"\nTotal bytes read: {totalBytesRead} (0x{totalBytesRead:X})");
            Console.WriteLine($"Data saved to: {outputFile}");
        }
    }

    private byte[]? ReadLargeBlock(int address)
    {
        // Build the Large Read command: N followed by 6 hex characters
        string command = $"N{address:X6}";

        // Send command one character at a time and wait for echo
        if (!SendCommandWithEcho(command))
        {
            return null;
        }

        // Read the response: 256 bytes of binary data + 'O' terminator
        byte[] buffer = new byte[257]; // 256 data bytes + 1 terminator
        int totalRead = 0;

        while (totalRead < 257)
        {
            int bytesRead = serialPort!.Read(buffer, totalRead, 257 - totalRead);
            if (bytesRead == 0)
            {
                Console.WriteLine("\nTimeout waiting for data");
                return null;
            }
            totalRead += bytesRead;
        }

        // Verify the terminator is 'O' (0x4F)
        if (buffer[256] != 0x4F)
        {
            Console.WriteLine($"\nInvalid terminator: expected 'O' (0x4F), got 0x{buffer[256]:X2}");
            return null;
        }

        // Return only the data, without the terminator
        byte[] data = new byte[256];
        Array.Copy(buffer, 0, data, 0, 256);
        return data;
    }

    private bool SendCommandWithEcho(string command)
    {
        foreach (char c in command)
        {
            // Send one character
            serialPort!.Write(new char[] { c }, 0, 1);

            // Wait for echo
            try
            {
                int echo = serialPort.ReadByte();
                
                // Check for error responses
                if (echo == 'Q' || echo == '?' || echo == '!')
                {
                    Console.WriteLine($"\nError response from machine: {(char)echo}");
                    return false;
                }

                // Verify echo matches what we sent
                if (echo != c)
                {
                    Console.WriteLine($"\nEcho mismatch: sent '{c}' (0x{(int)c:X2}), received '{(char)echo}' (0x{echo:X2})");
                    return false;
                }
            }
            catch (TimeoutException)
            {
                Console.WriteLine($"\nTimeout waiting for echo of '{c}'");
                return false;
            }
        }

        return true;
    }
}
