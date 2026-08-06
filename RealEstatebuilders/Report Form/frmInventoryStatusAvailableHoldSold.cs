using Microsoft.Reporting.WinForms;
using HubBuilders.ProjectConnection;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

namespace HubBuilders.Report_Form
{
    public partial class frmInventoryStatusAvailableHoldSold : Sample
    {
        public frmInventoryStatusAvailableHoldSold()
        {
            InitializeComponent();
            ReportHeaderSocietyHelper.Attach(this);
        }

        private void frmInventoryStatusAvailableHoldSold_Load(object sender, EventArgs e)
        {
            LoadAllRecords();
        }

        private void LoadAllRecords()
        {
            try
            {
                string connectionString = projectconnection.conReturn();

                using (var con = new SQLiteConnection(connectionString))
                {
                    con.Open();

                    string query = @"
-- @SocietyId, @BlockId, @PlotTypeId, @Status
SELECT 
  s.name AS society, 
  b.name AS block, 
  pt.code AS plot_type,
  SUM(CASE WHEN p.status='Available' THEN 1 ELSE 0 END) AS available,
  SUM(CASE WHEN p.status='Hold'      THEN 1 ELSE 0 END) AS hold,
  SUM(CASE WHEN p.status='Sold'      THEN 1 ELSE 0 END) AS sold
FROM plots p
JOIN blocks    b  ON b.id = p.block_id
JOIN societies s  ON s.id = p.society_id
JOIN plot_types pt ON pt.id = p.plot_type_id
WHERE (@SocietyId  IS NULL OR p.society_id  = @SocietyId)
  AND (@BlockId    IS NULL OR p.block_id    = @BlockId)
  AND (@PlotTypeId IS NULL OR p.plot_type_id= @PlotTypeId)
  AND (@Status     IS NULL OR p.status      = @Status)
GROUP BY s.name, b.name, pt.code
ORDER BY s.name, b.name, pt.code;";

                    using (var cmd = new SQLiteCommand(query, con))
                    {
                        // All filters OFF by default
                        cmd.Parameters.AddWithValue("@SocietyId", DBNull.Value);
                        cmd.Parameters.AddWithValue("@BlockId", DBNull.Value);
                        cmd.Parameters.AddWithValue("@PlotTypeId", DBNull.Value);
                        cmd.Parameters.AddWithValue("@Status", DBNull.Value);

                        using (var da = new SQLiteDataAdapter(cmd))
                        {
                            var dt = new DataTable();
                            da.Fill(dt);

                            if (dt.Rows.Count == 0)
                            {
                                MessageBox.Show("No records found!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return;
                            }

                            var rds = new ReportDataSource("DataSet1", dt);
                            string reportPath = Path.Combine(Application.StartupPath, "Reports", "inventoryStatusReport.rdlc");

                            if (!File.Exists(reportPath))
                            {
                                MessageBox.Show("Report file not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }

                            reportViewer1.LocalReport.ReportPath = reportPath;
            ReportHeaderSocietyHelper.Apply(reportViewer1.LocalReport, this);
                            reportViewer1.LocalReport.DataSources.Clear();
                            reportViewer1.LocalReport.DataSources.Add(rds);
                            reportViewer1.RefreshReport();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "SQLite Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // OPTIONAL: Agar tum filters add karna chaho, is method ko UI se call karke values pass kar do.
        // Example usage:
        // GetData(cbSociety.SelectedValue, cbBlock.SelectedValue, cbPlotType.SelectedValue, cbStatus.Text);
        public void GetData(object societyId = null, object blockId = null, object plotTypeId = null, string status = null)
        {
            try
            {
                string qry = @"
-- @SocietyId, @BlockId, @PlotTypeId, @Status
SELECT 
  s.name AS society, 
  b.name AS block, 
  pt.code AS plot_type,
  SUM(CASE WHEN p.status='Available' THEN 1 ELSE 0 END) AS available,
  SUM(CASE WHEN p.status='Hold'      THEN 1 ELSE 0 END) AS hold,
  SUM(CASE WHEN p.status='Sold'      THEN 1 ELSE 0 END) AS sold
FROM plots p
JOIN blocks    b  ON b.id = p.block_id
JOIN societies s  ON s.id = p.society_id
JOIN plot_types pt ON pt.id = p.plot_type_id
WHERE (@SocietyId  IS NULL OR p.society_id  = @SocietyId)
  AND (@BlockId    IS NULL OR p.block_id    = @BlockId)
  AND (@PlotTypeId IS NULL OR p.plot_type_id= @PlotTypeId)
  AND (@Status     IS NULL OR p.status      = @Status)
GROUP BY s.name, b.name, pt.code
ORDER BY s.name, b.name, pt.code;";

                string connectionString = projectconnection.conReturn();

                using (var con = new SQLiteConnection(connectionString))
                using (var cmd = new SQLiteCommand(qry, con))
                {
                    // Normalize params (NULL if not provided)
                    cmd.Parameters.AddWithValue("@SocietyId", societyId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@BlockId", blockId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@PlotTypeId", plotTypeId ?? (object)DBNull.Value);
                    if (!string.IsNullOrWhiteSpace(status))
                        cmd.Parameters.AddWithValue("@Status", status);
                    else
                        cmd.Parameters.AddWithValue("@Status", DBNull.Value);

                    var sd = new SQLiteDataAdapter(cmd);
                    var dt = new DataTable();
                    sd.Fill(dt);

                    var rds = new ReportDataSource("DataSet1", dt);
                    string reportPath = Path.Combine(Application.StartupPath, "Reports", "inventoryStatusReport.rdlc");
                    if (!File.Exists(reportPath))
                    {
                        MessageBox.Show("Report file not found:\n" + reportPath, "Report",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    reportViewer1.LocalReport.ReportPath = reportPath;
            ReportHeaderSocietyHelper.Apply(reportViewer1.LocalReport, this);
                    reportViewer1.LocalReport.DataSources.Clear();
                    reportViewer1.LocalReport.DataSources.Add(rds);
                    reportViewer1.RefreshReport();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "SQLite Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
