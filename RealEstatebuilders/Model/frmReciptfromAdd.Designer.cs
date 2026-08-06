namespace HubBuilders.Model
{
    partial class frmReciptfromAdd
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
            this.txtRefNo = new Guna.UI2.WinForms.Guna2TextBox();
            this.dtpReceiptDate = new Guna.UI2.WinForms.Guna2DateTimePicker();
            this.label7 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.cmbMode = new Guna.UI2.WinForms.Guna2ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.numAmount = new Guna.UI2.WinForms.Guna2NumericUpDown();
            this.label8 = new System.Windows.Forms.Label();
            this.txtReceiptNotes = new Guna.UI2.WinForms.Guna2TextBox();
            this.cmbCustomerPlot = new Guna.UI2.WinForms.Guna2ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.numAmount)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Size = new System.Drawing.Size(175, 30);
            this.label1.Text = "Recipt Form Add";
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
            this.txtRefNo.Location = new System.Drawing.Point(272, 121);
            this.txtRefNo.Margin = new System.Windows.Forms.Padding(6, 8, 6, 8);
            this.txtRefNo.Name = "txtRefNo";
            this.txtRefNo.PasswordChar = '\0';
            this.txtRefNo.PlaceholderText = "";
            this.txtRefNo.SelectedText = "";
            this.txtRefNo.ShadowDecoration.Parent = this.txtRefNo;
            this.txtRefNo.Size = new System.Drawing.Size(245, 36);
            this.txtRefNo.TabIndex = 1;
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
            this.dtpReceiptDate.Location = new System.Drawing.Point(535, 121);
            this.dtpReceiptDate.MaxDate = new System.DateTime(9998, 12, 31, 0, 0, 0, 0);
            this.dtpReceiptDate.MinDate = new System.DateTime(1753, 1, 1, 0, 0, 0, 0);
            this.dtpReceiptDate.Name = "dtpReceiptDate";
            this.dtpReceiptDate.ShadowDecoration.Parent = this.dtpReceiptDate;
            this.dtpReceiptDate.Size = new System.Drawing.Size(267, 41);
            this.dtpReceiptDate.TabIndex = 2;
            this.dtpReceiptDate.Value = new System.DateTime(2024, 6, 21, 19, 54, 39, 787);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.ForeColor = System.Drawing.Color.White;
            this.label7.Location = new System.Drawing.Point(548, 95);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(104, 21);
            this.label7.TabIndex = 147;
            this.label7.Text = "Receipt Date";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.Color.White;
            this.label3.Location = new System.Drawing.Point(281, 92);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(61, 21);
            this.label3.TabIndex = 145;
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
            this.cmbMode.Location = new System.Drawing.Point(18, 121);
            this.cmbMode.Name = "cmbMode";
            this.cmbMode.ShadowDecoration.Parent = this.cmbMode;
            this.cmbMode.Size = new System.Drawing.Size(245, 36);
            this.cmbMode.TabIndex = 0;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.White;
            this.label4.Location = new System.Drawing.Point(36, 97);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(54, 21);
            this.label4.TabIndex = 143;
            this.label4.Text = "Mode";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.White;
            this.label2.Location = new System.Drawing.Point(277, 183);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(69, 21);
            this.label2.TabIndex = 154;
            this.label2.Text = "Amount";
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
            this.numAmount.Location = new System.Drawing.Point(269, 207);
            this.numAmount.Name = "numAmount";
            this.numAmount.ShadowDecoration.Parent = this.numAmount;
            this.numAmount.Size = new System.Drawing.Size(248, 36);
            this.numAmount.TabIndex = 4;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.ForeColor = System.Drawing.Color.White;
            this.label8.Location = new System.Drawing.Point(46, 260);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(47, 21);
            this.label8.TabIndex = 151;
            this.label8.Text = "Note";
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
            this.txtReceiptNotes.Location = new System.Drawing.Point(18, 284);
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
            this.cmbCustomerPlot.Location = new System.Drawing.Point(18, 207);
            this.cmbCustomerPlot.Name = "cmbCustomerPlot";
            this.cmbCustomerPlot.ShadowDecoration.Parent = this.cmbCustomerPlot;
            this.cmbCustomerPlot.Size = new System.Drawing.Size(245, 36);
            this.cmbCustomerPlot.TabIndex = 3;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.ForeColor = System.Drawing.Color.White;
            this.label5.Location = new System.Drawing.Point(27, 183);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(206, 21);
            this.label5.TabIndex = 149;
            this.label5.Text = "Allocation Customer Name";
            // 
            // frmReciptfromAdd
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(817, 450);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.numAmount);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.txtReceiptNotes);
            this.Controls.Add(this.cmbCustomerPlot);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.txtRefNo);
            this.Controls.Add(this.dtpReceiptDate);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.cmbMode);
            this.Controls.Add(this.label4);
            this.Name = "frmReciptfromAdd";
            this.Text = "frmReciptfromAdd";
            this.Load += new System.EventHandler(this.FrmReceiptFormAdd_Load);
            this.Controls.SetChildIndex(this.label4, 0);
            this.Controls.SetChildIndex(this.cmbMode, 0);
            this.Controls.SetChildIndex(this.label3, 0);
            this.Controls.SetChildIndex(this.label7, 0);
            this.Controls.SetChildIndex(this.dtpReceiptDate, 0);
            this.Controls.SetChildIndex(this.txtRefNo, 0);
            this.Controls.SetChildIndex(this.label5, 0);
            this.Controls.SetChildIndex(this.cmbCustomerPlot, 0);
            this.Controls.SetChildIndex(this.txtReceiptNotes, 0);
            this.Controls.SetChildIndex(this.label8, 0);
            this.Controls.SetChildIndex(this.numAmount, 0);
            this.Controls.SetChildIndex(this.label2, 0);
            ((System.ComponentModel.ISupportInitialize)(this.numAmount)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public Guna.UI2.WinForms.Guna2TextBox txtRefNo;
        public Guna.UI2.WinForms.Guna2DateTimePicker dtpReceiptDate;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label3;
        public Guna.UI2.WinForms.Guna2ComboBox cmbMode;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label2;
        private Guna.UI2.WinForms.Guna2NumericUpDown numAmount;
        private System.Windows.Forms.Label label8;
        public Guna.UI2.WinForms.Guna2TextBox txtReceiptNotes;
        public Guna.UI2.WinForms.Guna2ComboBox cmbCustomerPlot;
        private System.Windows.Forms.Label label5;
    }
}