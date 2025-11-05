using System.Drawing.Drawing2D;

namespace EmbroideryCommunicator
{
    public partial class EmbroideryViewerForm : Form
    {
        private EmbroideryPattern? _pattern;
        private float _zoomFactor = 1.0f;
        private PointF _panOffset = new PointF(0, 0);
        private Point _lastMousePos;
        private bool _isDragging = false;
        private int _maxStitchesToDisplay = int.MaxValue;
        private bool _showPreviewImage = false;
        private byte[]? _previewImageData = null;
        private bool _showJumps = true;
        private bool _showStitchLines = true;
        private bool _showStitchPoints = false;
        private float _baseScale = 1.0f; // Base scale to fit pattern in window
        private byte[]? _currentFileData = null; // Store current file data for saving
        private string? _currentFileName = null; // Store current filename

        // Colors to rotate through for different thread segments
        private readonly Color[] _threadColors = new Color[]
        {
            Color.Blue,           // Initial color
            Color.Red,            // After first color change
            Color.Green,          // After second color change
            Color.Purple,         // After third color change
            Color.Orange,         // After fourth color change
            Color.DarkCyan,       // After fifth color change
            Color.Magenta,        // After sixth color change
            Color.Brown,          // After seventh color change
            Color.DarkGreen,      // After eighth color change
            Color.Navy            // After ninth color change
        };
        private int _currentColorIndex = 0;

        public EmbroideryViewerForm()
        {
            InitializeComponent();

            // Enable double buffering on the render panel
            typeof(Panel).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
                null, renderPanel, new object[] { true });

            // Handle resize to repaint the panel
            this.Resize += EmbroideryViewerForm_Resize;
            renderPanel.Resize += (s, e) => renderPanel.Invalidate();

            // Handle trackbar value change
            trackBar.ValueChanged += TrackBar_ValueChanged;

            // Handle double-click on render panel to open file
            renderPanel.DoubleClick += renderPanel_DoubleClick;
        }

        private void renderPanel_DoubleClick(object? sender, EventArgs e)
        {
            // Only open file dialog if no pattern is loaded
            if (_pattern == null)
            {
                openToolStripMenuItem_Click(sender, e);
            }
        }

        private void EmbroideryViewerForm_Resize(object sender, EventArgs e)
        {
            // Force repaint when form is resized
            renderPanel.Invalidate();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "EXP Files (*.exp)|*.exp|All Files (*.*)|*.*";
                openFileDialog.Title = "Open Embroidery File";
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    LoadFile(openFileDialog.FileName);
                }
            }
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseFile();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_currentFileData == null || _currentFileData.Length == 0)
            {
                MessageBox.Show("No file data available to save", "Save Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "Embroidery Files (*.exp)|*.exp|All Files (*.*)|*.*";
                saveDialog.DefaultExt = "exp";
                saveDialog.FileName = _currentFileName ?? "embroidery.exp";
                saveDialog.Title = "Save Embroidery File As";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        File.WriteAllBytes(saveDialog.FileName, _currentFileData);
                        MessageBox.Show($"File saved successfully to:\n{saveDialog.FileName}",
                            "Save Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error saving file: {ex.Message}", "Save Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void LoadFile(string filePath)
        {
            try
            {
                // Validate file
                if (!ExpFileParser.IsValidExpFile(filePath))
                {
                    MessageBox.Show("The selected file does not appear to be a valid .EXP file.",
                        "Invalid File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Read and store file data
                _currentFileData = File.ReadAllBytes(filePath);
                _currentFileName = Path.GetFileName(filePath);

                // Parse the file
                _pattern = ExpFileParser.Parse(filePath);

                // Calculate base scale and reset view
                _baseScale = CalculateBaseScale();
                _zoomFactor = 1.0f;
                _panOffset = new PointF(0, 0);

                // Configure trackbar for this pattern
                if (_pattern.Stitches.Count > 0)
                {
                    trackBar.Minimum = 0;
                    trackBar.Maximum = _pattern.Stitches.Count;
                    trackBar.Value = _pattern.Stitches.Count; // Show all stitches by default
                    trackBar.TickFrequency = Math.Max(1, _pattern.Stitches.Count / 20); // ~20 ticks
                    trackBar.Enabled = true;
                    _maxStitchesToDisplay = _pattern.Stitches.Count;
                }
                else
                {
                    trackBar.Enabled = false;
                }

                // Update status
                UpdateStatus();

                // Update window title with filename
                this.Text = $"Embroidery Viewer - {_pattern.FileName}";

                // Generate preview image
                try
                {
                    byte[] fileData = File.ReadAllBytes(filePath);
                    _previewImageData = ExpFileParser.GeneratePreviewImage(fileData);
                }
                catch
                {
                    _previewImageData = null;
                }

                // Enable close and save as menu items
                closeToolStripMenuItem.Enabled = true;
                saveAsToolStripMenuItem.Enabled = true;

                // Update scroll bars
                UpdateScrollBars();

                // Trigger repaint
                renderPanel.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading file: {ex.Message}",
                    "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Loads embroidery file data from memory. Closes any existing file first.
        /// </summary>
        /// <param name="fileName">The name of the file (without path)</param>
        /// <param name="fileData">The raw .EXP file data</param>
        public void LoadFileFromMemory(string fileName, byte[] fileData)
        {
            try
            {
                // Close any existing file first
                if (_pattern != null)
                {
                    CloseFile();
                }

                // Validate file data
                if (fileData == null || fileData.Length == 0)
                {
                    MessageBox.Show("File data is empty or invalid.",
                        "Invalid File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Store file data and filename
                _currentFileData = fileData;
                _currentFileName = fileName;

                // Parse the file data directly
                _pattern = ExpFileParser.ParseFromBytes(fileData, fileName);

                // Calculate base scale and reset view
                _baseScale = CalculateBaseScale();
                _zoomFactor = 1.0f;
                _panOffset = new PointF(0, 0);

                // Configure trackbar for this pattern
                if (_pattern.Stitches.Count > 0)
                {
                    trackBar.Minimum = 0;
                    trackBar.Maximum = _pattern.Stitches.Count;
                    trackBar.Value = _pattern.Stitches.Count; // Show all stitches by default
                    trackBar.TickFrequency = Math.Max(1, _pattern.Stitches.Count / 20); // ~20 ticks
                    trackBar.Enabled = true;
                    _maxStitchesToDisplay = _pattern.Stitches.Count;
                }
                else
                {
                    trackBar.Enabled = false;
                }

                // Update status
                UpdateStatus();

                // Update window title with filename
                this.Text = $"Embroidery Viewer - {fileName}";

                // Generate preview image
                try
                {
                    _previewImageData = ExpFileParser.GeneratePreviewImage(fileData);
                }
                catch
                {
                    _previewImageData = null;
                }

                // Enable close and save as menu items
                closeToolStripMenuItem.Enabled = true;
                saveAsToolStripMenuItem.Enabled = true;

                // Update scroll bars
                UpdateScrollBars();

                // Trigger repaint
                renderPanel.Invalidate();

                // Show the form and bring it to front
                this.Show();
                this.BringToFront();
                this.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading file: {ex.Message}",
                    "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CloseFile()
        {
            _pattern = null;
            _zoomFactor = 1.0f;
            _baseScale = 1.0f;
            _panOffset = new PointF(0, 0);
            _maxStitchesToDisplay = int.MaxValue;
            _currentColorIndex = 0;
            _previewImageData = null;
            _currentFileData = null;
            _currentFileName = null;
            closeToolStripMenuItem.Enabled = false;
            saveAsToolStripMenuItem.Enabled = false;

            // Reset trackbar
            trackBar.Minimum = 0;
            trackBar.Maximum = 0;
            trackBar.Value = 0;
            trackBar.Enabled = false;

            // Reset window title
            this.Text = "Embroidery Viewer";

            // Reset scroll bars
            renderPanel.AutoScrollMinSize = Size.Empty;

            UpdateStatus();
            renderPanel.Invalidate();
        }

        private void UpdateStatus()
        {
            if (_pattern == null)
            {
                fileNameLabel.Text = "No file loaded";
                stitchCountLabel.Text = "Stitches: -";
                zoomLabel.Text = "Zoom: -";
                dimensionsLabel.Text = "Dimensions: -";
            }
            else
            {
                fileNameLabel.Text = $"File: {_pattern.FileName}";
                stitchCountLabel.Text = $"Stitches: {_pattern.TotalStitches} | Jumps: {_pattern.JumpCount} | Color Changes: {_pattern.ColorChangeCount}";
                zoomLabel.Text = $"Zoom: {_zoomFactor * 100:F0}%";

                var dims = _pattern.GetDimensionsMm();
                dimensionsLabel.Text = $"Dimensions: {dims.Width:F1} × {dims.Height:F1} mm";
            }
        }

        private void renderPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.White);

            if (_pattern == null || _pattern.Stitches.Count == 0)
            {
                // Draw "No file loaded" message
                string message = "No file loaded. Use File → Open to load an .EXP file.";
                using (Font font = new Font("Segoe UI", 12))
                using (Brush brush = new SolidBrush(Color.Gray))
                {
                    SizeF textSize = g.MeasureString(message, font);
                    PointF textPos = new PointF(
                        (renderPanel.Width - textSize.Width) / 2,
                        (renderPanel.Height - textSize.Height) / 2);
                    g.DrawString(message, font, brush, textPos);
                }
                return;
            }

            // Get pattern bounds
            RectangleF bounds = _pattern.GetBounds();

            // Apply zoom
            float scale = _baseScale * _zoomFactor;

            // Calculate pattern size in screen coordinates
            float patternScreenWidth = bounds.Width * scale;
            float patternScreenHeight = bounds.Height * scale;

            // Get scroll position
            Point scrollPos = renderPanel.AutoScrollPosition;

            // Position calculation depends on whether scroll bars are active
            float drawX, drawY;

            if (renderPanel.AutoScrollMinSize.IsEmpty)
            {
                // No scroll bars - center the pattern in the viewport
                drawX = (renderPanel.Width - patternScreenWidth) / 2 + _panOffset.X;
                drawY = (renderPanel.Height - patternScreenHeight) / 2 + _panOffset.Y;
            }
            else
            {
                // Scroll bars active - position based on scroll offset
                // The scroll position represents the top-left corner offset
                drawX = scrollPos.X;
                drawY = scrollPos.Y;
            }

            // Apply transformations
            float patternCenterX = bounds.Left + bounds.Width / 2;
            float patternCenterY = bounds.Top + bounds.Height / 2;

            // Move to draw position and account for pattern center
            g.TranslateTransform(drawX + patternScreenWidth / 2, drawY + patternScreenHeight / 2);
            g.ScaleTransform(scale, -scale); // Negative Y scale to flip vertically
            g.TranslateTransform(-patternCenterX, -patternCenterY);

            // Draw coordinate axes at origin
            using (Pen axisPen = new Pen(Color.LightGray, 0.5f))
            {
                g.DrawLine(axisPen, -50, 0, 50, 0); // X axis
                g.DrawLine(axisPen, 0, -50, 0, 50); // Y axis
            }

            // Draw stitches (only up to the max specified by trackbar)
            int stitchesToDraw = Math.Min(_maxStitchesToDisplay, _pattern.Stitches.Count);

            // Reset color index for drawing
            int colorIndex = 0;
            Color currentThreadColor = _threadColors[colorIndex];

            using (Pen jumpPen = new Pen(Color.FromArgb(255, 100, 100), 0.5f) { DashStyle = DashStyle.Dash }) // Light red for jumps
            using (Brush colorChangeBrush = new SolidBrush(Color.Gold)) // Gold marker for color changes
            {
                for (int i = 1; i < stitchesToDraw; i++)
                {
                    var prevStitch = _pattern.Stitches[i - 1];
                    var currStitch = _pattern.Stitches[i];

                    // Check if previous stitch was a color change
                    if (prevStitch.Type == StitchType.ColorChange)
                    {
                        // Advance to next color
                        colorIndex = (colorIndex + 1) % _threadColors.Length;
                        currentThreadColor = _threadColors[colorIndex];
                    }

                    if (currStitch.Type == StitchType.ColorChange)
                    {
                        // Draw color change marker
                        float markerSize = 4f;
                        g.FillEllipse(colorChangeBrush,
                            currStitch.X - markerSize / 2,
                            currStitch.Y - markerSize / 2,
                            markerSize, markerSize);
                    }
                    else if (currStitch.Type == StitchType.End)
                    {
                        // Don't draw end markers, just note the end
                        continue;
                    }
                    else
                    {
                        // Skip drawing jump stitches if jumps are hidden
                        if (currStitch.Type == StitchType.Jump && !_showJumps)
                        {
                            continue;
                        }

                        // Draw stitch lines if enabled
                        if (_showStitchLines)
                        {
                            // Create pen with current thread color
                            using (Pen normalPen = new Pen(currentThreadColor, 0.5f))
                            {
                                // Draw line from previous to current stitch
                                Pen pen = currStitch.Type == StitchType.Jump ? jumpPen : normalPen;

                                // Skip drawing if previous was a special command
                                if (prevStitch.Type != StitchType.ColorChange && prevStitch.Type != StitchType.End)
                                {
                                    g.DrawLine(pen, prevStitch.X, prevStitch.Y, currStitch.X, currStitch.Y);
                                }
                            }
                        }

                        // Draw stitch point if enabled
                        if (_showStitchPoints)
                        {
                            DrawStitchPoint(g, currStitch, currentThreadColor);
                        }
                    }
                }
            }

            // Draw preview image overlay if enabled
            if (_showPreviewImage && _previewImageData != null)
            {
                DrawPreviewOverlay(g);
            }
        }

        private void DrawPreviewOverlay(Graphics g)
        {
            if (_previewImageData == null)
                return;

            const int previewWidth = 72;
            const int previewHeight = 64;
            const int margin = 10;
            const int scale = 2; // Scale up for better visibility

            int scaledWidth = previewWidth * scale;
            int scaledHeight = previewHeight * scale;

            // Position in top-right corner
            int x = renderPanel.Width - scaledWidth - margin;
            int y = margin;

            // Reset transformation to draw in screen coordinates
            g.ResetTransform();

            // Draw semi-transparent white background
            using (Brush bgBrush = new SolidBrush(Color.FromArgb(200, 255, 255, 255)))
            {
                g.FillRectangle(bgBrush, x - 2, y - 2, scaledWidth + 4, scaledHeight + 4);
            }

            // Draw border
            using (Pen borderPen = new Pen(Color.Black, 1))
            {
                g.DrawRectangle(borderPen, x - 2, y - 2, scaledWidth + 4, scaledHeight + 4);
            }

            // Draw preview image
            for (int py = 0; py < previewHeight; py++)
            {
                for (int px = 0; px < previewWidth; px++)
                {
                    int byteIndex = (py * previewWidth + px) / 8;
                    int bitIndex = 7 - ((py * previewWidth + px) % 8);

                    if (byteIndex < _previewImageData.Length)
                    {
                        bool pixelSet = (_previewImageData[byteIndex] & (1 << bitIndex)) != 0;

                        if (pixelSet)
                        {
                            // Draw black pixel (scaled up)
                            using (Brush pixelBrush = new SolidBrush(Color.Black))
                            {
                                g.FillRectangle(pixelBrush,
                                    x + px * scale,
                                    y + py * scale,
                                    scale,
                                    scale);
                            }
                        }
                    }
                }
            }
        }

        private void renderPanel_MouseWheel(object sender, MouseEventArgs e)
        {
            if (_pattern == null)
                return;

            // Calculate zoom change
            float zoomDelta = e.Delta > 0 ? 1.1f : 0.9f;
            float newZoom = _zoomFactor * zoomDelta;

            // Limit zoom range
            newZoom = Math.Max(0.1f, Math.Min(newZoom, 20f));

            if (newZoom != _zoomFactor)
            {
                // Zoom towards mouse position
                Point mousePos = renderPanel.PointToClient(Cursor.Position);
                float zoomRatio = newZoom / _zoomFactor;

                // Adjust pan offset to zoom towards mouse
                _panOffset.X = mousePos.X - zoomRatio * (mousePos.X - _panOffset.X);
                _panOffset.Y = mousePos.Y - zoomRatio * (mousePos.Y - _panOffset.Y);

                _zoomFactor = newZoom;
                UpdateScrollBars();
                UpdateStatus();
                renderPanel.Invalidate();
            }
        }

        private void renderPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _lastMousePos = e.Location;
                renderPanel.Cursor = Cursors.Hand;
            }
        }

        private void renderPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                // Calculate drag delta
                int deltaX = e.X - _lastMousePos.X;
                int deltaY = e.Y - _lastMousePos.Y;

                // Update pan offset
                _panOffset.X += deltaX;
                _panOffset.Y += deltaY;

                // Update last position
                _lastMousePos = e.Location;

                // Repaint
                renderPanel.Invalidate();
            }
        }

        private void renderPanel_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = false;
                renderPanel.Cursor = Cursors.Default;
            }
        }

        private void EmbroideryViewerForm_KeyDown(object sender, KeyEventArgs e)
        {
            // Add keyboard shortcuts for zoom
            if (_pattern == null)
                return;

            if (e.Control)
            {
                if (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add)
                {
                    // Zoom in
                    float newZoom = _zoomFactor * 1.2f;
                    _zoomFactor = Math.Min(newZoom, 20f);
                    UpdateScrollBars();
                    UpdateStatus();
                    renderPanel.Invalidate();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract)
                {
                    // Zoom out
                    float newZoom = _zoomFactor * 0.8f;
                    _zoomFactor = Math.Max(newZoom, 0.1f);
                    UpdateScrollBars();
                    UpdateStatus();
                    renderPanel.Invalidate();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.D0 || e.KeyCode == Keys.NumPad0)
                {
                    // Reset zoom
                    _zoomFactor = 1.0f;
                    _panOffset = new PointF(0, 0);
                    UpdateScrollBars();
                    UpdateStatus();
                    renderPanel.Invalidate();
                    e.Handled = true;
                }
            }
        }

        private void TrackBar_ValueChanged(object? sender, EventArgs e)
        {
            if (_pattern == null)
                return;

            // Update the number of stitches to display based on trackbar value
            _maxStitchesToDisplay = trackBar.Value;

            // Trigger repaint to show the updated stitches
            renderPanel.Invalidate();
        }

        private void previewImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _showPreviewImage = previewImageToolStripMenuItem.Checked;
            renderPanel.Invalidate();
        }

        private void jumpsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _showJumps = jumpsToolStripMenuItem.Checked;
            renderPanel.Invalidate();
        }

        private void stitchLinesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _showStitchLines = stitchLinesToolStripMenuItem.Checked;
            renderPanel.Invalidate();
        }

        private void stitchPointsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _showStitchPoints = stitchPointsToolStripMenuItem.Checked;
            renderPanel.Invalidate();
        }

        private void DrawStitchPoint(Graphics g, StitchPoint stitch, Color threadColor)
        {
            float size = 3f;
            float halfSize = size / 2;

            switch (stitch.Type)
            {
                case StitchType.Normal:
                    // Normal stitch: solid circle in thread color
                    using (Brush normalBrush = new SolidBrush(threadColor))
                    {
                        g.FillEllipse(normalBrush, stitch.X - halfSize, stitch.Y - halfSize, size, size);
                    }
                    break;

                case StitchType.Jump:
                    // Jump: hollow red square
                    using (Pen jumpPointPen = new Pen(Color.Red, 0.5f))
                    {
                        g.DrawRectangle(jumpPointPen, stitch.X - halfSize, stitch.Y - halfSize, size, size);
                    }
                    break;

                case StitchType.ColorChange:
                    // Color change: solid gold diamond
                    PointF[] diamond = new PointF[]
                    {
                        new PointF(stitch.X, stitch.Y - halfSize * 1.5f),  // Top
                        new PointF(stitch.X + halfSize * 1.5f, stitch.Y),  // Right
                        new PointF(stitch.X, stitch.Y + halfSize * 1.5f),  // Bottom
                        new PointF(stitch.X - halfSize * 1.5f, stitch.Y)   // Left
                    };
                    using (Brush colorChangeBrush = new SolidBrush(Color.Gold))
                    {
                        g.FillPolygon(colorChangeBrush, diamond);
                    }
                    break;

                case StitchType.End:
                    // End/Cut: solid red X (cross)
                    using (Pen endPen = new Pen(Color.Red, 1f))
                    {
                        float crossSize = size * 1.2f;
                        g.DrawLine(endPen,
                            stitch.X - crossSize, stitch.Y - crossSize,
                            stitch.X + crossSize, stitch.Y + crossSize);
                        g.DrawLine(endPen,
                            stitch.X - crossSize, stitch.Y + crossSize,
                            stitch.X + crossSize, stitch.Y - crossSize);
                    }
                    break;
            }
        }

        private float CalculateBaseScale()
        {
            if (_pattern == null)
                return 1.0f;

            RectangleF bounds = _pattern.GetBounds();

            // Calculate scale to fit pattern in panel with some margin
            float marginPercent = 0.1f; // 10% margin
            float availableWidth = renderPanel.ClientSize.Width * (1 - 2 * marginPercent);
            float availableHeight = renderPanel.ClientSize.Height * (1 - 2 * marginPercent);

            float scaleX = availableWidth / bounds.Width;
            float scaleY = availableHeight / bounds.Height;

            return Math.Min(scaleX, scaleY);
        }

        private void SetZoom(float newZoomFactor)
        {
            if (_pattern == null)
                return;

            // Store the old zoom factor
            float oldZoomFactor = _zoomFactor;

            // Update to new zoom
            _zoomFactor = Math.Max(0.1f, Math.Min(newZoomFactor, 20f));
            _panOffset = new PointF(0, 0); // Reset pan when setting specific zoom

            // Get pattern bounds and calculate sizes
            RectangleF bounds = _pattern.GetBounds();
            float scale = _baseScale * _zoomFactor;
            float oldScale = _baseScale * oldZoomFactor;

            int newPatternWidth = (int)Math.Ceiling(bounds.Width * scale);
            int newPatternHeight = (int)Math.Ceiling(bounds.Height * scale);
            int oldPatternWidth = (int)Math.Ceiling(bounds.Width * oldScale);
            int oldPatternHeight = (int)Math.Ceiling(bounds.Height * oldScale);

            // Check if we need scroll bars at the new zoom level
            bool needsHorizontalScroll = newPatternWidth > renderPanel.ClientSize.Width;
            bool needsVerticalScroll = newPatternHeight > renderPanel.ClientSize.Height;

            // If we had scroll bars before and will have them after, maintain center
            if (!renderPanel.AutoScrollMinSize.IsEmpty && (needsHorizontalScroll || needsVerticalScroll))
            {
                // Get the current scroll position (as positive values)
                Point currentScroll = new Point(
                    -renderPanel.AutoScrollPosition.X,
                    -renderPanel.AutoScrollPosition.Y);

                // Calculate what percentage of the pattern we're scrolled to
                float scrollPercentX = 0.5f; // Default to center
                float scrollPercentY = 0.5f;

                if (oldPatternWidth > renderPanel.ClientSize.Width)
                {
                    scrollPercentX = (currentScroll.X + renderPanel.ClientSize.Width / 2.0f) / oldPatternWidth;
                }
                if (oldPatternHeight > renderPanel.ClientSize.Height)
                {
                    scrollPercentY = (currentScroll.Y + renderPanel.ClientSize.Height / 2.0f) / oldPatternHeight;
                }

                // Update scroll bars
                UpdateScrollBars();

                // Calculate new scroll position to maintain the same center
                if (needsHorizontalScroll)
                {
                    int newScrollX = (int)(scrollPercentX * newPatternWidth - renderPanel.ClientSize.Width / 2.0f);
                    newScrollX = Math.Max(0, Math.Min(newScrollX, newPatternWidth - renderPanel.ClientSize.Width));
                    renderPanel.AutoScrollPosition = new Point(-newScrollX, renderPanel.AutoScrollPosition.Y);
                }

                if (needsVerticalScroll)
                {
                    int newScrollY = (int)(scrollPercentY * newPatternHeight - renderPanel.ClientSize.Height / 2.0f);
                    newScrollY = Math.Max(0, Math.Min(newScrollY, newPatternHeight - renderPanel.ClientSize.Height));
                    renderPanel.AutoScrollPosition = new Point(renderPanel.AutoScrollPosition.X, -newScrollY);
                }
            }
            else
            {
                // No scroll bars before or after, just update normally
                UpdateScrollBars();
            }

            UpdateStatus();
            renderPanel.Invalidate();
        }

        private void UpdateScrollBars()
        {
            if (_pattern == null)
            {
                renderPanel.AutoScrollMinSize = Size.Empty;
                return;
            }

            // Get pattern bounds
            RectangleF bounds = _pattern.GetBounds();

            // Calculate the size of the pattern in screen coordinates
            float scale = _baseScale * _zoomFactor;
            int patternWidth = (int)Math.Ceiling(bounds.Width * scale);
            int patternHeight = (int)Math.Ceiling(bounds.Height * scale);

            // Check if pattern exceeds viewport size
            bool needsHorizontalScroll = patternWidth > renderPanel.ClientSize.Width;
            bool needsVerticalScroll = patternHeight > renderPanel.ClientSize.Height;

            // If pattern fits completely in viewport, no scroll bars needed
            if (!needsHorizontalScroll && !needsVerticalScroll)
            {
                renderPanel.AutoScrollMinSize = Size.Empty;
                // Reset pan offset when no scrolling is needed
                _panOffset = new PointF(0, 0);
                return;
            }

            // Calculate scroll area size
            // The scroll area should simply be the pattern size when it exceeds the viewport
            // This allows scrolling to see all parts of the pattern
            int scrollWidth = needsHorizontalScroll ? patternWidth : renderPanel.ClientSize.Width;
            int scrollHeight = needsVerticalScroll ? patternHeight : renderPanel.ClientSize.Height;

            // Set the AutoScrollMinSize to enable scroll bars
            renderPanel.AutoScrollMinSize = new Size(scrollWidth, scrollHeight);

            // When scrolling is enabled, reset pan offset to avoid double-offsetting
            _panOffset = new PointF(0, 0);
        }

        private void fitToWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetZoom(1.0f);
        }

        private void zoom50ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_pattern == null)
                return;

            _baseScale = CalculateBaseScale();
            SetZoom(0.5f);
        }

        private void zoom100ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_pattern == null)
                return;

            _baseScale = CalculateBaseScale();
            SetZoom(1.0f);
        }

        private void zoom200ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_pattern == null)
                return;

            _baseScale = CalculateBaseScale();
            SetZoom(2.0f);
        }

        private void EmbroideryViewerForm_DragEnter(object? sender, DragEventArgs e)
        {
            // Check if the data being dragged is a file
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[]? files = e.Data.GetData(DataFormats.FileDrop) as string[];

                // Check if exactly one file is being dragged and it's an .exp file
                if (files != null && files.Length == 1 &&
                    Path.GetExtension(files[0]).Equals(".exp", StringComparison.OrdinalIgnoreCase))
                {
                    e.Effect = DragDropEffects.Copy;
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void EmbroideryViewerForm_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[]? files = e.Data.GetData(DataFormats.FileDrop) as string[];

                if (files != null && files.Length == 1)
                {
                    string filePath = files[0];

                    // Check if it's an .exp file
                    if (Path.GetExtension(filePath).Equals(".exp", StringComparison.OrdinalIgnoreCase))
                    {
                        // Close any existing file before loading the new one
                        if (_pattern != null)
                        {
                            CloseFile();
                        }

                        // Load the dropped file
                        LoadFile(filePath);
                    }
                }
            }
        }

        private void toEmbroideryModuleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Check if we have a loaded pattern
            if (_pattern == null || _currentFileData == null || _currentFileName == null)
            {
                MessageBox.Show("No embroidery pattern loaded. Please open a file first.", "No Pattern Loaded",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Confirm with the user
            string fileName = Path.GetFileNameWithoutExtension(_currentFileName);
            DialogResult confirmResult = MessageBox.Show(
                $"Do you want to upload '{fileName}' to the Embroidery Module?\n\n" +
                "This will transfer the pattern to the machine's memory.",
                "Confirm Upload",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmResult != DialogResult.Yes)
            {
                return;
            }

            try
            {
                // Create preview image from the current file data
                byte[]? previewData = ExpFileParser.GeneratePreviewImage(_currentFileData);
                
                if (previewData == null || previewData.Length == 0)
                {
                    MessageBox.Show("Failed to generate preview image for upload.", "Upload Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Create EmbroideryFile object
                EmbroideryFile fileToUpload = new EmbroideryFile
                {
                    FileName = fileName,
                    FileData = _currentFileData,
                    PreviewImageData = previewData,
                    FileExtraData = null
                };

                // Find the MainForm and call the upload method
                MainForm? mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
                
                if (mainForm == null)
                {
                    MessageBox.Show("Main form not found. Please ensure the application is running correctly.", "Upload Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Call the upload method on the main form
                _ = mainForm.UploadEmbroideryFileAsync(fileToUpload);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error preparing file for upload: {ex.Message}", "Upload Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
