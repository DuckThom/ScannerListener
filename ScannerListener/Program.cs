using System;
using System.Windows.Forms;
using System.Configuration;
using System.IO.Ports;
using System.IO;

namespace ScannerListener
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Get a list of serial port names.
            string[] _ports = SerialPort.GetPortNames();
            string _dbPath = ConfigurationManager.AppSettings["dbPath"];

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (!ConfigurationManager.AppSettings.HasKeys())
            {
                _dbPath = createConfig();
            }

            if (_ports.Length > 0)
            {
                if (File.Exists(_dbPath))
                {
                    runApp(_ports);
                }
                else
                {
                    MessageBox.Show(
                        "Database file not found at: " + _dbPath,
                        "Database path invalid",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );

                    createConfig();

                    runApp(_ports);
                }

            }
            else
            {
                MessageBox.Show(
                    "No COM ports were detected on this system",
                    "No COM ports found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        static void runApp(string[] ports)
        {
            Application.Run(new ListeningForm(ports));
        }

        static string createConfig()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.CheckFileExists = true;
            dialog.CheckPathExists = true;
            dialog.Multiselect = false;
            dialog.Title = "Select the database file";
            dialog.Filter = "Database Files |*.accdb;*.mdb";
            dialog.FilterIndex = 0;

            DialogResult clickedOK = dialog.ShowDialog();

            if (clickedOK == DialogResult.OK)
            {
                config.AppSettings.Settings.Remove("dbPath");
                config.AppSettings.Settings.Add("dbPath", dialog.FileName.Replace("\\", "/"));
                config.AppSettings.Settings.Add("port", "");

                config.Save(ConfigurationSaveMode.Full);

                return dialog.FileName;
            } else
            {
                Application.Exit();

                return "";
            }
            
        }
    }
}
