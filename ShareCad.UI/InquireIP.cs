using System;
using System.Net;
using System.Windows.Forms;

namespace ShareCad.UI
{
    public partial class InquireIP : Form
    {
        public IPAddress IP { get; private set; }
        public int Port { get; private set; }

        public InquireIP()
        {
            InitializeComponent();
            AcceptButton = buttonOk;
        }

        private void InquireIP_Load(object sender, EventArgs e)
        {
            TryParseIPAddress();
        }

        public DialogResult Show(string title)
        {
            Text = title;
            return ShowDialog();
        }

        private void textBoxIP_TextChanged(object sender, EventArgs e)
        {
            TryParseIPAddress();
        }

        private void TryParseIPAddress()
        {
            bool parseResult = IPAddress.TryParse(textBoxIP.Text, out IPAddress ip);
            bool parsePort = int.TryParse(textBoxPort.Text, out int port);
            bool parsed = parseResult && parsePort;

            labelParseResult.Visible = !parsed;
            buttonOk.Enabled = parsed;

            if (parsed)
            {
                IP = ip;
                Port = port;
            }
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
