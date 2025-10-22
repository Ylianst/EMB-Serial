# Serial Capture Tool - WinForms Application

A Windows Forms application that acts as a relay between two serial ports, capturing and logging high-level command traffic in both directions.

## Features

- **Dual Serial Port Relay**: Connects two serial ports and forwards data bidirectionally
- **High-Level Command Parsing**: Interprets and displays protocol commands with their responses
- **Real-Time Display**: Live capture window showing command traffic
- **Detailed Logging**: Saves all captured traffic to a log file
- **Configurable Options**:
  - Software Port (COM port for software side)
  - Machine Port (COM port for machine side)
  - Baud Rate (9600, 19200, 38400, 57600, 115200)
  - Filter NULL characters from Software
  - Debug Mode (shows individual character forwarding)
  - Show Errors (displays protocol errors)
- **Dynamic Baud Rate Switching**: Automatically changes baud rate when detecting specific commands (e.g., TrMEJ05)
- **Upload Progress Tracking**: Monitors data upload operations with progress indicators

## Supported Commands

The tool recognizes and parses these command types:

- **R** - Read command (6 hex address → 64 hex chars + O)
- **N** - Large Read command (6 hex address → 256 data bytes + O)
- **W** - Write command (6 hex address + data + ?)
- **PS** - Upload command (4 hex address → 256 bytes upload)
- **L** - Sum/Checksum command (12 hex chars → 8 hex checksum + O)
- **TrME** family - Session commands (TrME, TrMEY, TrMEYQ, TrMEJ05)
- **RF?** - Reset command
- **EBYQ** - Other protocol commands

## Usage

1. **Launch the Application**: Run `SerialCaptureTool.exe`

2. **Configure Settings** (first time or as needed):
   - From the **Ports** menu:
     - Select **Software Port** (the port connected to your software)
     - Select **Machine Port** (the port connected to your hardware/machine)
   - From the **Baud Rate** menu:
     - Choose the initial baud rate (default: 19200)
   - From the **Options** menu:
     - Toggle **Filter NULL from SW**: Removes NULL (0x00) bytes from software before forwarding
     - Toggle **Debug Mode**: Shows every character transmitted (verbose)
     - Toggle **Show Errors**: Displays protocol error messages
     - Select **Set Log File...** to change the log file path
   - All settings are automatically saved to the Windows Registry

3. **Start Capture**: Click "Start Capture" button
   - The tool waits for an 'R' command from the Software port before enabling forwarding
   - Once enabled, all commands and responses are captured and displayed

4. **Monitor Traffic**: Watch the capture window for high-level command information
   - Commands from Software are shown with their responses from Machine
   - Read commands display both ASCII and HEX representations
   - Upload commands show progress and final data

5. **Stop Capture**: Click "Stop Capture" when finished
   - All data is saved to the log file
   - Ports are properly closed

## Display Format

### Read Commands (R/N)
```
R000000 -->
   ASCII: ....text....
   HEX: 01 02 03 04 ...
```

### Write Commands (W)
```
W000000data? --> Write data to 000000
```

### Upload Commands (PS)
```
PS0000 --> Machine ready (OE), waiting for 256 bytes from Software...
  [Upload Progress: 32/256 bytes received]
  [Upload Progress: 64/256 bytes received]
  ...
PS0000 --> Upload 256 bytes to address 0000:
   ASCII: ................
   HEX: 00 01 02 03 ...
```

## Log File

All captured traffic is written to the specified log file with:
- Timestamp when session started
- Configuration details (ports, baud rate, settings)
- All commands and responses
- Error messages (if Show Errors is enabled)
- Debug character information (if Debug Mode is enabled)
- Session end timestamp

## Technical Details

- Built with .NET 8.0 and Windows Forms
- Uses System.IO.Ports for serial communication
- Multi-threaded design with separate reader threads for each port
- Thread-safe data processing and UI updates
- Blocking reads with infinite timeout for efficient operation
- Automatic baud rate switching support (e.g., 19200 → 57600 on TrMEJ05)
- Settings persistence using Windows Registry (HKEY_CURRENT_USER\Software\SerialCaptureTool)

## Notes

- The application waits for an 'R' character from the Software port before starting to forward data
- NULL character filtering only applies to data from Software to Machine (not during uploads)
- The capture window shows a high-level view; enable Debug Mode for character-level detail
- Protocol errors are only shown if "Show Errors" is enabled
- The RF? command provides no high-level information and is not displayed (to reduce clutter)
- Port and baud rate selections are saved automatically and restored when you reopen the application
- Settings are stored in: `HKEY_CURRENT_USER\Software\SerialCaptureTool`
