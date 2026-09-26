using Guna.UI2.WinForms;
using HubBuilders.Model;
using HubBuilders.Report_Form;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HubBuilders.View
{
    public partial class frmPasswordView : SampleView
    {
        public frmPasswordView()
        {
            InitializeComponent();
        }

        private void frmPasswordView_Load(object sender, EventArgs e)
        {
            if (!StaffAccess.Can("users", "add"))
            {
                btnAdd.Enabled = false;
            }
            if (!StaffAccess.Can("users", "edit") && gunaDataGridView1.Columns.Contains("dgvedit"))
                gunaDataGridView1.Columns["dgvedit"].Visible = false;
            if (!StaffAccess.Can("users", "delete") && gunaDataGridView1.Columns.Contains("dgvdel"))
                gunaDataGridView1.Columns["dgvdel"].Visible = false;
            if (MainClass.IsAdmin)
            {
                // Admin: staff entries + role rules (layout same, sirf Add ke paas)
                try
                {
                    if (btnAdd != null && btnAdd.Parent != null && btnAdd.Parent.Controls["btnStaffLog"] == null)
                    {
                        var btnStaffLog = new Guna2Button
                        {
                            Name = "btnStaffLog",
                            Text = "Staff Entry Log",
                            Size = new Size(150, btnAdd.Height),
                            Location = new Point(Math.Max(8, btnAdd.Left - 160), btnAdd.Top),
                            BorderRadius = btnAdd.BorderRadius,
                            FillColor = Color.FromArgb(10, 40, 100),
                            Font = btnAdd.Font,
                            ForeColor = Color.White,
                            Animated = true
                        };
                        btnStaffLog.Click += (s, ev) =>
                        {
                            if (StaffAccess.Deny("staff_entry_log", "open")) return;
                            using (var frm = new frmStaffEntryLog())
                                frm.ShowDialog(this);
                        };
                        btnAdd.Parent.Controls.Add(btnStaffLog);
                        btnStaffLog.BringToFront();
                    }
                    if (btnAdd != null && btnAdd.Parent != null && btnAdd.Parent.Controls["btnStaffRules"] == null)
                    {
                        var btnRules = new Guna2Button
                        {
                            Name = "btnStaffRules",
                            Text = "Staff Rules",
                            Size = new Size(130, btnAdd.Height),
                            Location = new Point(Math.Max(8, btnAdd.Left - 300), btnAdd.Top),
                            BorderRadius = btnAdd.BorderRadius,
                            FillColor = Color.FromArgb(0, 120, 110),
                            Font = btnAdd.Font,
                            ForeColor = Color.White,
                            Animated = true
                        };
                        btnRules.Click += (s, ev) =>
                        {
                            using (var frm = new frmStaffRolePermissions())
                                frm.ShowDialog(this);
                        };
                        btnAdd.Parent.Controls.Add(btnRules);
                        btnRules.BringToFront();
                    }
                    if (btnAdd != null && btnAdd.Parent != null && btnAdd.Parent.Controls["btnBlockMonthly"] == null)
                    {
                        var btnMonthly = new Guna2Button
                        {
                            Name = "btnBlockMonthly",
                            Text = "Block Monthly",
                            Size = new Size(140, btnAdd.Height),
                            Location = new Point(Math.Max(8, btnAdd.Left - 450), btnAdd.Top),
                            BorderRadius = btnAdd.BorderRadius,
                            FillColor = Color.FromArgb(10, 40, 100),
                            Font = btnAdd.Font,
                            ForeColor = Color.White,
                            Animated = true
                        };
                        btnMonthly.Click += (s, ev) =>
                        {
                            using (var frm = new frmBlockMonthlyInstallment())
                                frm.ShowDialog(this);
                        };
                        btnAdd.Parent.Controls.Add(btnMonthly);
                        btnMonthly.BringToFront();
                    }
                }
                catch { }
            }
            LoadData();

        }
        private void LoadData()
        {
            try
            {
                string qry;
                Dictionary<string, object> p = null;
                if (string.IsNullOrWhiteSpace(txtSearch.Text))
                {
                    // Password grid mein plain text mat dikhao — edit pe DB se load
                    qry = "SELECT userID, username, '****' AS upass, uName, uphone, Role FROM users";
                }
                else
                {
                    qry = "SELECT userID, username, '****' AS upass, uName, uphone, Role FROM users WHERE uName LIKE @s";
                    p = new Dictionary<string, object> { { "@s", "%" + txtSearch.Text.Trim() + "%" } };
                }

                ListBox lb = new ListBox();
                lb.Items.Add(dgvid);
                lb.Items.Add(dgvusername);
                lb.Items.Add(dgvpass);
                lb.Items.Add(dgvName);
                lb.Items.Add(dgvphone);
                lb.Items.Add(dgvRole);

                if (p == null)
                    MainClass.loadData(qry, gunaDataGridView1, lb);
                else
                    MainClass.loadData(qry, gunaDataGridView1, lb, p);
            }
            catch (Exception ex)
            {
                HubBuilders.AppSafety.ShowSoft(ex);
            }
        }
        private void guna2DataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            MainClass.SrNo(gunaDataGridView1);
        }
        public override void btnAdd_Click(object sender, EventArgs e)
        {
            MainClass.BlurBackground(new frmPasswordAdd());
            LoadData();

        }

        public override void txtSearch_TextChanged(object sender, EventArgs e)
        {
            LoadData();
         
        }

        private void gunaDataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
            if (gunaDataGridView1.CurrentCell == null) return;

            if (gunaDataGridView1.CurrentCell.OwningColumn.Name == "dgvedit")
            {
                // ADMIN-ONLY
                if (StaffAccess.Deny("users", "edit")) return;

                frmPasswordAdd frm = new frmPasswordAdd();
                frm.id = Convert.ToInt32(gunaDataGridView1.CurrentRow.Cells["dgvid"].Value);
                // Password grid se mat lo — form Load DB se real password bharega

                MainClass.BlurBackground(frm);
                LoadData();
            }

            if (gunaDataGridView1.CurrentCell.OwningColumn.Name == "dgvdel")
            {
                // ADMIN-ONLY
                if (StaffAccess.Deny("users", "delete")) return;

                YesOrNoMessage customMessageBox1 = new YesOrNoMessage("Are you sure you want to delete?", "Delete Confirmation");
                DialogResult dialogResult = customMessageBox1.ShowDialog();
                if (dialogResult == DialogResult.Yes)
                {
                    int id = Convert.ToInt32(gunaDataGridView1.CurrentRow.Cells["dgvid"].Value);
                    string un = "";
                    try { un = Convert.ToString(gunaDataGridView1.CurrentRow.Cells["dgvusername"].Value); } catch { }
                    if (MainClass.IsBuiltInAdminUsername(un))
                    {
                        MessageBox.Show("Admin user delete nahi ho sakta.",
                            "Admin", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    string qry = "Delete from users where userID = @id";
                    Hashtable ht = new Hashtable { { "@id", id } };
                    MainClass.SQL(qry, ht);
                    CustomeMessage customMessageBox11 = new CustomeMessage("Delete Successfully", "hub city hotel");
                    customMessageBox11.ShowDialog();
                    LoadData();
                    txtSearch.Text = "";
                }
            }
            }
            catch (Exception ex)
            {
                HubBuilders.AppSafety.ShowSoft(ex);
            }
        }
    }
}
