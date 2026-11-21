using System;
using System.Collections;
using System.Data;
using System.Windows.Forms;

namespace HubBuilders.Model
{
    public partial class frmCustomerAdd : SampleAdd
    {
        public frmCustomerAdd()
        {
            InitializeComponent();
            // default date today
            if (txtDate != null) txtDate.Value = DateTime.Today;
        }

        // edit mode ke liye
        public int id = 0;

        // Ctrl+S shortkey
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.S))
            {
                btnSave_Click(this, EventArgs.Empty);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        public override void btnClose_Click(object sender, EventArgs e)
        {
            SafeClose();
        }

        public override void btnSave_Click(object sender, EventArgs e)
        {
            // --- basic required validation (roman urdu: khaali na chhorein)
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Name required hai.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus(); return;
            }
            if (string.IsNullOrWhiteSpace(txtmobile.Text))
            {
                MessageBox.Show("Mobile required hai.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtmobile.Focus(); return;
            }
            if (string.IsNullOrWhiteSpace(txtCNIC.Text))
            {
                MessageBox.Show("CNIC required hai.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCNIC.Focus(); return;
            }

            // Allow same CNIC / mobile — drop DB UNIQUE if present
            try { MainClass.EnsureCustomerSchema(); } catch { }

            // --- data prep (SQLite TEXT date ko ISO format de dein)
            string createdAt = (txtDate?.Value ?? DateTime.Today).ToString("yyyy-MM-dd");

            string qry;
            if (id == 0)
            {
                // INSERT (snake_case columns)
                qry = @"INSERT INTO customers (name, father_name, mobile, cnic, notes, created_at, sync_id)
                        VALUES (@name, @father_name, @mobile, @cnic, @notes, @created_at, lower(hex(randomblob(16))))";
            }
            else
            {
                // UPDATE
                qry = @"UPDATE customers SET 
                            name=@name,
                            father_name=@father_name,
                            mobile=@mobile,
                            cnic=@cnic,
                            notes=@notes,
                            created_at=@created_at
                        WHERE id=@id";
            }

            Hashtable ht = new Hashtable();
            if (id > 0) ht.Add("@id", id);
            ht.Add("@name", txtName.Text.Trim());
            ht.Add("@father_name", txtfathername.Text.Trim());
            ht.Add("@mobile", txtmobile.Text.Trim());
            ht.Add("@cnic", txtCNIC.Text.Trim());
            ht.Add("@notes", txtNote.Text.Trim());
            ht.Add("@created_at", createdAt);

            if (MainClass.SQL(qry, ht) > 0)
            {
                // success message (tumhara custom message box use ho raha hai)
                CustomeMessage customMessageBox1 = new CustomeMessage(
                    id == 0 ? "Customer save ho gaya." : "Customer update ho gaya.",
                    "RealEstate Builders"
                );
                customMessageBox1.ShowDialog();

                // reset
                id = 0;
                txtName.Text = "";
                txtfathername.Text = "";
                txtmobile.Text = "";
                txtCNIC.Text = "";
                txtNote.Text = "";
                if (txtDate != null) txtDate.Value = DateTime.Today;
                txtName.Focus();
            }
        }

        // Mobile only digits
        private void txtmobile_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
                CustomeMessage customMessageBox = new CustomeMessage("Yahan sirf numbers allow hain.", "Ghalti");
                customMessageBox.ShowDialog();
            }
        }

        // CNIC only digits (agar hyphen format chahiye to MaskedTextBox use karo)
        private void txtcnic_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
                CustomeMessage customMessageBox = new CustomeMessage("Yahan sirf numbers allow hain.", "Ghalti");
                customMessageBox.ShowDialog();
            }
        }

        // --- OPTIONAL: Edit mode load helper (View se call karna ho)
        public void LoadForEdit(int editId)
        {
            id = editId;
            string q = "SELECT id, name, father_name, mobile, cnic, notes, created_at FROM customers WHERE id=@id";
            Hashtable ht = new Hashtable(); ht.Add("@id", id);
            DataTable dt = MainClass.ExecuteSelectQuery(q, ht);
            if (dt.Rows.Count == 1)
            {
                var r = dt.Rows[0];
                txtName.Text = Convert.ToString(r["name"]);
                txtfathername.Text = Convert.ToString(r["father_name"]);
                txtmobile.Text = Convert.ToString(r["mobile"]);
                txtCNIC.Text = Convert.ToString(r["cnic"]);
                txtNote.Text = Convert.ToString(r["notes"]);
                DateTime d;
                if (DateTime.TryParse(Convert.ToString(r["created_at"]), out d)) txtDate.Value = d;
                else txtDate.Value = DateTime.Today;
            }
        }
    }
}
