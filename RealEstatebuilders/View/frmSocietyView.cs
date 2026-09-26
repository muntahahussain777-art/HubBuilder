using HubBuilders.Model;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace HubBuilders.View
{
    public partial class frmSocietyView : SampleView
    {
        public frmSocietyView()
        {
            InitializeComponent();
        }

        private void frmSocietyView_Load(object sender, EventArgs e)
        {
            try
            {
                /* StaffAccess ApplyStaffAccessUi handles Add/Edit/Delete visibility */
                GetData();
            }
            catch (Exception ex)
            {
                ShowError("Failed to load data on form load.\n\n" + ex.Message);
            }
        }

        public void GetData()
        {
            try
            {
                // ? Societies listing (name/city pe search)
                string qry = @"SELECT id, name, city, created_at 
                               FROM societies
                               WHERE (name LIKE @q OR city LIKE @q)
                               ORDER BY id DESC;";

                // mapping listbox (LoadData/LoadData1 pattern ke liye)
                var lb = new ListBox();
                lb.Items.Add(dgvid);       // id
                lb.Items.Add(dgvSociety);  // name
                lb.Items.Add(dgvBlock);    // city
                lb.Items.Add(dgvDate);     // created_at

                var q = "%" + (txtSearch.Text ?? string.Empty).Trim() + "%";
                var pars = new Dictionary<string, object> { { "@q", q } };

                MainClass.LoadData1(qry, guna2DataGridView1, lb, pars);
                guna2DataGridView1.ClearSelection();
            }
            catch (Exception ex)
            {
                ShowError("Failed to fetch societies.\n\n" + ex.Message);
            }
        }

        public override void btnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                // Add new / edit form
                using (var f = new frmSocietyForm())
                {
                    MainClass.BlurBackground(f);
                }
                GetData();
            }
            catch (Exception ex)
            {
                ShowError("Failed to open add/edit form.\n\n" + ex.Message);
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
                ShowError("Search failed.\n\n" + ex.Message);
            }
        }

        private void guna2DataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0) return;
                if (guna2DataGridView1.CurrentCell == null) return;

                var colName = guna2DataGridView1.CurrentCell.OwningColumn?.Name;
                if (string.IsNullOrEmpty(colName)) return;

                // ? EDIT (ADMIN-ONLY)
                if (colName == "dgvedit")
                {
                    if (StaffAccess.Deny(StaffAccess.ModuleForForm(this), "edit")) return;

                    if (!TryGetSelectedId(out var id))
                    {
                        ShowError("Invalid row selection. ID missing.");
                        return;
                    }

                    using (var f = new frmSocietyForm())
                    {
                        f.LoadForEdit(id); // form khud DB se load karega
                        MainClass.BlurBackground(f);
                    }

                    GetData();
                    return;
                }

                // ? DELETE (ADMIN-ONLY)
                if (colName == "dgvdel")
                {
                    if (StaffAccess.Deny(StaffAccess.ModuleForForm(this), "delete")) return;

                    if (!TryGetSelectedId(out var id))
                    {
                        ShowError("Invalid row selection. ID missing.");
                        return;
                    }

                    using (var ask = new YesOrNoMessage("Are you sure you want to delete?", "Delete Confirmation"))
                    {
                        if (ask.ShowDialog() == DialogResult.Yes)
                        {
                            int res = MainClass.ExecuteNonQuery(
                                "DELETE FROM societies WHERE id=@id",
                                new Dictionary<string, object> { { "@id", id } });

                            if (res > 0)
                            {
                                using (var ok = new CustomeMessage("Delete Successfully", "RealEstate Builders"))
                                    ok.ShowDialog();
                                GetData();
                            }
                            else
                            {
                                ShowError("Delete failed or record missing.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("Operation failed.\n\n" + ex.Message);
            }
        }

        private bool TryGetSelectedId(out int id)
        {
            id = 0;
            try
            {
                var cell = guna2DataGridView1.CurrentRow?.Cells["dgvid"]?.Value;
                if (cell == null) return false;
                if (cell is int i)
                {
                    id = i;
                    return true;
                }
                var s = Convert.ToString(cell);
                return int.TryParse(s, out id);
            }
            catch
            {
                id = 0;
                return false;
            }
        }

        private void ShowError(string message)
        {
            using (var err = new ErrorFormMessage(message, "Error"))
            {
                err.ShowDialog();
            }
        }

        private void ShowInfo(string message)
        {
            using (var ok = new CustomeMessage(message, "RealEstate Builders"))
            {
                ok.ShowDialog();
            }
        }
    }
}
