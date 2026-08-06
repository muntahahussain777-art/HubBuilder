using System;
using System.Collections;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace HubBuilders.Model
{
    public partial class frmExpenseAdd : SampleAdd
    {
        public frmExpenseAdd()
        {
            InitializeComponent();
            KeyPreview = true;
        }

        public int id = 0;

        private Guna2ComboBox _cmbCategory;
        private Guna2Button _btnNewCat;
        private bool _categoryUiReady;

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.S))
            {
                btnSave_Click(this, EventArgs.Empty);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void frmExpenseAdd_Load(object sender, EventArgs e)
        {
            EnsureCategoryCombo();
            LoadCategories();

            if (id > 0)
            {
                DataTable dt = MainClass.GetData("Select * from Expensetbl where sid = " + id);
                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    txtname.Text = row["Name"].ToString();
                    txtamount.Text = row["Amount"].ToString();
                    SelectCategory(Convert.ToString(row["Category"]));
                    txtnotes.Text = row["Note"].ToString();
                    txtdate.Value = Convert.ToDateTime(row["EDate"]);
                }
            }
            else
            {
                txtdate.Value = DateTime.Today;
            }
        }

        /// <summary>Designer txtcat ki jagah combo — layout same position.</summary>
        private void EnsureCategoryCombo()
        {
            if (_categoryUiReady) return;
            _categoryUiReady = true;

            MainClass.EnsureExpenseCategorySchema();
            txtcat.Visible = false;

            _cmbCategory = new Guna2ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = txtcat.Location,
                Size = new Size(128, txtcat.Height),
                FillColor = Color.FromArgb(32, 36, 61),
                ForeColor = Color.White,
                Font = txtcat.Font,
                BorderColor = txtcat.BorderColor,
                BorderRadius = txtcat.BorderRadius,
                ItemHeight = 30
            };
            _cmbCategory.FocusedState.Parent = _cmbCategory;
            _cmbCategory.HoverState.Parent = _cmbCategory;
            _cmbCategory.ItemsAppearance.Parent = _cmbCategory;
            _cmbCategory.ShadowDecoration.Parent = _cmbCategory;

            _btnNewCat = new Guna2Button
            {
                Text = "New",
                Location = new Point(txtcat.Left + 132, txtcat.Top),
                Size = new Size(62, txtcat.Height),
                BorderRadius = 12,
                FillColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold)
            };
            _btnNewCat.Click += (s, ev) =>
            {
                using (var dlg = new frmExpenseCategoryAdd())
                {
                    MainClass.BlurBackground(dlg);
                    if (dlg.DialogResult == DialogResult.OK && !string.IsNullOrWhiteSpace(dlg.CategoryName))
                        LoadCategories(dlg.CategoryName);
                    else
                        LoadCategories();
                }
            };

            Controls.Add(_cmbCategory);
            Controls.Add(_btnNewCat);
            _cmbCategory.BringToFront();
            _btnNewCat.BringToFront();
        }

        private void LoadCategories(string selectName = null)
        {
            if (_cmbCategory == null) return;
            _cmbCategory.Items.Clear();
            try
            {
                var dt = MainClass.GetExpenseCategories();
                foreach (DataRow r in dt.Rows)
                    _cmbCategory.Items.Add(Convert.ToString(r["name"]));
            }
            catch { }

            if (!string.IsNullOrWhiteSpace(selectName))
                SelectCategory(selectName);
            else if (_cmbCategory.Items.Count > 0)
                _cmbCategory.SelectedIndex = 0;
        }

        private void SelectCategory(string cat)
        {
            if (_cmbCategory == null || string.IsNullOrWhiteSpace(cat)) return;
            int ix = _cmbCategory.Items.IndexOf(cat.Trim());
            if (ix < 0)
            {
                _cmbCategory.Items.Add(cat.Trim());
                ix = _cmbCategory.Items.Count - 1;
            }
            _cmbCategory.SelectedIndex = ix;
        }

        private string SelectedCategory()
        {
            if (_cmbCategory?.SelectedItem == null) return "";
            return Convert.ToString(_cmbCategory.SelectedItem);
        }

        public override void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtname.Text))
            {
                MessageBox.Show("Name is required.");
                return;
            }
            if (!decimal.TryParse((txtamount.Text ?? "").Trim(), out decimal amt) || amt < 0)
            {
                MessageBox.Show("Valid amount required.");
                txtamount.Focus();
                return;
            }
            string cat = SelectedCategory();
            if (string.IsNullOrWhiteSpace(cat))
            {
                MessageBox.Show("Category select karein ya New se add karein.");
                return;
            }

            try
            {
                string qry;
                if (id == 0)
                {
                    qry = "Insert into Expensetbl (Name, Amount, Category,Note, EDate) " +
                          "Values (@name, @amount, @cat,@note, @date)";
                }
                else
                {
                    qry = "Update Expensetbl set Name = @name, Amount = @amount, Category = @cat, Note = @note, EDate = @date where sid = @id";
                }

                Hashtable ht = new Hashtable
                {
                    { "@id", id },
                    { "@name", txtname.Text },
                    { "@amount", amt },
                    { "@cat", cat },
                    { "@note", txtnotes.Text },
                    { "@date", txtdate.Value.ToString("yyyy-MM-dd") },
                };

                int r = MainClass.DataInsertUpdateDelete(qry, ht);
                if (r > 0)
                {
                    MainClass.AddExpenseCategory(cat);
                    CustomeMessage customMessageBox = new CustomeMessage("کامیابی کے ساتھ محفوظ کر لیا گیا", "");
                    customMessageBox.ShowDialog();
                    MainClass.Enable_reset(this);
                    id = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void txtamount_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
            {
                e.Handled = true;
                CustomeMessage customMessageBox = new CustomeMessage("آپ یہاں صرف نمبر درج کر سکتے ہیں۔", "غلطی");
                customMessageBox.ShowDialog();
            }

            if (e.KeyChar == '.' && (sender as TextBox).Text.Contains("."))
            {
                e.Handled = true;
            }
        }
    }
}
