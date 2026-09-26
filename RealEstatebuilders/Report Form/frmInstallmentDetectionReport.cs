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
    /// VIP: date-range installment detection — expected vs collected vs remaining.
    /// Calendar-month dues (month after booking), same idea as Dues WhatsApp.
    /// Additive DataGrid report; does not change sync.
    /// </summary>
    public class frmInstallmentDetectionReport : Sample
    {
        private ComboBox cmbSociety;
        private DateTimePicker dtpFrom;
        private DateTimePicker dtpTo;
        private DataGridView dgv;
        private Label lblSummary;
        private Label lblPartialLegend;
        private Guna.UI2.WinForms.Guna2Button btnLoad;
        private bool _binding;

        public frmInstallmentDetectionReport()
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
            Text = "Installment Detection (VIP)";
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
                Text = "Installment Detection (VIP)"
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
                Height = 128,
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
                Size = new Size(160, 36)
            };
            btnLoad.Click += (s, e) => LoadReport();

            lblSummary = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(180, 230, 210),
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Location = new Point(12, 68),
                Text = ""
            };
            lblPartialLegend = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(200, 210, 230),
                Font = new Font("Segoe UI", 9F),
                Location = new Point(12, 92),
                Text = "Partial = some payment, but less than installment due  ·  Partial = thori payment mili, poori installment nahi"
            };

            panelFilters.Controls.Add(lblSoc);
            panelFilters.Controls.Add(cmbSociety);
            panelFilters.Controls.Add(lblFrom);
            panelFilters.Controls.Add(dtpFrom);
            panelFilters.Controls.Add(lblTo);
            panelFilters.Controls.Add(dtpTo);
            panelFilters.Controls.Add(btnLoad);
            panelFilters.Controls.Add(lblSummary);
            panelFilters.Controls.Add(lblPartialLegend);

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

        /// <summary>
        /// Due months overlapping [from,to]: calendar months starting month after booking
        /// through To month, intersected with From–To months. Requires booking+30d &lt;= To.
        /// </summary>
        internal static int CountDueMonthsInRange(DateTime booking, DateTime from, DateTime to)
        {
            booking = booking.Date;
            from = from.Date;
            to = to.Date;
            if (to < from) return 0;
            if ((to - booking).TotalDays < 30) return 0;

            var firstDue = new DateTime(booking.Year, booking.Month, 1).AddMonths(1);
            var fromMonth = new DateTime(from.Year, from.Month, 1);
            var toMonth = new DateTime(to.Year, to.Month, 1);
            var start = firstDue > fromMonth ? firstDue : fromMonth;
            if (start > toMonth) return 0;

            int n = 0;
            for (var m = start; m <= toMonth; m = m.AddMonths(1))
                n++;
            return n;
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

                var table = new DataTable();
                table.Columns.Add("S.N", typeof(int));
                table.Columns.Add("Customer", typeof(string));
                table.Columns.Add("Plot", typeof(string));
                table.Columns.Add("Block", typeof(string));
                table.Columns.Add("Society", typeof(string));
                table.Columns.Add("Monthly", typeof(double));
                table.Columns.Add("DueMonths", typeof(int));
                table.Columns.Add("Expected", typeof(double));
                table.Columns.Add("PaidInPeriod", typeof(double));
                table.Columns.Add("Remaining", typeof(double));
                table.Columns.Add("Status", typeof(string));

                int sn = 0;
                int customerCount = 0;
                int totalActive = 0;
                var seenCust = new System.Collections.Generic.HashSet<int>();
                double sumExpected = 0, sumPaid = 0;
                int unpaid = 0, partial = 0, paidOk = 0, notSet = 0;

                using (var con = new SQLiteConnection(projectconnection.conReturn()))
                {
                    con.Open();
                    using (var cmd = new SQLiteCommand(@"
SELECT cp.id AS customer_plot_id, cp.customer_id, c.name AS customer_name,
  s.name AS society_name, b.name AS block_name, p.plot_no, cp.booking_date,
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
JOIN customers c ON c.id = cp.customer_id
JOIN plots p ON p.id = cp.plot_id
LEFT JOIN societies s ON s.id = p.society_id
LEFT JOIN blocks b ON b.id = p.block_id
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
                                totalActive++;

                                double monthly = 0;
                                try { monthly = Convert.ToDouble(r["monthly_installment"]); } catch { monthly = 0; }
                                if (monthly <= 0)
                                {
                                    try { monthly = Convert.ToDouble(r["block_monthly"]); } catch { monthly = 0; }
                                }

                                double paid = 0;
                                try { paid = Convert.ToDouble(r["paid_in_period"]); } catch { paid = 0; }
                                if (paid < 0) paid = 0;

                                int dueMonths = 0;
                                double expected = 0;
                                string status;

                                if (monthly <= 0)
                                {
                                    // Still list Active — monthly not set on plot or block default.
                                    status = "Not set";
                                    notSet++;
                                }
                                else
                                {
                                    DateTime? booking = ParseDate(r["booking_date"]);
                                    if (booking != null)
                                        dueMonths = CountDueMonthsInRange(booking.Value, from, to);
                                    expected = monthly * dueMonths;

                                    if (expected <= 0.009)
                                    {
                                        // Active with monthly, but no due month in From–To (or no booking).
                                        status = "No due in range";
                                        notSet++;
                                        expected = 0;
                                    }
                                    else if (paid >= expected - 0.009) { status = "Paid"; paidOk++; }
                                    else if (paid > 0.009) { status = "Partial"; partial++; }
                                    else { status = "Unpaid"; unpaid++; }
                                }

                                double rem = expected - paid;

                                int custId = 0;
                                try { custId = Convert.ToInt32(r["customer_id"]); } catch { }
                                if (custId > 0 && seenCust.Add(custId)) customerCount++;

                                sumExpected += expected;
                                sumPaid += paid;

                                sn++;
                                var nr = table.NewRow();
                                nr["S.N"] = sn;
                                nr["Customer"] = Convert.ToString(r["customer_name"]);
                                nr["Plot"] = Convert.ToString(r["plot_no"]);
                                nr["Block"] = Convert.ToString(r["block_name"]);
                                nr["Society"] = Convert.ToString(r["society_name"]);
                                nr["Monthly"] = monthly;
                                nr["DueMonths"] = dueMonths;
                                nr["Expected"] = expected;
                                nr["PaidInPeriod"] = paid;
                                nr["Remaining"] = rem;
                                nr["Status"] = status;
                                table.Rows.Add(nr);
                            }
                        }
                    }
                }

                double sumRem = sumExpected - sumPaid;
                if (sumRem < 0) sumRem = 0;
                dgv.DataSource = table;
                StyleGrid(dgv);
                if (dgv.Columns.Contains("S.N"))
                {
                    dgv.Columns["S.N"].FillWeight = 40;
                    dgv.Columns["S.N"].DefaultCellStyle.Alignment =
                        DataGridViewContentAlignment.MiddleCenter;
                }
                foreach (var col in new[] { "Monthly", "Expected", "PaidInPeriod", "Remaining" })
                {
                    if (dgv.Columns.Contains(col))
                        dgv.Columns[col].DefaultCellStyle.Format = "N2";
                }

                var n = CultureInfo.InvariantCulture;
                lblSummary.Text =
                    "Total Active: " + totalActive
                    + "   |   Shown: " + table.Rows.Count
                    + "   |   Customers: " + customerCount
                    + "   |   Total installment: " + sumExpected.ToString("N2", n)
                    + "   |   Already collected (Le chuke): " + sumPaid.ToString("N2", n)
                    + "   |   Remaining (Baqi): " + sumRem.ToString("N2", n)
                    + "   |   Paid/Partial/Unpaid/Not set: "
                    + paidOk + "/" + partial + "/" + unpaid + "/" + notSet;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Installment Detection (VIP)", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
