using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace HubBuilders.Model
{
    /// <summary>Partner Cash Add/Edit — SampleAdd layout (ExpenseAdd jaisa fields form pe, Partner combo clear).</summary>
    public class frmPartnerCashAdd : SampleAdd
    {
        public long? EditId { get; set; }
        public int PartnerId { get; set; }

        private readonly Guna2ComboBox _cmbPartner = new Guna2ComboBox();
        private readonly Guna2ComboBox _cmbDir = new Guna2ComboBox();
        private readonly Guna2ComboBox _cmbCat = new Guna2ComboBox();
        private readonly Guna2ComboBox _cmbMode = new Guna2ComboBox();
        private readonly Guna2DateTimePicker _dtp = new Guna2DateTimePicker();
        private readonly Guna2TextBox _txtAmount = new Guna2TextBox();
        private readonly Guna2TextBox _txtNote = new Guna2TextBox();
        private readonly Guna2Button _btnNewCat = new Guna2Button();

        private readonly Label _lblPartner = new Label();
        private readonly Label _lblDate = new Label();
        private readonly Label _lblDir = new Label();
        private readonly Label _lblCat = new Label();
        private readonly Label _lblMode = new Label();
        private readonly Label _lblAmt = new Label();
        private readonly Label _lblNote = new Label();

        private readonly Panel _scrollHost = new Panel();

        private const int PartnerBlockHeight = 68;

        private static readonly Font UiFont = new Font("Segoe UI Semibold", 13f, FontStyle.Bold);
        private static readonly Font UiFontReg = new Font("Segoe UI", 13f, FontStyle.Regular);
        private static readonly Color DarkFill = Color.FromArgb(37, 41, 74);
        private static readonly Color Accent = Color.FromArgb(112, 51, 255);

        public frmPartnerCashAdd()
        {
            MainClass.EnsurePartnerMoneySchema();

            ClientSize = new Size(720, 620);
            MinimumSize = new Size(680, 560);

            try { label1.Text = EditId.HasValue ? "Edit Partner Cash" : "Add Partner Cash"; } catch { }

            BuildLayout();

            Load += FrmPartnerCashAdd_Load;
            Resize += (s, e) => LayoutPartnerAndScroll();
        }

        private void LayoutPartnerAndScroll()
        {
            // Partner combo hamesha header ke neeche — scroll ke bahar (screenshot: Date upar aa raha tha)
            const int headerH = 82;
            const int footerH = 71;
            int left = 20;
            int w = Math.Max(200, ClientSize.Width - left * 2);

            _lblPartner.SetBounds(left, headerH + 6, w, 22);
            _cmbPartner.SetBounds(left, headerH + 30, w, 36);

            int scrollTop = headerH + PartnerBlockHeight;
            int scrollH = Math.Max(120, ClientSize.Height - scrollTop - footerH);
            _scrollHost.SetBounds(0, scrollTop, ClientSize.Width, scrollH);
        }

        private void FrmPartnerCashAdd_Load(object sender, EventArgs e)
        {
            try { label1.Text = EditId.HasValue ? "Edit Partner Cash" : "Add Partner Cash"; } catch { }
            ApplyBottomButtonStyle();
            LayoutPartnerAndScroll();
            EnsurePanelsOnTop();

            LoadPartners();
            FillCategories();

            if (EditId.HasValue)
                LoadEdit();
            else if (PartnerId > 0)
            {
                try { _cmbPartner.SelectedValue = PartnerId; } catch { }
            }
        }

        private void EnsurePanelsOnTop()
        {
            _scrollHost.SendToBack();
            _lblPartner.BringToFront();
            _cmbPartner.BringToFront();
            foreach (Control c in Controls)
            {
                if (c is Panel p && (p.Dock == DockStyle.Top || p.Dock == DockStyle.Bottom))
                    p.BringToFront();
            }
        }

        private void BuildLayout()
        {
            // Partner — form pe (scroll ke bahar), LayoutPartnerAndScroll position karega
            _lblPartner.Text = "Partner *";
            _lblPartner.ForeColor = Color.White;
            _lblPartner.Font = UiFont;
            _lblPartner.AutoSize = true;
            Controls.Add(_lblPartner);

            StyleCombo(_cmbPartner);
            _cmbPartner.MaxDropDownItems = 25;
            Controls.Add(_cmbPartner);

            _scrollHost.AutoScroll = true;
            _scrollHost.Padding = new Padding(16, 8, 24, 12);
            Controls.Add(_scrollHost);

            int y = 4;
            const int left = 12;
            const int w = 620;

            void Lbl(Label l, string text)
            {
                l.Text = text;
                l.ForeColor = Color.White;
                l.Font = UiFont;
                l.AutoSize = true;
                l.Left = left;
                l.Top = y;
                _scrollHost.Controls.Add(l);
                y += 24;
            }

            Lbl(_lblDate, "Date");
            StyleDate(_dtp);
            _dtp.SetBounds(left, y, w, 36);
            _scrollHost.Controls.Add(_dtp);
            y += 46;

            Lbl(_lblDir, "Direction *  (In = paise aaye | Out = karcha)");
            StyleCombo(_cmbDir);
            _cmbDir.Items.AddRange(new object[] { "In", "Out" });
            _cmbDir.SelectedIndex = 1;
            _cmbDir.SelectedIndexChanged += (s, ev) => FillCategories();
            _cmbDir.SetBounds(left, y, w, 36);
            _scrollHost.Controls.Add(_cmbDir);
            y += 46;

            Lbl(_lblCat, "Category *");
            StyleCombo(_cmbCat);
            _cmbCat.SetBounds(left, y, 460, 36);
            _scrollHost.Controls.Add(_cmbCat);

            _btnNewCat.Text = "New Category";
            _btnNewCat.SetBounds(left + 472, y, 148, 36);
            _btnNewCat.BorderRadius = 12;
            _btnNewCat.FillColor = Color.FromArgb(0, 150, 136);
            _btnNewCat.ForeColor = Color.White;
            _btnNewCat.Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold);
            _btnNewCat.Click += (s, ev) => AddNewCategory();
            _scrollHost.Controls.Add(_btnNewCat);
            y += 46;

            Lbl(_lblMode, "Mode");
            StyleCombo(_cmbMode);
            _cmbMode.Items.AddRange(new object[] { "Cash", "Bank" });
            _cmbMode.SelectedIndex = 0;
            _cmbMode.SetBounds(left, y, w, 36);
            _scrollHost.Controls.Add(_cmbMode);
            y += 46;

            Lbl(_lblAmt, "Amount *");
            WireTextBox(_txtAmount);
            _txtAmount.PlaceholderText = "0.00";
            _txtAmount.SetBounds(left, y, w, 36);
            _scrollHost.Controls.Add(_txtAmount);
            y += 46;

            Lbl(_lblNote, "Note");
            WireTextBox(_txtNote);
            _txtNote.Multiline = true;
            _txtNote.SetBounds(left, y, w, 64);
            _scrollHost.Controls.Add(_txtNote);
            y += 78;

            _scrollHost.Controls.Add(new Panel { Left = 0, Top = y, Width = 8, Height = 40 });
        }

        private static void StyleCombo(Guna2ComboBox c)
        {
            c.DropDownStyle = ComboBoxStyle.DropDownList;
            c.AutoRoundedCorners = true;
            c.BackColor = Color.Transparent;
            c.FillColor = DarkFill;
            c.ForeColor = Color.White;
            c.Font = UiFontReg;
            c.BorderColor = Accent;
            c.BorderRadius = 14;
            c.ItemHeight = 30;
            // OwnerDrawFixed mat lagao — is se text blank rehta tha
            c.FocusedState.Parent = c;
            c.HoverState.Parent = c;
            c.ItemsAppearance.Parent = c;
            c.ShadowDecoration.Parent = c;
        }

        private static void StyleDate(Guna2DateTimePicker d)
        {
            d.FillColor = DarkFill;
            d.ForeColor = Color.White;
            d.Font = UiFontReg;
            d.BorderColor = Accent;
            d.BorderRadius = 14;
            d.Format = DateTimePickerFormat.Short;
            d.Value = DateTime.Today;
            d.Checked = true;
        }

        private static void WireTextBox(Guna2TextBox t)
        {
            t.FillColor = DarkFill;
            t.ForeColor = Color.White;
            t.Font = UiFontReg;
            t.BorderColor = Accent;
            t.BorderRadius = 12;
            t.FocusedState.Parent = t;
            t.HoverState.Parent = t;
            t.DisabledState.Parent = t;
            t.ShadowDecoration.Parent = t;
        }

        private void ApplyBottomButtonStyle()
        {
            try
            {
                foreach (Control c in Controls)
                    ApplyBottomButtonStyleRecursive(c);
            }
            catch { }
        }

        private void ApplyBottomButtonStyleRecursive(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is Guna2Button gb && (gb.Name == "btnSave" || gb.Name == "btnClose"))
                {
                    gb.ForeColor = Color.White;
                    gb.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
                    if (gb.Name == "btnSave")
                        gb.Text = EditId.HasValue ? "Update" : "Save";
                }
                if (c.Controls.Count > 0)
                    ApplyBottomButtonStyleRecursive(c);
            }
        }

        private void FillCategories(string selectName = null)
        {
            string dir = _cmbDir.SelectedItem?.ToString() ?? "Out";
            string keep = selectName ?? (_cmbCat.SelectedItem?.ToString() ?? "");
            _cmbCat.DataSource = null;
            _cmbCat.Items.Clear();
            try
            {
                var dt = MainClass.GetPartnerMoneyCategories(dir);
                foreach (DataRow r in dt.Rows)
                    _cmbCat.Items.Add(Convert.ToString(r["name"]));
            }
            catch { }

            if (!string.IsNullOrWhiteSpace(keep))
            {
                int ix = _cmbCat.Items.IndexOf(keep);
                if (ix < 0) { _cmbCat.Items.Add(keep); ix = _cmbCat.Items.Count - 1; }
                _cmbCat.SelectedIndex = ix;
            }
            else if (_cmbCat.Items.Count > 0)
            {
                _cmbCat.SelectedIndex = 0;
            }
        }

        private void AddNewCategory()
        {
            string dir = _cmbDir.SelectedItem?.ToString() ?? "Both";
            using (var dlg = new frmPartnerCategoryAdd(dir))
            {
                MainClass.BlurBackground(dlg);
                if (dlg.DialogResult == DialogResult.OK)
                {
                    if (!string.IsNullOrWhiteSpace(dlg.CategoryName))
                        FillCategories(dlg.CategoryName);
                    else
                        FillCategories();
                }
            }
            try
            {
                _scrollHost.AutoScrollPosition = new Point(0, 0);
            }
            catch { }
        }

        private void LoadPartners()
        {
            try
            {
                var dt = MainClass.GetData(
                    "SELECT person_id AS id, name FROM Person WHERE is_partner=1 ORDER BY name;");
                var table = dt.Clone();
                var hint = table.NewRow();
                hint["id"] = 0;
                hint["name"] = "— Partner select karein —";
                table.Rows.Add(hint);
                foreach (DataRow r in dt.Rows)
                    table.ImportRow(r);

                _cmbPartner.DisplayMember = "name";
                _cmbPartner.ValueMember = "id";
                _cmbPartner.DataSource = table;
                _cmbPartner.SelectedIndex = table.Rows.Count > 1 ? 0 : (table.Rows.Count == 1 ? 0 : -1);
            }
            catch (Exception ex)
            {
                AppSafety.ShowSoft(ex);
            }
        }

        private void LoadEdit()
        {
            var dt = MainClass.GetData("SELECT * FROM partner_money WHERE id=" + EditId.Value);
            if (dt.Rows.Count == 0) return;
            var r = dt.Rows[0];
            try { _cmbPartner.SelectedValue = Convert.ToInt32(r["partner_id"]); } catch { }
            try { _dtp.Value = Convert.ToDateTime(r["txn_date"]); } catch { }
            string dir = Convert.ToString(r["direction"]);
            _cmbDir.SelectedItem = dir == "In" ? "In" : "Out";
            FillCategories(Convert.ToString(r["category"] ?? ""));
            string mode = Convert.ToString(r["mode"] ?? "Cash");
            _cmbMode.SelectedItem = mode == "Bank" ? "Bank" : "Cash";
            _txtAmount.Text = Convert.ToDecimal(r["amount"]).ToString("0.00");
            _txtNote.Text = Convert.ToString(r["note"] ?? "");
        }

        private bool TryGetAmount(out decimal amount)
        {
            amount = 0;
            string s = (_txtAmount.Text ?? "").Replace(",", "").Trim();
            return decimal.TryParse(s, out amount) && amount > 0;
        }

        private int SelectedPartnerId()
        {
            try
            {
                if (_cmbPartner.SelectedValue == null || _cmbPartner.SelectedValue == DBNull.Value) return 0;
                return Convert.ToInt32(_cmbPartner.SelectedValue);
            }
            catch { return 0; }
        }

        public override void btnSave_Click(object sender, EventArgs e)
        {
            int pid = SelectedPartnerId();
            if (pid <= 0)
            {
                MessageBox.Show("Partner select karein. Pehle Partner Form se partner add karein.");
                _cmbPartner.DroppedDown = true;
                return;
            }
            if (!TryGetAmount(out decimal amt))
            {
                MessageBox.Show("Amount > 0 likhein.");
                _txtAmount.Focus();
                return;
            }
            if (_cmbCat.SelectedItem == null)
            {
                MessageBox.Show("Category select karein ya New Category se add karein.");
                return;
            }

            string dir = _cmbDir.SelectedItem?.ToString() ?? "Out";
            string cat = _cmbCat.SelectedItem?.ToString() ?? "";
            string mode = _cmbMode.SelectedItem?.ToString() ?? "Cash";
            string date = _dtp.Value.ToString("yyyy-MM-dd");
            string note = (_txtNote.Text ?? "").Trim();

            try
            {
                if (EditId.HasValue)
                {
                    int n = MainClass.ExecuteNonQuery(@"
UPDATE partner_money SET
  partner_id=@p, txn_date=@d, direction=@dir, amount=@amt,
  mode=@mode, category=@cat, note=@note
WHERE id=@id;",
                        new Dictionary<string, object>
                        {
                            { "@p", pid }, { "@d", date }, { "@dir", dir },
                            { "@amt", amt }, { "@mode", mode },
                            { "@cat", cat }, { "@note", note }, { "@id", EditId.Value }
                        });
                    if (n <= 0) { MessageBox.Show("Update fail."); return; }
                }
                else
                {
                    long id = MainClass.InsertPartnerMoney(
                        pid, date, dir, amt, mode, cat, null, null, null, note);
                    if (id <= 0) { MessageBox.Show("Save fail."); return; }
                }
                try
                {
                    MainClass.LogStaffEntry(EditId.HasValue ? "Update" : "Insert", "partner_money",
                        dir + " " + amt.ToString("N2") + " partner=" + pid);
                }
                catch { }
                DialogResult = DialogResult.OK;
                SafeClose();
            }
            catch (Exception ex)
            {
                AppSafety.ShowSoft(ex);
            }
        }
    }

    /// <summary>Category add/edit/delete — clear naam wala TextBox + Add button.</summary>
    public class frmPartnerCategoryAdd : Form
    {
        public string CategoryName { get; private set; }
        public string Direction { get; private set; }

        private readonly TextBox _txtName = new TextBox();
        private readonly ComboBox _cmbDir = new ComboBox();
        private readonly DataGridView _grid = new DataGridView();
        private readonly Button _btnAdd = new Button();
        private readonly Button _btnUpdate = new Button();
        private readonly Button _btnClose = new Button();
        private int? _editId;

        private static readonly Color Bg = Color.FromArgb(32, 36, 61);
        private static readonly Color FieldBg = Color.FromArgb(37, 41, 74);

        public frmPartnerCategoryAdd(string defaultDir)
        {
            Direction = string.IsNullOrWhiteSpace(defaultDir) ? "Both" : defaultDir;

            Text = "Manage Categories";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(560, 520);
            BackColor = Bg;
            KeyPreview = true;

            var title = new Label
            {
                Text = "Category Add / Edit / Delete",
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 14f, FontStyle.Bold),
                AutoSize = true,
                Left = 16,
                Top = 12
            };
            Controls.Add(title);

            _grid.SetBounds(16, 44, 528, 200);
            StyleGrid();
            BuildGridColumns();
            _grid.CellClick += Grid_CellClick;
            Controls.Add(_grid);

            var lblName = new Label
            {
                Text = "Category name likhein *",
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold),
                AutoSize = true,
                Left = 16,
                Top = 258
            };
            Controls.Add(lblName);

            _txtName.SetBounds(16, 286, 528, 32);
            _txtName.BackColor = FieldBg;
            _txtName.ForeColor = Color.White;
            _txtName.Font = new Font("Segoe UI", 13f);
            _txtName.BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(_txtName);

            var lblDir = new Label
            {
                Text = "Direction",
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold),
                AutoSize = true,
                Left = 16,
                Top = 328
            };
            Controls.Add(lblDir);

            _cmbDir.SetBounds(16, 356, 528, 32);
            _cmbDir.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbDir.BackColor = FieldBg;
            _cmbDir.ForeColor = Color.White;
            _cmbDir.Font = new Font("Segoe UI", 13f);
            _cmbDir.FlatStyle = FlatStyle.Flat;
            _cmbDir.Items.AddRange(new object[] { "In", "Out", "Both" });
            int ix = _cmbDir.Items.IndexOf(Direction);
            _cmbDir.SelectedIndex = ix >= 0 ? ix : 2;
            Controls.Add(_cmbDir);

            StyleBtn(_btnAdd, "Add", Color.FromArgb(112, 51, 255), 16, 404);
            _btnAdd.Click += (s, e) => SaveNewOrUpdate();
            Controls.Add(_btnAdd);

            StyleBtn(_btnUpdate, "Update", Color.FromArgb(0, 150, 136), 140, 404);
            _btnUpdate.Visible = false;
            _btnUpdate.Click += (s, e) => SaveNewOrUpdate();
            Controls.Add(_btnUpdate);

            StyleBtn(_btnClose, "Close", Color.Crimson, 432, 404);
            _btnClose.Click += (s, e) => CloseWithResult();
            Controls.Add(_btnClose);

            LoadGrid();
            Shown += (s, e) => { _txtName.Focus(); _txtName.SelectAll(); };
        }

        private static void StyleBtn(Button b, string text, Color back, int x, int y)
        {
            b.Text = text;
            b.SetBounds(x, y, 120, 40);
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = back;
            b.ForeColor = Color.White;
            b.Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold);
            b.Cursor = Cursors.Hand;
        }

        private void StyleGrid()
        {
            _grid.AutoGenerateColumns = false;
            _grid.AllowUserToAddRows = false;
            _grid.ReadOnly = true;
            _grid.RowHeadersVisible = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.BackgroundColor = FieldBg;
            _grid.GridColor = Color.FromArgb(60, 65, 90);
            _grid.BorderStyle = BorderStyle.FixedSingle;
            _grid.DefaultCellStyle.BackColor = FieldBg;
            _grid.DefaultCellStyle.ForeColor = Color.White;
            _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 121, 107);
            _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(25, 28, 50);
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            _grid.EnableHeadersVisualStyles = false;
        }

        private void BuildGridColumns()
        {
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colId", Visible = false });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colName",
                HeaderText = "Category",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colDir",
                HeaderText = "Dir",
                Width = 70
            });
            _grid.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colEdit",
                HeaderText = "",
                Text = "Edit",
                UseColumnTextForButtonValue = true,
                Width = 60
            });
            _grid.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colDel",
                HeaderText = "",
                Text = "Del",
                UseColumnTextForButtonValue = true,
                Width = 50
            });
        }

        private void LoadGrid()
        {
            _grid.Rows.Clear();
            var dt = MainClass.GetPartnerMoneyCategories(null);
            foreach (DataRow r in dt.Rows)
            {
                _grid.Rows.Add(
                    Convert.ToInt32(r["id"]),
                    Convert.ToString(r["name"]),
                    Convert.ToString(r["direction"]));
            }
        }

        private void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var col = _grid.Columns[e.ColumnIndex].Name;
            var row = _grid.Rows[e.RowIndex];
            if (row.Cells["colId"].Value == null
                || !int.TryParse(Convert.ToString(row.Cells["colId"].Value), out int id)
                || id <= 0)
                return;
            string name = Convert.ToString(row.Cells["colName"].Value);
            string dir = Convert.ToString(row.Cells["colDir"].Value);

            if (col == "colEdit")
            {
                _editId = id;
                _txtName.Text = name;
                int i = _cmbDir.Items.IndexOf(dir);
                _cmbDir.SelectedIndex = i >= 0 ? i : 0;
                _btnAdd.Visible = false;
                _btnUpdate.Visible = true;
                _txtName.Focus();
            }
            else if (col == "colDel")
            {
                if (MessageBox.Show("Delete '" + name + "'?", "Confirm",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
                if (MainClass.DeletePartnerMoneyCategory(id))
                {
                    if (_editId == id) ResetEntry();
                    LoadGrid();
                }
            }
        }

        private void SaveNewOrUpdate()
        {
            string n = (_txtName.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(n))
            {
                MessageBox.Show("Category name likhein.");
                _txtName.Focus();
                return;
            }
            string dir = _cmbDir.SelectedItem?.ToString() ?? "Both";

            bool wasUpdate = _editId.HasValue;
            bool ok = wasUpdate
                ? MainClass.UpdatePartnerMoneyCategory(_editId.Value, n, dir)
                : MainClass.AddPartnerMoneyCategory(n, dir);

            if (!ok)
            {
                MessageBox.Show("Save fail — shayad yeh naam pehle se hai.");
                return;
            }

            CategoryName = n;
            Direction = dir;
            LoadGrid();
            ResetEntry();
            MessageBox.Show(wasUpdate ? "Update ho gayi." : "Category add ho gayi.", "OK");
        }

        private void ResetEntry()
        {
            _editId = null;
            _txtName.Clear();
            _btnAdd.Visible = true;
            _btnUpdate.Visible = false;
            _txtName.Focus();
        }

        private void CloseWithResult()
        {
            DialogResult = !string.IsNullOrWhiteSpace(CategoryName) ? DialogResult.OK : DialogResult.Cancel;
            Close();
        }
    }
}
