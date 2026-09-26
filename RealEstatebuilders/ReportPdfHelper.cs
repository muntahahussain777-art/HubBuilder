using Guna.UI2.WinForms;
using Microsoft.Reporting.WinForms;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace HubBuilders
{
    /// <summary>Existing Print Preview ke saath Save PDF — form layout same.</summary>
    internal static class ReportPdfHelper
    {
        public static void Attach(
            Panel panelButtons,
            Guna2Button btnPreview,
            ReportViewer viewer,
            string fileBase,
            Action runPreview)
        {
            if (panelButtons == null || btnPreview == null || viewer == null) return;
            if (panelButtons.Controls["btnSavePdf"] != null) return;
            var b = new Guna2Button();
            b.Name = "btnSavePdf";
            b.Text = "Save PDF";
            b.BorderRadius = btnPreview.BorderRadius;
            b.FillColor = Color.FromArgb(0, 150, 136);
            b.Font = btnPreview.Font;
            b.ForeColor = Color.White;
            b.Size = new Size(Math.Min(140, btnPreview.Width), btnPreview.Height);
            int x = btnPreview.Right + 8;
            int y = btnPreview.Top;
            if (x + b.Width > panelButtons.Width - 8)
            {
                panelButtons.Height = Math.Max(panelButtons.Height, btnPreview.Bottom + 12 + b.Height);
                x = btnPreview.Left;
                y = btnPreview.Bottom + 6;
            }
            b.Location = new Point(x, y);
            b.Click += (s, e) =>
            {
                try
                {
                    if (runPreview != null) runPreview();
                    string mime, enc, ext;
                    Warning[] warnings;
                    string[] streams;
                    byte[] bytes = viewer.LocalReport.Render(
                        "PDF", null, out mime, out enc, out ext, out streams, out warnings);
                    if (bytes == null || bytes.Length == 0)
                    {
                        MessageBox.Show("PDF ban nahi saki. Pehle Print Preview karein.", "Save PDF",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    using (var dlg = new SaveFileDialog())
                    {
                        dlg.Title = "Save PDF";
                        dlg.Filter = "PDF (*.pdf)|*.pdf";
                        dlg.FileName = fileBase + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf";
                        string today = HubAutoBackup.TodayFolder();
                        if (!string.IsNullOrWhiteSpace(today))
                        {
                            try { Directory.CreateDirectory(today); dlg.InitialDirectory = today; } catch { }
                        }
                        if (dlg.ShowDialog() != DialogResult.OK) return;
                        File.WriteAllBytes(dlg.FileName, bytes);
                        MessageBox.Show("PDF save ho gayi:\n" + dlg.FileName, "Save PDF",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("PDF error: " + ex.Message, "Save PDF",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            panelButtons.Controls.Add(b);
            b.BringToFront();
        }
    }
}
