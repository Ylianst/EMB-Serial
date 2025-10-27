using System.IO.Ports;
using System.Text;
using Bernina.SerialStack;
using Microsoft.Win32;

namespace SerialComm
{
    public partial class MainForm : Form
    {
        private SerialStack? _serialStack;
        private bool _isConnected = false;
        private bool _showSerialTraffic = false;
        private bool _showDebug = false;
        private string? _selectedComPort = null;
        private bool _autoSwitchTo57600 = false;
        private const string RegistryKeyPath = @"Software\BerninaSerialComm";
        private const string ComPortValueName = "LastComPort";
        private const string AutoSwitch57600ValueName = "AutoSwitchTo57600";

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Set default read address
            txtReadAddress.Text = "200100";

            // Load last used COM port from registry
            LoadLastComPort();
            
            // Load auto-switch setting from registry
            LoadAutoSwitchSetting();

            // Populate COM port menu
            RefreshComPortsMenu();

            // Initialize status
            UpdateConnectionStatus("Disconnected", false);
            
            // Disable command buttons until connected
            btnRead.Enabled = false;
            btnLargeRead.Enabled = false;
            btnWrite.Enabled = false;
        }

        private void RefreshComPortsMenu()
        {
            // Clear existing COM port menu items (keep other menu items)
            var itemsToRemove = new List<ToolStripItem>();
            foreach (ToolStripItem item in selectCOMPortToolStripMenuItem.DropDownItems)
            {
                if (item.Tag?.ToString() == "comport")
                {
                    itemsToRemove.Add(item);
                }
            }
            foreach (var item in itemsToRemove)
            {
                selectCOMPortToolStripMenuItem.DropDownItems.Remove(item);
            }

            // Add available COM ports
            string[] ports = SerialPort.GetPortNames();
            
            if (ports.Length > 0)
            {
                foreach (string port in ports)
                {
                    ToolStripMenuItem portItem = new ToolStripMenuItem(port);
                    portItem.Tag = "comport";
                    portItem.Click += ComPortMenuItem_Click;
                    
                    // Check the currently selected port
                    if (port == _selectedComPort)
                    {
                        portItem.Checked = true;
                    }
                    
                    selectCOMPortToolStripMenuItem.DropDownItems.Add(portItem);
                }
            }
            else
            {
                ToolStripMenuItem noPortsItem = new ToolStripMenuItem("No ports available");
                noPortsItem.Tag = "comport";
                noPortsItem.Enabled = false;
                selectCOMPortToolStripMenuItem.DropDownItems.Add(noPortsItem);
            }
        }

        private void ComPortMenuItem_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem)
            {
                // Uncheck all other COM port menu items
                foreach (ToolStripItem item in selectCOMPortToolStripMenuItem.DropDownItems)
                {
                    if (item is ToolStripMenuItem mi && item.Tag?.ToString() == "comport")
                    {
                        mi.Checked = false;
                    }
                }
                
                // Check the selected item
                menuItem.Checked = true;
                _selectedComPort = menuItem.Text;
                
                UpdateStatus($"Selected COM port: {_selectedComPort}");
            }
        }

        private void LoadLastComPort()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
                {
                    if (key != null)
                    {
                        string? lastPort = key.GetValue(ComPortValueName) as string;
                        if (!string.IsNullOrEmpty(lastPort))
                        {
                            _selectedComPort = lastPort;
                        }
                    }
                }
            }
            catch
            {
                // If registry read fails, just use default selection
            }
        }

        private void LoadAutoSwitchSetting()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
                {
                    if (key != null)
                    {
                        object? value = key.GetValue(AutoSwitch57600ValueName);
                        if (value != null)
                        {
                            _autoSwitchTo57600 = Convert.ToBoolean(value);
                            autoSwitchTo57600BaudToolStripMenuItem.Checked = _autoSwitchTo57600;
                        }
                    }
                }
            }
            catch
            {
                // If registry read fails, use default (false)
            }
        }

        private void SaveAutoSwitchSetting(bool enabled)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath))
                {
                    key.SetValue(AutoSwitch57600ValueName, enabled);
                }
            }
            catch
            {
                // If registry write fails, continue without saving
            }
        }

        private void SaveLastComPort(string portName)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath))
                {
                    key.SetValue(ComPortValueName, portName);
                }
            }
            catch
            {
                // If registry write fails, continue without saving
            }
        }

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            if (_isConnected)
            {
                Disconnect();
            }
            else
            {
                await ConnectAsync();
            }
        }

        private async Task ConnectAsync()
        {
            if (string.IsNullOrEmpty(_selectedComPort))
            {
                MessageBox.Show("Please select a COM port from the Connection menu first.", "Connection Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string portName = _selectedComPort;

            // Disable connection controls
            btnConnect.Enabled = false;
            connectToolStripMenuItem.Enabled = false;
            selectCOMPortToolStripMenuItem.Enabled = false;

            UpdateStatus("Connecting to " + portName + "...");
            UpdateConnectionStatus("Connecting...", false);
            AppendOutput("Attempting to connect to " + portName);

            try
            {
                // Create serial stack
                _serialStack = new SerialStack(portName);

                // Subscribe to events
                _serialStack.ConnectionStateChanged += OnConnectionStateChanged;
                _serialStack.CommandCompleted += OnCommandCompleted;
                _serialStack.SerialTraffic += OnSerialTraffic;
                _serialStack.DebugMessage += OnDebugMessage;

                // Try to connect
                bool connected = await _serialStack.OpenAsync();

                if (connected)
                {
                    _isConnected = true;

                    // Save the COM port to registry for next time
                    SaveLastComPort(portName);

                    // Update UI
                    btnConnect.Text = "Disconnect";
                    btnConnect.Enabled = true;

                    // Update menu items
                    connectToolStripMenuItem.Enabled = false;
                    disconnectToolStripMenuItem.Enabled = true;
                    selectCOMPortToolStripMenuItem.Enabled = false;
                    sessionStartToolStripMenuItem.Enabled = true;
                    sessionEndToolStripMenuItem.Enabled = true;
                    protocolResetToolStripMenuItem.Enabled = true;
                    firmwareInfoToolStripMenuItem.Enabled = true;
                    readEmbroideryFilesToolStripMenuItem.Enabled = true;
                    readToolStripMenuItem.Enabled = true;
                    largeReadToolStripMenuItem.Enabled = true;
                    writeToolStripMenuItem.Enabled = true;
                    sumToolStripMenuItem.Enabled = true;
                    memoryViewerToolStripMenuItem.Enabled = true;
                    
                    // Enable command buttons
                    btnRead.Enabled = true;
                    btnLargeRead.Enabled = true;
                    btnWrite.Enabled = true;

                    UpdateStatus("Connected");
                    UpdateConnectionStatus($"Connected: {portName} @ {_serialStack.BaudRate} baud", true);
                    UpdateBaudRateMenuItems();
                    AppendOutput($"Connected successfully at {_serialStack.BaudRate} baud");
                    
                    // Auto-switch to 57600 if enabled and currently at 19200
                    if (_autoSwitchTo57600 && _serialStack.BaudRate == 19200)
                    {
                        AppendOutput("Auto-switching to 57600 baud...");
                        await Task.Delay(100); // Brief pause
                        await SwitchTo57600BaudAsync();
                    }
                    else
                    {
                        AppendOutput("Ready to send commands");
                        AppendOutput("");
                    }
                }
                else
                {
                    _serialStack?.Dispose();
                    _serialStack = null;

                    btnConnect.Text = "Connect";
                    btnConnect.Enabled = true;
                    connectToolStripMenuItem.Enabled = true;
                    selectCOMPortToolStripMenuItem.Enabled = true;

                    UpdateStatus("Connection failed");
                    UpdateConnectionStatus("Disconnected", false);
                    AppendOutput("Connection failed - machine did not respond");

                    MessageBox.Show("Failed to connect. Make sure the machine is powered on and connected.",
                        "Connection Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                _serialStack?.Dispose();
                _serialStack = null;

                btnConnect.Text = "Connect";
                btnConnect.Enabled = true;
                connectToolStripMenuItem.Enabled = true;
                selectCOMPortToolStripMenuItem.Enabled = true;

                UpdateStatus("Connection error");
                UpdateConnectionStatus("Disconnected", false);
                AppendOutput($"Connection error: {ex.Message}");

                MessageBox.Show($"Connection error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            Disconnect();
        }

        private void Disconnect()
        {
            if (_serialStack != null)
            {
                AppendOutput("Disconnecting...");
                _serialStack.Close();
                _serialStack.Dispose();
                _serialStack = null;
            }

            _isConnected = false;

            // Update UI
            btnConnect.Text = "Connect";
            btnConnect.Enabled = true;

            // Update menu items
            connectToolStripMenuItem.Enabled = true;
            disconnectToolStripMenuItem.Enabled = false;
            selectCOMPortToolStripMenuItem.Enabled = true;
            sessionStartToolStripMenuItem.Enabled = false;
            sessionEndToolStripMenuItem.Enabled = false;
            protocolResetToolStripMenuItem.Enabled = false;
            firmwareInfoToolStripMenuItem.Enabled = false;
            readEmbroideryFilesToolStripMenuItem.Enabled = false;
            readToolStripMenuItem.Enabled = false;
            largeReadToolStripMenuItem.Enabled = false;
            writeToolStripMenuItem.Enabled = false;
            sumToolStripMenuItem.Enabled = false;
            memoryViewerToolStripMenuItem.Enabled = false;
            
            // Disable command buttons
            btnRead.Enabled = false;
            btnLargeRead.Enabled = false;
            btnWrite.Enabled = false;
            
            UpdateBaudRateMenuItems();

            UpdateStatus("Disconnected");
            UpdateConnectionStatus("Disconnected", false);
            AppendOutput("Disconnected");
            AppendOutput("");
        }

        private async void btnRead_Click(object sender, EventArgs e)
        {
            await PerformReadAsync();
        }

        private async Task PerformReadAsync()
        {
            if (_serialStack == null || !_isConnected)
            {
                MessageBox.Show("Not connected to machine.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string addressStr = txtReadAddress.Text.Trim();
            if (string.IsNullOrWhiteSpace(addressStr))
            {
                MessageBox.Show("Please enter a read address.", "Input Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                int address = Convert.ToInt32(addressStr, 16);

                UpdateStatus($"Reading from 0x{address:X6}...");
                AppendOutput($"Read command: Address 0x{address:X6}");

                var result = await _serialStack.ReadAsync(address);

                if (result.Success)
                {
                    AppendOutput($"Read successful:");
                    AppendOutput($"  Hex Data: {result.Response}");

                    if (result.BinaryData != null && result.BinaryData.Length > 0)
                    {
                        // Display as ASCII if printable
                        StringBuilder ascii = new StringBuilder();
                        foreach (byte b in result.BinaryData)
                        {
                            if (b >= 32 && b <= 126)
                                ascii.Append((char)b);
                            else
                                ascii.Append('.');
                        }
                        AppendOutput($"  ASCII: {ascii}");

                        // Display hex dump
                        AppendOutput($"  Hex Dump:");
                        for (int i = 0; i < result.BinaryData.Length; i += 16)
                        {
                            StringBuilder hexLine = new StringBuilder($"    {i:X4}: ");
                            StringBuilder asciiLine = new StringBuilder("  ");

                            for (int j = 0; j < 16 && i + j < result.BinaryData.Length; j++)
                            {
                                byte b = result.BinaryData[i + j];
                                hexLine.Append($"{b:X2} ");
                                asciiLine.Append((b >= 32 && b <= 126) ? (char)b : '.');
                            }

                            AppendOutput(hexLine.ToString() + asciiLine.ToString());
                        }
                    }

                    UpdateStatus("Read complete");
                }
                else
                {
                    AppendOutput($"Read failed: {result.ErrorMessage}");
                    UpdateStatus("Read failed");
                    MessageBox.Show($"Read failed: {result.ErrorMessage}", "Command Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                AppendOutput("");
            }
            catch (Exception ex)
            {
                AppendOutput($"Read error: {ex.Message}");
                UpdateStatus("Error");
                MessageBox.Show($"Invalid address format. Use hex format (e.g., 200100).", "Input Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async void btnLargeRead_Click(object sender, EventArgs e)
        {
            await PerformLargeReadAsync();
        }

        private async Task PerformLargeReadAsync()
        {
            if (_serialStack == null || !_isConnected)
            {
                MessageBox.Show("Not connected to machine.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string addressStr = txtReadAddress.Text.Trim();
            if (string.IsNullOrWhiteSpace(addressStr))
            {
                MessageBox.Show("Please enter an address.", "Input Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                int address = Convert.ToInt32(addressStr, 16);

                UpdateStatus($"Large reading from 0x{address:X6}...");
                AppendOutput($"Large Read command: Address 0x{address:X6}");

                var result = await _serialStack.LargeReadAsync(address);

                if (result.Success)
                {
                    AppendOutput($"Large Read successful:");

                    if (result.BinaryData != null && result.BinaryData.Length > 0)
                    {
                        // Display as ASCII
                        StringBuilder ascii = new StringBuilder();
                        foreach (byte b in result.BinaryData)
                        {
                            if (b >= 32 && b <= 126)
                                ascii.Append((char)b);
                            else
                                ascii.Append('.');
                        }
                        AppendOutput($"  ASCII: {ascii}");

                        // Display hex dump
                        AppendOutput($"  Hex Dump:");
                        for (int i = 0; i < result.BinaryData.Length; i += 16)
                        {
                            StringBuilder hexLine = new StringBuilder($"    {i:X4}: ");
                            StringBuilder asciiLine = new StringBuilder("  ");

                            for (int j = 0; j < 16 && i + j < result.BinaryData.Length; j++)
                            {
                                byte b = result.BinaryData[i + j];
                                hexLine.Append($"{b:X2} ");
                                asciiLine.Append((b >= 32 && b <= 126) ? (char)b : '.');
                            }

                            AppendOutput(hexLine.ToString() + asciiLine.ToString());
                        }
                    }

                    UpdateStatus("Large Read complete");
                }
                else
                {
                    AppendOutput($"Large Read failed: {result.ErrorMessage}");
                    UpdateStatus("Large Read failed");
                    MessageBox.Show($"Large Read failed: {result.ErrorMessage}", "Command Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                AppendOutput("");
            }
            catch (Exception ex)
            {
                AppendOutput($"Large Read error: {ex.Message}");
                UpdateStatus("Error");
                MessageBox.Show($"Invalid address format. Use hex format (e.g., 0240F5).", "Input Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async void btnWrite_Click(object sender, EventArgs e)
        {
            await PerformWriteAsync();
        }

        private async Task PerformWriteAsync()
        {
            if (_serialStack == null || !_isConnected)
            {
                MessageBox.Show("Not connected to machine.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string addressStr = txtWriteAddress.Text.Trim();
            string dataStr = txtWriteData.Text.Trim().Replace(" ", "");

            if (string.IsNullOrWhiteSpace(addressStr))
            {
                MessageBox.Show("Please enter a write address.", "Input Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(dataStr))
            {
                MessageBox.Show("Please enter data to write.", "Input Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                int address = Convert.ToInt32(addressStr, 16);

                // Convert hex string to byte array
                if (dataStr.Length % 2 != 0)
                {
                    MessageBox.Show("Data must be valid hex (even number of characters).", "Input Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                byte[] data = new byte[dataStr.Length / 2];
                for (int i = 0; i < dataStr.Length; i += 2)
                {
                    data[i / 2] = Convert.ToByte(dataStr.Substring(i, 2), 16);
                }

                UpdateStatus($"Writing to 0x{address:X6}...");
                AppendOutput($"Write command: Address 0x{address:X6}, Data: {dataStr}");

                var result = await _serialStack.WriteAsync(address, data);

                if (result.Success)
                {
                    AppendOutput($"Write successful");
                    UpdateStatus("Write complete");
                }
                else
                {
                    AppendOutput($"Write failed: {result.ErrorMessage}");
                    UpdateStatus("Write failed");
                    MessageBox.Show($"Write failed: {result.ErrorMessage}", "Command Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                AppendOutput("");
            }
            catch (Exception ex)
            {
                AppendOutput($"Write error: {ex.Message}");
                UpdateStatus("Error");
                MessageBox.Show($"Invalid input format. Use hex format (e.g., Address: 200100, Data: 01).", "Input Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async Task PerformSumAsync()
        {
            if (_serialStack == null || !_isConnected)
            {
                MessageBox.Show("Not connected to machine.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Show dialog to get address and length
            using (SumDialog dialog = new SumDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    int address = dialog.Address;
                    int length = dialog.Length;

                    UpdateStatus($"Sending Sum command...");
                    AppendOutput($"Sum Command: Address 0x{address:X6}, Length 0x{length:X6}");

                    var result = await _serialStack.SumCommandAsync(address, length);

                    if (result.Success)
                    {
                        AppendOutput($"Sum Command successful:");
                        AppendOutput($"  Checksum: 0x{result.Response}");

                        // Try to parse the response as a hex number for display
                        if (long.TryParse(result.Response, System.Globalization.NumberStyles.HexNumber, null, out long checksumValue))
                        {
                            AppendOutput($"  Decimal: {checksumValue}");
                        }

                        UpdateStatus("Sum Command complete");
                    }
                    else
                    {
                        AppendOutput($"Sum Command failed: {result.ErrorMessage}");
                        UpdateStatus("Sum Command failed");
                        MessageBox.Show($"Sum Command failed: {result.ErrorMessage}", "Command Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    AppendOutput("");
                }
            }
        }

        private async Task PerformSessionStartAsync()
        {
            if (_serialStack == null || !_isConnected)
            {
                MessageBox.Show("Not connected to machine.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check if we're in Sewing Machine mode
            UpdateStatus("Checking session mode...");
            var sessionMode = await _serialStack.GetCurrentSessionModeAsync();
            
            if (sessionMode == null)
            {
                MessageBox.Show("Failed to detect session mode.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("Ready");
                return;
            }

            // Only start session if we're in Sewing Machine mode
            if (sessionMode != Bernina.SerialStack.SessionMode.SewingMachine)
            {
                AppendOutput("Session Start: Already in Embroidery Module mode (session already open)");
                UpdateStatus("Ready");
                return;
            }

            UpdateStatus("Sending Session Start command...");
            AppendOutput("Session Start command: Sending TrMEYQ");

            var result = await _serialStack.SessionStartAsync();

            if (result.Success)
            {
                AppendOutput("Session Start successful");
                UpdateStatus("Session Start complete");
            }
            else
            {
                AppendOutput($"Session Start failed: {result.ErrorMessage}");
                UpdateStatus("Session Start failed");
                MessageBox.Show($"Session Start failed: {result.ErrorMessage}", "Command Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            AppendOutput("");
        }

        private async Task PerformSessionEndAsync()
        {
            if (_serialStack == null || !_isConnected)
            {
                MessageBox.Show("Not connected to machine.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            UpdateStatus("Sending Session End command...");
            AppendOutput("Session End command: Sending TrME");

            var result = await _serialStack.SessionEndAsync();

            if (result.Success)
            {
                AppendOutput("Session End successful");
                UpdateStatus("Session End complete");
            }
            else
            {
                AppendOutput($"Session End failed: {result.ErrorMessage}");
                UpdateStatus("Session End failed");
                MessageBox.Show($"Session End failed: {result.ErrorMessage}", "Command Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            AppendOutput("");
        }

        private async Task PerformProtocolResetAsync()
        {
            if (_serialStack == null || !_isConnected)
            {
                MessageBox.Show("Not connected to machine.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            UpdateStatus("Sending Protocol Reset command...");
            AppendOutput("Protocol Reset command: Sending RF?");

            var result = await _serialStack.ProtocolResetAsync();

            if (result.Success)
            {
                AppendOutput("Protocol Reset successful");
                UpdateStatus("Protocol Reset complete");
            }
            else
            {
                AppendOutput($"Protocol Reset failed: {result.ErrorMessage}");
                UpdateStatus("Protocol Reset failed");
                MessageBox.Show($"Protocol Reset failed: {result.ErrorMessage}", "Command Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            AppendOutput("");
        }

        private async Task PerformFirmwareInfoAsync()
        {
            if (_serialStack == null || !_isConnected)
            {
                MessageBox.Show("Not connected to machine.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            UpdateStatus("Reading firmware information...");
            AppendOutput("Firmware Info: Detecting mode and reading from address 0x200100");

            var firmwareInfo = await _serialStack.ReadFirmwareInfoAsync();

            if (firmwareInfo != null)
            {
                // Convert SessionMode enum to string
                string modeString = firmwareInfo.Mode == Bernina.SerialStack.SessionMode.SewingMachine 
                    ? "Sewing Machine" 
                    : "Embroidery Module";
                
                AppendOutput("Firmware Info successful:");
                AppendOutput($"  Mode: {modeString}");
                AppendOutput($"  Version: {firmwareInfo.Version}");
                
                // Only display language if it's not null (Sewing Machine mode)
                if (firmwareInfo.Language != null)
                {
                    AppendOutput($"  Language: {firmwareInfo.Language}");
                }
                
                AppendOutput($"  Manufacturer: {firmwareInfo.Manufacturer}");
                AppendOutput($"  Date: {firmwareInfo.Date}");
                
                // Only display PC card status in Embroidery Module mode
                if (firmwareInfo.Mode == Bernina.SerialStack.SessionMode.EmbroideryModule)
                {
                    string pcCardStatus = firmwareInfo.PcCardInserted ? "Yes" : "No";
                    AppendOutput($"  PC Card Inserted: {pcCardStatus}");
                }
                
                UpdateStatus("Firmware Info complete");
            }
            else
            {
                AppendOutput("Firmware Info failed: Unable to read or parse firmware information");
                UpdateStatus("Firmware Info failed");
                MessageBox.Show("Failed to read firmware information from the machine.", "Command Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            AppendOutput("");
        }

        private async Task PerformReadEmbroideryFilesAsync()
        {
            if (_serialStack == null || !_isConnected)
            {
                MessageBox.Show("Not connected to machine.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            UpdateStatus("Reading embroidery files from internal memory...");
            AppendOutput("Read Embroidery Files: Reading from Embroidery Module Memory");

            // Read files from Embroidery Module Memory
            var files = await _serialStack.ReadEmbroideryFilesAsync(Bernina.SerialStack.StorageLocation.EmbroideryModuleMemory);

            if (files != null)
            {
                AppendOutput($"Read Embroidery Files successful: Found {files.Count} file(s)");
                AppendOutput("");

                if (files.Count > 0)
                {
                    // Display file list
                    AppendOutput("File List:");
                    AppendOutput("  ID  | Attributes | File Name");
                    AppendOutput("  ----|------------|----------------------------------");

                    foreach (var file in files)
                    {
                        // Decode file attributes
                        string attrDesc = DecodeFileAttributes(file.FileAttributes);
                        
                        AppendOutput($"  {file.FileId,3} | 0x{file.FileAttributes:X2} ({attrDesc,4}) | {file.FileName}");
                    }
                }
                else
                {
                    AppendOutput("  (No files found)");
                }

                UpdateStatus("Read Embroidery Files complete");
            }
            else
            {
                AppendOutput("Read Embroidery Files failed: Unable to read file list");
                UpdateStatus("Read Embroidery Files failed");
                MessageBox.Show("Failed to read embroidery files from the machine.", "Command Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            AppendOutput("");
        }

        private string DecodeFileAttributes(byte attributes)
        {
            // Decode file attributes based on documentation
            // 0xAC (10101100) = 2 block, Readonly, Alphabet
            // 0xA4 (10100100) = 1 block, Readonly
            // 0x86 (10000110) = 1 block, Memory
            
            bool isReadonly = (attributes & 0x80) != 0;  // Bit 7
            bool isTwoBlock = (attributes & 0x08) != 0;  // Bit 3
            bool isAlphabet = (attributes & 0x04) != 0;  // Bit 2
            
            if (isReadonly && isTwoBlock && isAlphabet)
                return "R2A";  // Readonly, 2 block, Alphabet
            else if (isReadonly && !isTwoBlock)
                return "RO";   // Readonly
            else if (!isReadonly)
                return "RW";   // Read/Write (Memory)
            else
                return "??";   // Unknown
        }

        private async void btnSwitchBaud_Click(object sender, EventArgs e)
        {
            await SwitchTo57600BaudAsync();
        }

        private async Task SwitchTo19200BaudAsync()
        {
            if (_serialStack == null || !_isConnected)
            {
                MessageBox.Show("Not connected to machine.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_serialStack.BaudRate == 19200)
            {
                MessageBox.Show("Already connected at 19200 baud.", "Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            UpdateStatus("Switching to 19200 baud...");
            AppendOutput($"Switching from {_serialStack.BaudRate} to 19200 baud...");

            bool success = await _serialStack.ChangeTo19200BaudAsync();

            if (success)
            {
                AppendOutput($"Successfully switched to 19200 baud");
            UpdateConnectionStatus($"Connected: {_selectedComPort} @ {_serialStack.BaudRate} baud", true);
                UpdateStatus("Baud rate changed");
                UpdateBaudRateMenuItems();
            }
            else
            {
                AppendOutput("Failed to switch baud rate");
                UpdateStatus("Baud rate change failed");
                MessageBox.Show("Failed to switch to 19200 baud.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            AppendOutput("");
        }

        private async Task SwitchTo57600BaudAsync()
        {
            if (_serialStack == null || !_isConnected)
            {
                MessageBox.Show("Not connected to machine.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_serialStack.BaudRate == 57600)
            {
                MessageBox.Show("Already connected at 57600 baud.", "Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            UpdateStatus("Switching to 57600 baud...");
            AppendOutput($"Switching from {_serialStack.BaudRate} to 57600 baud...");

            bool success = await _serialStack.ChangeTo57600BaudAsync();

            if (success)
            {
                AppendOutput($"Successfully switched to 57600 baud");
                UpdateConnectionStatus($"Connected: {_selectedComPort} @ {_serialStack.BaudRate} baud", true);
                UpdateStatus("Baud rate changed");
                UpdateBaudRateMenuItems();
            }
            else
            {
                AppendOutput("Failed to switch baud rate");
                UpdateStatus("Baud rate change failed");
                MessageBox.Show("Failed to switch to 57600 baud.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            AppendOutput("");
        }

        // Menu item handlers
        private void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnConnect_Click(sender, e);
        }

        private void disconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnDisconnect_Click(sender, e);
        }

        private void readToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnRead_Click(sender, e);
        }

        private void largeReadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnLargeRead_Click(sender, e);
        }

        private void writeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnWrite_Click(sender, e);
        }

        private void sumToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _ = PerformSumAsync();
        }

        private void sessionStartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _ = PerformSessionStartAsync();
        }

        private void sessionEndToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _ = PerformSessionEndAsync();
        }

        private void protocolResetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _ = PerformProtocolResetAsync();
        }

        private void firmwareInfoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _ = PerformFirmwareInfoAsync();
        }

        private void readEmbroideryFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _ = PerformReadEmbroideryFilesAsync();
        }

        private void switchTo19200BaudToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _ = SwitchTo19200BaudAsync();
        }

        private void switchTo57600BaudToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnSwitchBaud_Click(sender, e);
        }

        private void autoSwitchTo57600BaudToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _autoSwitchTo57600 = autoSwitchTo57600BaudToolStripMenuItem.Checked;
            SaveAutoSwitchSetting(_autoSwitchTo57600);
            
            if (_autoSwitchTo57600)
            {
                AppendOutput("Auto-switch to 57600 baud enabled");
            }
            else
            {
                AppendOutput("Auto-switch to 57600 baud disabled");
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void memoryViewerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_serialStack == null || !_isConnected)
            {
                MessageBox.Show("Not connected to machine.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Create and show a new memory viewer window
            MemoryWindow memoryWindow = new MemoryWindow(_serialStack);
            memoryWindow.Show();
        }

        private void imageViewerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_serialStack == null || !_isConnected)
            {
                MessageBox.Show("Not connected to machine.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Create and show the Image Viewer with the current serial stack connection
            ImageViewer imageViewer = new ImageViewer(_serialStack);
            imageViewer.Show();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "Bernina Serial Communication Tool\n\n" +
                "A high-level interface for communicating with Bernina machines over serial ports.\n\n" +
                "Features:\n" +
                "• Automatic baud rate detection\n" +
                "• Read and Large Read commands\n" +
                "• Write operations\n" +
                "• Sum command support (memory checksum)\n" +
                "• Baud rate switching to 57600\n" +
                "• Memory Viewer for monitoring multiple memory regions\n\n" +
                "Built with SerialStack.cs",
                "About",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void clearOutputToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtOutput.Clear();
        }

        private void showSerialTrafficToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _showSerialTraffic = showSerialTrafficToolStripMenuItem.Checked;

            if (_showSerialTraffic)
            {
                AppendOutput("--- Serial traffic debug mode ENABLED ---");
                AppendOutput("All bytes sent/received will be displayed in ASCII and HEX");
                AppendOutput("");
            }
            else
            {
                AppendOutput("--- Serial traffic debug mode DISABLED ---");
                AppendOutput("");
            }
        }

        private void showDebugToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _showDebug = showDebugToolStripMenuItem.Checked;

            if (_showDebug)
            {
                AppendOutput("--- Debug mode ENABLED ---");
                AppendOutput("SerialStack debug messages will be displayed");
                AppendOutput("");
            }
            else
            {
                AppendOutput("--- Debug mode DISABLED ---");
                AppendOutput("");
            }
        }

        // Event handlers
        private void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnConnectionStateChanged(sender, e)));
                return;
            }

            AppendOutput($"Connection state: {e.NewState} - {e.Message}");
        }

        private void OnCommandCompleted(object? sender, CommandCompletedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnCommandCompleted(sender, e)));
                return;
            }

            // Already handled in individual command methods
        }

        private void OnSerialTraffic(object? sender, SerialTrafficEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnSerialTraffic(sender, e)));
                return;
            }

            // Display serial traffic if debug mode is enabled
            string direction = e.IsSent ? "TX" : "RX";
            AppendSerialTraffic(direction, e.Data);
        }

        private void OnDebugMessage(object? sender, DebugMessageEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnDebugMessage(sender, e)));
                return;
            }

            // Display debug message if debug mode is enabled
            if (_showDebug)
            {
                txtOutput.AppendText($"[{e.Timestamp:HH:mm:ss.fff}] [DEBUG] {e.Message}\r\n");
            }
        }

        // Helper methods
        private void UpdateStatus(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateStatus(message)));
                return;
            }

            toolStripStatusLabel.Text = message;
        }

        private void UpdateConnectionStatus(string message, bool connected)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateConnectionStatus(message, connected)));
                return;
            }

            toolStripStatusLabelConnection.Text = message;

            if (connected)
            {
                toolStripStatusLabelConnection.ForeColor = Color.Black;
            }
            else
            {
                toolStripStatusLabelConnection.ForeColor = Color.Red;
            }
        }

        private void AppendOutput(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => AppendOutput(message)));
                return;
            }

            txtOutput.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
        }

        private void UpdateBaudRateMenuItems()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateBaudRateMenuItems()));
                return;
            }

            if (_serialStack == null || !_isConnected)
            {
                // Not connected - disable both menu items
                switchTo19200BaudToolStripMenuItem.Enabled = false;
                switchTo57600BaudToolStripMenuItem.Enabled = false;
            }
            else
            {
                // Connected - enable/disable based on current baud rate
                int currentBaudRate = _serialStack.BaudRate;
                
                // Enable "Switch to 19200" only if NOT already at 19200
                switchTo19200BaudToolStripMenuItem.Enabled = (currentBaudRate != 19200);
                
                // Enable "Switch to 57600" only if NOT already at 57600
                switchTo57600BaudToolStripMenuItem.Enabled = (currentBaudRate != 57600);
            }
        }

        private void AppendSerialTraffic(string direction, byte[] data)
        {
            if (!_showSerialTraffic || data == null || data.Length == 0)
            {
                return;
            }

            if (InvokeRequired)
            {
                Invoke(new Action(() => AppendSerialTraffic(direction, data)));
                return;
            }

            // Build ASCII representation
            StringBuilder ascii = new StringBuilder();
            foreach (byte b in data)
            {
                if (b >= 32 && b <= 126)
                    ascii.Append((char)b);
                else if (b == 0x0D)
                    ascii.Append("\\r");
                else if (b == 0x0A)
                    ascii.Append("\\n");
                else if (b == 0x09)
                    ascii.Append("\\t");
                else
                    ascii.Append($"[{b:X2}]");
            }

            // Build HEX representation
            StringBuilder hex = new StringBuilder();
            foreach (byte b in data)
            {
                hex.Append($"{b:X2} ");
            }

            // Output to display
            txtOutput.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {direction}: {ascii}\r\n");
            txtOutput.AppendText($"{"".PadLeft(22)}HEX: {hex}\r\n");
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_serialStack != null)
            {
                _serialStack.Close();
                _serialStack.Dispose();
                _serialStack = null;
            }
        }

    }
}
