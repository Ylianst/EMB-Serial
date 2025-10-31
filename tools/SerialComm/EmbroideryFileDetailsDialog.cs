using Bernina.SerialStack;
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
    }
}
