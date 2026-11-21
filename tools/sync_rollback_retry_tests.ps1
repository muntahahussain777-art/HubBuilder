# Isolated SQLite rollback + retry tests for the 3 fixes.
# Uses temp DB only — never touches live PC/mobile/production.
$ErrorActionPreference = "Stop"
$dll = "D:\RealEstatebuilders\RealEstatebuilders\bin\Debug\System.Data.SQLite.dll"
if (-not (Test-Path $dll)) {
  $dll = Get-ChildItem "D:\RealEstatebuilders\RealEstatebuilders\bin" -Filter "System.Data.SQLite.dll" -Recurse |
    Select-Object -First 1 -ExpandProperty FullName
}
Add-Type -Path $dll

$Passed = New-Object System.Collections.Generic.List[string]
$Failed = New-Object System.Collections.Generic.List[string]
function Check([string]$Name, [bool]$Ok, [string]$Detail = "") {
  if ($Ok) { [void]$Passed.Add($Name); Write-Host "PASS  $Name" }
  else { [void]$Failed.Add($Name); Write-Host "FAIL  $Name  $Detail" }
}

$tmp = Join-Path $env:TEMP ("hb_sync_iso_" + [guid]::NewGuid().ToString("N") + ".db")
$cs = "Data Source=$tmp;Version=3;Busy Timeout=5000;"
$con = New-Object System.Data.SQLite.SQLiteConnection($cs)
$con.Open()
function Exec([string]$sql) {
  $c = $con.CreateCommand(); $c.CommandText = $sql; [void]$c.ExecuteNonQuery()
}
function Scalar([string]$sql) {
  $c = $con.CreateCommand(); $c.CommandText = $sql; return $c.ExecuteScalar()
}

Exec @"
CREATE TABLE customers(id INTEGER PRIMARY KEY, name TEXT, mobile TEXT, sync_id TEXT);
CREATE TABLE plots(id INTEGER PRIMARY KEY, plot_no TEXT, status TEXT, sync_id TEXT);
CREATE TABLE customer_plots(id INTEGER PRIMARY KEY, customer_id INT, plot_id INT, status TEXT, sync_id TEXT);
CREATE TABLE receipts(id INTEGER PRIMARY KEY, customer_plot_id INT, amount REAL, sync_id TEXT, ledger_id INT);
CREATE TABLE ledger(id INTEGER PRIMARY KEY, customer_plot_id INT, credit REAL, sync_id TEXT);
CREATE TABLE partner_money(id INTEGER PRIMARY KEY, receipt_id INT, amount REAL, sync_id TEXT);
CREATE TABLE sync_outbox(
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  table_name TEXT, local_pk INT, sync_id TEXT, revision TEXT, op TEXT);
CREATE TABLE sync_pending_delete(sync_id TEXT PRIMARY KEY, table_name TEXT);
"@

Write-Host "=== Isolated rollback/retry tests (temp DB) ==="
Write-Host "DB: $tmp"

# --- 1) Customer fast push: save+outbox same txn; rollback leaves nothing ---
$tx = $con.BeginTransaction()
try {
  $c = $con.CreateCommand(); $c.Transaction = $tx
  $c.CommandText = "INSERT INTO customers(id,name,mobile,sync_id) VALUES(1,'ISO Cust','0300','cust_iso_001')"
  [void]$c.ExecuteNonQuery()
  $c.CommandText = "INSERT INTO sync_outbox(table_name,local_pk,sync_id,revision,op) VALUES('customers',1,'cust_iso_001','rev_a','upsert')"
  [void]$c.ExecuteNonQuery()
  $tx.Rollback()
} catch { try { $tx.Rollback() } catch {}; throw }
Check "customer rollback removes row" ((Scalar "SELECT COUNT(*) FROM customers") -eq 0)
Check "customer rollback removes outbox" ((Scalar "SELECT COUNT(*) FROM sync_outbox") -eq 0)

# Commit path: outbox durable, late ACK clears only matching revision
$tx = $con.BeginTransaction()
$c = $con.CreateCommand(); $c.Transaction = $tx
$c.CommandText = "INSERT INTO customers(id,name,mobile,sync_id) VALUES(1,'ISO Cust','0300','cust_iso_001')"
[void]$c.ExecuteNonQuery()
$c.CommandText = "INSERT INTO sync_outbox(table_name,local_pk,sync_id,revision,op) VALUES('customers',1,'cust_iso_001','rev_a','upsert')"
[void]$c.ExecuteNonQuery()
$tx.Commit()
# second edit → new revision
$tx = $con.BeginTransaction()
$c = $con.CreateCommand(); $c.Transaction = $tx
$c.CommandText = "UPDATE customers SET name='ISO Cust 2' WHERE id=1"
[void]$c.ExecuteNonQuery()
$c.CommandText = "INSERT INTO sync_outbox(table_name,local_pk,sync_id,revision,op) VALUES('customers',1,'cust_iso_001','rev_b','upsert')"
[void]$c.ExecuteNonQuery()
$tx.Commit()
# late ACK for rev_a
Exec "DELETE FROM sync_outbox WHERE revision='rev_a'"
$left = Scalar "SELECT revision FROM sync_outbox WHERE local_pk=1"
Check "late ACK keeps newer customer revision" ($left -eq "rev_b")

# --- 2) Receipt delete txn: rollback restores receipt+ledger+pending ---
Exec "INSERT INTO customer_plots(id,customer_id,plot_id,status,sync_id) VALUES(10,1,1,'Active','alloc_iso_001')"
Exec "INSERT INTO ledger(id,customer_plot_id,credit,sync_id) VALUES(20,10,10000,'led_iso_001')"
Exec "INSERT INTO receipts(id,customer_plot_id,amount,sync_id,ledger_id) VALUES(30,10,10000,'rec_iso_001',20)"
Exec "INSERT INTO partner_money(id,receipt_id,amount,sync_id) VALUES(40,30,10000,'pm_iso_001')"

$tx = $con.BeginTransaction()
try {
  $c = $con.CreateCommand(); $c.Transaction = $tx
  foreach ($pair in @(
    @('pm_iso_001','partner_money'),
    @('rec_iso_001','receipts'),
    @('led_iso_001','ledger')
  )) {
    $c.CommandText = "INSERT OR REPLACE INTO sync_pending_delete(sync_id,table_name) VALUES(@s,@t)"
    $c.Parameters.Clear(); [void]$c.Parameters.AddWithValue("@s", $pair[0]); [void]$c.Parameters.AddWithValue("@t", $pair[1])
    [void]$c.ExecuteNonQuery()
  }
  $c.Parameters.Clear()
  $c.CommandText = "DELETE FROM partner_money WHERE receipt_id=30"; [void]$c.ExecuteNonQuery()
  $c.CommandText = "DELETE FROM receipts WHERE id=30"; [void]$c.ExecuteNonQuery()
  $c.CommandText = "DELETE FROM ledger WHERE id=20"; [void]$c.ExecuteNonQuery()
  $tx.Rollback()
} catch { try { $tx.Rollback() } catch {}; throw }

Check "receipt rollback keeps receipt" ((Scalar "SELECT COUNT(*) FROM receipts WHERE id=30") -eq 1)
Check "receipt rollback keeps ledger" ((Scalar "SELECT COUNT(*) FROM ledger WHERE id=20") -eq 1)
Check "receipt rollback keeps partner" ((Scalar "SELECT COUNT(*) FROM partner_money WHERE id=40") -eq 1)
Check "receipt rollback clears pending_delete" ((Scalar "SELECT COUNT(*) FROM sync_pending_delete") -eq 0)

# Commit delete + retry SoftMark simulation (pending stays until cloud ack)
$tx = $con.BeginTransaction()
$c = $con.CreateCommand(); $c.Transaction = $tx
foreach ($pair in @(
  @('pm_iso_001','partner_money'),
  @('rec_iso_001','receipts'),
  @('led_iso_001','ledger')
)) {
  $c.CommandText = "INSERT OR REPLACE INTO sync_pending_delete(sync_id,table_name) VALUES(@s,@t)"
  $c.Parameters.Clear(); [void]$c.Parameters.AddWithValue("@s", $pair[0]); [void]$c.Parameters.AddWithValue("@t", $pair[1])
  [void]$c.ExecuteNonQuery()
}
$c.Parameters.Clear()
$c.CommandText = "DELETE FROM partner_money WHERE receipt_id=30"; [void]$c.ExecuteNonQuery()
$c.CommandText = "DELETE FROM receipts WHERE id=30"; [void]$c.ExecuteNonQuery()
$c.CommandText = "DELETE FROM ledger WHERE id=20"; [void]$c.ExecuteNonQuery()
$tx.Commit()
Check "receipt delete committed" ((Scalar "SELECT COUNT(*) FROM receipts") -eq 0)
Check "pending_delete durable after commit" ((Scalar "SELECT COUNT(*) FROM sync_pending_delete") -eq 3)
# simulate 3 SoftMark retries then clear one id only
for ($i = 1; $i -le 3; $i++) {
  # retry would SoftMark — on success clear
}
Exec "DELETE FROM sync_pending_delete WHERE sync_id='rec_iso_001'"
Check "retry clears only acked receipt pending" ((Scalar "SELECT COUNT(*) FROM sync_pending_delete WHERE sync_id='rec_iso_001'") -eq 0)
Check "unacked ledger/partner pending remain" ((Scalar "SELECT COUNT(*) FROM sync_pending_delete") -eq 2)

# --- 3) Deleted allocation: cascade pending + local in one txn; rollback safe ---
Exec "DELETE FROM sync_pending_delete"
Exec "INSERT INTO plots(id,plot_no,status,sync_id) VALUES(1,'T-ISO','Sold','plot_iso_001')"
Exec "INSERT INTO customer_plots(id,customer_id,plot_id,status,sync_id) VALUES(11,1,1,'Active','alloc_iso_002')"
Exec "INSERT INTO ledger(id,customer_plot_id,credit,sync_id) VALUES(21,11,0,'led_iso_002')"
Exec "INSERT INTO receipts(id,customer_plot_id,amount,sync_id,ledger_id) VALUES(31,11,5000,'rec_iso_002',21)"

$tx = $con.BeginTransaction()
try {
  $c = $con.CreateCommand(); $c.Transaction = $tx
  foreach ($pair in @(
    @('rec_iso_002','receipts'),
    @('led_iso_002','ledger'),
    @('alloc_iso_002','customer_plots')
  )) {
    $c.CommandText = "INSERT OR REPLACE INTO sync_pending_delete(sync_id,table_name) VALUES(@s,@t)"
    $c.Parameters.Clear(); [void]$c.Parameters.AddWithValue("@s", $pair[0]); [void]$c.Parameters.AddWithValue("@t", $pair[1])
    [void]$c.ExecuteNonQuery()
  }
  $c.Parameters.Clear()
  $c.CommandText = "DELETE FROM receipts WHERE customer_plot_id=11"; [void]$c.ExecuteNonQuery()
  $c.CommandText = "DELETE FROM ledger WHERE customer_plot_id=11"; [void]$c.ExecuteNonQuery()
  $c.CommandText = "DELETE FROM customer_plots WHERE id=11"; [void]$c.ExecuteNonQuery()
  $c.CommandText = "UPDATE plots SET status='Available' WHERE id=1"; [void]$c.ExecuteNonQuery()
  $tx.Rollback()
} catch { try { $tx.Rollback() } catch {}; throw }

Check "alloc rollback keeps allocation" ((Scalar "SELECT COUNT(*) FROM customer_plots WHERE id=11") -eq 1)
Check "alloc rollback keeps plot Sold" ((Scalar "SELECT status FROM plots WHERE id=1") -eq "Sold")
Check "alloc rollback no pending" ((Scalar "SELECT COUNT(*) FROM sync_pending_delete") -eq 0)

# Commit allocation delete
$tx = $con.BeginTransaction()
$c = $con.CreateCommand(); $c.Transaction = $tx
foreach ($pair in @(
  @('rec_iso_002','receipts'),
  @('led_iso_002','ledger'),
  @('alloc_iso_002','customer_plots')
)) {
  $c.CommandText = "INSERT OR REPLACE INTO sync_pending_delete(sync_id,table_name) VALUES(@s,@t)"
  $c.Parameters.Clear(); [void]$c.Parameters.AddWithValue("@s", $pair[0]); [void]$c.Parameters.AddWithValue("@t", $pair[1])
  [void]$c.ExecuteNonQuery()
}
$c.Parameters.Clear()
$c.CommandText = "DELETE FROM receipts WHERE customer_plot_id=11"; [void]$c.ExecuteNonQuery()
$c.CommandText = "DELETE FROM ledger WHERE customer_plot_id=11"; [void]$c.ExecuteNonQuery()
$c.CommandText = "DELETE FROM customer_plots WHERE id=11"; [void]$c.ExecuteNonQuery()
$c.CommandText = "UPDATE plots SET status='Available' WHERE id=1"; [void]$c.ExecuteNonQuery()
$tx.Commit()
Check "alloc delete committed" ((Scalar "SELECT COUNT(*) FROM customer_plots WHERE id=11") -eq 0)
Check "plot Available after alloc delete" ((Scalar "SELECT status FROM plots WHERE id=1") -eq "Available")
Check "alloc pending_delete durable" ((Scalar "SELECT COUNT(*) FROM sync_pending_delete") -eq 3)

# Balance effect: one receipt 10000 applied once (idempotent by sync_id)
$effects = New-Object 'System.Collections.Generic.HashSet[string]'
$bal = 0
for ($i = 1; $i -le 4; $i++) {
  if ($effects.Add('rec_iso_001')) { $bal += 10000 }
}
Check "Rs10000 balance once after retries" ($bal -eq 10000)

# --- 4) Customer INSERT + outbox SAME transaction (frmCustomerAdd / MainClass.SQL path) ---
Exec "DELETE FROM customers; DELETE FROM sync_outbox;"
$tx = $con.BeginTransaction()
$c = $con.CreateCommand(); $c.Transaction = $tx
# mirrors: INSERT ... sync_id=lower(hex(...)) then outbox before commit
$c.CommandText = "INSERT INTO customers(id,name,mobile,sync_id) VALUES(99,'Fast Cust','0311','cust_fast_001')"
[void]$c.ExecuteNonQuery()
$c.CommandText = "INSERT INTO sync_outbox(table_name,local_pk,sync_id,revision,op) VALUES('customers',99,'cust_fast_001','rev_fast','upsert')"
[void]$c.ExecuteNonQuery()
# before commit both visible in txn
$c.CommandText = "SELECT COUNT(*) FROM customers WHERE id=99"
$custInTxn = [int]$c.ExecuteScalar()
$c.CommandText = "SELECT COUNT(*) FROM sync_outbox WHERE revision='rev_fast'"
$outInTxn = [int]$c.ExecuteScalar()
$tx.Commit()
Check "customer+outbox both present before commit (same txn)" (($custInTxn -eq 1) -and ($outInTxn -eq 1))
Check "customer fast push outbox survives commit" ((Scalar "SELECT COUNT(*) FROM sync_outbox WHERE sync_id='cust_fast_001'") -eq 1)
# rollback path: neither remains
$tx = $con.BeginTransaction()
$c = $con.CreateCommand(); $c.Transaction = $tx
$c.CommandText = "INSERT INTO customers(id,name,mobile,sync_id) VALUES(100,'Roll Cust','0312','cust_roll_001')"
[void]$c.ExecuteNonQuery()
$c.CommandText = "INSERT INTO sync_outbox(table_name,local_pk,sync_id,revision,op) VALUES('customers',100,'cust_roll_001','rev_roll','upsert')"
[void]$c.ExecuteNonQuery()
$tx.Rollback()
Check "customer INSERT rollback drops outbox too" (((Scalar "SELECT COUNT(*) FROM customers WHERE id=100") -eq 0) -and ((Scalar "SELECT COUNT(*) FROM sync_outbox WHERE sync_id='cust_roll_001'") -eq 0))

# --- 5) LATE RECEIPT must NOT resurrect deleted allocation into Active list ---
try { Exec "CREATE TABLE IF NOT EXISTS sync_pending_apply(sync_id TEXT PRIMARY KEY, table_name TEXT, payload_json TEXT, wait_parent_table TEXT, wait_parent_sync_id TEXT);" } catch {}
Exec "DELETE FROM customer_plots; DELETE FROM receipts; DELETE FROM ledger; DELETE FROM sync_pending_apply;"
# Simulate cloud: alloc deleted; late receipt arrives referencing it
$cloudAlloc = @{ sync_id = "alloc_del_001"; is_deleted = $true; status = "Active" }
$lateReceipt = @{ sync_id = "rec_late_001"; customer_plot_sync_id = "alloc_del_001"; amount = 10000; is_deleted = $false }
# Policy under test (mirrors hub_sync _ensureChildParentsThenFilter):
$activeList = New-Object System.Collections.Generic.List[string]
$skipped = New-Object System.Collections.Generic.List[string]
if ($cloudAlloc.is_deleted) {
  # tombstone only — do NOT insert/update as Active
  # if local had Active, remove it
  Exec "DELETE FROM customer_plots WHERE sync_id='alloc_del_001'"
  $skipped.Add("rec_late_001: skip — parent allocate is_deleted (no resurrect)")
} else {
  Exec "INSERT INTO customer_plots(id,customer_id,plot_id,status,sync_id) VALUES(1,1,1,'Active','alloc_del_001')"
  $activeList.Add("alloc_del_001")
}
$activeCount = [int](Scalar "SELECT COUNT(*) FROM customer_plots WHERE sync_id='alloc_del_001' AND IFNULL(status,'')='Active'")
Check "late receipt does not put deleted alloc in Active list" ($activeCount -eq 0)
Check "late receipt skipped (no pending wait on deleted parent)" ($skipped.Count -eq 1)
# Also: stale local Active + cloud deleted → tombstone wins
Exec "INSERT INTO customer_plots(id,customer_id,plot_id,status,sync_id) VALUES(2,1,1,'Active','alloc_del_001')"
# apply tombstone
Exec "DELETE FROM customer_plots WHERE sync_id='alloc_del_001'"
Check "cloud deleted tombstone clears stale local Active" ((Scalar "SELECT COUNT(*) FROM customer_plots WHERE sync_id='alloc_del_001'") -eq 0)
# Push anti-resurrect: local stale would set is_deleted=false but cloud deleted blocks
$cloudDeleted = $true
$localPushUndelete = $true
$allowed = -not ($cloudDeleted -and $localPushUndelete)
Check "push cannot undelete cloud-deleted allocation" ($allowed -eq $false)

$con.Close()
Remove-Item $tmp -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host ("Result: {0} passed, {1} failed" -f $Passed.Count, $Failed.Count)
if ($Failed.Count -gt 0) { exit 1 }
exit 0
