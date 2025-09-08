using System;
using System.IO.Ports;
using System.Windows.Forms;

namespace Review
{
    public partial class Form1 : Form
    {
        // SerialPort instance para sa komunikasyon kay Arduino
        // Ginagamit ito para mag-open ng serial connection, magbasa at magsulat ng data
        private SerialPort _serial;

        public Form1()
        {
            InitializeComponent();
            // Initialize ang SerialPort object dito
            // Bakit dito? Para ready siya gamitin sa buong lifetime ng form instance.
            _serial = new SerialPort();
        }

        // Handler para sa Connect button click
        // Dito natin pini-parse ang user input (COM at baud), kino-configure ang SerialPort,
        // binubura ang log (para new session) at kino-subscribe ang DataReceived event
        private void btnConnect_Click(object sender, EventArgs e)
        {
            // Kukunin natin ang COM port at baud rate mula sa mga textbox
            // Taglish explanation: sinisigurado muna natin na meron talagang laman ang input bago mag-proceed
            string com = txtCom.Text.Trim();
            if (string.IsNullOrEmpty(com))
            {
                // Ipapakita sa user na kailangan maglagay ng COM port
                // MessageBox para user feedback kung may missing input
                MessageBox.Show("Please enter COM port (e.g., COM3)", "Input required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validate baud rate: kailangan numeric ito
            // Kung hindi numeric, hindi pwedeng mag-open ng serial connection
            if (!int.TryParse(txtBaud.Text.Trim(), out int baud))
            {
                // Inform the user na invalid ang baud rate input
                MessageBox.Show("Please enter a valid baud rate (e.g., 9600)", "Input required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Setup ng serial port gamit ang user-provided values
                // Unsubscribe muna at isara ang port kung naka-open na para maiwasan ang exception
                if (_serial.IsOpen)
                {
                    // Bago isara, tanggalin ang DataReceived handler para hindi tumawag habang nagsasara
                    _serial.DataReceived -= Serial_DataReceived;
                    _serial.Close();
                }

                // I-set ang properties ng SerialPort bago mag-open
                // PortName = COM port; BaudRate = komunikasyon speed
                // Parity, DataBits, StopBits, Handshake ay default na standard para sa karamihan ng Arduino sketches
                _serial.PortName = com;
                _serial.BaudRate = baud;
                _serial.Parity = Parity.None;
                _serial.DataBits = 8;
                _serial.StopBits = StopBits.One;
                _serial.Handshake = Handshake.None;
                // Timeout values: para hindi mag-hang ang read/write operations nang sobra
                _serial.ReadTimeout = 1000;
                _serial.WriteTimeout = 1000;

                // Clear log para makita agad ng user ang bagong incoming data mula sa simula
                txtLog.Clear();

                // Buksan ang serial port - dito talaga nagkakaroon ng physical connection sa Arduino via COM
                _serial.Open();

                // Subscribe sa DataReceived event para makatanggap ng incoming data asynchronously
                // DataReceived ay tatakbo sa background thread, kaya kailangan natin ng Invoke kapag ia-update UI
                _serial.DataReceived += Serial_DataReceived;

                // Update UI state: ipakita na connected at i-disable ang connect button para maiwasan duplicate opens
                lblStatus.Text = $"Status: Connected to {com} @ {baud}";
                btnConnect.Enabled = false;
                btnDisconnect.Enabled = true;
            }
            catch (Exception ex)
            {
                // Simpleng error handling: ipapakita sa user ang exception message
                // Taglish: para malaman ng user bakit hindi nag-connect (e.g., port in use, invalid COM)
                MessageBox.Show("Connection failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Status: Error";
            }
        }

        // Handler para sa Disconnect button click
        // Dito isinasara natin ang serial port at inuun-update ang UI
        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            // Taglish: kung nakabukas ang port, isara natin at tanggalin ang event handler
            try
            {
                if (_serial != null && _serial.IsOpen)
                {
                    // Unsubscribe muna para hindi tumanggap ng bagong data habang nagsasara
                    _serial.DataReceived -= Serial_DataReceived;
                    _serial.Close();
                }

                // I-update ang UI para ipakita na disconnected na
                lblStatus.Text = "Status: Disconnected";
                btnConnect.Enabled = true;
                btnDisconnect.Enabled = false;
            }
            catch (Exception ex)
            {
                // Kung may exception habang nagdi-disconnect, ipapakita sa user
                MessageBox.Show("Error disconnecting: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // DataReceived event handler - tumatakbo ito sa background thread, hindi sa UI thread
        // Dito natin binabasa ang incoming serial data at ipinapasa sa AppendLog para i-update ang UI
        private void Serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                // ReadExisting babasahin lahat ng available na data bilang string
                // Ito simple approach lang; pwedeng palitan ng ReadLine kung may newline-terminated messages
                string data = _serial.ReadExisting();
                if (!string.IsNullOrEmpty(data))
                {
                    // Append sa log nang thread-safe gamit ang helper function
                    AppendLog(data);
                }
            }
            catch
            {
                // Ignore read errors para hindi mag-crash ang app sa simpleng demo
                // Sa production, mas maganda mag-log ng error at mag-handle ng specific exceptions
            }
        }

        // Helper para mag-append ng text sa txtLog sa thread-safe na paraan
        // Kung hindi tayo sa UI thread, gagamitin ang Invoke para mag-execute sa UI thread
        private void AppendLog(string text)
        {
            if (txtLog.InvokeRequired)
            {
                // Kung background thread, mag-Invoke tayo para maka-access ng safe sa UI control
                txtLog.Invoke(new Action(() =>
                {
                    // AppendText hindi nagre-replace; pinupuno lang ang existing log
                    txtLog.AppendText(text);
                }));
            }
            else
            {
                // Kung nasa UI thread na, diretso append
                txtLog.AppendText(text);
            }
        }

        // Siguraduhing isara ang serial port kapag nagsasara ang form
        // Para mai-release ang COM port resource at maiwasang mag-leak
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_serial != null)
            {
                try
                {
                    if (_serial.IsOpen)
                    {
                        // Alisin muna ang event handler bago magsara
                        _serial.DataReceived -= Serial_DataReceived;
                        _serial.Close();
                    }
                }
                catch
                {
                    // Ignore errors dito para hindi pumigil sa form closing; sa mas complex na app, mag-log nalang
                }

                // Dispose para i-release lahat ng resources ng SerialPort object
                _serial.Dispose();
                _serial = null;
            }

            base.OnFormClosing(e);
        }
    }
}
