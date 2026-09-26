using HubBuilders.ProjectConnection;
using Microsoft.Reporting.WinForms;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace HubBuilders.Report_Form
{
    /// <summary>
    /// Lasbela Village Receipt (رسید) — new report only.
    /// Name suggest → auto plot/block/size/father/baqaya. Receipt no = same as Receipt Form.
    /// Does not redesign other reports/forms.
    /// </summary>
    public partial class frmLasbelaVillageReceipt : Sample
    {
        private sealed class AllocSuggest
        {
            public int CustomerPlotId;
            public int CustomerId;
            public string Display;
            public string Name;
            public string Father;
            public string PlotNo;
            public string Size;
            public string Block;
            public string Category;
            public double Remaining;
            public double MonthlyInstallment;
            public override string ToString() { return Display; }
        }

        private ListBox _nameSuggest;
        private bool _busy;
        private double _remainingDue;
        private List<AllocSuggest> _allocCache = new List<AllocSuggest>();
        private int? _selectedCpId;
        private int? _selectedCustomerId;
        private string _selectedName = "";
        private long? _savedReceiptId;
        private long? _savedLedgerId;

        public frmLasbelaVillageReceipt()
        {
            InitializeComponent();
            BuildFields();
            WireSuggestAndAuto();
            WireLasbelaSavePdf();
            LayoutLasbelaButtons();
        }

        private void WireLasbelaSavePdf()
        {
            if (panelButtons == null || btnPreview == null || reportViewer1 == null) return;
            if (panelButtons.Controls["btnSavePdf"] != null) return;

            var btnSave = new Guna.UI2.WinForms.Guna2Button();
            btnSave.Name = "btnSavePdf";
            btnSave.Text = "Save PDF";
            btnSave.BorderRadius = btnPreview.BorderRadius;
            btnSave.FillColor = Color.FromArgb(0, 150, 136);
            btnSave.Font = btnPreview.Font;
            btnSave.ForeColor = Color.White;
            btnSave.Size = new Size(380, 42);
            btnSave.Location = new Point(10, 56);
            btnSave.Click += btnSavePdf_Click;
            panelButtons.Controls.Add(btnSave);
        }

        private void LayoutLasbelaButtons()
        {
            try
            {
                panelButtons.Height = 110;
                btnPreview.Location = new Point(10, 8);
                btnPreview.Size = new Size(180, 42);
                btnClear.Location = new Point(210, 8);
                btnClear.Size = new Size(180, 42);
                btnClear.BringToFront();

                var btnSave = panelButtons.Controls["btnSavePdf"] as Guna.UI2.WinForms.Guna2Button;
                if (btnSave != null)
                {
                    btnSave.Location = new Point(10, 56);
                    btnSave.Size = new Size(380, 42);
                    btnSave.BringToFront();
                }
            }
            catch { }
        }

        private void frmLasbelaVillageReceipt_Load(object sender, EventArgs e)
        {
            try { MainClass.EnsureAllocationSchema(); } catch { }
            if (string.IsNullOrWhiteSpace(txtReceiptDate.Text))
                txtReceiptDate.Text = DateTime.Now.ToString("dd-MM-yyyy");
            if (string.IsNullOrWhiteSpace(txtPaymentDate.Text))
                txtPaymentDate.Text = DateTime.Now.ToString("dd-MM-yyyy");
            SuggestNextReceiptNo();
            LoadAllocCache();
        }

        private void BuildFields()
        {
            int y = 4;
            const int labelW = 360;
            const int boxW = 360;
            const int boxH = 28;
            const int gap = 6;

            void AddLabel(string text)
            {
                var lbl = new Label
                {
                    Text = text,
                    AutoSize = false,
                    Width = labelW,
                    Height = 22,
                    Left = 4,
                    Top = y,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold)
                };
                panelFields.Controls.Add(lbl);
                y += 22;
            }

            TextBox AddBox(string name)
            {
                var tb = new TextBox
                {
                    Name = name,
                    Width = boxW,
                    Height = boxH,
                    Left = 4,
                    Top = y,
                    Font = new Font("Segoe UI", 10f)
                };
                panelFields.Controls.Add(tb);
                y += boxH + gap;
                return tb;
            }

            AddLabel("Receipt No / رسید نمبر");
            txtReceiptNo = AddBox("txtReceiptNo");
            AddLabel("Date / تاریخ");
            txtReceiptDate = AddBox("txtReceiptDate");
            AddLabel("Name / محترم محترمہ مسماۃ (type to search)");
            txtPersonName = AddBox("txtPersonName");
            AddLabel("Father-Husband / والد شوہر");
            txtFatherOrHusband = AddBox("txtFatherOrHusband");
            AddLabel("Received Amount / رقم");
            txtReceivedAmount = AddBox("txtReceivedAmount");
            AddLabel("Amount in Words / رقم الفاظ میں");
            txtAmountInWords = AddBox("txtAmountInWords");
            AddLabel("Payment Mode / کیش چیک ڈرافٹ");
            txtPaymentMode = new ComboBox
            {
                Name = "txtPaymentMode",
                Width = boxW,
                Height = boxH,
                Left = 4,
                Top = y,
                Font = new Font("Segoe UI", 10f),
                DropDownStyle = ComboBoxStyle.DropDown, // list + free type/edit
                FlatStyle = FlatStyle.Standard
            };
            txtPaymentMode.Items.AddRange(new object[]
            {
                "Cash",
                "Cross Cheque",
                "Bank Draft",
                "Pay Order"
            });
            txtPaymentMode.SelectedIndex = 0;
            panelFields.Controls.Add(txtPaymentMode);
            y += boxH + gap;
            AddLabel("Payment Date / بتاریخ");
            txtPaymentDate = AddBox("txtPaymentDate");
            AddLabel("Plot No / پلاٹ نمبر");
            txtPlotNo = AddBox("txtPlotNo");
            AddLabel("Size / سائز");
            txtPlotSize = AddBox("txtPlotSize");
            AddLabel("Block / بلاک");
            txtBlock = AddBox("txtBlock");
            AddLabel("Category / کیٹیگری");
            txtCategory = AddBox("txtCategory");
            AddLabel("Current Amount / نیچے والی رقم");
            txtCurrentAmount = AddBox("txtCurrentAmount");
            AddLabel("Balance / بقایا رقم");
            txtBalanceAmount = AddBox("txtBalanceAmount");

            panelFields.Height = y + 20;

            try
            {
                txtCurrentAmount.ReadOnly = false;
                UpdateManualEntryMode();
            }
            catch { }
        }

        private void UpdateManualEntryMode()
        {
            bool hasCustomer = _selectedCpId.HasValue && _selectedCpId.Value > 0;
            try
            {
                txtFatherOrHusband.ReadOnly = hasCustomer;
                txtPlotNo.ReadOnly = hasCustomer;
                txtPlotSize.ReadOnly = hasCustomer;
                txtBlock.ReadOnly = hasCustomer;
                txtCategory.ReadOnly = hasCustomer;
                txtBalanceAmount.ReadOnly = hasCustomer;
            }
            catch { }
        }

        private void WireSuggestAndAuto()
        {
            _nameSuggest = new ListBox
            {
                Font = new Font("Segoe UI", 9.5f),
                BackColor = Color.FromArgb(37, 41, 74),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false,
                IntegralHeight = false,
                Height = 180,
                Width = Math.Max(360, txtPersonName.Width)
            };
            panelFields.Controls.Add(_nameSuggest);
            _nameSuggest.BringToFront();

            txtPersonName.TextChanged += (s, e) =>
            {
                if (_busy) return;
                if (_selectedCpId.HasValue)
                {
                    string now = (txtPersonName.Text ?? "").Trim();
                    if (!string.Equals(now, _selectedName, StringComparison.OrdinalIgnoreCase))
                    {
                        _selectedCpId = null;
                        _selectedCustomerId = null;
                        _savedReceiptId = null;
                        _savedLedgerId = null;
                        _remainingDue = 0;
                        UpdateManualEntryMode();
                    }
                }
                RefreshNameSuggest();
            };
            txtPersonName.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Down && _nameSuggest.Visible && _nameSuggest.Items.Count > 0)
                {
                    e.SuppressKeyPress = true;
                    _nameSuggest.Focus();
                    if (_nameSuggest.SelectedIndex < 0) _nameSuggest.SelectedIndex = 0;
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    ApplySelectedSuggest();
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    _nameSuggest.Visible = false;
                }
            };
            txtPersonName.Leave += (s, e) =>
            {
                BeginInvoke(new Action(() =>
                {
                    if (_nameSuggest.Focused) return;
                    _nameSuggest.Visible = false;
                }));
            };

            _nameSuggest.Click += (s, e) => ApplySelectedSuggest();
            _nameSuggest.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    ApplySelectedSuggest();
                }
            };

            txtReceivedAmount.TextChanged += (s, e) =>
            {
                if (_busy) return;
                RecalcBalanceFromReceived();
            };
        }

        private void LoadAllocCache()
        {
            _allocCache = new List<AllocSuggest>();
            try
            {
                const string sql = @"
SELECT
  cp.id AS customer_plot_id,
  cp.customer_id AS customer_id,
  COALESCE(c.name,'') AS customer_name,
  COALESCE(c.father_name,'') AS father_name,
  CAST(p.plot_no AS TEXT) AS plot_no,
  COALESCE(pt.code,'') AS plot_size,
  COALESCE(b.name,'') AS block_name,
  COALESCE(NULLIF(TRIM(cp.deal_type),''),'Installment') AS deal_type,
  COALESCE(cp.price,0) AS plot_price,
  COALESCE(cp.monthly_installment,0) AS monthly_installment,
  COALESCE((SELECT SUM(amount) FROM receipts r WHERE r.customer_plot_id = cp.id),0) AS total_paid
FROM customer_plots cp
JOIN customers c ON c.id = cp.customer_id
JOIN plots p ON p.id = cp.plot_id
LEFT JOIN blocks b ON b.id = p.block_id
LEFT JOIN plot_types pt ON pt.id = p.plot_type_id
WHERE cp.status = 'Active'
ORDER BY c.name COLLATE NOCASE, p.plot_no;";

                using (var con = new SQLiteConnection(projectconnection.conReturn()))
                using (var cmd = new SQLiteCommand(sql, con))
                {
                    con.Open();
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            string name = Convert.ToString(r["customer_name"] ?? "").Trim();
                            string father = Convert.ToString(r["father_name"] ?? "").Trim();
                            string plot = Convert.ToString(r["plot_no"] ?? "").Trim();
                            string size = Convert.ToString(r["plot_size"] ?? "").Trim();
                            string block = Convert.ToString(r["block_name"] ?? "").Trim();
                            string cat = Convert.ToString(r["deal_type"] ?? "Installment").Trim();
                            double price = 0, paid = 0;
                            try { price = Convert.ToDouble(r["plot_price"]); } catch { }
                            try { paid = Convert.ToDouble(r["total_paid"]); } catch { }
                            double rem = Math.Round(price - paid, 2);
                            if (rem < 0) rem = 0;
                            double monthly = 0;
                            try { monthly = Convert.ToDouble(r["monthly_installment"]); } catch { }
                            if (monthly < 0) monthly = 0;

                            string disp = name;
                            if (father.Length > 0) disp += " / " + father;
                            disp += " | Plot " + plot;
                            if (block.Length > 0) disp += " | " + block;
                            if (size.Length > 0) disp += " | " + size;

                            _allocCache.Add(new AllocSuggest
                            {
                                CustomerPlotId = Convert.ToInt32(r["customer_plot_id"]),
                                CustomerId = Convert.ToInt32(r["customer_id"]),
                                Display = disp,
                                Name = name,
                                Father = father,
                                PlotNo = plot,
                                Size = size,
                                Block = block,
                                Category = cat,
                                Remaining = rem,
                                MonthlyInstallment = monthly
                            });
                        }
                    }
                }
            }
            catch { }
        }

        private void RefreshNameSuggest()
        {
            if (txtPersonName == null || _nameSuggest == null) return;
            string q = (txtPersonName.Text ?? "").Trim();
            _nameSuggest.BeginUpdate();
            try
            {
                _nameSuggest.Items.Clear();
                foreach (var a in _allocCache)
                {
                    if (q.Length == 0
                        || a.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                        || a.Father.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                        || a.PlotNo.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                        || a.Display.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _nameSuggest.Items.Add(a);
                        if (_nameSuggest.Items.Count >= 80) break;
                    }
                }
            }
            finally { _nameSuggest.EndUpdate(); }

            _nameSuggest.Left = txtPersonName.Left;
            _nameSuggest.Top = txtPersonName.Bottom + 2;
            _nameSuggest.Width = Math.Max(360, txtPersonName.Width);
            if (_nameSuggest.Items.Count > 0 && txtPersonName.Focused)
            {
                if (_nameSuggest.SelectedIndex < 0) _nameSuggest.SelectedIndex = 0;
                _nameSuggest.Visible = true;
                _nameSuggest.BringToFront();
            }
            else
            {
                _nameSuggest.Visible = false;
            }
        }

        private void ApplySelectedSuggest()
        {
            AllocSuggest a = null;
            if (_nameSuggest != null && _nameSuggest.SelectedItem is AllocSuggest sel)
                a = sel;
            else if (_nameSuggest != null && _nameSuggest.Items.Count > 0)
                a = _nameSuggest.Items[0] as AllocSuggest;
            if (a == null) return;

            _busy = true;
            try
            {
                txtPersonName.Text = a.Name;
                txtFatherOrHusband.Text = a.Father;
                txtPlotNo.Text = a.PlotNo;
                txtPlotSize.Text = a.Size;
                txtBlock.Text = a.Block;
                txtCategory.Text = a.Category;
                _remainingDue = a.Remaining;
                _selectedCpId = a.CustomerPlotId;
                _selectedCustomerId = a.CustomerId;
                _selectedName = a.Name ?? "";
                _savedReceiptId = null;
                _savedLedgerId = null;
                if (a.MonthlyInstallment > 0)
                    txtReceivedAmount.Text = FormatMoney(a.MonthlyInstallment);
                else
                {
                    txtReceivedAmount.Clear();
                    txtBalanceAmount.Text = FormatMoney(_remainingDue);
                }
                txtCurrentAmount.Clear();
                RecalcBalanceFromReceived();
                UpdateManualEntryMode();
                _nameSuggest.Visible = false;
                try { txtReceivedAmount.Focus(); } catch { }
            }
            finally { _busy = false; }
        }

        private void RecalcBalanceFromReceived()
        {
            if (_busy || !_selectedCpId.HasValue) return;
            _busy = true;
            try
            {
                string raw = (txtReceivedAmount.Text ?? "").Trim().Replace(",", "");
                double paidNow = 0;
                double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out paidNow);
                if (paidNow < 0) paidNow = 0;

                double bal = Math.Round(_remainingDue - paidNow, 2);
                if (bal < 0) bal = 0;
                txtBalanceAmount.Text = FormatMoney(bal);
            }
            finally { _busy = false; }
        }

        private static string FormatMoney(double v)
        {
            return v.ToString("N0", CultureInfo.InvariantCulture);
        }

        /// <summary>Same auto Ref No as Receipt Form: 001, 002… gap-fill.</summary>
        private void SuggestNextReceiptNo()
        {
            try
            {
                txtReceiptNo.Text = ComputeNextRefNo();
            }
            catch
            {
                try { txtReceiptNo.Text = "001"; } catch { }
            }
        }

        private static string ComputeNextRefNo()
        {
            var used = new HashSet<int>();
            try
            {
                using (var con = new SQLiteConnection(projectconnection.conReturn()))
                using (var cmd = new SQLiteCommand(
                    "SELECT ref_no FROM receipts WHERE ref_no IS NOT NULL AND TRIM(ref_no) <> '';", con))
                {
                    con.Open();
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            int? n = ParseNumericRef(Convert.ToString(r["ref_no"]));
                            if (n.HasValue && n.Value > 0) used.Add(n.Value);
                        }
                    }
                }
            }
            catch { }
            int next = 1;
            while (used.Contains(next)) next++;
            return next.ToString("D3");
        }

        private static int? ParseNumericRef(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            string t = raw.Trim();
            foreach (char c in t)
                if (!char.IsDigit(c)) return null;
            int v;
            if (!int.TryParse(t, out v)) return null;
            return v;
        }

        private bool SaveAmountToReceiptForm()
        {
            if (!_selectedCpId.HasValue || _selectedCpId.Value <= 0)
                return true;

            string dateText = (txtPaymentDate.Text ?? "").Trim();
            if (dateText.Length == 0) dateText = txtReceiptDate.Text;
            string notes = "Lasbela Village Receipt";
            string words = (txtAmountInWords.Text ?? "").Trim();
            if (words.Length > 0) notes = notes + " / " + words;
            string err;
            if (!LasbelaReceiptToCustomerReceipt.TrySave(
                _selectedCpId,
                txtReceivedAmount.Text,
                dateText,
                txtPaymentMode != null ? txtPaymentMode.Text : "",
                txtReceiptNo.Text,
                notes,
                ref _savedReceiptId,
                ref _savedLedgerId,
                out err))
            {
                MessageBox.Show(err, "Lasbela Village Receipt",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            try { LoadAllocCache(); } catch { }
            return true;
        }

        private void btnPreview_Click(object sender, EventArgs e)
        {
            try
            {
                if (!SaveAmountToReceiptForm()) return;

                string reportsDir = Path.Combine(Application.StartupPath, "Reports");
                string templatePath = Path.Combine(reportsDir, "LasbelaVillageReceipt.png");
                string reportPath = Path.Combine(reportsDir, "LasbelaVillageReceipt.rdlc");

                if (!File.Exists(templatePath))
                {
                    MessageBox.Show("Receipt template image not found in Reports folder.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (!File.Exists(reportPath))
                {
                    MessageBox.Show("Report file not found!", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                byte[] imageBytes = ComposeDualPortraitA4(templatePath);

                var dt = new DataTable();
                dt.Columns.Add("ReceiptImage", typeof(byte[]));
                dt.Rows.Add(imageBytes);

                reportViewer1.Reset();
                reportViewer1.LocalReport.ReportPath = reportPath;
                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(new ReportDataSource("DataSet1", dt));
                reportViewer1.RefreshReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Lasbela Village Receipt",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            foreach (Control c in panelFields.Controls)
            {
                if (c is TextBox tb) tb.Clear();
                else if (c is ComboBox cb)
                {
                    cb.SelectedIndex = -1;
                    cb.Text = "";
                }
            }
            _remainingDue = 0;
            _selectedCpId = null;
            _selectedCustomerId = null;
            _selectedName = "";
            _savedReceiptId = null;
            _savedLedgerId = null;
            txtReceiptDate.Text = DateTime.Now.ToString("dd-MM-yyyy");
            txtPaymentDate.Text = DateTime.Now.ToString("dd-MM-yyyy");
            try
            {
                if (txtPaymentMode != null && txtPaymentMode.Items.Count > 0)
                    txtPaymentMode.SelectedIndex = 0;
            }
            catch { }
            SuggestNextReceiptNo();
            UpdateManualEntryMode();
            if (_nameSuggest != null) _nameSuggest.Visible = false;
            reportViewer1.Clear();
        }

        private void btnSavePdf_Click(object sender, EventArgs e)
        {
            if (_busy) return;
            _busy = true;
            try
            {
                Cursor = Cursors.WaitCursor;
                btnPreview_Click(this, EventArgs.Empty);

                string mime, enc, ext;
                Warning[] warnings;
                string[] streams;
                byte[] bytes = reportViewer1.LocalReport.Render(
                    "PDF", null, out mime, out enc, out ext, out streams, out warnings);
                if (bytes == null || bytes.Length == 0)
                {
                    MessageBox.Show("PDF ban nahi saki. Pehle Print Preview karein.", "Save PDF",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string safeName = MakeFileSafe((txtPersonName.Text ?? "").Trim());
                string pdfPath = HubBuilders.HubWhatsAppPdfHelper.SavePdfForWhatsApp(bytes, "LasbelaVillageReceipt_" + safeName);

                string folder = HubBuilders.HubAutoBackup.TodayFolder();
                if (!string.IsNullOrWhiteSpace(folder))
                {
                    try
                    {
                        Directory.CreateDirectory(folder);
                        File.WriteAllBytes(Path.Combine(folder, Path.GetFileName(pdfPath)), bytes);
                    }
                    catch { }
                }

                if (!_selectedCustomerId.HasValue || _selectedCustomerId.Value <= 0)
                {
                    MessageBox.Show("PDF save ho gayi:\n" + pdfPath, "Save PDF",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string mobile = LoadCustomerMobile(_selectedCustomerId.Value);
                string digits = HubBuilders.HubWhatsAppPdfHelper.NormalizePhoneToDigits(mobile);
                if (string.IsNullOrWhiteSpace(digits))
                {
                    MessageBox.Show("PDF save ho gayi lekin customer ka mobile nahi mila:\n" + pdfPath,
                        "Save PDF", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string msg = BuildWhatsAppMessage();
                bool attached = false;
                var waThread = new Thread(() =>
                {
                    attached = HubBuilders.HubWhatsAppPdfHelper.SendPdfToWhatsApp(pdfPath, digits, msg);
                });
                waThread.SetApartmentState(ApartmentState.STA);
                waThread.IsBackground = true;
                waThread.Start();
                waThread.Join();

                if (attached)
                {
                    MessageBox.Show(
                        "PDF save ho gayi aur WhatsApp open — PDF attach ho gayi.\nSend dabakar bhej dein.\n\n" + pdfPath,
                        "Save PDF + WhatsApp", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    HubBuilders.HubWhatsAppPdfHelper.PrepareClipboardForManualPaste(pdfPath);
                    HubBuilders.HubWhatsAppPdfHelper.OpenExplorerWithPdf(pdfPath);
                    MessageBox.Show(
                        "WhatsApp open ho gaya.\nPDF clipboard pe hai — chat box mein Ctrl+V dabayein.\nYa Explorer se file WhatsApp par drag karein.\n\nWhatsApp Desktop app use karein (browser mein file attach nahi hoti).\n\n" + pdfPath,
                        "Save PDF + WhatsApp", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("PDF error: " + ex.Message, "Save PDF",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
                _busy = false;
            }
        }

        private string BuildWhatsAppMessage()
        {
            string name = (txtPersonName.Text ?? "").Trim();
            string amt = (txtReceivedAmount.Text ?? "").Trim();
            string refNo = (txtReceiptNo.Text ?? "").Trim();
            string plot = (txtPlotNo.Text ?? "").Trim();
            var lines = new List<string>();
            lines.Add("Assalam o Alaikum,");
            if (name.Length > 0) lines.Add("Customer: " + name);
            if (amt.Length > 0) lines.Add("Received Amount: " + amt);
            if (refNo.Length > 0) lines.Add("Receipt No: " + refNo);
            if (plot.Length > 0) lines.Add("Plot No: " + plot);
            lines.Add("Lasbela Village Receipt attached.");
            return string.Join("\n", lines);
        }

        private static string LoadCustomerMobile(int customerId)
        {
            try
            {
                using (var con = new SQLiteConnection(projectconnection.conReturn()))
                using (var cmd = new SQLiteCommand(
                    "SELECT COALESCE(mobile,'') FROM customers WHERE id=@id LIMIT 1;", con))
                {
                    cmd.Parameters.AddWithValue("@id", customerId);
                    con.Open();
                    return Convert.ToString(cmd.ExecuteScalar() ?? "");
                }
            }
            catch { return ""; }
        }

        private static string MakeFileSafe(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Receipt";
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            name = name.Trim();
            return name.Length > 0 ? name : "Receipt";
        }

        /// <summary>
        /// Same perfect Lasbela receipt (unchanged seats), printed twice on one A4 Portrait page.
        /// </summary>
        private byte[] ComposeDualPortraitA4(string templatePath)
        {
            using (Bitmap single = ComposeReceiptBitmap(templatePath))
            {
                // A4 Portrait @ 200 DPI
                const int pageW = 1654;
                const int pageH = 2339;
                using (var page = new Bitmap(pageW, pageH, PixelFormat.Format32bppArgb))
                using (var g = Graphics.FromImage(page))
                {
                    g.Clear(Color.White);
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    float half = pageH / 2f;
                    float marginX = 28f;
                    float marginY = 16f;
                    float maxW = pageW - 2f * marginX;
                    float maxH = half - 2f * marginY;

                    float scale = Math.Min(maxW / single.Width, maxH / single.Height);
                    float dw = single.Width * scale;
                    float dh = single.Height * scale;
                    float x = (pageW - dw) / 2f;

                    // Top copy
                    float yTop = marginY + (maxH - dh) / 2f;
                    g.DrawImage(single, x, yTop, dw, dh);

                    // Bottom copy (same receipt)
                    float yBot = half + marginY + (maxH - dh) / 2f;
                    g.DrawImage(single, x, yBot, dw, dh);

                    // Light cut guide between copies
                    using (var pen = new Pen(Color.FromArgb(170, 170, 180), 1.2f) { DashStyle = DashStyle.Dash })
                        g.DrawLine(pen, 40, half, pageW - 40, half);

                    return ToPngBytes(page);
                }
            }
        }

        private Bitmap ComposeReceiptBitmap(string templatePath)
        {
            using (var msIn = new MemoryStream(File.ReadAllBytes(templatePath)))
            using (var src = Image.FromStream(msIn))
            {
                var bmp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.DrawImage(src, 0, 0, src.Width, src.Height);
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                    float w = bmp.Width;
                    float h = bmp.Height;
                    float fs = Math.Max(18f, h * 0.028f) + 4f; // +4 font size; seats/positions unchanged
                    using (Font f = CreateFormFont(fs))
                    {
                        Color ink = Color.FromArgb(15, 20, 90);

                        // Normalized seats calibrated against LasbelaVillageReceipt.png underlines.
                        // Signature underline (bottom-left) intentionally has NO field.
                        DrawOnLine(g, txtReceiptDate.Text, 0.04f, 0.318f, 0.18f, w, h, f, ink, false, StringAlignment.Center);
                        DrawOnLine(g, txtReceiptNo.Text, 0.72f, 0.306f, 0.17f, w, h, f, ink, false, StringAlignment.Center);
                        // Name + Father: near Urdu labels (visual right / center-right of blank)
                        DrawOnLine(g, txtPersonName.Text, 0.34f, 0.395f, 0.24f, w, h, f, ink, true, StringAlignment.Center);
                        DrawOnLine(g, txtFatherOrHusband.Text, 0.44f, 0.466f, 0.22f, w, h, f, ink, true, StringAlignment.Center);
                        DrawOnLine(g, txtAmountInWords.Text, 0.20f, 0.541f, 0.32f, w, h, f, ink, true, StringAlignment.Far);
                        DrawOnLine(g, txtReceivedAmount.Text, 0.68f, 0.541f, 0.18f, w, h, f, ink, false, StringAlignment.Center);
                        DrawOnLine(g, txtPaymentDate.Text, 0.05f, 0.627f, 0.20f, w, h, f, ink, false, StringAlignment.Center);
                        DrawOnLine(g, txtPaymentMode.Text, 0.34f, 0.627f, 0.28f, w, h, f, ink, true, StringAlignment.Center);
                        // Category: slightly more left
                        DrawOnLine(g, txtCategory.Text, 0.06f, 0.708f, 0.14f, w, h, f, ink, false, StringAlignment.Center);
                        DrawOnLine(g, txtBlock.Text, 0.30f, 0.708f, 0.14f, w, h, f, ink, false, StringAlignment.Center);
                        DrawOnLine(g, txtPlotSize.Text, 0.50f, 0.708f, 0.14f, w, h, f, ink, false, StringAlignment.Center);
                        DrawOnLine(g, txtPlotNo.Text, 0.70f, 0.708f, 0.14f, w, h, f, ink, false, StringAlignment.Center);
                        DrawOnLine(g, txtBalanceAmount.Text, 0.40f, 0.800f, 0.18f, w, h, f, ink, false, StringAlignment.Center);
                        DrawOnLine(g, txtCurrentAmount.Text, 0.68f, 0.800f, 0.18f, w, h, f, ink, false, StringAlignment.Center);
                    }
                }
                return bmp;
            }
        }

        private static Font CreateFormFont(float size)
        {
            string[] preferred =
            {
                "Jameel Noori Nastaleeq",
                "Urdu Typesetting",
                "Microsoft Uighur",
                "Segoe UI Semibold",
                "Segoe UI",
                "Arabic Typesetting"
            };
            foreach (string name in preferred)
            {
                try { return new Font(name, size, FontStyle.Bold, GraphicsUnit.Pixel); }
                catch { }
            }
            return new Font(FontFamily.GenericSansSerif, size, FontStyle.Bold, GraphicsUnit.Pixel);
        }

        private static void DrawOnLine(
            Graphics g, string text,
            float xPct, float lineYPct, float wPct,
            float imgW, float imgH, Font font, Color color,
            bool rtl, StringAlignment align)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            string t = text.Trim();
            float lineY = lineYPct * imgH;
            float maxW = wPct * imgW;
            float boxH = Math.Max(font.GetHeight(g) * 1.15f, imgH * 0.032f);
            float top = lineY - boxH - 1f;
            if (top < 0f) top = 0f;
            var rect = new RectangleF(xPct * imgW, top, maxW, boxH);

            using (var brush = new SolidBrush(color))
            using (var sf = new StringFormat(StringFormat.GenericTypographic))
            {
                if (rtl)
                    sf.FormatFlags |= StringFormatFlags.DirectionRightToLeft;
                sf.Alignment = align;
                sf.LineAlignment = StringAlignment.Far;
                sf.Trimming = StringTrimming.None;
                sf.FormatFlags |= StringFormatFlags.NoWrap;

                Font drawFont = font;
                Font shrunk = null;
                try
                {
                    // Keep text inside field width — shrink if needed so nothing overflows
                    float size = font.Size;
                    float minSize = Math.Max(11f, font.Size - 6f);
                    SizeF measured = g.MeasureString(t, drawFont, int.MaxValue, sf);
                    while (measured.Width > maxW && size > minSize)
                    {
                        size -= 0.5f;
                        if (shrunk != null) shrunk.Dispose();
                        shrunk = new Font(font.FontFamily, size, font.Style, font.Unit);
                        drawFont = shrunk;
                        measured = g.MeasureString(t, drawFont, int.MaxValue, sf);
                    }
                    g.DrawString(t, drawFont, brush, rect, sf);
                }
                finally
                {
                    if (shrunk != null) shrunk.Dispose();
                }
            }
        }

        private static byte[] ToPngBytes(Bitmap bmp)
        {
            using (var ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
        }
    }
}
