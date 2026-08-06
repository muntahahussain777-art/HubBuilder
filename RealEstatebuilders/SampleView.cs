using System;
using System.Windows.Forms;

namespace HubBuilders
{
    public partial class SampleView : Sample
    {
        public SampleView()
        {
            InitializeComponent();
            // Designer Click hata kar guarded wrap — Close click-through se Add dobara na khule
            if (btnAdd != null)
            {
                btnAdd.Click -= btnAdd_Click;
                btnAdd.Click += BtnAdd_GuardedClick;
            }
        }

        private void BtnAdd_GuardedClick(object sender, EventArgs e)
        {
            if (MainClass.UiClicksBlocked) return;
            btnAdd_Click(sender, e);
        }

        /// <summary>MainForm se Ctrl+N — bina andar click kiye Add.</summary>
        public bool TriggerAddShortcut()
        {
            if (btnAdd == null || !btnAdd.Enabled || !btnAdd.Visible) return false;
            BtnAdd_GuardedClick(btnAdd, EventArgs.Empty);
            return true;
        }

        public virtual void btnAdd_Click(object sender, EventArgs e)
        {
        }

        public virtual void txtSearch_TextChanged(object sender, EventArgs e)
        {
        }

        public virtual void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
        }

        /// <summary>Ctrl+N = Add button — saari SampleView forms.</summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.N))
            {
                if (btnAdd != null && btnAdd.Enabled && btnAdd.Visible)
                {
                    BtnAdd_GuardedClick(btnAdd, EventArgs.Empty);
                    return true;
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
