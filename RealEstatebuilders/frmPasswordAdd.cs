using HubBuilders.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace HubBuilders.Report_Form
{
    public partial class frmPasswordAdd : SampleAdd
    {
        public frmPasswordAdd()
        {
            InitializeComponent();
        }

        public int id = 0;

        private void frmPasswordAdd_Load(object sender, EventArgs e)
        {
            LoadData();
        }

        private void LoadData()
        {
            if (id > 0)
            {
                // Agar MainClass.GetData parameterized support karta ho to use karein.
                DataTable dt = MainClass.GetData(
                    "SELECT * FROM users WHERE userID = @id",
                    new Dictionary<string, object> { { "@id", id } });
                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    txtName.Text = Convert.ToString(row["uName"]);
                    txtusername.Text = Convert.ToString(row["username"]);
                    txtpassword.Text = Convert.ToString(row["upass"]);
                    txtmobile.Text = Convert.ToString(row["uphone"]);
                    txtRole.Text = Convert.ToString(row["Role"]);
                }
            }
        }

        public override void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                string u = (txtusername.Text ?? "").Trim();
                string p = (txtpassword.Text ?? "").Trim();
                string n = (txtName.Text ?? "").Trim();
                string ph = (txtmobile.Text ?? "").Trim();
                string Rl = (txtRole.Text ?? "").Trim();


                if (string.IsNullOrWhiteSpace(u) || string.IsNullOrWhiteSpace(p) || string.IsNullOrWhiteSpace(n))
                {
                    MessageBox.Show("Username, Password, Name lazmi hain.");
                    return;
                }

                if (MainClass.IsBuiltInAdminUsername(u))
                {
                    MessageBox.Show("Admin login software ke sath lock hai. Change nahi ho sakta.",
                        "Admin", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (id > 0)
                {
                    DataTable cur = MainClass.GetData(
                        "SELECT username FROM users WHERE userID = @id",
                        new Dictionary<string, object> { { "@id", id } });
                    if (cur != null && cur.Rows.Count > 0
                        && MainClass.IsBuiltInAdminUsername(Convert.ToString(cur.Rows[0]["username"])))
                    {
                        MessageBox.Show("Admin login software ke sath lock hai. Change nahi ho sakta.",
                            "Admin", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                }

                string qry;
                if (id == 0)
                {
                    qry = "INSERT INTO users (username, upass, uName, uphone,Role) " +
                          "VALUES (@username, @upass, @name, @phone,@role)";
                }
                else
                {
                    // Yahan EXACT param names use hon: @username (na ke @user)
                    qry = "UPDATE users SET username = @username, upass = @upass, uName = @name, uphone = @phone ,Role = @role " +
                          "WHERE userID = @id";
                }

                Hashtable ht = new Hashtable
                {
                    { "@id", id },           // insert me ignore ho jayega; update me use hoga
                    { "@username", u },
                    { "@upass", p },
                    { "@name", n },
                    { "@phone", ph },
                    { "@role", Rl },
                };

                int r = MainClass.DataInsertUpdateDelete(qry, ht);
                if (r > 0)
                {
                    var customMessageBox = new CustomeMessage("کامیابی کے ساتھ محفوظ کر لیا گیا", "اسلامیہ پیٹرول پمپ");
                    customMessageBox.ShowDialog();
                    MainClass.Enable_reset(this);
                    id = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save/Update error: " + ex.Message);
            }
        }
    }
}
