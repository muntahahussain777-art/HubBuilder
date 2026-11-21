using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace HubBuilders
{
    /// <summary>
    /// Incremental HubBuilders ↔ Supabase sync. Sirf naya/changed data.
    /// Duplicate: Yes/No. 7-day pause NAHI.
    /// </summary>
    public static class HubSyncService
    {
        private static readonly string[] Tables =
        {
            "societies", "blocks", "plot_types", "plots", "plans",
            "customers", "brokers", "users", "customer_plots",
            "receipts", "ledger", "Expensetbl", "Person", "Deal", "Credit",
            "customer_plot_partners", "partner_money", "staff_entry_log", "staff_role_permissions",
            "block_monthly_installments"
        };

        /// Rare/full heal — saari business forms (slow path, har tick nahi).
        private static readonly string[] HotTables =
        {
            "societies", "blocks", "plot_types", "plots", "plans",
            "customers", "brokers", "users", "customer_plots",
            "ledger", "receipts", "Expensetbl", "Person", "Deal", "Credit",
            "customer_plot_partners", "partner_money", "block_monthly_installments"
        };

        /// Live forms — har tick ~sub-second pull (customer/allocate/receipt).
        private static readonly string[] LiveTables =
        {
            "customers", "customer_plots", "receipts", "ledger",
            "plots", "partner_money", "customer_plot_partners"
        };

        private static DateTime _lastFullIncrementalUtc = DateTime.MinValue;
        private static DateTime _lastDeepHealUtc = DateTime.MinValue;
        private static readonly object _dirtyTablesLock = new object();
        private static readonly HashSet<string> _dirtyTables =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, Dictionary<string, string>> Fks =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "blocks", new Dictionary<string, string> { { "society_id", "societies" } } },
                { "plot_types", new Dictionary<string, string> { { "society_id", "societies" } } },
                { "plots", new Dictionary<string, string> {
                    { "society_id", "societies" }, { "block_id", "blocks" }, { "plot_type_id", "plot_types" } } },
                { "customer_plots", new Dictionary<string, string> {
                    { "customer_id", "customers" }, { "plot_id", "plots" }, { "plan_id", "plans" }, { "broker_id", "brokers" } } },
                { "receipts", new Dictionary<string, string> { { "customer_plot_id", "customer_plots" }, { "ledger_id", "ledger" } } },
                { "ledger", new Dictionary<string, string> { { "customer_plot_id", "customer_plots" } } },
                { "customer_plot_partners", new Dictionary<string, string> {
                    { "customer_plot_id", "customer_plots" }, { "partner_id", "Person" } } },
                { "partner_money", new Dictionary<string, string> {
                    { "partner_id", "Person" }, { "customer_plot_id", "customer_plots" },
                    { "receipt_id", "receipts" }, { "society_id", "societies" } } },
                { "Deal", new Dictionary<string, string> {
                    { "society_id", "societies" }, { "owner_id", "Person" } } },
                { "Credit", new Dictionary<string, string> {
                    { "deal_id", "Deal" }, { "partner_id", "Person" } } },
                { "block_monthly_installments", new Dictionary<string, string> { { "block_id", "blocks" } } },
            };

        private static readonly HttpClient Http = CreateHttp();
        private static readonly HttpClient HttpFast = CreateHttpFast();
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
        private static System.Windows.Forms.Timer _timer;
        private static int _busy;
        private static int _manualHold;
        private static volatile int _tickAgain;
        private static volatile bool _dirty = true;
        private static int _quietUi;
        // MinValue: app start pe pehli ticks mein CatchUp — allocate miss na rahe
        private static DateTime _lastCatchUpUtc = DateTime.MinValue;
        private static DateTime _lastAllocCatchUpUtc = DateTime.MinValue;
        private static DateTime _lastPruneUtc = DateTime.MinValue;
        private static DateTime _lastHashPushUtc = DateTime.MinValue;
        private static DateTime _lastFullPruneUtc = DateTime.MinValue;
        private static DateTime _lastSafetyPushUtc = DateTime.MinValue;
        private static DateTime _lastMissingPushUtc = DateTime.MinValue;

        private static HttpClient CreateHttp()
        {
            try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; } catch { }
            var c = new HttpClient();
            c.Timeout = TimeSpan.FromSeconds(45);
            return c;
        }

        private static HttpClient CreateHttpFast()
        {
            try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; } catch { }
            var c = new HttpClient();
            c.Timeout = TimeSpan.FromSeconds(8);
            return c;
        }

        private static string Pk(string table)
        {
            if (string.Equals(table, "Expensetbl", StringComparison.OrdinalIgnoreCase)) return "sid";
            if (string.Equals(table, "Person", StringComparison.OrdinalIgnoreCase)) return "person_id";
            if (string.Equals(table, "Deal", StringComparison.OrdinalIgnoreCase)) return "deal_id";
            if (string.Equals(table, "Credit", StringComparison.OrdinalIgnoreCase)) return "credit_id";
            if (string.Equals(table, "users", StringComparison.OrdinalIgnoreCase)) return "userID";
            return "id";
        }

        private static string CloudTable(string local)
        {
            if (string.Equals(local, "Expensetbl", StringComparison.OrdinalIgnoreCase)) return "expensetbl";
            if (string.Equals(local, "Person", StringComparison.OrdinalIgnoreCase)) return "person";
            if (string.Equals(local, "Deal", StringComparison.OrdinalIgnoreCase)) return "deal";
            if (string.Equals(local, "Credit", StringComparison.OrdinalIgnoreCase)) return "credit";
            return local.ToLowerInvariant();
        }

        private static string FkCloudCol(string localFk)
        {
            return localFk.Substring(0, localFk.Length - 3) + "_sync_id";
        }

        private static string LocalColToCloud(string table, string col)
        {
            if (string.Equals(table, "users", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(col, "uName", StringComparison.OrdinalIgnoreCase)) return "uname";
                if (string.Equals(col, "Role", StringComparison.OrdinalIgnoreCase)) return "role";
            }
            if (string.Equals(table, "Expensetbl", StringComparison.OrdinalIgnoreCase))
            {
                if (col == "Name") return "name";
                if (col == "Amount") return "amount";
                if (col == "Category") return "category";
                if (col == "Note") return "note";
                if (col == "EDate") return "edate";
            }
            return col.ToLowerInvariant();
        }

        private static string CloudColToLocal(string table, string col)
        {
            if (string.Equals(table, "users", StringComparison.OrdinalIgnoreCase))
            {
                if (col == "uname") return "uName";
                if (col == "role") return "Role";
            }
            if (string.Equals(table, "Expensetbl", StringComparison.OrdinalIgnoreCase))
            {
                if (col == "name") return "Name";
                if (col == "amount") return "Amount";
                if (col == "category") return "Category";
                if (col == "note") return "Note";
                if (col == "edate") return "EDate";
            }
            return col;
        }

        private static Dictionary<string, object> CloudToLocalPayload(string localTable, Dictionary<string, object> cloud)
        {
            var p = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in cloud)
            {
                string k = kv.Key;
                if (k == "updated_at" || k == "is_deleted" || k == "dup_key") continue;
                if (k == "staff_name" && !string.Equals(localTable, "staff_entry_log", StringComparison.OrdinalIgnoreCase)) continue;
                if (k.EndsWith("_sync_id", StringComparison.OrdinalIgnoreCase) && !string.Equals(k, "sync_id", StringComparison.OrdinalIgnoreCase))
                {
                    string localFk = k.Substring(0, k.Length - 8) + "_id";
                    p["__fk_" + localFk] = kv.Value;
                    continue;
                }
                if (string.Equals(k, "sync_id", StringComparison.OrdinalIgnoreCase))
                {
                    p["sync_id"] = kv.Value;
                    continue;
                }
                p[CloudColToLocal(localTable, k)] = kv.Value;
            }
            return p;
        }

        private static List<Dictionary<string, object>> AsApplyRows(string localTable, List<Dictionary<string, object>> cloudRows)
        {
            var list = new List<Dictionary<string, object>>();
            foreach (var r in cloudRows)
            {
                list.Add(new Dictionary<string, object>
                {
                    { "table_name", localTable },
                    { "sync_id", r.ContainsKey("sync_id") ? r["sync_id"] : "" },
                    { "is_deleted", r.ContainsKey("is_deleted") ? r["is_deleted"] : false },
                    { "staff_name", r.ContainsKey("staff_name") ? r["staff_name"] : "" },
                    { "payload", CloudToLocalPayload(localTable, r) }
                });
            }
            return list;
        }

        private static Dictionary<string, object> LocalToCloudRow(string localTable, Dictionary<string, object> payload, SQLiteConnection con)
        {
            var outRow = new Dictionary<string, object>();
            outRow["sync_id"] = payload.ContainsKey("sync_id") ? payload["sync_id"] : "";
            string pk = Pk(localTable);
            Dictionary<string, string> fks;
            Fks.TryGetValue(localTable, out fks);
            foreach (var kv in payload)
            {
                string k = kv.Key;
                if (string.Equals(k, "sync_id", StringComparison.OrdinalIgnoreCase)) continue;
                if (k.StartsWith("__fk_", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(k, pk, StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(k, "id", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(k, "sid", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(k, "person_id", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(k, "deal_id", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(k, "credit_id", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(k, "userID", StringComparison.OrdinalIgnoreCase)) continue;
                if (fks != null)
                {
                    bool isFk = false;
                    foreach (string fkName in fks.Keys)
                    {
                        if (string.Equals(fkName, k, StringComparison.OrdinalIgnoreCase))
                        {
                            isFk = true;
                            break;
                        }
                    }
                    if (isFk) continue;
                }
                if (kv.Value == null || kv.Value is DBNull) continue;
                outRow[LocalColToCloud(localTable, k)] = kv.Value;
            }
            if (fks != null)
            {
                foreach (var fk in fks)
                {
                    object fkVal;
                    if (!payload.TryGetValue(fk.Key, out fkVal) || fkVal == null) continue;
                    string parentSync = LookupSyncId(con, fk.Value, fkVal);
                    if (!string.IsNullOrEmpty(parentSync))
                        outRow[FkCloudCol(fk.Key)] = parentSync;
                }
            }
            return outRow;
        }

        /// <summary>
        /// Push se pehle: cloud pe jo sync_id already is_deleted=true hain unhe SKIP —
        /// warna doosri device ka SoftMark overwrite ho jata hai (delete wapas zinda).
        /// Skipped ids local se bhi delete (confirm prune).
        /// </summary>
        private static HashSet<string> FetchCloudDeletedSyncIds(string cloud, List<string> syncIds)
        {
            var deleted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(cloud) || syncIds == null || syncIds.Count == 0)
                return deleted;
            for (int i = 0; i < syncIds.Count; i += 40)
            {
                int n = Math.Min(40, syncIds.Count - i);
                var sb = new StringBuilder();
                for (int j = 0; j < n; j++)
                {
                    string sid = (syncIds[i + j] ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(sid)) continue;
                    if (sb.Length > 0) sb.Append(",");
                    sb.Append(Uri.EscapeDataString(sid));
                }
                if (sb.Length == 0) continue;
                var rows = HttpGetListOk("/rest/v1/" + cloud
                    + "?sync_id=in.(" + sb + ")&is_deleted=eq.true&select=sync_id&limit=100", 4);
                if (rows == null) continue;
                foreach (var row in rows)
                {
                    string id = Convert.ToString(row.ContainsKey("sync_id") ? row["sync_id"] : "").Trim();
                    if (!string.IsNullOrWhiteSpace(id)) deleted.Add(id);
                }
            }
            return deleted;
        }

        private static void DeleteLocalByCloudDeleted(string cloud, HashSet<string> deletedIds)
        {
            if (deletedIds == null || deletedIds.Count == 0) return;
            string local = "";
            foreach (string t in Tables)
            {
                if (string.Equals(CloudTable(t), cloud, StringComparison.OrdinalIgnoreCase))
                {
                    local = t;
                    break;
                }
            }
            if (string.IsNullOrWhiteSpace(local)) return;
            try
            {
                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    Exec(con, "PRAGMA foreign_keys = OFF;");
                    try
                    {
                        foreach (string sid in deletedIds)
                        {
                            using (var cmd = new SQLiteCommand(
                                "DELETE FROM \"" + local + "\" WHERE sync_id=@s;", con))
                            {
                                cmd.Parameters.AddWithValue("@s", sid);
                                cmd.ExecuteNonQuery();
                            }
                            using (var cmd = new SQLiteCommand("DELETE FROM sync_known WHERE sync_id=@s;", con))
                            {
                                cmd.Parameters.AddWithValue("@s", sid);
                                cmd.ExecuteNonQuery();
                            }
                            using (var cmd = new SQLiteCommand("DELETE FROM sync_hash WHERE sync_id=@s;", con))
                            {
                                cmd.Parameters.AddWithValue("@s", sid);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    finally
                    {
                        try { Exec(con, "PRAGMA foreign_keys = ON;"); } catch { }
                    }
                }
                RaiseLiveChanged();
            }
            catch { }
        }

        private static bool FlushCloudBatch(List<Dictionary<string, object>> batch)
        {
            if (batch == null || batch.Count == 0) return true;
            var grouped = new Dictionary<string, List<Dictionary<string, object>>>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in batch)
            {
                string ct = Convert.ToString(row.ContainsKey("_cloud_table") ? row["_cloud_table"] : "");
                if (row.ContainsKey("_cloud_table")) row.Remove("_cloud_table");
                if (string.IsNullOrWhiteSpace(ct)) continue;
                if (!grouped.ContainsKey(ct)) grouped[ct] = new List<Dictionary<string, object>>();
                grouped[ct].Add(row);
            }
            bool allOk = true;
            foreach (var kv in grouped)
            {
                var ids = new List<string>();
                foreach (var row in kv.Value)
                {
                    string sid = Convert.ToString(row.ContainsKey("sync_id") ? row["sync_id"] : "").Trim();
                    if (!string.IsNullOrWhiteSpace(sid)) ids.Add(sid);
                }
                var cloudDeleted = FetchCloudDeletedSyncIds(kv.Key, ids);
                if (cloudDeleted.Count > 0)
                {
                    kv.Value.RemoveAll(row =>
                    {
                        string sid = Convert.ToString(row.ContainsKey("sync_id") ? row["sync_id"] : "").Trim();
                        return cloudDeleted.Contains(sid);
                    });
                    DeleteLocalByCloudDeleted(kv.Key, cloudDeleted);
                }
                if (kv.Value.Count == 0) continue;

                var hashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var row in kv.Value)
                {
                    string sid = Convert.ToString(row.ContainsKey("sync_id") ? row["sync_id"] : "");
                    string h = "";
                    if (row.ContainsKey("_row_hash"))
                    {
                        h = Convert.ToString(row["_row_hash"] ?? "");
                        row.Remove("_row_hash");
                    }
                    if (!string.IsNullOrWhiteSpace(sid) && !string.IsNullOrWhiteSpace(h))
                        hashes[sid] = h;
                }
                if (UpsertCloud(kv.Key, kv.Value))
                {
                    try
                    {
                        using (var con = new SQLiteConnection(MainClass.con_string))
                        {
                            con.Open();
                            foreach (var h in hashes)
                                SaveHash(con, h.Key, h.Value);
                        }
                    }
                    catch { }
                }
                else allOk = false;
            }
            batch.Clear();
            return allOk;
        }

        private static bool UpsertCloud(string cloud, List<Dictionary<string, object>> rows)
        {
            return UpsertCloudCore(cloud, rows, false);
        }

        /// PushOneNow — HttpFast (8s) taake WinForms→mobile <1s.
        private static bool UpsertCloudFast(string cloud, List<Dictionary<string, object>> rows)
        {
            return UpsertCloudCore(cloud, rows, true);
        }

        private static bool UpsertCloudCore(string cloud, List<Dictionary<string, object>> rows, bool fast)
        {
            if (rows == null || rows.Count == 0) return true;
            try
            {
                string url = SupabaseConfig.Url.TrimEnd('/') + "/rest/v1/" + cloud + "?on_conflict=sync_id";
                var req = new HttpRequestMessage(HttpMethod.Post, url);
                ApplyHeaders(req);
                req.Headers.TryAddWithoutValidation("Prefer", "resolution=merge-duplicates,return=minimal");
                req.Content = new StringContent(Json.Serialize(rows), Encoding.UTF8, "application/json");
                HttpClient client = fast ? HttpFast : Http;
                using (var resp = client.SendAsync(req).GetAwaiter().GetResult())
                {
                    if (resp.IsSuccessStatusCode) return true;
                    if (rows.Count > 1)
                    {
                        bool ok = true;
                        foreach (var row in rows)
                        {
                            if (!UpsertCloudCore(cloud, new List<Dictionary<string, object>> { row }, fast))
                                ok = false;
                        }
                        return ok;
                    }
                    if (string.Equals(cloud, "users", StringComparison.OrdinalIgnoreCase) && !_mergingUser)
                        return MergeUserIntoLiveUsername(rows[0]);
                    return false;
                }
            }
            catch
            {
                if (rows.Count > 1)
                {
                    bool ok = true;
                    foreach (var row in rows)
                    {
                        if (!UpsertCloudCore(cloud, new List<Dictionary<string, object>> { row }, fast))
                            ok = false;
                    }
                    return ok;
                }
                if (string.Equals(cloud, "users", StringComparison.OrdinalIgnoreCase) && !_mergingUser)
                    return MergeUserIntoLiveUsername(rows[0]);
                return false;
            }
        }

        /// Password form ne purane (tombstone) sync_id se push kiya to unique username
        /// live Admin pe lag jata hai. Live row pe password merge karo — login mat todo.
        private static bool MergeUserIntoLiveUsername(Dictionary<string, object> row)
        {
            if (row == null) return false;
            string un = Convert.ToString(row.ContainsKey("username") ? row["username"] : "").Trim();
            if (string.IsNullOrWhiteSpace(un)) return false;
            try
            {
                var live = HttpGetListOk(
                    "/rest/v1/users?is_deleted=eq.false&username=ilike." + Uri.EscapeDataString(un)
                    + "&select=sync_id&limit=1", 8);
                if (live == null || live.Count == 0) return false;
                string liveSid = Convert.ToString(live[0].ContainsKey("sync_id") ? live[0]["sync_id"] : "").Trim();
                if (string.IsNullOrWhiteSpace(liveSid)) return false;
                string oldSid = Convert.ToString(row.ContainsKey("sync_id") ? row["sync_id"] : "").Trim();
                row["sync_id"] = liveSid;
                row["is_deleted"] = false;
                _mergingUser = true;
                bool ok;
                try { ok = UpsertCloud("users", new List<Dictionary<string, object>> { row }); }
                finally { _mergingUser = false; }
                if (!ok) return false;
                try
                {
                    using (var con = new SQLiteConnection(MainClass.con_string))
                    {
                        con.Open();
                        using (var cmd = new SQLiteCommand(
                            "UPDATE users SET sync_id=@live WHERE lower(trim(ifnull(username,''))) = lower(trim(@u));", con))
                        {
                            cmd.Parameters.AddWithValue("@live", liveSid);
                            cmd.Parameters.AddWithValue("@u", un);
                            cmd.ExecuteNonQuery();
                        }
                        using (var cmd = new SQLiteCommand(
                            "DELETE FROM users WHERE lower(trim(ifnull(username,''))) = lower(trim(@u)) AND ifnull(sync_id,'') <> @live "
                            + "AND userID NOT IN (SELECT MIN(userID) FROM users WHERE lower(trim(ifnull(username,''))) = lower(trim(@u)));", con))
                        {
                            cmd.Parameters.AddWithValue("@u", un);
                            cmd.Parameters.AddWithValue("@live", liveSid);
                            try { cmd.ExecuteNonQuery(); } catch { }
                        }
                        if (!string.IsNullOrWhiteSpace(oldSid) && !string.Equals(oldSid, liveSid, StringComparison.OrdinalIgnoreCase))
                        {
                            using (var cmd = new SQLiteCommand("DELETE FROM sync_hash WHERE sync_id=@s;", con))
                            {
                                cmd.Parameters.AddWithValue("@s", oldSid);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
                catch { }
                return true;
            }
            catch { return false; }
        }

        public static void EnsureLocalSchema()
        {
            try
            {
                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    Exec(con, @"CREATE TABLE IF NOT EXISTS sync_state (
  k TEXT PRIMARY KEY,
  v TEXT
);");
                    Exec(con, @"CREATE TABLE IF NOT EXISTS sync_known (
  sync_id TEXT PRIMARY KEY,
  table_name TEXT
);");
                    Exec(con, @"CREATE TABLE IF NOT EXISTS sync_hash (
  sync_id TEXT PRIMARY KEY,
  row_hash TEXT
);");
                    Exec(con, @"CREATE TABLE IF NOT EXISTS sync_alias (
  sync_id TEXT PRIMARY KEY,
  table_name TEXT NOT NULL,
  local_pk INTEGER NOT NULL
);");
                    Exec(con, @"CREATE TABLE IF NOT EXISTS sync_pending_delete (
  sync_id TEXT PRIMARY KEY,
  table_name TEXT NOT NULL,
  queued_at TEXT DEFAULT (datetime('now','localtime'))
);");
                    Exec(con, @"CREATE TABLE IF NOT EXISTS sync_outbox (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  table_name TEXT NOT NULL,
  local_pk INTEGER NOT NULL,
  sync_id TEXT NOT NULL,
  revision TEXT NOT NULL,
  op TEXT NOT NULL DEFAULT 'upsert',
  created_at TEXT DEFAULT (datetime('now'))
);");
                    Exec(con, @"CREATE TABLE IF NOT EXISTS sync_pending_apply (
  sync_id TEXT PRIMARY KEY,
  table_name TEXT NOT NULL,
  payload_json TEXT NOT NULL,
  wait_parent_table TEXT,
  wait_parent_sync_id TEXT,
  cloud_updated_at TEXT,
  created_at TEXT DEFAULT (datetime('now'))
);");
                    Exec(con, @"CREATE TABLE IF NOT EXISTS sync_conflict (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  table_name TEXT NOT NULL,
  sync_id TEXT NOT NULL,
  local_json TEXT,
  cloud_json TEXT,
  created_at TEXT DEFAULT (datetime('now'))
);");
                    foreach (string t in Tables)
                    {
                        if (!TableExists(con, t)) continue;
                        EnsureColumn(con, t, "sync_id", "TEXT");
                        Exec(con, "UPDATE \"" + t + "\" SET sync_id = lower(hex(randomblob(16))) WHERE sync_id IS NULL OR TRIM(sync_id)='';");
                    }
                    // sync_id index = tez + safe apply (SCAN band) — UNIQUE nahi (purani dup break na ho)
                    EnsureSyncIdIndexes(con);
                }
                SeedHashesIfAlreadySynced();
                try { BlockMonthlyInstallmentStore.EnsureSchema(); } catch { }
                try { ReconcileOneActiveCustomerPlots(); } catch { }
                try { BlockMonthlyInstallmentStore.ApplyToEmptyActive(); } catch { }
                // Schema bump: last_pull / first_pull WIPE mat karo — entries protect
                string sch = GetState("sync_schema");
                // 11 = outbox + pending_apply + anti-resurrect (entries wipe nahi)
                if (string.IsNullOrWhiteSpace(sch) || string.CompareOrdinal(sch, "11") < 0)
                {
                    SetState("sync_schema", "11");
                    if (string.IsNullOrWhiteSpace(GetState("alias_backfill_done")))
                        SetState("alias_backfill_done", "");
                }
            }
            catch { }
            try
            {
                if (SupabaseConfig.IsConfigured && GetState("alias_backfill_done") != "1")
                {
                    BackfillSyncAliases();
                    SetState("alias_backfill_done", "1");
                }
            }
            catch { }
        }

        /// <summary>Non-unique sync_id indexes — wipe/dup risk ke bina lookup tez.</summary>
        private static void EnsureSyncIdIndexes(SQLiteConnection con)
        {
            string[] indexed =
            {
                "customers", "customer_plots", "receipts", "ledger", "plots",
                "partner_money", "customer_plot_partners", "brokers", "plans",
                "societies", "blocks", "plot_types", "users", "Person", "Deal",
                "Credit", "Expensetbl", "block_monthly_installments"
            };
            foreach (string t in indexed)
            {
                try
                {
                    if (!TableExists(con, t)) continue;
                    string idx = "idx_" + t.ToLowerInvariant() + "_sync_id";
                    Exec(con, "CREATE INDEX IF NOT EXISTS \"" + idx + "\" ON \"" + t + "\"(sync_id);");
                }
                catch { }
            }
        }

        /// <summary>Cloud duplicate sync_ids → local row (pull FK fix after merge).</summary>
        private static void BackfillSyncAliases()
        {
            using (var con = new SQLiteConnection(MainClass.con_string))
            {
                con.Open();
                Exec(con, @"CREATE TABLE IF NOT EXISTS sync_alias (
  sync_id TEXT PRIMARY KEY,
  table_name TEXT NOT NULL,
  local_pk INTEGER NOT NULL
);");
                AliasCloudTable(con, "societies", "societies",
                    "SELECT id FROM societies WHERE lower(trim(ifnull(name,'')))=@n LIMIT 1",
                    row =>
                    {
                        object n;
                        row.TryGetValue("name", out n);
                        return new[] { new KeyValuePair<string, object>("@n", P(new Dictionary<string, object> { { "name", n } }, "name")) };
                    });
                AliasCloudTable(con, "customers", "customers",
                    "SELECT id FROM customers WHERE lower(trim(ifnull(name,'')))=@n AND ifnull(mobile,'')=@m AND ifnull(cnic,'')=@c LIMIT 1",
                    row =>
                    {
                        object n, m, c;
                        row.TryGetValue("name", out n);
                        row.TryGetValue("mobile", out m);
                        row.TryGetValue("cnic", out c);
                        return new[]
                        {
                            new KeyValuePair<string, object>("@n", Convert.ToString(n ?? "").Trim().ToLowerInvariant()),
                            new KeyValuePair<string, object>("@m", Convert.ToString(m ?? "")),
                            new KeyValuePair<string, object>("@c", Convert.ToString(c ?? ""))
                        };
                    });
                AliasCloudTable(con, "brokers", "brokers",
                    "SELECT id FROM brokers WHERE lower(trim(ifnull(name,'')))=@n AND ifnull(phone,'')=@p LIMIT 1",
                    row =>
                    {
                        object n, p;
                        row.TryGetValue("name", out n);
                        row.TryGetValue("phone", out p);
                        return new[]
                        {
                            new KeyValuePair<string, object>("@n", Convert.ToString(n ?? "").Trim().ToLowerInvariant()),
                            new KeyValuePair<string, object>("@p", Convert.ToString(p ?? ""))
                        };
                    });
                AliasCloudTable(con, "plots", "plots",
                    "SELECT id FROM plots WHERE society_id=@s AND ifnull(plot_no,'')=@p LIMIT 1",
                    row =>
                    {
                        object ss, pn;
                        row.TryGetValue("society_sync_id", out ss);
                        row.TryGetValue("plot_no", out pn);
                        long? soc = LocalIdBySync(con, "societies", Convert.ToString(ss ?? ""));
                        if (!soc.HasValue) return null;
                        return new[]
                        {
                            new KeyValuePair<string, object>("@s", soc.Value),
                            new KeyValuePair<string, object>("@p", Convert.ToString(pn ?? ""))
                        };
                    });
                // customer_plots: content alias (same customer+plot) band —
                // cancel→reallocate / do devices ke alag sync_id merge na hon.
                // Sirf empty sync_id wali legacy local row cloud identity le sakti hai.
                AliasCloudTable(con, "customer_plots", "customer_plots",
                    "SELECT id FROM customer_plots WHERE customer_id=@c AND plot_id=@p AND ifnull(sync_id,'')='' LIMIT 1",
                    row =>
                    {
                        object cs, ps;
                        row.TryGetValue("customer_sync_id", out cs);
                        row.TryGetValue("plot_sync_id", out ps);
                        long? cust = LocalIdBySync(con, "customers", Convert.ToString(cs ?? ""));
                        long? plot = LocalIdBySync(con, "plots", Convert.ToString(ps ?? ""));
                        if (!cust.HasValue || !plot.HasValue) return null;
                        return new[]
                        {
                            new KeyValuePair<string, object>("@c", cust.Value),
                            new KeyValuePair<string, object>("@p", plot.Value)
                        };
                    });
            }
        }

        /// <summary>
        /// Partner Cash / Receipt / Allocate: content-key pe silent merge mat karo.
        /// Har unique sync_id alag row; LWW sirf same sync_id pe.
        /// </summary>
        private static bool SkipContentDupMerge(string table)
        {
            return string.Equals(table, "partner_money", StringComparison.OrdinalIgnoreCase)
                || string.Equals(table, "receipts", StringComparison.OrdinalIgnoreCase)
                || string.Equals(table, "customer_plots", StringComparison.OrdinalIgnoreCase)
                || string.Equals(table, "customers", StringComparison.OrdinalIgnoreCase)
                || string.Equals(table, "plots", StringComparison.OrdinalIgnoreCase)
                || string.Equals(table, "ledger", StringComparison.OrdinalIgnoreCase);
        }

        private static void AliasCloudTable(
            SQLiteConnection con,
            string cloudTable,
            string localTable,
            string findSql,
            Func<Dictionary<string, object>, KeyValuePair<string, object>[]> bind)
        {
            try
            {
                if (!TableExists(con, localTable)) return;
                var rows = HttpGetList("/rest/v1/" + cloudTable + "?is_deleted=eq.false&select=*&limit=5000");
                foreach (var row in rows)
                {
                    object sidObj;
                    if (!row.TryGetValue("sync_id", out sidObj)) continue;
                    string cloudSync = Convert.ToString(sidObj ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(cloudSync)) continue;
                    var pars = bind(row);
                    if (pars == null) continue;
                    long? localPk = null;
                    using (var cmd = new SQLiteCommand(findSql, con))
                    {
                        foreach (var p in pars) cmd.Parameters.AddWithValue(p.Key, p.Value ?? "");
                        object o = cmd.ExecuteScalar();
                        if (o != null && o != DBNull.Value) localPk = Convert.ToInt64(o);
                    }
                    if (localPk.HasValue) SaveSyncAlias(con, localTable, cloudSync, localPk.Value);
                }
            }
            catch { }
        }

        public static event Action LiveChanged;

        public static void RaiseLiveChanged()
        {
            if (_quietUi > 0) return;
            try
            {
                Form f = FindMainForm();
                Action go = delegate
                {
                    try { if (LiveChanged != null) LiveChanged(); } catch { }
                };
                if (f != null && !f.IsDisposed && f.IsHandleCreated && f.InvokeRequired)
                    f.BeginInvoke(go);
                else go();
            }
            catch { }
        }

        public static void NotifyActivity(string action, string module)
        {
            try
            {
                Form f = FindMainForm();
                Action go = delegate
                {
                    try { if (LiveChanged != null) LiveChanged(); } catch { }
                };
                if (f != null && !f.IsDisposed && f.IsHandleCreated && f.InvokeRequired)
                    f.BeginInvoke(go);
                else go();
            }
            catch { }
        }

        private static Form FindMainForm()
        {
            try
            {
                foreach (Form x in Application.OpenForms)
                {
                    if (x is MainForm && !x.IsDisposed) return x;
                }
            }
            catch { }
            return null;
        }

        private static string PrettyModule(string module)
        {
            if (string.IsNullOrWhiteSpace(module)) return "entry";
            if (string.Equals(module, "customers", StringComparison.OrdinalIgnoreCase)) return "Customer";
            if (string.Equals(module, "societies", StringComparison.OrdinalIgnoreCase)) return "Society";
            if (string.Equals(module, "blocks", StringComparison.OrdinalIgnoreCase)) return "Block";
            if (string.Equals(module, "plots", StringComparison.OrdinalIgnoreCase)) return "Plot";
            if (string.Equals(module, "plot_types", StringComparison.OrdinalIgnoreCase)) return "Plot Type";
            if (string.Equals(module, "receipts", StringComparison.OrdinalIgnoreCase)) return "Receipt";
            if (string.Equals(module, "Expensetbl", StringComparison.OrdinalIgnoreCase)) return "Expense";
            if (string.Equals(module, "brokers", StringComparison.OrdinalIgnoreCase)) return "Broker";
            if (string.Equals(module, "Person", StringComparison.OrdinalIgnoreCase)) return "Person";
            if (string.Equals(module, "Deal", StringComparison.OrdinalIgnoreCase)) return "Deal";
            if (string.Equals(module, "Credit", StringComparison.OrdinalIgnoreCase)) return "Credit";
            if (string.Equals(module, "partner_money", StringComparison.OrdinalIgnoreCase)) return "Partner Cash";
            if (string.Equals(module, "customer_plots", StringComparison.OrdinalIgnoreCase)) return "Allocation";
            return module;
        }

        /// <summary>Restore ke baad: hashes clear + pehle push (warna cloud/mobile purana reh jata).</summary>
        public static void NotifyDatabaseRestored()
        {
            try
            {
                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    try { Exec(con, "DELETE FROM sync_hash;"); } catch { }
                }
                SetState("push_hashes_ready", "");
                SetState("restore_push_first", "1");
                // Full-pull mat chalao — restored data cloud ke purane se overwrite na ho
                if (GetState("first_pull_done") != "1")
                    SetState("first_pull_done", "1");
                if (string.IsNullOrWhiteSpace(GetState("last_pull_utc")))
                    SetState("last_pull_utc", DateTime.UtcNow.ToString("o"));
                MarkDirty();
                RaiseLiveChanged();
                ScheduleQuickSync();
            }
            catch { }
        }

        /// <summary>Local insert/update/delete ke baad turant background push + open lists refresh.</summary>
        public static void MarkDirty()
        {
            _dirty = true;
        }

        /// <summary>PushOneNow success ke baad — tick sirf pull kare, mass PushChanged na.</summary>
        public static void ClearDirty()
        {
            _dirty = false;
            lock (_dirtyTablesLock) _dirtyTables.Clear();
        }

        public static void MarkDirtyTable(string table)
        {
            _dirty = true;
            if (string.IsNullOrWhiteSpace(table)) return;
            lock (_dirtyTablesLock) _dirtyTables.Add(table.Trim());
        }

        public static void NotifyLocalWrite()
        {
            try { MarkDirty(); } catch { }
            try { ScheduleBackgroundPushPublic(); } catch { }
            try { RaiseLiveChanged(); } catch { }
        }

        /// <summary>
        /// Allocate/Receipt jaisi forms jo MainClass.SQL bypass karti hain —
        /// turant usi row ka sync_id + cloud upsert, phir pull (full PushChanged wait nahi).
        /// </summary>
        public static void NotifyLocalWrite(string table, long rowId)
        {
            NotifyLocalWrites(new[] { new KeyValuePair<string, long>(table, rowId) });
        }

        public static void NotifyLocalWrites(IEnumerable<KeyValuePair<string, long>> rows)
        {
            try { RaiseLiveChanged(); } catch { }
            var list = new List<KeyValuePair<string, long>>();
            if (rows != null)
            {
                foreach (var kv in rows)
                {
                    if (kv.Value > 0 && !string.IsNullOrWhiteSpace(kv.Key))
                        list.Add(kv);
                }
            }
            if (list.Count == 0)
            {
                NotifyLocalWrite();
                return;
            }
            // Outbox pehle (durable) — phir HTTP bahar
            var jobs = new List<Tuple<string, long, string>>();
            foreach (var kv in list)
            {
                try
                {
                    MarkDirtyTable(kv.Key);
                    string syncId = "";
                    using (var con = new SQLiteConnection(MainClass.con_string))
                    {
                        con.Open();
                        if (!TableExists(con, kv.Key)) continue;
                        string pk = Pk(kv.Key);
                        using (var u = new SQLiteCommand(
                            "UPDATE \"" + kv.Key + "\" SET sync_id=lower(hex(randomblob(16))) WHERE \"" + pk + "\"=@id AND (sync_id IS NULL OR TRIM(sync_id)='');", con))
                        {
                            u.Parameters.AddWithValue("@id", kv.Value);
                            u.ExecuteNonQuery();
                        }
                        using (var q = new SQLiteCommand(
                            "SELECT sync_id FROM \"" + kv.Key + "\" WHERE \"" + pk + "\"=@id LIMIT 1;", con))
                        {
                            q.Parameters.AddWithValue("@id", kv.Value);
                            syncId = Convert.ToString(q.ExecuteScalar() ?? "").Trim();
                        }
                    }
                    string rev = EnqueueOutbox(kv.Key, kv.Value, syncId, "upsert");
                    jobs.Add(Tuple.Create(kv.Key, kv.Value, rev));
                }
                catch { MarkDirtyTable(kv.Key); }
            }
            ThreadPool.QueueUserWorkItem(_ =>
            {
                bool anyFail = false;
                foreach (var j in jobs)
                {
                    try
                    {
                        if (!PushOneNow(j.Item1, j.Item2, j.Item3)) anyFail = true;
                    }
                    catch { anyFail = true; MarkDirtyTable(j.Item1); }
                }
                try
                {
                    if (!anyFail) ClearDirty();
                    ScheduleQuickSync();
                }
                catch { }
            });
        }

        /// <summary>Null sync_id — sirf LiveTables (har tick pura DB mat ghumao).</summary>
        private static void EnsureMissingSyncIds()
        {
            try
            {
                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    int changed = 0;
                    foreach (string table in LiveTables)
                    {
                        if (!TableExists(con, table)) continue;
                        using (var cmd = new SQLiteCommand(
                            "UPDATE \"" + table + "\" SET sync_id = lower(hex(randomblob(16))) WHERE sync_id IS NULL OR TRIM(sync_id)='';",
                            con))
                        {
                            int n = cmd.ExecuteNonQuery();
                            if (n > 0)
                            {
                                changed += n;
                                MarkDirtyTable(table);
                            }
                        }
                    }
                    if (changed > 0) _dirty = true;
                }
            }
            catch { }
        }

        private static bool IsLastPullStale(int minutes)
        {
            try
            {
                string lp = GetState("last_pull_utc");
                if (string.IsNullOrWhiteSpace(lp)) return true;
                DateTime t;
                if (!DateTime.TryParse(lp, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out t))
                    return true;
                if (t.Kind == DateTimeKind.Unspecified)
                    t = DateTime.SpecifyKind(t, DateTimeKind.Utc);
                return (DateTime.UtcNow - t.ToUniversalTime()).TotalMinutes > minutes;
            }
            catch { return true; }
        }

        /// <summary>Manual Pull Now — wait for auto-tick, phir pull. Busy pe turant fail nahi.</summary>
        public static bool TryPullNow()
        {
            if (!SupabaseConfig.IsConfigured) return false;
            Interlocked.Increment(ref _manualHold);
            try
            {
                if (!WaitForSyncLock(90)) return false;
                try
                {
                    if (GetState("restore_push_first") == "1" && CanPush())
                    {
                        PushPendingDeletes();
                        if (PushChanged())
                        {
                            _dirty = false;
                            SetState("restore_push_first", "");
                        }
                    }
                    if (NeedsFullPull())
                        FullPullAllTables();
                    else
                    {
                        PullIncremental();
                        PullRecentHeal(120);
                        CatchUpMissing();
                    }
                    ReconcileUsersFromCloud();
                    SafePruneGoneFromCloud(true);
                    RaiseLiveChanged();
                    return true;
                }
                catch { return false; }
                finally { Interlocked.Exchange(ref _busy, 0); }
            }
            finally { Interlocked.Decrement(ref _manualHold); }
        }

        /// <summary>Manual Sync Now — wait for auto-tick, pull+push+soft-delete prune.</summary>
        public static bool TrySyncNow()
        {
            if (!SupabaseConfig.IsConfigured) return false;
            MarkDirty();
            Interlocked.Increment(ref _manualHold);
            try
            {
                if (!WaitForSyncLock(90)) return false;
                try
                {
                    // Restore ke baad: pehle local → cloud (Partner Cash + sab), phir pull
                    if (GetState("restore_push_first") == "1")
                    {
                        if (CanPush())
                        {
                            PushPendingDeletes();
                            if (PushChanged())
                            {
                                _dirty = false;
                                SetState("restore_push_first", "");
                            }
                        }
                        if (NeedsFullPull())
                            FullPullAllTables();
                        else
                        {
                            PullIncremental();
                            PullRecentHeal(120);
                            CatchUpMissing();
                        }
                        ReconcileUsersFromCloud();
                        SafePruneGoneFromCloud(true);
                        RaiseLiveChanged();
                        return true;
                    }
                    if (NeedsFullPull())
                        FullPullAllTables();
                    else
                    {
                        PullIncremental();
                        PullRecentHeal(120);
                        CatchUpMissing();
                    }
                    ReconcileUsersFromCloud();
                    if (CanPush())
                    {
                        PushPendingDeletes();
                        if (PushChanged()) _dirty = false;
                    }
                    SafePruneGoneFromCloud(true);
                    RaiseLiveChanged();
                    return true;
                }
                catch { return false; }
                finally { Interlocked.Exchange(ref _busy, 0); }
            }
            finally { Interlocked.Decrement(ref _manualHold); }
        }

        /// <summary>
        /// Allocate-chain parents + customer_plots + ledger/receipts (FK order).
        /// Used only by Force Recover — normal Sync/Pull untouched.
        /// </summary>
        private static readonly string[] AllocateRecoverTables =
        {
            "societies", "blocks", "plot_types", "plots", "plans",
            "customers", "brokers", "customer_plots", "ledger", "receipts"
        };

        /// <summary>
        /// Force Recover UI entry — DISABLED (galati se press = pehle wala masla).
        /// Normal Sync/Pull use karein. Internal missing-insert logic alag rehti hai.
        /// </summary>
        public static bool TryForceRecoverFromCloud(out string summary)
        {
            summary = "Force Recover band hai — Sync Now / Pull Now use karein. Galati se recover nahi hoga.";
            return false;
        }

        /// <summary>
        /// Pull-missing-only: cloud is_deleted=false + local sync_id absent → INSERT via ApplyFetched.
        /// Local same sync_id → skip (no update / no second row). Deleted cloud rows not fetched.
        /// Multi-pass + parent ensure for customer_plots (plot chain / customer / placeholder).
        /// </summary>
        public static void ForceRecoverMissingFromCloud(
            out int recovered, out int skipped, out int allocateRecovered)
        {
            string ignore;
            ForceRecoverMissingFromCloud(out recovered, out skipped, out allocateRecovered, out ignore);
        }

        public static void ForceRecoverMissingFromCloud(
            out int recovered, out int skipped, out int allocateRecovered, out string skipReasons)
        {
            recovered = 0;
            skipped = 0;
            allocateRecovered = 0;
            skipReasons = "";
            var skipList = new List<string>();
            try
            {
                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    // Stale content-dup aliases hide missing cloud rows — clear union-safe tables
                    Exec(con,
                        "DELETE FROM sync_alias WHERE lower(table_name) IN ('partner_money','receipts','customer_plots');");
                }
            }
            catch { }

            // Pass 1–2: parents → customer_plots → receipts. Pass 3: customer_plots retry after placeholders.
            for (int pass = 0; pass < 3; pass++)
            {
                foreach (string table in AllocateRecoverTables)
                {
                    bool isAlloc = string.Equals(table, "customer_plots", StringComparison.OrdinalIgnoreCase);
                    string cloud = CloudTable(table);
                    HashSet<string> local;
                    using (var con = new SQLiteConnection(MainClass.con_string))
                    {
                        con.Open();
                        local = LocalSyncIdsForTable(con, table);
                    }
                    for (int page = 0; page < 60; page++)
                    {
                        var rows = HttpGetList("/rest/v1/" + cloud
                            + "?is_deleted=eq.false&select=sync_id&order=sync_id.asc&limit=1000&offset="
                            + (page * 1000));
                        if (rows.Count == 0) break;
                        var missing = new List<string>();
                        foreach (var row in rows)
                        {
                            string id = Convert.ToString(row.ContainsKey("sync_id") ? row["sync_id"] : "");
                            if (string.IsNullOrWhiteSpace(id)) continue;
                            if (local.Contains(id))
                            {
                                if (pass == 0) skipped++;
                                continue;
                            }
                            missing.Add(id);
                        }
                        for (int i = 0; i < missing.Count; i += 30)
                        {
                            int n = Math.Min(30, missing.Count - i);
                            var sb = new StringBuilder();
                            for (int j = 0; j < n; j++)
                            {
                                if (j > 0) sb.Append(',');
                                sb.Append(missing[i + j]);
                            }
                            // is_deleted=eq.false again — intentional deletes stay out
                            var fetched = HttpGetList("/rest/v1/" + cloud
                                + "?is_deleted=eq.false&sync_id=in.(" + sb + ")");
                            if (fetched.Count == 0) continue;

                            if (isAlloc)
                                EnsureRecoverParentsForCustomerPlots(fetched, skipList);
                            else if (string.Equals(table, "customers", StringComparison.OrdinalIgnoreCase))
                                FilterOutRecoverPlaceholderCustomers(fetched, skipList);
                            else if (string.Equals(table, "receipts", StringComparison.OrdinalIgnoreCase)
                                || string.Equals(table, "ledger", StringComparison.OrdinalIgnoreCase))
                                FilterRecoverChildrenToExistingAllocs(table, fetched, skipList);

                            if (fetched.Count == 0) continue;

                            int before = LocalRowCount(table);
                            ApplyFetched(AsApplyRows(table, fetched));
                            int after = LocalRowCount(table);
                            int added = Math.Max(0, after - before);
                            recovered += added;
                            if (isAlloc
                                || string.Equals(table, "receipts", StringComparison.OrdinalIgnoreCase))
                                allocateRecovered += added;

                            // Only mark local when actually present — failed FK inserts retry next pass
                            using (var con = new SQLiteConnection(MainClass.con_string))
                            {
                                con.Open();
                                foreach (var r in fetched)
                                {
                                    string id = Convert.ToString(r.ContainsKey("sync_id") ? r["sync_id"] : "");
                                    if (string.IsNullOrWhiteSpace(id)) continue;
                                    if (LocalIdBySyncExact(con, table, id).HasValue)
                                        local.Add(id);
                                }
                            }
                        }
                        if (rows.Count < 1000) break;
                    }
                }
            }

            // Final skip reasons for Active allocs still missing after all passes
            try
            {
                HashSet<string> localCp;
                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    localCp = LocalSyncIdsForTable(con, "customer_plots");
                }
                var still = HttpGetList("/rest/v1/customer_plots"
                    + "?status=eq.Active&is_deleted=eq.false&select=sync_id,plot_sync_id,customer_sync_id"
                    + "&order=sync_id.asc&limit=1000");
                foreach (var row in still)
                {
                    string id = Convert.ToString(row.ContainsKey("sync_id") ? row["sync_id"] : "");
                    if (string.IsNullOrWhiteSpace(id) || localCp.Contains(id)) continue;
                    string ps = Convert.ToString(row.ContainsKey("plot_sync_id") ? row["plot_sync_id"] : "");
                    string cs = Convert.ToString(row.ContainsKey("customer_sync_id") ? row["customer_sync_id"] : "");
                    string why;
                    using (var con = new SQLiteConnection(MainClass.con_string))
                    {
                        con.Open();
                        bool hasPlot = !string.IsNullOrWhiteSpace(ps) && LocalIdBySync(con, "plots", ps).HasValue;
                        bool hasCust = !string.IsNullOrWhiteSpace(cs) && LocalIdBySync(con, "customers", cs).HasValue;
                        if (!hasPlot)
                            why = id.Substring(0, Math.Min(8, id.Length)) + ": plot parent missing";
                        else if (string.IsNullOrWhiteSpace(cs) && !hasCust)
                            why = id.Substring(0, Math.Min(8, id.Length)) + ": customer null/unresolved";
                        else if (!hasCust)
                            why = id.Substring(0, Math.Min(8, id.Length)) + ": customer not in cloud/local";
                        else
                            why = id.Substring(0, Math.Min(8, id.Length)) + ": insert failed";
                    }
                    if (!skipList.Exists(s => s.StartsWith(id.Substring(0, Math.Min(8, id.Length)), StringComparison.OrdinalIgnoreCase)))
                        skipList.Add(why);
                }
            }
            catch { }

            if (skipList.Count > 0)
            {
                var uniq = new List<string>();
                foreach (string s in skipList)
                {
                    if (!uniq.Contains(s)) uniq.Add(s);
                    if (uniq.Count >= 6) break;
                }
                skipReasons = string.Join("; ", uniq);
            }
        }

        /// <summary>
        /// Force Recover only: plot chain + real cloud customer (incl. soft-deleted).
        /// NO placeholder — orphan / null-customer allocates removed from list (hisab safe).
        /// </summary>
        private static void EnsureRecoverParentsForCustomerPlots(
            List<Dictionary<string, object>> cloudRows, List<string> skipList)
        {
            if (cloudRows == null || cloudRows.Count == 0) return;
            var keep = new List<Dictionary<string, object>>();
            foreach (var row in cloudRows)
            {
                try
                {
                    string allocSync = Convert.ToString(row.ContainsKey("sync_id") ? row["sync_id"] : "");
                    string plotSync = Convert.ToString(row.ContainsKey("plot_sync_id") ? row["plot_sync_id"] : "");
                    string custSync = Convert.ToString(row.ContainsKey("customer_sync_id") ? row["customer_sync_id"] : "");
                    string planSync = Convert.ToString(row.ContainsKey("plan_sync_id") ? row["plan_sync_id"] : "");
                    string brokerSync = Convert.ToString(row.ContainsKey("broker_sync_id") ? row["broker_sync_id"] : "");
                    string shortId = allocSync.Length >= 8 ? allocSync.Substring(0, 8) : allocSync;

                    if (!string.IsNullOrWhiteSpace(custSync)
                        && custSync.Trim().StartsWith("recover_", StringComparison.OrdinalIgnoreCase))
                    {
                        skipList.Add(shortId + ": skip — placeholder customer_sync_id");
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(plotSync))
                        EnsureRecoverPlotChain(plotSync);
                    if (!string.IsNullOrWhiteSpace(planSync))
                        FetchAndApplyMissingRows("plans", new List<string> { planSync }, true);
                    if (!string.IsNullOrWhiteSpace(brokerSync))
                        FetchAndApplyMissingRows("brokers", new List<string> { brokerSync }, true);
                    if (!string.IsNullOrWhiteSpace(custSync))
                        FetchAndApplyMissingRows("customers", new List<string> { custSync }, true);

                    using (var con = new SQLiteConnection(MainClass.con_string))
                    {
                        con.Open();
                        long? plotId = string.IsNullOrWhiteSpace(plotSync)
                            ? null
                            : LocalIdBySync(con, "plots", plotSync);
                        long? custId = string.IsNullOrWhiteSpace(custSync)
                            ? null
                            : LocalIdBySync(con, "customers", custSync);

                        if (!plotId.HasValue)
                        {
                            skipList.Add(shortId + ": plot parent missing");
                            SavePendingApply("customer_plots", row, "plots", plotSync);
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(custSync) || !custId.HasValue)
                        {
                            skipList.Add(shortId + ": skip — no valid cloud customer");
                            SavePendingApply("customer_plots", row, "customers", custSync);
                            continue;
                        }

                        if (IsLocalRecoverPlaceholderCustomer(con, custId.Value))
                        {
                            skipList.Add(shortId + ": skip — Recovered placeholder customer");
                            continue;
                        }

                        keep.Add(row);
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        string id = Convert.ToString(row.ContainsKey("sync_id") ? row["sync_id"] : "?");
                        skipList.Add((id.Length >= 8 ? id.Substring(0, 8) : id) + ": " + ex.Message);
                    }
                    catch { }
                }
            }
            cloudRows.Clear();
            cloudRows.AddRange(keep);
        }

        private static void FilterOutRecoverPlaceholderCustomers(
            List<Dictionary<string, object>> rows, List<string> skipList)
        {
            if (rows == null || rows.Count == 0) return;
            var keep = new List<Dictionary<string, object>>();
            foreach (var row in rows)
            {
                if (IsForceRecoverPlaceholderCustomerPayload(row))
                {
                    string id = Convert.ToString(row.ContainsKey("sync_id") ? row["sync_id"] : "?");
                    skipList.Add((id.Length >= 8 ? id.Substring(0, 8) : id) + ": skip — Recovered placeholder");
                    continue;
                }
                keep.Add(row);
            }
            rows.Clear();
            rows.AddRange(keep);
        }

        private static void FilterRecoverChildrenToExistingAllocs(
            string table, List<Dictionary<string, object>> rows, List<string> skipList)
        {
            if (rows == null || rows.Count == 0) return;
            var keep = new List<Dictionary<string, object>>();
            using (var con = new SQLiteConnection(MainClass.con_string))
            {
                con.Open();
                foreach (var row in rows)
                {
                    string id = Convert.ToString(row.ContainsKey("sync_id") ? row["sync_id"] : "");
                    string cpSync = Convert.ToString(
                        row.ContainsKey("customer_plot_sync_id") ? row["customer_plot_sync_id"] : "");
                    string shortId = id.Length >= 8 ? id.Substring(0, 8) : id;
                    if (string.IsNullOrWhiteSpace(cpSync)
                        || !LocalIdBySync(con, "customer_plots", cpSync.Trim()).HasValue)
                    {
                        skipList.Add(shortId + ": skip — parent allocate missing (" + table + ")");
                        SavePendingApply(table, row, "customer_plots", cpSync);
                        continue;
                    }
                    keep.Add(row);
                }
            }
            rows.Clear();
            rows.AddRange(keep);
        }

        private static bool IsForceRecoverPlaceholderCustomerPayload(Dictionary<string, object> row)
        {
            if (row == null) return false;
            string sid = Convert.ToString(row.ContainsKey("sync_id") ? row["sync_id"] : "") ?? "";
            string name = Convert.ToString(row.ContainsKey("name") ? row["name"] : "") ?? "";
            string notes = Convert.ToString(row.ContainsKey("notes") ? row["notes"] : "") ?? "";
            if (sid.Trim().StartsWith("recover_", StringComparison.OrdinalIgnoreCase)) return true;
            if (notes.IndexOf("force-recover", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (name.StartsWith("Recovered (", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static bool IsLocalRecoverPlaceholderCustomer(SQLiteConnection con, long customerId)
        {
            try
            {
                using (var cmd = new SQLiteCommand(
                    "SELECT name, notes, sync_id FROM customers WHERE id=@id LIMIT 1;", con))
                {
                    cmd.Parameters.AddWithValue("@id", customerId);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read()) return true;
                        string name = Convert.ToString(r["name"] ?? "");
                        string notes = Convert.ToString(r["notes"] ?? "");
                        string sid = Convert.ToString(r["sync_id"] ?? "");
                        if (sid.Trim().StartsWith("recover_", StringComparison.OrdinalIgnoreCase)) return true;
                        if (notes.IndexOf("force-recover", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                        if (name.StartsWith("Recovered (", StringComparison.OrdinalIgnoreCase)) return true;
                    }
                }
            }
            catch { return true; }
            return false;
        }

        private static void EnsureRecoverPlotChain(string plotSyncId)
        {
            if (string.IsNullOrWhiteSpace(plotSyncId)) return;
            using (var con = new SQLiteConnection(MainClass.con_string))
            {
                con.Open();
                if (LocalIdBySync(con, "plots", plotSyncId).HasValue) return;
            }
            // Include soft-deleted plot if Active alloc still points at it
            var plots = HttpGetList("/rest/v1/plots?sync_id=eq." + Uri.EscapeDataString(plotSyncId.Trim()));
            if (plots == null || plots.Count == 0) return;
            var plot = plots[0];
            plot["is_deleted"] = false; // soft-deleted plot still needed as FK parent
            string societySync = Convert.ToString(plot.ContainsKey("society_sync_id") ? plot["society_sync_id"] : "");
            string blockSync = Convert.ToString(plot.ContainsKey("block_sync_id") ? plot["block_sync_id"] : "");
            string typeSync = Convert.ToString(plot.ContainsKey("plot_type_sync_id") ? plot["plot_type_sync_id"] : "");
            if (!string.IsNullOrWhiteSpace(societySync))
                FetchAndApplyMissingRows("societies", new List<string> { societySync }, true);
            if (!string.IsNullOrWhiteSpace(blockSync))
                FetchAndApplyMissingRows("blocks", new List<string> { blockSync }, true);
            if (!string.IsNullOrWhiteSpace(typeSync))
                FetchAndApplyMissingRows("plot_types", new List<string> { typeSync }, true);
            ApplyFetched(AsApplyRows("plots", plots));
        }

        private static void FetchAndApplyMissingRows(string table, List<string> syncIds, bool includeDeleted)
        {
            if (syncIds == null || syncIds.Count == 0) return;
            if (string.Equals(table, "customers", StringComparison.OrdinalIgnoreCase))
            {
                syncIds = syncIds.FindAll(s =>
                    !string.IsNullOrWhiteSpace(s)
                    && !s.Trim().StartsWith("recover_", StringComparison.OrdinalIgnoreCase));
                if (syncIds.Count == 0) return;
            }
            var need = new List<string>();
            using (var con = new SQLiteConnection(MainClass.con_string))
            {
                con.Open();
                foreach (string id in syncIds)
                {
                    if (string.IsNullOrWhiteSpace(id)) continue;
                    if (!LocalIdBySync(con, table, id.Trim()).HasValue)
                        need.Add(id.Trim());
                }
            }
            if (need.Count == 0) return;
            for (int i = 0; i < need.Count; i += 30)
            {
                int n = Math.Min(30, need.Count - i);
                var sb = new StringBuilder();
                for (int j = 0; j < n; j++)
                {
                    if (j > 0) sb.Append(',');
                    sb.Append(need[i + j]);
                }
                string q = "/rest/v1/" + CloudTable(table) + "?sync_id=in.(" + sb + ")";
                if (!includeDeleted) q += "&is_deleted=eq.false";
                var fetched = HttpGetList(q);
                if (fetched != null && fetched.Count > 0)
                {
                    if (string.Equals(table, "customers", StringComparison.OrdinalIgnoreCase))
                    {
                        var skip = new List<string>();
                        FilterOutRecoverPlaceholderCustomers(fetched, skip);
                        if (fetched.Count == 0) continue;
                    }
                    // Soft-deleted PLOTS may be needed as FK; deleted ALLOCATIONS must NOT resurrect.
                    if (includeDeleted)
                    {
                        if (string.Equals(table, "customer_plots", StringComparison.OrdinalIgnoreCase))
                        {
                            // Keep is_deleted as-is — ApplyFetched tombstone removes local Active
                        }
                        else
                        {
                            foreach (var r in fetched)
                                r["is_deleted"] = false;
                        }
                    }
                    ApplyFetched(AsApplyRows(table, fetched));
                }
            }
        }

        /// <summary>DISABLED — Recovered placeholders caused bad hisab. No-op for safety.</summary>
        private static long EnsureRecoverPlaceholderCustomer(
            SQLiteConnection con, string customerSyncId, string allocSyncId)
        {
            return 0;
        }

        /// <summary>Isi table ke sync_id only — alias se missing hide nahi.</summary>
        private static HashSet<string> LocalSyncIdsForTable(SQLiteConnection con, string table)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!TableExists(con, table)) return ids;
                using (var cmd = new SQLiteCommand(
                    "SELECT sync_id FROM \"" + table + "\" WHERE sync_id IS NOT NULL AND TRIM(sync_id)<>'';", con))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        string id = Convert.ToString(r[0] ?? "");
                        if (!string.IsNullOrWhiteSpace(id)) ids.Add(id);
                    }
                }
            }
            catch { }
            return ids;
        }

        /// <summary>Auto tick chalne do finish — max waitSec. CompareExchange se lock lo.</summary>
        private static bool WaitForSyncLock(int waitSec)
        {
            int max = Math.Max(10, waitSec) * 4; // 250ms steps
            for (int i = 0; i < max; i++)
            {
                if (Interlocked.CompareExchange(ref _busy, 1, 0) == 0)
                    return true;
                Thread.Sleep(250);
            }
            return false;
        }

        /// <summary>Offline delete queue — internet aane par SoftMark retry.</summary>
        private static void QueuePendingDelete(string localTable, string syncId)
        {
            string sid = (syncId ?? "").Trim();
            string table = (localTable ?? "").Trim();
            if (sid.Length == 0 || table.Length == 0) return;
            try
            {
                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    using (var cmd = new SQLiteCommand(
                        "INSERT OR REPLACE INTO sync_pending_delete(sync_id, table_name) VALUES(@s,@t);", con))
                    {
                        cmd.Parameters.AddWithValue("@s", sid);
                        cmd.Parameters.AddWithValue("@t", table);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
            MarkDirty();
        }

        /// <summary>Local DELETE ke saath same transaction mein pending_delete likho.</summary>
        public static void QueuePendingDeleteIds(
            SQLiteConnection con, SQLiteTransaction tx, string table, System.Collections.IEnumerable syncIds)
        {
            if (con == null || syncIds == null || string.IsNullOrWhiteSpace(table)) return;
            foreach (object o in syncIds)
            {
                string sid = Convert.ToString(o ?? "").Trim();
                if (sid.Length == 0) continue;
                using (var cmd = new SQLiteCommand(
                    "INSERT OR REPLACE INTO sync_pending_delete(sync_id, table_name) VALUES(@s,@t);", con, tx))
                {
                    cmd.Parameters.AddWithValue("@s", sid);
                    cmd.Parameters.AddWithValue("@t", table.Trim());
                    cmd.ExecuteNonQuery();
                }
            }
            MarkDirty();
        }

        /// <summary>Cascade items ke sync_ids pending_delete mein (HTTP nahi).</summary>
        public static void QueuePendingDeletesForCascade(
            SQLiteConnection con, SQLiteTransaction tx, IEnumerable<CloudDeleteItem> items)
        {
            if (con == null || items == null) return;
            foreach (var i in items)
            {
                if (i == null || i.SyncIds == null || i.SyncIds.Count == 0) continue;
                QueuePendingDeleteIds(con, tx, i.Table, i.SyncIds);
            }
        }

        /// <summary>Pending pehle txn mein likha — ab sirf SoftMark HTTP (bahar).</summary>
        public static void SoftMarkQueuedCascade(IEnumerable<CloudDeleteItem> items)
        {
            if (items == null) return;
            foreach (var i in items)
            {
                if (i == null || i.SyncIds == null) continue;
                foreach (string sid in i.SyncIds)
                {
                    if (string.IsNullOrWhiteSpace(sid)) continue;
                    try
                    {
                        if (SoftMarkCloudDeleted(i.Table, sid))
                            RemovePendingDelete(sid);
                    }
                    catch { }
                }
            }
            try { ScheduleQuickSync(); } catch { }
        }

        private static void RemovePendingDelete(string syncId)
        {
            if (string.IsNullOrWhiteSpace(syncId)) return;
            try
            {
                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    using (var cmd = new SQLiteCommand(
                        "DELETE FROM sync_pending_delete WHERE sync_id=@s;", con))
                    {
                        cmd.Parameters.AddWithValue("@s", syncId.Trim());
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        private static bool HasPendingDeletes()
        {
            try
            {
                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT 1 FROM sync_pending_delete LIMIT 1;", con))
                    {
                        return cmd.ExecuteScalar() != null;
                    }
                }
            }
            catch { return false; }
        }

        private static HashSet<string> LoadPendingDeleteSyncIds()
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT sync_id FROM sync_pending_delete;", con))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            string s = Convert.ToString(r[0] ?? "").Trim();
                            if (!string.IsNullOrWhiteSpace(s)) ids.Add(s);
                        }
                    }
                }
            }
            catch { }
            return ids;
        }

        /// <summary>Queued deletes — soft-mark tombstone only (no hard-delete; other devices need is_deleted=true).</summary>
        private static bool PushPendingDeletes()
        {
            if (!SupabaseConfig.IsConfigured) return !HasPendingDeletes();
            var pending = new List<Tuple<string, string, string>>();
            try
            {
                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT table_name, sync_id, queued_at FROM sync_pending_delete ORDER BY queued_at ASC;", con))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            string t = Convert.ToString(r[0] ?? "").Trim();
                            string s = Convert.ToString(r[1] ?? "").Trim();
                            string q = Convert.ToString(r[2] ?? "").Trim();
                            if (t.Length > 0 && s.Length > 0)
                                pending.Add(Tuple.Create(t, s, q));
                        }
                    }
                }
            }
            catch { return false; }
            if (pending.Count == 0) return true;
            bool allOk = true;
            foreach (var p in pending)
            {
                // Soft tombstone only — hard delete se mobile/PC delete signal kho jata hai
                if (SoftMarkCloudDeleted(p.Item1, p.Item2))
                    RemovePendingDelete(p.Item2);
                else
                    allOk = false;
            }
            return allOk;
        }

        private static bool PendingDeleteAged(string queuedAt, int minSec)
        {
            if (string.IsNullOrWhiteSpace(queuedAt)) return true;
            DateTime q;
            if (!DateTime.TryParse(queuedAt, out q)) return true;
            if (q.Kind == DateTimeKind.Unspecified)
                q = DateTime.SpecifyKind(q, DateTimeKind.Local);
            return (DateTime.Now - q.ToLocalTime()).TotalSeconds >= minSec;
        }

        /// Local save/delete ke baad: poora tick (PULL pehle, phir PUSH) — sirf push se mobile slow rehta tha.
        private static void ScheduleQuickSync()
        {
            if (!SupabaseConfig.IsConfigured) return;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try { KickTick(); }
                catch { }
            });
        }

        private static void KickTick()
        {
            if (Interlocked.CompareExchange(ref _manualHold, 0, 0) != 0) return;
            if (Interlocked.Exchange(ref _busy, 1) == 1)
            {
                Interlocked.Exchange(ref _tickAgain, 1);
                return;
            }
            try { TickSafeCore(); }
            catch { }
            finally { Interlocked.Exchange(ref _busy, 0); }
            if (Interlocked.Exchange(ref _tickAgain, 0) != 0)
                ThreadPool.QueueUserWorkItem(__ => { try { KickTick(); } catch { } });
        }

        public static void ScheduleBackgroundPushPublic()
        {
            ScheduleQuickSync();
        }

        public static void PushOne(string table, long rowId)
        {
            MarkDirtyTable(table);
            if (!PushOneNow(table, rowId))
                MarkDirtyTable(table);
            else
                ClearDirty();
            ScheduleQuickSync();
        }

        private static void DrainOutbox()
        {
            var jobs = new List<Tuple<string, long, string>>();
            try
            {
                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    if (!TableExists(con, "sync_outbox")) return;
                    using (var cmd = new SQLiteCommand(
                        "SELECT table_name, local_pk, revision FROM sync_outbox ORDER BY id ASC LIMIT 40;", con))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            long pk;
                            if (!long.TryParse(Convert.ToString(r[1] ?? ""), out pk) || pk <= 0) continue;
                            jobs.Add(Tuple.Create(Convert.ToString(r[0] ?? ""), pk, Convert.ToString(r[2] ?? "")));
                        }
                    }
                }
            }
            catch { return; }
            foreach (var j in jobs)
                PushOneNow(j.Item1, j.Item2, j.Item3);
        }

        /// <summary>Local save ke saath outbox — revision se late ACK purani edit clear nahi kare.</summary>
        public static string EnqueueOutbox(string table, long rowId, string syncId, string op = "upsert")
        {
            string rev = Guid.NewGuid().ToString("N");
            try
            {
                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    return EnqueueOutboxOnConnection(con, null, table, rowId, syncId, op, rev);
                }
            }
            catch { return rev; }
        }

        /// <summary>Customer INSERT/UPDATE ke saath SAME SQLite transaction mein outbox.</summary>
        public static string EnqueueOutboxOnConnection(
            SQLiteConnection con, SQLiteTransaction tx,
            string table, long rowId, string syncId, string op = "upsert", string revision = null)
        {
            string rev = string.IsNullOrWhiteSpace(revision) ? Guid.NewGuid().ToString("N") : revision;
            if (con == null || rowId <= 0 || string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(syncId))
                return rev;
            using (var cmd = tx == null
                ? new SQLiteCommand(
                    "INSERT INTO sync_outbox(table_name, local_pk, sync_id, revision, op) VALUES(@t,@p,@s,@r,@o);", con)
                : new SQLiteCommand(
                    "INSERT INTO sync_outbox(table_name, local_pk, sync_id, revision, op) VALUES(@t,@p,@s,@r,@o);", con, tx))
            {
                cmd.Parameters.AddWithValue("@t", table);
                cmd.Parameters.AddWithValue("@p", rowId);
                cmd.Parameters.AddWithValue("@s", syncId.Trim());
                cmd.Parameters.AddWithValue("@r", rev);
                cmd.Parameters.AddWithValue("@o", op ?? "upsert");
                cmd.ExecuteNonQuery();
            }
            return rev;
        }

        /// <summary>Existing non-empty sync_id preserve; empty pe generate. Same connection/tx.</summary>
        public static string EnsureSyncIdOnConnection(
            SQLiteConnection con, SQLiteTransaction tx, string table, long rowId)
        {
            if (con == null || rowId <= 0 || string.IsNullOrWhiteSpace(table)) return "";
            string pk = Pk(table);
            string sid = "";
            using (var q = tx == null
                ? new SQLiteCommand("SELECT sync_id FROM \"" + table + "\" WHERE \"" + pk + "\"=@id LIMIT 1;", con)
                : new SQLiteCommand("SELECT sync_id FROM \"" + table + "\" WHERE \"" + pk + "\"=@id LIMIT 1;", con, tx))
            {
                q.Parameters.AddWithValue("@id", rowId);
                sid = Convert.ToString(q.ExecuteScalar() ?? "").Trim();
            }
            if (!string.IsNullOrWhiteSpace(sid)) return sid;
            using (var u = tx == null
                ? new SQLiteCommand(
                    "UPDATE \"" + table + "\" SET sync_id=lower(hex(randomblob(16))) WHERE \"" + pk + "\"=@id AND (sync_id IS NULL OR TRIM(sync_id)='');", con)
                : new SQLiteCommand(
                    "UPDATE \"" + table + "\" SET sync_id=lower(hex(randomblob(16))) WHERE \"" + pk + "\"=@id AND (sync_id IS NULL OR TRIM(sync_id)='');", con, tx))
            {
                u.Parameters.AddWithValue("@id", rowId);
                u.ExecuteNonQuery();
            }
            using (var q2 = tx == null
                ? new SQLiteCommand("SELECT sync_id FROM \"" + table + "\" WHERE \"" + pk + "\"=@id LIMIT 1;", con)
                : new SQLiteCommand("SELECT sync_id FROM \"" + table + "\" WHERE \"" + pk + "\"=@id LIMIT 1;", con, tx))
            {
                q2.Parameters.AddWithValue("@id", rowId);
                return Convert.ToString(q2.ExecuteScalar() ?? "").Trim();
            }
        }

        private static void ClearOutboxRevision(string revision)
        {
            if (string.IsNullOrWhiteSpace(revision)) return;
            try
            {
                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    using (var cmd = new SQLiteCommand(
                        "DELETE FROM sync_outbox WHERE revision=@r;", con))
                    {
                        cmd.Parameters.AddWithValue("@r", revision);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        private static bool IsPendingDeleteSyncId(string syncId)
        {
            if (string.IsNullOrWhiteSpace(syncId)) return false;
            try
            {
                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT 1 FROM sync_pending_delete WHERE sync_id=@s LIMIT 1;", con))
                    {
                        cmd.Parameters.AddWithValue("@s", syncId.Trim());
                        return cmd.ExecuteScalar() != null;
                    }
                }
            }
            catch { return false; }
        }

        /// <summary>1 row turant cloud. Non-empty sync_id kabhi regenerate nahi. Deleted resurrect nahi.</summary>
        public static bool PushOneNow(string table, long rowId)
        {
            return PushOneNow(table, rowId, null);
        }

        public static bool PushOneNow(string table, long rowId, string outboxRevision)
        {
            if (rowId <= 0 || string.IsNullOrWhiteSpace(table)) return false;
            if (table.IndexOf("staff_entry_log", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (table.StartsWith("sync_", StringComparison.OrdinalIgnoreCase)) return true;
            if (!SupabaseConfig.IsConfigured) return false;
            try
            {
                // Allocate/receipt: pehle parents — success check (missing parent = child outbox mein rahe)
                if (string.Equals(table, "customer_plots", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        using (var con = new SQLiteConnection(MainClass.con_string))
                        {
                            con.Open();
                            using (var cmd = new SQLiteCommand(
                                "SELECT customer_id, plot_id, broker_id FROM customer_plots WHERE id=@id LIMIT 1;", con))
                            {
                                cmd.Parameters.AddWithValue("@id", rowId);
                                using (var r = cmd.ExecuteReader())
                                {
                                    if (r.Read())
                                    {
                                        long cid = 0, pid = 0, bid = 0;
                                        try { cid = Convert.ToInt64(r["customer_id"] ?? 0); } catch { }
                                        try { pid = Convert.ToInt64(r["plot_id"] ?? 0); } catch { }
                                        try { if (r["broker_id"] != DBNull.Value) bid = Convert.ToInt64(r["broker_id"]); } catch { }
                                        if (cid > 0 && !PushOneNow("customers", cid))
                                        { MarkDirtyTable(table); return false; }
                                        if (pid > 0 && !PushOneNow("plots", pid))
                                        { MarkDirtyTable(table); return false; }
                                        if (bid > 0) PushOneNow("brokers", bid);
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
                else if (string.Equals(table, "receipts", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(table, "ledger", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        using (var con = new SQLiteConnection(MainClass.con_string))
                        {
                            con.Open();
                            using (var cmd = new SQLiteCommand(
                                "SELECT customer_plot_id FROM \"" + table + "\" WHERE id=@id LIMIT 1;", con))
                            {
                                cmd.Parameters.AddWithValue("@id", rowId);
                                object o = cmd.ExecuteScalar();
                                long allocId;
                                if (o != null && o != DBNull.Value
                                    && long.TryParse(Convert.ToString(o), out allocId) && allocId > 0)
                                {
                                    if (!PushOneNow("customer_plots", allocId))
                                    { MarkDirtyTable(table); return false; }
                                }
                            }
                        }
                    }
                    catch { }
                }
                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    if (!TableExists(con, table)) return false;
                    string pk = Pk(table);
                    // SIRF empty sync_id — existing ID kabhi mat chhedo
                    using (var fix = new SQLiteCommand(
                        "UPDATE \"" + table + "\" SET sync_id = lower(hex(randomblob(16))) WHERE \"" + pk + "\"=@id AND (sync_id IS NULL OR TRIM(sync_id)='');", con))
                    {
                        fix.Parameters.AddWithValue("@id", rowId);
                        fix.ExecuteNonQuery();
                    }
                    Dictionary<string, object> payload = null;
                    using (var cmd = new SQLiteCommand("SELECT * FROM \"" + table + "\" WHERE \"" + pk + "\"=@id LIMIT 1;", con))
                    {
                        cmd.Parameters.AddWithValue("@id", rowId);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (!r.Read()) return false;
                            payload = ReadPayload(r);
                        }
                    }
                    string syncId = Convert.ToString(payload.ContainsKey("sync_id") ? payload["sync_id"] : "").Trim();
                    if (string.IsNullOrWhiteSpace(syncId)) return false;
                    // Local pending delete / cloud tombstone — live upsert se resurrect mat karo
                    if (IsPendingDeleteSyncId(syncId)) return true;
                    var cloudDeleted = FetchCloudDeletedSyncIds(CloudTable(table), new List<string> { syncId });
                    if (cloudDeleted.Contains(syncId)) return true;

                    Dictionary<string, string> fks;
                    if (Fks.TryGetValue(table, out fks))
                    {
                        foreach (var kv in fks)
                        {
                            object fkVal;
                            if (!payload.TryGetValue(kv.Key, out fkVal) || fkVal == null) continue;
                            string parentSync = LookupSyncId(con, kv.Value, fkVal);
                            if (!string.IsNullOrEmpty(parentSync))
                                payload["__fk_" + kv.Key] = parentSync;
                        }
                    }
                    string staff = (MainClass.LoggedInName ?? MainClass.LoggedInUsername ?? "").Trim();
                    string hash = RowHash(payload);
                    string dupKey = DupKey(table, payload);
                    var cloudRow = LocalToCloudRow(table, payload, con);
                    cloudRow["dup_key"] = dupKey ?? "";
                    cloudRow["is_deleted"] = false;
                    cloudRow["staff_name"] = staff;
                    cloudRow["updated_at"] = UtcFilter(DateTime.UtcNow);
                    string cloud = CloudTable(table);
                    var rows = new List<Dictionary<string, object>> { cloudRow };
                    if (!UpsertCloudFast(cloud, rows) && !UpsertCloud(cloud, rows))
                    {
                        MarkDirtyTable(table);
                        return false;
                    }
                    // Race: upsert ke baad phir check — delete jeet gaya to SoftMark dubara
                    var stillDel = FetchCloudDeletedSyncIds(cloud, new List<string> { syncId });
                    if (stillDel.Contains(syncId))
                    {
                        SoftMarkCloudDeleted(table, syncId);
                        return true;
                    }
                    SaveHash(con, syncId, hash);
                    using (var cmd = new SQLiteCommand(
                        "INSERT OR REPLACE INTO sync_known(sync_id, table_name) VALUES(@s,@t);", con))
                    {
                        cmd.Parameters.AddWithValue("@s", syncId);
                        cmd.Parameters.AddWithValue("@t", table);
                        cmd.ExecuteNonQuery();
                    }
                }
                if (!string.IsNullOrWhiteSpace(outboxRevision))
                    ClearOutboxRevision(outboxRevision);
                try { ProcessPendingApplyQueue(); } catch { }
                RaiseLiveChanged();
                return true;
            }
            catch
            {
                try { MarkDirtyTable(table); } catch { }
                return false;
            }
        }

        internal static string TablePk(string table) { return Pk(table); }

        public static List<string> CollectCloudDeleteIds(string table, long localPk)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (localPk <= 0 || string.IsNullOrWhiteSpace(table)) return new List<string>();
            try
            {
                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    CollectDeleteSyncIds(con, table, localPk, ids);
                }
            }
            catch { }
            return new List<string>(ids);
        }

        public static void SyncCloudDelete(string table, long localPk, List<string> preCollected = null)
        {
            TombstoneLocalRow(table, localPk, preCollected);
        }

        public sealed class CloudDeleteItem
        {
            public string Table;
            public long LocalPk;
            public List<string> SyncIds;
        }

        /// <summary>Allocation delete — linked receipts/ledger/partners ke sync_ids pehle collect.</summary>
        public static List<CloudDeleteItem> CollectAllocationCascade(long allocId)
        {
            var items = new List<CloudDeleteItem>();
            if (allocId <= 0) return items;
            try
            {
                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    CollectChildDeleteItems(con, items, "receipts", "id", "customer_plot_id", allocId);
                    CollectChildDeleteItems(con, items, "ledger", "id", "customer_plot_id", allocId);
                    if (TableExists(con, "customer_plot_partners"))
                        CollectChildDeleteItems(con, items, "customer_plot_partners", "id", "customer_plot_id", allocId);
                    if (TableExists(con, "partner_money"))
                        CollectChildDeleteItems(con, items, "partner_money", "id", "customer_plot_id", allocId);
                    items.Add(new CloudDeleteItem
                    {
                        Table = "customer_plots",
                        LocalPk = allocId,
                        SyncIds = CollectCloudDeleteIds("customer_plots", allocId)
                    });
                }
            }
            catch { }
            return items;
        }

        public static void SyncCloudDeleteMany(IEnumerable<CloudDeleteItem> items)
        {
            if (items == null) return;
            foreach (var i in items)
            {
                try { SyncCloudDelete(i.Table, i.LocalPk, i.SyncIds); }
                catch { }
            }
        }

        /// <summary>Receipt delete — linked partner_money + ledger cloud tombstone.</summary>
        public static List<CloudDeleteItem> CollectReceiptCascade(long receiptId, long? ledgerId = null)
        {
            var items = new List<CloudDeleteItem>();
            if (receiptId <= 0) return items;
            try
            {
                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    CollectChildDeleteItems(con, items, "partner_money", "id", "receipt_id", receiptId);
                    if (ledgerId.HasValue && ledgerId.Value > 0)
                    {
                        items.Add(new CloudDeleteItem
                        {
                            Table = "ledger",
                            LocalPk = ledgerId.Value,
                            SyncIds = CollectCloudDeleteIds("ledger", ledgerId.Value)
                        });
                    }
                    else
                    {
                        try
                        {
                            using (var cmd = new SQLiteCommand(
                                "SELECT ledger_id FROM receipts WHERE id=@id LIMIT 1;", con))
                            {
                                cmd.Parameters.AddWithValue("@id", receiptId);
                                object o = cmd.ExecuteScalar();
                                if (o != null && o != DBNull.Value)
                                {
                                    long lid = Convert.ToInt64(o);
                                    items.Add(new CloudDeleteItem
                                    {
                                        Table = "ledger",
                                        LocalPk = lid,
                                        SyncIds = CollectCloudDeleteIds("ledger", lid)
                                    });
                                }
                            }
                        }
                        catch { }
                    }
                    items.Add(new CloudDeleteItem
                    {
                        Table = "receipts",
                        LocalPk = receiptId,
                        SyncIds = CollectCloudDeleteIds("receipts", receiptId)
                    });
                }
            }
            catch { }
            return items;
        }

        /// <summary>
        /// partner_money: pending_delete (txn mein agar tx ho) + local delete.
        /// SoftMark HTTP hamesha SQLite txn se bahar (last step).
        /// </summary>
        public static void DeletePartnerMoneyWhere(
            SQLiteConnection con, SQLiteTransaction tx, string whereSql, Action<SQLiteCommand> bind,
            bool softMarkCloud = true)
        {
            if (con == null || string.IsNullOrWhiteSpace(whereSql)) return;
            var items = new List<CloudDeleteItem>();
            try
            {
                using (var sel = new SQLiteCommand("SELECT id FROM partner_money WHERE " + whereSql + ";", con, tx))
                {
                    bind?.Invoke(sel);
                    using (var r = sel.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            if (r.IsDBNull(0)) continue;
                            long id = Convert.ToInt64(r.GetValue(0));
                            items.Add(new CloudDeleteItem
                            {
                                Table = "partner_money",
                                LocalPk = id,
                                SyncIds = CollectCloudDeleteIds("partner_money", id)
                            });
                        }
                    }
                }
                if (tx != null)
                    QueuePendingDeletesForCascade(con, tx, items);
                else
                {
                    foreach (var i in items)
                    {
                        if (i.SyncIds == null) continue;
                        foreach (string sid in i.SyncIds)
                            QueuePendingDelete("partner_money", sid);
                    }
                }
                using (var del = new SQLiteCommand("DELETE FROM partner_money WHERE " + whereSql + ";", con, tx))
                {
                    bind?.Invoke(del);
                    del.ExecuteNonQuery();
                }
                try { MarkDirty(); } catch { }
                if (softMarkCloud)
                    SoftMarkQueuedCascade(items);
            }
            catch { }
        }

        private static void CollectChildDeleteItems(
            SQLiteConnection con, List<CloudDeleteItem> items,
            string table, string pkCol, string fkCol, long allocId)
        {
            if (!TableExists(con, table)) return;
            try
            {
                using (var cmd = new SQLiteCommand(
                    "SELECT \"" + pkCol + "\" FROM \"" + table + "\" WHERE \"" + fkCol + "\"=@a;", con))
                {
                    cmd.Parameters.AddWithValue("@a", allocId);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            if (r.IsDBNull(0)) continue;
                            long id = Convert.ToInt64(r.GetValue(0));
                            items.Add(new CloudDeleteItem
                            {
                                Table = table,
                                LocalPk = id,
                                SyncIds = CollectCloudDeleteIds(table, id)
                            });
                        }
                    }
                }
            }
            catch { }
        }

        internal static void CollectDeleteSyncIds(SQLiteConnection con, string table, long localPk, HashSet<string> ids)
        {
            if (ids == null || localPk <= 0) return;
            string pk = Pk(table);
            try
            {
                using (var cmd = new SQLiteCommand(
                    "SELECT sync_id FROM \"" + table + "\" WHERE \"" + pk + "\"=@p LIMIT 1;", con))
                {
                    cmd.Parameters.AddWithValue("@p", localPk);
                    string s = Convert.ToString(cmd.ExecuteScalar() ?? "").Trim();
                    if (!string.IsNullOrWhiteSpace(s)) ids.Add(s);
                }
            }
            catch { }
            try
            {
                using (var cmd = new SQLiteCommand(
                    "SELECT sync_id FROM sync_alias WHERE lower(table_name)=lower(@t) AND local_pk=@p;", con))
                {
                    cmd.Parameters.AddWithValue("@t", table);
                    cmd.Parameters.AddWithValue("@p", localPk);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            string s = Convert.ToString(r[0] ?? "").Trim();
                            if (!string.IsNullOrWhiteSpace(s)) ids.Add(s);
                        }
                    }
                }
            }
            catch { }
        }

        internal static void TombstoneLocalRow(string localTable, long localPk, List<string> preCollected)
        {
            if (string.IsNullOrWhiteSpace(localTable) || localPk <= 0) return;
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (preCollected != null)
            {
                foreach (string s in preCollected)
                    if (!string.IsNullOrWhiteSpace(s)) ids.Add(s.Trim());
            }
            try
            {
                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    CollectDeleteSyncIds(con, localTable, localPk, ids);
                    foreach (string sid in ids)
                    {
                        // Turant hard delete — queue sirf fail par.
                        if (!PushDeletedOne(localTable, sid))
                            QueuePendingDelete(localTable, sid);
                    }
                    try
                    {
                        using (var cmd = new SQLiteCommand(
                            "DELETE FROM sync_alias WHERE lower(table_name)=lower(@t) AND local_pk=@p;", con))
                        {
                            cmd.Parameters.AddWithValue("@t", localTable);
                            cmd.Parameters.AddWithValue("@p", localPk);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { }
                }
            }
            catch { }
            ScheduleQuickSync();
        }

        /// Local delete ke baad cloud row hard delete (Supabase se bilkul khatam).
        public static void PushDeleted(string localTable, string syncId)
        {
            PushDeletedOne(localTable, syncId);
        }

        /// <summary>Soft-delete tombstone for other devices. Hard-delete sync path se band (mobile miss fix).</summary>
        private static bool PushDeletedOne(string localTable, string syncId)
        {
            if (string.IsNullOrWhiteSpace(localTable) || string.IsNullOrWhiteSpace(syncId)) return true;
            QueuePendingDelete(localTable, syncId);
            if (!SupabaseConfig.IsConfigured) return false;
            bool softOk = SoftMarkCloudDeleted(localTable, syncId);
            if (softOk) RemovePendingDelete(syncId);
            // Soft tombstone cloud pe rehti hai — PushPendingDeletes hard delete nahi karta
            try
            {
                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    using (var cmd = new SQLiteCommand("DELETE FROM sync_known WHERE sync_id=@s;", con))
                    {
                        cmd.Parameters.AddWithValue("@s", syncId);
                        cmd.ExecuteNonQuery();
                    }
                    using (var cmd = new SQLiteCommand("DELETE FROM sync_hash WHERE sync_id=@s;", con))
                    {
                        cmd.Parameters.AddWithValue("@s", syncId);
                        cmd.ExecuteNonQuery();
                    }
                    using (var cmd = new SQLiteCommand("DELETE FROM sync_alias WHERE sync_id=@s;", con))
                    {
                        cmd.Parameters.AddWithValue("@s", syncId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
            return softOk;
        }

        /// <summary>
        /// Cloud pe is_deleted=true TOMBSTONE lazmi. Row hard-missing ho to INSERT tombstone —
        /// warna mobile/PC delete signal nahi milta (absence pe local delete band hai).
        /// </summary>
        private static bool SoftMarkCloudDeleted(string localTable, string syncId)
        {
            if (string.IsNullOrWhiteSpace(localTable) || string.IsNullOrWhiteSpace(syncId)) return true;
            string sid = syncId.Trim();
            string cloud = CloudTable(localTable);
            string patchJson = Json.Serialize(new Dictionary<string, object>
            {
                { "is_deleted", true },
                { "updated_at", UtcFilter(DateTime.UtcNow) }
            });
            // Fast PATCH, phir slow Http — mobile delete signal miss na ho
            foreach (HttpClient client in new[] { HttpFast, Http })
            {
                try
                {
                    string url = SupabaseConfig.Url.TrimEnd('/') + "/rest/v1/" + cloud
                        + "?sync_id=eq." + Uri.EscapeDataString(sid);
                    var req = new HttpRequestMessage(new HttpMethod("PATCH"), url);
                    ApplyHeaders(req);
                    req.Headers.TryAddWithoutValidation("Prefer", "return=representation");
                    req.Content = new StringContent(patchJson, Encoding.UTF8, "application/json");
                    using (var resp = client.SendAsync(req).GetAwaiter().GetResult())
                    {
                        if (resp.IsSuccessStatusCode)
                        {
                            string respBody = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult() ?? "";
                            if (respBody.IndexOf("\"is_deleted\":true", StringComparison.OrdinalIgnoreCase) >= 0
                                || respBody.IndexOf("\"is_deleted\": true", StringComparison.OrdinalIgnoreCase) >= 0)
                                return true;
                        }
                    }
                }
                catch { }
            }
            try
            {
                var check = HttpGetListOk("/rest/v1/" + cloud
                    + "?sync_id=eq." + Uri.EscapeDataString(sid)
                    + "&select=sync_id,is_deleted&limit=1", 4);
                if (check != null && check.Count > 0 && CloudRowIsDeleted(check[0]))
                    return true;
            }
            catch { }
            // Tombstone upsert (row missing ya PATCH no-op) — mobile prune isi pe chalta hai
            try
            {
                var tomb = BuildDeleteTombstone(localTable, sid);
                EnrichTombstoneFromLocal(localTable, sid, tomb);
                tomb["updated_at"] = UtcFilter(DateTime.UtcNow);
                if (!UpsertCloudFast(cloud, new List<Dictionary<string, object>> { tomb })
                    && !UpsertCloud(cloud, new List<Dictionary<string, object>> { tomb }))
                    return false;
                var check2 = HttpGetListOk("/rest/v1/" + cloud
                    + "?sync_id=eq." + Uri.EscapeDataString(sid)
                    + "&select=sync_id,is_deleted&limit=1", 4);
                return check2 != null && check2.Count > 0 && CloudRowIsDeleted(check2[0]);
            }
            catch { return false; }
        }

        /// <summary>Minimal cloud row so is_deleted tombstone insert NOT NULL fail na kare.</summary>
        private static Dictionary<string, object> BuildDeleteTombstone(string localTable, string syncId)
        {
            var tomb = new Dictionary<string, object>
            {
                { "sync_id", syncId },
                { "is_deleted", true },
                { "updated_at", DateTime.UtcNow.ToString("o") },
                { "staff_name", (MainClass.LoggedInName ?? MainClass.LoggedInUsername ?? "system").Trim() }
            };
            string t = (localTable ?? "").Trim();
            if (string.Equals(t, "customer_plots", StringComparison.OrdinalIgnoreCase))
            {
                tomb["booking_date"] = DateTime.Today.ToString("yyyy-MM-dd");
                tomb["price"] = 0;
                tomb["down_payment"] = 0;
                tomb["status"] = "Cancelled";
                tomb["deal_type"] = "Installment";
                tomb["notes"] = "";
            }
            else if (string.Equals(t, "customers", StringComparison.OrdinalIgnoreCase))
            {
                tomb["name"] = "(deleted)";
                tomb["mobile"] = "";
                tomb["cnic"] = "";
            }
            else if (string.Equals(t, "receipts", StringComparison.OrdinalIgnoreCase))
            {
                tomb["receipt_date"] = DateTime.Today.ToString("yyyy-MM-dd");
                tomb["amount"] = 0;
                tomb["mode"] = "Cash";
            }
            else if (string.Equals(t, "ledger", StringComparison.OrdinalIgnoreCase))
            {
                tomb["txn_date"] = DateTime.Today.ToString("yyyy-MM-dd");
                tomb["txn_type"] = "Adjustment";
                tomb["debit"] = 0;
                tomb["credit"] = 0;
            }
            return tomb;
        }

        /// <summary>Local row abhi maujood ho to FK sync_ids tombstone mein daalo (allocate delete pehle cloud).</summary>
        private static void EnrichTombstoneFromLocal(string localTable, string syncId, Dictionary<string, object> tomb)
        {
            if (tomb == null || string.IsNullOrWhiteSpace(localTable) || string.IsNullOrWhiteSpace(syncId)) return;
            try
            {
                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    if (!TableExists(con, localTable)) return;
                    using (var cmd = new SQLiteCommand(
                        "SELECT * FROM \"" + localTable + "\" WHERE sync_id=@s LIMIT 1;", con))
                    {
                        cmd.Parameters.AddWithValue("@s", syncId);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (!r.Read()) return;
                            var payload = ReadPayload(r);
                            Dictionary<string, string> fks;
                            if (!Fks.TryGetValue(localTable, out fks) || fks == null) return;
                            foreach (var kv in fks)
                            {
                                object fkVal;
                                if (!payload.TryGetValue(kv.Key, out fkVal) || fkVal == null) continue;
                                string parent = LookupSyncId(con, kv.Value, fkVal);
                                if (!string.IsNullOrEmpty(parent))
                                    tomb[FkCloudCol(kv.Key)] = parent;
                            }
                            if (payload.ContainsKey("booking_date") && payload["booking_date"] != null)
                                tomb["booking_date"] = Convert.ToString(payload["booking_date"]);
                            if (payload.ContainsKey("status") && payload["status"] != null)
                                tomb["status"] = Convert.ToString(payload["status"]);
                            if (payload.ContainsKey("price") && payload["price"] != null)
                                tomb["price"] = payload["price"];
                        }
                    }
                }
            }
            catch { }
        }

        private static bool CloudRowIsDeleted(Dictionary<string, object> row)
        {
            if (row == null) return false;
            object del;
            if (!row.TryGetValue("is_deleted", out del) || del == null) return false;
            if (del is bool) return (bool)del;
            string ds = Convert.ToString(del);
            return string.Equals(ds, "true", StringComparison.OrdinalIgnoreCase) || ds == "1";
        }

        /// <summary>Sync tables pe hard DELETE band — hamesha soft tombstone (mobile delete signal).</summary>
        private static bool HardDeleteCloudRow(string localTable, string syncId)
        {
            return SoftMarkCloudDeleted(localTable, syncId);
        }

        /// Cloud login hard delete — username wale live users Supabase se hatao.
        public static void PushDeletedUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return;
            try
            {
                if (!SupabaseConfig.IsConfigured) return;
                string url = SupabaseConfig.Url.TrimEnd('/') + "/rest/v1/users?username=ilike."
                    + Uri.EscapeDataString(username.Trim());
                var req = new HttpRequestMessage(HttpMethod.Delete, url);
                ApplyHeaders(req);
                req.Headers.TryAddWithoutValidation("Prefer", "return=minimal");
                using (var resp = Http.SendAsync(req).GetAwaiter().GetResult()) { }
            }
            catch { }
        }

        public static void Start()
        {
            try
            {
                SupabaseConfig.Load();
                EnsureLocalSchema();
                if (_timer != null) return;
                _timer = new System.Windows.Forms.Timer { Interval = 400 };
                _timer.Tick += async (s, e) =>
                {
                    try { await System.Threading.Tasks.Task.Run((Action)TickSafe); }
                    catch { }
                };
                _timer.Start();
                ThreadPool.QueueUserWorkItem(_ => TickSafe());
            }
            catch { }
        }

        private static void TickSafe()
        {
            KickTick();
        }

        private static void TickSafeCore()
        {
            try
            {
                if (!SupabaseConfig.IsConfigured) return;
                EnsureMissingSyncIds();
                bool restoreFirst = GetState("restore_push_first") == "1";
                if (restoreFirst && CanPush())
                {
                    PushPendingDeletes();
                    if (PushChanged())
                    {
                        _dirty = false;
                        SetState("restore_push_first", "");
                    }
                }
                if (NeedsFullPull())
                    FullPullAllTables();
                else
                {
                    // FAST PATH (~sub-second):
                    // 1) Local dirty PUSH pehle — WinForms→mobile cloud pe pehle pahunche
                    if (CanPush())
                    {
                        PushPendingDeletes();
                        try { DrainOutbox(); } catch { }
                        if (PushChanged())
                        {
                            _dirty = false;
                            _lastHashPushUtc = DateTime.UtcNow;
                        }
                    }
                    try { ProcessPendingApplyQueue(); } catch { }

                    // Local row cloud pe nahi (hash ne skip kar di) — sirf missing sync_id, delete resurrect nahi
                    if ((DateTime.UtcNow - _lastMissingPushUtc).TotalSeconds >= 12)
                    {
                        try { PushMissingLiveRows(); } catch { }
                        _lastMissingPushUtc = DateTime.UtcNow;
                    }

                    // 2) Live PULL — mobile→WinForms
                    PullRecentDeletesTables(LiveTables, 180);
                    PullRecentHealTables(LiveTables, 180);
                    RaiseLiveChanged();
                    try { ProcessPendingApplyQueue(); } catch { }
                    if ((DateTime.UtcNow - _lastAllocCatchUpUtc).TotalSeconds >= 20)
                    {
                        _quietUi++;
                        try { PullRecentDeletesTables(LiveTables, 6 * 3600); }
                        finally { _quietUi--; }
                        _lastAllocCatchUpUtc = DateTime.UtcNow;
                        RaiseLiveChanged();
                    }

                    if ((DateTime.UtcNow - _lastFullIncrementalUtc).TotalSeconds >= 45)
                    {
                        PullIncremental();
                        _lastFullIncrementalUtc = DateTime.UtcNow;
                        RaiseLiveChanged();
                    }

                    // Allocate miss: recent window (full sync_id scan tick ko block na kare)
                    if ((DateTime.UtcNow - _lastAllocCatchUpUtc).TotalSeconds >= 25)
                    {
                        _quietUi++;
                        try { CatchUpRecentAllocates(180); }
                        finally { _quietUi--; }
                        _lastAllocCatchUpUtc = DateTime.UtcNow;
                        RaiseLiveChanged();
                    }

                    // 3) Deep heal / prune KABHI-KABHAR — har tick nahi (wipe nahi, sirf timing)
                    if ((DateTime.UtcNow - _lastDeepHealUtc).TotalSeconds >= 180)
                    {
                        PullRecentHealTables(HotTables, 120);
                        _lastDeepHealUtc = DateTime.UtcNow;
                        RaiseLiveChanged();
                    }
                    if ((DateTime.UtcNow - _lastPruneUtc).TotalSeconds >= 300)
                    {
                        SafePruneGoneFromCloud(true);
                        _lastPruneUtc = DateTime.UtcNow;
                        RaiseLiveChanged();
                    }
                }
                if ((DateTime.UtcNow - _lastCatchUpUtc).TotalSeconds >= 300)
                {
                    try { ReconcileUsersFromCloud(); } catch { }
                    _quietUi++;
                    try { CatchUpMissing(); }
                    finally { _quietUi--; }
                    _lastCatchUpUtc = DateTime.UtcNow;
                    RaiseLiveChanged();
                }
            }
            catch { }
        }

        /// Hash local pe hai lekin cloud pe row nahi — WinForms entry mobile tak nahi pahunchti.
        /// Cloud is_deleted ko missing mat samjho (delete wapas zinda nahi).
        private static void PushMissingLiveRows()
        {
            string[] tables = { "customers", "customer_plots", "plots", "receipts", "ledger" };
            int budget = 20;
            foreach (string table in tables)
            {
                if (budget <= 0) break;
                string cloud = CloudTable(table);
                var present = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                bool failed = false;
                for (int page = 0; page < 40 && !failed; page++)
                {
                    var rows = HttpGetListOk("/rest/v1/" + cloud
                        + "?select=sync_id&order=sync_id.asc&limit=1000&offset=" + (page * 1000), 12);
                    if (rows == null) { failed = true; break; }
                    foreach (var row in rows)
                    {
                        string id = Convert.ToString(row.ContainsKey("sync_id") ? row["sync_id"] : "").Trim();
                        if (id.Length > 0) present.Add(id);
                    }
                    if (rows.Count < 1000) break;
                }
                if (failed) continue;
                var pushIds = new List<long>();
                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    if (!TableExists(con, table)) continue;
                    string pk = Pk(table);
                    using (var cmd = new SQLiteCommand(
                        "SELECT \"" + pk + "\", sync_id FROM \"" + table + "\" WHERE sync_id IS NOT NULL AND TRIM(sync_id)<>'';", con))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read() && pushIds.Count < budget)
                        {
                            string sid = Convert.ToString(r[1] ?? "").Trim();
                            long id;
                            if (sid.Length == 0 || present.Contains(sid)) continue;
                            if (!long.TryParse(Convert.ToString(r[0] ?? ""), out id) || id <= 0) continue;
                            pushIds.Add(id);
                        }
                    }
                }
                foreach (long id in pushIds)
                {
                    if (PushOneNow(table, id)) budget--;
                }
            }
        }

        /// Sirf apni nayi/edit/delete — pura DB hash scan band (tick block = 1 min+ delay).
        private static bool CanPush()
        {
            if (!SupabaseConfig.IsConfigured) return false;
            return _dirty || HasPendingDeletes();
        }

        private static int LocalRowCount(string table)
        {
            try
            {
                using (var con = new SQLiteConnection(MainClass.con_string))
                using (var cmd = new SQLiteCommand("SELECT COUNT(1) FROM \"" + table + "\";", con))
                {
                    con.Open();
                    return Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                }
            }
            catch { return 0; }
        }

        private static bool NeedsFullPull()
        {
            return GetState("first_pull_done") != "1";
        }

        private static int CloudRowCount(string table)
        {
            try
            {
                var rows = HttpGetList("/rest/v1/" + CloudTable(table) + "?is_deleted=eq.false&select=sync_id&limit=1");
                return rows != null && rows.Count > 0 ? 1 : 0;
            }
            catch { return 0; }
        }

        private static void FullPullAllTables()
        {
            string maxTs = "1970-01-01T00:00:00.000Z";
            bool anyOk = false;
            bool anyFail = false;
            try
            {
            foreach (string table in Tables)
            {
                string cloud = CloudTable(table);
                for (int page = 0; page < 60; page++)
                {
                    var rows = HttpGetListOk("/rest/v1/" + cloud + "?is_deleted=eq.false&order=updated_at.asc,sync_id.asc&limit=500&offset=" + (page * 500));
                    if (rows == null)
                    {
                        anyFail = true;
                        break;
                    }
                    anyOk = true;
                    if (rows.Count == 0) break;
                    ApplyFetched(AsApplyRows(table, rows));
                    foreach (var row in rows)
                    {
                        object ts;
                        if (!row.TryGetValue("updated_at", out ts)) continue;
                        string s = Convert.ToString(ts ?? "");
                        if (string.CompareOrdinal(s, maxTs) > 0) maxTs = s;
                    }
                    if (rows.Count < 500) break;
                }
            }
            if (maxTs == "1970-01-01T00:00:00.000Z")
                maxTs = DateTime.UtcNow.ToString("o");
            if (anyOk && !anyFail)
            {
                SetState("last_pull_utc", maxTs);
                SetState("first_pull_done", "1");
            }
            }
            catch { }
        }

        private static void ReconcileUsersFromCloud()
        {
            try
            {
                // Offline / HTTP fail = null. Empty list mat samjho — warna local login wipe ho jata hai.
                var rows = HttpGetListOk("/rest/v1/users?is_deleted=eq.false&select=*&limit=500", 6);
                if (rows == null || rows.Count == 0) return;
                ApplyFetched(AsApplyRows("users", rows));
                var live = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var row in rows)
                {
                    string un = Convert.ToString(row.ContainsKey("username") ? row["username"] : "").Trim();
                    if (!string.IsNullOrWhiteSpace(un)) live.Add(un);
                }
                if (live.Count == 0) return;
                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    var gone = new List<long>();
                    using (var cmd = new SQLiteCommand(
                        "SELECT userID, username, sync_id FROM users;", con))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            string un = Convert.ToString(r["username"] ?? "").Trim();
                            string sid = Convert.ToString(r["sync_id"] ?? "").Trim();
                            if (string.IsNullOrWhiteSpace(un) || string.IsNullOrWhiteSpace(sid)) continue;
                            if (live.Contains(un)) continue;
                            if (MainClass.IsBuiltInAdminUsername(un)) continue;
                            gone.Add(Convert.ToInt64(r["userID"]));
                        }
                    }
                    foreach (long id in gone)
                    {
                        using (var cmd = new SQLiteCommand("DELETE FROM users WHERE userID=@id;", con))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                try { MainClass.EnsureLocalLoginUser(); } catch { }
            }
            catch { }
        }

        private static object JsonSafe(object val)
        {
            if (val == null || val is DBNull) return null;
            if (val is DateTime)
                return ((DateTime)val).ToUniversalTime().ToString("o");
            if (val is Guid) return ((Guid)val).ToString();
            if (val is byte[]) return null;
            if (val is string || val is bool || val is int || val is long || val is short || val is byte
                || val is double || val is float || val is decimal)
                return val;
            if (val is uint || val is ushort || val is ulong)
                return Convert.ToInt64(val);
            return Convert.ToString(val);
        }

        private static void SeedHashesIfAlreadySynced()
        {
            if (string.IsNullOrWhiteSpace(GetState("last_pull_utc"))) return;
            if (GetState("push_hashes_ready") == "1") return;
            using (var con = new SQLiteConnection(MainClass.con_string))
            {
                con.Open();
                foreach (string table in Tables)
                {
                    if (!TableExists(con, table)) continue;
                    using (var cmd = new SQLiteCommand("SELECT * FROM \"" + table + "\";", con))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            var payload = ReadPayload(r);
                            string syncId = Convert.ToString(payload.ContainsKey("sync_id") ? payload["sync_id"] : "");
                            if (string.IsNullOrWhiteSpace(syncId)) continue;
                            SaveHash(con, syncId, RowHash(payload));
                        }
                    }
                }
            }
            SetState("push_hashes_ready", "1");
        }

        private static Dictionary<string, object> ReadPayload(SQLiteDataReader r)
        {
            var payload = new Dictionary<string, object>();
            for (int i = 0; i < r.FieldCount; i++)
            {
                string col = r.GetName(i);
                object val = r.IsDBNull(i) ? null : JsonSafe(r.GetValue(i));
                payload[col] = val;
            }
            return payload;
        }

        private static bool PushChanged()
        {
            string staff = (MainClass.LoggedInName ?? MainClass.LoggedInUsername ?? "").Trim();
            var pendingDel = LoadPendingDeleteSyncIds();
            var batch = new List<Dictionary<string, object>>();
            string[] scanTables;
            lock (_dirtyTablesLock)
            {
                if (_dirtyTables.Count > 0)
                {
                    scanTables = new string[_dirtyTables.Count];
                    _dirtyTables.CopyTo(scanTables);
                    _dirtyTables.Clear();
                }
                else if (_dirty
                    && (DateTime.UtcNow - _lastSafetyPushUtc).TotalSeconds >= 8)
                {
                    // PushOneNow fail fallback — LiveTables jaldi dubara push
                    scanTables = LiveTables;
                    _lastSafetyPushUtc = DateTime.UtcNow;
                }
                else
                {
                    // PushOneNow already cloud pe — mass scan skip
                    return !_dirty;
                }
            }
            using (var con = new SQLiteConnection(MainClass.con_string))
            {
                con.Open();
                // Sirf dirty/Live — pura HotTables hash-scan tick ko minutes block karta tha
                foreach (string table in scanTables)
                {
                    if (!TableExists(con, table)) continue;
                    Exec(con, "UPDATE \"" + table + "\" SET sync_id = lower(hex(randomblob(16))) WHERE sync_id IS NULL OR TRIM(sync_id)='';");
                    using (var cmd = new SQLiteCommand("SELECT * FROM \"" + table + "\";", con))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            var payload = ReadPayload(r);
                            string syncId = Convert.ToString(payload.ContainsKey("sync_id") ? payload["sync_id"] : "");
                            if (string.IsNullOrWhiteSpace(syncId)) continue;
                            // Pending SoftMark — live upsert se tombstone overwrite mat karo
                            if (pendingDel.Contains(syncId)) continue;

                            Dictionary<string, string> fks;
                            if (Fks.TryGetValue(table, out fks))
                            {
                                foreach (var kv in fks)
                                {
                                    object fkVal;
                                    if (!payload.TryGetValue(kv.Key, out fkVal) || fkVal == null)
                                        continue;
                                    string parentSync = LookupSyncId(con, kv.Value, fkVal);
                                    if (!string.IsNullOrEmpty(parentSync))
                                        payload["__fk_" + kv.Key] = parentSync;
                                }
                            }

                            string hash = RowHash(payload);
                            if (string.Equals(LoadHash(con, syncId), hash, StringComparison.OrdinalIgnoreCase))
                                continue;

                            string dupKey = DupKey(table, payload);
                            var cloudRow = LocalToCloudRow(table, payload, con);
                            cloudRow["dup_key"] = dupKey ?? "";
                            cloudRow["is_deleted"] = false;
                            cloudRow["staff_name"] = staff;
                            cloudRow["updated_at"] = DateTime.UtcNow.ToString("o");
                            cloudRow["_cloud_table"] = CloudTable(table);
                            cloudRow["_row_hash"] = hash;
                            batch.Add(cloudRow);
                        }
                    }
                }
            }
            if (batch.Count == 0)
            {
                SetState("push_hashes_ready", "1");
                return true;
            }
            bool ok = FlushCloudBatch(batch);
            if (ok) SetState("push_hashes_ready", "1");
            return ok;
        }

        private static string LookupSyncId(SQLiteConnection con, string table, object id)
        {
            try
            {
                if (!TableExists(con, table)) return "";
                string pk = Pk(table);
                using (var cmd = new SQLiteCommand(
                    "SELECT sync_id FROM \"" + table + "\" WHERE \"" + pk + "\"=@id LIMIT 1;", con))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    object o = cmd.ExecuteScalar();
                    return Convert.ToString(o ?? "");
                }
            }
            catch { return ""; }
        }

        private static bool PostBatch(List<Dictionary<string, object>> batch)
        {
            if (batch == null || batch.Count == 0) return true;
            string url = SupabaseConfig.Url.TrimEnd('/') + "/rest/v1/hb_sync?on_conflict=sync_id";
            var req = new HttpRequestMessage(HttpMethod.Post, url);
            ApplyHeaders(req);
            req.Headers.TryAddWithoutValidation("Prefer", "resolution=merge-duplicates,return=minimal");
            req.Content = new StringContent(Json.Serialize(batch), Encoding.UTF8, "application/json");
            using (var resp = Http.SendAsync(req).GetAwaiter().GetResult())
                return resp.IsSuccessStatusCode;
        }

        private static int TableOrder(string table)
        {
            for (int i = 0; i < Tables.Length; i++)
                if (string.Equals(Tables[i], table, StringComparison.OrdinalIgnoreCase))
                    return i;
            return 500;
        }

        private static bool RowIsDeleted(Dictionary<string, object> row)
        {
            object del;
            if (row == null || !row.TryGetValue("is_deleted", out del) || del == null) return false;
            if (del is bool) return (bool)del;
            string s = Convert.ToString(del);
            return string.Equals(s, "True", StringComparison.OrdinalIgnoreCase)
                || s == "1";
        }

        private static bool _mergingUser;

        private static List<Dictionary<string, object>> HttpGetList(string pathQuery)
        {
            return HttpGetList(pathQuery, -1, -1);
        }

        private static List<Dictionary<string, object>> HttpGetList(string pathQuery, int from, int to)
        {
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Get, SupabaseConfig.Url.TrimEnd('/') + pathQuery);
                ApplyHeaders(req);
                req.Headers.TryAddWithoutValidation("Prefer", "count=none");
                if (from >= 0 && to >= from)
                {
                    req.Headers.TryAddWithoutValidation("Range-Unit", "items");
                    req.Headers.TryAddWithoutValidation("Range", from.ToString() + "-" + to.ToString());
                }
                using (var resp = Http.SendAsync(req).GetAwaiter().GetResult())
                {
                    string body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    int code = (int)resp.StatusCode;
                    if (code < 200 || code >= 300 || string.IsNullOrWhiteSpace(body) || body == "[]")
                        return new List<Dictionary<string, object>>();
                    var rows = Json.Deserialize<List<Dictionary<string, object>>>(body);
                    return rows ?? new List<Dictionary<string, object>>();
                }
            }
            catch
            {
                return new List<Dictionary<string, object>>();
            }
        }

        /// Success = list (maybe empty). Network/HTTP fail = null.
        private static List<Dictionary<string, object>> HttpGetListOk(string pathQuery)
        {
            return HttpGetListOk(pathQuery, 0);
        }

        private static List<Dictionary<string, object>> HttpGetListOk(string pathQuery, int timeoutSeconds)
        {
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Get, SupabaseConfig.Url.TrimEnd('/') + pathQuery);
                ApplyHeaders(req);
                req.Headers.TryAddWithoutValidation("Prefer", "count=none");
                using (var cts = timeoutSeconds > 0
                    ? new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds))
                    : new CancellationTokenSource())
                using (var resp = Http.SendAsync(req, cts.Token).GetAwaiter().GetResult())
                {
                    int code = (int)resp.StatusCode;
                    if (code < 200 || code >= 300) return null;
                    string body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (string.IsNullOrWhiteSpace(body) || body == "[]")
                        return new List<Dictionary<string, object>>();
                    var rows = Json.Deserialize<List<Dictionary<string, object>>>(body);
                    return rows ?? new List<Dictionary<string, object>>();
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Prune: sirf cloud-confirmed is_deleted=true. Live absence se local delete NAHI.
        /// Soft-delete cleanup = ApplyRow / ye confirm path. Sync/pull behavior untouched.
        /// Manual Sync/Pull pe force=true (throttle bypass).
        /// </summary>
        private static void SafePruneGoneFromCloud(bool force = false)
        {
            // force=false: max 2s throttle (tick pe force=true → turant)
            if (!force && (DateTime.UtcNow - _lastPruneUtc).TotalSeconds < 2) return;
            try
            {
                bool fullScan = (DateTime.UtcNow - _lastFullPruneUtc).TotalSeconds >= 30;
                if (fullScan) _lastFullPruneUtc = DateTime.UtcNow;
                string sinceQ = "";
                if (!fullScan)
                {
                    string since = DateTime.UtcNow.AddMinutes(-10).ToString("o");
                    sinceQ = "&updated_at=gt." + Uri.EscapeDataString(since) + "&order=updated_at.asc";
                }
                else
                    sinceQ = "&order=sync_id.asc";

                var pending = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                for (int ti = Tables.Length - 1; ti >= 0; ti--)
                {
                    string table = Tables[ti];
                    string cloud = CloudTable(table);
                    var confirmedDeleted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    bool failed = false;
                    int pageMax = fullScan ? 80 : 5;
                    for (int page = 0; page < pageMax; page++)
                    {
                        // Positive confirm only — is_deleted=true (not "missing from live")
                        var rows = HttpGetListOk("/rest/v1/" + cloud
                            + "?is_deleted=eq.true&select=sync_id" + sinceQ
                            + "&limit=1000&offset=" + (page * 1000));
                        if (rows == null) { failed = true; break; }
                        if (rows.Count == 0) break;
                        foreach (var row in rows)
                        {
                            string id = Convert.ToString(row.ContainsKey("sync_id") ? row["sync_id"] : "");
                            if (!string.IsNullOrWhiteSpace(id)) confirmedDeleted.Add(id.Trim());
                        }
                        if (rows.Count < 1000) break;
                    }
                    if (failed || confirmedDeleted.Count == 0) continue;

                    var gone = new List<string>();
                    using (var con = new SQLiteConnection(MainClass.con_string))
                    {
                        con.Open();
                        if (!TableExists(con, table)) continue;
                        foreach (string sid in confirmedDeleted)
                        {
                            using (var cmd = new SQLiteCommand(
                                "SELECT 1 FROM \"" + table + "\" WHERE sync_id=@s LIMIT 1;", con))
                            {
                                cmd.Parameters.AddWithValue("@s", sid);
                                if (cmd.ExecuteScalar() == null) continue;
                            }
                            gone.Add(sid);
                        }
                    }

                    if (gone.Count == 0) continue;
                    pending[table] = gone;
                }

                if (pending.Count == 0)
                {
                    _lastPruneUtc = DateTime.UtcNow;
                    return;
                }

                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    Exec(con, "PRAGMA foreign_keys = OFF;");
                    try
                    {
                        foreach (var kv in pending)
                        {
                            foreach (string sid in kv.Value)
                            {
                                using (var cmd = new SQLiteCommand("DELETE FROM \"" + kv.Key + "\" WHERE sync_id=@s;", con))
                                {
                                    cmd.Parameters.AddWithValue("@s", sid);
                                    cmd.ExecuteNonQuery();
                                }
                                using (var cmd = new SQLiteCommand("DELETE FROM sync_known WHERE sync_id=@s;", con))
                                {
                                    cmd.Parameters.AddWithValue("@s", sid);
                                    cmd.ExecuteNonQuery();
                                }
                                using (var cmd = new SQLiteCommand("DELETE FROM sync_hash WHERE sync_id=@s;", con))
                                {
                                    cmd.Parameters.AddWithValue("@s", sid);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                    finally
                    {
                        try { Exec(con, "PRAGMA foreign_keys = ON;"); } catch { }
                    }
                }
                RaiseLiveChanged();
                _lastPruneUtc = DateTime.UtcNow;
            }
            catch { }
        }

        private static bool IsSyncKnown(SQLiteConnection con, string syncId)
        {
            try
            {
                using (var cmd = new SQLiteCommand(
                    "SELECT 1 FROM sync_known WHERE sync_id=@s LIMIT 1;", con))
                {
                    cmd.Parameters.AddWithValue("@s", syncId);
                    return cmd.ExecuteScalar() != null;
                }
            }
            catch { return false; }
        }

        private static void PruneGoneFromCloud()
        {
            SafePruneGoneFromCloud();
        }

        private static HashSet<string> LocalSyncIds(SQLiteConnection con)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string t in Tables)
            {
                if (!TableExists(con, t)) continue;
                using (var cmd = new SQLiteCommand(
                    "SELECT sync_id FROM \"" + t + "\" WHERE sync_id IS NOT NULL AND TRIM(sync_id)<>'';", con))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        string id = Convert.ToString(r[0] ?? "");
                        if (!string.IsNullOrWhiteSpace(id)) ids.Add(id);
                    }
                }
            }
            try
            {
                using (var cmd = new SQLiteCommand("SELECT sync_id FROM sync_alias;", con))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        string id = Convert.ToString(r[0] ?? "");
                        if (!string.IsNullOrWhiteSpace(id)) ids.Add(id);
                    }
                }
            }
            catch { }
            return ids;
        }

        private static void ApplyFetched(List<Dictionary<string, object>> fetched)
        {
            if (fetched == null || fetched.Count == 0) return;
            fetched.Sort(delegate (Dictionary<string, object> a, Dictionary<string, object> b)
            {
                string ta = Convert.ToString(a.ContainsKey("table_name") ? a["table_name"] : "");
                string tb = Convert.ToString(b.ContainsKey("table_name") ? b["table_name"] : "");
                int c = TableOrder(ta).CompareTo(TableOrder(tb));
                if (c != 0) return c;
                int da = RowIsDeleted(a) ? 0 : 1;
                int db = RowIsDeleted(b) ? 0 : 1;
                c = da.CompareTo(db);
                if (c != 0) return c;
                string sa = Convert.ToString(a.ContainsKey("sync_id") ? a["sync_id"] : "");
                string sb = Convert.ToString(b.ContainsKey("sync_id") ? b["sync_id"] : "");
                return string.CompareOrdinal(sa, sb);
            });
            using (var con = new SQLiteConnection(MainClass.con_string))
            {
                con.Open();
                Exec(con, "PRAGMA foreign_keys=OFF;");
                try { Exec(con, "PRAGMA ignore_check_constraints=ON;"); } catch { }
                Exec(con, "DROP TRIGGER IF EXISTS trg_receipts_no_overpay_insert;");
                Exec(con, "DROP TRIGGER IF EXISTS trg_receipts_no_overpay_update;");
                Exec(con, "DROP TRIGGER IF EXISTS trg_cp_one_active_plot_ins;");
                Exec(con, "DROP TRIGGER IF EXISTS trg_cp_one_active_plot_upd;");
                _pullNotes.Clear();
                for (int pass = 0; pass < 6; pass++)
                {
                    foreach (var row in fetched)
                    {
                        try { ApplyRow(con, row, false); }
                        catch { }
                    }
                }
                try { Exec(con, "PRAGMA ignore_check_constraints=OFF;"); } catch { }
                Exec(con, "PRAGMA foreign_keys=ON;");
                RestoreSyncTriggers(con);
                // Union-safe pull can land 2 Active on same plot_id (triggers off). Soft-cancel older.
                try { ReconcileOneActiveCustomerPlots(con); } catch { }
            }
            RaiseLiveChanged();
        }

        /// <summary>
        /// Per plot_id: keep newest Active (booking_date → sync_id → id); soft-cancel others.
        /// Keeps sync_id + receipts; does NOT content-merge distinct sync_ids.
        /// </summary>
        public static int ReconcileOneActiveCustomerPlots(SQLiteConnection existing = null)
        {
            int n = 0;
            try
            {
                if (existing != null)
                {
                    n = ReconcileOneActiveCustomerPlotsCore(existing);
                }
                else
                {
                    using (var con = new SQLiteConnection(MainClass.con_string))
                    {
                        con.Open();
                        n = ReconcileOneActiveCustomerPlotsCore(con);
                    }
                }
                if (n > 0)
                {
                    try { MarkDirty(); } catch { }
                }
            }
            catch { }
            return n;
        }

        private static int ReconcileOneActiveCustomerPlotsCore(SQLiteConnection con)
        {
            if (!TableExists(con, "customer_plots")) return 0;
            using (var cmd = new SQLiteCommand(@"
UPDATE customer_plots
SET status = 'Cancelled',
    cancelled_date = CASE
      WHEN cancelled_date IS NULL OR TRIM(IFNULL(cancelled_date,'')) = ''
      THEN date('now','localtime')
      ELSE cancelled_date
    END
WHERE IFNULL(status,'') = 'Active'
  AND id IN (
    SELECT id FROM (
      SELECT cp.id AS id
      FROM customer_plots cp
      WHERE IFNULL(cp.status,'') = 'Active'
        AND EXISTS (
          SELECT 1 FROM customer_plots newer
          WHERE newer.plot_id = cp.plot_id
            AND IFNULL(newer.status,'') = 'Active'
            AND newer.id <> cp.id
            AND (
              IFNULL(newer.booking_date,'') > IFNULL(cp.booking_date,'')
              OR (
                IFNULL(newer.booking_date,'') = IFNULL(cp.booking_date,'')
                AND IFNULL(newer.sync_id,'') > IFNULL(cp.sync_id,'')
              )
              OR (
                IFNULL(newer.booking_date,'') = IFNULL(cp.booking_date,'')
                AND IFNULL(newer.sync_id,'') = IFNULL(cp.sync_id,'')
                AND newer.id > cp.id
              )
            )
        )
    )
  );", con))
            {
                return cmd.ExecuteNonQuery();
            }
        }

        private static readonly List<string> _pullNotes = new List<string>();

        private static void NotePull(string staff, string verb, string table)
        {
            // Background pull pe toast nahi — sirf grid refresh (LiveChanged).
        }

        internal static void RestoreSyncTriggers(SQLiteConnection con)
        {
            Exec(con, "DROP TRIGGER IF EXISTS trg_cp_one_active_plot_ins;");
            Exec(con, "DROP TRIGGER IF EXISTS trg_cp_one_active_plot_upd;");
            Exec(con, "DROP TRIGGER IF EXISTS trg_receipts_no_overpay_insert;");
            Exec(con, "DROP TRIGGER IF EXISTS trg_receipts_no_overpay_update;");
            Exec(con, @"
CREATE TRIGGER trg_cp_one_active_plot_ins
BEFORE INSERT ON customer_plots
FOR EACH ROW
WHEN NEW.status = 'Active'
BEGIN
  SELECT CASE
    WHEN EXISTS(
      SELECT 1 FROM customer_plots
      WHERE plot_id = NEW.plot_id AND status = 'Active'
    )
    THEN RAISE(ABORT, 'Ye plot pehle se Active allocation pe hai.')
  END;
END;");
            Exec(con, @"
CREATE TRIGGER trg_cp_one_active_plot_upd
BEFORE UPDATE OF plot_id, status ON customer_plots
FOR EACH ROW
WHEN NEW.status = 'Active'
BEGIN
  SELECT CASE
    WHEN EXISTS(
      SELECT 1 FROM customer_plots
      WHERE plot_id = NEW.plot_id AND status = 'Active' AND id <> NEW.id
    )
    THEN RAISE(ABORT, 'Ye plot pehle se Active allocation pe hai.')
  END;
END;");
            Exec(con, @"
CREATE TRIGGER trg_receipts_no_overpay_insert
BEFORE INSERT ON receipts
FOR EACH ROW
BEGIN
  SELECT
    CASE
      WHEN (
        (SELECT COALESCE(SUM(debit),0) FROM ledger WHERE customer_plot_id=NEW.customer_plot_id)
      ) > 0
      THEN
        CASE
          WHEN (
            (SELECT COALESCE(SUM(debit),0) FROM ledger WHERE customer_plot_id=NEW.customer_plot_id)
            - (SELECT COALESCE(SUM(amount),0) FROM receipts WHERE customer_plot_id=NEW.customer_plot_id)
            - NEW.amount
          ) >= -0.00001
          THEN 1
          ELSE RAISE(ABORT,'Overpayment blocked (INSERT).')
        END
      ELSE
        CASE
          WHEN (
            (SELECT COALESCE(price,0) FROM customer_plots WHERE id=NEW.customer_plot_id)
            - (SELECT COALESCE(SUM(amount),0) FROM receipts WHERE customer_plot_id=NEW.customer_plot_id)
            - NEW.amount
          ) >= -0.00001
          THEN 1
          ELSE RAISE(ABORT,'Overpayment blocked (INSERT).')
        END
    END;
END;");
            Exec(con, @"
CREATE TRIGGER trg_receipts_no_overpay_update
BEFORE UPDATE ON receipts
FOR EACH ROW
BEGIN
  SELECT
    CASE
      WHEN (
        (SELECT COALESCE(SUM(debit),0) FROM ledger WHERE customer_plot_id=NEW.customer_plot_id)
      ) > 0
      THEN
        CASE
          WHEN (
            (SELECT COALESCE(SUM(debit),0) FROM ledger WHERE customer_plot_id=NEW.customer_plot_id)
            - (SELECT COALESCE(SUM(amount),0) FROM receipts WHERE customer_plot_id=NEW.customer_plot_id AND id<>NEW.id)
            - NEW.amount
          ) >= -0.00001
          THEN 1
          ELSE RAISE(ABORT,'Overpayment blocked (UPDATE).')
        END
      ELSE
        CASE
          WHEN (
            (SELECT COALESCE(price,0) FROM customer_plots WHERE id=NEW.customer_plot_id)
            - (SELECT COALESCE(SUM(amount),0) FROM receipts WHERE customer_plot_id=NEW.customer_plot_id AND id<>NEW.id)
            - NEW.amount
          ) >= -0.00001
          THEN 1
          ELSE RAISE(ABORT,'Overpayment blocked (UPDATE).')
        END
    END;
END;");
        }

        /// <summary>Jo Supabase pe hai aur local pe nahi — insert-missing only (no wipe). Allocate = parent-ensure pehle.</summary>
        private static void CatchUpMissing()
        {
            CatchUpMissingTables(Tables);
        }

        /// <summary>Cloud pe hai, local pe nahi — sync_id se lao (allocate miss fix).</summary>
        private static void CatchUpMissingTables(string[] tables)
        {
            if (tables == null || tables.Length == 0) return;
            try
            {
                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    // Stale content-dup aliases hide missing cloud rows — clear union-safe tables
                    Exec(con,
                        "DELETE FROM sync_alias WHERE lower(table_name) IN ('partner_money','receipts','customer_plots');");
                }
            }
            catch { }
            HashSet<string> pendingDel = LoadPendingDeleteSyncIds();
            foreach (string table in tables)
            {
                string cloud = CloudTable(table);
                bool isAlloc = string.Equals(table, "customer_plots", StringComparison.OrdinalIgnoreCase);
                HashSet<string> local;
                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    // Per-table — dusri table ka sync_id missing allocate hide na kare
                    local = LocalSyncIdsForTable(con, table);
                }
                for (int page = 0; page < 60; page++)
                {
                    var rows = HttpGetList("/rest/v1/" + cloud + "?is_deleted=eq.false&select=sync_id&order=sync_id.asc&limit=1000&offset=" + (page * 1000));
                    if (rows == null || rows.Count == 0) break;
                    var missing = new List<string>();
                    foreach (var row in rows)
                    {
                        string id = Convert.ToString(row.ContainsKey("sync_id") ? row["sync_id"] : "");
                        if (string.IsNullOrWhiteSpace(id)) continue;
                        string key = id.Trim().ToLowerInvariant();
                        // Local delete pending / soft-mark in-flight — CatchUp wapas mat lao
                        if (pendingDel.Contains(key) || pendingDel.Contains(id.Trim())) continue;
                        if (!local.Contains(key))
                            missing.Add(id);
                    }
                    for (int i = 0; i < missing.Count; i += 30)
                    {
                        int n = Math.Min(30, missing.Count - i);
                        var sb = new StringBuilder();
                        for (int j = 0; j < n; j++)
                        {
                            if (j > 0) sb.Append(',');
                            sb.Append(missing[i + j]);
                        }
                        var fetched = HttpGetList("/rest/v1/" + cloud + "?is_deleted=eq.false&sync_id=in.(" + sb + ")");
                        if (fetched == null || fetched.Count == 0) continue;
                        if (isAlloc)
                        {
                            var skipList = new List<string>();
                            EnsureRecoverParentsForCustomerPlots(fetched, skipList);
                        }
                        if (fetched.Count > 0)
                        {
                            ApplyFetched(AsApplyRows(table, fetched));
                            foreach (var fr in fetched)
                            {
                                string id = Convert.ToString(fr.ContainsKey("sync_id") ? fr["sync_id"] : "");
                                if (!string.IsNullOrWhiteSpace(id))
                                    local.Add(id.Trim().ToLowerInvariant());
                            }
                        }
                    }
                    if (rows.Count < 1000) break;
                }
            }
        }

        /// Recent allocates only — full sync_id CatchUp har 20s tick block nahi.
        private static void CatchUpRecentAllocates(int windowSec)
        {
            int sec = Math.Max(60, windowSec);
            string since = UtcFilter(DateTime.UtcNow.AddSeconds(-sec));
            string cloud = CloudTable("customer_plots");
            var all = new List<Dictionary<string, object>>();
            for (int page = 0; page < 5; page++)
            {
                var rows = HttpGetList("/rest/v1/" + cloud
                    + "?is_deleted=eq.false&updated_at=gt." + Uri.EscapeDataString(since)
                    + "&order=updated_at.asc,sync_id.asc&limit=200&offset=" + (page * 200));
                if (rows == null || rows.Count == 0) break;
                all.AddRange(rows);
                if (rows.Count < 200) break;
            }
            if (all.Count == 0) return;
            HashSet<string> local;
            using (var con = new SQLiteConnection(MainClass.con_string))
            {
                con.Open();
                local = LocalSyncIdsForTable(con, "customer_plots");
            }
            var missing = new List<Dictionary<string, object>>();
            foreach (var row in all)
            {
                string id = Convert.ToString(row.ContainsKey("sync_id") ? row["sync_id"] : "").Trim();
                if (string.IsNullOrWhiteSpace(id)) continue;
                if (local.Contains(id.ToLowerInvariant())) continue;
                missing.Add(row);
            }
            if (missing.Count == 0) return;
            var skip = new List<string>();
            EnsureRecoverParentsForCustomerPlots(missing, skip);
            if (missing.Count > 0)
                ApplyFetched(AsApplyRows("customer_plots", missing));
        }

        /// PostgREST filter — 7-digit fractional + local offset avoid.
        private static string UtcFilter(DateTime utc)
        {
            return utc.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
                System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string UtcFilterIso(string iso)
        {
            DateTime t;
            if (!DateTime.TryParse(iso, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out t))
                return iso;
            if (t.Kind == DateTimeKind.Unspecified)
                t = DateTime.SpecifyKind(t, DateTimeKind.Utc);
            return UtcFilter(t.ToUniversalTime());
        }

        private static string JustBeforeUtc(string iso)
        {
            DateTime t;
            if (!DateTime.TryParse(iso, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out t))
                return iso;
            if (t.Kind == DateTimeKind.Unspecified)
                t = DateTime.SpecifyKind(t, DateTimeKind.Utc);
            return t.ToUniversalTime().AddMilliseconds(-1).ToString("o");
        }

        private static void PullIncremental()
        {
            string sinceRaw = GetState("last_pull_utc");
            if (string.IsNullOrWhiteSpace(sinceRaw))
                sinceRaw = "1970-01-01T00:00:00.000Z";
            string since = UtcFilterIso(sinceRaw);
            string maxApplied = since;
            string minDropped = null;
            bool anyFetchFail = false;
            bool anyApplyFail = false;
            var bag = new List<Dictionary<string, object>>();
            // Sequential — customer_plots se pehle plots/customers parents apply
            foreach (string table in Tables)
            {
                string cloud = CloudTable(table);
                var all = new List<Dictionary<string, object>>();
                for (int page = 0; page < 40; page++)
                {
                    // HttpGetListOk: null = network/HTTP fail (empty list se alag)
                    var rows = HttpGetListOk("/rest/v1/" + cloud + "?updated_at=gt." + Uri.EscapeDataString(since)
                        + "&order=updated_at.asc,sync_id.asc&limit=200&offset=" + (page * 200), 20);
                    if (rows == null) { anyFetchFail = true; break; }
                    if (rows.Count == 0) break;
                    all.AddRange(rows);
                    if (rows.Count < 200) break;
                }
                if (anyFetchFail) break;
                if (all.Count == 0) continue;

                var ready = all;
                if (string.Equals(table, "customer_plots", StringComparison.OrdinalIgnoreCase))
                {
                    ready = new List<Dictionary<string, object>>(all);
                    var skip = new List<string>();
                    var live = new List<Dictionary<string, object>>();
                    var deleted = new List<Dictionary<string, object>>();
                    foreach (var r in ready)
                    {
                        if (RowIsDeleted(r)) deleted.Add(r);
                        else live.Add(r);
                    }
                    int before = live.Count;
                    EnsureRecoverParentsForCustomerPlots(live, skip);
                    if (live.Count < before)
                    {
                        foreach (var r in all)
                        {
                            if (RowIsDeleted(r)) continue;
                            string sid = Convert.ToString(r.ContainsKey("sync_id") ? r["sync_id"] : "").Trim();
                            bool kept = false;
                            foreach (var k in live)
                            {
                                if (string.Equals(sid, Convert.ToString(k.ContainsKey("sync_id") ? k["sync_id"] : ""), StringComparison.OrdinalIgnoreCase))
                                { kept = true; break; }
                            }
                            if (kept) continue;
                            object ts;
                            if (!r.TryGetValue("updated_at", out ts)) continue;
                            string s = Convert.ToString(ts ?? "");
                            if (string.IsNullOrWhiteSpace(s)) continue;
                            if (minDropped == null || string.CompareOrdinal(s, minDropped) < 0)
                                minDropped = s;
                        }
                    }
                    ready = new List<Dictionary<string, object>>();
                    ready.AddRange(deleted);
                    ready.AddRange(live);
                }

                if (ready.Count == 0) continue;
                bag.AddRange(AsApplyRows(table, ready));
                foreach (var row in ready)
                {
                    object ts;
                    if (!row.TryGetValue("updated_at", out ts)) continue;
                    string s = UtcFilterIso(Convert.ToString(ts ?? ""));
                    if (string.IsNullOrWhiteSpace(s)) continue;
                    if (string.CompareOrdinal(s, maxApplied) > 0) maxApplied = s;
                }
            }
            try
            {
                if (bag.Count > 0) ApplyFetched(bag);
            }
            catch { anyApplyFail = true; }
            // Fetch/apply fail → last_pull aage mat badhao (doosri table success se miss na ho)
            if (anyFetchFail || anyApplyFail) return;
            string advance = maxApplied;
            // Parent-miss: durable pending_apply pe save; cursor sirf JustBefore(minDropped)
            if (!string.IsNullOrWhiteSpace(minDropped))
            {
                string cap = JustBeforeUtc(minDropped);
                if (string.CompareOrdinal(advance, cap) > 0) advance = cap;
            }
            if (string.CompareOrdinal(advance, since) > 0)
                SetState("last_pull_utc", advance);
            try { ProcessPendingApplyQueue(); } catch { }
        }

        private static void SavePendingApply(string table, Dictionary<string, object> cloudRow,
            string waitParentTable, string waitParentSync)
        {
            if (cloudRow == null) return;
            string sid = Convert.ToString(cloudRow.ContainsKey("sync_id") ? cloudRow["sync_id"] : "").Trim();
            if (string.IsNullOrWhiteSpace(sid) || string.IsNullOrWhiteSpace(table)) return;
            try
            {
                string json = Json.Serialize(cloudRow);
                string ts = Convert.ToString(cloudRow.ContainsKey("updated_at") ? cloudRow["updated_at"] : "");
                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    using (var cmd = new SQLiteCommand(
                        @"INSERT OR REPLACE INTO sync_pending_apply
(sync_id, table_name, payload_json, wait_parent_table, wait_parent_sync_id, cloud_updated_at)
VALUES(@s,@t,@j,@wt,@ws,@u);", con))
                    {
                        cmd.Parameters.AddWithValue("@s", sid);
                        cmd.Parameters.AddWithValue("@t", table);
                        cmd.Parameters.AddWithValue("@j", json ?? "");
                        cmd.Parameters.AddWithValue("@wt", waitParentTable ?? "");
                        cmd.Parameters.AddWithValue("@ws", waitParentSync ?? "");
                        cmd.Parameters.AddWithValue("@u", ts ?? "");
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        /// <summary>Parent milte hi skipped allocate/receipt apply — restart ke baad bhi.</summary>
        public static void ProcessPendingApplyQueue()
        {
            List<Dictionary<string, object>> items = new List<Dictionary<string, object>>();
            try
            {
                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    if (!TableExists(con, "sync_pending_apply")) return;
                    using (var cmd = new SQLiteCommand(
                        "SELECT sync_id, table_name, payload_json, wait_parent_table, wait_parent_sync_id FROM sync_pending_apply LIMIT 80;", con))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            items.Add(new Dictionary<string, object>
                            {
                                { "sync_id", Convert.ToString(r[0] ?? "") },
                                { "table_name", Convert.ToString(r[1] ?? "") },
                                { "payload_json", Convert.ToString(r[2] ?? "") },
                                { "wait_parent_table", Convert.ToString(r[3] ?? "") },
                                { "wait_parent_sync_id", Convert.ToString(r[4] ?? "") }
                            });
                        }
                    }
                }
            }
            catch { return; }
            foreach (var it in items)
            {
                string waitT = Convert.ToString(it["wait_parent_table"] ?? "").Trim();
                string waitS = Convert.ToString(it["wait_parent_sync_id"] ?? "").Trim();
                string table = Convert.ToString(it["table_name"] ?? "").Trim();
                string sid = Convert.ToString(it["sync_id"] ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(waitT) && !string.IsNullOrWhiteSpace(waitS))
                {
                    using (var con = new SQLiteConnection(MainClass.con_string))
                    {
                        con.Open();
                        if (!LocalIdBySync(con, waitT, waitS).HasValue) continue;
                    }
                }
                try
                {
                    var payload = Json.Deserialize<Dictionary<string, object>>(
                        Convert.ToString(it["payload_json"] ?? "{}"));
                    if (payload == null) continue;
                    payload["sync_id"] = sid;
                    if (RowIsDeleted(payload))
                    {
                        ApplyFetched(AsApplyRows(table, new List<Dictionary<string, object>> { payload }));
                    }
                    else
                    {
                        var list = new List<Dictionary<string, object>> { payload };
                        var skip = new List<string>();
                        if (string.Equals(table, "customer_plots", StringComparison.OrdinalIgnoreCase))
                            EnsureRecoverParentsForCustomerPlots(list, skip);
                        else if (string.Equals(table, "receipts", StringComparison.OrdinalIgnoreCase)
                              || string.Equals(table, "ledger", StringComparison.OrdinalIgnoreCase))
                            FilterRecoverChildrenToExistingAllocs(table, list, skip);
                        if (list.Count == 0) continue;
                        ApplyFetched(AsApplyRows(table, list));
                    }
                    using (var con = new SQLiteConnection(MainClass.con_string))
                    {
                        con.Open();
                        using (var cmd = new SQLiteCommand(
                            "DELETE FROM sync_pending_apply WHERE sync_id=@s;", con))
                        {
                            cmd.Parameters.AddWithValue("@s", sid);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// Recent window re-apply (incl. is_deleted). last_pull advance nahi —
        /// missed update/delete heal; same sync_id = update, no duplicate/wipe.
        /// </summary>
        private static void PullRecentHeal(int windowSec)
        {
            PullRecentHealTables(Tables, windowSec);
        }

        /// Sirf is_deleted=true recent — delete signal pehle apply.
        private static void PullRecentDeletesTables(string[] tables, int windowSec)
        {
            int sec = Math.Max(30, windowSec);
            string since = UtcFilter(DateTime.UtcNow.AddSeconds(-sec));
            var bag = new List<Dictionary<string, object>>();
            var gate = new object();
            Parallel.ForEach(tables, new ParallelOptions { MaxDegreeOfParallelism = 8 }, table =>
            {
                string cloud = CloudTable(table);
                var all = new List<Dictionary<string, object>>();
                for (int page = 0; page < 3; page++)
                {
                    var rows = HttpGetList("/rest/v1/" + cloud
                        + "?is_deleted=eq.true&updated_at=gt." + Uri.EscapeDataString(since)
                        + "&order=updated_at.asc,sync_id.asc&limit=200&offset=" + (page * 200));
                    if (rows == null || rows.Count == 0) break;
                    all.AddRange(rows);
                    if (rows.Count < 200) break;
                }
                if (all.Count == 0) return;
                lock (gate) bag.AddRange(AsApplyRows(table, all));
            });
            if (bag.Count > 0) ApplyFetched(bag);
        }

        private static void PullRecentHealTables(string[] tables, int windowSec)
        {
            int sec = Math.Max(15, windowSec);
            string since = UtcFilter(DateTime.UtcNow.AddSeconds(-sec));
            var bag = new List<Dictionary<string, object>>();
            var gate = new object();
            // customer_plots sequential + parents — parallel race se allocate miss
            var parallel = new List<string>();
            foreach (string t in tables)
            {
                if (string.Equals(t, "customer_plots", StringComparison.OrdinalIgnoreCase))
                {
                    string cloud = CloudTable(t);
                    var all = new List<Dictionary<string, object>>();
                    for (int page = 0; page < 5; page++)
                    {
                        var rows = HttpGetList("/rest/v1/" + cloud + "?updated_at=gt." + Uri.EscapeDataString(since)
                            + "&order=updated_at.asc,sync_id.asc&limit=200&offset=" + (page * 200));
                        if (rows == null || rows.Count == 0) break;
                        all.AddRange(rows);
                        if (rows.Count < 200) break;
                    }
                    if (all.Count == 0) continue;
                    var live = new List<Dictionary<string, object>>();
                    var deleted = new List<Dictionary<string, object>>();
                    foreach (var r in all)
                    {
                        if (RowIsDeleted(r)) deleted.Add(r);
                        else live.Add(r);
                    }
                    var skip = new List<string>();
                    EnsureRecoverParentsForCustomerPlots(live, skip);
                    var ready = new List<Dictionary<string, object>>();
                    ready.AddRange(deleted);
                    ready.AddRange(live);
                    if (ready.Count > 0)
                        bag.AddRange(AsApplyRows(t, ready));
                }
                else parallel.Add(t);
            }
            if (parallel.Count > 0)
            {
                Parallel.ForEach(parallel, new ParallelOptions { MaxDegreeOfParallelism = 8 }, table =>
                {
                    string cloud = CloudTable(table);
                    var all = new List<Dictionary<string, object>>();
                    for (int page = 0; page < 5; page++)
                    {
                        var rows = HttpGetList("/rest/v1/" + cloud + "?updated_at=gt." + Uri.EscapeDataString(since)
                            + "&order=updated_at.asc,sync_id.asc&limit=200&offset=" + (page * 200));
                        if (rows == null || rows.Count == 0) break;
                        all.AddRange(rows);
                        if (rows.Count < 200) break;
                    }
                    if (all.Count == 0) return;
                    lock (gate) bag.AddRange(AsApplyRows(table, all));
                });
            }
            if (bag.Count > 0) ApplyFetched(bag);
        }

        private static void ApplyRow(SQLiteConnection con, Dictionary<string, object> row, bool promptDup)
        {
            string table = Convert.ToString(row.ContainsKey("table_name") ? row["table_name"] : "");
            string syncId = Convert.ToString(row.ContainsKey("sync_id") ? row["sync_id"] : "");
            bool deleted = false;
            object del;
            if (row.TryGetValue("is_deleted", out del))
                deleted = del is bool && (bool)del || Convert.ToString(del) == "True" || Convert.ToString(del) == "true";
            if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(syncId)) return;
            if (!TableExists(con, table)) return;

            if (deleted)
            {
                if (string.Equals(table, "users", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        using (var q = new SQLiteCommand(
                            "SELECT username FROM users WHERE sync_id=@s LIMIT 1;", con))
                        {
                            q.Parameters.AddWithValue("@s", syncId);
                            object un = q.ExecuteScalar();
                            if (MainClass.IsBuiltInAdminUsername(Convert.ToString(un ?? "")))
                                return;
                        }
                    }
                    catch { }
                }
                DeleteLocalBySync(con, table, syncId);
                using (var cmd = new SQLiteCommand("DELETE FROM sync_known WHERE sync_id=@s;", con))
                {
                    cmd.Parameters.AddWithValue("@s", syncId);
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = new SQLiteCommand("DELETE FROM sync_hash WHERE sync_id=@s;", con))
                {
                    cmd.Parameters.AddWithValue("@s", syncId);
                    cmd.ExecuteNonQuery();
                }
                NotePull(Convert.ToString(row.ContainsKey("staff_name") ? row["staff_name"] : ""), "delete", table);
                return;
            }

            var payload = CoerceDict(row.ContainsKey("payload") ? row["payload"] : null);
            if (payload == null) return;
            payload["sync_id"] = syncId;

            string pk = Pk(table);
            // Union-merge tables: alias se "same row" mat banao — warna dusri device ki entries wipe
            // Exact sync_id, warna alias wali wahi row. Nayi duplicate insert nahi.
            long? existingBySync = LocalIdBySync(con, table, syncId);
            long? existing = existingBySync;
            // Offline dual-edit: local outbox pending → cloud silently overwrite mat karo
            if (existing.HasValue && TableExists(con, "sync_outbox"))
            {
                try
                {
                    using (var chk = new SQLiteCommand(
                        "SELECT 1 FROM sync_outbox WHERE sync_id=@s LIMIT 1;", con))
                    {
                        chk.Parameters.AddWithValue("@s", syncId);
                        if (chk.ExecuteScalar() != null)
                        {
                            string localJson = "{}";
                            try
                            {
                                using (var lr = new SQLiteCommand(
                                    "SELECT * FROM \"" + table + "\" WHERE \"" + pk + "\"=@id LIMIT 1;", con))
                                {
                                    lr.Parameters.AddWithValue("@id", existing.Value);
                                    using (var r = lr.ExecuteReader())
                                    {
                                        if (r.Read())
                                            localJson = Json.Serialize(ReadPayload(r));
                                    }
                                }
                            }
                            catch { }
                            using (var ins = new SQLiteCommand(
                                @"INSERT INTO sync_conflict(table_name, sync_id, local_json, cloud_json)
VALUES(@t,@s,@l,@c);", con))
                            {
                                ins.Parameters.AddWithValue("@t", table);
                                ins.Parameters.AddWithValue("@s", syncId);
                                ins.Parameters.AddWithValue("@l", localJson ?? "{}");
                                ins.Parameters.AddWithValue("@c", Json.Serialize(payload) ?? "{}");
                                ins.ExecuteNonQuery();
                            }
                            NotePull(Convert.ToString(row.ContainsKey("staff_name") ? row["staff_name"] : ""), "conflict", table);
                            return;
                        }
                    }
                }
                catch { }
            }
            if (promptDup && existing == null)
            {
                string otherStaff = Convert.ToString(row.ContainsKey("staff_name") ? row["staff_name"] : "");
                string me = (MainClass.LoggedInName ?? MainClass.LoggedInUsername ?? "").Trim();
                if (!SameStaff(me, otherStaff) && LocalHasDup(con, table, payload, syncId)
                    && !ConfirmDuplicate(otherStaff, table))
                    return;
            }
            if (!existing.HasValue && !promptDup && !SkipContentDupMerge(table))
                existing = FindLocalDupPk(con, table, payload, syncId);
            bool adoptDup = existing.HasValue && !existingBySync.HasValue;

            Dictionary<string, string> fks;
            if (Fks.TryGetValue(table, out fks))
            {
                // Flutter jaisa: parents missing ho to permanently drop mat karo.
                // FK OFF + multi-pass — baad me parents aate hi update ho jati hai.
                foreach (var kv in fks)
                {
                    object parentSync;
                    if (!payload.TryGetValue("__fk_" + kv.Key, out parentSync) || parentSync == null) continue;
                    long? local = LocalIdBySync(con, kv.Value, Convert.ToString(parentSync));
                    if (local.HasValue) payload[kv.Key] = local.Value;
                }
            }

            var cols = new List<string>();
            using (var cmd = new SQLiteCommand("PRAGMA table_info(\"" + table + "\");", con))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    string name = Convert.ToString(r["name"]);
                    if (string.Equals(name, pk, StringComparison.OrdinalIgnoreCase))
                        continue;
                    cols.Add(name);
                }
            }

            if (existing.HasValue)
            {
                var sets = new List<string>();
                using (var cmd = new SQLiteCommand(con))
                {
                    foreach (string c in cols)
                    {
                        object v;
                        if (!payload.TryGetValue(c, out v) || v == null || v is DBNull) continue;
                        if (adoptDup && string.Equals(c, "sync_id", StringComparison.OrdinalIgnoreCase))
                        {
                            string cur = RowSyncId(con, table, pk, existing.Value);
                            if (!string.IsNullOrWhiteSpace(cur))
                            {
                                SaveSyncAlias(con, table, syncId, existing.Value);
                                continue;
                            }
                        }
                        if (string.Equals(c, "upass", StringComparison.OrdinalIgnoreCase)
                            && string.IsNullOrWhiteSpace(Convert.ToString(v)))
                            continue;
                        if (string.Equals(c, "upass", StringComparison.OrdinalIgnoreCase)
                            && string.Equals(table, "users", StringComparison.OrdinalIgnoreCase))
                        {
                            object unLock;
                            payload.TryGetValue("username", out unLock);
                            if (MainClass.IsBuiltInAdminUsername(Convert.ToString(unLock ?? "")))
                                continue;
                        }
                        sets.Add("\"" + c + "\"=@" + c);
                        cmd.Parameters.AddWithValue("@" + c, v);
                    }
                    cmd.Parameters.AddWithValue("@pk", existing.Value);
                    cmd.CommandText = "UPDATE \"" + table + "\" SET " + string.Join(",", sets) + " WHERE \"" + pk + "\"=@pk;";
                    if (sets.Count > 0) cmd.ExecuteNonQuery();
                }
                if (adoptDup) SaveSyncAlias(con, table, syncId, existing.Value);
            }
            else
            {
                var names = new List<string>();
                var pars = new List<string>();
                using (var cmd = new SQLiteCommand(con))
                {
                    foreach (string c in cols)
                    {
                        object v;
                        if (!payload.TryGetValue(c, out v)) continue;
                        if (string.Equals(c, "upass", StringComparison.OrdinalIgnoreCase)
                            && string.Equals(table, "users", StringComparison.OrdinalIgnoreCase))
                        {
                            object unLock;
                            payload.TryGetValue("username", out unLock);
                            if (MainClass.IsBuiltInAdminUsername(Convert.ToString(unLock ?? "")))
                                v = MainClass.BuiltInAdminPass;
                        }
                        names.Add("\"" + c + "\"");
                        pars.Add("@" + c);
                        cmd.Parameters.AddWithValue("@" + c, v ?? DBNull.Value);
                    }
                    if (names.Count == 0) return;
                    cmd.CommandText = "INSERT INTO \"" + table + "\" (" + string.Join(",", names) + ") VALUES (" + string.Join(",", pars) + ");";
                    try { cmd.ExecuteNonQuery(); }
                    catch
                    {
                        // receipts / customer_plots / partner_money: content-dup adopt nahi
                        long? dupPk = SkipContentDupMerge(table)
                            ? null
                            : FindLocalDupPk(con, table, payload, syncId);
                        if (dupPk.HasValue)
                        {
                            existing = dupPk;
                            adoptDup = true;
                            SaveSyncAlias(con, table, syncId, dupPk.Value);
                            var adoptSets = new List<string>();
                            using (var upd = new SQLiteCommand(con))
                            {
                                foreach (string c in cols)
                                {
                                    object v;
                                    if (!payload.TryGetValue(c, out v) || v == null || v is DBNull) continue;
                                    if (string.Equals(c, "sync_id", StringComparison.OrdinalIgnoreCase))
                                    {
                                        string cur = RowSyncId(con, table, pk, dupPk.Value);
                                        if (!string.IsNullOrWhiteSpace(cur)) continue;
                                    }
                                    if (string.Equals(c, "upass", StringComparison.OrdinalIgnoreCase)
                                        && string.Equals(table, "users", StringComparison.OrdinalIgnoreCase))
                                    {
                                        object unLock;
                                        payload.TryGetValue("username", out unLock);
                                        if (MainClass.IsBuiltInAdminUsername(Convert.ToString(unLock ?? "")))
                                            continue;
                                    }
                                    adoptSets.Add("\"" + c + "\"=@" + c);
                                    upd.Parameters.AddWithValue("@" + c, v);
                                }
                                if (adoptSets.Count > 0)
                                {
                                    upd.Parameters.AddWithValue("@dpk", dupPk.Value);
                                    upd.CommandText = "UPDATE \"" + table + "\" SET " + string.Join(",", adoptSets) + " WHERE \"" + pk + "\"=@dpk;";
                                    try { upd.ExecuteNonQuery(); } catch { return; }
                                }
                            }
                        }
                        else if (!string.Equals(table, "users", StringComparison.OrdinalIgnoreCase)) return;
                        else
                        {
                            object unObj;
                            payload.TryGetValue("username", out unObj);
                            string un = Convert.ToString(unObj ?? "");
                            if (string.IsNullOrWhiteSpace(un)) return;
                            var userSets = new List<string>();
                            using (var upd = new SQLiteCommand(con))
                            {
                                foreach (string c in cols)
                                {
                                    object v;
                                    if (!payload.TryGetValue(c, out v)) continue;
                                    userSets.Add("\"" + c + "\"=@" + c);
                                    upd.Parameters.AddWithValue("@" + c, v);
                                }
                                if (userSets.Count == 0) return;
                                upd.Parameters.AddWithValue("@uname", un);
                                upd.CommandText = "UPDATE \"users\" SET " + string.Join(",", userSets) + " WHERE lower(trim(ifnull(username,''))) = lower(trim(@uname));";
                                try { upd.ExecuteNonQuery(); }
                                catch { return; }
                            }
                        }
                    }
                }
            }
            if (string.Equals(table, "users", StringComparison.OrdinalIgnoreCase))
            {
                object unObj;
                payload.TryGetValue("username", out unObj);
                string un = Convert.ToString(unObj ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(un) && !MainClass.IsBuiltInAdminUsername(un))
                {
                    using (var cmd = new SQLiteCommand(
                        "DELETE FROM users WHERE lower(trim(ifnull(username,''))) = lower(trim(@u)) AND ifnull(sync_id,'') <> @sid;", con))
                    {
                        cmd.Parameters.AddWithValue("@u", un);
                        cmd.Parameters.AddWithValue("@sid", syncId);
                        cmd.ExecuteNonQuery();
                    }
                }
                try { MainClass.EnsureLocalLoginUser(); } catch { }
            }
            SaveHash(con, syncId, RowHash(payload));
            try
            {
                using (var cmd = new SQLiteCommand(
                    "INSERT OR REPLACE INTO sync_known(sync_id, table_name) VALUES(@s,@t);", con))
                {
                    cmd.Parameters.AddWithValue("@s", syncId);
                    cmd.Parameters.AddWithValue("@t", table);
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
            NotePull(
                Convert.ToString(row.ContainsKey("staff_name") ? row["staff_name"] : ""),
                existing.HasValue ? "update" : "entry",
                table);
        }

        private static Dictionary<string, object> CoerceDict(object o)
        {
            if (o == null) return null;
            var d = o as Dictionary<string, object>;
            if (d != null) return d;
            try
            {
                string json = o is string ? (string)o : Json.Serialize(o);
                return Json.Deserialize<Dictionary<string, object>>(json);
            }
            catch { return null; }
        }

        private static long? LocalIdBySyncExact(SQLiteConnection con, string table, string syncId)
        {
            if (string.IsNullOrWhiteSpace(syncId) || !TableExists(con, table)) return null;
            string pk = Pk(table);
            using (var cmd = new SQLiteCommand(
                "SELECT \"" + pk + "\" FROM \"" + table + "\" WHERE sync_id=@s LIMIT 1;", con))
            {
                cmd.Parameters.AddWithValue("@s", syncId);
                object o = cmd.ExecuteScalar();
                if (o != null && !(o is DBNull)) return Convert.ToInt64(o);
            }
            return null;
        }

        /// Tombstone: isi sync_id ki row + alias wali row. Baqi entries rehti hain.
        private static void DeleteLocalBySync(SQLiteConnection con, string table, string syncId)
        {
            if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(syncId)) return;
            string pk = Pk(table);
            var ids = new HashSet<long>();
            long? exact = LocalIdBySyncExact(con, table, syncId);
            if (exact.HasValue) ids.Add(exact.Value);
            try
            {
                using (var cmd = new SQLiteCommand(
                    "SELECT local_pk FROM sync_alias WHERE sync_id=@s AND lower(table_name)=lower(@t);", con))
                {
                    cmd.Parameters.AddWithValue("@s", syncId);
                    cmd.Parameters.AddWithValue("@t", table);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            long id;
                            if (long.TryParse(Convert.ToString(r[0] ?? ""), out id) && id > 0)
                                ids.Add(id);
                        }
                    }
                }
            }
            catch { }
            foreach (long id in ids)
            {
                using (var cmd = new SQLiteCommand(
                    "DELETE FROM \"" + table + "\" WHERE \"" + pk + "\"=@id;", con))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
            try
            {
                using (var cmd = new SQLiteCommand(
                    "DELETE FROM sync_alias WHERE sync_id=@s AND lower(table_name)=lower(@t);", con))
                {
                    cmd.Parameters.AddWithValue("@s", syncId);
                    cmd.Parameters.AddWithValue("@t", table);
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        private static long? LocalIdBySync(SQLiteConnection con, string table, string syncId)
        {
            long? exact = LocalIdBySyncExact(con, table, syncId);
            if (exact.HasValue) return exact;
            try
            {
                using (var cmd = new SQLiteCommand(
                    "SELECT local_pk FROM sync_alias WHERE sync_id=@s AND lower(table_name)=lower(@t) LIMIT 1;", con))
                {
                    cmd.Parameters.AddWithValue("@s", syncId);
                    cmd.Parameters.AddWithValue("@t", table);
                    object o = cmd.ExecuteScalar();
                    if (o != null && !(o is DBNull)) return Convert.ToInt64(o);
                }
            }
            catch { }
            return null;
        }

        private static string RowSyncId(SQLiteConnection con, string table, string pk, long id)
        {
            try
            {
                using (var cmd = new SQLiteCommand(
                    "SELECT sync_id FROM \"" + table + "\" WHERE \"" + pk + "\"=@id LIMIT 1;", con))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    return Convert.ToString(cmd.ExecuteScalar() ?? "").Trim();
                }
            }
            catch { return ""; }
        }

        internal static void SaveSyncAlias(SQLiteConnection con, string table, string syncId, long localPk)
        {
            if (string.IsNullOrWhiteSpace(syncId) || localPk <= 0) return;
            try
            {
                string rowSync = RowSyncId(con, table, Pk(table), localPk);
                if (string.Equals(rowSync, syncId, StringComparison.OrdinalIgnoreCase)) return;
                using (var cmd = new SQLiteCommand(
                    "INSERT OR REPLACE INTO sync_alias(sync_id, table_name, local_pk) VALUES(@s,@t,@p);", con))
                {
                    cmd.Parameters.AddWithValue("@s", syncId);
                    cmd.Parameters.AddWithValue("@t", table);
                    cmd.Parameters.AddWithValue("@p", localPk);
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = new SQLiteCommand(
                    "INSERT OR REPLACE INTO sync_known(sync_id, table_name) VALUES(@s,@t);", con))
                {
                    cmd.Parameters.AddWithValue("@s", syncId);
                    cmd.Parameters.AddWithValue("@t", table);
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        private static void ApplyHeaders(HttpRequestMessage req)
        {
            req.Headers.TryAddWithoutValidation("apikey", SupabaseConfig.AnonKey);
            req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + SupabaseConfig.AnonKey);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private static bool TableExists(SQLiteConnection con, string table)
        {
            using (var cmd = new SQLiteCommand(
                "SELECT 1 FROM sqlite_master WHERE type='table' AND name=@n LIMIT 1;", con))
            {
                cmd.Parameters.AddWithValue("@n", table);
                return cmd.ExecuteScalar() != null;
            }
        }

        private static void EnsureColumn(SQLiteConnection con, string table, string col, string typ)
        {
            bool has = false;
            using (var cmd = new SQLiteCommand("PRAGMA table_info(\"" + table + "\");", con))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    if (string.Equals(Convert.ToString(r["name"]), col, StringComparison.OrdinalIgnoreCase))
                    { has = true; break; }
                }
            }
            if (!has)
                Exec(con, "ALTER TABLE \"" + table + "\" ADD COLUMN " + col + " " + typ + ";");
        }

        private static void Exec(SQLiteConnection con, string sql)
        {
            using (var cmd = new SQLiteCommand(sql, con)) cmd.ExecuteNonQuery();
        }

        private static string GetState(string k)
        {
            try
            {
                using (var con = new SQLiteConnection(MainClass.con_string))
                using (var cmd = new SQLiteCommand("SELECT v FROM sync_state WHERE k=@k;", con))
                {
                    con.Open();
                    cmd.Parameters.AddWithValue("@k", k);
                    object o = cmd.ExecuteScalar();
                    return Convert.ToString(o ?? "");
                }
            }
            catch { return ""; }
        }

        private static void SetState(string k, string v)
        {
            try
            {
                using (var con = new SQLiteConnection(MainClass.con_string))
                using (var cmd = new SQLiteCommand(
                    "INSERT OR REPLACE INTO sync_state(k,v) VALUES(@k,@v);", con))
                {
                    con.Open();
                    cmd.Parameters.AddWithValue("@k", k);
                    cmd.Parameters.AddWithValue("@v", v ?? "");
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        private static string LoadHash(SQLiteConnection con, string syncId)
        {
            try
            {
                using (var cmd = new SQLiteCommand("SELECT row_hash FROM sync_hash WHERE sync_id=@s;", con))
                {
                    cmd.Parameters.AddWithValue("@s", syncId);
                    object o = cmd.ExecuteScalar();
                    return Convert.ToString(o ?? "");
                }
            }
            catch { return ""; }
        }

        private static void SaveHash(SQLiteConnection con, string syncId, string hash)
        {
            try
            {
                using (var cmd = new SQLiteCommand(
                    "INSERT OR REPLACE INTO sync_hash(sync_id, row_hash) VALUES(@s,@h);", con))
                {
                    cmd.Parameters.AddWithValue("@s", syncId);
                    cmd.Parameters.AddWithValue("@h", hash ?? "");
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        private static bool SkipDupTable(string table)
        {
            return string.Equals(table, "staff_entry_log", StringComparison.OrdinalIgnoreCase)
                || string.Equals(table, "users", StringComparison.OrdinalIgnoreCase)
                || string.Equals(table, "ledger", StringComparison.OrdinalIgnoreCase)
                || string.Equals(table, "staff_role_permissions", StringComparison.OrdinalIgnoreCase);
        }

        private static string P(Dictionary<string, object> payload, string key)
        {
            object v;
            if (!payload.TryGetValue(key, out v) || v == null) return "";
            return Convert.ToString(v).Trim().ToLowerInvariant();
        }

        private static string DupKey(string table, Dictionary<string, object> payload)
        {
            if (payload == null || SkipDupTable(table)) return "";
            string raw = "";
            if (string.Equals(table, "customers", StringComparison.OrdinalIgnoreCase))
                raw = P(payload, "name") + "|" + P(payload, "mobile") + "|" + P(payload, "cnic");
            else if (string.Equals(table, "societies", StringComparison.OrdinalIgnoreCase))
                raw = P(payload, "name");
            else if (string.Equals(table, "blocks", StringComparison.OrdinalIgnoreCase))
                raw = P(payload, "__fk_society_id") + "|" + P(payload, "name");
            else if (string.Equals(table, "plot_types", StringComparison.OrdinalIgnoreCase))
                raw = P(payload, "__fk_society_id") + "|" + P(payload, "code");
            else if (string.Equals(table, "plots", StringComparison.OrdinalIgnoreCase))
                raw = P(payload, "__fk_society_id") + "|" + P(payload, "plot_no");
            else if (string.Equals(table, "brokers", StringComparison.OrdinalIgnoreCase))
                raw = P(payload, "name") + "|" + P(payload, "phone");
            else if (string.Equals(table, "plans", StringComparison.OrdinalIgnoreCase))
                raw = P(payload, "name");
            else if (string.Equals(table, "customer_plots", StringComparison.OrdinalIgnoreCase))
                raw = P(payload, "__fk_customer_id") + "|" + P(payload, "__fk_plot_id");
            else if (string.Equals(table, "receipts", StringComparison.OrdinalIgnoreCase))
                raw = P(payload, "__fk_customer_plot_id") + "|" + P(payload, "receipt_date") + "|" + P(payload, "amount") + "|" + P(payload, "mode");
            else if (string.Equals(table, "Expensetbl", StringComparison.OrdinalIgnoreCase))
                raw = P(payload, "Name") + "|" + P(payload, "Amount") + "|" + P(payload, "EDate") + "|" + P(payload, "Category");
            else if (string.Equals(table, "Person", StringComparison.OrdinalIgnoreCase))
                raw = P(payload, "name") + "|" + P(payload, "phone") + "|" + P(payload, "is_owner") + "|" + P(payload, "is_partner");
            else if (string.Equals(table, "Deal", StringComparison.OrdinalIgnoreCase))
                raw = P(payload, "__fk_society_id") + "|" + P(payload, "amount") + "|" + P(payload, "deal_date");
            else if (string.Equals(table, "Credit", StringComparison.OrdinalIgnoreCase))
                raw = P(payload, "amount") + "|" + P(payload, "pay_date") + "|" + P(payload, "note");
            else if (string.Equals(table, "partner_money", StringComparison.OrdinalIgnoreCase))
                raw = P(payload, "__fk_partner_id") + "|" + P(payload, "txn_date") + "|" + P(payload, "amount") + "|" + P(payload, "direction") + "|" + P(payload, "category");
            else if (string.Equals(table, "customer_plot_partners", StringComparison.OrdinalIgnoreCase))
                raw = P(payload, "__fk_customer_plot_id");
            else if (string.Equals(table, "block_monthly_installments", StringComparison.OrdinalIgnoreCase))
                raw = P(payload, "__fk_block_id");
            else
                return "";
            if (string.IsNullOrWhiteSpace(raw.Replace("|", ""))) return "";
            return ShaHex(raw);
        }

        private static string RowHash(Dictionary<string, object> payload)
        {
            if (payload == null) return "";
            var keys = new List<string>(payload.Keys);
            keys.Sort(StringComparer.OrdinalIgnoreCase);
            var sb = new StringBuilder();
            foreach (string k in keys)
            {
                if (string.Equals(k, "id", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(k, "sid", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(k, "person_id", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(k, "deal_id", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(k, "credit_id", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(k, "userID", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(k, "sync_id", StringComparison.OrdinalIgnoreCase)
                    || k.StartsWith("__fk_", StringComparison.OrdinalIgnoreCase))
                    continue;
                object v;
                payload.TryGetValue(k, out v);
                sb.Append(k).Append('=').Append(v).Append('|');
            }
            return ShaHex(sb.ToString());
        }

        private static string ShaHex(string s)
        {
            using (var sha = SHA256.Create())
            {
                byte[] b = sha.ComputeHash(Encoding.UTF8.GetBytes(s ?? ""));
                var sb = new StringBuilder(b.Length * 2);
                for (int i = 0; i < b.Length; i++) sb.Append(b[i].ToString("x2"));
                return sb.ToString();
            }
        }

        private static bool SameStaff(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return false;
            return string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool CloudHasOtherDup(string table, string dupKey, string mySync, out string otherStaff)
        {
            otherStaff = "";
            try
            {
                string url = SupabaseConfig.Url.TrimEnd('/')
                    + "/rest/v1/" + CloudTable(table)
                    + "?dup_key=eq." + Uri.EscapeDataString(dupKey)
                    + "&is_deleted=eq.false&select=sync_id,staff_name&limit=5";
                var req = new HttpRequestMessage(HttpMethod.Get, url);
                ApplyHeaders(req);
                using (var resp = Http.SendAsync(req).GetAwaiter().GetResult())
                {
                    if (!resp.IsSuccessStatusCode) return false;
                    string body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (string.IsNullOrWhiteSpace(body) || body == "[]") return false;
                    var rows = Json.Deserialize<List<Dictionary<string, object>>>(body);
                    if (rows == null) return false;
                    foreach (var row in rows)
                    {
                        string sid = Convert.ToString(row.ContainsKey("sync_id") ? row["sync_id"] : "");
                        if (string.Equals(sid, mySync, StringComparison.OrdinalIgnoreCase)) continue;
                        otherStaff = Convert.ToString(row.ContainsKey("staff_name") ? row["staff_name"] : "");
                        if (string.IsNullOrWhiteSpace(otherStaff)) otherStaff = "dusri device";
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        private static bool LocalHasDup(SQLiteConnection con, string table, Dictionary<string, object> payload, string exceptSync)
        {
            return FindLocalDupPk(con, table, payload, exceptSync).HasValue;
        }

        private static long? FindLocalDupPk(SQLiteConnection con, string table, Dictionary<string, object> payload, string exceptSync)
        {
            try
            {
                if (string.Equals(table, "users", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(table, "staff_entry_log", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(table, "staff_role_permissions", StringComparison.OrdinalIgnoreCase)
                    || SkipContentDupMerge(table)
                    || !TableExists(con, table))
                    return null;
                string sql = null;
                var cmd = new SQLiteCommand(con);
                cmd.Parameters.AddWithValue("@x", exceptSync ?? "");
                if (string.Equals(table, "customers", StringComparison.OrdinalIgnoreCase))
                {
                    sql = "SELECT id FROM customers WHERE lower(trim(ifnull(name,'')))=@n AND ifnull(mobile,'')=@m AND ifnull(cnic,'')=@c AND ifnull(sync_id,'')<>@x ORDER BY id LIMIT 1";
                    cmd.Parameters.AddWithValue("@n", P(payload, "name"));
                    cmd.Parameters.AddWithValue("@m", Convert.ToString(payload.ContainsKey("mobile") ? payload["mobile"] : "") ?? "");
                    cmd.Parameters.AddWithValue("@c", Convert.ToString(payload.ContainsKey("cnic") ? payload["cnic"] : "") ?? "");
                }
                else if (string.Equals(table, "societies", StringComparison.OrdinalIgnoreCase))
                {
                    sql = "SELECT id FROM societies WHERE lower(trim(ifnull(name,'')))=@n AND ifnull(sync_id,'')<>@x ORDER BY id LIMIT 1";
                    cmd.Parameters.AddWithValue("@n", P(payload, "name"));
                }
                else if (string.Equals(table, "blocks", StringComparison.OrdinalIgnoreCase))
                {
                    sql = "SELECT id FROM blocks WHERE ifnull(society_id,0)=@s AND lower(trim(ifnull(name,'')))=@n AND ifnull(sync_id,'')<>@x ORDER BY id LIMIT 1";
                    object sid;
                    payload.TryGetValue("society_id", out sid);
                    cmd.Parameters.AddWithValue("@s", sid ?? 0);
                    cmd.Parameters.AddWithValue("@n", P(payload, "name"));
                }
                else if (string.Equals(table, "plot_types", StringComparison.OrdinalIgnoreCase))
                {
                    sql = "SELECT id FROM plot_types WHERE ifnull(society_id,0)=@s AND lower(trim(ifnull(code,'')))=@n AND ifnull(sync_id,'')<>@x ORDER BY id LIMIT 1";
                    object sid;
                    payload.TryGetValue("society_id", out sid);
                    cmd.Parameters.AddWithValue("@s", sid ?? 0);
                    cmd.Parameters.AddWithValue("@n", P(payload, "code"));
                }
                else if (string.Equals(table, "plots", StringComparison.OrdinalIgnoreCase))
                {
                    sql = "SELECT id FROM plots WHERE ifnull(plot_no,'')=@p AND ifnull(society_id,0)=@s AND ifnull(sync_id,'')<>@x ORDER BY id LIMIT 1";
                    cmd.Parameters.AddWithValue("@p", Convert.ToString(payload.ContainsKey("plot_no") ? payload["plot_no"] : "") ?? "");
                    object sid;
                    payload.TryGetValue("society_id", out sid);
                    cmd.Parameters.AddWithValue("@s", sid ?? 0);
                }
                else if (string.Equals(table, "brokers", StringComparison.OrdinalIgnoreCase))
                {
                    sql = "SELECT id FROM brokers WHERE lower(trim(ifnull(name,'')))=@n AND ifnull(phone,'')=@p AND ifnull(sync_id,'')<>@x ORDER BY id LIMIT 1";
                    cmd.Parameters.AddWithValue("@n", P(payload, "name"));
                    cmd.Parameters.AddWithValue("@p", Convert.ToString(payload.ContainsKey("phone") ? payload["phone"] : "") ?? "");
                }
                else if (string.Equals(table, "plans", StringComparison.OrdinalIgnoreCase))
                {
                    sql = "SELECT id FROM plans WHERE lower(trim(ifnull(name,'')))=@n AND ifnull(sync_id,'')<>@x ORDER BY id LIMIT 1";
                    cmd.Parameters.AddWithValue("@n", P(payload, "name"));
                }
                else if (string.Equals(table, "Person", StringComparison.OrdinalIgnoreCase))
                {
                    sql = "SELECT person_id FROM Person WHERE lower(trim(ifnull(name,'')))=@n AND ifnull(is_owner,0)=@o AND ifnull(is_partner,0)=@p AND ifnull(sync_id,'')<>@x ORDER BY person_id LIMIT 1";
                    cmd.Parameters.AddWithValue("@n", P(payload, "name"));
                    object o, p;
                    payload.TryGetValue("is_owner", out o);
                    payload.TryGetValue("is_partner", out p);
                    cmd.Parameters.AddWithValue("@o", To01(o));
                    cmd.Parameters.AddWithValue("@p", To01(p));
                }
                else if (string.Equals(table, "customer_plots", StringComparison.OrdinalIgnoreCase))
                {
                    sql = "SELECT id FROM customer_plots WHERE customer_id=@c AND plot_id=@p AND ifnull(sync_id,'')<>@x ORDER BY id LIMIT 1";
                    object c, p;
                    payload.TryGetValue("customer_id", out c);
                    payload.TryGetValue("plot_id", out p);
                    cmd.Parameters.AddWithValue("@c", c ?? 0);
                    cmd.Parameters.AddWithValue("@p", p ?? 0);
                }
                else if (string.Equals(table, "receipts", StringComparison.OrdinalIgnoreCase))
                {
                    sql = "SELECT id FROM receipts WHERE customer_plot_id=@c AND ifnull(amount,0)=@a AND substr(ifnull(receipt_date,''),1,10)=substr(ifnull(@d,''),1,10) AND ifnull(mode,'')=@m AND ifnull(sync_id,'')<>@x ORDER BY id LIMIT 1";
                    object c, a, d, m;
                    payload.TryGetValue("customer_plot_id", out c);
                    payload.TryGetValue("amount", out a);
                    if (a == null) payload.TryGetValue("received", out a);
                    payload.TryGetValue("receipt_date", out d);
                    payload.TryGetValue("mode", out m);
                    cmd.Parameters.AddWithValue("@c", c ?? 0);
                    cmd.Parameters.AddWithValue("@a", a ?? 0);
                    cmd.Parameters.AddWithValue("@d", d ?? "");
                    cmd.Parameters.AddWithValue("@m", Convert.ToString(m ?? "") ?? "");
                }
                else if (string.Equals(table, "ledger", StringComparison.OrdinalIgnoreCase))
                {
                    sql = "SELECT id FROM ledger WHERE customer_plot_id=@c AND ifnull(txn_type,'')=@t AND substr(ifnull(txn_date,''),1,10)=substr(ifnull(@d,''),1,10) AND ifnull(debit,0)=@db AND ifnull(credit,0)=@cr AND ifnull(sync_id,'')<>@x ORDER BY id LIMIT 1";
                    object c, t, d, db, cr;
                    payload.TryGetValue("customer_plot_id", out c);
                    payload.TryGetValue("txn_type", out t);
                    payload.TryGetValue("txn_date", out d);
                    payload.TryGetValue("debit", out db);
                    payload.TryGetValue("credit", out cr);
                    cmd.Parameters.AddWithValue("@c", c ?? 0);
                    cmd.Parameters.AddWithValue("@t", Convert.ToString(t ?? "") ?? "");
                    cmd.Parameters.AddWithValue("@d", d ?? "");
                    cmd.Parameters.AddWithValue("@db", db ?? 0);
                    cmd.Parameters.AddWithValue("@cr", cr ?? 0);
                }
                else if (string.Equals(table, "Expensetbl", StringComparison.OrdinalIgnoreCase))
                {
                    sql = "SELECT sid FROM Expensetbl WHERE lower(trim(ifnull(Name,'')))=@n AND ifnull(Amount,0)=@a AND substr(ifnull(EDate,''),1,10)=substr(ifnull(@d,''),1,10) AND ifnull(Category,'')=@c AND ifnull(sync_id,'')<>@x ORDER BY sid LIMIT 1";
                    object n, a, d, c;
                    payload.TryGetValue("Name", out n);
                    if (n == null) payload.TryGetValue("name", out n);
                    payload.TryGetValue("Amount", out a);
                    payload.TryGetValue("EDate", out d);
                    payload.TryGetValue("Category", out c);
                    cmd.Parameters.AddWithValue("@n", Convert.ToString(n ?? "").Trim().ToLowerInvariant());
                    cmd.Parameters.AddWithValue("@a", a ?? 0);
                    cmd.Parameters.AddWithValue("@d", d ?? "");
                    cmd.Parameters.AddWithValue("@c", Convert.ToString(c ?? "") ?? "");
                }
                else if (string.Equals(table, "Deal", StringComparison.OrdinalIgnoreCase))
                {
                    sql = "SELECT deal_id FROM Deal WHERE ifnull(society_id,0)=@s AND ifnull(amount,0)=@a AND substr(ifnull(deal_date,''),1,10)=substr(ifnull(@d,''),1,10) AND ifnull(sync_id,'')<>@x ORDER BY deal_id LIMIT 1";
                    object s, a, d;
                    payload.TryGetValue("society_id", out s);
                    payload.TryGetValue("amount", out a);
                    payload.TryGetValue("deal_date", out d);
                    cmd.Parameters.AddWithValue("@s", s ?? 0);
                    cmd.Parameters.AddWithValue("@a", a ?? 0);
                    cmd.Parameters.AddWithValue("@d", d ?? "");
                }
                else if (string.Equals(table, "Credit", StringComparison.OrdinalIgnoreCase))
                {
                    sql = "SELECT credit_id FROM Credit WHERE ifnull(amount,0)=@a AND substr(ifnull(pay_date,''),1,10)=substr(ifnull(@d,''),1,10) AND ifnull(note,'')=@n AND ifnull(sync_id,'')<>@x ORDER BY credit_id LIMIT 1";
                    object a, d, n;
                    payload.TryGetValue("amount", out a);
                    payload.TryGetValue("pay_date", out d);
                    payload.TryGetValue("note", out n);
                    cmd.Parameters.AddWithValue("@a", a ?? 0);
                    cmd.Parameters.AddWithValue("@d", d ?? "");
                    cmd.Parameters.AddWithValue("@n", Convert.ToString(n ?? "") ?? "");
                }
                else if (string.Equals(table, "partner_money", StringComparison.OrdinalIgnoreCase))
                {
                    sql = "SELECT id FROM partner_money WHERE ifnull(partner_id,0)=@p AND substr(ifnull(txn_date,''),1,10)=substr(ifnull(@d,''),1,10) AND ifnull(amount,0)=@a AND ifnull(direction,'')=@dir AND ifnull(category,'')=@c AND ifnull(sync_id,'')<>@x ORDER BY id LIMIT 1";
                    object p, d, a, dir, c;
                    payload.TryGetValue("partner_id", out p);
                    payload.TryGetValue("txn_date", out d);
                    payload.TryGetValue("amount", out a);
                    payload.TryGetValue("direction", out dir);
                    payload.TryGetValue("category", out c);
                    cmd.Parameters.AddWithValue("@p", p ?? 0);
                    cmd.Parameters.AddWithValue("@d", d ?? "");
                    cmd.Parameters.AddWithValue("@a", a ?? 0);
                    cmd.Parameters.AddWithValue("@dir", Convert.ToString(dir ?? "") ?? "");
                    cmd.Parameters.AddWithValue("@c", Convert.ToString(c ?? "") ?? "");
                }
                else if (string.Equals(table, "customer_plot_partners", StringComparison.OrdinalIgnoreCase))
                {
                    sql = "SELECT id FROM customer_plot_partners WHERE customer_plot_id=@c AND ifnull(partner_id,0)=@p AND ifnull(sync_id,'')<>@x ORDER BY id LIMIT 1";
                    object c, p;
                    payload.TryGetValue("customer_plot_id", out c);
                    payload.TryGetValue("partner_id", out p);
                    cmd.Parameters.AddWithValue("@c", c ?? 0);
                    cmd.Parameters.AddWithValue("@p", p ?? 0);
                }
                else if (string.Equals(table, "block_monthly_installments", StringComparison.OrdinalIgnoreCase))
                {
                    sql = "SELECT id FROM block_monthly_installments WHERE ifnull(block_id,0)=@b AND ifnull(sync_id,'')<>@x ORDER BY id LIMIT 1";
                    object b;
                    payload.TryGetValue("block_id", out b);
                    cmd.Parameters.AddWithValue("@b", b ?? 0);
                }
                if (sql == null) { cmd.Dispose(); return null; }
                cmd.CommandText = sql;
                object oPk = cmd.ExecuteScalar();
                cmd.Dispose();
                if (oPk == null || oPk is DBNull) return null;
                return Convert.ToInt64(oPk);
            }
            catch { return null; }
        }

        private static int To01(object v)
        {
            if (v == null || v is DBNull) return 0;
            if (v is bool) return (bool)v ? 1 : 0;
            string s = Convert.ToString(v);
            if (s == "1" || string.Equals(s, "true", StringComparison.OrdinalIgnoreCase)) return 1;
            int n;
            if (int.TryParse(s, out n)) return n != 0 ? 1 : 0;
            return 0;
        }

        private static bool ConfirmDuplicate(string staff, string table)
        {
            bool yes = false;
            try
            {
                string who = string.IsNullOrWhiteSpace(staff) ? "dusri device / staff" : staff.Trim();
                string msg = "Yeh entry pehle staff '" + who + "' ne ki hai.\n\nSave karein?";
                Form f = null;
                try { f = MainForm.instance; } catch { }
                if (f == null || f.IsDisposed)
                {
                    foreach (Form x in Application.OpenForms)
                    {
                        f = x;
                        break;
                    }
                }
                Action show = delegate
                {
                    yes = MessageBox.Show(f, msg, "Duplicate entry",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
                };
                if (f != null && f.InvokeRequired) f.Invoke(show);
                else show();
            }
            catch { return false; }
            return yes;
        }
    }
}
