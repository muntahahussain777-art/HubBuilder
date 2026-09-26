using System;
using System.Windows.Forms;

namespace HubBuilders
{
    public partial class SampleAdd : Sample
    {
        private bool _closeOnce = false;

        public SampleAdd()
        {
            InitializeComponent();
            WireCloseOnce();
            this.Load += (s, e) =>
            {
                WireCloseOnce();
                WireSaveGuard();
            };
            WireSaveGuard();
        }

        private void WireSaveGuard()
        {
            if (btnSave == null) return;
            btnSave.Click -= btnSave_Click;
            btnSave.Click -= BtnSave_GuardedClick;
            btnSave.Click += BtnSave_GuardedClick;
        }

        private void BtnSave_GuardedClick(object sender, EventArgs e)
        {
            string mod = StaffAccess.ModuleForForm(this);
            // Add form: id often 0 = add, else edit — subclasses handle; gate both save paths lightly
            if (!string.IsNullOrWhiteSpace(mod))
            {
                if (!StaffAccess.Can(mod, "add") && !StaffAccess.Can(mod, "edit"))
                {
                    StaffAccess.Deny(mod, "add");
                    return;
                }
            }
            btnSave_Click(sender, e);
        }

        private void WireCloseOnce()
        {
            if (btnClose == null) return;
            btnClose.DialogResult = DialogResult.None;
            btnClose.Animated = false;
            // Pehli mouse press pe band — Guna Click kabhi focus kha jata hai
            btnClose.MouseDown -= BtnClose_MouseDownOnce;
            btnClose.MouseDown += BtnClose_MouseDownOnce;
        }

        private void BtnClose_MouseDownOnce(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                SafeClose();
        }

        public virtual void btnClose_Click(object sender, EventArgs e)
        {
            SafeClose();
        }

        /// <summary>Har Add form ke Close ke liye — ek hi baar band.</summary>
        protected void SafeClose()
        {
            if (_closeOnce || this.IsDisposed) return;
            _closeOnce = true;
            try
            {
                if (btnClose != null)
                {
                    btnClose.Enabled = false;
                    btnClose.DialogResult = DialogResult.None;
                }
            }
            catch { }
            MainClass.CloseDialog(this);
        }

        public virtual void btnSave_Click(object sender, EventArgs e)
        {
        }

        /// <summary>Ctrl+S = Save, Esc = Close — saari SampleAdd forms.</summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.S))
            {
                if (btnSave != null && btnSave.Enabled && btnSave.Visible)
                {
                    BtnSave_GuardedClick(btnSave, EventArgs.Empty);
                    return true;
                }
            }

            if (keyData == Keys.Escape)
            {
                // Dropdown open ho to pehle woh band ho
                if (ActiveControl is ComboBox cb && cb.DroppedDown)
                    return false;
                SafeClose();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void SampleAdd_Load(object sender, EventArgs e)
        {
        }
    }
}
