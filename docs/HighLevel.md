# High Level Protocol

Now that we have the basics of the serial protocol understood, we can build code that reads an area of memory, writes at a location. You can build a low-level stack in code that will do all that and we now need to figure out the high level workings of the machine.

The machine uses the EXP format internally:
https://www.appropedia.org/EXP_Embroidery_File_Format
https://edutechwiki.unige.ch/en/Embroidery_format_EXP

I noticed that the EXP files normally end with 0x8081 which is code to "Stop". If a EXP file does not end with this, the software will add it at the end of the file before uploading to the machine. It will then remove the ending 0x8081 when downloading and saving the file. We probably want to make sure the 0x8081 is present when updating to the machine.

## List Files in Internal Memory

This sequence is performed when you first enter the software and reads the BIOS version and the name of all of the embroidery files. It will read both read-only and read/write files. The user will then be presented with a list of file that they can preview or download. The preview and download flows will be covered later.

- Delect the baud rate the connect to the machine, uses RF? for detection, `TrMEJ05` to change baud rate.
- Check if in embroidery mode and change to it , uses `R57FF80` and `TrMEYQ`.
- Read the embroidery module firmware version, uses `N200100`.

- Peforms this unknown read
```
R024004 --> 00000000004B0000000000000000000000000000004B00000000000000000000
```

- Check if a PC Card is in the slot, 0x82 = No PC Card, 0x83 = Yes, card present.
```
RFFFED9 --> 8300330000000000000000000000000200000100000000000000000000000000  <- PC Card Present
            8200000000000000000000000000000200000100000000000000000000000000  <- No PC Card
```

Read the pointer to the block allocation array. We see 0x0206A0 is the location of the block vector.
```
R024040 --> 000206A000020764000000000000000000000000000000000000000000000000
```

Read allocation of blocks (?), we see 0305 and the 03 is the number of user (Mx) files.
This must indicate if a PC Card or build-in memory is present?
```
R0206A0 --> 1D1B2A0380002C070200000305000000FFFFFF00FC6000000000000000000000
```

- Select if you want to read from the Embroidery Module Memory or PC Card
    - Invoke function 0x00A1 with no arguments to read from Embroidery Module Memory.
    - Invoke function 0x0051 with no arguments to read fro PC Card.

- Invoke function 0x0031 with arguments (01, 00)    (Unknown)
- Invoke function 0x0021 with no arguments.         (Unknown)

- Read the number of files with R024080, the first byte in 0x024080. Here we have 0x2D or 45 files.
```
R024080 --> 2D00000000000000000000000000000000000000000000000000000000908F93
```

- Unknown more unknown read/writes
```
R024004 --> 00FFFED4000203FC000282AE00020422000201DC0002408000FFFEE400020424
RFFFEDB --> 6300000000000000000000000003000001200000000000000000000000000000
W0201E100? --> Write 00 to 0201E1
```

- Next, read the file types. This has one byte for each file with the bits indicating what type of file it is.
```
AC = 2 block, Readonly, Alphabet - 10101100    (7 of them, 1 to 7)
A4 = 1 block, Readonly           - 10100100    (20 of them)
86 = 1 block, Memory             - 10000110    

R0240B9 --> ACACACACACACACA4A4A4A4A4A4A4A4A4A4A4A4A4A4A4A4A4A4A4A40044726966
```

- Download and hash addr 0x0240D5, length 0x000360 
- Invoke function 0x0061 with arguments (01) - "Move 1 Page Forward" - Load second page of file names.

- Next, read the file types again. This has one byte for each file with the bits indicating what type of file it is. Read until you hit the 0x00 byte, note that not all of them will be used as we only have 45 files total.
```
AC = 2 block, Readonly, Alphabet - 10101100    (None)
A4 = 1 block, Readonly           - 10100100    (15 of them)
86 = 1 block, Memory             - 10000110    (3 of them)

R0240B9 --> A4A4A4A4A4A4A4A4A4A4A4A4A4A4A4868686A4A4A4A4A4A4A4A4A40052633239
```
- Download and hash addr 0x0240D5, length 0x000240
- Do this?
```
R024040 --> 000206A000020764000000000000000000000000000000000000000000000000
R0206A0 --> 1D1B2A0300002C070200000305000000FFFFFF00FC6000000000000000000000
```
- Invoke function 0x0101 with no arguments.
- Close embroidery session using `TrME`


## Read Files from Embroidery Modules Memory (Details)

Here is an example startup sequence in more detail

```
(Start)
(Check that we are in embroidery mode)
R57FF80 --> B4A5000020DF002B797D03700FCE0C08535332FF0370FFFFFFFFFFFFFFFFFFFF

(Read the embroidery module firmware version)
N200100 --> "NMMV03.01·English     ·Bernina Electronic  AG  ·July 98"
    This read gets the firmware version of the machine. It's done very early. You can use a short "R" command to get just the version number, "NMMV03.01" but using the "N" command gets you the Version, Language, Manufacturer and Firmware date.

(Don't know what this is)
R024004 --> 00000000004B0000000000000000000000000000004B00000000000000000000
RFFFED9 --> 8300330000000000000000000000000200000100000000000000000000000000

Read pointer to block management vector, 0x0206A0
R024040 --> 000206A000020764000000000000000000000000000000000000000000000000

Read block management ventor, here 0305, the 03 is number of user files.
R0206A0 --> 1D1B2A0380002C070200000305000000FFFFFF00FC6000000000000000000000

WFFFED000A1? --> Write 00A1 at FFFED0   (Invoke Function 0x00a1)  - Indicate we want to read from embroidery module memory, not the PC Card
RFFFED0 --> 00020000004000000083006300000000000000000000000003000001A0000000

W0201DC01? --> Write 01 to 0201DC
W0201E100? --> Write 00 to 0201E1
WFFFED00031? --> Write 0031 to FFFED0   (Invoke Function 0x0031)
RFFFED0 --> 0002000000400000008300630000000000000000000000000300000130000000

WFFFED00021? --> Write 0021 to FFFED0   (Invoke Function 0x0021)
RFFFED0 --> 0002000000400000008300630000000000000000000000000300000120000000

Read the number of files, we have 0x2D or 45 files.
R024080 --> 2D00000000000000000000000000000000000000000000000000000000908F93

Unknown read
R024004 --> 00FFFED4000203FC000282AE00020422000201DC0002408000FFFEE400020424
RFFFEDB --> 6300000000000000000000000003000001200000000000000000000000000000
W0201E100? --> Write 00 to 0201E1       (Set argument 1 to zero)

Read File Types, 1 byte per file. There are the 27 first files.
R0240B9 --> ACACACACACACACA4A4A4A4A4A4A4A4A4A4A4A4A4A4A4A4A4A4A4A40044726966

(Readonly page contains 27 (0x1B) element names, 32 bytes each, these are the readonly files)
(Start block read of readonly page, Addr 0240D5 for length 0x360, 0x0240D5 to 0x024435)
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
L0240D5000360 --> Response: 00004CC9    (SUM Page, Addr 0240D5 for length 0x360)
(Completed block reading readonly page)

W0201E101? --> Write 01 to 0201E1       (Set argument to 0x01)
WFFFED00061? --> Write 0061 to FFFED0   (Invoke Function 0x0061)
RFFFED0 --> 0002000000400000008300630000000000000000000000000300000160000000

Read File Types, 1 byte per file. There are the 18 next files, rest is unused.
R0240B9 --> A4A4A4A4A4A4A4A4A4A4A4A4A4A4A4868686A4A4A4A4A4A4A4A4A40052633239

(Readwrite page contains 18 (0x12) element names, 32 bytes each, these are the read/write files)
(Start block read of page 2Readwrite Addr 0240D5 for length 0x240, 0x0240D5 to 0x024315) 
R0240D5 --> 5263323938763200000000000000000000000000000000000000000000000000
N0240F5 -->
   ASCII: 10FanLG.........................Nv735...........................V2EmbScissLG....................dm27 ...........................me014...........................995183..........................Sp362...........................Sp533...........................
   HEX: 31 30 46 61 6E 4C 47 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 4E 76 37 33 35 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 56 32 45 6D 62 53 63 69 73 73 4C 47 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 64 6D 32 37 20 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 6D 65 30 31 34 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 39 39 35 31 38 33 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 53 70 33 36 32 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 53 70 35 33 33 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
N0241F5 -->
   ASCII: Wl318...........................Wm244...........................lb109...........................Fl572...........................Wl998...........................EPB35...........................Cr070...........................Cr060...........................
   HEX: 57 6C 33 31 38 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 57 6D 32 34 34 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 6C 62 31 30 39 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 46 6C 35 37 32 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 57 6C 39 39 38 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 45 50 42 33 35 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 43 72 30 37 30 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 43 72 30 36 30 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
R0242F5 --> 4E76383530000000000000000000000000000000000000000000000000000000
L0240D5000240 --> Response: 00001C79    (SUM Page, Addr 0240D5 for length 0x240)
(Completed block reading readwrite page)

Read pointer to block management vector, 0x0206A0
R024040 --> 000206A000020764000000000000000000000000000000000000000000000000

Read block management ventor, here 0305, the 03 is number of user files.
R0206A0 --> 1D1B2A0300002C070200000305000000FFFFFF00FC6000000000000000000000

WFFFED00101? --> Write 0101 to FFFED0   (Invoke Function 0x0101)
RFFFED0 --> 000000000040000000830063000000000000000000000000FF00000100000000

(End)
```

## Read Files from the Add-in Card

TrMEYQ --> Acknowledged
WFFFED00051? --> Write 0051 to FFFED0 - Indicate we want to read from the PC Card, not the embroidery module memory
RFFFED0 -->
   ASCII: .....@.....3................P...
   HEX: 00 02 00 00 00 40 00 00 00 83 00 33 00 00 00 00 00 00 00 00 00 00 00 00 03 00 00 01 50 00 00 00
W0201DC01? --> Write 01 to 0201DC
W0201E100? --> Write 00 to 0201E1
WFFFED00031? --> Write 0031 to FFFED0
RFFFED0 -->
   ASCII: .....@.....3................0...
   HEX: 00 02 00 00 00 40 00 00 00 83 00 33 00 00 00 00 00 00 00 00 00 00 00 00 03 00 00 01 30 00 00 00
WFFFED00021? --> Write 0021 to FFFED0
RFFFED0 -->
   ASCII: .....@.....3................ ...
   HEX: 00 02 00 00 00 40 00 00 00 83 00 33 00 00 00 00 00 00 00 00 00 00 00 00 03 00 00 01 20 00 00 00
R024080 --> Read the number of files, here 0x11 or 17 files.
   ASCII: ................................
   HEX: 11 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 90 8F 93
R024004 -->
   ASCII: ..............."......@........$
   HEX: 00 FF FE D4 00 02 03 FC 00 02 82 AE 00 02 04 22 00 02 01 DC 00 02 40 80 00 FF FE E4 00 02 04 24
RFFFEDB -->
   ASCII: 3................ ..............
   HEX: 33 00 00 00 00 00 00 00 00 00 00 00 00 03 00 00 01 20 00 00 00 00 00 00 00 00 00 00 00 00 00 00
W0201E100? --> Write 00 to 0201E1
R0240B9 --> Read the file types, 5 fonts (AC) and 12 images (A4)
   ASCII: ............................Drif
   HEX: AC AC AC AC AC A4 A4 A4 A4 A4 A4 A4 A4 A4 A4 A4 A4 86 86 86 86 A4 A4 A4 A4 A4 A4 00 44 72 69 66
R0240D5 --> Read file names, 17 of them  at 32 bytes each.
   ASCII: Drifterv4-5Rev6.................
   HEX: 44 72 69 66 74 65 72 76 34 2D 35 52 65 76 36 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
N0240F5 -->
   ASCII: Lisav4-5Rev66...................BlackBoardv4-5Rev6..............SwissBlock v4-5 rev6a...........Zurichv45rev6a..................1    ...........................2    ...........................3    ...........................4    ...........................
   HEX: 4C 69 73 61 76 34 2D 35 52 65 76 36 36 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 42 6C 61 63 6B 42 6F 61 72 64 76 34 2D 35 52 65 76 36 00 00 00 00 00 00 00 00 00 00 00 00 00 00 53 77 69 73 73 42 6C 6F 63 6B 20 76 34 2D 35 20 72 65 76 36 61 00 00 00 00 00 00 00 00 00 00 00 5A 75 72 69 63 68 76 34 35 72 65 76 36 61 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 31 20 20 20 20 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 32 20 20 20 20 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 33 20 20 20 20 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 34 20 20 20 20 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
N0241F5 -->
   ASCII: 5    ...........................6    ...........................7    ...........................8    ...........................9    ...........................10   ...........................11   ...........................12   ...........................
   HEX: 35 20 20 20 20 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 36 20 20 20 20 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 37 20 20 20 20 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 38 20 20 20 20 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 39 20 20 20 20 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 31 30 20 20 20 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 31 31 20 20 20 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 31 32 20 20 20 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
L0240D5000220 --> Sum starting at 0240D5 with length 000220 is 000024B0
WFFFED00101? --> Write 0101 to FFFED0
RFFFED0 -->
   ASCII: .....@.....3....................
   HEX: 00 00 00 00 00 40 00 00 00 83 00 33 00 00 00 00 00 00 00 00 00 00 00 00 FF 00 00 01 00 00 00 00

Session Ended - 2025-10-25 00:15:24.148

## Delete a File in Internal Memory (Not PC Card)

TrMEYQ --> Acknowledged (Switch to Embroidery Mode)
WFFFED00041? --> Write 0041 to FFFED0 - Unknown call, this is unique to delete.
RFFFED0 -->
   ASCII: .....@.....3................@...
   HEX: 00 02 00 00 00 40 00 00 00 83 00 33 00 00 00 00 00 00 00 00 00 00 00 00 03 00 00 01 40 00 00 00
W0201DC2E? --> Write 2E to 0201DC - File to delete, in this case number 46.
W0201E101? --> Write 01 to 0201E1
WFFFED00801? --> Write 0801 to FFFED0 - Invoke "Delete a file"
RFFFED0 -->
   ASCII: .....@.....3................@...
   HEX: 00 02 00 00 00 40 00 00 00 83 00 33 00 00 00 00 00 00 00 00 00 00 00 00 04 00 00 01 40 00 00 00

WFFFED000A1? --> Write 00A1 to FFFED0 - Indicate we want to read from embroidery module, not PC card.

At this point, we re-read all of the files in memory just like the normal startup sequence.

## Get the Preview Image for a File

TrMEYQ --> Acknowledged
WFFFED000A1? --> Write 00A1 to FFFED0 - Indicate we want to read from embroidery module, not PC card.
RFFFED0 -->
   ASCII: .....@.....c....................
   HEX: 00 02 00 00 00 40 00 00 00 83 00 63 00 00 00 00 00 00 00 00 00 00 00 00 03 00 00 01 A0 00 00 00
W0201DC01? --> Write 01 to 0201DC
W0201E100? --> Write 00 to 0201E1
WFFFED00031? --> Write 0031 to FFFED0
RFFFED0 -->
   ASCII: .....@.....c................0...
   HEX: 00 02 00 00 00 40 00 00 00 83 00 63 00 00 00 00 00 00 00 00 00 00 00 00 03 00 00 01 30 00 00 00
WFFFED00021? --> Write 0021 to FFFED0
RFFFED0 -->
   ASCII: .....@.....c................ ...
   HEX: 00 02 00 00 00 40 00 00 00 83 00 63 00 00 00 00 00 00 00 00 00 00 00 00 03 00 00 01 20 00 00 00
R024080 --> Read the number of files in memory, there are 45 (0x2D) right now.
   ASCII: -...............................
   HEX: 2D 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 90 8F 93
R024480 --> This is a new read unique to preview
   ASCII: ...>............................
   HEX: 01 00 09 3E FF 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00

Start reading the preview data, 0x2452E for a length of 558 (0x22E) bytes.
R02452E -->
   ASCII: ................................
   HEX: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
N02454E --> (Here, the capture is not correct as it's not capturing the full 8 bit values)
   ASCII: ..........................................................................................................................?...............................?........?......................?........?............................................................
   HEX: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 07 3F 00 00 00 00 7F 00 00 0F 3F 0F 3F 3F 01 3F 3F 00 1F 3F 1F 3F 3F 03 3F 3F 00 3F 3F 1F 3F 3F 0F 3F 3F 00 7F 3F 1F 3F 3F 0F 3F 3F 00 3F 3F 1F 3F 3F 1F 3F 3F 01 3F 3F 1F 3F 3F 3F 3F 3F 01 3F 3F 1F 3F 3F 3F 3F 3F 03 3F 3F 1F 3F 3F 7F 3F 3F 03 3F 7F 3F 3F 3F 7F 00 3F 07 3F 3F 3F 3F 3F 3F 00 00 07 3F 3F 3F 3F 3F 3F 00 00 0F 3F 1F 3F 3F 3F 3F 00 00 0F 3F 1F 3F 3F 3F 3F 00 00 1F 3F 1F 3F 3F 3F 3F 00 00 1F 3F 3F 3F 3F 3F 3F 00 00 1F 3F 3F 3F 3F 3F 3F 00 00 1F 3F 3F 3F 3F 3F 3F 00 00
N02464E --> (Here, the capture is not correct as it's not capturing the full 8 bit values)
   ASCII: ?........?........?........?........?........?....?...?....?..<?....?...?........?.....?..?........?........?.?........?........?...............................................................................................................................
   HEX: 3F 3F 3F 3F 3F 3F 3F 00 00 3F 3F 3F 3F 3F 3F 3F 00 00 3F 3F 3F 3F 3F 3F 3F 00 08 3F 3F 1F 3F 3F 7F 3F 00 0C 3F 3F 0F 3F 3F 7F 3F 00 1C 3F 3F 0F 3F 3F 3F 3F 3F 1C 3F 3F 0F 3F 3F 3F 3F 3F 3C 3F 3F 0F 3F 3F 3F 3F 3F 3F 3F 3F 0F 3F 3F 7F 3F 3F 3F 3F 3F 1F 3F 3F 3F 3F 3F 3F 3F 3F 1F 3F 3F 3F 1F 3F 3F 3F 3F 1F 3F 3F 3F 1F 3F 3F 3F 3F 3F 3F 3F 3F 0F 3F 3F 1F 3F 3F 3F 3F 3F 07 3F 3F 1F 3F 3F 3F 3F 3F 03 3F 3F 00 00 00 1F 3F 3F 00 3F 3F 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
R02474E -->
   ASCII: ................................
   HEX: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
L02452E00022E --> 0000B25B

WFFFED00101? --> Write 0101 to FFFED0   - End Session / Cler State Invocation
RFFFED0 -->
   ASCII: .....@.....c....................
   HEX: 00 00 00 00 00 40 00 00 00 83 00 63 00 00 00 00 00 00 00 00 00 00 00 00 FF 00 00 01 00 00 00 00

## Preview Image Location

In memory the preview image location is 02452E0 + (0x22E * FileIndex)

L02452E00022E -> 1
L02475C00022E -> 2
L02498A00022E -> 3
L024BB800022E -> 4
L0265E000022E -> 16
L02680E00022E -> 17
L026A3C00022E -> 18
L027BAC00022E -> 26
L027DDA00022E -> 27

L02452E00022E -> 28
L026A3C00022E -> 45

## Preview Image Decoding

72 x 62 pixels black & white image. 558 (0x22E) bytes long.

00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 1C 00 00 00 00 00 00 00 00 7E 00 00 00 00 00 00 00 00 FF 00 00 00 00 00 00 00 03 FF C0 00 00 00 00 00 01 CF E7 F3 80 00 00 00 00 03 FF C3 FF C0 00 00 00 00 07 FF 00 FF C0 00 00 00 00 0F FC 00 3F F8 00 00 00 00 1F F0 00 0F F8 00 00 00 00 1F 80 00 00 F8 00 00 00 01 1F 00 00 00 F8 00 00 00 01 FF 00 00 00 FF 80 00 00 03 FF 00 00 00 7F C0 00 00 1F FE 00 00 00 7F F8 00 00 3F F8 00 00 00 3F FC 00 00 3F 00 00 00 00 00 FC 00 00 3E 00 00 00 00 00 7C 00 00 3E 00 00 00 00 00 7C 00 00 1F 00 00 00 00 00 F8 00 00 1F 80 00 00 00 00 F8 00 00 07 80 00 00 00 01 E0 00 00 03 80 00 00 00 01 C0 00 00 03 80 00 00 00 01 C0 00 00 03 C0 00 00 00 01 C0 00 00 03 C0 00 00 00 03 C0 00 00 03 C0 00 00 00 03 C0 00 00 03 C0 00 00 00 03 C0 00 00 03 C0 00 00 00 03 C0 00 00 03 C0 00 00 00 03 C0 00 00 03 C0 00 00 00 01 E0 00 00 03 C0 00 00 00 01 E0 00 00 03 80 00 00 00 01 E0 00 00 03 80 00 00 00 01 E0 00 00 03 C0 00 00 00 01 E0 00 00 03 C0 00 00 00 01 E0 00 00 03 C0 00 00 00 03 C0 00 00 03 C0 00 00 00 03 C0 00 00 03 C0 00 00 00 03 C0 00 00 03 C0 00 00 00 03 C0 00 00 01 E0 00 00 00 03 C0 00 00 01 E0 00 00 00 07 80 00 00 01 F0 00 00 00 0F 80 00 00 00 F8 00 00 00 1F 80 00 00 00 FE 00 00 00 7F 00 00 00 00 7F D8 00 1B FE 00 00 00 00 1F F8 00 1F FC 00 00 00 00 07 F8 00 1F F0 00 00 00 00 01 FC 00 3F 80 00 00 00 00 01 FE 00 3F 80 00 00 00 00 03 FF 00 7F C0 00 00 00 00 07 EF 81 F7 E0 00 00 00 00 03 C7 C3 F3 E0 00 00 00 00 00 03 FF E0 00 00 00 00 00 00 01 FF 80 00 00 00 00 00 00 00 FF 00 00 00 00 00 00 00 00 FF 00 00 00 00 00 00 00 00 FF 00 00 00 00 00 00 00 01 F7 80 00 00 00 00 00 00 01 C3 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00

00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 80 00 00 00 00 00 00 00 00 C0 00 00 00 03 00 00 00 00 C0 00 00 00 03 00 00 00 07 C0 00 00 00 03 60 00 00 07 D8 00 00 00 03 E0 00 00 07 FC 00 00 00 3F E0 00 01 C3 EC 00 00 00 77 C0 00 01 E1 EC 00 00 00 7B C1 80 00 E3 E0 00 00 00 03 C7 80 1E E3 E0 00 00 00 07 CF 00 1F FF FC 00 00 00 3F FE F0 0F FF FC 00 00 00 3F FF F8 33 FF FC 00 00 00 3F FF A0 3B 9F F8 00 00 00 3F F1 CC 27 47 F8 00 00 00 1F E0 CE 3D 67 FF 78 00 09 FF E6 F6 01 E7 F3 F8 00 0F DF E7 9C 00 07 73 E0 00 07 9D C6 00 00 03 1B C0 00 07 B0 80 00 00 01 FF C0 00 03 FF 80 00 00 03 FF C0 00 03 BF 80 00 00 03 FF F0 00 07 FF 80 00 00 03 F1 FE DB FF 9F 80 00 00 01 EF FF FF CF E7 00 00 00 01 9F 81 FB 07 F3 00 00 00 01 8F 00 08 00 10 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00

## Downloading a File

When we download a file, both the main file and preview icon are downloaded one after the other. Here we are downloading the file number 42 (0x2A).

RF? --> Acknowledged
TrMEYQ --> Acknowledged

WFFFED000A1? --> Write 00A1 to FFFED0    (Indicate we want to read from embroidery module, 0x51 for PC Card)
RFFFED0 -->
   ASCII: .....@.....c....................
   HEX: 00 02 00 00 00 40 00 00 00 83 00 63 00 00 00 00 00 00 00 00 00 00 00 00 03 00 00 01 A0 00 00 00

W0201DC2A? --> Write 2A to 0201DC       (We want file 42 metadata and preview image)
W0201E101? --> Write 01 to 0201E1
WFFFED00061? --> Write 0061 to FFFED0
RFFFED0 -->
   ASCII: .....@.....c................`...
   HEX: 00 02 00 00 00 40 00 00 00 83 00 63 00 00 00 00 00 00 00 00 00 00 00 00 03 00 00 01 60 00 00 00

WFFFED00021? --> Write 0021 to FFFED0
RFFFED0 -->
   ASCII: .....@.....c................ ...
   HEX: 00 02 00 00 00 40 00 00 00 83 00 63 00 00 00 00 00 00 00 00 00 00 00 00 03 00 00 01 20 00 00 00

R024080 -->                             (Read the number of files in storage)
   ASCII: -...............................
   HEX: 2D 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 90 8F 93
R024004 -->
   ASCII: ..............."......@........$
   HEX: 00 FF FE D4 00 02 03 FC 00 02 82 AE 00 02 04 22 00 02 01 DC 00 02 40 80 00 FF FE E4 00 02 04 24
RFFFEDB -->
   ASCII: c................ ..............
   HEX: 63 00 00 00 00 00 00 00 00 00 00 00 00 03 00 00 01 20 00 00 00 00 00 00 00 00 00 00 00 00 00 00
RF? --> Acknowledged

W0201DC2A? --> Write 2A to 0201DC       (We want the complete file 42)
W0201E101? --> Write 01 to 0201E1
WFFFED00401? --> Write 0401 to FFFED0
RFFFED0 -->
   ASCII: .....@.....c................ ...
   HEX: 00 02 00 00 00 40 00 00 00 83 00 63 00 00 00 00 00 00 00 00 00 00 00 00 04 00 00 01 20 00 00 00

The following 6 reads, read a bunch of data before the main file. It could be searching for "40 8A 00 00 01 3C 80 02 0F 4D" which marks the start of the file. The file is 16838 bytes long or 0x41C6 in length.

Interesting: 0x408A + 0x013C = 0x41C6, the exact length of the file. The file could have multiple parts!

It looks like the first 0x408A bytes are the exact .EXP file format that is compatible with InkScape. This is great news as we can import and export right into a modern open source software. The remaining 0x013C bytes looks like extracted instructions that the machine will follow, this part has to be figured out.

This address 0x028E98 looks fixed, hard coded.

R028E98 -->
   ASCII: ................................
   HEX: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
R028EB8 -->
   ASCII: ................................
   HEX: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
R028ED8 -->
   ASCII: ................................
   HEX: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
R028EF8 -->
   ASCII: ................................
   HEX: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
R028F18 -->
   ASCII: ................................
   HEX: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
R028F38 --> (This reads the 16 bytes ahead of the main file)
   ASCII: ..........@....<...M...P........
   HEX: 00 00 00 00 00 00 00 00 00 00 40 8A 00 00 01 3C 80 02 0F 4D 80 02 0F 50 03 FB FE 0D FC F5 06 00

R024480 -->
   ASCII: ...>............................
   HEX: 01 00 09 3E FF 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00

Starts reading the file, it's 16838 (0x41C6) in length. 0x028F48 to 0x02D10E.

R028F48 --> (Start of main file download, this address looks fixed)
   ASCII: ...M...P...............&.$......
   HEX: 80 02 0F 4D 80 02 0F 50 03 FB FE 0D FC F5 06 00 00 10 01 10 01 E4 FC 26 FD 24 FD E4 FB DB F4 19
N028F68 -->
(... many more large reads ...)
R02D0E8 -->
   ASCII: .^..?.`....oQ1.P..0.o....a;?.:<.
   HEX: DE 5E EC E3 3F 0E 60 8D 01 BC BD 6F 51 31 D2 50 E3 EC 30 00 6F 83 0E B3 B2 61 3B 3F B8 3A 3C E5
R02D108 --> (Last read of main file download, last 6 bytes)
   ASCII: 0.g6e...........................
   HEX: 30 BD 67 36 65 AD 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
L028F480041C6 --> 0019EEE9 (Check the main file)

Download the preview image, it's 558 (0x22E) in lengths.
This address 0x02452E looks fixed, hard coded.

R02452E --> (Download the preview image, this address looks fixed)
   ASCII: .............................|0@
   HEX: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 18 00 00 00 00 00 00 00 00 7C 30 40
N02454E -->
N02464E -->
R02474E --> (Last read of preview image)
   ASCII: ................................
   HEX: 80 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
L02452E00022E --> 0000BDAE (Check the preview image)

Session Ended - 2025-10-20 23:29:06.136

## Uploading a File

We are uploading a file of length 4406 (0x1136). Our file starts with:
   80 04 38 EE 80 04 38 EE 38 EE FF FF 00 01 0B FC 0B FC F3 05 F4 06 F4 FD F5...

It ends with:
   ... 00 02 00 FD 00 02 FF 00 01 00 00 01 80 04 EE 32 80 81

Preview Data Length: 558 bytes
Preview Data (Hex):
00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 0F 00 00 03 00 00 00 00 00 1F 80 38 07 C0 00 00 00 00 1F E0 7F 0F E0 00 00 00 00 1F B9 FF 8F E0 00 00 00 00 1F 0F FF DC 70 00 00 00 00 1E 07 EF D8 70 00 00 00 3E 1E 07 C1 FF E0 00 00 00 3F 0E 17 71 FF CE 00 00 00 7F 07 F7 3B F7 FB 80 00 00 7F 03 F6 7B F1 CE 40 00 00 7E 00 76 7B F3 3F E0 00 00 3E 01 F6 41 62 FF E0 00 00 1F C7 E3 F8 65 9F E0 00 03 E7 FE E7 BE 6F FD E0 00 0F F8 F1 CE FB DF F9 A0 00 0F D8 06 DA EC DD F9 E0 00 1F 08 1D D7 FD F9 C1 F0 00 1E 08 F7 9F FF 73 80 5E 00 3E 1F 9F FF 0F F1 80 8F 00 3E 38 F3 F9 FB 91 FF 07 00 3E 7F 87 F0 1F 10 38 07 80 1E 00 0F F7 7E 15 04 07 80 1F 80 3D FF F2 17 CC 07 80 1F C1 FF E6 FE 17 C8 07 80 0F FF FF ED E7 D6 F8 0F 00 03 FF BB EF FF D0 F8 1F 00 01 FE 3B CF EC B0 F8 3F 00 07 F8 7B CF FA 31 F8 7E E0 1F FC 77 C1 F7 63 F7 FF F8 3F 3E F7 C3 FF 47 C9 FF FC 3C 1F F7 CF E6 DF 8D FF FC 3C 0E E7 F9 E3 FF E7 FF FE 3C 07 EF F1 EF FE 7B F0 1E 3C 0F EF 9F FF FD FE 00 0E 3C FF DF 03 F6 03 FB FF 8C 3C 7F FE 07 E7 FF F0 00 88 1E 1F BC 0F C7 FF C0 00 18 0F FF 30 1B C0 FE 00 00 00 07 FE 00 17 80 00 00 00 00 01 F8 00 16 00 00 00 00 00 00 00 00 18 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00

This is the extra data, Length: 316 bytes which includes 8 bytes at the front that has the length in small-endian. The H8 architecture employs big-endian byte ordering so, it's likely this data is not being used by the embroidery module.

00 00 3C 01 00 00 00 00 8F 1C 44 67 96 03 F4 CE 20 05 5B D7 3F B5 F2 61 39 1A EB 7E B8 82 5D 49 17 9B 73 C8 8F 2D 44 67 96 03 C5 FF 20 34 6A E6 0E B5 F2 50 39 1A EB 7E B8 82 5D 49 17 9B 73 C8 8F 2D 44 67 96 03 C5 FF 20 34 6A E6 0E B5 F2 50 39 1A EB 7E B8 82 5D 49 17 9B 73 C8 8F 2D 44 67 96 03 C5 FF 20 34 6A E6 0E B5 F2 50 39 1A EB 7E B8 82 5D 49 17 9B 73 C8 8F 2D 44 67 96 03 C5 FF 20 34 6A E6 0E B5 F2 50 39 1A EB 7E B8 82 5D 49 17 9B 73 C8 8F 2D 44 67 96 03 C5 FF 20 34 6A E6 0E B5 F2 50 39 1A EB 7E B8 82 5D 49 17 9B 73 C8 8F 2D 44 67 96 03 C5 FF 20 34 6A E6 0E B5 F2 50 39 1A EB 7E B8 82 5D 49 17 9B 73 C8 8F 2D 44 67 96 03 C5 FF 20 34 6A E6 0E B5 F2 50 39 1A EB 7E B8 82 5D 49 17 9B 73 C8 8F 2D 44 67 96 03 C5 FF 20 34 6A E6 0E B5 F2 50 39 1A EB 7E B8 82 5D 49 17 9B 73 C8 8F 2D 44 67 96 03 C5 FF 20 34 6A E6 0E B5 F2 50 39 1A EB 7E B8 82 5D 49 15 9B 73 CA 8E 2F 44 66 96 00 C7 FD 22 37 6A E5 0C B5 F1 51 3A 18 E8 7E 34 82 B6 C4 4D FF 73 79

The header writes starts at 0x028E98 with 0x0359 (857) - I don't know what that is.
We write the length of the file 0x00001136 at 0x028F40
We write the length of the extra data at 0x0000013C at (0x028F40 + 4)
We write the file data at (0x028F40 + 8) that 4406 bytes
We write 8 zero bytes at 0x024500
We write the preview data at 0x024508

There are 2 mistery values.
  - The first 2 bytes right at the start of the first write.
  - The write value to 0x02409D, sometimes it's 0x00 to 0x08?

WFFFED00011? --> Write 0011 to FFFED0           (Gets the machine ready for an upload)
RFFFED0 -->
   ASCII: .....@..........................
   HEX: 00 02 00 00 00 40 00 00 00 82 00 00 00 00 00 00 00 00 00 00 00 00 00 00 03 00 00 01 10 00 00 00

(Write to 0x028E98 the main header, 176 bytes total)
  - 2 bytes (Some unknown length, around 5x the length of EXP)
  - 166 0x00 bytes
  - 4 bytes EXP file length (Must end with 0x8081)
  - 4 bytes Extra data length (316 bytes normally)
  - EXP file
  - Extra data

W028E98 0359000000000000000000000000000000000000000000000000000000000000? --> Write 0359000000000000000000000000000000000000000000000000000000000000 to 028E98
W028EB8 0000000000000000000000000000000000000000000000000000000000000000? --> Write 0000000000000000000000000000000000000000000000000000000000000000 to 028EB8
W028ED8 0000000000000000000000000000000000000000000000000000000000000000? --> Write 0000000000000000000000000000000000000000000000000000000000000000 to 028ED8
W028EF8 0000000000000000? --> Write 0000000000000000 to 028EF8
PS028F --> Machine ready (OE), waiting for upload data
PS028F --> Upload 256 bytes to address 028F:
   ASCII: ...................................................................6...<..8...8.8...............................................................................................................................................................................
   HEX: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 11 36 00 00 01 3C*80 04 
   38 EE 80 04 38 EE 38 EE FF FF 00 01 0B FC 0B FC F3 05 F4 06 F4 FD F5 FD F7 FC F7 FC 0E 03 0D 03 0C 02 0C 03 07 FD 07 FD 03 FA FA 05 F9 05 F5 00 F5 00 0C 00 09 FE 09 FE 07 F8 01 F7 FB F6 F7 F7 00 FF 06 05 05 07 02 05 FF 01 04 00 F9 04 08 00 F6 03 0B 02 F6 00 09 04 F4 FD 0B 06 F7 FC 08 08 F5 F8 09 0C F5 F5 09 0E F5 F3 09 10 F7 F3 05 0E F8 F0 05 11 FB F3 01 0E FB EF 02 13 FB ED 01 14 FC EC 00 14 FE EC FE 13 FF ED FD 12 00 EE FC 11 02 EF FA 10 03 EF F9 0F 04 F1 F9 0E 03 F0 FA 0E 02 F1 FB 0E 01 F1 FC 0E 00 F1 FC 0D 00 F2 FC 0D 00 F3 FD 0C FF F3
PS0290 --> Machine ready (OE), waiting for upload data
PS0290 --> Upload 256 bytes to address 0290:
   ASCII: ................................................................................................................................................................................................................................................................
   HEX: FD 0B FF F5 FE 09 FE F8 FF 08 FD F8 00 07 FD FA 00 06 FC FC 01 04 FD FE 00 03 03 FD FC 02 F9 08 FC 06 FE 09 FF 07 05 0C 06 0A 05 03 F8 F8 FD F9 00 F3 07 F7 0C FD 08 01 07 00 0B 06 0A 06 03 0B 03 0B 00 07 00 08 FF 02 02 F6 02 F6 FC F5 FC F5 F8 FC F8 FC 09 05 05 08 06 08 05 08 FF 08 FF 09 FF F6 FE F7 FF F6 FA FA FA FA F5 FA F8 FF F9 FF FD 03 00 FC 04 04 00 FB 03 05 00 FB 03 06 01 FA 03 08 01 F9 01 08 04 F9 FF 09 05 F8 FD 0A 08 F7 FA 0B 0B F6 F7 0C 0C F6 F7 0D 0C F6 F6 0C 0D F6 F6 0D 0D F6 F4 0C 0E F8 F4 0A 0E F9 F3 0A 0E FA F3 08 0E FC F5 06 0C FF F3 03 0E 01 F1 01 0E 04 F1 FE 0E 07 F2 FC 0C 08 F4 FB 0B 08 F5 FB 09 08 F6 FB 07 08 F8 FC 06 07 F8 FC 04 06 FA FD 02 06 FA FE 00 03 FC FF 01 03 FB FF 02 03 FB FF 01 04 FF FC 00 02 FF FF F9 00 FC FF 01 0D FE 08 FD 08
PS0291 --> Machine ready (OE), waiting for upload data
PS0291 --> Upload 256 bytes to address 0291:
   ASCII: ................................................................................................................................................................................................................................................................
   HEX: F4 FE FE F8 FE F8 F9 00 F9 FF F9 FE FD F9 FD FA 02 F9 00 0A 0B 08 0B 02 03 0B 0A 09 0C FB 02 F9 FF 01 FC 06 FB 06 F9 FC F9 FB F9 F5 F5 FE FE FE 08 03 08 03 05 07 04 06 0B 04 08 F9 FE F4 FE F8 FE 01 03 FF FE 05 04 FF FC 04 06 00 FA 03 09 02 F7 01 0B 04 F7 FF 09 05 F6 FD 09 08 F7 FA 06 08 FA FA 03 09 FA F6 03 0C FB F3 00 0E FD F1 FE 10 00 F4 FC 0B 02 F2 FA 0C 04 F3 F9 0A 05 F4 F7 08 06 F7 F9 04 08 F9 F9 00 06 FE FD FD 03 00 FE FE 02 02 FF FF 01 FE 01 00 00 FD FE 07 FF F9 FC 0B FF F6 FD 0B 01 F5 FB 0C 02 F3 FA 0C 04 F5 F8 0A 08 F4 F5 09 08 F5 F5 08 09 F6 F4 07 0A F7 F5 06 08 FA F7 03 0B FA F5 02 08 FD F7 FF 0A 00 F8 FC 06 01 FC FB 05 04 00 F9 03 03 01 FC 03 03 02 FB 02 03 02 FC 02 03 FF FF 04 FF 0B 01 0A 03 09 06 05 09 04 09 03 0D FE 0B FC 07 FC 06 F6 05 F9 01
PS0292 --> Machine ready (OE), waiting for upload data
PS0292 --> Upload 256 bytes to address 0292:
   ASCII: ................................................................................................................................................................................................................................................................
   HEX: F8 00 F5 FE F6 FC F7 F9 F8 F6 FA F8 F8 F7 F9 F6 F8 F8 F6 F8 FA FC F3 FC FB FF 07 FE F7 FA F8 FA FD F4 FE F4 00 F4 00 F4 FF F5 00 F4 FE F5 FA F9 F9 FA F9 FC F8 FF FE 05 01 08 07 08 07 07 0D 05 0B 02 0A 03 0A 03 0C 02 0C 03 09 02 09 02 08 06 08 06 05 07 04 08 04 07 F9 F6 FA F6 F9 F6 F6 FD F5 FC F6 FD 0A 02 0A 03 09 06 08 07 09 06 03 08 02 08 00 02 00 0C F5 06 F5 00 03 FA 03 06 01 F9 FD 00 FF FE FE FE 04 00 F8 02 0A 00 F5 04 0D FE F5 07 0B FD F8 07 09 FB FF 05 03 FB 00 05 00 FC 03 03 FF FD 05 02 FD FD 05 00 FC FE 07 FF FA FF 0A FB F6 00 0B FC F5 01 0C FB F4 01 0B FB F3 02 0C FA F2 02 0D FB F1 03 0E F9 F1 04 0D F8 F1 05 0E F8 F0 06 0D F7 F1 07 0D F5 F2 09 0C F4 F2 0A 0B F4 F2 0A 0A F3 F3 0A 0A F4 F3 0A 09 F4 F7 08 05 F6 F7 0C 05 F3 F9 0D 03 F2 F9 0D 03 F1 FA 0D
PS0293 --> Machine ready (OE), waiting for upload data
PS0293 --> Upload 256 bytes to address 0293:
   ASCII: ................................................................................................................................................................................................................................................................
   HEX: 02 F3 FA 0C 01 F3 FB 0C 01 F4 FB 0A 00 F6 FD 09 FF F7 FE 08 FE F7 FE 09 FD F7 FE 09 FF F7 FD 08 FE F9 FE 05 FE FB FD 04 FF FB FE 05 FF FA FF 05 FD FA 00 06 FA FC F8 F9 F9 00 F7 F8 F9 F9 FE F7 00 FA 0D 03 07 09 07 09 03 08 03 08 FF 03 F6 FF F4 01 F6 04 F9 04 F9 05 FD 03 06 FA 07 FC 07 FD 07 FE 08 FE 0D FF 0C 03 0C 02 0C 05 0A 07 08 0A 06 0C 04 0C 01 0D 00 0D FE 05 FA 0D F8 0B F5 08 F7 06 F5 03 F6 02 F5 00 FD FF F6 FB F8 F8 FA F5 05 0A 07 07 08 05 0A 03 0C FF 0C FD 0B FB 09 F9 09 F8 05 FA 04 F6 03 F4 00 F5 FF F5 FD F4 FA F6 F9 F6 F6 F9 F4 FA F3 FD F7 FF FD 08 FE 08 01 0B 01 0B 01 0B 03 07 04 08 08 05 08 04 07 FF 03 F7 FF F8 FC F9 FB F9 F8 FA F9 FD FA FC F6 FB F8 FC F9 FC F8 FC F9 FD F7 FD F7 FD F8 FE F9 FE F6 F9 FA FA 00 FA 0B 02 F9 FD FC 05 02 06 0B 07 07 02
PS0294 --> Machine ready (OE), waiting for upload data
PS0294 --> Upload 256 bytes to address 0294:
   ASCII: ................................................................................................................................................................................................................................................................
   HEX: 07 03 08 03 09 04 08 03 09 03 08 03 08 04 0C 05 06 04 07 04 05 05 05 06 04 07 02 07 01 07 FD 04 FC 02 F4 FC FA F6 FB F6 FF F3 FF F2 FF F3 FE F4 FE F4 F9 F5 FA F8 FF 00 03 FE FE 06 05 FD FD 06 07 FD FA 06 0A FE F8 05 0D FF F3 05 0F FE F2 06 10 FD F0 05 11 FF EF 04 13 00 ED 03 13 01 ED 04 13 00 EE 04 12 00 EE 04 12 00 EE 04 12 00 EE 04 13 00 EC 03 14 01 EC 03 15 01 EB 03 15 01 EC 03 14 01 ED 03 12 00 EE 04 12 00 EF 04 11 FF F0 05 10 FE F1 06 0F FD F1 07 10 FC F1 08 0F FB F4 08 0C FA F6 0A 0B F9 F8 0A 08 F9 FB 09 07 F9 FD 0A 05 F8 FE 0A 04 F8 00 08 03 FA 02 05 01 FE 03 02 00 FD FE 02 F9 02 FE 00 01 03 06 06 07 04 06 04 04 06 04 06 04 07 04 08 03 09 03 08 03 08 02 07 03 09 03 09 06 06 05 06 0A 02 04 FA 04 FA FE F7 FE F7 F5 FB FD 07 FC F8 0C 01 08 08 02 0C FD 07
PS0295 --> Machine ready (OE), waiting for upload data
PS0295 --> Upload 256 bytes to address 0295:
   ASCII: ................................................................................................................................................................................................................................................................
   HEX: FC 07 F8 FD F7 FD 02 02 0C 04 0A F9 FE F5 FE F5 F9 FC F7 00 FE 03 01 05 FE FD 07 04 F7 F6 0B 09 F8 F4 0A 09 FB F7 05 08 00 FA 02 06 FF FA 02 05 01 FB 00 05 04 FB FD 07 07 FD FB 05 0A FF F7 03 0B 00 F7 03 0B 01 F6 01 0A 04 F6 FF 09 07 F6 FC 09 07 F6 FD 09 07 F6 FB 08 07 F6 FB 07 08 F9 FA 03 08 FA F7 01 0B FD F5 FF 0C FE F4 FE 0C 01 F5 FB 09 03 F7 FA 06 06 F7 F6 07 08 F7 F6 05 08 FA F7 02 06 FB F7 01 06 FC F8 00 07 FC F8 00 07 FC F8 01 07 FB F9 01 06 FB F9 01 05 FB FA 01 05 FB FA 01 05 FB FA 01 05 FB F9 01 05 FB FA 01 05 FB FA 01 04 FB FB 01 03 FB FB 02 03 FB FC 01 02 FB FC 01 02 FB FD 01 01 FB FD 01 01 FC FE 01 00 FB FE 01 FF FC FE 01 FF FC FE 02 FF FB FE 02 02 FF FF 00 FA FD FE FF FF 00 F9 FC F6 FE F7 01 FA 05 FD 05 05 F9 0A FC 07 02 08 02 04 07 04 0A FB 0B
PS0296 --> Machine ready (OE), waiting for upload data
PS0296 --> Upload 256 bytes to address 0296:
   ASCII: ................................................................................................................................................................................................................................................................
   HEX: FB 02 F5 00 F8 FD 04 03 0C 01 08 FE 05 F9 01 F7 FE FA 02 04 FE FA FF 09 FE F9 FF 09 FE FA FF 08 FE F9 FE 08 FE F9 FF 09 FE F7 FE 09 00 F7 FC 0A 01 F6 FB 08 02 F7 FA 07 02 F9 FA 04 03 FB F9 03 03 FB FC 03 FA FE F6 FA F7 FA FA F5 FA F5 FE F6 FF F6 FE F6 FE F5 FE F5 FE F9 FE F9 00 FE 04 09 04 0A 04 09 03 0C 02 0D 03 0C 04 09 03 0A 04 09 01 01 FA F6 F9 F6 FA F6 FE F5 FD F6 FE F5 FD F2 FE F3 FB F9 FC FC FE FE FD FF 01 FC 00 08 03 FB FE 07 06 FC FC 07 09 FC F8 07 0A FD F7 07 0C FD F4 07 0F FB F2 09 11 FA F0 09 12 FB EF 09 13 FA EF 09 13 FB ED 08 15 FC EB 08 16 FC EB 07 15 FD EB 07 16 FD EB 06 16 FE EB 06 15 FE EB 06 16 FE EB 06 15 FD EC 06 15 FD EC 07 15 FC EC 08 14 FA ED 0A 14 F9 EE 0B 12 F8 EF 0B 12 F8 F0 0C 10 F7 F2 0C 0D F8 F5 0C 0E F4 F4 0F 0D F3 F5 10 0D F4
PS0297 --> Machine ready (OE), waiting for upload data
PS0297 --> Upload 256 bytes to address 0297:
   ASCII: ........................................./......................................................................................................................................................................................................................
   HEX: F6 0F 0B F4 F9 0E 09 F4 FB 0E 06 F5 FE 0D 05 F6 FF 0C 04 F7 00 0A 02 F9 02 09 01 F9 02 06 00 FC 03 03 FF FE 01 01 00 FF FB 2F 00 FF 00 01 02 09 05 09 06 08 07 04 08 04 0B FF 0C FF 05 FB 05 FA 01 F6 00 F5 F7 FC 04 03 05 0B FD 08 FD 07 F4 04 F3 03 F5 FA F5 FA FD F8 FC F7 02 09 02 09 09 05 08 04 08 00 08 00 0A FC 0A FC 00 F6 01 F5 F9 FD FC FE FE 02 01 FC 01 08 01 F6 02 0C 02 F3 00 0F 04 F2 FE 0D 04 F5 FE 0D 05 F5 FC 0A 07 F9 FA 07 08 FD F6 06 0D FD F6 04 0B 01 F2 01 0E 03 F1 01 0F 04 F3 FE 0B 06 F4 FD 09 07 F3 F9 0B 0B F3 F6 0A 0D F4 F4 09 0F F7 F5 06 0D F9 F5 04 0D F8 EF 04 13 FB F1 01 0F FC EE 00 13 FE F0 FF 10 FF ED FD 13 00 EE FC 12 01 EE FB 12 03 ED F9 10 05 EF F8 0F 05 F0 F7 0D 07 F2 F5 0C 08 F3 F4 0A 0A F4 F4 08 0B F6 F2 07 0B F6 F3 06 0A F7 F5 05 08 F7
PS0298 --> Machine ready (OE), waiting for upload data
PS0298 --> Upload 256 bytes to address 0298:
   ASCII: .............................................................................................................)..................................................................................................................................................
   HEX: F7 05 07 FB F9 01 08 FC F8 FF 08 FF F8 FD 07 FE F9 FD 07 FF FA FD 07 FF FB FD 05 01 FD FD 04 00 FE FD 05 FF FF FE 03 FF FE FD 02 03 FF FF 00 02 00 FF 0F 13 FF 04 02 04 08 00 02 F8 FA FF 02 09 01 FA FB FF FE FF FC 00 02 FE FE 06 04 F8 FD 0C 07 F4 FA 0F 07 F6 FB 0C 09 EF FA 12 09 F1 FC 0C 04 FA FE 03 02 FD FE 03 01 FF 00 01 B8 29 FF FF 00 FD 00 02 FD FA FD 0A FB FB FB FB 09 FF FC 0A F8 F5 00 F6 01 F6 09 F7 07 FE 07 FE 09 01 F7 01 F7 00 FA 08 FA 07 01 09 00 09 08 07 06 FD 03 01 FF FA FF 0B 00 F2 FC 13 01 EB FB 16 02 EA FA 16 03 E9 FA 16 04 EE F9 10 06 EE F7 10 06 F3 F7 0A 0A F2 F5 0B 0A F6 F5 06 0A FB F5 02 10 FC F0 01 0D FE F3 FE 0F 01 F1 FB 0F 02 F2 FA 0F 02 F3 FB 0A 02 F7 FA 0A 04 F8 F9 0A 06 F9 F8 08 05 FA F8 08 06 FB F8 06 05 FF F9 04 05 00 F8 03 06 01 FA
PS0299 --> Machine ready (OE), waiting for upload data
PS0299 --> Upload 256 bytes to address 0299:
   ASCII: ................................................................................................................................................................................................................................................................
   HEX: 02 06 02 FA 00 06 03 FA 00 06 03 FB 01 05 03 FB 01 05 03 FD 00 04 01 00 02 04 FB F5 F8 F7 F7 FA F3 FC F9 FF F9 FE F7 00 F7 00 F7 04 F8 04 FA 07 FB 07 FF 08 FF 08 07 01 07 00 FC F6 08 F8 00 08 00 07 FA F7 04 04 F9 09 FA FB FA FA 05 F8 05 F8 06 FC 07 FC F7 07 F7 07 00 0A 00 0A 0D FE 01 F3 05 FE FF FE 04 01 F6 FF 0D 07 EE F9 10 0D F2 F6 0C 0D F0 F2 0E 11 EF F3 0C 10 F6 F4 07 0D F7 F2 05 0E FC F3 01 0D FF F0 FD 0F 01 F3 FC 0A 05 F2 F8 0B 06 F6 F9 07 08 F7 F7 05 08 FC F8 FF 0C 01 F4 FA 0D 04 F5 F9 0A 04 F9 F9 09 06 FA F7 09 07 FA F6 09 07 FB F5 07 0A FD F5 05 09 FF F5 04 09 00 F6 04 07 00 F8 04 07 00 F8 04 07 FF F8 05 07 FF F9 05 07 FF F9 04 06 00 FA 04 06 00 FA 04 06 00 FB 03 05 01 FC 03 05 01 FC 03 05 01 FD 02 04 01 FD 03 05 01 FD 02 04 02 FE 01 04 03 FE 01 04
PS029A --> Machine ready (OE), waiting for upload data
PS029A --> Upload 256 bytes to address 029A:
   ASCII: ................................................................................................................................................................................................................................................................
   HEX: 03 FE 00 05 03 FE 00 05 03 FF 01 05 03 00 01 01 02 05 01 06 00 F9 FF F9 FD F4 FD FA FD F9 F8 F7 FA FB F9 FB F7 FB F6 FB F6 FD F7 FC F7 FF F8 FF F4 02 FB 01 0A 04 06 08 00 0E FD 06 FF 00 05 F7 00 F6 FD F9 F6 FB FD 00 0A FE 08 00 08 00 08 02 09 01 08 03 09 03 08 05 08 05 06 05 07 05 03 07 04 06 02 09 03 0A FF 01 FD F6 FD F6 FB F6 FB F5 FB F8 FC F8 F7 F7 F6 F8 F7 FB F6 FB F6 FE F7 FE F6 00 F6 00 F7 04 F6 04 FB 09 FB 08 03 0D 03 0D 0A 09 07 02 08 01 F6 FC F7 FC FB F7 FC F8 02 F5 02 F5 08 F9 09 F9 0C FE 0D FF FF FF F2 03 F3 04 FA 09 F9 0A FA 09 04 0C 04 0B 09 04 09 03 0C FE 00 01 FF FC FD 07 FF FA FD 07 00 F9 FC 07 01 F9 FB 08 03 F7 F8 09 06 F6 F5 09 09 F6 F2 09 0C F5 F0 08 0D F6 EF 07 0E F7 F0 05 0E F8 EF 05 0F F9 EE 03 11 F9 EC 04 13 F9 ED 03 12 FA ED 01 13 FC
PS029B --> Machine ready (OE), waiting for upload data
PS029B --> Upload 256 bytes to address 029B:
   ASCII: ................................................................................................................................................................................................................................................................
   HEX: EC 00 11 FF EF FC 15 03 EC FA 10 03 F1 F9 10 05 F1 F8 12 08 F0 F4 12 09 EF F2 12 0C F0 F0 12 0D F2 F1 0F 0E F5 F0 0D 0E F7 F0 0B 0E F8 EF 09 0E FB F0 07 0E FD F1 06 0E FE F2 04 0D 00 F2 03 0D 01 F3 02 0C 02 F3 02 0C 02 F4 01 0C 04 F4 00 0C 04 F5 FF 0B 05 F5 FF 0B 05 F7 FE 0A 06 F7 FE 0A 05 F8 FE 09 06 F9 FE 08 06 FA FD 07 06 FC FE 05 05 FD FE 05 06 FD FE 05 06 FD FE 05 05 FD FE 05 05 FE FF 04 04 FF FF 03 04 00 00 03 04 00 00 03 01 00 07 08 03 07 04 07 01 02 FC F9 FC F8 FA F9 FB F8 FC F8 FC F8 FC F5 FD F5 FD F6 FD F7 FD F7 FD F6 FA FA FB F9 F8 FC F9 FC F6 FF F6 00 F9 01 F9 02 FB 05 FA 04 FE 0C FD 0B 04 0A 03 0B 0B 04 0B 03 0A FE 0A FE 05 F9 05 F9 00 F2 FF 09 00 0A F7 05 F8 05 F5 00 F5 01 F7 F9 F7 F9 FE F5 FF F6 03 01 01 0D 02 0D 08 05 08 04 0D 00 0D 00 06 F8
PS029C --> Machine ready (OE), waiting for upload data
PS029C --> Upload 256 bytes to address 029C:
   ASCII: ................................................................................................................................................................................................................................................................
   HEX: 07 F9 00 F2 F9 F6 F4 FD F4 01 FC 02 0A FD 0D 02 FC FF 01 FC 02 06 03 FC 00 07 04 FC FE 06 06 FE FB 04 07 FF FA 03 09 01 F7 03 0A 02 F7 02 0B 04 F3 FE 0B 07 F4 FC 0A 09 F4 F9 09 09 F4 F8 09 0B F5 F7 08 0C F7 F5 06 0C F8 F5 05 0D F9 F5 03 0D FC F6 00 0A FE F7 FE 0A 00 F4 FC 0C 01 F3 FC 0D 00 F3 FC 0E 01 F4 FB 0A 04 F3 F8 0C 06 F3 F6 0B 08 F4 F5 0A 09 F4 F3 09 0B F6 F1 07 0C F7 F1 06 0C F8 F1 05 0E F8 F1 04 0F FA F0 02 0F FB F0 01 10 FC F0 00 11 FE EF FD 11 00 EF FC 11 01 F1 FB 0E 02 F3 FB 0D 02 F5 FA 09 03 FA FB 0A 05 F9 F8 08 06 FB F8 07 04 FC F9 06 04 FD F9 06 06 FD F8 06 07 FE F7 05 08 FF F6 05 0A 00 F6 04 09 00 F6 04 0A 01 F6 02 0C 02 F5 00 0C 05 F4 FE 0E 07 F3 FC 0E 08 F4 FC 0C 07 F5 FC 0C 08 F6 FA 0C 09 F7 FA 0B 09 F7 F9 0B 0A F8 F9 0A 0A F9 F8 0A 0B F9
PS029D --> Machine ready (OE), waiting for upload data
PS029D --> Upload 256 bytes to address 029D:
   ASCII: ................................................................................................................................................................................................................................................................
   HEX: F8 09 0B FA F7 09 0B FB F7 09 0B FB F7 08 0A FB F8 09 0A FB F8 08 09 FC F8 08 09 FC F8 07 09 FD F8 07 09 FD F8 07 08 FD F9 06 08 FE F9 06 09 FE F8 06 09 FD F8 07 09 FD F9 07 09 FD F8 06 09 FD F9 07 08 FD FA 06 07 FE FB 06 07 FD FC 06 05 FE FD 05 04 FE FE 06 03 FD FE 06 04 FC FE 07 05 FC FD 06 05 FD FE 06 04 FE FF 05 03 FE FF 05 03 FF FF 04 02 FF 00 05 02 FE 00 06 02 FE 00 05 02 FF 00 05 01 FF 00 01 01 04 FE FC 80 01 00 00 80 04 0E F9 80 04 0D F9 80 04 0E FA 80 04 0D F9 80 04 0E F9 80 04 0D F9 80 04 0E F9 80 04 0D F9 80 04 0E F9 80 04 0E FA 80 04 0D F9 80 04 0E F9 80 04 0D F9 80 04 0E F9 80 04 0D FA 80 04 0E F9 0D F9 00 01 FE 03 02 FE FE 01 FC 03 F9 01 F8 FF F8 FF F4 FF F8 01 F8 01 F5 04 FB 02 0B FB 0D FD 0E 00 0E 00 0A 02 08 FF 04 FB F8 06 F8 00 F8 FF F8 00
PS029E --> Machine ready (OE), waiting for upload data
PS029E --> Upload 256 bytes to address 029E:
   ASCII: ................................................................................................................................................................................................................................................................
   HEX: F8 00 F8 FF F5 03 F9 01 F8 02 F7 06 FD 02 FE F9 FD F9 F7 FC F7 FB F6 FE F6 FF F3 00 F8 00 FA 01 FC 02 09 FE 0A FF 0E 00 0E 00 09 04 09 04 06 05 05 06 FE F7 F7 FE F6 FE F7 FE F5 FE F4 FE F6 FF F4 04 00 03 00 01 FE FA 06 05 FE FA 06 05 FE FA 05 05 FF FA 05 06 FF FA 04 06 00 F8 04 08 00 F7 04 09 00 F7 04 0A 00 F6 04 0A 00 F6 03 0B 01 F5 02 0C 02 F5 01 0C 03 F4 00 0D 05 F4 FE 0D 06 F4 FD 0D 07 F4 FC 0E 07 F4 FB 0E 0A F3 F9 0F 0B F2 F7 0E 0C F3 F7 0E 0D F4 F4 0F 0F F3 F2 10 11 F2 F1 11 12 F2 F3 11 0F F4 FF 02 00 FF 80 04 01 0D 80 04 01 0E 80 04 01 0D 80 04 01 0E 80 04 02 0D 80 04 01 0D 80 04 01 0E 80 04 01 0D 80 04 01 0E 01 0D FF 02 00 FF FF 01 F8 01 F5 FD F6 FA F6 F8 F8 F9 F9 F8 F6 F7 FA FB FB FB F6 F8 F4 FC F9 FE 08 02 07 02 07 05 07 05 08 07 08 07 08 08 08 08
PS029F --> Machine ready (OE), waiting for upload data
PS029F --> Upload 256 bytes to address 029F:
   ASCII: ..................................................................................................................................................................................1.....................................................5.......................
   HEX: 08 06 07 05 07 04 06 04 0B 02 07 FE FD 01 01 00 80 04 F3 01 80 04 F2 00 80 04 F3 01 80 04 F3 00 80 04 F2 01 80 04 F3 01 80 04 F3 00 80 04 F2 01 80 04 F3 00 80 04 F3 01 80 04 F2 00 F3 01 FF FF 00 01 00 FE 00 FC FA F5 FB F7 F6 F7 F5 FA F9 FC F9 FC F5 FC F3 FF F8 FF FB 01 0B FE 08 01 07 02 07 03 07 02 07 04 07 03 06 04 06 04 08 08 05 0C 04 0A 01 04 00 FF DF B5 00 FF FE FF 02 01 F9 F8 FA F8 FC F6 FC F7 FC F3 FD F9 FD F9 FC FA F6 FA F8 FC F6 00 FC 01 08 FE 0D 05 09 06 06 08 03 07 04 08 04 0D 03 07 02 07 07 0B 06 06 01 01 FE FF 01 01 31 FD 01 00 00 01 FF FD 02 03 06 FF FC FA FE 05 01 04 FE 00 FE FF FF FD 03 05 FE F8 05 0B FE F2 05 0E FF F1 04 0F 00 F6 03 05 FF FE 01 01 FF FD 01 03 FF FF 00 01 35 F3 01 00 FF FC 03 01 01 FF FD FD FF 04 FF 03 01 02 FD FE 08 02 F5 F9
PS02A0 --> Machine ready (OE), waiting for upload data
PS02A0 --> Upload 256 bytes to address 02A0:
   ASCII: ..................................................................-........................................................2....<.......Dg.... .[.?..a9..~..]I..s..-Dg.... 4j....P9..~..]I..s..-Dg.... 4j....P9..~..]I..s..-Dg.... 4j....P9..~..]I..s..-Dg.... 4
   HEX: 0E 04 F5 F7 09 04 FF FD 03 02 FD FE 01 01 FF FF BE 01 FF 01 FD 02 FD FB 03 FF 01 04 01 FD 01 03 FC 01 FD FE 01 01 01 05 04 00 F8 FF 0C FE F3 FE 0E FD F3 FE 0A FF F9 FE 03 00 FB FE 04 01 FF 00 01 00 2D F2 02 00 00 FF 00 01 01 03 08 02 FB F7 00 06 05 01 FA FD FC FF FD 03 01 FC 03 0B 00 F1 04 0F 00 F1 04 10 01 F0 02 0B 01 FD 00 02 00 FD 00 02 FF 00 01 00 00 01 80 04 EE 32 80 81*00 00 3C 01 00 00 00 00 8F 1C 44 67 96 03 F4 CE 20 05 5B D7 3F B5 F2 61 39 1A EB 7E B8 82 5D 49 17 9B 73 C8 8F 2D 44 67 96 03 C5 FF 20 34 6A E6 0E B5 F2 50 39 1A EB 7E B8 82 5D 49 17 9B 73 C8 8F 2D 44 67 96 03 C5 FF 20 34 6A E6 0E B5 F2 50 39 1A EB 7E B8 82 5D 49 17 9B 73 C8 8F 2D 44 67 96 03 C5 FF 20 34 6A E6 0E B5 F2 50 39 1A EB 7E B8 82 5D 49 17 9B 73 C8 8F 2D 44 67 96 03 C5 FF 20 34
W02A100 6AE60EB5F250391AEB7EB8825D49179B73C88F2D44679603C5FF20346AE60EB5? --> Write 6AE60EB5F250391AEB7EB8825D49179B73C88F2D44679603C5FF20346AE60EB5 to 02A100
W02A120 F250391AEB7EB8825D49179B73C88F2D44679603C5FF20346AE60EB5F250391A? --> Write F250391AEB7EB8825D49179B73C88F2D44679603C5FF20346AE60EB5F250391A to 02A120
W02A140 EB7EB8825D49179B73C88F2D44679603C5FF20346AE60EB5F250391AEB7EB882? --> Write EB7EB8825D49179B73C88F2D44679603C5FF20346AE60EB5F250391AEB7EB882 to 02A140
W02A160 5D49179B73C88F2D44679603C5FF20346AE60EB5F250391AEB7EB8825D49179B? --> Write 5D49179B73C88F2D44679603C5FF20346AE60EB5F250391AEB7EB8825D49179B to 02A160
W02A180 73C88F2D44679603C5FF20346AE60EB5F250391AEB7EB8825D49159B73CA8E2F? --> Write 73C88F2D44679603C5FF20346AE60EB5F250391AEB7EB8825D49159B73CA8E2F to 02A180
W02A1A0 44669600C7FD22376AE50CB5F1513A18E87E3482B6C44DFF7379?             --> Write 44669600C7FD22376AE50CB5F1513A18E87E3482B6C44DFF7379 to 02A1A0

This ends the large block that has the header + exp + extra

We write the preview image in a different file starting at 0x024480
  - Write the header which is 174 bytes.
    - Write 0x0000093E (2366) this could indicate the type of the preview, 72x62 black & white.
    - Write 0xFF
    - Write 0x00 169 times.
  - Write the preview image (558 bytes)

Start: 024480
End:   02475C - 732 bytes

W024480 0000093EFF000000000000000000000000000000000000000000000000000000? --> Write 0000093EFF000000000000000000000000000000000000000000000000000000 to 024480
W0244A0 0000000000000000000000000000000000000000000000000000000000000000? --> Write 0000000000000000000000000000000000000000000000000000000000000000 to 0244A0
W0244C0 0000000000000000000000000000000000000000000000000000000000000000? --> Write 0000000000000000000000000000000000000000000000000000000000000000 to 0244C0
W0244E0 0000000000000000000000000000000000000000000000000000000000000000? --> Write 0000000000000000000000000000000000000000000000000000000000000000 to 0244E0

We write the preview image at 024500 + 8 bytes. So, the first 8 bytes of the next write at not part of the preview image.

PS0245 --> Machine ready (OE), waiting for upload data
PS0245 --> Upload 256 bytes to address 0245:
   ASCII: ..............................................................................................................................................................8............................p........p...>........?..q........;........{..@..~.v{.?...>..Ab......
   HEX: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 0F 00 00 03 00 00 00 00 00 1F 80 38 07 C0 00 00 00 00 1F E0 7F 0F E0 00 00 00 00 1F B9 FF 8F E0 00 00 00 00 1F 0F FF DC 70 00 00 00 00 1E 07 EF D8 70 00 00 00 3E 1E 07 C1 FF E0 00 00 00 3F 0E 17 71 FF CE 00 00 00 7F 07 F7 3B F7 FB 80 00 00 7F 03 F6 7B F1 CE 40 00 00 7E 00 76 7B F3 3F E0 00 00 3E 01 F6 41 62 FF E0 00 00 1F C7
PS0246 --> Machine ready (OE), waiting for upload data
PS0246 --> Upload 256 bytes to address 0246:
   ASCII: ..e........o...................................s.^.>........>8.......>.....8......~......=...................................;....?...{..1.~...w..c...?>...G...<........<........<.....{..<........<........<...................0...............................
   HEX: E3 F8 65 9F E0 00 03 E7 FE E7 BE 6F FD E0 00 0F F8 F1 CE FB DF F9 A0 00 0F D8 06 DA EC DD F9 E0 00 1F 08 1D D7 FD F9 C1 F0 00 1E 08 F7 9F FF 73 80 5E 00 3E 1F 9F FF 0F F1 80 8F 00 3E 38 F3 F9 FB 91 FF 07 00 3E 7F 87 F0 1F 10 38 07 80 1E 00 0F F7 7E 15 04 07 80 1F 80 3D FF F2 17 CC 07 80 1F C1 FF E6 FE 17 C8 07 80 0F FF FF ED E7 D6 F8 0F 00 03 FF BB EF FF D0 F8 1F 00 01 FE 3B CF EC B0 F8 3F 00 07 F8 7B CF FA 31 F8 7E E0 1F FC 77 C1 F7 63 F7 FF F8 3F 3E F7 C3 FF 47 C9 FF FC 3C 1F F7 CF E6 DF 8D FF FC 3C 0E E7 F9 E3 FF E7 FF FE 3C 07 EF F1 EF FE 7B F0 1E 3C 0F EF 9F FF FD FE 00 0E 3C FF DF 03 F6 03 FB FF 8C 3C 7F FE 07 E7 FF F0 00 88 1E 1F BC 0F C7 FF C0 00 18 0F FF 30 1B C0 FE 00 00 00 07 FE 00 17 80 00 00 00 00 01 F8 00 16 00 00 00 00 00 00 00 00 18 00 00 00
   
W024700 0000000000000000000000000000000000000000000000000000000000000000? --> Write 0000000000000000000000000000000000000000000000000000000000000000 to 024700
W024720 0000000000000000000000000000000000000000000000000000000000000000? --> Write 0000000000000000000000000000000000000000000000000000000000000000 to 024720
W024740 00000000000000000000000000000000000000000000000000000000? --> Write 00000000000000000000000000000000000000000000000000000000 to 024740

Completed writing the preview image

W02409D01? --> Write 01 to 02409D       (This looks like the file size, but not sure what the units are??)
W0240B9A4? --> Write A4 to 0240B9       (This is always 0xA4)

Here, we write the name of the file at 0x0240D5. In this case "3131" is "11"

W0240D5 3131000000000000000000000000000000000000000000000000000000000000? --> Write 3131000000000000000000000000000000000000000000000000000000000000 to 0240D5

WFFFED00201? --> Write 0201 to FFFED0    (Write the new file)
RFFFED0 -->
   ASCII: .....@..........................
   HEX: 00 02 00 00 00 40 00 00 00 82 00 00 00 00 00 00 00 00 00 00 00 00 00 00 04 00 00 01 10 00 00 00

At this point, we start reloading metadata of all of the files.

WFFFED000A1? --> Write 00A1 to FFFED0    (Indicate we want to read from embroidery module)
RFFFED0 -->
   ASCII: .....@.....c....................
   HEX: 00 02 00 00 00 40 00 00 00 82 00 63 00 00 00 00 00 00 00 00 00 00 00 00 03 00 00 01 A0 00 00 00

W0201DC01? --> Write 01 to 0201DC
W0201E100? --> Write 00 to 0201E1
WFFFED00031? --> Write 0031 to FFFED0
RFFFED0 -->
   ASCII: .....@.....c................0...
   HEX: 00 02 00 00 00 40 00 00 00 82 00 63 00 00 00 00 00 00 00 00 00 00 00 00 03 00 00 01 30 00 00 00

WFFFED00021? --> Write 0021 to FFFED0
RFFFED0 -->
   ASCII: .....@.....c................ ...
   HEX: 00 02 00 00 00 40 00 00 00 82 00 63 00 00 00 00 00 00 00 00 00 00 00 00 03 00 00 01 20 00 00 00

R024080 -->  (Read the number of file, 46 in this case)
   ASCII: ................................
   HEX: 2E 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 90 8F 93
R024004 -->
   ASCII: ..............."......@........$
   HEX: 00 FF FE D4 00 02 03 FC 00 02 82 AE 00 02 04 22 00 02 01 DC 00 02 40 80 00 FF FE E4 00 02 04 24
RFFFEDB -->
   ASCII: c................ ..............
   HEX: 63 00 00 00 00 00 00 00 00 00 00 00 00 03 00 00 01 20 00 00 00 00 00 00 00 00 00 00 00 00 00 00
W0201E100? --> Write 00 to 0201E1
R0240B9 -->
   ASCII: ............................Drif
   HEX: AC AC AC AC AC AC AC A4 A4 A4 A4 A4 A4 A4 A4 A4 A4 A4 A4 A4 A4 A4 A4 A4 A4 A4 A4 00 44 72 69 66
R0240D5 -->
   ASCII: Drifterv45Rev6..................
   HEX: 44 72 69 66 74 65 72 76 34 35 52 65 76 36 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
DEBUG: command='N0240F5' (len=7), machineResponseBytes.Count=258
DEBUG: First 10 response bytes as hex: 35 4C 69 73 61 56 34 35 52 65
N0240F5 --> [256 bytes]
   ASCII: LisaV45Rev8.....................BlackBoardv45Rev61..............SwissBlock v45 rev6a............Zurichv45rev6a..................ALICE v6m Rev5 Firmware v3......BAMBOO v6m Rev8 Firmware v3.....Lg5060..........................Cs021...........................
   HEX: 4C 69 73 61 56 34 35 52 65 76 38 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 42 6C 61 63 6B 42 6F 61 72 64 76 34 35 52 65 76 36 31 00 00 00 00 00 00 00 00 00 00 00 00 00 00 53 77 69 73 73 42 6C 6F 63 6B 20 76 34 35 20 72 65 76 36 61 00 00 00 00 00 00 00 00 00 00 00 00 5A 75 72 69 63 68 76 34 35 72 65 76 36 61 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 41 4C 49 43 45 20 76 36 6D 20 52 65 76 35 20 46 69 72 6D 77 61 72 65 20 76 33 00 00 00 00 00 00 42 41 4D 42 4F 4F 20 76 36 6D 20 52 65 76 38 20 46 69 72 6D 77 61 72 65 20 76 33 00 00 00 00 00 4C 67 35 30 36 30 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 43 73 30 32 31 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
DEBUG: command='N0241F5' (len=7), machineResponseBytes.Count=258
DEBUG: First 10 response bytes as hex: 35 46 6C 30 38 31 00 00 00 00
N0241F5 --> [256 bytes]
   ASCII: Fl081...........................Nv772...........................Nv722...........................Nv799...........................Bd130...........................Bd115v2.........................Cr070...........................Cr060...........................
   HEX: 46 6C 30 38 31 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 4E 76 37 37 32 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 4E 76 37 32 32 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 4E 76 37 39 39 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 42 64 31 33 30 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 42 64 31 31 35 76 32 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 43 72 30 37 30 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 43 72 30 36 30 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
DEBUG: command='N0242F5' (len=7), machineResponseBytes.Count=258
DEBUG: First 10 response bytes as hex: 35 4E 76 38 35 30 00 00 00 00
N0242F5 --> [256 bytes]
   ASCII: Nv850...........................Nv810...........................935125..........................965080..........................19V2Rose-Spray..................Me028...........................flower 2172.....................Me009...........................
   HEX: 4E 76 38 35 30 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 4E 76 38 31 30 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 39 33 35 31 32 35 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 39 36 35 30 38 30 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 31 39 56 32 52 6F 73 65 2D 53 70 72 61 79 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 4D 65 30 32 38 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 66 6C 6F 77 65 72 20 32 31 37 32 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 4D 65 30 30 39 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
R0243F5 -->
   ASCII: 985089..........................
   HEX: 39 38 35 30 38 39 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
R024415 -->
   ASCII: pm008...........................
   HEX: 70 6D 30 30 38 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00

L0240D5000360 --> Sum starting at 0240D5 with length 000360 is 00004CC9

W0201E101? --> Write 01 to 0201E1
WFFFED00061? --> Write 0061 to FFFED0
RFFFED0 -->
   ASCII: .....@.....c................`...
   HEX: 00 02 00 00 00 40 00 00 00 82 00 63 00 00 00 00 00 00 00 00 00 00 00 00 03 00 00 01 60 00 00 00
R0240B9 -->
   ASCII: ............................Rc29
   HEX: A4 A4 A4 A4 A4 A4 A4 A4 A4 A4 A4 A4 A4 A4 A4 86 86 86 86 A4 A4 A4 A4 A4 A4 A4 A4 00 52 63 32 39
R0240D5 -->
   ASCII: Rc298v2.........................
   HEX: 52 63 32 39 38 76 32 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
DEBUG: command='N0240F5' (len=7), machineResponseBytes.Count=258
DEBUG: First 10 response bytes as hex: 35 31 30 46 61 6E 4C 47 00 00
N0240F5 --> [256 bytes]
   ASCII: 10FanLG.........................Nv735...........................V2EmbScissLG....................dm27 ...........................me014...........................995183..........................Sp362...........................Sp533...........................
   HEX: 31 30 46 61 6E 4C 47 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 4E 76 37 33 35 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 56 32 45 6D 62 53 63 69 73 73 4C 47 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 64 6D 32 37 20 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 6D 65 30 31 34 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 39 39 35 31 38 33 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 53 70 33 36 32 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 53 70 35 33 33 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
DEBUG: command='N0241F5' (len=7), machineResponseBytes.Count=258
DEBUG: First 10 response bytes as hex: 35 57 6C 33 31 38 00 00 00 00
N0241F5 --> [256 bytes]
   ASCII: Wl318...........................Wm244...........................lb109...........................Fl572...........................Wl998...........................EPB35...........................Cr070...........................Cr060...........................
   HEX: 57 6C 33 31 38 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 57 6D 32 34 34 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 6C 62 31 30 39 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 46 6C 35 37 32 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 57 6C 39 39 38 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 45 50 42 33 35 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 43 72 30 37 30 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 43 72 30 36 30 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
R0242F5 -->
   ASCII: Nv850...........................
   HEX: 4E 76 38 35 30 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
R024315 -->
   ASCII: Nv810...........................
   HEX: 4E 76 38 31 30 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
L0240D5000260 --> Sum starting at 0240D5 with length 000260 is 00001DD6
R024040 -->
   ASCII: .......d........................
   HEX: 00 02 06 A0 00 02 07 64 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
R0206A0 -->
   ASCII: ..*.................. ..........
   HEX: 1D 1B 2A 04 00 00 0C 07 02 00 00 04 05 00 00 00 FF FF FF 00 FF 20 00 00 00 00 00 00 00 00 00 00
WFFFED00101? --> Write 0101 to FFFED0
RFFFED0 -->
   ASCII: .....@.....c....................
   HEX: 00 00 00 00 00 40 00 00 00 82 00 63 00 00 00 00 00 00 00 00 00 00 00 00 FF 00 00 01 00 00 00 00

# Prompts

## Create the Main Upload Block

In SerialStack.cs, I want to create a new method called "CreateMainDataBlock" that takes a EmbroideryFile and returns a byte[].

 - The 2 first bytes is (EmbroideryFile.FileData.length / 5) in network byte order.
 - Write 166 null characters
 - The next 4 bytes are the length EmbroideryFile.FileData.length in network byte order.
 - The next 4 bytes are the length EmbroideryFile.FileExtraData.length in network byte order, put 0x00000000 is FileExtraData is empty of null.
 - Write the entire EmbroideryFile.FileData
 - Write the entire EmbroideryFile.FileExtraData if present.

Note that EmbroideryFile.FileData must not be null and not be empty. The resulting byte[] size must be 176 bytes + EmbroideryFile.FileData.length + EmbroideryFile.FileExtraData.length. Double check this before exit.

## Create the Image Preview Upload Block

In SerialStack.cs, I want to create a new method called "CreatePreviewDataBlock" that takes a EmbroideryFile and returns a byte[].

 - Write 0x0000093EFF as the first 5 bytes in network byte order
 - Write 0x00 169 times.
 - Write the preview image EmbroideryFile.PreviewImageData

Note that EmbroideryFile.PreviewImageData must not be null and not be empty. The resulting byte[] size must be 174 bytes + EmbroideryFile.PreviewImageData.length. Double check this before exit.

## Create the Upload Method

In SerialStack.cs, I want to create a new method that will write a file to the embroidery module using the serial port. The method header looks like this:

        public async Task<bool> WriteEmbroideryFileAsync(EmbroideryFile file, StorageLocation location, Action<int, int>? progress = null)

This method will start looking a lot like ReadEmbroideryFileAsync() we do the following:

 - Use CreateMainDataBlock() to create the "main data block"
 - Use CreatePreviewDataBlock() to create the "preview data block"
 - Check that we are connected
 - Check that we are not busy
 - Set busy state
 - Ensure we are in embroidery mode, if not, change to embroidery mode.
 - If the location is PC Card, check if a PC card is present.
 - Do a protocol reset
 - Select the storage source (Embroidery Module or PC Card)
 - Write the "main data block" to 0x028E98
 - Write the "preview data block" to 0x024480
 - Write 0x01 to 0x02409D, write a comment that this is a block size value and may be changed in the future.
 - Write 0xA4 to 0x0240B9.
 - Write the filename to 0x0240D5. We must pad the data with 0x00 so that it's 32 bytes long. The file name can't be more than 31 bytes long in UTF-8.
 - Invoke method 0x0201. Put a 4 second wait on the method invocation for the machine to have time to store the data.

If everything is a success, return true.
