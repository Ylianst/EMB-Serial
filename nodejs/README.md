# EmbroideryStack

A Node.js application for communicating with embroidery machines (like Bernina Artista) via serial port using a low-level serial protocol.

## Overview

This project provides a robust interface to communicate with embroidery machines over a serial connection. It implements the character-by-character echo protocol with built-in retry logic and error handling.

## Features

- **Automatic baud rate detection**: Automatically detects the machine's baud rate (19200, 57600, 115200, or 4800)
- **Character-by-character transmission**: Sends commands one character at a time, waiting for echo confirmation
- **Automatic retry logic**: Built-in retry mechanism for Read, Large Read, and Sum operations
- **Error recovery**: Automatic resync (RF?) on communication errors
- **Checksum verification**: Sum command for verifying data integrity
- **Robust error handling**: Handles timeout, echo mismatch, and machine error responses

## Files

- **SerialStack.js**: Core module that provides low-level serial communication operations
- **TestStack.js**: Test application demonstrating usage of SerialStack
- **package.json**: Node.js package configuration
- **docs/SerialProtocol.md**: Detailed protocol documentation

## Installation

```bash
npm install
```

## Requirements

- Node.js >= 14.0.0
- Serial port connection to embroidery machine (default: `/dev/ttyUSB0`)
- Machine should be powered on and connected

## Usage

### Basic Example

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

### Running the Test Application

```bash
npm test
# or
node TestStack.js
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
