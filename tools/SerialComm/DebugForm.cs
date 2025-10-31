using System;
using System.Text;
using System.Windows.Forms;
using Bernina.SerialStack;

namespace EmbroideryCommunicator
{
    public partial class DebugForm : Form
    {
        private SerialStack? _serialStack;
        private bool _showSerialTraffic = false;
        private bool _showDebug = false;

        public DebugForm(SerialStack serialStack)
        {
            InitializeComponent();
            _serialStack = serialStack;
            
            if (_serialStack != null)
            {
                _serialStack.SerialTraffic += OnSerialTraffic;
                _serialStack.DebugMessage += OnDebugMessage;
                _serialStack.CommandCompleted += OnCommandCompleted;
            }
            
            // Disable commands initially until connected
            UpdateCommandMenuItems();
        }

        private void UpdateCommandMenuItems()
        {
            bool isConnected = _serialStack != null && _serialStack.IsConnected;
            
            readToolStripMenuItem.Enabled = isConnected;
            largeReadToolStripMenuItem.Enabled = isConnected;
            writeToolStripMenuItem.Enabled = isConnected;
            sumToolStripMenuItem.Enabled = isConnected;
            sessionStartToolStripMenuItem.Enabled = isConnected;
            sessionEndToolStripMenuItem.Enabled = isConnected;
            protocolResetToolStripMenuItem.Enabled = isConnected;
            firmwareInfoToolStripMenuItem.Enabled = isConnected;
            readEmbroideryFilesToolStripMenuItem.Enabled = isConnected;
            
            btnRead.Enabled = isConnected;
            btnLargeRead.Enabled = isConnected;
            btnWrite.Enabled = isConnected;
        }

        public void UpdateSerialStack(SerialStack? serialStack)
        {
            _serialStack = serialStack;
            UpdateCommandMenuItems();
        }

        public void UpdateConnectionStatus(bool isConnected)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateConnectionStatus(isConnected)));
                return;
            }

            UpdateCommandMenuItems();
            
            if (isConnected)
            {
                toolStripStatusLabel.Text = "Connected - Ready";
                toolStripStatusLabel.ForeColor = Color.Black;
            }
            else
            {
                toolStripStatusLabel.Text = "Disconnected";
                toolStripStatusLabel.ForeColor = Color.Red;
            }
        }

        // Button handlers
        private async void btnRead_Click(object sender, EventArgs e)
        {
            await PerformReadAsync();
        }

        private async void btnLargeRead_Click(object sender, EventArgs e)
        {
            await PerformLargeReadAsync();
        }

        private async void btnWrite_Click(object sender, EventArgs e)
        {
            await PerformWriteAsync();
        }

        // Menu handlers
        private void readToolStripMenuItem_Click(object sender, EventArgs e) => btnRead_Click(sender, e);
        private void largeReadToolStripMenuItem_Click(object sender, EventArgs e) => btnLargeRead_Click(sender, e);
        private void writeToolStripMenuItem_Click(object sender, EventArgs e) => btnWrite_Click(sender, e);

        private void sumToolStripMenuItem_Click(object sender, EventArgs e) => _ = PerformSumAsync();
        private void sessionStartToolStripMenuItem_Click(object sender, EventArgs e) => _ = PerformSessionStartAsync();
        private void sessionEndToolStripMenuItem_Click(object sender, EventArgs e) => _ = PerformSessionEndAsync();
        private void protocolResetToolStripMenuItem_Click(object sender, EventArgs e) => _ = PerformProtocolResetAsync();
        private void firmwareInfoToolStripMenuItem_Click(object sender, EventArgs e) => _ = PerformFirmwareInfoAsync();
        private void readEmbroideryFilesToolStripMenuItem_Click(object sender, EventArgs e) => _ = PerformReadEmbroideryFilesAsync();

        private void clearOutputToolStripMenuItem_Click(object sender, EventArgs e) => txtOutput.Clear();

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

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void DebugForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_serialStack != null)
            {
                _serialStack.SerialTraffic -= OnSerialTraffic;
                _serialStack.DebugMessage -= OnDebugMessage;
                _serialStack.CommandCompleted -= OnCommandCompleted;
            }
        }

        // Command implementations
        private async Task PerformReadAsync()
        {
            if (_serialStack == null || !_serialStack.IsConnected)
            {
                MessageBox.Show("Not connected to machine.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string addressStr = txtReadAddress.Text.Trim();
            if (string.IsNullOrWhiteSpace(addressStr))
            {
                MessageBox.Show("Please enter a read address.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                        StringBuilder ascii = new StringBuilder();
                        foreach (byte b in result.BinaryData)
                        {
                            if (b >= 32 && b <= 126)
                                ascii.Append((char)b);
                            else
                                ascii.Append('.');
                        }
                        AppendOutput($"  ASCII: {ascii}");

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
                    MessageBox.Show($"Read failed: {result.ErrorMessage}", "Command Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                AppendOutput("");
            }
            catch (Exception ex)
            {
                AppendOutput($"Read error: {ex.Message}");
                UpdateStatus("Error");
                MessageBox.Show($"Invalid address format. Use hex format (e.g., 200100).", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async Task PerformLargeReadAsync()
        {
            if (_serialStack == null || !_serialStack.IsConnected)
            {
                MessageBox.Show("Not connected to machine.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string addressStr = txtReadAddress.Text.Trim();
            if (string.IsNullOrWhiteSpace(addressStr))
            {
                MessageBox.Show("Please enter an address.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                        StringBuilder ascii = new StringBuilder();
                        foreach (byte b in result.BinaryData)
                        {
                            if (b >= 32 && b <= 126)
                                ascii.Append((char)b);
                            else
                                ascii.Append('.');
                        }
                        AppendOutput($"  ASCII: {ascii}");

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
                    MessageBox.Show($"Large Read failed: {result.ErrorMessage}", "Command Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                AppendOutput("");
            }
            catch (Exception ex)
            {
                AppendOutput($"Large Read error: {ex.Message}");
                UpdateStatus("Error");
                MessageBox.Show($"Invalid address format. Use hex format (e.g., 0240F5).", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async Task PerformWriteAsync()
        {
            if (_serialStack == null || !_serialStack.IsConnected)
            {
                MessageBox.Show("Not connected to machine.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string addressStr = txtWriteAddress.Text.Trim();
            string dataStr = txtWriteData.Text.Trim().Replace(" ", "");

            if (string.IsNullOrWhiteSpace(addressStr))
            {
                MessageBox.Show("Please enter a write address.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(dataStr))
            {
                MessageBox.Show("Please enter data to write.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                int address = Convert.ToInt32(addressStr, 16);

                if (dataStr.Length % 2 != 0)
                {
                    MessageBox.Show("Data must be valid hex (even number of characters).", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                    MessageBox.Show($"Write failed: {result.ErrorMessage}", "Command Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                AppendOutput("");
            }
            catch (Exception ex)
            {
                AppendOutput($"Write error: {ex.Message}");
                UpdateStatus("Error");
                MessageBox.Show($"Invalid input format. Use hex format (e.g., Address: 200100, Data: 01).", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async Task PerformSumAsync()
        {
            if (_serialStack == null || !_serialStack.IsConnected)
            {
                MessageBox.Show("Not connected to machine.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

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
                        MessageBox.Show($"Sum Command failed: {result.ErrorMessage}", "Command Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    AppendOutput("");
                }
            }
        }

        private async Task PerformSessionStartAsync()
        {
            if (_serialStack == null || !_serialStack.IsConnected)
            {
                MessageBox.Show("Not connected to machine.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var sessionMode = await _serialStack.GetCurrentSessionModeAsync();
            
            if (sessionMode == null)
            {
                MessageBox.Show("Failed to detect session mode.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (sessionMode != SessionMode.SewingMachine)
            {
                AppendOutput("Session Start: Already in Embroidery Module mode (session already open)");
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
                MessageBox.Show($"Session Start failed: {result.ErrorMessage}", "Command Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            AppendOutput("");
        }

        private async Task PerformSessionEndAsync()
        {
            if (_serialStack == null || !_serialStack.IsConnected)
            {
                MessageBox.Show("Not connected to machine.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                MessageBox.Show($"Session End failed: {result.ErrorMessage}", "Command Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            AppendOutput("");
        }

        private async Task PerformProtocolResetAsync()
        {
            if (_serialStack == null || !_serialStack.IsConnected)
            {
                MessageBox.Show("Not connected to machine.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                MessageBox.Show($"Protocol Reset failed: {result.ErrorMessage}", "Command Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            AppendOutput("");
        }

        private async Task PerformFirmwareInfoAsync()
        {
            if (_serialStack == null || !_serialStack.IsConnected)
            {
                MessageBox.Show("Not connected to machine.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            UpdateStatus("Reading firmware information...");
            AppendOutput("Firmware Info: Detecting mode and reading from address 0x200100");

            var firmwareInfo = await _serialStack.ReadFirmwareInfoAsync();

            if (firmwareInfo != null)
            {
                string modeString = firmwareInfo.Mode == SessionMode.SewingMachine 
                    ? "Sewing Machine" 
                    : "Embroidery Module";
                
                AppendOutput("Firmware Info successful:");
                AppendOutput($"  Mode: {modeString}");
                AppendOutput($"  Version: {firmwareInfo.Version}");
                
                if (firmwareInfo.Language != null)
                {
                    AppendOutput($"  Language: {firmwareInfo.Language}");
                }
                
                AppendOutput($"  Manufacturer: {firmwareInfo.Manufacturer}");
                AppendOutput($"  Date: {firmwareInfo.Date}");
                
                if (firmwareInfo.Mode == SessionMode.EmbroideryModule)
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
                MessageBox.Show("Failed to read firmware information from the machine.", "Command Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            AppendOutput("");
        }

        private async Task PerformReadEmbroideryFilesAsync()
        {
            if (_serialStack == null || !_serialStack.IsConnected)
            {
                MessageBox.Show("Not connected to machine.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            UpdateStatus("Reading embroidery files from internal memory...");
            AppendOutput("Read Embroidery Files: Reading from Embroidery Module Memory");

            var files = await _serialStack.ReadEmbroideryFilesAsync(StorageLocation.EmbroideryModuleMemory, true);

            if (files != null)
            {
                AppendOutput($"Read Embroidery Files successful: Found {files.Count} file(s)");
                AppendOutput("");

                if (files.Count > 0)
                {
                    AppendOutput("File List:");
                    AppendOutput("  ID  | Attributes | File Name");
                    AppendOutput("  ----|------------|----------------------------------");

                    foreach (var file in files)
                    {
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
                MessageBox.Show("Failed to read embroidery files from the machine.", "Command Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            AppendOutput("");
        }

        private string DecodeFileAttributes(byte attributes)
        {
            bool isReadonly = (attributes & 0x80) != 0;
            bool isTwoBlock = (attributes & 0x08) != 0;
            bool isAlphabet = (attributes & 0x04) != 0;
            
            if (isReadonly && isTwoBlock && isAlphabet)
                return "R2A";
            else if (isReadonly && !isTwoBlock)
                return "RO";
            else if (!isReadonly)
                return "RW";
            else
                return "??";
        }

        private void UpdateStatus(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateStatus(message)));
                return;
            }

            toolStripStatusLabel.Text = message;
        }

        public void AppendOutput(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => AppendOutput(message)));
                return;
            }

            txtOutput.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
        }

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
            // Already handled in individual command methods
        }

        private void OnSerialTraffic(object? sender, SerialTrafficEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnSerialTraffic(sender, e)));
                return;
            }

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

            if (_showDebug)
            {
                txtOutput.AppendText($"[{e.Timestamp:HH:mm:ss.fff}] [DEBUG] {e.Message}\r\n");
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

            StringBuilder hex = new StringBuilder();
            foreach (byte b in data)
            {
                hex.Append($"{b:X2} ");
            }

            txtOutput.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {direction}: {ascii}\r\n");
            txtOutput.AppendText($"{"".PadLeft(22)}HEX: {hex}\r\n");
        }
    }
}
