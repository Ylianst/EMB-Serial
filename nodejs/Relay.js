const net = require('net');
const SerialStack = require('./SerialStack');
const TcpProtocol = require('./TcpProtocol');

// Configuration
const PORT = 8888;
const HOST = '0.0.0.0';
const RELAY_VERSION = '1.0.0';

// Track active connection and serial stack
let activeConnection = null;
let serialStack = null;

// Configuration state
let config = {
  serialPort: '/dev/ttyUSB0',
  baudRate: 19200
};

// Create protocol handler
const protocol = new TcpProtocol();

// Create TCP server
const server = net.createServer((socket) => {
  // Check if there's already an active connection
  if (activeConnection) {
    console.log(`Connection rejected from ${socket.remoteAddress}:${socket.remotePort} - server already has an active connection`);
    socket.end('Server busy - only one connection allowed\n');
    return;
  }

  // Accept the connection
  activeConnection = socket;
  console.log(`Connection accepted from ${socket.remoteAddress}:${socket.remotePort}`);

  // Create SerialStack instance for this connection
  try {
    console.log('Creating SerialStack instance...');
    serialStack = new SerialStack(config.serialPort, config.baudRate);
    console.log('SerialStack instance created');
  } catch (error) {
    console.error(`Failed to create SerialStack: ${error.message}`);
    socket.end('Failed to initialize serial connection\n');
    activeConnection = null;
    return;
  }

  // Buffer for accumulating incoming data
  let receiveBuffer = Buffer.alloc(0);

  // Handle incoming data
  socket.on('data', (data) => {
    // Append new data to receive buffer
    receiveBuffer = Buffer.concat([receiveBuffer, data]);
    console.log(`Received ${data.length} bytes, buffer now ${receiveBuffer.length} bytes`);

    // Protect against buffer overflow (max 1MB)
    if (receiveBuffer.length > 1048576) {
      console.error('Receive buffer overflow, closing connection');
      socket.destroy();
      return;
    }

    // Try to decode messages from the buffer
    while (receiveBuffer.length > 0) {
      const message = protocol.decodeMessage(receiveBuffer);
      
      if (!message) {
        // Incomplete message, wait for more data
        console.log('Incomplete message, waiting for more data...');
        break;
      }

      // Complete message received
      console.log(`Decoded message: ${message.messageType} (RequestID: ${message.requestId}, Payload: ${message.payloadLength} bytes)`);

      // Remove the decoded message from the buffer
      receiveBuffer = receiveBuffer.slice(message.totalLength);

      // Handle the message
      handleMessage(socket, message);
    }
  });

  // Handle connection close
  socket.on('end', async () => {
    console.log(`Connection closed from ${socket.remoteAddress}:${socket.remotePort}`);
    
    // Clear the receive buffer
    receiveBuffer = Buffer.alloc(0);
    
    // Close SerialStack if it exists
    if (serialStack) {
      try {
        // End embroidery session if one is open
        if (serialStack.isOpen) {
          console.log('Ending embroidery session...');
          await serialStack.EndEmbroiderySession();
        }
        
        console.log('Closing SerialStack instance...');
        await serialStack.close();
        console.log('SerialStack instance closed');
      } catch (error) {
        console.error(`Error closing SerialStack: ${error.message}`);
      }
      serialStack = null;
    }
    
    activeConnection = null;
  });

  // Handle errors
  socket.on('error', async (err) => {
    console.error(`Socket error: ${err.message}`);
    
    // Clear the receive buffer
    receiveBuffer = Buffer.alloc(0);
    
    // Close SerialStack if it exists
    if (serialStack) {
      try {
        // End embroidery session if one is open
        if (serialStack.isOpen) {
          console.log('Ending embroidery session...');
          await serialStack.EndEmbroiderySession();
        }
        
        console.log('Closing SerialStack instance due to socket error...');
        await serialStack.close();
        console.log('SerialStack instance closed');
      } catch (error) {
        console.error(`Error closing SerialStack: ${error.message}`);
      }
      serialStack = null;
    }
    
    activeConnection = null;
  });
});

// Handle server errors
server.on('error', (err) => {
  if (err.code === 'EADDRINUSE') {
    console.error(`Port ${PORT} is already in use`);
  } else {
    console.error(`Server error: ${err.message}`);
  }
  process.exit(1);
});

// Start listening
server.listen(PORT, HOST, () => {
  console.log(`Relay.js TCP server listening on ${HOST}:${PORT}`);
  console.log('Accepting only one connection at a time');
  console.log('Press Ctrl+C to stop');
});

/**
 * Handle a decoded protocol message
 */
function handleMessage(socket, message) {
  const { messageType, requestId, payload } = message;

  try {
    switch (messageType) {
      case protocol.MessageTypes.GCFG: // Get Configuration
        handleGetConfig(socket, requestId);
        break;

      case protocol.MessageTypes.SCFG: // Set Configuration
        handleSetConfig(socket, requestId, payload);
        break;

      case protocol.MessageTypes.STAT: // Get Status
        handleGetStatus(socket, requestId);
        break;

      case protocol.MessageTypes.READ: // Read Memory
        handleRead(socket, requestId, payload);
        break;

      case protocol.MessageTypes.LRED: // Large Read Memory
        handleLargeRead(socket, requestId, payload);
        break;

      case protocol.MessageTypes.WRIT: // Write Memory
        handleWrite(socket, requestId, payload);
        break;

      case protocol.MessageTypes.UPLD: // Upload Block
        handleUpload(socket, requestId, payload);
        break;

      case protocol.MessageTypes.CSUM: // Calculate Checksum
        handleChecksum(socket, requestId, payload);
        break;

      case protocol.MessageTypes.SOPE: // Session Open
        handleSessionOpen(socket, requestId);
        break;

      case protocol.MessageTypes.SCLO: // Session Close
        handleSessionClose(socket, requestId);
        break;

      case protocol.MessageTypes.BAUD: // Change Baud Rate
        handleBaudChange(socket, requestId, payload);
        break;

      case protocol.MessageTypes.RSET: // Protocol Reset
        handleReset(socket, requestId);
        break;

      default:
        console.warn(`Unknown message type: ${messageType}`);
        const errorResponse = protocol.createErrorResponse(
          requestId,
          `Unknown message type: ${messageType}`,
          protocol.ErrorCodes.INVALID_FORMAT
        );
        socket.write(errorResponse);
        break;
    }
  } catch (error) {
    console.error(`Error handling message ${messageType}:`, error.message);
    const errorResponse = protocol.createErrorResponse(
      requestId,
      error.message,
      protocol.ErrorCodes.INVALID_PARAMETERS
    );
    socket.write(errorResponse);
  }
}

/**
 * Handle GCFG - Get Configuration
 */
function handleGetConfig(socket, requestId) {
  console.log('Handling GCFG - Get Configuration');
  const response = protocol.createConfigResponse(requestId, {
    serialPort: config.serialPort,
    baudRate: config.baudRate,
    relayVersion: RELAY_VERSION
  });
  socket.write(response);
}

/**
 * Handle SCFG - Set Configuration
 */
function handleSetConfig(socket, requestId, payload) {
  console.log('Handling SCFG - Set Configuration');
  try {
    const newConfig = protocol.parseJsonPayload(payload);
    
    // Only allow configuration changes when serial port is not open
    if (serialStack && serialStack.isOpen) {
      const errorResponse = protocol.createErrorResponse(
        requestId,
        'Cannot change configuration while serial port is open',
        protocol.ErrorCodes.INVALID_PARAMETERS
      );
      socket.write(errorResponse);
      return;
    }
    
    if (newConfig.serialPort) {
      config.serialPort = newConfig.serialPort;
    }
    if (newConfig.baudRate) {
      config.baudRate = newConfig.baudRate;
    }

    // Recreate SerialStack with new configuration
    if (serialStack) {
      serialStack = new SerialStack(config.serialPort, config.baudRate);
    }

    const response = protocol.createConfigResponse(requestId, {
      success: true,
      message: 'Configuration updated'
    });
    socket.write(response);
  } catch (error) {
    const errorResponse = protocol.createErrorResponse(
      requestId,
      `Failed to set configuration: ${error.message}`,
      protocol.ErrorCodes.INVALID_PARAMETERS
    );
    socket.write(errorResponse);
  }
}

/**
 * Handle STAT - Get Status
 */
async function handleGetStatus(socket, requestId) {
  console.log('Handling STAT - Get Status');
  
  let sessionOpen = false;
  if (serialStack && serialStack.isOpen) {
    try {
      sessionOpen = await serialStack.IsEmbroiderySessionOpen();
    } catch (error) {
      console.error('Error checking session status:', error.message);
    }
  }
  
  const status = {
    connected: serialStack ? serialStack.isOpen : false,
    baudRate: serialStack ? serialStack.baudRate : config.baudRate,
    sessionOpen: sessionOpen,
    lastError: ''
  };

  const response = protocol.createStatusResponse(requestId, status);
  socket.write(response);
}

/**
 * Handle READ - Read Memory
 */
async function handleRead(socket, requestId, payload) {
  console.log('Handling READ - Read Memory');
  try {
    if (!serialStack || !serialStack.isOpen) {
      const errorResponse = protocol.createErrorResponse(
        requestId,
        'Serial port not connected',
        protocol.ErrorCodes.PORT_NOT_CONNECTED
      );
      socket.write(errorResponse);
      return;
    }

    const address = protocol.parseAddress(payload);
    const hexData = await serialStack.read(address);
    
    const response = protocol.createReadDataResponse(requestId, hexData);
    socket.write(response);
  } catch (error) {
    console.error('Read error:', error.message);
    const errorResponse = protocol.createErrorResponse(
      requestId,
      `Read failed: ${error.message}`,
      protocol.ErrorCodes.MACHINE_ERROR
    );
    socket.write(errorResponse);
  }
}

/**
 * Handle LRED - Large Read Memory
 */
async function handleLargeRead(socket, requestId, payload) {
  console.log('Handling LRED - Large Read Memory');
  try {
    if (!serialStack || !serialStack.isOpen) {
      const errorResponse = protocol.createErrorResponse(
        requestId,
        'Serial port not connected',
        protocol.ErrorCodes.PORT_NOT_CONNECTED
      );
      socket.write(errorResponse);
      return;
    }

    const address = protocol.parseAddress(payload);
    const binaryData = await serialStack.largeRead(address);
    
    // Convert string to Buffer for binary data
    const dataBuffer = Buffer.from(binaryData, 'latin1');
    const response = protocol.createLargeDataResponse(requestId, dataBuffer);
    socket.write(response);
  } catch (error) {
    console.error('Large read error:', error.message);
    const errorResponse = protocol.createErrorResponse(
      requestId,
      `Large read failed: ${error.message}`,
      protocol.ErrorCodes.MACHINE_ERROR
    );
    socket.write(errorResponse);
  }
}

/**
 * Handle WRIT - Write Memory
 */
async function handleWrite(socket, requestId, payload) {
  console.log('Handling WRIT - Write Memory');
  try {
    if (!serialStack || !serialStack.isOpen) {
      const errorResponse = protocol.createErrorResponse(
        requestId,
        'Serial port not connected',
        protocol.ErrorCodes.PORT_NOT_CONNECTED
      );
      socket.write(errorResponse);
      return;
    }

    const { address, data } = protocol.parseWritePayload(payload);
    await serialStack.write(address, data);
    
    const response = protocol.createWriteAckResponse(requestId, 'O');
    socket.write(response);
  } catch (error) {
    console.error('Write error:', error.message);
    const errorResponse = protocol.createErrorResponse(
      requestId,
      `Write failed: ${error.message}`,
      protocol.ErrorCodes.MACHINE_ERROR
    );
    socket.write(errorResponse);
  }
}

/**
 * Handle UPLD - Upload Block
 */
async function handleUpload(socket, requestId, payload) {
  console.log('Handling UPLD - Upload Block');
  try {
    if (!serialStack || !serialStack.isOpen) {
      const errorResponse = protocol.createErrorResponse(
        requestId,
        'Serial port not connected',
        protocol.ErrorCodes.PORT_NOT_CONNECTED
      );
      socket.write(errorResponse);
      return;
    }

    const { address, data } = protocol.parseUploadPayload(payload);
    await serialStack.upload(address, data);
    
    const response = protocol.createUploadAckResponse(requestId, 'O');
    socket.write(response);
  } catch (error) {
    console.error('Upload error:', error.message);
    const errorResponse = protocol.createErrorResponse(
      requestId,
      `Upload failed: ${error.message}`,
      protocol.ErrorCodes.MACHINE_ERROR
    );
    socket.write(errorResponse);
  }
}

/**
 * Handle CSUM - Calculate Checksum
 */
async function handleChecksum(socket, requestId, payload) {
  console.log('Handling CSUM - Calculate Checksum');
  try {
    if (!serialStack || !serialStack.isOpen) {
      const errorResponse = protocol.createErrorResponse(
        requestId,
        'Serial port not connected',
        protocol.ErrorCodes.PORT_NOT_CONNECTED
      );
      socket.write(errorResponse);
      return;
    }

    const { address, length } = protocol.parseChecksumPayload(payload);
    const sumValue = await serialStack.sum(address, length);
    
    // Convert sum value to 8-character hex string
    const checksumHex = sumValue.toString(16).toUpperCase().padStart(8, '0');
    const response = protocol.createChecksumResponse(requestId, checksumHex);
    socket.write(response);
  } catch (error) {
    console.error('Checksum error:', error.message);
    const errorResponse = protocol.createErrorResponse(
      requestId,
      `Checksum failed: ${error.message}`,
      protocol.ErrorCodes.MACHINE_ERROR
    );
    socket.write(errorResponse);
  }
}

/**
 * Handle SOPE - Session Open
 */
async function handleSessionOpen(socket, requestId) {
  console.log('Handling SOPE - Session Open');
  try {
    if (!serialStack || !serialStack.isOpen) {
      const errorResponse = protocol.createErrorResponse(
        requestId,
        'Serial port not connected',
        protocol.ErrorCodes.PORT_NOT_CONNECTED
      );
      socket.write(errorResponse);
      return;
    }

    const wasStarted = await serialStack.StartEmbroiderySession();
    
    const response = protocol.createSessionAckResponse(requestId, 'O');
    socket.write(response);
  } catch (error) {
    console.error('Session open error:', error.message);
    const errorResponse = protocol.createErrorResponse(
      requestId,
      `Session open failed: ${error.message}`,
      protocol.ErrorCodes.SESSION_ALREADY_OPEN
    );
    socket.write(errorResponse);
  }
}

/**
 * Handle SCLO - Session Close
 */
async function handleSessionClose(socket, requestId) {
  console.log('Handling SCLO - Session Close');
  try {
    if (!serialStack || !serialStack.isOpen) {
      const errorResponse = protocol.createErrorResponse(
        requestId,
        'Serial port not connected',
        protocol.ErrorCodes.PORT_NOT_CONNECTED
      );
      socket.write(errorResponse);
      return;
    }

    const wasEnded = await serialStack.EndEmbroiderySession();
    
    const response = protocol.createSessionAckResponse(requestId, 'O');
    socket.write(response);
  } catch (error) {
    console.error('Session close error:', error.message);
    const errorResponse = protocol.createErrorResponse(
      requestId,
      `Session close failed: ${error.message}`,
      protocol.ErrorCodes.SESSION_NOT_OPEN
    );
    socket.write(errorResponse);
  }
}

/**
 * Handle BAUD - Change Baud Rate
 * Note: Ignores client's baud rate request. Always auto-detects and upgrades to 57600.
 */
async function handleBaudChange(socket, requestId, payload) {
  console.log('Handling BAUD - Auto-detect and upgrade to maximum baud rate');
  try {
    if (!serialStack) {
      const errorResponse = protocol.createErrorResponse(
        requestId,
        'Serial stack not initialized',
        protocol.ErrorCodes.PORT_NOT_CONFIGURED
      );
      socket.write(errorResponse);
      return;
    }

    // If port is not open, open it with auto-detection
    if (!serialStack.isOpen) {
      console.log('Opening serial port with auto-detection...');
      await serialStack.open(); // Auto-detects baud rate
      
      // Try to upgrade to 57600 if not already there
      if (serialStack.baudRate !== 57600) {
        console.log(`Currently at ${serialStack.baudRate} baud, upgrading to 57600...`);
        try {
          await serialStack.upgradeSpeed();
          console.log(`Successfully upgraded to ${serialStack.baudRate} baud`);
        } catch (error) {
          console.log(`Could not upgrade to 57600, staying at ${serialStack.baudRate} baud`);
        }
      } else {
        console.log('Already at 57600 baud');
      }
    } else {
      // Port is already open, try to upgrade if not at 57600
      if (serialStack.baudRate !== 57600) {
        console.log(`Port already open at ${serialStack.baudRate} baud, upgrading to 57600...`);
        try {
          await serialStack.upgradeSpeed();
          console.log(`Successfully upgraded to ${serialStack.baudRate} baud`);
        } catch (error) {
          console.log(`Could not upgrade to 57600, staying at ${serialStack.baudRate} baud`);
        }
      } else {
        console.log('Already at 57600 baud');
      }
    }
    
    const response = protocol.createBaudAckResponse(requestId, 'O');
    socket.write(response);
  } catch (error) {
    console.error('Baud rate change error:', error.message);
    const errorResponse = protocol.createErrorResponse(
      requestId,
      `Baud rate change failed: ${error.message}`,
      protocol.ErrorCodes.BAUD_CHANGE_FAILED
    );
    socket.write(errorResponse);
  }
}

/**
 * Handle RSET - Protocol Reset
 */
async function handleReset(socket, requestId) {
  console.log('Handling RSET - Protocol Reset');
  try {
    if (!serialStack || !serialStack.isOpen) {
      const errorResponse = protocol.createErrorResponse(
        requestId,
        'Serial port not connected',
        protocol.ErrorCodes.PORT_NOT_CONNECTED
      );
      socket.write(errorResponse);
      return;
    }

    await serialStack.resync();
    
    const response = protocol.createResetAckResponse(requestId, 'O');
    socket.write(response);
  } catch (error) {
    console.error('Reset error:', error.message);
    const errorResponse = protocol.createErrorResponse(
      requestId,
      `Reset failed: ${error.message}`,
      protocol.ErrorCodes.MACHINE_ERROR
    );
    socket.write(errorResponse);
  }
}

// Handle graceful shutdown
process.on('SIGINT', async () => {
  console.log('\nShutting down server...');
  
  // Close SerialStack if it exists
  if (serialStack) {
    try {
      // End embroidery session if one is open
      if (serialStack.isOpen) {
        console.log('Ending embroidery session...');
        await serialStack.EndEmbroiderySession();
      }
      
      console.log('Closing SerialStack instance...');
      await serialStack.close();
      console.log('SerialStack instance closed');
    } catch (error) {
      console.error(`Error closing SerialStack: ${error.message}`);
    }
    serialStack = null;
  }
  
  if (activeConnection) {
    activeConnection.end();
  }
  
  server.close(() => {
    console.log('Server closed');
    process.exit(0);
  });
});
