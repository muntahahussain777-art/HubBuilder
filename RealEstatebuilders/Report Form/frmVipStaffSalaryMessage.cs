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
    /// Additive VIP form: staff → owner salary WhatsApp.
    /// Does not change existing forms, reports, or calculation logic.
    /// </summary>
    public class frmVipStaffSalaryMessage : Sample
    {
        private ComboBox cmbOwner;
        private TextBox txtNumber;
        private TextBox txtStaffName;
        private TextBox txtMonth;
        private TextBox txtMessage;
        private bool _filling;
        private bool _messageEdited;

        public frmVipStaffSalaryMessage()
        {
            InitializeUi();
            LoadOwnerNames();
            FillDefaultMessage();
        }

        private void InitializeUi()
        {
            Text = "Staff Salary Message (VIP)";
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
                Text = "Staff Salary Message (VIP)"
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
            panelBody.Controls.Add(Lbl("Owner name (type or select)", 12, y));
            y += 24;
            cmbOwner = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDown,
                Font = new Font("Segoe UI", 11F),
                Location = new Point(12, y),
                Size = new Size(560, 32)
            };
            cmbOwner.TextChanged += (s, e) =>
            {
                if (_filling) return;
                if (!_messageEdited) FillDefaultMessage();
            };
            panelBody.Controls.Add(cmbOwner);
            y += 44;

            panelBody.Controls.Add(Lbl("Owner WhatsApp number (03xx…)", 12, y));
            y += 24;
            txtNumber = new TextBox
            {
                Font = new Font("Segoe UI", 11F),
                Location = new Point(12, y),
                Size = new Size(560, 32)
            };
            panelBody.Controls.Add(txtNumber);
            y += 44;

            panelBody.Controls.Add(Lbl("Staff name (aap ka naam)", 12, y));
            y += 24;
            txtStaffName = new TextBox
            {
                Font = new Font("Segoe UI", 11F),
                Location = new Point(12, y),
                Size = new Size(560, 32),
                Text = StaffDisplayName()
            };
            txtStaffName.TextChanged += (s, e) =>
            {
                if (_filling) return;
                if (!_messageEdited) FillDefaultMessage();
            };
            panelBody.Controls.Add(txtStaffName);
            y += 44;

            panelBody.Controls.Add(Lbl("Salary month", 12, y));
            y += 24;
            txtMonth = new TextBox
            {
                Font = new Font("Segoe UI", 11F),
                Location = new Point(12, y),
                Size = new Size(560, 32),
                Text = DateTime.Now.ToString("MMMM yyyy", CultureInfo.InvariantCulture)
            };
            txtMonth.TextChanged += (s, e) =>
            {
                if (_filling) return;
                if (!_messageEdited) FillDefaultMessage();
            };
            panelBody.Controls.Add(txtMonth);
            y += 44;

            panelBody.Controls.Add(Lbl("VIP WhatsApp message (edit kar sakte ho)", 12, y));
            y += 24;
            txtMessage = new TextBox
            {
                Font = new Font("Segoe UI", 11F),
                Location = new Point(12, y),
                Size = new Size(720, 280),
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
            y += 296;

            var hint = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(200, 210, 230),
                Location = new Point(12, y),
                Text = "Owner name + number likho · message Hub Builders ke naam se hai · WhatsApp se bhejo"
            };
            panelBody.Controls.Add(hint);
            y += 32;

            var btnReset = MakeBtn("Default VIP Message", 12, y, 220, Color.FromArgb(10, 40, 100));
            btnReset.Click += (s, e) =>
            {
                _messageEdited = false;
                FillDefaultMessage();
            };
            var btnWhatsApp = MakeBtn("WhatsApp", 242, y, 180, Color.FromArgb(18, 140, 126));
            btnWhatsApp.Click += (s, e) => SendWhatsApp();
            var btnClear = MakeBtn("Clear", 432, y, 140, Color.FromArgb(90, 100, 120));
            btnClear.Click += (s, e) => ClearForm();

            panelBody.Controls.Add(btnReset);
            panelBody.Controls.Add(btnWhatsApp);
            panelBody.Controls.Add(btnClear);

            Controls.Add(panelBody);
            Controls.Add(panelTop);
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

        private static string StaffDisplayName()
        {
            try
            {
                string n = (MainClass.LoggedInName ?? "").Trim();
                if (n.Length > 0) return n;
                n = (MainClass.LoggedInUsername ?? "").Trim();
                if (n.Length > 0) return n;
            }
            catch { }
            return "";
        }

        private void LoadOwnerNames()
        {
            try
            {
                using (var con = new SQLiteConnection(projectconnection.conReturn()))
                using (var cmd = new SQLiteCommand(
                    "SELECT name FROM Person WHERE is_owner=1 AND name IS NOT NULL AND TRIM(name)<>'' ORDER BY name COLLATE NOCASE;", con))
                {
                    con.Open();
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            string n = Convert.ToString(r["name"]);
                            if (!string.IsNullOrWhiteSpace(n)) cmbOwner.Items.Add(n.Trim());
                        }
                    }
                }
            }
            catch { }
        }

        internal static string BuildVipMessage(string ownerName, string staffName, string month, DateTime when)
        {
            string owner = (ownerName ?? "").Trim();
            string staff = (staffName ?? "").Trim();
            string mon = (month ?? "").Trim();
            if (owner.Length == 0) owner = "Janab";
            if (staff.Length == 0) staff = "Hub Builders Staff";
            if (mon.Length == 0) mon = when.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
            string date = when.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture);

            var sb = new StringBuilder();
            sb.AppendLine("Assalam o Alaikum " + owner + ",");
            sb.AppendLine();
            sb.AppendLine("*HUB BUILDERS*");
            sb.AppendLine("Staff Salary Request");
            sb.AppendLine("Date: " + date);
            sb.AppendLine();
            sb.AppendLine("Ehtiram ke sath darkhwast hai.");
            sb.AppendLine();
            sb.AppendLine("Main *" + staff + "*, Hub Builders ka staff hoon.");
            sb.AppendLine("Barah-e-karam arz hai ke meri *" + mon + "* ki salary abhi tak receive nahi hui.");
            sb.AppendLine();
            sb.AppendLine("Allah ta'ala aap ko sehat, khushhali aur barkat ata farmaye.");
            sb.AppendLine("Agar is mahine ki salary release farma dein to mein dil se shukrguzar rahunga.");
            sb.AppendLine();
            sb.AppendLine("Shukriya.");
            sb.AppendLine("Wassalam,");
            sb.AppendLine(staff);
            sb.AppendLine("Hub Builders");
            return sb.ToString().TrimEnd() + Environment.NewLine;
        }

        private void FillDefaultMessage()
        {
            _filling = true;
            try
            {
                txtMessage.Text = BuildVipMessage(
                    cmbOwner.Text,
                    txtStaffName != null ? txtStaffName.Text : "",
                    txtMonth != null ? txtMonth.Text : "",
                    DateTime.Now);
                _messageEdited = false;
            }
            finally { _filling = false; }
        }

        private void ClearForm()
        {
            _filling = true;
            try
            {
                cmbOwner.Text = "";
                txtNumber.Clear();
                txtMonth.Text = DateTime.Now.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
                _messageEdited = false;
            }
            finally { _filling = false; }
            FillDefaultMessage();
            try { cmbOwner.Focus(); } catch { }
        }

        private void SendWhatsApp()
        {
            string digits = NormalizePhoneToDigits(txtNumber.Text);
            if (string.IsNullOrWhiteSpace(digits) || digits.Length < 11)
            {
                MessageBox.Show("Owner ka WhatsApp number likhein (03xx...).", "WhatsApp",
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
                var psi = new ProcessStartInfo { FileName = url, UseShellExecute = true };
                Process.Start(psi);
                return true;
            }
            catch { return false; }
        }
    }
}
