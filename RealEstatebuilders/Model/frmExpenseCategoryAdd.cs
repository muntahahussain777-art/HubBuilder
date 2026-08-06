using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace HubBuilders.Model
{
    /// <summary>Expense categories — add / edit / delete (alag se), phir combo se select.</summary>
    public class frmExpenseCategoryAdd : Form
    {
        public string CategoryName { get; private set; }

        private readonly TextBox _txtName = new TextBox();
        private readonly DataGridView _grid = new DataGridView();
        private readonly Button _btnAdd = new Button();
        private readonly Button _btnUpdate = new Button();
        private readonly Button _btnClose = new Button();
        private int? _editId;
        private bool _anySaved;

        private static readonly Color Bg = Color.FromArgb(32, 36, 61);
        private static readonly Color FieldBg = Color.FromArgb(37, 41, 74);

        public frmExpenseCategoryAdd()
        {
            Text = "Expense Categories";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(520, 480);
            BackColor = Bg;
            KeyPreview = true;

            Controls.Add(new Label
            {
                Text = "Category Add / Edit / Delete",
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 14f, FontStyle.Bold),
                AutoSize = true,
                Left = 16,
                Top = 12
            });

            _grid.SetBounds(16, 44, 488, 200);
            StyleGrid();
            BuildGridColumns();
            _grid.CellClick += Grid_CellClick;
            Controls.Add(_grid);

            Controls.Add(new Label
            {
                Text = "Category name likhein *",
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold),
                AutoSize = true,
                Left = 16,
                Top = 258
            });

            _txtName.SetBounds(16, 286, 488, 32);
            _txtName.BackColor = FieldBg;
            _txtName.ForeColor = Color.White;
            _txtName.Font = new Font("Segoe UI", 13f);
            _txtName.BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(_txtName);

            StyleBtn(_btnAdd, "Add", Color.FromArgb(112, 51, 255), 16, 336);
            _btnAdd.Click += (s, e) => SaveNewOrUpdate();
            Controls.Add(_btnAdd);

            StyleBtn(_btnUpdate, "Update", Color.FromArgb(0, 150, 136), 146, 336);
            _btnUpdate.Visible = false;
            _btnUpdate.Click += (s, e) => SaveNewOrUpdate();
            Controls.Add(_btnUpdate);

            StyleBtn(_btnClose, "Close", Color.Crimson, 384, 336);
            _btnClose.Click += (s, e) => CloseWithResult();
            Controls.Add(_btnClose);

            LoadGrid();
            Shown += (s, e) => _txtName.Focus();
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
        }

        private void StyleGrid()
        {
            _grid.AutoGenerateColumns = false;
            _grid.AllowUserToAddRows = false;
            _grid.ReadOnly = true;
            _grid.RowHeadersVisible = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.BackgroundColor = FieldBg;
            _grid.ForeColor = Color.White;
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
            _grid.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colEdit",
                Text = "Edit",
                UseColumnTextForButtonValue = true,
                Width = 60
            });
            _grid.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colDel",
                Text = "Del",
                UseColumnTextForButtonValue = true,
                Width = 50
            });
        }

        private void LoadGrid()
        {
            _grid.Rows.Clear();
            var dt = MainClass.GetExpenseCategories();
            foreach (DataRow r in dt.Rows)
                _grid.Rows.Add(Convert.ToInt32(r["id"]), Convert.ToString(r["name"]));
        }

        private void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var col = _grid.Columns[e.ColumnIndex].Name;
            var row = _grid.Rows[e.RowIndex];
            int id = Convert.ToInt32(row.Cells["colId"].Value);
            string name = Convert.ToString(row.Cells["colName"].Value);

            if (col == "colEdit")
            {
                _editId = id;
                _txtName.Text = name;
                _btnAdd.Visible = false;
                _btnUpdate.Visible = true;
                _txtName.Focus();
            }
            else if (col == "colDel")
            {
                if (MessageBox.Show("Delete '" + name + "'?", "Confirm",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
                if (MainClass.DeleteExpenseCategory(id))
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

            bool wasUpdate = _editId.HasValue;
            bool ok = wasUpdate
                ? MainClass.UpdateExpenseCategory(_editId.Value, n)
                : MainClass.AddExpenseCategory(n);

            if (!ok)
            {
                MessageBox.Show("Save fail — shayad naam pehle se mojood hai.");
                return;
            }

            CategoryName = n;
            _anySaved = true;
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
            DialogResult = _anySaved || !string.IsNullOrWhiteSpace(CategoryName)
                ? DialogResult.OK
                : DialogResult.Cancel;
            Close();
        }
    }
}
