using HubBuilders.Report_Form;
using System;
using System.Windows.Forms;

namespace HubBuilders.View
{
    public partial class AllReportForm : Sample
    {
        public AllReportForm()
        {
            InitializeComponent();
            AddVipCustomerAllPlotsReceiptsButton();
            AddDuesWhatsAppListButton();
            AddReceiptsMinusPartnerCreditButton();
            AddAvailablePlotsReportButton();
            AddPlotStatusOwnerReportButton();
            AddStaffSalaryVipMessageButton();
            AddPlotAllocationVipWhatsAppButton();
            AddExpenseReportButton();
            AddVipAllocateTotalsButton();
            AddInstallmentDetectionButton();
            AddMonthCollectionVipButton();
            AddVipBusinessAnalysisButton();
            AddRoughInstallmentEstimateButton();
            EnableReportFormScroll();
        }

        /// <summary>Neeche Backup And Restore tak scroll ho sake.</summary>
        private void EnableReportFormScroll()
        {
            try
            {
                AutoScroll = true;
                int bottom = 0;
                if (groupBox2 != null) bottom = Math.Max(bottom, groupBox2.Bottom);
                if (groupBox1 != null) bottom = Math.Max(bottom, groupBox1.Bottom);
                AutoScrollMinSize = new System.Drawing.Size(0, bottom + 24);
            }
            catch { }
        }

        private static void OpenReport(Func<Form> create)
        {
            HubBuilders.AppSafety.Run(() =>
            {
                using (Form frm = create())
                    frm.ShowDialog();
            });
        }

        private void guna2Button1_Click(object sender, EventArgs e)
        {
            OpenReport(() => new frmCompleteCustomer());
        }

        private void totalSalesSUmmary_Click(object sender, EventArgs e)
        {
            OpenReport(() => new frmCustomerPlotBookingAllocationlist());
        }

        private void btnDues_Click(object sender, EventArgs e)
        {
            OpenReport(() => new frmDuesperPlot());
        }

        private void btnAgingReport_Click(object sender, EventArgs e)
        {
            OpenReport(() => new frmAging());
        }

        private void categorywisesalesreport_Click(object sender, EventArgs e)
        {
            OpenReport(() => new frmCustomerTotalsRollUp());
        }

        private void btnReceiptsDetail_Click(object sender, EventArgs e)
        {
            OpenReport(() => new frmReciptDetails());
        }

        private void guna2Button7_Click(object sender, EventArgs e)
        {
            OpenReport(() => new frmDailyWiseReport());
        }

        private void btnmonthlywisereport_Click(object sender, EventArgs e)
        {
            OpenReport(() => new frmmonthlywise());
        }

        private void guna2Button6_Click(object sender, EventArgs e)
        {
            OpenReport(() => new frmCosgsExpense_Receipts());
        }

        private void btnInventoryStatus_Click(object sender, EventArgs e)
        {
            OpenReport(() => new frmInventoryStatusAvailableHoldSold());
        }

        private void btnProfitLost_Click(object sender, EventArgs e)
        {
            OpenReport(() => new frmProfitAndLoss());
        }

        private void btnCustomer_Click(object sender, EventArgs e)
        {
            OpenReport(() => new Backup_RestoreForm());
        }

        private void btnBroker_Click(object sender, EventArgs e)
        {
            OpenReport(() => new frmBrokerCommissionform());
        }

        private void guna2Button2_Click(object sender, EventArgs e)
        {
            OpenReport(() => new frmSeprateCustomer());
        }

        private void btnApplicationForm_Click(object sender, EventArgs e)
        {
            OpenReport(() => new frmApplicationForm());
        }

        private void btnApplicationFormBlank_Click(object sender, EventArgs e)
        {
            OpenReport(() => new frmApplicationFormBlank());
        }

        private void btnTemporaryAllotmentLetter_Click(object sender, EventArgs e)
        {
            OpenReport(() => new frmTemporaryAllotmentLetter());
        }

        private void btnTemporaryAllotmentLetterBlank_Click(object sender, EventArgs e)
        {
            OpenReport(() => new frmTemporaryAllotmentLetterBlank());
        }

        private void btnLasbelaVillageReceipt_Click(object sender, EventArgs e)
        {
            OpenReport(() => new frmLasbelaVillageReceipt());
        }

        private void btnLasbelaMarketBhawani_Click(object sender, EventArgs e)
        {
            OpenReport(() => new frmLasbelaMarketBhawaniReceipt());
        }

        private void btnVipCustomerReceiptStatement_Click(object sender, EventArgs e)
        {
            OpenReport(() => new frmVipCustomerReceiptStatement());
        }

        private void btnVipCompleteDuesTracker_Click(object sender, EventArgs e)
        {
            OpenReport(() => new frmVipCompleteDuesTracker());
        }

        private void btnVipSocietyProfitClosing_Click(object sender, EventArgs e)
        {
            OpenReport(() => new frmVipSocietyProfitClosing());
        }

        private void btnVipBrokerDetails_Click(object sender, EventArgs e)
        {
            OpenReport(() => new frmVipBrokerDetails());
        }

        private void btnVipDuesAging_Click(object sender, EventArgs e)
        {
            OpenReport(() => new frmVipDuesAging());
        }

        private void btnVipPartnerCashBook_Click(object sender, EventArgs e)
        {
            OpenReport(() => new frmVipPartnerCashBook());
        }

        private void AddVipCustomerAllPlotsReceiptsButton()
        {
            try
            {
                if (groupBox1 == null) return;
                if (groupBox1.Controls.Find("btnVipCustomerAllPlotsReceipts", true).Length > 0) return;

                var btn = new Guna.UI2.WinForms.Guna2Button();
                btn.Name = "btnVipCustomerAllPlotsReceipts";
                btn.Animated = true;
                btn.AutoRoundedCorners = true;
                btn.BorderRadius = 20;
                btn.CheckedState.Parent = btn;
                btn.CustomImages.Parent = btn;
                btn.ShadowDecoration.Parent = btn;
                btn.FillColor = System.Drawing.Color.FromArgb(10, 40, 100);
                btn.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
                btn.ForeColor = System.Drawing.Color.White;
                btn.HoverState.Parent = btn;
                btn.Location = new System.Drawing.Point(510, 511);
                btn.Size = new System.Drawing.Size(477, 43);
                btn.Text = "Customer Plots Receipts (VIP)";
                btn.Click += btnVipCustomerAllPlotsReceipts_Click;

                groupBox1.Controls.Add(btn);
                btn.BringToFront();
            }
            catch
            {
                // ignore; report can still be opened by other means
            }
        }

        private void btnVipCustomerAllPlotsReceipts_Click(object sender, EventArgs e)
        {
            OpenReport(() => new frmVipCustomerAllPlotsReceipts());
        }

        private void AddDuesWhatsAppListButton()
        {
            try
            {
                if (groupBox1 == null) return;
                if (groupBox1.Controls.Find("btnDuesWhatsAppList", true).Length > 0) return;

                var btn = new Guna.UI2.WinForms.Guna2Button();
                btn.Name = "btnDuesWhatsAppList";
                btn.Animated = true;
                btn.AutoRoundedCorners = true;
                btn.BorderRadius = 20;
                btn.CheckedState.Parent = btn;
                btn.CustomImages.Parent = btn;
                btn.ShadowDecoration.Parent = btn;
                btn.FillColor = System.Drawing.Color.FromArgb(18, 140, 126);
                btn.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
                btn.ForeColor = System.Drawing.Color.White;
                btn.HoverState.Parent = btn;
                btn.Location = new System.Drawing.Point(15, 560);
                btn.Size = new System.Drawing.Size(480, 43);
                btn.Text = "Dues WhatsApp Reminder";
                btn.Click += btnDuesWhatsAppList_Click;

                groupBox1.Controls.Add(btn);
                btn.BringToFront();
            }
            catch
            {
            }
        }

        private void btnDuesWhatsAppList_Click(object sender, EventArgs e)
        {
            OpenReport(() => new frmDuesWhatsAppList());
        }

        private void AddReceiptsMinusPartnerCreditButton()
        {
            try
            {
                if (groupBox1 == null) return;
                if (groupBox1.Controls.Find("btnReceiptsMinusPartnerCredit", true).Length > 0) return;

                var btn = new Guna.UI2.WinForms.Guna2Button();
                btn.Name = "btnReceiptsMinusPartnerCredit";
                btn.Animated = true;
                btn.AutoRoundedCorners = true;
                btn.BorderRadius = 20;
                btn.CheckedState.Parent = btn;
                btn.CustomImages.Parent = btn;
                btn.ShadowDecoration.Parent = btn;
                btn.FillColor = System.Drawing.Color.FromArgb(10, 40, 100);
                btn.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
                btn.ForeColor = System.Drawing.Color.White;
                btn.HoverState.Parent = btn;
                btn.Location = new System.Drawing.Point(510, 560);
                btn.Size = new System.Drawing.Size(477, 43);
                btn.Text = "Receipts − Partner Credit";
                btn.Click += (s, e) => OpenReport(() => new frmReceiptsMinusPartnerCredit());
                groupBox1.Controls.Add(btn);
                btn.BringToFront();
            }
            catch { }
        }

        private void AddAvailablePlotsReportButton()
        {
            try
            {
                if (groupBox1 == null) return;
                if (groupBox1.Controls.Find("btnAvailablePlotsReport", true).Length > 0) return;

                var btn = new Guna.UI2.WinForms.Guna2Button();
                btn.Name = "btnAvailablePlotsReport";
                btn.Animated = true;
                btn.AutoRoundedCorners = true;
                btn.BorderRadius = 20;
                btn.CheckedState.Parent = btn;
                btn.CustomImages.Parent = btn;
                btn.ShadowDecoration.Parent = btn;
                btn.FillColor = System.Drawing.Color.FromArgb(184, 148, 68);
                btn.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
                btn.ForeColor = System.Drawing.Color.White;
                btn.HoverState.Parent = btn;
                btn.Location = new System.Drawing.Point(15, 610);
                btn.Size = new System.Drawing.Size(480, 43);
                btn.Text = "Available Plots Report";
                btn.Click += (s, e) => OpenReport(() => new frmAvailablePlotsReport());
                groupBox1.Controls.Add(btn);
                btn.BringToFront();
            }
            catch { }
        }

        private void AddPlotStatusOwnerReportButton()
        {
            try
            {
                if (groupBox1 == null) return;
                if (groupBox1.Controls.Find("btnPlotStatusOwnerReport", true).Length > 0) return;

                var btn = new Guna.UI2.WinForms.Guna2Button();
                btn.Name = "btnPlotStatusOwnerReport";
                btn.Animated = true;
                btn.AutoRoundedCorners = true;
                btn.BorderRadius = 20;
                btn.CheckedState.Parent = btn;
                btn.CustomImages.Parent = btn;
                btn.ShadowDecoration.Parent = btn;
                btn.FillColor = System.Drawing.Color.FromArgb(0, 120, 90);
                btn.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
                btn.ForeColor = System.Drawing.Color.White;
                btn.HoverState.Parent = btn;
                btn.Location = new System.Drawing.Point(510, 610);
                btn.Size = new System.Drawing.Size(477, 43);
                btn.Text = "Plot Status Report (Owner)";
                btn.Click += (s, e) => OpenReport(() => new frmPlotStatusOwnerReport());
                groupBox1.Controls.Add(btn);
                btn.BringToFront();
            }
            catch { }
        }

        private void AddStaffSalaryVipMessageButton()
        {
            try
            {
                if (groupBox1 == null) return;
                if (groupBox1.Controls.Find("btnVipStaffSalaryMessage", true).Length > 0) return;

                try
                {
                    groupBox1.Height = 718;
                    if (groupBox2 != null)
                    {
                        groupBox2.Top = 738;
                        int need = groupBox2.Bottom + 16;
                        if (ClientSize.Height < need)
                            ClientSize = new System.Drawing.Size(ClientSize.Width, need);
                    }
                }
                catch { }

                var btn = new Guna.UI2.WinForms.Guna2Button();
                btn.Name = "btnVipStaffSalaryMessage";
                btn.Animated = true;
                btn.AutoRoundedCorners = true;
                btn.BorderRadius = 20;
                btn.CheckedState.Parent = btn;
                btn.CustomImages.Parent = btn;
                btn.ShadowDecoration.Parent = btn;
                btn.FillColor = System.Drawing.Color.FromArgb(18, 140, 126);
                btn.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
                btn.ForeColor = System.Drawing.Color.White;
                btn.HoverState.Parent = btn;
                btn.Location = new System.Drawing.Point(15, 660);
                btn.Size = new System.Drawing.Size(972, 43);
                btn.Text = "Staff Salary Message (VIP)";
                btn.Click += (s, e) => OpenReport(() => new frmVipStaffSalaryMessage());
                groupBox1.Controls.Add(btn);
                btn.BringToFront();
            }
            catch { }
        }

        private void AddPlotAllocationVipWhatsAppButton()
        {
            try
            {
                if (groupBox1 == null) return;
                if (groupBox1.Controls.Find("btnVipPlotAllocationWhatsApp", true).Length > 0) return;

                try
                {
                    groupBox1.Height = 820;
                    if (groupBox2 != null)
                    {
                        groupBox2.Top = 840;
                        int need = groupBox2.Bottom + 16;
                        if (ClientSize.Height < need)
                            ClientSize = new System.Drawing.Size(ClientSize.Width, need);
                    }
                }
                catch { }

                var btn = new Guna.UI2.WinForms.Guna2Button();
                btn.Name = "btnVipPlotAllocationWhatsApp";
                btn.Animated = true;
                btn.AutoRoundedCorners = true;
                btn.BorderRadius = 20;
                btn.CheckedState.Parent = btn;
                btn.CustomImages.Parent = btn;
                btn.ShadowDecoration.Parent = btn;
                btn.FillColor = System.Drawing.Color.FromArgb(18, 140, 126);
                btn.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
                btn.ForeColor = System.Drawing.Color.White;
                btn.HoverState.Parent = btn;
                btn.Location = new System.Drawing.Point(15, 710);
                btn.Size = new System.Drawing.Size(972, 43);
                btn.Text = "Plot Allocation Message (VIP)";
                btn.Click += (s, e) => OpenReport(() => new frmVipPlotAllocationWhatsApp());
                groupBox1.Controls.Add(btn);
                btn.BringToFront();
            }
            catch { }
        }

        private void AddExpenseReportButton()
        {
            try
            {
                if (groupBox1 == null) return;
                if (groupBox1.Controls.Find("btnExpenseReport", true).Length > 0) return;

                try
                {
                    groupBox1.Height = 870;
                    if (groupBox2 != null)
                    {
                        groupBox2.Top = 890;
                        int need = groupBox2.Bottom + 16;
                        if (ClientSize.Height < need)
                            ClientSize = new System.Drawing.Size(ClientSize.Width, need);
                    }
                }
                catch { }

                var btn = new Guna.UI2.WinForms.Guna2Button();
                btn.Name = "btnExpenseReport";
                btn.Animated = true;
                btn.AutoRoundedCorners = true;
                btn.BorderRadius = 20;
                btn.CheckedState.Parent = btn;
                btn.CustomImages.Parent = btn;
                btn.ShadowDecoration.Parent = btn;
                btn.FillColor = System.Drawing.Color.FromArgb(10, 40, 100);
                btn.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
                btn.ForeColor = System.Drawing.Color.White;
                btn.HoverState.Parent = btn;
                btn.Location = new System.Drawing.Point(15, 760);
                btn.Size = new System.Drawing.Size(972, 43);
                btn.Text = "Expense Report";
                btn.Click += (s, e) => OpenReport(() => new frmExpenseReport());
                groupBox1.Controls.Add(btn);
                btn.BringToFront();
            }
            catch { }
        }

        private void AddVipAllocateTotalsButton()
        {
            try
            {
                if (groupBox1 == null) return;
                if (groupBox1.Controls.Find("btnVipAllocateTotals", true).Length > 0) return;

                try
                {
                    groupBox1.Height = 920;
                    if (groupBox2 != null)
                    {
                        groupBox2.Top = 940;
                        int need = groupBox2.Bottom + 16;
                        if (ClientSize.Height < need)
                            ClientSize = new System.Drawing.Size(ClientSize.Width, need);
                    }
                }
                catch { }

                var btn = new Guna.UI2.WinForms.Guna2Button();
                btn.Name = "btnVipAllocateTotals";
                btn.Animated = true;
                btn.AutoRoundedCorners = true;
                btn.BorderRadius = 20;
                btn.CheckedState.Parent = btn;
                btn.CustomImages.Parent = btn;
                btn.ShadowDecoration.Parent = btn;
                btn.FillColor = System.Drawing.Color.FromArgb(10, 40, 100);
                btn.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
                btn.ForeColor = System.Drawing.Color.White;
                btn.HoverState.Parent = btn;
                btn.Location = new System.Drawing.Point(15, 810);
                btn.Size = new System.Drawing.Size(972, 43);
                btn.Text = "Allocate Totals (VIP)";
                btn.Click += (s, e) => OpenReport(() => new frmVipAllocateTotals());
                groupBox1.Controls.Add(btn);
                btn.BringToFront();
            }
            catch { }
        }

        private void AddInstallmentDetectionButton()
        {
            try
            {
                if (groupBox1 == null) return;
                if (groupBox1.Controls.Find("btnInstallmentDetection", true).Length > 0) return;

                try
                {
                    groupBox1.Height = 970;
                    if (groupBox2 != null)
                    {
                        groupBox2.Top = 990;
                        int need = groupBox2.Bottom + 16;
                        if (ClientSize.Height < need)
                            ClientSize = new System.Drawing.Size(ClientSize.Width, need);
                    }
                }
                catch { }

                var btn = new Guna.UI2.WinForms.Guna2Button();
                btn.Name = "btnInstallmentDetection";
                btn.Animated = true;
                btn.AutoRoundedCorners = true;
                btn.BorderRadius = 20;
                btn.CheckedState.Parent = btn;
                btn.CustomImages.Parent = btn;
                btn.ShadowDecoration.Parent = btn;
                btn.FillColor = System.Drawing.Color.FromArgb(10, 40, 100);
                btn.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
                btn.ForeColor = System.Drawing.Color.White;
                btn.HoverState.Parent = btn;
                btn.Location = new System.Drawing.Point(15, 860);
                btn.Size = new System.Drawing.Size(972, 43);
                btn.Text = "Installment Detection (VIP)";
                btn.Click += (s, e) => OpenReport(() => new frmInstallmentDetectionReport());
                groupBox1.Controls.Add(btn);
                btn.BringToFront();
            }
            catch { }
        }

        private void AddMonthCollectionVipButton()
        {
            try
            {
                if (groupBox1 == null) return;
                if (groupBox1.Controls.Find("btnMonthCollectionVip", true).Length > 0) return;

                try
                {
                    groupBox1.Height = 1020;
                    if (groupBox2 != null)
                    {
                        groupBox2.Top = 1040;
                        int need = groupBox2.Bottom + 16;
                        if (ClientSize.Height < need)
                            ClientSize = new System.Drawing.Size(ClientSize.Width, need);
                    }
                }
                catch { }

                var btn = new Guna.UI2.WinForms.Guna2Button();
                btn.Name = "btnMonthCollectionVip";
                btn.Animated = true;
                btn.AutoRoundedCorners = true;
                btn.BorderRadius = 20;
                btn.CheckedState.Parent = btn;
                btn.CustomImages.Parent = btn;
                btn.ShadowDecoration.Parent = btn;
                btn.FillColor = System.Drawing.Color.FromArgb(10, 40, 100);
                btn.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
                btn.ForeColor = System.Drawing.Color.White;
                btn.HoverState.Parent = btn;
                btn.Location = new System.Drawing.Point(15, 910);
                btn.Size = new System.Drawing.Size(972, 43);
                btn.Text = "Month Collection (VIP)";
                btn.Click += (s, e) => OpenReport(() => new frmMonthCollectionVip());
                groupBox1.Controls.Add(btn);
                btn.BringToFront();
            }
            catch { }
        }

        private void AddVipBusinessAnalysisButton()
        {
            try
            {
                if (groupBox1 == null) return;
                if (groupBox1.Controls.Find("btnVipBusinessAnalysis", true).Length > 0) return;

                try
                {
                    groupBox1.Height = 1070;
                    if (groupBox2 != null)
                    {
                        groupBox2.Top = 1090;
                        int need = groupBox2.Bottom + 16;
                        if (ClientSize.Height < need)
                            ClientSize = new System.Drawing.Size(ClientSize.Width, need);
                    }
                }
                catch { }

                var btn = new Guna.UI2.WinForms.Guna2Button();
                btn.Name = "btnVipBusinessAnalysis";
                btn.Animated = true;
                btn.AutoRoundedCorners = true;
                btn.BorderRadius = 20;
                btn.CheckedState.Parent = btn;
                btn.CustomImages.Parent = btn;
                btn.ShadowDecoration.Parent = btn;
                btn.FillColor = System.Drawing.Color.FromArgb(10, 40, 100);
                btn.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
                btn.ForeColor = System.Drawing.Color.White;
                btn.HoverState.Parent = btn;
                btn.Location = new System.Drawing.Point(15, 960);
                btn.Size = new System.Drawing.Size(972, 43);
                btn.Text = "Complete Business Analysis (VIP)";
                btn.Click += (s, e) => OpenReport(() => new frmVipBusinessAnalysis());
                groupBox1.Controls.Add(btn);
                btn.BringToFront();
            }
            catch { }
        }

        private void AddRoughInstallmentEstimateButton()
        {
            try
            {
                if (groupBox1 == null) return;
                if (groupBox1.Controls.Find("btnRoughInstallmentEstimate", true).Length > 0) return;

                try
                {
                    groupBox1.Height = 1120;
                    if (groupBox2 != null)
                    {
                        groupBox2.Top = 1140;
                        int need = groupBox2.Bottom + 16;
                        if (ClientSize.Height < need)
                            ClientSize = new System.Drawing.Size(ClientSize.Width, need);
                    }
                }
                catch { }

                var btn = new Guna.UI2.WinForms.Guna2Button();
                btn.Name = "btnRoughInstallmentEstimate";
                btn.Animated = true;
                btn.AutoRoundedCorners = true;
                btn.BorderRadius = 20;
                btn.CheckedState.Parent = btn;
                btn.CustomImages.Parent = btn;
                btn.ShadowDecoration.Parent = btn;
                btn.FillColor = System.Drawing.Color.FromArgb(10, 40, 100);
                btn.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
                btn.ForeColor = System.Drawing.Color.White;
                btn.HoverState.Parent = btn;
                btn.Location = new System.Drawing.Point(15, 1010);
                btn.Size = new System.Drawing.Size(972, 43);
                btn.Text = "Rough Installment Estimate (VIP)";
                btn.Click += (s, e) => OpenReport(() => new frmRoughInstallmentEstimate());
                groupBox1.Controls.Add(btn);
                btn.BringToFront();
            }
            catch { }
        }
    }
}
