﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO; // StreamWriter
using System.IO.Ports; // Serial Ports

namespace ADC_Logger
{
    public partial class Form1 : Form
    {
        string path = null;
        string filePath = System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "\\DataSync\\"; // Make sure you have a "DataSync" folder on Desktop

        StreamWriter sw;
        string buffer = null;
        bool isLogging = false;
        double time = 0;
        double timeInterval;

        List<double> livePlotBuffer;

        int _hours = 0;
        int _minutes = 0;
        int _seconds = 0;

        public Form1(){
            InitializeComponent();
            button1.Enabled = true;  // COM Port scan button
            button2.Enabled = true;  // Connect button
            button3.Enabled = false; // Disconnect button
            button4.Enabled = false; // Start Logging button
            button5.Enabled = false; // Stop Logging button
            button5.Visible = false;
            getAvailablePorts();
            serialPort1.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(DataReceived);
        }

        private void Form1_Load(object sender, EventArgs e){
            // Dummy values to make charts look good at startup
            chart1.Series["ADC"].Points.AddXY(0, 0);
            chart1.Series["ADC"].Points.AddXY(2, 10);
            chart1.Series["ADC"].Points.AddXY(4, 5);
            chart1.Series["ADC"].Points.AddXY(6, 15);
            chart1.Series["ADC"].Points.AddXY(8, 5);
            chart1.Series["ADC"].Points.AddXY(10, 20);
            chart2.Series["Live ADC"].Points.AddXY(2, 90);
            chart2.Series["Live ADC"].Points.AddXY(3, 30);
            chart2.Series["Live ADC"].Points.AddXY(4, 60);
            chart2.Series["Live ADC"].Points.AddXY(5, 40);
            chart2.Series["Live ADC"].Points.AddXY(7, 80);
            chart2.ChartAreas["ChartArea1"].AxisX.LabelStyle.Enabled = false;

            // Default time stamps
            timeInterval = 1.0f/Convert.ToDouble(textBox2.Text);

            // Setup Live Plot
            livePlotBuffer = new List<double>(); // Store incoming data for live plot
            timer2.Interval = 1; // Plot a point every 1ms
            timer2.Enabled = false;
        }

        void getAvailablePorts(){
            // Get the available COM Ports and put them in the ComboBox1
            String[] ports = SerialPort.GetPortNames();
            comboBox1.Items.AddRange(ports);
        }

        private void DataReceived(object sender, SerialDataReceivedEventArgs e){
            if (isLogging){
                try{
                    buffer = serialPort1.ReadLine(); // Format incoming data as "data\n" in CSV
                    sw.WriteLine(buffer); // Write to CSV
                    livePlotBuffer.Add(Convert.ToDouble(buffer.Replace("\n",""))); // Add to Live Plot buffer
                }catch (Exception ex){
                    MessageBox.Show("ERROR: Issues with receiving data from Serial Port!");
                }
            }else{
                try{
                    serialPort1.DiscardInBuffer();
                }catch (Exception ex){
                    MessageBox.Show("ERROR: Issues in discarding serial port input buffer!");
                }
            }
        }

        private void startTimer() {
            // Reset timer values
            _hours = 0;
            _minutes = 0;
            _seconds = 0;

            // Reset timer display values
            hours.Text = "00";
            mins.Text = "00";
            secs.Text = "00";

            // Start timer1
            timer1.Enabled = true;
            timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e){
            // Timer1 event handler
            increaseSeconds();

            // Update timer display
            hours.Text = _hours.ToString("00");
            mins.Text = _minutes.ToString("00");
            secs.Text = _seconds.ToString("00");
        }

        private void increaseSeconds(){
            if (_seconds == 59){
                _seconds = 0;
                increaseMinutes();
            }else{
                _seconds++;
            }
        }

        private void increaseMinutes(){
            if (_seconds == 59){
                _minutes = 0;
                increaseHours();
            }else{
                _minutes++;
            }
        }

        private void increaseHours(){
            _hours++;
        }

        private void button1_Click(object sender, EventArgs e){
            // Refresh COM Ports Button
            comboBox1.Items.Clear();
            getAvailablePorts();
        }

        private void button2_Click(object sender, EventArgs e){
            // Connect to COM Port
            try{
                if (comboBox1.Text == "COM Ports" || comboBox2.Text == "Baud Rate"){
                    MessageBox.Show("Please select COM port settings.");
                }else{
                    // Settings for the serial port
                    serialPort1.PortName = comboBox1.Text;
                    serialPort1.BaudRate = Convert.ToInt32(comboBox2.Text);
                    serialPort1.Parity = Parity.None;
                    serialPort1.StopBits = StopBits.One;
                    serialPort1.DataBits = 8;

                    // Open Serial Port connection
                    try{
                        if (!serialPort1.IsOpen)
                            serialPort1.Open();
                    }catch (Exception ex){
                        MessageBox.Show("ERROR: Cannot open selected Serial Port!");
                    }

                    // Change button access
                    button2.Enabled = false; // Connect button
                    button3.Enabled = true;  // Disconnect button
                    button4.Enabled = true;  // Start Logging button
                    button5.Enabled = true;  // Stop Logging button
                }
            }
            catch (UnauthorizedAccessException){
                MessageBox.Show("ERROR: Unauthorized Access to COM Port!");
            }
        }

        private void button3_Click(object sender, EventArgs e){
            // Disconnect from COM Port
            serialPort1.Close();
            button2.Enabled = true;  // Connect button
            button3.Enabled = false; // Disconnect button
            button4.Enabled = false; // Start Logging button
            button5.Enabled = false; // Stop Logging button
            button5.Visible = false;
        }

        private void button4_Click(object sender, EventArgs e){
            // Start Logging
            time = 0;
            timeInterval = 1.0f / Convert.ToDouble(textBox2.Text);
            chart2.Series["Live ADC"].Points.Clear();
            timer2.Enabled = true;
            timer2.Start();

            // Determine the name of the output CSV file
            if (String.IsNullOrEmpty(textBox1.Text)){
                path = filePath + "DefaultNoNameOutputFile.csv";
            }else{
                path = filePath + textBox1.Text + ".csv";
            }

            // Declare a new StreamWriter
            try{
                sw = new StreamWriter(path);
                isLogging = true;
                startTimer();
            }
            catch (Exception ex){
                MessageBox.Show("ERROR: Unable to write to chosen file location!");
            }

            button4.Enabled = false; // Turn off Start Logging button
            button4.Visible = false;
            button5.Enabled = true;  // Turn on Stop Logging button
            button5.Visible = true;
        }

        private void button5_Click(object sender, EventArgs e){
            // Stop Logging button
            isLogging = false;
            sw.Close();
            timer1.Stop();
            timer1.Enabled = false;
            timer2.Stop();
            timer2.Enabled = false;
            livePlotBuffer.Clear(); // Clear live plot buffer
            button4.Enabled = true;  // Turn off Stop Logging button
            button4.Visible = true;
            button5.Enabled = false; // Turn on Start Logging button
            button5.Visible = false;
        }

        private void button6_Click(object sender, EventArgs e){
            // Browse CSV file to load into DataGridView
            try{
                // Select a CSV file
                openFileDialog1.ShowDialog();
                string fn = openFileDialog1.FileName;
                string readFile = File.ReadAllText(fn);
                string[] line = readFile.Split('\n');

                // Clear existing data in DataGridView
                dataGridView1.Rows.Clear();
                dataGridView1.Refresh();

                // Add columns to Data Grid
                int count = 1; // Extra column for Timestamps
                foreach (string s1 in line[0].Split(',')) {
                    count++;
                }
                dataGridView1.ColumnCount = count;

                // Add rows to Data Grid
                string entry;
                time = 0;
                timeInterval = 1.0f / Convert.ToDouble(textBox2.Text);
                foreach (string s2 in readFile.Split('\n')) {
                    if (s2 != "") {
                        entry = time.ToString() + ',' + s2; // Row = "Timestamp,data\n"
                        dataGridView1.Rows.Add(entry.Split(','));

                        time += timeInterval;
                        entry = "";
                    }
                }

                // Show row numbers
                foreach (DataGridViewRow row in dataGridView1.Rows) {
                    row.HeaderCell.Value = String.Format("{0}", row.Index + 1);
                }
            }
            catch (Exception ex) {
                MessageBox.Show("ERROR: CSV file was not selected!");
            }
        }

        private void button7_Click(object sender, EventArgs e){
            // Plot new data in DataGridView in the chart
            int rowCount = dataGridView1.RowCount - 1; // Remove extra row
            double c1 = 0, c2 = 0;

            // Clear existing datapoints in the plot
            chart1.Series["ADC"].Points.Clear();

            // Start plotting points from DataGridView
            for (int i = 1; i < rowCount; ++i) {
                c1 = Convert.ToDouble(dataGridView1.Rows[i].Cells[0].Value);
                c2 = Convert.ToDouble(dataGridView1.Rows[i].Cells[1].Value);
                chart1.Series["ADC"].Points.AddXY(c1, c2);
            }
        }

        private void timer2_Tick(object sender, EventArgs e){
            if (isLogging && livePlotBuffer.Count != 0) {
                chart2.Series["Live ADC"].Points.AddY(livePlotBuffer[0]);
                livePlotBuffer.Clear();
            }
        }

    }
}
