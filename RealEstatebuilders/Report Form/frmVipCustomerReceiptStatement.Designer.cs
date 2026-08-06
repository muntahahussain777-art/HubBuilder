namespace HubBuilders.Report_Form
{
    partial class frmVipCustomerReceiptStatement
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
            this.panelButtons = new System.Windows.Forms.Panel();
            this.btnClear = new Guna.UI2.WinForms.Guna2Button();
            this.btnPreview = new Guna.UI2.WinForms.Guna2Button();
            this.panelFieldsHost = new System.Windows.Forms.Panel();
            this.lblHint = new System.Windows.Forms.Label();
            this.btnFilter = new Guna.UI2.WinForms.Guna2Button();
            this.dtpTo = new System.Windows.Forms.DateTimePicker();
            this.dtpFrom = new System.Windows.Forms.DateTimePicker();
            this.lblTo = new System.Windows.Forms.Label();
            this.lblFrom = new System.Windows.Forms.Label();
            this.chkDateFilter = new System.Windows.Forms.CheckBox();
            this.txtNameSearch = new System.Windows.Forms.TextBox();
            this.lblNameSearch = new System.Windows.Forms.Label();
            this.cmbCustomerPlot = new System.Windows.Forms.ComboBox();
            this.lblAlloc = new System.Windows.Forms.Label();
            this.cmbHeaderSociety = new System.Windows.Forms.ComboBox();
            this.lblHeaderSociety = new System.Windows.Forms.Label();
            this.reportViewer1 = new Microsoft.Reporting.WinForms.ReportViewer();
            this.panelTop.SuspendLayout();
            this.panelLeft.SuspendLayout();
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
            this.labelTitle.Size = new System.Drawing.Size(520, 32);
            this.labelTitle.TabIndex = 0;
            this.labelTitle.Text = "Customer Receipt Statement";
            // 
            // panelLeft
            // 
            this.panelLeft.Controls.Add(this.panelFieldsHost);
            this.panelLeft.Controls.Add(this.panelButtons);
            this.panelLeft.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelLeft.Location = new System.Drawing.Point(0, 70);
            this.panelLeft.Name = "panelLeft";
            this.panelLeft.Padding = new System.Windows.Forms.Padding(8);
            this.panelLeft.Size = new System.Drawing.Size(420, 650);
            this.panelLeft.TabIndex = 1;
            // 
            // panelButtons
            // 
            this.panelButtons.Controls.Add(this.btnClear);
            this.panelButtons.Controls.Add(this.btnPreview);
            this.panelButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelButtons.Location = new System.Drawing.Point(8, 582);
            this.panelButtons.Name = "panelButtons";
            this.panelButtons.Size = new System.Drawing.Size(404, 60);
            this.panelButtons.TabIndex = 1;
            // 
            // btnClear
            // 
            this.btnClear.BorderRadius = 8;
            this.btnClear.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(120)))));
            this.btnClear.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Bold);
            this.btnClear.ForeColor = System.Drawing.Color.White;
            this.btnClear.Location = new System.Drawing.Point(210, 8);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(180, 42);
            this.btnClear.TabIndex = 1;
            this.btnClear.Text = "Clear";
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // btnPreview
            // 
            this.btnPreview.BorderRadius = 8;
            this.btnPreview.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(10)))), ((int)(((byte)(40)))), ((int)(((byte)(100)))));
            this.btnPreview.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Bold);
            this.btnPreview.ForeColor = System.Drawing.Color.White;
            this.btnPreview.Location = new System.Drawing.Point(10, 8);
            this.btnPreview.Name = "btnPreview";
            this.btnPreview.Size = new System.Drawing.Size(180, 42);
            this.btnPreview.TabIndex = 0;
            this.btnPreview.Text = "Print Preview";
            this.btnPreview.Click += new System.EventHandler(this.btnPreview_Click);
            // 
            // panelFieldsHost
            // 
            this.panelFieldsHost.Controls.Add(this.lblHint);
            this.panelFieldsHost.Controls.Add(this.btnFilter);
            this.panelFieldsHost.Controls.Add(this.dtpTo);
            this.panelFieldsHost.Controls.Add(this.dtpFrom);
            this.panelFieldsHost.Controls.Add(this.lblTo);
            this.panelFieldsHost.Controls.Add(this.lblFrom);
            this.panelFieldsHost.Controls.Add(this.chkDateFilter);
            this.panelFieldsHost.Controls.Add(this.txtNameSearch);
            this.panelFieldsHost.Controls.Add(this.lblNameSearch);
            this.panelFieldsHost.Controls.Add(this.cmbCustomerPlot);
            this.panelFieldsHost.Controls.Add(this.lblAlloc);
            this.panelFieldsHost.Controls.Add(this.cmbHeaderSociety);
            this.panelFieldsHost.Controls.Add(this.lblHeaderSociety);
            this.panelFieldsHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelFieldsHost.Location = new System.Drawing.Point(8, 8);
            this.panelFieldsHost.Name = "panelFieldsHost";
            this.panelFieldsHost.Size = new System.Drawing.Size(404, 574);
            this.panelFieldsHost.TabIndex = 0;
            // 
            // lblHeaderSociety
            // 
            this.lblHeaderSociety.AutoSize = true;
            this.lblHeaderSociety.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.lblHeaderSociety.ForeColor = System.Drawing.Color.White;
            this.lblHeaderSociety.Location = new System.Drawing.Point(4, 12);
            this.lblHeaderSociety.Name = "lblHeaderSociety";
            this.lblHeaderSociety.Size = new System.Drawing.Size(200, 19);
            this.lblHeaderSociety.TabIndex = 11;
            this.lblHeaderSociety.Text = "Header Society (optional)";
            // 
            // cmbHeaderSociety
            // 
            this.cmbHeaderSociety.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbHeaderSociety.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cmbHeaderSociety.FormattingEnabled = true;
            this.cmbHeaderSociety.Location = new System.Drawing.Point(4, 36);
            this.cmbHeaderSociety.Name = "cmbHeaderSociety";
            this.cmbHeaderSociety.Size = new System.Drawing.Size(390, 25);
            this.cmbHeaderSociety.TabIndex = 12;
            // 
            // lblNameSearch
            // 
            this.lblNameSearch.AutoSize = true;
            this.lblNameSearch.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.lblNameSearch.ForeColor = System.Drawing.Color.White;
            this.lblNameSearch.Location = new System.Drawing.Point(4, 138);
            this.lblNameSearch.Name = "lblNameSearch";
            this.lblNameSearch.Size = new System.Drawing.Size(100, 19);
            this.lblNameSearch.TabIndex = 3;
            this.lblNameSearch.Text = "Name Search";
            // 
            // txtNameSearch
            // 
            this.txtNameSearch.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtNameSearch.Location = new System.Drawing.Point(4, 160);
            this.txtNameSearch.Name = "txtNameSearch";
            this.txtNameSearch.Size = new System.Drawing.Size(280, 25);
            this.txtNameSearch.TabIndex = 4;
            // 
            // btnFilter
            // 
            this.btnFilter.BorderRadius = 6;
            this.btnFilter.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(10)))), ((int)(((byte)(40)))), ((int)(((byte)(100)))));
            this.btnFilter.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.btnFilter.ForeColor = System.Drawing.Color.White;
            this.btnFilter.Location = new System.Drawing.Point(290, 158);
            this.btnFilter.Name = "btnFilter";
            this.btnFilter.Size = new System.Drawing.Size(104, 28);
            this.btnFilter.TabIndex = 5;
            this.btnFilter.Text = "Filter";
            this.btnFilter.Click += new System.EventHandler(this.btnFilter_Click);
            // 
            // chkDateFilter
            // 
            this.chkDateFilter.AutoSize = true;
            this.chkDateFilter.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.chkDateFilter.ForeColor = System.Drawing.Color.White;
            this.chkDateFilter.Location = new System.Drawing.Point(4, 200);
            this.chkDateFilter.Name = "chkDateFilter";
            this.chkDateFilter.Size = new System.Drawing.Size(160, 23);
            this.chkDateFilter.TabIndex = 6;
            this.chkDateFilter.Text = "Use Date Filter";
            this.chkDateFilter.UseVisualStyleBackColor = true;
            // 
            // lblFrom
            // 
            this.lblFrom.AutoSize = true;
            this.lblFrom.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblFrom.ForeColor = System.Drawing.Color.White;
            this.lblFrom.Location = new System.Drawing.Point(4, 232);
            this.lblFrom.Name = "lblFrom";
            this.lblFrom.Size = new System.Drawing.Size(40, 17);
            this.lblFrom.TabIndex = 7;
            this.lblFrom.Text = "From";
            // 
            // dtpFrom
            // 
            this.dtpFrom.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.dtpFrom.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpFrom.Location = new System.Drawing.Point(60, 228);
            this.dtpFrom.Name = "dtpFrom";
            this.dtpFrom.Size = new System.Drawing.Size(130, 24);
            this.dtpFrom.TabIndex = 8;
            // 
            // lblTo
            // 
            this.lblTo.AutoSize = true;
            this.lblTo.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblTo.ForeColor = System.Drawing.Color.White;
            this.lblTo.Location = new System.Drawing.Point(200, 232);
            this.lblTo.Name = "lblTo";
            this.lblTo.Size = new System.Drawing.Size(22, 17);
            this.lblTo.TabIndex = 9;
            this.lblTo.Text = "To";
            // 
            // dtpTo
            // 
            this.dtpTo.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.dtpTo.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpTo.Location = new System.Drawing.Point(230, 228);
            this.dtpTo.Name = "dtpTo";
            this.dtpTo.Size = new System.Drawing.Size(130, 24);
            this.dtpTo.TabIndex = 10;
            // 
            // lblHint
            // 
            this.lblHint.ForeColor = System.Drawing.Color.Gainsboro;
            this.lblHint.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblHint.Location = new System.Drawing.Point(4, 270);
            this.lblHint.Name = "lblHint";
            this.lblHint.Size = new System.Drawing.Size(390, 90);
            this.lblHint.TabIndex = 2;
            this.lblHint.Text = "Header Society empty = Hub Builders.\r\nSelect society = that name in header.\r\nName/Date filters optional.";
            // 
            // cmbCustomerPlot
            // 
            this.cmbCustomerPlot.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCustomerPlot.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cmbCustomerPlot.FormattingEnabled = true;
            this.cmbCustomerPlot.Location = new System.Drawing.Point(4, 100);
            this.cmbCustomerPlot.Name = "cmbCustomerPlot";
            this.cmbCustomerPlot.Size = new System.Drawing.Size(390, 25);
            this.cmbCustomerPlot.TabIndex = 1;
            // 
            // lblAlloc
            // 
            this.lblAlloc.AutoSize = true;
            this.lblAlloc.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.lblAlloc.ForeColor = System.Drawing.Color.White;
            this.lblAlloc.Location = new System.Drawing.Point(4, 72);
            this.lblAlloc.Name = "lblAlloc";
            this.lblAlloc.Size = new System.Drawing.Size(220, 19);
            this.lblAlloc.TabIndex = 0;
            this.lblAlloc.Text = "Allocation Customer / Plot";
            // 
            // reportViewer1
            // 
            this.reportViewer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.reportViewer1.Location = new System.Drawing.Point(420, 70);
            this.reportViewer1.Name = "reportViewer1";
            this.reportViewer1.ServerReport.BearerToken = null;
            this.reportViewer1.Size = new System.Drawing.Size(860, 650);
            this.reportViewer1.TabIndex = 2;
            this.reportViewer1.ZoomMode = Microsoft.Reporting.WinForms.ZoomMode.PageWidth;
            // 
            // frmVipCustomerReceiptStatement
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1280, 720);
            this.Controls.Add(this.reportViewer1);
            this.Controls.Add(this.panelLeft);
            this.Controls.Add(this.panelTop);
            this.Name = "frmVipCustomerReceiptStatement";
            this.Text = "Customer Receipt Statement";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.frmVipCustomerReceiptStatement_Load);
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            this.panelLeft.ResumeLayout(false);
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
        private System.Windows.Forms.Panel panelButtons;
        private Guna.UI2.WinForms.Guna2Button btnClear;
        private Guna.UI2.WinForms.Guna2Button btnPreview;
        private System.Windows.Forms.Panel panelFieldsHost;
        private System.Windows.Forms.Label lblHint;
        private System.Windows.Forms.ComboBox cmbCustomerPlot;
        private System.Windows.Forms.Label lblAlloc;
        private System.Windows.Forms.Label lblHeaderSociety;
        private System.Windows.Forms.ComboBox cmbHeaderSociety;
        private System.Windows.Forms.Label lblNameSearch;
        private System.Windows.Forms.TextBox txtNameSearch;
        private Guna.UI2.WinForms.Guna2Button btnFilter;
        private System.Windows.Forms.CheckBox chkDateFilter;
        private System.Windows.Forms.Label lblFrom;
        private System.Windows.Forms.DateTimePicker dtpFrom;
        private System.Windows.Forms.Label lblTo;
        private System.Windows.Forms.DateTimePicker dtpTo;
        private Microsoft.Reporting.WinForms.ReportViewer reportViewer1;
    }
}
