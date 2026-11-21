namespace HubBuilders.View
{
    partial class frmReceiptFormView
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
            this.guna2DataGridView1 = new Guna.UI2.WinForms.Guna2DataGridView();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgvRowType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgvReceiptId = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgvDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgvDescription = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgvDebit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgvCredit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblFromDate = new System.Windows.Forms.Label();
            this.fromdate = new Guna.UI2.WinForms.Guna2DateTimePicker();
            this.lblToDate = new System.Windows.Forms.Label();
            this.todate = new Guna.UI2.WinForms.Guna2DateTimePicker();
            this.btnLoadDates = new Guna.UI2.WinForms.Guna2Button();
            this.lblTotalReceiptAmount = new System.Windows.Forms.Label();
            this.lblTotalAdvance = new System.Windows.Forms.Label();
            this.lblReceiptCount = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.guna2DataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.lblReceiptCount);
            this.panel1.Controls.Add(this.lblTotalAdvance);
            this.panel1.Controls.Add(this.lblTotalReceiptAmount);
            this.panel1.Controls.Add(this.btnLoadDates);
            this.panel1.Controls.Add(this.todate);
            this.panel1.Controls.Add(this.lblToDate);
            this.panel1.Controls.Add(this.fromdate);
            this.panel1.Controls.Add(this.lblFromDate);
            this.panel1.Size = new System.Drawing.Size(982, 118);
            this.panel1.Controls.SetChildIndex(this.label1, 0);
            this.panel1.Controls.SetChildIndex(this.txtSearch, 0);
            this.panel1.Controls.SetChildIndex(this.btnAdd, 0);
            this.panel1.Controls.SetChildIndex(this.lblFromDate, 0);
            this.panel1.Controls.SetChildIndex(this.fromdate, 0);
            this.panel1.Controls.SetChildIndex(this.lblToDate, 0);
            this.panel1.Controls.SetChildIndex(this.todate, 0);
            this.panel1.Controls.SetChildIndex(this.btnLoadDates, 0);
            this.panel1.Controls.SetChildIndex(this.lblTotalReceiptAmount, 0);
            this.panel1.Controls.SetChildIndex(this.lblTotalAdvance, 0);
            this.panel1.Controls.SetChildIndex(this.lblReceiptCount, 0);
            // 
            // btnAdd
            // 
            this.btnAdd.ButtonMode = Guna.UI2.WinForms.Enums.ButtonMode.DefaultButton;
            this.btnAdd.CheckedState.Parent = this.btnAdd;
            this.btnAdd.CustomImages.Parent = this.btnAdd;
            this.btnAdd.DialogResult = System.Windows.Forms.DialogResult.None;
            this.btnAdd.HoverState.Parent = this.btnAdd;
            this.btnAdd.ImageAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.btnAdd.Location = new System.Drawing.Point(875, 37);
            this.btnAdd.ShadowDecoration.Parent = this.btnAdd;
            this.btnAdd.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.btnAdd.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            this.btnAdd.TextTransform = Guna.UI2.WinForms.Enums.TextTransform.None;
            // 
            // txtSearch
            // 
            this.txtSearch.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.txtSearch.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.txtSearch.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtSearch.DisabledState.Parent = this.txtSearch;
            this.txtSearch.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtSearch.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtSearch.FocusedState.Parent = this.txtSearch;
            this.txtSearch.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtSearch.HoverState.Parent = this.txtSearch;
            this.txtSearch.ShadowDecoration.Parent = this.txtSearch;
            this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
            // 
            // lblFromDate
            // 
            this.lblFromDate.AutoSize = true;
            this.lblFromDate.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblFromDate.ForeColor = System.Drawing.Color.White;
            this.lblFromDate.Location = new System.Drawing.Point(250, 14);
            this.lblFromDate.Name = "lblFromDate";
            this.lblFromDate.Size = new System.Drawing.Size(40, 17);
            this.lblFromDate.TabIndex = 20;
            this.lblFromDate.Text = "From";
            // 
            // fromdate
            // 
            this.fromdate.AutoRoundedCorners = true;
            this.fromdate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.fromdate.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.fromdate.BorderRadius = 14;
            this.fromdate.BorderThickness = 1;
            this.fromdate.CheckedState.Parent = this.fromdate;
            this.fromdate.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.fromdate.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.fromdate.ForeColor = System.Drawing.Color.White;
            this.fromdate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.fromdate.HoverState.Parent = this.fromdate;
            this.fromdate.Location = new System.Drawing.Point(294, 10);
            this.fromdate.MaxDate = new System.DateTime(9998, 12, 31, 0, 0, 0, 0);
            this.fromdate.MinDate = new System.DateTime(1753, 1, 1, 0, 0, 0, 0);
            this.fromdate.Name = "fromdate";
            this.fromdate.ShadowDecoration.Parent = this.fromdate;
            this.fromdate.Size = new System.Drawing.Size(130, 28);
            this.fromdate.TabIndex = 21;
            this.fromdate.Value = new System.DateTime(2026, 1, 1, 0, 0, 0, 0);
            this.fromdate.ValueChanged += new System.EventHandler(this.fromdate_ValueChanged);
            // 
            // lblToDate
            // 
            this.lblToDate.AutoSize = true;
            this.lblToDate.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblToDate.ForeColor = System.Drawing.Color.White;
            this.lblToDate.Location = new System.Drawing.Point(432, 14);
            this.lblToDate.Name = "lblToDate";
            this.lblToDate.Size = new System.Drawing.Size(23, 17);
            this.lblToDate.TabIndex = 22;
            this.lblToDate.Text = "To";
            // 
            // todate
            // 
            this.todate.AutoRoundedCorners = true;
            this.todate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.todate.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.todate.BorderRadius = 14;
            this.todate.BorderThickness = 1;
            this.todate.CheckedState.Parent = this.todate;
            this.todate.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.todate.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.todate.ForeColor = System.Drawing.Color.White;
            this.todate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.todate.HoverState.Parent = this.todate;
            this.todate.Location = new System.Drawing.Point(460, 10);
            this.todate.MaxDate = new System.DateTime(9998, 12, 31, 0, 0, 0, 0);
            this.todate.MinDate = new System.DateTime(1753, 1, 1, 0, 0, 0, 0);
            this.todate.Name = "todate";
            this.todate.ShadowDecoration.Parent = this.todate;
            this.todate.Size = new System.Drawing.Size(130, 28);
            this.todate.TabIndex = 23;
            this.todate.Value = new System.DateTime(2026, 1, 1, 0, 0, 0, 0);
            this.todate.ValueChanged += new System.EventHandler(this.todate_ValueChanged);
            // 
            // btnLoadDates
            // 
            this.btnLoadDates.Animated = true;
            this.btnLoadDates.AutoRoundedCorners = true;
            this.btnLoadDates.BorderRadius = 15;
            this.btnLoadDates.CheckedState.Parent = this.btnLoadDates;
            this.btnLoadDates.CustomImages.Parent = this.btnLoadDates;
            this.btnLoadDates.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.btnLoadDates.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Bold);
            this.btnLoadDates.ForeColor = System.Drawing.Color.White;
            this.btnLoadDates.HoverState.Parent = this.btnLoadDates;
            this.btnLoadDates.Location = new System.Drawing.Point(600, 10);
            this.btnLoadDates.Name = "btnLoadDates";
            this.btnLoadDates.ShadowDecoration.Parent = this.btnLoadDates;
            this.btnLoadDates.Size = new System.Drawing.Size(90, 28);
            this.btnLoadDates.TabIndex = 24;
            this.btnLoadDates.Text = "Load";
            this.btnLoadDates.Click += new System.EventHandler(this.btnLoadDates_Click);
            // 
            // lblTotalReceiptAmount
            // 
            this.lblTotalReceiptAmount.AutoSize = true;
            this.lblTotalReceiptAmount.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.lblTotalReceiptAmount.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(220)))), ((int)(((byte)(180)))));
            this.lblTotalReceiptAmount.Location = new System.Drawing.Point(250, 48);
            this.lblTotalReceiptAmount.Name = "lblTotalReceiptAmount";
            this.lblTotalReceiptAmount.Size = new System.Drawing.Size(180, 19);
            this.lblTotalReceiptAmount.TabIndex = 25;
            this.lblTotalReceiptAmount.Text = "Total Amount: 0";
            // 
            // lblTotalAdvance
            // 
            this.lblTotalAdvance.AutoSize = true;
            this.lblTotalAdvance.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.lblTotalAdvance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(220)))), ((int)(((byte)(180)))));
            this.lblTotalAdvance.Location = new System.Drawing.Point(460, 48);
            this.lblTotalAdvance.Name = "lblTotalAdvance";
            this.lblTotalAdvance.Size = new System.Drawing.Size(130, 19);
            this.lblTotalAdvance.TabIndex = 26;
            this.lblTotalAdvance.Text = "Total Receipt: 0";
            // 
            // lblReceiptCount
            // 
            this.lblReceiptCount.AutoSize = true;
            this.lblReceiptCount.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.lblReceiptCount.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(210)))), ((int)(((byte)(120)))));
            this.lblReceiptCount.Location = new System.Drawing.Point(640, 48);
            this.lblReceiptCount.Name = "lblReceiptCount";
            this.lblReceiptCount.Size = new System.Drawing.Size(100, 19);
            this.lblReceiptCount.TabIndex = 27;
            this.lblReceiptCount.Text = "Total Advance: 0";
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
            this.dataGridViewTextBoxColumn3,
            this.dataGridViewTextBoxColumn4,
            this.dgvRowType,
            this.dgvReceiptId,
            this.dgvDate,
            this.dgvDescription,
            this.dgvDebit,
            this.dgvCredit});
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
            this.guna2DataGridView1.Location = new System.Drawing.Point(12, 145);
            this.guna2DataGridView1.MultiSelect = false;
            this.guna2DataGridView1.Name = "guna2DataGridView1";
            this.guna2DataGridView1.ReadOnly = true;
            this.guna2DataGridView1.RowHeadersVisible = false;
            this.guna2DataGridView1.RowTemplate.Height = 30;
            this.guna2DataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.guna2DataGridView1.Size = new System.Drawing.Size(958, 326);
            this.guna2DataGridView1.TabIndex = 179;
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
            this.guna2DataGridView1.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.Grid_CellHandler);
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
            // dgvRowType
            // 
            this.dgvRowType.HeaderText = "RowType";
            this.dgvRowType.Name = "dgvRowType";
            this.dgvRowType.ReadOnly = true;
            // 
            // dgvReceiptId
            // 
            this.dgvReceiptId.HeaderText = "ReceiptId";
            this.dgvReceiptId.Name = "dgvReceiptId";
            this.dgvReceiptId.ReadOnly = true;
            // 
            // dgvDate
            // 
            this.dgvDate.HeaderText = "Date";
            this.dgvDate.Name = "dgvDate";
            this.dgvDate.ReadOnly = true;
            // 
            // dgvDescription
            // 
            this.dgvDescription.HeaderText = "Description";
            this.dgvDescription.Name = "dgvDescription";
            this.dgvDescription.ReadOnly = true;
            // 
            // dgvDebit
            // 
            this.dgvDebit.HeaderText = "Debit";
            this.dgvDebit.Name = "dgvDebit";
            this.dgvDebit.ReadOnly = true;
            // 
            // dgvCredit
            // 
            this.dgvCredit.HeaderText = "Credit";
            this.dgvCredit.Name = "dgvCredit";
            this.dgvCredit.ReadOnly = true;
            // 
            // frmReceiptFormView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(982, 483);
            this.Controls.Add(this.guna2DataGridView1);
            this.Name = "frmReceiptFormView";
            this.Text = "frmReceiptFormView";
            this.Load += new System.EventHandler(this.frmReceiptFormView_Load);
            this.Controls.SetChildIndex(this.panel1, 0);
            this.Controls.SetChildIndex(this.guna2DataGridView1, 0);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.guna2DataGridView1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Guna.UI2.WinForms.Guna2DataGridView guna2DataGridView1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private System.Windows.Forms.DataGridViewTextBoxColumn dgvRowType;
        private System.Windows.Forms.DataGridViewTextBoxColumn dgvReceiptId;
        private System.Windows.Forms.DataGridViewTextBoxColumn dgvDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn dgvDescription;
        private System.Windows.Forms.DataGridViewTextBoxColumn dgvDebit;
        private System.Windows.Forms.DataGridViewTextBoxColumn dgvCredit;
        private System.Windows.Forms.Label lblFromDate;
        private Guna.UI2.WinForms.Guna2DateTimePicker fromdate;
        private System.Windows.Forms.Label lblToDate;
        private Guna.UI2.WinForms.Guna2DateTimePicker todate;
        private Guna.UI2.WinForms.Guna2Button btnLoadDates;
        private System.Windows.Forms.Label lblTotalReceiptAmount;
        private System.Windows.Forms.Label lblTotalAdvance;
        private System.Windows.Forms.Label lblReceiptCount;
    }
}
