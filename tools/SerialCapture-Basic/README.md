# Serial Port Man-in-the-Middle Capture Tool

A C# console application that captures and logs bidirectional serial communication between two serial ports. This tool acts as a transparent man-in-the-middle, forwarding all data while logging everything for analysis.

## Features

- **Bidirectional forwarding**: Data received on either port is automatically forwarded to the other
- **Detailed logging**: All traffic is logged with precise timestamps and source information
- **Hex and ASCII display**: Data is shown in both hexadecimal and ASCII representation
- **Color-coded console output**: Different colors for each port for easy visual tracking
- **Graceful shutdown**: Press CTRL+C to cleanly close ports and finalize the log file

## Configuration

Edit `config.ini` to configure the serial ports:

```ini
serial1=COM8
serial2=COM9
baud=9600
file=output.txt
```

**Settings:**
- `serial1`: First serial port name (e.g., COM8, /dev/ttyUSB0)
- `serial2`: Second serial port name (e.g., COM9, /dev/ttyUSB1)
- `baud`: Baud rate (e.g., 9600, 115200)
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

## Log Format

The log file contains entries in the following format:

```
[2025-10-14 18:37:45.123] COM8 → COM9: 48 65 6C 6C 6F (Hello)
[2025-10-14 18:37:45.456] COM9 → COM8: 4F 4B (OK)
```

Each entry includes:
- **Timestamp**: Date and time with millisecond precision
- **Source → Destination**: Which port received the data and where it was forwarded
- **Hex data**: Byte values in hexadecimal format
- **ASCII representation**: Printable characters (non-printable shown as '.')

## Use Cases

- **Protocol analysis**: Capture communication between two devices to reverse-engineer protocols
- **Debugging**: Monitor serial communication to identify issues
- **Testing**: Verify data integrity in serial communications
- **Development**: Understand how devices communicate before writing custom code

## Requirements

- .NET 8.0 SDK
- Windows, Linux, or macOS
- Access to the serial ports (may require administrator/root privileges)

## Troubleshooting

**"Port is already in use" error:**
- Ensure no other application (terminal emulator, IDE, etc.) has the ports open
- Close any serial monitoring tools

**"Access denied" error:**
- On Windows: Run as Administrator
- On Linux: Add user to `dialout` group: `sudo usermod -a -G dialout $USER`
- On macOS: Ensure proper permissions for `/dev/tty.*` devices

**"Port not found" error:**
- Verify the port names in `config.ini` match actual ports on your system
- On Windows: Check Device Manager
- On Linux/macOS: List ports with `ls /dev/tty*` or `ls /dev/cu.*`

## Building from Source

```bash
dotnet restore
dotnet build
```

## License

This is a utility tool created for educational and development purposes.
