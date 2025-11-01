using Bernina.SerialStack;
using System.Drawing;

namespace EmbroideryCommunicator
{
    public partial class EmbroideryFileControl : UserControl
    {
        private EmbroideryFile? _embroideryFile;
        private bool _selected = false;

        // Static list to track all EmbroideryFileControl instances for selection management
        private static List<EmbroideryFileControl> _allControls = new();

        // Event raised when this control is selected
        public event EventHandler? SelectionChanged;

        public EmbroideryFileControl()
        {
            InitializeComponent();
            _allControls.Add(this);

            // Make the control focusable and selectable
            this.SetStyle(ControlStyles.Selectable, true);
            this.TabStop = true;

            // Wire up click events for selection
            this.Click += EmbroideryFileControl_Click;
            pbPreview.Click += EmbroideryFileControl_Click;
            lblFileName.Click += EmbroideryFileControl_Click;
            attributesPanel.Click += EmbroideryFileControl_Click;
            lockPictureBox.Click += EmbroideryFileControl_Click;
            alphabetPictureBox.Click += EmbroideryFileControl_Click;
            userPictureBox.Click += EmbroideryFileControl_Click;

            // Wire up mouse down events for right-click selection
            this.MouseDown += EmbroideryFileControl_MouseDown;
            pbPreview.MouseDown += EmbroideryFileControl_MouseDown;
            lblFileName.MouseDown += EmbroideryFileControl_MouseDown;
            attributesPanel.MouseDown += EmbroideryFileControl_MouseDown;
            lockPictureBox.MouseDown += EmbroideryFileControl_MouseDown;
            alphabetPictureBox.MouseDown += EmbroideryFileControl_MouseDown;
            userPictureBox.MouseDown += EmbroideryFileControl_MouseDown;

            this.DoubleClick += EmbroideryFileControl_DoubleClick;
            pbPreview.DoubleClick += EmbroideryFileControl_DoubleClick;
            lblFileName.DoubleClick += EmbroideryFileControl_DoubleClick;
            attributesPanel.DoubleClick += EmbroideryFileControl_DoubleClick;
            lockPictureBox.DoubleClick += EmbroideryFileControl_DoubleClick;
            alphabetPictureBox.DoubleClick += EmbroideryFileControl_DoubleClick;
            userPictureBox.DoubleClick += EmbroideryFileControl_DoubleClick;
        }

        /// <summary>
        /// Gets or sets whether this control is selected
        /// </summary>
        public bool Selected
        {
            get { return _selected; }
            set
            {
                if (_selected != value)
                {
                    _selected = value;
                    UpdateSelectionAppearance();
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Sets the embroidery file data and updates the UI
        /// </summary>
        public void SetEmbroideryFile(EmbroideryFile file)
        {
            _embroideryFile = file;
            UpdateUI();
        }

        private void EmbroideryFileControl_MouseDown(object? sender, MouseEventArgs e)
        {
            // On right-click, select the control before showing context menu
            if (e.Button == MouseButtons.Right)
            {
                // Deselect all other controls and select this one
                foreach (var control in _allControls)
                {
                    if (control != this && control.Selected)
                    {
                        control.Selected = false;
                    }
                }

                // Select this control and give it focus
                this.Selected = true;
                this.Focus();
            }
        }

        private void EmbroideryFileControl_Click(object? sender, EventArgs e)
        {
            this.Focus();
        }

        /*
        private void EmbroideryFileControl_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // Show context menu
                ContextMenuStrip contextMenu = new ContextMenuStrip();
                ToolStripMenuItem detailsItem = new ToolStripMenuItem("Details...", null, (s, args) => ShowDetails());
                contextMenu.Items.Add(detailsItem);
                contextMenu.Show(this, e.Location);
            }
        }
        */

        private void EmbroideryFileControl_DoubleClick(object? sender, EventArgs e)
        {
            // Show details on double-click
            ShowDetails();
        }

        private void ShowDetails()
        {
            if (_embroideryFile == null)
            {
                MessageBox.Show("No file data available", "Details", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Create and show the details dialog
            using (EmbroideryFileDetailsDialog detailsDialog = new EmbroideryFileDetailsDialog(_embroideryFile))
            {
                detailsDialog.ShowDialog();
            }
        }

        private void UpdateSelectionAppearance()
        {
            if (_selected)
            {
                // Light blue background when selected
                this.BackColor = Color.FromArgb(173, 216, 230); // Light blue
                pbPreview.BackColor = Color.FromArgb(173, 216, 230); // Light blue for preview too
            }
            else
            {
                // White background when not selected
                this.BackColor = Color.White;
                pbPreview.BackColor = Color.White;
            }
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

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            this.Selected = true;
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            this.Selected = false;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // Handle Enter or Space to show details
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                ShowDetails();
                e.Handled = true;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _allControls.Remove(this);
            }
            base.Dispose(disposing);
        }

        private void detailsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowDetails();
        }

        private void contextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            downloadToolStripMenuItem.Enabled = (_embroideryFile != null) && (_embroideryFile.FileAttributes & 0x08) == 0;
        }
        private async void downloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_embroideryFile == null)
            {
                MessageBox.Show("No file data available", "Download Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Find the parent MainForm to access the SerialStack
            Form? parentForm = this.FindForm();
            if (parentForm == null || parentForm.GetType().Name != "MainForm")
            {
                MessageBox.Show("Cannot access parent form", "Download Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Use reflection to access the private _serialStack field
            var serialStackField = parentForm.GetType().GetField("_serialStack", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (serialStackField == null)
            {
                MessageBox.Show("Cannot access serial stack", "Download Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SerialStack? serialStack = serialStackField.GetValue(parentForm) as SerialStack;
            
            if (serialStack == null || !serialStack.IsConnected)
            {
                MessageBox.Show("Not connected to machine", "Download Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Show Save As dialog with suggested filename
            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "Embroidery Files (*.exp)|*.exp|All Files (*.*)|*.*";
                saveDialog.DefaultExt = "exp";
                saveDialog.FileName = _embroideryFile.FileName + ".exp";
                saveDialog.Title = "Save Embroidery File";

                if (saveDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    // Determine storage location (check if this file control is in PC Card tab)
                    StorageLocation location = StorageLocation.EmbroideryModuleMemory;
                    
                    // Check if this control's parent hierarchy contains the PC Card flow layout panel
                    Control? parent = this.Parent;
                    while (parent != null)
                    {
                        if (parent.Name == "flowLayoutPanelPcCards")
                        {
                            location = StorageLocation.PCCard;
                            break;
                        }
                        parent = parent.Parent;
                    }

                    // Show progress (simplified - just disable the control)
                    this.Enabled = false;
                    this.Cursor = Cursors.WaitCursor;

                    // Read the file data from the machine
                    EmbroideryFile? downloadedFile = await serialStack.ReadEmbroideryFileAsync(
                        location,
                        _embroideryFile.FileId,
                        null  // Progress callback (optional)
                    );

                    if (downloadedFile == null || downloadedFile.FileData == null)
                    {
                        MessageBox.Show("Failed to read file from machine", "Download Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Save the file data to disk
                    File.WriteAllBytes(saveDialog.FileName, downloadedFile.FileData);

                    MessageBox.Show($"File saved successfully to:\n{saveDialog.FileName}", "Download Complete",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error downloading file: {ex.Message}", "Download Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    this.Enabled = true;
                    this.Cursor = Cursors.Default;
                }
            }
        }
    }
}
