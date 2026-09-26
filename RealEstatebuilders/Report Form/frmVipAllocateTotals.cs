using HubBuilders.ProjectConnection;
using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace HubBuilders.Report_Form
{
    /// <summary>
    /// VIP: All Active allocated plots — Total Allocate Amount, Total Paid, Remaining (Baqi Lena).
    /// Additive-only; does not change sync or existing allocate/receipt UI.
    /// </summary>
    public class frmVipAllocateTotals : Sample
    {
        private ComboBox cmbSociety;
        private DataGridView dgv;
        private Label lblSummary;
        private Guna.UI2.WinForms.Guna2Button btnLoad;
        private bool _binding;

        public frmVipAllocateTotals()
        {
            InitializeUi();
            LoadSocieties();
            LoadReport();
        }

        private void InitializeUi()
        {
            Text = "Allocate Totals (VIP)";
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            BackColor = Color.FromArgb(32, 36, 61);

            var panelTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(32, 36, 61)
            };
            var title = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(12, 16),
                Text = "Allocate Totals (VIP)"
            };
            var close = new Guna.UI2.WinForms.Guna2ControlBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FillColor = Color.FromArgb(139, 152, 166),
                IconColor = Color.White,
                Size = new Size(56, 36)
            };
            panelTop.Controls.Add(title);
            panelTop.Controls.Add(close);
            panelTop.Resize += (s, e) => close.Location = new Point(panelTop.Width - 70, 12);

            var panelFilters = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
                Padding = new Padding(12),
                BackColor = Color.FromArgb(32, 36, 61)
            };
            var lblSoc = new Label
            {
                Text = "Society",
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(12, 8),
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold)
            };
            cmbSociety = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F),
                Location = new Point(12, 30),
                Size = new Size(280, 28)
            };
            cmbSociety.SelectedIndexChanged += (s, e) =>
            {
                if (!_binding) LoadReport();
            };
            btnLoad = new Guna.UI2.WinForms.Guna2Button
            {
                Text = "Load Report",
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                FillColor = Color.FromArgb(18, 140, 126),
                ForeColor = Color.White,
                BorderRadius = 8,
                Location = new Point(310, 26),
                Size = new Size(160, 36)
            };
            btnLoad.Click += (s, e) => LoadReport();
            lblSummary = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(180, 230, 210),
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                Location = new Point(12, 68),
                Text = ""
            };
            panelFilters.Controls.Add(lblSoc);
            panelFilters.Controls.Add(cmbSociety);
            panelFilters.Controls.Add(btnLoad);
            panelFilters.Controls.Add(lblSummary);

            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.FromArgb(32, 36, 66),
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false
            };
            StyleGrid(dgv);

            Controls.Add(dgv);
            Controls.Add(panelFilters);
            Controls.Add(panelTop);
        }

        private static void StyleGrid(DataGridView gv)
        {
            Color dark = Color.FromArgb(32, 36, 66);
            Color header = Color.FromArgb(20, 45, 95);
            gv.EnableHeadersVisualStyles = false;
            gv.ColumnHeadersDefaultCellStyle.BackColor = header;
            gv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            gv.DefaultCellStyle.BackColor = dark;
            gv.DefaultCellStyle.ForeColor = Color.White;
            gv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(45, 52, 90);
            gv.DefaultCellStyle.SelectionForeColor = Color.White;
            gv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(37, 41, 74);
            gv.GridColor = Color.FromArgb(50, 56, 76);
        }

        private void LoadSocieties()
        {
            _binding = true;
            try
            {
                var dt = new DataTable();
                dt.Columns.Add("id", typeof(int));
                dt.Columns.Add("name", typeof(string));
                dt.Rows.Add(0, "(All Societies)");

                using (var con = new SQLiteConnection(projectconnection.conReturn()))
                using (var cmd = new SQLiteCommand("SELECT id, name FROM societies ORDER BY name COLLATE NOCASE;", con))
                {
                    con.Open();
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                            dt.Rows.Add(Convert.ToInt32(r["id"]), Convert.ToString(r["name"]));
                    }
                }

                cmbSociety.DisplayMember = "name";
                cmbSociety.ValueMember = "id";
                cmbSociety.DataSource = dt;
                cmbSociety.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                HubBuilders.AppSafety.ShowSoft(ex);
            }
            finally
            {
                _binding = false;
            }
        }

        private int GetSocietyId()
        {
            try
            {
                if (cmbSociety?.SelectedValue == null || cmbSociety.SelectedValue == DBNull.Value)
                    return 0;
                return Convert.ToInt32(cmbSociety.SelectedValue);
            }
            catch { return 0; }
        }

        private void LoadReport()
        {
            if (_binding) return;
            try
            {
                int societyId = GetSocietyId();
                int plotCount = 0;
                double totalAllocate = 0, totalPaid = 0;

                using (var con = new SQLiteConnection(projectconnection.conReturn()))
                {
                    con.Open();
                    using (var cmd = new SQLiteCommand(@"
SELECT
  COUNT(*) AS plot_count,
  COALESCE(SUM(COALESCE(cp.price,0)),0) AS total_allocate,
  COALESCE(SUM((
    SELECT COALESCE(SUM(r.amount),0)
    FROM receipts r
    WHERE r.customer_plot_id = cp.id
  )),0) AS total_paid
FROM customer_plots cp
JOIN plots p ON p.id = cp.plot_id
WHERE cp.status = 'Active'
  AND (@sid = 0 OR p.society_id = @sid)", con))
                    {
                        cmd.Parameters.AddWithValue("@sid", societyId);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                plotCount = Convert.ToInt32(r["plot_count"]);
                                totalAllocate = Convert.ToDouble(r["total_allocate"]);
                                totalPaid = Convert.ToDouble(r["total_paid"]);
                            }
                        }
                    }
                }

                double remaining = totalAllocate - totalPaid;
                var table = new DataTable();
                table.Columns.Add("Particular", typeof(string));
                table.Columns.Add("Amount", typeof(double));
                table.Rows.Add("Allocated Plots (count)", plotCount);
                table.Rows.Add("Total Allocate Amount", totalAllocate);
                table.Rows.Add("Total Paid (Receipts)", totalPaid);
                table.Rows.Add("Remaining / Baqi Lena", remaining);

                dgv.DataSource = table;
                StyleGrid(dgv);
                if (dgv.Columns.Contains("Amount"))
                    dgv.Columns["Amount"].DefaultCellStyle.Format = "N2";

                var n = CultureInfo.InvariantCulture;
                lblSummary.Text = "Plots: " + plotCount
                    + "   |   Allocate: " + totalAllocate.ToString("N2", n)
                    + "   |   Paid: " + totalPaid.ToString("N2", n)
                    + "   |   Remaining: " + remaining.ToString("N2", n);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Allocate Totals (VIP)", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
