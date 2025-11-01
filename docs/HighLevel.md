# High Level Protocol

Now that we have the basics of the serial protocol understood, we can build code that reads an area of memory, writes at a location. You can build a low-level stack in code that will do all that and we now need to figure out the high level workings of the machine.

The machine uses the EXP format internally:
https://www.appropedia.org/EXP_Embroidery_File_Format
https://edutechwiki.unige.ch/en/Embroidery_format_EXP

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

72 x 64 pixels black & white image. 558 (0x22E) bytes long.

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


