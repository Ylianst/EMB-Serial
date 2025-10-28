using System.Drawing;
using System.Drawing.Imaging;

namespace SerialComm
{
    public partial class ImageViewer : Form
    {
        private Bernina.SerialStack.SerialStack? _serialStack;
        private const int ImageWidth = 72;
        private const int ImageHeight = 64;

        public ImageViewer(Bernina.SerialStack.SerialStack serialStack)
        {
            InitializeComponent();
            _serialStack = serialStack;
        }

        private async void btnLoad_Click(object sender, EventArgs e)
        {
            await LoadPreviewImage();
        }

        private async Task LoadPreviewImage()
        {
            try
            {
                // Validate FileId input
                if (!int.TryParse(txtFileId.Text, out int fileId))
                {
                    MessageBox.Show("Please enter a valid File ID number.", "Input Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Validate serial stack is connected
                if (_serialStack == null || !_serialStack.IsConnected)
                {
                    MessageBox.Show("Not connected to machine. Please connect first.", "Connection Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Get storage location from dropdown
                string selectedLocation = cmbStorageLocation.SelectedItem?.ToString() ?? "Memory";
                Bernina.SerialStack.StorageLocation location = selectedLocation == "PC Card"
                    ? Bernina.SerialStack.StorageLocation.PCCard
                    : Bernina.SerialStack.StorageLocation.EmbroideryModuleMemory;

                // Show loading status
                btnLoad.Enabled = false;
                lblStatus.Text = $"Loading preview for File ID {fileId} from {selectedLocation}...";

                // Call the async method to read the preview
                byte[]? previewData = await _serialStack.ReadEmbroideryFilePreviewAsync(location, fileId);

                if (previewData == null)
                {
                    MessageBox.Show("Failed to read preview image. Please check the File ID and try again.", "Load Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    lblStatus.Text = "Error loading preview";
                    return;
                }

                if (previewData.Length != 0x22E)
                {
                    MessageBox.Show($"Invalid preview data size: {previewData.Length} bytes (expected 558 bytes).", "Data Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    lblStatus.Text = "Error: Invalid preview data size";
                    return;
                }

                // Create and display the bitmap from preview data
                Bitmap bitmap = CreateBitmapFromBits(previewData, ImageWidth, ImageHeight);
                pictureBox.Image?.Dispose();
                pictureBox.Image = bitmap;

                lblStatus.Text = $"Preview loaded: {ImageWidth}x{ImageHeight} pixels ({previewData.Length} bytes)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading preview: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Error";
            }
            finally
            {
                btnLoad.Enabled = true;
            }
        }

        private Bitmap CreateBitmapFromBits(byte[] data, int width, int height)
        {
            // Create a bitmap with the specified dimensions
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            // Initialize entire bitmap to white
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);
            }

            int bitIndex = 0;
            int totalBits = data.Length * 8; // Use 8 bits per byte

            // Only set black pixels where needed
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (bitIndex < totalBits)
                    {
                        // Get the byte and bit position
                        int byteIndex = bitIndex / 8; // 8 bits per byte
                        int bitPosition = 7 - (bitIndex % 8); // Use bits 7-0

                        // Extract the bit value
                        bool bitValue = (data[byteIndex] & (1 << bitPosition)) != 0;

                        // Only set pixel if it should be black (bit value is 1)
                        if (bitValue)
                        {
                            bitmap.SetPixel(x, y, Color.Black);
                        }

                        bitIndex++;
                    }
                    else
                    {
                        // No more bits to process, remaining pixels are already white
                        break;
                    }
                }
                
                // If we've processed all bits, no need to continue with remaining rows
                if (bitIndex >= totalBits)
                    break;
            }

            return bitmap;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                pictureBox.Image?.Dispose();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
