using System;
using System.Windows.Forms;

namespace HubBuilders
{
    public partial class SampleView : Sample
    {
        private DateTime _lastLiveRefreshUtc = DateTime.MinValue;
        private bool _liveRefreshQueued;
        private readonly Timer _liveRefreshTimer = new Timer { Interval = 800 };

        public SampleView()
        {
            InitializeComponent();
            // Designer Click hata kar guarded wrap — Close click-through se Add dobara na khule
            if (btnAdd != null)
            {
                btnAdd.Click -= btnAdd_Click;
                btnAdd.Click += BtnAdd_GuardedClick;
            }
            Load += SampleView_Load;
            FormClosed += SampleView_FormClosed;
            _liveRefreshTimer.Tick += (s, e) =>
            {
                _liveRefreshTimer.Stop();
                _liveRefreshQueued = false;
                DoRefreshLiveData();
            };
        }

        private void SampleView_Load(object sender, EventArgs e)
        {
            try { HubSyncService.LiveChanged += OnLiveChanged; } catch { }
            try { ApplyStaffAccessUi(); } catch { }
        }

        /// <summary>Role rules: Add / Edit / Delete buttons — layout same.</summary>
        protected void ApplyStaffAccessUi()
        {
            string mod = StaffAccess.ModuleForForm(this);
            if (string.IsNullOrWhiteSpace(mod)) return;
            if (btnAdd != null)
                btnAdd.Enabled = StaffAccess.Can(mod, "add");
            try
            {
                foreach (Control c in Controls)
                {
                    var gv = c as DataGridView;
                    if (gv == null) continue;
                    if (gv.Columns.Contains("dgvedit"))
                        gv.Columns["dgvedit"].Visible = StaffAccess.Can(mod, "edit");
                    if (gv.Columns.Contains("dgvdel"))
                        gv.Columns["dgvdel"].Visible = StaffAccess.Can(mod, "delete");
                }
                // nested grids (common pattern)
                ApplyGridPerms(this, mod);
            }
            catch { }
        }

        private static void ApplyGridPerms(Control root, string mod)
        {
            if (root == null) return;
            foreach (Control c in root.Controls)
            {
                var gv = c as DataGridView;
                if (gv != null)
                {
                    if (gv.Columns.Contains("dgvedit"))
                        gv.Columns["dgvedit"].Visible = StaffAccess.Can(mod, "edit");
                    if (gv.Columns.Contains("dgvdel"))
                        gv.Columns["dgvdel"].Visible = StaffAccess.Can(mod, "delete");
                }
                if (c.HasChildren) ApplyGridPerms(c, mod);
            }
        }

        private void SampleView_FormClosed(object sender, FormClosedEventArgs e)
        {
            try { HubSyncService.LiveChanged -= OnLiveChanged; } catch { }
            try { _liveRefreshTimer.Stop(); } catch { }
        }

        private void OnLiveChanged()
        {
            try
            {
                if (IsDisposed || !IsHandleCreated) return;
                BeginInvoke(new Action(RefreshLiveData));
            }
            catch { }
        }

        /// <summary>Open list ko sync/save ke baad turant refresh — scroll position same rahe.</summary>
        protected void RefreshLiveData()
        {
            try
            {
                if (IsDisposed || !IsHandleCreated) return;
                // Auto-sync bar bar GetData → scrollbar upar jump. Debounce + scroll restore.
                if ((DateTime.UtcNow - _lastLiveRefreshUtc).TotalMilliseconds < 1500)
                {
                    if (_liveRefreshQueued) return;
                    _liveRefreshQueued = true;
                    _liveRefreshTimer.Stop();
                    _liveRefreshTimer.Start();
                    return;
                }
                DoRefreshLiveData();
            }
            catch { }
        }

        private void DoRefreshLiveData()
        {
            try
            {
                if (IsDisposed || !IsHandleCreated) return;

                DataGridView gv = FindPrimaryGrid(this);
                // User scrollbar drag kar raha ho to abhi mat reload — baad mein
                if (gv != null && Control.MouseButtons != MouseButtons.None)
                {
                    try
                    {
                        var pt = gv.PointToClient(Control.MousePosition);
                        if (gv.ClientRectangle.Contains(pt))
                        {
                            if (!_liveRefreshQueued)
                            {
                                _liveRefreshQueued = true;
                                _liveRefreshTimer.Stop();
                                _liveRefreshTimer.Start();
                            }
                            return;
                        }
                    }
                    catch { }
                }

                _lastLiveRefreshUtc = DateTime.UtcNow;

                int firstRow = -1;
                int selRow = -1;
                int selCol = -1;
                if (gv != null && gv.RowCount > 0)
                {
                    try { firstRow = gv.FirstDisplayedScrollingRowIndex; } catch { firstRow = -1; }
                    try
                    {
                        if (gv.CurrentCell != null)
                        {
                            selRow = gv.CurrentCell.RowIndex;
                            selCol = gv.CurrentCell.ColumnIndex;
                        }
                    }
                    catch { selRow = -1; }
                }

                var mi = GetType().GetMethod("GetData",
                    System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.NonPublic);
                if (mi != null && mi.GetParameters().Length == 0)
                    mi.Invoke(this, null);

                if (gv == null || gv.IsDisposed || gv.RowCount <= 0) return;
                try
                {
                    if (firstRow >= 0 && firstRow < gv.RowCount)
                        gv.FirstDisplayedScrollingRowIndex = firstRow;
                }
                catch { }
                try
                {
                    if (selRow >= 0 && selRow < gv.RowCount)
                    {
                        int col = selCol;
                        if (col < 0 || col >= gv.ColumnCount) col = 0;
                        if (gv.Columns[col].Visible)
                            gv.CurrentCell = gv.Rows[selRow].Cells[col];
                    }
                }
                catch { }
            }
            catch { }
        }

        private static DataGridView FindPrimaryGrid(Control root)
        {
            if (root == null) return null;
            DataGridView best = null;
            FindPrimaryGridWalk(root, ref best);
            return best;
        }

        private static void FindPrimaryGridWalk(Control root, ref DataGridView best)
        {
            foreach (Control c in root.Controls)
            {
                var gv = c as DataGridView;
                if (gv != null && gv.Visible)
                {
                    if (best == null || gv.RowCount >= best.RowCount)
                        best = gv;
                }
                if (c.HasChildren) FindPrimaryGridWalk(c, ref best);
            }
        }

        private void BtnAdd_GuardedClick(object sender, EventArgs e)
        {
            if (MainClass.UiClicksBlocked) return;
            string mod = StaffAccess.ModuleForForm(this);
            if (!string.IsNullOrWhiteSpace(mod) && StaffAccess.Deny(mod, "add")) return;
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
