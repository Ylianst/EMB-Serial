using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Text.RegularExpressions;

namespace SerialComm
{
    public partial class ImageViewer : Form
    {
        private bool _use7Bits = true; // Toggle between 7-bit and 8-bit mode

        public ImageViewer()
        {
            InitializeComponent();
        }

        private void btnConvert_Click(object sender, EventArgs e)
        {
            ConvertAndDisplayImage();
        }

        private void trackBarWidth_Scroll(object sender, EventArgs e)
        {
            // Update the width display
            txtWidth.Text = trackBarWidth.Value.ToString();
            
            // Auto-convert if there's already HEX data
            if (!string.IsNullOrWhiteSpace(txtHexData.Text))
            {
                ConvertAndDisplayImage();
            }
        }

        private void ConvertAndDisplayImage()
        {
            try
            {
                // Validate inputs
                string hexData = txtHexData.Text.Trim();
                if (string.IsNullOrEmpty(hexData))
                {
                    MessageBox.Show("Please enter HEX data.", "Input Required",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int width = trackBarWidth.Value;

                // Parse HEX data into bytes
                byte[] data = ParseHexData(hexData);
                if (data == null || data.Length == 0)
                {
                    MessageBox.Show("Failed to parse HEX data. Please ensure it contains valid hexadecimal values.", "Parse Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Calculate height based on data length and width (7 bits per byte)
                int totalBits = data.Length * 8;
                int height = (int)Math.Ceiling((double)totalBits / width);

                // Limit height to 512 pixels to match max width
                if (height > 512)
                {
                    height = 512;
                }

                // Create the 1-bit per pixel image
                Bitmap bitmap = CreateBitmapFromBits(data, width, height);
                
                // Display the image
                pictureBox.Image?.Dispose();
                pictureBox.Image = bitmap;

                lblStatus.Text = $"Image created: {width}x{height} pixels from {data.Length} bytes (7 bits/byte)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating image: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Error";
            }
        }

        private byte[] ParseHexData(string hexData)
        {
            // Remove common separators and whitespace
            string cleaned = Regex.Replace(hexData, @"[\s,:\-]", "");

            // Check if it's valid hex
            if (!Regex.IsMatch(cleaned, @"^[0-9A-Fa-f]*$"))
            {
                return null;
            }

            // Ensure even length (each byte needs 2 hex chars)
            if (cleaned.Length % 2 != 0)
            {
                cleaned = "0" + cleaned; // Pad with leading zero
            }

            // Convert to bytes
            List<byte> bytes = new List<byte>();
            for (int i = 0; i < cleaned.Length; i += 2)
            {
                string byteStr = cleaned.Substring(i, 2);
                bytes.Add(Convert.ToByte(byteStr, 16));
            }

            return bytes.ToArray();
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

                        // Extract the bit value from the lower 7 bits
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
                if (bitIndex >= totalBits) break;
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
