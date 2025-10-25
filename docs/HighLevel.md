# High Level Protocol

Now that we have the basics of the serial protocol understood, we can build code that reads an area of memory, writes at a location and we have this load (?) operation. You can build a low-level stack in code that will do all that and we now need to figure out the high level workings of the machine.

## Startup Sequence

This sequence is performed when you first enter the software and reads the BIOS version and the name of all of the embroidery files. It will read both read-only and read/write files. The user will then be presented with a list of file that they can preview or download. The preview and download flows will be covered later.

- Delect the baud rate the connect to the machine, uses RF? for detection, `TrMEJ05` to change baud rate.
- Check if in embroidery mode and change to it , uses `R57FF80` and `TrMEYQ`.
- Read the embroidery module firmware version, uses `N200100`.

- Peforms these 4 reads (Unknown)
```
R024004 --> 00000000004B0000000000000000000000000000004B00000000000000000000
RFFFED9 --> 8300330000000000000000000000000200000100000000000000000000000000
```

Read the pointer to the block allocation array. We see 0x0206A0 is the location of the block vector.
```
R024040 --> 000206A000020764000000000000000000000000000000000000000000000000
```

Read allocation of blocks (?), we see 0305 and the 03 is the number of user (Mx) files.
```
R0206A0 --> 1D1B2A0380002C070200000305000000FFFFFF00FC6000000000000000000000
```
- Invoke function 0x00A1 with no arguments.         (Unknown)
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

- Next, read the file types. This has one byte for each file with the bits indicating what type of file it is. Read until you hit the 0x00 byte.
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

WFFFED000A1? --> Write 00A1 at FFFED0   (Invoke Function 0x00a1)
RFFFED0 --> 00020000004000000083006300000000000000000000000003000001A0000000

W0201DC01? --> Write 01 to 0201DC
W0201E100? --> Write 00 to 0201E1
WFFFED00031? --> Write 0031 to FFFED0   (Invoke Function 0x0031)
RFFFED0 --> 0002000000400000008300630000000000000000000000000300000130000000

WFFFED00021? --> Write 0021 to FFFED0   (Invoke Function 0x0021)
RFFFED0 --> 0002000000400000008300630000000000000000000000000300000120000000

(Don't know what this is)
R024080 --> 2D00000000000000000000000000000000000000000000000000000000908F93
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
