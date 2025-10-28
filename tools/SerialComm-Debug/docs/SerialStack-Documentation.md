# SerialStack Documentation

## Overview

`SerialStack` is a high-level C# class that provides an easy-to-use interface for communicating with Bernina machines over serial ports. It abstracts away the complexity of the low-level serial protocol, handles automatic baud rate detection, manages command queuing, and provides automatic error recovery.

## Features

- ✅ **Automatic Baud Rate Detection**: Tests multiple baud rates (19200, 57600, 115200, 4800) and connects at the correct one
- ✅ **Command Queuing**: Queue multiple commands even while the stack is busy
- ✅ **Error Recovery**: Automatically sends `RF?` reset command on protocol errors
- ✅ **Connection State Management**: Track connection state with events
- ✅ **Thread-Safe**: Safe to use from multiple threads
- ✅ **Async/Await Support**: Modern async API for all operations
- ✅ **High-Level Commands**: Simple methods for Read, LargeRead, Write, and L commands
- ✅ **Event-Driven**: Subscribe to connection state changes and command completions

## Installation

Simply include `SerialStack.cs` in your C# project. The class requires:
- .NET 8.0 or later
- `System.IO.Ports` package (automatically included in .NET)

## Quick Start

```csharp
using Bernina.SerialStack;

// Create the stack
using var stack = new SerialStack("COM3");

// Open connection (auto-detects baud rate)
bool connected = await stack.OpenAsync();

if (connected)
{
    // Read machine firmware version
    var result = await stack.ReadAsync(0x200100);
    
    if (result.Success)
    {
        Console.WriteLine($"Firmware: {result.Response}");
    }
    
    // Close when done
    stack.Close();
}
```

## API Reference

### Constructor

```csharp
public SerialStack(string portName)
```

Creates a new `SerialStack` instance.

**Parameters:**
- `portName`: The COM port name (e.g., "COM3", "COM7")

**Example:**
```csharp
var stack = new SerialStack("COM3");
```

---

### Properties

#### `State`
```csharp
public ConnectionState State { get; }
```

Gets the current connection state.

**Values:**
- `Disconnected`: Not connected
- `Connecting`: Connection attempt in progress
- `Connected`: Successfully connected
- `Error`: Connection error occurred

---

#### `BaudRate`
```csharp
public int BaudRate { get; }
```

Gets the current baud rate (19200, 57600, 115200, or 4800).

---

#### `IsConnected`
```csharp
public bool IsConnected { get; }
```

Returns `true` if currently connected, `false` otherwise.

---

### Methods

#### `OpenAsync()`
```csharp
public async Task<bool> OpenAsync()
```

Opens the connection and auto-detects the correct baud rate by sending `RF?` at different speeds.

**Returns:** `true` if connection successful, `false` otherwise

**Example:**
```csharp
if (await stack.OpenAsync())
{
    Console.WriteLine($"Connected at {stack.BaudRate} baud");
}
```

---

#### `Close()`
```csharp
public void Close()
```

Closes the connection and stops command processing.

**Example:**
```csharp
stack.Close();
```

---

#### `ReadAsync()`
```csharp
public async Task<CommandResult> ReadAsync(int address)
```

Sends a Read command (`R` + 6 hex chars) and returns 32 bytes of hex-encoded data.

**Parameters:**
- `address`: Memory address to read from (0x000000 to 0xFFFFFF)

**Returns:** `CommandResult` with response data

**Example:**
```csharp
var result = await stack.ReadAsync(0x200100);
if (result.Success)
{
    Console.WriteLine($"Data: {result.Response}");
    // Access binary data
    byte[] bytes = result.BinaryData;
}
```

---

#### `LargeReadAsync()`
```csharp
public async Task<CommandResult> LargeReadAsync(int address)
```

Sends a Large Read command (`N` + 6 hex chars) and returns 256 bytes of binary data.

**Parameters:**
- `address`: Memory address to read from

**Returns:** `CommandResult` with 256 bytes of data

**Example:**
```csharp
var result = await stack.LargeReadAsync(0x0240F5);
if (result.Success)
{
    byte[] data = result.BinaryData; // 256 bytes
}
```

---

#### `WriteAsync()`
```csharp
public async Task<CommandResult> WriteAsync(int address, byte[] data)
```

Sends a Write command (`W` + 6 hex chars + hex data + `?`).

**Parameters:**
- `address`: Memory address to write to
- `data`: Byte array to write (will be hex-encoded)

**Returns:** `CommandResult` indicating success/failure

**Example:**
```csharp
byte[] dataToWrite = { 0x01, 0x02, 0x03 };
var result = await stack.WriteAsync(0x0201E1, dataToWrite);

if (result.Success)
{
    Console.WriteLine("Write successful!");
}
```

---

#### `LCommandAsync()`
```csharp
public async Task<CommandResult> LCommandAsync(string parameters)
```

Sends an L command (`L` + 12 hex chars).

**Parameters:**
- `parameters`: Exactly 12 hex characters

**Returns:** `CommandResult` with variable-length response

**Example:**
```csharp
var result = await stack.LCommandAsync("0240D5000360");
if (result.Success)
{
    Console.WriteLine($"Response: {result.Response}");
}
```

---

#### `SendCommandAsync()`
```csharp
public async Task<CommandResult> SendCommandAsync(string command)
```

Sends a custom command string.

**Parameters:**
- `command`: The command to send (e.g., "RF?", "TrME")

**Returns:** `CommandResult` with response

**Example:**
```csharp
var result = await stack.SendCommandAsync("RF?");
```

---

#### `ChangeTo57600BaudAsync()`
```csharp
public async Task<bool> ChangeTo57600BaudAsync()
```

Changes the baud rate to 57600. If already at 57600, does nothing. If at a different baud rate, sends the `TrMEJ05` command, switches to 57600, and re-establishes the connection using `RF?`.

**Returns:** `true` if successful, `false` if error

**Example:**
```csharp
// Connect at any baud rate
await stack.OpenAsync();

// Switch to 57600 if not already there
bool success = await stack.ChangeTo57600BaudAsync();
if (success)
{
    Console.WriteLine($"Now at {stack.BaudRate} baud");
}
```

**Note:** This method:
1. Checks if already at 57600 (returns `true` immediately if so)
2. Sends `TrMEJ05` command to machine
3. Closes and reopens the serial port at 57600 baud
4. Sends `RF?` to re-establish connection
5. Restarts command processing

---

### Events

#### `ConnectionStateChanged`
```csharp
public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
```

Raised when the connection state changes.

**Event Args:**
- `OldState`: Previous connection state
- `NewState`: New connection state
- `Message`: Optional descriptive message

**Example:**
```csharp
stack.ConnectionStateChanged += (sender, e) =>
{
    Console.WriteLine($"State: {e.OldState} -> {e.NewState}");
    Console.WriteLine($"Message: {e.Message}");
};
```

---

#### `CommandCompleted`
```csharp
public event EventHandler<CommandCompletedEventArgs>? CommandCompleted;
```

Raised when a command completes (successfully or with error).

**Event Args:**
- `Command`: The command that completed
- `Success`: Whether the command succeeded
- `Response`: Response data (if successful)
- `ErrorMessage`: Error description (if failed)

**Example:**
```csharp
stack.CommandCompleted += (sender, e) =>
{
    Console.WriteLine($"Command: {e.Command}");
    Console.WriteLine($"Success: {e.Success}");
    if (!e.Success)
    {
        Console.WriteLine($"Error: {e.ErrorMessage}");
    }
};
```

---

### CommandResult Class

```csharp
public class CommandResult
{
    public bool Success { get; set; }
    public string? Response { get; set; }
    public string? ErrorMessage { get; set; }
    public byte[]? BinaryData { get; set; }
}
```

**Properties:**
- `Success`: `true` if command succeeded, `false` if error
- `Response`: String response from machine (may be hex-encoded)
- `ErrorMessage`: Error description if `Success` is `false`
- `BinaryData`: Decoded binary data (for Read/LargeRead commands)

---

## Usage Examples

### Example 1: Basic Connection and Read

```csharp
using var stack = new SerialStack("COM3");

if (await stack.OpenAsync())
{
    // Read firmware version
    var result = await stack.ReadAsync(0x200100);
    
    if (result.Success)
    {
        Console.WriteLine($"Firmware: {result.Response}");
    }
    
    stack.Close();
}
```

---

### Example 2: Multiple Queued Commands

```csharp
using var stack = new SerialStack("COM3");
await stack.OpenAsync();

// Queue multiple commands - they execute in order
var task1 = stack.ReadAsync(0x200100);
var task2 = stack.ReadAsync(0x200120);
var task3 = stack.LargeReadAsync(0x0240F5);

// Wait for all to complete
await Task.WhenAll(task1, task2, task3);

Console.WriteLine($"Command 1: {task1.Result.Success}");
Console.WriteLine($"Command 2: {task2.Result.Success}");
Console.WriteLine($"Command 3: {task3.Result.Success}");

stack.Close();
```

---

### Example 3: Write and Verify

```csharp
using var stack = new SerialStack("COM3");
await stack.OpenAsync();

// Write data
byte[] data = { 0x42 };
var writeResult = await stack.WriteAsync(0x0201E1, data);

if (writeResult.Success)
{
    // Verify by reading back
    var readResult = await stack.ReadAsync(0x0201E1);
    
    if (readResult.Success && readResult.BinaryData != null)
    {
        byte writtenValue = readResult.BinaryData[0];
        Console.WriteLine($"Verified: 0x{writtenValue:X2}");
    }
}

stack.Close();
```

---

### Example 4: Event Monitoring

```csharp
using var stack = new SerialStack("COM3");

// Monitor connection state
stack.ConnectionStateChanged += (s, e) =>
{
    Console.WriteLine($"[State] {e.NewState}: {e.Message}");
};

// Monitor commands
stack.CommandCompleted += (s, e) =>
{
    if (e.Success)
    {
        Console.WriteLine($"[OK] {e.Command}");
    }
    else
    {
        Console.WriteLine($"[ERROR] {e.Command}: {e.ErrorMessage}");
    }
};

await stack.OpenAsync();
await stack.ReadAsync(0x200100);
stack.Close();
```

---

### Example 5: Error Handling

```csharp
using var stack = new SerialStack("COM3");

try
{
    if (!await stack.OpenAsync())
    {
        Console.WriteLine("Failed to connect");
        return;
    }
    
    var result = await stack.ReadAsync(0x200100);
    
    if (!result.Success)
    {
        Console.WriteLine($"Command failed: {result.ErrorMessage}");
        // Stack will automatically send RF? and retry
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Exception: {ex.Message}");
}
finally
{
    stack.Close();
}
```

---

## How It Works

### Auto-Baud Detection

When `OpenAsync()` is called, the stack:

1. Tries each baud rate (19200, 57600, 115200, 4800)
2. Sends `R`, `F`, `?` individually
3. Waits for each character to be echoed back
4. If all three echo correctly, connection is established
5. Stays at the first successful baud rate

### Command Execution

1. Commands are queued in a thread-safe queue
2. Background task processes one command at a time
3. Each character is sent individually and echo is expected
4. Response is collected until complete (based on command type)
5. Result is returned via `Task<CommandResult>`

### Error Recovery

If an error occurs (machine sends `Q`, `?`, or `!`):

1. Current command fails with error result
2. Stack automatically sends `RF?` reset command
3. Protocol state is reset
4. Next queued command can proceed

### Response Parsing

The stack automatically parses responses based on command type:

- **Read (R)**: Expects command echo + 64 hex chars + `O`
- **Large Read (N)**: Expects command echo + 256 bytes + `O`
- **Write (W)**: Expects command echo only
- **L command**: Variable length, ends with `O`
- **Other**: Expects command echo

---

## Thread Safety

The `SerialStack` class is thread-safe:

- Command queue uses `ConcurrentQueue`
- State changes are protected with locks
- Multiple threads can safely queue commands
- Commands execute sequentially in queue order

---

## Best Practices

1. **Always use `using` statement**: Ensures proper disposal
   ```csharp
   using var stack = new SerialStack("COM3");
   ```

2. **Check connection state**: Before sending commands
   ```csharp
   if (stack.IsConnected)
   {
       await stack.ReadAsync(0x200100);
   }
   ```

3. **Handle errors gracefully**: Check `Success` property
   ```csharp
   var result = await stack.ReadAsync(0x200100);
   if (!result.Success)
   {
       Console.WriteLine($"Error: {result.ErrorMessage}");
   }
   ```

4. **Subscribe to events**: For debugging and monitoring
   ```csharp
   stack.ConnectionStateChanged += OnStateChanged;
   stack.CommandCompleted += OnCommandCompleted;
   ```

5. **Close connection**: When done
   ```csharp
   stack.Close();
   // or let using statement handle it
   ```

---

## Troubleshooting

### "Failed to connect at any baud rate"

- Check that the COM port is correct
- Ensure the machine is powered on
- Verify no other application is using the port
- Try manually at different baud rates

### Commands timing out

- Check serial cable connection
- Verify machine is responding
- Increase timeout values in code if needed

### Permission denied errors

- Run as Administrator (Windows)
- Add user to `dialout` group (Linux)
- Check port permissions (macOS)

### Commands fail with "Q", "?", or "!" errors

- This is normal if the machine doesn't understand a command
- Stack automatically sends `RF?` to recover
- Check that addresses and data are valid

---

## Protocol Details

For detailed information about the Bernina serial protocol, see `SerialProtocol.md`.

Key protocol concepts:
- Character-by-character echo mechanism
- Protocol reset with `RF?`
- Fixed-length responses for R/N commands
- Error indicators: `Q`, `?`, `!`

---

## License

This class is provided as-is for working with Bernina machines.
