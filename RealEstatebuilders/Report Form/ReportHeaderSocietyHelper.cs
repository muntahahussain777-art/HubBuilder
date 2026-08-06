using HubBuilders.ProjectConnection;
using Microsoft.Reporting.WinForms;
using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;

namespace HubBuilders.Report_Form
{
    /// <summary>
    /// Shared VIP society header for standard RDLC reports.
    /// Select society → header shows society name; empty → Hub Builders.
    /// Does not alter Application / Blank / Temporary / Lasbela receipt reports.
    /// </summary>
    public static class ReportHeaderSocietyHelper
    {
        public const string ComboName = "cmbReportHeaderSociety";
        public const string LabelName = "lblReportHeaderSociety";
        public const string DefaultBrand = "Hub Builders";

        public static void Attach(Form form)
        {
            if (form == null) return;
            if (form.Controls.Find(ComboName, true).Length > 0) return;

            // Prefer top title panel so filters/search stay untouched
            Control host = form;
            var panels = form.Controls.Find("panel1", false);
            if (panels.Length > 0) host = panels[0];

            // Right side of title bar (left of window control boxes) — no filter overlap
            int comboWidth = 250;
            int comboX = Math.Max(220, host.ClientSize.Width - 380);

            var lbl = new Label
            {
                Name = LabelName,
                Text = "Header Society",
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Location = new Point(comboX, 4),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            var cmb = new ComboBox
            {
                Name = ComboName,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f),
                Location = new Point(comboX, 24),
                Size = new Size(comboWidth, 24),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            host.Controls.Add(lbl);
            host.Controls.Add(cmb);
            lbl.BringToFront();
            cmb.BringToFront();

            LoadSocieties(cmb);

            cmb.SelectedIndexChanged += (s, e) =>
            {
                try
                {
                    var viewers = form.Controls.Find("reportViewer1", true);
                    if (viewers.Length == 0) return;
                    var rv = viewers[0] as ReportViewer;
                    if (rv == null || string.IsNullOrWhiteSpace(rv.LocalReport.ReportPath)) return;
                    Apply(rv.LocalReport, form);
                    rv.RefreshReport();
                }
                catch
                {
                    // ignore until report is bound
                }
            };
        }

        public static void LoadSocieties(ComboBox cmb)
        {
            if (cmb == null) return;
            var dt = new DataTable();
            dt.Columns.Add("id", typeof(int));
            dt.Columns.Add("name", typeof(string));
            dt.Rows.Add(0, "(Hub Builders — default)");

            try
            {
                using (var con = new SQLiteConnection(projectconnection.conReturn()))
                using (var cmd = new SQLiteCommand("SELECT id, name FROM societies ORDER BY name;", con))
                {
                    con.Open();
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                            dt.Rows.Add(Convert.ToInt32(r["id"]), Convert.ToString(r["name"]));
                    }
                }
            }
            catch
            {
                // keep default row only
            }

            cmb.DisplayMember = "name";
            cmb.ValueMember = "id";
            cmb.DataSource = dt;
            if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;
        }

        public static string GetBrand(Form form)
        {
            try
            {
                var found = form.Controls.Find(ComboName, true);
                if (found.Length == 0) return DefaultBrand;
                var cmb = found[0] as ComboBox;
                if (cmb == null || cmb.SelectedValue == null) return DefaultBrand;
                int id = Convert.ToInt32(cmb.SelectedValue);
                if (id <= 0) return DefaultBrand;
                string name = Convert.ToString(cmb.Text);
                return string.IsNullOrWhiteSpace(name) ? DefaultBrand : name.Trim();
            }
            catch
            {
                return DefaultBrand;
            }
        }

        /// <summary>Call after ReportPath is set, before RefreshReport.</summary>
        public static void Apply(LocalReport report, Form form)
        {
            if (report == null) return;
            try
            {
                string brand = GetBrand(form);
                report.SetParameters(new ReportParameter("HeaderBrand", brand));
            }
            catch
            {
                // Report may not declare HeaderBrand yet, or path not loaded — ignore safely
            }
        }
    }
}
