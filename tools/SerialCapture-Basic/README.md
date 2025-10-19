# Serial Port Man-in-the-Middle Capture Tool

A C# console application that captures and logs bidirectional serial communication between two serial ports. This tool acts as a transparent man-in-the-middle, forwarding all data while logging everything for analysis. Note that this tool will not change baud rates in the middle of a capture, so, if you are capturing traffic and the software tells the machine to switch from 19200 baud to 57600 baud, the software will not follow and so, all traffic after this will not be forwarded correctly.

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

```
dotnet run
```

Or run the compiled executable:

```
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

## Requirements

- .NET 8.0 SDK
- Windows, Linux, or macOS
- Access to the serial ports (may require administrator privileges)

## Troubleshooting

**"Port is already in use" error:**
- Ensure no other application (terminal emulator, IDE, etc.) has the ports open
- Close any serial monitoring tools