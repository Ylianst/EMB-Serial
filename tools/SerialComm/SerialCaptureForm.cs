using System.IO.Ports;
using System.Text;
using Microsoft.Win32;

namespace SerialComm
{
    public partial class SerialCaptureForm : Form
    {
        private const string RegistryKeyPath = @"Software\SerialCaptureTool";

        private SerialPort? port1;
        private SerialPort? port2;
        private StreamWriter? logWriter;
        private readonly object logLock = new object();
        private readonly object dataProcessingLock = new object();
        private int currentBaudRate = 19200;

        // Command parsing state
        private StringBuilder softwareCommandBuffer = new StringBuilder();
        private StringBuilder machineResponseBuffer = new StringBuilder();
        private List<byte> machineResponseBytes = new List<byte>();
        private string? currentCommand = null;
        private int expectedResponseBytes = 0;
        private bool waitingForResponse = false;
        private DateTime commandStartTime;
        private CommandType currentCommandType = CommandType.Unknown;
        private DateTime lastSoftwareCharTime = DateTime.Now;
        private System.Threading.Timer? commandTimeoutTimer = null;

        // Upload command state
        private bool waitingForUploadData = false;
        private StringBuilder uploadDataBuffer = new StringBuilder();
        private List<byte> uploadDataBytes = new List<byte>();
        private int uploadBytesReceived = 0;

        // Thread control
        private volatile bool isRunning = false;
        private Thread? softwareThread;
        private Thread? machineThread;

        // Buffers for each port
        private readonly byte[] softwareBuffer = new byte[4096];
        private readonly byte[] machineBuffer = new byte[4096];

        // Forwarding control
        private bool forwardingEnabled = false;

        // Settings from UI
        private bool debugMode = false;
        private bool showErrors = false;

        // Selected ports and baud rate
        private string? selectedSoftwarePort = null;
        private string? selectedMachinePort = null;
        private int selectedBaudRate = 19200;

        enum CommandType
        {
            Unknown,
            Read,
            LargeRead,
            Write,
            Session,
            Reset,
            Sum,
            Upload,
            Other
        }

        public SerialCaptureForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Load settings from registry
            LoadSettings();

            // Populate menus
            PopulatePortMenus();
            PopulateBaudRateMenu();

            // Sync checkboxes with menu items
            chkDebugMode.Checked = menuDebugMode.Checked;
            chkShowErrors.Checked = menuShowErrors.Checked;

            UpdateStatus("Ready to start capture", Color.Blue);
        }

        private void LoadSettings()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
                {
                    if (key != null)
                    {
                        selectedSoftwarePort = key.GetValue("SoftwarePort") as string;
                        selectedMachinePort = key.GetValue("MachinePort") as string;
                        selectedBaudRate = (int)(key.GetValue("BaudRate") ?? 19200);

                        bool debug = (int)(key.GetValue("DebugMode") ?? 0) == 1;
                        bool errors = (int)(key.GetValue("ShowErrors") ?? 0) == 1;

                        menuDebugMode.Checked = debug;
                        menuShowErrors.Checked = errors;

                        string? logFile = key.GetValue("LogFile") as string;
                        if (!string.IsNullOrEmpty(logFile))
                        {
                            txtLogFile.Text = logFile;
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors loading settings
            }
        }

        private void SaveSettings()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath))
                {
                    if (selectedSoftwarePort != null)
                        key.SetValue("SoftwarePort", selectedSoftwarePort);
                    if (selectedMachinePort != null)
                        key.SetValue("MachinePort", selectedMachinePort);
                    key.SetValue("BaudRate", selectedBaudRate);
                    key.SetValue("DebugMode", menuDebugMode.Checked ? 1 : 0);
                    key.SetValue("ShowErrors", menuShowErrors.Checked ? 1 : 0);
                    key.SetValue("LogFile", txtLogFile.Text);
                }
            }
            catch
            {
                // Ignore errors saving settings
            }
        }

        private void PopulatePortMenus()
        {
            string[] ports = SerialPort.GetPortNames();

            // Clear existing items
            menuSoftwarePort.DropDownItems.Clear();
            menuMachinePort.DropDownItems.Clear();

            if (ports.Length == 0)
            {
                menuSoftwarePort.DropDownItems.Add("(No ports available)").Enabled = false;
                menuMachinePort.DropDownItems.Add("(No ports available)").Enabled = false;
                return;
            }

            // Add port items to Software Port menu
            foreach (string port in ports)
            {
                var item = new ToolStripMenuItem(port);
                item.Click += (s, e) =>
                {
                    selectedSoftwarePort = port;
                    UpdatePortMenuChecks();
                    SaveSettings();
                };
                menuSoftwarePort.DropDownItems.Add(item);
            }

            // Add port items to Machine Port menu
            foreach (string port in ports)
            {
                var item = new ToolStripMenuItem(port);
                item.Click += (s, e) =>
                {
                    selectedMachinePort = port;
                    UpdatePortMenuChecks();
                    SaveSettings();
                };
                menuMachinePort.DropDownItems.Add(item);
            }

            // Set default selections if not already set
            if (selectedSoftwarePort == null && ports.Length > 0)
                selectedSoftwarePort = ports[0];

            if (selectedMachinePort == null && ports.Length > 1)
                selectedMachinePort = ports[1];
            else if (selectedMachinePort == null && ports.Length > 0)
                selectedMachinePort = ports[0];

            UpdatePortMenuChecks();
        }

        private void UpdatePortMenuChecks()
        {
            // Update Software Port menu checks
            foreach (ToolStripMenuItem item in menuSoftwarePort.DropDownItems)
            {
                item.Checked = (item.Text == selectedSoftwarePort);
            }

            // Update Machine Port menu checks
            foreach (ToolStripMenuItem item in menuMachinePort.DropDownItems)
            {
                item.Checked = (item.Text == selectedMachinePort);
            }
        }

        private void PopulateBaudRateMenu()
        {
            int[] baudRates = { 19200, 57600 };

            foreach (int rate in baudRates)
            {
                var item = new ToolStripMenuItem(rate.ToString());
                item.Click += (s, e) =>
                {
                    selectedBaudRate = rate;
                    UpdateBaudRateMenuChecks();
                    SaveSettings();
                };
                menuSpeed.DropDownItems.Add(item);
            }

            UpdateBaudRateMenuChecks();
        }

        private void UpdateBaudRateMenuChecks()
        {
            foreach (ToolStripMenuItem item in menuSpeed.DropDownItems)
            {
                item.Checked = (item.Text == selectedBaudRate.ToString());
            }
        }

        private void menuDebugMode_CheckedChanged(object sender, EventArgs e)
        {
            chkDebugMode.Checked = menuDebugMode.Checked;
            SaveSettings();
        }

        private void menuShowErrors_CheckedChanged(object sender, EventArgs e)
        {
            chkShowErrors.Checked = menuShowErrors.Checked;
            SaveSettings();
        }

        private void chkDebugMode_CheckedChanged(object sender, EventArgs e)
        {
            menuDebugMode.Checked = chkDebugMode.Checked;
            SaveSettings();
        }

        private void chkShowErrors_CheckedChanged(object sender, EventArgs e)
        {
            menuShowErrors.Checked = chkShowErrors.Checked;
            SaveSettings();
        }

        private void menuSetLogFile_Click(object sender, EventArgs e)
        {
            btnBrowseLog_Click(sender, e);
        }

        private void btnBrowseLog_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "Log Files (*.log)|*.log|Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
                dialog.FileName = txtLogFile.Text;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtLogFile.Text = dialog.FileName;
                    SaveSettings();
                }
            }
        }

        private void btnStartStop_Click(object sender, EventArgs e)
        {
            if (!isRunning)
            {
                StartCapture();
            }
            else
            {
                StopCapture();
            }
        }

        private void StartCapture()
        {
            try
            {
                // Validate selections
                if (string.IsNullOrEmpty(selectedSoftwarePort) || string.IsNullOrEmpty(selectedMachinePort))
                {
                    MessageBox.Show("Please select both Software and Machine ports from the Ports menu.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (selectedSoftwarePort == selectedMachinePort)
                {
                    MessageBox.Show("Software and Machine ports must be different.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Get settings from UI
                string softwarePort = selectedSoftwarePort;
                string machinePort = selectedMachinePort;
                currentBaudRate = selectedBaudRate;
                debugMode = chkDebugMode.Checked;
                showErrors = chkShowErrors.Checked;
                string logFile = txtLogFile.Text;

                // Initialize log file
                logWriter = new StreamWriter(logFile, false, Encoding.UTF8);
                logWriter.AutoFlush = true;
                WriteLog($"Serial Capture Session - {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                WriteLog($"Software: {softwarePort}, Machine: {machinePort}, Initial Baud: {currentBaudRate}");
                WriteLog($"Debug: {debugMode}, Show Errors: {showErrors}");
                WriteLog("");

                // Initialize serial ports
                port1 = new SerialPort(softwarePort, currentBaudRate)
                {
                    DataBits = 8,
                    Parity = Parity.None,
                    StopBits = StopBits.One,
                    Handshake = Handshake.None,
                    ReadTimeout = Timeout.Infinite,
                    WriteTimeout = 500
                };

                port2 = new SerialPort(machinePort, currentBaudRate)
                {
                    DataBits = 8,
                    Parity = Parity.None,
                    StopBits = StopBits.One,
                    Handshake = Handshake.None,
                    ReadTimeout = Timeout.Infinite,
                    WriteTimeout = 500
                };

                // Open ports
                AppendCapture($"Opening Software port ({softwarePort})...");
                port1.Open();
                AppendCapture($"Opening Machine port ({machinePort})...");
                port2.Open();

                // Flush any existing data
                FlushSerialPort(port1, softwarePort);
                FlushSerialPort(port2, machinePort);

                AppendCapture("Both ports opened successfully!");
                AppendCapture("Starting capture...\n");

                // Reset state
                forwardingEnabled = false;
                softwareCommandBuffer.Clear();
                machineResponseBuffer.Clear();
                currentCommand = null;
                waitingForResponse = false;
                waitingForUploadData = false;

                // Start reader threads
                isRunning = true;
                softwareThread = new Thread(() => SoftwareReaderThread(port1, port2));
                softwareThread.Name = "Software Reader";
                softwareThread.IsBackground = true;
                softwareThread.Start();

                machineThread = new Thread(() => MachineReaderThread(port2, port1));
                machineThread.Name = "Machine Reader";
                machineThread.IsBackground = true;
                machineThread.Start();

                // Update UI
                txtLogFile.Enabled = false;
                btnBrowseLog.Enabled = false;
                menuStrip.Enabled = false;
                btnStartStop.Text = "Stop Capture";
                UpdateStatus("Capturing... (waiting for 'R' from Software)", Color.Green);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting capture: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                StopCapture();
            }
        }

        private void StopCapture()
        {
            isRunning = false;

            try
            {
                // Wait for threads to finish
                softwareThread?.Join(1000);
                machineThread?.Join(1000);

                // Close ports
                if (port1 != null && port1.IsOpen)
                {
                    port1.Close();
                    port1.Dispose();
                    port1 = null;
                }

                if (port2 != null && port2.IsOpen)
                {
                    port2.Close();
                    port2.Dispose();
                    port2 = null;
                }

                // Close log
                if (logWriter != null)
                {
                    lock (logLock)
                    {
                        WriteLog("");
                        WriteLog($"Session Ended - {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                        logWriter.Close();
                        logWriter.Dispose();
                        logWriter = null;
                    }
                }

                AppendCapture("\nCapture stopped.");
            }
            catch (Exception ex)
            {
                AppendCapture($"Error during cleanup: {ex.Message}");
            }
            finally
            {
                txtLogFile.Enabled = true;
                btnBrowseLog.Enabled = true;
                menuStrip.Enabled = true;
                btnStartStop.Text = "Start Capture";
                UpdateStatus("Capture stopped", Color.Red);
            }
        }

        private void SoftwareReaderThread(SerialPort sourcePort, SerialPort destinationPort)
        {
            try
            {
                while (isRunning && sourcePort.IsOpen)
                {
                    try
                    {
                        int bytesRead = sourcePort.Read(softwareBuffer, 0, softwareBuffer.Length);
                        if (bytesRead > 0)
                        {
                            ProcessAndForwardData(softwareBuffer, bytesRead, sourcePort, destinationPort, "Software", "Machine");
                        }
                    }
                    catch (TimeoutException) { continue; }
                    catch (InvalidOperationException) { break; }
                    catch (IOException) { break; }
                }
            }
            catch (Exception ex)
            {
                AppendCapture($"ERROR in Software reader thread: {ex.Message}");
            }
        }

        private void MachineReaderThread(SerialPort sourcePort, SerialPort destinationPort)
        {
            try
            {
                while (isRunning && sourcePort.IsOpen)
                {
                    try
                    {
                        int bytesRead = sourcePort.Read(machineBuffer, 0, machineBuffer.Length);
                        if (bytesRead > 0)
                        {
                            ProcessAndForwardData(machineBuffer, bytesRead, sourcePort, destinationPort, "Machine", "Software");
                        }
                    }
                    catch (TimeoutException) { continue; }
                    catch (InvalidOperationException) { break; }
                    catch (IOException) { break; }
                }
            }
            catch (Exception ex)
            {
                AppendCapture($"ERROR in Machine reader thread: {ex.Message}");
            }
        }

        private void ProcessAndForwardData(byte[] buffer, int bytesRead, SerialPort sourcePort, SerialPort destinationPort, string sourceName, string destName)
        {
            lock (dataProcessingLock)
            {
                try
                {
                    // Check if we need to enable forwarding
                    if (!forwardingEnabled && sourceName == "Software")
                    {
                        int rIndex = -1;
                        for (int i = 0; i < bytesRead; i++)
                        {
                            if (buffer[i] == 'R')
                            {
                                rIndex = i;
                                forwardingEnabled = true;
                                AppendCapture("*** Forwarding ENABLED - 'R' received from Software ***\n", Color.Magenta);
                                UpdateStatus("Capturing commands...", Color.Green);
                                break;
                            }
                        }

                        if (rIndex >= 0)
                        {
                            byte[] dataToForward = PrepareDataForForwarding(buffer, rIndex, bytesRead, sourceName);
                            if (dataToForward.Length > 0)
                            {
                                destinationPort.Write(dataToForward, 0, dataToForward.Length);
                                ProcessBytes(dataToForward, sourceName);
                            }
                        }
                        return;
                    }

                    if (!forwardingEnabled && sourceName == "Machine")
                    {
                        return;
                    }

                    if (forwardingEnabled)
                    {
                        byte[] dataToForward = PrepareDataForForwarding(buffer, 0, bytesRead, sourceName);
                        if (dataToForward.Length > 0)
                        {
                            destinationPort.Write(dataToForward, 0, dataToForward.Length);

                            if (waitingForUploadData && sourceName == "Software")
                            {
                                byte[] originalData = new byte[bytesRead];
                                Array.Copy(buffer, 0, originalData, 0, bytesRead);
                                ProcessBytes(originalData, sourceName);
                            }
                            else
                            {
                                ProcessBytes(dataToForward, sourceName);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppendCapture($"ERROR in data transfer: {ex.Message}");
                }
            }
        }

        private byte[] PrepareDataForForwarding(byte[] buffer, int startIndex, int totalBytes, string sourceName)
        {
            int lengthToForward = totalBytes - startIndex;
            byte[] result = new byte[lengthToForward];
            Array.Copy(buffer, startIndex, result, 0, lengthToForward);
            return result;
        }

        private void ProcessBytes(byte[] data, string sourceName)
        {
            foreach (byte b in data)
            {
                ProcessByte(b, sourceName);
            }
        }

        private void ProcessByte(byte dataByte, string sourceName)
        {
            char c = (char)dataByte;

            if (debugMode)
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                string charDisplay = (dataByte >= 32 && dataByte <= 126) ? c.ToString() : $"[0x{dataByte:X2}]";
                Color color = sourceName == "Software" ? Color.Cyan : Color.Yellow;
                AppendCapture($"[{timestamp}] {sourceName[0]}: {charDisplay}", color);
                WriteLog($"[{timestamp}] {sourceName}: {dataByte:X2} ({charDisplay})");
            }

            if (sourceName == "Software")
            {
                if (waitingForUploadData)
                {
                    uploadDataBuffer.Append(c);
                    uploadDataBytes.Add(dataByte);
                    uploadBytesReceived++;

                    if (uploadBytesReceived % 32 == 0 || uploadBytesReceived == 256)
                    {
                        AppendCapture($"  [Upload Progress: {uploadBytesReceived}/256 bytes received]", Color.Gray);
                    }

                    if (uploadBytesReceived >= 256)
                    {
                        waitingForUploadData = false;
                    }
                    return;
                }

                softwareCommandBuffer.Append(c);
                lastSoftwareCharTime = DateTime.Now;
                commandTimeoutTimer?.Dispose();

                string cmd = softwareCommandBuffer.ToString();

                if (IsCommandComplete(cmd))
                {
                    if (cmd == "TrME" || cmd == "TrMEY")
                    {
                        commandTimeoutTimer = new System.Threading.Timer(_ =>
                        {
                            lock (dataProcessingLock)
                            {
                                string currentCmd = softwareCommandBuffer.ToString();
                                if ((currentCmd == "TrME" || currentCmd == "TrMEY") &&
                                    (DateTime.Now - lastSoftwareCharTime).TotalMilliseconds >= 300)
                                {
                                    CompleteCommand(currentCmd);
                                }
                            }
                        }, null, 300, Timeout.Infinite);
                    }
                    else
                    {
                        CompleteCommand(cmd);
                    }
                }
            }
            else // Machine
            {
                bool isErrorChar = (c == 'Q' || c == '?' || c == '!');
                bool isWriteCommandEcho = waitingForResponse && currentCommandType == CommandType.Write && c == '?';

                if (isErrorChar && !isWriteCommandEcho && !waitingForResponse && !waitingForUploadData)
                {
                    if (showErrors)
                    {
                        AppendCapture($"\n<< ERROR: Machine sent '{c}' - resetting state\n", Color.Red);
                        WriteLog($"ERROR: Machine sent '{c}' - resetting state");
                    }

                    softwareCommandBuffer.Clear();
                    machineResponseBuffer.Clear();
                    currentCommand = null;
                    waitingForResponse = false;
                    expectedResponseBytes = 0;
                    currentCommandType = CommandType.Unknown;
                    waitingForUploadData = false;
                    uploadDataBuffer.Clear();
                    uploadBytesReceived = 0;
                    commandTimeoutTimer?.Dispose();
                    commandTimeoutTimer = null;
                    return;
                }

                if (waitingForUploadData == false && uploadBytesReceived == 256 && c == 'O')
                {
                    string uploadData = uploadDataBuffer.ToString();
                    DisplayUploadCommand(currentCommand!, uploadData);

                    machineResponseBuffer.Clear();
                    uploadDataBuffer.Clear();
                    uploadBytesReceived = 0;
                    currentCommand = null;
                    waitingForResponse = false;
                    expectedResponseBytes = 0;
                    currentCommandType = CommandType.Unknown;
                    return;
                }

                if (waitingForResponse)
                {
                    machineResponseBuffer.Append(c);
                    machineResponseBytes.Add(dataByte);

                    if (IsResponseComplete(machineResponseBuffer.ToString(), currentCommandType, expectedResponseBytes))
                    {
                        string response = machineResponseBuffer.ToString();

                        if (currentCommandType == CommandType.Upload && response.EndsWith("OE"))
                        {
                            AppendCapture($"{currentCommand} --> Machine ready (OE), waiting for 256 bytes from Software...", Color.Cyan);
                            WriteLog($"{currentCommand} --> Machine ready (OE), waiting for upload data");

                            waitingForUploadData = true;
                            uploadDataBuffer.Clear();
                            uploadBytesReceived = 0;
                            machineResponseBuffer.Clear();
                            waitingForResponse = false;
                            return;
                        }

                        DisplayCommand(currentCommand!, response, currentCommandType);

                        if (currentCommand == "TrMEJ05")
                        {
                            Task.Run(() => ChangeBaudRate(57600));
                        }

                        machineResponseBuffer.Clear();
                        currentCommand = null;
                        waitingForResponse = false;
                        expectedResponseBytes = 0;
                        currentCommandType = CommandType.Unknown;
                    }
                }
                else
                {
                    machineResponseBuffer.Append(c);
                    string buffer = machineResponseBuffer.ToString();
                    if (buffer.EndsWith("BOSN") || buffer.EndsWith("BOS"))
                    {
                        AppendCapture($"<< Machine: {buffer}", Color.Yellow);
                        machineResponseBuffer.Clear();
                    }
                }
            }
        }

        private void CompleteCommand(string cmd)
        {
            commandTimeoutTimer?.Dispose();
            commandTimeoutTimer = null;

            currentCommand = cmd;
            commandStartTime = DateTime.Now;
            currentCommandType = DetectCommandType(cmd);
            expectedResponseBytes = GetExpectedResponseLength(cmd, currentCommandType);
            waitingForResponse = true;
            machineResponseBytes.Clear();
            softwareCommandBuffer.Clear();
        }

        private bool IsCommandComplete(string cmd)
        {
            if (string.IsNullOrEmpty(cmd)) return false;

            if (cmd.StartsWith("R") && cmd.Length == 7)
                return IsHexString(cmd.Substring(1, 6));

            if (cmd.StartsWith("N") && cmd.Length == 7)
                return IsHexString(cmd.Substring(1, 6));

            if (cmd.StartsWith("PS") && cmd.Length == 6)
                return IsHexString(cmd.Substring(2, 4));

            if (cmd.StartsWith("W") && cmd.Contains("?"))
                return true;

            if (cmd.StartsWith("L") && cmd.Length == 13)
                return IsHexString(cmd.Substring(1, 12));

            if (cmd == "TrMEJ05" || cmd == "TrMEYQ" || cmd == "TrMEY" || cmd == "TrME" || cmd == "RF?" || cmd == "EBYQ")
                return true;

            return false;
        }

        private CommandType DetectCommandType(string cmd)
        {
            if (cmd.StartsWith("R") && cmd.Length == 7) return CommandType.Read;
            if (cmd.StartsWith("N") && cmd.Length == 7) return CommandType.LargeRead;
            if (cmd.StartsWith("PS") && cmd.Length == 6) return CommandType.Upload;
            if (cmd.StartsWith("W")) return CommandType.Write;
            if (cmd.StartsWith("L")) return CommandType.Sum;
            if (cmd == "RF?" || cmd.StartsWith("TrME")) return CommandType.Session;
            return CommandType.Other;
        }

        private int GetExpectedResponseLength(string cmd, CommandType type)
        {
            switch (type)
            {
                case CommandType.Read: return cmd.Length + 64 + 1;
                case CommandType.LargeRead: return cmd.Length + 256 + 1;
                case CommandType.Upload: return cmd.Length + 2;
                case CommandType.Write: return cmd.Length;
                case CommandType.Session:
                    if (cmd == "TrMEYQ") return cmd.Length + 1;
                    return cmd.Length;
                case CommandType.Other:
                    if (cmd == "EBYQ") return cmd.Length + 1;
                    return cmd.Length;
                case CommandType.Sum: return cmd.Length + 8 + 1;
                default: return cmd.Length;
            }
        }

        private bool IsResponseComplete(string response, CommandType type, int expectedLength)
        {
            if (string.IsNullOrEmpty(response)) return false;

            if (response == "Q" || response == "?" || response == "!") return true;

            if (type == CommandType.Sum || type == CommandType.Read || type == CommandType.LargeRead)
                return response.Length >= expectedLength && response[expectedLength - 1] == 'O';

            if (expectedLength > 0)
                return response.Length >= expectedLength;

            return false;
        }

        private void DisplayCommand(string command, string response, CommandType type)
        {
            if (response == "Q" || response == "?" || response == "!")
            {
                if (showErrors)
                {
                    AppendCapture($"{command} --> ERROR: Machine responded with '{response}'", Color.Red);
                    WriteLog($"{command} --> ERROR: Machine responded with '{response}'");
                }
                machineResponseBytes.Clear();
                return;
            }

            if (type == CommandType.Read || type == CommandType.LargeRead)
            {
                if (type == CommandType.LargeRead)
                {
                    // LargeRead returns raw binary data
                    // Response format: <command_echo><256_data_bytes><'O'>
                    // Command is 1 char 'N' (not including the address which is part of data)
                    
                    // Debug: log the actual details
                    WriteLog($"DEBUG: command='{command}' (len={command.Length}), machineResponseBytes.Count={machineResponseBytes.Count}");
                    WriteLog($"DEBUG: First 10 response bytes as hex: {string.Join(" ", machineResponseBytes.Take(10).Select(b => b.ToString("X2")))}");
                    
                    // The command is just "N" (1 char), the address is part of the response data
                    // So we skip 1 byte for 'N', take 256 bytes, and ignore the trailing 'O'
                    int dataStart = 1; // Skip only the 'N' character
                    int dataLength = 256; // We want exactly 256 bytes
                    
                    if (machineResponseBytes.Count < dataStart + dataLength + 1)
                    {
                        WriteLog($"ERROR: Not enough bytes. Expected at least {dataStart + dataLength + 1}, got {machineResponseBytes.Count}");
                        machineResponseBytes.Clear();
                        return;
                    }
                    
                    byte[] dataBytes = new byte[dataLength];
                    for (int i = 0; i < dataBytes.Length; i++)
                    {
                        dataBytes[i] = machineResponseBytes[dataStart + i];
                    }

                    string ascii = GetPrintableAsciiFromBytes(dataBytes);
                    string hex = BytesToHex(dataBytes);

                    AppendCapture($"{command} --> [{dataBytes.Length} bytes]", Color.Cyan);
                    AppendCapture($"   ASCII: {ascii}", Color.Yellow);
                    AppendCapture($"   HEX: {hex}", Color.Gray);

                    WriteLog($"{command} --> [{dataBytes.Length} bytes]");
                    WriteLog($"   ASCII: {ascii}");
                    WriteLog($"   HEX: {hex}");
                }
                else // Read command returns hex-encoded ASCII
                {
                    string data = "";
                    if (response.Length > command.Length)
                    {
                        data = response.Substring(command.Length);
                        if (data.EndsWith("O"))
                            data = data.Substring(0, data.Length - 1);
                    }

                    string hexDisplay = FormatHexWithSpaces(data);
                    string asciiDisplay = DecodeHexString(data);

                    AppendCapture($"{command} -->", Color.Cyan);
                    AppendCapture($"   ASCII: {asciiDisplay}", Color.Yellow);
                    AppendCapture($"   HEX: {hexDisplay}", Color.Gray);

                    WriteLog($"{command} -->");
                    WriteLog($"   ASCII: {asciiDisplay}");
                    WriteLog($"   HEX: {hexDisplay}");
                }

                machineResponseBytes.Clear();
            }
            else if (type == CommandType.Write)
            {
                string writeInfo = "Acknowledged";
                if (command.Length > 7)
                {
                    string address = command.Substring(1, 6);
                    string dataStr = command.Substring(7, command.Length - 8);
                    writeInfo = $"Write {dataStr} to {address}";
                }

                AppendCapture($"{command} --> {writeInfo}", Color.Cyan);
                WriteLog($"{command} --> {writeInfo}");
            }
            else if (type == CommandType.Session)
            {
                if (command != "RF?")
                {
                    AppendCapture($"{command} --> Acknowledged", Color.Cyan);
                    WriteLog($"{command} --> Acknowledged");
                }
            }
            else if (type == CommandType.Sum)
            {
                string data = response.Substring(command.Length);
                if (data.EndsWith("O"))
                    data = data.Substring(0, data.Length - 1);

                string address = command.Substring(1, 6);
                string length = command.Substring(7, 6);

                AppendCapture($"{command} --> Sum starting at {address} with length {length} is {data}", Color.Cyan);
                WriteLog($"{command} --> Sum starting at {address} with length {length} is {data}");
            }
            else
            {
                string extra = "";
                if (response.Length > command.Length)
                    extra = response.Substring(command.Length);

                string responseText = string.IsNullOrEmpty(extra) ? "Acknowledged" : extra;
                AppendCapture($"{command} --> {responseText}", Color.Cyan);
                WriteLog($"{command} --> {responseText}");
            }
        }

        private void DisplayUploadCommand(string command, string uploadData)
        {
            string address = command.Substring(2, 4);
            byte[] dataBytes = uploadDataBytes.ToArray();
            string ascii = GetPrintableAsciiFromBytes(dataBytes);
            string hex = BytesToHex(dataBytes);

            AppendCapture($"{command} --> Upload 256 bytes to address {address}:", Color.Cyan);
            AppendCapture($"   ASCII: {ascii}", Color.Yellow);
            AppendCapture($"   HEX: {hex}", Color.Gray);

            WriteLog($"{command} --> Upload 256 bytes to address {address}:");
            WriteLog($"   ASCII: {ascii}");
            WriteLog($"   HEX: {hex}");
            
            uploadDataBytes.Clear();
        }

        private string GetPrintableAscii(string data)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in data)
            {
                sb.Append(c >= 32 && c <= 126 ? c : '.');
            }
            return sb.ToString();
        }

        private string GetPrintableAsciiFromBytes(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in bytes)
            {
                sb.Append(b >= 32 && b <= 126 ? (char)b : '.');
            }
            return sb.ToString();
        }

        private string BytesToHex(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder(bytes.Length * 3);
            for (int i = 0; i < bytes.Length; i++)
            {
                if (i > 0)
                    sb.Append(' ');
                sb.AppendFormat("{0:X2}", bytes[i]);
            }
            return sb.ToString();
        }

        private string DecodeHexString(string hexString)
        {
            if (hexString.Length % 2 != 0)
                return "[Invalid hex string - odd length]";

            StringBuilder result = new StringBuilder();
            for (int i = 0; i < hexString.Length; i += 2)
            {
                try
                {
                    string hexByte = hexString.Substring(i, 2);
                    byte byteValue = Convert.ToByte(hexByte, 16);
                    char c = (char)byteValue;
                    result.Append(c >= 32 && c <= 126 ? c : '.');
                }
                catch
                {
                    result.Append('?');
                }
            }
            return result.ToString();
        }

        private string FormatHexWithSpaces(string hexString)
        {
            if (hexString.Length % 2 != 0)
                return hexString;

            StringBuilder result = new StringBuilder();
            for (int i = 0; i < hexString.Length; i += 2)
            {
                if (i > 0)
                    result.Append(' ');
                result.Append(hexString.Substring(i, 2));
            }
            return result.ToString();
        }

        private bool IsHexString(string str)
        {
            foreach (char c in str)
            {
                if (!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
                    return false;
            }
            return true;
        }

        private void ChangeBaudRate(int newBaudRate)
        {
            lock (dataProcessingLock)
            {
                try
                {
                    Thread.Sleep(100);

                    AppendCapture($"*** Changing baud rate from {currentBaudRate} to {newBaudRate} ***", Color.Magenta);

                    if (port1 != null && port1.IsOpen)
                        port1.Close();
                    if (port2 != null && port2.IsOpen)
                        port2.Close();

                    Thread.Sleep(100);

                    currentBaudRate = newBaudRate;

                    if (port1 != null)
                    {
                        port1.BaudRate = newBaudRate;
                        port1.Open();
                    }
                    if (port2 != null)
                    {
                        port2.BaudRate = newBaudRate;
                        port2.Open();
                    }

                    AppendCapture($"*** Baud rate changed successfully to {newBaudRate} ***\n", Color.Magenta);
                }
                catch (Exception ex)
                {
                    AppendCapture($"ERROR changing baud rate: {ex.Message}", Color.Red);
                }
            }
        }

        private void FlushSerialPort(SerialPort port, string portName)
        {
            try
            {
                int bytesToRead = port.BytesToRead;
                if (bytesToRead > 0)
                {
                    byte[] buffer = new byte[bytesToRead];
                    int bytesRead = port.Read(buffer, 0, bytesToRead);
                    AppendCapture($"  Flushed {bytesRead} bytes from {portName}");
                }
                port.DiscardInBuffer();
                port.DiscardOutBuffer();
            }
            catch (Exception ex)
            {
                AppendCapture($"  Warning: Error flushing {portName}: {ex.Message}");
            }
        }

        private void WriteLog(string message)
        {
            logWriter?.WriteLine(message);
        }

        private void AppendCapture(string message, Color? color = null)
        {
            if (txtCapture.InvokeRequired)
            {
                txtCapture.Invoke(new Action(() => AppendCapture(message, color)));
                return;
            }

            txtCapture.AppendText(message + Environment.NewLine);
            txtCapture.SelectionStart = txtCapture.Text.Length;
            txtCapture.ScrollToCaret();
        }

        private void UpdateStatus(string message, Color color)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateStatus(message, color)));
                return;
            }

            toolStripStatusLabel.Text = message;
            toolStripStatusLabel.ForeColor = color;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isRunning)
            {
                var result = MessageBox.Show("Capture is still running. Stop and exit?", "Confirm Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    StopCapture();
                }
                else
                {
                    e.Cancel = true;
                }
            }
        }
    }
}
