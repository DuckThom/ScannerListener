using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO.Ports;

namespace ScannerListener
{
    public partial class ListeningForm : Form
    {
        bool _continue = false;
        SerialPort _serialPort;
        Thread readThread;

        public ListeningForm(Array ports)
        {
            InitializeComponent();

            // Display each port name to the console.
            foreach (string port in ports)
            {
                // Add a new item to the combo box
                portComboBox.Items.Add(port);
            }
            test("COM3");
            // Add an on change event handler to the combo box
            //portComboBox.SelectedIndexChanged += new System.EventHandler(portComboBox_SelectedIndexChanged);
        }

        //private void portComboBox_SelectedIndexChanged(object sender, System.EventArgs e)
        private void test(string port)
        {
            //ComboBox comboBox = (ComboBox)sender;

            readThread = new Thread(Read);

            try
            {
                portLabel.Text = port;//comboBox.SelectedItem.ToString();

                // Create a new SerialPort object with default settings.
                _serialPort = new SerialPort();
                _serialPort.BaudRate = 57600;
                _serialPort.WriteTimeout = 5000; // 5 seconds

                // Allow the user to set the appropriate properties.
                _serialPort.PortName = port;//comboBox.SelectedItem.ToString();

                _serialPort.Open();
                _continue = true;
                readThread.Start();
            } catch (UnauthorizedAccessException)
            {
                Console.WriteLine("[UnauthorizedAccessException] Can't open device");

                _serialPort = null;
                readThread = null;
            }
        }

        private void Read()
        {
            while (_continue)
            {
                try
                {
                    string message = _serialPort.ReadTo("@");
                    Console.WriteLine(message);

                    if (message == "HALLO")
                    {
                        Console.WriteLine("Sending ready signal");
                        //_serialPort.WriteLine("1");
                        _serialPort.WriteLine("1");
                    }

                    if (message == "START")
                    {
                        Console.WriteLine("Receiving shit");

                        _serialPort.WriteLine("2");
                    }
                }
                catch (TimeoutException) {
                    Console.WriteLine("[TimeoutException] Timeout caught!");
                }
            }
        }
    }
}
