using System;
using System.Text;
using System.Threading.Tasks;
using Bernina.SerialStack;

/// <summary>
/// Example program demonstrating how to use the SerialStack class
/// </summary>
class ExampleUsage
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Bernina SerialStack Example");
        Console.WriteLine("===========================\n");

        // Create the serial stack with the COM port
        using var stack = new SerialStack("COM3"); // Change to your COM port

        // Subscribe to connection state changes
        stack.ConnectionStateChanged += (sender, e) =>
        {
            Console.WriteLine($"Connection State: {e.OldState} -> {e.NewState}");
            if (!string.IsNullOrEmpty(e.Message))
            {
                Console.WriteLine($"  Message: {e.Message}");
            }
        };

        // Subscribe to command completion events
        stack.CommandCompleted += (sender, e) =>
        {
            Console.WriteLine($"Command Completed: {e.Command}");
            Console.WriteLine($"  Success: {e.Success}");
            if (e.Success && !string.IsNullOrEmpty(e.Response))
            {
                Console.WriteLine($"  Response: {GetPrintableString(e.Response)}");
            }
            else if (!e.Success && !string.IsNullOrEmpty(e.ErrorMessage))
            {
                Console.WriteLine($"  Error: {e.ErrorMessage}");
            }
        };

        try
        {
            // Open connection (will auto-detect baud rate)
            Console.WriteLine("Connecting to machine...");
            bool connected = await stack.OpenAsync();

            if (!connected)
            {
                Console.WriteLine("Failed to connect to machine!");
                return;
            }

            Console.WriteLine($"Connected successfully at {stack.BaudRate} baud\n");

            // Example 1: Read machine firmware version (address 0x200100)
            Console.WriteLine("Example 1: Reading machine firmware version...");
            var result = await stack.ReadAsync(0x200100);
            if (result.Success)
            {
                string ascii = GetPrintableString(result.Response ?? "");
                Console.WriteLine($"  Firmware: {ascii}\n");
            }
            else
            {
                Console.WriteLine($"  Error: {result.ErrorMessage}\n");
            }

            // Example 2: Large Read - read 256 bytes from address 0x0240F5
            Console.WriteLine("Example 2: Performing large read (256 bytes)...");
            var largeResult = await stack.LargeReadAsync(0x0240F5);
            if (largeResult.Success)
            {
                string ascii = GetPrintableString(largeResult.Response ?? "");
                Console.WriteLine($"  Data: {ascii}\n");
            }
            else
            {
                Console.WriteLine($"  Error: {largeResult.ErrorMessage}\n");
            }

            // Example 3: Write data to an address
            Console.WriteLine("Example 3: Writing data to address 0x0201E1...");
            byte[] dataToWrite = { 0x01 };
            var writeResult = await stack.WriteAsync(0x0201E1, dataToWrite);
            if (writeResult.Success)
            {
                Console.WriteLine("  Write successful!");
                
                // Verify the write by reading back
                Console.WriteLine("  Verifying write...");
                var verifyResult = await stack.ReadAsync(0x0201E1);
                if (verifyResult.Success && verifyResult.BinaryData != null)
                {
                    Console.WriteLine($"  Verified value: 0x{verifyResult.BinaryData[0]:X2}\n");
                }
            }
            else
            {
                Console.WriteLine($"  Error: {writeResult.ErrorMessage}\n");
            }

            // Example 4: L command
            Console.WriteLine("Example 4: Sending L command...");
            var lResult = await stack.LCommandAsync("0240D5000360");
            if (lResult.Success)
            {
                Console.WriteLine($"  L command response: {lResult.Response}\n");
            }
            else
            {
                Console.WriteLine($"  Error: {lResult.ErrorMessage}\n");
            }

            // Example 5: Sending multiple commands in quick succession (queue demonstration)
            Console.WriteLine("Example 5: Queuing multiple read commands...");
            var task1 = stack.ReadAsync(0x200100);
            var task2 = stack.ReadAsync(0xFFFED9);
            var task3 = stack.ReadAsync(0x200120);
            
            // Wait for all to complete
            await Task.WhenAll(task1, task2, task3);
            
            Console.WriteLine("  All queued commands completed!");
            Console.WriteLine($"    Command 1: {(task1.Result.Success ? "Success" : "Failed")}");
            Console.WriteLine($"    Command 2: {(task2.Result.Success ? "Success" : "Failed")}");
            Console.WriteLine($"    Command 3: {(task3.Result.Success ? "Success" : "Failed")}\n");

            // Example 6: Change baud rate to 57600
            Console.WriteLine("Example 6: Changing baud rate to 57600...");
            Console.WriteLine($"  Current baud rate: {stack.BaudRate}");
            bool baudChanged = await stack.ChangeTo57600BaudAsync();
            if (baudChanged)
            {
                Console.WriteLine($"  Successfully switched to: {stack.BaudRate} baud\n");
            }
            else
            {
                Console.WriteLine("  Failed to change baud rate or already at 57600\n");
            }

            // Example 7: Send custom command
            Console.WriteLine("Example 7: Sending custom RF? reset command...");
            var resetResult = await stack.SendCommandAsync("RF?");
            if (resetResult.Success)
            {
                Console.WriteLine("  Reset command successful!\n");
            }
            else
            {
                Console.WriteLine($"  Error: {resetResult.ErrorMessage}\n");
            }

            // Close the connection
            Console.WriteLine("Closing connection...");
            stack.Close();
            Console.WriteLine("Connection closed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    /// <summary>
    /// Converts a string to a printable ASCII representation
    /// </summary>
    private static string GetPrintableString(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return "";
        }

        StringBuilder sb = new StringBuilder();
        foreach (char c in input)
        {
            if (c >= 32 && c <= 126)
            {
                sb.Append(c);
            }
            else if (c == '\r')
            {
                sb.Append("\\r");
            }
            else if (c == '\n')
            {
                sb.Append("\\n");
            }
            else if (c == '\t')
            {
                sb.Append("\\t");
            }
            else
            {
                sb.Append('.');
            }
        }
        return sb.ToString();
    }
}
