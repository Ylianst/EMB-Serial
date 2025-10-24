# High Level Protocol

Now that we have the basics of the serial protocol understood, we can build code that reads an area of memory, writes at a location and we have this load (?) operation. You can build a low-level stack in code that will do all that and we now need to figure out the high level workings of the machine.

## Machine Name, Language & Version

```
N200100 --> "NMMV03.01·English     ·Bernina Electronic  AG  ·July 98"
```

Reading memory at 0x200100 will give you the machine firmware version. You also see the firmware language and manufacturer after this. It's a set of fixed length null terminated strings.

## Invoke Machine Function Call

You can invoke code to run on the machine by placing argument 1 in 0x0201E1 and argument 2 in 0x0201DC and then writing the function call number into 0xFFFED0. For example:

W0201DC01? --> Write 01 to 0201DC         // Set argument 2
W0201E100? --> Write 00 to 0201E1         // Set argument 1
WFFFED00031? --> Write 0031 to FFFED0     // Invoke function 0x0031

When a function is called, the software always reads 0xFFFED0, this is likely to read the 2 first bytes that may indicate is the invocation completed and if so, what is the return value.

RFFFED0 -->
   ASCII: .....@.....3................@...
   HEX: 00 02 00 00 00 40 00 00 00 83 00 33 00 00 00 00 00 00 00 00 00 00 00 00 04 00 00 01 40 00 00 00

Note that most of this data past the two first bytes is likely of no use and only requested because the read command always pulls 32 bytes.

A guess is that this read is made to make sure the first two bytes are not equal to the number of the calling function. So, you write 0x0031 to FFFED0 and read it back to make sure it's changed confirming the call completed. Most of the time the read value will be 0x0002, however method call 0x0101 will return a value of 0x0000.

This method invocation system allows the software to tell the machine to do all sorts of things. Figuring out what function call do what is the real magic.

## Emboidery Module Session Start

When starting up you communicate with the sewing machine, but if you want to access the embroidery module, you need to open the communication path to the embroidery module first. If you read 0x57FF80 the first bytes will be 0xB4A5 if the emboidery session is not started. In this case, you can't read/write data to the embroidery module.

R57FF80 --> B4A5000020DF002B797D03700FCE0C08535332FF0370FFFFFFFFFFFFFFFFFFFF  (Session Closed)

To start the embroidery module communication, you need to send command "TrMEYQ" and get a "O" in return. If you don't get a "O", the embroidery module is not attached. Once the session started, you can read 0x57FF80 again to check the session state.

R57FF80 --> 00CE800400CF80010000800403378004033704370704000A00F6F9FC07040010  (Session Open)

If the embroidery module session is started, you now read 0x00CE. Once the session is started, you can't start it again, so sending "TrMEYQ" again will not work (it will not return "O") also, once the session is open, you can't change the baudrate, that too will not return "O".

Do not send the "TrMEYQ" again if the session is already opened. If you do that, you will not get a "O" confirmation and the communication will be in an invalid state. You will not be able to close the session anymore and will need to turn off/on the machine to reset it into a good state.

When first starting up, the embroidery software will issue a R57FF80 to see if a session is already started or not. If it's not started, it will start it. Opening the embroidery module session is required to download/upload/view/delete embroidery files.

## Emboidery Module Session End

To close the session, send the "TrME" command. You can see how R57FF80 looks before and after the command: 

R57FF80 --> 00CE800400CF80010000800403378004033704370704000A00F6F9FC07040010  (Session Open)
Send "TrME" to close the session.
R57FF80 --> B4A5000020DF002B797D03700FCE0C08535332FF0370FFFFFFFFFFFFFFFFFFFF  (Session Closed)

You notice that once "TrME" is send, the session closes and the 0x57FF80 memory location reverts to 0xB4A5. The embroidery software has a tendency to close the embroidery module connection each time it's done with some operation probably in order to keep the default state being to communicate with the sewing machine. So, if you disconnect the serial cable at normal times, the serial port will be ready for sewing software.

## Startup Sequence

This sequence is performed when you first enter the software and reads the BIOS version and the name of all of the embroidery files. The user will then be presented with a list of file that they can preview or download. The preview and download flows will be covered later.

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
