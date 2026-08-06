namespace HubBuilders.Report_Form
{
    partial class frmVipBrokerDetails
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.panelTop = new System.Windows.Forms.Panel();
            this.guna2ControlBox2 = new Guna.UI2.WinForms.Guna2ControlBox();
            this.guna2ControlBox1 = new Guna.UI2.WinForms.Guna2ControlBox();
            this.labelTitle = new System.Windows.Forms.Label();
            this.panelLeft = new System.Windows.Forms.Panel();
            this.dgvList = new System.Windows.Forms.DataGridView();
            this.panelButtons = new System.Windows.Forms.Panel();
            this.btnClear = new Guna.UI2.WinForms.Guna2Button();
            this.btnPreview = new Guna.UI2.WinForms.Guna2Button();
            this.panelFieldsHost = new System.Windows.Forms.Panel();
            this.lblHint = new System.Windows.Forms.Label();
            this.cmbBroker = new System.Windows.Forms.ComboBox();
            this.lblBroker = new System.Windows.Forms.Label();
            this.dtpTo = new System.Windows.Forms.DateTimePicker();
            this.lblTo = new System.Windows.Forms.Label();
            this.dtpFrom = new System.Windows.Forms.DateTimePicker();
            this.lblFrom = new System.Windows.Forms.Label();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.lblSearch = new System.Windows.Forms.Label();
            this.cmbFilterSociety = new System.Windows.Forms.ComboBox();
            this.lblFilterSociety = new System.Windows.Forms.Label();
            this.cmbHeaderSociety = new System.Windows.Forms.ComboBox();
            this.lblHeaderSociety = new System.Windows.Forms.Label();
            this.reportViewer1 = new Microsoft.Reporting.WinForms.ReportViewer();
            this.panelTop.SuspendLayout();
            this.panelLeft.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvList)).BeginInit();
            this.panelButtons.SuspendLayout();
            this.panelFieldsHost.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelTop
            // 
            this.panelTop.Controls.Add(this.guna2ControlBox2);
            this.panelTop.Controls.Add(this.guna2ControlBox1);
            this.panelTop.Controls.Add(this.labelTitle);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(1280, 70);
            this.panelTop.TabIndex = 0;
            // 
            // guna2ControlBox2
            // 
            this.guna2ControlBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.guna2ControlBox2.ControlBoxType = Guna.UI2.WinForms.Enums.ControlBoxType.MaximizeBox;
            this.guna2ControlBox2.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(139)))), ((int)(((byte)(152)))), ((int)(((byte)(166)))));
            this.guna2ControlBox2.HoverState.Parent = this.guna2ControlBox2;
            this.guna2ControlBox2.IconColor = System.Drawing.Color.White;
            this.guna2ControlBox2.Location = new System.Drawing.Point(1150, 12);
            this.guna2ControlBox2.Name = "guna2ControlBox2";
            this.guna2ControlBox2.ShadowDecoration.Parent = this.guna2ControlBox2;
            this.guna2ControlBox2.Size = new System.Drawing.Size(56, 36);
            this.guna2ControlBox2.TabIndex = 2;
            // 
            // guna2ControlBox1
            // 
            this.guna2ControlBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.guna2ControlBox1.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(139)))), ((int)(((byte)(152)))), ((int)(((byte)(166)))));
            this.guna2ControlBox1.HoverState.Parent = this.guna2ControlBox1;
            this.guna2ControlBox1.IconColor = System.Drawing.Color.White;
            this.guna2ControlBox1.Location = new System.Drawing.Point(1212, 12);
            this.guna2ControlBox1.Name = "guna2ControlBox1";
            this.guna2ControlBox1.ShadowDecoration.Parent = this.guna2ControlBox1;
            this.guna2ControlBox1.Size = new System.Drawing.Size(56, 36);
            this.guna2ControlBox1.TabIndex = 1;
            // 
            // labelTitle
            // 
            this.labelTitle.AutoSize = true;
            this.labelTitle.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold);
            this.labelTitle.ForeColor = System.Drawing.Color.White;
            this.labelTitle.Location = new System.Drawing.Point(12, 16);
            this.labelTitle.Name = "labelTitle";
            this.labelTitle.Size = new System.Drawing.Size(420, 32);
            this.labelTitle.TabIndex = 0;
            this.labelTitle.Text = "Broker Complete Details (VIP)";
            // 
            // panelLeft
            // 
            this.panelLeft.Controls.Add(this.dgvList);
            this.panelLeft.Controls.Add(this.panelButtons);
            this.panelLeft.Controls.Add(this.panelFieldsHost);
            this.panelLeft.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelLeft.Location = new System.Drawing.Point(0, 70);
            this.panelLeft.Name = "panelLeft";
            this.panelLeft.Padding = new System.Windows.Forms.Padding(8);
            this.panelLeft.Size = new System.Drawing.Size(430, 650);
            this.panelLeft.TabIndex = 1;
            // 
            // panelFieldsHost
            // 
            this.panelFieldsHost.Controls.Add(this.lblHint);
            this.panelFieldsHost.Controls.Add(this.cmbBroker);
            this.panelFieldsHost.Controls.Add(this.lblBroker);
            this.panelFieldsHost.Controls.Add(this.dtpTo);
            this.panelFieldsHost.Controls.Add(this.lblTo);
            this.panelFieldsHost.Controls.Add(this.dtpFrom);
            this.panelFieldsHost.Controls.Add(this.lblFrom);
            this.panelFieldsHost.Controls.Add(this.txtSearch);
            this.panelFieldsHost.Controls.Add(this.lblSearch);
            this.panelFieldsHost.Controls.Add(this.cmbFilterSociety);
            this.panelFieldsHost.Controls.Add(this.lblFilterSociety);
            this.panelFieldsHost.Controls.Add(this.cmbHeaderSociety);
            this.panelFieldsHost.Controls.Add(this.lblHeaderSociety);
            this.panelFieldsHost.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelFieldsHost.Location = new System.Drawing.Point(8, 8);
            this.panelFieldsHost.Name = "panelFieldsHost";
            this.panelFieldsHost.Size = new System.Drawing.Size(414, 300);
            this.panelFieldsHost.TabIndex = 0;
            // 
            // lblHeaderSociety
            // 
            this.lblHeaderSociety.AutoSize = true;
            this.lblHeaderSociety.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.lblHeaderSociety.ForeColor = System.Drawing.Color.White;
            this.lblHeaderSociety.Location = new System.Drawing.Point(4, 6);
            this.lblHeaderSociety.Name = "lblHeaderSociety";
            this.lblHeaderSociety.Size = new System.Drawing.Size(180, 19);
            this.lblHeaderSociety.TabIndex = 0;
            this.lblHeaderSociety.Text = "Header Society (optional)";
            // 
            // cmbHeaderSociety
            // 
            this.cmbHeaderSociety.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbHeaderSociety.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cmbHeaderSociety.FormattingEnabled = true;
            this.cmbHeaderSociety.Location = new System.Drawing.Point(4, 28);
            this.cmbHeaderSociety.Name = "cmbHeaderSociety";
            this.cmbHeaderSociety.Size = new System.Drawing.Size(400, 25);
            this.cmbHeaderSociety.TabIndex = 1;
            // 
            // lblFilterSociety
            // 
            this.lblFilterSociety.AutoSize = true;
            this.lblFilterSociety.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.lblFilterSociety.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(200)))), ((int)(((byte)(120)))));
            this.lblFilterSociety.Location = new System.Drawing.Point(4, 60);
            this.lblFilterSociety.Name = "lblFilterSociety";
            this.lblFilterSociety.Size = new System.Drawing.Size(160, 19);
            this.lblFilterSociety.TabIndex = 2;
            this.lblFilterSociety.Text = "Society (important)";
            // 
            // cmbFilterSociety
            // 
            this.cmbFilterSociety.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbFilterSociety.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cmbFilterSociety.FormattingEnabled = true;
            this.cmbFilterSociety.Location = new System.Drawing.Point(4, 82);
            this.cmbFilterSociety.Name = "cmbFilterSociety";
            this.cmbFilterSociety.Size = new System.Drawing.Size(400, 25);
            this.cmbFilterSociety.TabIndex = 3;
            // 
            // lblFrom
            // 
            this.lblFrom.AutoSize = true;
            this.lblFrom.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.lblFrom.ForeColor = System.Drawing.Color.White;
            this.lblFrom.Location = new System.Drawing.Point(4, 116);
            this.lblFrom.Name = "lblFrom";
            this.lblFrom.Size = new System.Drawing.Size(80, 19);
            this.lblFrom.TabIndex = 4;
            this.lblFrom.Text = "From Date";
            // 
            // dtpFrom
            // 
            this.dtpFrom.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.dtpFrom.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpFrom.Location = new System.Drawing.Point(4, 138);
            this.dtpFrom.Name = "dtpFrom";
            this.dtpFrom.Size = new System.Drawing.Size(190, 25);
            this.dtpFrom.TabIndex = 5;
            // 
            // lblTo
            // 
            this.lblTo.AutoSize = true;
            this.lblTo.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.lblTo.ForeColor = System.Drawing.Color.White;
            this.lblTo.Location = new System.Drawing.Point(210, 116);
            this.lblTo.Name = "lblTo";
            this.lblTo.Size = new System.Drawing.Size(64, 19);
            this.lblTo.TabIndex = 6;
            this.lblTo.Text = "To Date";
            // 
            // dtpTo
            // 
            this.dtpTo.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.dtpTo.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpTo.Location = new System.Drawing.Point(210, 138);
            this.dtpTo.Name = "dtpTo";
            this.dtpTo.Size = new System.Drawing.Size(194, 25);
            this.dtpTo.TabIndex = 7;
            // 
            // lblBroker
            // 
            this.lblBroker.AutoSize = true;
            this.lblBroker.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.lblBroker.ForeColor = System.Drawing.Color.White;
            this.lblBroker.Location = new System.Drawing.Point(4, 172);
            this.lblBroker.Name = "lblBroker";
            this.lblBroker.Size = new System.Drawing.Size(120, 19);
            this.lblBroker.TabIndex = 8;
            this.lblBroker.Text = "Broker";
            // 
            // cmbBroker
            // 
            this.cmbBroker.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbBroker.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cmbBroker.FormattingEnabled = true;
            this.cmbBroker.Location = new System.Drawing.Point(4, 194);
            this.cmbBroker.Name = "cmbBroker";
            this.cmbBroker.Size = new System.Drawing.Size(400, 25);
            this.cmbBroker.TabIndex = 9;
            // 
            // lblSearch
            // 
            this.lblSearch.AutoSize = true;
            this.lblSearch.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.lblSearch.ForeColor = System.Drawing.Color.White;
            this.lblSearch.Location = new System.Drawing.Point(4, 228);
            this.lblSearch.Name = "lblSearch";
            this.lblSearch.Size = new System.Drawing.Size(140, 19);
            this.lblSearch.TabIndex = 10;
            this.lblSearch.Text = "Search (broker / customer / plot)";
            // 
            // txtSearch
            // 
            this.txtSearch.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtSearch.Location = new System.Drawing.Point(4, 250);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(400, 25);
            this.txtSearch.TabIndex = 11;
            // 
            // lblHint
            // 
            this.lblHint.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblHint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(190)))), ((int)(((byte)(210)))));
            this.lblHint.Location = new System.Drawing.Point(4, 278);
            this.lblHint.Name = "lblHint";
            this.lblHint.Size = new System.Drawing.Size(400, 18);
            this.lblHint.TabIndex = 12;
            this.lblHint.Text = "Broker → Customer → Plot → Commission | TOTAL row = sum";
            // 
            // panelButtons
            // 
            this.panelButtons.Controls.Add(this.btnClear);
            this.panelButtons.Controls.Add(this.btnPreview);
            this.panelButtons.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelButtons.Location = new System.Drawing.Point(8, 308);
            this.panelButtons.Name = "panelButtons";
            this.panelButtons.Size = new System.Drawing.Size(414, 56);
            this.panelButtons.TabIndex = 1;
            // 
            // btnPreview
            // 
            this.btnPreview.BorderRadius = 8;
            this.btnPreview.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(10)))), ((int)(((byte)(40)))), ((int)(((byte)(100)))));
            this.btnPreview.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Bold);
            this.btnPreview.ForeColor = System.Drawing.Color.White;
            this.btnPreview.Location = new System.Drawing.Point(4, 8);
            this.btnPreview.Name = "btnPreview";
            this.btnPreview.Size = new System.Drawing.Size(200, 40);
            this.btnPreview.TabIndex = 0;
            this.btnPreview.Text = "Print Preview";
            this.btnPreview.Click += new System.EventHandler(this.btnPreview_Click);
            // 
            // btnClear
            // 
            this.btnClear.BorderRadius = 8;
            this.btnClear.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(120)))));
            this.btnClear.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Bold);
            this.btnClear.ForeColor = System.Drawing.Color.White;
            this.btnClear.Location = new System.Drawing.Point(210, 8);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(196, 40);
            this.btnClear.TabIndex = 1;
            this.btnClear.Text = "Clear";
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // dgvList
            // 
            this.dgvList.AllowUserToAddRows = false;
            this.dgvList.AllowUserToDeleteRows = false;
            this.dgvList.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(66)))));
            this.dgvList.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(45)))), ((int)(((byte)(95)))));
            this.dgvList.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.White;
            this.dgvList.ColumnHeadersDefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(45)))), ((int)(((byte)(95)))));
            this.dgvList.ColumnHeadersDefaultCellStyle.SelectionForeColor = System.Drawing.Color.White;
            this.dgvList.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.dgvList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvList.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(66)))));
            this.dgvList.DefaultCellStyle.ForeColor = System.Drawing.Color.White;
            this.dgvList.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(52)))), ((int)(((byte)(90)))));
            this.dgvList.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.White;
            this.dgvList.DefaultCellStyle.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.dgvList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvList.EnableHeadersVisualStyles = false;
            this.dgvList.ForeColor = System.Drawing.Color.White;
            this.dgvList.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(56)))), ((int)(((byte)(76)))));
            this.dgvList.Location = new System.Drawing.Point(8, 364);
            this.dgvList.MultiSelect = false;
            this.dgvList.Name = "dgvList";
            this.dgvList.ReadOnly = true;
            this.dgvList.RowHeadersVisible = false;
            this.dgvList.RowsDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(66)))));
            this.dgvList.RowsDefaultCellStyle.ForeColor = System.Drawing.Color.White;
            this.dgvList.RowsDefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(52)))), ((int)(((byte)(90)))));
            this.dgvList.RowsDefaultCellStyle.SelectionForeColor = System.Drawing.Color.White;
            this.dgvList.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.dgvList.AlternatingRowsDefaultCellStyle.ForeColor = System.Drawing.Color.White;
            this.dgvList.AlternatingRowsDefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(52)))), ((int)(((byte)(90)))));
            this.dgvList.AlternatingRowsDefaultCellStyle.SelectionForeColor = System.Drawing.Color.White;
            this.dgvList.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvList.Size = new System.Drawing.Size(414, 278);
            this.dgvList.TabIndex = 2;
            // 
            // reportViewer1
            // 
            this.reportViewer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.reportViewer1.Location = new System.Drawing.Point(430, 70);
            this.reportViewer1.Name = "reportViewer1";
            this.reportViewer1.ServerReport.BearerToken = null;
            this.reportViewer1.Size = new System.Drawing.Size(850, 650);
            this.reportViewer1.TabIndex = 2;
            // 
            // frmVipBrokerDetails
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(61)))));
            this.ClientSize = new System.Drawing.Size(1280, 720);
            this.Controls.Add(this.reportViewer1);
            this.Controls.Add(this.panelLeft);
            this.Controls.Add(this.panelTop);
            this.Name = "frmVipBrokerDetails";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Broker Complete Details (VIP)";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.frmVipBrokerDetails_Load);
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            this.panelLeft.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvList)).EndInit();
            this.panelButtons.ResumeLayout(false);
            this.panelFieldsHost.ResumeLayout(false);
            this.panelFieldsHost.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel panelTop;
        private Guna.UI2.WinForms.Guna2ControlBox guna2ControlBox2;
        private Guna.UI2.WinForms.Guna2ControlBox guna2ControlBox1;
        private System.Windows.Forms.Label labelTitle;
        private System.Windows.Forms.Panel panelLeft;
        private System.Windows.Forms.Panel panelFieldsHost;
        private System.Windows.Forms.Panel panelButtons;
        private System.Windows.Forms.DataGridView dgvList;
        private Guna.UI2.WinForms.Guna2Button btnPreview;
        private Guna.UI2.WinForms.Guna2Button btnClear;
        private System.Windows.Forms.Label lblHeaderSociety;
        private System.Windows.Forms.ComboBox cmbHeaderSociety;
        private System.Windows.Forms.Label lblFilterSociety;
        private System.Windows.Forms.ComboBox cmbFilterSociety;
        private System.Windows.Forms.Label lblFrom;
        private System.Windows.Forms.DateTimePicker dtpFrom;
        private System.Windows.Forms.Label lblTo;
        private System.Windows.Forms.DateTimePicker dtpTo;
        private System.Windows.Forms.Label lblBroker;
        private System.Windows.Forms.ComboBox cmbBroker;
        private System.Windows.Forms.Label lblSearch;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Label lblHint;
        private Microsoft.Reporting.WinForms.ReportViewer reportViewer1;
    }
}
