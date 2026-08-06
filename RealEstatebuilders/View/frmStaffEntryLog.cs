using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;

namespace HubBuilders.View
{
    /// <summary>
    /// Admin: kaun staff kis entry pe kaam kiya — staff_entry_log (additive table).
    /// </summary>
    public class frmStaffEntryLog : Form
    {
        private readonly DataGridView _grid = new DataGridView();
        private readonly TextBox _txtSearch = new TextBox();
        private readonly Button _btnRefresh = new Button();
        private readonly Button _btnClose = new Button();
        private readonly Label _lblHint = new Label();

        public frmStaffEntryLog()
        {
            Text = "Staff Entry Log — kis ne kya kiya";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(900, 520);
            Size = new Size(1000, 600);
            BackColor = Color.FromArgb(32, 36, 61);
            KeyPreview = true;

            var top = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                Padding = new Padding(12, 10, 12, 8)
            };
            Controls.Add(top);

            _lblHint.AutoSize = false;
            _lblHint.Dock = DockStyle.Top;
            _lblHint.Height = 28;
            _lblHint.ForeColor = Color.White;
            _lblHint.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            _lblHint.Text = "Yahan har staff ki Insert / Update / Delete entries dikhengi (Customer, Receipt, Allocate, …).";
            top.Controls.Add(_lblHint);

            var row = new Panel { Dock = DockStyle.Fill };
            top.Controls.Add(row);
            row.BringToFront();

            var lbl = new Label
            {
                Text = "Search staff / module:",
                AutoSize = true,
                ForeColor = Color.White,
                Left = 0,
                Top = 8
            };
            row.Controls.Add(lbl);

            _txtSearch.Left = 160;
            _txtSearch.Top = 4;
            _txtSearch.Width = 320;
            _txtSearch.Font = new Font("Segoe UI", 10f);
            _txtSearch.TextChanged += (s, e) => LoadLog();
            row.Controls.Add(_txtSearch);

            _btnRefresh.Text = "Refresh";
            _btnRefresh.Left = 500;
            _btnRefresh.Top = 2;
            _btnRefresh.Width = 100;
            _btnRefresh.Height = 30;
            _btnRefresh.Click += (s, e) => LoadLog();
            row.Controls.Add(_btnRefresh);

            _btnClose.Text = "Close";
            _btnClose.Left = 610;
            _btnClose.Top = 2;
            _btnClose.Width = 90;
            _btnClose.Height = 30;
            _btnClose.Click += (s, e) => Close();
            row.Controls.Add(_btnClose);

            _grid.Dock = DockStyle.Fill;
            _grid.ReadOnly = true;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.BackgroundColor = Color.White;
            _grid.RowHeadersVisible = false;
            Controls.Add(_grid);
            _grid.BringToFront();

            Load += (s, e) => LoadLog();
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape) Close();
                if (e.KeyCode == Keys.F5) LoadLog();
            };
        }

        private void LoadLog()
        {
            try
            {
                MainClass.EnsureStaffAuditSchema();
                string q = @"
SELECT
  id AS [Sr],
  created_at AS [DateTime],
  staff_username AS [Staff User],
  staff_name AS [Staff Name],
  staff_role AS [Role],
  action_type AS [Action],
  module_name AS [Form / Table],
  IFNULL(detail,'') AS [Detail]
FROM staff_entry_log
";
                string s = (_txtSearch.Text ?? "").Trim();
                if (s.Length > 0)
                {
                    q += @"
WHERE staff_username LIKE @s
   OR staff_name LIKE @s
   OR module_name LIKE @s
   OR action_type LIKE @s
   OR IFNULL(detail,'') LIKE @s
";
                }
                q += " ORDER BY id DESC LIMIT 2000;";

                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    using (var cmd = new SQLiteCommand(q, con))
                    {
                        if (s.Length > 0)
                            cmd.Parameters.AddWithValue("@s", "%" + s + "%");
                        using (var da = new SQLiteDataAdapter(cmd))
                        {
                            var dt = new DataTable();
                            da.Fill(dt);
                            _grid.DataSource = dt;
                        }
                    }
                }

                if (_grid.Rows.Count == 0)
                {
                    _lblHint.Text = "Abhi koi log nahi. Staff login karke koi entry save kare — yahan dikhegi.";
                }
                else
                {
                    _lblHint.Text = "Total shown: " + _grid.Rows.Count + "  |  Staff User / Name se pata chalega kis ne kiya.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Staff log load fail:\n" + ex.Message, "Staff Entry Log",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
