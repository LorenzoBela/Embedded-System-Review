using System;
using System.IO.Ports;
using System.Windows.Forms;

namespace Review
{
    public partial class Form1 : Form
    {
        // Simplest SerialPort object
        private SerialPort sp = new SerialPort();

        public Form1()
        {
            InitializeComponent();
        }

        // Simple Connect button handler (barebones)
        private void btnConnect_Click(object sender, EventArgs e)
        {
            // Kuhanin ang inputs mula sa textbox
            string comPort = txtCom.Text.Trim();
            string baudText = txtBaud.Text.Trim();

            // Basic validation: hindi pwedeng empty at baud dapat integer
            if (string.IsNullOrEmpty(comPort) || !int.TryParse(baudText, out int baudRate))
            {
                MessageBox.Show("Invalid port or baudrate.", "Input required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Kung naka-open na, isara muna para clean state
            if (sp.IsOpen)
                sp.Close();

            // I-set ang port at baud
            sp.PortName = comPort;
            sp.BaudRate = baudRate;

            try
            {
                // Subukang buksan ang serial port
                sp.Open();
                // Simple feedback
                MessageBox.Show($"Connected to {comPort}", "Connected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                lblStatus.Text = $"Status: Connected {comPort} @{baudRate}";
                btnConnect.Enabled = false;
                btnDisconnect.Enabled = true;
            }
            catch (Exception ex)
            {
                // Ipakita lang ang error, hindi komplikado
                MessageBox.Show($"Failed to open {comPort}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Status: Error";
            }
        }

        // Simple Disconnect button handler
        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            try
            {
                if (sp != null && sp.IsOpen)
                {
                    sp.Close();
                }

                MessageBox.Show("Disconnected.", "Disconnected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                lblStatus.Text = "Status: Disconnected";
                btnConnect.Enabled = true;
                btnDisconnect.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to disconnect: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Ensure port closed on exit
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                if (sp != null && sp.IsOpen)
                    sp.Close();
            }
            catch { }

            base.OnFormClosing(e);
        }
    }
}
