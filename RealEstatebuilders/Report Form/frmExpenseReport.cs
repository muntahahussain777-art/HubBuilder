using HubBuilders.ProjectConnection;
using Microsoft.Reporting.WinForms;
using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace HubBuilders.Report_Form
{
    /// <summary>
    /// Expense Form report (Expensetbl) — RDLC list + date/search. Additive only.
    /// </summary>
    public class frmExpenseReport : Sample
    {
        private DateTimePicker dtpFrom;
        private DateTimePicker dtpTo;
        private TextBox txtSearch;
        private Guna.UI2.WinForms.Guna2Button btnLoad;
        private Guna.UI2.WinForms.Guna2Button btnClear;
        private ReportViewer reportViewer1;

        public frmExpenseReport()
        {
            InitializeUi();
            dtpFrom.Value = DateTime.Today.AddMonths(-1);
            dtpTo.Value = DateTime.Today;
        }

        private void InitializeUi()
        {
            Text = "Expense Report";
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
                Text = "Expense Report"
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
                Height = 110,
                Padding = new Padding(12),
                BackColor = Color.FromArgb(32, 36, 61)
            };

            dtpFrom = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd-MMM-yyyy",
                Font = new Font("Segoe UI", 10F),
                Location = new Point(12, 30),
                Size = new Size(160, 28)
            };
            dtpTo = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd-MMM-yyyy",
                Font = new Font("Segoe UI", 10F),
                Location = new Point(190, 30),
                Size = new Size(160, 28)
            };
            txtSearch = new TextBox
            {
                Font = new Font("Segoe UI", 10F),
                Location = new Point(370, 30),
                Size = new Size(280, 28)
            };

            btnLoad = new Guna.UI2.WinForms.Guna2Button
            {
                Text = "Show / PDF Preview",
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                FillColor = Color.FromArgb(184, 148, 68),
                ForeColor = Color.White,
                BorderRadius = 8,
                Location = new Point(12, 68),
                Size = new Size(200, 36)
            };
            btnLoad.Click += (s, e) => LoadReport(filterDates: true);

            btnClear = new Guna.UI2.WinForms.Guna2Button
            {
                Text = "All Expenses",
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                FillColor = Color.FromArgb(10, 40, 100),
                ForeColor = Color.White,
                BorderRadius = 8,
                Location = new Point(222, 68),
                Size = new Size(160, 36)
            };
            btnClear.Click += (s, e) =>
            {
                txtSearch.Clear();
                LoadReport(filterDates: false);
            };

            var hint = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(200, 210, 230),
                Font = new Font("Segoe UI", 9F),
                Location = new Point(400, 76),
                Text = "Expense Form entries · ReportViewer se PDF / print"
            };

            panelFilters.Controls.Add(Lbl("From", 12, 8));
            panelFilters.Controls.Add(dtpFrom);
            panelFilters.Controls.Add(Lbl("To", 190, 8));
            panelFilters.Controls.Add(dtpTo);
            panelFilters.Controls.Add(Lbl("Search (Name / Category / Note)", 370, 8));
            panelFilters.Controls.Add(txtSearch);
            panelFilters.Controls.Add(btnLoad);
            panelFilters.Controls.Add(btnClear);
            panelFilters.Controls.Add(hint);

            reportViewer1 = new ReportViewer
            {
                Dock = DockStyle.Fill,
                ProcessingMode = ProcessingMode.Local
            };

            Controls.Add(reportViewer1);
            Controls.Add(panelFilters);
            Controls.Add(panelTop);
        }

        private static Label Lbl(string t, int x, int y) => new Label
        {
            Text = t,
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(x, y),
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold)
        };

        private void LoadReport(bool filterDates)
        {
            try
            {
                var dt = BuildData(filterDates);
                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("Koi expense record nahi mila.", "Expense Report",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    reportViewer1.Clear();
                    return;
                }

                string reportPath = Path.Combine(Application.StartupPath, "Reports", "ExpenseReport.rdlc");
                if (!File.Exists(reportPath))
                {
                    MessageBox.Show("Report file not found:\n" + reportPath, "Expense Report",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                reportViewer1.Reset();
                reportViewer1.ProcessingMode = ProcessingMode.Local;
                reportViewer1.LocalReport.ReportPath = reportPath;
                try { ReportHeaderSocietyHelper.Apply(reportViewer1.LocalReport, this); } catch { }
                try
                {
                    reportViewer1.LocalReport.SetParameters(
                        new ReportParameter("HeaderBrand", "Hub Builders"));
                }
                catch { }
                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(new ReportDataSource("DataSet1", dt));
                reportViewer1.RefreshReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Expense Report", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DataTable BuildData(bool filterDates)
        {
            var dt = new DataTable();
            dt.Columns.Add("sn", typeof(int));
            dt.Columns.Add("edate", typeof(string));
            dt.Columns.Add("name", typeof(string));
            dt.Columns.Add("category", typeof(string));
            dt.Columns.Add("amount", typeof(double));
            dt.Columns.Add("note", typeof(string));

            string fromStr = dtpFrom.Value.Date.ToString("yyyy-MM-dd");
            string toStr = dtpTo.Value.Date.ToString("yyyy-MM-dd");
            string kw = (txtSearch.Text ?? "").Trim();

            using (var con = new SQLiteConnection(projectconnection.conReturn()))
            using (var cmd = new SQLiteCommand(@"
SELECT
  COALESCE(e.EDate,'') AS edate,
  COALESCE(e.Name,'') AS name,
  COALESCE(e.Category,'') AS category,
  COALESCE(e.Amount,0) AS amount,
  COALESCE(e.Note,'') AS note
FROM Expensetbl e
WHERE
  (@useDates = 0 OR (
       DATE(e.EDate) >= DATE(@from)
   AND DATE(e.EDate) < DATE(@to, '+1 day')
  ))
  AND (
       @kw = ''
    OR e.Name LIKE @kwLike COLLATE NOCASE
    OR IFNULL(e.Category,'') LIKE @kwLike COLLATE NOCASE
    OR IFNULL(e.Note,'') LIKE @kwLike COLLATE NOCASE
  )
ORDER BY DATE(e.EDate) ASC, e.sid ASC;", con))
            {
                con.Open();
                cmd.Parameters.AddWithValue("@useDates", filterDates ? 1 : 0);
                cmd.Parameters.AddWithValue("@from", fromStr);
                cmd.Parameters.AddWithValue("@to", toStr);
                cmd.Parameters.AddWithValue("@kw", kw);
                cmd.Parameters.AddWithValue("@kwLike", "%" + kw + "%");

                using (var r = cmd.ExecuteReader())
                {
                    int sn = 0;
                    while (r.Read())
                    {
                        sn++;
                        string d = Convert.ToString(r["edate"] ?? "").Trim();
                        if (d.Length >= 10) d = d.Substring(0, 10);
                        double amt = 0;
                        try { amt = Convert.ToDouble(r["amount"]); } catch { }
                        dt.Rows.Add(
                            sn,
                            d,
                            Convert.ToString(r["name"] ?? ""),
                            Convert.ToString(r["category"] ?? ""),
                            amt,
                            Convert.ToString(r["note"] ?? ""));
                    }
                }
            }
            return dt;
        }
    }
}
