using System;
using System.Collections;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace HubBuilders.Model
{
    // UI:
    // cmbCustomer, cmbSociety, cmbCustomerPlot, dtpFrom, dtpTo, btnLoad, grdLedger,
    // lblOpeningBalance, lblClosingBalance, lblBalance, btnPrint
    //
    // Rule for labels within selected period:
    //   lblOpeningBalance = Total Debit (period)
    //   lblClosingBalance = Total Credit (period)
    //   lblBalance        = Debit - Credit
    public partial class frmCustomerLedgerAdd : Sample
    {
        private bool _cmbCustomerPlotIsAllocationIds = true;
        private bool _gridColsReady = false;

        // ===== Printing state =====
        private PrintDocument _doc;
        private int _printRowIndex = 0;
        private DataTable _printTable;     // rows to render
        private string _printTitle;        // "Customer Ledger"
        private string _printSubTitle;     // customer/society/plot + period
        private string _printFooterLeft;   // generated on

        // Typography
        private readonly Font _fTitle = new Font("Segoe UI", 16, FontStyle.Bold);
        private readonly Font _fSub = new Font("Segoe UI", 9, FontStyle.Regular);
        private readonly Font _fHdr = new Font("Segoe UI Semibold", 9, FontStyle.Bold);
        private readonly Font _fRow = new Font("Segoe UI", 9, FontStyle.Regular);

        private readonly Pen _pen = new Pen(Color.FromArgb(60, 60, 60), 1f);

        // Page box, content box, table geometry
        private float _pageLeft, _pageTop, _pageRight, _pageBottom;
        private float _contentLeft, _contentRight, _contentWidth;
        private float _y;

        // Table column widths (points in 1/100 inch device units)
        private float[] _colW; // computed per page
        private readonly string[] _heads = { "Date", "Voucher", "Narration", "Debit", "Credit", "Running" };

        public frmCustomerLedgerAdd()
        {
            InitializeComponent();

            if (dtpFrom != null) dtpFrom.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            if (dtpTo != null) dtpTo.Value = DateTime.Today;

            if (cmbCustomer != null) cmbCustomer.SelectionChangeCommitted += (_, __) => LoadAllocations();
            if (cmbSociety != null) cmbSociety.SelectionChangeCommitted += (_, __) => LoadAllocations();
            if (btnLoad != null) btnLoad.Click += btnLoad_Click;

            if (this.Controls.ContainsKey("btnPrint"))
            {
                var bp = this.Controls["btnPrint"] as Button;
                if (bp != null) bp.Click += btnPrint_Click;
            }

            LoadCustomers();
            LoadSocieties();
            LoadAllocations();
            EnsureGridColumns();
        }

        // ---------------------- Master data ----------------------

        private void LoadCustomers()
        {
            try
            {
                const string q = @"
                    SELECT id,
                           name || CASE WHEN IFNULL(father_name,'') <> '' THEN ' / ' || father_name ELSE '' END AS label
                    FROM customers
                    ORDER BY name COLLATE NOCASE";
                var dt = MainClass.ExecuteSelectQuery(q, null);
                BindCombo(cmbCustomer, dt, "label", "id");
            }
            catch (Exception ex) { MessageBox.Show("Customers load error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void LoadSocieties()
        {
            try
            {
                const string q = @"SELECT id, name FROM societies ORDER BY name COLLATE NOCASE";
                var dt = MainClass.ExecuteSelectQuery(q, null);
                BindCombo(cmbSociety, dt, "name", "id");
            }
            catch (Exception ex) { MessageBox.Show("Societies load error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void LoadAllocations()
        {
            try
            {
                int? customerId = GetId(cmbCustomer);
                int? societyId = GetId(cmbSociety);

                _cmbCustomerPlotIsAllocationIds = true;

                bool hasCP =
                    TableExists("customer_plots") &&
                    ColumnExists("customer_plots", "id") &&
                    ColumnExists("customer_plots", "customer_id") &&
                    ColumnExists("customer_plots", "plot_id");

                DataTable dt = null;
                var ht = new Hashtable();

                if (hasCP && customerId.HasValue)
                {
                    string sql = @"
                        SELECT
                            cp.id     AS value_id,
                            p.plot_no AS label
                        FROM customer_plots cp
                        JOIN plots p     ON p.id = cp.plot_id
                        JOIN societies s ON s.id = p.society_id
                        WHERE cp.customer_id = @cid
                        {AND_SOC}
                        ORDER BY p.plot_no COLLATE NOCASE";
                    ht["@cid"] = customerId.Value;

                    if (societyId.HasValue) { sql = sql.Replace("{AND_SOC}", "AND s.id = @sid"); ht["@sid"] = societyId.Value; }
                    else sql = sql.Replace("{AND_SOC}", "");

                    dt = MainClass.ExecuteSelectQuery(sql, ht);
                }

                if (dt == null || dt.Rows.Count == 0)
                {
                    _cmbCustomerPlotIsAllocationIds = false;
                    ht = new Hashtable();

                    string sqlPlots = @"
                        SELECT
                            p.id      AS value_id,
                            p.plot_no AS label
                        FROM plots p
                        {JOIN_SOC}
                        WHERE 1=1
                        {AND_SOC}
                        {AND_STATUS}
                        ORDER BY p.plot_no COLLATE NOCASE";
                    if (societyId.HasValue)
                    {
                        sqlPlots = sqlPlots.Replace("{JOIN_SOC}", "JOIN societies s ON s.id = p.society_id");
                        sqlPlots = sqlPlots.Replace("{AND_SOC}", "AND s.id = @sid");
                        ht["@sid"] = societyId.Value;
                    }
                    else
                    {
                        sqlPlots = sqlPlots.Replace("{JOIN_SOC}", "");
                        sqlPlots = sqlPlots.Replace("{AND_SOC}", "");
                    }

                    sqlPlots = sqlPlots.Replace("{AND_STATUS}", "AND p.status = 'Sold'");
                    dt = MainClass.ExecuteSelectQuery(sqlPlots, ht);
                }

                BindCombo(cmbCustomerPlot, dt, "label", "value_id");
            }
            catch (Exception ex) { MessageBox.Show("Allocations load error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // ---------------------- Load ledger (period preview) ----------------------

        private void btnLoad_Click(object sender, EventArgs e)
        {
            if (cmbCustomer == null || GetId(cmbCustomer) == null)
            {
                MessageBox.Show("Customer required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbCustomer?.Focus();
                return;
            }

            try
            {
                int customerId = GetId(cmbCustomer).Value;
                int? societyId = GetId(cmbSociety);
                int? selectedValueId = GetId(cmbCustomerPlot);

                string fromStart = (dtpFrom?.Value ?? DateTime.Today).ToString("yyyy-MM-dd") + " 00:00:00";
                string toEnd = (dtpTo?.Value ?? DateTime.Today).ToString("yyyy-MM-dd") + " 23:59:59";

                // Period rows: ledger UNION allocation price as debit
                DataTable dt = LoadRows(fromStart, toEnd, customerId, societyId, selectedValueId);
                if (!dt.Columns.Contains("RunningBalance")) dt.Columns.Add("RunningBalance", typeof(double));

                // Opening (for running balance seed)
                double opening = SumLedgerNetBefore(fromStart, customerId, societyId, selectedValueId)
                               + SumAllocationPriceBefore(fromStart, customerId, societyId, selectedValueId);

                double bal = opening, periodDebit = 0, periodCredit = 0;

                foreach (DataRow r in dt.Rows)
                {
                    double d = ToDouble(r["debit"]);
                    double c = ToDouble(r["credit"]);
                    periodDebit += d;
                    periodCredit += c;
                    bal += (d - c);
                    r["RunningBalance"] = bal;
                }

                EnsureGridColumns();
                grdLedger.DataSource = dt;

                // Labels per rule
                if (lblOpeningBalance != null) lblOpeningBalance.Text = periodDebit.ToString("N2", CultureInfo.InvariantCulture);
                if (lblClosingBalance != null) lblClosingBalance.Text = periodCredit.ToString("N2", CultureInfo.InvariantCulture);
                if (lblBalance != null) lblBalance.Text = (periodDebit - periodCredit).ToString("N2", CultureInfo.InvariantCulture);
            }
            catch (Exception ex) { MessageBox.Show("Load error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // ---------------------- Print (A4 centered table, no autosave) ----------------------

        private void btnPrint_Click(object sender, EventArgs e)
        {
            if (grdLedger?.DataSource == null)
            {
                btnLoad_Click(null, EventArgs.Empty);
                if (grdLedger?.DataSource == null) { MessageBox.Show("No data."); return; }
            }

            _printTable = ((DataTable)grdLedger.DataSource).Copy();

            string cust = cmbCustomer?.Text ?? "";
            string soc = cmbSociety?.Text ?? "";
            string plot = cmbCustomerPlot?.Text ?? "";
            string dr = $"{(dtpFrom?.Value ?? DateTime.Today):yyyy-MM-dd} → {(dtpTo?.Value ?? DateTime.Today):yyyy-MM-dd}";

            _printTitle = "Customer Ledger";
            _printSubTitle = $"Customer: {cust}    Society: {soc}    Plot: {plot}    Period: {dr}";
            _printFooterLeft = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}";

            _doc = new PrintDocument();
            // A4 in hundredths of an inch: 827x1169; use sane margins and extra inner padding for “left/right gaps”
            _doc.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
            _doc.DefaultPageSettings.Margins = new Margins(60, 60, 60, 60); // outer 0.6in margins
            _doc.OriginAtMargins = true;
            _doc.BeginPrint += (_, __) => { _printRowIndex = 0; };
            _doc.PrintPage += Doc_PrintPage;

            // Preview only; printing and (if you choose "Microsoft Print to PDF") the OS Save dialog will appear—no autosave.
            using (var ppd = new PrintPreviewDialog { Document = _doc, Width = 1200, Height = 800 })
            {
                try { ppd.ShowDialog(this); } catch { /* ignore */ }
            }

            using (var pd = new PrintDialog { Document = _doc, AllowSomePages = true })
            {
                if (pd.ShowDialog(this) == DialogResult.OK)
                {
                    _doc.PrinterSettings = pd.PrinterSettings;
                    // NOTE: No PrintToFile/PrintFileName set — you decide/save yourself.
                    _doc.Print();
                }
            }
        }

        private void Doc_PrintPage(object sender, PrintPageEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PageUnit = GraphicsUnit.Display;

            // Outer page box from margins
            _pageLeft = e.MarginBounds.Left;
            _pageTop = e.MarginBounds.Top;
            _pageRight = e.MarginBounds.Right;
            _pageBottom = e.MarginBounds.Bottom;

            // Inner content region with additional left/right gaps (visual breathing room)
            float sideGap = 30f; // ~0.3in inner gap in addition to margin
            _contentLeft = _pageLeft + sideGap;
            _contentRight = _pageRight - sideGap;
            _contentWidth = _contentRight - _contentLeft;

            _y = _pageTop;

            DrawHeader(g);
            DrawSummary(g);

            // Compute column widths and centered table X
            // Fixed ideal widths; narration is elastic
            float wDate = 90, wVoucher = 100, wDebit = 90, wCredit = 90, wRunning = 110;
            float wNarrMin = 220;
            float fixedSum = wDate + wVoucher + wDebit + wCredit + wRunning;
            float wNarr = Math.Max(wNarrMin, _contentWidth - fixedSum);
            float tableWidth = fixedSum + wNarr;
            float startX = _contentLeft + (_contentWidth - tableWidth) / 2f; // center table within content

            _colW = new[] { wDate, wVoucher, wNarr, wDebit, wCredit, wRunning };

            // Header row
            float x = startX;
            float hdrH = 26f;
            for (int i = 0; i < _heads.Length; i++)
            {
                var r = new RectangleF(x, _y, _colW[i], hdrH);
                using (var b = new LinearGradientBrush(r, Color.White, Color.FromArgb(238, 241, 247), 90f))
                    g.FillRectangle(b, r);
                using (var pen = new Pen(Color.FromArgb(180, 180, 180)))
                    g.DrawRectangle(pen, Rectangle.Round(r));
                g.DrawString(_heads[i], _fHdr, Brushes.Black,
                    new RectangleF(r.Left + 6, r.Top + 5, r.Width - 12, r.Height - 10),
                    new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near, Trimming = StringTrimming.EllipsisCharacter });
                x += _colW[i];
            }
            _y += hdrH;

            // Body rows
            float rowH = 22f;
            int rowsDrawnThisPage = 0;
            while (_printRowIndex < _printTable.Rows.Count)
            {
                if (_y + rowH > _pageBottom - 40) break;

                var row = _printTable.Rows[_printRowIndex];
                x = startX;

                // Alternate fill
                if ((_printRowIndex % 2) == 1)
                {
                    using (var b = new SolidBrush(Color.FromArgb(250, 250, 250)))
                        g.FillRectangle(b, new RectangleF(x, _y, tableWidth, rowH));
                }

                // Cells
                DrawCell(g, row["txn_date"], _colW[0], ref x, rowH, fmt: "yyyy-MM-dd", alignRight: false);
                DrawCell(g, row["voucher_no"], _colW[1], ref x, rowH, alignRight: false);
                DrawCell(g, row["narration"], _colW[2], ref x, rowH, alignRight: false);
                DrawCell(g, row["debit"], _colW[3], ref x, rowH, fmt: "N2", alignRight: true);
                DrawCell(g, row["credit"], _colW[4], ref x, rowH, fmt: "N2", alignRight: true);
                DrawCell(g, row["RunningBalance"], _colW[5], ref x, rowH, fmt: "N2", alignRight: true);

                // Row grid
                using (var pen = new Pen(Color.FromArgb(230, 230, 230)))
                    g.DrawRectangle(pen, Rectangle.Round(new RectangleF(startX, _y, tableWidth, rowH)));

                _y += rowH;
                _printRowIndex++;
                rowsDrawnThisPage++;
            }

            // Footer line + text
            var footerY = _pageBottom - 24;
            using (var pen = new Pen(Color.FromArgb(200, 200, 200)))
                g.DrawLine(pen, _contentLeft, footerY, _contentRight, footerY);

            g.DrawString(_printFooterLeft, _fSub, Brushes.Gray, _contentLeft, footerY + 6);
            g.DrawString($"Page {e.PageSettings.PrinterSettings.ToPage:0}", _fSub, Brushes.Gray, _contentRight - 60, footerY + 6);

            e.HasMorePages = _printRowIndex < _printTable.Rows.Count;
        }

        private void DrawHeader(Graphics g)
        {
            float bannerH = _fTitle.GetHeight(g) + _fSub.GetHeight(g) + 16;

            var r = new RectangleF(_contentLeft, _y, _contentWidth, bannerH);
            using (var gp = new GraphicsPath())
            {
                float radius = 8f;
                AddRoundedRect(gp, r, radius);
                using (var b = new LinearGradientBrush(r, Color.FromArgb(33, 48, 88), Color.FromArgb(46, 65, 120), 90f))
                    g.FillPath(b, gp);
            }

            using (var wb = new SolidBrush(Color.White))
            {
                g.DrawString(_printTitle, _fTitle, wb, new RectangleF(_contentLeft + 10, _y + 6, _contentWidth - 20, 30),
                    new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near });
                g.DrawString(_printSubTitle, _fSub, wb, new RectangleF(_contentLeft + 10, _y + 8 + _fTitle.GetHeight(g), _contentWidth - 20, 20),
                    new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near, Trimming = StringTrimming.EllipsisCharacter });
            }
            _y += bannerH + 12;
        }

        private void DrawSummary(Graphics g)
        {
            string debitStr = lblOpeningBalance?.Text ?? "0.00";
            string creditStr = lblClosingBalance?.Text ?? "0.00";
            string netStr = lblBalance?.Text ?? "0.00";

            float boxH = 58f;
            float boxW = 220f;
            float spacing = 14f;
            float total = boxW * 3 + spacing * 2;
            float startX = _contentLeft + (_contentWidth - total) / 2f;

            DrawSummaryCard(g, "Debit (Period)", debitStr, startX + 0 * (boxW + spacing), _y, boxW, boxH, Color.FromArgb(245, 250, 255));
            DrawSummaryCard(g, "Credit (Period)", creditStr, startX + 1 * (boxW + spacing), _y, boxW, boxH, Color.FromArgb(255, 247, 247));
            DrawSummaryCard(g, "Balance (D − C)", netStr, startX + 2 * (boxW + spacing), _y, boxW, boxH, Color.FromArgb(246, 255, 246));

            _y += boxH + 16f;
        }

        private void DrawSummaryCard(Graphics g, string caption, string value, float x, float y, float w, float h, Color bg)
        {
            var r = new RectangleF(x, y, w, h);
            using (var gp = new GraphicsPath())
            {
                AddRoundedRect(gp, r, 8f);
                using (var b = new SolidBrush(bg)) g.FillPath(b, gp);
                using (var pen = new Pen(Color.FromArgb(210, 210, 210))) g.DrawPath(pen, gp);
            }
            g.DrawString(caption, _fSub, Brushes.Gray, new RectangleF(x + 10, y + 8, w - 20, 16),
                new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near, Trimming = StringTrimming.EllipsisCharacter });

            using (var f = new Font("Segoe UI Semibold", 12f))
                g.DrawString(value, f, Brushes.Black, new RectangleF(x + 10, y + 24, w - 20, h - 30),
                    new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near });
        }

        private void DrawCell(Graphics g, object val, float w, ref float x, float h, string fmt = null, bool alignRight = false)
        {
            var r = new RectangleF(x, _y, w, h);
            using (var pen = new Pen(Color.FromArgb(230, 230, 230)))
                g.DrawRectangle(pen, Rectangle.Round(r));

            string s;
            if (val == null || val == DBNull.Value) s = "";
            else if (fmt == "yyyy-MM-dd")
            {
                // input can be string/datetime
                DateTime dt;
                if (DateTime.TryParse(Convert.ToString(val), out dt)) s = dt.ToString("yyyy-MM-dd");
                else s = Convert.ToString(val);
            }
            else if (!string.IsNullOrEmpty(fmt) && val is IConvertible)
                s = Convert.ToDouble(val, CultureInfo.InvariantCulture).ToString(fmt, CultureInfo.InvariantCulture);
            else s = Convert.ToString(val);

            var sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, LineAlignment = StringAlignment.Center };
            sf.Alignment = alignRight ? StringAlignment.Far : StringAlignment.Near;

            g.DrawString(s, _fRow, Brushes.Black, new RectangleF(r.Left + 6, r.Top + 2, r.Width - 12, r.Height - 4), sf);
            x += w;
        }

        private static void AddRoundedRect(GraphicsPath path, RectangleF r, float radius)
        {
            float d = radius * 2;
            path.StartFigure();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
        }

        // ---------------------- SQL + Data ----------------------

        private double SumLedgerNetBefore(string fromStart, int customerId, int? societyId, int? selectedValueId)
        {
            string sql = @"
                SELECT COALESCE(SUM(COALESCE(l.debit,0) - COALESCE(l.credit,0)), 0.0)
                FROM ledger l
                JOIN customer_plots cp ON cp.id = l.customer_plot_id
                JOIN plots p ON p.id = cp.plot_id
                JOIN societies s ON s.id = p.society_id
                WHERE l.txn_date < @from AND cp.customer_id = @cid
                {AND_SOC} {AND_ALLOC_OR_PLOT}";
            var ht = new Hashtable { { "@from", fromStart }, { "@cid", customerId } };

            if (societyId.HasValue) { sql = sql.Replace("{AND_SOC}", "AND s.id = @sid"); ht["@sid"] = societyId.Value; }
            else sql = sql.Replace("{AND_SOC}", "");

            if (selectedValueId.HasValue)
            {
                if (_cmbCustomerPlotIsAllocationIds) { sql = sql.Replace("{AND_ALLOC_OR_PLOT}", "AND cp.id = @alloc"); ht["@alloc"] = selectedValueId.Value; }
                else { sql = sql.Replace("{AND_ALLOC_OR_PLOT}", "AND p.id = @pid"); ht["@pid"] = selectedValueId.Value; }
            }
            else sql = sql.Replace("{AND_ALLOC_OR_PLOT}", "");

            return ToDouble(MainClass.SQLScalar(sql, ht));
        }

        private double SumAllocationPriceBefore(string fromStart, int customerId, int? societyId, int? selectedValueId)
        {
            string sql = @"
                SELECT COALESCE(SUM(COALESCE(cp.price,0)), 0.0)
                FROM customer_plots cp
                JOIN plots p ON p.id = cp.plot_id
                JOIN societies s ON s.id = p.society_id
                WHERE cp.customer_id = @cid
                  AND cp.booking_date < substr(@from, 1, 10)
                  {AND_SOC} {AND_ALLOC_OR_PLOT}";
            var ht = new Hashtable { { "@cid", customerId }, { "@from", fromStart } };

            if (societyId.HasValue) { sql = sql.Replace("{AND_SOC}", "AND s.id = @sid"); ht["@sid"] = societyId.Value; }
            else sql = sql.Replace("{AND_SOC}", "");

            if (selectedValueId.HasValue)
            {
                if (_cmbCustomerPlotIsAllocationIds) { sql = sql.Replace("{AND_ALLOC_OR_PLOT}", "AND cp.id = @alloc"); ht["@alloc"] = selectedValueId.Value; }
                else { sql = sql.Replace("{AND_ALLOC_OR_PLOT}", "AND p.id = @pid"); ht["@pid"] = selectedValueId.Value; }
            }
            else sql = sql.Replace("{AND_ALLOC_OR_PLOT}", "");

            return ToDouble(MainClass.SQLScalar(sql, ht));
        }

        private DataTable LoadRows(string fromStart, string toEnd, int customerId, int? societyId, int? selectedValueId)
        {
            string rowsSql = @"
                SELECT 
                    l.txn_date              AS txn_date,
                    l.voucher_no            AS voucher_no,
                    l.narration             AS narration,
                    COALESCE(l.debit, 0.0)  AS debit,
                    COALESCE(l.credit, 0.0) AS credit,
                    l.txn_type              AS txn_type
                FROM ledger l
                JOIN customer_plots cp ON cp.id = l.customer_plot_id
                JOIN plots p ON p.id = cp.plot_id
                JOIN societies s ON s.id = p.society_id
                WHERE l.txn_date >= @from AND l.txn_date <= @to
                  AND cp.customer_id = @cid
                  {AND_SOC_L} {AND_ALLOC_OR_PLOT_L}

                UNION ALL

                SELECT
                    cp.booking_date || ' 00:00:00'   AS txn_date,
                    NULL                              AS voucher_no,
                    'Allocation price'                AS narration,
                    COALESCE(cp.price, 0.0)           AS debit,
                    0.0                               AS credit,
                    'Allocation'                      AS txn_type
                FROM customer_plots cp
                JOIN plots p ON p.id = cp.plot_id
                JOIN societies s ON s.id = p.society_id
                WHERE cp.booking_date >= substr(@from, 1, 10) AND cp.booking_date <= substr(@to, 1, 10)
                  AND cp.customer_id = @cid
                  {AND_SOC_CP} {AND_ALLOC_OR_PLOT_CP}
                ORDER BY txn_date ASC";

            var rowsHt = new Hashtable { { "@from", fromStart }, { "@to", toEnd }, { "@cid", customerId } };

            if (societyId.HasValue)
            {
                rowsSql = rowsSql.Replace("{AND_SOC_L}", "AND s.id = @sid");
                rowsSql = rowsSql.Replace("{AND_SOC_CP}", "AND s.id = @sid");
                rowsHt["@sid"] = societyId.Value;
            }
            else
            {
                rowsSql = rowsSql.Replace("{AND_SOC_L}", "");
                rowsSql = rowsSql.Replace("{AND_SOC_CP}", "");
            }

            if (selectedValueId.HasValue)
            {
                if (_cmbCustomerPlotIsAllocationIds)
                {
                    rowsSql = rowsSql.Replace("{AND_ALLOC_OR_PLOT_L}", "AND cp.id = @alloc");
                    rowsSql = rowsSql.Replace("{AND_ALLOC_OR_PLOT_CP}", "AND cp.id = @alloc");
                    rowsHt["@alloc"] = selectedValueId.Value;
                }
                else
                {
                    rowsSql = rowsSql.Replace("{AND_ALLOC_OR_PLOT_L}", "AND p.id = @pid");
                    rowsSql = rowsSql.Replace("{AND_ALLOC_OR_PLOT_CP}", "AND p.id = @pid");
                    rowsHt["@pid"] = selectedValueId.Value;
                }
            }
            else
            {
                rowsSql = rowsSql.Replace("{AND_ALLOC_OR_PLOT_L}", "");
                rowsSql = rowsSql.Replace("{AND_ALLOC_OR_PLOT_CP}", "");
            }

            return MainClass.ExecuteSelectQuery(rowsSql, rowsHt) ?? new DataTable();
        }

        // ---------------------- Grid schema ----------------------

        private void EnsureGridColumns()
        {
            if (_gridColsReady || grdLedger == null) return;

            grdLedger.AutoGenerateColumns = false;
            grdLedger.Columns.Clear();

            grdLedger.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Date",
                DataPropertyName = "txn_date",
                Name = "colDate",
                DefaultCellStyle = { Format = "yyyy-MM-dd" },
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            });

            grdLedger.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "VoucherNo",
                DataPropertyName = "voucher_no",
                Name = "colVoucher",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            });

            grdLedger.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Narration",
                DataPropertyName = "narration",
                Name = "colNarration",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            grdLedger.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Debit",
                DataPropertyName = "debit",
                Name = "colDebit",
                DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight },
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            });

            grdLedger.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Credit",
                DataPropertyName = "credit",
                Name = "colCredit",
                DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight },
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            });

            grdLedger.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Type",
                DataPropertyName = "txn_type",
                Name = "colType",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            });

            grdLedger.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "RunningBalance",
                DataPropertyName = "RunningBalance",
                Name = "colRunning",
                DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight },
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            });

            _gridColsReady = true;
        }

        // ---------------------- Utils ----------------------

        private static void BindCombo(ComboBox cb, DataTable dt, string display, string value)
        {
            if (cb == null) return;
            cb.DataSource = dt;
            cb.DisplayMember = display;
            cb.ValueMember = value;
            if (cb.Items.Count > 0) cb.SelectedIndex = 0;
        }

        private static int? GetId(ComboBox cb)
        {
            if (cb == null || cb.SelectedValue == null || cb.SelectedValue == DBNull.Value) return null;
            int v; return int.TryParse(cb.SelectedValue.ToString(), out v) ? v : (int?)null;
        }

        private static double ToDouble(object o)
        {
            if (o == null || o == DBNull.Value) return 0.0;
            try { if (o is IConvertible) return Convert.ToDouble(o, CultureInfo.InvariantCulture); } catch { }
            double v; return double.TryParse(Convert.ToString(o), NumberStyles.Any, CultureInfo.InvariantCulture, out v) ? v : 0.0;
        }

        private static bool TableExists(string table)
        {
            try
            {
                const string q = "SELECT name FROM sqlite_master WHERE type='table' AND name=@t LIMIT 1";
                var ht = new Hashtable { { "@t", table } };
                var o = MainClass.SQLScalar(q, ht);
                return o != null && o != DBNull.Value;
            }
            catch { return false; }
        }

        private static bool ColumnExists(string table, string column)
        {
            try
            {
                var ti = MainClass.ExecuteSelectQuery("PRAGMA table_info(" + table + ")", null);
                if (ti == null) return false;
                foreach (DataRow r in ti.Rows)
                    if (string.Equals(Convert.ToString(r["name"]), column, StringComparison.OrdinalIgnoreCase))
                        return true;
                return false;
            }
            catch { return false; }
        }
    }
}
