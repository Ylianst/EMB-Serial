# Embroidery Serial

This is an attempt at learning how the serial protocol of the Bernina Artista 180 works. This is a embroidery machine from the late 90's with a DB9 serial port at what quite popular at the time. There is old software for it on Windows XP that no longer works on new operating systems, but we can looks at the serial traffic by putting a modern computers in the middle and taking a look at the traffic.

![image](https://github.com/Ylianst/EMB-Serial/blob/main/docs/images/serial-setup.png)

The 180 will start at 19200 bauds N,8,1 serial settings but can be told to go up to a higher speed. The software will send out traffic at various baud rates to try to find the machine since, if it's been told to move to a higher speed, it will no longer be responsive at the default 19200 baud speed. The low level protocol has been wroked out and you can take a [look at it here](https://github.com/Ylianst/EMB-Serial/blob/main/docs/SerialProtocol.md). There is a basic set of commands: Read/LargeRead/Write/Load. Here is a [C# Serial Stack](https://github.com/Ylianst/EMB-Serial/blob/main/Tools/SerialComm/SerialStack.cs) class that implements the low-level protocol so you can easily build software to perform these commands.

Once the basics of the protocol have been understood, the high level communication needs to be mastered next. I have an early document on the [high-level protocol](https://github.com/Ylianst/EMB-Serial/blob/main/docs/SerialProtocol.md). Much work remains to be done.

I have a bunch of tools that can be useful to work on figuring out the serial protocol. All tools here at C# and build with Visual Studio 2022.

- [SerialComm](https://github.com/Ylianst/EMB-Serial/blob/main/Tools/SerialComm), this is a high level tool that will find the machine's baud rate, connect to it and allow common low-level protocol operations like Read/Write/Load. Useful to work on the high level protocol.
- [MemoryDump](https://github.com/Ylianst/EMB-Serial/blob/main/Tools/MemoryDump), this tool downloads the entire content of the machine's memory. It take less than an hour to do depending on the serial speed and generates a 16 megabyte file.
- [SerialCapture](https://github.com/Ylianst/EMB-Serial/blob/main/Tools/SerialCapture), this tool in intended to be used between software and machine to look at the traffic on the wire. You need to specify both machine and software COM ports and the starting baud rate. It will generate low level and high level output files.
- [SerialCapture-Basic](https://github.com/Ylianst/EMB-Serial/blob/main/Tools/SerialCapture-Basic), this is an early version of the SerialCapture tool that captures raw serial traffic. Probably not useful anymore because the other tools are better.
- [SerialCapture-HighLevel](https://github.com/Ylianst/EMB-Serial/blob/main/Tools/SerialCapture-HighLevel), another version of the SerialCapture tool, but only generates very high level logs. This is the most useful tool for figuring out the high level protocol.

If you have any information on this machine, [please open an issue in GitHub](https://github.com/Ylianst/EMB-Serial/issues).
