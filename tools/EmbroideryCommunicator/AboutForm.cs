using System;
using System.Reflection;
using System.Windows.Forms;

namespace EmbroideryCommunicator
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
            LoadApplicationInfo();
        }

        private void LoadApplicationInfo()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            // Get version
            Version? version = assembly.GetName().Version;
            string versionString = version != null ? version.ToString() : "1.0.0.0";

            lblVersion.Text = $"Version {versionString}";
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("https://github.com/Ylianst/EMB-Serial");
            }
            catch (Exception) { }
        }
    }
}
