using System;
using System.Drawing;
using System.Net;
using System.Windows.Forms;

namespace ShareCad
{
    public partial class ControllerWindow : Form
    {
        private Networking.NetworkFunction networkRole;

        public event Action<Networking.NetworkFunction, IPAddress> OnActivateShareFunctionality;


        public ControllerWindow()
        {
            InitializeComponent();
        }

        private void ControllerWindow_Load(object sender, EventArgs e)
        {
        }

        private void ApplyRadioButtonStyle(RadioButton radioButton)
        {
            if (radioButton.Checked)
            {
                radioButton.BackColor = Color.FromArgb(192, 255, 192);
            }
            else
            {
                radioButton.BackColor = SystemColors.Control;
            }
        }

        private void RadioButton_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton radioButton = (RadioButton)sender;

            ApplyRadioButtonStyle(radioButton);

            if (!radioButton.Checked)
            {
                return;
            }

            switch (radioButton.Text)
            {
                case "Vært":
                    networkRole = Networking.NetworkFunction.Host;
                    break;
                case "Gæst":
                    networkRole = Networking.NetworkFunction.Guest;
                    break;
                default:
                    break;
            }
        }

        private void buttonActivateNetworking_Click(object sender, EventArgs e)
        {
            if (!IPAddress.TryParse(textBoxGuestTargetIP.Text, out IPAddress targetAddress))
            {
                MessageBox.Show("Ugyldig IP addresse indtastet");
                return;
            }

            radioButtonGuest.Enabled = false;
            radioButtonHost.Enabled = false;
            buttonActivateNetworking.Enabled = false;

            OnActivateShareFunctionality?.Invoke(networkRole, targetAddress);
        }

        private void ControlSharecadForm_Load(object sender, EventArgs e)
        {

        }

        private void radioButtonGuest_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
