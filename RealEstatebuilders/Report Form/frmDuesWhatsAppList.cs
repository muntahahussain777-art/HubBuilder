using HubBuilders.ProjectConnection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace HubBuilders.Report_Form
{
    /// <summary>
    /// Additive: list of Active plots with remaining dues + missed months,
    /// WhatsApp button per row. Does not change existing forms/DB.
    /// </summary>
    public class frmDuesWhatsAppList : Sample
    {
        private ComboBox cmbSociety;
        private ComboBox cmbMinMissed;
        private DateTimePicker dtpAsOf;
        private TextBox txtSearch;
        private Guna.UI2.WinForms.Guna2Button btnLoad;
        private DataGridView dgv;
        private DataTable _dt;
        private bool _busy;

        public frmDuesWhatsAppList()
        {
            InitializeUi();
            LoadSocieties();
            dtpAsOf.Value = DateTime.Today;
            cmbMinMissed.SelectedIndex = 0; // 1 month+
        }

        private void InitializeUi()
        {
            Text = "Dues WhatsApp Reminder";
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
                Text = "Dues WhatsApp Reminder"
            };
            var close = new Guna.UI2.WinForms.Guna2ControlBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FillColor = Color.FromArgb(139, 152, 166),
                IconColor = Color.White,
                Size = new Size(56, 36),
                Location = new Point(ClientSize.Width - 70, 12)
            };
            panelTop.Controls.Add(title);
            panelTop.Controls.Add(close);
            panelTop.Resize += (s, e) =>
            {
                close.Location = new Point(panelTop.Width - 70, 12);
            };

            var panelFilters = new Panel
            {
                Dock = DockStyle.Top,
                Height = 110,
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

            var lblMiss = Lbl("Min missed months", 250, 8);
            cmbMinMissed = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F),
                Location = new Point(250, 30),
                Size = new Size(160, 28)
            };
            cmbMinMissed.Items.Add("1 month+");
            cmbMinMissed.Items.Add("2 months+");
            cmbMinMissed.Items.Add("3 months+");
            cmbMinMissed.Items.Add("4 months+");
            cmbMinMissed.Items.Add("5 months+");

            var lblAsOf = Lbl("As Of", 430, 8);
            dtpAsOf = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd-MMM-yyyy",
                Font = new Font("Segoe UI", 10F),
                Location = new Point(430, 30),
                Size = new Size(160, 28)
            };

            var lblSearch = Lbl("Search", 610, 8);
            txtSearch = new TextBox
            {
                Font = new Font("Segoe UI", 10F),
                Location = new Point(610, 30),
                Size = new Size(220, 28)
            };

            btnLoad = new Guna.UI2.WinForms.Guna2Button
            {
                Text = "Load Dues List",
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                FillColor = Color.FromArgb(18, 140, 126),
                ForeColor = Color.White,
                BorderRadius = 8,
                Location = new Point(12, 68),
                Size = new Size(200, 36)
            };
            btnLoad.Click += (s, e) => LoadList();

            var hint = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(200, 210, 230),
                Location = new Point(230, 76),
                Text = "Sirf remaining dues wale plots · row pe WhatsApp → details message"
            };

            panelFilters.Controls.Add(lblSoc);
            panelFilters.Controls.Add(cmbSociety);
            panelFilters.Controls.Add(lblMiss);
            panelFilters.Controls.Add(cmbMinMissed);
            panelFilters.Controls.Add(lblAsOf);
            panelFilters.Controls.Add(dtpAsOf);
            panelFilters.Controls.Add(lblSearch);
            panelFilters.Controls.Add(txtSearch);
            panelFilters.Controls.Add(btnLoad);
            panelFilters.Controls.Add(hint);

            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.FromArgb(32, 36, 66),
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false
            };
            dgv.CellContentClick += Dgv_CellContentClick;
            StyleGrid(dgv);

            Controls.Add(dgv);
            Controls.Add(panelFilters);
            Controls.Add(panelTop);
        }

        private static Label Lbl(string text, int x, int y)
        {
            return new Label
            {
                AutoSize = true,
                Text = text,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Location = new Point(x, y)
            };
        }

        private static void StyleGrid(DataGridView gv)
        {
            Color dark = Color.FromArgb(32, 36, 66);
            Color header = Color.FromArgb(20, 45, 95);
            Color sel = Color.FromArgb(45, 52, 90);
            gv.EnableHeadersVisualStyles = false;
            gv.ColumnHeadersDefaultCellStyle.BackColor = header;
            gv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            gv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
            gv.DefaultCellStyle.BackColor = dark;
            gv.DefaultCellStyle.ForeColor = Color.White;
            gv.DefaultCellStyle.SelectionBackColor = sel;
            gv.DefaultCellStyle.SelectionForeColor = Color.White;
            gv.DefaultCellStyle.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
            gv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(37, 41, 74);
            gv.AlternatingRowsDefaultCellStyle.ForeColor = Color.White;
            gv.GridColor = Color.FromArgb(50, 56, 76);
        }

        private void LoadSocieties()
        {
            try
            {
                cmbSociety.Items.Clear();
                cmbSociety.Items.Add(new IdName(0, "(All Societies)"));
                using (var con = new SQLiteConnection(projectconnection.conReturn()))
                {
                    con.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT id, name FROM societies ORDER BY name COLLATE NOCASE", con))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                            cmbSociety.Items.Add(new IdName(Convert.ToInt32(r["id"]), Convert.ToString(r["name"])));
                    }
                }
                cmbSociety.DisplayMember = "Name";
                cmbSociety.ValueMember = "Id";
                cmbSociety.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Societies", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private int GetSocietyId()
        {
            var item = cmbSociety.SelectedItem as IdName;
            return item == null ? 0 : item.Id;
        }

        private int GetMinMissed()
        {
            // 0→1, 1→2, 2→3, 3→4, 4→5
            int idx = cmbMinMissed.SelectedIndex;
            if (idx < 0) return 1;
            return idx + 1;
        }

        private void LoadList()
        {
            if (_busy) return;
            _busy = true;
            Cursor = Cursors.WaitCursor;
            try
            {
                _dt = BuildDuesTable();
                dgv.Columns.Clear();
                dgv.DataSource = _dt;
                StyleGrid(dgv);
                HideCol("customer_plot_id");
                HideCol("mobile_raw");
                HideCol("Monthly");
                if (dgv.Columns.Contains("S.N"))
                {
                    dgv.Columns["S.N"].FillWeight = 40;
                    dgv.Columns["S.N"].DefaultCellStyle.Alignment =
                        DataGridViewContentAlignment.MiddleCenter;
                }
                if (dgv.Columns.Contains("Missed"))
                    dgv.Columns["Missed"].HeaderText = "Missed Months";
                if (dgv.Columns.Contains("Cycles"))
                    dgv.Columns["Cycles"].HeaderText = "30-Day Cycles";
                if (!dgv.Columns.Contains("dgvwa"))
                {
                    var btn = new DataGridViewButtonColumn
                    {
                        Name = "dgvwa",
                        HeaderText = "WhatsApp",
                        Text = "WhatsApp",
                        UseColumnTextForButtonValue = true,
                        Width = 110,
                        FlatStyle = FlatStyle.Flat
                    };
                    dgv.Columns.Add(btn);
                    dgv.Columns["dgvwa"].DisplayIndex = dgv.Columns.Count - 1;
                }
                if (_dt.Rows.Count == 0)
                {
                    MessageBox.Show(
                        "Koi dues / missed-month plot nahi mila.",
                        "Dues WhatsApp",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Load failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
                _busy = false;
            }
        }

        private void HideCol(string name)
        {
            if (dgv.Columns.Contains(name))
                dgv.Columns[name].Visible = false;
        }

        private DataTable BuildDuesTable()
        {
            try { MainClass.EnsureAllocationSchema(); } catch { }
            var result = new DataTable();
            result.Columns.Add("S.N", typeof(int));
            result.Columns.Add("customer_plot_id", typeof(int));
            result.Columns.Add("Customer", typeof(string));
            result.Columns.Add("Mobile", typeof(string));
            result.Columns.Add("mobile_raw", typeof(string));
            result.Columns.Add("Society", typeof(string));
            result.Columns.Add("Block", typeof(string));
            result.Columns.Add("Plot", typeof(string));
            result.Columns.Add("Booking", typeof(string));
            result.Columns.Add("Price", typeof(double));
            result.Columns.Add("Paid", typeof(double));
            result.Columns.Add("Due", typeof(double));
            result.Columns.Add("Missed", typeof(string));
            result.Columns.Add("MissedCount", typeof(int));
            result.Columns.Add("Cycles", typeof(string));
            result.Columns.Add("CycleCount", typeof(int));
            result.Columns.Add("LastPay", typeof(string));
            result.Columns.Add("Monthly", typeof(double));

            int societyId = GetSocietyId();
            int minMissed = GetMinMissed();
            string kw = (txtSearch.Text ?? "").Trim();
            DateTime asOf = dtpAsOf.Value.Date;
            int sn = 0;

            const string sql = @"
SELECT
  cp.id AS customer_plot_id,
  COALESCE(c.name,'') AS customer_name,
  COALESCE(c.mobile,'') AS mobile,
  COALESCE(s.name,'') AS society_name,
  COALESCE(b.name,'') AS block_name,
  COALESCE(p.plot_no,'') AS plot_no,
  cp.booking_date AS booking_date,
  COALESCE(cp.price, 0) AS plot_price,
  COALESCE(cp.monthly_installment, 0) AS monthly_installment,
  COALESCE(bmi.monthly_amount, 0) AS block_monthly,
  COALESCE((SELECT SUM(amount) FROM receipts r WHERE r.customer_plot_id = cp.id), 0) AS total_paid,
  COALESCE((SELECT MAX(receipt_date) FROM receipts r2 WHERE r2.customer_plot_id = cp.id), '') AS last_payment_date
FROM customer_plots cp
JOIN customers c ON c.id = cp.customer_id
JOIN plots p ON p.id = cp.plot_id
LEFT JOIN societies s ON s.id = p.society_id
LEFT JOIN blocks b ON b.id = p.block_id
LEFT JOIN block_monthly_installments bmi ON bmi.block_id = p.block_id
WHERE cp.status = 'Active'
  AND (@sid = 0 OR p.society_id = @sid)
  AND (@kw = '' OR c.name LIKE '%' || @kw || '%' COLLATE NOCASE
                 OR c.mobile LIKE '%' || @kw || '%' COLLATE NOCASE
                 OR CAST(p.plot_no AS TEXT) LIKE '%' || @kw || '%' COLLATE NOCASE
                 OR s.name LIKE '%' || @kw || '%' COLLATE NOCASE)
ORDER BY s.name, c.name, p.plot_no;";

            using (var con = new SQLiteConnection(projectconnection.conReturn()))
            {
                con.Open();
                try { BlockMonthlyInstallmentStore.EnsureSchema(); } catch { }
                using (var cmd = new SQLiteCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@sid", societyId);
                    cmd.Parameters.AddWithValue("@kw", kw);
                    using (var da = new SQLiteDataAdapter(cmd))
                    {
                        var raw = new DataTable();
                        da.Fill(raw);
                        foreach (DataRow r in raw.Rows)
                        {
                            int cpId = Convert.ToInt32(r["customer_plot_id"]);
                            double price = SafeDouble(r["plot_price"]);
                            double paid = SafeDouble(r["total_paid"]);
                            double due = Math.Round(price - paid, 2);
                            // Plot rate complete (receipt se) → reminder mein bilkul nahi
                            if (due <= 0.009) continue;

                            DateTime booking = ParseDateSafe(r["booking_date"], asOf);
                            // Pehle poora 30 din cycle complete hona chahiye
                            int days = (asOf.Date - booking.Date).Days;
                            if (days < 30) continue;

                            // ADVANCE = receipt_date < booking+7 → ignore for installment clear only
                            HashSet<string> paidMonths;
                            List<DateTime> paidDates;
                            double installmentPaid;
                            LoadInstallmentReceiptData(con, cpId, booking,
                                out paidMonths, out paidDates, out installmentPaid);
                            double monthly = r.Table.Columns.Contains("monthly_installment")
                                ? SafeDouble(r["monthly_installment"]) : 0d;
                            if (monthly <= 0 && r.Table.Columns.Contains("block_monthly"))
                                monthly = SafeDouble(r["block_monthly"]);
                            int receiptCount = paidDates != null ? paidDates.Count : 0;
                            string missed;
                            int missedCount;
                            HashSet<string> clearedMonthKeys;
                            // FIFO/cover uses post-advance paid only; Due display still uses all receipts
                            ComputeMissedMonths(booking, asOf, paidMonths, monthly, installmentPaid, receiptCount,
                                out missed, out missedCount, out clearedMonthKeys);

                            string cycles;
                            int cycleCount;
                            ComputeMissed30DayCycles(booking, asOf, paidDates, clearedMonthKeys,
                                out cycles, out cycleCount);

                            // Sab months+cycles paid → hide; else min filter on either
                            if (missedCount == 0 && cycleCount == 0) continue;
                            if (missedCount < minMissed && cycleCount < minMissed) continue;

                            sn++;
                            var nr = result.NewRow();
                            nr["S.N"] = sn;
                            nr["customer_plot_id"] = cpId;
                            nr["Customer"] = Convert.ToString(r["customer_name"]);
                            nr["Mobile"] = Convert.ToString(r["mobile"]);
                            nr["mobile_raw"] = Convert.ToString(r["mobile"]);
                            nr["Society"] = Convert.ToString(r["society_name"]);
                            nr["Block"] = Convert.ToString(r["block_name"]);
                            nr["Plot"] = Convert.ToString(r["plot_no"]);
                            nr["Booking"] = booking.ToString("yyyy-MM-dd");
                            nr["Price"] = price;
                            nr["Paid"] = paid;
                            nr["Due"] = due;
                            nr["Missed"] = missed;
                            nr["MissedCount"] = missedCount;
                            nr["Cycles"] = cycles;
                            nr["CycleCount"] = cycleCount;
                            nr["LastPay"] = Convert.ToString(r["last_payment_date"]);
                            nr["Monthly"] = monthly;
                            result.Rows.Add(nr);
                        }
                    }
                }
            }
            return result;
        }

        private void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                if (dgv.Columns[e.ColumnIndex].Name != "dgvwa") return;
                var drv = dgv.Rows[e.RowIndex].DataBoundItem as DataRowView;
                if (drv == null) return;
                SendWhatsApp(drv.Row);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "WhatsApp", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SendWhatsApp(DataRow row)
        {
            string mobile = Convert.ToString(row["mobile_raw"]);
            string digits = NormalizePhoneToDigits(mobile);
            if (string.IsNullOrWhiteSpace(digits))
            {
                MessageBox.Show("Customer ka mobile number nahi mila.", "WhatsApp",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string msg = BuildWhatsAppMessage(row);
            OpenWhatsAppWithPrefill(digits, msg);
        }

        private static string BuildWhatsAppMessage(DataRow row)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Assalam o Alaikum,");
            sb.AppendLine();
            sb.AppendLine("*" + Convert.ToString(row["Customer"]) + "* — Dues Reminder");
            sb.AppendLine("Society: " + Convert.ToString(row["Society"]));
            sb.AppendLine("Block/Plot: " + Convert.ToString(row["Block"]) + " / " + Convert.ToString(row["Plot"]));
            sb.AppendLine("Booking: " + Convert.ToString(row["Booking"]));
            sb.AppendLine("Plot Price: " + FormatMoney(row["Price"]));
            sb.AppendLine("Total Paid: " + FormatMoney(row["Paid"]));
            sb.AppendLine("*Remaining Due: " + FormatMoney(row["Due"]) + "*");
            double monthly = 0;
            try { monthly = Convert.ToDouble(row["Monthly"]); } catch { }
            if (monthly > 0)
            {
                sb.AppendLine("Is month ki installment: " + FormatMoney(monthly));
                sb.AppendLine("Is month apko " + FormatMoney(monthly) + " installment deni hai.");
            }
            sb.AppendLine("Missed Months (" + Convert.ToString(row["MissedCount"]) + "): " + Convert.ToString(row["Missed"]));
            sb.AppendLine("30-Day Cycles Missed (" + Convert.ToString(row["CycleCount"]) + "): " + Convert.ToString(row["Cycles"]));
            string last = Convert.ToString(row["LastPay"]);
            if (!string.IsNullOrWhiteSpace(last))
                sb.AppendLine("Last Payment: " + last);
            sb.AppendLine();
            sb.AppendLine("Barah-e-karam apni dues clear kar dein. Shukriya.");
            sb.AppendLine("Hub Builders");
            return sb.ToString();
        }

        /// <summary>
        /// Installment reminder only: skip ADVANCE receipts (date &lt; booking+7 days).
        /// Due amount (price−all paid) stays outside this helper.
        /// 30-day cycle count still starts on booking; first list appear after day 30.
        /// </summary>
        private static void LoadInstallmentReceiptData(
            SQLiteConnection con, int customerPlotId, DateTime booking,
            out HashSet<string> paidMonths, out List<DateTime> paidDates, out double installmentPaid)
        {
            paidMonths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            paidDates = new List<DateTime>();
            installmentPaid = 0d;
            DateTime advanceCutoff = booking.Date.AddDays(7);
            try
            {
                using (var cmd = new SQLiteCommand(@"
SELECT receipt_date, amount FROM receipts
WHERE customer_plot_id = @id
  AND amount > 0
  AND receipt_date IS NOT NULL
  AND TRIM(receipt_date) <> '';", con))
                {
                    cmd.Parameters.AddWithValue("@id", customerPlotId);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            string raw = Convert.ToString(r["receipt_date"] ?? "");
                            DateTime d;
                            if (!DateTime.TryParse(raw, out d))
                            {
                                if (raw.Length >= 7)
                                {
                                    // No parseable day → cannot prove post-advance; skip for clear logic
                                }
                                continue;
                            }
                            // ADVANCE window: receipt_date < booking+7 (date-only)
                            if (d.Date < advanceCutoff)
                                continue;

                            paidDates.Add(d.Date);
                            paidMonths.Add(d.ToString("yyyy-MM"));
                            installmentPaid += SafeDouble(r["amount"]);
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// 30-day cycles. Paid if receipt date in cycle range OR overlaps a cleared calendar month.
        /// No independent FIFO on cycles (avoids double-counting totalPaid).
        /// </summary>
        private static void ComputeMissed30DayCycles(
            DateTime booking, DateTime asOf, List<DateTime> paidDates,
            HashSet<string> clearedMonthKeys,
            out string cyclesCsv, out int cycleCount)
        {
            var missed = new List<string>();
            try
            {
                booking = booking.Date;
                asOf = asOf.Date;
                int completed = (asOf - booking).Days / 30;
                if (completed < 1)
                {
                    cyclesCsv = "—";
                    cycleCount = 0;
                    return;
                }

                for (int i = 0; i < completed; i++)
                {
                    DateTime start = booking.AddDays(i * 30);
                    DateTime endExclusive = booking.AddDays((i + 1) * 30);
                    DateTime endInclusive = endExclusive.AddDays(-1);
                    bool paidByDate = false;
                    if (paidDates != null)
                    {
                        foreach (var pd in paidDates)
                        {
                            if (pd >= start && pd < endExclusive)
                            {
                                paidByDate = true;
                                break;
                            }
                        }
                    }
                    if (paidByDate)
                        continue;
                    if (CycleOverlapsClearedMonth(start, endInclusive, clearedMonthKeys))
                        continue;
                    missed.Add(
                        "C" + (i + 1) + " (" +
                        start.ToString("dd-MMM", CultureInfo.InvariantCulture) +
                        " → " +
                        endInclusive.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture) +
                        ")");
                }
            }
            catch { }

            cycleCount = missed.Count;
            cyclesCsv = cycleCount == 0 ? "None" : string.Join(", ", missed);
        }

        private static bool CycleOverlapsClearedMonth(
            DateTime start, DateTime endInclusive, HashSet<string> clearedMonthKeys)
        {
            if (clearedMonthKeys == null || clearedMonthKeys.Count == 0) return false;
            DateTime cur = new DateTime(start.Year, start.Month, 1);
            DateTime end = new DateTime(endInclusive.Year, endInclusive.Month, 1);
            while (cur <= end)
            {
                if (clearedMonthKeys.Contains(cur.ToString("yyyy-MM")))
                    return true;
                cur = cur.AddMonths(1);
            }
            return false;
        }

        /// <summary>
        /// Calendar months after booking. Receipt date in month → paid.
        /// Cover slots: monthly&gt;0 → floor(totalPaid/monthly); else each receipt covers 1 period.
        /// Returns clearedKeys for cycle sync.
        /// </summary>
        private static void ComputeMissedMonths(
            DateTime booking, DateTime asOf, HashSet<string> paidMonths,
            double monthlyInstallment, double totalPaid, int receiptCount,
            out string missedCsv, out int missedCount, out HashSet<string> clearedKeys)
        {
            var missed = new List<string>();
            clearedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                DateTime cursor = new DateTime(booking.Year, booking.Month, 1).AddMonths(1);
                DateTime end = new DateTime(asOf.Year, asOf.Month, 1);
                if (cursor > end)
                {
                    missedCsv = "—";
                    missedCount = 0;
                    return;
                }

                var dueKeys = new List<DateTime>();
                while (cursor <= end)
                {
                    dueKeys.Add(cursor);
                    cursor = cursor.AddMonths(1);
                }

                int coverLeft = CoverSlots(monthlyInstallment, totalPaid, receiptCount);

                foreach (var m in dueKeys)
                {
                    string key = m.ToString("yyyy-MM");
                    bool paidByDate = paidMonths != null && paidMonths.Contains(key);
                    if (paidByDate)
                    {
                        clearedKeys.Add(key);
                        continue;
                    }
                    if (coverLeft > 0)
                    {
                        coverLeft--;
                        clearedKeys.Add(key);
                        continue;
                    }
                    missed.Add(m.ToString("MMM-yyyy", CultureInfo.InvariantCulture));
                }
            }
            catch { }

            missedCount = missed.Count;
            missedCsv = missedCount == 0 ? "None" : string.Join(", ", missed);
        }

        private static int CoverSlots(double monthlyInstallment, double totalPaid, int receiptCount)
        {
            if (monthlyInstallment > 0.009)
                return (int)Math.Floor((totalPaid / monthlyInstallment) + 1e-9);
            return receiptCount < 0 ? 0 : receiptCount;
        }

        private static string FormatMoney(object o)
        {
            return SafeDouble(o).ToString("N0", CultureInfo.InvariantCulture);
        }

        private static string NormalizePhoneToDigits(string input)
        {
            try
            {
                var digits = new string((input ?? "").Where(char.IsDigit).ToArray());
                if (digits.StartsWith("03") && digits.Length == 11)
                    digits = "92" + digits.Substring(1);
                return digits;
            }
            catch { return ""; }
        }

        private static void OpenWhatsAppWithPrefill(string digitsE164, string message)
        {
            try
            {
                string encoded = Uri.EscapeDataString(message ?? "");
                if (!TryOpenUrl("whatsapp://send?phone=" + digitsE164 + "&text=" + encoded))
                    TryOpenUrl("https://wa.me/" + digitsE164 + "?text=" + encoded);
            }
            catch { }
        }

        private static bool TryOpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                return true;
            }
            catch { return false; }
        }

        private static double SafeDouble(object o)
        {
            if (o == null || o == DBNull.Value) return 0d;
            double v;
            if (double.TryParse(Convert.ToString(o), NumberStyles.Any, CultureInfo.InvariantCulture, out v)) return v;
            if (double.TryParse(Convert.ToString(o), NumberStyles.Any, CultureInfo.CurrentCulture, out v)) return v;
            return 0d;
        }

        private static DateTime ParseDateSafe(object o, DateTime fallback)
        {
            try
            {
                DateTime d;
                if (DateTime.TryParse(Convert.ToString(o), out d)) return d.Date;
            }
            catch { }
            return fallback.Date;
        }

        private sealed class IdName
        {
            public IdName(int id, string name) { Id = id; Name = name; }
            public int Id { get; }
            public string Name { get; }
            public override string ToString() => Name;
        }
    }
}
