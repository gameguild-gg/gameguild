# Compliance Gaps — known issues vs NEW findings

> Modules `GameGuild.Compliance.Audit` / `Consent` / `FERPA` / `KYC` · Documented against `develop` commit `ac959fd8f`.
> Citation shorthand as in `01-user-flows.md`.

## Known issues (already tracked, delivered, or deliberately accepted)

| # | Issue | Evidence | Status |
|---|-------|----------|--------|
| K1 | Compliance-family documentation task | issue #1072 (open) — this package delivers its Audit/Consent/FERPA/KYC half; `Compliance.FinancialCrime` remains undocumented | in progress |
| K2 | **Payment money events are not projected into Compliance.Audit** — refund/chargeback/success audits stay in the module-local `audit_trails` table; only Billing webhook-secret rotation reaches the central module | `docs/commerce/payments/06-gaps.md` N15; `Billing:/Commands/RotateWebhookSecret/RotateWebhookSecretCommand.cs:126-141` | known from the payments package; the consumer side is documented in `04-event-inventory.md` §6 |
| K3 | Wiring-gap guard program for exists-but-unwired artifacts | issue #1184 (merged guards work) — the Consent DI gap N1 below is a live instance its guards did not cover | known mechanism, instance is NEW (N1) |
| K4 | Durable audit logging requirement | issue #346 — delivered via the security-event pipeline + spool (`Audit:/Services/SecurityEventLogger.cs`) | delivered |
| K5 | Feature issues that produced this surface: entity-change audit #166, replay #169, value snapshots #176, chain of custody #177, coverage sweeps #180/#181, module sinks #180/#181/#184, field-level family #186, PII redaction #192, archive tier #193, tenant clamp #294, query-operations DAC #375, replay gating #386, sign-in gate #267 | module doc comments throughout | delivered |
| K6 | Replay SHA-256 "detects content changes; does not certify event provenance or synchronize source clocks" | deliberate, documented at `Audit:/Controllers/AuditReplayController.cs:54-56,123-125` | accepted |
| K7 | `GET admin/security-audit/authentication` stays SystemAdmin because the underlying query has no tenant filter | deliberate fail-closed, documented at `SecurityAuditController.cs:80-87` | accepted pending a tenant filter |
| K8 | KYC sign-in gate disabled until `Authentication:Compliance:SignInGate` is enabled | `API:/Core/Setup/ApiProductComposition.cs:120-125` comment | deliberate rollout control |
| K9 | `X-Forwarded-For` trusted as the IP-of-record (first value) | `Audit:/Services/SecurityEventLogger.cs:340-342` — platform-wide pattern behind a trusted proxy | accepted assumption; flagged in `06-security-analysis.md` §2 |

## NEW gaps found by this audit

### Availability / wiring

**N1 — The Consent module's service layer is never registered: every `api/compliance/consent` endpoint is dead at runtime.**
`AddConsentModule` (`Consent:/Extensions/DependencyInjection.cs:7-16`) registers `IConsentService` and the four repositories — and has **zero call sites** in the host (repo-wide grep: only `tests/GameGuild.Compliance.Consent.UnitTests/ConsentCoverageCompletionTests.cs:269` calls it, in isolation). The module *is* enabled for CQRS handler discovery (`API:/Core/Setup/ModuleConfiguration.cs:23`), so the handlers exist, but resolving any of them fails on `IConsentService` → every controller action 500s with a DI resolution error. FERPA and KYC get explicit host registration (`API:/Core/Setup/ApiProductComposition.cs:115-116`); Consent was missed. The DI-wiring unit test passes because it builds its own service collection — it never verifies the host's.

**N2 — Re-granting a revoked consent is a guaranteed unique-constraint failure.**
`GrantConsentAsync` (`Consent:/Services/ConsentService.cs:53-67`) returns early only when the existing row `IsGranted`; when the user previously revoked, it falls through to `AddAsync` on a *new* `UserConsent` row for the same `(UserId, PolicyVersionId)` — which the schema makes unique (`Entities/ConsentEntities.cs:88`). The DB throws, the service has no catch, and the API returns 500. There is no re-consent path at all: a user who withdraws consent can never grant it again without manual DB surgery.

**N3 — Statutory deadline machinery is decorative: `Expired` is unreachable in both the GDPR and FERPA request machines.**
Consent DSRs store a 30-day deadline (`ConsentEntities.cs:106`) and FERPA inspection requests a 45-day deadline (`Ferpa_` `FerpaEntities.cs:216`), but no worker, command, or query ever assigns `DataSubjectRequestStatus.Expired` (`:198`) or `FerpaRequestStatus.Expired` (`:46`) — grep finds no assignment sites. Late completions are merely annotated (`OnTime` metadata, `ConsentService.cs:131`); nothing escalates, reports, or auto-closes overdue requests. `GET data-subject-requests/pending` in the audit tier (`FieldLevelAuditController.cs:236`) is a separate surface and also does no deadline enforcement.

### Authorization / repudiation

**N4 — Consent and FERPA mutations take the acting user from the request body.**
`ProcessRequestBody.ProcessedByUserId` (`ConsentController.cs:44-46`) and `CompleteFerpaInspectionRequestBody.ProcessedByUserId` (`FerpaController.cs:59-61`) let the client name any user as the processor; the audit events then permanently record that attribution (`ConsentService.cs:125-131`). The security-alert endpoints in the same family correctly derive the actor from context — the pattern exists and was not applied here. Additionally, completing an already-completed DSR silently overwrites the previous processor's attribution (`ConsentService.cs:123-124` — no state guard, unlike the FERPA entity which at least throws).

**N5 — FERPA's read/write surfaces have no tenant or ownership scoping: any authenticated user can act on any student.**
`GET students/{id}/records`, `GET students/{id}/disclosures`, `POST consents`, `POST inspection-requests/{id}/complete`, `PUT directory-policy` — all bare `[Authorize]` (`FerpaController.cs:13-63`) with the handlers applying zero scope checks (`Ferpa:/Services/FerpaService.cs` end-to-end). The consent controller has the same shape (`ConsentController.cs:25-38` — any user reads any user's consents, revokes them, creates tenant-wide policies). The guardian on a disclosure consent is an unverifiable caller-supplied id (`FerpaCommands.cs:29`, `FerpaService.cs:81`), and the disclosure basis (`CourtOrder`, `SchoolOfficial`, …) is caller-asserted with no evidence field (`FerpaService.cs:110-123`). Guard failures surface as unhandled 500s — the FERPA controller has no exception mapping at all.

**N6 — The FERPA directory-information opt-out policy is stored and never consulted.**
`FerpaDirectoryInformationPolicy.OptOutEnabled` exists (`FerpaEntities.cs:93`) with an annual-notice timestamp and upsert surface (`FerpaService.cs:51-71`), but neither the disclosure gate nor the directory-information read filters against it — a student's directory info is disclosed and logged regardless of the tenant's opt-out regime.

### KYC

**N7 — Two KYC ledgers with no bridge.**
The legacy ledger (`user_kyc_verifications`, `Kyc:/Services/KycService.cs`) is read by the sign-in compliance gate (`API:/Core/Compliance/KycAndHoldSignInCompliancePolicy.cs:53`) and payout eligibility (`Modules/GameGuild.Finance.Economy.Payouts/ComplianceKycPayoutEligibilitySource.cs:26`); the SumSub pipeline maintains its own bindings and publishes signed evidence to the economy compliance ledger (`Kyc:/SumSubKycAmlOrchestrator.cs:268-293`, read at `API:/Core/Controllers/EconomyKycController.cs:83`). Nothing synchronizes them: a user approved by SumSub is invisible to the sign-in gate; a user approved in the legacy ledger is invisible to the economy tier. `UpdateVerificationStatusAsync`'s only writers are internal service calls (no controller), so in practice the legacy ledger is nearly write-orphaned — but still the source of truth for sign-in deny/challenge decisions.

**N8 — Consent, FERPA, the KYC surfaces and the SumSub webhook are unthrottled.**
No `EnableRateLimiting` anywhere in `ConsentController` / `FerpaController` (`[Authorize]` only), none on `EconomyKycController`, and the **anonymous** SumSub webhook (`EconomyKycController.cs:121-123`) runs signature verification and S3 writes with no `Webhook`-style policy — unlike the Billing webhook ingress which pairs `[AllowAnonymous]` with `RateLimitPolicies.Webhook` + a source-security filter.

**N9 — Legacy KYC status writes have no transition table.**
`UpdateVerificationStatusAsync` (`KycService.cs:70-72`) and `ProcessProviderWebhookAsync` (`:232-241`) assign any enum value: `Rejected → Approved`, `Approved → Pending`, anything. The two consumers key behavioral decisions off `Approved` (`KycAndHoldSignInCompliancePolicy.cs:68-77`, `ComplianceKycPayoutEligibilitySource.cs:36-37`), so a regressed row silently flips a user's access. Contrast the SumSub tier, which versions every state change and guards ordering (`SumSubKycAmlOrchestrator.cs:199-227`).

**N10 — KYC verification rows can be hard-deleted without any audit trail.**
`DeleteKycVerificationHandler` (`Kyc:/Handlers/KycHandlers.cs:139-152`) issues a repository `DeleteAsync` — no audit event, no soft delete, no guard. No controller reaches it today, but the handler is registered; any future caller inherits an evidence-destruction primitive.

**N11 — KYC document upload is a stub that discards the document.**
`UploadDocumentAsync` (`KycService.cs:176-214`) never reads `documentStream`; the `// PLANNED` comment (`:191`) confirms no storage backend, yet the method returns a fabricated `kyc-documents/{id}/{file}` URL and mutates `DocumentTypes` as if a document existed. Callers believe evidence was uploaded.

### Audit tier

**N12 — Rate-limit inconsistencies on exactly the expensive audit surfaces.**
Unthrottled: `AuditController` list/search/statistics/progress/history/download actions (class has no policy; only the four export POSTs + scheduled-exports POST carry `ExpensiveOperations`, `AuditController.cs:457,547,560,669`), the whole `AuditQueryController` (DSL scans bounded only by config), the action-type **export** (`:329` — a sibling of two ExpensiveOperations routes), `SecurityAuditController:export` (`:175`), and `SecurityAuditController` reads. The DSL executor caps are the only DoS backstop on the query tier.

**N13 — No DLQ or monitoring for the terminal security-event capture outcomes.**
When both persistence and spooling fail (`SpoolingDisabled`, `SecurityEventLogger.cs:275-282`) or a spool record is unparseable (`:120-122`), the event exists only as a critical log line or a stuck file — nothing alarms, no replay endpoint exists, and the spool itself grows unbounded during an outage with no depth alert (`delivery-status` exposes stats, `SecurityEventController.cs:44-48`, but no rule watches them).

**N14 — The central `AuditLogs` table is not tamper-evident.**
Only the separate `TamperEvidentAuditLogs` surface carries hash chains + ECDSA signatures (`TamperEvidentAuditService.cs:95-159`); the table every sink in the platform writes to relies on ordinary row integrity. A privileged DB actor can `UPDATE`/`DELETE` `AuditLogs` with no cryptographic trace (retention deletion is equally indistinguishable — visible only through `SecurityLogRetentionExecutions` history).

**N15 — Dead expiry states falsify KYC reporting.**
No writer ever assigns `KycVerificationStatus.Expired` (`KycVerificationStatus.cs:23`) or `KycAmlState.Expired` (`SumSubKycAmlProvider.cs:20`) — expiry is purely consumer-side (`ExpiresAt` checks). Consequently `ExpiredVerifications` in the compliance report is permanently 0 (`KycService.cs:270`), understating lapse exposure; the same "status says Approved, consumers say expired" split is documented as deliberate at `KycAndHoldSignInCompliancePolicy.cs:83-86`, but the report does not follow the consumers' derivation.

**N16 — Export progress is per-instance state.**
`DistributedAuditExportProgressTracker` is registered as a singleton (`AuditModule.cs:166`) with no shared store — behind more than one API instance, `GET export/{exportId}/progress` 404s unless it lands on the originating instance, and the webhook's final counts come from whichever instance ran the export.

## Priority suggestion

1. **N1** (one-line host registration) — the module is either dead on purpose (then remove it from enabled modules) or alive (then register it); today it is Schrödinger's module.
2. **N2** (re-grant path) — GDPR-wise, withdrawal must be reversible-by-user; today it permanently bricks consent.
3. **N5 + N4** — add the platform's standard policy/DAC/ownership layer to Consent/FERPA before any tenant uses them; derive actors from context.
4. **N9 + N10 + N11** — legacy KYC ledger hardening or an explicit deprecation in favor of the SumSub tier (N7 decision first).
5. **N12 + N8** — align rate limits with the module's own export conventions.
6. **N13, N14, N16** — observability and evidence hardening for the audit tier.
