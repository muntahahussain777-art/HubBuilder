namespace HubBuilders.Model
{
    partial class frmAllocationCancelAdd
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
            this.txtAllocationAmount = new Guna.UI.WinForms.GunaTextBox();
            this.txtReceivedAmount = new Guna.UI.WinForms.GunaTextBox();
            this.dtpCancelDate = new Guna.UI.WinForms.GunaDateTimePicker();
            this.txtRefundableAmount = new Guna.UI.WinForms.GunaTextBox();
            this.txtDeductionAmount = new Guna.UI.WinForms.GunaTextBox();
            this.cmbDeductionPercent = new Guna.UI.WinForms.GunaComboBox();
            this.txtCancelNotes = new Guna.UI.WinForms.GunaTextBox();
            this.gunaLabel1 = new Guna.UI.WinForms.GunaLabel();
            this.gunaLabel2 = new Guna.UI.WinForms.GunaLabel();
            this.gunaLabel3 = new Guna.UI.WinForms.GunaLabel();
            this.gunaLabel5 = new Guna.UI.WinForms.GunaLabel();
            this.gunaLabel6 = new Guna.UI.WinForms.GunaLabel();
            this.gunaLabel7 = new Guna.UI.WinForms.GunaLabel();
            this.gunaLabel8 = new Guna.UI.WinForms.GunaLabel();
            this.gunaLabel9 = new Guna.UI.WinForms.GunaLabel();
            this.txtAllocation = new Guna.UI.WinForms.GunaTextBox();
            this.cmbCustomerPlot = new Guna.UI.WinForms.GunaComboBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Size = new System.Drawing.Size(229, 37);
            this.label1.Text = "Cancel Allocation";
            // 
            // txtAllocationAmount
            // 
            this.txtAllocationAmount.BaseColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.txtAllocationAmount.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.txtAllocationAmount.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtAllocationAmount.FocusedBaseColor = System.Drawing.Color.White;
            this.txtAllocationAmount.FocusedBorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(88)))), ((int)(((byte)(255)))));
            this.txtAllocationAmount.FocusedForeColor = System.Drawing.SystemColors.ControlText;
            this.txtAllocationAmount.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtAllocationAmount.Location = new System.Drawing.Point(347, 269);
            this.txtAllocationAmount.Name = "txtAllocationAmount";
            this.txtAllocationAmount.PasswordChar = '\0';
            this.txtAllocationAmount.SelectedText = "";
            this.txtAllocationAmount.Size = new System.Drawing.Size(317, 39);
            this.txtAllocationAmount.TabIndex = 9;
            // 
            // txtReceivedAmount
            // 
            this.txtReceivedAmount.BaseColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.txtReceivedAmount.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.txtReceivedAmount.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtReceivedAmount.FocusedBaseColor = System.Drawing.Color.White;
            this.txtReceivedAmount.FocusedBorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(88)))), ((int)(((byte)(255)))));
            this.txtReceivedAmount.FocusedForeColor = System.Drawing.SystemColors.ControlText;
            this.txtReceivedAmount.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtReceivedAmount.Location = new System.Drawing.Point(681, 265);
            this.txtReceivedAmount.Name = "txtReceivedAmount";
            this.txtReceivedAmount.PasswordChar = '\0';
            this.txtReceivedAmount.SelectedText = "";
            this.txtReceivedAmount.Size = new System.Drawing.Size(317, 39);
            this.txtReceivedAmount.TabIndex = 10;
            // 
            // dtpCancelDate
            // 
            this.dtpCancelDate.BaseColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.dtpCancelDate.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.dtpCancelDate.CustomFormat = null;
            this.dtpCancelDate.DropDownAlign = System.Windows.Forms.LeftRightAlignment.Right;
            this.dtpCancelDate.FocusedColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(88)))), ((int)(((byte)(255)))));
            this.dtpCancelDate.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.dtpCancelDate.ForeColor = System.Drawing.Color.Black;
            this.dtpCancelDate.Location = new System.Drawing.Point(5, 377);
            this.dtpCancelDate.MaxDate = new System.DateTime(9998, 12, 31, 0, 0, 0, 0);
            this.dtpCancelDate.MinDate = new System.DateTime(1753, 1, 1, 0, 0, 0, 0);
            this.dtpCancelDate.Name = "dtpCancelDate";
            this.dtpCancelDate.OnHoverBaseColor = System.Drawing.Color.White;
            this.dtpCancelDate.OnHoverBorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(88)))), ((int)(((byte)(255)))));
            this.dtpCancelDate.OnHoverForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(88)))), ((int)(((byte)(255)))));
            this.dtpCancelDate.OnPressedColor = System.Drawing.Color.Black;
            this.dtpCancelDate.Size = new System.Drawing.Size(326, 39);
            this.dtpCancelDate.TabIndex = 12;
            this.dtpCancelDate.Text = "Monday, March 16, 2026";
            this.dtpCancelDate.Value = new System.DateTime(2026, 3, 16, 15, 18, 19, 747);
            // 
            // txtRefundableAmount
            // 
            this.txtRefundableAmount.BaseColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.txtRefundableAmount.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.txtRefundableAmount.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtRefundableAmount.FocusedBaseColor = System.Drawing.Color.White;
            this.txtRefundableAmount.FocusedBorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(88)))), ((int)(((byte)(255)))));
            this.txtRefundableAmount.FocusedForeColor = System.Drawing.SystemColors.ControlText;
            this.txtRefundableAmount.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtRefundableAmount.Location = new System.Drawing.Point(18, 498);
            this.txtRefundableAmount.Name = "txtRefundableAmount";
            this.txtRefundableAmount.PasswordChar = '\0';
            this.txtRefundableAmount.SelectedText = "";
            this.txtRefundableAmount.Size = new System.Drawing.Size(317, 39);
            this.txtRefundableAmount.TabIndex = 14;
            // 
            // txtDeductionAmount
            // 
            this.txtDeductionAmount.BaseColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.txtDeductionAmount.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.txtDeductionAmount.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtDeductionAmount.FocusedBaseColor = System.Drawing.Color.White;
            this.txtDeductionAmount.FocusedBorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(88)))), ((int)(((byte)(255)))));
            this.txtDeductionAmount.FocusedForeColor = System.Drawing.SystemColors.ControlText;
            this.txtDeductionAmount.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtDeductionAmount.Location = new System.Drawing.Point(681, 377);
            this.txtDeductionAmount.Name = "txtDeductionAmount";
            this.txtDeductionAmount.PasswordChar = '\0';
            this.txtDeductionAmount.SelectedText = "";
            this.txtDeductionAmount.Size = new System.Drawing.Size(317, 39);
            this.txtDeductionAmount.TabIndex = 13;
            // 
            // cmbDeductionPercent
            // 
            this.cmbDeductionPercent.BackColor = System.Drawing.Color.Transparent;
            this.cmbDeductionPercent.BaseColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.cmbDeductionPercent.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.cmbDeductionPercent.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cmbDeductionPercent.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDeductionPercent.FocusedColor = System.Drawing.Color.Empty;
            this.cmbDeductionPercent.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbDeductionPercent.ForeColor = System.Drawing.Color.Black;
            this.cmbDeductionPercent.FormattingEnabled = true;
            this.cmbDeductionPercent.Location = new System.Drawing.Point(339, 381);
            this.cmbDeductionPercent.Name = "cmbDeductionPercent";
            this.cmbDeductionPercent.OnHoverItemBaseColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(88)))), ((int)(((byte)(255)))));
            this.cmbDeductionPercent.OnHoverItemForeColor = System.Drawing.Color.White;
            this.cmbDeductionPercent.Size = new System.Drawing.Size(317, 35);
            this.cmbDeductionPercent.TabIndex = 15;
            this.cmbDeductionPercent.SelectedIndexChanged += new System.EventHandler(this.cmbDeductionPercent_SelectedIndexChanged);
            // 
            // txtCancelNotes
            // 
            this.txtCancelNotes.BaseColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.txtCancelNotes.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.txtCancelNotes.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtCancelNotes.FocusedBaseColor = System.Drawing.Color.White;
            this.txtCancelNotes.FocusedBorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(88)))), ((int)(((byte)(255)))));
            this.txtCancelNotes.FocusedForeColor = System.Drawing.SystemColors.ControlText;
            this.txtCancelNotes.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtCancelNotes.Location = new System.Drawing.Point(344, 460);
            this.txtCancelNotes.Multiline = true;
            this.txtCancelNotes.Name = "txtCancelNotes";
            this.txtCancelNotes.PasswordChar = '\0';
            this.txtCancelNotes.SelectedText = "";
            this.txtCancelNotes.Size = new System.Drawing.Size(654, 108);
            this.txtCancelNotes.TabIndex = 16;
            // 
            // gunaLabel1
            // 
            this.gunaLabel1.AutoSize = true;
            this.gunaLabel1.Font = new System.Drawing.Font("Segoe UI", 13.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.gunaLabel1.Location = new System.Drawing.Point(9, 227);
            this.gunaLabel1.Name = "gunaLabel1";
            this.gunaLabel1.Size = new System.Drawing.Size(125, 31);
            this.gunaLabel1.TabIndex = 18;
            this.gunaLabel1.Text = "Allocation";
            // 
            // gunaLabel2
            // 
            this.gunaLabel2.AutoSize = true;
            this.gunaLabel2.Font = new System.Drawing.Font("Segoe UI", 13.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.gunaLabel2.Location = new System.Drawing.Point(368, 227);
            this.gunaLabel2.Name = "gunaLabel2";
            this.gunaLabel2.Size = new System.Drawing.Size(219, 31);
            this.gunaLabel2.TabIndex = 19;
            this.gunaLabel2.Text = "Allocation Amount";
            // 
            // gunaLabel3
            // 
            this.gunaLabel3.AutoSize = true;
            this.gunaLabel3.Font = new System.Drawing.Font("Segoe UI", 13.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.gunaLabel3.Location = new System.Drawing.Point(696, 227);
            this.gunaLabel3.Name = "gunaLabel3";
            this.gunaLabel3.Size = new System.Drawing.Size(202, 31);
            this.gunaLabel3.TabIndex = 20;
            this.gunaLabel3.Text = "Received Amount";
            // 
            // gunaLabel5
            // 
            this.gunaLabel5.AutoSize = true;
            this.gunaLabel5.Font = new System.Drawing.Font("Segoe UI", 13.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.gunaLabel5.Location = new System.Drawing.Point(12, 334);
            this.gunaLabel5.Name = "gunaLabel5";
            this.gunaLabel5.Size = new System.Drawing.Size(220, 31);
            this.gunaLabel5.TabIndex = 22;
            this.gunaLabel5.Text = "Deduction Amount";
            // 
            // gunaLabel6
            // 
            this.gunaLabel6.AutoSize = true;
            this.gunaLabel6.Font = new System.Drawing.Font("Segoe UI", 13.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.gunaLabel6.Location = new System.Drawing.Point(354, 334);
            this.gunaLabel6.Name = "gunaLabel6";
            this.gunaLabel6.Size = new System.Drawing.Size(230, 31);
            this.gunaLabel6.TabIndex = 23;
            this.gunaLabel6.Text = "Refundable Amount";
            // 
            // gunaLabel7
            // 
            this.gunaLabel7.AutoSize = true;
            this.gunaLabel7.Font = new System.Drawing.Font("Segoe UI", 13.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.gunaLabel7.Location = new System.Drawing.Point(25, 451);
            this.gunaLabel7.Name = "gunaLabel7";
            this.gunaLabel7.Size = new System.Drawing.Size(230, 31);
            this.gunaLabel7.TabIndex = 25;
            this.gunaLabel7.Text = "Refundable Amount";
            // 
            // gunaLabel8
            // 
            this.gunaLabel8.AutoSize = true;
            this.gunaLabel8.Font = new System.Drawing.Font("Segoe UI", 13.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.gunaLabel8.Location = new System.Drawing.Point(677, 330);
            this.gunaLabel8.Name = "gunaLabel8";
            this.gunaLabel8.Size = new System.Drawing.Size(220, 31);
            this.gunaLabel8.TabIndex = 24;
            this.gunaLabel8.Text = "Deduction Amount";
            // 
            // gunaLabel9
            // 
            this.gunaLabel9.AutoSize = true;
            this.gunaLabel9.Font = new System.Drawing.Font("Segoe UI", 13.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.gunaLabel9.Location = new System.Drawing.Point(347, 420);
            this.gunaLabel9.Name = "gunaLabel9";
            this.gunaLabel9.Size = new System.Drawing.Size(67, 31);
            this.gunaLabel9.TabIndex = 26;
            this.gunaLabel9.Text = "Note";
            // 
            // txtAllocation
            // 
            this.txtAllocation.BaseColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.txtAllocation.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.txtAllocation.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtAllocation.FocusedBaseColor = System.Drawing.Color.White;
            this.txtAllocation.FocusedBorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(88)))), ((int)(((byte)(255)))));
            this.txtAllocation.FocusedForeColor = System.Drawing.SystemColors.ControlText;
            this.txtAllocation.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtAllocation.Location = new System.Drawing.Point(12, 167);
            this.txtAllocation.Name = "txtAllocation";
            this.txtAllocation.PasswordChar = '\0';
            this.txtAllocation.SelectedText = "";
            this.txtAllocation.Size = new System.Drawing.Size(317, 39);
            this.txtAllocation.TabIndex = 27;
            // 
            // cmbCustomerPlot
            // 
            this.cmbCustomerPlot.BackColor = System.Drawing.Color.Transparent;
            this.cmbCustomerPlot.BaseColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.cmbCustomerPlot.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.cmbCustomerPlot.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cmbCustomerPlot.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCustomerPlot.FocusedColor = System.Drawing.Color.Empty;
            this.cmbCustomerPlot.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbCustomerPlot.ForeColor = System.Drawing.Color.Black;
            this.cmbCustomerPlot.FormattingEnabled = true;
            this.cmbCustomerPlot.Location = new System.Drawing.Point(5, 269);
            this.cmbCustomerPlot.Name = "cmbCustomerPlot";
            this.cmbCustomerPlot.OnHoverItemBaseColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(88)))), ((int)(((byte)(255)))));
            this.cmbCustomerPlot.OnHoverItemForeColor = System.Drawing.Color.White;
            this.cmbCustomerPlot.Size = new System.Drawing.Size(317, 35);
            this.cmbCustomerPlot.TabIndex = 11;
            // 
            // frmAllocationCancelAdd
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(11F, 28F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1031, 721);
            this.Controls.Add(this.txtAllocation);
            this.Controls.Add(this.gunaLabel9);
            this.Controls.Add(this.gunaLabel7);
            this.Controls.Add(this.gunaLabel8);
            this.Controls.Add(this.gunaLabel6);
            this.Controls.Add(this.gunaLabel5);
            this.Controls.Add(this.gunaLabel3);
            this.Controls.Add(this.gunaLabel2);
            this.Controls.Add(this.gunaLabel1);
            this.Controls.Add(this.txtCancelNotes);
            this.Controls.Add(this.cmbDeductionPercent);
            this.Controls.Add(this.txtRefundableAmount);
            this.Controls.Add(this.txtDeductionAmount);
            this.Controls.Add(this.dtpCancelDate);
            this.Controls.Add(this.cmbCustomerPlot);
            this.Controls.Add(this.txtReceivedAmount);
            this.Controls.Add(this.txtAllocationAmount);
            this.Margin = new System.Windows.Forms.Padding(7, 12, 7, 12);
            this.Name = "frmAllocationCancelAdd";
            this.Text = "frmAllocationCancelAdd";
            this.Load += new System.EventHandler(this.frmAllocationCancelAdd_Load);
            this.Controls.SetChildIndex(this.txtAllocationAmount, 0);
            this.Controls.SetChildIndex(this.txtReceivedAmount, 0);
            this.Controls.SetChildIndex(this.cmbCustomerPlot, 0);
            this.Controls.SetChildIndex(this.dtpCancelDate, 0);
            this.Controls.SetChildIndex(this.txtDeductionAmount, 0);
            this.Controls.SetChildIndex(this.txtRefundableAmount, 0);
            this.Controls.SetChildIndex(this.cmbDeductionPercent, 0);
            this.Controls.SetChildIndex(this.txtCancelNotes, 0);
            this.Controls.SetChildIndex(this.gunaLabel1, 0);
            this.Controls.SetChildIndex(this.gunaLabel2, 0);
            this.Controls.SetChildIndex(this.gunaLabel3, 0);
            this.Controls.SetChildIndex(this.gunaLabel5, 0);
            this.Controls.SetChildIndex(this.gunaLabel6, 0);
            this.Controls.SetChildIndex(this.gunaLabel8, 0);
            this.Controls.SetChildIndex(this.gunaLabel7, 0);
            this.Controls.SetChildIndex(this.gunaLabel9, 0);
            this.Controls.SetChildIndex(this.txtAllocation, 0);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private Guna.UI.WinForms.GunaTextBox txtAllocationAmount;
        private Guna.UI.WinForms.GunaTextBox txtReceivedAmount;
        private Guna.UI.WinForms.GunaDateTimePicker dtpCancelDate;
        private Guna.UI.WinForms.GunaTextBox txtRefundableAmount;
        private Guna.UI.WinForms.GunaTextBox txtDeductionAmount;
        private Guna.UI.WinForms.GunaComboBox cmbDeductionPercent;
        private Guna.UI.WinForms.GunaTextBox txtCancelNotes;
        private Guna.UI.WinForms.GunaLabel gunaLabel1;
        private Guna.UI.WinForms.GunaLabel gunaLabel2;
        private Guna.UI.WinForms.GunaLabel gunaLabel3;
        private Guna.UI.WinForms.GunaLabel gunaLabel5;
        private Guna.UI.WinForms.GunaLabel gunaLabel6;
        private Guna.UI.WinForms.GunaLabel gunaLabel7;
        private Guna.UI.WinForms.GunaLabel gunaLabel8;
        private Guna.UI.WinForms.GunaLabel gunaLabel9;
        private Guna.UI.WinForms.GunaTextBox txtAllocation;
        private Guna.UI.WinForms.GunaComboBox cmbCustomerPlot;
    }
}