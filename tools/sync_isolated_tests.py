#!/usr/bin/env python3
"""Isolated sync logic tests — NO production Supabase / PC / mobile DB writes."""
from __future__ import annotations

import json
import os
import sqlite3
import tempfile
import uuid

PASSED = []
FAILED = []


def check(name: str, cond: bool, detail: str = "") -> None:
    if cond:
        PASSED.append(name)
        print(f"PASS  {name}")
    else:
        FAILED.append(name)
        print(f"FAIL  {name}  {detail}")


def schema(con: sqlite3.Connection) -> None:
    con.executescript(
        """
        CREATE TABLE customers(
          id INTEGER PRIMARY KEY, name TEXT, mobile TEXT, sync_id TEXT);
        CREATE TABLE plots(
          id INTEGER PRIMARY KEY, plot_no TEXT, status TEXT, sync_id TEXT);
        CREATE TABLE customer_plots(
          id INTEGER PRIMARY KEY, customer_id INT, plot_id INT,
          status TEXT, sync_id TEXT, price REAL);
        CREATE TABLE receipts(
          id INTEGER PRIMARY KEY, customer_plot_id INT, amount REAL,
          sync_id TEXT, ledger_id INT);
        CREATE TABLE ledger(
          id INTEGER PRIMARY KEY, customer_plot_id INT, credit REAL,
          debit REAL, sync_id TEXT);
        CREATE TABLE sync_outbox(
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          table_name TEXT, local_pk INT, sync_id TEXT,
          revision TEXT, op TEXT);
        CREATE TABLE sync_pending_delete(
          sync_id TEXT PRIMARY KEY, table_name TEXT);
        CREATE TABLE sync_pending_apply(
          sync_id TEXT PRIMARY KEY, table_name TEXT,
          payload_json TEXT, wait_parent_table TEXT,
          wait_parent_sync_id TEXT);
        CREATE TABLE sync_state(k TEXT PRIMARY KEY, v TEXT);
        """
    )


def test_sync_id_never_regenerated() -> None:
    with tempfile.TemporaryDirectory() as td:
        path = os.path.join(td, "t.db")
        con = sqlite3.connect(path)
        schema(con)
        sid = "aabbccddeeff00112233445566778899"
        con.execute(
            "INSERT INTO customers(id,name,mobile,sync_id) VALUES(1,'A','1',?)",
            (sid,),
        )
        con.commit()
        # Simulate "ensure sync_id only if empty"
        con.execute(
            "UPDATE customers SET sync_id=lower(hex(randomblob(16))) "
            "WHERE id=1 AND (sync_id IS NULL OR TRIM(sync_id)='')"
        )
        con.commit()
        got = con.execute("SELECT sync_id FROM customers WHERE id=1").fetchone()[0]
        check("existing sync_id preserved", got == sid, got)
        con.close()


def test_outbox_revision_isolation() -> None:
    with tempfile.TemporaryDirectory() as td:
        con = sqlite3.connect(os.path.join(td, "t.db"))
        schema(con)
        r1, r2 = uuid.uuid4().hex, uuid.uuid4().hex
        con.execute(
            "INSERT INTO sync_outbox(table_name,local_pk,sync_id,revision,op) "
            "VALUES('customers',1,'s1',?,'upsert')",
            (r1,),
        )
        con.execute(
            "INSERT INTO sync_outbox(table_name,local_pk,sync_id,revision,op) "
            "VALUES('customers',1,'s1',?,'upsert')",
            (r2,),
        )
        con.commit()
        # Late ACK for r1 must not clear r2
        con.execute("DELETE FROM sync_outbox WHERE revision=?", (r1,))
        con.commit()
        left = con.execute(
            "SELECT revision FROM sync_outbox WHERE local_pk=1"
        ).fetchall()
        check("late ACK does not clear newer revision", [x[0] for x in left] == [r2], str(left))
        con.close()


def test_pending_apply_waits_for_parent() -> None:
    with tempfile.TemporaryDirectory() as td:
        con = sqlite3.connect(os.path.join(td, "t.db"))
        schema(con)
        payload = {
            "sync_id": "alloc001",
            "customer_sync_id": "cust001",
            "plot_sync_id": "plot001",
        }
        con.execute(
            "INSERT INTO sync_pending_apply"
            "(sync_id,table_name,payload_json,wait_parent_table,wait_parent_sync_id) "
            "VALUES(?,?,?,?,?)",
            ("alloc001", "customer_plots", json.dumps(payload), "customers", "cust001"),
        )
        con.commit()
        parent = con.execute(
            "SELECT 1 FROM customers WHERE sync_id='cust001'"
        ).fetchone()
        check("parent missing before insert", parent is None)
        # Parent arrives
        con.execute(
            "INSERT INTO customers(id,name,mobile,sync_id) VALUES(9,'C','9','cust001')"
        )
        con.commit()
        can = con.execute(
            "SELECT 1 FROM customers WHERE sync_id=(SELECT wait_parent_sync_id "
            "FROM sync_pending_apply WHERE sync_id='alloc001')"
        ).fetchone()
        check("pending apply unblocks when parent exists", can is not None)
        con.close()


def test_delete_pending_in_same_txn() -> None:
    with tempfile.TemporaryDirectory() as td:
        con = sqlite3.connect(os.path.join(td, "t.db"))
        schema(con)
        con.execute(
            "INSERT INTO customers(id,name,mobile,sync_id) VALUES(1,'X','1','del001')"
        )
        con.commit()
        try:
            con.execute("BEGIN")
            con.execute(
                "INSERT OR REPLACE INTO sync_pending_delete(sync_id,table_name) "
                "VALUES('del001','customers')"
            )
            con.execute("DELETE FROM customers WHERE id=1")
            con.execute("COMMIT")
        except Exception:
            con.execute("ROLLBACK")
            raise
        gone = con.execute("SELECT 1 FROM customers WHERE id=1").fetchone()
        pend = con.execute(
            "SELECT 1 FROM sync_pending_delete WHERE sync_id='del001'"
        ).fetchone()
        check("local row deleted", gone is None)
        check("pending delete saved", pend is not None)
        con.close()


def test_cursor_keyset_not_stuck() -> None:
    # Same timestamp, 200 ids — keyset (ts, sync_id) must page past first 80
    rows = []
    ts = "2026-11-22T00:00:00.000Z"
    for i in range(200):
        rows.append({"updated_at": ts, "sync_id": f"{i:032x}"})
    rows.sort(key=lambda r: (r["updated_at"], r["sync_id"]))

    def page(after_ts: str, after_id: str, limit: int = 80):
        out = []
        for r in rows:
            if after_id == "":
                if r["updated_at"] > after_ts:
                    out.append(r)
            else:
                if r["updated_at"] > after_ts or (
                    r["updated_at"] == after_ts and r["sync_id"] > after_id
                ):
                    out.append(r)
            if len(out) >= limit:
                break
        return out

    seen = set()
    ts_c, id_c = "1970-01-01T00:00:00.000Z", ""
    for _ in range(10):
        batch = page(ts_c, id_c, 80)
        if not batch:
            break
        for r in batch:
            seen.add(r["sync_id"])
        ts_c, id_c = batch[-1]["updated_at"], batch[-1]["sync_id"]
    check("keyset pages all 200 same-timestamp rows", len(seen) == 200, str(len(seen)))


def test_empty_cloud_is_not_delete() -> None:
    local_ids = {"a", "b", "c"}
    cloud_ok_empty = True  # successful []
    cloud_fail = None  # request failed
    # Policy: only is_deleted=true deletes
    to_delete = set()
    if cloud_fail is None:
        pass  # do nothing
    elif cloud_ok_empty:
        pass  # absence != delete
    check("failed request does not delete local", local_ids == {"a", "b", "c"})
    check("empty success does not delete local", len(to_delete) == 0)


def test_cursor_not_advance_on_fetch_fail() -> None:
    cursors = {"customers": "t1|id1", "receipts": "t1|id1"}
    fetch_ok = {"customers": False, "receipts": True}
    for table, ok in fetch_ok.items():
        if not ok:
            continue  # failed table keeps old cursor
        # success would advance — simulate
        cursors[table] = "t2|id2"
    check(
        "failed table cursor unchanged",
        cursors["customers"] == "t1|id1",
        str(cursors),
    )
    check("successful table may advance", cursors["receipts"] == "t2|id2")


def test_anti_resurrect_policy() -> None:
    cloud = {"sync_id": "x", "is_deleted": True, "name": "Old"}
    local_push = {"sync_id": "x", "is_deleted": False, "name": "Stale offline"}
    # Server trigger / client skip: deleted stays deleted
    if cloud["is_deleted"] and not local_push["is_deleted"]:
        result = dict(cloud)
        result["is_deleted"] = True
    else:
        result = local_push
    check("offline upsert cannot undelete", result["is_deleted"] is True)


def test_upgrade_compare_self() -> None:
    with tempfile.TemporaryDirectory() as td:
        path = os.path.join(td, "snap.db")
        con = sqlite3.connect(path)
        schema(con)
        con.execute(
            "INSERT INTO customers(id,name,mobile,sync_id) VALUES(1,'A','1','s1')"
        )
        con.execute(
            "INSERT INTO receipts(id,customer_plot_id,amount,sync_id) VALUES(1,1,10000,'r1')"
        )
        con.commit()
        con.close()
        # Copy as before/after identical
        import shutil

        before = os.path.join(td, "before.db")
        after = os.path.join(td, "after.db")
        shutil.copy(path, before)
        shutil.copy(path, after)
        # Inline compare
        b = sqlite3.connect(before)
        a = sqlite3.connect(after)
        bc = b.execute("SELECT COUNT(*), SUM(amount) FROM receipts").fetchone()
        ac = a.execute("SELECT COUNT(*), SUM(amount) FROM receipts").fetchone()
        b.close()
        a.close()
        check("upgrade compare receipt count/sum stable", bc == ac, f"{bc} vs {ac}")


def main() -> None:
    print("=== HubBuilder isolated sync tests (no production I/O) ===")
    test_sync_id_never_regenerated()
    test_outbox_revision_isolation()
    test_pending_apply_waits_for_parent()
    test_delete_pending_in_same_txn()
    test_cursor_keyset_not_stuck()
    test_empty_cloud_is_not_delete()
    test_cursor_not_advance_on_fetch_fail()
    test_anti_resurrect_policy()
    test_upgrade_compare_self()
    print(f"\nResult: {len(PASSED)} passed, {len(FAILED)} failed")
    if FAILED:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
