using System;
using System.Drawing;
using System.Windows.Forms;

namespace EmbroideryCommunicator
{
    public partial class EmbroideryFileDetailsDialog : Form
    {
        private EmbroideryFile? _embroideryFile;

        public EmbroideryFileDetailsDialog(EmbroideryFile file)
        {
            InitializeComponent();
            _embroideryFile = file;
            SetupUI();
        }

        private void SetupUI()
        {
            if (_embroideryFile == null)
            {
                return;
            }

            // Update preview image (double size: 144x128 instead of 72x64)
            if (_embroideryFile.PreviewImageData != null && _embroideryFile.PreviewImageData.Length == 0x22E)
            {
                try
                {
                    Bitmap preview = ConvertPreviewDataToBitmap(_embroideryFile.PreviewImageData);
                    pbPreview.Image = new Bitmap(preview, new Size(144, 128));
                    preview.Dispose();
                }
                catch
                {
                    pbPreview.Text = "Error loading preview";
                }
            }
            else
            {
                pbPreview.Text = "No preview";
            }

            // Update details
            lblFileId.Text = $"File ID: {_embroideryFile.FileId}";
            lblFileName.Text = $"Name: {_embroideryFile.FileName}";

            // Read-only files cannot be deleted
            deleteButton.Enabled = ((_embroideryFile.FileAttributes & 0x20) == 0);

            // Build attribute details
            string attributeDetails = "File Attributes:\n";
            if ((_embroideryFile.FileAttributes & 0x20) != 0)
            {
                attributeDetails += "  • Read-Only\n";
            }
            if ((_embroideryFile.FileAttributes & 0x08) != 0)
            {
                attributeDetails += "  • Alphabet\n";
            }
            if ((_embroideryFile.FileAttributes & 0x02) != 0)
            {
                attributeDetails += "  • User File\n";
            }
            if (attributeDetails == "File Attributes:\n")
            {
                attributeDetails += "  • None";
            }

            lblAttributeDetails.Text = attributeDetails;
        }

        /// <summary>
        /// Converts 558 bytes (72x64 1-bit per pixel) preview data to a Bitmap
        /// Black pixels are rendered as opaque black, white pixels are transparent
        /// </summary>
        private Bitmap ConvertPreviewDataToBitmap(byte[] previewData)
        {
            if (previewData == null || previewData.Length != 0x22E)
            {
                throw new ArgumentException("Preview data must be exactly 558 bytes (72x64 1-bit per pixel)");
            }

            const int width = 72;
            const int height = 64;
            const int bytesPerRow = width / 8; // 9 bytes per row (72 pixels / 8)

            // Create bitmap with alpha channel support
            Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            for (int y = 0; y < height; y++)
            {
                int rowOffset = y * bytesPerRow;

                for (int x = 0; x < width; x++)
                {
                    int byteIndex = rowOffset + (x / 8);
                    int bitIndex = 7 - (x % 8);

                    if (byteIndex < previewData.Length)
                    {
                        byte bit = (byte)((previewData[byteIndex] >> bitIndex) & 1);
                        // Black pixels (bit=1) are opaque black, white pixels (bit=0) are transparent
                        Color pixelColor = bit == 1 ? Color.Black : Color.Transparent;
                        bitmap.SetPixel(x, y, pixelColor);
                    }
                }
            }

            return bitmap;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private async void downloadButton_Click(object sender, EventArgs e)
        {
            if (_embroideryFile == null)
            {
                MessageBox.Show("No file data available", "Download Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Find the parent MainForm by walking up the owner chain
            Form? parentForm = this.Owner;
            while (parentForm != null && !(parentForm is MainForm))
            {
                parentForm = parentForm.Owner;
            }

            if (parentForm is MainForm mainForm)
            {
                // Determine storage location - check if the dialog was opened from PC Card tab
                // We'll need to check the calling context, but for now default to EmbroideryModuleMemory
                StorageLocation location = StorageLocation.EmbroideryModuleMemory;

                // Check if this dialog has a Tag property that indicates the storage location
                // (This would need to be set when creating the dialog)
                if (this.Tag is StorageLocation tagLocation)
                {
                    location = tagLocation;
                }

                // Call the MainForm's download method
                await mainForm.DownloadEmbroideryFileAsync(_embroideryFile, location);
            }
            else
            {
                MessageBox.Show("Cannot access parent form", "Download Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void viewButton_Click(object sender, EventArgs e)
        {
            if (_embroideryFile == null)
            {
                MessageBox.Show("No file data available", "View Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Find the parent MainForm by walking up the owner chain
            Form? parentForm = this.Owner;
            while (parentForm != null && !(parentForm is MainForm))
            {
                parentForm = parentForm.Owner;
            }

            if (parentForm is MainForm mainForm)
            {
                // Determine storage location - check if the dialog was opened from PC Card tab
                StorageLocation location = StorageLocation.EmbroideryModuleMemory;

                // Check if this dialog has a Tag property that indicates the storage location
                // (This would need to be set when creating the dialog)
                if (this.Tag is StorageLocation tagLocation)
                {
                    location = tagLocation;
                }

                // Call the MainForm's view method
                await mainForm.ViewEmbroideryFileAsync(_embroideryFile, location);
            }
            else
            {
                MessageBox.Show("Cannot access parent form", "View Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void deleteButton_Click(object sender, EventArgs e)
        {
            if (_embroideryFile == null)
            {
                MessageBox.Show("No file data available", "Delete Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Find the parent MainForm by walking up the owner chain
            Form? parentForm = this.Owner;
            while (parentForm != null && !(parentForm is MainForm))
            {
                parentForm = parentForm.Owner;
            }

            if (parentForm is MainForm mainForm)
            {
                // Determine storage location - check if the dialog was opened from PC Card tab
                StorageLocation location = StorageLocation.EmbroideryModuleMemory;

                // Check if this dialog has a Tag property that indicates the storage location
                // (This would need to be set when creating the dialog)
                if (this.Tag is StorageLocation tagLocation)
                {
                    location = tagLocation;
                }

                // Call the MainForm's delete method
                await mainForm.DeleteEmbroideryFileAsync(_embroideryFile, location);

                // Close the dialog after deletion is complete (whether successful or not)
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Cannot access parent form", "Delete Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
