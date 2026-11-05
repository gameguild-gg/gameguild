# SharedKernel STRIDE Threat Model — what goes wrong when the FOUNDATION is wrong

> Surfaces: `GameGuild.SharedKernel` + API host core (`apps/api/Source/GameGuild.API/Core/`) · Documented against `origin/develop` @ `ce31d5850c` (2026-11-05). Docs 01–07 were cut at `43603cb5b`; every cite reused below was re-verified unchanged at this base.
> This is the **meta-threat-model**: module STRIDE documents (e.g. `docs/authorization/07-stride-threat-model.md` family 7) analyze threats that travel *through* these surfaces — this one analyzes the surfaces *themselves*. The premise: the kernel is the substrate every module sits on, so a kernel-class failure multiplies across all modules at once. Six surfaces per commission: the CQRS pipeline, the outbox core, the middleware chain ordering, the EntityBase soft-delete contract, DataProtection, and LogRedaction.
> Cross-references: anatomy in [`01-cqrs-pipeline.md`](01-cqrs-pipeline.md), [`03-outbox-core.md`](03-outbox-core.md), [`04-middleware-chain.md`](04-middleware-chain.md), [`02-entity-softdelete.md`](02-entity-softdelete.md), [`05-security-primitives.md`](05-security-primitives.md); open items in [`07-gaps.md`](07-gaps.md) (N1–N7) tracked as **#1248** (open); data flows in [`09-dfds.md`](09-dfds.md).

Path convention: `file:line` relative to `apps/api/Source/` unless stated otherwise. `SK/` = `Modules/GameGuild.SharedKernel/`, `Host/` = `GameGuild.API/`.

## 1. Methodology and trust boundaries

STRIDE applied per surface, weighting **E** (elevation of privilege) and **T** (tampering) — the two
categories a shared kernel cannot offload to modules — and treating **D** (denial of service) as a
first-class foundation risk: the kernel is a single point that, when jammed, jams every module.
Trust boundaries crossed by the kernel (reused by the DFDs in `09-dfds.md`):

- **TB1** internet/proxy → host edge (ForwardedHeaders, TLS, the exception funnel)
- **TB2** host middleware chain → CQRS pipeline (principal/tenant/actor already materialized when the behavior chain starts)
- **TB3** any pipeline behavior ↔ every request (the position privilege — §2)
- **TB4** command transaction → outbox store (the durability boundary)
- **TB5** outbox dispatcher → module consumer code (the kernel executes module handlers)
- **TB6** DLQ ↔ replay admin surface (`Policies.SystemBackupRestore` tier)
- **TB7** DataProtection key material ↔ database + environment (key-at-rest surface)
- **TB8** process → log sinks (what leaves the process, in what form)

A note on attacker model: most of these surfaces sit *behind* the perimeter — the realistic
attacker for TB4–TB7 is an insider, a compromised co-tenant code path, or a supply-chain/PR-level
substitution (a malicious or careless `IPipelineBehavior` registration is indistinguishable from
a legitimate one at build time). That is precisely why the foundation's threat model must be
written down: its trust decisions are wiring decisions, not runtime ones.

## 2. Surface S1 — the CQRS pipeline itself

The property that makes the pipeline a threat surface: **one mediator dispatches every enabled
module's commands and queries** (`Host/Core/Setup/ApplicationLayerExtensions.cs:133-145`), and the
chain is assembled from `IEnumerable<IPipelineBehavior<TRequest,TResponse>>` in DI registration
order — **first registered = outermost** (`SK/CQRS/Implementation/MediatorSender.cs:145-168`,
resolution at `:173-197`). Every behavior receives the full request object and the `next`
delegate; nothing validates what a behavior does with either.

| Threat | Vector | Current control (mitigation) | Verdict |
|---|---|---|---|
| **E — behavior positioned outside authorization** | any module composition step that registers `IPipelineBehavior<,>` before `AddPermissionServices` runs places that behavior *ahead* of `AuthorizationBehavior` — it then executes for every request of its generic type, authenticated or not, and can call `next()` itself (bypassing the deny path) or fabricate a response | order is a **convention**, not a ratchet: `AuthorizationBehavior` is outermost only because `AuthorizationModuleExtensions.cs:352` (inside `AddPermissionServices`, invoked `InfrastructureLayerExtensions.cs:219`) registers before `:240-242`; no architecture test pins the order (the G7-candidate in `07-gaps.md` §"Suggested ratchets" would cover it) | **open (structural)** — the AuthorizationBehavior *position* is the single biggest unwritten trust decision in the codebase; today's chain is correct by inspection, not by enforcement |
| **E — post-authorization request mutation** | `AuthorizationBehavior` decides on the request's *type metadata* (`[AuthorizeRequest]`, `Behaviors/AuthorizationBehavior.cs:55-58`) and ACL `ResourceId` (`:118-120`); an inner behavior (or validator, `InfrastructureLayerExtensions.cs:240`) can then mutate the *same request instance* — the object authorized is not provably the object executed | none structural — behaviors share the request by reference; mitigation is code review of every registered behavior (four today, §`01-cqrs-pipeline.md` §2) | **open (structural, bounded)** — attack requires adding a hostile behavior, i.e. TB3 compromise; recorded because the pipeline cannot detect it even in principle |
| **I — behavior observes every request payload** | a registered behavior (or the handler itself) reads command DTOs — credentials ceremonies, recovery payloads, KYC documents — across **all modules**, since the mediator is shared (`ApplicationLayerExtensions.cs:133-145`) | none — by design; the trust decision is "who may register a behavior", and the answer today is "any module's DI extension" | **accepted (design)** — the blast radius of one hostile registration is platform-wide; this is why the order/ratchet gaps above matter more than they would in a per-module mediator |
| **D — silent short-circuit** | a behavior that returns without invoking `next()` (cache hit, fabrication) drops the handler, its transaction, and its outbox event with no trace — the pipeline has **no behavior-level observability** (`ObservabilityBehavior` exists at `SK/CQRS/Behaviors/ObservabilityBehavior.cs:26` and is never registered — gap N1 / #1248) | HTTP-level `UseSerilogRequestLogging` + `CostTelemetryMiddleware` still see a normal response; `UseCaseOperationBehavior`'s event only stages when the handler ran (`Host/Core/Eventing/UseCaseOperationBehavior.cs:117-154`) | **open (N1)** — a swallowed command is indistinguishable from a fast one |
| **E — fail-open dispatch** | missing handler, unknown permission, unavailable module command | missing handler → `InvalidOperationException` (`MediatorSender.cs:271-275`); unknown permission pattern → throws, never defaults to Write (`AuthorizationBehavior.cs:228-234`); disabled-module commands → explicit `UnavailableCommandHandler` stubs (`SK/CQRS/Extensions/ServiceCollectionExtensions.cs:77-111`) | **covered** — the pipeline's failure direction is uniformly closed |
| **R — pipeline decisions unlogged** | no record that a given command passed/short-circuited at each behavior | none at behavior level (N1); the durable audit trail starts at the handler (`UseCaseOperationOccurredV1`, PII-free payload `SK/Events/UseCaseOperationContext.cs:198-204`); permission *evaluations* are logged by the authorization module, not by the behavior position | **partial** — repudiation analysis for "was this command authorized" rests on the authorization module's own logs, not on the pipeline |

The one-sentence summary of S1: **the pipeline is a broadcast channel with a trust decision
expressed as DI order, and the only thing standing between "correct by inspection" and
"compromised by one registration" is review discipline** — arm the ratchets (§9).

## 3. Surface S2 — the outbox core

Assets: `gameguild.integration` (`outbox_messages` + `inbox_receipts`,
`Host/Database/EventTransportModelConfiguration.cs:17-19`). The outbox's job is to make
side effects survive crashes; its threat profile is *blast radius amplification* — one durable
row fans out to every registered consumer (`Host/Core/Eventing/OutboxDispatcher.cs:150-185`).

| Threat | Vector | Current control (mitigation) | Verdict |
|---|---|---|---|
| **T — poison message (deserialize/handler-resolution failure)** | a payload that cannot be deserialized or a handler that cannot be resolved throws at dispatch level | recorded against the synthetic consumer `GameGuild.EventTransport` and follows the same 5-attempt/≤900 s backoff/DLQ path as any consumer (`OutboxDispatcher.cs:345-375`) | **covered (containment)** — the poison itself cannot crash the loop (`OutboxBackgroundService` catches cycle exceptions) |
| **D — poison message blocks its aggregate stream forever** | per-aggregate FIFO: an event is claimable only if **no earlier sibling with `CompletedAtUtc IS NULL` exists** (`OutboxDispatcher.cs:80-87`) — and a *dead-lettered* event keeps `CompletedAtUtc = NULL`, so the FIFO exclusion subquery never exempts it | none on the dispatch path: the DLQ does not unblock the aggregate; the only exits are admin `:replay` (which re-arms and re-poisons unless the cause is fixed — §below) or manual completion; first recorded by this audit (the mechanics were documented in `03-outbox-core.md`, the stall consequence was not) | **open (design property, NEW finding S2-a)** — one attacker-induced or accidental poison event freezes *all later events for that `(TenantId, AggregateType, AggregateId)`* — the blast radius is the aggregate's entire downstream, not the one event; severity scales with what shares the aggregate key |
| **E — DLQ escape via replay** | replaying an event re-executes consumers; if a consumer already applied its side effect on attempt N and dead-lettered on attempt N+1 (partial work + rollback gap inside the consumer's own transaction), replay re-applies | replay only touches receipts with `DeadLetteredAtUtc != null` — **completed** receipts are never re-armed (`Host/Core/Eventing/EventReplayService.cs:69-90`); consumer execution is inbox-gated (`TryLockAsync` skips completed receipts, `Host/Core/Eventing/InboxStore.cs:10-64`) and each consumer runs in its own scope + transaction (`OutboxDispatcher.cs:242-343`); semantics are therefore at-least-once with idempotency pushed to consumers | **partial (by design)** — "escape" requires a consumer whose HandleAsync commits side effects outside its consumer transaction; the kernel cannot enforce consumer-internal atomicity (documented contract, `03-outbox-core.md` §1 consumer isolation) |
| **E — unauthorized replay** | replay re-arms arbitrary dead letters (attempt counter reset, `EventReplayService.cs:82-94`) — a replay flood is a re-execution flood | replay surface is the admin controller gated `Policies.SystemBackupRestore` (SystemAdmin or delegated `system:backup-restore`, `Host/Core/Controllers/EventTransportAdminController.cs:14-17`); the CQRS command `ReplayOutboxEventCommand` rides the same pipeline/authorization | **covered** — gated at the backup/restore tier, platform-scoped |
| **T — forged outbox rows** | direct DB write into `outbox_messages` (TB4 insider) executes module handlers on attacker-chosen payloads | serialization validates stable type names + schema version + `[NonPersonalEventData]` PII gates (`DurableEventSerializer` + `DurableIntegrationEventValidator`, `03-outbox-core.md` §1); the write side is in-transaction only | **accepted** — an actor with DB write access is already inside the perimeter; the validator at least forces well-formed, contract-known events |
| **T — duplicate dispatch after lease expiry** | dispatcher crash between claim (2-min lease, `OutboxDispatcher.cs:17`) and finalize → another dispatcher re-claims | finalization serialized by `FOR UPDATE` on the parent row + delivery-tombstone check (`OutboxDispatcher.cs:190-211`); inbox receipts gate consumer re-execution | **covered** (at-least-once, deduplicated at consumer) |
| **I — failure text persists request data** | `InboxReceipt.LastError` stores `exception.ToString()` truncated to 4000 chars (`InboxStore.cs:106`) — handler exceptions routinely embed request values; the same text is served on the admin dead-letters surface (`EventTransportAdminController.cs:25-29`) | truncation only; no redaction pass over `LastError` (LogRedaction is call-site discipline, §7) | **open (NEW finding S2-b)** — the one kernel store that durably persists unredacted exception text, readable by backup/restore-tier admins |
| **D — queue exhaustion** | flood of events (a module emitting per-request events) outpaces 50/2 s dispatch | backlog drain skips the delay when a full batch dispatches (`OutboxBackgroundService.cs:55-58`); no per-tenant fairness — one tenant's aggregate storm can starve others in FIFO order | **partial** — drain exists, fairness does not (scale-dependent; operational) |
| **D/R — hand-rolled queues invisible** | Economy ×5 + Learning.Assessments queues have no DLQ bookkeeping and no replay; their failures are invisible to the admin status surface | gap N5 (`07-gaps.md`), tracked in #1248 scope notes | **open (known, N5)** |

## 4. Surface S3 — the middleware chain ordering

The chain is one method, `Host/Core/Setup/PipelineExtensions.cs:33-188`. Its security property
is **order** — and the order is enforced by comments plus one un-armed validator.

| Threat | Vector | Current control (mitigation) | Verdict |
|---|---|---|---|
| **E — security-spine reorder** | moving tenant resolution before authentication, or authorization before actor context, silently degrades every check built on the spine (`PipelineExtensions.cs:98-119` documents each dependency) | `MiddlewareOrderValidator` — complete, with per-violation fix messages (`Modules/GameGuild.Identity.Context/Middleware/MiddlewareOrderValidator.cs:33-106`) — is **never invoked** (gap N3); no architecture test pins `ConfigurePipeline` source order (N6) | **open (the armed-vs-unarmed tripwire, #1248 N3)** — the order is correct today (verified `04-middleware-chain.md` §1 at this base); a one-line reorder in a refactor passes CI. This is the canonical "foundation wrong, every module wrong" case: the spine feeds `ActorContext` to every policy handler and to `AuthorizationBehavior` |
| **D — unauthenticated flood reaches auth machinery** | rate limiting runs **after** authorization (`:122` vs `:119`): a pre-auth flood burns JWT signature validation and token-store lookups before any limiter rejects | deliberate trade-off (authenticated throttling — per-user keys need identity; anonymous buckets collapse behind NATs): deny-list 403s first inside the block (`RateLimitAccessControlMiddleware`, `:124`), anonymous endpoints get per-IP partitions, guest issuance strictly per-IP (`05-security-primitives.md` §6) | **accepted (documented trade-off)** — residual: auth-path resource consumption by unauthenticated traffic; revisit only with a pre-auth coarse per-IP limiter (would break the per-user model if applied globally) |
| **S — forwarded-header spoofing** | `UseForwardedHeaders` at `:46` trusts `X-Forwarded-*`; a directly-exposed host lets clients mint remote IPs → poison rate-limit keys, geolocation, sign-in alerts | deployment topology: the API is expected behind the proxy/LB that owns those headers; loopback/probe exemptions in HTTPS redirect show the edge cases are curated (`:276-299`) | **accepted (topology trust)** — flag for deployment review: a host exposed without the proxy violates the assumption silently |
| **I — response-cache cross-tenant bleed** | `UseResponseCaching` (`:93`) with JWT + multi-tenant: a cached response keyed without user/tenant variance serves one principal's data to another (in-code warning at `:92`) | convention: only the reviewed anonymous surface (`AnonymousEndpointRegistry`, ~150 justified entries, CI-enforced both directions — `05-security-primitives.md` §6) may rely on shared caching | **covered by convention + registry** — residual: nothing mechanically forces `Vary` on any *new* cached authenticated endpoint |
| **S — error-page leakage** | a middleware above the exception funnel throwing a framework error | `ExceptionHandlingMiddleware` outermost (`:40`): RFC 7807 for everything, `HasStarted`-guarded per branch (`SK/Middlewares/ExceptionHandlingMiddleware.cs:26-85`, `:35-39`), internal detail stripped for 500s | **covered** |

## 5. Surface S4 — EntityBase soft-delete (deleted-data resurrection)

Assets: 364 `DeletedAt` columns across 550 tables (`02-entity-softdelete.md` §2). The soft-delete
contract is *the* deletion primitive most modules inherit; its threat is **query-path confusion**
between the tombstone state and the live view.

| Threat | Vector | Current control (mitigation) | Verdict |
|---|---|---|---|
| **I — tombstoned rows served as live data** | a read path over a soft-deletable table **without** a `HasQueryFilter` returns deleted rows: 364 `DeletedAt` columns vs **31** query-filter registrations (gap N7) — the difference is protected only by per-call-site `.Where(e => !e.IsDeleted)` discipline, and no inventory says which unfiltered reads are hand-protected | per-entity opt-in filters; two coexisting predicate styles compile to the same SQL (`02-entity-softdelete.md` §2); auth grant loads filter by hand as the cited example | **open (N7)** — the disclosure primitive: any forgotten filter is a deleted-data disclosure, and "was this read protected?" is unanswerable without a census |
| **E/T — resurrection into live uniqueness space** | `Restore()` is public and idempotent (`SK/Entities/EntityBase.cs:112-121`): any code path holding the entity resurrects it; meanwhile the `*_live` partial unique indexes (17, migration `20261102054159_AddSoftDeletePartialIndexesHotTables`) allowed a *new* live row with the same natural key to be created after the delete — restoring the old row now collides (unique violation) or, worse, on tables without `HasFilter` uniqueness, silently coexists | `SoftDelete()` refuses unpersisted entities and is the only sanctioned tombstone writer (`:93-107`); uniqueness-among-live-rows is the documented intent of `HasFilter` (`02-entity-softdelete.md` §3) | **open (NEW finding S4-a)** — restore-after-recreate is an integrity edge the kernel does not model: the collision fails closed on `*_live` tables but succeeds silently elsewhere |
| **E — deletion bypassed by direct `DeletedAt = null` writes** | importer or bulk path sets `DeletedAt` by hand, skipping the guards (`02-entity-softdelete.md` §3 rule 3) | convention only (rule 3); EF materialization setters are public | **accepted (convention)** — same trust class as any direct property write |
| **D — dead-row accumulation** | tombstones never age out at the kernel level; full indexes carry dead weight forever except the 17 `*_live` partials | partial indexes on hot paths; physical retirement is module-specific (outbox is the one kernel-level reaper, `03-outbox-core.md` §1 cleanup) | **partial (slow-burn)** — capacity planning, not security boundary |
| **R — deletion deniability** | soft delete keeps the row (good for forensics) but `Restore()` leaves no audit trail of the restore itself | neither `SoftDelete` nor `Restore` emits a domain event (`SK/Entities/EntityBase.cs:93-121` — they only `Touch()`); audit is whatever the *command* around them emitted | **open (NEW finding S4-b)** — an undelete performed by any handler is invisible in the event stream |

## 6. Surface S5 — DataProtection (key compromise blast radius)

Chain: env-only cert → validated at boot → encrypts the key ring → ring persisted via
`CertificateProtectedKeyRepository` into Postgres (`05-security-primitives.md` §4). The ring is
**shared across all replicas** (`SetApplicationName`, `Host/Core/Setup/DataProtectionStartupConfiguration.cs:36`)
and protects every DataProtection purpose the platform uses.

| Threat | Vector | Current control (mitigation) | Verdict |
|---|---|---|---|
| **I — certificate private-key compromise** | whoever holds `DATAPROTECTION_CERTIFICATE_KEY_BASE64` decrypts the entire ring (TB7): every protected purpose, every tenant, every replica — the blast radius is platform-wide by construction | env-only, fail-fast (missing vars refuse boot, `:53-58`); RSA ≥ 2048 + validity window enforced (`:66-72`); the key material never lives in config files or the repo | **accepted (single-ring design)** — the mitigation is operational custody of the env vars; there is no in-tree ring-rotation or key-compromise runbook surfaced in the kernel docs |
| **T — key-row tampering at rest** | DB-level attacker swaps `DataProtectionKeys` rows (injects own ring, or corrupts envelopes) | every read takes `FOR UPDATE` over all rows, converts legacy rows transactionally, and fails closed on any malformed envelope (`CryptographicException`, `Host/Core/Setup/CertificateProtectedKeyRepository.cs:44-86, 148-149`); `StoreElement` always wraps in xmlenc (`:88-96`) | **covered (fail-closed)** — tamper degrades to unprotect errors, not silent plaintext |
| **T — attacker with DB only (no cert)** | cannot forge decryptable ring rows; **can delete them** | no anti-deletion guard | **partial → DoS**: key-row deletion makes protected payloads unprotectable after ring regeneration; detectable (boot/read errors) but destructive — backups are the control |
| **D — certificate expiry = fleet boot outage** | validity window check (`:66-72`) refuses an expired cert for **every** environment including CI (`:32-34` comment: an unavailable key must never select plaintext) | deliberate fail-closed | **covered (by design)** — operational requirement: cert lifetime monitoring is load-bearing |
| **I — plaintext fallback** | misconfiguration selects unencrypted key storage | impossible by construction — the repository and certificate are unconditional (`:34-43`); no code path reaches an in-memory/default repository | **covered** |

## 7. Surface S6 — LogRedaction (what escapes)

`SK/Diagnostics/LogRedaction.cs` is the kernel's only redaction utility — and it is
**call-site discipline, not pipeline enforcement** (`05-security-primitives.md` §3): nothing
enriches or filters the log stream itself.

| Threat | Vector | Current control (mitigation) | Verdict |
|---|---|---|---|
| **I — unkeyed `RedactId` is offline-verifiable** | `RedactId` = first 8 hex chars of **unkeyed** SHA-256 (`:42-65`): deterministic across hosts and restarts (good for correlation), which means an attacker holding a candidate-ID list can *confirm* a `tid:`/`uid:` mapping offline by brute-forcing candidates — no secret is needed | 32-bit prefix over high-entropy GUID inputs makes confirmation feasible only against a *candidate set* (not discovery); emails/usernames use the keyed HMAC path instead (`:75-110`) | **accepted (documented)** — the residual is dictionary-confirmation against known-ID lists; do not use `RedactId` for low-entropy identifiers |
| **I — process-local keyed fingerprints** | `ProtectIdentifier`/`ProtectContact`/`MaskIpAddress` HMAC under a per-process 32-byte key (`:15`, `:184-188`) — fingerprints cannot be correlated across hosts or restarts | deliberate: cross-line correlation only within one process | **accepted (privacy > correlation)** — incident response must join via correlation ids, not fingerprints |
| **I — the redaction gap is everything not wrapped** | a missed call site logs raw identifiers — the utility cannot see or fix this; current reference users are few (exception middleware `:31,47,62`; the idempotency middleware is unwired, N2) | per-call-site discipline; `Sanitize` only neutralizes control characters (log forging, CWE-117 — `:210-218`), it does **not** redact content | **open (structural)** — the exception path itself passes `securityException.InternalMessage` through `Sanitize` only (`SK/Middlewares/ExceptionHandlingMiddleware.cs:36`): sanitized, not redacted |
| **I — exception text in durable stores** | handler failures persist `exception.ToString()` (see S2-b) and are also emitted at `LogError`/`LogWarning` with the raw exception (`OutboxDispatcher.cs:338-342, 374`) | none on these paths | **open (S2-b, same finding)** — logs and `inbox_receipts` share the exposure |
| **I — secrets** | hashing a low-entropy secret would let log readers test guesses | `RedactSecret` returns a constant, no fingerprint (`:195-203`, rationale in-doc) | **covered** |
| **S — log forging** | untrusted strings with control/line-separator chars forge log lines | `Sanitize` regex replaces them (`:220`) | **covered** (for call sites that use it) |

## 8. Cross-cutting — the foundation failure multiplier

| Threat | Why it is kernel-class | Control | Verdict |
|---|---|---|---|
| **Wiring drift defeats complete primitives** | N1 (dead `ObservabilityBehavior`), N2 (unwired `IdempotencyMiddleware`), N3 (un-armed order validator) — each primitive is individually sound; the *absence of its wiring* is the vulnerability class (no pipeline visibility, no generic idempotency, no order enforcement) | tracked as **#1248** (open); suggested ratchets in `07-gaps.md` | **open** — the meta-finding: this codebase's failure mode is not broken components but disconnected ones |
| **Module-local reimplementation of platform machinery** | five Economy outboxes + one academic queue (N5) inherit none of the retry/DLQ/replay guarantees audited above — every S2 mitigation silently does not apply to them | known design tension, first consolidated in `03-outbox-core.md` §2 | **open (N5)** |
| **One DI container = one trust domain** | every module registers behaviors, middleware, consumers and validators into the same container the kernel orchestrates from; TB3 is unfenced by any module boundary at runtime | module-isolation conventions (host-side composition, `06-event-bridge-registry.md` §1); review discipline | **accepted (architecture)** — the modular monolith's stated trade-off, recorded here so module threat models can cite it |

## 9. Threat-to-issue index and ratchets

| Finding | Where tracked | State |
|---|---|---|
| Poison event blocks aggregate FIFO (S2-a) | this document (NEW) | open — propose: exempt `DeadLetteredAtUtc IS NOT NULL` siblings in the FIFO subquery (`OutboxDispatcher.cs:80-87`) **or** document the admin replay dependency as the designed exit |
| `LastError` persists unredacted exception text (S2-b) | this document (NEW) | open — propose: redaction pass in `InboxStore.RecordFailureAsync` (`InboxStore.cs:106`) before truncation |
| Pipeline order not ratcheted (S1, = N6-adjacent) | `07-gaps.md` ratchet candidates 1 & 3 | open — G7-candidate |
| Observability/idempotency/order-validator unwired | **#1248** (N1, N2, N3) | open |
| CSV locale drift | #1248 (N4) | open |
| Hand-rolled outboxes | #1248 scope (N5) | open |
| Soft-delete filter inconsistency + no census (N7) | `07-gaps.md` N7 | open |
| Restore-after-recreate collision / silent undelete (S4-a/b) | this document (NEW) | open — small: emit a domain event from `Restore()`, document the recreate-then-restore rule next to `HasFilter` guidance |
| DataProtection ring rotation / custody runbook | this document (NEW) | open (operational) |
| `RedactId` dictionary-confirmation caveat | this document (NEW, guidance) | accepted once documented here |

Priority argument for the orchestrator: arm the tripwires before hardening the components —
N3 (one invocation) and the S1 order ratchet convert the two biggest "correct by inspection"
trust decisions into enforced ones; S2-a is the highest-impact runtime finding (aggregate-stream
freeze) because it converts a single poison row into a tenant-visible outage.
