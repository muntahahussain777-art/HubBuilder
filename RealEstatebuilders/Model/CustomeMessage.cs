using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HubBuilders.Model
{
    public partial class CustomeMessage : Sample
    {
        public CustomeMessage(string message, string title)
        {
            InitializeComponent();
            lblM.Text = message;
            this.Text = title;  // Set the title of the message box form

            lblM.AutoSize = true;
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            // Optional: Agar aap chahte hain ke thodi padding ho to:
            this.Padding = new Padding(10);
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (this.Modal) this.DialogResult = DialogResult.OK; else this.Close();
        }
    }
}
