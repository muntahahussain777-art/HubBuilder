using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace HubBuilders.Model
{
    public partial class frmBlockFormAdd : SampleAdd
    {
        public frmBlockFormAdd()
        {
            InitializeComponent();
            this.Load += FrmBlockFormAdd_Load;
        }

        // Edit mode (optional)
        public int id = 0; // 0 = insert, >0 = update

        private void FrmBlockFormAdd_Load(object sender, EventArgs e)
        {
            FillSocieties();
            // focus
            cbSociety.Focus();
        }

        // Ctrl+S shortcut
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.S))
            {
                btnSave_Click(this, EventArgs.Empty);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void FillSocieties()
        {
            // societies ko cbSociety me load karein
            string q = "SELECT id, name FROM societies ORDER BY name;";
            MainClass.LoadDataIntoComboBox(q, cbSociety, displayMember: "name", valueMember: "id");
            // SelectedIndex -1 already method me set hota hai
        }

        public override void btnClose_Click(object sender, EventArgs e)
        {
            SafeClose();
        }

        public override void btnSave_Click(object sender, EventArgs e)
        {
            if (cbSociety.SelectedValue == null)
            {
                MessageBox.Show("Society select karo.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cbSociety.DroppedDown = true; return;
            }
            if (string.IsNullOrWhiteSpace(txtBlockName.Text))
            {
                MessageBox.Show("Block name/code required hai.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtBlockName.Focus(); return;
            }

            int societyId = Convert.ToInt32(cbSociety.SelectedValue);
            string blockName = txtBlockName.Text.Trim();

            // (society_id, name) unique check
            string dupQry = "SELECT COUNT(1) FROM blocks WHERE society_id=@sid AND lower(name)=lower(@name) "
                          + (id > 0 ? "AND id<>@id" : "");
            var dupCnt = MainClass.ExecuteScalar(dupQry, new Dictionary<string, object> {
        {"@sid", societyId}, {"@name", blockName}, {"@id", id}
    });
            if (Convert.ToInt32(dupCnt ?? 0) > 0)
            {
                MessageBox.Show("Is society mein yeh block already mojood hai.", "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtBlockName.Focus(); return;
            }

            string qry;
            var p = new Dictionary<string, object> {
        {"@society_id", societyId},
        {"@name", blockName}
    };

            if (id == 0)
                qry = "INSERT INTO blocks (society_id, name) VALUES (@society_id, @name);";
            else
            {
                qry = "UPDATE blocks SET society_id=@society_id, name=@name WHERE id=@id;";
                p["@id"] = id;
            }

            int res = MainClass.ExecuteNonQuery(qry, p);
            if (res > 0)
            {
                new CustomeMessage(id == 0 ? "Block save ho gaya." : "Block update ho gaya.", "RealEstate Builders").ShowDialog();
                id = 0;
                cbSociety.SelectedIndex = -1;
                txtBlockName.Clear();
                cbSociety.Focus();
            }
            else
            {
                new ErrorFormMessage("Save/Update fail ho gaya.", "Error").ShowDialog();
            }
        }

        // OPTIONAL: Edit mode (agar view se call karna ho)
        public void LoadForEdit(int editId)
        {
            // ensure combos loaded
            if (cbSociety.DataSource == null)
            {
                string q = "SELECT id, name FROM societies ORDER BY name;";
                MainClass.LoadDataIntoComboBox(q, cbSociety, "name", "id");
            }

            id = editId;
            var dt = MainClass.ExecuteSelectQuery("SELECT id, society_id, name FROM blocks WHERE id=@id",
                new System.Collections.Hashtable { { "@id", id } });

            if (dt != null && dt.Rows.Count == 1)
            {
                cbSociety.SelectedValue = Convert.ToInt32(dt.Rows[0]["society_id"]);
                txtBlockName.Text = Convert.ToString(dt.Rows[0]["name"]);
            }
        }
    }
}
