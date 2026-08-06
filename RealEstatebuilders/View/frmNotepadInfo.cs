using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace HubBuilders.View
{
    public partial class frmNotepadInfo : Sample
    {
        // UI
        private TextBox txtInfo;
        private Button btnClose, btnEnglish, btnRoman, btnWrap, btnSaveTxt; // RENAMED

        // State
        private enum Lang { English, Roman }
        private Lang current = Lang.English;
        private bool wrap = true;

        // Texts
        private readonly string englishText = @"Home (Dashboard)
- Dashboard page contains overview KPIs, latest receipts, and shortcuts.

Society & Blocks
- Add/manage societies and related blocks/phases.

Plots & Inventory
- Create plot types, manage plot inventory (Available/Booked/Allocated).

Allocation
- Allocate a plot to a customer. Configure price/plan/installments.

Customers
- Maintain customer records; link multiple allocations.

Receipts
- Add/edit/delete receipts with Ref# and Notes.

Ledger
- Opening, credits (receipts), and closing balance with search.

Expenses
- Track office/project expenses with categories.

Reports & Backup
- Multiple reports (ledger, receipts, allocations) and backup/restore.

Shortcuts
- Ctrl+E English, Ctrl+R Roman Urdu, Ctrl+S Save, Ctrl+C Copy All, Ctrl+L Wrap, ESC Close.";

        private readonly string romanText = @"Home (Dashboard)
- Dashboard par overview KPIs, latest receipts, aur shortcuts.

Society & Blocks
- Societies aur blocks/phases manage karo.

Plots & Inventory
- Plot types banao, plot inventory manage karo (Available/Booked/Allocated).

Allocation
- Customer ko plot allocate karo; price/plan/installments set karo.

Customers
- Customer records rakho; multi-allocation link.

Receipts
- Receipts add/edit/delete with Ref# aur Notes.

Ledger
- Opening, credits (receipts), aur closing balance with search.

Expenses
- Office/project expenses categorize karke record karo.

Reports & Backup
- Multiple reports (ledger, receipts, allocations) + backup/restore.

Shortcuts
- Ctrl+E English, Ctrl+R Roman, Ctrl+S Save, Ctrl+C Copy All, Ctrl+L Wrap, ESC Close.";

        public frmNotepadInfo()
        {
            InitializeComponent();
            Text = "Application Information — Hub Builders";
            MinimumSize = new Size(720, 520);
            Size = new Size(900, 640);
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;

            RightToLeft = RightToLeft.No;
            RightToLeftLayout = false;

            BuildUi();
            ApplyLanguage(Lang.English);
        }

        private void BuildUi()
        {
            var top = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Color.FromArgb(32, 36, 61)
            };
            Controls.Add(top);

            var leftStrip = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                Width = 520,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(8, 8, 0, 8)
            };
            top.Controls.Add(leftStrip);

            btnEnglish = MkBtn("English", (s, e) => ApplyLanguage(Lang.English));
            btnRoman = MkBtn("Roman Urdu", (s, e) => ApplyLanguage(Lang.Roman));
            btnSaveTxt = MkBtn("Save (Ctrl+S)", (s, e) => SaveToTxt());   // RENAMED
            btnWrap = MkBtn("Wrap: ON (Ctrl+L)", (s, e) => ToggleWrap());

            leftStrip.Controls.AddRange(new Control[] { btnEnglish, btnRoman, btnSaveTxt, btnWrap }); // RENAMED

            btnClose = new Button
            {
                Text = "Close  (ESC)",
                AutoSize = true,
                FlatStyle = FlatStyle.System,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnClose.Click += (s, e) => Close();

            var rightStrip = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 160,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 8, 8, 8),
                WrapContents = false
            };
            rightStrip.Controls.Add(btnClose);
            top.Controls.Add(rightStrip);

            txtInfo = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = wrap,
                Font = new Font("Segoe UI", 12f, FontStyle.Regular),
                BackColor = Color.FromArgb(20, 22, 35),
                ForeColor = Color.Gainsboro,
                BorderStyle = BorderStyle.None
            };
            Controls.Add(txtInfo);
        }

        private Button MkBtn(string text, EventHandler onClick)
        {
            var b = new Button
            {
                Text = text,
                AutoSize = true,
                FlatStyle = FlatStyle.System,
                Margin = new Padding(6, 4, 0, 4)
            };
            b.Click += onClick;
            return b;
        }

        private void ApplyLanguage(Lang lang)
        {
            current = lang;
            switch (lang)
            {
                case Lang.English:
                    txtInfo.Text = englishText;
                    txtInfo.RightToLeft = RightToLeft.No;
                    break;
                case Lang.Roman:
                    txtInfo.Text = romanText;
                    txtInfo.RightToLeft = RightToLeft.No;
                    break;
            }

            btnEnglish.Font = new Font(btnEnglish.Font, lang == Lang.English ? FontStyle.Bold : FontStyle.Regular);
            btnRoman.Font = new Font(btnRoman.Font, lang == Lang.Roman ? FontStyle.Bold : FontStyle.Regular);
        }

        private void SaveToTxt()
        {
            using (var sfd = new SaveFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                FileName = $"REB-Info-{current}-{DateTime.Now:yyyyMMdd-HHmm}.txt"
            })
            {
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    var utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
                    File.WriteAllText(sfd.FileName, txtInfo.Text, utf8Bom);
                    MessageBox.Show(this, "Saved.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void ToggleWrap()
        {
            wrap = !wrap;
            txtInfo.WordWrap = wrap;
            txtInfo.ScrollBars = wrap ? ScrollBars.Vertical : ScrollBars.Both;
            // btnWrap text update remains same
            btnWrap.Text = wrap ? "Wrap: ON (Ctrl+L)" : "Wrap: OFF (Ctrl+L)";
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape) { Close(); return true; }
            if (keyData == (Keys.Control | Keys.E)) { ApplyLanguage(Lang.English); return true; }
            if (keyData == (Keys.Control | Keys.R)) { ApplyLanguage(Lang.Roman); return true; }
            if (keyData == (Keys.Control | Keys.S)) { SaveToTxt(); return true; }
            if (keyData == (Keys.Control | Keys.C)) { try { Clipboard.SetText(txtInfo.Text); } catch { } return true; }
            if (keyData == (Keys.Control | Keys.L)) { ToggleWrap(); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
