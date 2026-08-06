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
    public partial class frmStatementsFormAdd : Sample
    {
        public frmStatementsFormAdd()
        {
            InitializeComponent();
            if (btnClose != null)
            {
                btnClose.DialogResult = DialogResult.None;
                btnClose.Animated = false;
                btnClose.Click += (s, e) => MainClass.CloseDialog(this);
            }
        }
    }
}
