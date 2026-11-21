#!/usr/bin/env python3
"""Read-only ambiguous sync_id report. Never merge/delete. Optional local DB path."""
from __future__ import annotations

import argparse
import os
import sqlite3
import sys


def report(db_path: str) -> int:
    if not os.path.isfile(db_path):
        print(f"SKIP  DB not found (no writes): {db_path}")
        return 0
    con = sqlite3.connect(f"file:{db_path}?mode=ro", uri=True)
    issues = []
    tables = [
        ("customers", ["name", "mobile"]),
        ("customer_plots", ["customer_id", "plot_id"]),
        ("receipts", ["customer_plot_id", "amount", "receipt_date"]),
        ("plots", ["plot_no"]),
    ]
    for table, cols in tables:
        try:
            con.execute(f'SELECT 1 FROM "{table}" LIMIT 1')
        except sqlite3.Error:
            continue
        # Missing sync_id
        miss = con.execute(
            f'SELECT COUNT(*) FROM "{table}" WHERE sync_id IS NULL OR TRIM(sync_id)=""'
        ).fetchone()[0]
        if miss:
            issues.append(f"{table}: {miss} rows missing sync_id (do NOT auto-merge)")
        # Duplicate sync_id
        dups = con.execute(
            f'''SELECT sync_id, COUNT(*) c FROM "{table}"
                WHERE sync_id IS NOT NULL AND TRIM(sync_id)<>""
                GROUP BY sync_id HAVING c>1'''
        ).fetchall()
        for sid, c in dups:
            issues.append(f"{table}: sync_id={sid} appears {c} times")
        # Same business key, different sync_ids (ambiguous)
        if table == "customers":
            rows = con.execute(
                '''SELECT lower(trim(ifnull(name,''))), lower(trim(ifnull(mobile,''))),
                          GROUP_CONCAT(DISTINCT sync_id), COUNT(DISTINCT sync_id)
                   FROM customers
                   WHERE sync_id IS NOT NULL AND TRIM(sync_id)<>''
                   GROUP BY 1,2 HAVING COUNT(DISTINCT sync_id)>1'''
            ).fetchall()
            for name, mob, sids, n in rows:
                issues.append(
                    f"customers ambiguous name/mobile '{name}'/'{mob}': {n} sync_ids → {sids}"
                )
    con.close()
    print(f"=== Ambiguous sync report (read-only): {db_path} ===")
    if not issues:
        print("PASS  no ambiguous cases found")
        return 0
    for i in issues:
        print(f"REPORT  {i}")
    print(f"Total reports: {len(issues)} (manual review only — no auto merge/delete)")
    return 0


def main() -> None:
    p = argparse.ArgumentParser()
    p.add_argument(
        "db",
        nargs="?",
        default="",
        help="Optional local SQLite path (read-only). Empty = skip.",
    )
    args = p.parse_args()
    if not args.db:
        print("SKIP  no DB path — pass a copy for upgrade compare")
        sys.exit(0)
    sys.exit(report(args.db))


if __name__ == "__main__":
    main()
