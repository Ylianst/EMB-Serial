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
            
            // Test CreateMainDataBlock and CreatePreviewDataBlock for validation
            TestDataBlockCreation(file);
        }
        
        private void TestDataBlockCreation(EmbroideryFile testFile)
        {
            try
            {
                testFile.FileData = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }; // 5 bytes of test data
                testFile.FileExtraData = new byte[] { 0xAA, 0xBB }; // 2 bytes of extra data
                
                // Create a SerialStack instance (just for calling the methods)
                var serialStack = new SerialStack("COM1"); // Dummy port name
                
                // Test CreateMainDataBlock
                System.Diagnostics.Debug.WriteLine("=== Testing CreateMainDataBlock ===");
                byte[] mainBlock = serialStack.CreateMainDataBlock(testFile);
                System.Diagnostics.Debug.WriteLine($"Main block size: {mainBlock.Length} bytes (expected: {176 + 5 + 2} = 183)");
                System.Diagnostics.Debug.WriteLine($"Main block hex: {BitConverter.ToString(mainBlock).Replace("-", " ")}");
                System.Diagnostics.Debug.WriteLine("");
                
                // Test CreatePreviewDataBlock
                System.Diagnostics.Debug.WriteLine("=== Testing CreatePreviewDataBlock ===");
                byte[] previewBlock = serialStack.CreatePreviewDataBlock(testFile);
                System.Diagnostics.Debug.WriteLine($"Preview block size: {previewBlock.Length} bytes (expected: {174 + 558} = 732)");
                System.Diagnostics.Debug.WriteLine($"Preview block hex (first 200 bytes): {BitConverter.ToString(previewBlock, 0, Math.Min(200, previewBlock.Length)).Replace("-", " ")}");
                System.Diagnostics.Debug.WriteLine($"Preview block hex (last 50 bytes): {BitConverter.ToString(previewBlock, Math.Max(0, previewBlock.Length - 50), Math.Min(50, previewBlock.Length)).Replace("-", " ")}");
                System.Diagnostics.Debug.WriteLine("");
                
                // Verify the structure of MainDataBlock
                System.Diagnostics.Debug.WriteLine("=== MainDataBlock Structure Verification ===");
                int offset = 0;
                
                // First 2 bytes: FileData.length * 5 (5 * 5 = 25 = 0x0019)
                int multipliedLength = (mainBlock[offset] << 8) | mainBlock[offset + 1];
                System.Diagnostics.Debug.WriteLine($"Bytes 0-1 (FileData.length * 5): 0x{multipliedLength:X4} = {multipliedLength} (expected: {testFile.FileData.Length * 5})");
                offset += 2;
                
                // Skip 166 null bytes
                offset += 166;
                
                // Next 4 bytes: FileData.length
                int fileDataLength = (mainBlock[offset] << 24) | (mainBlock[offset + 1] << 16) | (mainBlock[offset + 2] << 8) | mainBlock[offset + 3];
                System.Diagnostics.Debug.WriteLine($"Bytes 168-171 (FileData.length): 0x{fileDataLength:X8} = {fileDataLength} (expected: {testFile.FileData.Length})");
                offset += 4;
                
                // Next 4 bytes: FileExtraData.length
                int fileExtraDataLength = (mainBlock[offset] << 24) | (mainBlock[offset + 1] << 16) | (mainBlock[offset + 2] << 8) | mainBlock[offset + 3];
                System.Diagnostics.Debug.WriteLine($"Bytes 172-175 (FileExtraData.length): 0x{fileExtraDataLength:X8} = {fileExtraDataLength} (expected: {testFile.FileExtraData.Length})");
                offset += 4;
                
                System.Diagnostics.Debug.WriteLine($"FileData starts at offset {offset}: {BitConverter.ToString(mainBlock, offset, Math.Min(10, mainBlock.Length - offset)).Replace("-", " ")}");
                System.Diagnostics.Debug.WriteLine("");
                
                // Verify the structure of PreviewDataBlock
                System.Diagnostics.Debug.WriteLine("=== PreviewDataBlock Structure Verification ===");
                offset = 0;
                
                // First 5 bytes: 0x0000093EFF
                long headerValue = ((long)previewBlock[offset] << 32) | ((long)previewBlock[offset + 1] << 24) | 
                                   ((long)previewBlock[offset + 2] << 16) | ((long)previewBlock[offset + 3] << 8) | 
                                   previewBlock[offset + 4];
                System.Diagnostics.Debug.WriteLine($"Bytes 0-4 (header): 0x{headerValue:X10} (expected: 0x0000093EFF)");
                offset += 5;
                
                // Skip 169 null bytes
                offset += 169;
                
                System.Diagnostics.Debug.WriteLine($"PreviewImageData starts at offset {offset}: {BitConverter.ToString(previewBlock, offset, Math.Min(20, previewBlock.Length - offset)).Replace("-", " ")}");
                System.Diagnostics.Debug.WriteLine("");
                
                System.Diagnostics.Debug.WriteLine("=== Test Complete ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Test failed with exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
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
