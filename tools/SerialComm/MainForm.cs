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
        private const string RegistryKeyPath = @"Software\BerninaSerialComm";
        private const string ComPortValueName = "LastComPort";

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Populate COM port list
            RefreshComPorts();

            // Set default read address
            txtReadAddress.Text = "200100";

            // Load last used COM port from registry
            LoadLastComPort();

            // Initialize status
            UpdateConnectionStatus("Disconnected", false);
        }

        private void RefreshComPorts()
        {
            comboBoxComPort.Items.Clear();
            string[] ports = SerialPort.GetPortNames();

            if (ports.Length > 0)
            {
                foreach (string port in ports)
                {
                    comboBoxComPort.Items.Add(port);
                }
                comboBoxComPort.SelectedIndex = 0;
            }
            else
            {
                comboBoxComPort.Items.Add("No ports available");
                comboBoxComPort.SelectedIndex = 0;
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
                            // Try to select the last used port if it exists in the list
                            int index = comboBoxComPort.Items.IndexOf(lastPort);
                            if (index >= 0)
                            {
                                comboBoxComPort.SelectedIndex = index;
                            }
                        }
                    }
                }
            }
            catch
            {
                // If registry read fails, just use default selection
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
            await ConnectAsync();
        }

        private async Task ConnectAsync()
        {
            if (comboBoxComPort.SelectedItem == null ||
                comboBoxComPort.SelectedItem.ToString() == "No ports available")
            {
                MessageBox.Show("Please select a valid COM port.", "Connection Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string portName = comboBoxComPort.SelectedItem.ToString()!;

            // Disable connection controls
            btnConnect.Enabled = false;
            comboBoxComPort.Enabled = false;
            connectToolStripMenuItem.Enabled = false;

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

                // Try to connect
                bool connected = await _serialStack.OpenAsync();

                if (connected)
                {
                    _isConnected = true;

                    // Save the COM port to registry for next time
                    SaveLastComPort(portName);

                    // Update UI
                    btnConnect.Enabled = false;
                    btnDisconnect.Enabled = true;
                    comboBoxComPort.Enabled = false;

                    // Update menu items
                    connectToolStripMenuItem.Enabled = false;
                    disconnectToolStripMenuItem.Enabled = true;
                    readToolStripMenuItem.Enabled = true;
                    largeReadToolStripMenuItem.Enabled = true;
                    writeToolStripMenuItem.Enabled = true;
                    loadToolStripMenuItem.Enabled = true;
                    memoryViewerToolStripMenuItem.Enabled = true;

                    UpdateStatus("Connected");
                    UpdateConnectionStatus($"Connected: {portName} @ {_serialStack.BaudRate} baud", true);
                    UpdateBaudRateMenuItems();
                    AppendOutput($"Connected successfully at {_serialStack.BaudRate} baud");
                    AppendOutput("Ready to send commands");
                    AppendOutput("");
                }
                else
                {
                    _serialStack?.Dispose();
                    _serialStack = null;

                    btnConnect.Enabled = true;
                    comboBoxComPort.Enabled = true;
                    connectToolStripMenuItem.Enabled = true;

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

                btnConnect.Enabled = true;
                comboBoxComPort.Enabled = true;
                connectToolStripMenuItem.Enabled = true;

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
            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
            comboBoxComPort.Enabled = true;

            // Update menu items
            connectToolStripMenuItem.Enabled = true;
            disconnectToolStripMenuItem.Enabled = false;
            readToolStripMenuItem.Enabled = false;
            largeReadToolStripMenuItem.Enabled = false;
            writeToolStripMenuItem.Enabled = false;
            loadToolStripMenuItem.Enabled = false;
            memoryViewerToolStripMenuItem.Enabled = false;
            
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

        private async void btnLoad_Click(object sender, EventArgs e)
        {
            await PerformLoadAsync();
        }

        private async Task PerformLoadAsync()
        {
            if (_serialStack == null || !_isConnected)
            {
                MessageBox.Show("Not connected to machine.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string addressStr = txtLoadAddress.Text.Trim();
            string lengthStr = txtLoadLength.Text.Trim();

            if (string.IsNullOrWhiteSpace(addressStr) || addressStr.Length != 6)
            {
                MessageBox.Show("Load command requires a 6-character hex address (e.g., 0240D5).", "Input Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(lengthStr) || lengthStr.Length != 6)
            {
                MessageBox.Show("Load command requires a 6-character hex length (e.g., 000360).", "Input Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string parameters = addressStr + lengthStr;

                UpdateStatus($"Sending Load command...");
                AppendOutput($"Load Command: Address 0x{addressStr}, Length 0x{lengthStr}");

                var result = await _serialStack.LCommandAsync(parameters);

                if (result.Success)
                {
                    AppendOutput($"Load Command successful:");
                    AppendOutput($"  Response: {result.Response}");
                    UpdateStatus("Load Command complete");
                }
                else
                {
                    AppendOutput($"Load Command failed: {result.ErrorMessage}");
                    UpdateStatus("Load Command failed");
                    MessageBox.Show($"Load Command failed: {result.ErrorMessage}", "Command Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                AppendOutput("");
            }
            catch (Exception ex)
            {
                AppendOutput($"Load Command error: {ex.Message}");
                UpdateStatus("Error");
                MessageBox.Show($"Error: {ex.Message}", "Command Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
                UpdateConnectionStatus($"Connected: {comboBoxComPort.SelectedItem} @ {_serialStack.BaudRate} baud", true);
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
                UpdateConnectionStatus($"Connected: {comboBoxComPort.SelectedItem} @ {_serialStack.BaudRate} baud", true);
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

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnLoad_Click(sender, e);
        }

        private void switchTo19200BaudToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _ = SwitchTo19200BaudAsync();
        }

        private void switchTo57600BaudToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnSwitchBaud_Click(sender, e);
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

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "Bernina Serial Communication Tool\n\n" +
                "A high-level interface for communicating with Bernina machines over serial ports.\n\n" +
                "Features:\n" +
                "• Automatic baud rate detection\n" +
                "• Read and Large Read commands\n" +
                "• Write operations\n" +
                "• Load command support\n" +
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
            AppendOutput("Output cleared");
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
                toolStripStatusLabelConnection.ForeColor = Color.Green;
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
