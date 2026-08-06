namespace HubBuilders.Model
{
    partial class frmReceiptFormAdd
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmReceiptFormAdd));
            this.dtpReceiptDate = new Guna.UI2.WinForms.Guna2DateTimePicker();
            this.label7 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.cmbMode = new Guna.UI2.WinForms.Guna2ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtRefNo = new Guna.UI2.WinForms.Guna2TextBox();
            this.guna2Panel1 = new Guna.UI2.WinForms.Guna2Panel();
            this.label5 = new System.Windows.Forms.Label();
            this.btnAllocateManual = new Guna.UI2.WinForms.Guna2Button();
            this.btnSaveReceipt = new Guna.UI2.WinForms.Guna2Button();
            this.grdReceiptView = new Guna.UI2.WinForms.Guna2DataGridView();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewImageColumn3 = new System.Windows.Forms.DataGridViewImageColumn();
            this.dataGridViewImageColumn4 = new System.Windows.Forms.DataGridViewImageColumn();
            this.label2 = new System.Windows.Forms.Label();
            this.cmbCustomerPlot = new Guna.UI2.WinForms.Guna2ComboBox();
            this.txtReceiptNotes = new Guna.UI2.WinForms.Guna2TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.numAmount = new Guna.UI2.WinForms.Guna2NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.guna2Panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdReceiptView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numAmount)).BeginInit();
            this.SuspendLayout();
            // 
            // dtpReceiptDate
            // 
            this.dtpReceiptDate.AutoRoundedCorners = true;
            this.dtpReceiptDate.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.dtpReceiptDate.BorderRadius = 19;
            this.dtpReceiptDate.BorderThickness = 1;
            this.dtpReceiptDate.CheckedState.Parent = this.dtpReceiptDate;
            this.dtpReceiptDate.CustomFormat = "yyyy-MM-dd";
            this.dtpReceiptDate.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.dtpReceiptDate.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dtpReceiptDate.ForeColor = System.Drawing.Color.White;
            this.dtpReceiptDate.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpReceiptDate.HoverState.Parent = this.dtpReceiptDate;
            this.dtpReceiptDate.Location = new System.Drawing.Point(1034, 114);
            this.dtpReceiptDate.MaxDate = new System.DateTime(9998, 12, 31, 0, 0, 0, 0);
            this.dtpReceiptDate.MinDate = new System.DateTime(1753, 1, 1, 0, 0, 0, 0);
            this.dtpReceiptDate.Name = "dtpReceiptDate";
            this.dtpReceiptDate.ShadowDecoration.Parent = this.dtpReceiptDate;
            this.dtpReceiptDate.Size = new System.Drawing.Size(267, 41);
            this.dtpReceiptDate.TabIndex = 4;
            this.dtpReceiptDate.Value = new System.DateTime(2024, 6, 21, 19, 54, 39, 787);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.ForeColor = System.Drawing.Color.White;
            this.label7.Location = new System.Drawing.Point(1047, 88);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(104, 21);
            this.label7.TabIndex = 137;
            this.label7.Text = "Receipt Date";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.Color.White;
            this.label3.Location = new System.Drawing.Point(780, 85);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(61, 21);
            this.label3.TabIndex = 131;
            this.label3.Text = "Ref No";
            // 
            // cmbMode
            // 
            this.cmbMode.AutoRoundedCorners = true;
            this.cmbMode.BackColor = System.Drawing.Color.Transparent;
            this.cmbMode.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.cmbMode.BorderRadius = 17;
            this.cmbMode.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cmbMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbMode.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.cmbMode.FocusedColor = System.Drawing.Color.Empty;
            this.cmbMode.FocusedState.Parent = this.cmbMode;
            this.cmbMode.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbMode.ForeColor = System.Drawing.Color.White;
            this.cmbMode.FormattingEnabled = true;
            this.cmbMode.HoverState.Parent = this.cmbMode;
            this.cmbMode.ItemHeight = 30;
            this.cmbMode.Items.AddRange(new object[] {
            "Cash",
            "Bank"});
            this.cmbMode.ItemsAppearance.Parent = this.cmbMode;
            this.cmbMode.Location = new System.Drawing.Point(517, 114);
            this.cmbMode.Name = "cmbMode";
            this.cmbMode.ShadowDecoration.Parent = this.cmbMode;
            this.cmbMode.Size = new System.Drawing.Size(245, 36);
            this.cmbMode.TabIndex = 2;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.White;
            this.label4.Location = new System.Drawing.Point(535, 90);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(54, 21);
            this.label4.TabIndex = 125;
            this.label4.Text = "Mode";
            // 
            // txtRefNo
            // 
            this.txtRefNo.AutoRoundedCorners = true;
            this.txtRefNo.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.txtRefNo.BorderRadius = 17;
            this.txtRefNo.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtRefNo.DefaultText = "";
            this.txtRefNo.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.txtRefNo.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.txtRefNo.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtRefNo.DisabledState.Parent = this.txtRefNo;
            this.txtRefNo.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtRefNo.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.txtRefNo.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtRefNo.FocusedState.Parent = this.txtRefNo;
            this.txtRefNo.Font = new System.Drawing.Font("Segoe UI Semibold", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtRefNo.ForeColor = System.Drawing.Color.White;
            this.txtRefNo.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtRefNo.HoverState.Parent = this.txtRefNo;
            this.txtRefNo.Location = new System.Drawing.Point(771, 114);
            this.txtRefNo.Margin = new System.Windows.Forms.Padding(6, 8, 6, 8);
            this.txtRefNo.Name = "txtRefNo";
            this.txtRefNo.PasswordChar = '\0';
            this.txtRefNo.PlaceholderText = "";
            this.txtRefNo.SelectedText = "";
            this.txtRefNo.ShadowDecoration.Parent = this.txtRefNo;
            this.txtRefNo.Size = new System.Drawing.Size(245, 36);
            this.txtRefNo.TabIndex = 3;
            // 
            // guna2Panel1
            // 
            this.guna2Panel1.Controls.Add(this.label5);
            this.guna2Panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.guna2Panel1.Location = new System.Drawing.Point(0, 0);
            this.guna2Panel1.Name = "guna2Panel1";
            this.guna2Panel1.ShadowDecoration.Parent = this.guna2Panel1;
            this.guna2Panel1.Size = new System.Drawing.Size(1312, 65);
            this.guna2Panel1.TabIndex = 144;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Segoe UI Black", 24F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.ForeColor = System.Drawing.Color.White;
            this.label5.Location = new System.Drawing.Point(21, 9);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(370, 45);
            this.label5.TabIndex = 145;
            this.label5.Text = "Customer Plot Receipt";
            // 
            // btnAllocateManual
            // 
            this.btnAllocateManual.Animated = true;
            this.btnAllocateManual.AutoRoundedCorners = true;
            this.btnAllocateManual.BorderRadius = 18;
            this.btnAllocateManual.CheckedState.Parent = this.btnAllocateManual;
            this.btnAllocateManual.CustomImages.Parent = this.btnAllocateManual;
            this.btnAllocateManual.FillColor = System.Drawing.Color.Crimson;
            this.btnAllocateManual.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnAllocateManual.ForeColor = System.Drawing.Color.White;
            this.btnAllocateManual.HoverState.Parent = this.btnAllocateManual;
            this.btnAllocateManual.Location = new System.Drawing.Point(941, 191);
            this.btnAllocateManual.Name = "btnAllocateManual";
            this.btnAllocateManual.ShadowDecoration.Parent = this.btnAllocateManual;
            this.btnAllocateManual.Size = new System.Drawing.Size(167, 38);
            this.btnAllocateManual.TabIndex = 7;
            this.btnAllocateManual.Text = "Close";
            // 
            // btnSaveReceipt
            // 
            this.btnSaveReceipt.Animated = true;
            this.btnSaveReceipt.AutoRoundedCorners = true;
            this.btnSaveReceipt.BorderRadius = 18;
            this.btnSaveReceipt.CheckedState.Parent = this.btnSaveReceipt;
            this.btnSaveReceipt.CustomImages.Parent = this.btnSaveReceipt;
            this.btnSaveReceipt.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.btnSaveReceipt.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSaveReceipt.ForeColor = System.Drawing.Color.White;
            this.btnSaveReceipt.HoverState.Parent = this.btnSaveReceipt;
            this.btnSaveReceipt.Location = new System.Drawing.Point(770, 192);
            this.btnSaveReceipt.Name = "btnSaveReceipt";
            this.btnSaveReceipt.ShadowDecoration.Parent = this.btnSaveReceipt;
            this.btnSaveReceipt.Size = new System.Drawing.Size(167, 38);
            this.btnSaveReceipt.TabIndex = 6;
            this.btnSaveReceipt.Text = "Save";
            // 
            // grdReceiptView
            // 
            this.grdReceiptView.AllowUserToAddRows = false;
            this.grdReceiptView.AllowUserToDeleteRows = false;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.grdReceiptView.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.grdReceiptView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grdReceiptView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdReceiptView.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(66)))));
            this.grdReceiptView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.grdReceiptView.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.grdReceiptView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(88)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.grdReceiptView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.grdReceiptView.ColumnHeadersHeight = 40;
            this.grdReceiptView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewTextBoxColumn3,
            this.dataGridViewTextBoxColumn4,
            this.dataGridViewImageColumn3,
            this.dataGridViewImageColumn4});
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(66)))));
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(66)))));
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.grdReceiptView.DefaultCellStyle = dataGridViewCellStyle3;
            this.grdReceiptView.EnableHeadersVisualStyles = false;
            this.grdReceiptView.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(66)))));
            this.grdReceiptView.Location = new System.Drawing.Point(11, 260);
            this.grdReceiptView.MultiSelect = false;
            this.grdReceiptView.Name = "grdReceiptView";
            this.grdReceiptView.ReadOnly = true;
            this.grdReceiptView.RowHeadersVisible = false;
            this.grdReceiptView.RowTemplate.Height = 30;
            this.grdReceiptView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grdReceiptView.Size = new System.Drawing.Size(1289, 460);
            this.grdReceiptView.TabIndex = 178;
            this.grdReceiptView.Theme = Guna.UI2.WinForms.Enums.DataGridViewPresetThemes.Default;
            this.grdReceiptView.ThemeStyle.AlternatingRowsStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.grdReceiptView.ThemeStyle.AlternatingRowsStyle.Font = null;
            this.grdReceiptView.ThemeStyle.AlternatingRowsStyle.ForeColor = System.Drawing.Color.Empty;
            this.grdReceiptView.ThemeStyle.AlternatingRowsStyle.SelectionBackColor = System.Drawing.Color.Empty;
            this.grdReceiptView.ThemeStyle.AlternatingRowsStyle.SelectionForeColor = System.Drawing.Color.Empty;
            this.grdReceiptView.ThemeStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(66)))));
            this.grdReceiptView.ThemeStyle.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(66)))));
            this.grdReceiptView.ThemeStyle.HeaderStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(88)))), ((int)(((byte)(255)))));
            this.grdReceiptView.ThemeStyle.HeaderStyle.BorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this.grdReceiptView.ThemeStyle.HeaderStyle.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.grdReceiptView.ThemeStyle.HeaderStyle.ForeColor = System.Drawing.Color.White;
            this.grdReceiptView.ThemeStyle.HeaderStyle.HeaightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            this.grdReceiptView.ThemeStyle.HeaderStyle.Height = 40;
            this.grdReceiptView.ThemeStyle.ReadOnly = true;
            this.grdReceiptView.ThemeStyle.RowsStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(66)))));
            this.grdReceiptView.ThemeStyle.RowsStyle.BorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.grdReceiptView.ThemeStyle.RowsStyle.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.grdReceiptView.ThemeStyle.RowsStyle.ForeColor = System.Drawing.Color.White;
            this.grdReceiptView.ThemeStyle.RowsStyle.Height = 30;
            this.grdReceiptView.ThemeStyle.RowsStyle.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(66)))));
            this.grdReceiptView.ThemeStyle.RowsStyle.SelectionForeColor = System.Drawing.Color.White;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.dataGridViewTextBoxColumn3.FillWeight = 50F;
            this.dataGridViewTextBoxColumn3.HeaderText = "Sr#";
            this.dataGridViewTextBoxColumn3.MinimumWidth = 50;
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.ReadOnly = true;
            this.dataGridViewTextBoxColumn3.Width = 50;
            // 
            // dataGridViewTextBoxColumn4
            // 
            this.dataGridViewTextBoxColumn4.HeaderText = "id";
            this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            this.dataGridViewTextBoxColumn4.ReadOnly = true;
            this.dataGridViewTextBoxColumn4.Visible = false;
            // 
            // dataGridViewImageColumn3
            // 
            this.dataGridViewImageColumn3.HeaderText = "";
            this.dataGridViewImageColumn3.Image = ((System.Drawing.Image)(resources.GetObject("dataGridViewImageColumn3.Image")));
            this.dataGridViewImageColumn3.ImageLayout = System.Windows.Forms.DataGridViewImageCellLayout.Zoom;
            this.dataGridViewImageColumn3.Name = "dataGridViewImageColumn3";
            this.dataGridViewImageColumn3.ReadOnly = true;
            // 
            // dataGridViewImageColumn4
            // 
            this.dataGridViewImageColumn4.HeaderText = "";
            this.dataGridViewImageColumn4.Image = ((System.Drawing.Image)(resources.GetObject("dataGridViewImageColumn4.Image")));
            this.dataGridViewImageColumn4.ImageLayout = System.Windows.Forms.DataGridViewImageCellLayout.Zoom;
            this.dataGridViewImageColumn4.Name = "dataGridViewImageColumn4";
            this.dataGridViewImageColumn4.ReadOnly = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.White;
            this.label2.Location = new System.Drawing.Point(21, 90);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(206, 21);
            this.label2.TabIndex = 124;
            this.label2.Text = "Allocation Customer Name";
            // 
            // cmbCustomerPlot
            // 
            this.cmbCustomerPlot.AutoRoundedCorners = true;
            this.cmbCustomerPlot.BackColor = System.Drawing.Color.Transparent;
            this.cmbCustomerPlot.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.cmbCustomerPlot.BorderRadius = 17;
            this.cmbCustomerPlot.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cmbCustomerPlot.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCustomerPlot.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.cmbCustomerPlot.FocusedColor = System.Drawing.Color.Empty;
            this.cmbCustomerPlot.FocusedState.Parent = this.cmbCustomerPlot;
            this.cmbCustomerPlot.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbCustomerPlot.ForeColor = System.Drawing.Color.White;
            this.cmbCustomerPlot.FormattingEnabled = true;
            this.cmbCustomerPlot.HoverState.Parent = this.cmbCustomerPlot;
            this.cmbCustomerPlot.ItemHeight = 30;
            this.cmbCustomerPlot.ItemsAppearance.Parent = this.cmbCustomerPlot;
            this.cmbCustomerPlot.Location = new System.Drawing.Point(12, 114);
            this.cmbCustomerPlot.Name = "cmbCustomerPlot";
            this.cmbCustomerPlot.ShadowDecoration.Parent = this.cmbCustomerPlot;
            this.cmbCustomerPlot.Size = new System.Drawing.Size(245, 36);
            this.cmbCustomerPlot.TabIndex = 0;
            // 
            // txtReceiptNotes
            // 
            this.txtReceiptNotes.AutoRoundedCorners = true;
            this.txtReceiptNotes.AutoSize = true;
            this.txtReceiptNotes.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.txtReceiptNotes.BorderRadius = 28;
            this.txtReceiptNotes.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtReceiptNotes.DefaultText = "";
            this.txtReceiptNotes.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.txtReceiptNotes.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.txtReceiptNotes.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtReceiptNotes.DisabledState.Parent = this.txtReceiptNotes;
            this.txtReceiptNotes.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtReceiptNotes.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.txtReceiptNotes.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtReceiptNotes.FocusedState.Parent = this.txtReceiptNotes;
            this.txtReceiptNotes.Font = new System.Drawing.Font("Segoe UI Semibold", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtReceiptNotes.ForeColor = System.Drawing.Color.White;
            this.txtReceiptNotes.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtReceiptNotes.HoverState.Parent = this.txtReceiptNotes;
            this.txtReceiptNotes.Location = new System.Drawing.Point(12, 191);
            this.txtReceiptNotes.Margin = new System.Windows.Forms.Padding(6, 8, 6, 8);
            this.txtReceiptNotes.Multiline = true;
            this.txtReceiptNotes.Name = "txtReceiptNotes";
            this.txtReceiptNotes.PasswordChar = '\0';
            this.txtReceiptNotes.PlaceholderText = "";
            this.txtReceiptNotes.SelectedText = "";
            this.txtReceiptNotes.ShadowDecoration.Parent = this.txtReceiptNotes;
            this.txtReceiptNotes.Size = new System.Drawing.Size(750, 58);
            this.txtReceiptNotes.TabIndex = 5;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.ForeColor = System.Drawing.Color.White;
            this.label8.Location = new System.Drawing.Point(40, 167);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(47, 21);
            this.label8.TabIndex = 138;
            this.label8.Text = "Note";
            // 
            // numAmount
            // 
            this.numAmount.AutoRoundedCorners = true;
            this.numAmount.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(61)))));
            this.numAmount.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.numAmount.BorderRadius = 17;
            this.numAmount.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.numAmount.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.numAmount.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.numAmount.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.numAmount.DisabledState.Parent = this.numAmount;
            this.numAmount.DisabledState.UpDownButtonFillColor = System.Drawing.Color.FromArgb(((int)(((byte)(177)))), ((int)(((byte)(177)))), ((int)(((byte)(177)))));
            this.numAmount.DisabledState.UpDownButtonForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(203)))), ((int)(((byte)(203)))), ((int)(((byte)(203)))));
            this.numAmount.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.numAmount.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.numAmount.FocusedState.Parent = this.numAmount;
            this.numAmount.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.numAmount.ForeColor = System.Drawing.Color.White;
            this.numAmount.Location = new System.Drawing.Point(263, 114);
            this.numAmount.Name = "numAmount";
            this.numAmount.ShadowDecoration.Parent = this.numAmount;
            this.numAmount.Size = new System.Drawing.Size(248, 36);
            this.numAmount.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(271, 90);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(69, 21);
            this.label1.TabIndex = 141;
            this.label1.Text = "Amount";
            // 
            // frmReceiptFormAdd
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1312, 747);
            this.Controls.Add(this.grdReceiptView);
            this.Controls.Add(this.btnAllocateManual);
            this.Controls.Add(this.btnSaveReceipt);
            this.Controls.Add(this.guna2Panel1);
            this.Controls.Add(this.txtRefNo);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.numAmount);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.txtReceiptNotes);
            this.Controls.Add(this.dtpReceiptDate);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.cmbCustomerPlot);
            this.Controls.Add(this.cmbMode);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label4);
            this.Name = "frmReceiptFormAdd";
            this.Text = "frmReceiptFormAdd";
            this.Load += new System.EventHandler(this.FrmReceiptFormAdd_Load);
            this.guna2Panel1.ResumeLayout(false);
            this.guna2Panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdReceiptView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numAmount)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        public Guna.UI2.WinForms.Guna2DateTimePicker dtpReceiptDate;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label3;
        public Guna.UI2.WinForms.Guna2ComboBox cmbMode;
        private System.Windows.Forms.Label label4;
        public Guna.UI2.WinForms.Guna2TextBox txtRefNo;
        private Guna.UI2.WinForms.Guna2Panel guna2Panel1;
        private System.Windows.Forms.Label label5;
        private Guna.UI2.WinForms.Guna2Button btnAllocateManual;
        private Guna.UI2.WinForms.Guna2Button btnSaveReceipt;
        private Guna.UI2.WinForms.Guna2DataGridView grdReceiptView;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private System.Windows.Forms.DataGridViewImageColumn dataGridViewImageColumn3;
        private System.Windows.Forms.DataGridViewImageColumn dataGridViewImageColumn4;
        private System.Windows.Forms.Label label2;
        public Guna.UI2.WinForms.Guna2ComboBox cmbCustomerPlot;
        public Guna.UI2.WinForms.Guna2TextBox txtReceiptNotes;
        private System.Windows.Forms.Label label8;
        private Guna.UI2.WinForms.Guna2NumericUpDown numAmount;
        private System.Windows.Forms.Label label1;
    }
}