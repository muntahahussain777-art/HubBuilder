using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace HubBuilders.View
{
    /// <summary>Admin: role → form open/add/edit/delete rules. Additive UI only.</summary>
    public class frmStaffRolePermissions : Form
    {
        private ComboBox _cmbRole;
        private DataGridView _dgv;
        private DataTable _dt;
        private Guna2Button _btnSave;
        private Guna2Button _btnClose;

        public frmStaffRolePermissions()
        {
            Text = "Staff Role Permissions";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(720, 520);
            BackColor = Color.FromArgb(32, 36, 61);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BuildUi();
            Load += (s, e) => ReloadRoles();
        }

        private void BuildUi()
        {
            var top = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.FromArgb(28, 32, 55) };
            var lbl = new Label
            {
                Text = "Role",
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(12, 16)
            };
            _cmbRole = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(70, 12),
                Width = 220,
                Font = new Font("Segoe UI Semibold", 11f)
            };
            _cmbRole.SelectedIndexChanged += (s, e) => LoadGrid();
            top.Controls.Add(lbl);
            top.Controls.Add(_cmbRole);

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
                Text = "Save Rules",
                Size = new Size(140, 36),
                Location = new Point(12, 14),
                BorderRadius = 15,
                FillColor = Color.FromArgb(0, 150, 136),
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                ForeColor = Color.White
            };
            _btnSave.Click += (s, e) => Save();
            _btnClose = new Guna2Button
            {
                Text = "Close",
                Size = new Size(110, 36),
                Location = new Point(170, 14),
                BorderRadius = 15,
                FillColor = Color.FromArgb(112, 51, 255),
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                ForeColor = Color.White
            };
            _btnClose.Click += (s, e) => Close();
            bottom.Controls.Add(_btnSave);
            bottom.Controls.Add(_btnClose);

            Controls.Add(_dgv);
            Controls.Add(bottom);
            Controls.Add(top);
        }

        private void ReloadRoles()
        {
            try
            {
                StaffAccess.EnsureSchema();
                _cmbRole.Items.Clear();
                foreach (var r in StaffAccess.DistinctRoles())
                    _cmbRole.Items.Add(r);
                if (_cmbRole.Items.Count > 0) _cmbRole.SelectedIndex = 0;
                else LoadGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Staff Rules", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadGrid()
        {
            try
            {
                string role = _cmbRole.SelectedItem == null ? "" : Convert.ToString(_cmbRole.SelectedItem);
                _dt = StaffAccess.LoadRoleGrid(role);
                _dgv.DataSource = _dt;
                if (_dgv.Columns.Contains("module_key"))
                    _dgv.Columns["module_key"].Visible = false;
                if (_dgv.Columns.Contains("Module"))
                    _dgv.Columns["Module"].ReadOnly = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Staff Rules", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Save()
        {
            try
            {
                if (!MainClass.IsAdmin)
                {
                    MessageBox.Show("Only Admin can save staff rules.", "Access Denied",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string role = _cmbRole.SelectedItem == null ? "" : Convert.ToString(_cmbRole.SelectedItem);
                if (string.IsNullOrWhiteSpace(role))
                {
                    MessageBox.Show("Role select karein.");
                    return;
                }
                _dgv.EndEdit();
                StaffAccess.SaveRoleGrid(role, _dt);
                MessageBox.Show("Rules save ho gaye: " + role, "Staff Rules",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
