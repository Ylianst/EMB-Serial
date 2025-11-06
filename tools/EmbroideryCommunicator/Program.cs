namespace EmbroideryCommunicator
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            // Check if a .exp file was provided as an argument
            string? expFilePath = null;
            foreach (string arg in args)
            {
                if (File.Exists(arg) && Path.GetExtension(arg).Equals(".exp", StringComparison.OrdinalIgnoreCase))
                {
                    expFilePath = arg;
                    break;
                }
            }

            // If an .exp file was provided, open it directly in EmbroideryViewerForm
            if (!string.IsNullOrEmpty(expFilePath))
            {
                try
                {
                    byte[] fileData = File.ReadAllBytes(expFilePath);
                    string fileName = Path.GetFileName(expFilePath);
                    
                    var embroideryViewerForm = new EmbroideryViewerForm();
                    embroideryViewerForm.LoadFileFromMemory(fileName, fileData);
                    Application.Run(embroideryViewerForm);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading file: {ex.Message}", "Load Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                // Normal mode: show MainForm
                // Check if debug mode is enabled via command-line argument
                bool debugMode = args.Contains("-debug", StringComparer.OrdinalIgnoreCase);
                Application.Run(new MainForm(debugMode));
            }
        }
    }
}
