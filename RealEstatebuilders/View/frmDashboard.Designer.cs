namespace HubBuilders.View
{
    partial class frmDashboard
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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea2 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend2 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.panel1 = new System.Windows.Forms.Panel();
            this.chartPL = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.panel2 = new System.Windows.Forms.Panel();
            this.cbYear = new Guna.UI2.WinForms.Guna2ComboBox();
            this.panel3 = new System.Windows.Forms.Panel();
            this.chartReceipts = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chartPL)).BeginInit();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chartReceipts)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.Controls.Add(this.chartPL);
            this.panel1.Location = new System.Drawing.Point(12, 70);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(937, 353);
            this.panel1.TabIndex = 1;
            // 
            // chartPL
            // 
            chartArea1.Name = "ChartArea1";
            this.chartPL.ChartAreas.Add(chartArea1);
            this.chartPL.Dock = System.Windows.Forms.DockStyle.Fill;
            legend1.Name = "Legend1";
            this.chartPL.Legends.Add(legend1);
            this.chartPL.Location = new System.Drawing.Point(0, 0);
            this.chartPL.Name = "chartPL";
            series1.ChartArea = "ChartArea1";
            series1.Legend = "Legend1";
            series1.Name = "Series1";
            this.chartPL.Series.Add(series1);
            this.chartPL.Size = new System.Drawing.Size(937, 353);
            this.chartPL.TabIndex = 1;
            this.chartPL.Text = "chart1";
            // 
            // panel2
            // 
            this.panel2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.panel2.Controls.Add(this.cbYear);
            this.panel2.Location = new System.Drawing.Point(638, 12);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(323, 45);
            this.panel2.TabIndex = 2;
            // 
            // cbYear
            // 
            this.cbYear.AutoRoundedCorners = true;
            this.cbYear.BackColor = System.Drawing.Color.Transparent;
            this.cbYear.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.cbYear.BorderRadius = 17;
            this.cbYear.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cbYear.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbYear.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.cbYear.FocusedColor = System.Drawing.Color.Empty;
            this.cbYear.FocusedState.Parent = this.cbYear;
            this.cbYear.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbYear.ForeColor = System.Drawing.Color.White;
            this.cbYear.FormattingEnabled = true;
            this.cbYear.HoverState.Parent = this.cbYear;
            this.cbYear.ItemHeight = 30;
            this.cbYear.ItemsAppearance.Parent = this.cbYear;
            this.cbYear.Location = new System.Drawing.Point(39, 4);
            this.cbYear.Name = "cbYear";
            this.cbYear.ShadowDecoration.Parent = this.cbYear;
            this.cbYear.Size = new System.Drawing.Size(245, 36);
            this.cbYear.TabIndex = 150;
            // 
            // panel3
            // 
            this.panel3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel3.Controls.Add(this.chartReceipts);
            this.panel3.Location = new System.Drawing.Point(12, 438);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(937, 353);
            this.panel3.TabIndex = 3;
            // 
            // chartReceipts
            // 
            chartArea2.Name = "ChartArea1";
            this.chartReceipts.ChartAreas.Add(chartArea2);
            this.chartReceipts.Dock = System.Windows.Forms.DockStyle.Fill;
            legend2.Name = "Legend1";
            this.chartReceipts.Legends.Add(legend2);
            this.chartReceipts.Location = new System.Drawing.Point(0, 0);
            this.chartReceipts.Name = "chartReceipts";
            series2.ChartArea = "ChartArea1";
            series2.Legend = "Legend1";
            series2.Name = "Series1";
            this.chartReceipts.Series.Add(series2);
            this.chartReceipts.Size = new System.Drawing.Size(937, 353);
            this.chartReceipts.TabIndex = 1;
            this.chartReceipts.Text = "chart1";
            // 
            // frmDashboard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(1007, 788);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Name = "frmDashboard";
            this.Text = "frmDashboard";
            this.Load += new System.EventHandler(this.frmDashboard_Load);
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.chartPL)).EndInit();
            this.panel2.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.chartReceipts)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.DataVisualization.Charting.Chart chartPL;
        private System.Windows.Forms.Panel panel2;
        public Guna.UI2.WinForms.Guna2ComboBox cbYear;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.DataVisualization.Charting.Chart chartReceipts;
    }
}