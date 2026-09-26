using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace HubBuilders
{
    /// <summary>
    /// Additive Staff column on every bound grid — kaun staff ne entry ki.
    /// Existing columns/layout nahi badalta.
    /// </summary>
    public static class StaffGridHelper
    {
        private static readonly Regex IdInDetail = new Regex(
            @"id\s*=\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static Dictionary<string, string> _cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static DateTime _cacheAt = DateTime.MinValue;
        private static int _applying;
        private static readonly HashSet<int> _hooked = new HashSet<int>();

        public static void Attach(Control root)
        {
            if (root == null) return;
            Walk(root);
        }

        private static void Walk(Control c)
        {
            var gv = c as DataGridView;
            if (gv != null)
            {
                int key = gv.GetHashCode();
                if (_hooked.Add(key))
                    gv.DataBindingComplete += OnBound;
                else
                {
                    gv.DataBindingComplete -= OnBound;
                    gv.DataBindingComplete += OnBound;
                }
            }
            foreach (Control child in c.Controls)
                Walk(child);
        }

        private static void OnBound(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            var gv = sender as DataGridView;
            if (gv == null) return;
            DataTable dt = gv.DataSource as DataTable;
            if (dt == null)
            {
                var dv = gv.DataSource as DataView;
                if (dv != null) dt = dv.Table;
            }
            if (dt == null) return;
            Apply(gv, dt);
        }

        public static void Apply(DataGridView gv, DataTable dt)
        {
            if (gv == null || dt == null) return;
            if (Interlocked.Exchange(ref _applying, 1) == 1) return;
            try
            {
                if (!dt.Columns.Contains("staff_name"))
                    dt.Columns.Add("staff_name", typeof(string));

                if (gv.Columns["dgvStaff"] == null)
                {
                    gv.Columns.Add(new DataGridViewTextBoxColumn
                    {
                        Name = "dgvStaff",
                        HeaderText = "Staff",
                        DataPropertyName = "staff_name",
                        Width = 120,
                        ReadOnly = true,
                        AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                    });
                }
                else
                {
                    gv.Columns["dgvStaff"].DataPropertyName = "staff_name";
                    gv.Columns["dgvStaff"].HeaderText = "Staff";
                }

                RefreshCache();
                string idCol = FindIdColumn(dt);
                string tableHint = (dt.TableName ?? "").Trim();
                foreach (DataRow row in dt.Rows)
                {
                    if (row.RowState == DataRowState.Deleted) continue;
                    string staff = "";
                    if (idCol != null)
                    {
                        string id = Convert.ToString(row[idCol] ?? "");
                        if (!string.IsNullOrWhiteSpace(id))
                        {
                            string keyed = tableHint + "|" + id.Trim();
                            if (tableHint.Length > 0 && _cache.TryGetValue(keyed, out staff))
                            { }
                            else if (_cache.TryGetValue("id=" + id.Trim(), out staff) == false)
                                staff = "";
                        }
                    }
                    row["staff_name"] = staff ?? "";
                }
            }
            catch { }
            finally { Interlocked.Exchange(ref _applying, 0); }
        }

        private static string FindIdColumn(DataTable dt)
        {
            string[] names = { "id", "Id", "ID", "sid", "person_id", "deal_id", "credit_id", "userID",
                "receipt_id", "customer_plot_id", "sr_no" };
            foreach (string n in names)
                if (dt.Columns.Contains(n)) return n;
            foreach (DataColumn c in dt.Columns)
            {
                if (string.Equals(c.ColumnName, "id", StringComparison.OrdinalIgnoreCase))
                    return c.ColumnName;
                if (c.ColumnName.EndsWith("_id", StringComparison.OrdinalIgnoreCase)
                    && !c.ColumnName.Equals("society_id", StringComparison.OrdinalIgnoreCase))
                    return c.ColumnName;
            }
            return null;
        }

        private static void RefreshCache()
        {
            if ((DateTime.Now - _cacheAt).TotalSeconds < 8 && _cache.Count > 0) return;
            var next = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (var con = new SQLiteConnection(MainClass.con_string))
                using (var cmd = new SQLiteCommand(
                    "SELECT detail, module_name, staff_name, staff_username FROM staff_entry_log ORDER BY id ASC;", con))
                {
                    con.Open();
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            string detail = Convert.ToString(r["detail"] ?? "");
                            string module = Convert.ToString(r["module_name"] ?? "").Trim();
                            string name = Convert.ToString(r["staff_name"] ?? "").Trim();
                            if (name.Length == 0)
                                name = Convert.ToString(r["staff_username"] ?? "").Trim();
                            if (name.Length == 0) continue;
                            var m = IdInDetail.Match(detail ?? "");
                            if (!m.Success) continue;
                            string id = m.Groups[1].Value;
                            next["id=" + id] = name;
                            if (module.Length > 0)
                                next[module + "|" + id] = name;
                        }
                    }
                }
            }
            catch { }
            _cache = next;
            _cacheAt = DateTime.Now;
        }
    }
}
