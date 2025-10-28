using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Bernina.SerialStack;

namespace SerialComm
{
    public partial class MemoryDumpForm : Form
    {
        private SerialStack? _serialStack;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isDownloading = false;

        public MemoryDumpForm(SerialStack? serialStack)
        {
            InitializeComponent();
            _serialStack = serialStack;
        }

        private void MemoryDumpForm_Load(object sender, EventArgs e)
        {
            // Populate dropdown with options
            comboBoxMode.Items.Clear();
            comboBoxMode.Items.Add("Sewing Machine");
            comboBoxMode.Items.Add("Embroidery Module");
            comboBoxMode.SelectedIndex = 0;

            // Set default filename
            string deviceName = (comboBoxMode.SelectedIndex == 0) ? "SewingMachine" : "EmbroideryModule";
            textBoxFilename.Text = $"MemoryDump-{deviceName}-{DateTime.Now:yyyyMMdd}.bin";

            // Subscribe to connection state changes
            if (_serialStack != null)
            {
                _serialStack.ConnectionStateChanged += SerialStack_ConnectionStateChanged;
            }

            // Initial state
            UpdateUIState();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            // Unsubscribe from connection state changes
            if (_serialStack != null)
            {
                _serialStack.ConnectionStateChanged -= SerialStack_ConnectionStateChanged;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // Refresh UI state when form is shown (in case connection state changed while form was closed)
            UpdateUIState();
        }

        private void SerialStack_ConnectionStateChanged(object? sender, Bernina.SerialStack.ConnectionStateChangedEventArgs e)
        {
            // Update UI whenever connection state changes
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateUIState()));
            }
            else
            {
                UpdateUIState();
            }
        }

        private void comboBoxMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Update filename when mode changes (only the filename portion, not the path)
            string currentFullPath = textBoxFilename.Text;
            
            if (string.IsNullOrWhiteSpace(currentFullPath))
            {
                return;
            }

            string selectedMode = comboBoxMode.SelectedItem?.ToString() ?? "Sewing Machine";
            bool isEmbroideryModule = selectedMode == "Embroidery Module";
            
            // Extract directory and filename separately
            string directory = Path.GetDirectoryName(currentFullPath) ?? "";
            string filename = Path.GetFileName(currentFullPath);
            
            if (string.IsNullOrWhiteSpace(filename))
            {
                return;
            }
            
            string newFilename = filename;
            
            // Replace EmbroideryModule with SewingMachine if switching to Sewing Machine mode
            if (!isEmbroideryModule && filename.Contains("EmbroideryModule"))
            {
                newFilename = filename.Replace("EmbroideryModule", "SewingMachine");
            }
            
            // Replace SewingMachine with EmbroideryModule if switching to Embroidery Module mode
            if (isEmbroideryModule && filename.Contains("SewingMachine"))
            {
                newFilename = filename.Replace("SewingMachine", "EmbroideryModule");
            }
            
            // Reconstruct the full path with the new filename
            string newFullPath = string.IsNullOrEmpty(directory) 
                ? newFilename 
                : Path.Combine(directory, newFilename);
            
            textBoxFilename.Text = newFullPath;
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.DefaultExt = "bin";
                saveFileDialog.Filter = "Binary files (*.bin)|*.bin|All files (*.*)|*.*";

                // If filename is empty, suggest one based on the selected device and date
                if (string.IsNullOrWhiteSpace(textBoxFilename.Text))
                {
                    string selectedDevice = comboBoxMode.SelectedItem?.ToString() ?? "SewingMachine";
                    // Normalize device name for filename
                    string deviceName = selectedDevice == "Embroidery Module" ? "EmbroideryModule" : "SewingMachine";
                    string suggestedFilename = $"MemoryDump-{deviceName}-{DateTime.Now:yyyyMMdd}.bin";
                    saveFileDialog.FileName = suggestedFilename;
                }
                else
                {
                    saveFileDialog.FileName = textBoxFilename.Text;
                }

                saveFileDialog.Title = "Save Memory Dump As";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    textBoxFilename.Text = saveFileDialog.FileName;
                }
            }
        }

        private async void buttonStart_Click(object sender, EventArgs e)
        {
            if (_isDownloading)
            {
                return;
            }

            // Validate inputs
            if (string.IsNullOrWhiteSpace(textBoxFilename.Text))
            {
                MessageBox.Show("Please enter a filename or use Browse to select a location.", "Input Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_serialStack == null || !_serialStack.IsConnected)
            {
                MessageBox.Show("Not connected to machine.", "Connection Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _isDownloading = true;
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                UpdateUIState();
                await DownloadMemoryAsync(_cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                statusLabel.Text = "Download cancelled";
                statusLabel.ForeColor = Color.Black;
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Error: {ex.Message}";
                statusLabel.ForeColor = Color.Red;
                MessageBox.Show($"Error during download: {ex.Message}", "Download Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isDownloading = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                UpdateUIState();
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            if (_isDownloading)
            {
                _cancellationTokenSource?.Cancel();
                buttonCancel.Enabled = false;
            }
            else
            {
                Close();
            }
        }

        private async Task DownloadMemoryAsync(CancellationToken cancellationToken)
        {
            const int totalMemorySize = 0x1000000; // 16 MB (0x000000 to 0xFFFFFF)
            const int chunkSize = 256; // Read 256 bytes at a time using LargeRead

            string selectedMode = comboBoxMode.SelectedItem?.ToString() ?? "Sewing Machine";
            bool isEmbroideryModule = selectedMode == "Embroidery Module";
            string filePath = textBoxFilename.Text;

            try
            {
                // Step 1: Prepare the machine state
                statusLabel.Text = "Preparing machine...";
                statusLabel.ForeColor = Color.Black;

                var currentMode = await _serialStack!.GetCurrentSessionModeAsync();
                bool needsSessionStart = false;
                bool needsSessionEnd = false;

                if (isEmbroideryModule && currentMode == SessionMode.SewingMachine)
                {
                    statusLabel.Text = "Starting embroidery session...";
                    var sessionStartResult = await _serialStack.SessionStartAsync();
                    if (!sessionStartResult.Success)
                    {
                        throw new Exception($"Failed to start session: {sessionStartResult.ErrorMessage}");
                    }
                    needsSessionEnd = true;
                }
                else if (!isEmbroideryModule && currentMode == SessionMode.EmbroideryModule)
                {
                    statusLabel.Text = "Ending embroidery session...";
                    var sessionEndResult = await _serialStack.SessionEndAsync();
                    if (!sessionEndResult.Success)
                    {
                        throw new Exception($"Failed to end session: {sessionEndResult.ErrorMessage}");
                    }
                    needsSessionStart = true;
                }

                // Step 2: Create/overwrite the file
                statusLabel.Text = "Creating output file...";
                using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    progressBar.Maximum = totalMemorySize / chunkSize;
                    progressBar.Value = 0;

                    // Step 3: Download memory in chunks
                    for (int address = 0; address < totalMemorySize; address += chunkSize)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // Read chunk
                        var readResult = await _serialStack.LargeReadAsync(address);

                        if (!readResult.Success || readResult.BinaryData == null)
                        {
                            throw new Exception($"Failed to read memory at 0x{address:X6}: {readResult.ErrorMessage}");
                        }

                        // Write to file
                        fileStream.Write(readResult.BinaryData, 0, readResult.BinaryData.Length);

                        // Update progress with short, compact information
                        int progress = address / chunkSize;
                        progressBar.Value = Math.Min(progress, progressBar.Maximum);

                        double percentComplete = (address * 100.0) / totalMemorySize;
                        statusLabel.Text = $"{percentComplete:F1}% - 0x{address:X6}";

                        // Allow UI to update
                        Application.DoEvents();
                    }

                    fileStream.Close();
                }

                // Step 4: Recovery - restore session state if needed
                if (needsSessionEnd)
                {
                    statusLabel.Text = "Restoring session state...";
                    await _serialStack.SessionStartAsync();
                }
                else if (needsSessionStart)
                {
                    statusLabel.Text = "Restoring session state...";
                    await _serialStack.SessionEndAsync();
                }

                // Step 5: Success
                progressBar.Value = progressBar.Maximum;
                statusLabel.Text = "Download complete!";
                statusLabel.ForeColor = Color.Green;

                MessageBox.Show($"Memory dump successfully saved to:\n{filePath}", "Download Complete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                // Clean up partially written file
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
                catch { }

                throw;
            }
        }

        private void UpdateUIState()
        {
            // Check if we're connected
            bool isConnected = _serialStack != null && _serialStack.IsConnected;

            // Enable/disable controls based on download state and connection state
            comboBoxMode.Enabled = !_isDownloading && isConnected;
            textBoxFilename.Enabled = !_isDownloading;
            buttonBrowse.Enabled = !_isDownloading;
            buttonStart.Enabled = !_isDownloading && isConnected;
            buttonCancel.Text = _isDownloading ? "Cancel" : "Close";
            buttonCancel.Enabled = true; // Always allow close/cancel

            // Adjust colors
            if (_isDownloading)
            {
                comboBoxMode.BackColor = SystemColors.Control;
                textBoxFilename.BackColor = SystemColors.Control;
            }
            else
            {
                comboBoxMode.BackColor = SystemColors.Window;
                textBoxFilename.BackColor = SystemColors.Window;
            }
        }

    }
}
