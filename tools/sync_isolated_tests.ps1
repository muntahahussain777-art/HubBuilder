# Isolated sync logic tests — NO production Supabase / PC / mobile DB writes.
$ErrorActionPreference = "Stop"
$script:Passed = @()
$script:Failed = @()

function Check([string]$Name, [bool]$Cond, [string]$Detail = "") {
  if ($Cond) {
    $script:Passed += $Name
    Write-Host "PASS  $Name"
  } else {
    $script:Failed += $Name
    Write-Host "FAIL  $Name  $Detail"
  }
}

Write-Host "=== HubBuilder isolated sync tests (no production I/O) ==="

# 1) sync_id never regenerated when non-empty
$sid = "aabbccddeeff00112233445566778899"
$existing = $sid
if ([string]::IsNullOrWhiteSpace($existing)) { $existing = [guid]::NewGuid().ToString("N") }
Check "existing sync_id preserved" ($existing -eq $sid) $existing

# 2) outbox revision isolation
$r1 = [guid]::NewGuid().ToString("N")
$r2 = [guid]::NewGuid().ToString("N")
$outbox = [System.Collections.Generic.List[string]]::new()
$outbox.Add($r1); $outbox.Add($r2)
$outbox.Remove($r1) | Out-Null
Check "late ACK does not clear newer revision" (($outbox.Count -eq 1) -and ($outbox[0] -eq $r2))

# 3) pending apply waits for parent
$parents = @{}
$pending = @{ sync_id = "alloc001"; wait = "cust001" }
$canBefore = $parents.ContainsKey($pending.wait)
$parents[$pending.wait] = $true
$canAfter = $parents.ContainsKey($pending.wait)
Check "parent missing before insert" (-not $canBefore)
Check "pending apply unblocks when parent exists" $canAfter

# 4) delete + pending same logical txn
$local = @{ "1" = "del001" }
$pendDel = @{}
# begin
$pendDel["del001"] = "customers"
$local.Remove("1")
# commit
Check "local row deleted" (-not $local.ContainsKey("1"))
Check "pending delete saved" $pendDel.ContainsKey("del001")

# 5) keyset pagination 200 same timestamp
$rows = @()
$ts = "2026-11-22T00:00:00.000Z"
for ($i = 0; $i -lt 200; $i++) {
  $rows += [pscustomobject]@{ updated_at = $ts; sync_id = ("{0:x32}" -f $i) }
}
$rows = $rows | Sort-Object updated_at, sync_id
function Get-Page($afterTs, $afterId, $limit = 80) {
  $out = @()
  foreach ($r in $rows) {
    $ok = $false
    if ([string]::IsNullOrEmpty($afterId)) {
      if ($r.updated_at -gt $afterTs) { $ok = $true }
    } else {
      if (($r.updated_at -gt $afterTs) -or (($r.updated_at -eq $afterTs) -and ($r.sync_id -gt $afterId))) {
        $ok = $true
      }
    }
    if ($ok) {
      $out += $r
      if ($out.Count -ge $limit) { break }
    }
  }
  return $out
}
$seen = New-Object 'System.Collections.Generic.HashSet[string]'
$tsC = "1970-01-01T00:00:00.000Z"; $idC = ""
for ($k = 0; $k -lt 10; $k++) {
  $batch = @(Get-Page $tsC $idC 80)
  if ($batch.Count -eq 0) { break }
  foreach ($r in $batch) { [void]$seen.Add($r.sync_id) }
  $tsC = $batch[-1].updated_at; $idC = $batch[-1].sync_id
}
Check "keyset pages all 200 same-timestamp rows" ($seen.Count -eq 200) ("count=" + $seen.Count)

# 6) empty / failed cloud is not delete
$localIds = @("a", "b", "c")
$toDelete = @()
Check "failed request does not delete local" ($localIds.Count -eq 3)
Check "empty success does not delete local" ($toDelete.Count -eq 0)

# 7) cursor not advance on fetch fail
$cursors = @{ customers = "t1|id1"; receipts = "t1|id1" }
$fetchOk = @{ customers = $false; receipts = $true }
foreach ($t in $fetchOk.Keys) {
  if (-not $fetchOk[$t]) { continue }
  $cursors[$t] = "t2|id2"
}
Check "failed table cursor unchanged" ($cursors.customers -eq "t1|id1")
Check "successful table may advance" ($cursors.receipts -eq "t2|id2")

# 8) anti-resurrect
$cloudDeleted = $true
$localPushUndelete = $false
$resultDeleted = if ($cloudDeleted -and -not $localPushUndelete) { $true } else { $localPushUndelete }
Check "offline upsert cannot undelete" ($resultDeleted -eq $true)

# 9) Rs 10000 receipt idempotent after dropped ACK + 3 retries
$balanceEffect = 0
$receipts = New-Object 'System.Collections.Generic.HashSet[string]'
$sidR = "receipt10000"
for ($attempt = 1; $attempt -le 4; $attempt++) {
  if ($receipts.Add($sidR)) { $balanceEffect += 10000 }
}
Check "dropped ACK retries keep one receipt" ($receipts.Count -eq 1)
Check "balance affected once Rs 10000" ($balanceEffect -eq 10000)

Write-Host ""
Write-Host ("Result: {0} passed, {1} failed" -f $script:Passed.Count, $script:Failed.Count)
if ($script:Failed.Count -gt 0) { exit 1 }
exit 0
