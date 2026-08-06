using HubBuilders.Model;
using HubBuilders; // MainClass
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace HubBuilders.View
{
    public partial class frmOwnerFormView : SampleView
    {
        public frmOwnerFormView()
        {
            InitializeComponent();
        }

        private void frmOwnerFormView_Load(object sender, EventArgs e)
        {
            try
            {
                if (MainClass.LoggedInRole != "Admin")
                {
                    btnAdd.Enabled = false;  // Add button disable
                    guna2DataGridView1.Columns["dgvedit"].Visible = false; // Edit Button Hide
                    guna2DataGridView1.Columns["dgvdel"].Visible = false;  // Delete Button Hide
                }
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
                // dgvid, dgvname, dgvphone, dgvnote, dgvdate, dgvedit, dgvdel

                if (string.IsNullOrWhiteSpace(q))
                {
                    qry = @"
SELECT 
    person_id   AS dgvid,
    name        AS dgvname,
    phone       AS dgvphone,
    note        AS dgvnote,
    created_on  AS dgvdate
FROM Person
WHERE is_owner = 1
ORDER BY person_id DESC;";
                }
                else
                {
                    qry = @"
SELECT 
    person_id   AS dgvid,
    name        AS dgvname,
    phone       AS dgvphone,
    note        AS dgvnote,
    created_on  AS dgvdate
FROM Person
WHERE is_owner = 1
  AND (name LIKE @q OR phone LIKE @q OR note LIKE @q)
ORDER BY person_id DESC;";
                    pars = new Dictionary<string, object> { { "@q", "%" + q + "%" } };
                }

                var lb = new ListBox();
                lb.Items.Add(dgvid);
                lb.Items.Add(dgvname);
                lb.Items.Add(dgvphone);
                lb.Items.Add(dgvnote);
                lb.Items.Add(dgvdate);

                MainClass.LoadData1(qry, guna2DataGridView1, lb, pars);
                guna2DataGridView1.ClearSelection();
            }
            catch (Exception ex)
            {
                ShowError("Fetching owners failed.\n\n" + ex.Message);
            }
        }

        public override void btnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                using (var f = new frmOwnerForm())
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
                    if (MainClass.LoggedInRole != "Admin")
                    {
                        CustomeMessage msg = new CustomeMessage("Only Admin can edit owners!", "Access Denied");
                        msg.ShowDialog();
                        return;
                    }

                    if (!TryGetSelectedId(out int id)) { ShowError("Invalid selection. ID missing."); return; }

                    using (var frm = new frmOwnerForm())
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
                    if (MainClass.LoggedInRole != "Admin")
                    {
                        CustomeMessage msg = new CustomeMessage("Only Admin can delete owners!", "Access Denied");
                        msg.ShowDialog();
                        return;
                    }

                    if (!TryGetSelectedId(out int id)) { ShowError("Invalid selection. ID missing."); return; }
                    if (!AskYesNo("Delete this owner?\n\nNote: Agar Deal me reference ho to delete fail hoga.", "Delete Confirmation")) return;

                    int res = MainClass.ExecuteNonQuery(
                        "DELETE FROM Person WHERE person_id=@id AND is_owner=1;",
                        new Dictionary<string, object> { { "@id", id } });

                    if (res > 0) { ShowInfo("Delete successful."); LoadData(); }
                    else { ShowError("Delete failed ya record referenced/missing hai."); }
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
