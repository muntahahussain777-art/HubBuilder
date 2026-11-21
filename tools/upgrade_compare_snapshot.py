#!/usr/bin/env python3
"""Compare before/after upgrade snapshots (read-only). No production writes."""
from __future__ import annotations

import argparse
import json
import os
import sqlite3
import sys


def snapshot(db_path: str) -> dict:
    con = sqlite3.connect(f"file:{db_path}?mode=ro", uri=True)
    out = {"path": db_path, "tables": {}, "balances": {}}
    for t in ("customers", "plots", "customer_plots", "receipts", "ledger"):
        try:
            n = con.execute(f'SELECT COUNT(*) FROM "{t}"').fetchone()[0]
            ids = [
                r[0]
                for r in con.execute(
                    f'''SELECT sync_id FROM "{t}"
                        WHERE sync_id IS NOT NULL AND TRIM(sync_id)<>''
                        ORDER BY sync_id'''
                ).fetchall()
            ]
            out["tables"][t] = {"count": n, "sync_ids": ids}
        except sqlite3.Error:
            out["tables"][t] = {"count": -1, "sync_ids": []}
    try:
        links = con.execute(
            '''SELECT cp.sync_id, c.sync_id, p.sync_id
               FROM customer_plots cp
               LEFT JOIN customers c ON c.id=cp.customer_id
               LEFT JOIN plots p ON p.id=cp.plot_id
               ORDER BY cp.sync_id'''
        ).fetchall()
        out["links"] = [{"cp": a, "c": b, "p": c} for a, b, c in links]
    except sqlite3.Error:
        out["links"] = []
    try:
        bals = con.execute(
            '''SELECT customer_plot_id,
                      COALESCE(SUM(CASE WHEN debit IS NOT NULL THEN debit ELSE 0 END),0),
                      COALESCE(SUM(CASE WHEN credit IS NOT NULL THEN credit ELSE 0 END),0)
               FROM ledger GROUP BY customer_plot_id ORDER BY 1'''
        ).fetchall()
        out["balances"] = {
            str(cp): {"debit": float(d), "credit": float(c)} for cp, d, c in bals
        }
    except sqlite3.Error:
        pass
    try:
        rt = con.execute("SELECT COALESCE(SUM(amount),0) FROM receipts").fetchone()[0]
        out["receipt_total"] = float(rt or 0)
    except sqlite3.Error:
        out["receipt_total"] = None
    con.close()
    return out


def compare(before: dict, after: dict) -> list[str]:
    fails = []
    for t, b in before.get("tables", {}).items():
        a = after.get("tables", {}).get(t, {})
        if b.get("count") != a.get("count"):
            fails.append(f"{t} count {b.get('count')} → {a.get('count')}")
        if set(b.get("sync_ids") or []) != set(a.get("sync_ids") or []):
            fails.append(f"{t} sync_id set changed")
    if before.get("receipt_total") != after.get("receipt_total"):
        fails.append(
            f"receipt_total {before.get('receipt_total')} → {after.get('receipt_total')}"
        )
    if before.get("balances") != after.get("balances"):
        fails.append("per-allocation ledger balances changed")
    if before.get("links") != after.get("links"):
        fails.append("customer–plot sync_id links changed")
    return fails


def main() -> None:
    p = argparse.ArgumentParser()
    p.add_argument("--before", required=True)
    p.add_argument("--after", required=True)
    p.add_argument("--write-json", default="")
    args = p.parse_args()
    for path in (args.before, args.after):
        if not os.path.isfile(path):
            print(f"FAIL  missing snapshot DB: {path}")
            sys.exit(1)
    b, a = snapshot(args.before), snapshot(args.after)
    if args.write_json:
        with open(args.write_json, "w", encoding="utf-8") as f:
            json.dump({"before": b, "after": a}, f, indent=2)
    fails = compare(b, a)
    print("=== Upgrade compare (isolated copies) ===")
    for t, info in b["tables"].items():
        print(f"  {t}: before={info['count']} after={a['tables'].get(t,{}).get('count')}")
    print(f"  receipt_total: {b.get('receipt_total')} → {a.get('receipt_total')}")
    if fails:
        for f in fails:
            print(f"FAIL  {f}")
        sys.exit(1)
    print("PASS  IDs, counts, links, receipt totals, balances match")
    sys.exit(0)


if __name__ == "__main__":
    main()
