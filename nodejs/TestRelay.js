const net = require('net');
const TcpProtocol = require('./TcpProtocol');

/**
 * Test application for Relay.js TCP server
 * Connects to 127.0.0.1:8888 and tests READ, LRED, and CSUM commands
 */
class TestRelay {
  constructor(host = '127.0.0.1', port = 8888) {
    this.host = host;
    this.port = port;
    this.socket = null;
    this.protocol = new TcpProtocol();
    this.requestIdCounter = 1;
    this.receiveBuffer = Buffer.alloc(0);
    this.pendingRequests = new Map(); // Map of requestId -> {resolve, reject, timeout}
    this.connected = false;
  }

  /**
   * Generate a unique request ID
   */
  getNextRequestId() {
    const id = this.requestIdCounter.toString(16).toUpperCase().padStart(8, '0');
    this.requestIdCounter = (this.requestIdCounter + 1) & 0xFFFFFFFF;
    return id;
  }

  /**
   * Connect to the TCP server
   */
  async connect() {
    return new Promise((resolve, reject) => {
      console.log(`Connecting to ${this.host}:${this.port}...`);
      
      this.socket = net.createConnection({
        host: this.host,
        port: this.port
      });

      this.socket.on('connect', () => {
        console.log('Connected to relay server');
        this.connected = true;
        
        // Set up data handler
        this.socket.on('data', (data) => this.handleData(data));
        
        resolve();
      });

      this.socket.on('error', (err) => {
        console.error('Socket error:', err.message);
        if (!this.connected) {
          reject(err);
        }
      });

      this.socket.on('close', () => {
        console.log('Connection closed');
        this.connected = false;
        
        // Reject all pending requests
        for (const [requestId, pending] of this.pendingRequests.entries()) {
          clearTimeout(pending.timeout);
          pending.reject(new Error('Connection closed'));
        }
        this.pendingRequests.clear();
      });
    });
  }

  /**
   * Handle incoming data from the server
   */
  handleData(data) {
    // Append new data to receive buffer
    this.receiveBuffer = Buffer.concat([this.receiveBuffer, data]);

    // Try to decode messages from the buffer
    while (this.receiveBuffer.length > 0) {
      const message = this.protocol.decodeMessage(this.receiveBuffer);
      
      if (!message) {
        // Incomplete message, wait for more data
        break;
      }

      // Complete message received
      // Remove the decoded message from the buffer
      this.receiveBuffer = this.receiveBuffer.slice(message.totalLength);

      // Handle the response
      this.handleResponse(message);
    }
  }

  /**
   * Handle a decoded response message
   */
  handleResponse(message) {
    const { messageType, requestId, payload } = message;
    
    // Find the pending request
    const pending = this.pendingRequests.get(requestId);
    if (!pending) {
      console.warn(`Received response for unknown request ID: ${requestId}`);
      return;
    }

    // Clear timeout
    clearTimeout(pending.timeout);
    this.pendingRequests.delete(requestId);

    // Handle error responses
    if (messageType === this.protocol.MessageTypes.ERRO) {
      const error = this.protocol.parseJsonPayload(payload);
      pending.reject(new Error(`Server error: ${error.error} (code ${error.code})`));
      return;
    }

    // Resolve with the message
    pending.resolve(message);
  }

  /**
   * Send a command and wait for response
   */
  async sendCommand(messageType, payload = '') {
    if (!this.connected) {
      throw new Error('Not connected to server');
    }

    const requestId = this.getNextRequestId();
    const message = this.protocol.encodeMessage(messageType, requestId, payload);

    return new Promise((resolve, reject) => {
      // Set up timeout (30 seconds)
      const timeout = setTimeout(() => {
        this.pendingRequests.delete(requestId);
        reject(new Error(`Request ${requestId} timed out`));
      }, 30000);

      // Store pending request
      this.pendingRequests.set(requestId, { resolve, reject, timeout });

      // Send the message
      this.socket.write(message);
    });
  }

  /**
   * Get server configuration
   */
  async getConfig() {
    const response = await this.sendCommand(this.protocol.MessageTypes.GCFG);
    return this.protocol.parseJsonPayload(response.payload);
  }

  /**
   * Get server status
   */
  async getStatus() {
    const response = await this.sendCommand(this.protocol.MessageTypes.STAT);
    return this.protocol.parseJsonPayload(response.payload);
  }

  /**
   * Change baud rate (auto-detect and upgrade)
   */
  async changeBaudRate(baudRate = '57600') {
    const payload = baudRate.padEnd(5, '0');
    const response = await this.sendCommand(this.protocol.MessageTypes.BAUD, payload);
    const status = response.payload.toString('ascii');
    return status === 'O';
  }

  /**
   * Open embroidery session
   */
  async openSession() {
    const response = await this.sendCommand(this.protocol.MessageTypes.SOPE);
    const status = response.payload.toString('ascii');
    return status === 'O';
  }

  /**
   * Close embroidery session
   */
  async closeSession() {
    const response = await this.sendCommand(this.protocol.MessageTypes.SCLO);
    const status = response.payload.toString('ascii');
    return status === 'O';
  }

  /**
   * Read 32 bytes from memory
   */
  async read(address) {
    // Ensure address is 6 hex characters
    const addr = address.toUpperCase().padStart(6, '0');
    const response = await this.sendCommand(this.protocol.MessageTypes.READ, addr);
    return response.payload.toString('ascii');
  }

  /**
   * Large read 256 bytes from memory
   */
  async largeRead(address) {
    // Ensure address is 6 hex characters
    const addr = address.toUpperCase().padStart(6, '0');
    const response = await this.sendCommand(this.protocol.MessageTypes.LRED, addr);
    return response.payload; // Return as Buffer
  }

  /**
   * Calculate checksum of memory region
   */
  async checksum(address, length) {
    // Ensure address and length are 6 hex characters each
    const addr = address.toUpperCase().padStart(6, '0');
    const len = length.toUpperCase().padStart(6, '0');
    const payload = addr + len;
    const response = await this.sendCommand(this.protocol.MessageTypes.CSUM, payload);
    const checksumHex = response.payload.toString('ascii');
    return parseInt(checksumHex, 16);
  }

  /**
   * Convert hex string to ASCII string
   */
  hexToAscii(hexString) {
    let result = '';
    for (let i = 0; i < hexString.length; i += 2) {
      const byte = parseInt(hexString.substring(i, i + 2), 16);
      result += String.fromCharCode(byte);
    }
    return result;
  }

  /**
   * Initialize and test connection
   */
  async initialize() {
    try {
      console.log('=== Initializing TCP Relay Connection ===');
      await this.connect();
      
      // Get configuration
      const config = await this.getConfig();
      console.log('Server configuration:', config);
      
      // Get initial status
      const initialStatus = await this.getStatus();
      console.log('Initial status:', initialStatus);
      
      // Get status
      const status = await this.getStatus();
      console.log('Status after baud change:', status);
      
      return true;
    } catch (error) {
      console.error('Initialization failed:', error.message);
      throw error;
    }
  }

  /**
   * Test the Read command
   */
  async testRead() {
    console.log('\n=== Testing READ Command ===');
    
    try {
      // Read machine firmware version from 0x200100
      console.log('\nReading machine firmware version (0x200100)...');
      const firmwareData = await this.read('200100');
      const firmwareText = this.hexToAscii(firmwareData);
      console.log('Firmware data (HEX):', firmwareData);
      console.log('Firmware data (ASCII):', firmwareText);
      
      // Read another address for testing
      console.log('\nReading from address 0xFFFED9 (PC Card detection)...');
      const cardData = await this.read('FFFED9');
      console.log('Card detection data (HEX):', cardData);
      
      // Check PC card status (first byte: 0x82 = No Card, 0x83 = Card Present)
      const firstByte = cardData.substring(0, 2);
      if (firstByte === '82') {
        console.log('PC Card Status: Not Present');
      } else if (firstByte === '83') {
        console.log('PC Card Status: Present');
      } else {
        console.log(`PC Card Status: Unknown (0x${firstByte})`);
      }
      
      return true;
    } catch (error) {
      console.error('Read test failed:', error.message);
      return false;
    }
  }

  /**
   * Test the Large Read command
   */
  async testLargeRead() {
    console.log('\n=== Testing LRED (Large Read) Command ===');
    
    try {
      // Large read from 0x0240F5
      console.log('\nPerforming large read from address 0x0240F5...');
      const data1 = await this.largeRead('0240F5');
      const data1Hex = data1.toString('hex').toUpperCase();
      console.log('Large read data (first 100 hex chars):', data1Hex.substring(0, 100));
      console.log('Large read data length:', data1.length, 'bytes');
      
      // Large read from another address
      console.log('\nPerforming large read from address 0x0241F5...');
      const data2 = await this.largeRead('0241F5');
      const data2Hex = data2.toString('hex').toUpperCase();
      console.log('Large read data (first 100 hex chars):', data2Hex.substring(0, 100));
      console.log('Large read data length:', data2.length, 'bytes');
      
      return true;
    } catch (error) {
      console.error('Large read test failed:', error.message);
      return false;
    }
  }

  /**
   * Test Embroidery Session Management
   */
  async testEmbroiderySession() {
    console.log('\n=== Testing Embroidery Session Management ===');
    
    try {
      // Read 200100 before starting session
      console.log('\nReading machine firmware version (0x200100) before session...');
      const firmwareData1 = await this.read('200100');
      const firmwareText1 = this.hexToAscii(firmwareData1);
      console.log('Firmware data (HEX):', firmwareData1.substring(0, 32) + '...');
      console.log('Firmware data (ASCII):', firmwareText1);
      
      // Check current session status
      console.log('\nChecking embroidery session status...');
      const initialStatus = await this.getStatus();
      console.log(`Initial session status: ${initialStatus.sessionOpen ? 'OPEN' : 'CLOSED'}`);
      
      // Start embroidery session
      console.log('\nStarting embroidery session...');
      const started = await this.openSession();
      console.log(`Session start result: ${started ? 'Success' : 'Failed'}`);
      
      // Verify session is now open
      const sessionStatus = await this.getStatus();
      console.log(`Session status after start: ${sessionStatus.sessionOpen ? 'OPEN' : 'CLOSED'}`);
      
      // Read 200100 again during session
      console.log('\nReading machine firmware version (0x200100) during session...');
      const firmwareData2 = await this.read('200100');
      const firmwareText2 = this.hexToAscii(firmwareData2);
      console.log('Firmware data (HEX):', firmwareData2.substring(0, 32) + '...');
      console.log('Firmware data (ASCII):', firmwareText2);
      
      // Compare the two reads
      if (firmwareData1 === firmwareData2) {
        console.log('✓ Firmware data is identical in both sessions');
      } else {
        console.log('⚠ Firmware data differs between sessions');
      }
      
      // End embroidery session
      console.log('\nEnding embroidery session...');
      const ended = await this.closeSession();
      console.log(`Session end result: ${ended ? 'Success' : 'Failed'}`);
      
      // Verify session is now closed
      const finalStatus = await this.getStatus();
      console.log(`Final session status: ${finalStatus.sessionOpen ? 'OPEN' : 'CLOSED'}`);
      
      return true;
    } catch (error) {
      console.error('Embroidery session test failed:', error.message);
      return false;
    }
  }

  /**
   * Test the Checksum command and verify checksums
   */
  async testChecksumCommand() {
    console.log('\n=== Testing CSUM (Checksum) Command with Verification ===');
    
    try {
      // Test 1: Read 32 bytes and verify with Checksum
      console.log('\n--- Test 1: Verify READ (32 bytes) with CSUM ---');
      const address1 = '200100';
      const readData = await this.read(address1);
      
      // Calculate local checksum from the HEX data
      let localSum = 0;
      for (let i = 0; i < readData.length; i += 2) {
        const byte = parseInt(readData.substring(i, i + 2), 16);
        localSum += byte;
      }
      localSum = localSum & 0xFFFFFFFF; // Keep it as 32-bit value
      
      // Get sum from machine (32 bytes = 0x20)
      const machineSum = await this.checksum(address1, '000020');
      
      console.log(`Local checksum:   0x${localSum.toString(16).toUpperCase().padStart(8, '0')} (${localSum})`);
      console.log(`Machine checksum: 0x${machineSum.toString(16).toUpperCase().padStart(8, '0')} (${machineSum})`);
      
      if (localSum === machineSum) {
        console.log('✓ READ checksum VERIFIED - checksums match!');
      } else {
        console.log('✗ READ checksum MISMATCH - checksums do not match!');
        return false;
      }
      
      // Test 2: Large Read 256 bytes and verify with Checksum
      console.log('\n--- Test 2: Verify LRED (256 bytes) with CSUM ---');
      const address2 = '200100';
      const largeReadData = await this.largeRead(address2);
      
      // Calculate local checksum from the binary data
      let localLargeSum = 0;
      for (let i = 0; i < largeReadData.length; i++) {
        const byte = largeReadData[i];
        localLargeSum += byte;
      }
      localLargeSum = localLargeSum & 0xFFFFFFFF; // Keep it as 32-bit value
      
      // Get sum from machine (256 bytes = 0x100)
      const machineLargeSum = await this.checksum(address2, '000100');
      
      console.log(`Local checksum:   0x${localLargeSum.toString(16).toUpperCase().padStart(8, '0')} (${localLargeSum})`);
      console.log(`Machine checksum: 0x${machineLargeSum.toString(16).toUpperCase().padStart(8, '0')} (${machineLargeSum})`);
      
      // Debug: Let's also verify the first few bytes
      console.log('First 10 bytes from Large Read:');
      for (let i = 0; i < 10; i++) {
        const byte = largeReadData[i];
        console.log(`  Byte ${i}: 0x${byte.toString(16).toUpperCase().padStart(2, '0')} (${byte})`);
      }
      
      if (localLargeSum === machineLargeSum) {
        console.log('✓ LRED checksum VERIFIED - checksums match!');
      } else {
        console.log('✗ LRED checksum MISMATCH - checksums do not match!');
        console.log('Note: This may indicate data transmission issues');
      }
      
      // Test 3: Verify a smaller sum (single byte)
      console.log('\n--- Test 3: Verify single byte checksum ---');
      const address3 = '200100';
      const singleByteData = await this.read(address3);
      const firstByte = parseInt(singleByteData.substring(0, 2), 16);
      const singleByteSum = await this.checksum(address3, '000001');
      
      console.log(`First byte value: 0x${firstByte.toString(16).toUpperCase().padStart(2, '0')} (${firstByte})`);
      console.log(`Machine sum:      0x${singleByteSum.toString(16).toUpperCase().padStart(8, '0')} (${singleByteSum})`);
      
      if (firstByte === singleByteSum) {
        console.log('✓ Single byte checksum VERIFIED - values match!');
      } else {
        console.log('✗ Single byte checksum MISMATCH - values do not match!');
        return false;
      }
      
      return true;
    } catch (error) {
      console.error('Checksum command test failed:', error.message);
      return false;
    }
  }

  /**
   * Cleanup and close connection
   */
  async cleanup() {
    console.log('\n=== Cleaning Up ===');
    try {
      if (this.socket && this.connected) {
        this.socket.end();
        console.log('Connection closed successfully');
      }
    } catch (error) {
      console.error('Cleanup failed:', error.message);
    }
  }

  /**
   * Run all tests
   */
  async runAllTests() {
    console.log('╔════════════════════════════════════════════╗');
    console.log('║   TCP Relay Test Application               ║');
    console.log('╚════════════════════════════════════════════╝');
    
    try {
      // Initialize
      await this.initialize();
      
      // Wait a bit between tests
      await this.delay(50);
      
      // Run tests
      await this.testRead();
      await this.delay(50);
      
      await this.testEmbroiderySession();
      await this.delay(50);
      
      await this.testLargeRead();
      await this.delay(50);
      
      await this.testChecksumCommand();
      await this.delay(50);
      
      console.log('\n╔════════════════════════════════════════════╗');
      console.log('║   All Tests Completed                      ║');
      console.log('╚════════════════════════════════════════════╝');
      
    } catch (error) {
      console.error('\n╔════════════════════════════════════════════╗');
      console.error('║   Test Suite Failed                        ║');
      console.error('╚════════════════════════════════════════════╝');
      console.error('Error:', error.message);
    } finally {
      await this.cleanup();
    }
  }

  /**
   * Utility function to delay execution
   */
  delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
}

// Run the tests if this file is executed directly
if (require.main === module) {
  const test = new TestRelay();
  test.runAllTests().catch(error => {
    console.error('Fatal error:', error);
    process.exit(1);
  });
}

module.exports = TestRelay;
