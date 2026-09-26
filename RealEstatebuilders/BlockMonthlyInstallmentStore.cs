using System;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;

namespace HubBuilders
{
    /// <summary>
    /// VIP/Admin: default monthly installment per society block.
    /// Additive table block_monthly_installments — syncs like other catalog tables.
    /// </summary>
    internal static class BlockMonthlyInstallmentStore
    {
        public static void EnsureSchema()
        {
            try
            {
                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    using (var cmd = new SQLiteCommand(@"
CREATE TABLE IF NOT EXISTS block_monthly_installments (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  block_id INTEGER NOT NULL UNIQUE,
  monthly_amount REAL NOT NULL DEFAULT 0,
  sync_id TEXT
);", con))
                        cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        public static DataTable LoadSocieties()
        {
            try
            {
                return MainClass.GetData("SELECT id, name FROM societies ORDER BY name;") ?? new DataTable();
            }
            catch { return new DataTable(); }
        }

        /// <summary>Grid: block_id, Block, Monthly — one row per block in society.</summary>
        public static DataTable LoadGrid(int societyId)
        {
            EnsureSchema();
            var dt = new DataTable();
            dt.Columns.Add("block_id", typeof(int));
            dt.Columns.Add("Block", typeof(string));
            dt.Columns.Add("Monthly", typeof(decimal));
            if (societyId <= 0) return dt;
            try
            {
                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    using (var cmd = new SQLiteCommand(@"
SELECT b.id AS block_id, b.name AS Block,
  COALESCE(bmi.monthly_amount, 0) AS Monthly
FROM blocks b
LEFT JOIN block_monthly_installments bmi ON bmi.block_id = b.id
WHERE b.society_id = @sid
ORDER BY b.name;", con))
                    {
                        cmd.Parameters.AddWithValue("@sid", societyId);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                var nr = dt.NewRow();
                                nr["block_id"] = Convert.ToInt32(r["block_id"]);
                                nr["Block"] = Convert.ToString(r["Block"]);
                                decimal m = 0m;
                                try { m = Convert.ToDecimal(r["Monthly"]); } catch { m = 0m; }
                                if (m < 0) m = 0m;
                                nr["Monthly"] = m;
                                dt.Rows.Add(nr);
                            }
                        }
                    }
                }
            }
            catch { }
            return dt;
        }

        public static void SaveGrid(DataTable grid)
        {
            if (grid == null) return;
            EnsureSchema();
            using (var con = new SQLiteConnection(MainClass.con_string))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    foreach (DataRow row in grid.Rows)
                    {
                        int blockId;
                        try { blockId = Convert.ToInt32(row["block_id"]); }
                        catch { continue; }
                        if (blockId <= 0) continue;
                        decimal amount = 0m;
                        try { amount = Convert.ToDecimal(row["Monthly"]); } catch { amount = 0m; }
                        if (amount < 0) amount = 0m;
                        using (var cmd = new SQLiteCommand(@"
INSERT INTO block_monthly_installments(block_id, monthly_amount, sync_id)
VALUES(@b, @m, lower(hex(randomblob(16))))
ON CONFLICT(block_id) DO UPDATE SET
  monthly_amount = excluded.monthly_amount;", con, tx))
                        {
                            cmd.Parameters.AddWithValue("@b", blockId);
                            cmd.Parameters.AddWithValue("@m", (double)amount);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    tx.Commit();
                }
            }
            try { ApplyToEmptyActive(); } catch { }
            try { HubSyncService.MarkDirty(); } catch { }
        }

        public static double GetAmountForBlock(int blockId)
        {
            if (blockId <= 0) return 0;
            EnsureSchema();
            try
            {
                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT COALESCE(monthly_amount,0) FROM block_monthly_installments WHERE block_id=@b LIMIT 1;", con))
                    {
                        cmd.Parameters.AddWithValue("@b", blockId);
                        object v = cmd.ExecuteScalar();
                        if (v == null || v == DBNull.Value) return 0;
                        double d = Convert.ToDouble(v);
                        return d > 0 ? d : 0;
                    }
                }
            }
            catch { return 0; }
        }

        /// <summary>Upsert one block default (Block Form + VIP grid stay consistent).</summary>
        public static void UpsertForBlock(int blockId, double monthlyAmount)
        {
            if (blockId <= 0) return;
            EnsureSchema();
            if (monthlyAmount < 0) monthlyAmount = 0;
            using (var con = new SQLiteConnection(MainClass.con_string))
            {
                con.Open();
                using (var cmd = new SQLiteCommand(@"
INSERT INTO block_monthly_installments(block_id, monthly_amount, sync_id)
VALUES(@b, @m, lower(hex(randomblob(16))))
ON CONFLICT(block_id) DO UPDATE SET
  monthly_amount = excluded.monthly_amount;", con))
                {
                    cmd.Parameters.AddWithValue("@b", blockId);
                    cmd.Parameters.AddWithValue("@m", monthlyAmount);
                    cmd.ExecuteNonQuery();
                }
            }
            try { HubSyncService.MarkDirty(); } catch { }
            try { ApplyToEmptyActive(); } catch { }
        }

        /// <summary>Active allocations with empty monthly → block default. Returns rows updated.</summary>
        public static int ApplyToEmptyActive()
        {
            EnsureSchema();
            int n = 0;
            using (var con = new SQLiteConnection(MainClass.con_string))
            {
                con.Open();
                using (var cmd = new SQLiteCommand(@"
UPDATE customer_plots
SET monthly_installment = (
  SELECT bmi.monthly_amount
  FROM plots p
  JOIN block_monthly_installments bmi ON bmi.block_id = p.block_id
  WHERE p.id = customer_plots.plot_id
    AND IFNULL(bmi.monthly_amount, 0) > 0
  LIMIT 1
)
WHERE IFNULL(status,'') = 'Active'
  AND IFNULL(monthly_installment, 0) <= 0
  AND EXISTS (
    SELECT 1
    FROM plots p
    JOIN block_monthly_installments bmi ON bmi.block_id = p.block_id
    WHERE p.id = customer_plots.plot_id
      AND IFNULL(bmi.monthly_amount, 0) > 0
  );", con))
                {
                    n = cmd.ExecuteNonQuery();
                }
            }
            if (n > 0)
            {
                try { HubSyncService.MarkDirty(); } catch { }
            }
            return n;
        }
    }
}
