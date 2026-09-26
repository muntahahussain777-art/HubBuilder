using HubBuilders.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;

namespace HubBuilders.View
{
    public partial class frmPartnerCashView : SampleView
    {
        public frmPartnerCashView()
        {
            InitializeComponent();
        }

        private void frmPartnerCashView_Load(object sender, EventArgs e)
        {
            try
            {
                MainClass.EnsurePartnerMoneySchema();
                label1.Text = "Partner Cash Book";
                GetData();
            }
            catch (Exception ex)
            {
                ShowError("Form load failed.\n\n" + ex.Message);
            }
        }

        /// <summary>SampleView LiveChanged / sync pull ke baad turant refresh.</summary>
        private void GetData()
        {
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                MainClass.EnsurePartnerMoneySchema();
                string q = (txtSearch?.Text ?? string.Empty).Trim();
                string qry;
                string totalsQry;
                Dictionary<string, object> pars = null;

                const string fromJoin = @"
FROM partner_money pm
LEFT JOIN Person p ON p.person_id = pm.partner_id
LEFT JOIN customer_plots cp ON cp.id = pm.customer_plot_id
LEFT JOIN customers c ON c.id = cp.customer_id
LEFT JOIN plots pl ON pl.id = cp.plot_id
LEFT JOIN societies s ON s.id = COALESCE(pm.society_id, pl.society_id)";

                if (string.IsNullOrWhiteSpace(q))
                {
                    qry = @"
SELECT pm.id AS dgvid,
       IFNULL(NULLIF(trim(p.name),''), 'Partner #' || pm.partner_id) AS dgvpartner,
       pm.txn_date AS dgvdate,
       pm.direction AS dgvdir,
       printf('%.2f', pm.amount) AS dgvamount,
       IFNULL(pm.category,'') AS dgvcategory,
       IFNULL(pm.mode,'') AS dgvmode,
       IFNULL(c.name,'') AS dgvcustomer,
       IFNULL(CAST(pl.plot_no AS TEXT),'') AS dgvplot,
       IFNULL(pm.note,'') AS dgvnote
" + fromJoin + @"
ORDER BY pm.txn_date DESC, pm.id DESC;";
                    totalsQry = @"
SELECT
  COALESCE(SUM(CASE WHEN trim(lower(ifnull(pm.direction,'')))='in' THEN ifnull(pm.amount,0) ELSE 0 END), 0) AS tin,
  COALESCE(SUM(CASE WHEN trim(lower(ifnull(pm.direction,'')))='out' THEN ifnull(pm.amount,0) ELSE 0 END), 0) AS tout
" + fromJoin + ";";
                }
                else
                {
                    qry = @"
SELECT pm.id AS dgvid,
       IFNULL(NULLIF(trim(p.name),''), 'Partner #' || pm.partner_id) AS dgvpartner,
       pm.txn_date AS dgvdate,
       pm.direction AS dgvdir,
       printf('%.2f', pm.amount) AS dgvamount,
       IFNULL(pm.category,'') AS dgvcategory,
       IFNULL(pm.mode,'') AS dgvmode,
       IFNULL(c.name,'') AS dgvcustomer,
       IFNULL(CAST(pl.plot_no AS TEXT),'') AS dgvplot,
       IFNULL(pm.note,'') AS dgvnote
" + fromJoin + @"
WHERE IFNULL(p.name,'') LIKE @q OR pm.category LIKE @q OR pm.note LIKE @q
   OR pm.direction LIKE @q OR CAST(pm.amount AS TEXT) LIKE @q
   OR IFNULL(c.name,'') LIKE @q OR IFNULL(c.mobile,'') LIKE @q
   OR CAST(IFNULL(pl.plot_no,'') AS TEXT) LIKE @q
   OR IFNULL(s.name,'') LIKE @q
   OR CAST(pm.partner_id AS TEXT) LIKE @q
ORDER BY pm.txn_date DESC, pm.id DESC;";
                    totalsQry = @"
SELECT
  COALESCE(SUM(CASE WHEN trim(lower(ifnull(pm.direction,'')))='in' THEN ifnull(pm.amount,0) ELSE 0 END), 0) AS tin,
  COALESCE(SUM(CASE WHEN trim(lower(ifnull(pm.direction,'')))='out' THEN ifnull(pm.amount,0) ELSE 0 END), 0) AS tout
" + fromJoin + @"
WHERE IFNULL(p.name,'') LIKE @q OR pm.category LIKE @q OR pm.note LIKE @q
   OR pm.direction LIKE @q OR CAST(pm.amount AS TEXT) LIKE @q
   OR IFNULL(c.name,'') LIKE @q OR IFNULL(c.mobile,'') LIKE @q
   OR CAST(IFNULL(pl.plot_no,'') AS TEXT) LIKE @q
   OR IFNULL(s.name,'') LIKE @q
   OR CAST(pm.partner_id AS TEXT) LIKE @q;";
                    pars = new Dictionary<string, object> { { "@q", "%" + q + "%" } };
                }

                var lb = new ListBox();
                lb.Items.Add(dgvid);
                lb.Items.Add(dgvpartner);
                lb.Items.Add(dgvdate);
                lb.Items.Add(dgvdir);
                lb.Items.Add(dgvamount);
                lb.Items.Add(dgvcategory);
                lb.Items.Add(dgvmode);
                lb.Items.Add(dgvcustomer);
                lb.Items.Add(dgvplot);
                lb.Items.Add(dgvnote);

                MainClass.LoadData1(qry, guna2DataGridView1, lb, pars);
                guna2DataGridView1.ClearSelection();

                decimal tin = 0, tout = 0;
                var dtTotals = pars == null
                    ? MainClass.GetData(totalsQry)
                    : MainClass.ExecuteSelectQuery1(totalsQry, pars);
                if (dtTotals != null && dtTotals.Rows.Count > 0)
                {
                    var tr = dtTotals.Rows[0];
                    try { tin = Convert.ToDecimal(tr["tin"]); } catch { }
                    try { tout = Convert.ToDecimal(tr["tout"]); } catch { }
                }
                label1.Text = "Partner Cash Book  |  Total In: " + tin.ToString("N2")
                    + "  |  Total Out: " + tout.ToString("N2")
                    + "  |  Balance (In − Out): " + (tin - tout).ToString("N2");
            }
            catch (Exception ex)
            {
                ShowError("Fetching partner cash failed.\n\n" + ex.Message);
            }
        }

        public override void btnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                using (var f = new frmPartnerCashAdd())
                {
                    MainClass.BlurBackground(f);
                }
                GetData();
            }
            catch (Exception ex)
            {
                ShowError("Open add dialog failed.\n\n" + ex.Message);
            }
        }

        public override void txtSearch_TextChanged(object sender, EventArgs e)
        {
            try { GetData(); }
            catch (Exception ex) { ShowError("Search update failed.\n\n" + ex.Message); }
        }

        private void guna2DataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || guna2DataGridView1.CurrentCell == null) return;

                var col = guna2DataGridView1.CurrentCell.OwningColumn?.Name;
                if (string.IsNullOrEmpty(col)) return;

                if (col == "dgvedit")
                {
                    if (!TryGetSelectedId(out long id))
                    {
                        ShowError("Invalid selection. ID missing.");
                        return;
                    }

                    using (var frm = new frmPartnerCashAdd { EditId = id })
                    {
                        MainClass.BlurBackground(frm);
                    }
                    GetData();
                    return;
                }

                if (col == "dgvdel")
                {
                    if (!TryGetSelectedId(out long id))
                    {
                        ShowError("Invalid selection. ID missing.");
                        return;
                    }

                    if (!AskYesNo("Are you sure you want to delete?", "Delete Confirmation")) return;

                    try
                    {
                        int res = MainClass.ExecuteNonQuery(
                            "DELETE FROM partner_money WHERE id=@id;",
                            new Dictionary<string, object> { { "@id", id } });

                        if (res > 0)
                        {
                            try { HubSyncService.NotifyLocalWrite(); } catch { }
                            ShowInfo("Delete Successfully");
                            GetData();
                        }
                        else
                        {
                            ShowError("Delete failed ya record missing hai.");
                        }
                    }
                    catch (Exception exDel)
                    {
                        ShowError("Delete failed.\n\n" + exDel.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("Grid operation failed.\n\n" + ex.Message);
            }
        }

        private void guna2DataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            try { MainClass.SrNo(guna2DataGridView1); }
            catch { }
        }

        private bool TryGetSelectedId(out long id)
        {
            id = 0;
            try
            {
                var v = guna2DataGridView1.CurrentRow?.Cells["dgvid"]?.Value;
                if (v == null) return false;
                id = Convert.ToInt64(v);
                return id > 0;
            }
            catch { id = 0; return false; }
        }

        private void ShowError(string message)
        {
            try { new ErrorFormMessage(message, "Error").ShowDialog(); }
            catch { MessageBox.Show(message); }
        }

        private void ShowInfo(string message)
        {
            try { new CustomeMessage(message, "Hub Builders").ShowDialog(); }
            catch { MessageBox.Show(message); }
        }

        private bool AskYesNo(string message, string title)
        {
            return MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }
    }
}
