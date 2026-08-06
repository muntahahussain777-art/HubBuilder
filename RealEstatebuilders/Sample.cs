using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HubBuilders
{
    public partial class Sample : Form
    {
        public Sample()
        {
            InitializeComponent();
        }

        /// <summary>Child form se bhi MainForm navigation shortcuts (Alt+…) chalein.</summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            bool isNavChord =
                (keyData & Keys.Alt) == Keys.Alt ||
                keyData == Keys.F1 ||
                keyData == (Keys.Control | Keys.N) ||
                (keyData & (Keys.Control | Keys.Shift)) == (Keys.Control | Keys.Shift);

            if (isNavChord && !(this is MainForm) && !this.Modal)
            {
                try
                {
                    var main = MainForm.instance;
                    if (main != null && !main.IsDisposed && main.TryHandleGlobalShortcut(keyData))
                        return true;
                }
                catch { }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
