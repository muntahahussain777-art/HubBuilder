using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Guna.UI2.WinForms;   // optional: Guna2 buttons

namespace HubBuilders.Model
{
    public partial class frmPlotInventoryFormAdd : Sample
    {
        private DataTable _dtPreview;

        public frmPlotInventoryFormAdd()
        {
            InitializeComponent();
            this.Load += FrmPlotInventoryFormAdd_Load;

            // Top buttons
            btnGeneratePreview.Click += btnGeneratePreview_Click;
            btnCommit.Click += btnCommit_Click;
            btnLoadExisting.Click += btnLoadExisting_Click;
        }

        // -------- Helper: SelectedValue -> int (Int64/DataRowView safe) --------
        private bool TryGetSelectedId(ComboBox cb, out int id)
        {
            id = 0;
            var val = cb?.SelectedValue;
            if (val == null || val == DBNull.Value) return false;

            try
            {
                id = Convert.ToInt32(val); // handles Int64 as well
                return id > 0;
            }
            catch
            {
                if (val is DataRowView drv && drv.Row.Table.Columns.Contains("id"))
                {
                    id = Convert.ToInt32(drv["id"]);
                    return id > 0;
                }
                return false;
            }
        }

        // -------------------- FORM LOAD --------------------
        private void FrmPlotInventoryFormAdd_Load(object sender, EventArgs e)
        {
            // Societies
            MainClass.LoadDataIntoComboBox("SELECT id, name FROM societies ORDER BY name;", cbSociety, "name", "id");
            cbSociety.SelectedIndex = -1;

            // Cascade events (Guna2 me SelectedValueChanged reliable rehta hai)
            cbSociety.SelectionChangeCommitted += cbSociety_SelectionChangeCommitted;
            cbSociety.SelectedValueChanged += cbSociety_SelectionChangeCommitted;

            // Status list (must match CHECK constraint)
            cbStatus.Items.Clear();
            cbStatus.Items.AddRange(new object[] { "Available", "Hold", "Sold" });
            cbStatus.SelectedIndex = 0;

            // Count defaults
            if (numCount.Minimum < 1) numCount.Minimum = 1;
            if (numCount.Value < 1) numCount.Value = 1;

            // Data + Grid
            BuildPreviewTable();
            BindPreviewGrid();

            // Grid settings
            grdPreview.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grdPreview.MultiSelect = false;
            grdPreview.ReadOnly = true; // text columns read-only; buttons still clickable
            grdPreview.EditMode = DataGridViewEditMode.EditProgrammatically;

            grdPreview.CellContentClick += grdPreview_CellContentClick;
            grdPreview.CellFormatting += grdPreview_CellFormatting;
            grdPreview.DataError += (s, ev) => { ev.ThrowException = false; };

            // Button colors
            StyleButtons();
        }

        // -------------------- BUTTON STYLES --------------------
        private void StyleButtons()
        {
            StyleButton(btnGeneratePreview, Color.DodgerBlue, Color.White);
            StyleButton(btnLoadExisting, Color.MediumPurple, Color.White);
            StyleButton(btnCommit, Color.SeaGreen, Color.White);
        }
        private void StyleButton(Button b, Color back, Color fore)
        {
            if (b == null) return;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = back;
            b.ForeColor = fore;
            b.Cursor = Cursors.Hand;
        }
        private void StyleButton(Guna2Button b, Color back, Color fore)
        {
            if (b == null) return;
            b.FillColor = back;
            b.ForeColor = fore;
            b.HoverState.FillColor = ControlPaint.Light(back);
            b.PressedColor = ControlPaint.Dark(back);
            b.BorderThickness = 0;
            b.Cursor = Cursors.Hand;
        }

        // -------------------- CASCADE --------------------
        private void cbSociety_SelectionChangeCommitted(object sender, EventArgs e) => RefreshBlocksAndTypes();

        private void RefreshBlocksAndTypes()
        {
            cbBlock.DataSource = null;
            cbPlotType.DataSource = null;

            if (!TryGetSelectedId(cbSociety, out var sid)) return;

            MainClass.LoadDataIntoComboBox(
                $"SELECT id, name FROM blocks WHERE society_id={sid} ORDER BY name;",
                cbBlock, "name", "id");
            cbBlock.SelectedIndex = -1;

            MainClass.LoadDataIntoComboBox(
                $"SELECT id, code FROM plot_types WHERE society_id={sid} ORDER BY code;",
                cbPlotType, "code", "id");
            cbPlotType.SelectedIndex = -1;
        }

        // -------------------- DATATABLE & GRID --------------------
        private void BuildPreviewTable()
        {
            _dtPreview = new DataTable();
            _dtPreview.Columns.Add("plot_id", typeof(int));     // 0 => not in DB
            _dtPreview.Columns.Add("IsDB", typeof(bool));       // true => row from DB
            _dtPreview.Columns.Add("society_id", typeof(int));
            _dtPreview.Columns.Add("block_id", typeof(int));
            _dtPreview.Columns.Add("plot_type_id", typeof(int));
            _dtPreview.Columns.Add("PlotNo", typeof(string));
            _dtPreview.Columns.Add("Society", typeof(string));
            _dtPreview.Columns.Add("Block", typeof(string));
            _dtPreview.Columns.Add("Type", typeof(string));
            _dtPreview.Columns.Add("Status", typeof(string));
        }

        private void BindPreviewGrid()
        {
            grdPreview.AutoGenerateColumns = false;
            grdPreview.Columns.Clear();

            // Hidden tech columns
            grdPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "dgvPlotId", DataPropertyName = "plot_id", Visible = false });
            grdPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "dgvIsDB", DataPropertyName = "IsDB", Visible = false });
            grdPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "dgvSocietyId", DataPropertyName = "society_id", Visible = false });
            grdPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "dgvBlockId", DataPropertyName = "block_id", Visible = false });
            grdPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "dgvPlotTypeId", DataPropertyName = "plot_type_id", Visible = false });

            // Display columns
            grdPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "dgvPlotNo", HeaderText = "PlotNo", DataPropertyName = "PlotNo", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells });
            grdPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "dgvSociety", HeaderText = "Society", DataPropertyName = "Society", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            grdPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "dgvBlock", HeaderText = "Block", DataPropertyName = "Block", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            grdPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "dgvType", HeaderText = "Type", DataPropertyName = "Type", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells });
            grdPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "dgvStatus", HeaderText = "Status", DataPropertyName = "Status", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells });

            // Action buttons
            grdPreview.Columns.Add(new DataGridViewButtonColumn { Name = "dgvedit", HeaderText = "", Text = "Edit", UseColumnTextForButtonValue = true, Width = 60, FlatStyle = FlatStyle.Flat });
            grdPreview.Columns.Add(new DataGridViewButtonColumn { Name = "dgvdel", HeaderText = "", Text = "Delete", UseColumnTextForButtonValue = true, Width = 70, FlatStyle = FlatStyle.Flat });

            grdPreview.DataSource = _dtPreview;
        }

        private void grdPreview_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            var col = grdPreview.Columns[e.ColumnIndex];
            if (col.Name == "dgvedit")
            {
                grdPreview.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = Color.DodgerBlue;
                grdPreview.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.ForeColor = Color.White;
            }
            else if (col.Name == "dgvdel")
            {
                grdPreview.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = Color.IndianRed;
                grdPreview.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.ForeColor = Color.White;
            }
        }

        // -------------------- PREVIEW GENERATE --------------------
        private void btnGeneratePreview_Click(object sender, EventArgs e)
        {
            if (!TryGetSelectedId(cbSociety, out var sid)) { MessageBox.Show("Society select karo.", "Validation"); cbSociety.DroppedDown = true; return; }
            if (!TryGetSelectedId(cbBlock, out var bid)) { MessageBox.Show("Block select karo.", "Validation"); cbBlock.DroppedDown = true; return; }
            if (!TryGetSelectedId(cbPlotType, out var ptid)) { MessageBox.Show("Plot Type select karo.", "Validation"); cbPlotType.DroppedDown = true; return; }

            string start = (txtPlotNoStart.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(start)) { MessageBox.Show("Start Plot No required hai.", "Validation"); txtPlotNoStart.Focus(); return; }
            if (numCount.Value < 1) { MessageBox.Show("Count kam az kam 1.", "Validation"); numCount.Value = 1; return; }

            string status = Convert.ToString(cbStatus.SelectedItem ?? "Available");
            if (status != "Available" && status != "Hold" && status != "Sold") { cbStatus.SelectedIndex = 0; status = "Available"; }

            _dtPreview.Rows.Clear();

            string cur = start;
            int count = (int)numCount.Value;

            for (int i = 0; i < count; i++)
            {
                var r = _dtPreview.NewRow();
                r["plot_id"] = 0;
                r["IsDB"] = false;
                r["society_id"] = sid;
                r["block_id"] = bid;
                r["plot_type_id"] = ptid;
                r["PlotNo"] = cur;
                r["Society"] = cbSociety.Text;
                r["Block"] = cbBlock.Text;
                r["Type"] = cbPlotType.Text;
                r["Status"] = status;
                _dtPreview.Rows.Add(r);

                cur = NextPlotNo(cur);
            }

            grdPreview.Refresh();
        }

        // -------------------- LOAD EXISTING (DB) --------------------
        private void btnLoadExisting_Click(object sender, EventArgs e)
        {
            if (!TryGetSelectedId(cbSociety, out var sid)) { MessageBox.Show("Society select karo.", "Validation"); return; }
            if (!TryGetSelectedId(cbBlock, out var bid)) { MessageBox.Show("Block select karo.", "Validation"); return; }
            if (!TryGetSelectedId(cbPlotType, out var ptid)) { MessageBox.Show("Plot Type select karo.", "Validation"); return; }

            _dtPreview.Rows.Clear();

            var dt = MainClass.ExecuteSelectQuery(
                @"SELECT id, plot_no, status 
                  FROM plots 
                  WHERE society_id=@sid AND block_id=@bid AND plot_type_id=@ptid
                  ORDER BY id DESC;",
                new Hashtable { { "@sid", sid }, { "@bid", bid }, { "@ptid", ptid } });

            foreach (DataRow dr in dt.Rows)
            {
                var r = _dtPreview.NewRow();
                r["plot_id"] = Convert.ToInt32(dr["id"]);
                r["IsDB"] = true;
                r["society_id"] = sid;
                r["block_id"] = bid;
                r["plot_type_id"] = ptid;
                r["PlotNo"] = Convert.ToString(dr["plot_no"]);
                r["Status"] = Convert.ToString(dr["status"]);
                r["Society"] = cbSociety.Text;
                r["Block"] = cbBlock.Text;
                r["Type"] = cbPlotType.Text;
                _dtPreview.Rows.Add(r);
            }

            grdPreview.Refresh();
        }

        // -------------------- GRID ACTIONS (Edit/Delete) --------------------
        private void grdPreview_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return; // ignore header
            var colName = grdPreview.Columns[e.ColumnIndex]?.Name;
            if (string.IsNullOrEmpty(colName)) return;

            var drv = grdPreview.Rows[e.RowIndex].DataBoundItem as DataRowView;
            if (drv == null) return;

            if (colName == "dgvedit") { EditRow(drv); return; }
            if (colName == "dgvdel") { DeleteRow(drv); return; }
        }

        private void EditRow(DataRowView drv)
        {
            string curPlotNo = Convert.ToString(drv["PlotNo"]);
            string curStatus = Convert.ToString(drv["Status"]);

            using (var dlg = BuildEditDialog(curPlotNo, curStatus, out TextBox txtPlot, out ComboBox cbStat))
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;

                string newPlot = (txtPlot.Text ?? "").Trim();
                string newStat = Convert.ToString(cbStat.SelectedItem ?? "Available");
                if (string.IsNullOrWhiteSpace(newPlot)) { MessageBox.Show("PlotNo khaali nahi ho sakta.", "Validation"); return; }
                if (newStat != "Available" && newStat != "Hold" && newStat != "Sold") { MessageBox.Show("Status sirf Available/Hold/Sold.", "Validation"); return; }

                bool isDB = Convert.ToBoolean(drv["IsDB"]);
                if (isDB)
                {
                    int pid = Convert.ToInt32(drv["plot_id"]);
                    int sid = Convert.ToInt32(drv["society_id"]);
                    int bid = Convert.ToInt32(drv["block_id"]);
                    int ptid = Convert.ToInt32(drv["plot_type_id"]);

                    var dup = MainClass.ExecuteScalar(
                        @"SELECT COUNT(1) FROM plots
                          WHERE society_id=@sid AND block_id=@bid AND plot_type_id=@ptid
                            AND lower(plot_no)=lower(@pno) AND id<>@id",
                        new Dictionary<string, object> { { "@sid", sid }, { "@bid", bid }, { "@ptid", ptid }, { "@pno", newPlot }, { "@id", pid } });
                    if (Convert.ToInt32(dup ?? 0) > 0) { MessageBox.Show("Isi combo me yeh PlotNo pehle se mojood hai.", "Duplicate"); return; }

                    int up = MainClass.ExecuteNonQuery("UPDATE plots SET plot_no=@pno, status=@st WHERE id=@id",
                        new Dictionary<string, object> { { "@pno", newPlot }, { "@st", newStat }, { "@id", pid } });
                    if (up <= 0) { MessageBox.Show("DB update fail.", "Error"); return; }
                }

                drv["PlotNo"] = newPlot;
                drv["Status"] = newStat;
                drv.EndEdit();
                grdPreview.Refresh();
            }
        }

        private void DeleteRow(DataRowView drv)
        {
            bool isDB = Convert.ToBoolean(drv["IsDB"]);
            if (isDB)
            {
                int pid = Convert.ToInt32(drv["plot_id"]);
                var ask = new YesOrNoMessage("Delete this plot from DATABASE?", "Delete Confirmation");
                if (ask.ShowDialog() != DialogResult.Yes) return;

                int del = MainClass.ExecuteNonQuery("DELETE FROM plots WHERE id=@id",
                    new Dictionary<string, object> { { "@id", pid } });
                if (del <= 0) { new ErrorFormMessage("DB delete fail ya FK constraint.", "Error").ShowDialog(); return; }
            }
            else
            {
                var ask2 = new YesOrNoMessage("Remove this row from PREVIEW?", "Delete Confirmation");
                if (ask2.ShowDialog() != DialogResult.Yes) return;
            }

            drv.Delete();
            _dtPreview.AcceptChanges();
            grdPreview.Refresh();
        }

        // -------------------- COMMIT (insert preview rows to DB) --------------------
        private void btnCommit_Click(object sender, EventArgs e)
        {
            if (_dtPreview.Rows.Count == 0) { MessageBox.Show("Preview me kuch rows nahi hain.", "Info"); return; }

            int inserted = 0, duplicated = 0, failed = 0;
            MainClass.ExecuteNonQuery("PRAGMA foreign_keys = ON;");

            foreach (DataRow r in _dtPreview.Rows)
            {
                if (Convert.ToBoolean(r["IsDB"])) continue;

                int sid = Convert.ToInt32(r["society_id"]);
                int bid = Convert.ToInt32(r["block_id"]);
                int ptid = Convert.ToInt32(r["plot_type_id"]);
                string plotNo = Convert.ToString(r["PlotNo"]);
                string status = Convert.ToString(r["Status"]);
                string createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                var exists = MainClass.ExecuteScalar(
                    "SELECT COUNT(1) FROM plots WHERE society_id=@sid AND block_id=@bid AND plot_type_id=@ptid AND plot_no=@pno",
                    new Dictionary<string, object> { { "@sid", sid }, { "@bid", bid }, { "@ptid", ptid }, { "@pno", plotNo } });
                if (Convert.ToInt32(exists ?? 0) > 0) { duplicated++; continue; }

                int res = MainClass.ExecuteNonQuery(
                    @"INSERT INTO plots (society_id, block_id, plot_type_id, plot_no, status, created_at)
                      VALUES (@sid, @bid, @ptid, @pno, @status, @created_at);",
                    new Dictionary<string, object> {
                        {"@sid", sid},{"@bid", bid},{"@ptid", ptid},
                        {"@pno", plotNo},{"@status", status},{"@created_at", createdAt}
                    });

                if (res > 0) inserted++; else failed++;
            }

            new CustomeMessage($"Commit summary:\nInserted: {inserted}\nDuplicates skipped: {duplicated}\nFailed: {failed}",
                "RealEstate Builders").ShowDialog();
        }

        // -------------------- EDIT DIALOG --------------------
        private Form BuildEditDialog(string plotNo, string status, out TextBox txtPlot, out ComboBox cbStat)
        {
            var dlg = new Form
            {
                Text = "Edit Plot",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
                Width = 360,
                Height = 180
            };

            var lblP = new Label { Left = 15, Top = 20, Text = "Plot No", AutoSize = true };
            txtPlot = new TextBox { Left = 100, Top = 16, Width = 220, Text = plotNo };

            var lblS = new Label { Left = 15, Top = 55, Text = "Status", AutoSize = true };
            cbStat = new ComboBox { Left = 100, Top = 50, Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
            cbStat.Items.AddRange(new object[] { "Available", "Hold", "Sold" });
            int idx = Array.IndexOf(new[] { "Available", "Hold", "Sold" }, status);
            cbStat.SelectedIndex = idx >= 0 ? idx : 0;

            var btnOK = new Button { Text = "OK", Left = 160, Width = 70, Top = 90, DialogResult = DialogResult.OK };
            var btnCancel = new Button { Text = "Cancel", Left = 250, Width = 70, Top = 90, DialogResult = DialogResult.Cancel };

            dlg.Controls.Add(lblP); dlg.Controls.Add(txtPlot);
            dlg.Controls.Add(lblS); dlg.Controls.Add(cbStat);
            dlg.Controls.Add(btnOK); dlg.Controls.Add(btnCancel);
            dlg.AcceptButton = btnOK; dlg.CancelButton = btnCancel;

            return dlg;
        }

        // -------------------- PlotNo increment helper --------------------
        private string NextPlotNo(string current)
        {
            if (string.IsNullOrEmpty(current)) return "1";
            if (int.TryParse(current, out int n)) return (n + 1).ToString();

            var m = Regex.Match(current, @"^(.*?)(\d+)$");
            if (m.Success)
            {
                string prefix = m.Groups[1].Value;
                string digits = m.Groups[2].Value;
                int num = int.Parse(digits) + 1;
                return prefix + num.ToString(new string('0', digits.Length));
            }
            return current + "-1";
        }
    }
}
