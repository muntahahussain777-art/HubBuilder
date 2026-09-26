using HubBuilders.ProjectConnection;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace HubBuilders.Report_Form
{
    /// <summary>
    /// Owner-friendly plot status: Available / Installment / Reserve / Sold.
    /// Landscape A4 dense PDF. Additive — existing reports unchanged.
    /// </summary>
    public class frmPlotStatusOwnerReport : Sample
    {
        private ComboBox cmbSociety;
        private ComboBox cmbStatus;
        private TextBox txtWhatsApp;
        private Guna.UI2.WinForms.Guna2Button btnShow;
        private Guna.UI2.WinForms.Guna2Button btnPdf;
        private Guna.UI2.WinForms.Guna2Button btnPrint;
        private Guna.UI2.WinForms.Guna2Button btnWhatsApp;
        private DataGridView dgv;
        private Label lblSummary;
        private List<Row> _allRows = new List<Row>();
        private List<Row> _rows = new List<Row>();
        private Counts _counts;
        private string _societyName = "(All)";
        private bool _busy;

        private struct Row
        {
            public string Kind, Society, Block, PlotType, PlotNo, Customer;
        }

        private struct Counts
        {
            public int Available, Installment, Reserve, Sold, Total;
        }

        public frmPlotStatusOwnerReport()
        {
            InitializeUi();
            LoadSocieties();
        }

        private void InitializeUi()
        {
            Text = "Plot Status Report (Owner)";
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
                Text = "Plot Status Report (Owner)"
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
                Height = 118,
                Padding = new Padding(12),
                BackColor = Color.FromArgb(32, 36, 61)
            };

            cmbSociety = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.Black,
                BackColor = Color.White,
                Location = new Point(12, 28),
                Size = new Size(200, 28)
            };
            cmbStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.Black,
                BackColor = Color.White,
                Location = new Point(220, 28),
                Size = new Size(150, 28)
            };
            cmbStatus.Items.AddRange(new object[] {
                "(All Status)", "Available", "Installment", "Reserve", "Sold"
            });
            cmbStatus.SelectedIndex = 0;
            cmbStatus.SelectedIndexChanged += (s, e) =>
            {
                if (_allRows.Count == 0) return;
                ApplyStatusFilter();
                BindGrid();
                lblSummary.Text =
                    "Available " + _counts.Available +
                    "   |   Installment " + _counts.Installment +
                    "   |   Reserve " + _counts.Reserve +
                    "   |   Sold " + _counts.Sold +
                    "   |   Showing " + _rows.Count + " / " + _counts.Total;
            };

            btnShow = MakeBtn("Show Report", 380, 26, Color.FromArgb(184, 148, 68));
            btnPdf = MakeBtn("Save PDF", 548, 26, Color.FromArgb(0, 150, 136));
            btnPrint = MakeBtn("Print", 716, 26, Color.FromArgb(10, 40, 100));
            btnShow.Click += (s, e) => LoadData();
            btnPdf.Click += (s, e) => SavePdf();
            btnPrint.Click += (s, e) => PrintNow();

            txtWhatsApp = new TextBox
            {
                Font = new Font("Segoe UI", 10F),
                Location = new Point(12, 74),
                Size = new Size(240, 28)
            };
            btnWhatsApp = MakeBtn("WhatsApp PDF", 268, 72, Color.FromArgb(37, 211, 102));
            btnWhatsApp.Click += (s, e) => SendWhatsApp();

            lblSummary = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 230, 245),
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Location = new Point(468, 78),
                Text = "Show dabayein — Available / Installment / Reserve / Sold"
            };

            panelFilters.Controls.Add(Lbl("Society", 12, 8));
            panelFilters.Controls.Add(cmbSociety);
            panelFilters.Controls.Add(Lbl("Status", 220, 8));
            panelFilters.Controls.Add(cmbStatus);
            panelFilters.Controls.Add(btnShow);
            panelFilters.Controls.Add(btnPdf);
            panelFilters.Controls.Add(btnPrint);
            panelFilters.Controls.Add(Lbl("Owner WhatsApp number", 12, 56));
            panelFilters.Controls.Add(txtWhatsApp);
            panelFilters.Controls.Add(btnWhatsApp);
            panelFilters.Controls.Add(lblSummary);

            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                EnableHeadersVisualStyles = false,
                GridColor = Color.Silver,
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular)
            };
            dgv.DefaultCellStyle.ForeColor = Color.Black;
            dgv.DefaultCellStyle.BackColor = Color.White;
            dgv.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(210, 228, 255);
            dgv.AlternatingRowsDefaultCellStyle.ForeColor = Color.Black;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 247, 250);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(235, 238, 242);
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);

            Controls.Add(dgv);
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

        private static Guna.UI2.WinForms.Guna2Button MakeBtn(string text, int x, int y, Color fill)
        {
            return new Guna.UI2.WinForms.Guna2Button
            {
                Text = text,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                FillColor = fill,
                ForeColor = Color.White,
                BorderRadius = 8,
                Location = new Point(x, y),
                Size = new Size(170, 36)
            };
        }

        private void LoadSocieties()
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
            cmbSociety.SelectedIndex = 0;
        }

        private int SocietyId()
        {
            var s = cmbSociety.SelectedItem as IdName;
            return s == null ? 0 : s.Id;
        }

        private void LoadData()
        {
            if (_busy) return;
            _busy = true;
            try
            {
                int sid = SocietyId();
                var soc = cmbSociety.SelectedItem as IdName;
                _societyName = soc == null || soc.Id <= 0 ? "(All Societies)" : soc.Name;
                try { MainClass.RepairAllocatedPlotStatuses(); } catch { }
                _allRows = FetchRows(sid);
                _counts = CountOf(_allRows);
                ApplyStatusFilter();
                BindGrid();
                int allocated = _counts.Installment + _counts.Reserve + _counts.Sold;
                lblSummary.Text =
                    "Allocate jaisi entries: " + allocated +
                    "  (Installment " + _counts.Installment +
                    " + Reserve " + _counts.Reserve +
                    " + Sold " + _counts.Sold + ")" +
                    "   |   Available " + _counts.Available +
                    "   |   Showing " + _rows.Count;
                if (_rows.Count == 0)
                    MessageBox.Show("Is filter mein plot nahi mila.", "Plot Status",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Plot Status", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { _busy = false; }
        }

        private string StatusFilter()
        {
            string s = Convert.ToString(cmbStatus.SelectedItem) ?? "";
            if (s == "Available" || s == "Installment" || s == "Reserve" || s == "Sold")
                return s;
            return "";
        }

        private void ApplyStatusFilter()
        {
            string kind = StatusFilter();
            if (string.IsNullOrEmpty(kind))
                _rows = new List<Row>(_allRows);
            else
                _rows = _allRows.Where(r => r.Kind == kind).ToList();
        }

        private void BindGrid()
        {
            dgv.Columns.Clear();
            dgv.Rows.Clear();
            dgv.Columns.Add("sn", "#");
            dgv.Columns.Add("kind", "Status");
            dgv.Columns.Add("society", "Society");
            dgv.Columns.Add("block", "Block");
            dgv.Columns.Add("plot", "Plot No");
            dgv.Columns.Add("type", "Type");
            dgv.Columns.Add("cust", "Customer");
            dgv.Columns[0].FillWeight = 8;
            int n = 0;
            foreach (var r in _rows)
            {
                n++;
                int i = dgv.Rows.Add(n, r.Kind, r.Society, r.Block, r.PlotNo, r.PlotType, r.Customer);
                dgv.Rows[i].DefaultCellStyle.ForeColor = Color.Black;
                dgv.Rows[i].DefaultCellStyle.SelectionForeColor = Color.Black;
            }
            dgv.DefaultCellStyle.ForeColor = Color.Black;
            dgv.DefaultCellStyle.SelectionForeColor = Color.Black;
        }

        private static Counts CountOf(List<Row> rows)
        {
            var c = new Counts { Total = rows.Count };
            foreach (var r in rows)
            {
                if (r.Kind == "Available") c.Available++;
                else if (r.Kind == "Installment") c.Installment++;
                else if (r.Kind == "Reserve") c.Reserve++;
                else if (r.Kind == "Sold") c.Sold++;
            }
            return c;
        }

        private static List<Row> FetchRows(int societyId)
        {
            var list = new List<Row>();
            using (var con = new SQLiteConnection(projectconnection.conReturn()))
            {
                con.Open();
                using (var cmd = new SQLiteCommand(@"
SELECT 'Available' AS kind, s.name AS society, b.name AS block,
       IFNULL(pt.code,'') AS plot_type, p.plot_no AS plot_no, '' AS customer
FROM plots p
JOIN societies s ON s.id = p.society_id
JOIN blocks b ON b.id = p.block_id
JOIN plot_types pt ON pt.id = p.plot_type_id
WHERE p.status = 'Available'
  AND (@sid = 0 OR p.society_id = @sid)
  AND NOT EXISTS (
    SELECT 1 FROM customer_plots cp0
    WHERE cp0.plot_id = p.id
      AND IFNULL(cp0.status,'') <> 'Cancelled'
  )

UNION ALL

SELECT
  CASE
    WHEN LOWER(TRIM(IFNULL(cp.deal_type,''))) = 'sold' THEN 'Sold'
    WHEN LOWER(TRIM(IFNULL(cp.deal_type,''))) = 'reserve' THEN 'Reserve'
    ELSE 'Installment'
  END AS kind,
  s.name AS society, b.name AS block,
  IFNULL(pt.code,'') AS plot_type, p.plot_no AS plot_no,
  IFNULL(c.name,'') AS customer
FROM customer_plots cp
JOIN plots p ON p.id = cp.plot_id
JOIN customers c ON c.id = cp.customer_id
JOIN societies s ON s.id = p.society_id
JOIN blocks b ON b.id = p.block_id
JOIN plot_types pt ON pt.id = p.plot_type_id
WHERE IFNULL(cp.status,'') <> 'Cancelled'
  AND (@sid = 0 OR p.society_id = @sid)
;", con))
                {
                    cmd.Parameters.AddWithValue("@sid", societyId);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new Row
                            {
                                Kind = Convert.ToString(r["kind"]) ?? "",
                                Society = Convert.ToString(r["society"]) ?? "",
                                Block = Convert.ToString(r["block"]) ?? "",
                                PlotType = Convert.ToString(r["plot_type"]) ?? "",
                                PlotNo = Convert.ToString(r["plot_no"]) ?? "",
                                Customer = Convert.ToString(r["customer"]) ?? ""
                            });
                        }
                    }
                }
            }

            int Rank(string k)
            {
                if (k == "Available") return 0;
                if (k == "Installment") return 1;
                if (k == "Reserve") return 2;
                return 3;
            }
            list.Sort((a, b) =>
            {
                int c = Rank(a.Kind).CompareTo(Rank(b.Kind));
                if (c != 0) return c;
                c = string.Compare(a.Society, b.Society, StringComparison.OrdinalIgnoreCase);
                if (c != 0) return c;
                c = string.Compare(a.Block, b.Block, StringComparison.OrdinalIgnoreCase);
                if (c != 0) return c;
                return string.Compare(a.PlotNo, b.PlotNo, StringComparison.OrdinalIgnoreCase);
            });
            return list;
        }

        private bool EnsureData()
        {
            if (_allRows.Count == 0) LoadData();
            else ApplyStatusFilter();
            return _rows.Count > 0;
        }

        private void SavePdf()
        {
            if (!EnsureData()) return;
            try
            {
                using (var dlg = new SaveFileDialog())
                {
                    dlg.Title = "Save PDF";
                    dlg.Filter = "PDF (*.pdf)|*.pdf";
                    dlg.FileName = "HubBuilders_PlotStatus_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf";
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

        private void PrintNow()
        {
            if (!EnsureData()) return;
            try
            {
                var pages = BuildBitmaps();
                int i = 0;
                using (var doc = new PrintDocument())
                {
                    doc.DefaultPageSettings.Landscape = true;
                    try
                    {
                        doc.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
                    }
                    catch { }
                    doc.PrintPage += (s, e) =>
                    {
                        var img = pages[i];
                        e.Graphics.DrawImage(img, e.MarginBounds);
                        i++;
                        e.HasMorePages = i < pages.Count;
                    };
                    using (var dlg = new PrintPreviewDialog { Document = doc, Width = 1100, Height = 800 })
                        dlg.ShowDialog();
                }
                foreach (var b in pages) b.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Print", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SendWhatsApp()
        {
            if (!EnsureData()) return;
            string digits = NormalizePhone(txtWhatsApp.Text);
            if (string.IsNullOrWhiteSpace(digits))
            {
                MessageBox.Show("Owner ka WhatsApp number likhein (03xx...).", "WhatsApp",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtWhatsApp.Focus();
                return;
            }
            try
            {
                string dir = Path.Combine(Path.GetTempPath(), "HubBuilders");
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, "HubBuilders_PlotStatus_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf");
                File.WriteAllBytes(path, BuildPdfBytes());
                string msg =
                    "HUB BUILDERS — Plot Status Report\n" +
                    "Society: " + _societyName + "\n" +
                    "Available: " + _counts.Available +
                    " | Installment: " + _counts.Installment +
                    " | Reserve: " + _counts.Reserve +
                    " | Sold: " + _counts.Sold + "\n" +
                    "PDF attach karein (paperclip).";
                OpenWhatsApp(digits, msg);
                try { Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true }); } catch { }
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = "/select,\"" + path + "\"",
                        UseShellExecute = true
                    });
                }
                catch { }
                MessageBox.Show(
                    "WhatsApp open ho gaya.\nPDF ready hai — WhatsApp mein paperclip se attach kar dein:\n\n" + path,
                    "WhatsApp + PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "WhatsApp", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string NormalizePhone(string input)
        {
            var digits = new string((input ?? "").Where(char.IsDigit).ToArray());
            if (digits.StartsWith("03") && digits.Length == 11)
                digits = "92" + digits.Substring(1);
            return digits;
        }

        private static void OpenWhatsApp(string digits, string message)
        {
            string encoded = Uri.EscapeDataString(message ?? "");
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "whatsapp://send?phone=" + digits + "&text=" + encoded,
                    UseShellExecute = true
                });
            }
            catch
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://wa.me/" + digits + "?text=" + encoded,
                    UseShellExecute = true
                });
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
            int top = 92;
            int bottom = h - 28;
            int rowsPerPage = Math.Max(1, (bottom - top) / rowH);
            int pageCount = Math.Max(1, (int)Math.Ceiling(_rows.Count / (double)rowsPerPage));
            if (_rows.Count == 0) pageCount = 1;

            using (var titleFont = new Font("Segoe UI", 18F, FontStyle.Bold))
            using (var subFont = new Font("Segoe UI", 10F, FontStyle.Bold))
            using (var cellFont = new Font("Segoe UI", 9.5F, FontStyle.Regular))
            using (var headFont = new Font("Segoe UI", 9.5F, FontStyle.Bold))
            {
                for (int p = 0; p < pageCount; p++)
                {
                    var bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.White);
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                        g.DrawString("HUB BUILDERS", titleFont, Brushes.Black, 18, 10);
                        g.DrawString("Plot Status Report  —  " + _societyName +
                            (string.IsNullOrEmpty(StatusFilter()) ? "  —  All Status" : "  —  " + StatusFilter() + " only"), subFont, Brushes.Black, 18, 42);
                        g.DrawString(DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt") +
                            "    Page " + (p + 1) + " / " + pageCount, subFont, Brushes.Black, 1180, 16);
                        g.DrawString(
                            "Available: " + _counts.Available +
                            "     Installment: " + _counts.Installment +
                            "     Reserve: " + _counts.Reserve +
                            "     Sold: " + _counts.Sold +
                            "     Total plots: " + _counts.Total,
                            subFont, Brushes.Black, 18, 64);

                        int[] cols = { 18, 56, 168, 520, 720, 860, 1040 };
                        string[] heads = { "#", "Status", "Society", "Block", "Plot No", "Type", "Customer" };
                        using (var headBrush = new SolidBrush(Color.FromArgb(32, 36, 61)))
                            g.FillRectangle(headBrush, 18, top - 22, w - 36, 22);
                        for (int c = 0; c < heads.Length; c++)
                            g.DrawString(heads[c], headFont, Brushes.White, cols[c], top - 20);

                        int start = p * rowsPerPage;
                        int end = Math.Min(_rows.Count, start + rowsPerPage);
                        int y = top;
                        for (int i = start; i < end; i++)
                        {
                            var r = _rows[i];
                            if ((i - start) % 2 == 1)
                            {
                                using (var alt = new SolidBrush(Color.FromArgb(245, 247, 250)))
                                    g.FillRectangle(alt, 18, y, w - 36, rowH);
                            }
                            string[] vals =
                            {
                                (i + 1).ToString(), r.Kind, r.Society, r.Block, r.PlotNo, r.PlotType, r.Customer
                            };
                            for (int c = 0; c < vals.Length; c++)
                                g.DrawString(vals[c] ?? "", cellFont, Brushes.Black, cols[c], y + 3);
                            y += rowH;
                        }
                        g.DrawRectangle(Pens.Gray, 18, top - 22, w - 36, y - (top - 22));
                    }
                    pages.Add(bmp);
                }
            }
            return pages;
        }

        private class IdName
        {
            public int Id;
            public string Name;
            public IdName(int id, string name) { Id = id; Name = name; }
            public override string ToString() { return Name; }
        }

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
