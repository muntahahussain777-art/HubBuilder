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
            if (MainClass.LoggedInRole != "Admin")
            {
                btnAdd.Enabled = false;  // Add button disable
                gunaDataGridView1.Columns["dgvedit"].Visible = false; // Edit Button Hide
                gunaDataGridView1.Columns["dgvdel"].Visible = false;  // Delete Button Hide
            }
            else
            {
                // Admin: staff entries dekhne ka button (layout same, sirf Add ke paas)
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
                            using (var frm = new frmStaffEntryLog())
                                frm.ShowDialog(this);
                        };
                        btnAdd.Parent.Controls.Add(btnStaffLog);
                        btnStaffLog.BringToFront();
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
                if (MainClass.LoggedInRole != "Admin")
                {
                    CustomeMessage msg = new CustomeMessage("Only Admin can edit users!", "Access Denied");
                    msg.ShowDialog();
                    return;
                }

                frmPasswordAdd frm = new frmPasswordAdd();
                frm.id = Convert.ToInt32(gunaDataGridView1.CurrentRow.Cells["dgvid"].Value);
                // Password grid se mat lo — form Load DB se real password bharega

                MainClass.BlurBackground(frm);
                LoadData();
            }

            if (gunaDataGridView1.CurrentCell.OwningColumn.Name == "dgvdel")
            {
                // ADMIN-ONLY
                if (MainClass.LoggedInRole != "Admin")
                {
                    CustomeMessage msg = new CustomeMessage("Only Admin can delete users!", "Access Denied");
                    msg.ShowDialog();
                    return;
                }

                YesOrNoMessage customMessageBox1 = new YesOrNoMessage("Are you sure you want to delete?", "Delete Confirmation");
                DialogResult dialogResult = customMessageBox1.ShowDialog();
                if (dialogResult == DialogResult.Yes)
                {
                    int id = Convert.ToInt32(gunaDataGridView1.CurrentRow.Cells["dgvid"].Value);
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
