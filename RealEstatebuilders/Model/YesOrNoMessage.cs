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
    public partial class YesOrNoMessage : Sample
    {
        public YesOrNoMessage(string message, string title)
        {
            InitializeComponent();
            lblM.Text = message; // Set the custom message
            this.Text = title; // Set the custom title

            lblM.AutoSize = true;
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            // Optional: Agar aap chahte hain ke thodi padding ho to:
            this.Padding = new Padding(10);
        }

        private void btnYes_Click(object sender, EventArgs e)
        {
            if (this.Modal) this.DialogResult = DialogResult.Yes;
            else this.Close();
        }

        private void btnNo_Click(object sender, EventArgs e)
        {
            if (this.Modal) this.DialogResult = DialogResult.No;
            else this.Close();
        }
    }
}
