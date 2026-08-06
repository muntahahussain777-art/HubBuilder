namespace HubBuilders.Model
{
    partial class frmCustomerLedgerAdd
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmCustomerLedgerAdd));
            this.label5 = new System.Windows.Forms.Label();
            this.guna2Panel1 = new Guna.UI2.WinForms.Guna2Panel();
            this.btnLoad = new Guna.UI2.WinForms.Guna2Button();
            this.dtpFrom = new Guna.UI2.WinForms.Guna2DateTimePicker();
            this.label7 = new System.Windows.Forms.Label();
            this.cmbCustomer = new Guna.UI2.WinForms.Guna2ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.cmbCustomerPlot = new Guna.UI2.WinForms.Guna2ComboBox();
            this.cmbSociety = new Guna.UI2.WinForms.Guna2ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.dtpTo = new Guna.UI2.WinForms.Guna2DateTimePicker();
            this.label8 = new System.Windows.Forms.Label();
            this.guna2Panel2 = new Guna.UI2.WinForms.Guna2Panel();
            this.lblBalance = new System.Windows.Forms.Label();
            this.lblClosingBalance = new System.Windows.Forms.Label();
            this.lblOpeningBalance = new System.Windows.Forms.Label();
            this.grdLedger = new Guna.UI2.WinForms.Guna2DataGridView();
            this.dgvSr = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgvid = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgvedit = new System.Windows.Forms.DataGridViewImageColumn();
            this.dgvdel = new System.Windows.Forms.DataGridViewImageColumn();
            this.btnPrint = new Guna.UI2.WinForms.Guna2Button();
            this.guna2Panel1.SuspendLayout();
            this.guna2Panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdLedger)).BeginInit();
            this.SuspendLayout();
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Segoe UI Black", 24F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.ForeColor = System.Drawing.Color.White;
            this.label5.Location = new System.Drawing.Point(21, 9);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(406, 45);
            this.label5.TabIndex = 145;
            this.label5.Text = "Filter Panel Ledger Form";
            // 
            // guna2Panel1
            // 
            this.guna2Panel1.Controls.Add(this.label5);
            this.guna2Panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.guna2Panel1.Location = new System.Drawing.Point(0, 0);
            this.guna2Panel1.Name = "guna2Panel1";
            this.guna2Panel1.ShadowDecoration.Parent = this.guna2Panel1;
            this.guna2Panel1.Size = new System.Drawing.Size(1343, 65);
            this.guna2Panel1.TabIndex = 145;
            // 
            // btnLoad
            // 
            this.btnLoad.Animated = true;
            this.btnLoad.AutoRoundedCorners = true;
            this.btnLoad.BorderRadius = 14;
            this.btnLoad.CheckedState.Parent = this.btnLoad;
            this.btnLoad.CustomImages.Parent = this.btnLoad;
            this.btnLoad.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.btnLoad.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnLoad.ForeColor = System.Drawing.Color.White;
            this.btnLoad.HoverState.Parent = this.btnLoad;
            this.btnLoad.Location = new System.Drawing.Point(578, 176);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.ShadowDecoration.Parent = this.btnLoad;
            this.btnLoad.Size = new System.Drawing.Size(135, 30);
            this.btnLoad.TabIndex = 5;
            this.btnLoad.Text = "Load";
            this.btnLoad.Click += new System.EventHandler(this.btnLoad_Click);
            // 
            // dtpFrom
            // 
            this.dtpFrom.AutoRoundedCorners = true;
            this.dtpFrom.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.dtpFrom.BorderRadius = 19;
            this.dtpFrom.BorderThickness = 1;
            this.dtpFrom.CheckedState.Parent = this.dtpFrom;
            this.dtpFrom.CustomFormat = "yyyy-MM-dd";
            this.dtpFrom.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.dtpFrom.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dtpFrom.ForeColor = System.Drawing.Color.White;
            this.dtpFrom.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpFrom.HoverState.Parent = this.dtpFrom;
            this.dtpFrom.Location = new System.Drawing.Point(26, 170);
            this.dtpFrom.MaxDate = new System.DateTime(9998, 12, 31, 0, 0, 0, 0);
            this.dtpFrom.MinDate = new System.DateTime(1753, 1, 1, 0, 0, 0, 0);
            this.dtpFrom.Name = "dtpFrom";
            this.dtpFrom.ShadowDecoration.Parent = this.dtpFrom;
            this.dtpFrom.Size = new System.Drawing.Size(267, 41);
            this.dtpFrom.TabIndex = 3;
            this.dtpFrom.Tag = "";
            this.dtpFrom.Value = new System.DateTime(2024, 6, 21, 19, 54, 39, 787);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.ForeColor = System.Drawing.Color.White;
            this.label7.Location = new System.Drawing.Point(39, 144);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(48, 21);
            this.label7.TabIndex = 151;
            this.label7.Text = "From";
            // 
            // cmbCustomer
            // 
            this.cmbCustomer.AutoRoundedCorners = true;
            this.cmbCustomer.BackColor = System.Drawing.Color.Transparent;
            this.cmbCustomer.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.cmbCustomer.BorderRadius = 17;
            this.cmbCustomer.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cmbCustomer.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCustomer.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.cmbCustomer.FocusedColor = System.Drawing.Color.Empty;
            this.cmbCustomer.FocusedState.Parent = this.cmbCustomer;
            this.cmbCustomer.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbCustomer.ForeColor = System.Drawing.Color.White;
            this.cmbCustomer.FormattingEnabled = true;
            this.cmbCustomer.HoverState.Parent = this.cmbCustomer;
            this.cmbCustomer.ItemHeight = 30;
            this.cmbCustomer.ItemsAppearance.Parent = this.cmbCustomer;
            this.cmbCustomer.Location = new System.Drawing.Point(29, 98);
            this.cmbCustomer.Name = "cmbCustomer";
            this.cmbCustomer.ShadowDecoration.Parent = this.cmbCustomer;
            this.cmbCustomer.Size = new System.Drawing.Size(245, 36);
            this.cmbCustomer.TabIndex = 0;
            this.cmbCustomer.Tag = "";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.White;
            this.label2.Location = new System.Drawing.Point(38, 74);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(81, 21);
            this.label2.TabIndex = 146;
            this.label2.Text = "Customer";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.Color.White;
            this.label3.Location = new System.Drawing.Point(544, 74);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(67, 21);
            this.label3.TabIndex = 160;
            this.label3.Text = "Plot NO";
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
            this.cmbCustomerPlot.Location = new System.Drawing.Point(537, 98);
            this.cmbCustomerPlot.Name = "cmbCustomerPlot";
            this.cmbCustomerPlot.ShadowDecoration.Parent = this.cmbCustomerPlot;
            this.cmbCustomerPlot.Size = new System.Drawing.Size(245, 36);
            this.cmbCustomerPlot.TabIndex = 2;
            this.cmbCustomerPlot.Tag = "";
            // 
            // cmbSociety
            // 
            this.cmbSociety.AutoRoundedCorners = true;
            this.cmbSociety.BackColor = System.Drawing.Color.Transparent;
            this.cmbSociety.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.cmbSociety.BorderRadius = 17;
            this.cmbSociety.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cmbSociety.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSociety.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.cmbSociety.FocusedColor = System.Drawing.Color.Empty;
            this.cmbSociety.FocusedState.Parent = this.cmbSociety;
            this.cmbSociety.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbSociety.ForeColor = System.Drawing.Color.White;
            this.cmbSociety.FormattingEnabled = true;
            this.cmbSociety.HoverState.Parent = this.cmbSociety;
            this.cmbSociety.ItemHeight = 30;
            this.cmbSociety.ItemsAppearance.Parent = this.cmbSociety;
            this.cmbSociety.Location = new System.Drawing.Point(280, 98);
            this.cmbSociety.Name = "cmbSociety";
            this.cmbSociety.ShadowDecoration.Parent = this.cmbSociety;
            this.cmbSociety.Size = new System.Drawing.Size(245, 36);
            this.cmbSociety.TabIndex = 1;
            this.cmbSociety.Tag = "";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.ForeColor = System.Drawing.Color.White;
            this.label6.Location = new System.Drawing.Point(298, 74);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(64, 21);
            this.label6.TabIndex = 157;
            this.label6.Text = "Society";
            // 
            // dtpTo
            // 
            this.dtpTo.AutoRoundedCorners = true;
            this.dtpTo.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.dtpTo.BorderRadius = 19;
            this.dtpTo.BorderThickness = 1;
            this.dtpTo.CheckedState.Parent = this.dtpTo;
            this.dtpTo.CustomFormat = "yyyy-MM-dd";
            this.dtpTo.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.dtpTo.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dtpTo.ForeColor = System.Drawing.Color.White;
            this.dtpTo.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpTo.HoverState.Parent = this.dtpTo;
            this.dtpTo.Location = new System.Drawing.Point(299, 170);
            this.dtpTo.MaxDate = new System.DateTime(9998, 12, 31, 0, 0, 0, 0);
            this.dtpTo.MinDate = new System.DateTime(1753, 1, 1, 0, 0, 0, 0);
            this.dtpTo.Name = "dtpTo";
            this.dtpTo.ShadowDecoration.Parent = this.dtpTo;
            this.dtpTo.Size = new System.Drawing.Size(267, 41);
            this.dtpTo.TabIndex = 4;
            this.dtpTo.Tag = "";
            this.dtpTo.Value = new System.DateTime(2024, 6, 21, 19, 54, 39, 787);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.ForeColor = System.Drawing.Color.White;
            this.label8.Location = new System.Drawing.Point(312, 144);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(27, 21);
            this.label8.TabIndex = 162;
            this.label8.Text = "To";
            // 
            // guna2Panel2
            // 
            this.guna2Panel2.Controls.Add(this.lblBalance);
            this.guna2Panel2.Controls.Add(this.lblClosingBalance);
            this.guna2Panel2.Controls.Add(this.lblOpeningBalance);
            this.guna2Panel2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.guna2Panel2.Location = new System.Drawing.Point(0, 723);
            this.guna2Panel2.Name = "guna2Panel2";
            this.guna2Panel2.ShadowDecoration.Parent = this.guna2Panel2;
            this.guna2Panel2.Size = new System.Drawing.Size(1343, 65);
            this.guna2Panel2.TabIndex = 146;
            // 
            // lblBalance
            // 
            this.lblBalance.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblBalance.AutoSize = true;
            this.lblBalance.Font = new System.Drawing.Font("Segoe UI Semibold", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblBalance.ForeColor = System.Drawing.Color.White;
            this.lblBalance.Location = new System.Drawing.Point(1133, 18);
            this.lblBalance.Name = "lblBalance";
            this.lblBalance.Size = new System.Drawing.Size(162, 30);
            this.lblBalance.TabIndex = 147;
            this.lblBalance.Text = "Closing Balance";
            // 
            // lblClosingBalance
            // 
            this.lblClosingBalance.AutoSize = true;
            this.lblClosingBalance.Font = new System.Drawing.Font("Segoe UI Semibold", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblClosingBalance.ForeColor = System.Drawing.Color.White;
            this.lblClosingBalance.Location = new System.Drawing.Point(573, 18);
            this.lblClosingBalance.Name = "lblClosingBalance";
            this.lblClosingBalance.Size = new System.Drawing.Size(162, 30);
            this.lblClosingBalance.TabIndex = 146;
            this.lblClosingBalance.Text = "Closing Balance";
            // 
            // lblOpeningBalance
            // 
            this.lblOpeningBalance.AutoSize = true;
            this.lblOpeningBalance.Font = new System.Drawing.Font("Segoe UI Semibold", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblOpeningBalance.ForeColor = System.Drawing.Color.White;
            this.lblOpeningBalance.Location = new System.Drawing.Point(21, 18);
            this.lblOpeningBalance.Name = "lblOpeningBalance";
            this.lblOpeningBalance.Size = new System.Drawing.Size(174, 30);
            this.lblOpeningBalance.TabIndex = 145;
            this.lblOpeningBalance.Text = "Opening Balance";
            // 
            // grdLedger
            // 
            this.grdLedger.AllowUserToAddRows = false;
            this.grdLedger.AllowUserToDeleteRows = false;
            dataGridViewCellStyle4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.grdLedger.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle4;
            this.grdLedger.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grdLedger.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdLedger.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(66)))));
            this.grdLedger.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.grdLedger.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.grdLedger.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle5.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(88)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle5.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle5.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle5.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle5.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle5.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.grdLedger.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle5;
            this.grdLedger.ColumnHeadersHeight = 40;
            this.grdLedger.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dgvSr,
            this.dgvid,
            this.dgvedit,
            this.dgvdel});
            dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle6.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(66)))));
            dataGridViewCellStyle6.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle6.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle6.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(66)))));
            dataGridViewCellStyle6.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.grdLedger.DefaultCellStyle = dataGridViewCellStyle6;
            this.grdLedger.EnableHeadersVisualStyles = false;
            this.grdLedger.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(66)))));
            this.grdLedger.Location = new System.Drawing.Point(12, 224);
            this.grdLedger.MultiSelect = false;
            this.grdLedger.Name = "grdLedger";
            this.grdLedger.ReadOnly = true;
            this.grdLedger.RowHeadersVisible = false;
            this.grdLedger.RowTemplate.Height = 30;
            this.grdLedger.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grdLedger.Size = new System.Drawing.Size(1316, 475);
            this.grdLedger.TabIndex = 168;
            this.grdLedger.Theme = Guna.UI2.WinForms.Enums.DataGridViewPresetThemes.Default;
            this.grdLedger.ThemeStyle.AlternatingRowsStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.grdLedger.ThemeStyle.AlternatingRowsStyle.Font = null;
            this.grdLedger.ThemeStyle.AlternatingRowsStyle.ForeColor = System.Drawing.Color.Empty;
            this.grdLedger.ThemeStyle.AlternatingRowsStyle.SelectionBackColor = System.Drawing.Color.Empty;
            this.grdLedger.ThemeStyle.AlternatingRowsStyle.SelectionForeColor = System.Drawing.Color.Empty;
            this.grdLedger.ThemeStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(66)))));
            this.grdLedger.ThemeStyle.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(66)))));
            this.grdLedger.ThemeStyle.HeaderStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(88)))), ((int)(((byte)(255)))));
            this.grdLedger.ThemeStyle.HeaderStyle.BorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this.grdLedger.ThemeStyle.HeaderStyle.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.grdLedger.ThemeStyle.HeaderStyle.ForeColor = System.Drawing.Color.White;
            this.grdLedger.ThemeStyle.HeaderStyle.HeaightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            this.grdLedger.ThemeStyle.HeaderStyle.Height = 40;
            this.grdLedger.ThemeStyle.ReadOnly = true;
            this.grdLedger.ThemeStyle.RowsStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(66)))));
            this.grdLedger.ThemeStyle.RowsStyle.BorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.grdLedger.ThemeStyle.RowsStyle.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.grdLedger.ThemeStyle.RowsStyle.ForeColor = System.Drawing.Color.White;
            this.grdLedger.ThemeStyle.RowsStyle.Height = 30;
            this.grdLedger.ThemeStyle.RowsStyle.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(66)))));
            this.grdLedger.ThemeStyle.RowsStyle.SelectionForeColor = System.Drawing.Color.White;
            // 
            // dgvSr
            // 
            this.dgvSr.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.dgvSr.FillWeight = 50F;
            this.dgvSr.HeaderText = "Sr#";
            this.dgvSr.MinimumWidth = 50;
            this.dgvSr.Name = "dgvSr";
            this.dgvSr.ReadOnly = true;
            this.dgvSr.Width = 50;
            // 
            // dgvid
            // 
            this.dgvid.HeaderText = "id";
            this.dgvid.Name = "dgvid";
            this.dgvid.ReadOnly = true;
            this.dgvid.Visible = false;
            // 
            // dgvedit
            // 
            this.dgvedit.HeaderText = "";
            this.dgvedit.Image = ((System.Drawing.Image)(resources.GetObject("dgvedit.Image")));
            this.dgvedit.ImageLayout = System.Windows.Forms.DataGridViewImageCellLayout.Zoom;
            this.dgvedit.Name = "dgvedit";
            this.dgvedit.ReadOnly = true;
            // 
            // dgvdel
            // 
            this.dgvdel.HeaderText = "";
            this.dgvdel.Image = ((System.Drawing.Image)(resources.GetObject("dgvdel.Image")));
            this.dgvdel.ImageLayout = System.Windows.Forms.DataGridViewImageCellLayout.Zoom;
            this.dgvdel.Name = "dgvdel";
            this.dgvdel.ReadOnly = true;
            // 
            // btnPrint
            // 
            this.btnPrint.Animated = true;
            this.btnPrint.AutoRoundedCorners = true;
            this.btnPrint.BorderRadius = 14;
            this.btnPrint.CheckedState.Parent = this.btnPrint;
            this.btnPrint.CustomImages.Parent = this.btnPrint;
            this.btnPrint.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.btnPrint.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnPrint.ForeColor = System.Drawing.Color.White;
            this.btnPrint.HoverState.Parent = this.btnPrint;
            this.btnPrint.Location = new System.Drawing.Point(719, 176);
            this.btnPrint.Name = "btnPrint";
            this.btnPrint.ShadowDecoration.Parent = this.btnPrint;
            this.btnPrint.Size = new System.Drawing.Size(135, 30);
            this.btnPrint.TabIndex = 6;
            this.btnPrint.Text = "Print";
            this.btnPrint.Click += new System.EventHandler(this.btnPrint_Click);
            // 
            // frmCustomerLedgerAdd
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1343, 788);
            this.Controls.Add(this.btnPrint);
            this.Controls.Add(this.grdLedger);
            this.Controls.Add(this.btnLoad);
            this.Controls.Add(this.guna2Panel2);
            this.Controls.Add(this.dtpTo);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.cmbCustomerPlot);
            this.Controls.Add(this.cmbSociety);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.dtpFrom);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.cmbCustomer);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.guna2Panel1);
            this.Name = "frmCustomerLedgerAdd";
            this.Text = "frmCustomerLedgerAdd";
            this.guna2Panel1.ResumeLayout(false);
            this.guna2Panel1.PerformLayout();
            this.guna2Panel2.ResumeLayout(false);
            this.guna2Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdLedger)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label5;
        private Guna.UI2.WinForms.Guna2Panel guna2Panel1;
        public Guna.UI2.WinForms.Guna2DateTimePicker dtpFrom;
        private System.Windows.Forms.Label label7;
        public Guna.UI2.WinForms.Guna2ComboBox cmbCustomer;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        public Guna.UI2.WinForms.Guna2ComboBox cmbCustomerPlot;
        public Guna.UI2.WinForms.Guna2ComboBox cmbSociety;
        private System.Windows.Forms.Label label6;
        public Guna.UI2.WinForms.Guna2DateTimePicker dtpTo;
        private System.Windows.Forms.Label label8;
        private Guna.UI2.WinForms.Guna2Panel guna2Panel2;
        private System.Windows.Forms.Label lblClosingBalance;
        private System.Windows.Forms.Label lblOpeningBalance;
        private Guna.UI2.WinForms.Guna2DataGridView grdLedger;
        private Guna.UI2.WinForms.Guna2Button btnLoad;
        private System.Windows.Forms.DataGridViewTextBoxColumn dgvSr;
        private System.Windows.Forms.DataGridViewTextBoxColumn dgvid;
        private System.Windows.Forms.DataGridViewImageColumn dgvedit;
        private System.Windows.Forms.DataGridViewImageColumn dgvdel;
        private System.Windows.Forms.Label lblBalance;
        private Guna.UI2.WinForms.Guna2Button btnPrint;
    }
}