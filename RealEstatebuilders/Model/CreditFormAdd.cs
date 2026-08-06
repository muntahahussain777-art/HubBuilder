using System;
using System.Collections;
using System.Data;
using System.Windows.Forms;
using HubBuilders; // MainClass

namespace HubBuilders.Model
{
    public partial class CreditFormAdd : SampleAdd
    {
        public CreditFormAdd()
        {
            InitializeComponent();
            if (dtpPayDate != null) dtpPayDate.Value = DateTime.Today;

            FillSocieties();
            FillOwners();
            FillPartners();
        }

        public int id = 0; // credit_id (0 = insert)

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
            int oid = GetSelectedId(cboOwner);   // Person.person_id (owner)
            int pid = GetSelectedId(cboPartner); // Person.person_id (partner)

            if (sid <= 0) { MessageBox.Show("Society select karo."); return; }
            if (oid <= 0) { MessageBox.Show("Owner select karo."); return; }
            if (pid <= 0) { MessageBox.Show("Partner select karo."); return; }
            if (nudAmount == null || nudAmount.Value <= 0) { MessageBox.Show("Amount > 0 chahiye."); return; }

            // Deal selector UI nahi hai: latest deal auto-resolve
            int did = ResolveLatestDealId(sid, oid);
            if (did <= 0)
            {
                MessageBox.Show("Is Society + Owner pair ke liye koi Deal nahi mili. Pehle Deal create karo.");
                return;
            }

            string payDate = (dtpPayDate?.Value ?? DateTime.Today).ToString("yyyy-MM-dd");
            decimal amount = nudAmount.Value;
            string note = txtCreditNote?.Text?.Trim();

            string qry = (id == 0)
                ? @"INSERT INTO Credit (deal_id, partner_id, pay_date, amount, note)
                    VALUES (@did, @pid, @dt, @amt, @note);"
                : @"UPDATE Credit
                        SET deal_id=@did, partner_id=@pid, pay_date=@dt, amount=@amt, note=@note
                      WHERE credit_id=@id;";

            Hashtable ht = new Hashtable();
            if (id > 0) ht.Add("@id", id);
            ht.Add("@did", did);
            ht.Add("@pid", pid);
            ht.Add("@dt", payDate);
            ht.Add("@amt", amount);
            ht.Add("@note", string.IsNullOrWhiteSpace(note) ? (object)DBNull.Value : note);

            if (MainClass.SQL(qry, ht) > 0)
            {
                new CustomeMessage(id == 0 ? "Credit save ho gaya." : "Credit update ho gaya.", "RealEstate Builders").ShowDialog();
                ResetForm();
            }
        }

        public void LoadForEdit(int creditId)
        {
            id = creditId;

            if (cboSociety.DataSource == null) FillSocieties();
            if (cboOwner.DataSource == null) FillOwners();
            if (cboPartner.DataSource == null) FillPartners();

            string q = @"SELECT c.credit_id, c.deal_id, c.partner_id, c.pay_date, c.amount, c.note,
                                d.society_id, d.owner_id
                           FROM Credit c
                           JOIN Deal d ON d.deal_id = c.deal_id
                          WHERE c.credit_id=@id;";
            Hashtable ht = new Hashtable { { "@id", id } };
            DataTable dt = MainClass.ExecuteSelectQuery(q, ht);
            if (dt.Rows.Count != 1) return;

            var r = dt.Rows[0];

            SafeSelect(cboSociety, Convert.ToInt32(r["society_id"]));
            SafeSelect(cboOwner, Convert.ToInt32(r["owner_id"]));
            SafeSelect(cboPartner, Convert.ToInt32(r["partner_id"]));

            if (dtpPayDate != null)
            {
                if (DateTime.TryParse(Convert.ToString(r["pay_date"]), out var d)) dtpPayDate.Value = d;
                else dtpPayDate.Value = DateTime.Today;
            }
            if (nudAmount != null) nudAmount.Value = Convert.ToDecimal(r["amount"]);
            if (txtCreditNote != null) txtCreditNote.Text = Convert.ToString(r["note"]);
        }

        // ---- Helpers ----

        private void FillSocieties()
        {
            // MainClass.CBFill expects columns: id, Name
            MainClass.CBFill("SELECT id, name AS Name FROM societies ORDER BY name;", cboSociety);
        }

        private void FillOwners()
        {
            MainClass.CBFill("SELECT person_id AS id, name AS Name FROM Person WHERE is_owner=1 ORDER BY name;", cboOwner);
        }

        private void FillPartners()
        {
            MainClass.CBFill("SELECT person_id AS id, name AS Name FROM Person WHERE is_partner=1 ORDER BY name;", cboPartner);
        }

        private int ResolveLatestDealId(int societyId, int ownerId)
        {
            string q = @"SELECT deal_id
                          FROM Deal
                         WHERE society_id=@sid AND owner_id=@oid
                      ORDER BY date(deal_date) DESC, deal_id DESC
                         LIMIT 1;";
            Hashtable p = new Hashtable { { "@sid", societyId }, { "@oid", ownerId } };
            object obj = MainClass.SQLScalar(q, p);
            return obj == null || obj == DBNull.Value ? -1 : Convert.ToInt32(obj);
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
            if (cboPartner != null) cboPartner.SelectedIndex = -1;
            if (nudAmount != null) nudAmount.Value = 0;
            if (dtpPayDate != null) dtpPayDate.Value = DateTime.Today;
            if (txtCreditNote != null) txtCreditNote.Text = string.Empty;
            cboSociety?.Focus();
        }
    }
}
