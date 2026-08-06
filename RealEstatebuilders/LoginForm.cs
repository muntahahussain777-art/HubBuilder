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
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            if (!Isvalid()) return;

            try
            {
                string connectionString = projectconnection.ConnectionString;
                string role = null;
                string username = null;
                string uName = null;
                bool loggedIn = false;

                using (SQLiteConnection con = new SQLiteConnection(connectionString))
                using (SQLiteCommand cmd = new SQLiteCommand(
                    "SELECT Role, username, IFNULL(uName,'') AS uName FROM users WHERE username = @Username AND upass = @Password", con))
                {
                    cmd.Parameters.AddWithValue("@Username", txtname.Text.Trim());
                    cmd.Parameters.AddWithValue("@Password", txtpassword.Text.Trim());

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

                if (!loggedIn)
                {
                    MessageBox.Show("Invalid username or password!", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MainClass.SetSession(username, uName, role);
                // DB band hone ke baad schema — "database is locked" nahi aayega
                MainClass.EnsureAdditiveSchemasOnStartup();

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
