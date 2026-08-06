namespace HubBuilders.Model
{
    partial class frmPlotTypeAdd
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
            this.lblcode = new System.Windows.Forms.Label();
            this.txtCode = new Guna.UI2.WinForms.Guna2TextBox();
            this.txtSociety = new System.Windows.Forms.Label();
            this.cbSociety = new Guna.UI2.WinForms.Guna2ComboBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Size = new System.Drawing.Size(148, 30);
            this.label1.Text = "PlotTypeForm";
            // 
            // lblcode
            // 
            this.lblcode.AutoSize = true;
            this.lblcode.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblcode.ForeColor = System.Drawing.Color.White;
            this.lblcode.Location = new System.Drawing.Point(283, 94);
            this.lblcode.Name = "lblcode";
            this.lblcode.Size = new System.Drawing.Size(72, 21);
            this.lblcode.TabIndex = 107;
            this.lblcode.Text = "Plot Size";
            // 
            // txtCode
            // 
            this.txtCode.AutoRoundedCorners = true;
            this.txtCode.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.txtCode.BorderRadius = 17;
            this.txtCode.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtCode.DefaultText = "";
            this.txtCode.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.txtCode.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.txtCode.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtCode.DisabledState.Parent = this.txtCode;
            this.txtCode.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtCode.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.txtCode.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtCode.FocusedState.Parent = this.txtCode;
            this.txtCode.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtCode.ForeColor = System.Drawing.Color.White;
            this.txtCode.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtCode.HoverState.Parent = this.txtCode;
            this.txtCode.Location = new System.Drawing.Point(269, 118);
            this.txtCode.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtCode.Name = "txtCode";
            this.txtCode.PasswordChar = '\0';
            this.txtCode.PlaceholderForeColor = System.Drawing.Color.Black;
            this.txtCode.PlaceholderText = "";
            this.txtCode.SelectedText = "";
            this.txtCode.ShadowDecoration.Parent = this.txtCode;
            this.txtCode.Size = new System.Drawing.Size(233, 36);
            this.txtCode.TabIndex = 1;
            // 
            // txtSociety
            // 
            this.txtSociety.AutoSize = true;
            this.txtSociety.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtSociety.ForeColor = System.Drawing.Color.White;
            this.txtSociety.Location = new System.Drawing.Point(37, 94);
            this.txtSociety.Name = "txtSociety";
            this.txtSociety.Size = new System.Drawing.Size(64, 21);
            this.txtSociety.TabIndex = 105;
            this.txtSociety.Text = "Society";
            // 
            // cbSociety
            // 
            this.cbSociety.AutoRoundedCorners = true;
            this.cbSociety.BackColor = System.Drawing.Color.Transparent;
            this.cbSociety.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.cbSociety.BorderRadius = 17;
            this.cbSociety.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cbSociety.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbSociety.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.cbSociety.FocusedColor = System.Drawing.Color.Empty;
            this.cbSociety.FocusedState.Parent = this.cbSociety;
            this.cbSociety.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbSociety.ForeColor = System.Drawing.Color.White;
            this.cbSociety.FormattingEnabled = true;
            this.cbSociety.HoverState.Parent = this.cbSociety;
            this.cbSociety.ItemHeight = 30;
            this.cbSociety.ItemsAppearance.Parent = this.cbSociety;
            this.cbSociety.Location = new System.Drawing.Point(17, 118);
            this.cbSociety.Name = "cbSociety";
            this.cbSociety.ShadowDecoration.Parent = this.cbSociety;
            this.cbSociety.Size = new System.Drawing.Size(245, 36);
            this.cbSociety.TabIndex = 0;
            // 
            // frmPlotTypeAdd
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(514, 251);
            this.Controls.Add(this.lblcode);
            this.Controls.Add(this.txtCode);
            this.Controls.Add(this.txtSociety);
            this.Controls.Add(this.cbSociety);
            this.Name = "frmPlotTypeAdd";
            this.Text = "frmPlotTypeFormAdd";
            this.Controls.SetChildIndex(this.cbSociety, 0);
            this.Controls.SetChildIndex(this.txtSociety, 0);
            this.Controls.SetChildIndex(this.txtCode, 0);
            this.Controls.SetChildIndex(this.lblcode, 0);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblcode;
        public Guna.UI2.WinForms.Guna2TextBox txtCode;
        private System.Windows.Forms.Label txtSociety;
        public Guna.UI2.WinForms.Guna2ComboBox cbSociety;
    }
}