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
        private MemoryDumpForm? _memoryDumpForm = null;
        private ComPortMonitor? _comPortMonitor = null;
        private const string RegistryKeyPath = @"Software\BerninaSerialComm";
        private const string ComPortValueName = "LastComPort";
        private const string PreviewCacheValueName = "PreviewCache";
        private List<EmbroideryFileControl>? _embroideryFileControls = null;
        private List<EmbroideryFileControl>? _pcCardFileControls = null;
        private string[]? _previousPortList = null;
        private string? _lastAskedAboutPort = null;
        private IconCacheMode _iconCacheMode = IconCacheMode.Normal;
        private System.Windows.Forms.Timer? _serialStatsTimer = null;
        
        // Cached firmware information
        private FirmwareInfo? _sewingMachineFirmwareInfo = null;
        private FirmwareInfo? _embroideryModuleFirmwareInfo = null;

        private enum IconCacheMode
        {
            None,
            Normal,
            Fast
        }

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Load last used COM port from registry
            LoadLastComPort();

            // Load icon cache mode preference from registry
            LoadIconCacheModeFromRegistry();

            // Populate COM port menu
            RefreshComPortsMenu();

            // Initialize previous port list for change detection
            _previousPortList = SerialPort.GetPortNames();

            // Initialize status
            UpdateConnectionStatus("Disconnected", false);

            // Enable debug menu (can be opened anytime)
            showDeveloperDebugToolStripMenuItem.Enabled = true;

            // Enable double buffering on the form to prevent blinking during ListView updates
            this.DoubleBuffered = true;

            // Enable double buffering on the ListView to prevent blinking
            if (machineInfoListView != null)
            {
                // Use reflection to enable double buffering on the ListView
                typeof(ListView).InvokeMember("DoubleBuffered",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
                    null, machineInfoListView, new object[] { true });
            }

            // Remove embroidery and PC Card tabs from tab control on application startup
            if (embroideryTabPage != null && mainTabControl.TabPages.Contains(embroideryTabPage))
            {
                mainTabControl.TabPages.Remove(embroideryTabPage);
            }
            if (pcCardTabPage != null && mainTabControl.TabPages.Contains(pcCardTabPage))
            {
                mainTabControl.TabPages.Remove(pcCardTabPage);
            }

            // Start monitoring for COM port changes
            try
            {
                _comPortMonitor = new ComPortMonitor();
                _comPortMonitor.ComPortsChanged += ComPortMonitor_ComPortsChanged;
                _comPortMonitor.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to start COM port monitor: {ex.Message}");
                // Continue even if monitor fails - the app can still work with manual refresh
            }
        }

        private void ComPortMonitor_ComPortsChanged(object? sender, EventArgs e)
        {
            // Refresh the COM port menu when ports are added or removed
            if (InvokeRequired)
            {
                Invoke(new Action(() => ComPortMonitor_ComPortsChanged(sender, e)));
                return;
            }

            // Only suggest new ports when disconnected
            if (!_isConnected)
            {
                string[] currentPorts = SerialPort.GetPortNames();
                
                // Find new ports that weren't in the previous list
                if (_previousPortList != null)
                {
                    foreach (string port in currentPorts)
                    {
                        // Only ask about ports we haven't already asked about
                        if (!_previousPortList.Contains(port) && port != _selectedComPort && port != _lastAskedAboutPort)
                        {
                            // New port detected and it's not the currently selected one
                            _lastAskedAboutPort = port; // Mark that we've asked about this port
                            
                            DialogResult result = MessageBox.Show(
                                $"A new COM port \"{port}\" has been detected. Would you like to use this port to connect to the sewing machine?",
                                "New COM Port Detected",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question);

                            if (result == DialogResult.Yes)
                            {
                                _selectedComPort = port;
                                _lastAskedAboutPort = null; // Clear the tracking since we've selected it
                                UpdateStatus($"Selected COM port: {_selectedComPort}");
                            }
                            break; // Only ask about one new port at a time
                        }
                    }
                }
                
                // Update the previous port list
                _previousPortList = currentPorts;
            }

            RefreshComPortsMenu();
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
                // Sort ports numerically (COM1, COM2, ..., COM9, COM10, COM11, etc.)
                Array.Sort(ports, (a, b) =>
                {
                    // Extract numeric part from port names
                    string numA = System.Text.RegularExpressions.Regex.Replace(a, @"\D", "");
                    string numB = System.Text.RegularExpressions.Regex.Replace(b, @"\D", "");
                    
                    if (int.TryParse(numA, out int intA) && int.TryParse(numB, out int intB))
                    {
                        return intA.CompareTo(intB);
                    }
                    return a.CompareTo(b); // Fallback to string comparison
                });

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

                // Check if the selected COM port is currently available
                string[] availablePorts = SerialPort.GetPortNames();
                if (!availablePorts.Contains(_selectedComPort))
                {
                    MessageBox.Show($"The selected COM port \"{_selectedComPort}\" is not currently available on this system.", "Port Not Available",
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
                _serialStack.BusyStateChanged += OnBusyStateChanged;

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

                    // Load preview cache from registry based on icon cache mode
                    if (_iconCacheMode != IconCacheMode.None)
                    {
                        await LoadPreviewCacheFromRegistryAsync();
                    }

                    // Hide notConnectedLabel and show machineInfoListView
                    if (notConnectedLabel != null)
                    {
                        notConnectedLabel.Visible = false;
                    }
                    if (machineInfoListView != null)
                    {
                        machineInfoListView.Visible = true;
                    }

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

                    // Get and display machine information
                    UpdateStatus("Retrieving machine information...");
                    await PopulateMachineInfoAsync();

                    // Start timer to poll serial stats every 5 seconds
                    _serialStatsTimer = new System.Windows.Forms.Timer();
                    _serialStatsTimer.Interval = 1000; // 1 second
                    _serialStatsTimer.Tick += UpdateSerialStats;
                    _serialStatsTimer.Start();

                    if (_embroideryModuleFirmwareInfo != null)
                    {
                        // Load embroidery files with previews
                        UpdateStatus("Loading embroidery files...");
                        ShowProgressBar(true, "Loading embroidery files...");

                        // Clear any previous files before starting
                        ClearEmbroideryFiles();
                        _embroideryFileControls = new List<EmbroideryFileControl>();

                        // Determine fast cache lookup based on icon cache mode
                        bool useFastCacheLookup = (_iconCacheMode == IconCacheMode.Fast);

                        // Don't close session if PC Card is inserted (we'll load PC Card files next)
                        bool closeSession = true;// !_embroideryModuleFirmwareInfo.PcCardInserted;

                        var embroideryFiles = await _serialStack.ReadEmbroideryFilesAsync(
                            StorageLocation.EmbroideryModuleMemory,
                            true,
                            (current, total) => UpdateProgress(current, total, false),
                            (file) => AddFileToUIRealTime(file),
                            useFastCacheLookup,
                            closeSession
                        );

                        ShowProgressBar(false);

                        if (embroideryFiles != null)
                        {
                            UpdateStatus($"Loaded {embroideryFiles.Count} embroidery files");

                            // Update machine info with file count
                            UpdateMachineInfoFileCount(embroideryFiles.Count);

                            // Save the preview cache to registry immediately after loading
                            await SavePreviewCacheToRegistryAsync();
                        }
                        else
                        {
                            UpdateStatus("Failed to load embroidery files");
                        }

                        if (_embroideryModuleFirmwareInfo.PcCardInserted)
                        {
                            // Load files from PC Card
                            UpdateStatus("Loading PC Card files...");
                            ShowProgressBar(true, "Loading PC Card files...");

                            // Clear any previous PC Card files
                            ClearPcCardFiles();
                            _pcCardFileControls = new List<EmbroideryFileControl>();

                            var pcCardFiles = await _serialStack.ReadEmbroideryFilesAsync(
                                StorageLocation.PCCard,
                                true,
                                (current, total) => UpdateProgress(current, total, true),
                                (file) => AddPcCardFileToUIRealTime(file),
                                useFastCacheLookup
                            );

                            ShowProgressBar(false);

                            if (pcCardFiles != null)
                            {
                                UpdateStatus($"Loaded {pcCardFiles.Count} PC Card files");
                            }
                            else
                            {
                                UpdateStatus("Failed to load PC Card files");
                            }

                            // Save the preview cache to registry after loading PC Card files
                            await SavePreviewCacheToRegistryAsync();
                        }
                    }
                    else
                    {
                        UpdateStatus("No embroidery module detected");
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
            // Stop the serial stats timer
            if (_serialStatsTimer != null)
            {
                _serialStatsTimer.Stop();
                _serialStatsTimer.Dispose();
                _serialStatsTimer = null;
            }

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
            ClearPcCardFiles();

            // Clear machine info ListView and hide it
            if (machineInfoListView != null)
            {
                machineInfoListView.Items.Clear();
                machineInfoListView.Groups.Clear();
                machineInfoListView.Visible = false;
            }

            // Show notConnectedLabel
            if (notConnectedLabel != null)
            {
                notConnectedLabel.Visible = true;
            }

            // Remove embroidery and PC Card tabs when disconnecting
            if (embroideryTabPage != null && mainTabControl.TabPages.Contains(embroideryTabPage))
            {
                mainTabControl.TabPages.Remove(embroideryTabPage);
            }
            if (pcCardTabPage != null && mainTabControl.TabPages.Contains(pcCardTabPage))
            {
                mainTabControl.TabPages.Remove(pcCardTabPage);
            }

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

        private void downloadMemoryDumpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_serialStack == null || !_isConnected)
            {
                MessageBox.Show("Please connect to the machine first.", "Not Connected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // If memory dump form already exists and is not disposed, just focus it
            if (_memoryDumpForm != null && !_memoryDumpForm.IsDisposed)
            {
                _memoryDumpForm.Focus();
                _memoryDumpForm.BringToFront();
                return;
            }

            // Create new memory dump form
            _memoryDumpForm = new MemoryDumpForm(_serialStack);
            _memoryDumpForm.Show();
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

        private void OnBusyStateChanged(object? sender, BusyStateChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnBusyStateChanged(sender, e)));
                return;
            }

            // Show/hide the busy status label based on the busy state
            toolStripStatusLabelBusy.Visible = e.IsBusy;
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

        private void ClearPcCardFiles()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ClearPcCardFiles()));
                return;
            }

            flowLayoutPanelPcCards.Controls.Clear();
            if (_pcCardFileControls != null)
            {
                foreach (var control in _pcCardFileControls)
                {
                    control?.Dispose();
                }
                _pcCardFileControls.Clear();
            }
        }

        private void AddPcCardFileToUIRealTime(EmbroideryFile file)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => AddPcCardFileToUIRealTime(file)));
                return;
            }

            // Add PC Card tab as soon as the first file appears
            if (pcCardTabPage != null && !mainTabControl.TabPages.Contains(pcCardTabPage))
            {
                mainTabControl.TabPages.Add(pcCardTabPage);
            }

            // Create a new control for this file
            var fileControl = new EmbroideryFileControl();
            fileControl.SetEmbroideryFile(file);
            
            // Add to PC Card flow layout panel (will appear immediately)
            flowLayoutPanelPcCards.Controls.Add(fileControl);
            
            // Track the control
            if (_pcCardFileControls != null)
            {
                _pcCardFileControls.Add(fileControl);
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

        private void UpdateProgress(int current, int total, bool pccard)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateProgress(current, total, pccard)));
                return;
            }

            if (total > 0)
            {
                int percentage = (int)((current * 100) / total);
                toolStripProgressBar.Value = Math.Min(percentage, 100);
                if (pccard) {
                    UpdateStatus($"Loading PC Card files...");
                }
                else
                {
                    UpdateStatus($"Loading embroidery files...");
                }
            }
        }

        private void AddFileToUIRealTime(EmbroideryFile file)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => AddFileToUIRealTime(file)));
                return;
            }

            // Add embroidery tab as soon as the first file appears
            if (embroideryTabPage != null && !mainTabControl.TabPages.Contains(embroideryTabPage))
            {
                mainTabControl.TabPages.Add(embroideryTabPage);
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
            
            // Don't auto-scroll - let user control the scroll position
            // flowLayoutPanelFiles.ScrollControlIntoView(fileControl);
        }

        private void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnConnect_Click(sender, e);
        }

        private void disconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Disconnect();
        }

        private void IconCacheNone_Click(object sender, EventArgs e)
        {
            SetIconCacheMode(IconCacheMode.None);
        }

        private void IconCacheNormal_Click(object sender, EventArgs e)
        {
            SetIconCacheMode(IconCacheMode.Normal);
        }

        private void IconCacheFast_Click(object sender, EventArgs e)
        {
            SetIconCacheMode(IconCacheMode.Fast);
        }

        private void SetIconCacheMode(IconCacheMode mode)
        {
            _iconCacheMode = mode;

            // Update menu item checks
            iconCacheNoneToolStripMenuItem.Checked = (mode == IconCacheMode.None);
            iconCacheNormalToolStripMenuItem.Checked = (mode == IconCacheMode.Normal);
            iconCacheFastToolStripMenuItem.Checked = (mode == IconCacheMode.Fast);

            // Save preference to registry
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath))
                {
                    key.SetValue("IconCacheMode", mode.ToString());
                }
            }
            catch
            {
                // If registry write fails, continue
            }

            UpdateStatus($"Icon cache mode changed to: {mode}");
        }

        private void LoadIconCacheModeFromRegistry()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
                {
                    if (key != null)
                    {
                        string? modeStr = key.GetValue("IconCacheMode") as string;
                        if (!string.IsNullOrEmpty(modeStr) && Enum.TryParse<IconCacheMode>(modeStr, out var mode))
                        {
                            _iconCacheMode = mode;
                        }
                    }
                }
            }
            catch
            {
                // If registry read fails, use default
            }

            // Update menu item checks
            iconCacheNoneToolStripMenuItem.Checked = (_iconCacheMode == IconCacheMode.None);
            iconCacheNormalToolStripMenuItem.Checked = (_iconCacheMode == IconCacheMode.Normal);
            iconCacheFastToolStripMenuItem.Checked = (_iconCacheMode == IconCacheMode.Fast);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private async Task PopulateMachineInfoAsync()
        {
            if (_serialStack == null || machineInfoListView == null)
            {
                return;
            }

            try
            {
                // Clear existing items
                machineInfoListView.Items.Clear();

                // Get firmware information from both sewing machine and embroidery module
                var firmwareInfoTuple = await _serialStack.ReadAllFirmwareInfoAsync();

                if (firmwareInfoTuple == null)
                {
                    UpdateStatus("Failed to retrieve firmware information");
                    return;
                }

                // Store firmware information for later reference
                _sewingMachineFirmwareInfo = firmwareInfoTuple.Value.SewingMachine;
                _embroideryModuleFirmwareInfo = firmwareInfoTuple.Value.EmbroideryModule;

                // Display Sewing Machine firmware info
                if (_sewingMachineFirmwareInfo != null)
                {
                    AddListViewItem("Firmware Version", _sewingMachineFirmwareInfo.Version ?? "Unknown", "Sewing Machine");
                    AddListViewItem("Language", _sewingMachineFirmwareInfo.Language ?? "Unknown", "Sewing Machine");
                    AddListViewItem("Manufacturer", _sewingMachineFirmwareInfo.Manufacturer ?? "Unknown", "Sewing Machine");
                    AddListViewItem("Firmware Date", _sewingMachineFirmwareInfo.Date ?? "Unknown", "Sewing Machine");
                }

                // Display Embroidery Module firmware info
                if (_embroideryModuleFirmwareInfo != null)
                {
                    AddListViewItem("Embroidery Module", "Attached", "Sewing Machine");
                    AddListViewItem("Firmware Version", _embroideryModuleFirmwareInfo.Version ?? "Unknown", "Embroidery Module");
                    AddListViewItem("Manufacturer", _embroideryModuleFirmwareInfo.Manufacturer ?? "Unknown", "Embroidery Module");
                    AddListViewItem("Firmware Date", _embroideryModuleFirmwareInfo.Date ?? "Unknown", "Embroidery Module");
                    
                    // Check if PC Card is attached
                    string pcCardStatus = _embroideryModuleFirmwareInfo.PcCardInserted ? "Inserted" : "Not Inserted";
                    AddListViewItem("PC Card", pcCardStatus, "Embroidery Module");
                }
                else
                {
                    AddListViewItem("Embroidery Module", "Not Attached", "Sewing Machine");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error retrieving machine info: {ex.Message}");
            }
        }

        private void AddListViewItem(string label, string value, string groupName)
        {
            if (machineInfoListView == null)
            {
                return;
            }

            // Find or create the group
            ListViewGroup? group = null;
            foreach (ListViewGroup g in machineInfoListView.Groups)
            {
                if (g.Header == groupName)
                {
                    group = g;
                    break;
                }
            }

            if (group == null)
            {
                group = new ListViewGroup(groupName, groupName);
                machineInfoListView.Groups.Add(group);
            }

            // Create the item
            ListViewItem item = new ListViewItem(new[] { label, value }, group);
            machineInfoListView.Items.Add(item);
        }

        private void UpdateMachineInfoFileCount(int fileCount)
        {
            if (machineInfoListView == null)
            {
                return;
            }

            try
            {
                // Update Embroidery Module file count
                foreach (ListViewItem item in machineInfoListView.Items)
                {
                    if (item.Text == "Number of files" && item.Group?.Header == "Embroidery Module")
                    {
                        item.SubItems[1].Text = fileCount.ToString();
                        break;
                    }
                }

                // Update PC Card file count (same as Embroidery Module for now)
                foreach (ListViewItem item in machineInfoListView.Items)
                {
                    if (item.Text == "Number of files" && item.Group?.Header == "PC Card")
                    {
                        item.SubItems[1].Text = fileCount.ToString();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error updating file count: {ex.Message}");
            }
        }

        private void UpdateSerialStats(object? sender, EventArgs e)
        {
            if (_serialStack == null || machineInfoListView == null)
            {
                return;
            }

            try
            {
                // Read the current counter values from the serial stack
                long bytesSent = _serialStack.BytesSent;
                long bytesReceived = _serialStack.BytesReceived;
                long commandsSent = _serialStack.CommandsSent;
                long commandsReceived = _serialStack.CommandsReceived;

                // Use BeginUpdate to prevent flickering while updating multiple items
                machineInfoListView.BeginUpdate();
                try
                {
                    // Find or create the "Serial Communication" group
                    ListViewGroup? group = null;
                    foreach (ListViewGroup g in machineInfoListView.Groups)
                    {
                        if (g.Header == "Serial Communication")
                        {
                            group = g;
                            break;
                        }
                    }

                    if (group == null)
                    {
                        group = new ListViewGroup("Serial Communication", "Serial Communication");
                        machineInfoListView.Groups.Add(group);
                    }

                    // Update or add the stat items
                    string sessionDisplayText = _serialStack.CurrentSessionState == SessionState.Sewing ? "Sewing Machine" : 
                                               _serialStack.CurrentSessionState == SessionState.Embroidery ? "Embroidery Module" : 
                                               _serialStack.CurrentSessionState.ToString();
                    UpdateOrAddListViewItem("Session", sessionDisplayText, group);
                    UpdateOrAddListViewItem("Bytes Sent", bytesSent.ToString(), group);
                    UpdateOrAddListViewItem("Bytes Received", bytesReceived.ToString(), group);
                    UpdateOrAddListViewItem("Commands Sent", commandsSent.ToString(), group);
                    UpdateOrAddListViewItem("Commands Received", commandsReceived.ToString(), group);
                }
                finally
                {
                    // Always call EndUpdate to resume drawing
                    machineInfoListView.EndUpdate();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating serial stats: {ex.Message}");
            }
        }

        private void UpdateOrAddListViewItem(string label, string value, ListViewGroup group)
        {
            if (machineInfoListView == null)
            {
                return;
            }

            // Find existing item
            ListViewItem? existingItem = null;
            foreach (ListViewItem item in machineInfoListView.Items)
            {
                if (item.Text == label && item.Group == group)
                {
                    existingItem = item;
                    break;
                }
            }

            if (existingItem != null)
            {
                // Only update if the value has changed - this prevents unnecessary redraws and blinking
                if (existingItem.SubItems[1].Text != value)
                {
                    existingItem.SubItems[1].Text = value;
                }
            }
            else
            {
                // Add new item
                ListViewItem newItem = new ListViewItem(new[] { label, value }, group);
                machineInfoListView.Items.Add(newItem);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Stop COM port monitoring
            if (_comPortMonitor != null)
            {
                _comPortMonitor.ComPortsChanged -= ComPortMonitor_ComPortsChanged;
                _comPortMonitor.Dispose();
                _comPortMonitor = null;
            }

            // Close memory dump form if it exists
            if (_memoryDumpForm != null && !_memoryDumpForm.IsDisposed)
            {
                _memoryDumpForm.Close();
            }

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
