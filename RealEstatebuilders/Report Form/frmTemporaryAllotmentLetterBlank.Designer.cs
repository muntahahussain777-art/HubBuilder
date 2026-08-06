namespace HubBuilders.Report_Form
{
    partial class frmTemporaryAllotmentLetterBlank
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
            this.panelFields = new System.Windows.Forms.Panel();
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
            this.labelTitle.Size = new System.Drawing.Size(620, 32);
            this.labelTitle.TabIndex = 0;
            this.labelTitle.Text = "Temporary Allotment Letter — Blank (Text Only)";
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
            this.btnPreview.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(121)))), ((int)(((byte)(107)))));
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
            this.panelFieldsHost.AutoScroll = true;
            this.panelFieldsHost.Controls.Add(this.panelFields);
            this.panelFieldsHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelFieldsHost.Location = new System.Drawing.Point(8, 8);
            this.panelFieldsHost.Name = "panelFieldsHost";
            this.panelFieldsHost.Size = new System.Drawing.Size(404, 574);
            this.panelFieldsHost.TabIndex = 0;
            // 
            // panelFields
            // 
            this.panelFields.Location = new System.Drawing.Point(0, 0);
            this.panelFields.Name = "panelFields";
            this.panelFields.Size = new System.Drawing.Size(380, 900);
            this.panelFields.TabIndex = 0;
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
            // frmTemporaryAllotmentLetterBlank
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1280, 720);
            this.Controls.Add(this.reportViewer1);
            this.Controls.Add(this.panelLeft);
            this.Controls.Add(this.panelTop);
            this.Name = "frmTemporaryAllotmentLetterBlank";
            this.Text = "Temporary Allotment Letter Blank";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.frmTemporaryAllotmentLetterBlank_Load);
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            this.panelLeft.ResumeLayout(false);
            this.panelButtons.ResumeLayout(false);
            this.panelFieldsHost.ResumeLayout(false);
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
        private System.Windows.Forms.Panel panelFields;
        private Microsoft.Reporting.WinForms.ReportViewer reportViewer1;

        internal System.Windows.Forms.TextBox txtDate;
        internal System.Windows.Forms.TextBox txtName;
        internal System.Windows.Forms.TextBox txtFather;
        internal System.Windows.Forms.TextBox txtMembershipNo;
        internal System.Windows.Forms.TextBox txtPlotNo;
        internal System.Windows.Forms.TextBox txtArea;
        internal System.Windows.Forms.TextBox txtLocationDetail;
        internal System.Windows.Forms.TextBox txtNic;
    }
}
