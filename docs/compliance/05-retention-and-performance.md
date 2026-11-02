# Compliance Retention and Performance — 38 tables, purge workers, index coverage

> Module `GameGuild.Compliance.Audit` (plus Consent/FERPA/KYC stores) · Documented against `develop` commit `ac959fd8f`.
> Citation shorthand as in `01-user-flows.md`. All table mappings live in `Audit:/Configuration/AuditModelConfiguration.cs` (38 `ToTable` calls) unless noted.

## 1. The 38 tables, grouped by retention family

| Family | Tables | Write volume driver | Retention mechanism |
|--------|--------|--------------------|---------------------|
| **Central audit (hot)** | `AuditLogs` (:74), `TamperEvidentAuditLogs` (:107), `AuditDataAccessObservations` (:66) | every audited action platform-wide + every admin read of audit data | per-tenant `SecurityLogRetentionPolicy` → delete + archive pass (§2.1) |
| **Central audit (archive)** | `ArchivedAuditLogs` (:224) | retention archive moves (issue #193) | served for eDiscovery; no second-level purge defined |
| **Retention governance** | `SecurityLogRetentionPolicies` (:204), `SecurityLogRetentionExecutions` (:214), `AuditRetentionConfigurations` (:43), `AuditRetentionSimulationRuns` (:52) | low | append-only execution history |
| **Exports** | `ScheduledAuditExports` (:139), `AuditExportHistories` (:166) | cron schedule | files expire (`ExpireExpiredFilesAsync`); history rows kept |
| **Alerting** | `SecurityAlerts` (:183) | rule thresholds | no purge worker — manual lifecycle only |
| **PII redaction (issue #192)** | `PiiRedactionRules` (:244), `PiiTokenVaultEntries` (:260), `PiiRedactionOperations` (:274) | every redacted write | rules versioned; vault/token operations append |
| **Query DSL** | `SavedAuditQueries` (:286), `SavedAuditQueryRevisions` (:304) | admin usage | revisions deliberately survive view deletion (:300-301) |
| **Field-level (issue #186)** | `FieldAccessAudits` (:317), `SensitiveFieldClassifications` (:345), `FieldAccessAnomalies` (:359), `BulkExportEvents` (:374), `CrossBorderTransferLogs` (:388), `GdprProcessingActivities` (:403), `SensitiveDataRetentionPolicies` (:420) | every sensitive-field read/write | `EnforceSensitiveDataRetentionCommand` purge-or-mask pass (dry-run supported) |
| **Value snapshots (issue #176)** | `MutationValueSnapshots` (:433), `MutationValuePayloadChunks` (:457), `MutationValueSnapshotArchives` (:471), `ValueSnapshotRetentionPolicies` (:495), `ValueSnapshotRetentionExecutions` (:505), `ValueSnapshotRollbackPlans` (:513) | every mutation of every audited entity | `EnforceValueSnapshotRetentionCommand`: delete (hold-aware) + archive-tier stripping + dedup compaction |
| **Chain of custody (issue #177)** | `CustodyEvidenceItems` (:533), `CustodyEvents` (:549), `EvidenceCustodians` (:561), `EvidenceTransfers` (:572), `LegalHolds` (:588), `LegalHoldNotifications` (:603), `CustodyAccessLogs` (:616) | forensics ops | evidence **never** auto-purged; legal holds additionally suspend central-log deletion |
| **Compliance packaging** | `ComplianceEvidenceDocuments` (:11), `ComplianceSealedPackages` (:29) | evidence uploads / package builds | no purge worker — sealed packages are records |

Plus the sibling-module stores: Consent — `consent_policies`, `consent_policy_versions`, `user_consents`, `data_subject_requests` (`Consent:/Entities/ConsentEntities.cs:28,55,87,132`); FERPA — `ferpa_education_records`, `ferpa_directory_information_policies`, `ferpa_disclosure_consents`, `ferpa_disclosure_logs`, `ferpa_inspection_requests` (`Ferpa:/Entities/FerpaEntities.cs:49,87,118,169,201`); KYC — `user_kyc_verifications` (`Kyc:/Models/UserKycVerification.cs:8`), `compliance_sumsub_applicant_bindings`, `compliance_sumsub_webhook_inbox` (`Kyc:/SumSubEvidencePersistence.cs:43,70`).

**No Consent/FERPA/KYC table has any retention worker** — `RetentionUntil` (FERPA records, `FerpaEntities.cs:70`) and DSR deadlines are stored and never acted on. The audit module alone enforces retention.

## 2. Purge / archive workers in operation

### 2.1 Security-log retention (`SecurityLogRetentionBackgroundService`)

- Cadence: `RetentionEnforcementInterval` default 24 h, first pass 30 s after boot (`SecurityLogRetentionBackgroundService.cs:18-22`); per-tenant passes over all policies (`:45-51`); manual + dry-run via `POST audit/security-events/retention/enforce` (`SecurityEventController.cs:181`).
- Policy: per-tenant `RetentionDays` + per-category overrides JSON + `LegalHoldUntilUtc` + `ArchiveAfterDays`, revision concurrency-token (`SecurityLogRetentionService.cs:115-159`, `AuditModelConfiguration.cs:207`). Defaults: `DefaultRetentionDays` 400, batch 5000 (`SecurityEventPipelineOptions.cs:57,60`).
- Legal hold suspends **every** deletion and archive move for the tenant, while still recording the pass (`SecurityLogRetentionService.cs:176-190`).
- Policy changes are themselves captured as security events (`:144-156`).

### 2.2 Scheduled export expiry — `ScheduledAuditExportBackgroundService` (1-min poll) runs `ProcessDueAsync` + `ExpireExpiredFilesAsync` (`ScheduledAuditExportBackgroundService.cs:23-24`): files removed after the configured retention period, history retained.

### 2.3 Field-level + snapshot retention — on-demand only (`FieldLevelAuditController.cs:291`, `ValueSnapshotController.cs:196`), both dry-run capable; snapshot enforcement respects legal holds and does dedup compaction.

### 2.4 What does NOT purge: `SecurityAlerts`, custody family, packaging stores, all Consent/FERPA/KYC tables, `ArchivedAuditLogs` itself. Alert backlog growth is unbounded (§6).

## 3. Retention templates (what-if tier)

`AuditRetentionPolicyTemplates` (`Audit:/Constants/AuditRetentionPolicyTemplates.cs:105+`, validated at boot `AuditModule.cs:43`): `platform-baseline` 730 d retention / 90 hot / 180 warm / 365 cold (:121) with obligations audit-trail ≥400 d and security-evidence ≥730 d (:132-133); SOC 2 (:154), ISO 27001 (:181), plus GDPR, HIPAA, PCI DSS, FedRAMP presets with framework-sourced obligations. Tenants configure the real policy at `PUT audit/retention-simulation/configuration`; simulations (`POST audit/retention-simulation`) forecast deletions **without changing stored retention** (`AuditRetentionSimulationController.cs` doc :13-14).

## 4. Partitioning

**None.** No `PARTITION BY` exists in any migration or model configuration (grep across the module and `GameGuild.API/Database/Migrations`); the size lever is the hot/archive table split (`AuditLogs` → `ArchivedAuditLogs`) plus the retention batch cap (5000). For a platform writing an `AuditLogs` row per admin read (§6) plus every audited mutation, range partitioning on `CreatedAt` (or at minimum a BRIN index) is the obvious next step if volumes grow.

## 5. Index coverage on hot query paths

| Hot path | Query shape | Index | Verdict |
|----------|-------------|-------|---------|
| Admin list / date-range search | `TenantId + CreatedAt` filter, `CreatedAt DESC` (`AuditService.ApplyFilters :418-435` + ordering `:297-298`) | `ix (TenantId, CreatedAt)` (:95) | covered |
| Replay capture sweeps | `CreatedAt range + DeletedAt IS NULL` (`AuditReplayCaptureService.SelectAsync`, issue #1157 note :97-102) | partial `ix_audit_logs_created_tenant_live` on `(CreatedAt, TenantId) WHERE DeletedAt IS NULL` (:100-102) | covered — deliberate distinct-property second index |
| Export streams (CSV/JSON/XML) | same filter + paging (`ExportAuditLogsCommand` → `StreamAuditLogsAsync :311-346`) | composite above | covered; `Take` capped 1000 per page (`:440`) |
| Filter by action/resource/user | single-column indexes (:88-92) | covered |
| Correlation-id drill-down | `CorrelationId` (:89) | covered |
| DSL executor | filter trees over the same columns + `actor`/text (`AuditQueryDslExecutor`) | relies on the above; row-scan cap + timeout bound the worst case (`AuditModule.cs:188-223`) | bounded by config, not by indexes alone |
| Security alerts queue | `TenantId + Status + LastSeenAtUtc` (:199) | covered |
| Alert dedup | `DeduplicationKey` (:197) | covered |
| Retention candidate scan | `TenantId + CreatedAt` over category ranges | composite above; batched 5000 | covered |
| Archive re-entry idempotency | unique `(TenantId, SourceAuditLogId)` (:238) + `(TenantId, OriginalCreatedAt)` (:239) | covered |
| Tamper-evident chain append | `max(SequenceNumber) per TenantId` (`TamperEvidentAuditService.cs:105-110`) | unique `(TenantId, SequenceNumber)` (:131) | covered |
| Value snapshot search/history | `(TenantId, EntityName, KeyId, OccurredAtUtc)` (:448), `CorrelationId` (:450) | covered |
| Snapshot dedup | unique `(TenantId, ContentSha256)` on chunks (:462) | covered |
| KYC inbox dedupe | unique `ProviderEventId` (`SumSubEvidencePersistence.cs:81`), `(ApplicantId, IssuedAt)` (:82) | covered |
| KYC binding lookup | unique `(TenantId, SubjectHash)`, `ApplicantId`, `IdempotencyKeyHash` (:63-65) | covered |
| **Gaps** | `AuditLogs.SearchText` free-text predicate (`AuditLogSearchFilter.BuildPredicate :430`) | no trigram/full-text index on `Description` | sequential-filter inside the bounded scan — acceptable at current scale, first thing to degrade |
| | `statistics` endpoint | 7 separate `CountAsync` calls (`AuditController.cs:418-433`) | 7 round trips per dashboard hit; no cached/materialized counters |
| | KYC compliance report | `GetByDateRangeAsync` unbounded load + in-memory grouping (`KycService.cs:262-281`) | full-range materialization |

## 6. Growth loops worth flagging

1. **Every admin read of audit data writes audit data** — list, search, statistics, dashboard all call `LogAdminActionAsync` first (`AuditController.cs:95,187,298,412`; `SecurityAuditController.cs:67,99,128,158`). An analyst paging through the log generates a log row per page view — bounded only by the retention window.
2. **Export progress tracker is per-instance memory** (`DistributedAuditExportProgressTracker`, `AuditModule.cs:166`) — multi-instance deployments cannot see each other's export progress (`GET export/{id}/progress` 404s on the wrong instance).
3. **`AuditDataAccessObservations`** records batches of accessed (tenant, createdAt) pairs for every read (`AuditService.cs:303-307,324-345`) — another per-read write, retained under the same governance family.
4. **Spool files grow unbounded** during a DB outage (append-only JSONL, `SecurityEventFileSpool.cs:47-61`); no size cap, no rotation, no alert on depth (see `04-event-inventory.md` §3).
