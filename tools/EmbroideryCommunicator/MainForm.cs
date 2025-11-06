using System.IO.Ports;
using Microsoft.Win32;

namespace EmbroideryCommunicator
{
    public partial class MainForm : Form
    {
        private SerialStack? _serialStack;
        private bool _isConnected = false;
        private string? _selectedComPort = null;
        private static DebugForm? _debugForm = null;
        private static SerialCaptureForm? _serialCaptureForm = null;
        private MemoryDumpForm? _memoryDumpForm = null;
        private EmbroideryViewerForm? _embroideryViewerForm = null;
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
            // Check if serial capture form is open
            if (_serialCaptureForm != null && !_serialCaptureForm.IsDisposed)
            {
                MessageBox.Show("Cannot connect to the machine while the Serial Capture tool is running. Please close the Serial Capture window first.", 
                    "Connection Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

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
                    serialCaptureToolStripMenuItem.Enabled = false;

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

                    // Enable PC Card Refresh menu item if embroidery module is present
                    pcCardRefreshToolStripMenuItem.Enabled = (_embroideryModuleFirmwareInfo != null);

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

                        var embroideryFiles = await _serialStack.ReadEmbroideryFilesAsync(
                            StorageLocation.EmbroideryModuleMemory,
                            true,
                            (current, total) => UpdateProgress(current, total, false),
                            (file) => AddFileToUIRealTime(file),
                            useFastCacheLookup,
                            true
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

                            // Add PC Card tab immediately since PC Card is inserted
                            if (pcCardTabPage != null && !mainTabControl.TabPages.Contains(pcCardTabPage))
                            {
                                mainTabControl.TabPages.Add(pcCardTabPage);
                            }

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
            pcCardRefreshToolStripMenuItem.Enabled = false;
            serialCaptureToolStripMenuItem.Enabled = true;

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

            try
            {
                if (total > 0)
                {
                    toolStripProgressBar.Visible = true;
                    int percentage = (int)((current * 100) / total);
                    toolStripProgressBar.Value = Math.Min(percentage, 100);
                }
                else
                {
                    toolStripProgressBar.Visible = false;
                    toolStripProgressBar.Value = 0;
                }
            }
            catch (Exception) { }
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

        private async void pcCardRefreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_serialStack == null || !_isConnected || _embroideryModuleFirmwareInfo == null)
            {
                return;
            }

            try
            {
                // Refresh PC Card files
                UpdateStatus("Refreshing PC Card files...");
                ShowProgressBar(true, "Refreshing PC Card files...");

                // Clear existing PC Card files
                ClearPcCardFiles();
                _pcCardFileControls = new List<EmbroideryFileControl>();

                // Determine fast cache lookup based on icon cache mode
                bool useFastCacheLookup = (_iconCacheMode == IconCacheMode.Fast);

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
                    // Add PC Card tab at the start since we're refreshing (PC Card must be present to click this menu item)
                    if (pcCardTabPage != null && !mainTabControl.TabPages.Contains(pcCardTabPage))
                    {
                        mainTabControl.TabPages.Add(pcCardTabPage);
                    }

                    if (pcCardFiles.Count > 0)
                    {
                        UpdateStatus($"Loaded {pcCardFiles.Count} PC Card files");
                    }
                    else
                    {
                        UpdateStatus("No files found on PC Card");
                        // Keep the PC Card tab visible even with no files
                    }
                }
                else
                {
                    UpdateStatus("PC Card not detected");

                    // Remove PC Card tab only if PC Card is not detected
                    if (pcCardTabPage != null && mainTabControl.TabPages.Contains(pcCardTabPage))
                    {
                        mainTabControl.TabPages.Remove(pcCardTabPage);
                    }
                }

                // Save the preview cache to registry after refreshing
                await SavePreviewCacheToRegistryAsync();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error refreshing PC Card: {ex.Message}");
                ShowProgressBar(false);
            }
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

        /// <summary>
        /// Prints debug information about a downloaded embroidery file to the debug output.
        /// Outputs extra data and preview data in hexadecimal format.
        /// </summary>
        /// <param name="file">The embroidery file to print debug info for</param>
        private void PrintFileDebugInfo(EmbroideryFile file)
        {
            if (file == null)
            {
                return;
            }

            var debugMessages = new System.Text.StringBuilder();
            debugMessages.AppendLine($"=== Debug Info for File: {file.FileName} ===");

            // Print extra data if present
            if (file.FileExtraData != null && file.FileExtraData.Length > 0)
            {
                debugMessages.AppendLine($"Extra Data Length: {file.FileExtraData.Length} bytes");
                debugMessages.Append("Extra Data (Hex): ");
                debugMessages.AppendLine(BitConverter.ToString(file.FileExtraData).Replace("-", " "));
            }
            else
            {
                debugMessages.AppendLine("Extra Data: None");
            }

            // Print preview data if present
            if (file.PreviewImageData != null && file.PreviewImageData.Length > 0)
            {
                debugMessages.AppendLine($"Preview Data Length: {file.PreviewImageData.Length} bytes");
                debugMessages.Append("Preview Data (Hex): ");
                debugMessages.AppendLine(BitConverter.ToString(file.PreviewImageData).Replace("-", " "));
            }
            else
            {
                debugMessages.AppendLine("Preview Data: None");
            }

            debugMessages.AppendLine("=====================================");

            // Send debug message via the event system
            OnDebugMessage(this, new DebugMessageEventArgs { Message = debugMessages.ToString() });
        }

        /// <summary>
        /// Updates the local file lists and UI after a file has been deleted.
        /// Removes the deleted file from the list and decrements FileId for all subsequent files.
        /// Refreshes all EmbroideryFileControl instances to reflect the changes.
        /// </summary>
        /// <param name="deletedFileId">The FileId of the deleted file</param>
        /// <param name="location">Storage location (EmbroideryModuleMemory or PCCard)</param>
        private void UpdateFileListsAfterDelete(int deletedFileId, StorageLocation location)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateFileListsAfterDelete(deletedFileId, location)));
                return;
            }

            // Determine which list and panel to update
            List<EmbroideryFileControl>? fileControls = location == StorageLocation.EmbroideryModuleMemory 
                ? _embroideryFileControls 
                : _pcCardFileControls;
            
            FlowLayoutPanel? panel = location == StorageLocation.EmbroideryModuleMemory 
                ? flowLayoutPanelFiles 
                : flowLayoutPanelPcCards;

            if (fileControls == null || panel == null)
            {
                return;
            }

            // Find and remove the deleted file control
            EmbroideryFileControl? controlToRemove = null;
            int indexToRemove = -1;

            for (int i = 0; i < fileControls.Count; i++)
            {
                var control = fileControls[i];
                var file = control.GetEmbroideryFile();
                
                if (file != null && file.FileId == deletedFileId)
                {
                    controlToRemove = control;
                    indexToRemove = i;
                    break;
                }
            }

            if (controlToRemove != null && indexToRemove >= 0)
            {
                // Remove from UI
                panel.Controls.Remove(controlToRemove);
                controlToRemove.Dispose();

                // Remove from list
                fileControls.RemoveAt(indexToRemove);

                // Update FileId for all subsequent files (decrement by 1)
                for (int i = indexToRemove; i < fileControls.Count; i++)
                {
                    var control = fileControls[i];
                    var file = control.GetEmbroideryFile();
                    
                    if (file != null)
                    {
                        // Decrement the FileId
                        file.FileId--;
                        
                        // Refresh the control to reflect the new FileId
                        control.SetEmbroideryFile(file);
                    }
                }
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

        private void serialCaptureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // If serial capture form already exists and is not disposed, just focus it
            if (_serialCaptureForm != null && !_serialCaptureForm.IsDisposed)
            {
                _serialCaptureForm.Focus();
                _serialCaptureForm.BringToFront();
                return;
            }

            // Create new serial capture form
            _serialCaptureForm = new SerialCaptureForm();
            
            // Subscribe to FormClosed event to reset the reference when the form is closed
            _serialCaptureForm.FormClosed += (s, args) =>
            {
                _serialCaptureForm = null;
            };
            
            _serialCaptureForm.Show();
        }

        private void embroideryViewerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // If embroidery viewer form already exists and is not disposed, just focus it
            if (_embroideryViewerForm != null && !_embroideryViewerForm.IsDisposed)
            {
                _embroideryViewerForm.Focus();
                _embroideryViewerForm.BringToFront();
                return;
            }

            // Create new embroidery viewer form
            _embroideryViewerForm = new EmbroideryViewerForm();
            
            // Subscribe to FormClosed event to reset the reference when the form is closed
            _embroideryViewerForm.FormClosed += (s, args) =>
            {
                _embroideryViewerForm = null;
            };
            
            _embroideryViewerForm.Show();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (AboutForm aboutForm = new AboutForm())
            {
                aboutForm.ShowDialog(this);
            }
        }

        /// <summary>
        /// Downloads an embroidery file from the machine and opens it in the EmbroideryViewerForm.
        /// Shows progress during download.
        /// </summary>
        /// <param name="embroideryFile">The embroidery file to view (must have FileId and FileName set)</param>
        /// <param name="location">Storage location (EmbroideryModuleMemory or PCCard)</param>
        public async Task ViewEmbroideryFileAsync(EmbroideryFile embroideryFile, StorageLocation location)
        {
            if (_serialStack == null || !_isConnected)
            {
                MessageBox.Show("Not connected to machine", "View Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (embroideryFile == null)
            {
                MessageBox.Show("No file data available", "View Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check if the machine is currently busy
            if (_serialStack.IsBusy)
            {
                MessageBox.Show("Machine is currently busy with another operation. Please wait for the current operation to complete.", 
                    "Machine Busy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Show progress bar
                ShowProgressBar(true, $"Downloading {embroideryFile.FileName}...");

                // Read the file data from the machine with progress callback
                EmbroideryFile? downloadedFile = await _serialStack.ReadEmbroideryFileAsync(
                    location,
                    embroideryFile.FileId,
                    (current, total) => UpdateProgress(current, total, false)
                );

                // Hide progress bar
                ShowProgressBar(false);

                if (downloadedFile == null || downloadedFile.FileData == null)
                {
                    // Check if the failure was due to busy state
                    if (_serialStack.IsBusy)
                    {
                        MessageBox.Show("Machine is currently busy with another operation. Please wait for the current operation to complete.", 
                            "Machine Busy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        MessageBox.Show("Failed to read file from machine", "View Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    return;
                }

                // Populate preview data and other metadata from the already-loaded file controls
                List<EmbroideryFileControl>? sourceControls = location == StorageLocation.EmbroideryModuleMemory
                    ? _embroideryFileControls
                    : _pcCardFileControls;

                if (sourceControls != null)
                {
                    foreach (var control in sourceControls)
                    {
                        var cachedFile = control.GetEmbroideryFile();
                        if (cachedFile != null && cachedFile.FileId == embroideryFile.FileId)
                        {
                            // Copy preview data and other metadata from the cached file
                            downloadedFile.PreviewImageData = cachedFile.PreviewImageData;
                            downloadedFile.FileName = cachedFile.FileName;
                            downloadedFile.FileAttributes = cachedFile.FileAttributes;
                            break;
                        }
                    }
                }

                // Print debug information about the downloaded file
                PrintFileDebugInfo(downloadedFile);

                // Show progress bar
                ShowProgressBar(false, "Download Completed");

                // Open or focus the embroidery viewer form
                if (_embroideryViewerForm == null || _embroideryViewerForm.IsDisposed)
                {
                    _embroideryViewerForm = new EmbroideryViewerForm();
                    
                    // Subscribe to FormClosed event to reset the reference when the form is closed
                    _embroideryViewerForm.FormClosed += (s, args) =>
                    {
                        _embroideryViewerForm = null;
                    };
                }

                // Load the file data into the viewer
                _embroideryViewerForm.LoadFileFromMemory(embroideryFile.FileName, downloadedFile.FileData);
            }
            catch (Exception ex)
            {
                // Hide progress bar on error
                ShowProgressBar(false);
                
                MessageBox.Show($"Error viewing file: {ex.Message}", "View Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Downloads an embroidery file from the machine and saves it to disk.
        /// Shows a SaveFileDialog and displays progress during download.
        /// </summary>
        /// <param name="embroideryFile">The embroidery file to download (must have FileId and FileName set)</param>
        /// <param name="location">Storage location (EmbroideryModuleMemory or PCCard)</param>
        public async Task DownloadEmbroideryFileAsync(EmbroideryFile embroideryFile, StorageLocation location)
        {
            if (_serialStack == null || !_isConnected)
            {
                MessageBox.Show("Not connected to machine", "Download Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (embroideryFile == null)
            {
                MessageBox.Show("No file data available", "Download Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check if the machine is currently busy
            if (_serialStack.IsBusy)
            {
                MessageBox.Show("Machine is currently busy with another operation. Please wait for the current operation to complete.", 
                    "Machine Busy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Show Save As dialog with suggested filename
            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "Embroidery Files (*.exp)|*.exp|All Files (*.*)|*.*";
                saveDialog.DefaultExt = "exp";
                saveDialog.FileName = embroideryFile.FileName + ".exp";
                saveDialog.Title = "Save Embroidery File";

                if (saveDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    // Show progress bar
                    ShowProgressBar(true, $"Downloading {embroideryFile.FileName}...");

                    // Read the file data from the machine with progress callback
                    EmbroideryFile? downloadedFile = await _serialStack.ReadEmbroideryFileAsync(
                        location,
                        embroideryFile.FileId,
                        (current, total) => UpdateProgress(current, total, false)
                    );

                    // Hide progress bar
                    ShowProgressBar(false);

                    if (downloadedFile == null || downloadedFile.FileData == null)
                    {
                        // Check if the failure was due to busy state
                        if (_serialStack.IsBusy)
                        {
                            MessageBox.Show("Machine is currently busy with another operation. Please wait for the current operation to complete.", 
                                "Machine Busy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        else
                        {
                            MessageBox.Show("Failed to read file from machine", "Download Error", 
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        return;
                    }

                    // Populate preview data and other metadata from the already-loaded file controls
                    List<EmbroideryFileControl>? sourceControls = location == StorageLocation.EmbroideryModuleMemory
                        ? _embroideryFileControls
                        : _pcCardFileControls;

                    if (sourceControls != null)
                    {
                        foreach (var control in sourceControls)
                        {
                            var cachedFile = control.GetEmbroideryFile();
                            if (cachedFile != null && cachedFile.FileId == embroideryFile.FileId)
                            {
                                // Copy preview data and other metadata from the cached file
                                downloadedFile.PreviewImageData = cachedFile.PreviewImageData;
                                downloadedFile.FileName = cachedFile.FileName;
                                downloadedFile.FileAttributes = cachedFile.FileAttributes;
                                break;
                            }
                        }
                    }

                    // Print debug information about the downloaded file
                    PrintFileDebugInfo(downloadedFile);

                    // Save the file data to disk
                    File.WriteAllBytes(saveDialog.FileName, downloadedFile.FileData);

                    MessageBox.Show($"File saved successfully to:\n{saveDialog.FileName}", "Download Complete",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    // Hide progress bar on error
                    ShowProgressBar(false);
                    
                    MessageBox.Show($"Error downloading file: {ex.Message}", "Download Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Deletes an embroidery file from the machine after user confirmation.
        /// Shows a warning dialog before deleting and refreshes the UI after successful deletion.
        /// </summary>
        /// <param name="embroideryFile">The embroidery file to delete (must have FileId and FileName set)</param>
        /// <param name="location">Storage location (EmbroideryModuleMemory or PCCard)</param>
        public async Task DeleteEmbroideryFileAsync(EmbroideryFile embroideryFile, StorageLocation location)
        {
            if (_serialStack == null || !_isConnected)
            {
                MessageBox.Show("Not connected to machine", "Delete Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (embroideryFile == null)
            {
                MessageBox.Show("No file data available", "Delete Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check if the machine is currently busy
            if (_serialStack.IsBusy)
            {
                MessageBox.Show("Machine is currently busy with another operation. Please wait for the current operation to complete.", 
                    "Machine Busy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Show confirmation dialog with warning icon
            string locationName = location == StorageLocation.EmbroideryModuleMemory ? "Embroidery Module Memory" : "PC Card";
            DialogResult result = MessageBox.Show(
                $"Are you sure you want to permanently delete '{embroideryFile.FileName}' from {locationName}?\n\nThis operation cannot be undone.",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (result != DialogResult.Yes)
            {
                return;
            }

            try
            {
                // Show progress
                UpdateStatus($"Deleting {embroideryFile.FileName}...");

                // Delete the file from the machine
                bool success = await _serialStack.DeleteEmbroideryFileAsync(location, embroideryFile.FileId);

                if (success)
                {
                    UpdateStatus($"Deleted {embroideryFile.FileName}");

                    // Update local file lists and UI after successful deletion
                    UpdateFileListsAfterDelete(embroideryFile.FileId, location);
                }
                else
                {
                    // Check if the failure was due to busy state
                    if (_serialStack.IsBusy)
                    {
                        MessageBox.Show("Machine is currently busy with another operation. Please wait for the current operation to complete.", 
                            "Machine Busy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete file from machine", "Delete Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    UpdateStatus("Delete failed");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus("Delete error");
                MessageBox.Show($"Error deleting file: {ex.Message}", "Delete Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Uploads an embroidery file to the Embroidery Module memory.
        /// Shows progress during upload and refreshes the UI after successful upload.
        /// </summary>
        /// <param name="embroideryFile">The embroidery file to upload (must have FileName, FileData, and PreviewImageData set)</param>
        public async Task UploadEmbroideryFileAsync(EmbroideryFile embroideryFile)
        {
            if (_serialStack == null || !_isConnected)
            {
                MessageBox.Show("Not connected to machine", "Upload Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (embroideryFile == null)
            {
                MessageBox.Show("No file data available", "Upload Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validate required fields
            if (string.IsNullOrEmpty(embroideryFile.FileName))
            {
                MessageBox.Show("File name is required", "Upload Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (embroideryFile.FileData == null || embroideryFile.FileData.Length == 0)
            {
                MessageBox.Show("File data is empty", "Upload Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (embroideryFile.PreviewImageData == null || embroideryFile.PreviewImageData.Length == 0)
            {
                MessageBox.Show("Preview image data is required", "Upload Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check if the machine is currently busy
            if (_serialStack.IsBusy)
            {
                MessageBox.Show("Machine is currently busy with another operation. Please wait for the current operation to complete.", 
                    "Machine Busy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check if embroidery module is present
            if (_embroideryModuleFirmwareInfo == null)
            {
                MessageBox.Show("Embroidery module not detected. Please ensure the embroidery module is attached and try reconnecting.", 
                    "Embroidery Module Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Set the user file attribute
            embroideryFile.FileAttributes = 0x02;

            try
            {
                // Show progress bar
                ShowProgressBar(true, $"Uploading {embroideryFile.FileName}...");

                // Upload the file to the machine
                bool success = await _serialStack.WriteEmbroideryFileAsync(
                    embroideryFile,
                    StorageLocation.EmbroideryModuleMemory,
                    (current, total) => UpdateProgress(current, total, false)
                );

                // Hide progress bar
                ShowProgressBar(false);

                if (success)
                {
                    UpdateStatus($"Uploaded {embroideryFile.FileName}");

                    // Add the new file to the appropriate list and update filenames if needed
                    await AddUploadedFileAndVerifyAsync(embroideryFile, StorageLocation.EmbroideryModuleMemory);
                }
                else
                {
                    // Check if the failure was due to busy state
                    if (_serialStack.IsBusy)
                    {
                        MessageBox.Show("Machine is currently busy with another operation. Please wait for the current operation to complete.", 
                            "Machine Busy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        MessageBox.Show("Failed to upload file to machine", "Upload Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    UpdateStatus("Upload failed");
                }
            }
            catch (Exception ex)
            {
                // Hide progress bar on error
                ShowProgressBar(false);
                
                UpdateStatus("Upload error");
                MessageBox.Show($"Error uploading file: {ex.Message}", "Upload Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Adds an uploaded file to the appropriate list and verifies the filename.
        /// The machine may have changed the filename, so we do a lightweight refresh to check.
        /// </summary>
        /// <param name="uploadedFile">The file that was uploaded</param>
        /// <param name="location">Storage location where the file was uploaded</param>
        private async Task AddUploadedFileAndVerifyAsync(EmbroideryFile uploadedFile, StorageLocation location)
        {
            if (_serialStack == null || !_isConnected)
            {
                return;
            }

            // Determine which list to work with
            List<EmbroideryFileControl>? fileControls = location == StorageLocation.EmbroideryModuleMemory 
                ? _embroideryFileControls 
                : _pcCardFileControls;
            
            FlowLayoutPanel? panel = location == StorageLocation.EmbroideryModuleMemory 
                ? flowLayoutPanelFiles 
                : flowLayoutPanelPcCards;

            if (fileControls == null || panel == null)
            {
                return;
            }

            // Determine the next FileId by finding the highest FileId in the current list
            int nextFileId = 0;
            foreach (var control in fileControls)
            {
                var file = control.GetEmbroideryFile();
                if (file != null && file.FileId >= nextFileId)
                {
                    nextFileId = file.FileId + 1;
                }
            }

            // Set the FileId for the new file
            uploadedFile.FileId = nextFileId;

            // Add the file to the UI immediately
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    var fileControl = new EmbroideryFileControl();
                    fileControl.SetEmbroideryFile(uploadedFile);
                    panel.Controls.Add(fileControl);
                    fileControls.Add(fileControl);
                }));
            }
            else
            {
                var fileControl = new EmbroideryFileControl();
                fileControl.SetEmbroideryFile(uploadedFile);
                panel.Controls.Add(fileControl);
                fileControls.Add(fileControl);
            }

            // Now do a lightweight refresh to get filenames without preview images
            UpdateStatus("Verifying filename...");
            
            try
            {
                // Read file list without preview images
                var updatedFiles = await _serialStack.ReadEmbroideryFilesAsync(
                    location,
                    false, // No preview images
                    null,  // No progress callback
                    null,  // No real-time callback
                    false  // No fast cache lookup
                );

                if (updatedFiles != null)
                {
                    // Check each file in the updated list and update if filename or attributes changed
                    foreach (var updatedFile in updatedFiles)
                    {
                        // Find the corresponding control by FileId
                        foreach (var control in fileControls)
                        {
                            var currentFile = control.GetEmbroideryFile();
                            if (currentFile != null && currentFile.FileId == updatedFile.FileId)
                            {
                                bool needsUpdate = false;
                                
                                // Check if filename changed
                                if (currentFile.FileName != updatedFile.FileName)
                                {
                                    // Update the filename in the current file object
                                    currentFile.FileName = updatedFile.FileName;
                                    needsUpdate = true;
                                    UpdateStatus($"Filename updated: {uploadedFile.FileName} -> {updatedFile.FileName}");
                                }
                                
                                // Check if file attributes changed
                                if (currentFile.FileAttributes != updatedFile.FileAttributes)
                                {
                                    // Update the attributes in the current file object
                                    currentFile.FileAttributes = updatedFile.FileAttributes;
                                    needsUpdate = true;
                                    UpdateStatus($"File attributes updated for {currentFile.FileName}");
                                }
                                
                                // Refresh the control if anything changed
                                if (needsUpdate)
                                {
                                    control.SetEmbroideryFile(currentFile);
                                }
                                break;
                            }
                        }
                    }
                }

                UpdateStatus("File added successfully");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error verifying filename: {ex.Message}");
            }
        }

        /// <summary>
        /// Refreshes the embroidery files list from the machine.
        /// </summary>
        private async Task RefreshEmbroideryFilesAsync()
        {
            if (_serialStack == null || !_isConnected || _embroideryModuleFirmwareInfo == null)
            {
                return;
            }

            try
            {
                ShowProgressBar(true, "Refreshing embroidery files...");

                // Clear existing files
                ClearEmbroideryFiles();
                _embroideryFileControls = new List<EmbroideryFileControl>();

                // Determine fast cache lookup based on icon cache mode
                bool useFastCacheLookup = (_iconCacheMode == IconCacheMode.Fast);

                var embroideryFiles = await _serialStack.ReadEmbroideryFilesAsync(
                    StorageLocation.EmbroideryModuleMemory,
                    true,
                    (current, total) => UpdateProgress(current, total, false),
                    (file) => AddFileToUIRealTime(file),
                    useFastCacheLookup,
                    true
                );

                ShowProgressBar(false);

                if (embroideryFiles != null)
                {
                    UpdateStatus($"Loaded {embroideryFiles.Count} embroidery files");
                    UpdateMachineInfoFileCount(embroideryFiles.Count);
                    await SavePreviewCacheToRegistryAsync();
                }
                else
                {
                    UpdateStatus("Failed to refresh embroidery files");
                }
            }
            catch (Exception ex)
            {
                ShowProgressBar(false);
                UpdateStatus($"Error refreshing files: {ex.Message}");
            }
        }
    }
}
