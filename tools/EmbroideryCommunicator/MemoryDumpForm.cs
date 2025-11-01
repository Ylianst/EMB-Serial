using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Bernina.SerialStack;

namespace EmbroideryCommunicator
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

            // Check if the device is busy with another operation
            if (_serialStack.IsBusy)
            {
                MessageBox.Show("The device is currently busy with another operation. Please wait for it to complete before starting a memory download.", "Device Busy",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            const int chunkSize = 256; // Read 256 bytes at a time

            string selectedMode = comboBoxMode.SelectedItem?.ToString() ?? "Sewing Machine";
            bool isEmbroideryModule = selectedMode == "Embroidery Module";
            string filePath = textBoxFilename.Text;

            try
            {
                // Prepare UI
                statusLabel.Text = "Starting download...";
                statusLabel.ForeColor = Color.Black;
                progressBar.Maximum = totalMemorySize / chunkSize;
                progressBar.Value = 0;

                // Determine session mode
                SessionMode targetMode = isEmbroideryModule ? SessionMode.EmbroideryModule : SessionMode.SewingMachine;

                // Call the SerialStack method with progress callback and cancellation token
                bool success = await _serialStack!.DownloadMemoryAsync(
                    targetMode,
                    filePath,
                    (currentAddress, totalSize) =>
                    {
                        try
                        {
                            // Update progress bar and status
                            int progress = currentAddress / chunkSize;
                            progressBar.Value = Math.Min(progress, progressBar.Maximum);

                            double percentComplete = (currentAddress * 100.0) / totalSize;
                            statusLabel.Text = $"{percentComplete:F1}% - 0x{currentAddress:X6}";

                            // Allow UI to update
                            Application.DoEvents();
                        }
                        catch (Exception) { }
                    },
                    cancellationToken
                );

                if (success)
                {
                    // Success
                    progressBar.Value = progressBar.Maximum;
                    statusLabel.Text = "Download complete!";
                    statusLabel.ForeColor = Color.Green;

                    MessageBox.Show($"Memory dump successfully saved to:\n{filePath}", "Download Complete",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    // Failed
                    statusLabel.Text = "Download failed";
                    statusLabel.ForeColor = Color.Red;
                    MessageBox.Show("Memory download failed. Check the connection and try again.", "Download Failed",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (OperationCanceledException)
            {
                statusLabel.Text = "Download cancelled";
                statusLabel.ForeColor = Color.Black;
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
