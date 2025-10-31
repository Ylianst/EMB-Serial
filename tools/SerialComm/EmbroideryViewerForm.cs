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

                // Parse the file
                _pattern = ExpFileParser.Parse(filePath);

                // Reset view
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

                // Enable close menu item
                closeToolStripMenuItem.Enabled = true;

                // Trigger repaint
                renderPanel.Invalidate();
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
            _panOffset = new PointF(0, 0);
            _maxStitchesToDisplay = int.MaxValue;
            closeToolStripMenuItem.Enabled = false;
            
            // Reset trackbar
            trackBar.Minimum = 0;
            trackBar.Maximum = 0;
            trackBar.Value = 0;
            trackBar.Enabled = false;
            
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
            
            // Calculate scale to fit pattern in panel with some margin
            float marginPercent = 0.1f; // 10% margin
            float availableWidth = renderPanel.Width * (1 - 2 * marginPercent);
            float availableHeight = renderPanel.Height * (1 - 2 * marginPercent);
            
            float scaleX = availableWidth / bounds.Width;
            float scaleY = availableHeight / bounds.Height;
            float baseScale = Math.Min(scaleX, scaleY);

            // Apply zoom
            float scale = baseScale * _zoomFactor;

            // Calculate center offset
            float centerX = renderPanel.Width / 2;
            float centerY = renderPanel.Height / 2;
            float patternCenterX = bounds.Left + bounds.Width / 2;
            float patternCenterY = bounds.Top + bounds.Height / 2;

            // Apply transformations
            g.TranslateTransform(centerX + _panOffset.X, centerY + _panOffset.Y);
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
            
            using (Pen normalPen = new Pen(Color.Blue, 0.5f))
            using (Pen jumpPen = new Pen(Color.Red, 0.5f) { DashStyle = DashStyle.Dash })
            using (Brush colorChangeBrush = new SolidBrush(Color.Green))
            {
                for (int i = 1; i < stitchesToDraw; i++)
                {
                    var prevStitch = _pattern.Stitches[i - 1];
                    var currStitch = _pattern.Stitches[i];

                    if (currStitch.Type == StitchType.ColorChange)
                    {
                        // Draw color change marker
                        float markerSize = 3f;
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
                        // Draw line from previous to current stitch
                        Pen pen = currStitch.Type == StitchType.Jump ? jumpPen : normalPen;
                        
                        // Skip drawing if previous was a special command
                        if (prevStitch.Type != StitchType.ColorChange && prevStitch.Type != StitchType.End)
                        {
                            g.DrawLine(pen, prevStitch.X, prevStitch.Y, currStitch.X, currStitch.Y);
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
                    UpdateStatus();
                    renderPanel.Invalidate();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract)
                {
                    // Zoom out
                    float newZoom = _zoomFactor * 0.8f;
                    _zoomFactor = Math.Max(newZoom, 0.1f);
                    UpdateStatus();
                    renderPanel.Invalidate();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.D0 || e.KeyCode == Keys.NumPad0)
                {
                    // Reset zoom
                    _zoomFactor = 1.0f;
                    _panOffset = new PointF(0, 0);
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
    }
}
