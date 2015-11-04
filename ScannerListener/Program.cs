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
            string[] ports = SerialPort.GetPortNames();
            string dbPath = ConfigurationManager.AppSettings["dbPath"];

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (ports.Length > 0)
            {
                if (File.Exists(dbPath))
                {
                    Application.Run(new ListeningForm(ports));
                } else
                {
                    MessageBox.Show(
                        "Database file not found at: " + dbPath,
                        "Database path invalid",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
                
            } else
            {
                MessageBox.Show(
                    "No COM ports were detected on this system",
                    "No COM ports found",
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error
                );
            }
        }
    }
}
