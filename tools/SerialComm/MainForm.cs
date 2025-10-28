using System.IO.Ports;
using Bernina.SerialStack;
using Microsoft.Win32;

namespace SerialComm
{
    public partial class MainForm : Form
    {
        private SerialStack? _serialStack;
        private bool _isConnected = false;
        private string? _selectedComPort = null;
        private static DebugForm? _debugForm = null;
        private const string RegistryKeyPath = @"Software\BerninaSerialComm";
        private const string ComPortValueName = "LastComPort";
        private const string PreviewCacheValueName = "PreviewCache";
        private List<EmbroideryFileControl>? _embroideryFileControls = null;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Load last used COM port from registry
            LoadLastComPort();

            // Populate COM port menu
            RefreshComPortsMenu();

            // Initialize status
            UpdateConnectionStatus("Disconnected", false);

            // Enable debug menu (can be opened anytime)
            showDeveloperDebugToolStripMenuItem.Enabled = true;
        }

        private void RefreshComPortsMenu()
        {
            var itemsToRemove = new List<ToolStripItem>();
            foreach (ToolStripItem item in selectCOMPortToolStripMenuItem.DropDownItems)
            {
                if (item.Tag?.ToString() == "comport")
                {
                    itemsToRemove.Add(item);
                }
            }
            foreach (var item in itemsToRemove)
            {
                selectCOMPortToolStripMenuItem.DropDownItems.Remove(item);
            }

            string[] ports = SerialPort.GetPortNames();
            
            if (ports.Length > 0)
            {
                foreach (string port in ports)
                {
                    ToolStripMenuItem portItem = new ToolStripMenuItem(port);
                    portItem.Tag = "comport";
                    portItem.Click += ComPortMenuItem_Click;
                    
                    if (port == _selectedComPort)
                    {
                        portItem.Checked = true;
                    }
                    
                    selectCOMPortToolStripMenuItem.DropDownItems.Add(portItem);
                }
            }
            else
            {
                ToolStripMenuItem noPortsItem = new ToolStripMenuItem("No ports available");
                noPortsItem.Tag = "comport";
                noPortsItem.Enabled = false;
                selectCOMPortToolStripMenuItem.DropDownItems.Add(noPortsItem);
            }
        }

        private void ComPortMenuItem_Click(object? sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem)
            {
                foreach (ToolStripItem item in selectCOMPortToolStripMenuItem.DropDownItems)
                {
                    if (item is ToolStripMenuItem mi && item.Tag?.ToString() == "comport")
                    {
                        mi.Checked = false;
                    }
                }
                
                menuItem.Checked = true;
                _selectedComPort = menuItem.Text;
                UpdateStatus($"Selected COM port: {_selectedComPort}");
            }
        }

        private void LoadLastComPort()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
                {
                    if (key != null)
                    {
                        string? lastPort = key.GetValue(ComPortValueName) as string;
                        if (!string.IsNullOrEmpty(lastPort))
                        {
                            _selectedComPort = lastPort;
                        }
                    }
                }
            }
            catch
            {
                // If registry read fails, just use default selection
            }
        }

        private void SaveLastComPort(string portName)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath))
                {
                    key.SetValue(ComPortValueName, portName);
                }
            }
            catch
            {
                // If registry write fails, continue without saving
            }
        }

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            if (_isConnected)
            {
                Disconnect();
            }
            else
            {
                if (string.IsNullOrEmpty(_selectedComPort))
                {
                    MessageBox.Show("Please select a COM port from the Connection menu first.", "Connection Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                await ConnectAsync();
            }
        }

        private async Task ConnectAsync()
        {
            if (string.IsNullOrEmpty(_selectedComPort))
            {
                MessageBox.Show("Please select a COM port from the Connection menu first.", "Connection Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string portName = _selectedComPort;

            btnConnect.Enabled = false;
            connectToolStripMenuItem.Enabled = false;
            selectCOMPortToolStripMenuItem.Enabled = false;

            UpdateStatus("Connecting to " + portName + "...");
            UpdateConnectionStatus("Connecting...", false);

            try
            {
                _serialStack = new SerialStack(portName);
                _serialStack.ConnectionStateChanged += OnConnectionStateChanged;
                _serialStack.DebugMessage += OnDebugMessage;

                bool connected = await _serialStack.OpenAsync();

                if (connected)
                {
                    _isConnected = true;
                    SaveLastComPort(portName);

                    btnConnect.Text = "Disconnect";
                    btnConnect.Enabled = true;

                    connectToolStripMenuItem.Enabled = false;
                    disconnectToolStripMenuItem.Enabled = true;
                    selectCOMPortToolStripMenuItem.Enabled = false;
                    showDeveloperDebugToolStripMenuItem.Enabled = true;

                    UpdateStatus("Connected");
                    UpdateConnectionStatus($"Connected: {portName} @ {_serialStack.BaudRate} baud", true);

                    // Load preview cache from registry
                    await LoadPreviewCacheFromRegistryAsync();

                    // Change baud rate to 57600 for faster file reading
                    UpdateStatus("Switching to 57600 baud...");
                    bool baudChangeSuccess = await _serialStack.ChangeTo57600BaudAsync();
                    if (baudChangeSuccess)
                    {
                        UpdateStatus("Baud rate changed to 57600");
                        UpdateConnectionStatus($"Connected: {portName} @ {_serialStack.BaudRate} baud", true);
                    }
                    else
                    {
                        UpdateStatus("Failed to change baud rate, continuing at current speed");
                    }

                    // Load embroidery files with previews
                    UpdateStatus("Loading embroidery files...");
                    ShowProgressBar(true, "Loading embroidery files...");
                    
                    // Clear any previous files before starting
                    ClearEmbroideryFiles();
                    _embroideryFileControls = new List<EmbroideryFileControl>();
                    
                    var embroideryFiles = await _serialStack.ReadEmbroideryFilesAsync(
                        StorageLocation.EmbroideryModuleMemory, 
                        true, 
                        (current, total) => UpdateProgress(current, total),
                        (file) => AddFileToUIRealTime(file)
                    );

                    ShowProgressBar(false);

                    if (embroideryFiles != null)
                    {
                        UpdateStatus($"Loaded {embroideryFiles.Count} embroidery files");
                        
                        // Save the preview cache to registry immediately after loading
                        UpdateStatus("Saving preview cache...");
                        await SavePreviewCacheToRegistryAsync();
                    }
                    else
                    {
                        UpdateStatus("Failed to load embroidery files");
                    }
                }
                else
                {
                    _serialStack?.Dispose();
                    _serialStack = null;

                    btnConnect.Text = "Connect";
                    btnConnect.Enabled = true;
                    connectToolStripMenuItem.Enabled = true;
                    selectCOMPortToolStripMenuItem.Enabled = true;

                    UpdateStatus("Connection failed");
                    UpdateConnectionStatus("Disconnected", false);

                    MessageBox.Show("Failed to connect. Make sure the machine is powered on and connected.",
                        "Connection Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                _serialStack?.Dispose();
                _serialStack = null;

                btnConnect.Text = "Connect";
                btnConnect.Enabled = true;
                connectToolStripMenuItem.Enabled = true;
                selectCOMPortToolStripMenuItem.Enabled = true;

                UpdateStatus("Connection error");
                UpdateConnectionStatus("Disconnected", false);

                MessageBox.Show($"Connection error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Disconnect()
        {
            if (_serialStack != null)
            {
                _serialStack.Close();
                _serialStack.Dispose();
                _serialStack = null;
            }

            _isConnected = false;

            btnConnect.Text = "Connect";
            btnConnect.Enabled = true;

            connectToolStripMenuItem.Enabled = true;
            disconnectToolStripMenuItem.Enabled = false;
            selectCOMPortToolStripMenuItem.Enabled = true;
            showDeveloperDebugToolStripMenuItem.Enabled = true;

            // Clear displayed embroidery files
            ClearEmbroideryFiles();

            // Update debug form when disconnecting
            if (_debugForm != null && !_debugForm.IsDisposed)
            {
                _debugForm.UpdateSerialStack(_serialStack);
                _debugForm.UpdateConnectionStatus(false);
            }

            UpdateStatus("Disconnected");
            UpdateConnectionStatus("Disconnected", false);
        }

        private void showDeveloperDebugToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_serialStack == null)
            {
                // Allow showing debug form even when not connected
                _serialStack = new SerialStack("COM1"); // Placeholder - won't actually connect
            }

            // If debug form already exists and is not disposed, just focus it
            if (_debugForm != null && !_debugForm.IsDisposed)
            {
                _debugForm.Focus();
                _debugForm.BringToFront();
                return;
            }

            // Create new debug form
            _debugForm = new DebugForm(_serialStack);
            _debugForm.UpdateConnectionStatus(_isConnected);
            _debugForm.Show();
        }

        private async Task LoadPreviewCacheFromRegistryAsync()
        {
            if (_serialStack == null)
            {
                return;
            }

            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
                {
                    if (key != null)
                    {
                        byte[]? cacheData = key.GetValue(PreviewCacheValueName) as byte[];
                        if (cacheData != null && cacheData.Length > 0)
                        {
                            bool success = await _serialStack.DeserializeCacheAsync(cacheData);
                            if (success)
                            {
                                UpdateStatus("Preview cache loaded");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error loading preview cache: {ex.Message}");
            }
        }

        private async Task SavePreviewCacheToRegistryAsync()
        {
            if (_serialStack == null)
            {
                return;
            }

            try
            {
                byte[] compressedData = await _serialStack.SerializeCacheAsync();
                
                if (compressedData.Length > 0)
                {
                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath))
                    {
                        key.SetValue(PreviewCacheValueName, compressedData, RegistryValueKind.Binary);
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error saving preview cache: {ex.Message}");
            }
        }

        private void OnDebugMessage(object? sender, DebugMessageEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnDebugMessage(sender, e)));
                return;
            }

            // Forward debug messages to debug form if it exists
            if (_debugForm != null && !_debugForm.IsDisposed)
            {
                _debugForm.AppendOutput(e.Message);
            }
        }

        private void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnConnectionStateChanged(sender, e)));
                return;
            }

            // Update debug form if it exists with the actual SerialStack instance
            if (_debugForm != null && !_debugForm.IsDisposed)
            {
                _debugForm.UpdateSerialStack(_serialStack);
                _debugForm.UpdateConnectionStatus(e.NewState == ConnectionState.Connected);
            }
        }

        private void UpdateStatus(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateStatus(message)));
                return;
            }

            toolStripStatusLabel.Text = message;
        }

        private void UpdateConnectionStatus(string message, bool connected)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateConnectionStatus(message, connected)));
                return;
            }

            toolStripStatusLabelConnection.Text = message;

            if (connected)
            {
                toolStripStatusLabelConnection.ForeColor = Color.Black;
            }
            else
            {
                toolStripStatusLabelConnection.ForeColor = Color.Red;
            }
        }

        private void DisplayEmbroideryFiles(List<EmbroideryFile> files)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => DisplayEmbroideryFiles(files)));
                return;
            }

            // Clear previous files
            ClearEmbroideryFiles();

            // Create and add controls for each file
            _embroideryFileControls = new List<EmbroideryFileControl>(files.Count);

            foreach (var file in files)
            {
                var fileControl = new EmbroideryFileControl();
                fileControl.SetEmbroideryFile(file);
                flowLayoutPanelFiles.Controls.Add(fileControl);
                _embroideryFileControls.Add(fileControl);
            }
        }

        private void ClearEmbroideryFiles()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ClearEmbroideryFiles()));
                return;
            }

            flowLayoutPanelFiles.Controls.Clear();
            if (_embroideryFileControls != null)
            {
                foreach (var control in _embroideryFileControls)
                {
                    control?.Dispose();
                }
                _embroideryFileControls.Clear();
            }
        }

        private void ShowProgressBar(bool visible, string message = "")
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ShowProgressBar(visible, message)));
                return;
            }

            if (visible)
            {
                toolStripProgressBar.Visible = true;
                toolStripProgressBar.Value = 0;
                toolStripProgressBar.Maximum = 100;
                UpdateStatus(message);
            }
            else
            {
                toolStripProgressBar.Visible = false;
            }
        }

        private void UpdateProgress(int current, int total)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateProgress(current, total)));
                return;
            }

            if (total > 0)
            {
                int percentage = (int)((current * 100) / total);
                toolStripProgressBar.Value = Math.Min(percentage, 100);
                UpdateStatus($"Loading embroidery files... {current}/{total}");
            }
        }

        private void AddFileToUIRealTime(EmbroideryFile file)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => AddFileToUIRealTime(file)));
                return;
            }

            // Create a new control for this file
            var fileControl = new EmbroideryFileControl();
            fileControl.SetEmbroideryFile(file);
            
            // Add to flow layout panel (will appear immediately)
            flowLayoutPanelFiles.Controls.Add(fileControl);
            
            // Track the control
            if (_embroideryFileControls != null)
            {
                _embroideryFileControls.Add(fileControl);
            }
            
            // Auto-scroll to the newly added file
            flowLayoutPanelFiles.ScrollControlIntoView(fileControl);
        }

        private void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnConnect_Click(sender, e);
        }

        private void disconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Disconnect();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Close debug form if it exists
            if (_debugForm != null && !_debugForm.IsDisposed)
            {
                _debugForm.Close();
            }

            if (_serialStack != null)
            {
                _serialStack.Close();
                _serialStack.Dispose();
                _serialStack = null;
            }
        }
    }
}
