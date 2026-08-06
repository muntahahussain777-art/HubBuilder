using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace HubBuilders.View
{
    public partial class TutorialForm : Sample
    {
        private readonly List<TutorialStep> steps = new List<TutorialStep>();
        private int currentStep = 0;

        private Label lblTitle;
        private Label lblDescription;
        private Button btnNext;
        private Button btnBack;

        public TutorialForm()
        {
            InitializeComponent();

            // Window
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(32, 36, 61);
            Opacity = 0.95;
            KeyPreview = true; // ESC/Keys

            InitializeTutorialUI();
            LoadSteps_REBuilders();      // <- updated steps (correct workflow)
            ShowStep(currentStep);
        }

        // --- UI ---
        private void InitializeTutorialUI()
        {
            // Title
            lblTitle = new Label
            {
                Dock = DockStyle.Top,
                Height = 64,
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(lblTitle);

            // Description
            lblDescription = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 13f, FontStyle.Regular),
                ForeColor = Color.Gainsboro,
                TextAlign = ContentAlignment.TopCenter,
                Padding = new Padding(24, 12, 24, 12)
            };
            Controls.Add(lblDescription);

            // Nav panel
            var navPanel = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = Color.FromArgb(28, 32, 54) };
            Controls.Add(navPanel);

            // Right-docked button strip
            var rightStrip = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Dock = DockStyle.Right,
                Padding = new Padding(0, 10, 12, 10),
                AutoSize = true,
                WrapContents = false
            };
            navPanel.Controls.Add(rightStrip);

            btnBack = new Button
            {
                Text = "Back",
                Size = new Size(96, 32),
                Enabled = false,
                FlatStyle = FlatStyle.System
            };
            btnBack.Click += BtnBack_Click;

            btnNext = new Button
            {
                Text = "Next",
                Size = new Size(96, 32),
                FlatStyle = FlatStyle.System
            };
            btnNext.Click += BtnNext_Click;

            rightStrip.Controls.Add(btnBack);
            rightStrip.Controls.Add(btnNext);
        }

        // --- Steps (Correct workflow) ---
        private void LoadSteps_REBuilders()
        {
            steps.Clear();

            steps.Add(new TutorialStep
            {
                Title = "Welcome — Real Estate Builders",
                Description =
@"Ye guided tour app ke workflow ko asaan lafzon me batata hai.

Keys:
• Enter/Right = Next
• Backspace/Left = Back
• ESC = Close"
            });

            steps.Add(new TutorialStep
            {
                Title = "Quick Start (Workflow)",
                Description =
@"Recommended order:
1) Society
2) Blocks / Phases (A, B, …)
3) Plot Types (e.g., 5 Marla, 10 Marla)
4) Plots / Inventory (auto-generate or add)
5) Customers (add customer)
6) Allocation (customer ko plot dena)
7) Receipts (credit enter karna)
8) Ledger / Reports"
            });

            steps.Add(new TutorialStep
            {
                Title = "Societies",
                Description =
@"Sabse pehle Society add karein (e.g., 'ABC Housing').
Is se neeche ke modules proper map honge."
            });

            steps.Add(new TutorialStep
            {
                Title = "Blocks / Phases",
                Description =
@"Har Society ke andar Blocks/Phases banayein (A, B, C...).
Search aur edit options available hain."
            });

            steps.Add(new TutorialStep
            {
                Title = "Plot Types",
                Description =
@"Society-level plot types define karein (code/size).
Example: '5M', '10M', '1K'."
            });

            steps.Add(new TutorialStep
            {
                Title = "Plots & Inventory",
                Description =
@"Plots add ya bulk ‘Inventory Generate’ karein.
Status: Available/Hold/Sold system manage karta hai."
            });

            steps.Add(new TutorialStep
            {
                Title = "Customers",
                Description =
@"Customer add/edit: Name, Father Name, Mobile, CNIC, Notes.
Yahin ka record Allocation me link hota hai."
            });

            steps.Add(new TutorialStep
            {
                Title = "Allocation",
                Description =
@"Customer ko plot allocate karein:
• Society/Block/Type/Plot select
• Price & Booking Date set
• Status: 'Active' par plot Hold ho jata hai."
            });

            steps.Add(new TutorialStep
            {
                Title = "Receipts (Credit)",
                Description =
@"Allocation ke against payments enter karein:
• Cash/Bank mode
• Ref No & Notes
• Overpayment guard enabled (remaining se zyada allow nahi)."
            });

            steps.Add(new TutorialStep
            {
                Title = "Ledger",
                Description =
@"Ledger me Opening (Debit), Receipts (Credit) aur Closing balance dikhte hain.
Search me ref no/notes/customer ke hisaab se filter kar sakte hain."
            });

            steps.Add(new TutorialStep
            {
                Title = "Expenses",
                Description =
@"Project/Office expenses categorize karke add karein.
Reports me club ho kar aayenge."
            });

            steps.Add(new TutorialStep
            {
                Title = "Reports",
                Description =
@"Receipt Register, Customer Ledger, Allocation Summary, Date Filters.
Export/print options aasan hain."
            });

            steps.Add(new TutorialStep
            {
                Title = "Shortcuts",
                Description =
@"Productivity keys:
• Ctrl+F = Search
• Ctrl+A = Allocation picker
• ESC = Close dialog
• Enter = Confirm/Next"
            });

            steps.Add(new TutorialStep
            {
                Title = "Activation & Backup",
                Description =
@"First-run activation code required.
Daily backup/restore se data safe rakhein (prefer NVMe SSD)."
            });

            steps.Add(new TutorialStep
            {
                Title = "End",
                Description =
@"Tour complete ✅
Settings se tutorial dobara bhi open kar sakte hain."
            });
        }

        // --- Navigation ---
        private void ShowStep(int index)
        {
            if (index < 0 || index >= steps.Count) return;

            var step = steps[index];
            lblTitle.Text = step.Title;
            lblDescription.Text = step.Description;

            btnBack.Enabled = (index > 0);
            btnNext.Text = (index == steps.Count - 1) ? "Finish" : "Next";
        }

        private void BtnNext_Click(object sender, EventArgs e)
        {
            if (currentStep < steps.Count - 1)
            {
                currentStep++;
                ShowStep(currentStep);
            }
            else
            {
                Close();
            }
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            if (currentStep > 0)
            {
                currentStep--;
                ShowStep(currentStep);
            }
        }

        // Keyboard: ESC, Enter/Right = Next, Backspace/Left = Back
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                Close();
                return true;
            }
            if (keyData == Keys.Enter || keyData == Keys.Right)
            {
                BtnNext_Click(this, EventArgs.Empty);
                return true;
            }
            if (keyData == Keys.Back || keyData == Keys.Left)
            {
                BtnBack_Click(this, EventArgs.Empty);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    public sealed class TutorialStep
    {
        public string Title { get; set; }
        public string Description { get; set; }
    }
}
