# EmbroideryStack

A Node.js application for communicating with embroidery machines (like Bernina Artista) via serial port using a low-level serial protocol.

## Overview

This project provides a robust interface to communicate with embroidery machines over a serial connection. It consists of two main components:

1. **SerialStack.js**: Low-level serial communication module with character-by-character echo protocol
2. **Relay.js**: TCP server that relays commands to the embroidery machine, providing network access to the serial interface

## Features

### SerialStack Features
- **Automatic baud rate detection**: Automatically detects the machine's baud rate (19200, 57600, 115200, or 4800)
- **Character-by-character transmission**: Sends commands one character at a time, waiting for echo confirmation
- **Automatic retry logic**: Built-in retry mechanism for Read, Large Read, and Sum operations
- **Error recovery**: Automatic resync (RF?) on communication errors
- **Checksum verification**: Sum command for verifying data integrity
- **Robust error handling**: Handles timeout, echo mismatch, and machine error responses

### Relay.js Features
- **TCP to Serial Bridge**: Exposes embroidery machine over TCP on port 8888
- **Automatic Initialization**: On connection, automatically opens serial port, closes any active embroidery session, and upgrades to maximum baud rate (57600)
- **Command Queueing**: Queues incoming commands until initialization completes for accurate responses
- **Single Connection**: Only allows one TCP client at a time to prevent conflicts
- **Comprehensive Logging**: Logs all incoming commands and outgoing responses for debugging
- **Systemd Integration**: Can be installed as a background service with automatic restart
- **Custom Protocol**: Implements a structured TCP protocol for reliable communication (see docs/TcpProtocol.md)

## Files

- **SerialStack.js**: Core module that provides low-level serial communication operations
- **Relay.js**: TCP server that provides network access to the embroidery machine
- **TcpProtocol.js**: Protocol handler for TCP communication
- **TestStack.js**: Test application demonstrating usage of SerialStack
- **TestRelay.js**: Test application demonstrating usage of Relay.js TCP interface
- **package.json**: Node.js package configuration
- **docs/SerialProtocol.md**: Detailed serial protocol documentation
- **docs/TcpProtocol.md**: Detailed TCP protocol documentation

## Installation

```bash
npm install
```

## Requirements

- Node.js >= 14.0.0
- Serial port connection to embroidery machine (default: `/dev/ttyUSB0`)
- Machine should be powered on and connected

## Usage

### Using Relay.js (TCP Server)

Relay.js provides a TCP server that exposes the embroidery machine over the network on port 8888. This is the recommended way to use EmbroideryStack as it handles all initialization automatically and can run as a background service.

#### Running Relay.js

```bash
# Run in foreground (development/testing)
node Relay.js

# View help and available commands
node Relay.js --help

# Install as systemd service and start it (requires sudo)
sudo node Relay.js --install

# Start the service
sudo node Relay.js --start

# Stop the service
sudo node Relay.js --stop

# Uninstall the service
sudo node Relay.js --uninstall
```

#### Relay.js Command-Line Arguments

- **`--help`** or **`-h`**: Display help information with all available commands
- **`--install`**: Install Relay.js as a systemd service, enable it to start on boot, and start it immediately
- **`--uninstall`**: Stop the service, disable it from starting on boot, and remove the systemd service file
- **`--start`**: Start the relay service (must be installed first)
- **`--stop`**: Stop the relay service
- **(no arguments)**: Run the server in foreground mode

#### Service Management

When installed as a service, Relay.js runs in the background and automatically:
- Starts on system boot
- Restarts automatically if it crashes (after 10 seconds)
- Logs to systemd journal for easy monitoring

Useful systemd commands:
```bash
# Check service status
sudo systemctl status relay

# View live logs
sudo journalctl -u relay -f

# View recent logs
sudo journalctl -u relay -n 50

# Restart the service
sudo systemctl restart relay
```

#### How Relay.js Works

When a TCP client connects to Relay.js on port 8888:

1. **Automatic Initialization**: 
   - Creates SerialStack instance
   - Opens serial port with automatic baud rate detection
   - Checks for and closes any existing embroidery session
   - Upgrades to maximum baud rate (57600)
   - Only after initialization completes, commands are processed

2. **Command Processing**:
   - Commands received during initialization are queued
   - Once initialization completes, queued commands are processed in order
   - Subsequent commands are handled immediately
   - All requests and responses are logged for debugging

3. **Connection Management**:
   - Only one TCP client can connect at a time (prevents conflicts)
   - Additional connection attempts are rejected with a "busy" message
   - When client disconnects, serial port is closed gracefully

4. **Protocol**:
   - Uses a custom structured TCP protocol (see docs/TcpProtocol.md)
   - Supports commands: GCFG, SCFG, STAT, READ, LRED, WRIT, UPLD, CSUM, SOPE, SCLO, BAUD, RSET
   - All responses include request ID for matching requests/responses
   - Error responses include error codes and messages

See **TestRelay.js** for example TCP client code.

### Using SerialStack.js Directly

For direct serial port access without the TCP layer:

```javascript
const SerialStack = require('./SerialStack');

async function example() {
  const stack = new SerialStack('/dev/ttyUSB0', 19200);
  
  try {
    // Open connection
    await stack.open();
    
    // Perform resync to ensure machine is ready
    await stack.resync();
    
    // Read firmware version from address 0x200100
    const data = await stack.read('200100');
    const firmware = stack.hexToAscii(data);
    console.log('Firmware:', firmware);
    
    // Close connection
    await stack.close();
  } catch (error) {
    console.error('Error:', error.message);
  }
}

example();
```

### Running the Test Applications

```bash
# Test SerialStack directly
npm test
# or
node TestStack.js

# Test Relay.js TCP interface (requires Relay.js running)
node TestRelay.js
```

## SerialStack API

### Constructor

```javascript
new SerialStack(portPath = '/dev/ttyUSB0', baudRate = 19200)
```

Creates a new SerialStack instance.

### Methods

#### `async open()`
Opens the serial port connection.

#### `async close()`
Closes the serial port connection.

#### `async sendCommand(command)`
Sends a command character by character, waiting for echo after each character.

**Parameters:**
- `command` (string): The command string to send

**Returns:** Promise<string> - The full echo response

**Throws:** Error if machine returns 'Q', '?', or '!' or if echo doesn't match

#### `async resync()`
Performs a protocol resync operation (RF?). This resets the machine's protocol state.

**Returns:** Promise<boolean> - True if successful

#### `async read(address, retryCount = 0)`
Reads 32 bytes from the specified address. Returns 64 HEX characters.

**Parameters:**
- `address` (string): 6-character HEX address (e.g., "200100")
- `retryCount` (number): Internal retry counter (default: 0)

**Returns:** Promise<string> - 64 HEX characters representing 32 bytes of data

**Features:**
- Automatic retry on failure (up to 3 attempts)
- Automatic resync between retries
- Validates response terminator ('O')

#### `async largeRead(address, retryCount = 0)`
Reads 256 bytes from the specified address. Returns 256 binary characters.

**Parameters:**
- `address` (string): 6-character HEX address (e.g., "0240F5")
- `retryCount` (number): Internal retry counter (default: 0)

**Returns:** Promise<string> - 256 characters of binary data

**Features:**
- Automatic retry on failure (up to 3 attempts)
- Automatic resync between retries
- Validates response terminator ('O')

#### `async sum(address, length, retryCount = 0)`
Calculates the checksum of a memory region.

**Parameters:**
- `address` (string): 6-character HEX address (e.g., "200100")
- `length` (string): 6-character HEX length (e.g., "000020" for 32 bytes)
- `retryCount` (number): Internal retry counter (default: 0)

**Returns:** Promise<number> - The sum value as a number

**Features:**
- Returns 8 HEX characters as a 32-bit integer
- Automatic retry on failure (up to 3 attempts)
- Automatic resync between retries
- Useful for verifying data integrity after reads/writes

**Example:**
```javascript
// Calculate checksum of 32 bytes starting at 0x200100
const checksum = await stack.sum('200100', '000020');
console.log(`Checksum: 0x${checksum.toString(16)}`);
```

#### `async write(address, data, retryCount = 0)`
Writes data to a specific memory address.

**Parameters:**
- `address` (string): 6-character HEX address (e.g., "0201E1")
- `data` (string): HEX data to write (e.g., "01" for single byte, "0061" for two bytes)
- `retryCount` (number): Internal retry counter (default: 0)

**Returns:** Promise<void>

**Features:**
- Validates address is 6 characters
- Validates data has even number of HEX characters
- Automatic retry on failure (up to 3 attempts)
- Automatic resync between retries
- No confirmation from machine - recommend reading back to verify

**Example:**
```javascript
// Write 0x01 to address 0x0201E1
await stack.write('0201E1', '01');

// Write 0x0061 to address 0xFFFED0
await stack.write('FFFED0', '0061');

// Verify write succeeded
const result = await stack.read('0201E0');
```

#### `async upload(address, data, retryCount = 0)`
Uploads 256 bytes of binary data to a 256-byte aligned address.

**Parameters:**
- `address` (string): 4-character HEX address prefix (e.g., "028F" for address 0x028F00)
- `data` (Buffer|string): Exactly 256 bytes of binary data
- `retryCount` (number): Internal retry counter (default: 0)

**Returns:** Promise<void>

**Features:**
- Address must be 4 characters (last 2 digits assumed to be 00)
- Data must be exactly 256 bytes
- Waits for "OE" confirmation before sending data
- Waits for "O" final confirmation after data sent
- Automatic retry on failure (up to 3 attempts)
- Automatic resync between retries

**Example:**
```javascript
// Upload 256 bytes to address 0x028F00
const data = Buffer.alloc(256);
// ... fill data with your content
await stack.upload('028F', data);

// Can also use string with latin1 encoding
const dataString = 'x'.repeat(256);
await stack.upload('028F', dataString);
```

#### `async upgradeSpeed()`
Upgrades serial connection from 19200 baud to 57600 baud.

**Returns:** Promise<boolean> - True if successful

**Features:**
- Does nothing if already at 57600 baud
- Sends TrMEJ05 command
- Switches baud rate after command echo
- Waits for "BOS" message at new baud rate
- Confirms with EBYQ command and waits for "O"
- Performs resync to verify connection
- **Important**: Must be completed quickly or machine reverts to 19200 baud

**Example:**
```javascript
await stack.open(); // Connects at detected baud rate
if (stack.baudRate === 19200) {
  await stack.upgradeSpeed(); // Upgrade to 57600
}
```

#### `async IsEmbroiderySessionOpen()`
Checks if an embroidery session is currently open.

**Returns:** Promise<boolean> - True if session is open, false if closed

**Features:**
- Reads address 0x57FF80
- Session is open if first byte is 0x00
- Session is closed if first byte is 0xB4 (or other non-zero value)
- Logs session status for debugging

**Example:**
```javascript
const isOpen = await stack.IsEmbroiderySessionOpen();
if (isOpen) {
  console.log('Embroidery session is active');
} else {
  console.log('Sewing machine session is active');
}
```

#### `async StartEmbroiderySession()`
Starts an embroidery session to communicate with the embroidery module.

**Returns:** Promise<boolean> - True if session was started, false if already open

**Features:**
- First checks if session is already open
- Does nothing if session is already open (returns false)
- Sends TrMEYQ command
- Waits for "O" confirmation
- Makes embroidery module memory accessible
- Required for accessing embroidery files and functions

**Important Notes:**
- Do NOT call if session is already open (will cause invalid state)
- Cannot change baud rate while session is open
- Close session when done to return to sewing machine mode

**Example:**
```javascript
const started = await stack.StartEmbroiderySession();
if (started) {
  // Session was opened, can now access embroidery module
  // ... perform embroidery operations
  await stack.EndEmbroiderySession();
}
```

#### `async EndEmbroiderySession()`
Ends an embroidery session and returns to sewing machine mode.

**Returns:** Promise<boolean> - True if session was ended, false if already closed

**Features:**
- First checks if session is currently open
- Does nothing if session is already closed (returns false)
- Sends TrME command
- Returns control to sewing machine processor
- Recommended to close session when not needed

**Example:**
```javascript
const ended = await stack.EndEmbroiderySession();
if (ended) {
  console.log('Returned to sewing machine mode');
}
```

#### `hexToAscii(hexString)`
Helper method to convert HEX string to ASCII.

**Parameters:**
- `hexString` (string): HEX string (e.g., "48656C6C6F")

**Returns:** string - ASCII representation

#### `asciiToHex(asciiString)`
Helper method to convert ASCII to HEX string.

**Parameters:**
- `asciiString` (string): ASCII string (e.g., "Hello")

**Returns:** string - HEX representation

## Configuration

### Serial Port Settings

The serial communication uses the following settings:
- **Baud Rate**: 19200 (can be changed to 57600)
- **Data Bits**: 8
- **Parity**: None
- **Stop Bits**: 1
- **Flow Control**: None

### Timeouts

- **Character Timeout**: 500ms (time to wait for each character echo)
- **Command Timeout**: 5000ms (time to wait for full command response)
- **Max Retries**: 3 attempts for read operations

## Protocol Details

### Resync Command (RF?)
Resets the machine's protocol state. Must be used when:
- Communication errors occur
- Machine returns 'Q', '?', or '!'
- Initial connection to verify machine is listening

### Read Command (R)
Format: `R` + 6-character HEX address

Example: `R200100`

Returns: 64 HEX characters + 'O' (representing 32 bytes of data)

### Large Read Command (N)
Format: `N` + 6-character HEX address

Example: `N0240F5`

Returns: 256 binary characters + 'O' (256 bytes of data)

## Error Handling

The module implements comprehensive error handling:

1. **Echo Mismatch**: If the echo doesn't match the sent character, an error is thrown
2. **Machine Error Response**: If machine returns 'Q', '?', or '!', an error is thrown
3. **Timeout**: If no response within timeout period, an error is thrown
4. **Automatic Retry**: On read failures, the module:
   - Performs a resync (RF?)
   - Retries the operation (up to 3 times)
   - Throws final error if all retries fail

## Known Addresses

Based on the protocol documentation:

- `0x200100`: Machine firmware version and manufacturer info
- `0x57FF80`: Embroidery module session status
- `0xFFFED9`: PC Card detection (0x82 = No Card, 0x83 = Card Present)
- `0xFFFED0`: Function invocation register

## Notes

- The machine echoes back every character sent by the software
- The software does NOT echo back traffic from the machine
- Always wait for echo confirmation before sending the next character
- If an error occurs during a command, perform resync before continuing
- The machine must be in the correct mode (sewing vs embroidery) for certain operations

## License

ISC

## References

See `docs/SerialProtocol.md` for complete protocol specification.
