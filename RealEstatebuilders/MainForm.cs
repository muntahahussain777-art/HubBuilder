using HubBuilders.Model;
using HubBuilders.Report_Form;
using HubBuilders.View;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace HubBuilders
{
    public partial class MainForm : Sample
    {
        private string fullText = "Hub Builders";
        // Number of characters currently drawn
        private int currentCharCount = 0;
        // Timer for animation
        private Timer animationTimer = new Timer();

        private static readonly Color NavNormal = Color.FromArgb(39, 49, 70);
        private static readonly Color NavActive = Color.FromArgb(0, 150, 136);
        private Guna.UI2.WinForms.Guna2Button _activeNav;

        public MainForm()
        {
            InitializeComponent();
            _obj = this; // BlurBackground / instance ke liye sahi form — doosra MainForm create na ho
            // Ensure pictureBox2 has the desired size
            pictureBox2.Size = new Size(672, 688);
            // Set DoubleBuffered to reduce flicker

            //pictureBox2.SetDoubleBuffered(true);

            // Set up timer properties
            animationTimer.Interval = 100; // adjust delay as needed
            animationTimer.Tick += AnimationTimer_Tick;
            animationTimer.Start();

            // Hook up the Paint event to draw animated content
            pictureBox2.Paint += pictureBox2_Paint;

            this.KeyPreview = true;
            WireNavTooltips();
            AddSyncPullNavButtons();
            try { /* LiveNotice off — bar bar toast band */ } catch { }
        }
        static MainForm _obj;
        public static MainForm instance
        {
            get { if (_obj == null) { _obj = new MainForm(); } return _obj; }
        }

        private Guna.UI2.WinForms.Guna2Button[] NavButtons()
        {
            return new[]
            {
                btnHome, btnCustomer, btnSociety, btnInventory, btnplot,
                btnPlotInventory, btnAllogation, btnLedger, btnReciptForm,
                btnExpense, btnReports, btnPasswordchange
            };
        }

        private void WireNavTooltips()
        {
            var tip = new ToolTip { ShowAlways = true, AutoPopDelay = 8000 };
            tip.SetToolTip(btnHome, "Shortcut: Alt+H");
            tip.SetToolTip(btnCustomer, "Shortcut: Alt+C");
            tip.SetToolTip(btnSociety, "Shortcut: Alt+S");
            tip.SetToolTip(btnInventory, "Shortcut: Alt+B");
            tip.SetToolTip(btnplot, "Shortcut: Alt+P");
            tip.SetToolTip(btnPlotInventory, "Shortcut: Alt+I");
            tip.SetToolTip(btnAllogation, "Shortcut: Alt+A");
            tip.SetToolTip(btnLedger, "Shortcut: Alt+L");
            tip.SetToolTip(btnReciptForm, "Shortcut: Alt+R");
            tip.SetToolTip(btnExpense, "Shortcut: Alt+E");
            tip.SetToolTip(btnReports, "Shortcut: Alt+T");
            tip.SetToolTip(btnPasswordchange, "Shortcut: Alt+W");
        }

        /// <summary>Minimal Sync/Pull/Recover — existing nav style, no redesign.</summary>
        private void AddSyncPullNavButtons()
        {
            try
            {
                var tip = new ToolTip { ShowAlways = true, AutoPopDelay = 10000 };
                var btnSync = MakeNavActionButton("  Sync Now", 104);
                var btnPull = MakeNavActionButton("  Pull Now", 105);
                var btnRecover = MakeNavActionButton("  Force Recover", 106);
                btnRecover.FillColor = Color.FromArgb(160, 90, 30);
                btnRecover.Size = new Size(228, 34);
                tip.SetToolTip(btnSync, "Push local + merge from cloud (safe — no wipe)");
                tip.SetToolTip(btnPull, "Pull from cloud into SQLite (safe — no wipe)");
                tip.SetToolTip(btnRecover,
                    "Missing allocate data cloud se wapas — sirf absent sync_id INSERT, duplicates skip, deleted resurrect nahi");
                btnSync.Click += async (s, e) =>
                {
                    btnSync.Enabled = false;
                    btnPull.Enabled = false;
                    btnRecover.Enabled = false;
                    try
                    {
                        bool ok = await Task.Run(() => HubSyncService.TrySyncNow());
                        MessageBox.Show(this,
                            ok ? "Sync complete."
                               : "Sync abhi busy tha / incomplete. 10 sec baad Sync Now dobara dabayein (internet on rakhein).",
                            "Sync Now", MessageBoxButtons.OK,
                            ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                    }
                    finally
                    {
                        btnSync.Enabled = true;
                        btnPull.Enabled = true;
                        btnRecover.Enabled = true;
                    }
                };
                btnPull.Click += async (s, e) =>
                {
                    btnSync.Enabled = false;
                    btnPull.Enabled = false;
                    btnRecover.Enabled = false;
                    try
                    {
                        bool ok = await Task.Run(() => HubSyncService.TryPullNow());
                        MessageBox.Show(this,
                            ok ? "Pull complete."
                               : "Pull abhi busy tha / incomplete. 10 sec baad Pull Now dobara dabayein (internet on rakhein).",
                            "Pull Now", MessageBoxButtons.OK,
                            ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                    }
                    finally
                    {
                        btnSync.Enabled = true;
                        btnPull.Enabled = true;
                        btnRecover.Enabled = true;
                    }
                };
                btnRecover.Click += async (s, e) =>
                {
                    var ask = MessageBox.Show(this,
                        "Cloud se missing allocate rows wapas laoga (same sync_id skip).\n"
                        + "Local extras wipe nahi. Intentionally deleted resurrect nahi.\n\nContinue?",
                        "Force Recover from Cloud",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                    if (ask != DialogResult.Yes) return;

                    btnSync.Enabled = false;
                    btnPull.Enabled = false;
                    btnRecover.Enabled = false;
                    try
                    {
                        string summary = "";
                        bool ok = false;
                        await Task.Run(() =>
                        {
                            ok = HubSyncService.TryForceRecoverFromCloud(out summary);
                        });
                        MessageBox.Show(this,
                            ok
                                ? (string.IsNullOrWhiteSpace(summary) ? "Recovered / Sync complete." : summary)
                                : (string.IsNullOrWhiteSpace(summary)
                                    ? "Recover busy/fail — 10 sec baad dobara try (internet on)."
                                    : summary),
                            "Force Recover",
                            MessageBoxButtons.OK,
                            ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                    }
                    finally
                    {
                        btnSync.Enabled = true;
                        btnPull.Enabled = true;
                        btnRecover.Enabled = true;
                    }
                };
                flowLayoutPanel1.Controls.Add(btnSync);
                flowLayoutPanel1.Controls.Add(btnPull);
                flowLayoutPanel1.Controls.Add(btnRecover);
            }
            catch { }
        }

        private static Guna.UI2.WinForms.Guna2Button MakeNavActionButton(string text, int tab)
        {
            var b = new Guna.UI2.WinForms.Guna2Button();
            b.AutoRoundedCorners = true;
            b.BorderRadius = 16;
            b.FillColor = Color.FromArgb(39, 49, 70);
            b.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            b.ForeColor = Color.White;
            b.Size = new Size(208, 34);
            b.TabIndex = tab;
            b.Text = text;
            b.TextAlign = HorizontalAlignment.Left;
            return b;
        }

        private void SetActiveNav(Guna.UI2.WinForms.Guna2Button btn)
        {
            foreach (var b in NavButtons())
            {
                if (b == null) continue;
                b.FillColor = NavNormal;
            }
            if (btn != null)
            {
                btn.FillColor = NavActive;
                _activeNav = btn;
            }
            else
            {
                _activeNav = null;
            }
        }

        private void AddControls(Form F)
        {
            try
            {
                // Purane embedded forms dispose — memory/flicker; UI layout same (Dock Fill)
                while (Controlspanel.Controls.Count > 0)
                {
                    var old = Controlspanel.Controls[0];
                    Controlspanel.Controls.RemoveAt(0);
                    try { old.Dispose(); } catch { }
                }
                F.TopLevel = false;
                Controlspanel.Controls.Add(F);
                F.Dock = DockStyle.Fill;
                F.Show();
                // Focus child turant — warna Ctrl+N tab tak nahi chalta jab tak andar click na ho
                FocusChildForm(F);
            }
            catch (Exception ex)
            {
                AppSafety.ShowSoft(ex);
            }
        }

        private void FocusChildForm(Form F)
        {
            if (F == null || F.IsDisposed) return;
            try
            {
                F.Select();
                F.Focus();
                Controlspanel.Select();
                // Layout ke baad dubara — Alt shortcut ke baad focus pakka child pe
                BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (F.IsDisposed) return;
                        F.BringToFront();
                        if (!F.Focus())
                            Controlspanel.Focus();
                        // Search box ho to wahan focus — typing + Ctrl+N ready
                        var search = FindControlByName(F, "txtSearch");
                        if (search != null && search.CanFocus)
                            search.Focus();
                        else
                            F.SelectNextControl(F, true, true, true, true);
                    }
                    catch { }
                }));
            }
            catch { }
        }

        private static Control FindControlByName(Control root, string name)
        {
            if (root == null) return null;
            if (string.Equals(root.Name, name, StringComparison.OrdinalIgnoreCase))
                return root;
            foreach (Control c in root.Controls)
            {
                var found = FindControlByName(c, name);
                if (found != null) return found;
            }
            return null;
        }

        private void OpenChild(Func<Form> create, Guna.UI2.WinForms.Guna2Button navBtn = null)
        {
            AppSafety.Run(() =>
            {
                Form f = create();
                string mod = StaffAccess.ModuleForForm(f);
                if (!string.IsNullOrWhiteSpace(mod) && StaffAccess.Deny(mod, "open"))
                {
                    try { f.Dispose(); } catch { }
                    return;
                }
                AddControls(f);
                SetActiveNav(navBtn);
            });
        }

        /// <summary>Sidebar + menu forms ke jaldi shortcuts. F1 = list.</summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (TryHandleGlobalShortcut(keyData))
                return true;
            return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>Child forms se bhi call — Alt shortcuts har jagah.</summary>
        public bool TryHandleGlobalShortcut(Keys keyData)
        {
            // Ctrl+N = active list form ka Add — focus sidebar pe ho tab bhi
            if (keyData == (Keys.Control | Keys.N))
            {
                if (TryCtrlNOnActiveChild())
                    return true;
            }

            // F1 = shortcut help
            if (keyData == Keys.F1)
            {
                ShowShortcutHelp();
                return true;
            }

            // Sidebar forms
            if (keyData == (Keys.Alt | Keys.H)) { btnHome_Click(btnHome, EventArgs.Empty); return true; }
            if (keyData == (Keys.Alt | Keys.C)) { btnCustomer_Click(btnCustomer, EventArgs.Empty); return true; }
            if (keyData == (Keys.Alt | Keys.S)) { btnSociety_Click(btnSociety, EventArgs.Empty); return true; }
            if (keyData == (Keys.Alt | Keys.B)) { btnInventory_Click(btnInventory, EventArgs.Empty); return true; }
            if (keyData == (Keys.Alt | Keys.P)) { btnplot_Click(btnplot, EventArgs.Empty); return true; }
            if (keyData == (Keys.Alt | Keys.I)) { btnPlotInventory_Click(btnPlotInventory, EventArgs.Empty); return true; }
            if (keyData == (Keys.Alt | Keys.A)) { btnAllogation_Click(btnAllogation, EventArgs.Empty); return true; }
            if (keyData == (Keys.Alt | Keys.L)) { btnLedger_Click(btnLedger, EventArgs.Empty); return true; }
            if (keyData == (Keys.Alt | Keys.R)) { btnReciptForm_Click(btnReciptForm, EventArgs.Empty); return true; }
            if (keyData == (Keys.Alt | Keys.E)) { btnExpense_Click(btnExpense, EventArgs.Empty); return true; }
            if (keyData == (Keys.Alt | Keys.T)) { btnReports_Click(btnReports, EventArgs.Empty); return true; }
            if (keyData == (Keys.Alt | Keys.W)) { btnPasswordchange_Click(btnPasswordchange, EventArgs.Empty); return true; }

            // Menu forms (Ctrl+Shift)
            if (keyData == (Keys.Control | Keys.Shift | Keys.O)) { ownerFormAddToolStripMenuItem_Click(null, EventArgs.Empty); return true; }
            if (keyData == (Keys.Control | Keys.Shift | Keys.N)) { partnerFormAddToolStripMenuItem_Click(null, EventArgs.Empty); return true; }
            if (keyData == (Keys.Control | Keys.Shift | Keys.D)) { dealFormToolStripMenuItem1_Click(null, EventArgs.Empty); return true; }
            if (keyData == (Keys.Control | Keys.Shift | Keys.U)) { creditFormToolStripMenuItem_Click(null, EventArgs.Empty); return true; }
            if (keyData == (Keys.Control | Keys.Shift | Keys.B)) { brokerAddToolStripMenuItem_Click(null, EventArgs.Empty); return true; }
            if (keyData == (Keys.Control | Keys.Shift | Keys.G)) { agreementDesignToolStripMenuItem_Click(null, EventArgs.Empty); return true; }
            if (keyData == (Keys.Control | Keys.Shift | Keys.M)) { customerToolStripMenuItem_Click(null, EventArgs.Empty); return true; }

            return false;
        }

        private bool TryCtrlNOnActiveChild()
        {
            try
            {
                if (Controlspanel == null) return false;
                foreach (Control c in Controlspanel.Controls)
                {
                    if (c is SampleView view)
                        return view.TriggerAddShortcut();
                }
            }
            catch { }
            return false;
        }

        private void ShowShortcutHelp()
        {
            MessageBox.Show(
                "FORM SHORTCUTS\n" +
                "─────────────────────\n" +
                "Alt+H  Home\n" +
                "Alt+C  Customer\n" +
                "Alt+S  Society\n" +
                "Alt+B  Block\n" +
                "Alt+P  Plot\n" +
                "Alt+I  Plot Inventory\n" +
                "Alt+A  Allocate\n" +
                "Alt+L  Ledger\n" +
                "Alt+R  Receipt Form\n" +
                "Alt+E  Expense\n" +
                "Alt+T  Reports\n" +
                "Alt+W  Password Change\n" +
                "\n" +
                "MENU SHORTCUTS\n" +
                "─────────────────────\n" +
                "Ctrl+Shift+M  New Customer Add\n" +
                "Ctrl+Shift+O  Owner\n" +
                "Ctrl+Shift+N  Partner\n" +
                "Ctrl+Shift+D  Deal\n" +
                "Ctrl+Shift+U  Credit\n" +
                "Ctrl+Shift+B  Broker\n" +
                "Ctrl+Shift+G  Agreement Design\n" +
                "\n" +
                "ADD FORM\n" +
                "─────────────────────\n" +
                "Ctrl+S  Save | Esc  Close | Ctrl+N  Add (list)\n" +
                "\nF1 = yeh list",
                "Hub Builders — Shortcuts",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (currentCharCount < fullText.Length)
            {
                currentCharCount++;
                pictureBox2.Invalidate(); // force repaint to update animation
            }
            else
            {
                animationTimer.Stop();
            }
        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            //if (Properties.Settings.Default.ShowTutorial)
            //{
            //    using (TutorialForm tutorial = new TutorialForm())
            //    {
            //        tutorial.ShowDialog();
            //    }
            //    // Tutorial dekhne ke baad flag ko false set kar dein taa ke agli baar dikhaye na
            //    Properties.Settings.Default.ShowTutorial = false;
            //                Properties.Settings.Default.Save();
            //}
            try { HubAutoBackup.Start(this); } catch { }
        }
        private async void pictureBox1_Click(object sender, EventArgs e)
        {
            // 1. HUB text ke liye ek Label create karo
            Label hubLabel = new Label();
            hubLabel.Font = new Font("Segoe UI", 36, FontStyle.Bold);
            hubLabel.ForeColor = Color.FromArgb(95, 61, 204);
            hubLabel.BackColor = Color.Transparent;
            hubLabel.AutoSize = true;
            hubLabel.Text = ""; // Shuru mein khali
                                // Label ko center mein set karo (initial location; adjust as needed)
            hubLabel.Location = new Point((this.ClientSize.Width - hubLabel.Width) / 2, (this.ClientSize.Height - hubLabel.Height) / 2);
            this.Controls.Add(hubLabel);
            hubLabel.BringToFront();

            // 2. "HUB" text ko letter-by-letter animate karo over 1000ms
            await AnimateHubTextAsync(hubLabel, "Hub Builders", 1000);

            // 3. Thodi der ruk jao taa ke user text ko enjoy kar sake
            await Task.Delay(500);

            // 4. Current form ko slide aur fade out karo (animation duration 600ms)
            await SlideFadeOutAsync(this, 600);

            // 5. Clean up: HUB label remove karo, form hide karo aur opacity reset karo
            this.Controls.Remove(hubLabel);
            this.Hide();
            this.Opacity = 1;
            MainClass.ClearSession();

            // 6. LoginForm ko create karo, off-screen (right side) set karo aur opacity 0 se shuru karo
            LoginForm login = new LoginForm();
            Rectangle screenBounds = Screen.PrimaryScreen.WorkingArea;
            login.StartPosition = FormStartPosition.Manual;
            login.Location = new Point(screenBounds.Width, (screenBounds.Height - login.Height) / 2);
            login.Opacity = 0;
            login.Show();

            // 7. LoginForm ko slide aur fade in karo
            await SlideFadeInAsync(login, 600);
        }

        // AnimateHubTextAsync: Label mein "HUB" text ko letter-by-letter animate karta hai
        private async Task AnimateHubTextAsync(Label lbl, string fullText, int duration)
        {
            lbl.Text = "";
            int totalLetters = fullText.Length;
            int delay = duration / totalLetters;

            for (int i = 0; i < totalLetters; i++)
            {
                lbl.Text += fullText[i];
                await Task.Delay(delay);
            }
        }

        // SlideFadeOutAsync: Form ko left side ki taraf slide aur fade out karta hai
        private async Task SlideFadeOutAsync(Form form, int duration)
        {
            int steps = 30;
            int delay = duration / steps;
            int startX = form.Left;
            int targetX = -form.Width; // Off-screen left
            int deltaX = (startX - targetX) / steps;
            double stepOpacity = form.Opacity / steps;

            for (int i = 0; i < steps; i++)
            {
                form.Left -= deltaX;
                form.Opacity -= stepOpacity;
                await Task.Delay(delay);
            }
            form.Opacity = 0;
        }

        // SlideFadeInAsync: Form ko right side se center ki taraf slide aur fade in karta hai
        private async Task SlideFadeInAsync(Form form, int duration)
        {
            int steps = 30;
            int delay = duration / steps;
            int targetX = (Screen.PrimaryScreen.WorkingArea.Width - form.Width) / 2;
            int startX = form.Left;
            int deltaX = (startX - targetX) / steps;
            double stepOpacity = (1 - form.Opacity) / steps;

            for (int i = 0; i < steps; i++)
            {
                form.Left -= deltaX;
                form.Opacity += stepOpacity;
                await Task.Delay(delay);
            }
            form.Left = targetX;
            form.Opacity = 1;
        }

        // label3_Click event handler
        private async void label3_Click(object sender, EventArgs e)
        {
            // frmNotepadInfo: aapka notepad jaisa form jismein application info hogi
            frmNotepadInfo infoForm = new frmNotepadInfo();

            // Screen bounds nikalain
            Rectangle screenBounds = Screen.PrimaryScreen.WorkingArea;

            // Manual start position set karain
            infoForm.StartPosition = FormStartPosition.Manual;

            // Target location: screen center
            int targetX = (screenBounds.Width - infoForm.Width) / 2;
            int targetY = (screenBounds.Height - infoForm.Height) / 2;

            // Form ko initially off-screen bottom set karain
            infoForm.Location = new Point(targetX, screenBounds.Height);
            infoForm.Opacity = 0; // Shuruat mein invisible

            // Form ko show karain
            infoForm.Show();

            // Slide aur fade in animation se form ko center le aayen over 600ms
            await SlideFadeInFromBottomAsync(infoForm, 600);
        }

        // Asynchronous method jo form ko slide aur fade in karega from bottom
        private async Task SlideFadeInFromBottomAsync(Form form, int duration)
        {
            int steps = 30;                     // Animation steps
            int delay = duration / steps;       // Har step ke delay (milliseconds)

            // Starting Y position (off-screen bottom)
            int startY = form.Top;
            // Target Y position (centered vertically)
            int targetY = (Screen.PrimaryScreen.WorkingArea.Height - form.Height) / 2;

            // Total vertical distance to move
            int totalDistance = startY - targetY;
            int deltaY = totalDistance / steps;

            // Opacity increment per step
            double stepOpacity = (1 - form.Opacity) / steps;

            for (int i = 0; i < steps; i++)
            {
                form.Top -= deltaY;           // Move form up
                form.Opacity += stepOpacity;   // Increase opacity gradually
                await Task.Delay(delay);       // Wait for next step
            }

            // Ensure final values
            form.Top = targetY;
            form.Opacity = 1;
        }
        private void pictureBox2_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            // Gradient background
            using (LinearGradientBrush bgBrush = new LinearGradientBrush(
                new Rectangle(0, 0, pictureBox2.Width, pictureBox2.Height),
                Color.Black,
                Color.DarkRed,
                LinearGradientMode.ForwardDiagonal))
            {
                g.FillRectangle(bgBrush, 0, 0, pictureBox2.Width, pictureBox2.Height);
            }

            // Animated text
            // Animated text
            string textToDraw = fullText.Substring(0, Math.Min(currentCharCount, fullText.Length));
            using (Font font = new Font("Segoe UI", 48, FontStyle.Bold))
            {
                SizeF textSize = g.MeasureString(textToDraw, font);
                // Check: Agar text size 0 ho to skip drawing text
                if (textSize.Width > 0 && textSize.Height > 0)
                {
                    float textX = (pictureBox2.Width - textSize.Width) / 2;
                    float textY = (pictureBox2.Height - textSize.Height) / 2;
                    using (LinearGradientBrush textBrush = new LinearGradientBrush(
                        new Rectangle((int)textX, (int)textY, (int)textSize.Width, (int)textSize.Height),
                        Color.Gold,
                        Color.White,
                        LinearGradientMode.Vertical))
                    {
                        g.DrawString(textToDraw, font, textBrush, textX, textY);
                    }
                }
            }



            // VIP Lock Icon
            int lockSize = 60;
            float lockX = (pictureBox2.Width - lockSize) / 2;
            float lockY = (pictureBox2.Height / 2) - 150; // adjust as needed
            using (Pen lockPen = new Pen(Color.Gold, 4))
            {
                RectangleF lockBody = new RectangleF(lockX, lockY + lockSize / 2, lockSize, lockSize);
                g.DrawRectangle(lockPen, lockBody.X, lockBody.Y, lockBody.Width, lockBody.Height);
                g.DrawArc(lockPen, lockX, lockY, lockSize, lockSize, 0, 180);
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnCustomer_Click(object sender, EventArgs e)
        {
            OpenChild(() => new frmCustomerView(), btnCustomer);
        }

        private void customerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (StaffAccess.Deny("customers", "add")) return;
            AppSafety.Run(() =>
            {
                frmCustomerAdd add = new frmCustomerAdd();
                add.ShowDialog();
            });
        }

        private void btnSociety_Click(object sender, EventArgs e)
        {
            OpenChild(() => new frmSocietyView(), btnSociety);
        }

        private void btnInventory_Click(object sender, EventArgs e)
        {
            OpenChild(() => new frmBlockFormView(), btnInventory);

        }

        private void btnplot_Click(object sender, EventArgs e)
        {
            OpenChild(() => new frmPlotTypeFormView(), btnplot);
        }

        private void btnPlotInventory_Click(object sender, EventArgs e)
        {
              OpenChild(() => new frmPlotInventoryFormAdd(), btnPlotInventory);
        }

        private void btnAllogation_Click(object sender, EventArgs e)
        {
            OpenChild(() => new frmAllocatePlotFormView(), btnAllogation);
        }
        private void btnLedger_Click(object sender, EventArgs e)
        {
            OpenChild(() => new frmCustomerLedgerAdd(), btnLedger);
        }

        private void btnReciptForm_Click(object sender, EventArgs e)
        {
            OpenChild(() => new frmReceiptFormView(), btnReciptForm);
        }

        private void btnReports_Click(object sender, EventArgs e)
        {
            OpenChild(() => new AllReportForm(), btnReports);
        }

        private void agreementDesignToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AppSafety.Run(() =>
            {
                MyForm my = new MyForm();
                my.ShowDialog();
            });
        }

        private void btnHome_Click(object sender, EventArgs e)
        {
            OpenChild(() => new frmDashboard(), btnHome);
        }

        private void btnExpense_Click(object sender, EventArgs e)
        {
            OpenChild(() => new frmExpenseVIew(), btnExpense);
        }

        private void btnPasswordchange_Click(object sender, EventArgs e)
        {
            if (StaffAccess.Deny("users", "open")) return;
            OpenChild(() => new frmPasswordView(), btnPasswordchange);
        }
        private void dealFormToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            OpenChild(() => new DealFormView());
        }

        private void creditFormToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenChild(() => new CreditFormView());
        }

        private void ownerFormAddToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenChild(() => new frmOwnerFormView());
        }

        private void partnerFormAddToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenChild(() => new frmPartnerFormView());
        }

        private void partnerCashBookToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenChild(() => new frmPartnerCashView());
        }

        private void brokerAddToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenChild(() => new frmBrokerView());
        }

        private void ownerReports1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmOwnerPartnerForm own = new frmOwnerPartnerForm();
            own.ShowDialog();
        }

        private void label3_Click_1(object sender, EventArgs e)
        {
            MainClass.ClearSession();
            this.Hide();
            LoginForm log = new LoginForm();
            log.ShowDialog();
            this.Close();
        }

        private void loginFormToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainClass.ClearSession();
            this.Hide();
            LoginForm log = new LoginForm();
            log.ShowDialog();
            this.Close();
        }

        private void applicationInfoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmNotepadInfo note = new frmNotepadInfo();
            note.ShowDialog();  
        }

        private void staffEntryLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (StaffAccess.Deny("staff_entry_log", "open")) return;
            AppSafety.Run(() =>
            {
                using (var frm = new frmStaffEntryLog())
                    frm.ShowDialog(this);
            });
        }

        private void applicationCloseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
