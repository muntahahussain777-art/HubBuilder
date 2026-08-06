using HubBuilders.Model;
using System;
using System.Collections.Generic;
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
                LoadData();
            }
            catch (Exception ex)
            {
                ShowError("Form load failed.\n\n" + ex.Message);
            }
        }

        private void LoadData()
        {
            try
            {
                MainClass.EnsurePartnerMoneySchema();
                string q = (txtSearch?.Text ?? string.Empty).Trim();
                string qry;
                Dictionary<string, object> pars = null;

                if (string.IsNullOrWhiteSpace(q))
                {
                    qry = @"
SELECT pm.id AS dgvid,
       p.name AS dgvpartner,
       pm.txn_date AS dgvdate,
       pm.direction AS dgvdir,
       printf('%.2f', pm.amount) AS dgvamount,
       IFNULL(pm.category,'') AS dgvcategory,
       IFNULL(pm.mode,'') AS dgvmode,
       IFNULL(pm.note,'') AS dgvnote
FROM partner_money pm
JOIN Person p ON p.person_id = pm.partner_id
ORDER BY pm.txn_date DESC, pm.id DESC;";
                }
                else
                {
                    qry = @"
SELECT pm.id AS dgvid,
       p.name AS dgvpartner,
       pm.txn_date AS dgvdate,
       pm.direction AS dgvdir,
       printf('%.2f', pm.amount) AS dgvamount,
       IFNULL(pm.category,'') AS dgvcategory,
       IFNULL(pm.mode,'') AS dgvmode,
       IFNULL(pm.note,'') AS dgvnote
FROM partner_money pm
JOIN Person p ON p.person_id = pm.partner_id
WHERE p.name LIKE @q OR pm.category LIKE @q OR pm.note LIKE @q
   OR pm.direction LIKE @q OR CAST(pm.amount AS TEXT) LIKE @q
ORDER BY pm.txn_date DESC, pm.id DESC;";
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
                lb.Items.Add(dgvnote);

                MainClass.LoadData1(qry, guna2DataGridView1, lb, pars);
                guna2DataGridView1.ClearSelection();

                var bal = MainClass.ExecuteScalar(@"
SELECT
  COALESCE(SUM(CASE WHEN direction='In' THEN amount ELSE 0 END),0) -
  COALESCE(SUM(CASE WHEN direction='Out' THEN amount ELSE 0 END),0)
FROM partner_money;");
                decimal b = 0;
                try { b = Convert.ToDecimal(bal ?? 0); } catch { }
                label1.Text = "Partner Cash Book  |  Balance (In − Out): " + b.ToString("N2");
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
                LoadData();
            }
            catch (Exception ex)
            {
                ShowError("Open add dialog failed.\n\n" + ex.Message);
            }
        }

        public override void txtSearch_TextChanged(object sender, EventArgs e)
        {
            try { LoadData(); }
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
                    LoadData();
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
                            ShowInfo("Delete Successfully");
                            LoadData();
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
