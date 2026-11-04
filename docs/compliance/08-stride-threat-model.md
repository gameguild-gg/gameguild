# Compliance STRIDE Threat Model — regulatory grade

Status: living document, current as of 2026-11-04 against `origin/develop` (21b58ac6a7).
Supersedes the STRIDE-lite section of [`06-security-analysis.md`](06-security-analysis.md) (kept as the lite pass; this document is the full per-family model).

Companion documents: [`09-security-architecture.md`](09-security-architecture.md) for trust boundaries and the PII permission map, [`11-dfds.md`](11-dfds.md) for data flow diagrams, [`10-cross-module-sequences.md`](10-cross-module-sequences.md) for cross-module sequences.

Citation shorthand as in [`README.md`](README.md): `Audit:` `Consent:` `Ferpa:` `Kyc:` `API:`. All line numbers re-verified against 21b58ac6a7.

## 1. Methodology and verification note

STRIDE per flow family — Spoofing, Tampering, Repudiation, Information disclosure, Denial of service, Elevation of privilege — through a **regulatory lens**: each family is judged not only against an attacker but against what an auditor, a data-protection authority, or an AML examiner would ask the evidence to prove.

**Post-#1268 status (verified 2026-11-04)**: PR #1268 (`fix/compliance-consent-ferpa` — Consent DI wiring, FERPA ownership, deadline sweepers, server-side processor ids) is **OPEN and NOT merged** at develop 21b58ac6a7. At the documented tree, `AddConsentModule` (`Consent:/Extensions/DependencyInjection.cs:7-16`) still has zero host call sites (only `API:/Core/Setup/ApiProductComposition.cs:120` calls `AddFerpaModule`, and `:121` the KYC composition) — gap N1 remains LIVE, and the FERPA ownership/processor-id fixes it carries are not yet in effect. This document models the merged tree; where #1268 changes the answer, the row says "fixed by #1268 when it lands".

Flow families (match the DFDs in `11-dfds.md`):

| # | Family | Entry surface |
|---|--------|---------------|
| F1 | Audit event ingestion — sinks, security pipeline, spool | `Audit:/Services/SecurityEventLogger.cs`, sink fan-in |
| F2 | Audit query and export — DSL, CSV/JSON/XML, webhooks | `AuditController`, `AuditQueryController` |
| F3 | Retention, purge and legal hold | `SecurityLogRetentionBackgroundService` et al. |
| F4 | Tamper-evident chains, custody, packaging, replay | `TamperEvidentAuditService`, custody + replay controllers |
| F5 | Consent lifecycle — grant, withdraw, DSAR | `ConsentController` (dead at runtime, N1) |
| F6 | FERPA education records, disclosures, inspection | `FerpaController` |
| F7 | KYC legacy ledger and the sign-in gate | `Kyc:/Services/KycService.cs`, `KycAndHoldSignInCompliancePolicy` |
| F8 | KYC SumSub pipeline and the economy evidence tier | `EconomyKycController`, `SumSubKycAmlOrchestrator` |

## 2. F1 — Audit event ingestion

Entry: every producer sink of §2 in [`04-event-inventory.md`](04-event-inventory.md) — `IAuthenticationAuditEventSink`, `IApiKeyAuditEventSink`, SCIM, content/learning/teams, `IConsentAuditEventSink`, direct `IAuditService`, entity-change capture. Engine: `Audit:/Services/SecurityEventLogger.cs` (taxonomy → classify → 3 DB attempts → spool → drainer).

| Threat | Scenario | Mitigation or gap |
|--------|----------|-------------------|
| S — forged audit producer | In-process code impersonates a sink | Sinks are interfaces bound by the host (`Audit:/Extensions/AuditModule.cs:135-147`); only composed modules can reach them. Residual: any code inside the API process can call `IAuditService.LogAsync` directly and attribute the action to another actor id — the API trusts the caller-supplied actor (same class as N4) |
| T — event mutation in flight | Row edited between emit and persist | Events persist inside the producer's transaction where transactional capture is required (MFA events — `CentralAuthenticationAuditEventSink.cs:18-19,50-53`); central `AuditLogs` itself is NOT hash-chained (N14 — a DB-level `UPDATE` is only visible via `EntityChangeAudit` history) |
| R — "that security event never happened" | Denial of an alert-worthy action | Durable pipeline with spool replay idempotent by event id (`SecurityEventLogger.cs:125-129,285-291`); spool records survive restarts (`SecurityEventFileSpool.cs:14-18`). Gap: both-legs failure terminates in a critical log line, not a DLQ (N13) |
| I — PII leaks into durable tiers | Payload hits disk spool or logs unredacted | Both fallback tiers are PII-sanitized fail-closed before leaving the process (`SecurityEventLogger.cs:246-253` spool, `:263-282` critical log); `RedactRequestForDurableFallbackAsync` is itself fail-closed per its doc comment |
| D — spool flooding during a DB outage | Disk exhaustion via unbounded spool growth | No spool size cap and no depth alert (N13; `05-retention-and-performance.md` §4). The drainer stops on first still-failing delivery (`:139-141`) so it does not amplify load |
| E — spool poisoning | Attacker with filesystem access plants forged replay lines | Spool files are per-instance JSONL under the app's data directory; no integrity check (hash/signature) on spool records — a forged line replays as a real event. Mitigated only by the OS/process boundary; flagged as residual, no tracking issue yet |

**Regulatory verdict**: the ingestion tier meets the durability intent of #346 (nothing silently lost while the process lives), but an examiner asking "prove this record was not altered" gets an answer only for the opt-in tamper-evident tier (F4), not for `AuditLogs` (N14).

## 3. F2 — Audit query and export

Entry: `GET/POST v1/admin/audit-logs*` (`Audit:/Controllers/AuditController.cs`, 1116 lines), `v1/audit-queries` DSL (`AuditQueryController`), scheduled exports, export webhooks.

| Threat | Scenario | Mitigation or gap |
|--------|----------|-------------------|
| S — investigator identity spoofing | One admin performs reads attributed to another | Attribution derives from the authenticated context (`LogAdminActionAsync` on every read/search/export); `AuthenticationAuditEvent` rides the signed-JWT identity. Residual: `X-Forwarded-For` first value is the IP-of-record (`SecurityEventLogger.cs:340-342`) — spoofable unless the proxy overwrites it (K9) |
| T — export content tampering in transit | Row altered between DB and consumer | Scheduled-export artifacts carry file checksums (`AuditModelConfiguration.cs:164-179` history rows); compliance packages are sealed + signed with verification headers on download (`CompliancePackagingController.cs:72-92`); ad-hoc CSV/JSON exports carry no per-export digest — integrity rests on TLS |
| R — "we never exported that data" | Denial of a bulk PII export | `AuditExportHistories` attribute every scheduled run; ad-hoc exports are admin-audited; webhook deliveries carry `Idempotency-Key = exportId-status` (`AuditExportWebhookNotifier.cs:63-173`). Strong |
| I — export as PII firehose | 10k+ rows of IP/UA/session/metadata to one `Audit.Export` holder | Column whitelisting for CSV (`AuditController.cs:481-489`); bulk-export monitoring thresholds are advisory only — no hard block (gap, `06-security-analysis.md` §3.1). CSV export itself is invariant-culture correct (`AuditCsvExporter.cs:11-27`, `AuditController.cs:380,500-603` — the #1248 locale-drift class does NOT live in this module; the four drift files are Assets/AI/ProductMetrics, `docs/sharedkernel/07-gaps.md` N4) |
| I — DSL as a PII oracle | Crafted queries exfiltrate arbitrary columns | DSL executor is config-bounded (`MaxRowsScanned`, `MaxTimeoutSeconds`) and runs the same tenant clamp; unclassified free-form `Description`/`Metadata` still pass redaction (rule-set quality is tenant-administered) |
| D — export/DSL resource exhaustion | Unbounded scans or concurrent exports | `ExpensiveOperations` policy on the four export POSTs + scheduled-exports POST (`AuditController.cs:457,547,560,669`); listing/statistics/action-type export remain unthrottled (N12); DSL caps are the real backstop |
| E — cross-tenant export | Tenant A exports tenant B's audit rows | Three-layer model — policy + DAC permission + tenant clamp on every route; the known exception stays SystemAdmin fail-closed (K7, `SecurityAuditController.cs:80-87`) |

**Regulatory verdict**: the export machinery is what a data-protection authority would audit first; the tenant clamp and checksummed scheduled artifacts hold, while the advisory-only volume monitoring and the un-whitelisted JSON/XML legs are the findings to fix before a real DSAR volume arrives.

## 4. F3 — Retention, purge and legal hold

Entry: `SecurityLogRetentionBackgroundService` (`AuditModule.cs:125`, 24 h cadence), archive tier (#193), retention templates + simulation controller, legal holds honored by every purge worker (`05-retention-and-performance.md`).

| Threat | Scenario | Mitigation or gap |
|--------|----------|-------------------|
| S — forged hold | Row inserted claiming a hold to block lawful deletion | Hold rows are admin-audited like every other mutation; no separate permission gate beyond the audit-tier policy stack |
| T — purge deletes evidence under hold | Worker ignores holds under load | Holds are honored by every purge worker (verified per `05-retention-and-performance.md` §hold); execution-row history (`SecurityLogRetentionExecutions`) records what ran |
| R — "the record was deleted lawfully" | Deletion indistinguishable from malicious removal | Retention deletion is visible ONLY through execution history — a malicious `DELETE` and a lawful purge produce the same end state on `AuditLogs` (N14 corollary). The tamper-evident tier's chain makes gaps in ITS table detectable, not the central table's |
| I — archived PII outlives policy | Archive tier keeps what purge removed | Archive moves are part of the retention chain with their own execution rows; gap: archive destinations inherit the export firehose posture — no documented at-rest redaction of archived payloads |
| D — purge storm | Full-table sweep starves OLTP | Per-tenant passes with per-tenant failure isolation; batch sizes configurable (`05-retention-and-performance.md`) |
| E — retention template abuse | Tenant admin sets retention to zero to evaporate evidence | Templates are admin-surfaced and audited; no floor enforcement documented — flagged residual |

**Regulatory verdict**: the purge chain is honest about what it does (execution rows) but cannot prove what it did NOT do — the N14 chain gap is the single largest evidence-integrity exposure in the family.

## 5. F4 — Tamper evidence, custody, packaging, replay

Entry: `TamperEvidentAuditService` (`Audit:/Services/TamperEvidentAuditService.cs:95-159`), `ChainOfCustodyController`, `CompliancePackagingController`, `AuditReplayController`.

| Threat | Scenario | Mitigation or gap |
|--------|----------|-------------------|
| S — chain writer impersonation | Any writer appends to a tenant chain | Appends run through the module service with signing restricted to the key-holder; writer authorization follows the three-layer model. Residual: chain appends serialize per tenant (`:148-156` conflict retries) — documented throughput ceiling, not a spoofing vector |
| T — row edit breaking the chain | DB-level `UPDATE` of a `TamperEvidentAuditLogs` row | Content hash → chain hash(content, previousHash, sequence) → ECDSA signature in one transaction (`:95-159`); unique `(TenantId, SequenceNumber)` (`Configuration/AuditModelConfiguration.cs:131`); `AuditChainEvidenceVerifier.cs:10-40` recomputes both hashes and the signature (with the documented microsecond-tick recovery for pre-precision rows). This is the family's strong answer to N14 — but it covers ONLY this opt-in table |
| T — custody evidence substitution | Evidence file swapped after preserve | SHA-256 content hash at preserve time, re-verifiable on demand; append-only custody events with unique `(TenantId, EvidenceId, SequenceNumber)` (`AuditModelConfiguration.cs:555`); ECDSA challenge proofs on transfer |
| R — packaging dispute | Consumer denies receiving a package | Sealed + signed artifacts, checksum headers verified on download (`CompliancePackagingController.cs:72-92`); replay snapshots carry canonical-bytes SHA-256 revalidated on reconstruct (`AuditReplayController.cs:78,127-141`) with the deliberate provenance caveat documented (`:54-56`, K6) |
| I — key material exposure | Signing key leak lets an attacker forge chains | Versioned key options with active key id (`AuditModule.cs:160-175`, `AuditSigningOptions.cs`) — rotation preserves verification by key id; key custody is deployment-secret posture, not documented HSM |
| D — signature verification as CPU sink | Bulk verify requests | Verification endpoints sit behind the admin policy stack; no dedicated rate policy on the verification route (N12 family) |
| E — cross-tenant chain append | Tenant A extends tenant B's chain | Chain sequence keys are per-tenant; the tenant clamp applies on read/verify paths |

**Regulatory verdict**: this is the examiner-grade surface — per-tenant signed chains, custody sequence numbers, sealed packages. The structural weakness is scope, not strength: everything outside this opt-in tier (including every consent, FERPA and KYC mutation) has no tamper evidence at all.

## 6. F5 — Consent lifecycle

Entry: `api/compliance/consent` (`Consent:/Controllers/ConsentController.cs`) — **dead at runtime at develop 21b58ac6a7 (N1 live; #1268 open)**. Engine: `ConsentService` + `IConsentAuditEventSink` bridge.

| Threat | Scenario | Mitigation or gap |
|--------|----------|-------------------|
| S — anyone grants/revokes for anyone | No ownership check between caller and data subject | `ConsentController.cs:25-38` — bare `[Authorize]`, handlers take arbitrary `UserId`; consent rows are not proof of the data subject's action. Fixed by #1268's ownership checks when it lands |
| T — consent row edited | Flipping `IsGranted` server-side | Plain rows, no hash chain, no entity-change capture wired for these tables (the module predates the sink bridge); audit trail exists only via `IConsentAuditEventSink` events (which fail soft — `ConsentService.cs:141-162` swallows sink failures so the consent write proceeds) |
| R — withdrawal dispute | "I never withdrew" | IP/UA/method stored per row (`ConsentEntities.cs:110-119`) but client-controlled when the caller is not the subject; processor attribution on DSR completion is client-supplied `ProcessedByUserId` (`ConsentController.cs:44-46`, recorded at `ConsentService.cs:125-131`) — N4 |
| I — read-any-user consents | `GET users/{userId}` unscoped | Same bare-`[Authorize]` hole (N5 twin); consent metadata (policy content, timestamps) exposes the subject's relationship to sensitive policies (e.g. marketing, profiling) |
| D — re-grant bricking | Withdraw then re-grant → 500 | `GrantConsentAsync` falls through to `AddAsync` on a revoked row (`ConsentService.cs:53-67`) against the unique `(UserId, PolicyVersionId)` index (`ConsentEntities.cs:88`) — N2; GDPR-wise withdrawal must be reversible by the user |
| E — tenant-wide policy injection | Any user creates/versions policies | `POST policies`, `POST policies/{id}/versions` under the same bare `[Authorize]` — policy content is the consent oracle for the whole tenant |

**Regulatory verdict (at the merged tree)**: not deployable — the wiring gap (N1) means the API 500s before any of this matters; the underlying authorization shape (S/E rows) would fail a GDPR Article 7 review ("demonstrable consent") even once wired. #1268 addresses N1, N2 (re-grant path), N4 (server-side processor) and deadline sweepers; until merged these all stand.

## 7. F6 — FERPA education records

Entry: `api/compliance/ferpa` (`Ferpa:/Controllers/FerpaController.cs:13-63`), host-registered (`API:/Core/Setup/ApiProductComposition.cs:120`).

| Threat | Scenario | Mitigation or gap |
|--------|----------|-------------------|
| S — guardian impersonation | Caller asserts any guardian id on a disclosure consent | Guardian id is caller-supplied and unverifiable (`FerpaCommands.cs:29`, applied at `FerpaService.cs:81`) — N5 |
| T — disclosure log editing | Post-hoc edit of what was disclosed | Append-only disclosure logs with basis checks (`FerpaService.cs:110-123`) — no hash chain; the log is the module's own table, not the tamper-evident tier |
| R — basis dispute | "I disclosed under SchoolOfficial but no evidence exists" | Basis is caller-asserted with no evidence field (`FerpaService.cs:110-123`); inspection completion attribution is client-supplied `ProcessedByUserId` (`FerpaController.cs:59-61`) — N4 twin |
| I — any authenticated user reads any student's records | PII disclosure in the module that exists to prevent it | `GET students/{id}/records`, `/disclosures`, `/consents` all bare `[Authorize]` with zero scope checks in handlers (N5) — the headline FERPA failure |
| I — directory-information opt-out ignored | Opt-out policy stored, never consulted | `FerpaDirectoryInformationPolicy.OptOutEnabled` (`FerpaEntities.cs:93`) not read by the disclosure gate or directory read (`FerpaService.cs:51-71`) — N6 |
| D — deadline machinery decorative | 45-day inspection clock never enforced | `FerpaRequestStatus.Expired` unreachable (N3); late completions merely annotated |
| E — need-to-know inverted | Platform grants every authenticated user school-official access | FERPA's own regulation requires need-to-know; the module grants it to everyone (N5). #1268 adds ownership scoping — not merged |

**Regulatory verdict**: as merged, the FERPA module would fail its own statute — 34 CFR 99.30/99.31 require disclosure to school officials with legitimate educational interest, and the read surface grants the whole authenticated platform. The regulator-facing external parties (eligible student, guardian, school official, ED/FPCO examiner) are modeled in `11-dfds.md` §8.

## 8. F7 — KYC legacy ledger and the sign-in gate

Entry: no controller. Writers are internal service calls; readers are the sign-in compliance gate (`API:/Core/Compliance/KycAndHoldSignInCompliancePolicy.cs:53-90`) and economy payout eligibility (`Modules/GameGuild.Finance.Economy.Payouts/ComplianceKycPayoutEligibilitySource.cs:22-38`).

| Threat | Scenario | Mitigation or gap |
|--------|----------|-------------------|
| S — no direct spoofing surface (no controller) | — | The ledger is only reachable in-process; spoofing reduces to E on the callers |
| T — arbitrary status writes | `Rejected → Approved` with no transition table | `UpdateVerificationStatusAsync` (`KycService.cs:70-72`) and `ProcessProviderWebhookAsync` (`:232-241`) assign any enum value — N9; both consumers key deny/challenge and payout eligibility off `Approved` (`KycAndHoldSignInCompliancePolicy.cs:68-77`, `ComplianceKycPayoutEligibilitySource.cs:36-37`) so a regressed row silently flips access and money |
| T — evidence destruction | Hard delete of a verification row | `DeleteKycVerificationHandler` (`KycHandlers.cs:139-152`) — `DeleteAsync`, no audit event, no soft delete — N10 |
| R — provider webhook attribution | Legacy ledger's `ProcessProviderWebhookAsync` trusts caller-supplied `providerData` jsonb | No signature verification on this path (contrast F8); repudiation defense rests entirely on the SumSub tier |
| I — `ProviderData` jsonb unredacted | Raw provider payload, full-row internal reads | `Kyc:/Models/UserKycVerification.cs:73-74`; `GetVerificationsByUserId` returns full rows to internal callers; compliance-report endpoint surfaces aggregates only (`KycService.cs:266-272`) |
| D — stub upload path | `UploadDocumentAsync` discards the stream | `KycService.cs:191-204` (`// PLANNED`) returns a fabricated `kyc-documents/...` URL and mutates `DocumentTypes` — N11; callers believe evidence exists |
| E — dead `Expired` status falsifies reporting | Report says 0 expired forever | `KycVerificationStatus.Expired` never assigned by any writer (N15); `ExpiredVerifications` permanently 0 (`KycService.cs:270`) while consumers derive expiry from `ExpiresAt` (`KycAndHoldSignInCompliancePolicy.cs:83-86` documents the split) |

**Regulatory verdict**: an AML examiner reading this ledger finds no transition control, no deletion trail, and a stubbed document rail — the sign-in gate consumes it anyway (K8: disabled until `Authentication:Compliance:SignInGate` is enabled, `ApiProductComposition.cs:120-125` region). Two-ledger divergence N7 stays the structural finding — see `10-cross-module-sequences.md` §6.

## 9. F8 — KYC SumSub pipeline and the economy evidence tier

Entry: `POST api/v{version}/integrations/economy/sumsub/webhook` (`API:/Core/Controllers/EconomyKycController.cs:121-143` region, `[AllowAnonymous]`) and the self-scoped applicant endpoints (`TryActor`, `:100-110`). Engine: `Kyc:/SumSubKycAmlOrchestrator.cs`.

| Threat | Scenario | Mitigation or gap |
|--------|----------|-------------------|
| S — forged webhook | Unsigned POST mints an approval | HMAC digest + issued-at tolerance (`SumSubKycAmlProvider.cs:47-52`, tolerance `SumSubKycAmlOptions.WebhookTolerance`), fail-closed ingest; raw payload archived to S3 BEFORE state change (`SumSubKycAmlOrchestrator.cs:158-159`, store unavailability fails closed via `KycModule.cs:37-45`) |
| T — replay with different payload | Same event id, edited body | Inbox dedupe by `ProviderEventId`; different payload hash → `KycEvidenceConflictException` (`SumSubKycAmlOrchestrator.cs:161-178`); out-of-order events deferred not dropped (`:199-205`) |
| R — provider decision dispute | "SumSub never approved me" | Versioned evidence envelopes with policy version, payload hash, signature flag, jurisdiction (`:268-293`), published to the economy compliance ledger via `EconomyKycEvidenceStore` (`API:/Core/Integration/EconomyKycEvidenceStore.cs:7-22`) — the strongest repudiation defense in the family |
| I — webhook logs leaking PII | Rejection reasons/payloads in logs | Rejections recorded with machine reasons only (`invalid-signature`/`unknown-applicant`/`invalid-timestamp`, `:189-198`); raw payloads live in S3 + inbox, not logs |
| D — anonymous unthrottled signature CPU | Bulk garbage digests | No `Webhook` rate-limit policy on the anonymous endpoint (N8) — contrast the Billing webhook ingress which pairs `[AllowAnonymous]` with `RateLimitPolicies.Webhook`; signature verification + S3 writes are unthrottled |
| E — cross-subject evidence reads | Applicant A reads applicant B | Self-service surface derives subject from `ActorContext` (`EconomyKycController.cs:100-110`); evidence reads are id-scoped — correct |

**Regulatory verdict**: the SumSub tier is what an AML examiner wants — signed ingress, archived raw payloads, versioned decisions, jurisdiction pinning on approvals (`:295-324`). The finding is N7: this tier and F7's legacy ledger never reconcile; approval here is invisible to the sign-in gate, and legacy approval is invisible to payout authorization (which reads the legacy ledger through `ComplianceKycPayoutEligibilitySource` — see `10-cross-module-sequences.md` §6 for the exact divergence points).

## 10. Summary matrix

| Family | S | T | R | I | D | E | Regulatory blocker |
|--------|---|---|---|---|---|---|---------------------|
| F1 ingestion | ok | gap N14 | strong | strong | gap N13 | residual spool poison | central table not tamper-evident |
| F2 query/export | residual K9 | partial | strong | gap volume-block | gap N12 | strong | export firehose advisory-only |
| F3 retention | ok | strong | gap N14-c | residual archive | ok | residual floor | deletion vs purge indistinguishable |
| F4 tamper tier | ok | **strong** | strong | ok | residual | ok | scope = opt-in table only |
| F5 consent | gap N5 | gap | gap N4 | gap N5 | gap N2 | gap N5 | not deployable — N1 live, #1268 open |
| F6 FERPA | gap guardian | partial | gap N4 | **gap N5/N6** | gap N3 | **gap N5** | need-to-know inverted |
| F7 KYC legacy | n/a | **gap N9/N10** | weak | gap ProviderData | gap N11 | gap N15 | no transition control |
| F8 SumSub | strong | strong | **strong** | ok | gap N8 | strong | divergence N7 |
