using HubBuilders.Model;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace HubBuilders.View
{
    public partial class frmExpenseVIew : SampleView
    {
        public frmExpenseVIew()
        {
            InitializeComponent();
        }

        private void frmExpenseVIew_Load(object sender, EventArgs e)
        {
            try
            {
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
                string qry;
                var q = (txtSearch?.Text ?? string.Empty).Trim();

                // Parameterized search
                if (string.IsNullOrWhiteSpace(q))
                {
                    qry = @"SELECT sid, Name, Amount, Category, Note, EDate FROM Expensetbl
                            ORDER BY sid DESC;";
                    var lbAll = new ListBox();
                    lbAll.Items.Add(dgvid);
                    lbAll.Items.Add(dgvname);
                    lbAll.Items.Add(dgvamount);
                    lbAll.Items.Add(dgvcat);
                    lbAll.Items.Add(dgvNote);
                    lbAll.Items.Add(dgvdate);
                    MainClass.LoadData1(qry, guna2DataGridView1, lbAll, null);
                }
                else
                {
                    qry = @"SELECT sid, Name, Amount, Category, Note, EDate
                            FROM Expensetbl
                            WHERE Name LIKE @q OR Category LIKE @q OR Note LIKE @q
                            ORDER BY sid DESC;";
                    var pars = new Dictionary<string, object> { { "@q", "%" + q + "%" } };

                    var lb = new ListBox();
                    lb.Items.Add(dgvid);
                    lb.Items.Add(dgvname);
                    lb.Items.Add(dgvamount);
                    lb.Items.Add(dgvcat);
                    lb.Items.Add(dgvNote);
                    lb.Items.Add(dgvdate);
                    MainClass.LoadData1(qry, guna2DataGridView1, lb, pars);
                }

                guna2DataGridView1.ClearSelection();
            }
            catch (Exception ex)
            {
                ShowError("Fetching expenses failed.\n\n" + ex.Message);
            }
        }

        public override void btnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                using (var f = new frmExpenseAdd())
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
            try
            {
                LoadData();
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
                    if (!TryGetSelectedId(out int id))
                    {
                        ShowError("Invalid selection. ID missing.");
                        return;
                    }

                    using (var frm = new frmExpenseAdd())
                    {
                        // Populate fields from selected row (existing flow preserved)
                        frm.id = id;
                        frm.txtname.Text = Convert.ToString(guna2DataGridView1.CurrentRow.Cells["dgvname"].Value);
                        frm.txtamount.Text = Convert.ToString(guna2DataGridView1.CurrentRow.Cells["dgvamount"].Value);
                        frm.txtnotes.Text = Convert.ToString(guna2DataGridView1.CurrentRow.Cells["dgvNote"].Value);
                        frm.txtdate.Text = Convert.ToString(guna2DataGridView1.CurrentRow.Cells["dgvdate"].Value);

                        MainClass.BlurBackground(frm);
                    }
                    LoadData();
                    return;
                }

                if (col == "dgvdel")
                {
                    if (!TryGetSelectedId(out int id))
                    {
                        ShowError("Invalid selection. ID missing.");
                        return;
                    }

                    if (!AskYesNo("Are you sure you want to delete?", "Delete Confirmation")) return;

                    try
                    {
                        int res = MainClass.ExecuteNonQuery(
                            "DELETE FROM Expensetbl WHERE sid=@id;",
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
            try
            {
                MainClass.SrNo(guna2DataGridView1);
            }
            catch (Exception ex)
            {
                ShowError("Row numbering failed.\n\n" + ex.Message);
            }
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
