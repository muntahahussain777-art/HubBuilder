using HubBuilders.Model;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace HubBuilders.View
{
    public partial class frmBlockFormView : SampleView
    {
        public frmBlockFormView()
        {
            InitializeComponent();
        }

        private void frmBlockFormView_Load(object sender, EventArgs e)
        {
            try
            {
                /* StaffAccess ApplyStaffAccessUi handles Add/Edit/Delete visibility */
                GetData();
            }
            catch (Exception ex)
            {
                ShowError("Form load failed.\n\n" + ex.Message);
            }
        }

        public void GetData()
        {
            try
            {
                string qry = @"
SELECT b.id, s.name AS society, b.name AS block
FROM blocks b
INNER JOIN societies s ON s.id = b.society_id
WHERE (s.name LIKE @q OR b.name LIKE @q)
ORDER BY b.id DESC;";

                var pars = new Dictionary<string, object>
                {
                    { "@q", "%" + (txtSearch?.Text ?? string.Empty).Trim() + "%" }
                };

                var lb = new ListBox();
                lb.Items.Add(dgvid);       // b.id
                lb.Items.Add(dgvSociety);  // s.name AS society
                lb.Items.Add(dgvBlock);    // b.name AS block

                MainClass.LoadData1(qry, guna2DataGridView1, lb, pars);
                guna2DataGridView1.ClearSelection();
            }
            catch (Exception ex)
            {
                ShowError("Fetching blocks failed.\n\n" + ex.Message);
            }
        }

        public override void btnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                using (var f = new frmBlockFormAdd())
                {
                    MainClass.BlurBackground(f);
                }
                GetData();
            }
            catch (Exception ex)
            {
                ShowError("Opening add/edit dialog failed.\n\n" + ex.Message);
            }
        }

        public override void txtSearch_TextChanged(object sender, EventArgs e)
        {
            try
            {
                GetData();
            }
            catch (Exception ex)
            {
                ShowError("Search update failed.\n\n" + ex.Message);
            }
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

                    if (!TryGetSelectedId(out int id))
                    {
                        ShowError("Invalid selection. ID missing.");
                        return;
                    }

                    using (var frm = new frmBlockFormAdd())
                    {
                        frm.LoadForEdit(id);
                        MainClass.BlurBackground(frm);
                    }
                    GetData();
                    return;
                }

                if (col == "dgvdel")
                {
                    // ADMIN-ONLY
                    if (StaffAccess.Deny(StaffAccess.ModuleForForm(this), "delete")) return;

                    if (!TryGetSelectedId(out int id))
                    {
                        ShowError("Invalid selection. ID missing.");
                        return;
                    }

                    if (!AskYesNo("Are you sure you want to delete?", "Delete Confirmation")) return;

                    try
                    {
                        int res = MainClass.ExecuteNonQuery(
                            "DELETE FROM blocks WHERE id=@id;",
                            new Dictionary<string, object> { { "@id", id } });

                        if (res > 0)
                        {
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

        // Helpers
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
