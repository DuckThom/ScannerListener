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
using System.Data.OleDb;

namespace ScannerListener
{
    public partial class ListeningForm : Form
    {
        string _port = ConfigurationManager.AppSettings["port"];
        string _dbPath = ConfigurationManager.AppSettings["dbPath"];
        string _path = Application.StartupPath;
        string _filename;

        SerialPort _serialPort = new SerialPort();
        OleDbConnection _dbConnection = new OleDbConnection();

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

            SetStatus("Done");

            // Add an on change event handler to the combo box
            portComboBox.SelectedIndexChanged += new System.EventHandler(portComboBox_SelectedIndexChanged);

            if (_port != "")
            {
                Console.WriteLine("Setting preferred COM port");
                portComboBox.SelectedItem = _port;
            }
        }

        private void portComboBox_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;

            if (comboBox.SelectedIndex != -1)
            {
                string port = comboBox.SelectedItem.ToString();

                if (port != "")
                {
                    // Close the port if it is already open
                    if (_serialPort.IsOpen)
                    {
                        Console.WriteLine("[Error] Selected port in use");
                        SetStatus("Selected port in use");
                        return;
                    }

                    // Save the port to file
                    SavePort(port);

                    // Try to start the thread
                    startThread(port);
                }
            }
        }

        private void SavePort(string port)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);

            Console.WriteLine("Saving port to file");

            config.AppSettings.Settings.Remove("port");
            config.AppSettings.Settings.Add("port", port);

            config.Save(ConfigurationSaveMode.Minimal);
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

                    stopButton.Visible = true;
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
            while (_serialPort.IsOpen)
            {
                try
                {
                    //string message = _serialPort.ReadTo("@");
                    // Console.WriteLine(message);
                    string message = "EINDE";

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

                        _filename = WriteToFile(ByteArray, _path);

                        SetStatus("Done");
                    }

                    // Read the file and send the data to the printer
                    if (message == "EINDE")
                    {
                        // Do print st00fs here
                        Console.WriteLine("Reading file");
                        SetStatus("Parsing data");

                        int i = 0;

                        string[] file = File.ReadAllLines(Application.StartupPath + "/files/TEST.Dat");// + _filename);

                        Product[] products = new Product[file.Length];

                        foreach (string line in file)
                        {
                            if (line != null)
                            {
                                string productNumber = line.Substring(0, 12);
                                string productQty = line.Substring(12);

                                Console.WriteLine(productNumber);

                                products[i] = new Product(productNumber, productQty, "", "");
                                i++;
                            }
                        }

                        for (int n = 0; n < products.Length; n++)
                        {
                            // Add the location and name from the database to the product object
                            products[i] = UpdateProducts(products[i]);
                        }

                        // Print the data
                        PrintData(products);
                    }
                }
                catch (TimeoutException) {
                    Console.WriteLine("[TimeoutException] Timeout caught!");
                }
                catch (IOException)
                {
                    Console.WriteLine("[IOException] Probably caused by closed serial port while reading");
                }
                catch (InvalidOperationException)
                {
                    Console.WriteLine("[InvalidOperationException] Probably caused by closed serial port while reading");
                }
            }
        }

        // Write the ByteArray to file
        private string WriteToFile(byte[] ByteArray, string path)
        {
            string name = "GO_" + DateTime.Now.Day + DateTime.Now.Month + DateTime.Now.Year + DateTime.Now.Hour + DateTime.Now.Minute + DateTime.Now.Second + ".DAT";

            try
            {
                FileStream fs = File.OpenWrite(_path + name);
                SetStatus("Saving file...");

                foreach (byte Byte in ByteArray)
                {
                    // Write only some of the characters to the file
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
            if (statusLabel.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetStatus);
                Invoke(d, new object[] { text });
            }
            else
            {
                statusLabel.Text = text;
            }
        }

        private Product UpdateProducts(Product product)
        {
            OleDbConnection _dbConnection = new OleDbConnection();

            _dbConnection.ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + Application.StartupPath + "/files/TestDB.accdb";//_dbPath;

            DataSet dataSet = new DataSet();

            var myAdapter = new OleDbDataAdapter();

            OleDbCommand command = new OleDbCommand("SELECT * FROM tblArtikelen WHERE nummer ='" + product.getNumber() + "'", _dbConnection);

            myAdapter.SelectCommand = command;
            myAdapter.Fill(dataSet, "tblArtikelen");

            DataRowCollection rowCollection = dataSet.Tables["tblArtikelen"].Rows;

            foreach (DataRow row in rowCollection)
            {
                // Update the location and name in the product object
                product.setLocation(row["lokatie"].ToString());
                product.setName(row["naam"].ToString());
            }

            Console.WriteLine(product.getName());

            _dbConnection.Close();

            return product;
        }

        private void PrintData(Product[] products)
        {
            Console.WriteLine("Printing data");
            SetStatus("Printing data");


            SetStatus("Done!");

            // Sleep for 2 seconds
            Thread.Sleep(2000);
        }

        // Stop the listening thread
        private void stopButton_Click(object sender, EventArgs e)
        {
            Button self = (Button)sender;

            portComboBox.SelectedIndex = -1;
            portLabel.Text = "Select a COM port from the dropdown menu";

            SavePort("");

            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }

            self.Visible = false;

            SetStatus("Done");
        }
    }
}
