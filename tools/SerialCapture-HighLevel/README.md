# Serial Port High-Level Command Monitor

A C# console application that captures and logs serial communication between software and a Bernina machine, displaying high-level protocol commands instead of individual characters. This tool acts as a transparent man-in-the-middle, forwarding all data while parsing and displaying complete commands and responses.

## Features

- **High-level command parsing**: Displays complete commands (R, N, W, TrME, etc.) instead of individual bytes
- **Automatic baud rate switching**: Detects `TrMEJ05` command and switches from 19200 to 57600 baud
- **Protocol-aware display**: Shows commands with their responses in a clean, readable format
- **Error detection**: Highlights error responses (Q, ?, !) from the machine
- **Dual format for N commands**: Large read (N) commands show both ASCII and HEX data
- **Bidirectional forwarding**: Data is transparently forwarded between software and machine
- **Detailed logging**: All traffic logged to file with timestamps
- **Color-coded console output**: Easy visual tracking of commands and responses

## Supported Commands

- **R (Read)**: `R` + 6 hex chars → Returns 32 bytes of HEX-encoded data
- **N (Large Read)**: `N` + 6 hex chars → Returns 256 bytes of binary data
- **W (Write)**: `W` + 6 hex chars + data + `?` → Writes data to address
- **TrME commands**: Session control (TrME, TrMEYQ, TrMEJ05)
- **RF?**: Reset command
- **EBYQ**: Baud rate confirmation
- **L command**: `L` + 12 hex chars → Variable response

## Configuration

Edit `config.ini` to configure the serial ports:

```ini
Software=COM7
Machine=COM15
Baud=19200
file=output.txt
```

**Settings:**
- `Software`: Serial port connected to the software (e.g., COM7)
- `Machine`: Serial port connected to the machine (e.g., COM15)
- `Baud`: Initial baud rate (typically 19200, will auto-switch to 57600)
- `file`: Output log filename

## Usage

### Running the Application

```bash
dotnet run
```

Or run the compiled executable:

```bash
cd bin\Debug\net8.0
SerialCapture.exe
```

### Stopping the Application

Press **CTRL+C** to gracefully stop the application. This will:
- Close both serial ports
- Finalize the log file
- Display cleanup status

## Output Format

The console displays one command per line with its response:

```
[14:23:45.123] >> RF?
[14:23:45.125] << Acknowledged

[14:23:45.200] >> R200100
[14:23:45.215] << Response: SRMV03.01.FritzGegaufAG.Oct

[14:23:45.300] >> TrMEJ05
[14:23:45.310] << Acknowledged
*** Changing baud rate from 19200 to 57600 ***

[14:23:45.450] >> N0240F5
[14:23:45.480] << Response (256 bytes):
   ASCII: LisaV45Rev8.....................BlackBoardv45Rev61...
   HEX: 4C 69 73 61 56 34 35 52 65 76 38 ...

[14:23:46.100] >> W0201E101?
[14:23:46.110] << Write acknowledged
```

**Error display:**
```
[14:23:47.200] >> R2001ZZ
[14:23:47.205] << ERROR: Machine responded with 'Q'
```

## Protocol Details

The application understands the Bernina low-level serial protocol:

1. **Character echoing**: Machine echoes each character from software
2. **Command buffering**: Software commands are buffered until complete
3. **Response parsing**: Machine responses are captured and displayed
4. **Baud rate switching**: Automatic switch to 57600 when `TrMEJ05` detected
5. **Error handling**: Detects and displays Q, ?, ! error responses

See `SerialProtocol.md` in the DataReader documentation for full protocol details.

## Use Cases

- **Protocol monitoring**: Watch high-level communication between software and machine
- **Debugging**: Identify communication issues at the command level
- **Development**: Understand command sequences and responses
- **Analysis**: Study how the software interacts with the machine

## Requirements

- .NET 8.0 SDK
- Windows, Linux, or macOS
- Access to the serial ports (may require administrator/root privileges)
- Two serial ports (real or virtual) for man-in-the-middle setup

## Troubleshooting

**"Port is already in use" error:**
- Ensure no other application has the ports open
- Close any serial monitoring tools or terminal emulators

**"Access denied" error:**
- On Windows: Run as Administrator
- On Linux: Add user to `dialout` group: `sudo usermod -a -G dialout $USER`
- On macOS: Ensure proper permissions for `/dev/tty.*` devices

**"Port not found" error:**
- Verify the port names in `config.ini` match actual ports on your system
- On Windows: Check Device Manager
- On Linux/macOS: List ports with `ls /dev/tty*` or `ls /dev/cu.*`

**Commands not displaying:**
- Check that data is flowing (forwarding must be enabled with 'R' command)
- Verify baud rate is correct in config.ini
- Check that both ports are properly connected

## Building from Source

```bash
dotnet restore
dotnet build
```

## Architecture

The application uses a state machine to:
1. Buffer incoming characters from software
2. Detect complete commands based on protocol rules
3. Wait for and capture machine responses
4. Display commands with their responses
5. Handle special cases (baud rate changes, errors, unsolicited messages)

## License

This is a utility tool created for educational and development purposes.
