using System;
using System.Collections;
using System.Data;
using System.Windows.Forms;

namespace HubBuilders.Model
{
    public partial class frmSocietyForm : SampleAdd
    {
        public frmSocietyForm()
        {
            InitializeComponent();
            if (txtDate != null) txtDate.Value = DateTime.Today; // agar DateTimePicker ha
        }

        public int id = 0;

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
            // --- Required: Society Name
            if (string.IsNullOrWhiteSpace(txtSocietyName.Text))
            {
                MessageBox.Show("Society Name required hai.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtSocietyName.Focus();
                return;
            }

            string name = txtSocietyName.Text.Trim();
            string city = txtCity.Text.Trim();
            string createdAt = (txtDate?.Value ?? DateTime.Today).ToString("yyyy-MM-dd HH:mm:ss");

            // --- Duplicate check on name (self exclude if edit)
            string dupQry = "SELECT COUNT(1) FROM societies WHERE lower(name)=lower(@name) " + (id > 0 ? "AND id<>@id" : "");
            Hashtable dupHt = new Hashtable();
            dupHt.Add("@name", name);
            if (id > 0) dupHt.Add("@id", id);

            var dupCountObj = MainClass.SQLScalar(dupQry, dupHt);
            int dupCount = Convert.ToInt32(dupCountObj ?? 0);
            if (dupCount > 0)
            {
                MessageBox.Show("Ye Society Name pehle se mojood hai.", "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtSocietyName.Focus();
                return;
            }

            // --- Insert/Update queries (societies)
            string qry;
            if (id == 0)
            {
                qry = @"INSERT INTO societies (name, city, created_at)
                        VALUES (@name, @city, @created_at)";
            }
            else
            {
                qry = @"UPDATE societies SET 
                            name=@name,
                            city=@city,
                            created_at=@created_at
                        WHERE id=@id";
            }

            Hashtable ht = new Hashtable();
            if (id > 0) ht.Add("@id", id);
            ht.Add("@name", name);
            ht.Add("@city", string.IsNullOrWhiteSpace(city) ? (object)DBNull.Value : city);
            ht.Add("@created_at", createdAt);

            if (MainClass.SQL(qry, ht) > 0)
            {
                CustomeMessage ok = new CustomeMessage(
                    id == 0 ? "Society save ho gayi." : "Society update ho gayi.",
                    "RealEstate Builders");
                ok.ShowDialog();

                // reset
                id = 0;
                txtSocietyName.Text = "";
                txtCity.Text = "";
                if (txtDate != null) txtDate.Value = DateTime.Today;
                txtSocietyName.Focus();
            }
        }

        // --- OPTIONAL: Edit mode load helper (View se call karo)
        public void LoadForEdit(int editId)
        {
            id = editId;
            string q = "SELECT id, name, city, created_at FROM societies WHERE id=@id";
            Hashtable ht = new Hashtable();
            ht.Add("@id", id);

            DataTable dt = MainClass.ExecuteSelectQuery(q, ht);
            if (dt.Rows.Count == 1)
            {
                var r = dt.Rows[0];
                txtSocietyName.Text = Convert.ToString(r["name"]);
                txtCity.Text = Convert.ToString(r["city"]);
                DateTime d;
                if (DateTime.TryParse(Convert.ToString(r["created_at"]), out d) && txtDate != null)
                    txtDate.Value = d;
            }
        }
    }
}
