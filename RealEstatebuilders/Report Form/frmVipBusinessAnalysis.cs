using HubBuilders.ProjectConnection;
using Microsoft.Reporting.WinForms;
using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace HubBuilders.Report_Form
{
    /// <summary>
    /// VIP: Complete Business Analysis — allocate / paid / baqi / period collection /
    /// installment target / expenses / net. RDLC + PDF/Excel. Additive; no sync change.
    /// </summary>
    public class frmVipBusinessAnalysis : Sample
    {
        private ComboBox cmbSociety;
        private DateTimePicker dtpFrom;
        private DateTimePicker dtpTo;
        private Guna.UI2.WinForms.Guna2Button btnLoad;
        private Guna.UI2.WinForms.Guna2Button btnSavePdf;
        private Guna.UI2.WinForms.Guna2Button btnSaveExcel;
        private ReportViewer reportViewer1;
        private Label lblHint;
        private bool _binding;

        public frmVipBusinessAnalysis()
        {
            InitializeUi();
            LoadSocieties();
            var today = DateTime.Today;
            dtpFrom.Value = new DateTime(today.Year, today.Month, 1);
            dtpTo.Value = today;
            LoadReport();
        }

        private void InitializeUi()
        {
            Text = "Complete Business Analysis (VIP)";
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
                Text = "Complete Business Analysis (VIP)"
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
                Height = 120,
                Padding = new Padding(12),
                BackColor = Color.FromArgb(32, 36, 61)
            };
            var lblSoc = Lbl("Society", 12, 8);
            cmbSociety = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F),
                Location = new Point(12, 30),
                Size = new Size(220, 28)
            };
            cmbSociety.SelectedIndexChanged += (s, e) =>
            {
                if (!_binding) LoadReport();
            };

            var lblFrom = Lbl("From date", 250, 8);
            dtpFrom = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd-MMM-yyyy",
                Font = new Font("Segoe UI", 10F),
                Location = new Point(250, 30),
                Size = new Size(150, 28)
            };
            var lblTo = Lbl("To date", 420, 8);
            dtpTo = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd-MMM-yyyy",
                Font = new Font("Segoe UI", 10F),
                Location = new Point(420, 30),
                Size = new Size(150, 28)
            };

            btnLoad = new Guna.UI2.WinForms.Guna2Button
            {
                Text = "Show Report",
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                FillColor = Color.FromArgb(18, 140, 126),
                ForeColor = Color.White,
                BorderRadius = 8,
                Location = new Point(590, 26),
                Size = new Size(140, 36)
            };
            btnLoad.Click += (s, e) => LoadReport();

            btnSavePdf = new Guna.UI2.WinForms.Guna2Button
            {
                Text = "Save PDF",
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                FillColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                BorderRadius = 8,
                Location = new Point(740, 26),
                Size = new Size(120, 36)
            };
            btnSavePdf.Click += (s, e) => ExportFile("PDF", "PDF (*.pdf)|*.pdf", ".pdf");

            btnSaveExcel = new Guna.UI2.WinForms.Guna2Button
            {
                Text = "Save Excel",
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                FillColor = Color.FromArgb(46, 125, 50),
                ForeColor = Color.White,
                BorderRadius = 8,
                Location = new Point(870, 26),
                Size = new Size(130, 36)
            };
            btnSaveExcel.Click += (s, e) => ExportFile("EXCELOPENXML",
                "Excel (*.xlsx)|*.xlsx", ".xlsx");

            lblHint = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(200, 210, 230),
                Font = new Font("Segoe UI", 9F),
                Location = new Point(12, 72),
                MaximumSize = new Size(1100, 0),
                Text = "Owner glance: Total Allocate · Lifetime Paid · Baqi Lena · Is period wasool · Installment target · Kharcha · Net"
            };

            panelFilters.Controls.Add(lblSoc);
            panelFilters.Controls.Add(cmbSociety);
            panelFilters.Controls.Add(lblFrom);
            panelFilters.Controls.Add(dtpFrom);
            panelFilters.Controls.Add(lblTo);
            panelFilters.Controls.Add(dtpTo);
            panelFilters.Controls.Add(btnLoad);
            panelFilters.Controls.Add(btnSavePdf);
            panelFilters.Controls.Add(btnSaveExcel);
            panelFilters.Controls.Add(lblHint);

            reportViewer1 = new ReportViewer
            {
                Dock = DockStyle.Fill,
                ProcessingMode = ProcessingMode.Local
            };

            Controls.Add(reportViewer1);
            Controls.Add(panelFilters);
            Controls.Add(panelTop);
        }

        private static Label Lbl(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(x, y),
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold)
            };
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
                using (var cmd = new SQLiteCommand(
                    "SELECT id, name FROM societies ORDER BY name COLLATE NOCASE;", con))
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

        private string GetSocietyLabel()
        {
            try
            {
                if (cmbSociety?.SelectedItem is DataRowView drv)
                    return Convert.ToString(drv["name"]) ?? "(All Societies)";
                return cmbSociety?.Text ?? "(All Societies)";
            }
            catch { return "(All Societies)"; }
        }

        private void LoadReport()
        {
            if (_binding) return;
            try
            {
                DateTime from = dtpFrom.Value.Date;
                DateTime to = dtpTo.Value.Date;
                if (to < from)
                {
                    MessageBox.Show("To date From se pehle nahi ho sakti.", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var dt = BuildAnalysis(from, to, GetSocietyId());
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "BusinessAnalysis.rdlc");
                if (!File.Exists(reportPath))
                {
                    MessageBox.Show("Report file not found:\n" + reportPath,
                        "Complete Business Analysis (VIP)",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string fromLbl = from.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture);
                string toLbl = to.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture);
                string rangeLbl = fromLbl + "  →  " + toLbl;
                string socLbl = GetSocietyLabel();

                reportViewer1.Reset();
                reportViewer1.ProcessingMode = ProcessingMode.Local;
                reportViewer1.LocalReport.ReportPath = reportPath;
                try { ReportHeaderSocietyHelper.Apply(reportViewer1.LocalReport, this); } catch { }
                try
                {
                    reportViewer1.LocalReport.SetParameters(new[]
                    {
                        new ReportParameter("HeaderBrand", "Hub Builders"),
                        new ReportParameter("DateRange", rangeLbl),
                        new ReportParameter("SocietyName", socLbl)
                    });
                }
                catch { }

                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(new ReportDataSource("DataSet1", dt));
                reportViewer1.RefreshReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Complete Business Analysis (VIP)",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportFile(string format, string filter, string ext)
        {
            try
            {
                LoadReport();
                string mime, enc, fext;
                Warning[] warnings;
                string[] streams;
                byte[] bytes = reportViewer1.LocalReport.Render(
                    format, null, out mime, out enc, out fext, out streams, out warnings);
                if (bytes == null || bytes.Length == 0)
                {
                    MessageBox.Show("File ban nahi saki. Pehle Show Report karein.",
                        "Export", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                using (var dlg = new SaveFileDialog())
                {
                    dlg.Title = "Save " + (ext == ".pdf" ? "PDF" : "Excel");
                    dlg.Filter = filter;
                    dlg.FileName = "BusinessAnalysis_" +
                        DateTime.Now.ToString("yyyyMMdd_HHmmss") + ext;
                    try
                    {
                        string today = HubAutoBackup.TodayFolder();
                        if (!string.IsNullOrWhiteSpace(today))
                        {
                            Directory.CreateDirectory(today);
                            dlg.InitialDirectory = today;
                        }
                    }
                    catch { }
                    if (dlg.ShowDialog() != DialogResult.OK) return;
                    File.WriteAllBytes(dlg.FileName, bytes);
                    MessageBox.Show("Save ho gaya:\n" + dlg.FileName, "Export",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Export error: " + ex.Message, "Export",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Same as Installment Detection: calendar months after booking overlapping [from,to].
        /// </summary>
        private static int CountDueMonthsInRange(DateTime booking, DateTime from, DateTime to)
        {
            return frmInstallmentDetectionReport.CountDueMonthsInRange(booking, from, to);
        }

        private static DateTime? ParseDate(object raw)
        {
            if (raw == null || raw == DBNull.Value) return null;
            string s = Convert.ToString(raw)?.Trim() ?? "";
            if (s.Length >= 10)
            {
                DateTime d;
                if (DateTime.TryParseExact(s.Substring(0, 10), "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
                    return d.Date;
                if (DateTime.TryParse(s.Substring(0, 10), out d)) return d.Date;
            }
            DateTime d2;
            if (DateTime.TryParse(s, out d2)) return d2.Date;
            return null;
        }

        private DataTable BuildAnalysis(DateTime from, DateTime to, int societyId)
        {
            string fromYmd = from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string toYmd = to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            try { BlockMonthlyInstallmentStore.EnsureSchema(); } catch { }

            int activePlots = 0;
            int customersWithDues = 0;
            double totalAllocate = 0, lifetimePaid = 0;
            double periodCollection = 0, periodExpense = 0;
            double expectedTarget = 0, targetCollected = 0, remainingTarget = 0;
            int installmentDuePlots = 0;
            int paidOk = 0, partial = 0, unpaid = 0;

            using (var con = new SQLiteConnection(projectconnection.conReturn()))
            {
                con.Open();

                // Lifetime allocate / paid / baqi + active plot count + dues customers
                using (var cmd = new SQLiteCommand(@"
SELECT
  cp.id AS customer_plot_id,
  cp.customer_id,
  COALESCE(cp.price,0) AS price,
  COALESCE((
    SELECT SUM(r.amount) FROM receipts r WHERE r.customer_plot_id = cp.id
  ), 0) AS paid_life
FROM customer_plots cp
JOIN plots p ON p.id = cp.plot_id
WHERE IFNULL(cp.status,'') = 'Active'
  AND (@sid = 0 OR p.society_id = @sid);", con))
                {
                    cmd.Parameters.AddWithValue("@sid", societyId);
                    var seenDuesCust = new System.Collections.Generic.HashSet<int>();
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            activePlots++;
                            double price = 0, paid = 0;
                            try { price = Convert.ToDouble(r["price"]); } catch { }
                            try { paid = Convert.ToDouble(r["paid_life"]); } catch { }
                            totalAllocate += price;
                            lifetimePaid += paid;
                            if (price - paid > 0.009)
                            {
                                int cid = 0;
                                try { cid = Convert.ToInt32(r["customer_id"]); } catch { }
                                if (cid > 0) seenDuesCust.Add(cid);
                            }
                        }
                    }
                    customersWithDues = seenDuesCust.Count;
                }

                // Period collection (receipts in From–To for active allocations / society)
                using (var cmd = new SQLiteCommand(@"
SELECT COALESCE(SUM(r.amount),0) AS t
FROM receipts r
JOIN customer_plots cp ON cp.id = r.customer_plot_id
JOIN plots p ON p.id = cp.plot_id
WHERE IFNULL(cp.status,'') = 'Active'
  AND r.receipt_date IS NOT NULL AND TRIM(r.receipt_date) <> ''
  AND substr(r.receipt_date,1,10) >= @from
  AND substr(r.receipt_date,1,10) <= @to
  AND (@sid = 0 OR p.society_id = @sid);", con))
                {
                    cmd.Parameters.AddWithValue("@from", fromYmd);
                    cmd.Parameters.AddWithValue("@to", toYmd);
                    cmd.Parameters.AddWithValue("@sid", societyId);
                    object o = cmd.ExecuteScalar();
                    try { periodCollection = Convert.ToDouble(o); } catch { periodCollection = 0; }
                }

                // Period expenses
                using (var cmd = new SQLiteCommand(@"
SELECT COALESCE(SUM(COALESCE(e.Amount,0)),0) AS t
FROM Expensetbl e
WHERE DATE(e.EDate) >= DATE(@from)
  AND DATE(e.EDate) < DATE(@to, '+1 day');", con))
                {
                    cmd.Parameters.AddWithValue("@from", fromYmd);
                    cmd.Parameters.AddWithValue("@to", toYmd);
                    object o = cmd.ExecuteScalar();
                    try { periodExpense = Convert.ToDouble(o); } catch { periodExpense = 0; }
                }

                // Installment target (same spirit as Installment Detection)
                using (var cmd = new SQLiteCommand(@"
SELECT cp.id AS customer_plot_id, cp.booking_date,
  COALESCE(cp.monthly_installment, 0) AS monthly_installment,
  COALESCE(bmi.monthly_amount, 0) AS block_monthly,
  COALESCE((
    SELECT SUM(r.amount) FROM receipts r
    WHERE r.customer_plot_id = cp.id
      AND r.receipt_date IS NOT NULL AND TRIM(r.receipt_date) <> ''
      AND substr(r.receipt_date,1,10) >= @from
      AND substr(r.receipt_date,1,10) <= @to
  ), 0) AS paid_in_period
FROM customer_plots cp
JOIN plots p ON p.id = cp.plot_id
LEFT JOIN block_monthly_installments bmi ON bmi.block_id = p.block_id
WHERE IFNULL(cp.status,'') = 'Active'
  AND (@sid = 0 OR p.society_id = @sid);", con))
                {
                    cmd.Parameters.AddWithValue("@sid", societyId);
                    cmd.Parameters.AddWithValue("@from", fromYmd);
                    cmd.Parameters.AddWithValue("@to", toYmd);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            double monthly = 0;
                            try { monthly = Convert.ToDouble(r["monthly_installment"]); } catch { }
                            if (monthly <= 0)
                            {
                                try { monthly = Convert.ToDouble(r["block_monthly"]); } catch { }
                            }
                            if (monthly <= 0) continue;

                            DateTime? booking = ParseDate(r["booking_date"]);
                            if (booking == null) continue;

                            int dueMonths = CountDueMonthsInRange(booking.Value, from, to);
                            if (dueMonths <= 0) continue;

                            double expected = monthly * dueMonths;
                            double paid = 0;
                            try { paid = Convert.ToDouble(r["paid_in_period"]); } catch { }
                            if (paid < 0) paid = 0;

                            installmentDuePlots++;
                            expectedTarget += expected;
                            targetCollected += paid;
                            double rem = expected - paid;
                            if (rem > 0.009) remainingTarget += rem;

                            if (paid >= expected - 0.009) paidOk++;
                            else if (paid > 0.009) partial++;
                            else unpaid++;
                        }
                    }
                }
            }

            double baqiLena = totalAllocate - lifetimePaid;
            double periodNet = periodCollection - periodExpense;

            var table = new DataTable();
            table.Columns.Add("sn", typeof(int));
            table.Columns.Add("section", typeof(string));
            table.Columns.Add("particular", typeof(string));
            table.Columns.Add("amount", typeof(double));
            table.Columns.Add("note", typeof(string));

            int sn = 0;
            void Add(string section, string particular, double amount, string note)
            {
                sn++;
                table.Rows.Add(sn, section, particular, amount, note);
            }

            Add("1. Lifetime (Ab tak)", "Total Allocate Amount", totalAllocate,
                "Active plots ki total price");
            Add("1. Lifetime (Ab tak)", "Total Received from Customers", lifetimePaid,
                "Ab tak jitna customer ne diya (sab receipts)");
            Add("1. Lifetime (Ab tak)", "Still to Collect (Baqi Lena)", baqiLena,
                "Allocate − Paid (jo abhi lena baqi hai)");

            Add("2. This Period (From–To)", "This Period Collection", periodCollection,
                "Is date range mein kitna wasool hua");
            Add("2. This Period (From–To)", "Total Expenses (Period)", periodExpense,
                "Is date range ka total kharcha");
            Add("2. This Period (From–To)", "Net (Collection − Expense)", periodNet,
                "Wasool minus kharcha (period)");

            Add("3. Installment Target (Period)", "Expected Target (Installment)", expectedTarget,
                "Active plots ka expected installment (due months)");
            Add("3. Installment Target (Period)", "Already Collected (toward target)", targetCollected,
                "Is period mein installment-due plots se wasool");
            Add("3. Installment Target (Period)", "Remaining Target", remainingTarget,
                "Jo abhi lena baqi — fully paid plots is mein nahi");

            Add("4. Counts", "Active Plots", activePlots, "Active allocated plots");
            Add("4. Counts", "Customers with Dues (Baqi)", customersWithDues,
                "Jin customers ka abhi baqi lena hai");
            Add("4. Counts", "Installment-due Plots (Period)", installmentDuePlots,
                "Is range mein jinke installment due thay");
            Add("4. Counts", "Installment Status (Paid / Partial / Unpaid)", 0,
                paidOk + " / " + partial + " / " + unpaid);

            return table;
        }
    }
}
