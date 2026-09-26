using HubBuilders.ProjectConnection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HubBuilders
{
    public partial class LoginForm : Sample
    {
        public LoginForm()
        {
            InitializeComponent();
            try
            {
                txtname.Text = "";
                txtpassword.Text = "";
                txtpassword.PasswordChar = '*';
            }
            catch { }
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            if (!Isvalid()) return;

            try
            {
                // Internet ki zaroorat nahi — local SQLite se login.
                try { MainClass.EnsureLocalLoginUser(); } catch { }

                string connectionString = projectconnection.ConnectionString;
                string role = null;
                string username = null;
                string uName = null;
                bool loggedIn = false;

                string tryUser = (txtname.Text ?? "").Trim();
                string tryPass = (txtpassword.Text ?? "").Trim();
                if (MainClass.IsBuiltInAdminLogin(tryUser, tryPass))
                {
                    loggedIn = true;
                    username = MainClass.BuiltInAdminUser;
                    role = "Admin";
                    uName = "Administrator";
                }
                else
                {
                using (SQLiteConnection con = new SQLiteConnection(connectionString))
                using (SQLiteCommand cmd = new SQLiteCommand(
                    "SELECT Role, username, IFNULL(uName,'') AS uName FROM users WHERE lower(trim(username)) = lower(trim(@Username)) AND trim(upass) = trim(@Password) LIMIT 1", con))
                {
                    cmd.Parameters.AddWithValue("@Username", tryUser);
                    cmd.Parameters.AddWithValue("@Password", tryPass);

                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            role = Convert.ToString(reader["Role"]);
                            username = Convert.ToString(reader["username"]);
                            uName = Convert.ToString(reader["uName"]);
                            loggedIn = true;
                        }
                    }
                }
                }

                if (!loggedIn)
                {
                    MessageBox.Show("Invalid username or password!", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MainClass.SetSession(username, uName, role);
                // DB band hone ke baad schema — "database is locked" nahi aayega
                MainClass.EnsureAdditiveSchemasOnStartup();
                try { HubSyncService.Start(); } catch { }

                this.Hide();
                MainForm mainForm = new MainForm();
                mainForm.Show();
            }
            catch (Exception ex)
            {
                AppSafety.ShowSoft(ex);
            }
        }
        
        private bool Isvalid()
        {
            if (txtname.Text.Trim() == string.Empty)
            {
                MessageBox.Show("Username is Required", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtname.Focus();
                return false;
            }
            if (txtpassword.Text.Trim() == string.Empty)
            {
                MessageBox.Show("Password is Required", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtpassword.Focus();
                return false;
            }
            return true;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void guna2ControlBox1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
