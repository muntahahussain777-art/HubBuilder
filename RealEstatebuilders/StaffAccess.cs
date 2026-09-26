using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;

namespace HubBuilders
{
    /// <summary>
    /// Admin-defined staff role rules: open / add / edit / delete per module.
    /// Additive table staff_role_permissions — purana data safe.
    /// </summary>
    internal static class StaffAccess
    {
        public sealed class ModuleDef
        {
            public string Key;
            public string Title;
            public ModuleDef(string key, string title) { Key = key; Title = title; }
        }

        public static readonly ModuleDef[] Modules =
        {
            new ModuleDef("home", "Home"),
            new ModuleDef("customers", "Customer"),
            new ModuleDef("societies", "Society"),
            new ModuleDef("blocks", "Block"),
            new ModuleDef("plot_types", "Plot Type"),
            new ModuleDef("plots", "Plot Inventory"),
            new ModuleDef("customer_plots", "Allocate"),
            new ModuleDef("ledger", "Ledger"),
            new ModuleDef("receipts", "Receipt Form"),
            new ModuleDef("expensetbl", "Expense"),
            new ModuleDef("reports", "Reports"),
            new ModuleDef("users", "Settings / Password"),
            new ModuleDef("brokers", "Broker"),
            new ModuleDef("person", "Owner / Partner"),
            new ModuleDef("deal", "Deal"),
            new ModuleDef("credit", "Credit"),
            new ModuleDef("partner_money", "Partner Cash"),
            new ModuleDef("staff_entry_log", "Staff Entry Log"),
            new ModuleDef("backup", "Backup Restore"),
            new ModuleDef("agreement", "Agreement Design"),
            new ModuleDef("dues_whatsapp", "Dues WhatsApp"),
        };

        private static readonly Dictionary<string, Perm> _cache =
            new Dictionary<string, Perm>(StringComparer.OrdinalIgnoreCase);
        private static string _cacheRole = "";
        private static DateTime _cacheAt = DateTime.MinValue;

        private struct Perm
        {
            public bool Open, Add, Edit, Delete;
        }

        public static void EnsureSchema()
        {
            try
            {
                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    using (var cmd = new SQLiteCommand(@"
CREATE TABLE IF NOT EXISTS staff_role_permissions (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  role_name TEXT NOT NULL,
  module_key TEXT NOT NULL,
  can_open INTEGER NOT NULL DEFAULT 1,
  can_add INTEGER NOT NULL DEFAULT 1,
  can_edit INTEGER NOT NULL DEFAULT 1,
  can_delete INTEGER NOT NULL DEFAULT 1,
  sync_id TEXT,
  UNIQUE(role_name, module_key)
);", con))
                        cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        public static void Invalidate()
        {
            _cache.Clear();
            _cacheRole = "";
            _cacheAt = DateTime.MinValue;
        }

        public static bool IsAdminRole(string role = null)
        {
            string r = string.IsNullOrWhiteSpace(role) ? MainClass.LoggedInRole : role;
            return string.Equals((r ?? "").Trim(), "Admin", StringComparison.OrdinalIgnoreCase);
        }

        public static bool Can(string moduleKey, string action)
        {
            try
            {
                if (IsAdminRole()) return true;
                string role = (MainClass.LoggedInRole ?? "").Trim();
                if (string.IsNullOrWhiteSpace(role)) return true;
                EnsureSchema();
                RefreshCacheIfNeeded(role);
                Perm p;
                if (!_cache.TryGetValue(moduleKey ?? "", out p))
                    return DefaultFor(moduleKey, action);
                string a = (action ?? "open").Trim().ToLowerInvariant();
                if (a == "open" || a == "view") return p.Open;
                if (a == "add" || a == "insert") return p.Add;
                if (a == "edit" || a == "update") return p.Edit;
                if (a == "delete" || a == "del") return p.Delete;
                return p.Open;
            }
            catch { return true; }
        }

        public static bool Deny(string moduleKey, string action, bool showMessage = true)
        {
            if (Can(moduleKey, action)) return false;
            if (showMessage)
            {
                try
                {
                    MessageBox.Show(
                        "Aap ke role (" + (MainClass.LoggedInRole ?? "") + ") ke liye yeh action allow nahi.",
                        "Access Denied",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                catch { }
            }
            return true;
        }

        public static string ModuleForSqlTable(string table)
        {
            string t = (table ?? "").Trim().ToLowerInvariant();
            if (t == "users") return "users";
            if (t == "customers") return "customers";
            if (t == "societies") return "societies";
            if (t == "blocks") return "blocks";
            if (t == "plot_types") return "plot_types";
            if (t == "plots") return "plots";
            if (t == "customer_plots" || t == "allocation_cancellations") return "customer_plots";
            if (t == "ledger") return "ledger";
            if (t == "receipts") return "receipts";
            if (t == "expensetbl") return "expensetbl";
            if (t == "brokers") return "brokers";
            if (t == "person") return "person";
            if (t == "deal") return "deal";
            if (t == "credit") return "credit";
            if (t == "partner_money" || t == "customer_plot_partners") return "partner_money";
            if (t == "staff_entry_log") return "staff_entry_log";
            return t;
        }

        public static string ModuleForForm(Form f)
        {
            if (f == null) return "";
            string n = f.GetType().Name ?? "";
            if (n.IndexOf("Customer", StringComparison.OrdinalIgnoreCase) >= 0) return "customers";
            if (n.IndexOf("Society", StringComparison.OrdinalIgnoreCase) >= 0) return "societies";
            if (n.IndexOf("Block", StringComparison.OrdinalIgnoreCase) >= 0) return "blocks";
            if (n.IndexOf("PlotType", StringComparison.OrdinalIgnoreCase) >= 0) return "plot_types";
            if (n.IndexOf("PlotInventory", StringComparison.OrdinalIgnoreCase) >= 0) return "plots";
            if (n.IndexOf("Allocate", StringComparison.OrdinalIgnoreCase) >= 0) return "customer_plots";
            if (n.IndexOf("Ledger", StringComparison.OrdinalIgnoreCase) >= 0) return "ledger";
            if (n.IndexOf("Receipt", StringComparison.OrdinalIgnoreCase) >= 0) return "receipts";
            if (n.IndexOf("Expense", StringComparison.OrdinalIgnoreCase) >= 0) return "expensetbl";
            if (n.IndexOf("Password", StringComparison.OrdinalIgnoreCase) >= 0) return "users";
            if (n.IndexOf("Broker", StringComparison.OrdinalIgnoreCase) >= 0) return "brokers";
            if (n.IndexOf("Owner", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("PartnerForm", StringComparison.OrdinalIgnoreCase) >= 0) return "person";
            if (n.IndexOf("PartnerCash", StringComparison.OrdinalIgnoreCase) >= 0) return "partner_money";
            if (n.IndexOf("Deal", StringComparison.OrdinalIgnoreCase) >= 0) return "deal";
            if (n.IndexOf("Credit", StringComparison.OrdinalIgnoreCase) >= 0) return "credit";
            if (n.IndexOf("StaffEntry", StringComparison.OrdinalIgnoreCase) >= 0) return "staff_entry_log";
            if (n.IndexOf("Backup", StringComparison.OrdinalIgnoreCase) >= 0) return "backup";
            if (n.IndexOf("Agreement", StringComparison.OrdinalIgnoreCase) >= 0) return "agreement";
            if (n.IndexOf("DuesWhatsApp", StringComparison.OrdinalIgnoreCase) >= 0) return "dues_whatsapp";
            if (n.IndexOf("Report", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("AllReport", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Dashboard", StringComparison.OrdinalIgnoreCase) >= 0) return "reports";
            if (n.IndexOf("Application", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Allotment", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Lasbela", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Vip", StringComparison.OrdinalIgnoreCase) >= 0) return "reports";
            return "";
        }

        public static DataTable LoadRoleGrid(string roleName)
        {
            EnsureSchema();
            var dt = new DataTable();
            dt.Columns.Add("module_key", typeof(string));
            dt.Columns.Add("Module", typeof(string));
            dt.Columns.Add("Open", typeof(bool));
            dt.Columns.Add("Add", typeof(bool));
            dt.Columns.Add("Edit", typeof(bool));
            dt.Columns.Add("Delete", typeof(bool));
            string role = (roleName ?? "").Trim();
            var map = new Dictionary<string, Perm>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT module_key, can_open, can_add, can_edit, can_delete FROM staff_role_permissions WHERE lower(trim(role_name))=lower(trim(@r));", con))
                    {
                        cmd.Parameters.AddWithValue("@r", role);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                map[Convert.ToString(r["module_key"])] = new Perm
                                {
                                    Open = ToBool(r["can_open"], true),
                                    Add = ToBool(r["can_add"], true),
                                    Edit = ToBool(r["can_edit"], true),
                                    Delete = ToBool(r["can_delete"], true),
                                };
                            }
                        }
                    }
                }
            }
            catch { }
            foreach (var m in Modules)
            {
                Perm p;
                if (!map.TryGetValue(m.Key, out p))
                    p = DefaultPerm(m.Key);
                var nr = dt.NewRow();
                nr["module_key"] = m.Key;
                nr["Module"] = m.Title;
                nr["Open"] = p.Open;
                nr["Add"] = p.Add;
                nr["Edit"] = p.Edit;
                nr["Delete"] = p.Delete;
                dt.Rows.Add(nr);
            }
            return dt;
        }

        public static void SaveRoleGrid(string roleName, DataTable grid)
        {
            if (grid == null) return;
            string role = (roleName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(role)) return;
            if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)) return;
            EnsureSchema();
            using (var con = new SQLiteConnection(MainClass.con_string))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    foreach (DataRow row in grid.Rows)
                    {
                        string key = Convert.ToString(row["module_key"]);
                        int o = ToInt(row["Open"]);
                        int a = ToInt(row["Add"]);
                        int e = ToInt(row["Edit"]);
                        int d = ToInt(row["Delete"]);
                        using (var cmd = new SQLiteCommand(@"
INSERT INTO staff_role_permissions(role_name, module_key, can_open, can_add, can_edit, can_delete, sync_id)
VALUES(@r,@m,@o,@a,@e,@d, lower(hex(randomblob(16))))
ON CONFLICT(role_name, module_key) DO UPDATE SET
  can_open=excluded.can_open,
  can_add=excluded.can_add,
  can_edit=excluded.can_edit,
  can_delete=excluded.can_delete;", con, tx))
                        {
                            cmd.Parameters.AddWithValue("@r", role);
                            cmd.Parameters.AddWithValue("@m", key);
                            cmd.Parameters.AddWithValue("@o", o);
                            cmd.Parameters.AddWithValue("@a", a);
                            cmd.Parameters.AddWithValue("@e", e);
                            cmd.Parameters.AddWithValue("@d", d);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    tx.Commit();
                }
            }
            Invalidate();
            try { HubSyncService.MarkDirty(); } catch { }
        }

        public static List<string> DistinctRoles()
        {
            var list = new List<string>();
            try
            {
                var dt = MainClass.GetData("SELECT DISTINCT Role FROM users WHERE Role IS NOT NULL AND TRIM(Role)<>'' ORDER BY Role;");
                if (dt != null)
                {
                    foreach (DataRow r in dt.Rows)
                    {
                        string s = Convert.ToString(r[0]).Trim();
                        if (string.IsNullOrWhiteSpace(s)) continue;
                        if (string.Equals(s, "Admin", StringComparison.OrdinalIgnoreCase)) continue;
                        if (!list.Exists(x => string.Equals(x, s, StringComparison.OrdinalIgnoreCase)))
                            list.Add(s);
                    }
                }
            }
            catch { }
            if (!list.Exists(x => string.Equals(x, "Manager", StringComparison.OrdinalIgnoreCase)))
                list.Insert(0, "Manager");
            if (!list.Exists(x => string.Equals(x, "Operator", StringComparison.OrdinalIgnoreCase)))
                list.Add("Operator");
            return list;
        }

        private static void RefreshCacheIfNeeded(string role)
        {
            if (string.Equals(_cacheRole, role, StringComparison.OrdinalIgnoreCase)
                && (DateTime.UtcNow - _cacheAt).TotalSeconds < 8
                && _cache.Count > 0)
                return;
            _cache.Clear();
            _cacheRole = role;
            _cacheAt = DateTime.UtcNow;
            try
            {
                using (var con = new SQLiteConnection(MainClass.con_string))
                {
                    con.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT module_key, can_open, can_add, can_edit, can_delete FROM staff_role_permissions WHERE lower(trim(role_name))=lower(trim(@r));", con))
                    {
                        cmd.Parameters.AddWithValue("@r", role);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                _cache[Convert.ToString(r["module_key"])] = new Perm
                                {
                                    Open = ToBool(r["can_open"], true),
                                    Add = ToBool(r["can_add"], true),
                                    Edit = ToBool(r["can_edit"], true),
                                    Delete = ToBool(r["can_delete"], true),
                                };
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private static bool DefaultFor(string moduleKey, string action)
        {
            var p = DefaultPerm(moduleKey);
            string a = (action ?? "open").Trim().ToLowerInvariant();
            if (a == "open" || a == "view") return p.Open;
            if (a == "add" || a == "insert") return p.Add;
            if (a == "edit" || a == "update") return p.Edit;
            if (a == "delete" || a == "del") return p.Delete;
            return p.Open;
        }

        private static Perm DefaultPerm(string moduleKey)
        {
            // No row saved yet: non-admin can use business forms; Settings / Staff Log closed.
            string k = (moduleKey ?? "").ToLowerInvariant();
            if (k == "users" || k == "staff_entry_log")
                return new Perm { Open = false, Add = false, Edit = false, Delete = false };
            return new Perm { Open = true, Add = true, Edit = true, Delete = true };
        }

        private static bool ToBool(object v, bool def)
        {
            try
            {
                if (v == null || v == DBNull.Value) return def;
                if (v is bool) return (bool)v;
                int n;
                if (int.TryParse(Convert.ToString(v), out n)) return n != 0;
                return Convert.ToBoolean(v);
            }
            catch { return def; }
        }

        private static int ToInt(object v)
        {
            try
            {
                if (v is bool) return (bool)v ? 1 : 0;
                int n;
                if (int.TryParse(Convert.ToString(v), out n)) return n != 0 ? 1 : 0;
                return Convert.ToBoolean(v) ? 1 : 0;
            }
            catch { return 1; }
        }
    }
}
