# Serial Port Command Monitor

A C# console application that captures and logs serial communication between software and a Bernina machine, displaying commands and individual characters. This tool acts as a transparent man-in-the-middle, forwarding all data while parsing and displaying complete commands and responses.

This software will attempt detect a switch command from 19200 to 57600 baud and automatically follow and continue to forward traffic at the new higher speed.

## Configuration

Edit `config.ini` to configure the serial ports:

```ini
Software=COM7
Machine=COM6
Baud=19200
#Baud=57600
file=output.txt
hfile=houtput.txt
loghex=false
```

The baud rate specified here is the initial rate which is 19200 bauds when first turning on the machine. If the machine was already changed to using 57600 bauds before starting this application, you need to change the initial baud rate to 57600. The line `#Baud=57600` is commented out (not active). Change the # to the other line to switch baudrates.

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

## Requirements

- .NET 8.0 SDK
- Windows, Linux, or macOS
- Access to the serial ports (may require administrator privileges)
- Two serial ports (real or virtual) for man-in-the-middle setup