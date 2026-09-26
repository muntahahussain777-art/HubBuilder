using HubBuilders.ProjectConnection;
using System;
using System.Data.SQLite;
using System.Globalization;

namespace HubBuilders.Report_Form
{
    /// <summary>
    /// Lasbela Village / Market Bhawani رسید → same receipts + ledger as Receipt Form.
    /// No UI. Preview/PDF pe save; same amount dobara preview = update (duplicate nahi).
    /// </summary>
    internal static class LasbelaReceiptToCustomerReceipt
    {
        public static string MapMode(string raw)
        {
            string s = (raw ?? "").Trim();
            if (s.Length == 0) return "Cash";
            if (s.Equals("Cash", StringComparison.OrdinalIgnoreCase)) return "Cash";
            if (s.IndexOf("bank", StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("cheque", StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("check", StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("draft", StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("pay order", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Bank";
            return "Cash";
        }

        public static string ParseDbDate(string raw)
        {
            string t = (raw ?? "").Trim();
            DateTime dt;
            string[] fmts = { "dd-MM-yyyy", "d-M-yyyy", "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy" };
            if (DateTime.TryParseExact(t, fmts, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                return dt.ToString("yyyy-MM-dd");
            if (DateTime.TryParse(t, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                return dt.ToString("yyyy-MM-dd");
            return DateTime.Today.ToString("yyyy-MM-dd");
        }

        public static double ParseAmount(string raw)
        {
            string t = (raw ?? "").Trim().Replace(",", "");
            double v;
            if (!double.TryParse(t, NumberStyles.Any, CultureInfo.InvariantCulture, out v))
                double.TryParse(t, NumberStyles.Any, CultureInfo.CurrentCulture, out v);
            if (v < 0) v = 0;
            return Math.Round(v, 2);
        }

        /// <summary>
        /// Amount &gt; 0 + customer select → receipts table. Fail pe preview mat chalao.
        /// Amount empty → true (sirf print).
        /// </summary>
        public static bool TrySave(
            int? customerPlotId,
            string amountText,
            string dateText,
            string modeText,
            string refNo,
            string notes,
            ref long? savedReceiptId,
            ref long? savedLedgerId,
            out string error)
        {
            error = null;
            double amount = ParseAmount(amountText);
            if (amount <= 0) return true;

            if (!customerPlotId.HasValue || customerPlotId.Value <= 0)
                return true;

            int cpId = customerPlotId.Value;
            string receiptDate = ParseDbDate(dateText);
            string mode = MapMode(modeText);
            string refClean = (refNo ?? "").Trim();
            if (refClean.Length == 0) refClean = null;
            string notesClean = (notes ?? "").Trim();
            if (notesClean.Length == 0) notesClean = null;

            double remaining = GetRemaining(cpId, savedReceiptId);
            if (remaining <= 0)
            {
                error = "Is allocation ka debit settle ho chuka hai. New receipt add nahi ho sakti.";
                return false;
            }
            if (amount > remaining)
            {
                error = "Amount remaining se zyada hai.\nRemaining: "
                    + remaining.ToString("N2") + "\nPlease enter equal or less.";
                return false;
            }

            bool isUpdate = savedReceiptId.HasValue && savedReceiptId.Value > 0;
            SQLiteTransaction tx = null;
            try
            {
                try { MainClass.EnsurePartnerMoneySchema(); } catch { }
                MainClass.AcquireSharedConnection();
                using (var pragma = new SQLiteCommand("PRAGMA foreign_keys = ON;", MainClass.con))
                    pragma.ExecuteNonQuery();
                tx = MainClass.con.BeginTransaction();

                int partnerId = 0;
                using (var q = new SQLiteCommand(
                    "SELECT partner_id FROM customer_plot_partners WHERE customer_plot_id=@cp;", MainClass.con, tx))
                {
                    q.Parameters.AddWithValue("@cp", cpId);
                    object po = q.ExecuteScalar();
                    if (po != null && po != DBNull.Value) partnerId = Convert.ToInt32(po);
                }

                if (isUpdate)
                {
                    if (!savedLedgerId.HasValue || savedLedgerId.Value <= 0)
                    {
                        using (var q = new SQLiteCommand(
                            "SELECT ledger_id FROM receipts WHERE id=@rid;", MainClass.con, tx))
                        {
                            q.Parameters.AddWithValue("@rid", savedReceiptId.Value);
                            object o = q.ExecuteScalar();
                            if (o != null && o != DBNull.Value) savedLedgerId = Convert.ToInt64(o);
                        }
                    }
                    if (savedLedgerId.HasValue && savedLedgerId.Value > 0)
                    {
                        using (var cmd = new SQLiteCommand(@"
UPDATE ledger
SET txn_date=@date, credit=@amt, txn_type=@tt, narration=@nar
WHERE id=@lid;", MainClass.con, tx))
                        {
                            cmd.Parameters.AddWithValue("@date", receiptDate);
                            cmd.Parameters.AddWithValue("@amt", amount);
                            cmd.Parameters.AddWithValue("@tt", mode == "Bank" ? "ReceiptBank" : "ReceiptCash");
                            cmd.Parameters.AddWithValue("@nar", (object)(notesClean ?? ("Receipt (" + mode + ")")));
                            cmd.Parameters.AddWithValue("@lid", savedLedgerId.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    using (var cmd = new SQLiteCommand(@"
UPDATE receipts
SET customer_plot_id=@cpid, receipt_date=@date, amount=@amt, mode=@mode, ref_no=@ref, notes=@notes
WHERE id=@rid;", MainClass.con, tx))
                    {
                        cmd.Parameters.AddWithValue("@cpid", cpId);
                        cmd.Parameters.AddWithValue("@date", receiptDate);
                        cmd.Parameters.AddWithValue("@amt", amount);
                        cmd.Parameters.AddWithValue("@mode", mode);
                        cmd.Parameters.AddWithValue("@ref", (object)(refClean ?? (object)DBNull.Value));
                        cmd.Parameters.AddWithValue("@notes", (object)(notesClean ?? (object)DBNull.Value));
                        cmd.Parameters.AddWithValue("@rid", savedReceiptId.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    using (var cmd = new SQLiteCommand(@"
INSERT INTO ledger (customer_plot_id, txn_date, voucher_no, narration, debit, credit, txn_type)
VALUES (@cpid, @date, @vno, @nar, 0, @amt, @tt);", MainClass.con, tx))
                    {
                        cmd.Parameters.AddWithValue("@cpid", cpId);
                        cmd.Parameters.AddWithValue("@date", receiptDate);
                        cmd.Parameters.AddWithValue("@vno", DBNull.Value);
                        cmd.Parameters.AddWithValue("@nar", (object)(notesClean ?? ("Receipt (" + mode + ")")));
                        cmd.Parameters.AddWithValue("@amt", amount);
                        cmd.Parameters.AddWithValue("@tt", mode == "Bank" ? "ReceiptBank" : "ReceiptCash");
                        cmd.ExecuteNonQuery();
                    }
                    using (var idCmd = new SQLiteCommand("SELECT last_insert_rowid();", MainClass.con, tx))
                        savedLedgerId = Convert.ToInt64(idCmd.ExecuteScalar());

                    using (var cmd = new SQLiteCommand(@"
INSERT INTO receipts (customer_plot_id, receipt_date, amount, mode, ref_no, notes, ledger_id)
VALUES (@cpid, @date, @amt, @mode, @ref, @notes, @lid);", MainClass.con, tx))
                    {
                        cmd.Parameters.AddWithValue("@cpid", cpId);
                        cmd.Parameters.AddWithValue("@date", receiptDate);
                        cmd.Parameters.AddWithValue("@amt", amount);
                        cmd.Parameters.AddWithValue("@mode", mode);
                        cmd.Parameters.AddWithValue("@ref", (object)(refClean ?? (object)DBNull.Value));
                        cmd.Parameters.AddWithValue("@notes", (object)(notesClean ?? (object)DBNull.Value));
                        cmd.Parameters.AddWithValue("@lid", savedLedgerId.Value);
                        cmd.ExecuteNonQuery();
                    }
                    using (var idCmd = new SQLiteCommand("SELECT last_insert_rowid();", MainClass.con, tx))
                        savedReceiptId = Convert.ToInt64(idCmd.ExecuteScalar());
                }

                if (savedReceiptId.HasValue && savedReceiptId.Value > 0)
                {
                    MainClass.UpsertPartnerMoneyForReceipt(
                        savedReceiptId.Value,
                        partnerId,
                        cpId,
                        receiptDate,
                        (decimal)amount,
                        mode,
                        notesClean,
                        MainClass.con,
                        tx);
                }

                tx.Commit();
            }
            catch (Exception ex)
            {
                try { if (tx != null) tx.Rollback(); } catch { }
                error = "Receipt Form mein save nahi hua: " + ex.Message;
                if (!isUpdate)
                {
                    savedReceiptId = null;
                    savedLedgerId = null;
                }
                return false;
            }
            finally
            {
                MainClass.ReleaseSharedConnection();
            }

            try
            {
                MainClass.LogStaffEntry(
                    isUpdate ? "Update" : "Insert",
                    "receipts",
                    "Lasbela receipt amount=" + amount.ToString("0.##") + "; cpid=" + cpId);
            }
            catch { }
            try { HubBuilders.HubSyncService.NotifyLocalWrite(); } catch { }
            return true;
        }

        private static double GetRemaining(int cpId, long? excludeReceiptId)
        {
            try
            {
                using (var con = new SQLiteConnection(projectconnection.conReturn()))
                using (var cmd = new SQLiteCommand(@"
SELECT
  COALESCE(cp.price,0)
  - COALESCE((
        SELECT SUM(r.amount) FROM receipts r
        WHERE r.customer_plot_id = cp.id
          AND (@ex = 0 OR r.id <> @ex)
    ),0) AS rem
FROM customer_plots cp
WHERE cp.id = @cp;", con))
                {
                    cmd.Parameters.AddWithValue("@cp", cpId);
                    cmd.Parameters.AddWithValue("@ex", excludeReceiptId.HasValue ? excludeReceiptId.Value : 0L);
                    con.Open();
                    object o = cmd.ExecuteScalar();
                    double rem = 0;
                    if (o != null && o != DBNull.Value) rem = Convert.ToDouble(o);
                    if (rem < 0) rem = 0;
                    return Math.Round(rem, 2);
                }
            }
            catch { return 0; }
        }
    }
}
