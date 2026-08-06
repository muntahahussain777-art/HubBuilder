namespace HubBuilders.Report_Form
{
    partial class frmBrokerCommissionform
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.guna2ControlBox1 = new Guna.UI2.WinForms.Guna2ControlBox();
            this.label1 = new System.Windows.Forms.Label();
            this.reportViewer1 = new Microsoft.Reporting.WinForms.ReportViewer();
            this.label2 = new System.Windows.Forms.Label();
            this.txtSearch = new Guna.UI2.WinForms.Guna2TextBox();
            this.todate = new Guna.UI2.WinForms.Guna2DateTimePicker();
            this.fromdate = new Guna.UI2.WinForms.Guna2DateTimePicker();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.btnSearch = new Guna.UI2.WinForms.Guna2Button();
            this.guna2ControlBox2 = new Guna.UI2.WinForms.Guna2ControlBox();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.guna2ControlBox2);
            this.panel1.Controls.Add(this.guna2ControlBox1);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1050, 78);
            this.panel1.TabIndex = 174;
            // 
            // guna2ControlBox1
            // 
            this.guna2ControlBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.guna2ControlBox1.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(139)))), ((int)(((byte)(152)))), ((int)(((byte)(166)))));
            this.guna2ControlBox1.HoverState.Parent = this.guna2ControlBox1;
            this.guna2ControlBox1.IconColor = System.Drawing.Color.White;
            this.guna2ControlBox1.Location = new System.Drawing.Point(982, 12);
            this.guna2ControlBox1.Name = "guna2ControlBox1";
            this.guna2ControlBox1.ShadowDecoration.Parent = this.guna2ControlBox1;
            this.guna2ControlBox1.Size = new System.Drawing.Size(56, 36);
            this.guna2ControlBox1.TabIndex = 3;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 27.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(367, 50);
            this.label1.TabIndex = 1;
            this.label1.Text = "Broker_Commission";
            // 
            // reportViewer1
            // 
            this.reportViewer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.reportViewer1.Location = new System.Drawing.Point(12, 129);
            this.reportViewer1.Name = "reportViewer1";
            this.reportViewer1.ServerReport.BearerToken = null;
            this.reportViewer1.Size = new System.Drawing.Size(1026, 411);
            this.reportViewer1.TabIndex = 173;
            this.reportViewer1.ZoomMode = Microsoft.Reporting.WinForms.ZoomMode.PageWidth;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(61)))));
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.White;
            this.label2.Location = new System.Drawing.Point(137, 97);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(60, 20);
            this.label2.TabIndex = 180;
            this.label2.Text = "Search";
            // 
            // txtSearch
            // 
            this.txtSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSearch.AutoRoundedCorners = true;
            this.txtSearch.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(61)))));
            this.txtSearch.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.txtSearch.BorderRadius = 13;
            this.txtSearch.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtSearch.DefaultText = "";
            this.txtSearch.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.txtSearch.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.txtSearch.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtSearch.DisabledState.Parent = this.txtSearch;
            this.txtSearch.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtSearch.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(61)))));
            this.txtSearch.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtSearch.FocusedState.Parent = this.txtSearch;
            this.txtSearch.ForeColor = System.Drawing.Color.White;
            this.txtSearch.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtSearch.HoverState.Parent = this.txtSearch;
            this.txtSearch.Location = new System.Drawing.Point(198, 93);
            this.txtSearch.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.PasswordChar = '\0';
            this.txtSearch.PlaceholderText = "";
            this.txtSearch.SelectedText = "";
            this.txtSearch.ShadowDecoration.Parent = this.txtSearch;
            this.txtSearch.Size = new System.Drawing.Size(154, 28);
            this.txtSearch.TabIndex = 179;
            // 
            // todate
            // 
            this.todate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.todate.AutoRoundedCorners = true;
            this.todate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(61)))));
            this.todate.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.todate.BorderRadius = 14;
            this.todate.BorderThickness = 1;
            this.todate.CheckedState.Parent = this.todate;
            this.todate.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(61)))));
            this.todate.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.todate.ForeColor = System.Drawing.Color.White;
            this.todate.Format = System.Windows.Forms.DateTimePickerFormat.Long;
            this.todate.HoverState.Parent = this.todate;
            this.todate.Location = new System.Drawing.Point(650, 92);
            this.todate.MaxDate = new System.DateTime(9998, 12, 31, 0, 0, 0, 0);
            this.todate.MinDate = new System.DateTime(1753, 1, 1, 0, 0, 0, 0);
            this.todate.Name = "todate";
            this.todate.ShadowDecoration.Parent = this.todate;
            this.todate.Size = new System.Drawing.Size(240, 31);
            this.todate.TabIndex = 176;
            this.todate.Value = new System.DateTime(2024, 6, 21, 19, 54, 39, 787);
            // 
            // fromdate
            // 
            this.fromdate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.fromdate.AutoRoundedCorners = true;
            this.fromdate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(61)))));
            this.fromdate.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.fromdate.BorderRadius = 14;
            this.fromdate.BorderThickness = 1;
            this.fromdate.CheckedState.Parent = this.fromdate;
            this.fromdate.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(61)))));
            this.fromdate.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.fromdate.ForeColor = System.Drawing.Color.White;
            this.fromdate.Format = System.Windows.Forms.DateTimePickerFormat.Long;
            this.fromdate.HoverState.Parent = this.fromdate;
            this.fromdate.Location = new System.Drawing.Point(398, 93);
            this.fromdate.MaxDate = new System.DateTime(9998, 12, 31, 0, 0, 0, 0);
            this.fromdate.MinDate = new System.DateTime(1753, 1, 1, 0, 0, 0, 0);
            this.fromdate.Name = "fromdate";
            this.fromdate.ShadowDecoration.Parent = this.fromdate;
            this.fromdate.Size = new System.Drawing.Size(220, 31);
            this.fromdate.TabIndex = 175;
            this.fromdate.Value = new System.DateTime(2024, 6, 21, 19, 54, 39, 787);
            // 
            // label8
            // 
            this.label8.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label8.AutoSize = true;
            this.label8.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(61)))));
            this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.ForeColor = System.Drawing.Color.White;
            this.label8.Location = new System.Drawing.Point(624, 97);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(27, 20);
            this.label8.TabIndex = 177;
            this.label8.Text = "To";
            // 
            // label7
            // 
            this.label7.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label7.AutoSize = true;
            this.label7.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(61)))));
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.ForeColor = System.Drawing.Color.White;
            this.label7.Location = new System.Drawing.Point(352, 98);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(46, 20);
            this.label7.TabIndex = 178;
            this.label7.Text = "From";
            // 
            // btnSearch
            // 
            this.btnSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSearch.Animated = true;
            this.btnSearch.AutoRoundedCorners = true;
            this.btnSearch.BorderRadius = 15;
            this.btnSearch.CheckedState.Parent = this.btnSearch;
            this.btnSearch.CustomImages.Parent = this.btnSearch;
            this.btnSearch.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.btnSearch.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSearch.ForeColor = System.Drawing.Color.White;
            this.btnSearch.HoverState.Parent = this.btnSearch;
            this.btnSearch.Location = new System.Drawing.Point(920, 91);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.ShadowDecoration.Parent = this.btnSearch;
            this.btnSearch.Size = new System.Drawing.Size(95, 33);
            this.btnSearch.TabIndex = 181;
            this.btnSearch.Text = "Load";
            this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
            // 
            // guna2ControlBox2
            // 
            this.guna2ControlBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.guna2ControlBox2.ControlBoxType = Guna.UI2.WinForms.Enums.ControlBoxType.MaximizeBox;
            this.guna2ControlBox2.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(139)))), ((int)(((byte)(152)))), ((int)(((byte)(166)))));
            this.guna2ControlBox2.HoverState.Parent = this.guna2ControlBox2;
            this.guna2ControlBox2.IconColor = System.Drawing.Color.White;
            this.guna2ControlBox2.Location = new System.Drawing.Point(920, 12);
            this.guna2ControlBox2.Name = "guna2ControlBox2";
            this.guna2ControlBox2.ShadowDecoration.Parent = this.guna2ControlBox2;
            this.guna2ControlBox2.Size = new System.Drawing.Size(56, 36);
            this.guna2ControlBox2.TabIndex = 5;
            // 
            // frmBrokerCommissionform
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1050, 541);
            this.Controls.Add(this.btnSearch);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.reportViewer1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtSearch);
            this.Controls.Add(this.todate);
            this.Controls.Add(this.fromdate);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Name = "frmBrokerCommissionform";
            this.Text = "frmBrokerCommissionform";
            this.Load += new System.EventHandler(this.frmBrokerCommissionform_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private Guna.UI2.WinForms.Guna2ControlBox guna2ControlBox1;
        private System.Windows.Forms.Label label1;
        private Microsoft.Reporting.WinForms.ReportViewer reportViewer1;
        private System.Windows.Forms.Label label2;
        public Guna.UI2.WinForms.Guna2TextBox txtSearch;
        private Guna.UI2.WinForms.Guna2DateTimePicker todate;
        private Guna.UI2.WinForms.Guna2DateTimePicker fromdate;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label7;
        private Guna.UI2.WinForms.Guna2Button btnSearch;
        private Guna.UI2.WinForms.Guna2ControlBox guna2ControlBox2;
    }
}