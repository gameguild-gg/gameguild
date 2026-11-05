# Migration rehearsal (forward + rollback)

> Purpose: close the readiness review's "rollback rehearsal UNKNOWN"
> (`docs/release/READINESS-2026-11.md` §6). The CI **Migration compatibility** lane already
> proves the FORWARD half on every qualifying PR (snapshot check → `dotnet ef database update`
> against a clean PostgreSQL 17 → DBML agreement). This document adds the missing half — the
> **rollback rehearsal** — plus the exact procedures, the current findings, and the reusable
> script (`scripts/devops/migration-rehearsal.sh`).
>
> Rehearsal evidence below was produced locally on 2026-11-04 against
> develop @ `630eb8c6d7` and PR #1285 head @ `8ce0c7b994`, using the CI lane's pinned image
> `postgres:17-alpine@sha256:b0f9560a…` on a disposable container. Numbers refer to that run.

## 1. Migration inventory facts (verified)

- The API's migration folder contains **226 real migrations** plus 47 `*.Security.cs` partial
  companion files (role/grant SQL invoked by their parent migration's `Up()`, sharing the
  parent's ID — they are not separate history rows) plus per-migration `*.Designer.cs` files.
  Count them with:
  `ls …/Database/Migrations | grep -v 'Designer\.cs$' | grep -v '^ApplicationDbContextModelSnapshot\.cs$' | grep -v '\.Security\.cs$' | grep -c '\.cs$'`
  (beware: real migrations legitimately contain the word "Snapshot" in their names —
  `ReconcileBountyEscrowModelSnapshot`, `AddMutationValueSnapshotsAndChainOfCustody`,
  `AddValueSnapshotArchiveAndLegalHoldDelivery` — so the current model snapshot must be
  excluded by its exact file name, never by a `ModelSnapshot.cs` suffix or `Snapshot`
  substring: `ReconcileBountyEscrowModelSnapshot.cs` itself ends in `ModelSnapshot.cs`,
  and a suffix filter silently undercounts by that one file; corrected here after the
  2026-11-04 run's script form tripped on it — the recorded 226 figure was and remains
  the true count at `630eb8c6d7`).
- The Identity.Authentication module carries 2 additional migrations in its own folder
  (`Modules/GameGuild.Identity.Authentication/Data/Migrations`) for its own DbContext; they are
  not part of the API chain rehearsed here.
- After a full forward apply, `__EFMigrationsHistory` contains exactly **226 rows** — file
  count == `dotnet ef migrations list` count == applied rows. That triple-equality is the
  history-completeness check (§2 step 2).

## 2. Procedure — forward rehearsal (what CI proves, reproduced locally)

1. Start disposable PostgreSQL 17 (CI-pinned digest):
   ```bash
   docker run -d --name gg-migration-rehearsal \
     -e POSTGRES_DB=gameguild -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres \
     -p 127.0.0.1:55432:5432 \
     mirror.gcr.io/library/postgres:17-alpine@sha256:b0f9560a2de083e2cc7382e75f808c7381a32852a7ec49117deedb300e552b24
   ```
   (Use `Host=127.0.0.1` — `localhost` resolves IPv6 first and misses the IPv4 port binding.)
2. Apply the complete history and verify completeness:
   ```bash
   export PATH="/c/Program Files/dotnet:$PATH"
   dotnet ef database update \
     --project apps/api/Source/GameGuild.API/GameGuild.API.csproj \
     --startup-project apps/api/Source/GameGuild.API/GameGuild.API.csproj \
     --connection "Host=127.0.0.1;Port=55432;Database=gameguild;Username=postgres;Password=postgres"
   dotnet ef migrations list --project … --startup-project … --connection … # every row must be applied
   docker exec gg-migration-rehearsal psql -U postgres -d gameguild \
     -tAc 'SELECT count(*) FROM "__EFMigrationsHistory";'   # == file count (226 on 2026-11-04)
   ```
   Result this run: 226/226 applied, no pending.
3. Snapshot the schema: `docker exec … pg_dump -s -U postgres gameguild > schema-at-latest.sql`.

## 3. Procedure — rollback rehearsal (the missing piece)

Roll back the N latest migrations **one at a time** with `dotnet ef database update <target>`
(EF executes each `Down()` in its own transaction and maintains the history table), verifying
after each step, then re-apply forward and diff the schema snapshot. The whole procedure is
scripted in `scripts/devops/migration-rehearsal.sh` (same container, same checks, JSON verdict
in `artifacts/migration-rehearsal/`).

### 3.1 Run of 2026-11-04 — develop @ 630eb8c6, last 3 migrations

| Step | Migration (PR) | Down() verdict | Verified after revert |
|---|---|---|---|
| Rollback 1 | `20261103141023_AddEconomyTopUpRefundReversalCapability` | **Clean inverse.** Deletes exactly the seeded `economy_registered_capabilities` row (`economy.reverse-provider-funding.v1`); history row removed. | Row gone, history count 225 |
| Rollback 2 | `20261102231010_AddSecurityBatch3Enhancements` (#1269) | **Structurally true inverse, LOSSY on one column.** All 11 FKs dropped, the 8 re-created partial indexes restored to their original unfiltered shape, the brand-new `ix_tenant_members_user_id_active` correctly NOT recreated, `Users.PasswordChangedAt` dropped. | Column gone (probe row's timestamp destroyed — see finding F1), 0 batch-3 FKs remain in `gameguild.authentication`/`auth`, `IX_testing_events_TenantId_Status_StartsAt` back to unfiltered |
| Rollback 3 | `20261102105545_AddLedgersRatingsAndAchievementsTables` | **Clean structural inverse.** All 11 tables dropped in FK-safe order; the `finance` schema itself remains (EF never inverts `EnsureSchema` — harmless). | 0 of the 11 tables remain; history count 223 |

**Round-trip:** after re-applying the three migrations forward, `pg_dump -s` output is
**byte-identical** to the pre-rollback snapshot (47,100 lines; only pg_dump's per-session
`\restrict` guard token differs). No destructive gaps on the schema level for these three.

### 3.2 Run of 2026-11-04 — PR #1285 head @ 8ce0c7b994 (fraud/DLQ tables), +2 migrations

| Step | Migration | Down() verdict |
|---|---|---|
| Rollback 1 | `20261104115341_AddFraudBackgroundScoringJobQueue` | **Clean inverse.** `DropTable("FraudBackgroundScoringJob")` removes table + its 3 indexes atomically. Data-loss note: any queued fraud re-scoring jobs are destroyed — pending fraud signals silently vanish (see F4). |
| Rollback 2 | `20261104111803_AddAcademicOutboxDeadLetter` | **BROKEN after use.** Clean on an empty table, but with any `Status='deadlettered'` row present the revert **fails** with `23514: check constraint "CK_AcademicOutboxDeliveries_Status" … violated by some row` (live reproduction §4). |

## 4. Findings (issue material)

**F1 — Lossy but correct: `AddSecurityBatch3Enhancements` Down() destroys `PasswordChangedAt`
data (#1269).** The Up() adds `Users.PasswordChangedAt` (password-age policy, #1269); the
Down() drops the column. A live probe (user row with `PasswordChangedAt = 2026-11-04T10:00Z`)
survived the rollback with the value silently destroyed. Structural rollback of a column add is
necessarily lossy; the operator implication is that rolling back #1269 **resets every user's
password-age clock**, which re-exempts all users from the password-age policy until they change
password again (the column returns NULL = "never changed"). Any rollback past this migration
must be recorded as a policy-reset event. Severity: acceptable-with-signoff (standard EF
trade-off), documented here.

**F2 — Upsert asymmetry: `AddEconomyTopUpRefundReversalCapability` Down() deletes, not restores.**
Up() is an idempotent upsert that **overwrites** `AllowedTemplateKinds`/`IsEnabled`/`RevokedAt`
on conflict; Down() deletes the row outright. If an operator had customized the capability row
post-migration (narrowed the template kinds, disabled it), rollback removes it entirely rather
than restoring the customization — and re-applying restores the migration's defaults, not the
operator's. Minor, but worth a line in the rollback checklist: re-verify
`economy_registered_capabilities` configuration after any round-trip of this migration.

**F3 — BLOCKER for PR #1285: the academic DLQ's check constraints are mutually contradictory,
making the terminal dead-letter write impossible.** Migration `20261104111803` adds:
- Status constraint admitting `'deadlettered'` (no underscore — matches the app's enum
  serializer `DeadLettered.ToString().ToLowerInvariant()`, `GradingPersistenceModelConfiguration.cs:741`), and
- a Lifecycle constraint testing the literal `'dead_lettered'` (underscore) against
  `"DeadLetteredAt" IS NOT NULL`.

Live reproduction (migrated DB, 2026-11-04):
```sql
UPDATE "AcademicOutboxDeliveries"
SET "Status"='deadlettered', "DeadLetteredAt"=now(), "NextAttemptAt"=NULL
WHERE …;
-- ERROR: 23514 CK_AcademicOutboxDeliveries_Lifecycle violated
```
i.e. the exact write `GradingEntities.DeadLetter()` performs fails at the database — the DLQ
can never record its terminal state, so the dispatcher keeps retrying poison (or the worker
faults). The only constraint-satisfying "dead" state is `Status='deadlettered'` with
`DeadLetteredAt IS NULL` — a half-state nothing writes. **Fix before merge: use one spelling in
both constraints and align with the app serializer.**

**F4 — PR #1285's academic DLQ migration is un-rollbackable once used** (compounds F3): the
Down() re-adds the original status constraint without `'deadlettered'`; with any dead-lettered
row present the revert fails (live `23514` on `CK_AcademicOutboxDeliveries_Status`), leaving the
database stranded mid-revert — in the rehearsal the fraud-queue migration had already reverted
when the academic revert failed, leaving the pair half-rolled-back. A rollback path that only
works on an empty table is not a rollback path. **Fix: Down() must either rewrite dead-lettered
rows to `'failed'` (with `NextAttemptAt` repopulated to satisfy the restored lifecycle
constraint) before re-adding the constraint, or use `NOT VALID` + `VALIDATE` after a data
migration — the rewrite is the correct choice.**

**F5 — Fraud queue Down() drops pending jobs silently** (`AddFraudBackgroundScoringJobQueue`):
rolling back after the queue is in service destroys every queued/pending re-scoring job. The
jobs represent deferred fraud assessments for real payments; their loss is invisible (no
audit event fires). Operator rule: before rolling back this migration, drain or export
`"FraudBackgroundScoringJob"` rows where `Status` is pending/retrying, and re-enqueue after any
forward re-apply.

**F6 — table-create migrations are data-loss by design.** `AddLedgersRatingsAndAchievementsTables`
Down() drops 11 tables including all finance ledger data. This is normal for EF scaffolds; the
rehearsal's job is to make the loss **explicit and signed-off** (this table), not to prevent it.

## 5. How to run the rehearsal

```bash
# from the repo root, Docker required (the script creates and destroys its own container)
bash scripts/devops/migration-rehearsal.sh 3        # rehearse the 3 latest migrations
REHEARSAL_PG_PORT=55433 REHEARSAL_EVIDENCE=artifacts/migration-rehearsal-alt \
  bash scripts/devops/migration-rehearsal.sh 1      # custom port/evidence dir
```

The script fails loudly on: incomplete history after forward apply, a history row surviving its
revert, a failing Down(), or any schema diff between the pre-rollback and post-round-trip
snapshots (pg_dump session tokens excepted). It does NOT attempt to prove data preservation —
§4 is the ledger of known lossy Downs. Run it before every release that carries schema
changes (the Devtron runbook's three-staging-release requirement is the natural trigger) and
file the verdict JSON with the release record.

CI note: the script is workflow-ready (plain bash + docker + dotnet ef, writes machine-readable
verdicts) but is intentionally NOT wired as a required lane by this change — the release owner
should first decide whether rollback rehearsal gates releases or reports alongside them.
