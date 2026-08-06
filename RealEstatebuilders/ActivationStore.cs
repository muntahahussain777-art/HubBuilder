using System;
using System.IO;
using System.Text;

namespace HubBuilders
{
    public static class ActivationStore
    {
        // Primary: ProgramData (machine-wide). Fallback: LocalAppData (per-user).
        private static readonly string DirPrimary =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                         "RealEstatebuilders", "ReceiptSuite");
        private static readonly string DirFallback =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "RealEstatebuilders", "ReceiptSuite");

        private const string FileName = "act.bin";
        private const string Magic = "ACTv1";

        private static bool? _cached; // process-level cache

        private static string PrimaryPath => Path.Combine(DirPrimary, FileName);
        private static string FallbackPath => Path.Combine(DirFallback, FileName);

        public static bool IsActivated()
        {
            if (_cached.HasValue) return _cached.Value;

            // 1) primary
            if (TryReadFlag(PrimaryPath, out var ok) && ok) { _cached = true; return true; }
            // 2) fallback
            if (TryReadFlag(FallbackPath, out ok) && ok) { _cached = true; return true; }

            _cached = false;
            return false;
        }

        public static void SaveActivated(string acceptedPlainCode)
        {
            // pehle primary par koshish
            if (TryWriteFlag(PrimaryPath, acceptedPlainCode)) { _cached = true; return; }
            // warna fallback
            if (TryWriteFlag(FallbackPath, acceptedPlainCode)) { _cached = true; return; }

            // dono fail ho jayein to runtime me prompt dubara aa sakta hai
            _cached = false;
        }

        public static void Clear()
        {
            SafeDelete(PrimaryPath);
            SafeDelete(FallbackPath);
            _cached = false;
        }

        // ---- helpers ----
        private static bool TryReadFlag(string path, out bool ok)
        {
            ok = false;
            try
            {
                if (!File.Exists(path)) return true;
                var txt = File.ReadAllText(path, Encoding.UTF8).Trim();
                var parts = txt.Split('|');
                ok = (parts.Length == 2 && parts[0] == Magic && !string.IsNullOrWhiteSpace(parts[1]));
                return true;
            }
            catch { return false; }
        }

        private static bool TryWriteFlag(string path, string code)
        {
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var payload = Magic + "|" + code;
                // atomic-ish write
                var tmp = path + ".tmp";
                File.WriteAllText(tmp, payload, Encoding.UTF8);
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
                return true;
            }
            catch { return false; }
        }

        private static void SafeDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }
    }
}
