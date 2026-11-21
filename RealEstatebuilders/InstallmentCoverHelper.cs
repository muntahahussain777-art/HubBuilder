using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace HubBuilders
{
    /// <summary>
    /// Additive receipt-form option: mark previous/current installment months
    /// so WhatsApp reminder can hide them. Never covers upcoming months.
    /// Does not change receipt totals or sync.
    /// Token lives in receipts.notes: [INST-COVER:yyyy-MM,yyyy-MM]
    /// </summary>
    public static class InstallmentCoverHelper
    {
        public const string TokenPrefix = "[INST-COVER:";
        private static readonly Regex TokenRx = new Regex(
            @"\[INST-COVER:([0-9,\-]+)\]",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static bool HasToken(string notes)
        {
            return !string.IsNullOrWhiteSpace(notes) &&
                   notes.IndexOf(TokenPrefix, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static string StripToken(string notes)
        {
            if (string.IsNullOrWhiteSpace(notes)) return "";
            return TokenRx.Replace(notes, "").Trim();
        }

        public static HashSet<string> ParseKeys(string notes)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(notes)) return keys;
            var m = TokenRx.Match(notes);
            if (!m.Success) return keys;
            foreach (var part in m.Groups[1].Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var k = part.Trim();
                if (k.Length == 7 && k[4] == '-') keys.Add(k);
            }
            return keys;
        }

        public static string ApplyToken(string notes, IList<string> keys)
        {
            var baseNotes = StripToken(notes);
            if (keys == null || keys.Count == 0) return baseNotes;
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(baseNotes))
            {
                sb.Append(baseNotes.Trim());
                sb.Append(' ');
            }
            sb.Append(TokenPrefix);
            sb.Append(string.Join(",", keys));
            sb.Append(']');
            return sb.ToString();
        }

        /// <summary>
        /// monthly&gt;0 → floor(amount/monthly) unpaid previous/current only (never upcoming).
        /// Extra slots beyond current dues are unused.
        /// monthly=0 → all unpaid months through asOf (lump-sum WhatsApp clear).
        /// </summary>
        public static List<string> ComputeCoverKeys(
            DateTime booking,
            DateTime asOf,
            double amount,
            double monthly,
            HashSet<string> alreadyPaid)
        {
            var keys = new List<string>();
            try
            {
                var paid = alreadyPaid ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var start = new DateTime(booking.Year, booking.Month, 1).AddMonths(1);
                var asOfMonth = new DateTime(asOf.Year, asOf.Month, 1);
                if (start > asOfMonth) return keys;

                int slots;
                if (monthly > 0.009)
                {
                    slots = (int)Math.Floor((amount / monthly) + 1e-9);
                    if (slots < 1) slots = 1;
                }
                else
                {
                    slots = 240;
                }

                // Stop at current month — upcoming never covered (client rule).
                var cursor = start;
                while (cursor <= asOfMonth && keys.Count < slots)
                {
                    string key = cursor.ToString("yyyy-MM", CultureInfo.InvariantCulture);
                    if (!paid.Contains(key))
                        keys.Add(key);
                    cursor = cursor.AddMonths(1);
                }
            }
            catch { }
            return keys;
        }
    }
}
