using HubBuilders.ProjectConnection;
using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace HubBuilders.Report_Form
{
    /// <summary>
    /// Ctrl+1: customer suggest search (Allocate-style). Ctrl+2 / Esc: apply &amp; close.
    /// Forms with txtSearch/txtNameSearch: fills that box. Combo customer: selects item.
    /// Forms without search: opens floating suggest box and filters grid if present.
    /// Does not redesign existing layouts.
    /// </summary>
    public static class Ctrl1SuggestHelper
    {
        private static readonly Color ThemeFill = Color.FromArgb(37, 41, 74);
        private static readonly Color ThemeBorder = Color.FromArgb(112, 51, 255);
        private static readonly Color ThemeText = Color.White;

        private static readonly ConditionalWeakTable<Form, State> States =
            new ConditionalWeakTable<Form, State>();

        private sealed class State
        {
            public Guna2TextBox Box;
            public ListBox Suggest;
            public Panel Host;
            public Label Hint;
            public bool Active;
            public bool Busy;
            public bool Built;
            public Control Anchor; // existing search / combo to overlay when possible
        }

        /// <summary>Call from Sample.ProcessCmdKey — only Report_Form windows.</summary>
        public static bool TryHandle(Form form, Keys keyData)
        {
            if (form == null || form.IsDisposed) return false;
            if (!IsReportForm(form)) return false;

            if (keyData == (Keys.Control | Keys.D1))
            {
                Show(form);
                return true;
            }
            if (keyData == (Keys.Control | Keys.D2))
            {
                Hide(form, apply: true);
                return true;
            }
            if (keyData == Keys.Escape)
            {
                var st = GetState(form, create: false);
                if (st != null && st.Active)
                {
                    Hide(form, apply: true);
                    return true;
                }
            }
            return false;
        }

        public static void Attach(Form form)
        {
            // Optional explicit attach — Sample hook already covers Report_Form.
            if (form == null) return;
            form.KeyPreview = true;
            GetState(form, create: true);
        }

        private static bool IsReportForm(Form form)
        {
            try
            {
                var ns = form.GetType().Namespace ?? "";
                if (ns.IndexOf("Report_Form", StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
                // Password helper sits in same namespace — skip
                string name = form.GetType().Name ?? "";
                if (name.IndexOf("Password", StringComparison.OrdinalIgnoreCase) >= 0)
                    return false;
                return true;
            }
            catch { }
            return false;
        }

        private static State GetState(Form form, bool create)
        {
            State st;
            if (States.TryGetValue(form, out st)) return st;
            if (!create) return null;
            st = new State();
            States.Add(form, st);
            return st;
        }

        private static void Show(Form form)
        {
            var st = GetState(form, create: true);
            EnsureBuilt(form, st);
            st.Active = true;
            st.Busy = true;
            try
            {
                // Prefer overlay on existing search / customer combo
                st.Anchor = FindAnchor(form);
                Position(form, st);
                if (st.Host != null)
                {
                    st.Host.Visible = true;
                    st.Host.BringToFront();
                }
                st.Box.Visible = true;
                st.Box.BringToFront();
                st.Suggest.Visible = false;

                // Prefill from existing search text
                string cur = "";
                var tb = FindSearchBox(form);
                if (tb != null) cur = (GetControlText(tb) ?? "").Trim();
                st.Box.Text = cur;
            }
            finally { st.Busy = false; }

            try
            {
                st.Box.Focus();
                st.Box.SelectAll();
                RefreshSuggest(form, st);
            }
            catch { }
        }

        private static void Hide(Form form, bool apply)
        {
            State st;
            if (!States.TryGetValue(form, out st) || st == null) return;
            if (apply) Apply(form, st);
            st.Active = false;
            if (st.Box != null) st.Box.Visible = false;
            if (st.Suggest != null) st.Suggest.Visible = false;
            if (st.Host != null) st.Host.Visible = false;
        }

        private static void EnsureBuilt(Form form, State st)
        {
            if (st.Built) return;

            st.Host = new Panel
            {
                Name = "ctrl1SuggestHost",
                BackColor = Color.FromArgb(32, 36, 61),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false,
                Size = new Size(420, 44),
                Padding = new Padding(4)
            };

            st.Hint = new Label
            {
                AutoSize = false,
                Text = "Ctrl+1 search · Enter select · Esc/Ctrl+2 close",
                ForeColor = Color.Silver,
                Font = new Font("Segoe UI", 8f),
                Dock = DockStyle.Bottom,
                Height = 16,
                TextAlign = ContentAlignment.MiddleLeft
            };

            st.Box = new Guna2TextBox
            {
                FillColor = ThemeFill,
                ForeColor = ThemeText,
                BorderColor = ThemeBorder,
                BorderRadius = 12,
                Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
                PlaceholderForeColor = Color.Silver,
                PlaceholderText = "Type customer name / mobile…",
                Cursor = Cursors.IBeam,
                Dock = DockStyle.Fill,
                TabStop = true
            };
            try
            {
                st.Box.FocusedState.BorderColor = Color.FromArgb(94, 148, 255);
                st.Box.HoverState.BorderColor = ThemeBorder;
            }
            catch { }

            st.Suggest = new ListBox
            {
                Font = new Font("Segoe UI", 10f),
                BackColor = ThemeFill,
                ForeColor = ThemeText,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false,
                IntegralHeight = false,
                Height = 220
            };

            st.Host.Controls.Add(st.Box);
            st.Host.Controls.Add(st.Hint);
            form.Controls.Add(st.Host);
            form.Controls.Add(st.Suggest);

            st.Box.TextChanged += (s, e) =>
            {
                if (st.Busy || !st.Active) return;
                RefreshSuggest(form, st);
            };

            st.Box.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Down && st.Suggest.Visible && st.Suggest.Items.Count > 0)
                {
                    e.SuppressKeyPress = true;
                    st.Suggest.Focus();
                    if (st.Suggest.SelectedIndex < 0) st.Suggest.SelectedIndex = 0;
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    Apply(form, st);
                    Hide(form, apply: false);
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    e.SuppressKeyPress = true;
                    Hide(form, apply: true);
                }
            };

            st.Suggest.Click += (s, e) =>
            {
                if (st.Suggest.SelectedItem == null) return;
                st.Busy = true;
                try { st.Box.Text = Convert.ToString(st.Suggest.SelectedItem); }
                finally { st.Busy = false; }
                Apply(form, st);
                Hide(form, apply: false);
            };

            st.Suggest.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    if (st.Suggest.SelectedItem != null)
                    {
                        st.Busy = true;
                        try { st.Box.Text = Convert.ToString(st.Suggest.SelectedItem); }
                        finally { st.Busy = false; }
                        Apply(form, st);
                        Hide(form, apply: false);
                    }
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    e.SuppressKeyPress = true;
                    st.Box.Focus();
                }
            };

            form.Resize += (s, e) =>
            {
                if (st.Active) Position(form, st);
            };

            st.Built = true;
        }

        private static void Position(Form form, State st)
        {
            if (st.Box == null) return;

            Control anchor = st.Anchor ?? FindAnchor(form);
            int x, y, w;
            if (anchor != null && anchor.Visible && !(anchor is ComboBox))
            {
                // Overlay / sit on existing search textbox
                var pt = form.PointToClient(anchor.Parent.PointToScreen(anchor.Location));
                x = pt.X;
                y = Math.Max(8, pt.Y - 2);
                w = Math.Max(280, anchor.Width);
                st.Host.Parent = form;
                st.Host.SetBounds(x, y, w, 48);
            }
            else if (anchor is ComboBox)
            {
                var pt = form.PointToClient(anchor.Parent.PointToScreen(anchor.Location));
                x = pt.X;
                y = Math.Max(8, pt.Y - 2);
                w = Math.Max(320, anchor.Width);
                st.Host.SetBounds(x, y, w, 48);
            }
            else
            {
                // Floating top-center — no layout change to filters
                w = 420;
                x = Math.Max(12, (form.ClientSize.Width - w) / 2);
                y = 72;
                st.Host.SetBounds(x, y, w, 48);
            }

            st.Host.BringToFront();
            st.Suggest.SetBounds(st.Host.Left, st.Host.Bottom + 2, Math.Max(st.Host.Width, 320), 220);
            if (st.Suggest.Visible) st.Suggest.BringToFront();
        }

        private static void RefreshSuggest(Form form, State st)
        {
            string q = (st.Box.Text ?? "").Trim();
            var items = LoadCustomerSuggestions(q);

            // Also include combo display items when anchored to a combo (plot list etc.)
            var combo = FindCustomerCombo(form);
            if (combo != null && items.Count < 40)
            {
                foreach (var s in FilterComboItems(combo, q))
                {
                    if (!items.Contains(s)) items.Add(s);
                    if (items.Count >= 100) break;
                }
            }

            st.Suggest.BeginUpdate();
            try
            {
                st.Suggest.Items.Clear();
                foreach (var s in items) st.Suggest.Items.Add(s);
            }
            finally { st.Suggest.EndUpdate(); }

            Position(form, st);
            if (st.Suggest.Items.Count > 0 && st.Box.Focused)
            {
                if (st.Suggest.SelectedIndex < 0) st.Suggest.SelectedIndex = 0;
                st.Suggest.Visible = true;
                st.Suggest.BringToFront();
                st.Host.BringToFront();
                st.Box.BringToFront();
            }
            else
            {
                st.Suggest.Visible = false;
            }
        }

        private static void Apply(Form form, State st)
        {
            if (st?.Box == null) return;
            string txt = (st.Box.Text ?? "").Trim();
            if (st.Suggest != null && st.Suggest.Visible && st.Suggest.SelectedItem != null)
            {
                string sel = Convert.ToString(st.Suggest.SelectedItem).Trim();
                if (sel.Length > 0) txt = sel;
            }
            if (txt.Length == 0)
            {
                // Clear search if empty apply
                var emptyBox = FindSearchBox(form);
                if (emptyBox != null) SetControlText(emptyBox, "");
                TryTriggerSearch(form);
                FilterGrids(form, "");
                return;
            }

            string nameOnly = StripPipe(txt);

            // 1) Customer / customer-plot combo FIRST (VIP Customer important, etc.)
            var combo = FindCustomerCombo(form);
            if (combo != null && TrySelectCombo(combo, txt))
            {
                // Also mirror into search box if present (filters combo lists)
                var searchSync = FindSearchBox(form);
                if (searchSync != null)
                {
                    SetControlText(searchSync, nameOnly);
                    TryTriggerSearch(form);
                    // Re-select after filter reload (receipt statement rebuilds combo)
                    TrySelectCombo(FindCustomerCombo(form) ?? combo, txt);
                }
                try { combo.Focus(); } catch { }
                return;
            }

            // 2) Existing search textbox
            var search = FindSearchBox(form);
            if (search != null)
            {
                SetControlText(search, nameOnly);
                TryTriggerSearch(form);
                try { search.Focus(); } catch { }
                return;
            }

            // 3) Fallback: filter any DataGridView on the form
            FilterGrids(form, nameOnly);
        }

        private static Control FindAnchor(Form form)
        {
            return FindSearchBox(form) ?? (Control)FindCustomerCombo(form);
        }

        private static Control FindSearchBox(Form form)
        {
            // monthlywise: txtSearch = year, not name — skip
            string formName = form.GetType().Name ?? "";
            if (formName.IndexOf("monthlywise", StringComparison.OrdinalIgnoreCase) >= 0)
                return null;

            string[] names = { "txtNameSearch", "txtSearch", "txtKeyword", "txtFilter" };
            foreach (var n in names)
            {
                var found = form.Controls.Find(n, true);
                if (found.Length > 0 && found[0] != null && found[0].Visible)
                    return found[0];
            }
            // Any control with "search" in name (TextBox / Guna2TextBox)
            try
            {
                foreach (Control c in Enumerate(form))
                {
                    if (c == null || !c.Visible) continue;
                    string n = (c.Name ?? "").ToLowerInvariant();
                    if (n.Contains("search") && (c is TextBox || c is Guna2TextBox))
                        return c;
                }
            }
            catch { }
            return null;
        }

        private static ComboBox FindCustomerCombo(Form form)
        {
            string[] names =
            {
                "cmbCustomer", "cmbCustomerPlot", "cmbCustomers",
                "comboCustomer", "cmbCust"
            };
            foreach (var n in names)
            {
                var found = form.Controls.Find(n, true);
                if (found.Length > 0)
                {
                    var cb = found[0] as ComboBox;
                    if (cb != null) return cb;
                }
            }

            // Code-built combos without Name: detect by DisplayMember / content
            try
            {
                foreach (Control c in Enumerate(form))
                {
                    var cb = c as ComboBox;
                    if (cb == null || !cb.Visible) continue;
                    string n = (cb.Name ?? "").ToLowerInvariant();
                    if (n.Contains("customer")) return cb;

                    string dm = (cb.DisplayMember ?? "").Trim().ToLowerInvariant();
                    if (dm == "display" || dm == "disp" || dm == "customer_name")
                        return cb;

                    // DataTable with display column shaped like "Name | mobile"
                    var dt = cb.DataSource as DataTable;
                    if (dt != null)
                    {
                        string col = string.IsNullOrWhiteSpace(cb.DisplayMember) ? "" : cb.DisplayMember;
                        if (col.Length > 0 && dt.Columns.Contains(col) && dt.Rows.Count > 0)
                        {
                            string sample = Convert.ToString(dt.Rows[0][col] ?? "");
                            if (sample.IndexOf('|') >= 0) return cb;
                        }
                        if (dt.Columns.Contains("display")) return cb;
                        if (dt.Columns.Contains("disp")) return cb;
                    }
                }
            }
            catch { }
            return null;
        }

        private static void TryTriggerSearch(Form form)
        {
            try
            {
                string[] btnNames = { "btnSearch", "btnPreview", "btnLoad", "btnRefresh", "btnGo" };
                foreach (var n in btnNames)
                {
                    var found = form.Controls.Find(n, true);
                    if (found.Length == 0) continue;
                    var b = found[0] as Button;
                    if (b != null && b.Enabled && b.Visible)
                    {
                        b.PerformClick();
                        return;
                    }
                    var gb = found[0] as Guna2Button;
                    if (gb != null && gb.Enabled && gb.Visible)
                    {
                        gb.PerformClick();
                        return;
                    }
                }
            }
            catch { }
        }

        private static void FilterGrids(Form form, string keyword)
        {
            keyword = (keyword ?? "").Trim();
            try
            {
                foreach (Control c in Enumerate(form))
                {
                    var dgv = c as DataGridView;
                    if (dgv == null || dgv.DataSource == null) continue;

                    if (dgv.DataSource is DataTable dt)
                    {
                        ApplyDataViewFilter(dt.DefaultView, dgv, keyword);
                    }
                    else if (dgv.DataSource is DataView dv)
                    {
                        ApplyDataViewFilter(dv, dgv, keyword);
                    }
                    else if (dgv.DataSource is BindingSource bs)
                    {
                        if (bs.DataSource is DataTable t)
                            ApplyDataViewFilter(t.DefaultView, dgv, keyword);
                        else if (bs.DataSource is DataView v)
                            ApplyDataViewFilter(v, dgv, keyword);
                        else
                        {
                            try { bs.Filter = BuildBindingFilter(bs, keyword); }
                            catch { }
                        }
                    }
                }
            }
            catch { }
        }

        private static void ApplyDataViewFilter(DataView view, DataGridView dgv, string keyword)
        {
            if (view == null) return;
            if (string.IsNullOrEmpty(keyword))
            {
                view.RowFilter = "";
                return;
            }
            var parts = new List<string>();
            string esc = EscapeFilter(keyword);
            // Prefer name-like columns
            foreach (DataColumn col in view.Table.Columns)
            {
                if (col.DataType != typeof(string) && col.DataType != typeof(object))
                    continue;
                string cn = col.ColumnName;
                string lower = cn.ToLowerInvariant();
                if (lower.Contains("name") || lower.Contains("customer") || lower.Contains("mobile")
                    || lower.Contains("plot") || lower.Contains("society") || lower.Contains("broker")
                    || lower.Contains("partner") || lower == "disp" || lower.Contains("missed"))
                {
                    parts.Add("CONVERT([" + cn + "], 'System.String') LIKE '%" + esc + "%'");
                }
            }
            if (parts.Count == 0)
            {
                // fallback: first few string columns
                int n = 0;
                foreach (DataColumn col in view.Table.Columns)
                {
                    if (col.DataType != typeof(string)) continue;
                    parts.Add("CONVERT([" + col.ColumnName + "], 'System.String') LIKE '%" + esc + "%'");
                    if (++n >= 4) break;
                }
            }
            if (parts.Count > 0)
                view.RowFilter = string.Join(" OR ", parts);
        }

        private static string BuildBindingFilter(BindingSource bs, string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return "";
            return ""; // unknown shape — skip
        }

        private static string EscapeFilter(string s)
        {
            return (s ?? "").Replace("'", "''").Replace("*", "[*]").Replace("%", "[%]");
        }

        private static List<string> LoadCustomerSuggestions(string q)
        {
            var list = new List<string>();
            try
            {
                using (var con = new SQLiteConnection(projectconnection.conReturn()))
                {
                    con.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT name, COALESCE(mobile,'') AS mobile FROM customers ORDER BY name;", con))
                    using (var r = cmd.ExecuteReader())
                    {
                        q = (q ?? "").Trim();
                        while (r.Read())
                        {
                            string name = Convert.ToString(r["name"] ?? "").Trim();
                            string mobile = Convert.ToString(r["mobile"] ?? "").Trim();
                            if (name.Length == 0) continue;
                            string disp = string.IsNullOrEmpty(mobile) ? name : (name + " | " + mobile);
                            if (q.Length == 0
                                || name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                                || mobile.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                list.Add(disp);
                                if (list.Count >= 100) break;
                            }
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        private static List<string> FilterComboItems(ComboBox cb, string q)
        {
            var list = new List<string>();
            try
            {
                if (cb.DataSource is DataTable dt)
                {
                    string col = ResolveDisplayColumn(dt, cb.DisplayMember);
                    if (col == null) return list;
                    foreach (DataRow row in dt.Rows)
                    {
                        string s = Convert.ToString(row[col] ?? "").Trim();
                        if (s.Length == 0) continue;
                        if (q.Length == 0 || s.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            list.Add(s);
                            if (list.Count >= 80) break;
                        }
                    }
                }
                else
                {
                    foreach (var item in cb.Items)
                    {
                        string s = Convert.ToString(item ?? "").Trim();
                        if (s.Length == 0) continue;
                        if (q.Length == 0 || s.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            list.Add(s);
                            if (list.Count >= 80) break;
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        private static bool TrySelectCombo(ComboBox cb, string txt)
        {
            if (cb == null) return false;
            string want = StripPipe(txt);
            string full = (txt ?? "").Trim();
            string fullMobile = "";
            int pipe = full.IndexOf('|');
            if (pipe >= 0) fullMobile = full.Substring(pipe + 1).Trim();

            try
            {
                if (cb.DataSource is DataTable dt)
                {
                    string col = ResolveDisplayColumn(dt, cb.DisplayMember);
                    if (col == null) return false;

                    int best = -1;
                    // Pass 1: exact full display match
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        string s = Convert.ToString(dt.Rows[i][col] ?? "").Trim();
                        if (s.Equals(full, StringComparison.OrdinalIgnoreCase))
                        {
                            best = i;
                            break;
                        }
                    }
                    // Pass 2: name|mobile exact parts
                    if (best < 0 && want.Length > 0)
                    {
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            string s = Convert.ToString(dt.Rows[i][col] ?? "").Trim();
                            string sn = StripPipe(s);
                            string sm = "";
                            int p = s.IndexOf('|');
                            if (p >= 0) sm = s.Substring(p + 1).Trim();
                            if (sn.Equals(want, StringComparison.OrdinalIgnoreCase)
                                && (fullMobile.Length == 0 || sm.Equals(fullMobile, StringComparison.OrdinalIgnoreCase)))
                            {
                                best = i;
                                break;
                            }
                        }
                    }
                    // Pass 3: starts with name
                    if (best < 0 && want.Length > 0)
                    {
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            string s = Convert.ToString(dt.Rows[i][col] ?? "").Trim();
                            string sn = StripPipe(s);
                            if (sn.Equals(want, StringComparison.OrdinalIgnoreCase)
                                || s.StartsWith(want + " |", StringComparison.OrdinalIgnoreCase)
                                || s.StartsWith(want + "|", StringComparison.OrdinalIgnoreCase))
                            {
                                best = i;
                                break;
                            }
                        }
                    }
                    // Pass 4: contains (last resort)
                    if (best < 0 && want.Length > 0)
                    {
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            string s = Convert.ToString(dt.Rows[i][col] ?? "").Trim();
                            if (s.IndexOf(want, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                best = i;
                                break;
                            }
                        }
                    }

                    if (best < 0) return false;

                    // Prefer SelectedValue when ValueMember present (reliable with DataTable)
                    string vm = (cb.ValueMember ?? "").Trim();
                    if (vm.Length > 0 && dt.Columns.Contains(vm))
                    {
                        cb.SelectedValue = dt.Rows[best][vm];
                    }
                    else
                    {
                        cb.SelectedIndex = best;
                    }
                    return true;
                }

                for (int i = 0; i < cb.Items.Count; i++)
                {
                    string s = Convert.ToString(cb.Items[i] ?? "").Trim();
                    if (s.Equals(full, StringComparison.OrdinalIgnoreCase)
                        || StripPipe(s).Equals(want, StringComparison.OrdinalIgnoreCase)
                        || s.IndexOf(want, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        cb.SelectedIndex = i;
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        private static string ResolveDisplayColumn(DataTable dt, string displayMember)
        {
            if (dt == null) return null;
            string col = (displayMember ?? "").Trim();
            if (col.Length > 0 && dt.Columns.Contains(col)) return col;
            if (dt.Columns.Contains("display")) return "display";
            if (dt.Columns.Contains("disp")) return "disp";
            if (dt.Columns.Contains("name")) return "name";
            if (dt.Columns.Contains("customer_name")) return "customer_name";
            return null;
        }

        private static string StripPipe(string txt)
        {
            if (string.IsNullOrWhiteSpace(txt)) return "";
            int p = txt.IndexOf('|');
            if (p >= 0) txt = txt.Substring(0, p);
            return txt.Trim();
        }

        private static string GetControlText(Control c)
        {
            if (c is Guna2TextBox g) return g.Text;
            return c.Text;
        }

        private static void SetControlText(Control c, string text)
        {
            if (c is Guna2TextBox g) g.Text = text ?? "";
            else c.Text = text ?? "";
        }

        private static IEnumerable<Control> Enumerate(Control root)
        {
            var stack = new Stack<Control>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var c = stack.Pop();
                yield return c;
                foreach (Control child in c.Controls)
                    stack.Push(child);
            }
        }
    }
}
