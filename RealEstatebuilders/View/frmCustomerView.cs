using HubBuilders.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace HubBuilders.View
{
    public partial class frmCustomerView : SampleView
    {
        public frmCustomerView()
        {
            InitializeComponent();
        }

        private void frmCustomerView_Load(object sender, EventArgs e)
        {
            try
            {
                // Ensure grid stays consistent after every bind
                guna2DataGridView1.DataBindingComplete += (s, ev) =>
                {
                    try { /* keep any column tweaks here if needed */ }
                    catch (Exception ex) { ShowError("Post-bind failed.\n\n" + ex.Message); }
                };
                /* StaffAccess ApplyStaffAccessUi handles Add/Edit/Delete visibility */

                try { MainClass.EnsureCustomerSchema(); } catch { }

                GetData();
            }
            catch (Exception ex)
            {
                ShowError("Form load failed.\n\n" + ex.Message);
            }
        }

        public void GetData()
        {
            try
            {
                string qry = @"
SELECT id, name, father_name, mobile, cnic, notes, created_at
FROM customers
WHERE (COALESCE(@q,'') = '' OR LOWER(name) LIKE '%'||LOWER(@q)||'%'
       OR LOWER(father_name) LIKE '%'||LOWER(@q)||'%'
       OR LOWER(mobile) LIKE '%'||LOWER(@q)||'%'
       OR LOWER(cnic) LIKE '%'||LOWER(@q)||'%')
ORDER BY datetime(created_at) DESC, id DESC;";

                var q = (txtSearch?.Text ?? string.Empty).Trim();
                var pars = new Dictionary<string, object> { { "@q", q } };

                var lb = new ListBox();
                lb.Items.Add(dgvid);          // id
                lb.Items.Add(dgvname);        // name
                lb.Items.Add(dgvFatherName);  // father_name
                lb.Items.Add(dgvMobile);      // mobile
                lb.Items.Add(dgvCNIC);        // cnic
                lb.Items.Add(dgvNote);        // notes
                lb.Items.Add(dgvDate);        // created_at

                MainClass.LoadData1(qry, guna2DataGridView1, lb, pars);
                guna2DataGridView1.ClearSelection();
            }
            catch (Exception ex)
            {
                ShowError("Fetching customers failed.\n\n" + ex.Message);
            }
        }

        public override void btnAdd_Click(object sender, EventArgs e)
        {
            // Close ke baad click-through se dobara Add na khule
            if (MainClass.UiClicksBlocked) return;
            try
            {
                using (var f = new frmCustomerAdd())
                {
                    MainClass.BlurBackground(f);
                }
                GetData();
            }
            catch (Exception ex)
            {
                ShowError("Opening add/edit dialog failed.\n\n" + ex.Message);
            }
        }

        public override void txtSearch_TextChanged(object sender, EventArgs e)
        {
            try
            {
                GetData();
            }
            catch (Exception ex)
            {
                ShowError("Search update failed.\n\n" + ex.Message);
            }
        }

        private void guna2DataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0 || guna2DataGridView1.CurrentCell == null) return;

                var colName = guna2DataGridView1.Columns[e.ColumnIndex].Name;

                if (colName == "dgvedit")
                {
                    // ADMIN-ONLY: same pattern as categories grid
                    if (StaffAccess.Deny(StaffAccess.ModuleForForm(this), "edit")) return;

                    if (!TryGetSelectedId(out int id))
                    {
                        ShowError("Invalid selection. ID missing.");
                        return;
                    }

                    if (MainClass.UiClicksBlocked) return;

                    using (var f = new frmCustomerAdd())
                    {
                        f.LoadForEdit(id);
                        MainClass.BlurBackground(f);
                    }
                    GetData();
                    return;
                }

                if (colName == "dgvdel")
                {
                    // ADMIN-ONLY: same pattern as categories grid
                    if (StaffAccess.Deny(StaffAccess.ModuleForForm(this), "delete")) return;

                    if (!TryGetSelectedId(out int id))
                    {
                        ShowError("Invalid selection. ID missing.");
                        return;
                    }

                    // keep your dependency guard
                    if (!CanDeleteCustomer(id, out string reason))
                    {
                        ShowError(reason);
                        return;
                    }

                    if (!AskYesNo("Are you sure you want to delete this customer?", "Delete Confirmation")) return;

                    try
                    {
                        int res = MainClass.ExecuteNonQuery(
                            "DELETE FROM customers WHERE id=@id;",
                            new Dictionary<string, object> { { "@id", id } });

                        if (res > 0)
                        {
                            ShowInfo("Customer deleted successfully.");
                            GetData();
                        }
                        else
                        {
                            ShowError("Delete failed ya record missing hai.");
                        }
                    }
                    catch (Exception exDel)
                    {
                        ShowError("Delete failed.\n\n" + exDel.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("Grid operation failed.\n\n" + ex.Message);
            }
        }

        /// <summary>
        /// Strict dependency check: block delete if customer exists in allocation/receipt/ledger etc.
        /// </summary>
        private bool CanDeleteCustomer(int customerId, out string reason)
        {
            reason = null;
            try
            {
                // Allocations (customer_plots)
                var allocCountObj = MainClass.ExecuteScalar(
                    "SELECT COUNT(*) FROM customer_plots WHERE customer_id=@cid;",
                    new Dictionary<string, object> { { "@cid", customerId } });
                long allocCount = ToLong(allocCountObj);

                // Receipts via allocations
                var recCountObj = MainClass.ExecuteScalar(
                    @"SELECT COUNT(*)
                      FROM receipts r
                      JOIN customer_plots cp ON cp.id = r.customer_plot_id
                      WHERE cp.customer_id=@cid;",
                    new Dictionary<string, object> { { "@cid", customerId } });
                long receiptCount = ToLong(recCountObj);

                // Ledger via allocations (if applicable in your schema)
                var ledCountObj = MainClass.ExecuteScalar(
                    @"SELECT COUNT(*)
                      FROM ledger l
                      JOIN customer_plots cp ON cp.id = l.customer_plot_id
                      WHERE cp.customer_id=@cid;",
                    new Dictionary<string, object> { { "@cid", customerId } });
                long ledgerCount = ToLong(ledCountObj);

                // Any other tables you want to enforce can be added similarly here.
                long totalRefs = allocCount + receiptCount + ledgerCount;

                if (totalRefs > 0)
                {
                    // Compose a precise message so the user knows where to clean first.
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("Customer delete allow nahi hai kyun ke linked records mojood hain:");
                    if (allocCount > 0) sb.AppendLine("- Allocations (customer_plots): " + allocCount);
                    if (receiptCount > 0) sb.AppendLine("- Receipts: " + receiptCount);
                    if (ledgerCount > 0) sb.AppendLine("- Ledger: " + ledgerCount);
                    sb.Append("Pehle in records ko delete/update karein phir customer delete hoga.");
                    reason = sb.ToString();
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                reason = "Dependency check failed.\n\n" + ex.Message;
                return false;
            }
        }

        // -------- Helpers --------
        private static long ToLong(object v)
        {
            if (v == null || v == DBNull.Value) return 0;
            try { return Convert.ToInt64(v); } catch { return 0; }
        }

        private bool TryGetSelectedId(out int id)
        {
            id = 0;
            try
            {
                var v = guna2DataGridView1.CurrentRow?.Cells["dgvid"]?.Value;
                if (v == null) return false;
                if (v is int i) { id = i; return true; }
                return int.TryParse(Convert.ToString(v), out id);
            }
            catch { id = 0; return false; }
        }

        private void ShowError(string message)
        {
            try { using (var err = new ErrorFormMessage(message, "Error")) err.ShowDialog(); }
            catch { MessageBox.Show(message, "Error"); }
        }

        private void ShowInfo(string message)
        {
            try { using (var ok = new CustomeMessage(message, "RealEstate Builders")) ok.ShowDialog(); }
            catch { MessageBox.Show(message, "Info"); }
        }

        private bool AskYesNo(string message, string title)
        {
            try { using (var ask = new YesOrNoMessage(message, title)) return ask.ShowDialog() == DialogResult.Yes; }
            catch { return MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes; }
        }
    }
}
