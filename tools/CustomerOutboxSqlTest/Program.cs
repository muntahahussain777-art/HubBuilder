using System;
using System.Collections;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using HubBuilders;

/// <summary>
/// Runs frmCustomerAdd-equivalent INSERT through MainClass.SQL().
/// Does NOT insert outbox itself. Background sync suppressed.
/// Temp DB only — never touches live PC DB / partner_money queue.
/// </summary>
static class Program
{
    static int Main()
    {
        string tmp = Path.Combine(Path.GetTempPath(), "hb_cust_outbox_" + Guid.NewGuid().ToString("N") + ".db");
        int fails = 0;
        try
        {
            string cs = "Data Source=" + tmp + ";Version=3;Busy Timeout=5000;Journal Mode=Wal;";
            // Redirect MainClass.con_string (readonly static) via reflection — temp DB only
            var fi = typeof(MainClass).GetField("con_string",
                BindingFlags.Public | BindingFlags.Static);
            if (fi == null) { Console.WriteLine("FAIL  con_string field missing"); return 1; }
            fi.SetValue(null, cs);

            MainClass.SuppressBackgroundSync = true; // HTTP/push band
            AppSafety.SuppressUi = true; // no MessageBox

            InitSchema(cs);
            // Corrupt outbox first — customer save must ROLL BACK
            using (var con = new SQLiteConnection(cs))
            {
                con.Open();
                using (var cmd = new SQLiteCommand("DROP TABLE IF EXISTS sync_outbox;", con))
                    cmd.ExecuteNonQuery();
                // Broken table: INSERT of full outbox columns will fail
                using (var cmd = new SQLiteCommand(
                    "CREATE TABLE sync_outbox (id INTEGER PRIMARY KEY);", con))
                    cmd.ExecuteNonQuery();
            }

            string qry = @"INSERT INTO customers (name, father_name, mobile, cnic, notes, created_at, sync_id)
                        VALUES (@name, @father_name, @mobile, @cnic, @notes, @created_at, lower(hex(randomblob(16))))";
            var ht = new Hashtable
            {
                { "@name", "ISO Fast Cust" },
                { "@father_name", "Father" },
                { "@mobile", "03001234567" },
                { "@cnic", "4210112345671" },
                { "@notes", "outbox-fail-rollback-test" },
                { "@created_at", "2026-11-22" }
            };
            int resFail = MainClass.SQL(qry, ht);
            long custFail = ScalarLong(cs, "SELECT COUNT(*) FROM customers WHERE notes='outbox-fail-rollback-test'");
            if (resFail == 0 && custFail == 0)
                Console.WriteLine("PASS  customer save rolls back when outbox insert fails");
            else
            {
                Console.WriteLine("FAIL  outbox-fail rollback (res=" + resFail + " cust=" + custFail + ")");
                fails++;
            }

            // Fix outbox schema — real save must create matching outbox (test does NOT insert outbox)
            using (var con = new SQLiteConnection(cs))
            {
                con.Open();
                using (var cmd = new SQLiteCommand("DROP TABLE IF EXISTS sync_outbox;", con))
                    cmd.ExecuteNonQuery();
                using (var cmd = new SQLiteCommand(@"
CREATE TABLE sync_outbox (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  table_name TEXT NOT NULL,
  local_pk INTEGER NOT NULL,
  sync_id TEXT NOT NULL,
  revision TEXT NOT NULL,
  op TEXT NOT NULL DEFAULT 'upsert',
  created_at TEXT DEFAULT (datetime('now'))
);", con))
                    cmd.ExecuteNonQuery();
            }

            ht["@notes"] = "outbox-success-test";
            int resOk = MainClass.SQL(qry, ht);
            long custOk = ScalarLong(cs, "SELECT COUNT(*) FROM customers WHERE notes='outbox-success-test'");
            long outboxOk = ScalarLong(cs, @"
SELECT COUNT(*) FROM sync_outbox o
JOIN customers c ON c.id=o.local_pk AND o.table_name='customers'
WHERE c.notes='outbox-success-test' AND o.sync_id=c.sync_id AND length(trim(c.sync_id))>0");
            string sid = ScalarStr(cs, "SELECT sync_id FROM customers WHERE notes='outbox-success-test' LIMIT 1");

            if (resOk > 0 && custOk == 1 && outboxOk == 1 && !string.IsNullOrWhiteSpace(sid))
                Console.WriteLine("PASS  MainClass.SQL customer INSERT writes matching outbox (bg sync off) sync_id=" + sid);
            else
            {
                Console.WriteLine("FAIL  customer+outbox after SQL (res=" + resOk + " cust=" + custOk + " outbox=" + outboxOk + " sid=" + sid + ")");
                fails++;
            }

            // SQL containing 'sync_' in VALUES must NOT skip outbox (table name is customers)
            ht["@notes"] = "has sync_ word in notes";
            ht["@name"] = "Cust sync_token";
            int resWord = MainClass.SQL(qry, ht);
            long outWord = ScalarLong(cs, @"
SELECT COUNT(*) FROM sync_outbox o
JOIN customers c ON c.id=o.local_pk
WHERE c.notes='has sync_ word in notes'");
            if (resWord > 0 && outWord == 1)
                Console.WriteLine("PASS  SQL text containing sync_ still enqueues outbox (table-name check)");
            else
            {
                Console.WriteLine("FAIL  sync_ in SQL text blocked outbox (res=" + resWord + " outbox=" + outWord + ")");
                fails++;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("FAIL  exception: " + ex);
            fails++;
        }
        finally
        {
            try { MainClass.SuppressBackgroundSync = false; } catch { }
            try { AppSafety.SuppressUi = false; } catch { }
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        }

        Console.WriteLine(fails == 0 ? "Result: all passed" : ("Result: " + fails + " failed"));
        return fails == 0 ? 0 : 1;
    }

    static void InitSchema(string cs)
    {
        using (var con = new SQLiteConnection(cs))
        {
            con.Open();
            using (var cmd = new SQLiteCommand(@"
CREATE TABLE IF NOT EXISTS customers(
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  name TEXT, father_name TEXT, mobile TEXT, cnic TEXT, notes TEXT, created_at TEXT, sync_id TEXT
);
CREATE TABLE IF NOT EXISTS staff_entry_log(
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  action_type TEXT, module_name TEXT, detail TEXT,
  staff_username TEXT, staff_name TEXT, staff_role TEXT,
  created_at TEXT DEFAULT (datetime('now','localtime'))
);
CREATE TABLE IF NOT EXISTS sync_outbox (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  table_name TEXT NOT NULL,
  local_pk INTEGER NOT NULL,
  sync_id TEXT NOT NULL,
  revision TEXT NOT NULL,
  op TEXT NOT NULL DEFAULT 'upsert',
  created_at TEXT DEFAULT (datetime('now'))
);", con))
                cmd.ExecuteNonQuery();
        }
    }

    static long ScalarLong(string cs, string sql)
    {
        using (var con = new SQLiteConnection(cs))
        {
            con.Open();
            using (var cmd = new SQLiteCommand(sql, con))
                return Convert.ToInt64(cmd.ExecuteScalar() ?? 0);
        }
    }

    static string ScalarStr(string cs, string sql)
    {
        using (var con = new SQLiteConnection(cs))
        {
            con.Open();
            using (var cmd = new SQLiteCommand(sql, con))
                return Convert.ToString(cmd.ExecuteScalar() ?? "");
        }
    }
}
