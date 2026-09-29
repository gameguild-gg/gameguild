# GameGuild issue closeout inventory

Snapshot date: 2026-09-28 (GitHub API).

This initial source inventory contains **328 unique issues** authored by or assigned to `mathrmartins` in `gameguild-gg/gameguild`: **188 open** and **140 closed**. Pull requests are excluded. The scope is the union of GitHub's `creator=mathrmartins` and `assignee=mathrmartins` issue filters, deduplicated by issue number.

`gameguild-issues-2026-09-28.csv` preserves issue metadata and audit columns. `gameguild-issues-source-2026-09-28.jsonl` preserves each original issue body and source metadata as one JSON-escaped record per issue. The audit columns are deliberately marked pending until each issue is compared with current code, tests, dependencies, and related issues. This snapshot is an inventory, **not a claim that any issue is implemented or resolved**. Update the audit columns only with source evidence and record the GitHub resolution (implementation, duplicate, obsolete, or non-actionable) with its supporting PR/test or rationale.


## Initial issue-level pass

Eight issues have an issue-level evidence pass: closed legacy issues #8, #9, #10, #12, and #13, plus #30, #308, and #309. #8/#9/#12 are supported by current repository structure and their closure comments; #10 is obsolete under the current .NET architecture; #13 is consolidated with #72 and both current application Dockerfiles built successfully. #30 and #308 have empty GitHub descriptions, so their criteria remain inferred from the title and linked PR and need owner confirmation; #309 is explicitly partial. The other 320 records have not received an issue-level audit.


## Linked implementation pull requests

The matrix links 15 issue records to 11 open implementation PRs: #30, #149, #251, #260, #261, #262, #308, #309, #324, #335, #353, #354, #384, #387, and #390. This includes related issue #387 on PR #584 and #149 related to the #260/#261 rate-limit work in PR #582. PR URLs, head commits, and the last captured CI states are in the matrix; refresh checks before treating them as current. These links are evidence pointers, not closure evidence.
