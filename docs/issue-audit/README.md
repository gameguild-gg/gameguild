# GameGuild issue closeout inventory

Snapshot date: 2026-09-28 (GitHub API).

This initial source inventory contains **328 unique issues** authored by or assigned to `mathrmartins` in `gameguild-gg/gameguild`: **188 open** and **140 closed**. Pull requests are excluded. The scope is the union of GitHub's `creator=mathrmartins` and `assignee=mathrmartins` issue filters, deduplicated by issue number.

`gameguild-issues-2026-09-28.csv` preserves issue metadata and audit columns. `gameguild-issues-source-2026-09-28.jsonl` preserves each original issue body and source metadata as one JSON-escaped record per issue. The audit columns are deliberately marked pending until each issue is compared with current code, tests, dependencies, and related issues. This snapshot is an inventory, **not a claim that any issue is implemented or resolved**. Update the audit columns only with source evidence and record the GitHub resolution (implementation, duplicate, obsolete, or non-actionable) with its supporting PR/test or rationale.


## Initial issue-level pass

Issues #30, #308, and #309 have an initial requirement/evidence pass. The two title-only issues (#30 and #308) have empty GitHub descriptions, so their criteria are marked as inferred from the title and linked PR and still need owner confirmation. #309 is explicitly partial. Their PRs have not merged; all three remain unresolved. The other 325 records are still untriaged.


## Linked implementation pull requests

A GitHub check snapshot identified open implementation PRs for 14 unique issues: #30, #149, #251, #260, #261, #262, #308, #309, #324, #335, #353, #354, #384, and #390. Their PR URLs, head commits, and CI gate states are recorded in the matrix. All 14 issues remain open across 11 open PRs; all linked PRs currently have a failing Web Verify and required gate, and #335 also has API Verify failing. Only #30, #308, and #309 have an initial issue-level evidence pass; the other linked PRs still need criteria-by-criteria review. The PR links are evidence pointers, not closure evidence.
