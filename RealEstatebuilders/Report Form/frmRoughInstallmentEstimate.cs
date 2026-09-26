using HubBuilders.ProjectConnection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace HubBuilders.Report_Form
{
    /// <summary>
    /// VIP: Rough Installment Estimate (andaza) — when many Active allocations have
    /// monthly=0, still estimate monthly and show expected / collected / baqi.
    /// Additive DataGrid report; does not change sync.
    /// </summary>
    public class frmRoughInstallmentEstimate : Sample
    {
        private ComboBox cmbSociety;
        private DateTimePicker dtpFrom;
        private DateTimePicker dtpTo;
        private DataGridView dgv;
        private Label lblSummary;
        private Label lblRules;
        private Guna.UI2.WinForms.Guna2Button btnLoad;
        private Guna.UI2.WinForms.Guna2Button btnSavePdf;
        private bool _binding;
        private DataTable _lastTable;
        private string _lastSummary = "";
        private string _lastSocietyName = "";
        private string _lastPeriodLabel = "";

        public frmRoughInstallmentEstimate()
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
            Text = "Rough Installment Estimate (VIP)";
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
                Text = "Rough Installment Estimate (VIP) — ANDAZA"
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
                Height = 168,
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

            var lblFrom = Lbl("From", 250, 8);
            dtpFrom = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd-MMM-yyyy",
                Font = new Font("Segoe UI", 10F),
                Location = new Point(250, 30),
                Size = new Size(150, 28)
            };
            var lblTo = Lbl("To", 420, 8);
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
                Text = "Load Report",
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
            btnSavePdf.Click += (s, e) => SavePdf();

            lblSummary = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(180, 230, 210),
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Location = new Point(12, 68),
                MaximumSize = new Size(1400, 0),
                Text = ""
            };
            lblRules = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 200, 140),
                Font = new Font("Segoe UI", 8.5F),
                Location = new Point(12, 98),
                MaximumSize = new Size(1500, 0),
                Text =
                    "ROUGH / Estimate (andaza — exact contract nahi).  "
                    + "Monthly: Exact = plot pe set · Block = block default · Estimate = same Society+Block(+Gaz) average, "
                    + "warna Society average, warna (Price−Paid)/12.  "
                    + "Expected = Monthly × due months in From–To.  Le chuke = receipts in range.  Baqi = Expected − Le chuke."
            };

            panelFilters.Controls.Add(lblSoc);
            panelFilters.Controls.Add(cmbSociety);
            panelFilters.Controls.Add(lblFrom);
            panelFilters.Controls.Add(dtpFrom);
            panelFilters.Controls.Add(lblTo);
            panelFilters.Controls.Add(dtpTo);
            panelFilters.Controls.Add(btnLoad);
            panelFilters.Controls.Add(btnSavePdf);
            panelFilters.Controls.Add(lblSummary);
            panelFilters.Controls.Add(lblRules);

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

        private string GetSocietyName()
        {
            try
            {
                if (cmbSociety?.SelectedItem is DataRowView drv)
                    return Convert.ToString(drv["name"]) ?? "";
                return Convert.ToString(cmbSociety?.Text) ?? "";
            }
            catch { return ""; }
        }

        /// <summary>Same due-month rule as Installment Detection (VIP).</summary>
        internal static int CountDueMonthsInRange(DateTime booking, DateTime from, DateTime to)
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

        private static double ReadDouble(IDataRecord r, string col)
        {
            try
            {
                if (r[col] == null || r[col] == DBNull.Value) return 0;
                return Convert.ToDouble(r[col]);
            }
            catch { return 0; }
        }

        private static int ReadInt(IDataRecord r, string col)
        {
            try
            {
                if (r[col] == null || r[col] == DBNull.Value) return 0;
                return Convert.ToInt32(r[col]);
            }
            catch { return 0; }
        }

        private static string NormKey(string s)
        {
            return (s ?? "").Trim().ToUpperInvariant();
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

                int societyId = GetSocietyId();
                string fromYmd = from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                string toYmd = to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                try { BlockMonthlyInstallmentStore.EnsureSchema(); } catch { }

                // Averages of set monthlies (cp.monthly_installment > 0) for Estimate fallback.
                var avgSocBlockGaz = new Dictionary<string, AvgAcc>(StringComparer.Ordinal);
                var avgSocBlock = new Dictionary<string, AvgAcc>(StringComparer.Ordinal);
                var avgSoc = new Dictionary<string, AvgAcc>(StringComparer.Ordinal);

                var rows = new List<RowData>();

                using (var con = new SQLiteConnection(projectconnection.conReturn()))
                {
                    con.Open();
                    using (var cmd = new SQLiteCommand(@"
SELECT cp.id AS customer_plot_id, cp.customer_id, c.name AS customer_name,
  s.id AS society_id, s.name AS society_name,
  b.id AS block_id, b.name AS block_name,
  p.plot_no, COALESCE(NULLIF(TRIM(pt.code),''), '') AS gaz,
  cp.booking_date,
  COALESCE(cp.price, 0) AS price,
  COALESCE(cp.monthly_installment, 0) AS monthly_installment,
  COALESCE(bmi.monthly_amount, 0) AS block_monthly,
  COALESCE((
    SELECT SUM(r.amount) FROM receipts r WHERE r.customer_plot_id = cp.id
  ), 0) AS paid_life,
  COALESCE((
    SELECT SUM(r.amount) FROM receipts r
    WHERE r.customer_plot_id = cp.id
      AND r.receipt_date IS NOT NULL AND TRIM(r.receipt_date) <> ''
      AND substr(r.receipt_date,1,10) >= @from
      AND substr(r.receipt_date,1,10) <= @to
  ), 0) AS paid_in_period
FROM customer_plots cp
JOIN customers c ON c.id = cp.customer_id
JOIN plots p ON p.id = cp.plot_id
LEFT JOIN societies s ON s.id = p.society_id
LEFT JOIN blocks b ON b.id = p.block_id
LEFT JOIN plot_types pt ON pt.id = p.plot_type_id
LEFT JOIN block_monthly_installments bmi ON bmi.block_id = p.block_id
WHERE IFNULL(cp.status,'') = 'Active'
  AND (@sid = 0 OR p.society_id = @sid)
ORDER BY s.name, b.name, c.name, p.plot_no;", con))
                    {
                        cmd.Parameters.AddWithValue("@sid", societyId);
                        cmd.Parameters.AddWithValue("@from", fromYmd);
                        cmd.Parameters.AddWithValue("@to", toYmd);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                var row = new RowData
                                {
                                    CustomerId = ReadInt(r, "customer_id"),
                                    Customer = Convert.ToString(r["customer_name"]) ?? "",
                                    SocietyId = ReadInt(r, "society_id"),
                                    Society = Convert.ToString(r["society_name"]) ?? "",
                                    BlockId = ReadInt(r, "block_id"),
                                    Block = Convert.ToString(r["block_name"]) ?? "",
                                    Plot = Convert.ToString(r["plot_no"]) ?? "",
                                    Gaz = Convert.ToString(r["gaz"]) ?? "",
                                    Booking = ParseDate(r["booking_date"]),
                                    Price = ReadDouble(r, "price"),
                                    MonthlyExact = ReadDouble(r, "monthly_installment"),
                                    BlockMonthly = ReadDouble(r, "block_monthly"),
                                    PaidLife = ReadDouble(r, "paid_life"),
                                    PaidInPeriod = ReadDouble(r, "paid_in_period")
                                };
                                if (row.PaidLife < 0) row.PaidLife = 0;
                                if (row.PaidInPeriod < 0) row.PaidInPeriod = 0;
                                rows.Add(row);

                                if (row.MonthlyExact > 0.009)
                                {
                                    string soc = NormKey(row.Society);
                                    string blk = NormKey(row.Block);
                                    string gaz = NormKey(row.Gaz);
                                    AccAdd(avgSoc, soc, row.MonthlyExact);
                                    AccAdd(avgSocBlock, soc + "|" + blk, row.MonthlyExact);
                                    if (!string.IsNullOrEmpty(gaz))
                                        AccAdd(avgSocBlockGaz, soc + "|" + blk + "|" + gaz, row.MonthlyExact);
                                }
                            }
                        }
                    }
                }

                var table = new DataTable();
                table.Columns.Add("S.N", typeof(int));
                table.Columns.Add("Customer", typeof(string));
                table.Columns.Add("Society", typeof(string));
                table.Columns.Add("Block", typeof(string));
                table.Columns.Add("Plot", typeof(string));
                table.Columns.Add("Gaz", typeof(string));
                table.Columns.Add("Price", typeof(double));
                table.Columns.Add("Paid", typeof(double));
                table.Columns.Add("Due", typeof(double));
                table.Columns.Add("Monthly", typeof(double));
                table.Columns.Add("Source", typeof(string));
                table.Columns.Add("DueMonths", typeof(int));
                table.Columns.Add("Expected", typeof(double));
                table.Columns.Add("Collected", typeof(double));
                table.Columns.Add("Baqi", typeof(double));
                table.Columns.Add("Status", typeof(string));

                int sn = 0;
                int totalActive = 0;
                int cntExact = 0, cntBlock = 0, cntEstimate = 0;
                double sumExpected = 0, sumPaid = 0;

                foreach (var row in rows)
                {
                    totalActive++;
                    double due = row.Price - row.PaidLife;
                    if (due < 0) due = 0;

                    double monthly;
                    string source;
                    if (row.MonthlyExact > 0.009)
                    {
                        monthly = row.MonthlyExact;
                        source = "Exact";
                        cntExact++;
                    }
                    else if (row.BlockMonthly > 0.009)
                    {
                        monthly = row.BlockMonthly;
                        source = "Block";
                        cntBlock++;
                    }
                    else
                    {
                        monthly = ResolveEstimate(row, due, avgSocBlockGaz, avgSocBlock, avgSoc);
                        source = "Estimate";
                        cntEstimate++;
                    }

                    int dueMonths = 0;
                    double expected = 0;
                    string status;
                    if (monthly <= 0.009)
                    {
                        status = "No estimate";
                        monthly = 0;
                        expected = 0;
                    }
                    else
                    {
                        if (row.Booking != null)
                            dueMonths = CountDueMonthsInRange(row.Booking.Value, from, to);
                        expected = monthly * dueMonths;
                        if (expected <= 0.009)
                        {
                            status = "No due in range";
                            expected = 0;
                        }
                        else if (row.PaidInPeriod >= expected - 0.009) status = "Paid";
                        else if (row.PaidInPeriod > 0.009) status = "Partial";
                        else status = "Unpaid";
                    }

                    double baqi = expected - row.PaidInPeriod;
                    if (baqi < 0) baqi = 0;

                    sumExpected += expected;
                    sumPaid += row.PaidInPeriod;

                    sn++;
                    var nr = table.NewRow();
                    nr["S.N"] = sn;
                    nr["Customer"] = row.Customer;
                    nr["Society"] = row.Society;
                    nr["Block"] = row.Block;
                    nr["Plot"] = row.Plot;
                    nr["Gaz"] = string.IsNullOrWhiteSpace(row.Gaz) ? "—" : row.Gaz;
                    nr["Price"] = row.Price;
                    nr["Paid"] = row.PaidLife;
                    nr["Due"] = due;
                    nr["Monthly"] = monthly;
                    nr["Source"] = source;
                    nr["DueMonths"] = dueMonths;
                    nr["Expected"] = expected;
                    nr["Collected"] = row.PaidInPeriod;
                    nr["Baqi"] = baqi;
                    nr["Status"] = status;
                    table.Rows.Add(nr);
                }

                double sumRem = sumExpected - sumPaid;
                if (sumRem < 0) sumRem = 0;

                dgv.DataSource = table;
                StyleGrid(dgv);
                if (dgv.Columns.Contains("S.N"))
                {
                    dgv.Columns["S.N"].FillWeight = 35;
                    dgv.Columns["S.N"].DefaultCellStyle.Alignment =
                        DataGridViewContentAlignment.MiddleCenter;
                }
                if (dgv.Columns.Contains("Source"))
                    dgv.Columns["Source"].FillWeight = 55;
                if (dgv.Columns.Contains("Status"))
                    dgv.Columns["Status"].FillWeight = 70;
                foreach (var col in new[] { "Price", "Paid", "Due", "Monthly", "Expected", "Collected", "Baqi" })
                {
                    if (dgv.Columns.Contains(col))
                        dgv.Columns[col].DefaultCellStyle.Format = "N2";
                }

                var n = CultureInfo.InvariantCulture;
                string summary =
                    "ROUGH / Estimate   |   Total Active: " + totalActive
                    + "   |   Exact / Block / Estimate: " + cntExact + " / " + cntBlock + " / " + cntEstimate
                    + "   |   Total installment (andaza+exact): " + sumExpected.ToString("N2", n)
                    + "   |   Already collected (Le chuke): " + sumPaid.ToString("N2", n)
                    + "   |   Remaining (Baqi): " + sumRem.ToString("N2", n);
                lblSummary.Text = summary;

                _lastTable = table;
                _lastSummary = summary;
                _lastSocietyName = GetSocietyName();
                _lastPeriodLabel = from.ToString("dd-MMM-yyyy") + " → " + to.ToString("dd-MMM-yyyy");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Rough Installment Estimate (VIP)",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void AccAdd(Dictionary<string, AvgAcc> map, string key, double monthly)
        {
            if (string.IsNullOrEmpty(key)) return;
            AvgAcc acc;
            if (!map.TryGetValue(key, out acc))
            {
                acc = new AvgAcc();
                map[key] = acc;
            }
            acc.Sum += monthly;
            acc.Count++;
        }

        private static double? AccAvg(Dictionary<string, AvgAcc> map, string key)
        {
            AvgAcc acc;
            if (string.IsNullOrEmpty(key) || !map.TryGetValue(key, out acc) || acc.Count < 1)
                return null;
            return acc.Sum / acc.Count;
        }

        private static double ResolveEstimate(
            RowData row,
            double due,
            Dictionary<string, AvgAcc> avgSocBlockGaz,
            Dictionary<string, AvgAcc> avgSocBlock,
            Dictionary<string, AvgAcc> avgSoc)
        {
            string soc = NormKey(row.Society);
            string blk = NormKey(row.Block);
            string gaz = NormKey(row.Gaz);

            double? avg = null;
            if (!string.IsNullOrEmpty(gaz))
                avg = AccAvg(avgSocBlockGaz, soc + "|" + blk + "|" + gaz);
            if (avg == null || avg.Value <= 0.009)
                avg = AccAvg(avgSocBlock, soc + "|" + blk);
            if (avg == null || avg.Value <= 0.009)
                avg = AccAvg(avgSoc, soc);

            if (avg != null && avg.Value > 0.009)
                return Math.Round(avg.Value, 0, MidpointRounding.AwayFromZero);

            if (due <= 0.009) return 0;
            double fallback = Math.Round(due / 12.0, 0, MidpointRounding.AwayFromZero);
            if (fallback < 1) fallback = 1;
            return fallback;
        }

        private void SavePdf()
        {
            try
            {
                if (_lastTable == null)
                    LoadReport();
                if (_lastTable == null || _lastTable.Rows.Count == 0)
                {
                    MessageBox.Show("Pehle Load Report karein.", "Save PDF",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using (var dlg = new SaveFileDialog())
                {
                    dlg.Title = "Save PDF";
                    dlg.Filter = "PDF (*.pdf)|*.pdf";
                    dlg.FileName = "HubBuilders_RoughInstallmentEstimate_"
                        + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf";
                    string today = HubAutoBackup.TodayFolder();
                    if (!string.IsNullOrWhiteSpace(today))
                    {
                        try { System.IO.Directory.CreateDirectory(today); dlg.InitialDirectory = today; } catch { }
                    }
                    if (dlg.ShowDialog() != DialogResult.OK) return;
                    System.IO.File.WriteAllBytes(dlg.FileName, BuildPdfBytes());
                    MessageBox.Show("PDF save ho gayi:\n" + dlg.FileName, "Save PDF",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Save PDF", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private byte[] BuildPdfBytes()
        {
            var pages = BuildBitmaps();
            try { return JpegPdf.FromBitmaps(pages, 842, 595); }
            finally { foreach (var b in pages) b.Dispose(); }
        }

        private List<Bitmap> BuildBitmaps()
        {
            const int w = 1754;
            const int h = 1240;
            var pages = new List<Bitmap>();
            int rowH = 20;
            int top = 110;
            int bottom = h - 28;

            var lineRows = new List<string[]>();
            if (_lastTable != null)
            {
                foreach (DataRow r in _lastTable.Rows)
                {
                    lineRows.Add(new[]
                    {
                        Convert.ToString(r["S.N"]) ?? "",
                        Convert.ToString(r["Customer"]) ?? "",
                        Convert.ToString(r["Society"]) ?? "",
                        Convert.ToString(r["Block"]) ?? "",
                        Convert.ToString(r["Plot"]) ?? "",
                        Convert.ToString(r["Gaz"]) ?? "",
                        FormatN(r["Monthly"]),
                        Convert.ToString(r["Source"]) ?? "",
                        FormatN(r["Expected"]),
                        FormatN(r["Collected"]),
                        FormatN(r["Baqi"]),
                        Convert.ToString(r["Status"]) ?? ""
                    });
                }
            }

            string[] headers =
            {
                "S.N", "Customer", "Society", "Block", "Plot", "Gaz",
                "Monthly", "Source", "Expected", "Collected", "Baqi", "Status"
            };
            float[] weights = { 0.04f, 0.16f, 0.10f, 0.08f, 0.06f, 0.06f, 0.08f, 0.07f, 0.09f, 0.09f, 0.08f, 0.09f };

            int idx = 0;
            int pageNo = 0;
            while (idx < lineRows.Count || pageNo == 0)
            {
                pageNo++;
                var bmp = new Bitmap(w, h);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.White);
                    using (var titleFont = new Font("Segoe UI", 16F, FontStyle.Bold))
                    using (var small = new Font("Segoe UI", 9F))
                    using (var bold = new Font("Segoe UI Semibold", 9F, FontStyle.Bold))
                    {
                        g.DrawString("Rough Installment Estimate (VIP) — ROUGH / ANDAZA", titleFont, Brushes.Black, 24, 18);
                        g.DrawString("Society: " + _lastSocietyName + "   ·   Period: " + _lastPeriodLabel
                            + "   ·   Page " + pageNo, small, Brushes.DimGray, 24, 48);
                        g.DrawString(_lastSummary ?? "", bold, Brushes.DarkSlateGray, new RectangleF(24, 68, w - 48, 36));

                        float x0 = 20;
                        float usable = w - 40;
                        float[] xs = new float[weights.Length];
                        float x = x0;
                        for (int i = 0; i < weights.Length; i++)
                        {
                            xs[i] = x;
                            x += usable * weights[i];
                        }

                        int y = top;
                        using (var hdrBrush = new SolidBrush(Color.FromArgb(20, 45, 95)))
                        {
                            g.FillRectangle(hdrBrush, 16, y, w - 32, rowH + 4);
                            for (int i = 0; i < headers.Length; i++)
                                g.DrawString(headers[i], bold, Brushes.White, xs[i], y + 2);
                        }
                        y += rowH + 6;

                        int drawn = 0;
                        while (idx < lineRows.Count && y + rowH < bottom)
                        {
                            var cells = lineRows[idx];
                            if (drawn % 2 == 1)
                                g.FillRectangle(new SolidBrush(Color.FromArgb(245, 247, 252)), 16, y - 1, w - 32, rowH);
                            for (int i = 0; i < cells.Length && i < xs.Length; i++)
                                g.DrawString(TrimPdf(cells[i], i == 1 ? 22 : 14), small, Brushes.Black, xs[i], y);
                            y += rowH;
                            idx++;
                            drawn++;
                        }
                    }
                }
                pages.Add(bmp);
                if (idx >= lineRows.Count) break;
            }
            return pages;
        }

        private static string FormatN(object v)
        {
            try
            {
                if (v == null || v == DBNull.Value) return "0";
                return Convert.ToDouble(v).ToString("N0", CultureInfo.InvariantCulture);
            }
            catch { return Convert.ToString(v) ?? ""; }
        }

        private static string TrimPdf(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Trim();
            if (s.Length <= max) return s;
            return s.Substring(0, max - 1) + "…";
        }

        private sealed class AvgAcc
        {
            public double Sum;
            public int Count;
        }

        private sealed class RowData
        {
            public int CustomerId;
            public string Customer;
            public int SocietyId;
            public string Society;
            public int BlockId;
            public string Block;
            public string Plot;
            public string Gaz;
            public DateTime? Booking;
            public double Price;
            public double MonthlyExact;
            public double BlockMonthly;
            public double PaidLife;
            public double PaidInPeriod;
        }

        /// <summary>Same JPEG-in-PDF approach as Month Collection (VIP).</summary>
        private static class JpegPdf
        {
            public static byte[] FromBitmaps(List<Bitmap> pages, int pageW, int pageH)
            {
                var jpegs = new List<byte[]>();
                var codec = ImageCodecInfo.GetImageEncoders().FirstOrDefault(c => c.MimeType == "image/jpeg");
                using (var enc = new EncoderParameters(1))
                {
                    enc.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 92L);
                    foreach (var bmp in pages)
                    {
                        using (var ms = new MemoryStream())
                        {
                            if (codec != null) bmp.Save(ms, codec, enc);
                            else bmp.Save(ms, ImageFormat.Jpeg);
                            jpegs.Add(ms.ToArray());
                        }
                    }
                }

                var kids = new StringBuilder();
                for (int i = 0; i < jpegs.Count; i++)
                {
                    if (i > 0) kids.Append(" ");
                    kids.Append((3 + i * 3)).Append(" 0 R");
                }

                using (var ms = new MemoryStream())
                {
                    var latin = Encoding.ASCII;
                    var rebuiltOffsets = new List<long>();
                    Action<string> writeAscii = s =>
                    {
                        var b = latin.GetBytes(s);
                        ms.Write(b, 0, b.Length);
                    };

                    writeAscii("%PDF-1.4\n");
                    rebuiltOffsets.Add(ms.Position);
                    writeAscii("1 0 obj<< /Type /Catalog /Pages 2 0 R >>endobj\n");
                    rebuiltOffsets.Add(ms.Position);
                    writeAscii("2 0 obj<< /Type /Pages /Count " + jpegs.Count + " /Kids [" + kids + "] >>endobj\n");
                    int obj = 3;
                    for (int i = 0; i < jpegs.Count; i++)
                    {
                        int imgObj = obj + 1;
                        int contObj = obj + 2;
                        string content = "q " + pageW + " 0 0 " + pageH + " 0 0 cm /Im0 Do Q\n";
                        rebuiltOffsets.Add(ms.Position);
                        writeAscii(obj + " 0 obj<< /Type /Page /Parent 2 0 R /MediaBox [0 0 " + pageW + " " + pageH +
                          "] /Resources << /XObject << /Im0 " + imgObj + " 0 R >> >> /Contents " + contObj + " 0 R >>endobj\n");
                        rebuiltOffsets.Add(ms.Position);
                        writeAscii(imgObj + " 0 obj<< /Type /XObject /Subtype /Image /Width " + pages[i].Width +
                          " /Height " + pages[i].Height + " /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length " +
                          jpegs[i].Length + " >>stream\n");
                        ms.Write(jpegs[i], 0, jpegs[i].Length);
                        writeAscii("\nendstream\nendobj\n");
                        rebuiltOffsets.Add(ms.Position);
                        writeAscii(contObj + " 0 obj<< /Length " + content.Length + " >>stream\n" + content + "endstream\nendobj\n");
                        obj += 3;
                    }
                    long xref = ms.Position;
                    int nObj = rebuiltOffsets.Count + 1;
                    writeAscii("xref\n0 " + nObj + "\n");
                    writeAscii("0000000000 65535 f \n");
                    foreach (var off in rebuiltOffsets)
                        writeAscii(off.ToString("D10") + " 00000 n \n");
                    writeAscii("trailer<< /Size " + nObj + " /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n");
                    return ms.ToArray();
                }
            }
        }
    }
}
