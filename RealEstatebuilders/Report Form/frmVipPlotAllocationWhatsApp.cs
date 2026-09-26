using HubBuilders.ProjectConnection;
using System;
using System.Collections.Generic;
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
    /// Additive VIP: plot allocate confirmation WhatsApp.
    /// Search Active allocations OR type name/number manually. Does not change other forms.
    /// </summary>
    public class frmVipPlotAllocationWhatsApp : Sample
    {
        private TextBox txtCustomer;
        private ListBox lstSuggest;
        private TextBox txtNumber;
        private TextBox txtFather;
        private TextBox txtCnic;
        private TextBox txtSociety;
        private TextBox txtBlock;
        private TextBox txtPlotNo;
        private TextBox txtPlotType;
        private TextBox txtBooking;
        private TextBox txtDealType;
        private TextBox txtPlotPrice;
        private TextBox txtMessage;
        private Label lblHint;

        private List<AllocSuggest> _cache = new List<AllocSuggest>();
        private AllocSuggest _selected;
        private bool _filling;
        private bool _messageEdited;
        private bool _busy;

        public frmVipPlotAllocationWhatsApp()
        {
            InitializeUi();
            LoadAllocCache();
            FillDefaultMessage();
        }

        private void InitializeUi()
        {
            Text = "Plot Allocation Message (VIP)";
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
                Text = "Plot Allocation Message (VIP)"
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

            var panelBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                AutoScroll = true,
                BackColor = Color.FromArgb(32, 36, 61)
            };

            int y = 8;
            panelBody.Controls.Add(Lbl("Customer (allocate se search / ya manually likhein)", 12, y));
            y += 24;
            txtCustomer = Field(12, y, 560);
            txtCustomer.TextChanged += (s, e) =>
            {
                if (_filling) return;
                RefreshSuggest();
                if (!_messageEdited) FillDefaultMessage();
            };
            panelBody.Controls.Add(txtCustomer);
            y += 36;

            lstSuggest = new ListBox
            {
                Font = new Font("Segoe UI", 10F),
                Location = new Point(12, y),
                Size = new Size(720, 110),
                Visible = false
            };
            lstSuggest.Click += (s, e) => ApplySelectedSuggest();
            lstSuggest.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    ApplySelectedSuggest();
                }
            };
            panelBody.Controls.Add(lstSuggest);
            y += 8;

            int fieldsTop = y + 120;
            Action placeFields = () =>
            {
                // fields placed below suggest area
            };
            placeFields();

            int fy = fieldsTop;
            panelBody.Controls.Add(Lbl("WhatsApp number (03xx… — change kar sakte ho)", 12, fy));
            fy += 24;
            txtNumber = Field(12, fy, 360);
            panelBody.Controls.Add(txtNumber);
            fy += 40;

            panelBody.Controls.Add(Lbl("Father / Husband", 12, fy));
            panelBody.Controls.Add(Lbl("CNIC", 390, fy));
            fy += 24;
            txtFather = Field(12, fy, 360);
            txtCnic = Field(390, fy, 280);
            panelBody.Controls.Add(txtFather);
            panelBody.Controls.Add(txtCnic);
            fy += 40;

            panelBody.Controls.Add(Lbl("Society", 12, fy));
            panelBody.Controls.Add(Lbl("Block", 390, fy));
            fy += 24;
            txtSociety = Field(12, fy, 360);
            txtBlock = Field(390, fy, 280);
            panelBody.Controls.Add(txtSociety);
            panelBody.Controls.Add(txtBlock);
            fy += 40;

            panelBody.Controls.Add(Lbl("Plot No", 12, fy));
            panelBody.Controls.Add(Lbl("Plot Type / Size", 390, fy));
            fy += 24;
            txtPlotNo = Field(12, fy, 360);
            txtPlotType = Field(390, fy, 280);
            panelBody.Controls.Add(txtPlotNo);
            panelBody.Controls.Add(txtPlotType);
            fy += 40;

            panelBody.Controls.Add(Lbl("Booking Date", 12, fy));
            panelBody.Controls.Add(Lbl("Deal Type", 390, fy));
            fy += 24;
            txtBooking = Field(12, fy, 360);
            txtDealType = Field(390, fy, 280);
            panelBody.Controls.Add(txtBooking);
            panelBody.Controls.Add(txtDealType);
            fy += 40;

            panelBody.Controls.Add(Lbl("Allocate / Plot Amount", 12, fy));
            fy += 24;
            txtPlotPrice = Field(12, fy, 360);
            txtPlotPrice.TextChanged += (s, e) =>
            {
                if (_filling) return;
                if (!_messageEdited) FillDefaultMessage();
            };
            panelBody.Controls.Add(txtPlotPrice);
            fy += 44;

            foreach (var tb in new[] { txtFather, txtCnic, txtSociety, txtBlock, txtPlotNo, txtPlotType, txtBooking, txtDealType })
            {
                tb.TextChanged += (s, e) =>
                {
                    if (_filling) return;
                    if (!_messageEdited) FillDefaultMessage();
                };
            }

            panelBody.Controls.Add(Lbl("VIP WhatsApp message (edit kar sakte ho)", 12, fy));
            fy += 24;
            txtMessage = new TextBox
            {
                Font = new Font("Segoe UI", 11F),
                Location = new Point(12, fy),
                Size = new Size(720, 260),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                AcceptsReturn = true
            };
            txtMessage.TextChanged += (s, e) =>
            {
                if (_filling) return;
                _messageEdited = true;
            };
            panelBody.Controls.Add(txtMessage);
            fy += 276;

            lblHint = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(200, 210, 230),
                Location = new Point(12, fy),
                Text = "Allocate customer search · ya name + number manually · receipts (credit) message mein · WhatsApp"
            };
            panelBody.Controls.Add(lblHint);
            fy += 32;

            var btnLoad = MakeBtn("Load / Refresh Message", 12, fy, 220, Color.FromArgb(10, 40, 100));
            btnLoad.Click += (s, e) =>
            {
                _messageEdited = false;
                if (_selected != null) ApplyAlloc(_selected, keepNumber: true);
                else FillDefaultMessage();
            };
            var btnWhatsApp = MakeBtn("WhatsApp", 242, fy, 180, Color.FromArgb(18, 140, 126));
            btnWhatsApp.Click += (s, e) => SendWhatsApp();
            var btnClear = MakeBtn("Clear", 432, fy, 140, Color.FromArgb(90, 100, 120));
            btnClear.Click += (s, e) => ClearForm();

            panelBody.Controls.Add(btnLoad);
            panelBody.Controls.Add(btnWhatsApp);
            panelBody.Controls.Add(btnClear);

            Controls.Add(panelBody);
            Controls.Add(panelTop);
        }

        private static TextBox Field(int x, int y, int w)
        {
            return new TextBox
            {
                Font = new Font("Segoe UI", 11F),
                Location = new Point(x, y),
                Size = new Size(w, 32)
            };
        }

        private static Guna.UI2.WinForms.Guna2Button MakeBtn(string text, int x, int y, int w, Color fill)
        {
            return new Guna.UI2.WinForms.Guna2Button
            {
                Text = text,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                FillColor = fill,
                ForeColor = Color.White,
                BorderRadius = 8,
                Location = new Point(x, y),
                Size = new Size(w, 40)
            };
        }

        private static Label Lbl(string text, int x, int y)
        {
            return new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(x, y),
                Text = text
            };
        }

        private void LoadAllocCache()
        {
            _cache = new List<AllocSuggest>();
            try
            {
                const string sql = @"
SELECT
  cp.id AS customer_plot_id,
  cp.customer_id AS customer_id,
  COALESCE(c.name,'') AS customer_name,
  COALESCE(c.father_name,'') AS father_name,
  COALESCE(c.mobile,'') AS mobile,
  COALESCE(c.cnic,'') AS cnic,
  COALESCE(s.name,'') AS society_name,
  COALESCE(b.name,'') AS block_name,
  CAST(p.plot_no AS TEXT) AS plot_no,
  COALESCE(pt.code,'') AS plot_type,
  COALESCE(cp.booking_date,'') AS booking_date,
  COALESCE(NULLIF(TRIM(cp.deal_type),''),'Installment') AS deal_type,
  COALESCE(cp.price,0) AS plot_price,
  COALESCE(cp.monthly_installment,0) AS monthly_installment,
  COALESCE(cp.down_payment,0) AS down_payment,
  COALESCE((SELECT SUM(amount) FROM receipts r WHERE r.customer_plot_id = cp.id),0) AS total_paid
FROM customer_plots cp
JOIN customers c ON c.id = cp.customer_id
JOIN plots p ON p.id = cp.plot_id
LEFT JOIN societies s ON s.id = p.society_id
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
                            var a = new AllocSuggest
                            {
                                CustomerPlotId = Convert.ToInt32(r["customer_plot_id"]),
                                CustomerId = Convert.ToInt32(r["customer_id"]),
                                Name = Convert.ToString(r["customer_name"] ?? "").Trim(),
                                Father = Convert.ToString(r["father_name"] ?? "").Trim(),
                                Mobile = Convert.ToString(r["mobile"] ?? "").Trim(),
                                Cnic = Convert.ToString(r["cnic"] ?? "").Trim(),
                                Society = Convert.ToString(r["society_name"] ?? "").Trim(),
                                Block = Convert.ToString(r["block_name"] ?? "").Trim(),
                                PlotNo = Convert.ToString(r["plot_no"] ?? "").Trim(),
                                PlotType = Convert.ToString(r["plot_type"] ?? "").Trim(),
                                BookingDate = Convert.ToString(r["booking_date"] ?? "").Trim(),
                                DealType = Convert.ToString(r["deal_type"] ?? "Installment").Trim(),
                                PlotPrice = SafeDouble(r["plot_price"]),
                                Monthly = SafeDouble(r["monthly_installment"]),
                                DownPayment = SafeDouble(r["down_payment"]),
                                TotalPaid = SafeDouble(r["total_paid"])
                            };
                            if (a.BookingDate.Length >= 10)
                                a.BookingDate = a.BookingDate.Substring(0, 10);

                            string disp = a.Name;
                            if (a.Father.Length > 0) disp += " / " + a.Father;
                            disp += " | Plot " + a.PlotNo;
                            if (a.Block.Length > 0) disp += " | " + a.Block;
                            if (a.Society.Length > 0) disp += " | " + a.Society;
                            a.Display = disp;
                            _cache.Add(a);
                        }
                    }
                }
            }
            catch { }
        }

        private void RefreshSuggest()
        {
            if (lstSuggest == null || txtCustomer == null) return;
            string q = (txtCustomer.Text ?? "").Trim();
            lstSuggest.BeginUpdate();
            try
            {
                lstSuggest.Items.Clear();
                foreach (var a in _cache)
                {
                    if (q.Length == 0
                        || a.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                        || a.Father.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                        || a.PlotNo.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                        || a.Mobile.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                        || a.Society.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                        || a.Display.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        lstSuggest.Items.Add(a);
                        if (lstSuggest.Items.Count >= 80) break;
                    }
                }
                lstSuggest.Visible = lstSuggest.Items.Count > 0 && q.Length > 0;
            }
            finally { lstSuggest.EndUpdate(); }
        }

        private void ApplySelectedSuggest()
        {
            if (lstSuggest == null || lstSuggest.SelectedItem == null) return;
            var a = lstSuggest.SelectedItem as AllocSuggest;
            if (a == null) return;
            ApplyAlloc(a, keepNumber: false);
        }

        private void ApplyAlloc(AllocSuggest a, bool keepNumber)
        {
            if (a == null) return;
            _selected = a;
            _busy = true;
            _filling = true;
            try
            {
                txtCustomer.Text = a.Name;
                if (!keepNumber) txtNumber.Text = a.Mobile;
                txtFather.Text = a.Father;
                txtCnic.Text = a.Cnic;
                txtSociety.Text = a.Society;
                txtBlock.Text = a.Block;
                txtPlotNo.Text = a.PlotNo;
                txtPlotType.Text = a.PlotType;
                txtBooking.Text = a.BookingDate;
                txtDealType.Text = a.DealType;
                txtPlotPrice.Text = a.PlotPrice > 0
                    ? a.PlotPrice.ToString("N0", CultureInfo.InvariantCulture)
                    : "";
                lstSuggest.Visible = false;
            }
            finally
            {
                _filling = false;
                _busy = false;
            }
            _messageEdited = false;
            FillDefaultMessage();
        }

        private List<ReceiptLine> LoadReceipts(int customerPlotId)
        {
            var list = new List<ReceiptLine>();
            if (customerPlotId <= 0) return list;
            try
            {
                using (var con = new SQLiteConnection(projectconnection.conReturn()))
                using (var cmd = new SQLiteCommand(@"
SELECT receipt_date, amount, IFNULL(mode,''), IFNULL(ref_no,''), IFNULL(notes,'')
FROM receipts
WHERE customer_plot_id = @id AND amount > 0
ORDER BY receipt_date ASC, id ASC;", con))
                {
                    con.Open();
                    cmd.Parameters.AddWithValue("@id", customerPlotId);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new ReceiptLine
                            {
                                Date = Convert.ToString(r[0] ?? "").Trim(),
                                Amount = SafeDouble(r[1]),
                                Mode = Convert.ToString(r[2] ?? "").Trim(),
                                RefNo = Convert.ToString(r[3] ?? "").Trim(),
                                Notes = Convert.ToString(r[4] ?? "").Trim()
                            });
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        internal static string BuildVipMessage(
            string customerName,
            string father,
            string mobile,
            string cnic,
            string society,
            string block,
            string plotNo,
            string plotType,
            string bookingDate,
            string dealType,
            double plotPrice,
            double monthly,
            IList<ReceiptLine> receipts)
        {
            string name = (customerName ?? "").Trim();
            if (name.Length == 0) name = "Janab";
            string date = DateTime.Now.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture);

            var receiptsSafe = receipts ?? new List<ReceiptLine>();
            double totalPaid = 0;
            foreach (var r in receiptsSafe) totalPaid += r.Amount;
            double remaining = Math.Round(plotPrice - totalPaid, 2);
            if (remaining < 0) remaining = 0;

            var sb = new StringBuilder();
            sb.AppendLine("Assalam o Alaikum *" + name + "*,");
            sb.AppendLine();
            sb.AppendLine("*HUB BUILDERS*");
            sb.AppendLine("Plot Allocation Confirmation");
            sb.AppendLine("Date: " + date);
            sb.AppendLine();
            sb.AppendLine("Mubarak ho!");
            sb.AppendLine("Aap ko Hub Builders ki taraf se *plot allocate* kar diya gaya hai.");
            sb.AppendLine("Allah ta'ala aap ke liye is plot mein barkat aur sukoon ata farmaye. Aameen.");
            sb.AppendLine();
            sb.AppendLine("*Customer Details:*");
            sb.AppendLine("Name: " + name);
            if (!string.IsNullOrWhiteSpace(father))
                sb.AppendLine("Father / Husband: " + father.Trim());
            if (!string.IsNullOrWhiteSpace(mobile))
                sb.AppendLine("Mobile: " + mobile.Trim());
            if (!string.IsNullOrWhiteSpace(cnic))
                sb.AppendLine("CNIC: " + cnic.Trim());
            sb.AppendLine();
            sb.AppendLine("*Plot Details:*");
            if (!string.IsNullOrWhiteSpace(society))
                sb.AppendLine("Society: " + society.Trim());
            if (!string.IsNullOrWhiteSpace(block))
                sb.AppendLine("Block: " + block.Trim());
            if (!string.IsNullOrWhiteSpace(plotNo))
                sb.AppendLine("Plot No: " + plotNo.Trim());
            if (!string.IsNullOrWhiteSpace(plotType))
                sb.AppendLine("Plot Type / Size: " + plotType.Trim());
            if (!string.IsNullOrWhiteSpace(bookingDate))
                sb.AppendLine("Booking Date: " + bookingDate.Trim());
            if (!string.IsNullOrWhiteSpace(dealType))
                sb.AppendLine("Deal Type: " + dealType.Trim());
            sb.AppendLine();
            sb.AppendLine("*Account Summary:*");
            sb.AppendLine("Allocate / Plot Amount: Rs " + FormatMoney(plotPrice));
            if (monthly > 0)
                sb.AppendLine("Monthly Installment: Rs " + FormatMoney(monthly));

            if (receiptsSafe.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("*Receipt Form — Credit Payments:*");
                for (int i = 0; i < receiptsSafe.Count; i++)
                {
                    var r = receiptsSafe[i];
                    string label = (i == 0) ? "Advance / 1st Receipt" : ("Receipt " + (i + 1));
                    string d = r.Date;
                    if (d.Length >= 10) d = d.Substring(0, 10);
                    string line = (i + 1) + ". " + label + " — " + d;
                    if (!string.IsNullOrWhiteSpace(r.Mode)) line += " | " + r.Mode;
                    if (!string.IsNullOrWhiteSpace(r.RefNo)) line += " | Ref: " + r.RefNo;
                    line += " | Credit: Rs " + FormatMoney(r.Amount);
                    sb.AppendLine(line);
                }
                sb.AppendLine("*Total Paid (Credit): Rs " + FormatMoney(totalPaid) + "*");
            }
            else
            {
                sb.AppendLine("Receipts (Credit): Abhi koi receipt save nahi hui.");
                sb.AppendLine("Total Paid (Credit): Rs 0");
            }

            sb.AppendLine("*Remaining Due: Rs " + FormatMoney(remaining) + "*");
            sb.AppendLine();
            sb.AppendLine("Aap ka plot Hub Builders ke paas secure hai.");
            sb.AppendLine("Barah-e-karam apni installment / dues time par clear karte rahein.");
            sb.AppendLine("Kisi bhi sawal ya madad ke liye Hub Builders office se rabta karein.");
            sb.AppendLine();
            sb.AppendLine("Shukriya — aap ka bharosa humari zimmedari hai.");
            sb.AppendLine("*Team Hub Builders*");
            return sb.ToString().TrimEnd() + Environment.NewLine;
        }

        private void FillDefaultMessage()
        {
            if (_busy) return;
            _filling = true;
            try
            {
                double price = ParseMoney(txtPlotPrice != null ? txtPlotPrice.Text : "");
                double monthly = _selected != null ? _selected.Monthly : 0;
                var receipts = _selected != null
                    ? LoadReceipts(_selected.CustomerPlotId)
                    : new List<ReceiptLine>();

                txtMessage.Text = BuildVipMessage(
                    txtCustomer != null ? txtCustomer.Text : "",
                    txtFather != null ? txtFather.Text : "",
                    txtNumber != null ? txtNumber.Text : "",
                    txtCnic != null ? txtCnic.Text : "",
                    txtSociety != null ? txtSociety.Text : "",
                    txtBlock != null ? txtBlock.Text : "",
                    txtPlotNo != null ? txtPlotNo.Text : "",
                    txtPlotType != null ? txtPlotType.Text : "",
                    txtBooking != null ? txtBooking.Text : "",
                    txtDealType != null ? txtDealType.Text : "",
                    price,
                    monthly,
                    receipts);
                _messageEdited = false;
            }
            finally { _filling = false; }
        }

        private void ClearForm()
        {
            _selected = null;
            _filling = true;
            try
            {
                txtCustomer.Clear();
                txtNumber.Clear();
                txtFather.Clear();
                txtCnic.Clear();
                txtSociety.Clear();
                txtBlock.Clear();
                txtPlotNo.Clear();
                txtPlotType.Clear();
                txtBooking.Clear();
                txtDealType.Clear();
                txtPlotPrice.Clear();
                if (lstSuggest != null)
                {
                    lstSuggest.Items.Clear();
                    lstSuggest.Visible = false;
                }
                _messageEdited = false;
            }
            finally { _filling = false; }
            FillDefaultMessage();
            try { txtCustomer.Focus(); } catch { }
        }

        private void SendWhatsApp()
        {
            string digits = NormalizePhoneToDigits(txtNumber.Text);
            if (string.IsNullOrWhiteSpace(digits) || digits.Length < 11)
            {
                MessageBox.Show("Customer ka WhatsApp number likhein (03xx...).", "WhatsApp",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                try { txtNumber.Focus(); } catch { }
                return;
            }
            string msg = (txtMessage.Text ?? "").Trim();
            if (msg.Length == 0)
            {
                FillDefaultMessage();
                msg = (txtMessage.Text ?? "").Trim();
            }
            OpenWhatsAppWithPrefill(digits, msg);
        }

        private static string FormatMoney(double v)
        {
            return Math.Round(v, 0).ToString("N0", CultureInfo.InvariantCulture);
        }

        private static double ParseMoney(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            double v;
            var cleaned = new string((s ?? "").Where(c => char.IsDigit(c) || c == '.' || c == '-').ToArray());
            if (double.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out v)) return v;
            if (double.TryParse(cleaned, NumberStyles.Any, CultureInfo.CurrentCulture, out v)) return v;
            return 0;
        }

        private static double SafeDouble(object o)
        {
            if (o == null || o == DBNull.Value) return 0d;
            double v;
            if (double.TryParse(Convert.ToString(o), NumberStyles.Any, CultureInfo.InvariantCulture, out v)) return v;
            if (double.TryParse(Convert.ToString(o), NumberStyles.Any, CultureInfo.CurrentCulture, out v)) return v;
            return 0d;
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

        private sealed class AllocSuggest
        {
            public int CustomerPlotId;
            public int CustomerId;
            public string Display = "";
            public string Name = "";
            public string Father = "";
            public string Mobile = "";
            public string Cnic = "";
            public string Society = "";
            public string Block = "";
            public string PlotNo = "";
            public string PlotType = "";
            public string BookingDate = "";
            public string DealType = "";
            public double PlotPrice;
            public double Monthly;
            public double DownPayment;
            public double TotalPaid;
            public override string ToString() => Display;
        }

        internal sealed class ReceiptLine
        {
            public string Date = "";
            public double Amount;
            public string Mode = "";
            public string RefNo = "";
            public string Notes = "";
        }
    }
}
