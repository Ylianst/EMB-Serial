using Bernina.SerialStack;
using System.Drawing;

namespace SerialComm
{
    public partial class EmbroideryFileControl : UserControl
    {
        private EmbroideryFile? _embroideryFile;

        public EmbroideryFileControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Sets the embroidery file data and updates the UI
        /// </summary>
        public void SetEmbroideryFile(EmbroideryFile file)
        {
            _embroideryFile = file;
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (_embroideryFile == null)
            {
                return;
            }

            // Update attributes
            lockPictureBox.Visible = (_embroideryFile.FileAttributes & 0x20) != 0;
            alphabetPictureBox.Visible = (_embroideryFile.FileAttributes & 0x08) != 0;
            userPictureBox.Visible = (_embroideryFile.FileAttributes & 0x02) != 0;

            // Update filename label
            lblFileName.Text = _embroideryFile.FileName;

            // Update preview image
            if (_embroideryFile.PreviewImageData != null && _embroideryFile.PreviewImageData.Length == 0x22E)
            {
                try
                {
                    Bitmap preview = ConvertPreviewDataToBitmap(_embroideryFile.PreviewImageData);
                    pbPreview.Image = preview;
                }
                catch
                {
                    pbPreview.Image = null;
                    pbPreview.Text = "Error loading preview";
                }
            }
            else
            {
                pbPreview.Image = null;
                pbPreview.Text = "No preview";
            }
        }

        /// <summary>
        /// Converts 558 bytes (72x64 1-bit per pixel) preview data to a Bitmap
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

            Bitmap bitmap = new Bitmap(width, height);

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
                        Color pixelColor = bit == 1 ? Color.Black : Color.White;
                        bitmap.SetPixel(x, y, pixelColor);
                    }
                }
            }

            return bitmap;
        }
    }
}
