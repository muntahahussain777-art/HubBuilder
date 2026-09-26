using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace HubBuilders.View
{
    /// <summary>VIP/Admin: Society → Block → default monthly installment. Same grid+save pattern as Staff Rules.</summary>
    public class frmBlockMonthlyInstallment : Form
    {
        private ComboBox _cmbSociety;
        private DataGridView _dgv;
        private DataTable _dt;
        private Guna2Button _btnSave;
        private Guna2Button _btnApplyEmpty;
        private Guna2Button _btnClose;

        public frmBlockMonthlyInstallment()
        {
            Text = "Block Monthly Installment (VIP)";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(720, 520);
            BackColor = Color.FromArgb(32, 36, 61);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BuildUi();
            Load += (s, e) => ReloadSocieties();
        }

        private void BuildUi()
        {
            var top = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.FromArgb(28, 32, 55) };
            var lbl = new Label
            {
                Text = "Society",
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(12, 16)
            };
            _cmbSociety = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(90, 12),
                Width = 320,
                Font = new Font("Segoe UI Semibold", 11f)
            };
            _cmbSociety.SelectedIndexChanged += (s, e) => LoadGrid();
            top.Controls.Add(lbl);
            top.Controls.Add(_cmbSociety);

            _dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(37, 41, 74),
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            _dgv.DefaultCellStyle.BackColor = Color.FromArgb(37, 41, 74);
            _dgv.DefaultCellStyle.ForeColor = Color.White;
            _dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(28, 32, 55);
            _dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            _dgv.EnableHeadersVisualStyles = false;

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 64, BackColor = Color.FromArgb(28, 32, 55) };
            _btnSave = new Guna2Button
            {
                Text = "Save",
                Size = new Size(120, 36),
                Location = new Point(12, 14),
                BorderRadius = 15,
                FillColor = Color.FromArgb(0, 150, 136),
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                ForeColor = Color.White
            };
            _btnSave.Click += (s, e) => Save();
            _btnApplyEmpty = new Guna2Button
            {
                Text = "Apply to empty",
                Size = new Size(150, 36),
                Location = new Point(144, 14),
                BorderRadius = 15,
                FillColor = Color.FromArgb(10, 40, 100),
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                ForeColor = Color.White
            };
            _btnApplyEmpty.Click += (s, e) => ApplyEmpty();
            _btnClose = new Guna2Button
            {
                Text = "Close",
                Size = new Size(110, 36),
                Location = new Point(310, 14),
                BorderRadius = 15,
                FillColor = Color.FromArgb(112, 51, 255),
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                ForeColor = Color.White
            };
            _btnClose.Click += (s, e) => Close();
            bottom.Controls.Add(_btnSave);
            bottom.Controls.Add(_btnApplyEmpty);
            bottom.Controls.Add(_btnClose);

            Controls.Add(_dgv);
            Controls.Add(bottom);
            Controls.Add(top);
        }

        private void ReloadSocieties()
        {
            try
            {
                BlockMonthlyInstallmentStore.EnsureSchema();
                _cmbSociety.DataSource = null;
                var dt = BlockMonthlyInstallmentStore.LoadSocieties();
                _cmbSociety.DisplayMember = "name";
                _cmbSociety.ValueMember = "id";
                _cmbSociety.DataSource = dt;
                if (_cmbSociety.Items.Count > 0) _cmbSociety.SelectedIndex = 0;
                else LoadGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Block Monthly", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int SelectedSocietyId()
        {
            try
            {
                if (_cmbSociety.SelectedValue == null || _cmbSociety.SelectedValue == DBNull.Value)
                    return 0;
                return Convert.ToInt32(_cmbSociety.SelectedValue);
            }
            catch { return 0; }
        }

        private void LoadGrid()
        {
            try
            {
                _dt = BlockMonthlyInstallmentStore.LoadGrid(SelectedSocietyId());
                _dgv.DataSource = _dt;
                if (_dgv.Columns.Contains("block_id"))
                    _dgv.Columns["block_id"].Visible = false;
                if (_dgv.Columns.Contains("Block"))
                    _dgv.Columns["Block"].ReadOnly = true;
                if (_dgv.Columns.Contains("Monthly"))
                {
                    _dgv.Columns["Monthly"].HeaderText = "Monthly Installment";
                    _dgv.Columns["Monthly"].ReadOnly = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Block Monthly", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Save()
        {
            try
            {
                if (!MainClass.IsAdmin)
                {
                    MessageBox.Show("Only Admin can save block monthly defaults.", "Access Denied",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (SelectedSocietyId() <= 0)
                {
                    MessageBox.Show("Society select karein.");
                    return;
                }
                _dgv.EndEdit();
                BlockMonthlyInstallmentStore.SaveGrid(_dt);
                MessageBox.Show("Block monthly amounts save ho gaye.", "Block Monthly",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyEmpty()
        {
            try
            {
                if (!MainClass.IsAdmin)
                {
                    MessageBox.Show("Only Admin can apply block defaults.", "Access Denied",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var ask = MessageBox.Show(
                    "Active allocations jinki monthly installment 0/empty hai un par block default set ho jayega.\n\nContinue?",
                    "Apply to empty",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (ask != DialogResult.Yes) return;
                _dgv.EndEdit();
                BlockMonthlyInstallmentStore.SaveGrid(_dt);
                int n = BlockMonthlyInstallmentStore.ApplyToEmptyActive();
                MessageBox.Show(
                    n + " Active allocation(s) update ho gayi.",
                    "Apply to empty",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Apply failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
