using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;

namespace ScannerListener
{
    public partial class ListeningForm : Form
    {
        string port = ConfigurationManager.AppSettings["port"];
        bool _continue = false;
        string _filename;
        SerialPort _serialPort = new SerialPort();
        Thread _readThread;

        delegate void SetTextCallback(string text);

        public ListeningForm(Array ports)
        {
            InitializeComponent();

            // Display each port name to the console.
            foreach (string port in ports)
            {
                // Add a new item to the combo box
                portComboBox.Items.Add(port);
            }

            // Add an on change event handler to the combo box
            portComboBox.SelectedIndexChanged += new System.EventHandler(portComboBox_SelectedIndexChanged);

            SetStatus("Done");
        }

        private void portComboBox_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;

            // Stop a loop that might be running
            _continue = false;
            
            // Close the port if it is already open
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }

            // Try to start the thread
            startThread(comboBox.SelectedItem.ToString());
        }

        private void startThread(string port)
        {
            for (int i = 0; i < 5; i++)
            {
                _readThread = new Thread(Read);

                try
                {
                    // Create a new SerialPort object with default settings.
                    _serialPort.BaudRate = 57600;
                    _serialPort.WriteTimeout = 5000; // 5 seconds
                    _serialPort.ReadTimeout = 5000; // 5 seconds
                    _serialPort.RtsEnable = true;

                    // Allow the user to set the appropriate properties.
                    _serialPort.PortName = port;

                    // Open the port
                    _serialPort.Open();

                    // Enable the loop
                    _continue = true;

                    // Set the thread as background
                    _readThread.IsBackground = true;

                    // Start the thread
                    _readThread.Start();

                    // Set the port label text
                    portLabel.Text = port;

                    // Stop the for loop
                    i = 6;

                    // Set the status message
                    SetStatus("Listening on port " + port);
                }
                catch (UnauthorizedAccessException)
                {
                    Console.WriteLine("[UnauthorizedAccessException] Can't open device");

                    _serialPort = null; // Reset the serial port variable
                    _readThread = null; // Reset the thread port variable

                    // Wait 1 second
                    Thread.Sleep(1000);
                }
            }
        }

        private void Read()
        {
            while (_continue)
            {
                try
                {
                    string message = _serialPort.ReadTo("@");
                    // Console.WriteLine(message);

                    if (message == "HALLO")
                    {
                        Console.WriteLine("Sending ready signal");
                        _serialPort.Write("1");
                        SetStatus("Receiving file...");
                    }

                    if (message == "START")
                    {
                        Thread.Sleep(1000);

                        // Start the transfer
                        _serialPort.Write("C");

                        Thread.Sleep(1000);

                        // Answer to the first response
                        _serialPort.Write(((char)6).ToString() + "C");

                        // Clear the input buffer
                        _serialPort.DiscardInBuffer();

                        Thread.Sleep(1000);

                        // Answer to the data
                        _serialPort.Write(((char)6).ToString());
                        Thread.Sleep(100);
                        _serialPort.Write(((char)21).ToString());
                        Thread.Sleep(100);
                        _serialPort.Write(((char)6).ToString() + "C");
                        Thread.Sleep(1000);

                        // Read the data
                        int count = _serialPort.BytesToRead;
                        byte[] ByteArray = new byte[count];
                        _serialPort.Read(ByteArray, 0, count);

                        // Answer to the closure
                        _serialPort.Write(((char)6).ToString());

                        _filename = WriteToFile(ByteArray);

                        SetStatus("Done");
                    }
                }
                catch (TimeoutException) {
                    Console.WriteLine("[TimeoutException] Timeout caught!");
                }
            }
        }

        // Write the ByteArray to file
        private string WriteToFile(byte[] ByteArray)
        {
            string name = "GO_" + DateTime.Now.Day + DateTime.Now.Month + DateTime.Now.Year + DateTime.Now.Hour + DateTime.Now.Minute + DateTime.Now.Second + ".DAT";
            string path = ConfigurationManager.AppSettings["path"];

            try
            {
                FileStream fs = File.OpenWrite(path + name);
                SetStatus("Saving file...");

                foreach (byte Byte in ByteArray)
                {
                    if (Byte != 0 && Byte < 60)
                    {
                        fs.WriteByte(Byte);
                    }
                }

                fs.Close();
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("[UnauthorizedAccessException] Could not open file");

                return "";
            }

            return name;
        }

        private void SetStatus(string text)
        {
            if (this.statusLabel.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetStatus);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.statusLabel.Text = text;
            }
        }
    }
}
