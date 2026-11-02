# Compliance Security Analysis — STRIDE-lite over the regulatory surfaces

> Modules `GameGuild.Compliance.Audit` / `Consent` / `FERPA` / `KYC` · Documented against `develop` commit `ac959fd8f`.
> Citation shorthand as in `01-user-flows.md`. Companion gap register: `07-gaps.md`.

## 1. Tamper evidence (T — tampering)

**What exists:**

- **Per-tenant signed hash chains** for high-risk events: content hash → chain hash(content, previousHash, sequence) → ECDSA signature over the chain hash, all inside one transaction with 3 conflict retries (`Audit:/Services/TamperEvidentAuditService.cs:95-159`); unique `(TenantId, SequenceNumber)` prevents sequence collisions at the DB level (`Configuration/AuditModelConfiguration.cs:131`); verification recomputes both hashes and the signature, with a documented microsecond-tick recovery for pre-precision-fix rows (`Services/AuditChainEvidenceVerifier.cs:10-40`).
- **Key management**: `EcdsaCryptographicSigningService` with active key id + versioned key options from configuration (`AuditModule.cs:160-175`, `Services/AuditSigningOptions.cs`) — signature survival across key rotation is by key id.
- **Custody evidence**: SHA-256 content hash at preserve time, re-verifiable on demand; append-only custody events with unique `(TenantId, EvidenceId, SequenceNumber)`; ECDSA-challenge transfer proofs (`Controllers/ChainOfCustodyController.cs`, `Configuration/AuditModelConfiguration.cs:555`).
- **Compliance packages**: sealed + signed artifacts with verification and checksum headers on download (`Controllers/CompliancePackagingController.cs:72-92`).
- **Replay snapshots**: canonical bytes, SHA-256 digest in header, revalidated on reconstruct (`Controllers/AuditReplayController.cs:78,127-141`).
- **SumSub inbox**: replay-with-different-payload → conflict; raw payloads archived to S3 pre-processing (`Kyc:/SumSubKycAmlOrchestrator.cs:158-178`).

**Residual exposure:** the *central* `AuditLogs` table (the one everything writes to) is **not** hash-chained — only `TamperEvidentAuditLogs` (a separate opt-in surface for admin/high-risk events) is. A DB-level `UPDATE` of `AuditLogs` rows is detectable only via change-capture history (`EntityChangeAudit`), not cryptographically. Retention deletion and archive moves are indistinguishable from malicious deletion except through execution-row history. Soft-delete (`DeletedAt`) exists on `AuditLog` but no query excludes soft-deleted rows by default in the list paths (the replay sweep index does, `AuditModelConfiguration.cs:100-102`) — deleted-row visibility differs per surface.

## 2. Repudiation defenses (R)

**Strong in the audit tier:** every admin read/search/export writes an attribution row (`IAuditService.LogAdminActionAsync`); PII token resolution requires a reason and audits at critical risk (`PiiRedactionPipelineController.cs:139-155`); alert acknowledge/resolve derives the actor from the request context (`SecurityEventController.cs` command handlers); retained immutable `SavedAuditQueryRevisions` show who changed which investigator view; `AuditExportHistories` attribute every scheduled export run; custody access logging covers every evidence read.

**Weak spots:**

1. **Consent/FERPA processor attribution is client-supplied** — `ProcessRequestBody.ProcessedByUserId` (`Consent:/Controllers/ConsentController.cs:44-46`) and `CompleteFerpaInspectionRequestBody.ProcessedByUserId` (`Ferpa:/Controllers/FerpaController.cs:59-61`) let the caller name anyone as the processor. The audit events then record that name (`ConsentService.cs:125-131`). Any dispute ("I never processed that erasure request") is unresolvable.
2. **Consent grant evidence is thin** — IP/user-agent/method stored on the row (`ConsentEntities.cs:110-119`), but any authenticated caller can grant/revoke *for any user* (no ownership check, `ConsentController.cs:29-38`), so "consent" rows are not proof of the data subject's action.
3. **`X-Forwarded-For` trusted blindly** for the IP-of-record (`Audit:/Services/SecurityEventLogger.cs:340-342`) — spoofable unless a trusted proxy overwrites it; exports and audit rows inherit the spoofed value.

## 3. Information disclosure (I) — PII exposure in exports and logs

**Defenses:**

- Central PII redaction pipeline (issue #192) intercepts `AuditLogs` writes at `SaveChanges` (`AuditLogPiiRedactionInterceptor`), with a deterministic HMAC tokenizer and an encrypted token vault for authorized unmasking (`AuditModule.cs:102-110`); redaction is fail-closed — protectable fields are replaced wholesale if the pipeline fails (`AuditService.cs:80-84` doc, `PiiRedactionService`).
- Both durable-fallback tiers (critical log payload, spool payload) are sanitized before leaving the process boundary (`SecurityEventLogger.cs:246-253,263-282`).
- Entity-change capture is redacted through the same pipeline (module doc `EntityChangeAuditController.cs:11-14`).
- Exports: tenant clamp on every export route; bulk-export monitoring thresholds watch export volume (`FieldLevelAuditController.cs:144-149`); export column whitelisting for CSV (`AuditController.cs:481-489`).

**Exposure risks:**

1. **Exports are PII firehoses by design** — a CSV/JSON/XML export of `AuditLogs` streams IP addresses, user agents, session ids, descriptions and metadata for up to 10k+ rows to anyone holding `Audit.Export` (`AuditController.cs:453-645`). Bulk-export monitoring exists but is advisory; the export is not blocked at any threshold (413 only for the action-type 10k cap and bulk-evaluate gate, which operators can bypass by using the main export route).
2. **Consent module reads any user's consents** (`GET api/compliance/consent/users/{userId}`) and FERPA reads any student's records + disclosure logs — no ownership or tenant scoping (`07-gaps.md` N4/N5); these are the modules whose *purpose* is privacy.
3. **`UserAgent` and full `Metadata` JSON** ride along in every export and every admin list — `Metadata` is producer-controlled free-form JSON; redaction rules target known patterns, and unclassified identifiers (e.g., internal ids, free-text `Description`) pass through. The rule set is tenant-administrable, so a tenant with weak rules exports weakly-redacted data.
4. **KYC `ProviderData` jsonb** (`Kyc:/Models/UserKycVerification.cs:73-74`) stores raw provider payload with no documented redaction; the legacy compliance-report endpoint surfaces per-country aggregates only, but `GetVerificationsByUserId` returns full rows to any internal caller.

## 5. Spoofing (S) / elevation surfaces

- SumSub webhook: HMAC digest + issued-at tolerance + inbox replay guard; `[AllowAnonymous]` is acceptable *because* the signature gate is fail-closed — but the endpoint lacks a rate-limit policy, so signature-verification CPU is an unthrottled anonymous surface (`07-gaps.md` N8).
- Export webhooks (egress): exact-host allow-list + HTTPS + default port + no query/userinfo/fragment + HMAC with ≥32-byte secret + idempotency key (`AuditExportWebhookNotifier.cs:63-119`) — solid SSRF posture; DNS-rebinding is the residual theoretical vector (host allow-list checked at validation time, `HttpClient` re-resolves at send time).
- FERPA disclosure basis is caller-asserted (`FerpaService.cs:110-123`) — a caller asserting `CourtOrder` or `SchoolOfficial` bypasses the consent requirement with no evidence field; the *log* is honest about the basis but the basis is unverifiable.

## 6. Denial of service (D)

- Rate limiting is present on the sub-surface controllers (`RateLimitPolicies.Api`) and on the heavy export POSTs (`ExpensiveOperations`), but absent on the main listing/DSL/statistics controllers and on Consent/FERPA/KYC surfaces (N8/N12) — the DSL executor is config-bounded (`MaxRowsScanned`, `MaxTimeoutSeconds`), which is the real DoS backstop there.
- Spool has no size cap (§4 of `05-retention-and-performance.md`).
- Tamper-evident chain appends serialize per tenant (conflict retries, `TamperEvidentAuditService.cs:148-156`) — a hot tenant's high-risk write throughput is bounded by chain contention; documented behavior, worth monitoring.

## 7. Elevation of privilege (E)

- The audit tier's three-layer model (policy + DAC + tenant clamp) is genuinely defense-in-depth; `SecurityAuditController.authentication` deliberately stays SystemAdmin because the underlying query has no tenant filter (remarks `:80-87`) — a correct fail-closed trade-off.
- The Consent/FERPA controllers invert the platform's own standard: policy-less `[Authorize]` means any authenticated user holds effective `consent:admin` and `ferpa:admin` (N4/N5). FERPA's own regulation requires need-to-know; the module grants it to everyone.
- `EconomyKycController` correctly self-scopes (`TryActor`, `:100-110`); no cross-subject KYC reads are possible through it.

## 8. Summary verdict per module

| Module | Tamper evidence | Repudiation defense | PII posture | Overall |
|--------|----------------|--------------------|-------------|---------|
| Audit | strong (chains + custody + packages) minus the unchained central table | strong (attribution everywhere) | good pipeline, heavy export firehose | production-grade with known sharp edges |
| Consent | none (plain rows) | weak (client-supplied attribution, grant-anyone) | stores IP/UA per consent row, fine; read-any-user hole | **not deployable as-is** (also dead wiring, N1) |
| FERPA | none (append-only logs only) | weak (same shape as Consent) | read-any-student hole in the module that exists to prevent it | needs an authorization layer before any real deployment |
| KYC | SumSub tier solid (signed evidence, versioned, jurisdiction-pinned); legacy ledger weak (arbitrary status writes, hard deletes) | medium (provider-signed vs client-asserted split) | jsonb provider data unredacted | two-ledger split is the structural risk (N7) |
