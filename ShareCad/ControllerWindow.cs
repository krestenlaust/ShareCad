using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShareCad
{
    public partial class ControllerWindow : Form
    {
        private Networking.NetworkFunction networkRole;

        public event Action<Networking.NetworkFunction> OnActivateShareFunctionality;

        public event Action OnSyncPull;

        public event Action OnSyncPush;

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
            radioButtonGuest.Enabled = false;
            radioButtonHost.Enabled = false;

            OnActivateShareFunctionality?.Invoke(networkRole);
        }

        private void ControlSharecadForm_Load(object sender, EventArgs e)
        {

        }

        private void buttonSyncPush_Click(object sender, EventArgs e)
        {
            OnSyncPush?.Invoke();
        }

        private void buttonSyncPull_Click(object sender, EventArgs e)
        {
            OnSyncPull?.Invoke();
        }
    }
}
