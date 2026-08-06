using Guna.UI2.WinForms;
using Microsoft.VisualBasic;
using HubBuilders; // MainClass
using HubBuilders.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace HubBuilders.View
{
    public partial class CreditFormView : SampleView
    {
        public CreditFormView()
        {
            InitializeComponent();
        }

        private void CreditFormView_Load(object sender, EventArgs e)
        {
            try { LoadData(); }
            catch (Exception ex) { ShowError("Form load failed.\n\n" + ex.Message); }
        }

        private void LoadData()
        {
            try
            {
                string q = (txtSearch?.Text ?? string.Empty).Trim();
                string qry;
                Dictionary<string, object> pars = null;

                // NOTE: DataGridView me columns ke NAME yeh hone chahiye:
                // dgvid, dgvSociety, dgvOwner, dgvDeal, dgvPartner, dgvAmount, dgvDate, dgvNote, dgvedit, dgvdel

                if (string.IsNullOrWhiteSpace(q))
                {
                    qry = @"
SELECT 
    c.credit_id                                         AS dgvid,
    s.name                                              AS dgvSociety,
    o.name                                              AS dgvOwner,
    (d.deal_date || ' | ' || printf('%.2f', d.amount))  AS dgvDeal,
    p.name                                              AS dgvPartner,
    c.amount                                            AS dgvAmount,
    c.pay_date                                          AS dgvDate,
    c.note                                              AS dgvNote
FROM Credit c
JOIN Deal d      ON d.deal_id   = c.deal_id
JOIN societies s ON s.id        = d.society_id
JOIN Person o    ON o.person_id = d.owner_id      -- owner
JOIN Person p    ON p.person_id = c.partner_id    -- partner
ORDER BY c.credit_id DESC;";
                }
                else
                {
                    qry = @"
SELECT 
    c.credit_id                                         AS dgvid,
    s.name                                              AS dgvSociety,
    o.name                                              AS dgvOwner,
    (d.deal_date || ' | ' || printf('%.2f', d.amount))  AS dgvDeal,
    p.name                                              AS dgvPartner,
    c.amount                                            AS dgvAmount,
    c.pay_date                                          AS dgvDate,
    c.note                                              AS dgvNote
FROM Credit c
JOIN Deal d      ON d.deal_id   = c.deal_id
JOIN societies s ON s.id        = d.society_id
JOIN Person o    ON o.person_id = d.owner_id
JOIN Person p    ON p.person_id = c.partner_id
WHERE s.name LIKE @q OR o.name LIKE @q OR p.name LIKE @q OR c.note LIKE @q
ORDER BY c.credit_id DESC;";
                    pars = new Dictionary<string, object> { { "@q", "%" + q + "%" } };
                }

                var lb = new ListBox();
                lb.Items.Add(dgvid);
                lb.Items.Add(dgvSociety);
                lb.Items.Add(dgvOwner);
                lb.Items.Add(dgvDeal);
                lb.Items.Add(dgvPartner);
                lb.Items.Add(dgvAmount);
                lb.Items.Add(dgvDate);
                lb.Items.Add(dgvNote);

                MainClass.LoadData1(qry, guna2DataGridView1, lb, pars);
                guna2DataGridView1.ClearSelection();
            }
            catch (Exception ex)
            {
                ShowError("Fetching credits failed.\n\n" + ex.Message);
            }
        }

        public override void btnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                using (var f = new CreditFormAdd())
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
                    if (!TryGetSelectedId(out int id)) { ShowError("Invalid selection. ID missing."); return; }

                    using (var frm = new CreditFormAdd())
                    {
                        frm.LoadForEdit(id);
                        MainClass.BlurBackground(frm);
                    }
                    LoadData();
                    return;
                }

                if (col == "dgvdel")
                {
                    if (!TryGetSelectedId(out int id)) { ShowError("Invalid selection. ID missing."); return; }
                    if (!AskYesNo("Delete this credit entry?", "Delete Confirmation")) return;

                    int res = MainClass.ExecuteNonQuery(
                        "DELETE FROM Credit WHERE credit_id=@id;",
                        new Dictionary<string, object> { { "@id", id } });

                    if (res > 0) { ShowInfo("Delete successful."); LoadData(); }
                    else { ShowError("Delete failed ya record missing hai."); }
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
            catch (Exception ex) { ShowError("Row numbering failed.\n\n" + ex.Message); }
        }

        // -------- Helpers --------
        private bool TryGetSelectedId(out int id)
        {
            id = 0;
            try
            {
                var v = guna2DataGridView1.CurrentRow?.Cells["dgvid"]?.Value;
                if (v == null) return false;
                if (v is int i) { id = i; return true; }
                return int.TryParse(Convert.ToString(v), out id);
            }
            catch { id = 0; return false; }
        }

        private void ShowError(string message)
        {
            try { using (var err = new ErrorFormMessage(message, "Error")) err.ShowDialog(); }
            catch { MessageBox.Show(message, "Error"); }
        }

        private void ShowInfo(string message)
        {
            try { using (var ok = new CustomeMessage(message, "RealEstate Builders")) ok.ShowDialog(); }
            catch { MessageBox.Show(message, "Info"); }
        }

        private bool AskYesNo(string message, string title)
        {
            try { using (var ask = new YesOrNoMessage(message, title)) return ask.ShowDialog() == DialogResult.Yes; }
            catch { return MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes; }
        }
    }
}
