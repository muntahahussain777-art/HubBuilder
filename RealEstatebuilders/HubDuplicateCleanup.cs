using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;

namespace HubBuilders
{
    /// <summary>
    /// Sync ne same name do dafa insert kar diye (local + cloud copy).
    /// UI nahi chhedta — extra rows merge karta hai.
    /// </summary>
    internal static class HubDuplicateCleanup
    {
        public static void Run()
        {
            try
            {
                if (MainClass.con.State != ConnectionState.Open)
                    MainClass.con.Open();
                Run(MainClass.con);
            }
            catch { }
        }

        public static void Run(SQLiteConnection con)
        {
            if (con == null) return;
            try
            {
                Exec(con, "PRAGMA foreign_keys=OFF;");
                try { Exec(con, "PRAGMA ignore_check_constraints=ON;"); } catch { }
                DropOverpay(con);
                for (int i = 0; i < 3; i++)
                {
                    MergeSocieties(con);
                    MergeNamed(con, "blocks", "id",
                        "ifnull(society_id,0), lower(trim(ifnull(name,'')))",
                        "ifnull(society_id,0)=@p0 AND lower(trim(ifnull(name,'')))=@p1");
                    MergeNamed(con, "plot_types", "id",
                        "ifnull(society_id,0), lower(trim(ifnull(code,'')))",
                        "ifnull(society_id,0)=@p0 AND lower(trim(ifnull(code,'')))=@p1");
                    MergeNamed(con, "plans", "id",
                        "lower(trim(ifnull(name,'')))",
                        "lower(trim(ifnull(name,'')))=@p0");
                    MergeNamed(con, "customers", "id",
                        "lower(trim(ifnull(name,''))), ifnull(mobile,''), ifnull(cnic,'')",
                        "lower(trim(ifnull(name,'')))=@p0 AND ifnull(mobile,'')=@p1 AND ifnull(cnic,'')=@p2");
                    MergeNamed(con, "brokers", "id",
                        "lower(trim(ifnull(name,''))), ifnull(phone,'')",
                        "lower(trim(ifnull(name,'')))=@p0 AND ifnull(phone,'')=@p1");
                    MergeNamed(con, "Person", "person_id",
                        "lower(trim(ifnull(name,''))), ifnull(is_owner,0), ifnull(is_partner,0)",
                        "lower(trim(ifnull(name,'')))=@p0 AND ifnull(is_owner,0)=@p1 AND ifnull(is_partner,0)=@p2");
                    MergeNamed(con, "plots", "id",
                        "ifnull(society_id,0), ifnull(plot_no,'')",
                        "ifnull(society_id,0)=@p0 AND ifnull(plot_no,'')=@p1");
                    // customer_plots: NEVER content-merge — cancel→reallocate / do devices ke alag sync_id
                    // receipts: NEVER content-merge — same day+amount alag entries ho sakti hain (Partner Cash pattern)
                    // partner_money: NEVER auto-merge/delete — Partner Cash Book max-safe
                    MergeNamed(con, "ledger", "id",
                        "ifnull(customer_plot_id,0), ifnull(txn_type,''), substr(ifnull(txn_date,''),1,10), ifnull(debit,0), ifnull(credit,0)",
                        "ifnull(customer_plot_id,0)=@p0 AND ifnull(txn_type,'')=@p1 AND substr(ifnull(txn_date,''),1,10)=@p2 AND ifnull(debit,0)=@p3 AND ifnull(credit,0)=@p4");
                    MergeNamed(con, "Deal", "deal_id",
                        "ifnull(society_id,0), ifnull(amount,0), substr(ifnull(deal_date,''),1,10)",
                        "ifnull(society_id,0)=@p0 AND ifnull(amount,0)=@p1 AND substr(ifnull(deal_date,''),1,10)=@p2");
                    MergeNamed(con, "Credit", "credit_id",
                        "ifnull(amount,0), substr(ifnull(pay_date,''),1,10), ifnull(note,'')",
                        "ifnull(amount,0)=@p0 AND substr(ifnull(pay_date,''),1,10)=@p1 AND ifnull(note,'')=@p2");
                    MergeNamed(con, "customer_plot_partners", "id",
                        "ifnull(customer_plot_id,0), ifnull(partner_id,0)",
                        "ifnull(customer_plot_id,0)=@p0 AND ifnull(partner_id,0)=@p1");
                    MergeNamed(con, "Expensetbl", "sid",
                        "lower(trim(ifnull(Name,''))), ifnull(Amount,0), substr(ifnull(EDate,''),1,10), ifnull(Category,'')",
                        "lower(trim(ifnull(Name,'')))=@p0 AND ifnull(Amount,0)=@p1 AND substr(ifnull(EDate,''),1,10)=@p2 AND ifnull(Category,'')=@p3");
                }
            }
            catch { }
            finally
            {
                try { HubSyncService.RestoreSyncTriggers(con); } catch { }
                try { Exec(con, "PRAGMA ignore_check_constraints=OFF;"); } catch { }
                try { Exec(con, "PRAGMA foreign_keys=ON;"); } catch { }
            }
        }

        private static void DropOverpay(SQLiteConnection con)
        {
            Exec(con, "DROP TRIGGER IF EXISTS trg_cp_one_active_plot_ins;");
            Exec(con, "DROP TRIGGER IF EXISTS trg_cp_one_active_plot_upd;");
            Exec(con, "DROP TRIGGER IF EXISTS trg_receipts_no_overpay_insert;");
            Exec(con, "DROP TRIGGER IF EXISTS trg_receipts_no_overpay_update;");
        }

        private static void MergeSocieties(SQLiteConnection con)
        {
            MergeNamed(con, "societies", "id",
                "lower(trim(ifnull(name,'')))",
                "lower(trim(ifnull(name,'')))=@p0");
        }

        private static void MergeNamed(SQLiteConnection con, string table, string pk, string groupCols, string whereSql)
        {
            if (!TableExists(con, table) || !HasCol(con, table, pk)) return;
            List<List<object>> groups;
            try
            {
                groups = Query(con, "SELECT " + groupCols + " FROM \"" + table + "\" GROUP BY " + groupCols + " HAVING COUNT(*)>1;");
            }
            catch { return; }
            foreach (var g in groups)
            {
                if (g.Count == 0) continue;
                var ids = QueryLong(con,
                    "SELECT \"" + pk + "\" FROM \"" + table + "\" WHERE " + whereSql
                    + " ORDER BY CASE WHEN ifnull(trim(sync_id),'')='' THEN 1 ELSE 0 END, \"" + pk + "\" ASC;",
                    g);
                if (ids.Count < 2) continue;
                long win = ids[0];
                for (int i = 1; i < ids.Count; i++)
                    MergePair(con, table, pk, win, ids[i]);
            }
        }

        private static void MergePair(SQLiteConnection con, string table, string pk, long winner, long loser)
        {
            if (winner == loser) return;
            CopySync(con, table, pk, winner, loser);
            try
            {
                using (var cmd = new SQLiteCommand(
                    "SELECT sync_id FROM \"" + table + "\" WHERE \"" + pk + "\"=@l LIMIT 1;", con))
                {
                    cmd.Parameters.AddWithValue("@l", loser);
                    string loseSync = Convert.ToString(cmd.ExecuteScalar() ?? "").Trim();
                    if (!string.IsNullOrWhiteSpace(loseSync))
                        HubSyncService.SaveSyncAlias(con, table, loseSync, winner);
                }
            }
            catch { }
            if (Eq(table, "societies"))
            {
                Remap(con, "blocks", "id", "society_id", loser, winner, "ifnull(society_id,0)=@t AND lower(trim(ifnull(name,'')))=(SELECT lower(trim(ifnull(name,''))) FROM blocks WHERE id=@l)");
                Remap(con, "plot_types", "id", "society_id", loser, winner, "ifnull(society_id,0)=@t AND lower(trim(ifnull(code,'')))=(SELECT lower(trim(ifnull(code,''))) FROM plot_types WHERE id=@l)");
                Remap(con, "plots", "id", "society_id", loser, winner, "ifnull(society_id,0)=@t AND ifnull(plot_no,'')=(SELECT ifnull(plot_no,'') FROM plots WHERE id=@l)");
                RemapSimple(con, "Deal", "deal_id", "society_id", loser, winner);
                RemapSimple(con, "partner_money", "id", "society_id", loser, winner);
            }
            else if (Eq(table, "blocks"))
                Remap(con, "plots", "id", "block_id", loser, winner, "ifnull(block_id,0)=@t AND ifnull(plot_no,'')=(SELECT ifnull(plot_no,'') FROM plots WHERE id=@l)");
            else if (Eq(table, "plot_types"))
                RemapSimple(con, "plots", "id", "plot_type_id", loser, winner);
            else if (Eq(table, "plots"))
                Remap(con, "customer_plots", "id", "plot_id", loser, winner, "ifnull(plot_id,0)=@t AND ifnull(customer_id,0)=(SELECT ifnull(customer_id,0) FROM customer_plots WHERE id=@l)");
            else if (Eq(table, "customers"))
                Remap(con, "customer_plots", "id", "customer_id", loser, winner, "ifnull(customer_id,0)=@t AND ifnull(plot_id,0)=(SELECT ifnull(plot_id,0) FROM customer_plots WHERE id=@l)");
            else if (Eq(table, "brokers"))
                RemapSimple(con, "customer_plots", "id", "broker_id", loser, winner);
            else if (Eq(table, "plans"))
                RemapSimple(con, "customer_plots", "id", "plan_id", loser, winner);
            else if (Eq(table, "Person"))
            {
                RemapSimple(con, "Deal", "deal_id", "owner_id", loser, winner);
                RemapSimple(con, "Credit", "credit_id", "partner_id", loser, winner);
                RemapSimple(con, "customer_plot_partners", "id", "partner_id", loser, winner);
                RemapSimple(con, "partner_money", "id", "partner_id", loser, winner);
            }
            else if (Eq(table, "customer_plots"))
            {
                RemapSimple(con, "receipts", "id", "customer_plot_id", loser, winner);
                RemapSimple(con, "ledger", "id", "customer_plot_id", loser, winner);
                RemapSimple(con, "customer_plot_partners", "id", "customer_plot_id", loser, winner);
                RemapSimple(con, "partner_money", "id", "customer_plot_id", loser, winner);
            }
            else if (Eq(table, "ledger"))
                RemapSimple(con, "receipts", "id", "ledger_id", loser, winner);
            else if (Eq(table, "receipts"))
                RemapSimple(con, "partner_money", "id", "receipt_id", loser, winner);
            else if (Eq(table, "Deal"))
                RemapSimple(con, "Credit", "credit_id", "deal_id", loser, winner);

            try
            {
                using (var cmd = new SQLiteCommand("DELETE FROM \"" + table + "\" WHERE \"" + pk + "\"=@l;", con))
                {
                    cmd.Parameters.AddWithValue("@l", loser);
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        private static void CopySync(SQLiteConnection con, string table, string pk, long winner, long loser)
        {
            if (!HasCol(con, table, "sync_id")) return;
            try
            {
                using (var cmd = new SQLiteCommand(@"
UPDATE """ + table + @""" SET sync_id=(SELECT sync_id FROM """ + table + @""" WHERE """ + pk + @"""=@l)
WHERE """ + pk + @"""=@w
  AND ifnull(trim(sync_id),'')=''
  AND ifnull((SELECT trim(ifnull(sync_id,'')) FROM """ + table + @""" WHERE """ + pk + @"""=@l),'')<>'';", con))
                {
                    cmd.Parameters.AddWithValue("@w", winner);
                    cmd.Parameters.AddWithValue("@l", loser);
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        private static void RemapSimple(SQLiteConnection con, string child, string childPk, string col, long from, long to)
        {
            Remap(con, child, childPk, col, from, to, null);
        }

        private static void Remap(SQLiteConnection con, string child, string childPk, string col, long from, long to, string siblingWhere)
        {
            if (!TableExists(con, child) || !HasCol(con, child, col)) return;
            try
            {
                using (var cmd = new SQLiteCommand(
                    "UPDATE \"" + child + "\" SET \"" + col + "\"=@t WHERE \"" + col + "\"=@f;", con))
                {
                    cmd.Parameters.AddWithValue("@t", to);
                    cmd.Parameters.AddWithValue("@f", from);
                    cmd.ExecuteNonQuery();
                    return;
                }
            }
            catch
            {
                try
                {
                    using (var cmd = new SQLiteCommand(
                        "DELETE FROM \"" + child + "\" WHERE \"" + col + "\"=@f AND EXISTS (SELECT 1 FROM \"" + child + "\" x WHERE x.\"" + col + "\"=@t);", con))
                    {
                        cmd.Parameters.AddWithValue("@t", to);
                        cmd.Parameters.AddWithValue("@f", from);
                        cmd.ExecuteNonQuery();
                    }
                    using (var cmd = new SQLiteCommand(
                        "UPDATE \"" + child + "\" SET \"" + col + "\"=@t WHERE \"" + col + "\"=@f;", con))
                    {
                        cmd.Parameters.AddWithValue("@t", to);
                        cmd.Parameters.AddWithValue("@f", from);
                        cmd.ExecuteNonQuery();
                        return;
                    }
                }
                catch { }
            }

            List<long> leftover;
            try
            {
                leftover = QueryLong(con, "SELECT \"" + childPk + "\" FROM \"" + child + "\" WHERE \"" + col + "\"=@p0;",
                    new List<object> { from });
            }
            catch { return; }

            foreach (long lid in leftover)
            {
                long? wid = null;
                if (!string.IsNullOrEmpty(siblingWhere))
                {
                    try
                    {
                        using (var cmd = new SQLiteCommand(
                            "SELECT \"" + childPk + "\" FROM \"" + child + "\" WHERE " + siblingWhere + " AND \"" + childPk + "\"<>@l LIMIT 1;", con))
                        {
                            cmd.Parameters.AddWithValue("@t", to);
                            cmd.Parameters.AddWithValue("@l", lid);
                            object o = cmd.ExecuteScalar();
                            if (o != null && o != DBNull.Value) wid = Convert.ToInt64(o);
                        }
                    }
                    catch { }
                }
                if (wid.HasValue)
                    MergePair(con, child, childPk, wid.Value, lid);
                else
                {
                    try
                    {
                        using (var cmd = new SQLiteCommand(
                            "UPDATE \"" + child + "\" SET \"" + col + "\"=@t WHERE \"" + childPk + "\"=@l;", con))
                        {
                            cmd.Parameters.AddWithValue("@t", to);
                            cmd.Parameters.AddWithValue("@l", lid);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { }
                }
            }
        }

        private static bool Eq(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TableExists(SQLiteConnection con, string table)
        {
            try
            {
                using (var cmd = new SQLiteCommand(
                    "SELECT 1 FROM sqlite_master WHERE type='table' AND name=@n LIMIT 1;", con))
                {
                    cmd.Parameters.AddWithValue("@n", table);
                    return cmd.ExecuteScalar() != null;
                }
            }
            catch { return false; }
        }

        private static bool HasCol(SQLiteConnection con, string table, string col)
        {
            try
            {
                using (var cmd = new SQLiteCommand("PRAGMA table_info(\"" + table + "\");", con))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        if (string.Equals(Convert.ToString(r["name"]), col, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }
            }
            catch { }
            return false;
        }

        private static void Exec(SQLiteConnection con, string sql)
        {
            try
            {
                using (var cmd = new SQLiteCommand(sql, con))
                    cmd.ExecuteNonQuery();
            }
            catch { }
        }

        private static List<List<object>> Query(SQLiteConnection con, string sql)
        {
            var rows = new List<List<object>>();
            using (var cmd = new SQLiteCommand(sql, con))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    var row = new List<object>();
                    for (int i = 0; i < r.FieldCount; i++)
                        row.Add(r.IsDBNull(i) ? "" : r.GetValue(i));
                    rows.Add(row);
                }
            }
            return rows;
        }

        private static List<long> QueryLong(SQLiteConnection con, string sql, List<object> pars)
        {
            var ids = new List<long>();
            using (var cmd = new SQLiteCommand(sql, con))
            {
                if (pars != null)
                {
                    for (int i = 0; i < pars.Count; i++)
                        cmd.Parameters.AddWithValue("@p" + i, pars[i] ?? "");
                }
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        if (r.IsDBNull(0)) continue;
                        ids.Add(Convert.ToInt64(r.GetValue(0)));
                    }
                }
            }
            return ids;
        }
    }
}
