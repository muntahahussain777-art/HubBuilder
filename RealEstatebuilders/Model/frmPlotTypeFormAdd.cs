using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace HubBuilders.Model
{
    public partial class frmPlotTypeAdd : SampleAdd
    {
        public frmPlotTypeAdd()
        {
            InitializeComponent();
            this.Load += FrmPlotTypeAdd_Load;

            // txtCode input filter (letters/digits/space/hyphen)
            this.txtCode.KeyPress += txtCode_KeyPress;
        }

        // 0 = insert, >0 = update
        public int id = 0;

        private void FrmPlotTypeAdd_Load(object sender, EventArgs e)
        {
            try
            {
                // Societies list fill
                string q = "SELECT id, name FROM societies ORDER BY name;";
                MainClass.LoadDataIntoComboBox(q, cbSociety, "name", "id");
                cbSociety.SelectedIndex = -1;

                // Agar societies hi nahi hain to guide karo
                if (cbSociety.Items.Count == 0)
                {
                    MessageBox.Show("Pehle Society add karein, phir Plot Type add hoga.",
                                    "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                cbSociety.Focus();
            }
            catch (Exception ex)
            {
                new ErrorFormMessage("Societies load nahi ho saki: " + ex.Message, "Error").ShowDialog();
            }
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

        public override void btnClose_Click(object sender, EventArgs e)
        {
            SafeClose();
        }

        public override void btnSave_Click(object sender, EventArgs e)
        {
            // ---- Validation
            if (cbSociety.SelectedValue == null || !int.TryParse(cbSociety.SelectedValue.ToString(), out var societyId))
            {
                MessageBox.Show("Society select karna zaroori hai.", "Validation",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cbSociety.DroppedDown = true;
                return;
            }

            // Normalize code: trim + multi-spaces -> single + uppercase
            string rawCode = txtCode.Text;
            string normCode = Regex.Replace(rawCode ?? "", @"\s+", " ").Trim();
            if (string.IsNullOrWhiteSpace(normCode))
            {
                MessageBox.Show("Plot Type code required hai.", "Validation",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCode.Focus();
                return;
            }
            // (optional) upper-case
            normCode = normCode.ToUpper();

            // ---- Duplicate check: (society_id, code) unique
            string dupQry = "SELECT COUNT(1) FROM plot_types WHERE society_id=@sid AND lower(code)=lower(@code) " +
                            (id > 0 ? "AND id<>@id" : "");
            var dupCnt = MainClass.ExecuteScalar(dupQry, new Dictionary<string, object>
            {
                {"@sid", societyId},
                {"@code", normCode},
                {"@id", id}
            });
            if (Convert.ToInt32(dupCnt ?? 0) > 0)
            {
                MessageBox.Show("Is society mein yeh plot type code already mojood hai.",
                                "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtCode.Focus();
                return;
            }

            // ---- Insert/Update
            string qry;
            var p = new Dictionary<string, object>
            {
                {"@society_id", societyId},
                {"@code", normCode}
            };

            if (id == 0)
            {
                qry = "INSERT INTO plot_types (society_id, code) VALUES (@society_id, @code);";
            }
            else
            {
                qry = "UPDATE plot_types SET society_id=@society_id, code=@code WHERE id=@id;";
                p["@id"] = id;
            }

            int res = MainClass.ExecuteNonQuery(qry, p);
            if (res > 0)
            {
                new CustomeMessage(id == 0 ? "Plot type save ho gaya." : "Plot type update ho gaya.",
                                   "RealEstate Builders").ShowDialog();
                ResetForm();
            }
            else
            {
                new ErrorFormMessage("Save/Update fail ho gaya.", "Error").ShowDialog();
            }
        }

        // Input filter: sirf letters, digits, space, hyphen
        private void txtCode_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;
            if (char.IsLetterOrDigit(e.KeyChar) || e.KeyChar == '-' || e.KeyChar == ' ') return;
            e.Handled = true;
        }

        private void ResetForm()
        {
            id = 0;
            cbSociety.SelectedIndex = -1;
            txtCode.Clear();
            cbSociety.Focus();
        }

        // ---- OPTIONAL: Edit mode loader (View se call karna ho to)
        public void LoadForEdit(int editId)
        {
            try
            {
                // ensure combos loaded
                if (cbSociety.DataSource == null)
                {
                    string q = "SELECT id, name FROM societies ORDER BY name;";
                    MainClass.LoadDataIntoComboBox(q, cbSociety, "name", "id");
                }

                id = editId;
                var dt = MainClass.ExecuteSelectQuery(
                    "SELECT id, society_id, code FROM plot_types WHERE id=@id",
                    new Hashtable { { "@id", id } });

                if (dt != null && dt.Rows.Count == 1)
                {
                    cbSociety.SelectedValue = Convert.ToInt32(dt.Rows[0]["society_id"]);
                    txtCode.Text = Convert.ToString(dt.Rows[0]["code"]);
                }
            }
            catch (Exception ex)
            {
                new ErrorFormMessage("Record load nahi ho saka: " + ex.Message, "Error").ShowDialog();
            }
        }
    }
}
