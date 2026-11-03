# Compliance State Machines and Transitions Audit

> Modules `GameGuild.Compliance.Audit` / `Consent` / `FERPA` / `KYC` · Documented against `develop` commit `ac959fd8f`.
> Citation shorthand as in `01-user-flows.md`.

## 1. Central audit record lifecycle (`AuditLogs`)

There is no state column on `AuditLog` — a row is born terminal. The lifecycle that matters is *derivational*: soft-delete eligibility under retention, the archive tier move, and the capture tiers that feed it. The tamper-evident tier (`TamperEvidentAuditLogs`) is a separate per-tenant append-only chain.

```mermaid
stateDiagram-v2
    direction TB
    state "Captured - plain tier" as Plain
    state "Captured - security tier with alerts" as Secured
    state "Spooled - disk jsonl" as Spooled
    state "Durable-log-only - both legs failed" as Logged
    state "Archived - ArchivedAuditLogs tier" as Archived
    state "Deleted - past cutoff no hold" as Deleted

    [*] --> Secured : security event persisted - SecurityEventLogger L200
    [*] --> Plain : non-security audit - AuditService L48
    Secured --> Spooled : db down and spool on L242
    Plain --> Logged : two attempts failed L84
    Secured --> Logged : db down and spooling disabled L275
    Spooled --> Secured : drainer replay - idempotent L105
    Spooled --> Spooled : unparseable record kept L120
    Plain --> Archived : archive window elapsed - no hold L200
    Secured --> Archived : archive window elapsed - no hold
    Archived --> Deleted : delete window elapsed - no hold
    Plain --> Deleted : past per-category cutoff L197
    Secured --> Deleted : past per-category cutoff
    note right of Deleted
        Legal hold suspends both moves - execution row still written
        SecurityLogRetentionService L176-190
    end note
```

### 1a. Transitions audit — central audit record

| Transition | Trigger | file:line | Guard |
|------------|---------|-----------|-------|
| birth (plain) | any `IAuditService.LogAsync` | `Audit:/Services/AuditService.cs:46-49` | PII interceptor redacts during save |
| birth (security) | taxonomy-classified events, alerts evaluated same context | `Audit:/Services/SecurityEventLogger.cs:187-198` | idempotency pre-check on replay only |
| capture → spool | db unavailable after bounded retries | `SecurityEventLogger.cs:242-260` | PII-sanitized before disk |
| spool → captured | drainer `ReplaySpooledEventsAsync` | `:105-155`, `SecurityEventSpoolDrainerBackgroundService.cs:34` | `AlreadyPersistedAsync` by event id |
| hot → archived | retention pass archive window | `SecurityLogRetentionService.cs` (issue #193 tiering), endpoints `SecurityEventController.cs:227,254` | legal hold suspends (`:176-190`) |
| archived / hot → deleted | per-category cutoff, batched | `:167-205` + options (`SecurityEventPipelineOptions` DefaultRetentionDays 400 `:57`, batch 5000 `:60`) | hold + dry-run flag |

**Unreachable / dead ends:** `Logged` (both persistence legs failed) has no replay path — the payload only survives in structured logs for manual reconciliation; nothing surfaces `SecurityEventSpoolingDisabled` outcomes as an alert (only a critical log line, `SecurityEventLogger.cs:263-282`).

## 2. Security alert (`SecurityAlerts`)

```mermaid
stateDiagram-v2
    direction LR
    state "Open" as Open
    state "Acknowledged" as Acknowledged
    state "Resolved" as Resolved

    [*] --> Open : rule evaluated on captured event - SecurityEventLogger L196
    Open --> Acknowledged : acknowledge action - SecurityEventController L79
    Acknowledged --> Resolved : resolve action L110
    Open --> Resolved : resolve without ack - no guard against skip
    Resolved --> Acknowledged : SecurityAlertTransitionException - 409 L98
```

Audit notes: transitions are guarded (`SecurityAlertTransitionException` → 409, `SecurityEventController.cs:98-103,129-133`); the acting administrator is derived from the request context, not the body (unlike Consent/FERPA). Skip-ahead (Open → Resolved) is permitted. Rules: failed-authentication thresholds, permission-change thresholds, bulk-permission-change thresholds (`AuditModule.cs:86-90`; integration coverage `PermissionPatternAlertsPostgreSqlTests.cs`).

## 3. Custody evidence and transfer (`CustodyEvidenceItems`, `EvidenceTransfers`)

```mermaid
stateDiagram-v2
    direction TB
    state "Preserved" as Preserved
    state "SealBroken" as SealBroken
    state "Transferring" as Transferring
    state "Released" as Released
    state "HoldProtected" as HoldProtected

    [*] --> Preserved : PreserveEvidenceCommand - content hashed SHA-256
    Preserved --> SealBroken : break-seal - forensic scope required
    Preserved --> Transferring : InitiateTransfer - challenge nonce plus ECDSA proof
    Transferring --> Preserved : AcknowledgeTransfer - counter-sign and custody handoff
    Transferring --> Transferring : proof rejected - retry allowed
    Preserved --> HoldProtected : legal hold created - automatic preservation
    HoldProtected --> Preserved : hold released - chains retained
    Preserved --> Released : release action - blocked under hold
    SealBroken --> Released : release after inspection
```

Audit notes: evidence release is **blocked while a legal hold is active** (`ChainOfCustodyController.cs:111-112` doc + `LegalHoldService`); custody reads are themselves logged (`CustodyAccessLogs` table); integrity is re-verifiable at any time against the stored hash (`:134-148`). Custodian registry is fail-closed: only registered active custodians may act (`:165-185`). No dead states observed; transfer has no timeout — a transfer left unacknowledged stays `Transferring` forever (operator-visible via `GET transfers`, no sweeper).

## 4. Scheduled audit export (`ScheduledAuditExports` + `AuditExportHistories`)

```mermaid
stateDiagram-v2
    direction LR
    state "Enabled" as Enabled
    state "Claimed" as Claimed
    state "Succeeded" as Succeeded
    state "Failed" as Failed
    state "Disabled" as Disabled

    [*] --> Enabled : CreateScheduledAuditExportCommand - cron validated
    Enabled --> Claimed : worker claims due row - stale-claim steal threshold
    Claimed --> Succeeded : file stored + history row + checksum
    Claimed --> Failed : error recorded - LastErrorMessage
    Failed --> Claimed : next due run
    Enabled --> Disabled : Delete action - history retained
    Succeeded --> Claimed : next occurrence
    note right of Succeeded
        Files removed after retention period
        history rows survive - worker ExpireExpiredFilesAsync
    end note
```

Audit notes: `Version` is a concurrency token (`AuditModelConfiguration.cs:159`) so two workers cannot both claim; the `(IsEnabled, NextRunAt)` index (`:160`) backs the due-scan; poll loop is 1 minute (`ScheduledAuditExportBackgroundService.cs:11`).

## 5. Consent and data-subject request (Consent module)

```mermaid
stateDiagram-v2
    direction TB
    state "NoConsent" as NoConsent
    state "Granted" as Granted
    state "Withdrawn" as Withdrawn

    NoConsent --> Granted : GrantConsentCommand - new row IsGranted true
    Granted --> Granted : idempotent re-grant returns existing
    Granted --> Withdrawn : RevokeConsentCommand - Revoke sets IsGranted false
    Withdrawn --> Granted : insert new row - unique index violation 500 - GAP N2

    state "DSR Pending" as Pending
    state "DSR InProgress" as InProgress
    state "DSR Completed" as Completed
    state "DSR Rejected" as Rejected
    state "DSR Expired" as Expired

    [*] --> Pending : SubmitDataSubjectRequestCommand - deadline plus 30 days
    Pending --> Completed : process command - Complete entity method
    Pending --> InProgress : never - no command sets it
    Pending --> Expired : never - no deadline sweeper exists
    Completed --> Rejected : never - terminal
```

Transitions audit:

| Transition | Trigger | file:line | Guard |
|------------|---------|-----------|-------|
| birth → Granted | grant | `Consent:/Services/ConsentService.cs:57-67` | unique `(UserId, PolicyVersionId)` index (`Entities/ConsentEntities.cs:88`) — **collides on re-grant** (N2) |
| Granted → Withdrawn | revoke | `:80-90` (entity `Revoke()` `:121-126`) | silent no-op when absent/already withdrawn |
| Pending → Completed | process | `:119-133` (entity `Complete` `:162-169`) | overwrites `ProcessingNotes`; `OnTime` recorded as metadata only |
| Pending → Rejected | **nothing calls** `Reject` (`ConsentEntities.cs:171-178`) | — | unreachable |
| Pending/any → Expired | **no worker / no code path sets it** (`:198` enum) | — | unreachable — 30-day GDPR clock is decorative (N3) |
| Pending → InProgress | **no setter exists in service/commands** | — | unreachable |

**Missing guards:** no state guard in `ProcessDataSubjectRequestAsync` — completing an already-Completed request silently overwrites processor attribution/notes (`ConsentService.cs:123-124`), unlike the FERPA entity which throws.

## 6. FERPA inspection request + disclosure consent

```mermaid
stateDiagram-v2
    direction TB
    state "F Pending" as FPending
    state "F InReview" as FInReview
    state "F Completed" as FCompleted
    state "F Denied" as FDenied
    state "F Expired" as FExpired

    [*] --> FPending : SubmitFerpaInspectionRequestCommand - deadline plus 45 days
    FPending --> FCompleted : complete - Approved true - Complete method
    FPending --> FDenied : complete - Approved false - Deny method
    FPending --> FInReview : never - no command assigns InReview
    FInReview --> FCompleted : permitted by guard - but unreachable entry
    FInReview --> FDenied : permitted by guard - unreachable entry
    FPending --> FExpired : never - no expiry sweeper
    FCompleted --> FPending : never - terminal guard throws

    state "C Active" as CActive
    state "C ExpiredByTime" as CExpired
    state "C Revoked" as CRevoked

    [*] --> CActive : GrantFerpaDisclosureConsentCommand
    CActive --> CExpired : ExpiresAt passes - passive - IsActiveAt false
    CActive --> CRevoked : revoke idempotent L145-154
    CRevoked --> CActive : never - grant always inserts a new row
```

Transitions audit:

| Transition | Trigger | file:line | Guard |
|------------|---------|-----------|-------|
| Pending → Completed/Denied | `CompleteFerpaInspectionRequestCommand` | `Ferpa:/Services/FerpaService.cs:155-171`, entity `:225-251` | `EnsureCanBeProcessed` allows Pending **or InReview**; terminal → `InvalidOperationException` (surfaces as 500 — the controller has no mapping for it) |
| Pending → InReview | nothing | only `FerpaEntities.cs:247` references the value | unreachable entry state |
| any → Expired | nothing sets `FerpaRequestStatus.Expired` (`:46`) | deadline `:216` never enforced | unreachable — 45-day clock decorative (N3) |
| consent revoke idempotence | `Revoke()` second call returns | `:146-151` | clean |
| disclosure gate | basis-based | `FerpaService.cs:110-123` | only Student/GuardianConsent require an active consent; all other bases caller-asserted |

## 7. KYC — legacy ledger (`user_kyc_verifications`)

```mermaid
stateDiagram-v2
    direction TB
    state "Pending" as Pending
    state "InProgress" as InProgress
    state "Approved" as Approved
    state "Rejected" as Rejected
    state "Suspended" as Suspended
    state "Expired" as Expired

    [*] --> Pending : SubmitVerificationAsync L22-46
    Pending --> InProgress : UpdateVerificationStatus - no guard L70
    Pending --> Approved : same - sets CompletedAt and ExpiresAt L72-77
    Pending --> Rejected : same - sets CompletedAt
    Approved --> Suspended : same
    Approved --> Expired : never written - expiry is consumer-side only
    Approved --> Pending : possible - no transition table - GAP N9
    Rejected --> Approved : possible - no transition table - GAP N9
    Suspended --> Approved : possible - same
    Expired --> InProgress : possible - same
    Pending --> Deleted : DeleteKycVerification - hard delete - GAP N10
```

Transitions audit:

1. **No transition table at all** — `UpdateVerificationStatusAsync` assigns any enum value (`Kyc:/Services/KycService.cs:70-72`); `ProcessProviderWebhookAsync` likewise (`:232-241`). Both consumers (sign-in gate, payout eligibility) read only `Approved`, so a regressed row silently flips a verified user to challenged/denied — and back.
2. **`Expired` is never assigned by any writer** — grep shows no assignment; expiry is *derived* (`ExpiresAt` checked in `KycRepository.cs:39-44`, `KycAndHoldSignInCompliancePolicy.cs:80-98`, `ComplianceKycPayoutEligibilitySource.cs:36-37`). The stored status stays `Approved` while the world treats it as lapsed — the sign-in gate documents this deliberately (`KycAndHoldSignInCompliancePolicy.cs:83-86`), but the report endpoint counts `Expired` rows that can never exist (`KycService.cs:270` — `ExpiredVerifications` is permanently 0).
3. **Hard delete of compliance evidence** (`KycHandlers.cs:139-152`) with no audit row and no guard — reachable by any code path that sends the command; no controller today, but the handler is registered.

## 8. KYC — SumSub evidence pipeline (`compliance_sumsub_applicant_bindings` + inbox)

```mermaid
stateDiagram-v2
    direction TB
    state "Created" as Created
    state "ApplicantPending" as ApplicantPending
    state "InReview" as InReview
    state "NeedsReview" as NeedsReview
    state "Approved" as Approved
    state "Rejected" as Rejected
    state "PipelineExpired" as PipelineExpired

    [*] --> Created : StartAsync - applicant created L109-119
    Created --> ApplicantPending : webhook reviewStatus pending - MapWebhookState L389
    Created --> InReview : webhook other - default branch L393
    ApplicantPending --> InReview : subsequent webhook
    InReview --> NeedsReview : reviewStatus completed - no GREEN or RED L391
    NeedsReview --> Approved : GREEN reviewAnswer L379
    NeedsReview --> Rejected : RED reviewAnswer L384
    ApplicantPending --> Approved : GREEN direct
    ApplicantPending --> Rejected : RED direct
    InReview --> Approved : GREEN direct
    InReview --> Rejected : RED direct
    Approved --> Rejected : later RED webhook - monotonic clock only guards ordering L199
    Rejected --> Approved : later GREEN webhook - possible - same
    Approved --> PipelineExpired : evidence ExpiresAt passes - consumer-side only
```

Transitions audit:

1. Ordering is protected by `LastProviderIssuedAt` monotonicity (`SumSubKycAmlOrchestrator.cs:199-205`) — **event-time** ordering, not state-machine legality: a genuine provider reversal (GREEN then RED) is applied, and evidence versions increment (`:217`), so both directions are legal and versioned. This is correct for KYC reality; the contrast with the legacy ledger (no ordering, no versioning) is the finding.
2. `KycAmlState.Expired` (`SumSubKycAmlProvider.cs:20`) is defined but never mapped or assigned — dead enum value, same as the legacy ledger's `Expired` but with consumers deriving expiry from the evidence envelope `ExpiresAt` instead.
3. Inbox states: `Processed` (error null), `Rejected` (invalid-signature / unknown-applicant / invalid-timestamp), `Deferred` (out-of-order), `Duplicate` — rejected/deferred rows have **no retry or DLQ worker**; they sit with `ProcessingError` for operator inspection (`:180-227`). No replay endpoint exists for them (contrast: Billing webhook inbox has `webhook-events/{id}:retry`).
4. DB check constraints back the machine: `State BETWEEN 1 AND 7`, `EvidenceVersion >= 0`, jurisdiction `^[A-Z]{3}$` (`SumSubEvidencePersistence.cs:45-53`), inbox `ReceivedAt >= IssuedAt` (`:71-73`) — good defense in depth.

## 9. Cross-cutting transitions summary

| Machine | Terminal states | Unreachable states | Missing guards |
|---------|-----------------|--------------------|----------------|
| Central audit record | Deleted | — (stateless) | `Logged` tier never replayed; `SpoolingDisabled` never alerted |
| Security alert | Resolved | — | Open → Resolved skip allowed (deliberate?) |
| Custody evidence | Released | — | transfer stuck in Transferring forever (no timeout/sweep) |
| Scheduled export | Disabled (history kept) | — | none observed |
| Consent | Withdrawn (re-grant impossible — bug) | `InProgress`, `Expired`, `Rejected` (DSR) | no re-complete guard on DSR; no deadline sweep |
| FERPA inspection | Completed, Denied | `InReview` (entry), `Expired` | terminal-state failure surfaces as 500 not 409 |
| FERPA consent | Revoked | — | passive expiry correct |
| KYC legacy | none (all reversible) | `Expired` (never written) | **no transition table at all**; hard delete |
| KYC SumSub | evidence expiry is passive | `Expired` enum dead | rejected/deferred inbox rows never retried |
