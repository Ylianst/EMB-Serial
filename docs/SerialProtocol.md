# Low-Level Serial Protocol

This is the serial protocol between "software" and "machine". The machine starts at 19200 baud, but the software will instruct it to move to 57600 baud. All serial communication is N,8,1 with no Handshake, so CTS/DTS does not play a factor. The protocol is generally ASCII based except when binary data is sent.

## Bernina CPU

- The Bernina Artista uses a Hitachi H8 CPU. CPU part number is H8/3003, HD6413003F16.
- Programming Manual: https://www.renesas.com/en/document/gde/h8300-programming-manual
- The CPU manual is at: https://www.renesas.com/en/document/mah/h83003-hardware-manual
- CPU emulator: http://bitsavers.informatik.uni-stuttgart.de/test_equipment/hp/64700/64784-97011_H8_3003_Softkey.PDF
- Documents and tools: https://www.renesas.com/en/products/h8-3003?tab=documentation
- GCC compiler support: https://h8300-hms.sourceforge.net/
- Memory map: 0FFD10 - 0FFF0F, page 501 of the manual.
- The DMA controller regs are 0xFFFF20 - 0xFFFF5F.

## General Concepts

- The software sends commands to the machine one character at a time and waits for the same character to be echoed back before sending the next character.
- The software does not echo back traffic from the machine, only the machine echoes software.
- If the machine does not understand a character from the software, it will send back the letter "Q", "?" or "!", otherwise it will echo back the character that the machine sent.
- If there is a problem, the software must send "RF?" and the machine will echo back "RF?", you are then ready to send the next command. If the software starts sending a command and gets a "Q", "?" or "!" echo back, you have o stop and send a "RF?" waitting for each character to be echoed before resuming. "RF?" should also be used as an command to detect that the machine is listening on the serial bus before sending other commands.
- The software seems to sometimes generate invalid Read commands, they are R commands followed by bytes that are not valid for a HEX value. In this case, the machine returns error response and the software seems to try again with a valid read value.
- When the machine first starts, it will be at 19200 bauds and send out "BOSN" (In HEX: 42 4F 53 4E)
- When the machine turned off, I see a single 0x00
- Then the software first starts, the software will send data in 4 groups at different baud rates.

```
52 52 52 52 52                 - at 19200 bauds (RRRRR)
52 52 52 02 66 03 6B 52 52 52  - at 57600 bauds
52 52 52 02 66 03 6B 52 52 52  - at 115200 bauds
52 52 52 52 52                 - at 4800 bauds
```

The software seems to be trying 4 different baud rates to see if the machine will answer on one of them. It's trying to send the "RF?" command. If the software gets back a "RF?" echo, it will start talking to the machine. The start sequence at 19200 bauds is:

```
RF?       - Just echoed back
R57FF80   - Echoed back + Data Block
TrME      - Just echoed back
R200100   - Echoed back + Data Block
TrMEJ05   - Echoed back causes baudrate change to 57600 bauds.
-- Baudrate change occurs, the machine will send "BOS" (42 4F 53) at 57600 bauds.
EBYQ      - Echoed back + "O", this seems to be the confirmation that we moved over to the new speed.
R200100   - Echoed back + Data Block
RF?       - Just echoed back
R200100   - Echoed back + Data Block
RFFFD24   - Echoed back + Data Block
...
```

After the machine sends "BOS" at 57600 bauds, it will revert to sending "BOS" at 19200 bauds periodically (every second). So, you may need to be quick to change baudrates and send "EBYQ" to the machine to keep it at the new speed or maybe you ahve time and this is expected? Not sure.

## The Protocol Reset Command

The "RF?" command causes the machine to reset it's protocol state. If you send a command and get "Q", "?" or "!" and an echo back, something is not quite right and you may be stuck until the protocol reset command much be sent before sending more commands. This command can also be used to initially find the machine on the serial bus.

## The Read Command

A "Read" command will cause the machine with return 32 bytes of data starting at the address specified by the command. This is fixed, all "Read" commands return this ammount of data back. The read command starts with a capital R followed by 6 characters that are the address to the read. Here are two value examples:

```
"R200100"  (52 32 30 30 31 30 30)
"RFFFED9"
```

- When receiving a "Read" command, the machine will respond with 65 characters. The data block is HEX encoded followed by the character "O". For example:

```
"R200100" will return:
"53524D5630332E30312000467269747A204765676175662041470D004F637420O"

"RFFFED9" will return:
"8300330000000000000000000000000200000100000000000000000000000000O"
```

## The Large Read Command

In addition to the read command, there is a "Large Read" command that worked the same way as "Read", but return 256 bytes of binary encoded data instead of 32 bytes of HEX encoded data. The command in "N" on the serial protocol followed by 6 HEX characters for the address. Here are two examples:

```
"N0240F5" will return:
"LisaV45Rev8.....................BlackBoardv45Rev61..............SwissBlock v45 rev6a............Zurichv45rev6a..................ALICE v6m Rev5 Firmware v3......BAMBOO v6m Rev8 Firmware v3.....Lg5060..........................Cs021...........................O"

"N0241F5"  will return:
"Fl081...........................Nv772...........................Nv722...........................Nv799...........................Bd130...........................Bd115v2.........................Cr070...........................Cr060...........................O"
```

Like the "Read" command, the "Large Read" command also completes the block with a capital "O". So, 256 characters of data are returned along with the ending capital "O".

When downloading a lot of data, the software will use the Large Read (N) command a lot, but if the last block that needs to be downloaded is 32 bytes of less, it will switch to used the Read (R) command to complete the download.

## The Write Command

It's also possible to write using the W command. Just like the read command, it's a W followed by 6 HEX characters for the address followed by the data to write in HEX and "?" to complete the command. Here are two examples of write commands:

```
"W0201E101?"
"WFFFED00061?"
```

The first command will write 0x01 to address 0x0201E1, the second command will write 0x0061 to address 0xFFFED0. There is no confirmation given that the write operation was a success, so often times the software with perform a read (R) operation after a set of writes to make sure the operation was a success.

## Session Start Command

At the start of a session, the software will send a session start command. On the serial port, it's "TrMEYQ". The machine will echo back each character and confim with a "O" reply. There is an example:

"TrMEYQ" will replay "O"

It's not known what the machine does with this. It's possible to send read commands before the session start and it will work.

## Session End Command

At the end of a session, the software will tell the machine it's done using the terminate command. On the serial port it's sent as "TrME". The machine will echo back the 4 charecters and nothing happens after this. It's not known what the machine does with this.

## The Unknown L (Load?) Command

There is a "L" command that is L followed by 12 serial characters in HEX format. The machine will reply with HEX values followed by "O" when competed. For example:

```
"L0240D5000360" will reply "00004CC9O"
"L0240D5000240" will reply "00001C79O"
```

In the first example, it could indicate LOAD 0x360 byteso data at position 0x0240D5. The first 6 bytes of the HEX is certainly a memory pointer and the second part looks like a size since the number is in the right range.
