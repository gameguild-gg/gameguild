# Disaster drill runbook (paper)

> Status: **PAPER DRILL — procedures defined, live execution not yet evidenced.** This runbook
> closes the "how would we actually do it" half of the readiness review's operational UNKNOWNs
> (§6 of `docs/release/READINESS-2026-11.md`); the restore-objective and key-rotation sections
> carry explicit KNOWN GAPS that must be fixed before the drills can be executed for real.
> Scope: PostgreSQL restore point objective, outbox poison-message recovery (DLQ + replay),
> JWT signing-key rotation emergency, and webhook endpoint (signing-secret) rotation.
> Audience: the on-call operator during an incident. Every command is written to be copy-pasted.

## 0. Drill cadence and record

- Run each drill **quarterly** and within one week of any change to the drilled component
  (new outbox family, key-rotation wiring landing, webhook provider change).
- Each drill records: date, operator, commit SHA deployed at drill time, outcome, defects found,
  and wall-clock timings (detection → decision → recovery). File the record under
  `docs/operations/drills/` (create it on first use) and link it from the release register.
- A drill that finds a defect is a **successful drill**. The defect becomes an issue with the
  drill record as evidence; the drill reruns after the fix.

## 1. Database restore point objective

### 1.1 Target objective (PROPOSED — not yet met, see gap)

| Parameter | Target | Current status |
|---|---|---|
| RPO (max acceptable data loss) | 15 minutes | **UNKNOWN — no backup/restore evidence exists in the repository or this review had no environment access** |
| RTO (max acceptable downtime) | 1 hour | **UNKNOWN — never rehearsed** |
| Backup cadence | nightly base backup + continuous WAL archiving (PITR) | Not defined in repo |
| Backup retention | 30 days, plus the last 7 daily restore points verified monthly | Not defined in repo |
| Restore verification | Monthly `pg_restore` into a scratch database + row-count/`pg_dump` sanity | Never executed |

### 1.2 KNOWN GAP — no backup procedure exists

`docs/operations/` documents worker behavior, Devtron release flow, and outbox cleanup, but
**no file defines PostgreSQL backup, WAL archiving, restore, or verification for production**.
The Devtron runbook (`devtron-production-release.md`) explicitly states image rollback never
reverses schema, which makes the database restore path the *only* recovery for data-level
disasters — and it is undocumented. `authorization-cache-configuration.md` documents Redis
snapshot mechanics only.

**Action (release owner):** define the backup topology (managed-snapshot vs. `pgBackRest`/
WAL-G), write the restore procedure, and execute drill §1.3 in staging. Until then the
restore-point objective is **NOT MET** and the readiness verdict for operational readiness
stays conditional.

### 1.3 Drill procedure (staging first, then production)

1. **Pick the restore point**: `SELECT pg_current_wal_lsn();` — record it. Continue normal
   staging traffic for 5+ minutes so the restore point is demonstrably in the past.
2. **Restore**: on a scratch PostgreSQL 17 instance (never over the live one), restore the
   latest base backup and replay WAL to the recorded LSN
   (`recovery_target_lsn` + `recovery_target_action = 'promote'` for pgBackRest/WAL-G;
   for managed snapshots use the provider's point-in-time restore to the recorded timestamp).
3. **Verify schema consistency**: the restored `__EFMigrationsHistory` must contain exactly the
   migration set deployed at backup time — compare against `dotnet ef migrations list` at the
   release SHA. A backup whose history disagrees with the deployed binary cannot be restored
   under the same image; see `migration-rehearsal.md` for the verification query.
4. **Verify data**: row counts of the money-critical families (economy postings, payments,
   billing orders) against the pre-drill snapshot; run one read-only smoke (health endpoint +
   wallet read for a seeded account) against a temporary API instance pointed at the restored DB.
5. **Record**: elapsed time for restore+replay (this is your measured RTO), LSN delta accepted
   (this bounds your real RPO), and any defects.

**Cutover in a real incident**: freeze writes (scale API to zero or revoke the runtime role's
INSERT/UPDATE via the economy gate roles), restore/replay to the chosen target, repoint the
connection string, restart workers **in dependency order** (see §2.4), unfreeze, verify with
the read-only smoke.

## 2. Outbox poison-message recovery (DLQ + replay)

### 2.1 What exists today (develop @ 630eb8c6)

- **Core outbox/inbox rail** (`gameguild.integration` schema): per-consumer receipts with
  `DeadLetteredAtUtc`; the `OutboxDispatcher` dead-letters a parent message only when every
  consumer receipt is terminal and at least one is dead-lettered. Retired identities are never
  deleted (`outbox-cleanup.md`).
- **Operator surface** — `EventTransportAdminController` (`/api/v1/admin/events`), gated by the
  `SystemBackupRestore` policy (SystemAdmin role or delegated `system:backup-restore`):
  - `GET /api/v1/admin/events/status` — transport status;
  - `GET /api/v1/admin/events/dead-letters` — the poison inventory;
  - `POST /api/v1/admin/events/{eventId}:replay?consumerName={consumer}` — requeue one event
    (optionally for a single consumer). Replay locks the parent; cleanup skips locked parents.
- **Billing webhooks**: `BillingWebhookRetryWorker` with full DLQ semantics (the reference
  pattern), raw-payload retention opt-in/default-off (PII-driven).
- **Academic outbox DLQ + replay** and the **durable fraud re-scoring queue**: implemented on
  open PR #1285 (`fix/worker-integrity`, issue #1218) — see §2.5 before relying on them.

### 2.2 Drill procedure

1. **Inject poison** (staging): enqueue a use-case event whose consumer throws deterministically
   (e.g. point a staging consumer at an unreachable dependency via feature flag), or replay an
   existing dead letter from the inventory.
2. **Observe the attempt ladder**: the delivery retries with backoff; confirm attempts increment
   and `NextAttemptAt` advances (core rail) rather than hot-looping.
3. **Confirm dead-letter**: the parent disappears from the pending sweep and appears in
   `GET /api/v1/admin/events/dead-letters` with the last error preserved.
4. **Fix the cause** (deploy the fix or flip the flag back).
5. **Replay**: `POST /api/v1/admin/events/{eventId}:replay` — the event must return to the
   pending set, complete, and the dead-letter inventory must shrink. Verify the consumer's
   side effects happened exactly once (idempotency receipts hold).
6. **Record**: attempt count to dead-letter, replay latency, duplicate side effects (must be
   zero), and the audit trail entries the replay produced.

SQL for direct inspection (read-only):

```sql
SELECT "EventId", "EventType", "OccurredAtUtc", "DeadLetteredAtUtc"
FROM "gameguild.integration".outbox_messages
WHERE "DeadLetteredAtUtc" IS NOT NULL
ORDER BY "DeadLetteredAtUtc" DESC LIMIT 50;

SELECT "EventId", "ConsumerName", "AttemptCount", "LastError", "DeadLetteredAtUtc"
FROM "gameguild.integration".inbox_receipts
WHERE "DeadLetteredAtUtc" IS NOT NULL
ORDER BY "DeadLetteredAtUtc" DESC LIMIT 50;
```

### 2.3 Rules

- Never `TRUNCATE` or hand-delete outbox/inbox rows; retired identities protect against
  redelivery (`outbox-cleanup.md`).
- Never mark a pending event completed to "empty the queue".
- One replay at a time per event; replaying while a dispatcher holds the lease waits on the
  parent lock — check `status` first.

### 2.4 Worker restart order after a restore or extended outage

Workers that drain queues must start **after** the database accepts writes and **after** the
core `OutboxDispatcher` (so replayed parents are not double-processed): API host →
OutboxDispatcher → BillingWebhookRetryWorker → domain dispatchers (economy — only when its
§5 pre-enable conditions hold, see readiness §1 row 7) → notification dispatchers → cleanup
workers last (so nothing is retired mid-recovery).

### 2.5 KNOWN GAPS

- **Academic outbox has no DLQ on develop** — retries forever (300 s cap, no ceiling). The fix
  (attempt ceiling + dead-letter + `ReplayAcademicOutboxEventCommand`) is on open PR #1285.
  **Blocking defect found in rehearsal (2026-11-04):** migration
  `20261104111803_AddAcademicOutboxDeadLetter` adds mutually contradictory check constraints —
  the status constraint admits `'deadlettered'` but the lifecycle constraint tests the literal
  `'dead_lettered'`, so the application's terminal dead-letter write (`Status='deadlettered'` +
  `DeadLetteredAt` set) violates the lifecycle constraint and **fails at the database**. The
  DLQ cannot record its terminal state as shipped; the constraint must use one spelling and the
  app + migration must agree. Additionally the migration's Down() re-adds the old status
  constraint without `'deadlettered'`, so **rollback fails outright once any dead-lettered row
  exists**. See `migration-rehearsal.md` §4 for the live reproduction.
- **Economy outboxes (five families) have no attempt cap/DLQ** (economy 07-gaps N3), and the
  core/compliance outboxes are write-only spools with no dispatcher (N1/N2) — poison accumulates
  silently. Replay tooling above covers the `gameguild.integration` rail only.
- **Fraud re-score channel is volatile** on develop (`DropWrite` channel — lost on restart);
  #1285's durable `FraudBackgroundScoringJob` table is the fix, pending merge (and see the
  rehearsal note on its rollback).

## 3. JWT signing-key rotation emergency

### 3.1 Scenario

The current HS256 signing secret is suspected compromised (or must rotate on schedule).
Containment today relies on the 60-minute access-token lifetime alone: there is **no per-jti
deny-list population (#1179)** and no revocation path that invalidates already-issued tokens
without rotating the key.

### 3.2 What exists on develop (and does not work)

- `KeyRotationController` — `GET /v1/auth/signing-keys`, `POST /v1/auth/signing-keys:rotate`,
  `POST /v1/auth/signing-keys:cleanup` (gated by `SystemConfigurationManage`).
- `KeyRotationService` / `KeyRotationBackgroundService` — full implementation.
- **`IKeyRotationService` is not registered in DI and the worker is never hosted — every call
  to the rotation surface fails, and no automatic rotation runs. Issue #1189 (open).**

### 3.3 KNOWN GAP — the fix path

The validated fix has a delivery history operators must know:

1. **PR #1164** (`fix/auth-critical`) carried the rotation wiring
   (`AddJwtSigningKeys` migration, cache, mapper, registration) — **closed 2026-11-03 unmerged**.
2. **PR #1264** (consolidation retry) — closed, superseded.
3. **PR #1273** (`feature/branch-consolidation-delivery-20261104`) — **merged to `main`
   2026-11-04 (f4b1d8e7)**. This is the current carrier of the validated auth contracts;
   the key-rotation wiring reaches `develop` only when that delivery is synced/forward-ported.

**Until the operator verifies, on the exact SHA being deployed, that (a) `IKeyRotationService`
resolves from DI and (b) `KeyRotationBackgroundService` is hosted, the emergency procedure is
§3.4 (static rotation), not the API surface.**

### 3.4 Emergency procedure with rotation dead (current develop)

1. Generate a new ≥32-byte secret; store it in the secret manager under a **new** key slot
   (e.g. `Jwt:SecretKey-2026-11B`), never overwrite the old value.
2. Deploy the config change rolling both values where the pipeline supports overlap
   (`Jwt:SecretKey` new + previous retained for validation if the host supports dual-validate;
   on develop HS256 single-secret mode does **not** dual-validate — expect a hard cutover).
3. **Hard cutover sequence**: freeze new sign-ins is NOT required — instead accept that all
   access tokens issued before cutover fail validation after restart. Client impact: every
   user re-authenticates via refresh token (refresh tokens are DB-backed and survive);
   communicate the one-time 401 wave.
4. Restart API pods (rolling, `maxUnavailable: 0`), watch the 401 rate return to baseline and
   sign-in success recover.
5. Revoke and destroy the old secret's stored value only after two access-token lifetimes
   (2 × 60 min) have elapsed.
6. Record: decision time, deploy time, 401-wave duration. File the incident.

### 3.5 Emergency procedure once rotation is wired (post-#1189)

1. `POST /v1/auth/signing-keys:rotate` — the new key becomes the signer; existing keys remain
   valid for verification for the overlap window.
2. Monitor validation-failure metrics through one access-token lifetime.
3. `POST /v1/auth/signing-keys:cleanup` after the overlap expires.
4. Drill this quarterly; the drill asserts the overlap window (zero forced re-authentications).

## 4. Webhook endpoint (signing-secret) rotation

### 4.1 What exists (works on develop)

- `POST /api/v1/billing/webhooks/secrets:rotate` (issue #408,
  `RotateWebhookSecretCommand`): records a rotation for a provider key (`stripe`, …); the new
  secret becomes the active verifier; the previous secret stays accepted for
  `Billing:Webhook:SecretRotationWindowHours` (default **24 h**); secrets are stored encrypted
  (ASP.NET DataProtection) and never appear in logs or responses. The acting administrator is
  resolved from the actor context, never the request.

### 4.2 Rotation procedure (zero-downtime, provider-side first)

1. **Provider side**: create a *new* webhook endpoint (or rotate the signing secret of the
   existing one) at the provider; you receive the new `whsec_…` value. Do not delete the old
   endpoint yet.
2. **GameGuild side**: `POST /api/v1/billing/webhooks/secrets:rotate` with
   `{"providerKey":"stripe","newSecret":"whsec_…"}`. From this instant both secrets verify.
3. **Provider side**: point delivery at the new endpoint/secret and confirm deliveries succeed
   (`GET /api/v1/billing/webhooks/…` delivery status, or the provider dashboard).
4. After the rotation window (24 h default) the old secret stops verifying automatically;
   decommission the old provider endpoint only after the window closes.
5. Rollback within the window: rotate again with the previous secret as `newSecret`.

### 4.3 Drill procedure

1. In staging, seed the provider simulator with a known secret; confirm deliveries verify.
2. Rotate to a second secret via the API (as an admin with the billing permission); deliver
   with the OLD secret — must still 200 (window). Deliver with the NEW secret — must 200.
3. Shrink the window (`Billing:Webhook:SecretRotationWindowHours=0` in staging), restart,
   deliver with the old secret — must 401. This proves expiry actually expires.
4. A delivery failing signature verification must land in the billing webhook retry rail and
   dead-letter after its attempts (inspect via §2 surfaces), never silently drop.
5. Record timings and any 5xx during rotation.

### 4.4 Notes and gaps

- The DataProtection key ring protects the stored secrets — the DP certificate rotation is a
  separate dependency (`DATAPROTECTION_CERTIFICATE_BASE64`/`KEY_BASE64` env vars; missing vars
  kill boot by design). Rotating the DP cert invalidates stored webhook secrets — schedule DP
  cert rotation as its own drill before attempting it in production.
- Provider allowlist: `google-pay`/`apple-pay` verify with provider public keys, not shared
  secrets; only secret-based providers (Stripe et al.) use this procedure.

## 5. Worker kill switches

- Economy dispatch/anchor workers default OFF (`DispatchWorkersEnabled`); keep them off until
  readiness §1 row 7's pre-enable conditions hold (kill-switch re-check, cancel paths, attempt
  caps — economy 07-gaps N1–N7). In a money-adjacent incident, the first lever is these flags,
  not the database.
- Known defect: the kill switch does not stop in-flight outbox provider sends (N6), and the
  SMS/Push/WhatsApp dispatchers poll while disabled on develop (missing `return`, #1218 — fixed
  on PR #1285).

## 6. Verdict table for the readiness review

| Drill | State | Blocker |
|---|---|---|
| DB restore point objective | **NOT MET (procedure defined, execution unevidenced)** | No production backup topology defined in-repo (§1.2) |
| Outbox poison recovery (core rail) | **READY-DRY** (surface exists; run §2.2 in staging to make it READY) | — |
| Academic/fraud DLQ (PR #1285) | **BROKEN AS SHIPPED** | Contradictory check constraints make the terminal dead-letter write fail; Down() un-rollbackable after use (§2.5, `migration-rehearsal.md` §4) |
| Key-rotation emergency | **NOT MET — rotation dead on develop (#1189)** | Fix delivered to `main` via #1273 (f4b1d8e7), pending develop sync; verify wiring per-SHA before relying on §3.5 |
| Webhook secret rotation | **READY-DRY** (endpoint works incl. dual-accept window) | Drill §4.3 to make it READY; DP-cert dependency noted |
