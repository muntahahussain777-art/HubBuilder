using HubBuilders.ProjectConnection;
using Microsoft.Reporting.WinForms;
using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace HubBuilders.Report_Form
{
    /// <summary>
    /// Available plots with Society/Block/PlotType filters + RDLC/PDF via ReportViewer.
    /// </summary>
    public class frmAvailablePlotsReport : Sample
    {
        private ComboBox cmbSociety;
        private ComboBox cmbBlock;
        private ComboBox cmbPlotType;
        private Guna.UI2.WinForms.Guna2Button btnLoad;
        private ReportViewer reportViewer1;
        private bool _binding;

        public frmAvailablePlotsReport()
        {
            InitializeUi();
            LoadSocieties();
        }

        private void InitializeUi()
        {
            Text = "Available Plots Report";
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            BackColor = Color.FromArgb(32, 36, 61);

            var panelTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(32, 36, 61)
            };
            var title = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(12, 16),
                Text = "Available Plots Report"
            };
            var close = new Guna.UI2.WinForms.Guna2ControlBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FillColor = Color.FromArgb(139, 152, 166),
                IconColor = Color.White,
                Size = new Size(56, 36)
            };
            panelTop.Controls.Add(title);
            panelTop.Controls.Add(close);
            panelTop.Resize += (s, e) => close.Location = new Point(panelTop.Width - 70, 12);

            var panelFilters = new Panel
            {
                Dock = DockStyle.Top,
                Height = 110,
                Padding = new Padding(12),
                BackColor = Color.FromArgb(32, 36, 61)
            };

            cmbSociety = MakeCombo(12, 30);
            cmbBlock = MakeCombo(250, 30);
            cmbPlotType = MakeCombo(488, 30);
            cmbSociety.SelectedIndexChanged += (s, e) =>
            {
                if (!_binding) OnSocietyChanged();
            };

            btnLoad = new Guna.UI2.WinForms.Guna2Button
            {
                Text = "Show / PDF Preview",
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                FillColor = Color.FromArgb(184, 148, 68),
                ForeColor = Color.White,
                BorderRadius = 8,
                Location = new Point(12, 68),
                Size = new Size(220, 36)
            };
            btnLoad.Click += (s, e) => LoadReport();

            var hint = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(200, 210, 230),
                Font = new Font("Segoe UI", 9F),
                Location = new Point(250, 76),
                Text = "ReportViewer toolbar se PDF export / print"
            };

            panelFilters.Controls.Add(Lbl("Society", 12, 8));
            panelFilters.Controls.Add(cmbSociety);
            panelFilters.Controls.Add(Lbl("Block", 250, 8));
            panelFilters.Controls.Add(cmbBlock);
            panelFilters.Controls.Add(Lbl("Plot Type", 488, 8));
            panelFilters.Controls.Add(cmbPlotType);
            panelFilters.Controls.Add(btnLoad);
            panelFilters.Controls.Add(hint);

            reportViewer1 = new ReportViewer
            {
                Dock = DockStyle.Fill,
                ProcessingMode = ProcessingMode.Local
            };

            Controls.Add(reportViewer1);
            Controls.Add(panelFilters);
            Controls.Add(panelTop);
        }

        private static Label Lbl(string t, int x, int y) => new Label
        {
            Text = t,
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(x, y),
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold)
        };

        private static ComboBox MakeCombo(int x, int y) => new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10F),
            Location = new Point(x, y),
            Size = new Size(220, 28)
        };

        private void LoadSocieties()
        {
            _binding = true;
            cmbSociety.Items.Clear();
            cmbSociety.Items.Add(new IdName(0, "(Select Society)"));
            using (var con = new SQLiteConnection(projectconnection.conReturn()))
            {
                con.Open();
                using (var cmd = new SQLiteCommand(
                    "SELECT id, name FROM societies ORDER BY name COLLATE NOCASE", con))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                        cmbSociety.Items.Add(new IdName(Convert.ToInt32(r["id"]), Convert.ToString(r["name"])));
                }
            }
            cmbSociety.DisplayMember = "Name";
            cmbSociety.SelectedIndex = 0;
            cmbBlock.Items.Clear();
            cmbBlock.Items.Add(new IdName(0, "(All Blocks)"));
            cmbBlock.DisplayMember = "Name";
            cmbBlock.SelectedIndex = 0;
            cmbPlotType.Items.Clear();
            cmbPlotType.Items.Add(new IdName(0, "(All Plot Types)"));
            cmbPlotType.DisplayMember = "Name";
            cmbPlotType.SelectedIndex = 0;
            _binding = false;
        }

        private void OnSocietyChanged()
        {
            _binding = true;
            cmbBlock.Items.Clear();
            cmbBlock.Items.Add(new IdName(0, "(All Blocks)"));
            cmbPlotType.Items.Clear();
            cmbPlotType.Items.Add(new IdName(0, "(All Plot Types)"));
            var soc = cmbSociety.SelectedItem as IdName;
            if (soc != null && soc.Id > 0)
            {
                using (var con = new SQLiteConnection(projectconnection.conReturn()))
                {
                    con.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT id, name FROM blocks WHERE society_id=@s ORDER BY name COLLATE NOCASE", con))
                    {
                        cmd.Parameters.AddWithValue("@s", soc.Id);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                                cmbBlock.Items.Add(new IdName(Convert.ToInt32(r["id"]), Convert.ToString(r["name"])));
                        }
                    }
                    using (var cmd = new SQLiteCommand(
                        "SELECT id, code AS name FROM plot_types WHERE society_id=@s ORDER BY code COLLATE NOCASE", con))
                    {
                        cmd.Parameters.AddWithValue("@s", soc.Id);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                                cmbPlotType.Items.Add(new IdName(Convert.ToInt32(r["id"]), Convert.ToString(r["name"])));
                        }
                    }
                }
            }
            cmbBlock.DisplayMember = "Name";
            cmbBlock.SelectedIndex = 0;
            cmbPlotType.DisplayMember = "Name";
            cmbPlotType.SelectedIndex = 0;
            _binding = false;
        }

        private void LoadReport()
        {
            try
            {
                var soc = cmbSociety.SelectedItem as IdName;
                if (soc == null || soc.Id <= 0)
                {
                    MessageBox.Show("Pehle Society select karein.", "Available Plots",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                var blk = cmbBlock.SelectedItem as IdName;
                var typ = cmbPlotType.SelectedItem as IdName;
                int blockId = blk == null ? 0 : blk.Id;
                int typeId = typ == null ? 0 : typ.Id;

                // Same hygiene as Allocate picker — Active alloc must not stay Available.
                // Case-insensitive repair so Installment/Sold never linger as Available.
                try { MainClass.RepairAllocatedPlotStatuses(); } catch { }
                try { RepairAllocatedStatusesForReport(); } catch { }
                var dt = BuildData(soc.Id, blockId, typeId);
                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("Is filter mein Available plots nahi mile.", "Available Plots",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    reportViewer1.Clear();
                    return;
                }

                string reportPath = Path.Combine(Application.StartupPath, "Reports", "AvailablePlotsReport.rdlc");
                if (!File.Exists(reportPath))
                {
                    MessageBox.Show("Report file not found:\n" + reportPath, "Available Plots",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                reportViewer1.Reset();
                reportViewer1.ProcessingMode = ProcessingMode.Local;
                reportViewer1.LocalReport.ReportPath = reportPath;
                try { ReportHeaderSocietyHelper.Apply(reportViewer1.LocalReport, this); } catch { }
                try
                {
                    reportViewer1.LocalReport.SetParameters(
                        new ReportParameter("HeaderBrand", "Hub Builders"));
                }
                catch { }
                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(new ReportDataSource("DataSet1", dt));
                reportViewer1.RefreshReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Available Plots", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Report-only hardened repair (case-insensitive). Sold → Sold, else Installment/etc → Hold.
        /// Does not touch sync/pull. Cancelled with cancelled_date left alone.
        /// </summary>
        private static void RepairAllocatedStatusesForReport()
        {
            using (var con = new SQLiteConnection(projectconnection.conReturn()))
            {
                con.Open();
                using (var cmd = new SQLiteCommand(@"
UPDATE customer_plots
SET status = 'Active'
WHERE LOWER(TRIM(IFNULL(status,''))) = 'cancelled'
  AND (cancelled_date IS NULL OR TRIM(IFNULL(cancelled_date,'')) = '')
  AND IFNULL(cancellation_id,0) = 0;", con))
                {
                    try { cmd.ExecuteNonQuery(); } catch { }
                }

                using (var cmd = new SQLiteCommand(@"
UPDATE plots
SET status = CASE
  WHEN EXISTS (
    SELECT 1 FROM customer_plots cp
    WHERE cp.plot_id = plots.id
      AND LOWER(TRIM(IFNULL(cp.status,''))) <> 'cancelled'
      AND LOWER(TRIM(IFNULL(cp.deal_type,''))) = 'sold'
  ) THEN 'Sold'
  ELSE 'Hold'
END
WHERE LOWER(TRIM(IFNULL(status,''))) = 'available'
  AND EXISTS (
    SELECT 1 FROM customer_plots cp
    WHERE cp.plot_id = plots.id
      AND LOWER(TRIM(IFNULL(cp.status,''))) <> 'cancelled'
  );", con))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static DataTable BuildData(int societyId, int blockId, int typeId)
        {
            var dt = new DataTable();
            dt.Columns.Add("sn", typeof(int));
            dt.Columns.Add("society", typeof(string));
            dt.Columns.Add("block", typeof(string));
            dt.Columns.Add("plot_type", typeof(string));
            dt.Columns.Add("plot_no", typeof(string));
            dt.Columns.Add("status", typeof(string));

            // At least as strict as Allocate picker (frmAllocatePlotFormAdd.RefreshAvailablePlots):
            // 1) status must be Available (case-insensitive)
            // 2) Hold/Sold never listed
            // 3) any Active customer_plots blocks (Sold/Installment/any deal_type)
            // Cancelled allocations do not block.
            using (var con = new SQLiteConnection(projectconnection.conReturn()))
            using (var cmd = new SQLiteCommand(@"
SELECT s.name AS society, b.name AS block, pt.code AS plot_type, p.plot_no, p.status
FROM plots p
JOIN societies s ON s.id = p.society_id
JOIN blocks b ON b.id = p.block_id
JOIN plot_types pt ON pt.id = p.plot_type_id
WHERE LOWER(TRIM(IFNULL(p.status,''))) = 'available'
  AND LOWER(TRIM(IFNULL(p.status,''))) NOT IN ('hold','sold')
  AND NOT EXISTS (
    SELECT 1 FROM customer_plots cp
    WHERE cp.plot_id = p.id
      AND LOWER(TRIM(IFNULL(cp.status,''))) = 'active'
  )
  AND p.society_id = @sid
  AND (@bid = 0 OR p.block_id = @bid)
  AND (@tid = 0 OR p.plot_type_id = @tid)
ORDER BY p.plot_no;", con))
            {
                cmd.Parameters.AddWithValue("@sid", societyId);
                cmd.Parameters.AddWithValue("@bid", blockId);
                cmd.Parameters.AddWithValue("@tid", typeId);
                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    int sn = 0;
                    while (r.Read())
                    {
                        sn++;
                        dt.Rows.Add(
                            sn,
                            Convert.ToString(r["society"]),
                            Convert.ToString(r["block"]),
                            Convert.ToString(r["plot_type"]),
                            Convert.ToString(r["plot_no"]),
                            Convert.ToString(r["status"]));
                    }
                }
            }
            return dt;
        }

        private sealed class IdName
        {
            public IdName(int id, string name) { Id = id; Name = name; }
            public int Id { get; }
            public string Name { get; }
            public override string ToString() => Name;
        }
    }
}
