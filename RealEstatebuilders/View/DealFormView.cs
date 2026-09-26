using Microsoft.VisualBasic;
using HubBuilders; // MainClass
using HubBuilders.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace HubBuilders.View
{
    public partial class DealFormView : SampleView
    {
        public DealFormView()
        {
            InitializeComponent();
        }

        private void DealFormView_Load(object sender, EventArgs e)
        {
            try
            {
                /* StaffAccess ApplyStaffAccessUi handles Add/Edit/Delete visibility */
                LoadData(); }
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
                // dgvid, dgvSociety, dgvOwner, dgvAmount, dgvDate, dgvNote, dgvedit, dgvdel

                if (string.IsNullOrWhiteSpace(q))
                {
                    qry = @"
SELECT 
    d.deal_id       AS dgvid,
    s.name          AS dgvSociety,
    p.name          AS dgvOwner,
    d.amount        AS dgvAmount,
    d.deal_date     AS dgvDate,
    d.note          AS dgvNote
FROM Deal d
JOIN societies s ON s.id = d.society_id
JOIN Person    p ON p.person_id = d.owner_id
ORDER BY d.deal_id DESC;";
                }
                else
                {
                    qry = @"
SELECT 
    d.deal_id       AS dgvid,
    s.name          AS dgvSociety,
    p.name          AS dgvOwner,
    d.amount        AS dgvAmount,
    d.deal_date     AS dgvDate,
    d.note          AS dgvNote
FROM Deal d
JOIN societies s ON s.id = d.society_id
JOIN Person    p ON p.person_id = d.owner_id
WHERE s.name LIKE @q OR p.name LIKE @q OR d.note LIKE @q
ORDER BY d.deal_id DESC;";
                    pars = new Dictionary<string, object> { { "@q", "%" + q + "%" } };
                }

                var lb = new ListBox();
                lb.Items.Add(dgvid);
                lb.Items.Add(dgvSociety);
                lb.Items.Add(dgvOwner);
                lb.Items.Add(dgvAmount);
                lb.Items.Add(dgvDate);
                lb.Items.Add(dgvNote);

                MainClass.LoadData1(qry, guna2DataGridView1, lb, pars);
                guna2DataGridView1.ClearSelection();
            }
            catch (Exception ex)
            {
                ShowError("Fetching deals failed.\n\n" + ex.Message);
            }
        }

        public override void btnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                using (var f = new DealFormAdd())
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
                    // ADMIN-ONLY
                    if (StaffAccess.Deny(StaffAccess.ModuleForForm(this), "edit")) return;

                    if (!TryGetSelectedId(out int id)) { ShowError("Invalid selection. ID missing."); return; }

                    using (var frm = new DealFormAdd())
                    {
                        frm.LoadForEdit(id);
                        MainClass.BlurBackground(frm);
                    }
                    LoadData();
                    return;
                }

                if (col == "dgvdel")
                {
                    // ADMIN-ONLY
                    if (StaffAccess.Deny(StaffAccess.ModuleForForm(this), "delete")) return;

                    if (!TryGetSelectedId(out int id)) { ShowError("Invalid selection. ID missing."); return; }
                    if (!AskYesNo("Delete this deal? Linked credits (if any) will also be deleted.", "Delete Confirmation")) return;

                    int res = MainClass.ExecuteNonQuery(
                        "DELETE FROM Deal WHERE deal_id=@id;",
                        new Dictionary<string, object> { { "@id", id } });

                    if (res > 0) { ShowInfo("Delete successful."); LoadData(); }
                    else { ShowError("Delete failed or record missing."); }
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
