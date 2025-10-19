# Embroidery Memory Dump Tool

This tool will download the entire content of the Artista 180 and generate a 16 megabyte binary (.bin) file.

![image](https://github.com/Ylianst/EMB-Serial/blob/main/docs/images/MemoryDump01.png)

To get started, you need to compile the tool and create a `config.ini` file in the same folder as the executable that looks like this:

```
serial=COM15
baud=19200
#baud=57600
file=output.bin
start=000000
```

This tool will not try to change the machine's serial baud rate, so, just specify the rate the machine is currently at. If you just turned on the Artista 180, the default starting speed is 19200. If you used other software to move the machine to a faster speed, then it will stay at the high speed until it's turned off and on again.

output.bin will be the resulting file, you can also specify the starting address to start dumping the memory at, but the default is 0x000000. It will automaticaly go until 0xFFFFF and stop.

If you have any information on this machine, [please open an issue in GitHub](https://github.com/Ylianst/EMB-Serial/issues).
