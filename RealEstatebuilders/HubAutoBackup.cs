using System;
using System.IO;
using System.Data.SQLite;
using System.Threading;
using System.Windows.Forms;

namespace HubBuilders
{
    /// <summary>
    /// Auto DB backup: first-time drive select, then HubBuilders\{yyyy-MM-dd}\.
    /// Interval: entry / 3h / 1d. Existing Backup form UI ko replace nahi karta.
    /// </summary>
    internal static class HubAutoBackup
    {
        private static readonly string CfgPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RealEstateBuilders", "hub_auto_backup.txt");
        private static System.Windows.Forms.Timer _timer;
        private static int _busy;

        public static string Root
        {
            get { return Read("root"); }
            set { Write("root", value ?? ""); }
        }

        /// <summary>entry | 3h | 1d</summary>
        public static string Interval
        {
            get
            {
                string v = Read("interval");
                if (v == "entry" || v == "3h" || v == "1d") return v;
                return "1d";
            }
            set { Write("interval", value ?? "1d"); }
        }

        public static DateTime LastUtc
        {
            get
            {
                DateTime d;
                if (DateTime.TryParse(Read("lastutc"), null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out d))
                    return d.ToUniversalTime();
                return DateTime.MinValue;
            }
            set { Write("lastutc", value.ToUniversalTime().ToString("o")); }
        }

        public static string TodayFolder()
        {
            string root = Root;
            if (string.IsNullOrWhiteSpace(root)) return "";
            string date = DateTime.Now.ToString("yyyy-MM-dd");
            return Path.Combine(root.TrimEnd('\\', '/'), "HubBuilders", date);
        }

        /// <summary>After each entry: date\HHmmss folder. 3h/1d: date folder.</summary>
        public static string BackupTargetFolder()
        {
            string today = TodayFolder();
            if (string.IsNullOrWhiteSpace(today)) return "";
            if (Interval == "entry")
                return Path.Combine(today, DateTime.Now.ToString("HHmmss"));
            return today;
        }

        public static void Start(Form host)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Root))
                    PromptDrive(host);
                if (_timer != null) return;
                _timer = new System.Windows.Forms.Timer { Interval = 60000 };
                _timer.Tick += (s, e) => Tick();
                _timer.Start();
                ThreadPool.QueueUserWorkItem(_ => { try { Tick(); } catch { } });
            }
            catch { }
        }

        public static void PromptDrive(IWin32Window owner)
        {
            try
            {
                using (var dlg = new FolderBrowserDialog())
                {
                    dlg.Description = "HubBuilders auto-backup ke liye drive / folder select karein (first time).";
                    dlg.ShowNewFolderButton = true;
                    if (dlg.ShowDialog(owner) != DialogResult.OK) return;
                    string picked = dlg.SelectedPath;
                    if (string.IsNullOrWhiteSpace(picked)) return;
                    string drive = Path.GetPathRoot(picked);
                    Root = string.IsNullOrWhiteSpace(drive) ? picked : drive;
                    Directory.CreateDirectory(TodayFolder());
                    RunSilent();
                }
            }
            catch { }
        }

        public static void OnLocalWrite()
        {
            if (Interval != "entry") return;
            ThreadPool.QueueUserWorkItem(_ => { try { RunSilent(); } catch { } });
        }

        public static void Tick()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Root)) return;
                string iv = Interval;
                if (iv == "entry") return;
                double hours = iv == "3h" ? 3 : 24;
                if ((DateTime.UtcNow - LastUtc).TotalHours < hours) return;
                RunSilent();
            }
            catch { }
        }

        public static string RunSilent()
        {
            if (Interlocked.Exchange(ref _busy, 1) == 1) return "";
            try
            {
                string folder = BackupTargetFolder();
                if (string.IsNullOrWhiteSpace(folder)) return "";
                Directory.CreateDirectory(folder);
                string dest = Path.Combine(folder, "RealEstateBuilders_backup.db");
                string src = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RealEstateBuilders", "RealEstateBuilders.db");
                if (!File.Exists(src)) return "";
                using (var source = new SQLiteConnection("Data Source=" + src + ";Version=3;Busy Timeout=8000;"))
                using (var backup = new SQLiteConnection("Data Source=" + dest + ";Version=3;"))
                {
                    source.Open();
                    backup.Open();
                    source.BackupDatabase(backup, "main", "main", -1, null, 0);
                }
                LastUtc = DateTime.UtcNow;
                return dest;
            }
            catch { return ""; }
            finally { Interlocked.Exchange(ref _busy, 0); }
        }

        private static string Read(string key)
        {
            try
            {
                if (!File.Exists(CfgPath)) return "";
                foreach (string line in File.ReadAllLines(CfgPath))
                {
                    int i = line.IndexOf('=');
                    if (i <= 0) continue;
                    if (string.Equals(line.Substring(0, i).Trim(), key, StringComparison.OrdinalIgnoreCase))
                        return line.Substring(i + 1).Trim();
                }
            }
            catch { }
            return "";
        }

        private static void Write(string key, string value)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CfgPath));
                var map = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (File.Exists(CfgPath))
                {
                    foreach (string line in File.ReadAllLines(CfgPath))
                    {
                        int i = line.IndexOf('=');
                        if (i <= 0) continue;
                        map[line.Substring(0, i).Trim()] = line.Substring(i + 1).Trim();
                    }
                }
                map[key] = value ?? "";
                using (var sw = new StreamWriter(CfgPath, false))
                {
                    foreach (var kv in map)
                        sw.WriteLine(kv.Key + "=" + kv.Value);
                }
            }
            catch { }
        }
    }
}
