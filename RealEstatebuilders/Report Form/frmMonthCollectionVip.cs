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
    /// VIP: Monthly installment collection (wasool) — Installment Groups (by monthly amount),
    /// current + previous month totals, receipt detail with partner / "paise lene wala"
    /// from Receipt Form (partner_money). Additive DataGrid report; does not change sync.
    /// </summary>
    public class frmMonthCollectionVip : Sample
    {
        private ComboBox cmbSociety;
        private ComboBox cmbScope;
        private DateTimePicker dtpFrom;
        private DateTimePicker dtpTo;
        private DataGridView dgv;
        private DataGridView dgvGroups;
        private Label lblSummary;
        private Label lblGroupsTitle;
        private Guna.UI2.WinForms.Guna2Button btnLoad;
        private Guna.UI2.WinForms.Guna2Button btnSavePdf;
        private bool _binding;
        private bool _scopeChanging;

        // Last loaded snapshot for PDF (no re-query on export).
        private DataTable _lastGroups;
        private DataTable _lastDetail;
        private string _lastSummaryLine1 = "";
        private string _lastSummaryLine2 = "";
        private string _lastSocietyName = "";
        private string _lastPeriodLabel = "";
        private double _lastGross;
        private double _lastCollection;
        private double _lastBaqi;

        public frmMonthCollectionVip()
        {
            InitializeUi();
            LoadSocieties();
            ApplyScopeDates(silent: true);
            LoadReport();
        }

        private void InitializeUi()
        {
            Text = "Month Collection (VIP)";
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
                Text = "Month Collection (VIP)"
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
                Height = 175,
                Padding = new Padding(12),
                BackColor = Color.FromArgb(32, 36, 61)
            };
            var lblSoc = Lbl("Society", 12, 8);
            cmbSociety = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F),
                Location = new Point(12, 30),
                Size = new Size(200, 28)
            };
            cmbSociety.SelectedIndexChanged += (s, e) =>
            {
                if (!_binding) LoadReport();
            };

            var lblScope = Lbl("Detail", 230, 8);
            cmbScope = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F),
                Location = new Point(230, 30),
                Size = new Size(170, 28)
            };
            cmbScope.Items.AddRange(new object[]
            {
                "Current month",
                "Previous month",
                "Custom From–To"
            });
            cmbScope.SelectedIndex = 0;
            cmbScope.SelectedIndexChanged += (s, e) =>
            {
                if (_binding || _scopeChanging) return;
                ApplyScopeDates(silent: false);
            };

            var lblFrom = Lbl("From", 420, 8);
            dtpFrom = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd-MMM-yyyy",
                Font = new Font("Segoe UI", 10F),
                Location = new Point(420, 30),
                Size = new Size(140, 28)
            };
            var lblTo = Lbl("To", 580, 8);
            dtpTo = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd-MMM-yyyy",
                Font = new Font("Segoe UI", 10F),
                Location = new Point(580, 30),
                Size = new Size(140, 28)
            };

            btnLoad = new Guna.UI2.WinForms.Guna2Button
            {
                Text = "Load Report",
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                FillColor = Color.FromArgb(18, 140, 126),
                ForeColor = Color.White,
                BorderRadius = 8,
                Location = new Point(740, 26),
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
                Location = new Point(890, 26),
                Size = new Size(120, 36)
            };
            btnSavePdf.Click += (s, e) => SavePdf();

            lblSummary = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(180, 230, 210),
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                Location = new Point(12, 72),
                MaximumSize = new Size(1400, 0),
                Text = ""
            };

            panelFilters.Controls.Add(lblSoc);
            panelFilters.Controls.Add(cmbSociety);
            panelFilters.Controls.Add(lblScope);
            panelFilters.Controls.Add(cmbScope);
            panelFilters.Controls.Add(lblFrom);
            panelFilters.Controls.Add(dtpFrom);
            panelFilters.Controls.Add(lblTo);
            panelFilters.Controls.Add(dtpTo);
            panelFilters.Controls.Add(btnLoad);
            panelFilters.Controls.Add(btnSavePdf);
            panelFilters.Controls.Add(lblSummary);

            var panelGroups = new Panel
            {
                Dock = DockStyle.Top,
                Height = 260,
                Padding = new Padding(12, 4, 12, 8),
                BackColor = Color.FromArgb(32, 36, 61)
            };
            lblGroupsTitle = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(180, 230, 210),
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                Location = new Point(12, 2),
                Text = "Installment Groups (Active)"
            };
            dgvGroups = new DataGridView
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                Location = new Point(12, 28),
                Size = new Size(panelGroups.Width - 24, 170),
                AllowUserToAddRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.FromArgb(32, 36, 66),
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false
            };
            StyleGrid(dgvGroups);
            panelGroups.Controls.Add(lblGroupsTitle);
            panelGroups.Controls.Add(dgvGroups);
            panelGroups.Resize += (s, e) =>
            {
                dgvGroups.Width = Math.Max(100, panelGroups.Width - 24);
                dgvGroups.Height = Math.Max(80, panelGroups.Height - 40);
            };

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
            Controls.Add(panelGroups);
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

        private void ApplyScopeDates(bool silent)
        {
            var today = DateTime.Today;
            var curStart = new DateTime(today.Year, today.Month, 1);
            var prevStart = curStart.AddMonths(-1);
            var prevEnd = curStart.AddDays(-1);

            _scopeChanging = true;
            try
            {
                int idx = cmbScope?.SelectedIndex ?? 0;
                if (idx == 0)
                {
                    dtpFrom.Value = curStart;
                    dtpTo.Value = today;
                }
                else if (idx == 1)
                {
                    dtpFrom.Value = prevStart;
                    dtpTo.Value = prevEnd;
                }
            }
            finally
            {
                _scopeChanging = false;
            }

            if (!silent && !_binding) LoadReport();
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

        private static double SumReceipts(SQLiteConnection con, int societyId, string fromYmd, string toYmd)
        {
            using (var cmd = new SQLiteCommand(@"
SELECT COALESCE(SUM(r.amount),0) AS t
FROM receipts r
JOIN customer_plots cp ON cp.id = r.customer_plot_id
JOIN plots p ON p.id = cp.plot_id
WHERE r.receipt_date IS NOT NULL AND TRIM(r.receipt_date) <> ''
  AND substr(r.receipt_date,1,10) >= @from
  AND substr(r.receipt_date,1,10) <= @to
  AND (@sid = 0 OR p.society_id = @sid);", con))
            {
                cmd.Parameters.AddWithValue("@from", fromYmd);
                cmd.Parameters.AddWithValue("@to", toYmd);
                cmd.Parameters.AddWithValue("@sid", societyId);
                var o = cmd.ExecuteScalar();
                if (o == null || o == DBNull.Value) return 0;
                return Convert.ToDouble(o);
            }
        }

        /// <summary>
        /// Active allocations grouped by Society | Block | monthly only
        /// (gaz dropped from key — distinct sizes joined in Gaz column).
        /// Grid rows: monthly &gt; 0 only. One "Not set" summary for monthly=0 count.
        /// Gross Total = sum of set group totals only.
        /// </summary>
        private static DataTable LoadInstallmentGroups(SQLiteConnection con, int societyId,
            out int activeCount, out int setCount, out int notSetCount, out double expectedTotal)
        {
            activeCount = 0;
            setCount = 0;
            notSetCount = 0;
            expectedTotal = 0;

            var groups = new DataTable();
            groups.Columns.Add("S.N", typeof(string));
            groups.Columns.Add("Society", typeof(string));
            groups.Columns.Add("Block", typeof(string));
            groups.Columns.Add("Gaz", typeof(string));
            groups.Columns.Add("Monthly", typeof(string));
            groups.Columns.Add("Customers", typeof(string));
            groups.Columns.Add("ExpectedTotal", typeof(string));
            groups.Columns.Add("Summary", typeof(string));

            using (var cmd = new SQLiteCommand(@"
SELECT society_name, block_name,
  REPLACE(GROUP_CONCAT(DISTINCT gaz), ',', ', ') AS gaz,
  monthly, COUNT(*) AS customers
FROM (
  SELECT
    COALESCE(NULLIF(TRIM(s.name),''), '—') AS society_name,
    COALESCE(NULLIF(TRIM(b.name),''), '—') AS block_name,
    COALESCE(NULLIF(TRIM(pt.code),''), '—') AS gaz,
    CASE
      WHEN COALESCE(cp.monthly_installment, 0) > 0 THEN COALESCE(cp.monthly_installment, 0)
      ELSE COALESCE(bmi.monthly_amount, 0)
    END AS monthly
  FROM customer_plots cp
  JOIN plots p ON p.id = cp.plot_id
  LEFT JOIN societies s ON s.id = p.society_id
  LEFT JOIN blocks b ON b.id = p.block_id
  LEFT JOIN plot_types pt ON pt.id = p.plot_type_id
  LEFT JOIN block_monthly_installments bmi ON bmi.block_id = p.block_id
  WHERE IFNULL(cp.status,'') = 'Active'
    AND (@sid = 0 OR p.society_id = @sid)
) t
GROUP BY society_name, block_name, monthly
ORDER BY society_name COLLATE NOCASE, block_name COLLATE NOCASE, monthly;", con))
            {
                cmd.Parameters.AddWithValue("@sid", societyId);
                using (var r = cmd.ExecuteReader())
                {
                    int sn = 0;
                    var n = CultureInfo.InvariantCulture;
                    while (r.Read())
                    {
                        double monthly = 0;
                        try { monthly = Convert.ToDouble(r["monthly"]); } catch { monthly = 0; }
                        int count = 0;
                        try { count = Convert.ToInt32(r["customers"]); } catch { count = 0; }
                        activeCount += count;

                        if (monthly > 0.009)
                        {
                            sn++;
                            setCount += count;
                            double total = monthly * count;
                            expectedTotal += total;
                            var nr = groups.NewRow();
                            nr["S.N"] = sn.ToString(n);
                            nr["Society"] = Convert.ToString(r["society_name"]) ?? "—";
                            nr["Block"] = Convert.ToString(r["block_name"]) ?? "—";
                            nr["Gaz"] = Convert.ToString(r["gaz"]) ?? "—";
                            nr["Customers"] = count.ToString(n);
                            nr["Monthly"] = monthly.ToString("N0", n);
                            nr["ExpectedTotal"] = total.ToString("N0", n);
                            nr["Summary"] = count.ToString(n) + " × " + monthly.ToString("N0", n)
                                + " = " + total.ToString("N0", n);
                            groups.Rows.Add(nr);
                        }
                        else
                        {
                            notSetCount += count;
                        }
                    }
                }
            }

            var nCult = CultureInfo.InvariantCulture;

            // One summary line for all Active allocations with resolved monthly = 0
            if (notSetCount > 0)
            {
                var notSet = groups.NewRow();
                notSet["S.N"] = "";
                notSet["Society"] = "Not set";
                notSet["Block"] = "";
                notSet["Gaz"] = "";
                notSet["Monthly"] = "Not set";
                notSet["Customers"] = notSetCount.ToString(nCult);
                notSet["ExpectedTotal"] = "—";
                notSet["Summary"] = notSetCount.ToString(nCult) + " customers · monthly not set";
                groups.Rows.Add(notSet);
            }

            // Gross footer: sum of set groups only; Not set count shown separately
            {
                var footer = groups.NewRow();
                footer["S.N"] = "";
                footer["Society"] = "GROSS TOTAL";
                footer["Block"] = "";
                footer["Gaz"] = "";
                footer["Monthly"] = "";
                footer["Customers"] = setCount.ToString(nCult);
                footer["ExpectedTotal"] = expectedTotal.ToString("N0", nCult);
                footer["Summary"] = setCount.ToString(nCult) + " set · Not set "
                    + notSetCount.ToString(nCult) + " · " + expectedTotal.ToString("N0", nCult);
                groups.Rows.Add(footer);
            }

            return groups;
        }

        private void LoadReport()
        {
            if (_binding) return;
            try
            {
                try { MainClass.EnsurePartnerMoneySchema(); } catch { }
                try { BlockMonthlyInstallmentStore.EnsureSchema(); } catch { }

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

                var today = DateTime.Today;
                var curStart = new DateTime(today.Year, today.Month, 1);
                var curEnd = today;
                var prevStart = curStart.AddMonths(-1);
                var prevEnd = curStart.AddDays(-1);
                string curFrom = curStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                string curTo = curEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                string prevFrom = prevStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                string prevTo = prevEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                string curLabel = curStart.ToString("MMM yyyy", CultureInfo.InvariantCulture);
                string prevLabel = prevStart.ToString("MMM yyyy", CultureInfo.InvariantCulture);

                var table = new DataTable();
                table.Columns.Add("S.N", typeof(int));
                table.Columns.Add("Date", typeof(string));
                table.Columns.Add("Customer", typeof(string));
                table.Columns.Add("Society", typeof(string));
                table.Columns.Add("Block", typeof(string));
                table.Columns.Add("Plot", typeof(string));
                table.Columns.Add("Amount", typeof(double));
                table.Columns.Add("CollectedBy", typeof(string));
                table.Columns.Add("ReceiptNo", typeof(string));
                table.Columns.Add("Mode", typeof(string));

                int sn = 0;
                double detailTotal = 0;
                DataTable groupsTable = null;
                int activeCount = 0, setCount = 0, notSetCount = 0;
                double expectedTotal = 0;

                using (var con = new SQLiteConnection(projectconnection.conReturn()))
                {
                    con.Open();
                    groupsTable = LoadInstallmentGroups(con, societyId,
                        out activeCount, out setCount, out notSetCount, out expectedTotal);

                    double curTotal = SumReceipts(con, societyId, curFrom, curTo);
                    double prevTotal = SumReceipts(con, societyId, prevFrom, prevTo);

                    using (var cmd = new SQLiteCommand(@"
SELECT r.id AS receipt_id, r.receipt_date, r.amount, r.mode,
  COALESCE(NULLIF(TRIM(r.ref_no),''), CAST(r.id AS TEXT)) AS receipt_no,
  c.name AS customer_name, p.plot_no, b.name AS block_name, s.name AS society_name,
  COALESCE(NULLIF(TRIM(per.name),''), '') AS collected_by
FROM receipts r
JOIN customer_plots cp ON cp.id = r.customer_plot_id
JOIN customers c ON c.id = cp.customer_id
JOIN plots p ON p.id = cp.plot_id
LEFT JOIN societies s ON s.id = p.society_id
LEFT JOIN blocks b ON b.id = p.block_id
LEFT JOIN partner_money pm ON pm.receipt_id = r.id
LEFT JOIN Person per ON per.person_id = pm.partner_id
WHERE r.receipt_date IS NOT NULL AND TRIM(r.receipt_date) <> ''
  AND substr(r.receipt_date,1,10) >= @from
  AND substr(r.receipt_date,1,10) <= @to
  AND (@sid = 0 OR p.society_id = @sid)
ORDER BY substr(r.receipt_date,1,10) DESC, r.id DESC;", con))
                    {
                        cmd.Parameters.AddWithValue("@from", fromYmd);
                        cmd.Parameters.AddWithValue("@to", toYmd);
                        cmd.Parameters.AddWithValue("@sid", societyId);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                double amt = 0;
                                try { amt = Convert.ToDouble(r["amount"]); } catch { amt = 0; }
                                detailTotal += amt;
                                sn++;
                                string dateRaw = Convert.ToString(r["receipt_date"]) ?? "";
                                string dateDisp = dateRaw.Length >= 10 ? dateRaw.Substring(0, 10) : dateRaw;
                                string collected = Convert.ToString(r["collected_by"]) ?? "";
                                if (string.IsNullOrWhiteSpace(collected)) collected = "—";

                                var nr = table.NewRow();
                                nr["S.N"] = sn;
                                nr["Date"] = dateDisp;
                                nr["Customer"] = Convert.ToString(r["customer_name"]);
                                nr["Society"] = Convert.ToString(r["society_name"]);
                                nr["Block"] = Convert.ToString(r["block_name"]);
                                nr["Plot"] = Convert.ToString(r["plot_no"]);
                                nr["Amount"] = amt;
                                nr["CollectedBy"] = collected;
                                nr["ReceiptNo"] = Convert.ToString(r["receipt_no"]);
                                nr["Mode"] = Convert.ToString(r["mode"]);
                                table.Rows.Add(nr);
                            }
                        }
                    }

                    var n = CultureInfo.InvariantCulture;
                    string detailLabel = from.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture)
                        + " → " + to.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture);
                    double baqi = expectedTotal - detailTotal;
                    if (baqi < 0) baqi = 0;

                    _lastGross = expectedTotal;
                    _lastCollection = detailTotal;
                    _lastBaqi = baqi;
                    _lastPeriodLabel = detailLabel;
                    _lastSocietyName = cmbSociety?.Text ?? "(All)";
                    _lastSummaryLine1 =
                        "Month installment TOTAL (Gross): " + expectedTotal.ToString("N0", n)
                        + "     |     Collection: " + detailTotal.ToString("N0", n)
                        + "     |     Still to collect (Baqi Lena): " + baqi.ToString("N0", n);
                    _lastSummaryLine2 =
                        "Active " + activeCount
                        + " · Monthly set " + setCount
                        + " · Not set " + notSetCount
                        + "   ·   Detail " + detailLabel + " · " + table.Rows.Count + " receipts"
                        + "   ·   " + curLabel + " collected: " + curTotal.ToString("N0", n)
                        + "   ·   " + prevLabel + " collected: " + prevTotal.ToString("N0", n);
                    lblSummary.Text = _lastSummaryLine1 + "\r\n" + _lastSummaryLine2;
                    if (lblGroupsTitle != null)
                    {
                        lblGroupsTitle.Text = "Installment Groups (Active) — "
                            + activeCount + " allocations · Month installment TOTAL (Gross) "
                            + expectedTotal.ToString("N0", n);
                    }
                }

                _lastGroups = groupsTable;
                _lastDetail = table;

                dgvGroups.DataSource = groupsTable;
                StyleGrid(dgvGroups);
                if (dgvGroups.Columns.Contains("S.N"))
                {
                    dgvGroups.Columns["S.N"].FillWeight = 28;
                    dgvGroups.Columns["S.N"].DefaultCellStyle.Alignment =
                        DataGridViewContentAlignment.MiddleCenter;
                }
                if (dgvGroups.Columns.Contains("Society"))
                    dgvGroups.Columns["Society"].FillWeight = 110;
                if (dgvGroups.Columns.Contains("Block"))
                    dgvGroups.Columns["Block"].FillWeight = 70;
                if (dgvGroups.Columns.Contains("Gaz"))
                {
                    dgvGroups.Columns["Gaz"].HeaderText = "Gaz (size)";
                    dgvGroups.Columns["Gaz"].FillWeight = 55;
                }
                if (dgvGroups.Columns.Contains("Monthly"))
                {
                    dgvGroups.Columns["Monthly"].HeaderText = "Monthly installment";
                    dgvGroups.Columns["Monthly"].FillWeight = 70;
                }
                if (dgvGroups.Columns.Contains("Customers"))
                    dgvGroups.Columns["Customers"].FillWeight = 55;
                if (dgvGroups.Columns.Contains("ExpectedTotal"))
                {
                    dgvGroups.Columns["ExpectedTotal"].HeaderText = "Group total";
                    dgvGroups.Columns["ExpectedTotal"].FillWeight = 70;
                }
                if (dgvGroups.Columns.Contains("Summary"))
                    dgvGroups.Columns["Summary"].HeaderText = "Group (count × amount)";

                // Highlight Gross Total footer row
                if (dgvGroups.Rows.Count > 0)
                {
                    var last = dgvGroups.Rows[dgvGroups.Rows.Count - 1];
                    var socVal = Convert.ToString(last.Cells["Society"].Value) ?? "";
                    if (socVal.StartsWith("GROSS", StringComparison.OrdinalIgnoreCase))
                    {
                        var bold = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
                        last.DefaultCellStyle.Font = bold;
                        last.DefaultCellStyle.ForeColor = Color.FromArgb(180, 230, 210);
                        last.DefaultCellStyle.BackColor = Color.FromArgb(20, 45, 95);
                    }
                }

                dgv.DataSource = table;
                StyleGrid(dgv);
                if (dgv.Columns.Contains("S.N"))
                {
                    dgv.Columns["S.N"].FillWeight = 40;
                    dgv.Columns["S.N"].DefaultCellStyle.Alignment =
                        DataGridViewContentAlignment.MiddleCenter;
                }
                if (dgv.Columns.Contains("Society"))
                    dgv.Columns["Society"].FillWeight = 100;
                if (dgv.Columns.Contains("Block"))
                    dgv.Columns["Block"].FillWeight = 70;
                if (dgv.Columns.Contains("Plot"))
                    dgv.Columns["Plot"].FillWeight = 55;
                if (dgv.Columns.Contains("CollectedBy"))
                    dgv.Columns["CollectedBy"].HeaderText = "Collected by";
                if (dgv.Columns.Contains("ReceiptNo"))
                    dgv.Columns["ReceiptNo"].HeaderText = "Receipt No";
                if (dgv.Columns.Contains("Amount"))
                    dgv.Columns["Amount"].DefaultCellStyle.Format = "N2";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Month Collection (VIP)", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SavePdf()
        {
            try
            {
                if (_lastGroups == null || _lastDetail == null)
                    LoadReport();
                if (_lastGroups == null || _lastDetail == null)
                {
                    MessageBox.Show("Pehle Load Report karein.", "Save PDF",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using (var dlg = new SaveFileDialog())
                {
                    dlg.Title = "Save PDF";
                    dlg.Filter = "PDF (*.pdf)|*.pdf";
                    dlg.FileName = "HubBuilders_MonthCollectionVIP_"
                        + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf";
                    string today = HubAutoBackup.TodayFolder();
                    if (!string.IsNullOrWhiteSpace(today))
                    {
                        try { Directory.CreateDirectory(today); dlg.InitialDirectory = today; } catch { }
                    }
                    if (dlg.ShowDialog() != DialogResult.OK) return;
                    File.WriteAllBytes(dlg.FileName, BuildPdfBytes());
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
            int rowH = 22;
            int top = 118;
            int bottom = h - 28;

            var groupRows = new List<string[]>();
            if (_lastGroups != null)
            {
                foreach (DataRow r in _lastGroups.Rows)
                {
                    groupRows.Add(new[]
                    {
                        Convert.ToString(r["S.N"]) ?? "",
                        Convert.ToString(r["Society"]) ?? "",
                        Convert.ToString(r["Block"]) ?? "",
                        Convert.ToString(r["Gaz"]) ?? "",
                        Convert.ToString(r["Monthly"]) ?? "",
                        Convert.ToString(r["Customers"]) ?? "",
                        Convert.ToString(r["ExpectedTotal"]) ?? "",
                        Convert.ToString(r["Summary"]) ?? ""
                    });
                }
            }

            var detailRows = new List<string[]>();
            if (_lastDetail != null)
            {
                foreach (DataRow r in _lastDetail.Rows)
                {
                    double amt = 0;
                    try { amt = Convert.ToDouble(r["Amount"]); } catch { amt = 0; }
                    detailRows.Add(new[]
                    {
                        Convert.ToString(r["S.N"]) ?? "",
                        Convert.ToString(r["Date"]) ?? "",
                        Convert.ToString(r["Customer"]) ?? "",
                        Convert.ToString(r["Society"]) ?? "",
                        Convert.ToString(r["Block"]) ?? "",
                        Convert.ToString(r["Plot"]) ?? "",
                        amt.ToString("N2", CultureInfo.InvariantCulture),
                        Convert.ToString(r["CollectedBy"]) ?? "",
                        Convert.ToString(r["ReceiptNo"]) ?? "",
                        Convert.ToString(r["Mode"]) ?? ""
                    });
                }
            }

            // Page 1+: installment groups, then receipts (section headers eat 1 row slot).
            var sections = new List<PdfSection>();
            sections.Add(new PdfSection
            {
                Title = "Installment Groups (Active)",
                Heads = new[] { "#", "Society", "Block", "Gaz", "Monthly", "Cust", "Group total", "Summary" },
                Cols = new[] { 18, 56, 280, 420, 560, 700, 800, 960 },
                Rows = groupRows
            });
            sections.Add(new PdfSection
            {
                Title = "Collection receipts — " + _lastPeriodLabel,
                Heads = new[] { "#", "Date", "Customer", "Society", "Block", "Plot", "Amount", "Collected by", "Receipt", "Mode" },
                Cols = new[] { 18, 56, 160, 420, 560, 680, 780, 920, 1180, 1380 },
                Rows = detailRows
            });

            // Flatten into drawable lines with section breaks.
            var lines = new List<PdfLine>();
            foreach (var sec in sections)
            {
                lines.Add(new PdfLine { Kind = PdfLineKind.Section, Text = sec.Title, Section = sec });
                lines.Add(new PdfLine { Kind = PdfLineKind.Header, Section = sec });
                foreach (var row in sec.Rows)
                    lines.Add(new PdfLine { Kind = PdfLineKind.Row, Cells = row, Section = sec });
                if (sec.Rows.Count == 0)
                    lines.Add(new PdfLine { Kind = PdfLineKind.Empty, Text = "(no rows)", Section = sec });
            }

            int rowsPerPage = Math.Max(1, (bottom - top) / rowH);
            int pageCount = Math.Max(1, (int)Math.Ceiling(lines.Count / (double)rowsPerPage));

            var n = CultureInfo.InvariantCulture;
            using (var titleFont = new Font("Segoe UI", 18F, FontStyle.Bold))
            using (var subFont = new Font("Segoe UI", 10F, FontStyle.Bold))
            using (var cellFont = new Font("Segoe UI", 9F, FontStyle.Regular))
            using (var headFont = new Font("Segoe UI", 9F, FontStyle.Bold))
            {
                for (int p = 0; p < pageCount; p++)
                {
                    var bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.White);
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                        g.DrawString("HUB BUILDERS", titleFont, Brushes.Black, 18, 10);
                        g.DrawString("Month Collection (VIP)  —  " + _lastSocietyName, subFont, Brushes.Black, 18, 42);
                        g.DrawString(DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt") +
                            "    Page " + (p + 1) + " / " + pageCount, subFont, Brushes.Black, 1180, 16);
                        g.DrawString(
                            "Month installment TOTAL (Gross): " + _lastGross.ToString("N0", n)
                            + "     Collection: " + _lastCollection.ToString("N0", n)
                            + "     Still to collect (Baqi Lena): " + _lastBaqi.ToString("N0", n),
                            subFont, Brushes.Black, 18, 66);
                        g.DrawString(_lastSummaryLine2 ?? "", cellFont, Brushes.Black, 18, 88);

                        int start = p * rowsPerPage;
                        int end = Math.Min(lines.Count, start + rowsPerPage);
                        int y = top;
                        for (int i = start; i < end; i++)
                        {
                            var line = lines[i];
                            if (line.Kind == PdfLineKind.Section)
                            {
                                using (var brush = new SolidBrush(Color.FromArgb(230, 240, 248)))
                                    g.FillRectangle(brush, 18, y, w - 36, rowH);
                                g.DrawString(line.Text ?? "", headFont, Brushes.Black, 22, y + 3);
                            }
                            else if (line.Kind == PdfLineKind.Header)
                            {
                                using (var headBrush = new SolidBrush(Color.FromArgb(32, 36, 61)))
                                    g.FillRectangle(headBrush, 18, y, w - 36, rowH);
                                for (int c = 0; c < line.Section.Heads.Length; c++)
                                    g.DrawString(line.Section.Heads[c], headFont, Brushes.White,
                                        line.Section.Cols[c], y + 3);
                            }
                            else if (line.Kind == PdfLineKind.Empty)
                            {
                                g.DrawString(line.Text ?? "", cellFont, Brushes.Gray, 22, y + 3);
                            }
                            else
                            {
                                if ((i - start) % 2 == 1)
                                {
                                    using (var alt = new SolidBrush(Color.FromArgb(245, 247, 250)))
                                        g.FillRectangle(alt, 18, y, w - 36, rowH);
                                }
                                for (int c = 0; c < line.Cells.Length && c < line.Section.Cols.Length; c++)
                                    g.DrawString(line.Cells[c] ?? "", cellFont, Brushes.Black,
                                        line.Section.Cols[c], y + 3);
                            }
                            y += rowH;
                        }
                        g.DrawRectangle(Pens.Gray, 18, top - 2, w - 36, y - (top - 2));
                    }
                    pages.Add(bmp);
                }
            }
            return pages;
        }

        private enum PdfLineKind { Section, Header, Row, Empty }

        private sealed class PdfSection
        {
            public string Title;
            public string[] Heads;
            public int[] Cols;
            public List<string[]> Rows;
        }

        private sealed class PdfLine
        {
            public PdfLineKind Kind;
            public string Text;
            public string[] Cells;
            public PdfSection Section;
        }

        /// <summary>Same JPEG-in-PDF approach as Plot Status Owner report.</summary>
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
