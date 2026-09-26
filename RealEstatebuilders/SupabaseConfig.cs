using System;
using System.IO;
using System.Web.Script.Serialization;

namespace HubBuilders
{
    /// <summary>Supabase HubBuilders project settings (exe folder or AppData).</summary>
    public static class SupabaseConfig
    {
        public static string Url { get; private set; }
        public static string AnonKey { get; private set; }
        public static bool IsConfigured
        {
            get
            {
                return !string.IsNullOrWhiteSpace(Url)
                    && Url.IndexOf("YOUR-PROJECT", StringComparison.OrdinalIgnoreCase) < 0
                    && !string.IsNullOrWhiteSpace(AnonKey)
                    && AnonKey.IndexOf("YOUR-ANON-KEY", StringComparison.OrdinalIgnoreCase) < 0;
            }
        }

        public static void Load()
        {
            try
            {
                Url = Environment.GetEnvironmentVariable("HUBBUILDERS_SUPABASE_URL") ?? "";
                AnonKey = Environment.GetEnvironmentVariable("HUBBUILDERS_SUPABASE_ANON_KEY") ?? "";
                if (IsConfigured) return;

                foreach (string path in CandidatePaths())
                {
                    if (!File.Exists(path)) continue;
                    string json = File.ReadAllText(path);
                    var ser = new JavaScriptSerializer();
                    var map = ser.Deserialize<System.Collections.Generic.Dictionary<string, object>>(json);
                    if (map == null) continue;
                    object u, k;
                    if (map.TryGetValue("url", out u)) Url = Convert.ToString(u) ?? "";
                    if (map.TryGetValue("anonKey", out k)) AnonKey = Convert.ToString(k) ?? "";
                    if (IsConfigured) return;
                }
            }
            catch { }
        }

        private static string[] CandidatePaths()
        {
            string exe = AppDomain.CurrentDomain.BaseDirectory ?? "";
            string app = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "HubBuilders", "supabase_config.json");
            return new[]
            {
                Path.Combine(exe, "supabase_config.json"),
                Path.Combine(exe, "supabase", "supabase_config.json"),
                app
            };
        }
    }
}
