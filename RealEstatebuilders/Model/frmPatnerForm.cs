using System;
using System.Collections;
using System.Data;
using System.Windows.Forms;

namespace HubBuilders.Model
{
    public partial class frmPatnerForm : SampleAdd
    {
        public frmPatnerForm()
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
            // Validation
            if (string.IsNullOrWhiteSpace(txtPartnerName.Text))
            {
                MessageBox.Show("Partner Name required hai.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPartnerName.Focus();
                return;
            }

            // Inputs
            string name = txtPartnerName.Text.Trim();
            string phone = txtPartnerPhone?.Text?.Trim();
            string note = txtPartnerNote?.Text?.Trim();
            string createdOn = (created_on?.Value ?? DateTime.Today).ToString("yyyy-MM-dd HH:mm:ss");

            // Duplicate check (by name among partners; exclude self if edit)
            string dupQry = "SELECT COUNT(1) FROM Person WHERE lower(name)=lower(@name) AND is_partner=1 " + (id > 0 ? "AND person_id<>@id" : "");
            Hashtable dupHt = new Hashtable();
            dupHt.Add("@name", name);
            if (id > 0) dupHt.Add("@id", id);

            int dupCount = Convert.ToInt32(MainClass.SQLScalar(dupQry, dupHt) ?? 0);
            if (dupCount > 0)
            {
                MessageBox.Show("Ye Partner Name pehle se mojood hai.", "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtPartnerName.Focus();
                return;
            }

            // Insert/Update Person as Partner (is_partner=1)
            string qry;
            if (id == 0)
            {
                qry = @"
INSERT INTO Person (name, phone, note, created_on, is_owner, is_partner)
VALUES (@name, @phone, @note, @created_on, 0, 1);";
            }
            else
            {
                qry = @"
UPDATE Person
   SET name=@name,
       phone=@phone,
       note=@note,
       created_on=@created_on,
       is_partner=1
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
                    id == 0 ? "Partner save ho gaya." : "Partner update ho gaya.",
                    "RealEstate Builders");
                ok.ShowDialog();

                // Reset
                id = 0;
                txtPartnerName.Text = "";
                txtPartnerPhone.Text = "";
                txtPartnerNote.Text = "";
                if (created_on != null) created_on.Value = DateTime.Today;
                txtPartnerName.Focus();
            }
        }

        // Edit-mode loader: Person → Partner form
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
                txtPartnerName.Text = Convert.ToString(r["name"]);
                txtPartnerPhone.Text = Convert.ToString(r["phone"]);
                txtPartnerNote.Text = Convert.ToString(r["note"]);

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
