using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace HubBuilders
{
    public static class ActivationCodes
    {
        // APNE 10 PLAINTEXT CODES YAHAN RAKHO (sirf test ke liye 3 de raha hoon)
        private static readonly string[] AllowedPlainCodes = new[]
        {
            "RB-7J9Q-2V6N",
            "TEST-123",
            "AA-11-XX",
        };

        private static readonly Regex NonAlphaNum = new Regex(@"[^A-Z0-9]", RegexOptions.Compiled);

        private static string Normalize(string s)
        {
            s = (s ?? "").Trim().ToUpperInvariant();
            return NonAlphaNum.Replace(s, ""); // sirf A-Z0-9
        }

        // DEBUG helper: dekhne ke liye ke list me asal me kya compare ho raha
        public static string[] DumpNormalizedAllowed()
        {
            return AllowedPlainCodes.Select(Normalize).ToArray();
        }

        public static bool IsValidPlainCode(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;

            var norm = Normalize(input);
            var list = DumpNormalizedAllowed();

            // TEMP DEBUG POPUP: jo compare ho raha wahi dikhao
            System.Windows.Forms.MessageBox.Show(
                "NORM=" + norm + "\r\nALLOWED=" + string.Join(",", list),
                "Activation Debug");

            return list.Contains(norm, StringComparer.Ordinal);
        }
    }
}
