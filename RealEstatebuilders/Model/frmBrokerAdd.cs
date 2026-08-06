using System;
using System.Collections;
using System.Data;
using System.Windows.Forms;

namespace HubBuilders.Model
{
    public partial class frmBrokerAdd : SampleAdd
    {
        public int id = 0;  // 0 = insert, >0 = update

        public frmBrokerAdd()
        {
            InitializeComponent();

            // Keyboard shortcuts
            this.KeyPreview = true;

            // Events
            this.Load += frmBrokerAdd_Load;
            txtphone.KeyPress += txtphone_KeyPress;

            // cbactive = Guna2ComboBox (NOT CheckBox)
            // Force dropdown-only and seed items
            cbactive.DropDownStyle = ComboBoxStyle.DropDownList;
            if (cbactive.Items.Count == 0)
            {
                cbactive.Items.AddRange(new object[] { "Active", "Inactive" });
            }
            cbactive.SelectedIndex = 0; // default Active
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.S))
            {
                btnSave_Click(this, EventArgs.Empty);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void frmBrokerAdd_Load(object sender, EventArgs e)
        {
            if (id > 0)
            {
                // Load broker by id
                DataTable dt = MainClass.GetData("SELECT * FROM brokers WHERE id = " + id);
                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];

                    txtname.Text  = row["name"]?.ToString() ?? "";
                    txtphone.Text = row["phone"] == DBNull.Value ? "" : row["phone"].ToString();

                    bool isActive = false;
                    if (row["is_active"] != DBNull.Value)
                        isActive = Convert.ToBoolean(row["is_active"]);

                    cbactive.SelectedIndex = isActive ? 0 : 1; // 0=Active, 1=Inactive
                }
            }
        }

        public override void btnSave_Click(object sender, EventArgs e)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(txtname.Text))
            {
                MessageBox.Show("Name lazmi hai.");
                return;
            }
            if (cbactive.SelectedIndex < 0)
            {
                MessageBox.Show("Active/Inactive select karein.");
                return;
            }

            try
            {
                string qry = (id == 0)
                    ? "INSERT INTO brokers (name, phone, is_active) VALUES (@name, @phone, @active)"
                    : "UPDATE brokers SET name = @name, phone = @phone, is_active = @active WHERE id = @id";

                // Map combobox to bit
                int activeBit = (cbactive.SelectedIndex == 0) ? 1 : 0;

                Hashtable ht = new Hashtable
                {
                    { "@id", id },
                    { "@name", txtname.Text.Trim() },
                    { "@phone", string.IsNullOrWhiteSpace(txtphone.Text) ? (object)DBNull.Value : txtphone.Text.Trim() },
                    { "@active", activeBit }
                };

                int r = MainClass.DataInsertUpdateDelete(qry, ht);
                if (r > 0)
                {
                    CustomeMessage ok = new CustomeMessage("کامیابی کے ساتھ محفوظ کر لیا گیا", "");
                    ok.ShowDialog();

                    MainClass.Enable_reset(this);
                    id = 0;
                    // Defaults after reset
                    if (cbactive.Items.Count == 0)
                        cbactive.Items.AddRange(new object[] { "Active", "Inactive" });
                    cbactive.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void txtphone_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Allow: digits, control keys, + - space ( ) only
            if (!char.IsControl(e.KeyChar) &&
                !char.IsDigit(e.KeyChar) &&
                e.KeyChar != '+' && e.KeyChar != '-' &&
                e.KeyChar != ' ' && e.KeyChar != '(' && e.KeyChar != ')')
            {
                e.Handled = true;
                CustomeMessage cm = new CustomeMessage("فون نمبر میں صرف ہندسے/+, -, ( ), space کی اجازت ہے", "غلطی");
                cm.ShowDialog();
            }
        }
    }
}
