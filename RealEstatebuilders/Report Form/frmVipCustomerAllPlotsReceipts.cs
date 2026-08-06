using HubBuilders.ProjectConnection;
using Microsoft.Reporting.WinForms;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace HubBuilders.Report_Form
{
    /// <summary>
    /// VIP: Customer -> multiple plots -> all receipts (plot-wise) + grand total.
    /// Additive-only: does not modify existing receipts UI.
    /// </summary>
    public class frmVipCustomerAllPlotsReceipts : Sample
    {
        private ComboBox cmbCustomer;
        private DateTimePicker dtpFrom;
        private DateTimePicker dtpTo;
        private Guna.UI2.WinForms.Guna2Button btnPreview;
        private Guna.UI2.WinForms.Guna2Button btnClear;
        private TextBox txtHint;
        private ReportViewer reportViewer1;
        private Panel _panelTop;
        private Guna.UI2.WinForms.Guna2ControlBox _gunaMinimize;
        private Guna.UI2.WinForms.Guna2ControlBox _gunaMaximize;
        private Guna.UI2.WinForms.Guna2ControlBox _gunaClose;

        private bool _busy;

        private const string Brand = "Hub Builders";

        public frmVipCustomerAllPlotsReceipts()
        {
            InitializeUi();
            LoadCustomers();
            dtpFrom.Value = DateTime.Today.AddYears(-5);
            dtpTo.Value = DateTime.Today;
        }

        private void InitializeUi()
        {
            this.Text = "Customer Plots Receipts (VIP)";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.FromArgb(32, 36, 61);

            _panelTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(32, 36, 61)
            };
            var panelTop = _panelTop;

            var labelTitle = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.White,
                Location = new Point(12, 16),
                Text = "Customer Plots Receipts (VIP)"
            };

            // Same as other VIP reports + Minimize; position from right edge (fixed X breaks on narrow width)
            _gunaMinimize = new Guna.UI2.WinForms.Guna2ControlBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                ControlBoxType = Guna.UI2.WinForms.Enums.ControlBoxType.MinimizeBox,
                FillColor = Color.FromArgb(139, 152, 166),
                IconColor = Color.White,
                Name = "guna2ControlBoxMinimize",
                Size = new Size(56, 36),
                TabIndex = 3
            };
            _gunaMaximize = new Guna.UI2.WinForms.Guna2ControlBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                ControlBoxType = Guna.UI2.WinForms.Enums.ControlBoxType.MaximizeBox,
                FillColor = Color.FromArgb(139, 152, 166),
                IconColor = Color.White,
                Name = "guna2ControlBox2",
                Size = new Size(56, 36),
                TabIndex = 2
            };
            _gunaClose = new Guna.UI2.WinForms.Guna2ControlBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FillColor = Color.FromArgb(139, 152, 166),
                IconColor = Color.White,
                Name = "guna2ControlBox1",
                Size = new Size(56, 36),
                TabIndex = 1
            };

            try
            {
                _gunaMinimize.HoverState.Parent = _gunaMinimize;
                _gunaMinimize.ShadowDecoration.Parent = _gunaMinimize;
                _gunaMaximize.HoverState.Parent = _gunaMaximize;
                _gunaMaximize.ShadowDecoration.Parent = _gunaMaximize;
                _gunaClose.HoverState.Parent = _gunaClose;
                _gunaClose.ShadowDecoration.Parent = _gunaClose;
            }
            catch { }

            panelTop.Controls.Add(labelTitle);
            panelTop.Controls.Add(_gunaMinimize);
            panelTop.Controls.Add(_gunaMaximize);
            panelTop.Controls.Add(_gunaClose);
            LayoutVipControlBoxes();
            panelTop.SizeChanged += (s, e) => LayoutVipControlBoxes();
            this.Shown += (s, e) => LayoutVipControlBoxes();

            var panelLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 430,
                Padding = new Padding(8),
                BackColor = Color.FromArgb(32, 36, 61)
            };

            var panelFields = new Panel
            {
                Dock = DockStyle.Top,
                Height = 356
            };
            var lblHeader = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(4, 6),
                Text = "Customer (important)"
            };

            cmbCustomer = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F),
                Location = new Point(4, 28),
                Size = new Size(400, 25)
            };

            var lblFrom = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(4, 116),
                Text = "From Date"
            };

            dtpFrom = new DateTimePicker
            {
                Font = new Font("Segoe UI", 10F),
                Format = DateTimePickerFormat.Short,
                Location = new Point(4, 138),
                Size = new Size(190, 25)
            };

            var lblTo = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(210, 116),
                Text = "To Date"
            };

            dtpTo = new DateTimePicker
            {
                Font = new Font("Segoe UI", 10F),
                Format = DateTimePickerFormat.Short,
                Location = new Point(210, 138),
                Size = new Size(194, 25)
            };

            var lblHint = new Label
            {
                Font = new Font("Segoe UI", 8.25F),
                ForeColor = Color.FromArgb(180, 190, 210),
                Location = new Point(4, 172),
                Size = new Size(400, 46),
                Text = "Shows receipts for all plots allocated to selected customer.\nHeader shows grand total amount."
            };

            txtHint = new TextBox
            {
                ReadOnly = true,
                Visible = false
            };

            panelFields.Controls.Add(lblHeader);
            panelFields.Controls.Add(cmbCustomer);
            panelFields.Controls.Add(lblFrom);
            panelFields.Controls.Add(dtpFrom);
            panelFields.Controls.Add(lblTo);
            panelFields.Controls.Add(dtpTo);
            panelFields.Controls.Add(lblHint);

            var panelButtons = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56
            };

            btnPreview = new Guna.UI2.WinForms.Guna2Button
            {
                BorderRadius = 8,
                FillColor = Color.FromArgb(10, 40, 100),
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(4, 8),
                Size = new Size(200, 40),
                Text = "Print Preview"
            };
            btnPreview.Click += btnPreview_Click;

            btnClear = new Guna.UI2.WinForms.Guna2Button
            {
                BorderRadius = 8,
                FillColor = Color.FromArgb(100, 100, 120),
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(210, 8),
                Size = new Size(196, 40),
                Text = "Clear"
            };
            btnClear.Click += btnClear_Click;

            panelButtons.Controls.Add(btnPreview);
            panelButtons.Controls.Add(btnClear);

            panelLeft.Controls.Add(panelFields);
            panelLeft.Controls.Add(panelButtons);

            reportViewer1 = new ReportViewer
            {
                Dock = DockStyle.Fill,
                ProcessingMode = ProcessingMode.Local,
                Location = new Point(430, 70)
            };

            // Same dock order as other VIP reports (reportViewer first, then left, then top)
            this.Controls.Add(reportViewer1);
            this.Controls.Add(panelLeft);
            this.Controls.Add(panelTop);
        }

        private void LayoutVipControlBoxes()
        {
            try
            {
                if (_panelTop == null || _gunaClose == null || _gunaMaximize == null || _gunaMinimize == null)
                    return;

                int w = _panelTop.ClientSize.Width;
                if (w <= 0)
                    w = ClientSize.Width;
                const int top = 12;
                const int gap = 4;
                const int btnW = 56;

                int xClose = Math.Max(8, w - btnW - 8);
                int xMax = xClose - gap - btnW;
                int xMin = xMax - gap - btnW;

                _gunaClose.Location = new Point(xClose, top);
                _gunaMaximize.Location = new Point(xMax, top);
                _gunaMinimize.Location = new Point(xMin, top);

                _gunaClose.BringToFront();
                _gunaMaximize.BringToFront();
                _gunaMinimize.BringToFront();
            }
            catch { }
        }

        private void LoadCustomers()
        {
            try
            {
                var table = new DataTable();
                table.Columns.Add("id", typeof(int));
                table.Columns.Add("display", typeof(string));

                using (var con = new SQLiteConnection(projectconnection.conReturn()))
                using (var cmd = new SQLiteCommand(
                    "SELECT id, name, COALESCE(mobile,'') AS mobile FROM customers ORDER BY name;",
                    con))
                {
                    con.Open();
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            int id = Convert.ToInt32(r["id"]);
                            string name = Convert.ToString(r["name"]);
                            string mobile = Convert.ToString(r["mobile"]);
                            string disp = string.IsNullOrWhiteSpace(mobile) ? name : (name + " | " + mobile);
                            table.Rows.Add(id, disp);
                        }
                    }
                }

                cmbCustomer.DisplayMember = "display";
                cmbCustomer.ValueMember = "id";
                cmbCustomer.DataSource = table;
                if (cmbCustomer.Items.Count > 0) cmbCustomer.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Customer load failed.\n\n" + ex.Message, "VIP Report", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            try
            {
                dtpFrom.Value = DateTime.Today.AddYears(-5);
                dtpTo.Value = DateTime.Today;
                cmbCustomer.SelectedIndex = cmbCustomer.Items.Count > 0 ? 0 : -1;
                reportViewer1?.Reset();
            }
            catch { }
        }

        private void btnPreview_Click(object sender, EventArgs e)
        {
            if (_busy) return;
            _busy = true;
            try
            {
                Cursor = Cursors.WaitCursor;

                if (cmbCustomer?.SelectedValue == null)
                {
                    MessageBox.Show("Customer select karein.", "VIP Report", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (dtpFrom.Value.Date > dtpTo.Value.Date)
                {
                    MessageBox.Show("From date To date se bara nahi ho sakta.", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int customerId = Convert.ToInt32(cmbCustomer.SelectedValue);

                string customerName;
                double grandTotal;
                int grandReceiptCount;
                DataTable dt = BuildData(customerId, dtpFrom.Value.Date, dtpTo.Value.Date,
                    out customerName, out grandTotal, out grandReceiptCount);

                if (dt == null || dt.Rows.Count == 0)
                {
                    MessageBox.Show("Is date range mein receipts nahi mile.", "VIP Report",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    reportViewer1?.RefreshReport();
                    return;
                }

                string reportPath = Path.Combine(Application.StartupPath, "Reports", "VipCustomerAllPlotsReceipts.rdlc");
                if (!File.Exists(reportPath))
                {
                    MessageBox.Show("Report file not found:\n" + reportPath, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                reportViewer1.Reset();
                reportViewer1.ProcessingMode = ProcessingMode.Local;
                reportViewer1.LocalReport.ReportPath = reportPath;
                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(new ReportDataSource("DataSet1", dt));

                string period = dtpFrom.Value.ToString("dd-MMM-yyyy") + "  ->  " + dtpTo.Value.ToString("dd-MMM-yyyy");
                reportViewer1.LocalReport.SetParameters(new ReportParameter[]
                {
                    new ReportParameter("HeaderBrand", Brand ?? "Hub Builders"),
                    new ReportParameter("CustomerNameText", string.IsNullOrWhiteSpace(customerName) ? "(Customer)" : customerName),
                    new ReportParameter("TotalAmountText", grandTotal.ToString("N2", CultureInfo.InvariantCulture)),
                    new ReportParameter("PeriodText", period)
                });

                reportViewer1.ZoomMode = ZoomMode.PageWidth;
                reportViewer1.RefreshReport();
            }
            catch (Exception ex)
            {
                string detail = ex.Message;
                try
                {
                    if (ex.InnerException != null)
                        detail += "\n\n" + ex.InnerException.Message;
                    if (ex.InnerException?.InnerException != null)
                        detail += "\n\n" + ex.InnerException.InnerException.Message;
                }
                catch { }
                MessageBox.Show("Preview failed.\n\n" + detail, "VIP Report",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
                _busy = false;
            }
        }

        private sealed class ReceiptDetail
        {
            public int CustomerPlotId;
            public string Society;
            public string Block;
            public string PlotNo;
            public string PlotType;

            public int ReceiptId;
            public string ReceiptDate;
            public decimal Amount;
            public string Mode;
            public string RefNo;
            public string Notes;
            public string CustomerName;
        }

        private DataTable BuildData(
            int customerId,
            DateTime fromDate,
            DateTime toDate,
            out string customerName,
            out double grandTotal,
            out int grandReceiptCount)
        {
            customerName = string.Empty;
            grandTotal = 0d;
            grandReceiptCount = 0;

            var dt = new DataTable();
            dt.Columns.Add("sr_no", typeof(int));
            dt.Columns.Add("row_type", typeof(string));
            dt.Columns.Add("row_label", typeof(string));
            dt.Columns.Add("receipt_date", typeof(string));
            dt.Columns.Add("receipt_id", typeof(string));
            dt.Columns.Add("mode", typeof(string));
            dt.Columns.Add("refno", typeof(string));
            dt.Columns.Add("notes", typeof(string));
            dt.Columns.Add("amount", typeof(double));

            var details = new List<ReceiptDetail>();

            string sql = @"
SELECT
  c.name AS customer_name,
  s.name AS society_name,
  bl.name AS block_name,
  CAST(p.plot_no AS TEXT) AS plot_no,
  IFNULL(pt.code,'') AS plot_type,
  cp.id AS customer_plot_id,
  r.id AS receipt_id,
  r.receipt_date AS receipt_date,
  r.amount AS amount,
  IFNULL(r.mode,'') AS mode,
  IFNULL(r.ref_no,'') AS refno,
  IFNULL(r.notes,'') AS notes
FROM customer_plots cp
JOIN customers  c  ON c.id = cp.customer_id
JOIN plots      p  ON p.id = cp.plot_id
LEFT JOIN societies  s  ON s.id = p.society_id
LEFT JOIN blocks     bl ON bl.id = p.block_id
LEFT JOIN plot_types pt ON pt.id = p.plot_type_id
JOIN receipts   r  ON r.customer_plot_id = cp.id
WHERE cp.status = 'Active'
  AND c.id = @cid
  AND DATE(r.receipt_date) >= DATE(@from)
  AND DATE(r.receipt_date) <= DATE(@to)
ORDER BY s.name,
         bl.name,
         CAST(p.plot_no AS INTEGER),
         DATE(r.receipt_date),
         r.id;";

            using (var con = new SQLiteConnection(projectconnection.conReturn()))
            using (var cmd = new SQLiteCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@cid", customerId);
                cmd.Parameters.AddWithValue("@from", fromDate.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@to", toDate.ToString("yyyy-MM-dd"));

                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        var item = new ReceiptDetail
                        {
                            CustomerPlotId = Convert.ToInt32(r["customer_plot_id"]),
                            Society = Convert.ToString(r["society_name"] ?? string.Empty),
                            Block = Convert.ToString(r["block_name"] ?? string.Empty),
                            PlotNo = Convert.ToString(r["plot_no"] ?? string.Empty),
                            PlotType = Convert.ToString(r["plot_type"] ?? string.Empty),

                            ReceiptId = Convert.ToInt32(r["receipt_id"]),
                            ReceiptDate = Convert.ToString(r["receipt_date"] ?? string.Empty),
                            Amount = Convert.ToDecimal(r["amount"] ?? 0m),

                            Mode = Convert.ToString(r["mode"] ?? string.Empty),
                            RefNo = Convert.ToString(r["refno"] ?? string.Empty),
                            Notes = Convert.ToString(r["notes"] ?? string.Empty),
                            CustomerName = Convert.ToString(r["customer_name"] ?? string.Empty)
                        };

                        details.Add(item);
                    }
                }
            }

            if (details.Count == 0)
                return dt;

            customerName = details[0].CustomerName ?? string.Empty;
            grandReceiptCount = details.Count;
            grandTotal = (double)details.Sum(x => x.Amount);

            // Group by plot
            var plotGroups = details
                .GroupBy(x => x.CustomerPlotId)
                .Select(g => new
                {
                    Group = g,
                    SortSoc = g.FirstOrDefault()?.Society ?? "",
                    SortBlock = g.FirstOrDefault()?.Block ?? "",
                    SortPlotNo = ToIntSafe(g.FirstOrDefault()?.PlotNo)
                })
                .OrderBy(x => x.SortSoc, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.SortBlock, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.SortPlotNo)
                .ToList();

            int sr = 1;

            foreach (var g in plotGroups)
            {
                var ordered = g.Group
                    .OrderBy(x => TryParseDate(x.ReceiptDate))
                    .ThenBy(x => x.ReceiptId)
                    .ToList();

                var first = ordered[0];
                string plotDisp = BuildPlotDisp(first.Society, first.Block, first.PlotNo, first.PlotType);
                decimal plotTotal = ordered.Sum(x => x.Amount);

                foreach (var x in ordered)
                {
                    dt.Rows.Add(
                        sr++,
                        "DETAIL",
                        plotDisp,
                        x.ReceiptDate,
                        x.ReceiptId.ToString(),
                        x.Mode,
                        x.RefNo,
                        x.Notes,
                        (double)x.Amount
                    );
                }

                dt.Rows.Add(
                    0,
                    "PLOT_TOTAL",
                    "PLOT TOTAL - " + plotDisp + "  (" + ordered.Count + " receipts)",
                    "",
                    "",
                    "",
                    "",
                    "",
                    (double)plotTotal
                );
            }

            dt.Rows.Add(
                0,
                "GRAND_TOTAL",
                "GRAND TOTAL  (" + grandReceiptCount + " receipts)",
                "",
                "",
                "",
                "",
                "",
                grandTotal
            );

            return dt;
        }

        private static string BuildPlotDisp(string society, string block, string plotNo, string plotType)
        {
            society = society ?? "";
            block = block ?? "";
            plotNo = plotNo ?? "";
            plotType = plotType ?? "";

            string pt = string.IsNullOrWhiteSpace(plotType) ? "" : (" [" + plotType.Trim() + "]");
            string sb = string.IsNullOrWhiteSpace(society) ? "" : society.Trim();
            string bl = string.IsNullOrWhiteSpace(block) ? "" : block.Trim();

            // Keep it compact for RDLC
            if (string.IsNullOrWhiteSpace(bl))
                return sb + " / Plot " + plotNo + pt;
            return sb + " / " + bl + " / Plot " + plotNo + pt;
        }

        private static int ToIntSafe(string s)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(s)) return 0;
                if (int.TryParse(s, out int v)) return v;

                // remove non digits e.g. "B-41" -> 41
                var digits = new string(s.Where(char.IsDigit).ToArray());
                if (int.TryParse(digits, out v)) return v;
                return 0;
            }
            catch { return 0; }
        }

        private static DateTime TryParseDate(string s)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(s)) return DateTime.MinValue;
                if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    return dt;
                return DateTime.MinValue;
            }
            catch { return DateTime.MinValue; }
        }
    }
}

