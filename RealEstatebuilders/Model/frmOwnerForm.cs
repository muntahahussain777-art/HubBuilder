using System;
using System.Collections;
using System.Data;
using System.Windows.Forms;

namespace HubBuilders.Model
{
    public partial class frmOwnerForm : SampleAdd
    {
        public frmOwnerForm()
        {
            InitializeComponent();
            if (created_on != null) created_on.Value = DateTime.Today; // DateTimePicker expected
        }

        public int id = 0; // person_id (edit mode), 0 = insert

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
            if (string.IsNullOrWhiteSpace(txtOwnerName.Text))
            {
                MessageBox.Show("Owner Name required hai.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtOwnerName.Focus();
                return;
            }

            string name = txtOwnerName.Text.Trim();
            string phone = txtOwnerPhone?.Text?.Trim();
            string note = txtOwnerNote?.Text?.Trim();
            string createdOn = (created_on?.Value ?? DateTime.Today).ToString("yyyy-MM-dd HH:mm:ss");

            // Duplicate among owners (exclude self if edit)
            string dupQry = "SELECT COUNT(1) FROM Person WHERE lower(name)=lower(@name) AND is_owner=1 " + (id > 0 ? "AND person_id<>@id" : "");
            Hashtable dupHt = new Hashtable();
            dupHt.Add("@name", name);
            if (id > 0) dupHt.Add("@id", id);

            int dupCount = Convert.ToInt32(MainClass.SQLScalar(dupQry, dupHt) ?? 0);
            if (dupCount > 0)
            {
                MessageBox.Show("Ye Owner Name pehle se mojood hai.", "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtOwnerName.Focus();
                return;
            }

            string qry;
            if (id == 0)
            {
                qry = @"
INSERT INTO Person (name, phone, note, created_on, is_owner, is_partner)
VALUES (@name, @phone, @note, @created_on, 1, 0);";
            }
            else
            {
                qry = @"
UPDATE Person
   SET name=@name,
       phone=@phone,
       note=@note,
       created_on=@created_on,
       is_owner=1
 WHERE person_id=@id;";
            }

            Hashtable ht = new Hashtable();
            if (id > 0) ht.Add("@id", id);
            ht.Add("@name", name);
            ht.Add("@phone", string.IsNullOrWhiteSpace(phone) ? (object)DBNull.Value : phone);
            ht.Add("@note", string.IsNullOrWhiteSpace(note) ? (object)DBNull.Value : note);
            ht.Add("@created_on", createdOn);

            if (MainClass.SQL(qry, ht) > 0)
            {
                CustomeMessage ok = new CustomeMessage(
                    id == 0 ? "Owner save ho gaya." : "Owner update ho gaya.",
                    "RealEstate Builders");
                ok.ShowDialog();

                id = 0;
                txtOwnerName.Text = "";
                txtOwnerPhone.Text = "";
                txtOwnerNote.Text = "";
                if (created_on != null) created_on.Value = DateTime.Today;
                txtOwnerName.Focus();
            }
        }

        public void LoadForEdit(int editPersonId)
        {
            id = editPersonId;
            string q = "SELECT person_id, name, phone, note, created_on FROM Person WHERE person_id=@id";
            Hashtable ht = new Hashtable();
            ht.Add("@id", id);

            DataTable dt = MainClass.ExecuteSelectQuery(q, ht);
            if (dt.Rows.Count == 1)
            {
                var r = dt.Rows[0];
                txtOwnerName.Text = Convert.ToString(r["name"]);
                txtOwnerPhone.Text = Convert.ToString(r["phone"]);
                txtOwnerNote.Text = Convert.ToString(r["note"]);

                if (created_on != null)
                {
                    DateTime d;
                    if (DateTime.TryParse(Convert.ToString(r["created_on"]), out d))
                        created_on.Value = d;
                    else
                        created_on.Value = DateTime.Today;
                }
            }
        }
    }
}
