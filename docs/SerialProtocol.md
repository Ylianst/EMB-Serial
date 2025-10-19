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

## The Unknown L Command

There is a "L" command that is L followed by 12 serial characters in HEX format. The machine will reply with HEX values followed by "O" when competed. For example:

```
"L0240D5000360" will reply "00004CC9O"
"L0240D5000240" will reply "00001C79O"
```

In the first example, it could indicate LOAD 0x360 byteso data at position 0x0240D5. The first 6 bytes of the HEX is certainly a memory pointer and the second part looks like a size since the number is in the right range.


## Reading the Machine Name & Version

Reading at 0x200100 will give you the machine firmware version. You also see the firmware language and manufacturer after this. We can use a large read (N) to get the entire string in one go.

```
N200100 --> "NMMV03.01·English     ·Bernina Electronic  AG  ·July 98"
```

## Startup Sequence

This sequence is done when you first enter the ArtLink software and reads the BIOS version and the name of all of the embroidery files. The user will then be presented with a list of file that they can preview or download. The preview and download flows will be covered later.

```
(Start)
R57FF80 --> B4A5000020DF002B797D03700FCE0C08535332FF0370FFFFFFFFFFFFFFFFFFFF

N200100 --> "NMMV03.01·English     ·Bernina Electronic  AG  ·July 98"
    This read gets the firmware version of the machine. It's done very early. You can use a short "R" command to get just the version number, "NMMV03.01" but using the "N" command gets you the Version, Language, Manufacturer and Firmware date.

R024004 --> 00000000004B0000000000000000000000000000004B00000000000000000000
RFFFED9 --> 8300330000000000000000000000000200000100000000000000000000000000
R024040 --> 000206A000020764000000000000000000000000000000000000000000000000
R0206A0 --> 1D1B2A0380002C070200000305000000FFFFFF00FC6000000000000000000000
WFFFED000A1? --> Write 00A1 at FFFED0.
RFFFED0 --> 00020000004000000083006300000000000000000000000003000001A0000000
W0201DC01? --> Write 01 to 0201DC
W0201E100? --> Write 00 to 0201E1       (Causes page 0 to be loaded into RAM?)

WFFFED00031? --> Write 0031 to FFFED0   (Causes the DMA transfer to start?)
RFFFED0 --> 0002000000400000008300630000000000000000000000000300000130000000
WFFFED00021? --> Write 0021 to FFFED0   (Causes the DMA transfer to start?)
RFFFED0 --> 0002000000400000008300630000000000000000000000000300000120000000

R024080 --> 2D00000000000000000000000000000000000000000000000000000000908F93
R024004 --> 00FFFED4000203FC000282AE00020422000201DC0002408000FFFEE400020424
RFFFEDB --> 6300000000000000000000000003000001200000000000000000000000000000
W0201E100? --> Write 00 to 0201E1

(Start reading page 1, R0240B9 to R024415)

R0240B9 --> ACACACACACACACA4A4A4A4A4A4A4A4A4A4A4A4A4A4A4A4A4A4A4A40044726966
R0240D5 --> 4472696674657276343552657636000000000000000000000000000000000000
N0240F5 -->
   ASCII: LisaV45Rev8.....................BlackBoardv45Rev61..............SwissBlock v45 rev6a............Zurichv45rev6a..................ALICE v6m Rev5 Firmware v3......BAMBOO v6m Rev8 Firmware v3.....Lg5060..........................Cs021...........................
   HEX: 4C 69 73 61 56 34 35 52 65 76 38 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 42 6C 61 63 6B 42 6F 61 72 64 76 34 35 52 65 76 36 31 00 00 00 00 00 00 00 00 00 00 00 00 00 00 53 77 69 73 73 42 6C 6F 63 6B 20 76 34 35 20 72 65 76 36 61 00 00 00 00 00 00 00 00 00 00 00 00 5A 75 72 69 63 68 76 34 35 72 65 76 36 61 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 41 4C 49 43 45 20 76 36 6D 20 52 65 76 35 20 46 69 72 6D 77 61 72 65 20 76 33 00 00 00 00 00 00 42 41 4D 42 4F 4F 20 76 36 6D 20 52 65 76 38 20 46 69 72 6D 77 61 72 65 20 76 33 00 00 00 00 00 4C 67 35 30 36 30 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 43 73 30 32 31 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
N0241F5 -->
   ASCII: Fl081...........................Nv772...........................Nv722...........................Nv799...........................Bd130...........................Bd115v2.........................Cr070...........................Cr060...........................
   HEX: 46 6C 30 38 31 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 4E 76 37 37 32 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 4E 76 37 32 32 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 4E 76 37 39 39 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 42 64 31 33 30 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 42 64 31 31 35 76 32 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 43 72 30 37 30 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 43 72 30 36 30 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
N0242F5 -->
   ASCII: Nv850...........................Nv810...........................935125..........................965080..........................19V2Rose-Spray..................Me028...........................flower 2172.....................Me009...........................
   HEX: 4E 76 38 35 30 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 4E 76 38 31 30 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 39 33 35 31 32 35 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 39 36 35 30 38 30 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 31 39 56 32 52 6F 73 65 2D 53 70 72 61 79 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 4D 65 30 32 38 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 66 6C 6F 77 65 72 20 32 31 37 32 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 4D 65 30 30 39 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
R0243F5 --> 3938353038390000000000000000000000000000000000000000000000000000
R024415 --> 706D303038000000000000000000000000000000000000000000000000000000

(Done reading page 1, we need to swap to page 2)

L0240D5000360 --> Response: 00004CC9    (This could be LOAD 0x360 of data at 0240D500)
W0201E101? --> Write 01 to 0201E1       (Causes page 1 to be loaded into RAM?)
WFFFED00061? --> Write 0061 to FFFED0   (Causes the DMA transfer to start?)
RFFFED0 --> 0002000000400000008300630000000000000000000000000300000160000000

(Start reading page 2, R0240B9 to R0242F5)

R0240B9 --> A4A4A4A4A4A4A4A4A4A4A4A4A4A4A4868686A4A4A4A4A4A4A4A4A40052633239
R0240D5 --> 5263323938763200000000000000000000000000000000000000000000000000
N0240F5 -->
   ASCII: 10FanLG.........................Nv735...........................V2EmbScissLG....................dm27 ...........................me014...........................995183..........................Sp362...........................Sp533...........................
   HEX: 31 30 46 61 6E 4C 47 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 4E 76 37 33 35 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 56 32 45 6D 62 53 63 69 73 73 4C 47 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 64 6D 32 37 20 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 6D 65 30 31 34 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 39 39 35 31 38 33 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 53 70 33 36 32 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 53 70 35 33 33 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
N0241F5 -->
   ASCII: Wl318...........................Wm244...........................lb109...........................Fl572...........................Wl998...........................EPB35...........................Cr070...........................Cr060...........................
   HEX: 57 6C 33 31 38 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 57 6D 32 34 34 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 6C 62 31 30 39 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 46 6C 35 37 32 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 57 6C 39 39 38 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 45 50 42 33 35 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 43 72 30 37 30 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 43 72 30 36 30 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
R0242F5 --> 4E76383530000000000000000000000000000000000000000000000000000000

(Competed reading page 2)

L0240D5000240 --> Response: 00001C79    (This could be LOAD 0x240 of data at 0240D500)
R024040 --> 000206A000020764000000000000000000000000000000000000000000000000
R0206A0 --> 1D1B2A0300002C070200000305000000FFFFFF00FC6000000000000000000000
WFFFED00101? --> Write 0101 to FFFED0
RFFFED0 --> 000000000040000000830063000000000000000000000000FF00000100000000
(End)
```

## The odd FFFED0 address

Still looking into this:

```
(Other commands)
WFFFED00031? --> Write 0031 to FFFED0
RFFFED0 --> 0002000000400000008300630000000000000000000000000300000130000000
WFFFED00021? --> Write 0021 to FFFED0
RFFFED0 --> 0002000000400000008300630000000000000000000000000300000120000000
(Other commands)
WFFFED00061? --> Write 0061 to FFFED0
RFFFED0 --> 0002000000400000008300630000000000000000000000000300000160000000
(Other commands)
WFFFED00101? --> Write 0101 to FFFED0
RFFFED0 --> 000000000040000000830063000000000000000000000000FF00000100000000
(End)
```
