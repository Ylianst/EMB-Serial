using System.Text;
using Bernina.SerialStack;

namespace SerialComm
{
    public partial class MemoryWindow : Form
    {
        private SerialStack? _serialStack;
        private int _currentAddress;
        private int _currentLength;
        private byte[]? _memoryData;
        private System.Threading.Timer? _refreshTimer;

        public MemoryWindow(SerialStack serialStack)
        {
            InitializeComponent();
            _serialStack = serialStack;
        }

        private void MemoryWindow_Load(object sender, EventArgs e)
        {
            // Set default values
            txtAddress.Text = "200100";
            txtLength.Text = "256";
            
            // Set up hex viewer with green on black theme (matching main window)
            txtHexView.Font = new Font("Consolas", 9);
            txtHexView.BackColor = Color.Black;
            txtHexView.ForeColor = Color.Lime;
            txtHexView.ReadOnly = true;
            txtHexView.WordWrap = false;
        }

        private async void btnLoad_Click(object sender, EventArgs e)
        {
            await LoadMemoryAsync();
        }

        private async Task LoadMemoryAsync()
        {
            if (_serialStack == null || !_serialStack.IsConnected)
            {
                MessageBox.Show("Not connected to machine.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Parse address
            if (!int.TryParse(txtAddress.Text.Trim(), System.Globalization.NumberStyles.HexNumber, 
                null, out int address))
            {
                MessageBox.Show("Invalid address. Use hex format (e.g., 200100).", "Input Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Parse length
            if (!int.TryParse(txtLength.Text.Trim(), out int length) || length <= 0 || length > 65536)
            {
                MessageBox.Show("Invalid length. Enter a value between 1 and 65536.", "Input Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _currentAddress = address;
            _currentLength = length;

            // Update window title
            this.Text = $"Memory Viewer - 0x{address:X6} ({length} bytes)";

            // Disable controls during load
            btnLoad.Enabled = false;
            txtAddress.Enabled = false;
            txtLength.Enabled = false;
            progressBar.Value = 0;
            progressBar.Visible = true;

            try
            {
                // Read memory block
                var result = await _serialStack.ReadMemoryBlockAsync(address, length, (current, total) =>
                {
                    // Update progress on UI thread
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() => UpdateProgress(current, total)));
                    }
                    else
                    {
                        UpdateProgress(current, total);
                    }
                });

                if (result.Success && result.BinaryData != null)
                {
                    _memoryData = result.BinaryData;
                    DisplayMemory();
                    lblStatus.Text = $"Loaded {length} bytes from 0x{address:X6}";
                }
                else
                {
                    MessageBox.Show($"Failed to read memory: {result.ErrorMessage}", "Read Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    lblStatus.Text = "Load failed";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading memory: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Error";
            }
            finally
            {
                // Re-enable controls
                btnLoad.Enabled = true;
                txtAddress.Enabled = true;
                txtLength.Enabled = true;
                progressBar.Visible = false;
                progressBar.Value = 0;
            }
        }

        private void UpdateProgress(int current, int total)
        {
            if (total > 0)
            {
                int percentage = (int)((double)current / total * 100);
                progressBar.Value = Math.Min(percentage, 100);
                lblStatus.Text = $"Loading... {current}/{total} bytes ({percentage}%)";
            }
        }

        private void DisplayMemory()
        {
            if (_memoryData == null)
            {
                return;
            }

            StringBuilder sb = new StringBuilder();
            
            // Header
            sb.AppendLine("Address   00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F  ASCII");
            sb.AppendLine("--------  -----------------------------------------------  ----------------");

            // Data rows
            for (int i = 0; i < _memoryData.Length; i += 16)
            {
                // Address
                sb.AppendFormat("{0:X8}  ", _currentAddress + i);

                // Hex bytes
                StringBuilder asciiLine = new StringBuilder();
                for (int j = 0; j < 16; j++)
                {
                    if (i + j < _memoryData.Length)
                    {
                        byte b = _memoryData[i + j];
                        sb.AppendFormat("{0:X2} ", b);
                        
                        // ASCII representation
                        if (b >= 32 && b <= 126)
                            asciiLine.Append((char)b);
                        else
                            asciiLine.Append('.');
                    }
                    else
                    {
                        sb.Append("   ");
                        asciiLine.Append(' ');
                    }
                }

                sb.Append(" ");
                sb.AppendLine(asciiLine.ToString());
            }

            txtHexView.Text = sb.ToString();
        }

        // Menu item handlers
        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_memoryData != null)
            {
                _ = LoadMemoryAsync();
            }
            else
            {
                MessageBox.Show("Load memory first before refreshing.", "Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_memoryData == null)
            {
                MessageBox.Show("No memory data to export.", "Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Binary Files (*.bin)|*.bin|All Files (*.*)|*.*";
                sfd.FileName = $"memory_{_currentAddress:X6}_{_currentLength}.bin";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        File.WriteAllBytes(sfd.FileName, _memoryData);
                        MessageBox.Show($"Memory data exported to {sfd.FileName}", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to export data: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void MemoryWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Stop auto-refresh timer
            _refreshTimer?.Dispose();
            _refreshTimer = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _refreshTimer?.Dispose();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
