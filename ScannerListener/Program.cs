using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using System.IO.Ports;

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

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (ports.Length > 0)
            {
                Application.Run(new ListeningForm(ports));
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
