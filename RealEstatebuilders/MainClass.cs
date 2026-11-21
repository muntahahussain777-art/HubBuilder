using HubBuilders.ProjectConnection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HubBuilders
{
    public class MainClass
    {
        // Connection string ko ProjectConnection class se le rahe hain
        public static string con_string = projectconnection.ConnectionString;  // Assuming projectconnection.ConnectionString is the SQLite connection string

        // SQLiteConnection object (replace SqlConnection with SQLiteConnection)
        public static SQLiteConnection con = new SQLiteConnection(con_string);

        /// <summary>Shared connection pe active work/transaction — helpers Close nahi karenge jab tak &gt; 0.</summary>
        private static int _sharedWorkDepth = 0;
        private static readonly object _conLock = new object();

        public static string LoggedInRole { get; private set; }
        public static string LoggedInUsername { get; private set; }
        public static string LoggedInName { get; private set; }

        // ✅ Role Set Karne Ka Function (purana call bhi chalega)
        public static void SetRole(string role)
        {
            LoggedInRole = NormalizeRole(role);
            try { StaffAccess.Invalidate(); } catch { }
        }

        /// <summary>Login ke baad staff session — entries pe kaun tha yeh track hota hai.</summary>
        public static void SetSession(string username, string displayName, string role)
        {
            LoggedInUsername = username ?? "";
            LoggedInName = string.IsNullOrWhiteSpace(displayName) ? (username ?? "") : displayName.Trim();
            LoggedInRole = NormalizeRole(role);
            try { EnsureStaffAuditSchema(); } catch { }
            try { StaffAccess.Invalidate(); } catch { }
        }

        /// <summary>Logout — stale role/audit identity clear.</summary>
        public static void ClearSession()
        {
            LoggedInUsername = "";
            LoggedInName = "";
            LoggedInRole = "";
            try { StaffAccess.Invalidate(); } catch { }
        }

        private static string NormalizeRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role)) return "";
            string r = role.Trim();
            if (string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase))
                return "Admin";
            return r;
        }

        public static bool IsAdmin =>
            string.Equals(LoggedInRole, "Admin", StringComparison.OrdinalIgnoreCase);

        /// <summary>Restore ke baad schema flags reset — next open pe migrate dobara chal sake.</summary>
        public static void ResetSchemaCaches()
        {
            _allocationSchemaReady = false;
            _customerPlotsColumnsReady = false;
            _plotStatusRepairDone = false;
            _dealTypeNormalizeDone = false;
            _staffAuditReady = false;
            _partnerMoneyReady = false;
            _expenseCategoryReady = false;
            _customerSchemaReady = false;
        }

        /// <summary>Purani DB restore ke baad — flags reset + nayi tables.</summary>
        public static void EnsureAdditiveSchemasAfterRestore()
        {
            ResetSchemaCaches();
            EnsureAdditiveSchemasOnStartup();
        }

        /// <summary>Login / startup — shared DB lock na ho, sirf missing tables add.</summary>
        public static void EnsureAdditiveSchemasOnStartup()
        {
            try
            {
                EnsureStaffAuditSchema();
                EnsurePartnerMoneySchema();
                EnsureExpenseCategorySchema();
                EnsureCustomerSchema();
                EnsureAllocationSchema(); // deal_type Installment/Reserve/Sold (old rows → Installment)
                try { StaffAccess.EnsureSchema(); } catch { }
                try { BlockMonthlyInstallmentStore.EnsureSchema(); } catch { }
                try { HubSyncService.ReconcileOneActiveCustomerPlots(); } catch { }
                try { BlockMonthlyInstallmentStore.ApplyToEmptyActive(); } catch { }
                HubSyncService.EnsureLocalSchema();
                try { HubDuplicateCleanup.Run(); } catch { }
            }
            catch { }
        }

        /// <summary>
        /// Built-in Admin login — software ke sath lock. Supabase / form se change nahi.
        /// </summary>
        public const string BuiltInAdminUser = "Admin";
        public const string BuiltInAdminPass = "hubbuilder786";

        public static bool IsBuiltInAdminUsername(string username)
        {
            return string.Equals((username ?? "").Trim(), BuiltInAdminUser, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsBuiltInAdminLogin(string username, string password)
        {
            return IsBuiltInAdminUsername(username)
                && string.Equals((password ?? "").Trim(), BuiltInAdminPass, StringComparison.Ordinal);
        }

        /// <summary>
        /// Login hamesha local SQLite + built-in Admin. Internet na ho to bhi chale.
        /// Admin row delete/overwrite se recover — password hamesha hubbuilder786.
        /// </summary>
        public static void EnsureLocalLoginUser()
        {
            try
            {
                using (var local = new SQLiteConnection(con_string))
                {
                    local.Open();
                    using (var chk = new SQLiteCommand(
                        "SELECT 1 FROM sqlite_master WHERE type='table' AND name='users' LIMIT 1;", local))
                    {
                        if (chk.ExecuteScalar() == null) return;
                    }
                    object exists;
                    using (var cmd = new SQLiteCommand(
                        "SELECT userID FROM users WHERE lower(trim(username)) = 'admin' LIMIT 1;", local))
                        exists = cmd.ExecuteScalar();
                    if (exists != null && exists != DBNull.Value)
                    {
                        using (var cmd = new SQLiteCommand(@"
UPDATE users
SET username=@u, upass=@p, Role=@r,
    uName = CASE WHEN IFNULL(TRIM(uName),'')='' THEN @n ELSE uName END
WHERE lower(trim(username)) = 'admin';", local))
                        {
                            cmd.Parameters.AddWithValue("@u", BuiltInAdminUser);
                            cmd.Parameters.AddWithValue("@p", BuiltInAdminPass);
                            cmd.Parameters.AddWithValue("@r", "Admin");
                            cmd.Parameters.AddWithValue("@n", "Administrator");
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        using (var cmd = new SQLiteCommand(
                            "INSERT INTO users (username, upass, uName, uphone, Role) VALUES (@u,@p,@n,'',@r);", local))
                        {
                            cmd.Parameters.AddWithValue("@u", BuiltInAdminUser);
                            cmd.Parameters.AddWithValue("@p", BuiltInAdminPass);
                            cmd.Parameters.AddWithValue("@n", "Administrator");
                            cmd.Parameters.AddWithValue("@r", "Admin");
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>Money/multi-step ops: shared con open rakho, helpers Close na karen.</summary>
        public static void AcquireSharedConnection()
        {
            lock (_conLock)
            {
                if (con.State != ConnectionState.Open)
                    con.Open();
                _sharedWorkDepth++;
            }
        }

        public static void ReleaseSharedConnection()
        {
            lock (_conLock)
            {
                if (_sharedWorkDepth > 0)
                    _sharedWorkDepth--;
                if (_sharedWorkDepth <= 0)
                {
                    _sharedWorkDepth = 0;
                    AppSafety.SafeClose(con);
                }
            }
        }

        private static void EnsureSharedOpen()
        {
            if (con.State != ConnectionState.Open)
                con.Open();
        }

        private static void ReleaseSharedIfIdle()
        {
            if (_sharedWorkDepth > 0) return;
            AppSafety.SafeClose(con);
        }

        private static bool _staffAuditReady = false;

        /// <summary>
        /// Sirf NAYI table — purani tables/columns touch nahi.
        /// Old DB restore pe disturb nahi: CREATE IF NOT EXISTS.
        /// </summary>
        public static void EnsureStaffAuditSchema()
        {
            if (_staffAuditReady) return;
            try
            {
                if (con.State != ConnectionState.Open)
                    con.Open();
                using (var cmd = new SQLiteCommand(@"
CREATE TABLE IF NOT EXISTS staff_entry_log (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  action_type TEXT NOT NULL,
  module_name TEXT NOT NULL,
  detail TEXT,
  staff_username TEXT,
  staff_name TEXT,
  staff_role TEXT,
  created_at TEXT DEFAULT (datetime('now','localtime'))
);", con))
                {
                    cmd.ExecuteNonQuery();
                }
                _staffAuditReady = true;
            }
            catch
            {
                // Schema fail pe app mat todo — audit optional hai
            }
            finally
            {
                ReleaseSharedIfIdle();
            }
        }

        /// <summary>Staff entry log — UI change ke baghair kaun ne kya kiya.</summary>
        public static void LogStaffEntry(string actionType, string moduleName, string detail = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(LoggedInUsername) && string.IsNullOrWhiteSpace(LoggedInRole))
                    return;

                // Alag connection — shared MainClass.con / transactions disturb na hon
                using (var local = new SQLiteConnection(con_string))
                {
                    local.Open();
                    using (var create = new SQLiteCommand(@"
CREATE TABLE IF NOT EXISTS staff_entry_log (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  action_type TEXT NOT NULL,
  module_name TEXT NOT NULL,
  detail TEXT,
  staff_username TEXT,
  staff_name TEXT,
  staff_role TEXT,
  created_at TEXT DEFAULT (datetime('now','localtime'))
);", local))
                    {
                        create.ExecuteNonQuery();
                    }

                    using (var cmd = new SQLiteCommand(@"
INSERT INTO staff_entry_log (action_type, module_name, detail, staff_username, staff_name, staff_role)
VALUES (@a, @m, @d, @u, @n, @r);", local))
                    {
                        cmd.Parameters.AddWithValue("@a", actionType ?? "");
                        cmd.Parameters.AddWithValue("@m", moduleName ?? "");
                        cmd.Parameters.AddWithValue("@d", (object)(detail ?? (object)DBNull.Value));
                        cmd.Parameters.AddWithValue("@u", LoggedInUsername ?? "");
                        cmd.Parameters.AddWithValue("@n", LoggedInName ?? "");
                        cmd.Parameters.AddWithValue("@r", LoggedInRole ?? "");
                        cmd.ExecuteNonQuery();
                    }
                }
                _staffAuditReady = true;
                try { HubSyncService.MarkDirty(); } catch { }
            }
            catch
            {
                // Audit fail se business save mat roko
            }
        }

        private static void TryAutoAuditFromSql(string qry)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(qry)) return;
                string s = qry.TrimStart();
                if (s.Length < 6) return;

                string head = s.Length >= 6 ? s.Substring(0, 6).ToUpperInvariant() : s.ToUpperInvariant();
                string action = null;
                if (head.StartsWith("INSERT")) action = "Insert";
                else if (head.StartsWith("UPDATE")) action = "Update";
                else if (head.StartsWith("DELETE")) action = "Delete";
                if (action == null) return;

                // Avoid recursion / noise
                if (s.IndexOf("staff_entry_log", StringComparison.OrdinalIgnoreCase) >= 0) return;
                if (s.IndexOf("sqlite_master", StringComparison.OrdinalIgnoreCase) >= 0) return;

                string module = ExtractSqlTableName(s, action);
                if (string.IsNullOrWhiteSpace(module)) module = "Unknown";

                string detail = s.Length > 180 ? s.Substring(0, 180) + "…" : s;
                LogStaffEntry(action, module, detail);
            }
            catch { }
        }

        /// <summary>Internal tables — check TABLE NAME only (not full SQL text).</summary>
        private static bool IsInternalSyncTable(string table)
        {
            if (string.IsNullOrWhiteSpace(table)) return true;
            string t = table.Trim().Trim('`', '[', ']', '"', '\'');
            if (t.Equals("staff_entry_log", StringComparison.OrdinalIgnoreCase)) return true;
            if (t.Equals("sqlite_master", StringComparison.OrdinalIgnoreCase)) return true;
            if (t.StartsWith("sync_", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>Test hook: background PushOneNow / Schedule band — outbox verify ke liye.</summary>
        public static bool SuppressBackgroundSync { get; set; }

        /// <summary>
        /// INSERT/UPDATE ke baad: sync_id ensure + outbox SAME txn mein.
        /// Internal table → false (skip). Business table outbox fail → throw (caller rollback).
        /// </summary>
        private static bool TryEnqueueOutboxInTxn(
            SQLiteConnection c, SQLiteTransaction tx,
            string qry, int res, long lastInsertId, System.Collections.IDictionary parameters,
            out string table, out long rowId, out string outboxRev)
        {
            table = null;
            rowId = 0;
            outboxRev = "";
            if (res <= 0 || c == null) return false;
            string head = (qry ?? "").TrimStart();
            bool isInsert = head.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase);
            bool isUpdate = head.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase);
            if (!isInsert && !isUpdate) return false;
            string action = isInsert ? "Insert" : "Update";
            table = ExtractSqlTableName(head, action);
            rowId = isInsert ? lastInsertId : IdFromParams(parameters);
            if (string.IsNullOrWhiteSpace(table) || rowId <= 0) return false;
            if (IsInternalSyncTable(table))
            {
                table = null;
                rowId = 0;
                return false;
            }
            // Ensure sync_outbox exists (schema 11) without leaving txn
            using (var create = tx == null
                ? new SQLiteCommand(@"CREATE TABLE IF NOT EXISTS sync_outbox (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  table_name TEXT NOT NULL,
  local_pk INTEGER NOT NULL,
  sync_id TEXT NOT NULL,
  revision TEXT NOT NULL,
  op TEXT NOT NULL DEFAULT 'upsert',
  created_at TEXT DEFAULT (datetime('now'))
);", c)
                : new SQLiteCommand(@"CREATE TABLE IF NOT EXISTS sync_outbox (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  table_name TEXT NOT NULL,
  local_pk INTEGER NOT NULL,
  sync_id TEXT NOT NULL,
  revision TEXT NOT NULL,
  op TEXT NOT NULL DEFAULT 'upsert',
  created_at TEXT DEFAULT (datetime('now'))
);", c, tx))
            {
                create.ExecuteNonQuery();
            }
            string syncIdNow = HubSyncService.EnsureSyncIdOnConnection(c, tx, table, rowId);
            if (string.IsNullOrWhiteSpace(syncIdNow))
                throw new InvalidOperationException("sync_id ensure failed for table " + table + " id=" + rowId);
            outboxRev = HubSyncService.EnqueueOutboxOnConnection(c, tx, table, rowId, syncIdNow, "upsert");
            if (string.IsNullOrWhiteSpace(outboxRev))
                throw new InvalidOperationException("sync_outbox enqueue failed for table " + table + " id=" + rowId);
            return true;
        }

        private static void ScheduleFastPush(string table, long rowId, string outboxRev)
        {
            if (SuppressBackgroundSync) return;
            if (string.IsNullOrWhiteSpace(table) || rowId <= 0) return;
            try { HubSyncService.RaiseLiveChanged(); } catch { }
            try { HubSyncService.MarkDirtyTable(table); } catch { }
            string revCapture = outboxRev ?? "";
            string tableCapture = table;
            long idCapture = rowId;
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    if (HubSyncService.PushOneNow(tableCapture, idCapture, revCapture))
                        HubSyncService.ClearDirty();
                    else
                        HubSyncService.MarkDirtyTable(tableCapture);
                    HubSyncService.ScheduleBackgroundPushPublic();
                }
                catch
                {
                    try { HubSyncService.MarkDirtyTable(tableCapture); } catch { }
                    try { HubSyncService.ScheduleBackgroundPushPublic(); } catch { }
                }
            });
        }

        private static void AfterSuccessfulWrite(string qry, int res, long lastInsertId, System.Collections.IDictionary parameters, bool skipOutboxEnqueue = false)
        {
            if (res <= 0) return;
            try
            {
                string head = (qry ?? "").TrimStart();
                bool isInsert = head.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase);
                bool isUpdate = head.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase);
                bool isDelete = head.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase);
                long idForLog = 0;
                if (isInsert && lastInsertId > 0) idForLog = lastInsertId;
                else if (isUpdate || isDelete) idForLog = IdFromParams(parameters);

                string s = (qry ?? "").TrimStart();
                string action = isInsert ? "Insert" : isUpdate ? "Update" : isDelete ? "Delete" : null;
                if (action != null)
                {
                    string module = ExtractSqlTableName(s, action);
                    if (!IsInternalSyncTable(module))
                    {
                        if (string.IsNullOrWhiteSpace(module)) module = "Unknown";
                        string detail = idForLog > 0
                            ? ("id=" + idForLog)
                            : (s.Length > 180 ? s.Substring(0, 180) + "…" : s);
                        LogStaffEntry(action, module, detail);
                    }
                }
            }
            catch { }
            try
            {
                string head = (qry ?? "").TrimStart();
                bool isInsert = head.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase);
                bool isUpdate = head.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase);
                bool isDelete = head.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase);
                if ((isInsert || isUpdate) && !skipOutboxEnqueue)
                {
                    // Fallback path — preferred path is TryEnqueueOutboxInTxn (same SQLite txn as write)
                    string action = isInsert ? "Insert" : "Update";
                    string table = ExtractSqlTableName(head, action);
                    long rowId = isInsert ? lastInsertId : IdFromParams(parameters);
                    if (!IsInternalSyncTable(table) && !string.IsNullOrWhiteSpace(table) && rowId > 0)
                    {
                        string syncIdNow = "";
                        string outboxRev = "";
                        try
                        {
                            using (var c = new System.Data.SQLite.SQLiteConnection(con_string))
                            {
                                c.Open();
                                syncIdNow = HubSyncService.EnsureSyncIdOnConnection(c, null, table, rowId);
                            }
                            if (!string.IsNullOrWhiteSpace(syncIdNow))
                                outboxRev = HubSyncService.EnqueueOutbox(table, rowId, syncIdNow, "upsert");
                        }
                        catch { }
                        ScheduleFastPush(table, rowId, outboxRev);
                    }
                    else if (string.IsNullOrWhiteSpace(table) || rowId <= 0)
                    {
                        try { HubSyncService.MarkDirty(); } catch { }
                        if (!SuppressBackgroundSync)
                            HubSyncService.ScheduleBackgroundPushPublic();
                    }
                }
                else if (isDelete)
                {
                    string delTable = ExtractSqlTableName(head, "Delete");
                    if (!IsInternalSyncTable(delTable))
                    {
                        try { HubSyncService.MarkDirty(); } catch { }
                        if (!SuppressBackgroundSync)
                            HubSyncService.ScheduleBackgroundPushPublic();
                    }
                }
            }
            catch { }
            try
            {
                string head = (qry ?? "").TrimStart();
                bool dml = head.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase)
                    || head.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase)
                    || head.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase);
                if (dml)
                {
                    string action = head.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase) ? "Insert"
                        : head.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase) ? "Update" : "Delete";
                    string t = ExtractSqlTableName(head, action);
                    if (!IsInternalSyncTable(t))
                        HubAutoBackup.OnLocalWrite();
                }
            }
            catch { }
            try
            {
                string head = (qry ?? "").TrimStart();
                bool isInsert = head.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase);
                bool isUpdate = head.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase);
                bool isDelete = head.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase);
                if (isInsert || isUpdate || isDelete)
                {
                    string action = isInsert ? "Insert" : isUpdate ? "Update" : "Delete";
                    string module = ExtractSqlTableName(head, action) ?? "entry";
                    if (!IsInternalSyncTable(module))
                    {
                        HubSyncService.NotifyActivity(action, module);
                    }
                    else
                        HubSyncService.RaiseLiveChanged();
                }
            }
            catch { }
        }

        private struct DeleteTarget
        {
            public string Table;
            public long LocalPk;
            public List<string> SyncIds;
        }

        private static List<DeleteTarget> PeekDeleteSyncIds(string qry, IDictionary ht)
        {
            var list = new List<DeleteTarget>();
            try
            {
                string s = (qry ?? "").TrimStart();
                if (!s.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase)) return list;
                if (s.IndexOf("sync_state", StringComparison.OrdinalIgnoreCase) >= 0) return list;
                if (s.IndexOf("sync_known", StringComparison.OrdinalIgnoreCase) >= 0) return list;
                if (s.IndexOf("sync_hash", StringComparison.OrdinalIgnoreCase) >= 0) return list;
                if (s.IndexOf("sync_alias", StringComparison.OrdinalIgnoreCase) >= 0) return list;
                if (s.IndexOf("sync_pending_delete", StringComparison.OrdinalIgnoreCase) >= 0) return list;
                if (s.IndexOf("sqlite_master", StringComparison.OrdinalIgnoreCase) >= 0) return list;
                string table = ExtractSqlTableName(s, "Delete");
                if (string.IsNullOrWhiteSpace(table)) return list;
                string pk = HubSyncService.TablePk(table);
                string select = System.Text.RegularExpressions.Regex.Replace(
                    s, @"^DELETE\s+FROM", "SELECT \"" + pk + "\" FROM",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                foreach (var row in QueryDeleteTargets(select, ht))
                {
                    if (row.Item1 <= 0) continue;
                    list.Add(new DeleteTarget
                    {
                        Table = table,
                        LocalPk = row.Item1,
                        SyncIds = HubSyncService.CollectCloudDeleteIds(table, row.Item1)
                    });
                }
            }
            catch { }
            return list;
        }

        private static List<Tuple<long, string>> QueryDeleteTargets(string sql, IDictionary ht)
        {
            var rows = new List<Tuple<long, string>>();
            try
            {
                Action<SQLiteConnection> run = c =>
                {
                    using (SQLiteCommand cmd = new SQLiteCommand(sql, c))
                    {
                        BindIDict(cmd, ht);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                if (r.FieldCount < 1 || r.IsDBNull(0)) continue;
                                long pk = Convert.ToInt64(r.GetValue(0));
                                string sid = r.FieldCount > 1 && !r.IsDBNull(1)
                                    ? Convert.ToString(r.GetValue(1) ?? "").Trim()
                                    : "";
                                rows.Add(Tuple.Create(pk, sid));
                            }
                        }
                    }
                };
                if (_sharedWorkDepth > 0)
                {
                    lock (_conLock)
                    {
                        EnsureSharedOpen();
                        run(con);
                    }
                }
                else
                {
                    using (var local = new SQLiteConnection(con_string))
                    {
                        local.Open();
                        run(local);
                    }
                }
            }
            catch { }
            return rows;
        }

        private static List<string> QueryTextColumn(string sql, IDictionary ht)
        {
            var ids = new List<string>();
            try
            {
                if (_sharedWorkDepth > 0)
                {
                    lock (_conLock)
                    {
                        EnsureSharedOpen();
                        using (SQLiteCommand cmd = new SQLiteCommand(sql, con))
                        {
                            BindIDict(cmd, ht);
                            using (var r = cmd.ExecuteReader())
                            {
                                while (r.Read())
                                {
                                    if (r.FieldCount < 1 || r.IsDBNull(0)) continue;
                                    string s = Convert.ToString(r.GetValue(0) ?? "");
                                    if (!string.IsNullOrWhiteSpace(s)) ids.Add(s.Trim());
                                }
                            }
                        }
                    }
                }
                else
                {
                    using (var local = new SQLiteConnection(con_string))
                    {
                        local.Open();
                        using (SQLiteCommand cmd = new SQLiteCommand(sql, local))
                        {
                            BindIDict(cmd, ht);
                            using (var r = cmd.ExecuteReader())
                            {
                                while (r.Read())
                                {
                                    if (r.FieldCount < 1 || r.IsDBNull(0)) continue;
                                    string s = Convert.ToString(r.GetValue(0) ?? "");
                                    if (!string.IsNullOrWhiteSpace(s)) ids.Add(s.Trim());
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return ids;
        }

        private static void BindIDict(SQLiteCommand cmd, IDictionary ht)
        {
            if (ht == null) return;
            foreach (DictionaryEntry item in ht)
                cmd.Parameters.AddWithValue(item.Key.ToString(), item.Value ?? DBNull.Value);
        }

        private static void PushCloudDeletes(List<DeleteTarget> ids, int res)
        {
            if (res <= 0 || ids == null || ids.Count == 0) return;
            foreach (var t in ids)
            {
                try { HubSyncService.SyncCloudDelete(t.Table, t.LocalPk, t.SyncIds); }
                catch { }
            }
        }

        private static void PushCloudUserDeletes(string qry, IDictionary ht, int res)
        {
            if (res <= 0) return;
            foreach (string un in PeekDeleteUsernames(qry, ht))
            {
                try { HubSyncService.PushDeletedUsername(un); }
                catch { }
            }
        }

        private static List<string> PeekDeleteUsernames(string qry, IDictionary ht)
        {
            var list = new List<string>();
            try
            {
                string s = (qry ?? "").TrimStart();
                if (!s.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase)) return list;
                string table = ExtractSqlTableName(s, "Delete");
                if (!string.Equals(table, "users", StringComparison.OrdinalIgnoreCase)) return list;
                string select = System.Text.RegularExpressions.Regex.Replace(
                    s, @"^DELETE\s+FROM", "SELECT username FROM",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                foreach (string un in QueryTextColumn(select, ht))
                    if (!string.IsNullOrWhiteSpace(un)) list.Add(un.Trim());
            }
            catch { }
            return list;
        }

        private static long IdFromParams(System.Collections.IDictionary parameters)
        {
            if (parameters == null) return 0;
            string[] keys = { "@id", "id", "@sid", "sid", "@person_id", "person_id",
                "@deal_id", "deal_id", "@credit_id", "credit_id", "@userID", "userID" };
            foreach (string k in keys)
            {
                if (!parameters.Contains(k)) continue;
                try { return Convert.ToInt64(parameters[k]); } catch { }
            }
            return 0;
        }

        private static string ExtractSqlTableName(string sql, string action)
        {
            try
            {
                string u = sql.Trim();
                var rx = action == "Insert"
                    ? new System.Text.RegularExpressions.Regex(@"INSERT\s+INTO\s+([`\[\]\w]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                    : action == "Update"
                        ? new System.Text.RegularExpressions.Regex(@"UPDATE\s+([`\[\]\w]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                        : new System.Text.RegularExpressions.Regex(@"DELETE\s+FROM\s+([`\[\]\w]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                var m = rx.Match(u);
                if (!m.Success) return null;
                return m.Groups[1].Value.Trim('`', '[', ']', '"', '\'');
            }
            catch { return null; }
        }

        public static object SQLScalar(string query, Hashtable parameters)
        {
            return ExecuteScalar1(query, parameters);
        }

        private static bool IsDbBusy(Exception ex)
        {
            string m = ex == null ? "" : (ex.Message ?? "");
            return m.IndexOf("locked", StringComparison.OrdinalIgnoreCase) >= 0
                || m.IndexOf("busy", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static int ExecNonQueryRetry(SQLiteConnection c, string qry, IDictionary ht, out long lastId, SQLiteTransaction tx = null)
        {
            lastId = 0;
            Exception last = null;
            for (int i = 0; i < 12; i++)
            {
                try
                {
                    using (SQLiteCommand cmd = tx == null
                        ? new SQLiteCommand(qry, c)
                        : new SQLiteCommand(qry, c, tx))
                    {
                        cmd.CommandType = CommandType.Text;
                        BindIDict(cmd, ht);
                        int n = cmd.ExecuteNonQuery();
                        lastId = c.LastInsertRowId;
                        return n;
                    }
                }
                catch (Exception ex)
                {
                    last = ex;
                    if (!IsDbBusy(ex) || i == 11) throw;
                    System.Threading.Thread.Sleep(50 * (i + 1));
                }
            }
            if (last != null) throw last;
            return 0;
        }

        private static void QueuePendingInTxn(SQLiteConnection c, SQLiteTransaction tx, List<DeleteTarget> delIds)
        {
            if (delIds == null) return;
            foreach (var t in delIds)
            {
                if (t.SyncIds == null || t.SyncIds.Count == 0) continue;
                HubSyncService.QueuePendingDeleteIds(c, tx, t.Table, t.SyncIds);
            }
        }

        public static int SQL(string qry, Hashtable ht)
        {
            int res = 0;
            long lastId = 0;
            var delIds = PeekDeleteSyncIds(qry, ht);
            string pushTable = null;
            long pushId = 0;
            string pushRev = "";
            bool outboxInTxn = false;
            try
            {
                string head = (qry ?? "").TrimStart();
                bool isDel = delIds != null && delIds.Count > 0;
                bool isUpsert = head.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase)
                    || head.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase);
                // Customer INSERT (sync_id in SQL) + outbox SAME txn; SoftMark/push HTTP bahar
                if (_sharedWorkDepth > 0)
                {
                    lock (_conLock)
                    {
                        EnsureSharedOpen();
                        if (isDel || isUpsert)
                        {
                            using (var tx = con.BeginTransaction())
                            {
                                try
                                {
                                    if (isDel) QueuePendingInTxn(con, tx, delIds);
                                    res = ExecNonQueryRetry(con, qry, ht, out lastId, tx);
                                    if (!isDel && res > 0)
                                        outboxInTxn = TryEnqueueOutboxInTxn(con, tx, qry, res, lastId, ht,
                                            out pushTable, out pushId, out pushRev);
                                    tx.Commit();
                                }
                                catch
                                {
                                    try { tx.Rollback(); } catch { }
                                    throw;
                                }
                            }
                        }
                        else
                            res = ExecNonQueryRetry(con, qry, ht, out lastId);
                    }
                }
                else
                {
                    using (var local = new SQLiteConnection(con_string))
                    {
                        local.Open();
                        if (isDel || isUpsert)
                        {
                            using (var tx = local.BeginTransaction())
                            {
                                try
                                {
                                    if (isDel) QueuePendingInTxn(local, tx, delIds);
                                    res = ExecNonQueryRetry(local, qry, ht, out lastId, tx);
                                    if (!isDel && res > 0)
                                        outboxInTxn = TryEnqueueOutboxInTxn(local, tx, qry, res, lastId, ht,
                                            out pushTable, out pushId, out pushRev);
                                    tx.Commit();
                                }
                                catch
                                {
                                    try { tx.Rollback(); } catch { }
                                    throw;
                                }
                            }
                        }
                        else
                            res = ExecNonQueryRetry(local, qry, ht, out lastId);
                    }
                }
                AfterSuccessfulWrite(qry, res, lastId, ht, skipOutboxEnqueue: outboxInTxn);
                if (outboxInTxn) ScheduleFastPush(pushTable, pushId, pushRev);
                PushCloudDeletes(delIds, res);
                PushCloudUserDeletes(qry, ht, res);
            }
            catch (Exception ex)
            {
                res = 0;
                lastId = 0;
                HubBuilders.AppSafety.ShowSoft(ex);
            }
            return res;
        }
        private static void BindGridFromDataTable(DataGridView gv, ListBox lb, DataTable dt)
        {
            if (gv == null || dt == null) return;
            gv.AutoGenerateColumns = false;

            if (lb != null)
            {
                for (int i = 0; i < lb.Items.Count && i < dt.Columns.Count; i++)
                {
                    var mapCol = lb.Items[i] as DataGridViewColumn;
                    if (mapCol == null) continue;
                    string colName = mapCol.Name;
                    if (string.IsNullOrEmpty(colName) || !gv.Columns.Contains(colName)) continue;
                    var gridCol = gv.Columns[colName];
                    if (gridCol == null) continue;
                    gridCol.DataPropertyName = dt.Columns[i].ColumnName;
                }
            }

            gv.DataSource = dt;
            try { StaffGridHelper.Apply(gv, dt); } catch { }
        }

        public static void LoadData(string qry, DataGridView gv, ListBox lb)
        {
            EnsureSrNoFormatting(gv);
            try
            {
                if (gv == null) return;
                DataTable dt = new DataTable();
                using (var local = new SQLiteConnection(con_string))
                {
                    local.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(qry, local))
                    using (SQLiteDataAdapter da = new SQLiteDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
                BindGridFromDataTable(gv, lb, dt);
            }
            catch (Exception ex)
            {
                HubBuilders.AppSafety.ShowSoft(ex);
            }
        }
        public static void LoadData1(string qry, DataGridView gv, ListBox lb, Dictionary<string, object> parameters)
        {
            EnsureSrNoFormatting(gv);

            try
            {
                if (gv == null) return;
                DataTable dt = new DataTable();
                using (var local = new SQLiteConnection(con_string))
                {
                    local.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(qry, local))
                    {
                        cmd.CommandType = CommandType.Text;
                        if (parameters != null)
                        {
                            foreach (var param in parameters)
                                cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                        using (SQLiteDataAdapter da = new SQLiteDataAdapter(cmd))
                            da.Fill(dt);
                    }
                }

                BindGridFromDataTable(gv, lb, dt);
            }
            catch (Exception ex)
            {
                HubBuilders.AppSafety.ShowSoft(ex);
            }
        }

        private static void EnsureSrNoFormatting(DataGridView gv)
        {
            if (gv == null) return;
            gv.CellFormatting -= gv_CellFormatting;
            gv.CellFormatting += gv_CellFormatting;
        }

        private static void gv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                var gv = sender as DataGridView;
                if (gv == null || gv.Columns.Count == 0) return;
                string colName = gv.Columns[e.ColumnIndex].Name ?? "";
                // Sirf Sr# column — pehle har cell pe saari rows rewrite hoti thin (galat count / flicker)
                if (!string.Equals(colName, "dgvSr", StringComparison.OrdinalIgnoreCase)
                    && e.ColumnIndex != 0)
                    return;
                if (gv.Columns.Contains("dgvSr")
                    && e.ColumnIndex != gv.Columns["dgvSr"].Index)
                    return;
                e.Value = (e.RowIndex + 1).ToString();
                e.FormattingApplied = true;
            }
            catch
            {
                // never crash UI formatting
            }
        }

        /// <summary>
        /// Modal band hone ke baad leftover mouse click parent (btnAdd) pe na lage.
        /// </summary>
        private static int _uiClickBlock;

        public static bool UiClicksBlocked => _uiClickBlock > 0;

        public static void BlurBackground(Form Model)
        {
            if (Model == null || Model.IsDisposed) return;

            _uiClickBlock++;

            Form host = null;
            foreach (Form f in Application.OpenForms)
            {
                if (f is MainForm && f.Visible && !f.IsDisposed)
                {
                    host = f;
                    break;
                }
            }

            // Close ke baad host minimize na ho — pehle state yaad rakho
            FormWindowState savedState = FormWindowState.Normal;
            try
            {
                if (host != null && !host.IsDisposed)
                    savedState = host.WindowState;
            }
            catch { }

            // Dim overlay — Owner link mat do (owned form band hone pe Windows kabhi host minimize kar deta hai)
            Form overlay = new Form();
            try
            {
                overlay.StartPosition = FormStartPosition.Manual;
                overlay.FormBorderStyle = FormBorderStyle.None;
                overlay.ShowInTaskbar = false;
                overlay.ShowIcon = false;
                overlay.ControlBox = false;
                overlay.Opacity = 0.45d;
                overlay.BackColor = Color.Black;

                if (host != null && !host.IsDisposed)
                {
                    if (host.WindowState == FormWindowState.Maximized)
                        overlay.Bounds = Screen.FromControl(host).Bounds;
                    else
                    {
                        overlay.Location = host.PointToScreen(Point.Empty);
                        overlay.Size = host.ClientSize;
                    }
                }
                else
                {
                    overlay.WindowState = FormWindowState.Maximized;
                }

                overlay.Show();
                Model.Enabled = true;
                Model.StartPosition = FormStartPosition.CenterParent;
                try { Model.DialogResult = DialogResult.None; } catch { }

                if (host != null && !host.IsDisposed)
                    Model.ShowDialog(host);
                else
                    Model.ShowDialog();
            }
            finally
            {
                try
                {
                    if (overlay != null && !overlay.IsDisposed)
                    {
                        try { overlay.Owner = null; } catch { }
                        overlay.Hide();
                        overlay.Close();
                        overlay.Dispose();
                    }
                }
                catch { }

                // Application minimize mat hone do — sirf restore, UI change nahi
                try
                {
                    if (host != null && !host.IsDisposed)
                    {
                        if (host.WindowState == FormWindowState.Minimized)
                        {
                            host.WindowState = (savedState == FormWindowState.Minimized)
                                ? FormWindowState.Normal
                                : savedState;
                        }
                        if (!host.Visible)
                            host.Visible = true;
                    }
                }
                catch { }

                // Click-through rokne ke liye thodi der block
                void unblockLater()
                {
                    try
                    {
                        var t = new Timer { Interval = 350 };
                        t.Tick += (s, e) =>
                        {
                            try { t.Stop(); t.Dispose(); } catch { }
                            if (_uiClickBlock > 0) _uiClickBlock--;
                        };
                        t.Start();
                    }
                    catch
                    {
                        if (_uiClickBlock > 0) _uiClickBlock--;
                    }
                }

                try
                {
                    if (host != null && !host.IsDisposed && host.IsHandleCreated)
                        host.BeginInvoke((Action)unblockLater);
                    else
                        unblockLater();
                }
                catch
                {
                    if (_uiClickBlock > 0) _uiClickBlock--;
                }
            }
        }

        /// <summary>Modal Add/Edit forms: ek click pe band.</summary>
        public static void CloseDialog(Form form)
        {
            if (form == null || form.IsDisposed) return;
            try
            {
                form.DialogResult = DialogResult.Cancel;
            }
            catch
            {
                try { form.Close(); } catch { }
            }
        }

        public static void CBFill(string qry, ComboBox cb)
        {
            try
            {
                if (cb == null) return;
                DataTable dt = new DataTable();
                using (var local = new SQLiteConnection(con_string))
                using (var cmd = new SQLiteCommand(qry, local))
                {
                    cmd.CommandType = CommandType.Text;
                    using (var da = new SQLiteDataAdapter(cmd))
                        da.Fill(dt);
                }
                cb.DisplayMember = "Name";
                cb.ValueMember = "id";
                cb.DataSource = dt;
                cb.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                AppSafety.ShowSoft(ex);
            }
        }

        public static void loadData(string qry, DataGridView gv, ListBox lb)
        {
            try
            {
                if (gv == null) return;
                DataTable dt = new DataTable();
                using (var local = new SQLiteConnection(con_string))
                {
                    local.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(qry, local))
                    using (SQLiteDataAdapter da = new SQLiteDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
                BindGridFromDataTable(gv, lb, dt);
            }
            catch (Exception ex)
            {
                HubBuilders.AppSafety.ShowSoft(ex);
            }
        }

        public static void loadData(string qry, DataGridView gv, ListBox lb, Dictionary<string, object> parameters)
        {
            try
            {
                if (gv == null) return;
                DataTable dt = new DataTable();
                using (var local = new SQLiteConnection(con_string))
                {
                    local.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(qry, local))
                    {
                        if (parameters != null)
                        {
                            foreach (var p in parameters)
                                cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);
                        }
                        using (SQLiteDataAdapter da = new SQLiteDataAdapter(cmd))
                            da.Fill(dt);
                    }
                }
                BindGridFromDataTable(gv, lb, dt);
            }
            catch (Exception ex)
            {
                HubBuilders.AppSafety.ShowSoft(ex);
            }
        }
        public static void LoadDataIntoComboBox(string query, ComboBox cb, string displayMember, string valueMember)
        {
            try
            {
                if (cb == null) return;
                DataTable dt = new DataTable();
                using (SQLiteConnection con = new SQLiteConnection(con_string))
                {
                    using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                    {
                        SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                        da.Fill(dt);
                    }
                }
                cb.DisplayMember = displayMember;
                cb.ValueMember = valueMember;
                cb.DataSource = dt;
                cb.SelectedIndex = -1; // To not select any item by default
            }
            catch (Exception ex)
            {
                AppSafety.ShowSoft(ex);
            }
        }
        public static DataTable ExecuteSelectQuery(string query, Hashtable parameters)
        {
            DataTable dt = new DataTable();
            try
            {
                using (SQLiteConnection con = new SQLiteConnection(con_string))
                {
                    using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                    {
                        if (parameters != null)
                        {
                            foreach (DictionaryEntry parameter in parameters)
                            {
                                cmd.Parameters.AddWithValue(parameter.Key.ToString(), parameter.Value);
                            }
                        }

                        SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                        da.Fill(dt);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error fetching data: " + ex.Message);
            }
            return dt;
        }

        public static DataTable GetData(string qry)
        {
            DataTable dt = new DataTable();
            try
            {
                using (var local = new SQLiteConnection(con_string))
                {
                    local.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(qry, local))
                    using (SQLiteDataAdapter da = new SQLiteDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }
            catch (Exception ex)
            {
                HubBuilders.AppSafety.ShowSoft(ex);
            }
            return dt;
        }

        public static DataTable GetData(string qry, Dictionary<string, object> parameters)
        {
            DataTable dt = new DataTable();
            try
            {
                using (var local = new SQLiteConnection(con_string))
                {
                    local.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(qry, local))
                    {
                        if (parameters != null)
                        {
                            foreach (var p in parameters)
                                cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);
                        }
                        using (SQLiteDataAdapter da = new SQLiteDataAdapter(cmd))
                            da.Fill(dt);
                    }
                }
            }
            catch (Exception ex)
            {
                HubBuilders.AppSafety.ShowSoft(ex);
            }
            return dt;
        }

        public static int DataInsertUpdateDelete(string qry, Hashtable ht)
        {
            int res = 0;
            long lastId = 0;
            var delIds = PeekDeleteSyncIds(qry, ht);
            try
            {
                try
                {
                    string head = (qry ?? "").TrimStart();
                    string act = head.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase) ? "add"
                        : head.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase) ? "edit"
                        : head.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase) ? "delete"
                        : null;
                    if (act != null
                        && head.IndexOf("staff_entry_log", StringComparison.OrdinalIgnoreCase) < 0
                        && head.IndexOf("sync_", StringComparison.OrdinalIgnoreCase) < 0
                        && head.IndexOf("staff_role_permissions", StringComparison.OrdinalIgnoreCase) < 0
                        && head.IndexOf("sqlite_master", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        string table = ExtractSqlTableName(head,
                            act == "add" ? "Insert" : act == "edit" ? "Update" : "Delete");
                        string mod = StaffAccess.ModuleForSqlTable(table);
                        if (!string.IsNullOrWhiteSpace(mod) && StaffAccess.Deny(mod, act))
                            return 0;
                    }
                }
                catch { }
                // Local delete + pending_delete EK transaction; SoftMark HTTP bahar
                bool isDel = delIds != null && delIds.Count > 0;
                if (_sharedWorkDepth > 0)
                {
                    lock (_conLock)
                    {
                        EnsureSharedOpen();
                        if (isDel)
                        {
                            using (var tx = con.BeginTransaction())
                            {
                                try
                                {
                                    QueuePendingInTxn(con, tx, delIds);
                                    res = ExecNonQueryRetry(con, qry, ht, out lastId, tx);
                                    tx.Commit();
                                }
                                catch
                                {
                                    try { tx.Rollback(); } catch { }
                                    throw;
                                }
                            }
                        }
                        else
                            res = ExecNonQueryRetry(con, qry, ht, out lastId);
                    }
                }
                else
                {
                    using (var local = new SQLiteConnection(con_string))
                    {
                        local.Open();
                        if (isDel)
                        {
                            using (var tx = local.BeginTransaction())
                            {
                                try
                                {
                                    QueuePendingInTxn(local, tx, delIds);
                                    res = ExecNonQueryRetry(local, qry, ht, out lastId, tx);
                                    tx.Commit();
                                }
                                catch
                                {
                                    try { tx.Rollback(); } catch { }
                                    throw;
                                }
                            }
                        }
                        else
                            res = ExecNonQueryRetry(local, qry, ht, out lastId);
                    }
                }
                AfterSuccessfulWrite(qry, res, lastId, ht);
                PushCloudDeletes(delIds, res);
                PushCloudUserDeletes(qry, ht, res);
            }
            catch (Exception ex)
            {
                HubBuilders.AppSafety.ShowSoft(ex);
            }
            return res;
        }
        public static void Enable_reset(Form p)
        {
            foreach (Control c in p.Controls)
            {
                if (c is Guna.UI2.WinForms.Guna2TextBox)
                {
                    Guna.UI2.WinForms.Guna2TextBox t = (Guna.UI2.WinForms.Guna2TextBox)c;
                    t.Text = "";
                }
                else if (c is Guna.UI2.WinForms.Guna2ComboBox)
                {
                    Guna.UI2.WinForms.Guna2ComboBox cb = (Guna.UI2.WinForms.Guna2ComboBox)c;
                    if (cb.Items.Count > 0)
                    {
                        cb.SelectedIndex = 0; // Set to first item if there are items
                    }
                }
                else if (c is Guna.UI2.WinForms.Guna2RadioButton)
                {
                    Guna.UI2.WinForms.Guna2RadioButton rb = (Guna.UI2.WinForms.Guna2RadioButton)c;
                    rb.Checked = false;
                }
                else if (c is Guna.UI2.WinForms.Guna2CheckBox)
                {
                    Guna.UI2.WinForms.Guna2CheckBox ck = (Guna.UI2.WinForms.Guna2CheckBox)c;
                    ck.Checked = false;
                }
                else if (c is Guna.UI2.WinForms.Guna2DateTimePicker)
                {
                    Guna.UI2.WinForms.Guna2DateTimePicker dp = (Guna.UI2.WinForms.Guna2DateTimePicker)c;
                    dp.Value = DateTime.Today;
                }
                else if (c is ListBox)
                {
                    ListBox list = (ListBox)c;
                }
                else if (c is NumericUpDown)
                {
                    NumericUpDown cb = (NumericUpDown)c;
                    cb.Value = 0;
                }
                else if (c is MaskedTextBox)
                {
                    MaskedTextBox cb = (MaskedTextBox)c;
                    cb.Text = "";
                }
            }
        }
        public static void SrNo(Guna.UI2.WinForms.Guna2DataGridView gv)
        {
            try
            {
                int count = 0;
                foreach (DataGridViewRow row in gv.Rows)
                {
                    count++;
                    row.Cells[0].Value = count;
                }
            }
            catch (Exception ex)
            {
                HubBuilders.AppSafety.ShowSoft(ex);
                con.Close();
            }
        }


        public static bool Validation(Form F)
        {
            bool isValid = false;
            int count = 0;
            foreach (Control c in F.Controls)
            {
                //using tag of the control to check if we want to validate it or not 
                if (Convert.ToString(c.Tag) != "" && Convert.ToString(c.Tag) != null)
                {
                    if (c is Guna.UI2.WinForms.Guna2TextBox)
                    {
                        Guna.UI2.WinForms.Guna2TextBox t = (Guna.UI2.WinForms.Guna2TextBox)c;
                        if (t.Text.Trim() == "")
                        {
                            t.BorderColor = Color.Red;
                            t.FocusedState.BorderColor = Color.Red;
                            t.HoverState.BorderColor = Color.Red;
                            count++;
                        }
                        else
                        {
                            t.BorderColor = Color.FromArgb(213, 218, 223);
                            t.FocusedState.BorderColor = Color.FromArgb(95, 61, 204);
                            t.HoverState.BorderColor = Color.FromArgb(95, 61, 204);
                        }

                    }
                }
                if (count == 0)
                {
                    isValid = true;
                }
                else
                {
                    isValid = false;
                }
            }
            return isValid;

        }
        public static int ExecuteNonQuery(string query, Dictionary<string, object> parameters = null)
        {
            try
            {
                int n = 0;
                long lastId = 0;
                var delIds = PeekDeleteSyncIds(query, parameters);
                bool isDel = delIds != null && delIds.Count > 0;
                string head = (query ?? "").TrimStart();
                bool isUpsert = head.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase)
                    || head.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase);
                string pushTable = null;
                long pushId = 0;
                string pushRev = "";
                bool outboxInTxn = false;
                // Local write + outbox/pending_delete EK transaction; HTTP bahar
                if (_sharedWorkDepth > 0)
                {
                    lock (_conLock)
                    {
                        EnsureSharedOpen();
                        if (isDel || isUpsert)
                        {
                            using (var tx = con.BeginTransaction())
                            {
                                try
                                {
                                    if (isDel) QueuePendingInTxn(con, tx, delIds);
                                    n = ExecNonQueryRetry(con, query, parameters, out lastId, tx);
                                    if (!isDel && n > 0)
                                        outboxInTxn = TryEnqueueOutboxInTxn(con, tx, query, n, lastId, parameters,
                                            out pushTable, out pushId, out pushRev);
                                    tx.Commit();
                                }
                                catch
                                {
                                    try { tx.Rollback(); } catch { }
                                    throw;
                                }
                            }
                        }
                        else
                            n = ExecNonQueryRetry(con, query, parameters, out lastId);
                    }
                }
                else
                {
                    using (var local = new SQLiteConnection(con_string))
                    {
                        local.Open();
                        if (isDel || isUpsert)
                        {
                            using (var tx = local.BeginTransaction())
                            {
                                try
                                {
                                    if (isDel) QueuePendingInTxn(local, tx, delIds);
                                    n = ExecNonQueryRetry(local, query, parameters, out lastId, tx);
                                    if (!isDel && n > 0)
                                        outboxInTxn = TryEnqueueOutboxInTxn(local, tx, query, n, lastId, parameters,
                                            out pushTable, out pushId, out pushRev);
                                    tx.Commit();
                                }
                                catch
                                {
                                    try { tx.Rollback(); } catch { }
                                    throw;
                                }
                            }
                        }
                        else
                            n = ExecNonQueryRetry(local, query, parameters, out lastId);
                    }
                }
                AfterSuccessfulWrite(query, n, lastId, parameters, skipOutboxEnqueue: outboxInTxn);
                if (outboxInTxn) ScheduleFastPush(pushTable, pushId, pushRev);
                PushCloudDeletes(delIds, n);
                PushCloudUserDeletes(query, parameters, n);
                return n;
            }
            catch (Exception ex)
            {
                MessageBox.Show("SQL Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return -1;
            }
        }

        // ✅ ExecuteScalar: Returns Single Value (for Last Insert ID)
        public static object ExecuteScalar(string query, Dictionary<string, object> parameters = null)
        {
            try
            {
                if (_sharedWorkDepth > 0)
                {
                    lock (_conLock)
                    {
                        EnsureSharedOpen();
                        using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                        {
                            if (parameters != null)
                            {
                                foreach (var param in parameters)
                                    cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                            }
                            return cmd.ExecuteScalar();
                        }
                    }
                }

                using (var local = new SQLiteConnection(con_string))
                {
                    local.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(query, local))
                    {
                        if (parameters != null)
                        {
                            foreach (var param in parameters)
                                cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                        return cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("SQL Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }
        public static object ExecuteScalar1(string query, Hashtable parameters)
        {
            object result = null;
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(projectconnection.ConnectionString))
                {
                    connection.Open();

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        // Add parameters to the command
                        if (parameters != null)
                        {
                            foreach (DictionaryEntry param in parameters)
                            {
                                command.Parameters.AddWithValue(param.Key.ToString(), param.Value);
                            }
                        }

                        // Execute scalar query and get the result
                        result = command.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                AppSafety.ShowSoft(ex);
                return null;
            }

            return result; // Return the scalar result
        }
        // ✅ ExecuteSelectQuery: SELECT Queries
        public static DataTable ExecuteSelectQuery1(string query, Dictionary<string, object> parameters = null)
        {
            try
            {
                if (con.State != ConnectionState.Open)
                    con.Open();

                using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                {
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            cmd.Parameters.AddWithValue(param.Key, param.Value);
                        }
                    }
                    using (SQLiteDataAdapter da = new SQLiteDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        return dt;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("SQL Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
            }
        }

        /// <summary>
        /// customers.cnic / customers.mobile pe UNIQUE hatao — same CNIC/mobile multiple customers OK.
        /// </summary>
        private static bool _customerSchemaReady = false;
        public static void EnsureCustomerSchema()
        {
            if (_customerSchemaReady) return;

            try
            {
                using (var local = new SQLiteConnection(con_string))
                {
                    local.Open();

                    void Exec(string sql)
                    {
                        using (var cmd = new SQLiteCommand(sql, local))
                            cmd.ExecuteNonQuery();
                    }

                    // 1) Drop named UNIQUE indexes on cnic / mobile (sql IS NOT NULL)
                    var dropIdx = new List<string>();
                    using (var cmd = new SQLiteCommand(@"
SELECT name, sql FROM sqlite_master
WHERE type='index' AND tbl_name='customers' AND sql IS NOT NULL;", local))
                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            string name = Convert.ToString(rd["name"]) ?? "";
                            string sql = Convert.ToString(rd["sql"]) ?? "";
                            if (string.IsNullOrWhiteSpace(name)) continue;
                            bool isUnique = sql.IndexOf("UNIQUE", StringComparison.OrdinalIgnoreCase) >= 0;
                            bool onCnicOrMobile =
                                sql.IndexOf("cnic", StringComparison.OrdinalIgnoreCase) >= 0
                                || sql.IndexOf("mobile", StringComparison.OrdinalIgnoreCase) >= 0;
                            if (isUnique && onCnicOrMobile)
                                dropIdx.Add(name);
                        }
                    }
                    foreach (var name in dropIdx)
                    {
                        try { Exec("DROP INDEX IF EXISTS \"" + name.Replace("\"", "\"\"") + "\";"); }
                        catch { }
                    }

                    // 2) Table-level UNIQUE → rebuild from original CREATE SQL (strip UNIQUE only)
                    string tableSql = "";
                    using (var cmd = new SQLiteCommand(
                        "SELECT sql FROM sqlite_master WHERE type='table' AND name='customers';", local))
                    {
                        tableSql = Convert.ToString(cmd.ExecuteScalar()) ?? "";
                    }

                    if (string.IsNullOrWhiteSpace(tableSql))
                    {
                        _customerSchemaReady = true;
                        return;
                    }

                    bool needsRebuild =
                        System.Text.RegularExpressions.Regex.IsMatch(
                            tableSql,
                            @"\b(cnic|mobile)\b[\s\w]*\bUNIQUE\b|UNIQUE\s*\([^)]*\b(cnic|mobile)\b",
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                    if (needsRebuild)
                    {
                        // Safe CREATE: sirf name+type (+ PK). DEFAULT/NOT NULL skip = no syntax errors
                        var cols = new List<string>();
                        var colDefs = new List<string>();
                        using (var cmd = new SQLiteCommand("PRAGMA table_info(customers);", local))
                        using (var rd = cmd.ExecuteReader())
                        {
                            while (rd.Read())
                            {
                                string cname = Convert.ToString(rd["name"]) ?? "";
                                if (string.IsNullOrWhiteSpace(cname)) continue;
                                string ctype = (Convert.ToString(rd["type"]) ?? "").Trim();
                                if (ctype.Length == 0) ctype = "TEXT";
                                int pk = Convert.ToInt32(rd["pk"]);
                                cols.Add(cname);

                                if (pk >= 1 && string.Equals(cname, "id", StringComparison.OrdinalIgnoreCase))
                                    colDefs.Add("id INTEGER PRIMARY KEY AUTOINCREMENT");
                                else
                                    colDefs.Add("\"" + cname.Replace("\"", "\"\"") + "\" " + ctype);
                            }
                        }

                        if (cols.Count == 0)
                            throw new Exception("customers table has no columns.");

                        // Ensure mobile/cnic exist as plain TEXT (no UNIQUE)
                        void EnsureCol(string n)
                        {
                            if (!cols.Exists(c => string.Equals(c, n, StringComparison.OrdinalIgnoreCase)))
                            {
                                cols.Add(n);
                                colDefs.Add("\"" + n + "\" TEXT");
                            }
                        }
                        EnsureCol("mobile");
                        EnsureCol("cnic");

                        string colList = string.Join(", ", cols.ConvertAll(c => "\"" + c.Replace("\"", "\"\"") + "\""));
                        string createNew = "CREATE TABLE customers__new (" + string.Join(", ", colDefs) + ")";

                        Exec("PRAGMA foreign_keys = OFF;");
                        try
                        {
                            try { Exec("DROP TABLE IF EXISTS customers__new;"); } catch { }
                            Exec(createNew);

                            // Copy only columns that exist on OLD table
                            var oldCols = new List<string>();
                            using (var cmd = new SQLiteCommand("PRAGMA table_info(customers);", local))
                            using (var rd = cmd.ExecuteReader())
                            {
                                while (rd.Read())
                                    oldCols.Add(Convert.ToString(rd["name"]));
                            }
                            var copyCols = cols.FindAll(c =>
                                oldCols.Exists(o => string.Equals(o, c, StringComparison.OrdinalIgnoreCase)));
                            string copyList = string.Join(", ", copyCols.ConvertAll(c => "\"" + c.Replace("\"", "\"\"") + "\""));

                            Exec("INSERT INTO customers__new (" + copyList + ") SELECT " + copyList + " FROM customers;");
                            Exec("DROP TABLE customers;");
                            Exec("ALTER TABLE customers__new RENAME TO customers;");
                            try
                            {
                                Exec("CREATE INDEX IF NOT EXISTS idx_customers_name ON customers(name);");
                                Exec("CREATE INDEX IF NOT EXISTS idx_customers_mobile ON customers(mobile);");
                                Exec("CREATE INDEX IF NOT EXISTS idx_customers_cnic ON customers(cnic);");
                            }
                            catch { }
                        }
                        finally
                        {
                            try { Exec("PRAGMA foreign_keys = ON;"); } catch { }
                        }
                    }
                }

                _customerSchemaReady = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Customer schema: " + ex.Message);
                try
                {
                    MessageBox.Show(
                        "Customer schema update (duplicate CNIC/Mobile allow) fail:\n" + ex.Message,
                        "Schema", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                catch { }
            }
        }

        private static void ExecTx(SQLiteTransaction tx, SQLiteConnection con, string sql)
        {
            using (var cmd = new SQLiteCommand(sql, con, tx))
                cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Cancel ke baad same plot dobara allocate ho sake — UNIQUE(plot_id) hata kar
        /// sirf Active allocation pe one-plot rule enforce karta hai.
        /// </summary>
        private static bool _allocationSchemaReady = false;
        private static bool _customerPlotsColumnsReady = false;
        private static bool _plotStatusRepairDone = false;
        private static bool _dealTypeNormalizeDone = false;

        private static bool CustomerPlotsHasColumn(SQLiteConnection c, string col)
        {
            if (c == null || string.IsNullOrWhiteSpace(col)) return false;
            try
            {
                using (var cmd = new SQLiteCommand("PRAGMA table_info(customer_plots);", c))
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

        private static void EnsureCustomerPlotsColumn(SQLiteConnection c, string col, string sqlType)
        {
            if (c == null || string.IsNullOrWhiteSpace(col) || string.IsNullOrWhiteSpace(sqlType)) return;
            if (CustomerPlotsHasColumn(c, col)) return;
            using (var cmd = new SQLiteCommand(
                "ALTER TABLE customer_plots ADD COLUMN \"" + col.Replace("\"", "\"\"") + "\" " + sqlType + ";", c))
                cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Purani client DB safe — missing cancelled_date / deal_type / etc. add.
        /// UI nahi; sirf ALTER ADD. Session mein ek baar (open hang avoid).
        /// </summary>
        public static void EnsureCustomerPlotsAllColumns(SQLiteConnection c)
        {
            if (c == null) return;
            if (_customerPlotsColumnsReady) return;
            try
            {
                // Table hi na ho to yahan create nahi — EnsureAllocationSchema handle karegi
                using (var chk = new SQLiteCommand(
                    "SELECT 1 FROM sqlite_master WHERE type='table' AND name='customer_plots' LIMIT 1;", c))
                {
                    if (chk.ExecuteScalar() == null) return;
                }

                EnsureCustomerPlotsColumn(c, "down_payment", "REAL DEFAULT 0");
                EnsureCustomerPlotsColumn(c, "notes", "TEXT");
                EnsureCustomerPlotsColumn(c, "broker_id", "INTEGER");
                EnsureCustomerPlotsColumn(c, "broker_commission", "NUMERIC");
                EnsureCustomerPlotsColumn(c, "cancelled_date", "TEXT");
                EnsureCustomerPlotsColumn(c, "cancellation_id", "INTEGER");
                EnsureCustomerPlotsColumn(c, "deal_type", "TEXT DEFAULT 'Installment'");
                EnsureCustomerPlotsColumn(c, "monthly_installment", "REAL DEFAULT 0");
                EnsureCustomerPlotsColumn(c, "sync_id", "TEXT");

                try
                {
                    if (!_dealTypeNormalizeDone)
                    {
                        using (var cmd = new SQLiteCommand(@"
UPDATE customer_plots
SET deal_type = 'Installment'
WHERE deal_type IS NULL
   OR TRIM(COALESCE(deal_type,'')) = ''
   OR LOWER(TRIM(deal_type)) NOT IN ('installment','reserve','sold');", c))
                            cmd.ExecuteNonQuery();
                    }
                }
                catch { }

                _customerPlotsColumnsReady = true;
            }
            catch { }
        }

        /// <summary>
        /// Active/Completed allocation ke plot Available na rahe.
        /// Manual cancel (cancelled_date set) touch nahi.
        /// Session mein ek baar — har GetData pe UPDATE hang avoid.
        /// </summary>
        public static void RepairAllocatedPlotStatuses()
        {
            if (_plotStatusRepairDone) return;
            try
            {
                using (var local = new SQLiteConnection(con_string))
                {
                    local.Open();
                    EnsureCustomerPlotsAllColumns(local);

                    if (CustomerPlotsHasColumn(local, "cancelled_date"))
                    {
                        using (var cmd = new SQLiteCommand(@"
UPDATE customer_plots
SET status = 'Active'
WHERE IFNULL(status,'') = 'Cancelled'
  AND (cancelled_date IS NULL OR TRIM(IFNULL(cancelled_date,'')) = '')
  AND IFNULL(cancellation_id,0) = 0;", local))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }

                    string soldExpr = CustomerPlotsHasColumn(local, "deal_type")
                        ? "IFNULL(cp.deal_type,'') = 'Sold'"
                        : "0";

                    using (var cmd = new SQLiteCommand(@"
UPDATE plots
SET status = CASE
  WHEN EXISTS (
    SELECT 1 FROM customer_plots cp
    WHERE cp.plot_id = plots.id
      AND IFNULL(cp.status,'') <> 'Cancelled'
      AND (" + soldExpr + @")
  ) THEN 'Sold'
  ELSE 'Hold'
END
WHERE IFNULL(status,'') = 'Available'
  AND EXISTS (
    SELECT 1 FROM customer_plots cp
    WHERE cp.plot_id = plots.id
      AND IFNULL(cp.status,'') <> 'Cancelled'
  );", local))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    _plotStatusRepairDone = true;
                }
            }
            catch { }
        }

        public static void EnsureAllocationSchema()
        {
            // Har form open pe full schema/UPDATE skip — pehle se ready ho to turant return
            if (_allocationSchemaReady)
            {
                try
                {
                    if (!_customerPlotsColumnsReady)
                    {
                        if (con.State != ConnectionState.Open)
                            con.Open();
                        EnsureCustomerPlotsAllColumns(con);
                    }
                }
                catch { }
                return;
            }

            try
            {
                if (con.State != ConnectionState.Open)
                    con.Open();
                // Client purani DB: pehle columns add — warna rebuild / deal_type crash
                EnsureCustomerPlotsAllColumns(con);
                EnsureCustomerPlotsMonthlyInstallmentColumn(con);
                EnsureCustomerPlotsDealTypeColumn(con);
            }
            catch { }

            if (_allocationSchemaReady) return;

            try
            {
                if (con.State != ConnectionState.Open)
                    con.Open();

                EnsureCustomerPlotsAllColumns(con);

                using (var pragmaOff = new SQLiteCommand("PRAGMA foreign_keys = OFF;", con))
                    pragmaOff.ExecuteNonQuery();

                string tableSql = "";
                using (var cmd = new SQLiteCommand(
                    "SELECT sql FROM sqlite_master WHERE type='table' AND name='customer_plots';", con))
                {
                    tableSql = Convert.ToString(cmd.ExecuteScalar()) ?? "";
                }

                bool needsRebuild = !string.IsNullOrEmpty(tableSql)
                    && System.Text.RegularExpressions.Regex.IsMatch(
                        tableSql,
                        @"plot_id\s+INTEGER\s+NOT\s+NULL\s+UNIQUE",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (needsRebuild)
                {
                    // Receipts triggers customer_plots ko reference karti hain —
                    // DROP TABLE se pehle unhe hatao, baad mein recreate.
                    var triggerSqls = new List<string>();
                    using (var cmd = new SQLiteCommand(@"
SELECT name, sql FROM sqlite_master
WHERE type='trigger'
  AND sql IS NOT NULL
  AND (sql LIKE '%customer_plots%' OR tbl_name='receipts' OR tbl_name='customer_plots');", con))
                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            string sql = Convert.ToString(rd["sql"]);
                            if (!string.IsNullOrWhiteSpace(sql))
                                triggerSqls.Add(sql);
                        }
                    }

                    using (var tx = con.BeginTransaction())
                    {
                        // Drop dependent triggers first
                        using (var dropList = new SQLiteCommand(@"
SELECT name FROM sqlite_master
WHERE type='trigger'
  AND (sql LIKE '%customer_plots%' OR tbl_name='receipts' OR tbl_name='customer_plots');", con, tx))
                        using (var rd = dropList.ExecuteReader())
                        {
                            var names = new List<string>();
                            while (rd.Read())
                                names.Add(Convert.ToString(rd[0]));
                            rd.Close();

                            foreach (var name in names)
                            {
                                if (string.IsNullOrWhiteSpace(name)) continue;
                                using (var d = new SQLiteCommand("DROP TRIGGER IF EXISTS \"" + name.Replace("\"", "\"\"") + "\";", con, tx))
                                    d.ExecuteNonQuery();
                            }
                        }

                        // Columns pehle ADD ho chuki hain — SELECT ab safe
                        using (var cmd = new SQLiteCommand(@"
CREATE TABLE customer_plots__new (
  id            INTEGER PRIMARY KEY AUTOINCREMENT,
  customer_id   INTEGER NOT NULL,
  plot_id       INTEGER NOT NULL,
  plan_id       INTEGER,
  booking_date  TEXT    NOT NULL,
  price         REAL    NOT NULL CHECK (price >= 0),
  down_payment  REAL    NOT NULL DEFAULT 0 CHECK (down_payment >= 0),
  notes         TEXT,
  status        TEXT    NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Closed','Cancelled','Completed')),
  broker_id     INTEGER,
  broker_commission NUMERIC,
  cancelled_date TEXT,
  cancellation_id INTEGER,
  deal_type     TEXT    NOT NULL DEFAULT 'Installment',
  monthly_installment REAL NOT NULL DEFAULT 0,
  FOREIGN KEY (customer_id) REFERENCES customers(id) ON DELETE CASCADE,
  FOREIGN KEY (plot_id)     REFERENCES plots(id)     ON DELETE RESTRICT,
  FOREIGN KEY (plan_id)     REFERENCES plans(id)     ON DELETE SET NULL
);
INSERT INTO customer_plots__new (
  id, customer_id, plot_id, plan_id, booking_date, price, down_payment, notes,
  status, broker_id, broker_commission, cancelled_date, cancellation_id, deal_type, monthly_installment)
SELECT id, customer_id, plot_id, plan_id, booking_date, price,
       COALESCE(down_payment, 0), notes,
       CASE WHEN status='Completed' THEN 'Completed' ELSE status END,
       broker_id, broker_commission, cancelled_date, cancellation_id,
       COALESCE(NULLIF(TRIM(deal_type),''), 'Installment'),
       COALESCE(monthly_installment, 0)
FROM customer_plots;
DROP TABLE customer_plots;
ALTER TABLE customer_plots__new RENAME TO customer_plots;
CREATE INDEX IF NOT EXISTS idx_cp_customer ON customer_plots(customer_id);
CREATE INDEX IF NOT EXISTS idx_cp_status   ON customer_plots(status);
CREATE INDEX IF NOT EXISTS IX_customer_plots_broker ON customer_plots(broker_id);
CREATE INDEX IF NOT EXISTS idx_cp_plot     ON customer_plots(plot_id);", con, tx))
                        {
                            cmd.ExecuteNonQuery();
                        }

                        // Recreate saved triggers (receipt overpay etc.)
                        foreach (var sql in triggerSqls)
                        {
                            // Skip our own active-plot triggers if somehow listed; we create them below
                            if (sql.IndexOf("trg_cp_one_active_plot", StringComparison.OrdinalIgnoreCase) >= 0)
                                continue;
                            using (var t = new SQLiteCommand(sql, con, tx))
                                t.ExecuteNonQuery();
                        }

                        tx.Commit();
                    }
                }

                // Active plot pe sirf 1 allocation
                using (var cmd = new SQLiteCommand(@"
SELECT COUNT(1) FROM sqlite_master
WHERE type='trigger' AND name='trg_cp_one_active_plot_ins';", con))
                {
                    long exists = Convert.ToInt64(cmd.ExecuteScalar() ?? 0);
                    if (exists == 0)
                    {
                        using (var t = new SQLiteCommand(@"
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
END;

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
END;", con))
                        {
                            t.ExecuteNonQuery();
                        }
                    }
                }

                // Ensure receipt overpay triggers exist (UPDATE + INSERT)
                using (var cmd = new SQLiteCommand(@"
SELECT COUNT(1) FROM sqlite_master
WHERE type='trigger' AND name='trg_receipts_no_overpay_update';", con))
                {
                    long exists = Convert.ToInt64(cmd.ExecuteScalar() ?? 0);
                    if (exists == 0)
                    {
                        using (var t = new SQLiteCommand(@"
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
END;", con))
                        {
                            t.ExecuteNonQuery();
                        }
                    }
                }

                using (var cmd = new SQLiteCommand(@"
SELECT COUNT(1) FROM sqlite_master
WHERE type='trigger' AND name='trg_receipts_no_overpay_insert';", con))
                {
                    long exists = Convert.ToInt64(cmd.ExecuteScalar() ?? 0);
                    if (exists == 0)
                    {
                        using (var t = new SQLiteCommand(@"
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
END;", con))
                        {
                            t.ExecuteNonQuery();
                        }
                    }
                }

                // Additive: deal_type = Installment | Reserve | Sold (purani entries → Installment)
                EnsureCustomerPlotsDealTypeColumn(con);
                EnsureCustomerPlotsMonthlyInstallmentColumn(con);

                _allocationSchemaReady = true;
            }
            catch (Exception ex)
            {
                // Columns pehle add ho chuki hon — soft warn, app band mat karo
                try
                {
                    EnsureCustomerPlotsAllColumns(con);
                    EnsureCustomerPlotsDealTypeColumn(con);
                    _allocationSchemaReady = true;
                }
                catch { }
                System.Diagnostics.Debug.WriteLine("Allocation schema: " + ex.Message);
            }
            finally
            {
                try
                {
                    using (var pragma = new SQLiteCommand("PRAGMA foreign_keys = ON;", con))
                        pragma.ExecuteNonQuery();
                }
                catch { }

                ReleaseSharedIfIdle();
            }
        }

        /// <summary>
        /// Additive only — old backup safe. Missing column add + sab purani rows Installment.
        /// </summary>
        private static void EnsureCustomerPlotsDealTypeColumn(SQLiteConnection c)
        {
            if (c == null) return;
            if (_dealTypeNormalizeDone && _customerPlotsColumnsReady) return;
            try
            {
                bool has = CustomerPlotsHasColumn(c, "deal_type");

                if (!has)
                {
                    using (var cmd = new SQLiteCommand(
                        "ALTER TABLE customer_plots ADD COLUMN deal_type TEXT NOT NULL DEFAULT 'Installment';", c))
                        cmd.ExecuteNonQuery();
                }

                if (!_dealTypeNormalizeDone)
                {
                    using (var cmd = new SQLiteCommand(@"
UPDATE customer_plots
SET deal_type = 'Installment'
WHERE deal_type IS NULL
   OR TRIM(COALESCE(deal_type,'')) = ''
   OR LOWER(TRIM(deal_type)) NOT IN ('installment','reserve','sold');

UPDATE customer_plots SET deal_type = 'Installment' WHERE LOWER(TRIM(deal_type)) = 'installment';
UPDATE customer_plots SET deal_type = 'Reserve'     WHERE LOWER(TRIM(deal_type)) = 'reserve';
UPDATE customer_plots SET deal_type = 'Sold'        WHERE LOWER(TRIM(deal_type)) = 'sold';", c))
                        cmd.ExecuteNonQuery();
                    _dealTypeNormalizeDone = true;
                }
            }
            catch { }
        }

        /// <summary>Additive — purani rows 0. Empty = WhatsApp message mein installment nahi.</summary>
        private static void EnsureCustomerPlotsMonthlyInstallmentColumn(SQLiteConnection c)
        {
            if (c == null) return;
            if (_customerPlotsColumnsReady) return;
            try
            {
                if (CustomerPlotsHasColumn(c, "monthly_installment")) return;
                using (var cmd = new SQLiteCommand(
                    "ALTER TABLE customer_plots ADD COLUMN monthly_installment REAL NOT NULL DEFAULT 0;", c))
                    cmd.ExecuteNonQuery();
            }
            catch { }
        }

        // ── Partner Cash / Hisab (additive — purani tables touch nahi) ─────────
        private static bool _partnerMoneyReady = false;

        public static void EnsurePartnerMoneySchema()
        {
            if (_partnerMoneyReady) return;
            try
            {
                // Alag connection — login / shared MainClass.con lock avoid
                using (var local = new SQLiteConnection(con_string))
                {
                    local.Open();

                    void Exec(string sql)
                    {
                        using (var cmd = new SQLiteCommand(sql, local))
                            cmd.ExecuteNonQuery();
                    }

                    Exec(@"
CREATE TABLE IF NOT EXISTS customer_plot_partners (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  customer_plot_id INTEGER NOT NULL UNIQUE,
  partner_id INTEGER NOT NULL,
  created_at TEXT DEFAULT (datetime('now','localtime'))
);");
                    Exec(@"
CREATE TABLE IF NOT EXISTS partner_money (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  partner_id INTEGER NOT NULL,
  txn_date TEXT NOT NULL,
  direction TEXT NOT NULL,
  amount REAL NOT NULL,
  mode TEXT,
  category TEXT,
  customer_plot_id INTEGER,
  receipt_id INTEGER,
  society_id INTEGER,
  note TEXT,
  created_at TEXT DEFAULT (datetime('now','localtime'))
);");
                    Exec("CREATE INDEX IF NOT EXISTS idx_pm_partner ON partner_money(partner_id, txn_date);");
                    Exec("CREATE INDEX IF NOT EXISTS idx_pm_receipt ON partner_money(receipt_id);");
                    Exec("CREATE INDEX IF NOT EXISTS idx_cpp_cp ON customer_plot_partners(customer_plot_id);");
                    Exec(@"
CREATE TABLE IF NOT EXISTS partner_money_categories (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  name TEXT NOT NULL UNIQUE,
  direction TEXT NOT NULL DEFAULT 'Both'
);");
                    SeedPartnerMoneyCategories(local);
                }
                _partnerMoneyReady = true;
            }
            catch (Exception ex)
            {
                // Login pe popup na todo — agli dafa dubara try
                System.Diagnostics.Debug.WriteLine("Partner money schema: " + ex.Message);
            }
        }

        private static void SeedPartnerMoneyCategories(SQLiteConnection c)
        {
            string[] defs =
            {
                "Receipt|In",
                "Manual In|In",
                "Broker Commission|In",
                "Zameen / Land|Out",
                "Office Expense|Out",
                "Staff Salary|Out",
                "Apne paas rakha|Out",
                "Other|Both"
            };
            foreach (var d in defs)
            {
                var p = d.Split('|');
                using (var cmd = new SQLiteCommand(
                    "INSERT OR IGNORE INTO partner_money_categories (name, direction) VALUES (@n, @dir);", c))
                {
                    cmd.Parameters.AddWithValue("@n", p[0]);
                    cmd.Parameters.AddWithValue("@dir", p[1]);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static DataTable GetPartnerMoneyCategories(string direction = null)
        {
            EnsurePartnerMoneySchema();
            if (string.IsNullOrWhiteSpace(direction) || direction == "Both")
            {
                return GetData(
                    "SELECT id, name, direction FROM partner_money_categories ORDER BY name;");
            }
            return ExecuteSelectQuery1(@"
SELECT id, name, direction FROM partner_money_categories
WHERE direction=@d OR direction='Both'
ORDER BY name;",
                new Dictionary<string, object> { { "@d", direction } });
        }

        public static bool AddPartnerMoneyCategory(string name, string direction = "Both")
        {
            EnsurePartnerMoneySchema();
            name = (name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (direction != "In" && direction != "Out" && direction != "Both") direction = "Both";
            int n = ExecuteNonQuery(
                "INSERT OR IGNORE INTO partner_money_categories (name, direction) VALUES (@n, @dir);",
                new Dictionary<string, object> { { "@n", name }, { "@dir", direction } });
            return n > 0;
        }

        /// <summary>Category ka naam/direction change karna.</summary>
        public static bool UpdatePartnerMoneyCategory(int id, string name, string direction)
        {
            EnsurePartnerMoneySchema();
            name = (name ?? "").Trim();
            if (id <= 0 || string.IsNullOrWhiteSpace(name)) return false;
            if (direction != "In" && direction != "Out" && direction != "Both") direction = "Both";
            try
            {
                int n = ExecuteNonQuery(
                    "UPDATE partner_money_categories SET name=@n, direction=@dir WHERE id=@id;",
                    new Dictionary<string, object> { { "@n", name }, { "@dir", direction }, { "@id", id } });
                return n > 0;
            }
            catch
            {
                // UNIQUE constraint (naam pehle se mojood) — caller ko false milega
                return false;
            }
        }

        /// <summary>Category delete — purani partner_money entries ka category text waisa hi reh jata hai (history safe).</summary>
        public static bool DeletePartnerMoneyCategory(int id)
        {
            EnsurePartnerMoneySchema();
            if (id <= 0) return false;
            int n = ExecuteNonQuery(
                "DELETE FROM partner_money_categories WHERE id=@id;",
                new Dictionary<string, object> { { "@id", id } });
            return n > 0;
        }

        /// <summary>
        /// Broker combo: active brokers + partners (key = B:id / P:id).
        /// </summary>
        public static void LoadBrokerPartnerCombo(ComboBox cb, string selectKey = null)
        {
            if (cb == null) return;
            EnsurePartnerMoneySchema();
            var dt = new DataTable();
            dt.Columns.Add("key", typeof(string));
            dt.Columns.Add("name", typeof(string));
            try
            {
                var brokers = GetData("SELECT id, name FROM brokers WHERE is_active=1 ORDER BY name;");
                foreach (DataRow r in brokers.Rows)
                {
                    var row = dt.NewRow();
                    int id = Convert.ToInt32(r["id"]);
                    row["key"] = "B:" + id;
                    row["name"] = Convert.ToString(r["name"]);
                    dt.Rows.Add(row);
                }
                var partners = GetData(
                    "SELECT person_id AS id, name FROM Person WHERE is_partner=1 ORDER BY name;");
                foreach (DataRow r in partners.Rows)
                {
                    var row = dt.NewRow();
                    int id = Convert.ToInt32(r["id"]);
                    row["key"] = "P:" + id;
                    row["name"] = Convert.ToString(r["name"]) + "  (Partner)";
                    dt.Rows.Add(row);
                }
            }
            catch { }

            cb.DisplayMember = "name";
            cb.ValueMember = "key";
            cb.DataSource = dt;
            cb.SelectedIndex = -1;
            if (!string.IsNullOrWhiteSpace(selectKey))
            {
                try { cb.SelectedValue = selectKey; } catch { }
            }
        }

        public static bool TryParseBrokerPartnerKey(object value, out string kind, out int id)
        {
            kind = null;
            id = 0;
            if (value == null || value == DBNull.Value) return false;
            string s = Convert.ToString(value)?.Trim();
            if (string.IsNullOrEmpty(s) || s.Length < 3 || s[1] != ':') return false;
            kind = char.ToUpperInvariant(s[0]).ToString();
            if (kind != "B" && kind != "P") return false;
            return int.TryParse(s.Substring(2), out id) && id > 0;
        }

        /// <summary>Allocate pe partner select → commission Partner Cash In.</summary>
        public static void SyncAllocationPartnerCommission(
            long customerPlotId,
            int partnerId,
            decimal amount,
            string txnDate,
            SQLiteConnection useCon = null,
            SQLiteTransaction tx = null)
        {
            EnsurePartnerMoneySchema();
            var c = useCon ?? con;
            HubSyncService.DeletePartnerMoneyWhere(c, tx,
                "customer_plot_id=@cp AND category='Broker Commission' AND IFNULL(receipt_id,0)=0",
                cmd => cmd.Parameters.AddWithValue("@cp", customerPlotId));

            if (partnerId <= 0) return;
            SetAllocationPartner(customerPlotId, partnerId, c, tx);
            if (amount <= 0) return;

            int? societyId = null;
            using (var q = new SQLiteCommand(@"
SELECT p.society_id FROM customer_plots cp
JOIN plots p ON p.id = cp.plot_id
WHERE cp.id=@cp;", c, tx))
            {
                q.Parameters.AddWithValue("@cp", customerPlotId);
                var o = q.ExecuteScalar();
                if (o != null && o != DBNull.Value) societyId = Convert.ToInt32(o);
            }

            InsertPartnerMoney(
                partnerId, txnDate, "In", amount, "Cash", "Broker Commission",
                (int)customerPlotId, null, societyId, "Allocate broker/partner commission", c, tx);
        }

        /// <summary>Receipt: is plot pe kaun sa partner paise lega (receipt se link).</summary>
        public static void SetAllocationPartner(long customerPlotId, int partnerId, SQLiteConnection useCon = null, SQLiteTransaction tx = null)
        {
            EnsurePartnerMoneySchema();
            var c = useCon ?? con;
            using (var cmd = new SQLiteCommand(@"
INSERT INTO customer_plot_partners (customer_plot_id, partner_id)
VALUES (@cp, @p)
ON CONFLICT(customer_plot_id) DO UPDATE SET partner_id=excluded.partner_id;", c, tx))
            {
                cmd.Parameters.AddWithValue("@cp", customerPlotId);
                cmd.Parameters.AddWithValue("@p", partnerId);
                cmd.ExecuteNonQuery();
            }
        }

        public static int? GetAllocationPartnerId(int customerPlotId)
        {
            EnsurePartnerMoneySchema();
            var o = ExecuteScalar(
                "SELECT partner_id FROM customer_plot_partners WHERE customer_plot_id=@cp;",
                new Dictionary<string, object> { { "@cp", customerPlotId } });
            if (o == null || o == DBNull.Value) return null;
            return Convert.ToInt32(o);
        }

        /// <summary>Partner ke paas paise In / Out — hisab kitab.</summary>
        public static long InsertPartnerMoney(
            int partnerId,
            string txnDate,
            string direction,
            decimal amount,
            string mode,
            string category,
            int? customerPlotId,
            long? receiptId,
            int? societyId,
            string note,
            SQLiteConnection useCon = null,
            SQLiteTransaction tx = null)
        {
            EnsurePartnerMoneySchema();
            if (partnerId <= 0 || amount <= 0) return 0;
            string dir = (direction ?? "In").Trim();
            if (dir != "In" && dir != "Out") dir = "In";

            var c = useCon ?? con;
            bool ownOpen = useCon == null && tx == null;
            if (ownOpen && c.State != ConnectionState.Open) c.Open();
            try
            {
                using (var cmd = new SQLiteCommand(@"
INSERT INTO partner_money
 (partner_id, txn_date, direction, amount, mode, category, customer_plot_id, receipt_id, society_id, note)
VALUES
 (@p, @d, @dir, @amt, @mode, @cat, @cp, @rid, @sid, @note);", c, tx))
                {
                    cmd.Parameters.AddWithValue("@p", partnerId);
                    cmd.Parameters.AddWithValue("@d", txnDate ?? DateTime.Today.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@dir", dir);
                    cmd.Parameters.AddWithValue("@amt", amount);
                    cmd.Parameters.AddWithValue("@mode", (object)mode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@cat", (object)category ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@cp", customerPlotId.HasValue ? (object)customerPlotId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@rid", receiptId.HasValue ? (object)receiptId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@sid", societyId.HasValue ? (object)societyId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@note", (object)note ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
                long newId;
                using (var idCmd = new SQLiteCommand("SELECT last_insert_rowid();", c, tx))
                    newId = Convert.ToInt64(idCmd.ExecuteScalar());
                try
                {
                    using (var sid = new SQLiteCommand(
                        "UPDATE partner_money SET sync_id = lower(hex(randomblob(16))) WHERE id=@id AND (sync_id IS NULL OR TRIM(sync_id)='');", c, tx))
                    {
                        sid.Parameters.AddWithValue("@id", newId);
                        sid.ExecuteNonQuery();
                    }
                }
                catch { }
                if (ownOpen)
                {
                    try { HubSyncService.NotifyLocalWrite(); } catch { }
                }
                return newId;
            }
            finally
            {
                if (ownOpen) ReleaseSharedIfIdle();
            }
        }

        public static void UpsertPartnerMoneyForReceipt(
            long receiptId,
            int partnerId,
            int customerPlotId,
            string txnDate,
            decimal amount,
            string mode,
            string note,
            SQLiteConnection useCon = null,
            SQLiteTransaction tx = null)
        {
            EnsurePartnerMoneySchema();
            var c = useCon ?? con;
            HubSyncService.DeletePartnerMoneyWhere(c, tx,
                "receipt_id=@rid",
                cmd => cmd.Parameters.AddWithValue("@rid", receiptId));
            if (partnerId <= 0 || amount <= 0) return;

            int? societyId = null;
            using (var q = new SQLiteCommand(@"
SELECT p.society_id FROM customer_plots cp
JOIN plots p ON p.id = cp.plot_id
WHERE cp.id=@cp;", c, tx))
            {
                q.Parameters.AddWithValue("@cp", customerPlotId);
                var o = q.ExecuteScalar();
                if (o != null && o != DBNull.Value) societyId = Convert.ToInt32(o);
            }

            InsertPartnerMoney(
                partnerId, txnDate, "In", amount, mode, "Receipt",
                customerPlotId, receiptId, societyId, note, c, tx);

            SetAllocationPartner(customerPlotId, partnerId, c, tx);
        }

        // ── Expense Categories (additive — Expensetbl column same rehta hai) ─────
        private static bool _expenseCategoryReady = false;

        public static void EnsureExpenseCategorySchema()
        {
            if (_expenseCategoryReady) return;
            try
            {
                using (var local = new SQLiteConnection(con_string))
                {
                    local.Open();

                    using (var cmd = new SQLiteCommand(@"
CREATE TABLE IF NOT EXISTS expense_categories (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  name TEXT NOT NULL UNIQUE
);", local))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    using (var seed = new SQLiteCommand(@"
INSERT OR IGNORE INTO expense_categories (name)
SELECT DISTINCT TRIM(Category) FROM Expensetbl
WHERE Category IS NOT NULL AND TRIM(Category) <> '';", local))
                    {
                        seed.ExecuteNonQuery();
                    }
                }
                _expenseCategoryReady = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Expense category schema: " + ex.Message);
            }
        }

        public static DataTable GetExpenseCategories()
        {
            EnsureExpenseCategorySchema();
            return GetData("SELECT id, name FROM expense_categories ORDER BY name;");
        }

        public static bool AddExpenseCategory(string name)
        {
            EnsureExpenseCategorySchema();
            name = (name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name)) return false;
            int n = ExecuteNonQuery(
                "INSERT OR IGNORE INTO expense_categories (name) VALUES (@n);",
                new Dictionary<string, object> { { "@n", name } });
            return n > 0;
        }

        public static bool UpdateExpenseCategory(int id, string name)
        {
            EnsureExpenseCategorySchema();
            name = (name ?? "").Trim();
            if (id <= 0 || string.IsNullOrWhiteSpace(name)) return false;
            try
            {
                int n = ExecuteNonQuery(
                    "UPDATE expense_categories SET name=@n WHERE id=@id;",
                    new Dictionary<string, object> { { "@n", name }, { "@id", id } });
                return n > 0;
            }
            catch { return false; }
        }

        public static bool DeleteExpenseCategory(int id)
        {
            EnsureExpenseCategorySchema();
            if (id <= 0) return false;
            int n = ExecuteNonQuery(
                "DELETE FROM expense_categories WHERE id=@id;",
                new Dictionary<string, object> { { "@id", id } });
            return n > 0;
        }
    }
}
