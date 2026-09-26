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
    /// Additive: Receipts total − Partner Cash Book (partner_money) balance.
    /// Does not change existing Receipt / Partner Cash UI.
    /// </summary>
    public class frmReceiptsMinusPartnerCredit : Sample
    {
        private DateTimePicker dtpFrom;
        private DateTimePicker dtpTo;
        private DataGridView dgv;
        private Label lblSummary;
        private Guna.UI2.WinForms.Guna2Button btnLoad;

        public frmReceiptsMinusPartnerCredit()
        {
            InitializeUi();
            dtpFrom.Value = DateTime.Today.AddYears(-5);
            dtpTo.Value = DateTime.Today;
            LoadReport();
        }

        private void InitializeUi()
        {
            Text = "Receipts − Partner Credit";
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
                Text = "Receipts − Partner Credit"
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
            var lblFrom = new Label
            {
                Text = "From",
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(12, 8),
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold)
            };
            dtpFrom = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd-MMM-yyyy",
                Location = new Point(12, 30),
                Size = new Size(160, 28),
                Font = new Font("Segoe UI", 10F)
            };
            var lblTo = new Label
            {
                Text = "To",
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(190, 8),
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold)
            };
            dtpTo = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd-MMM-yyyy",
                Location = new Point(190, 30),
                Size = new Size(160, 28),
                Font = new Font("Segoe UI", 10F)
            };
            btnLoad = new Guna.UI2.WinForms.Guna2Button
            {
                Text = "Load Report",
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                FillColor = Color.FromArgb(18, 140, 126),
                ForeColor = Color.White,
                BorderRadius = 8,
                Location = new Point(370, 26),
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
            panelFilters.Controls.Add(lblFrom);
            panelFilters.Controls.Add(dtpFrom);
            panelFilters.Controls.Add(lblTo);
            panelFilters.Controls.Add(dtpTo);
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

        private void LoadReport()
        {
            try
            {
                try { HubBuilders.MainClass.EnsurePartnerMoneySchema(); } catch { }

                string df = dtpFrom.Value.Date.ToString("yyyy-MM-dd");
                string dt = dtpTo.Value.Date.ToString("yyyy-MM-dd");

                double receipts = 0, partnerIn = 0, partnerOut = 0, creditForm = 0;
                using (var con = new SQLiteConnection(projectconnection.conReturn()))
                {
                    con.Open();
                    using (var cmd = new SQLiteCommand(@"
SELECT COALESCE(SUM(amount),0) FROM receipts
WHERE DATE(receipt_date) >= DATE(@df)
  AND DATE(receipt_date) < DATE(@dt, '+1 day')", con))
                    {
                        cmd.Parameters.AddWithValue("@df", df);
                        cmd.Parameters.AddWithValue("@dt", dt);
                        receipts = Convert.ToDouble(cmd.ExecuteScalar() ?? 0);
                    }
                    // Partner Cash Book — In
                    using (var cmd = new SQLiteCommand(@"
SELECT COALESCE(SUM(amount),0) FROM partner_money
WHERE UPPER(IFNULL(direction,'')) = 'IN'
  AND (
    IFNULL(TRIM(txn_date),'') = ''
    OR (DATE(txn_date) >= DATE(@df) AND DATE(txn_date) < DATE(@dt, '+1 day'))
  )", con))
                    {
                        cmd.Parameters.AddWithValue("@df", df);
                        cmd.Parameters.AddWithValue("@dt", dt);
                        partnerIn = Convert.ToDouble(cmd.ExecuteScalar() ?? 0);
                    }
                    // Partner Cash Book — Out
                    using (var cmd = new SQLiteCommand(@"
SELECT COALESCE(SUM(amount),0) FROM partner_money
WHERE UPPER(IFNULL(direction,'')) = 'OUT'
  AND (
    IFNULL(TRIM(txn_date),'') = ''
    OR (DATE(txn_date) >= DATE(@df) AND DATE(txn_date) < DATE(@dt, '+1 day'))
  )", con))
                    {
                        cmd.Parameters.AddWithValue("@df", df);
                        cmd.Parameters.AddWithValue("@dt", dt);
                        partnerOut = Convert.ToDouble(cmd.ExecuteScalar() ?? 0);
                    }
                    try
                    {
                        using (var cmd = new SQLiteCommand(@"
SELECT COALESCE(SUM(amount),0) FROM Credit
WHERE (
  IFNULL(TRIM(pay_date),'') = ''
  OR (DATE(pay_date) >= DATE(@df) AND DATE(pay_date) < DATE(@dt, '+1 day'))
)", con))
                        {
                            cmd.Parameters.AddWithValue("@df", df);
                            cmd.Parameters.AddWithValue("@dt", dt);
                            creditForm = Convert.ToDouble(cmd.ExecuteScalar() ?? 0);
                        }
                    }
                    catch { creditForm = 0; }
                }

                double partnerCash = partnerIn + partnerOut;
                double partnerTotal = partnerCash + creditForm;
                double bal = receipts - partnerTotal;
                var table = new DataTable();
                table.Columns.Add("Particular", typeof(string));
                table.Columns.Add("Amount", typeof(double));
                table.Rows.Add("Total Receipts (Receipt Form)", receipts);
                table.Rows.Add("Partner Cash Book — In", partnerIn);
                table.Rows.Add("Partner Cash Book — Out", partnerOut);
                table.Rows.Add("Partner Cash Book Total", partnerCash);
                if (creditForm > 0)
                    table.Rows.Add("Credit Form (Partner)", creditForm);
                table.Rows.Add("Total Partner Credit", partnerTotal);
                table.Rows.Add("Balance (Receipts − Partner Credit)", bal);

                dgv.DataSource = table;
                StyleGrid(dgv);
                if (dgv.Columns.Contains("Amount"))
                    dgv.Columns["Amount"].DefaultCellStyle.Format = "N2";

                lblSummary.Text = "Receipts: " + receipts.ToString("N2", CultureInfo.InvariantCulture)
                    + "   −   Partner Cash Book: " + partnerCash.ToString("N2", CultureInfo.InvariantCulture)
                    + "   =   Balance: " + bal.ToString("N2", CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Report", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
