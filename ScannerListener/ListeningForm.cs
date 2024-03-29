﻿using System;
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
using System.Drawing.Printing;

namespace ScannerListener
{
    public partial class ListeningForm : Form
    {
        string _port = ConfigurationManager.AppSettings["port"];
        string _dbPath = ConfigurationManager.AppSettings["dbPath"];
        string _path = Application.StartupPath;
        string _filename;

        int _totalPrinted = 0;
        int _totalToPrint = 0;

        SerialPort _serialPort = new SerialPort();
        //OleDbConnection _dbConnection = new OleDbConnection();
        Encoding _utf8 = Encoding.UTF8;

        Thread _readThread;
        Product[] _products;

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

            SetStatus("Select a COM port");

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
                    _serialPort.Encoding = Encoding.UTF8;

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

                    stopButton.Visible = true;
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show(
                            "Can't open COM port " + _port + "\r\nDevice is in use",
                            "Unauthorized Access Exception",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                    SetStatus("Can't open device");

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
                    // Set the status message
                    SetStatus("Listening on port " + _serialPort.PortName);

                    string message = _serialPort.ReadTo("@");
                    //Console.WriteLine(message);

                    if (message == "HALLO")
                    {
                        //Console.WriteLine("Sending ready signal");
                        _serialPort.Write("1");
                    }

                    if (message == "START")
                    {
                        SetStatus("Receiving file...");
                        Thread.Sleep(1000);

                        // Start the transfer
                        _serialPort.Write("C");

                        Thread.Sleep(1000);

                        // Answer to the first response
                        Console.WriteLine(_serialPort.ReadByte());
                        _serialPort.Write(((char)6).ToString() + "C");

                        // Clear the input buffer
                        _serialPort.DiscardInBuffer();

                        Thread.Sleep(1000);

                        // Answer to the data
                        _serialPort.Write(((char)6).ToString());
                        Thread.Sleep(100);
                        //_serialPort.Write(((char)21).ToString());
                        Thread.Sleep(100);
                        _serialPort.Write(((char)6).ToString() + "C");
                        Thread.Sleep(1000);

                        // Read the data
                        int count = _serialPort.BytesToRead;
                        //byte[] ByteArray = new byte[count];

                        //while (_serialPort.Read(ByteArray, 0, count));
                        byte[] buffer = new byte[count];
                        int offset = 0;
                        int toRead = count;

                        int read;
                        while (toRead > 0 && (read = _serialPort.Read(buffer, offset, toRead)) > 0)
                        {
                            offset += read;
                            toRead -= read;
                        }
                        if (toRead > 0) throw new EndOfStreamException();

                        Thread.Sleep(1000);

                        Console.WriteLine(buffer.Length);

                        for (double i = Math.Round((double)((buffer.Length - 765) / 1000)); i > 0.0; i--)
                        {
                            // Answer to the closure
                            _serialPort.Write(((char)6).ToString() + "C");
                            Thread.Sleep(1000);
                        }

                        _serialPort.Write(((char)6).ToString());

                        _filename = WriteToFile(buffer, _path);

                        // Read the file and send the data to the printer
                        if (File.Exists(_path + "/files/" + _filename))
                        {
                            //Console.WriteLine("Reading file");
                            SetStatus("Parsing data");

                            int i = 0;

                            string[] file = File.ReadAllLines(Application.StartupPath + "/files/" + _filename);

                            _products = new Product[file.Length];

                            foreach (string line in file)
                            {
                                if (line.Trim() != null && line.Length > 7)
                                {
                                    string productNumber = line.Substring(0, 7);
                                    string productQty = line.Substring(7);

                                    //Console.WriteLine(productNumber);
                                    //Console.WriteLine(productQty);

                                    _products[i] = new Product(productNumber, productQty, "", "", "", true);
                                    i++;
                                }
                            }

                            try
                            {
                                for (int n = 0; n < _products.Length; n++)
                                {
                                    // Add the location and name from the database to the product object

                                    _products[n] = UpdateProducts(_products[n]);
                                }

                                // Print the data
                                PrintData();
                            }
                            catch (InvalidOperationException e)
                            {
                                MessageBox.Show(
                                        e.Message,
                                        "Invalid Operation Exception",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Error
                                    );
                            }
                        } else
                        {
                            Console.WriteLine("File not found");
                        }
                    }
                }
                catch (TimeoutException)
                {
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

            if (!Directory.Exists(_path + "/files"))
            {
                try
                {
                    Directory.CreateDirectory(_path + "/files");
                }
                catch (IOException)
                {
                    Console.WriteLine("[IOException] Could not create directory");

                    MessageBox.Show(
                        "Could not create 'files' directory in '" + _path + "' \r\nNo write rights?",
                        "IOException",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );

                    return "";
                }
            }

            try
            {
                // Save the .dat file to [EXE Location]/files/
                FileStream fs = File.OpenWrite(_path + "/files/" + name);
                SetStatus("Saving file...");

                foreach (byte Byte in ByteArray)
                {
                    // Write only some of the characters to the file
                    if (Byte > 10 && Byte < 58)
                    {
                        fs.WriteByte(Byte);
                    }
                }

                fs.Close();
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("[UnauthorizedAccessException] Could not open file");
                SetStatus("Unable to save file");

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
            if (product != null)
            {
                // Create a new connection instance
                OleDbConnection dbConnection = new OleDbConnection();

                // Set the provider and data source
                dbConnection.ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + _dbPath.Replace("/", "\\") + ";Persist Security Info=False;";

                dbConnection.Open();

                DataSet dataSet = new DataSet();

                OleDbDataAdapter myAdapter = new OleDbDataAdapter();

                OleDbCommand command = new OleDbCommand("SELECT * FROM tblArtikelen WHERE Omnivers_nummer = '" + product.getNumber() + "'", dbConnection);

                myAdapter.SelectCommand = command;
                myAdapter.Fill(dataSet, "tblArtikelen");

                if (dataSet != null)
                {
                    DataRowCollection rowCollection = dataSet.Tables["tblArtikelen"].Rows;

                    foreach (DataRow row in rowCollection)
                    {
                        // Update the location and name in the product object
                        product.setLocation(row["Locatie"].ToString());
                        product.setName(row["Omschrijving1"].ToString());
                        product.setEAN(row["extArtikelcode"].ToString());
                    }
                }

                dbConnection.Close();

                return product;
            }

            return product;
        }

        private void PrintData()
        {
            Console.WriteLine("Printing data");
            SetStatus("Printing data");

            foreach (Product product in _products)
            {
                if (product != null)
                {
                    _totalToPrint++;
                }
            }

            PrintDocument printDocument = new PrintDocument();
            PaperSize paperSize = new PaperSize();

            paperSize.RawKind = (int)PaperKind.A4;

            printDocument.DefaultPageSettings.Landscape = true;
            printDocument.DefaultPageSettings.PaperSize = paperSize;
            printDocument.PrintPage += new PrintPageEventHandler(printDocument_PrintPage);

            printDocument.Print();

            // Sleep for 2 seconds
            Thread.Sleep(2000);

            SetStatus("Done!");

            _totalPrinted = 0;
            _totalToPrint = 0;
        }

        private void printDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics graphic = e.Graphics;

            Font titleFont = new Font("Courier New", 12, FontStyle.Bold);
            Font mainFont = new Font("Courier New", 12);

            SolidBrush brush = new SolidBrush(Color.Black);

            float titleHeight = titleFont.GetHeight();
            float mainHeight = mainFont.GetHeight();

            int startX = 10;
            int startY = 10;
            int offset = 40;
            int count = 0;

            string checkbox = "\u25A1".PadRight(10);
            string header = "Aantal".PadRight(10) + "Controle".PadRight(10) + "Artikel Nr.".PadRight(15) + "Omschrijving".PadRight(45) + "Locatie".PadRight(10) + "EAN".PadRight(16);

            graphic.DrawString(header, titleFont, brush, startX, startY);

            graphic.DrawLine(new Pen(brush), new Point(0, startY + offset), new Point(e.PageBounds.Width, startY + offset));

            offset = offset + (int)titleHeight + 10;

            foreach (Product product in _products)
            {
                if (product != null && product.getPrint() == true && count < 31)
                {
                    string qty = product.getQty().PadRight(10);

                    string number = product.getNumber().PadRight(15);
                    string name = product.getName().PadRight(45);
                    string location = product.getLocation().PadRight(10);
                    string ean = product.getEAN().PadRight(16);

                    string productLine = qty + checkbox + number + name + location + ean;

                    graphic.DrawString(productLine, mainFont, brush, startX, startY + offset);

                    offset = offset + (int)mainHeight + 5;

                    product.setPrint(false);

                    count++;
                }
            }

            _totalPrinted = _totalPrinted + count;

            if (_totalPrinted < _totalToPrint)
            {
                e.HasMorePages = true;
            }
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

            _totalPrinted = 0;

            self.Visible = false;

            SetStatus("Done");
        }
    }
}
