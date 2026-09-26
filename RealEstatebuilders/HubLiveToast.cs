using System;
using System.Drawing;
using System.Windows.Forms;

namespace HubBuilders
{
    /// <summary>Non-blocking live notice — existing forms/UI nahi badalta.</summary>
    internal static class HubLiveToast
    {
        public static void Show(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            try
            {
                Form host = null;
                foreach (Form x in Application.OpenForms)
                {
                    if (x is MainForm && !x.IsDisposed) { host = x; break; }
                }
                Action go = delegate { ShowCore(host, message.Trim()); };
                if (host != null && host.IsHandleCreated && host.InvokeRequired)
                    host.BeginInvoke(go);
                else
                    go();
            }
            catch { }
        }

        private static void ShowCore(Form host, string message)
        {
            try
            {
                var t = new Form();
                t.FormBorderStyle = FormBorderStyle.None;
                t.ShowInTaskbar = false;
                t.StartPosition = FormStartPosition.Manual;
                t.BackColor = Color.FromArgb(32, 36, 61);
                t.ForeColor = Color.White;
                t.Size = new Size(360, 52);
                t.TopMost = true;
                t.Padding = new Padding(1);
                var lbl = new Label
                {
                    Text = message,
                    Dock = DockStyle.Fill,
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(32, 36, 61),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(14, 0, 14, 0),
                    Font = new Font("Segoe UI", 10f, FontStyle.Regular)
                };
                t.Controls.Add(lbl);
                Rectangle area = host != null && !host.IsDisposed
                    ? Screen.FromControl(host).WorkingArea
                    : Screen.PrimaryScreen.WorkingArea;
                t.Location = new Point(area.Right - t.Width - 18, area.Top + 18);
                t.Show();
                var tm = new Timer { Interval = 3200 };
                tm.Tick += (s, e) =>
                {
                    try { tm.Stop(); tm.Dispose(); } catch { }
                    try { t.Close(); t.Dispose(); } catch { }
                };
                tm.Start();
            }
            catch { }
        }
    }
}
