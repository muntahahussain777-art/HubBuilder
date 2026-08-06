using System;
using System.Windows.Forms;

namespace HubBuilders
{
    public partial class ActivationForm : Form
    {
        public ActivationForm()
        {
            InitializeComponent();
            Wire();
        }

        private void Wire()
        {
            btnOk.Click += (s, e) =>
            {
                var input = (txtCode.Text ?? "").Trim();
                if (!ActivationCodes.IsValidPlainCode(input))
                {
                    MessageBox.Show("Invalid code.");
                    return;
                }
                ActivationStore.SaveActivated(input);
                DialogResult = DialogResult.OK;
                Close();
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        }
    }
}
