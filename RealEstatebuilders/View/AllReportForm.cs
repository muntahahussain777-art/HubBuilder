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
    }
}
