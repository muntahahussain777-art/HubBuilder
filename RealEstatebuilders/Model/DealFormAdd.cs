using System;
using System.Collections;
using System.Data;
using System.Windows.Forms;
using HubBuilders; // for MainClass

namespace HubBuilders.Model
{
    public partial class DealFormAdd : SampleAdd
    {
        public DealFormAdd()
        {
            InitializeComponent();
            if (dtpDealDate != null) dtpDealDate.Value = DateTime.Today;
            FillSocieties();
            FillOwners();
        }

        public int id = 0; // deal_id (0 = insert)

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
            int sid = GetSelectedId(cboSociety); // societies.id
            int oid = GetSelectedId(cboOwner);   // Person.person_id

            if (sid <= 0) { MessageBox.Show("Society select karo."); cboSociety.Focus(); return; }
            if (oid <= 0) { MessageBox.Show("Owner select karo."); cboOwner.Focus(); return; }
            if (nudAmount == null || nudAmount.Value <= 0) { MessageBox.Show("Amount > 0 chahiye."); nudAmount.Focus(); return; }

            string dealDate = (dtpDealDate?.Value ?? DateTime.Today).ToString("yyyy-MM-dd");
            decimal amount = nudAmount.Value;
            string note = txtDealNote?.Text?.Trim();

            // duplicate guard: same society-owner-date (exclude self when editing)
            string dupQry = "SELECT COUNT(1) FROM Deal WHERE society_id=@sid AND owner_id=@oid AND deal_date=@dt " +
                            (id > 0 ? "AND deal_id<>@id" : "");
            Hashtable dupHt = new Hashtable { { "@sid", sid }, { "@oid", oid }, { "@dt", dealDate } };
            if (id > 0) dupHt.Add("@id", id);
            int dup = Convert.ToInt32(MainClass.SQLScalar(dupQry, dupHt) ?? 0);
            if (dup > 0) { MessageBox.Show("Is date par is owner/society ki deal already maujood hai."); return; }

            string qry = (id == 0)
                ? @"INSERT INTO Deal (society_id, owner_id, amount, deal_date, note)
                    VALUES (@sid, @oid, @amt, @dt, @note);"
                : @"UPDATE Deal SET society_id=@sid, owner_id=@oid, amount=@amt, deal_date=@dt, note=@note
                    WHERE deal_id=@id;";

            Hashtable ht = new Hashtable();
            if (id > 0) ht.Add("@id", id);
            ht.Add("@sid", sid);
            ht.Add("@oid", oid);
            ht.Add("@amt", amount);
            ht.Add("@dt", dealDate);
            ht.Add("@note", string.IsNullOrWhiteSpace(note) ? (object)DBNull.Value : note);

            if (MainClass.SQL(qry, ht) > 0)
            {
                new CustomeMessage(id == 0 ? "Deal save ho gayi." : "Deal update ho gayi.", "RealEstate Builders").ShowDialog();
                ResetForm();
            }
        }

        public void LoadForEdit(int dealId)
        {
            id = dealId;

            if (cboSociety.DataSource == null) FillSocieties();
            if (cboOwner.DataSource == null) FillOwners();

            string q = "SELECT deal_id, society_id, owner_id, amount, deal_date, note FROM Deal WHERE deal_id=@id;";
            Hashtable ht = new Hashtable { { "@id", id } };
            DataTable dt = MainClass.ExecuteSelectQuery(q, ht);
            if (dt.Rows.Count != 1) return;

            var r = dt.Rows[0];
            SafeSelect(cboSociety, Convert.ToInt32(r["society_id"]));
            SafeSelect(cboOwner, Convert.ToInt32(r["owner_id"]));

            if (nudAmount != null) nudAmount.Value = Convert.ToDecimal(r["amount"]);

            if (dtpDealDate != null)
            {
                if (DateTime.TryParse(Convert.ToString(r["deal_date"]), out var d)) dtpDealDate.Value = d;
                else dtpDealDate.Value = DateTime.Today;
            }

            if (txtDealNote != null) txtDealNote.Text = Convert.ToString(r["note"]);
        }

        // --- Helpers (aligned with your MainClass API) ---

        private void FillSocieties()
        {
            // MainClass.CBFill expects columns: id, Name
            MainClass.CBFill("SELECT id, name AS Name FROM societies ORDER BY name;", cboSociety);
        }

        private void FillOwners()
        {
            // MainClass.CBFill expects columns: id, Name
            MainClass.CBFill("SELECT person_id AS id, name AS Name FROM Person WHERE is_owner=1 ORDER BY name;", cboOwner);
        }

        private static int GetSelectedId(ComboBox cb)
        {
            if (cb == null || cb.SelectedIndex < 0 || cb.SelectedValue == null || cb.SelectedValue is DBNull) return -1;
            return Convert.ToInt32(cb.SelectedValue);
        }

        private static void SafeSelect(ComboBox cb, int val)
        {
            if (cb == null || cb.DataSource == null) return;
            cb.SelectedValue = val;
            if (cb.SelectedIndex < 0 && cb.Items.Count > 0)
            {
                for (int i = 0; i < cb.Items.Count; i++)
                {
                    if (cb.Items[i] is DataRowView drv)
                    {
                        if (Convert.ToInt32(drv[cb.ValueMember]) == val) { cb.SelectedIndex = i; break; }
                    }
                }
            }
        }

        private void ResetForm()
        {
            id = 0;
            if (cboSociety != null) cboSociety.SelectedIndex = -1;
            if (cboOwner != null) cboOwner.SelectedIndex = -1;
            if (nudAmount != null) nudAmount.Value = 0;
            if (dtpDealDate != null) dtpDealDate.Value = DateTime.Today;
            if (txtDealNote != null) txtDealNote.Text = string.Empty;
            cboSociety?.Focus();
        }
    }
}
