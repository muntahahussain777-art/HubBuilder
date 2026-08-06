namespace HubBuilders.Model
{
    partial class frmAgingFormAdd
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmAgingFormAdd));
            this.guna2DataGridView1 = new Guna.UI2.WinForms.Guna2DataGridView();
            this.dgvSr = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgvid = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgvName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgvStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgvDailySalary = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgvAdvance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgvDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgvedit = new System.Windows.Forms.DataGridViewImageColumn();
            this.dgvdel = new System.Windows.Forms.DataGridViewImageColumn();
            this.btnClose = new Guna.UI2.WinForms.Guna2Button();
            this.btnSave = new Guna.UI2.WinForms.Guna2Button();
            this.dtpAsOf = new Guna.UI2.WinForms.Guna2DateTimePicker();
            this.label7 = new System.Windows.Forms.Label();
            this.cmbBlock = new Guna.UI2.WinForms.Guna2ComboBox();
            this.cmbSociety = new Guna.UI2.WinForms.Guna2ComboBox();
            this.super32423 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.guna2DataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // guna2DataGridView1
            // 
            this.guna2DataGridView1.AllowUserToAddRows = false;
            this.guna2DataGridView1.AllowUserToDeleteRows = false;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.guna2DataGridView1.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.guna2DataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.guna2DataGridView1.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.guna2DataGridView1.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(66)))));
            this.guna2DataGridView1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.guna2DataGridView1.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.guna2DataGridView1.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(88)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.guna2DataGridView1.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.guna2DataGridView1.ColumnHeadersHeight = 40;
            this.guna2DataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dgvSr,
            this.dgvid,
            this.dgvName,
            this.dgvStatus,
            this.dgvDailySalary,
            this.dgvAdvance,
            this.dgvDate,
            this.dgvedit,
            this.dgvdel});
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(66)))));
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(66)))));
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.guna2DataGridView1.DefaultCellStyle = dataGridViewCellStyle3;
            this.guna2DataGridView1.EnableHeadersVisualStyles = false;
            this.guna2DataGridView1.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(66)))));
            this.guna2DataGridView1.Location = new System.Drawing.Point(12, 171);
            this.guna2DataGridView1.MultiSelect = false;
            this.guna2DataGridView1.Name = "guna2DataGridView1";
            this.guna2DataGridView1.ReadOnly = true;
            this.guna2DataGridView1.RowHeadersVisible = false;
            this.guna2DataGridView1.RowTemplate.Height = 30;
            this.guna2DataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.guna2DataGridView1.Size = new System.Drawing.Size(785, 467);
            this.guna2DataGridView1.TabIndex = 169;
            this.guna2DataGridView1.Theme = Guna.UI2.WinForms.Enums.DataGridViewPresetThemes.Default;
            this.guna2DataGridView1.ThemeStyle.AlternatingRowsStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.guna2DataGridView1.ThemeStyle.AlternatingRowsStyle.Font = null;
            this.guna2DataGridView1.ThemeStyle.AlternatingRowsStyle.ForeColor = System.Drawing.Color.Empty;
            this.guna2DataGridView1.ThemeStyle.AlternatingRowsStyle.SelectionBackColor = System.Drawing.Color.Empty;
            this.guna2DataGridView1.ThemeStyle.AlternatingRowsStyle.SelectionForeColor = System.Drawing.Color.Empty;
            this.guna2DataGridView1.ThemeStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(66)))));
            this.guna2DataGridView1.ThemeStyle.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(66)))));
            this.guna2DataGridView1.ThemeStyle.HeaderStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(88)))), ((int)(((byte)(255)))));
            this.guna2DataGridView1.ThemeStyle.HeaderStyle.BorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this.guna2DataGridView1.ThemeStyle.HeaderStyle.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.guna2DataGridView1.ThemeStyle.HeaderStyle.ForeColor = System.Drawing.Color.White;
            this.guna2DataGridView1.ThemeStyle.HeaderStyle.HeaightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            this.guna2DataGridView1.ThemeStyle.HeaderStyle.Height = 40;
            this.guna2DataGridView1.ThemeStyle.ReadOnly = true;
            this.guna2DataGridView1.ThemeStyle.RowsStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(66)))));
            this.guna2DataGridView1.ThemeStyle.RowsStyle.BorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.guna2DataGridView1.ThemeStyle.RowsStyle.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.guna2DataGridView1.ThemeStyle.RowsStyle.ForeColor = System.Drawing.Color.White;
            this.guna2DataGridView1.ThemeStyle.RowsStyle.Height = 30;
            this.guna2DataGridView1.ThemeStyle.RowsStyle.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(66)))));
            this.guna2DataGridView1.ThemeStyle.RowsStyle.SelectionForeColor = System.Drawing.Color.White;
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
            // dgvName
            // 
            this.dgvName.HeaderText = "Name";
            this.dgvName.Name = "dgvName";
            this.dgvName.ReadOnly = true;
            // 
            // dgvStatus
            // 
            this.dgvStatus.HeaderText = "Status";
            this.dgvStatus.Name = "dgvStatus";
            this.dgvStatus.ReadOnly = true;
            // 
            // dgvDailySalary
            // 
            this.dgvDailySalary.HeaderText = "PerDaySalary";
            this.dgvDailySalary.Name = "dgvDailySalary";
            this.dgvDailySalary.ReadOnly = true;
            // 
            // dgvAdvance
            // 
            this.dgvAdvance.HeaderText = "Advance";
            this.dgvAdvance.Name = "dgvAdvance";
            this.dgvAdvance.ReadOnly = true;
            // 
            // dgvDate
            // 
            this.dgvDate.HeaderText = "Date";
            this.dgvDate.Name = "dgvDate";
            this.dgvDate.ReadOnly = true;
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
            // btnClose
            // 
            this.btnClose.Animated = true;
            this.btnClose.AutoRoundedCorners = true;
            this.btnClose.BorderRadius = 14;
            this.btnClose.CheckedState.Parent = this.btnClose;
            this.btnClose.CustomImages.Parent = this.btnClose;
            this.btnClose.FillColor = System.Drawing.Color.Crimson;
            this.btnClose.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnClose.ForeColor = System.Drawing.Color.White;
            this.btnClose.HoverState.Parent = this.btnClose;
            this.btnClose.Location = new System.Drawing.Point(420, 135);
            this.btnClose.Name = "btnClose";
            this.btnClose.ShadowDecoration.Parent = this.btnClose;
            this.btnClose.Size = new System.Drawing.Size(135, 30);
            this.btnClose.TabIndex = 4;
            this.btnClose.Text = "Close";
            // 
            // btnSave
            // 
            this.btnSave.Animated = true;
            this.btnSave.AutoRoundedCorners = true;
            this.btnSave.BorderRadius = 14;
            this.btnSave.CheckedState.Parent = this.btnSave;
            this.btnSave.CustomImages.Parent = this.btnSave;
            this.btnSave.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.btnSave.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.HoverState.Parent = this.btnSave;
            this.btnSave.Location = new System.Drawing.Point(279, 135);
            this.btnSave.Name = "btnSave";
            this.btnSave.ShadowDecoration.Parent = this.btnSave;
            this.btnSave.Size = new System.Drawing.Size(135, 30);
            this.btnSave.TabIndex = 3;
            this.btnSave.Text = "Save";
            // 
            // dtpAsOf
            // 
            this.dtpAsOf.AutoRoundedCorners = true;
            this.dtpAsOf.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.dtpAsOf.BorderRadius = 19;
            this.dtpAsOf.BorderThickness = 1;
            this.dtpAsOf.CheckedState.Parent = this.dtpAsOf;
            this.dtpAsOf.CustomFormat = "yyyy-MM-dd";
            this.dtpAsOf.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.dtpAsOf.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dtpAsOf.ForeColor = System.Drawing.Color.White;
            this.dtpAsOf.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpAsOf.HoverState.Parent = this.dtpAsOf;
            this.dtpAsOf.Location = new System.Drawing.Point(522, 92);
            this.dtpAsOf.MaxDate = new System.DateTime(9998, 12, 31, 0, 0, 0, 0);
            this.dtpAsOf.MinDate = new System.DateTime(1753, 1, 1, 0, 0, 0, 0);
            this.dtpAsOf.Name = "dtpAsOf";
            this.dtpAsOf.ShadowDecoration.Parent = this.dtpAsOf;
            this.dtpAsOf.Size = new System.Drawing.Size(267, 41);
            this.dtpAsOf.TabIndex = 2;
            this.dtpAsOf.Value = new System.DateTime(2024, 6, 21, 19, 54, 39, 787);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.ForeColor = System.Drawing.Color.White;
            this.label7.Location = new System.Drawing.Point(535, 66);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(88, 21);
            this.label7.TabIndex = 182;
            this.label7.Text = "As Of Date";
            // 
            // cmbBlock
            // 
            this.cmbBlock.AutoRoundedCorners = true;
            this.cmbBlock.BackColor = System.Drawing.Color.Transparent;
            this.cmbBlock.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.cmbBlock.BorderRadius = 17;
            this.cmbBlock.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cmbBlock.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbBlock.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.cmbBlock.FocusedColor = System.Drawing.Color.Empty;
            this.cmbBlock.FocusedState.Parent = this.cmbBlock;
            this.cmbBlock.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbBlock.ForeColor = System.Drawing.Color.White;
            this.cmbBlock.FormattingEnabled = true;
            this.cmbBlock.HoverState.Parent = this.cmbBlock;
            this.cmbBlock.ItemHeight = 30;
            this.cmbBlock.ItemsAppearance.Parent = this.cmbBlock;
            this.cmbBlock.Location = new System.Drawing.Point(269, 92);
            this.cmbBlock.Name = "cmbBlock";
            this.cmbBlock.ShadowDecoration.Parent = this.cmbBlock;
            this.cmbBlock.Size = new System.Drawing.Size(245, 36);
            this.cmbBlock.TabIndex = 1;
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
            this.cmbSociety.Location = new System.Drawing.Point(13, 92);
            this.cmbSociety.Name = "cmbSociety";
            this.cmbSociety.ShadowDecoration.Parent = this.cmbSociety;
            this.cmbSociety.Size = new System.Drawing.Size(245, 36);
            this.cmbSociety.TabIndex = 0;
            // 
            // super32423
            // 
            this.super32423.AutoSize = true;
            this.super32423.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.super32423.ForeColor = System.Drawing.Color.White;
            this.super32423.Location = new System.Drawing.Point(285, 68);
            this.super32423.Name = "super32423";
            this.super32423.Size = new System.Drawing.Size(50, 21);
            this.super32423.TabIndex = 178;
            this.super32423.Text = "Block";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.White;
            this.label4.Location = new System.Drawing.Point(31, 68);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(64, 21);
            this.label4.TabIndex = 177;
            this.label4.Text = "Society";
            // 
            // frmAgingFormAdd
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(809, 653);
            this.Controls.Add(this.dtpAsOf);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.cmbBlock);
            this.Controls.Add(this.cmbSociety);
            this.Controls.Add(this.super32423);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.guna2DataGridView1);
            this.Name = "frmAgingFormAdd";
            this.Text = "frmAgingFormAdd";
            ((System.ComponentModel.ISupportInitialize)(this.guna2DataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private Guna.UI2.WinForms.Guna2DataGridView guna2DataGridView1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dgvSr;
        private System.Windows.Forms.DataGridViewTextBoxColumn dgvid;
        private System.Windows.Forms.DataGridViewTextBoxColumn dgvName;
        private System.Windows.Forms.DataGridViewTextBoxColumn dgvStatus;
        private System.Windows.Forms.DataGridViewTextBoxColumn dgvDailySalary;
        private System.Windows.Forms.DataGridViewTextBoxColumn dgvAdvance;
        private System.Windows.Forms.DataGridViewTextBoxColumn dgvDate;
        private System.Windows.Forms.DataGridViewImageColumn dgvedit;
        private System.Windows.Forms.DataGridViewImageColumn dgvdel;
        private Guna.UI2.WinForms.Guna2Button btnClose;
        private Guna.UI2.WinForms.Guna2Button btnSave;
        public Guna.UI2.WinForms.Guna2DateTimePicker dtpAsOf;
        private System.Windows.Forms.Label label7;
        public Guna.UI2.WinForms.Guna2ComboBox cmbBlock;
        public Guna.UI2.WinForms.Guna2ComboBox cmbSociety;
        private System.Windows.Forms.Label super32423;
        private System.Windows.Forms.Label label4;
    }
}