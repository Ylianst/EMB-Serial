const SerialStack = require('./SerialStack');

/**
 * ListFiles - Connects to embroidery machine and lists all files in memory
 * 
 * This application demonstrates reading the file directory from the embroidery
 * machine's internal memory, including file names, types, and attributes.
 */

class ListFiles {
  constructor(portPath = '/dev/ttyUSB0') {
    this.stack = new SerialStack(portPath);
  }

  /**
   * Connect to the embroidery machine
   */
  async connect() {
    console.log('=== Connecting to Embroidery Machine ===\n');
    
    // Open serial port with automatic baud rate detection
    await this.stack.open();
    
    // Upgrade to 57600 baud for faster communication
    await this.stack.upgradeSpeed();
    
    console.log('✓ Connected successfully\n');
  }

  /**
   * Ensure we are in embroidery mode
   */
  async ensureEmbroideryMode() {
    console.log('=== Checking Embroidery Mode ===\n');
    
    const isOpen = await this.stack.IsEmbroiderySessionOpen();
    
    if (!isOpen) {
      console.log('Opening embroidery session...');
      await this.stack.StartEmbroiderySession();
    } else {
      console.log('Already in embroidery mode');
    }
    
    console.log('');
  }

  /**
   * Read the firmware version
   */
  async readFirmwareVersion() {
    console.log('=== Reading Firmware Version ===\n');
    
    // Read 256 bytes from 0x200100 which contains firmware info
    const data = await this.stack.largeRead('200100');
    
    // Parse the firmware version (null-terminated strings)
    const parseNullTerminatedString = (str, start, maxLen) => {
      let end = start;
      while (end < start + maxLen && str.charCodeAt(end) !== 0) {
        end++;
      }
      return str.substring(start, end);
    };
    
    const version = parseNullTerminatedString(data, 0, 32);
    const language = parseNullTerminatedString(data, 32, 32);
    const manufacturer = parseNullTerminatedString(data, 64, 32);
    const date = parseNullTerminatedString(data, 96, 32);
    
    console.log(`Version: ${version}`);
    console.log(`Language: ${language}`);
    console.log(`Manufacturer: ${manufacturer}`);
    console.log(`Date: ${date}\n`);
    
    return { version, language, manufacturer, date };
  }

  /**
   * Check if a PC Card is present
   */
  async checkPCCard() {
    console.log('=== Checking PC Card ===\n');
    
    // Read from 0xFFFED9 - first byte indicates PC card status
    const data = await this.stack.read('FFFED9');
    const firstByte = parseInt(data.substring(0, 2), 16);
    
    // 0x82 = No PC Card, 0x83 = PC Card Present
    const hasCard = (firstByte & 0x01) !== 0;
    
    console.log(`PC Card: ${hasCard ? 'Present' : 'Not Present'} (0x${firstByte.toString(16).toUpperCase()})\n`);
    
    return hasCard;
  }

  /**
   * Select storage location (Internal Memory or PC Card)
   * @param {boolean} usePCCard - true for PC Card, false for internal memory
   */
  async selectStorageLocation(usePCCard = false) {
    console.log(`=== Selecting Storage Location ===\n`);
    
    const functionCode = usePCCard ? '0051' : '00A1';
    const locationName = usePCCard ? 'PC Card' : 'Internal Memory';
    
    console.log(`Selecting ${locationName}...`);
    
    // Invoke function to select storage location
    await this.stack.write('FFFED0', functionCode);
    
    console.log(`✓ ${locationName} selected\n`);
  }

  /**
   * Invoke machine function calls
   */
  async invokeFunction(functionCode, arg1 = null, arg2 = null) {
    // Set arguments if provided
    if (arg2 !== null) {
      const arg2Hex = arg2.toString(16).padStart(2, '0').toUpperCase();
      await this.stack.write('0201DC', arg2Hex);
    }
    
    if (arg1 !== null) {
      const arg1Hex = arg1.toString(16).padStart(2, '0').toUpperCase();
      await this.stack.write('0201E1', arg1Hex);
    }
    
    // Invoke the function
    await this.stack.write('FFFED0', functionCode);
  }

  /**
   * Read the number of files in storage
   */
  async readFileCount() {
    const data = await this.stack.read('024080');
    const count = parseInt(data.substring(0, 2), 16);
    return count;
  }

  /**
   * Read file types for all files
   * @param {number} count - Number of files to read
   */
  async readFileTypes(count) {
    // File types start at 0x0240B9
    const typesPerRead = 32; // Each read gets 32 bytes
    const fileTypes = [];
    
    let address = 0x0240B9;
    let remaining = count;
    
    while (remaining > 0) {
      const data = await this.stack.read(address.toString(16).padStart(6, '0').toUpperCase());
      
      // Parse each byte as a file type
      for (let i = 0; i < Math.min(remaining, typesPerRead) * 2; i += 2) {
        const typeByte = parseInt(data.substring(i, i + 2), 16);
        if (typeByte !== 0) {
          fileTypes.push(typeByte);
        }
      }
      
      remaining -= typesPerRead;
      address += typesPerRead;
    }
    
    return fileTypes;
  }

  /**
   * Decode file type byte into attributes
   */
  decodeFileType(typeByte) {
    return {
      isReadOnly: (typeByte & 0x80) !== 0,
      isAlphabet: (typeByte & 0x08) !== 0,
      blockSize: (typeByte & 0x08) ? 2 : 1,
      isMemoryFile: (typeByte & 0x06) === 0x06,
      typeByte: typeByte
    };
  }

  /**
   * Read file names from a specific page
   * @param {number} page - Page number (0, 1, 2, ...)
   * @param {number} maxFiles - Maximum number of files to read
   */
  async readFileNames(page, maxFiles) {
    // File names start at 0x0240D5, each is 32 bytes
    const NAMES_PER_PAGE = 27; // Maximum names per page
    const filesToRead = Math.min(maxFiles, NAMES_PER_PAGE);
    
    const names = [];
    let address = 0x0240D5;
    
    // Read first file name with regular read (32 bytes)
    if (filesToRead > 0) {
      const data = await this.stack.read(address.toString(16).padStart(6, '0').toUpperCase());
      const name = this.parseFileName(data);
      names.push(name);
    }
    
    // Read remaining names in blocks of 8 (256 bytes)
    let remaining = filesToRead - 1;
    address += 32;
    
    while (remaining > 0) {
      const data = await this.stack.largeRead(address.toString(16).padStart(6, '0').toUpperCase());
      
      // Convert binary data to hex string
      const hexData = Buffer.from(data, 'latin1').toString('hex').toUpperCase();
      
      // Parse up to 8 file names from this block (each name is 32 bytes = 64 hex chars)
      for (let i = 0; i < Math.min(remaining, 8); i++) {
        const nameHex = hexData.substring(i * 64, (i + 1) * 64);
        const name = this.parseFileName(nameHex);
        names.push(name);
      }
      
      remaining -= 8;
      address += 256;
    }
    
    return names;
  }

  /**
   * Parse a file name from HEX data
   */
  parseFileName(hexData) {
    // Convert hex to ASCII and find null terminator
    let name = '';
    for (let i = 0; i < hexData.length && i < 64; i += 2) {
      const charCode = parseInt(hexData.substring(i, i + 2), 16);
      if (charCode === 0) break;
      name += String.fromCharCode(charCode);
    }
    return name;
  }

  /**
   * Load a specific page of file names
   * @param {number} pageNum - Page number (0 = first page, 1 = second page, etc.)
   */
  async loadPage(pageNum) {
    const functionCodes = ['0031', '0061', '00C1']; // Functions for pages 0, 1, 2
    
    if (pageNum >= functionCodes.length) {
      throw new Error(`Invalid page number: ${pageNum}`);
    }
    
    console.log(`Loading page ${pageNum + 1}...`);
    await this.invokeFunction(functionCodes[pageNum], pageNum === 0 ? 0 : pageNum, pageNum === 0 ? 1 : null);
    
    // Also invoke function 0x0021
    await this.invokeFunction('0021');
  }

  /**
   * List all files in internal memory
   */
  async listFiles() {
    console.log('=== Listing Files ===\n');
    
    // Select internal memory
    await this.selectStorageLocation(false);
    
    // Load first page
    await this.loadPage(0);
    
    // Read number of files
    const fileCount = await this.readFileCount();
    console.log(`Total files: ${fileCount}\n`);
    
    if (fileCount === 0) {
      console.log('No files found in memory.\n');
      return [];
    }
    
    // Read file types for all files
    const fileTypes = await this.readFileTypes(fileCount);
    
    const allFiles = [];
    let filesProcessed = 0;
    
    // Process files in pages (27 files per page)
    const NAMES_PER_PAGE = 27;
    let pageNum = 0;
    
    while (filesProcessed < fileCount) {
      const filesInThisPage = Math.min(fileCount - filesProcessed, NAMES_PER_PAGE);
      
      // Read file names for this page
      const names = await this.readFileNames(pageNum, filesInThisPage);
      
      // Combine names with types
      for (let i = 0; i < names.length; i++) {
        const fileIndex = filesProcessed + i;
        if (fileIndex < fileTypes.length) {
          const attributes = this.decodeFileType(fileTypes[fileIndex]);
          allFiles.push({
            index: fileIndex + 1,
            name: names[i],
            ...attributes
          });
        }
      }
      
      filesProcessed += filesInThisPage;
      pageNum++;
      
      // Load next page if needed
      if (filesProcessed < fileCount && pageNum < 3) {
        await this.loadPage(pageNum);
      }
    }
    
    // Display results
    console.log('=== File List ===\n');
    console.log('Index | Name                             | Type       | Blocks | Attributes');
    console.log('------|----------------------------------|------------|--------|------------------');
    
    for (const file of allFiles) {
      const type = file.isAlphabet ? 'Alphabet' : 'Design';
      const readonly = file.isReadOnly ? 'RO' : 'RW';
      const mem = file.isMemoryFile ? 'Mem' : '';
      const attrs = [readonly, mem].filter(a => a).join(', ');
      
      console.log(
        `${file.index.toString().padStart(5)} | ` +
        `${file.name.padEnd(32)} | ` +
        `${type.padEnd(10)} | ` +
        `${file.blockSize.toString().padStart(6)} | ` +
        `${attrs}`
      );
    }
    
    console.log('');
    
    // Invoke final cleanup function
    await this.invokeFunction('0101');
    
    return allFiles;
  }

  /**
   * Disconnect from the embroidery machine
   */
  async disconnect() {
    console.log('=== Disconnecting ===\n');
    
    // Close embroidery session if open
    await this.stack.EndEmbroiderySession();
    
    // Close serial port
    await this.stack.close();
    
    console.log('✓ Disconnected\n');
  }

  /**
   * Main execution method
   */
  async run() {
    try {
      await this.connect();
      await this.ensureEmbroideryMode();
      await this.readFirmwareVersion();
      await this.checkPCCard();
      
      const files = await this.listFiles();
      
      console.log(`\n✓ Successfully listed ${files.length} file(s)\n`);
      
      await this.disconnect();
      
    } catch (error) {
      console.error('Error:', error.message);
      console.error(error.stack);
      
      // Try to clean up
      try {
        await this.stack.close();
      } catch (closeError) {
        // Ignore cleanup errors
      }
      
      process.exit(1);
    }
  }
}

// Run the application if executed directly
if (require.main === module) {
  const portPath = process.argv[2] || '/dev/ttyUSB0';
  console.log(`Using serial port: ${portPath}\n`);
  
  const app = new ListFiles(portPath);
  app.run();
}

module.exports = ListFiles;
