# Compliance CQRS Wiring Matrix

> Modules `GameGuild.Compliance.Audit` / `Consent` / `FERPA` / `KYC` · Documented against `develop` commit `ac959fd8f`.
> Columns: route → command/query → validator → handler → auth → rate limit → unit test → integration test → HOLE?
> Citation shorthand: `Audit:` = `apps/api/Source/Modules/GameGuild.Compliance.Audit/`, `Consent:` = `apps/api/Source/Modules/GameGuild.Compliance.Consent/`, `Ferpa:` = `apps/api/Source/Modules/GameGuild.Compliance.FERPA/`, `Kyc:` = `apps/api/Source/Modules/GameGuild.Compliance.KYC/`, `API:` = `apps/api/Source/GameGuild.API/`. Unit tests under `apps/api/tests/GameGuild.Audit.UnitTests/` are cited by file name.

## Auth model (applies to the whole matrix)

Four different authorization postures coexist in this family — the asymmetry itself is a finding (`06-security-analysis.md` §2, `07-gaps.md` N4/N5):

1. **Audit tier (strong)**: controller `[Authorize]` + per-action policy (`Policies.AuditRead` = `Audit.Read`, `Policies.AuditExport` = `Audit.Export`, `Policies.SystemBackupRestore` = `System.BackupRestore`; constants at `Identity.Authorization/Policies.cs:194,197,207`) + query-operations DAC permissions (`RequireDacPermission(QueryPermission.Keys.List/Search/Query)`, issue #375) + `RequireSearchPermission` where free text is accepted + the issue #294 tenant clamp (`AuditController.cs:48-71`). Several sub-surfaces (security events, custody, packaging, field-level, PII, snapshots, retention simulation) instead lean on **service-layer guards** that throw `UnauthorizedAccessException` → 403.
2. **Consent tier**: bare `[Authorize]` (`ConsentController.cs:10`) — no policy, no DAC, no ownership check, no rate limit. Plus the module's service layer is not registered at all (gap N1).
3. **FERPA tier**: bare `[Authorize]` (`FerpaController.cs:10`) — same holes.
4. **KYC tier**: `EconomyKycController` `[Authorize]` + actor/tenant requirement (`API:/Core/Controllers/EconomyKycController.cs:26,100-110`); the SumSub webhook is `[AllowAnonymous]` with digest-header preconditions (`:121-131`).

**Validators: zero `AbstractValidator` classes exist in any of the four modules** (grep across `Modules/GameGuild.Compliance.*` returns none). Input validation is inline: `Validator.TryValidateObject` in `AuditController.ValidateRequest` (`AuditController.cs:960-977`), Accept-header parsing (`:791-820`), webhook-URL policy (`:822-836` + `AuditExportWebhookNotifier.cs:63-86`), fail-closed DSL parsing (`AuditQueryDslParser`), and exception-to-problem mapping per controller. The shared `ValidationBehavior` therefore never fires here; guards live in controllers/services. HOLE marks below flag where *neither* a validator nor an inline guard exists.

Rate limiting: the audit sub-surface controllers default to `RateLimitPolicies.Api` at class level; `AuditController`'s mutation-tier exports use `RateLimitPolicies.ExpensiveOperations`; **`AuditController` read actions, `AuditQueryController`, `SecurityAuditController`, `ConsentController`, `FerpaController`, `EconomyKycController` and the SumSub webhook carry no rate-limit attribute at all** (`07-gaps.md` N8/N12).

## AuditController — `v{version}/admin/audit-logs` (`Audit:/Controllers/AuditController.cs`, `[Authorize]` L24; tenant clamp L48-71)

| Route | Command / Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|-----------------|-----------|---------|------|------------|-----------|------------------|-------|
| GET `` (L76) | `AuditLogQuery` via `IAuditService` (L97-114) | — (Take clamped L111) | `AuditService.GetAuditLogsAsync` + count | AuditRead L77 + DAC list L78 + search perm L79 | **none** | `AuditTenantClampTests.cs`, `AuditLogQueryServiceTests.cs` | `AuditExportPostgreSqlHttpTests.cs` (shared surface) | unthrottled listing — N12 |
| GET `search/by-date-range` (L131) | `AuditLogQuery` + activity buckets (L194-212) | inline range validation L150-180 | `AuditService.GetAuditLogsAsync/CountAsync/GetAuditActivityAsync` | AuditRead L132 + DAC search L133 | **none** | `AuditDateRangeResolverTests.cs`, `AuditTenantClampTests.cs` | — | covered |
| POST `search-filter` (+`/api/audit/search-filter` L242) | `SearchAuditLogsWithConditionsCommand` (L266) | `AuditConditionTreeValidator` at translate time | `AuditConditionTreeTranslator` predicate path | AuditRead L243 + DAC query L244 | **none** | `AuditConditionTreeTests.cs` | — | covered |
| GET `search/by-action-type` (L274) | `SearchAuditActionTypesQuery` (L305) | TryValidateModel L283 | `AuditActionTypeSearchService` | AuditRead L275 + DAC search L276 | **none** | `AuditActionTypeSearchServiceTests.cs` | — | covered |
| GET `search/by-action-type/taxonomy` (L322) | static catalog | n/a | `AuditActionTypeTaxonomy.GetTaxonomy` L326 | AuditRead L323 + DAC list L324 | **none** | taxonomy covered via search tests | — | covered |
| GET `search/by-action-type/export` (L329) | `ExportAuditActionTypesQuery(request, 10_000)` (L362) | format whitelist L344-350 | `AuditActionTypeExportFormatter` (CSV/JSON/XML L382-392) | **AuditExport L330** | **none — sibling export routes are ExpensiveOperations** | `AuditActionTypeSearchServiceTests.cs` | — | **export without ExpensiveOperations policy rate limit** — N12 |
| GET `statistics` (L398) | 7× `GetAuditLogCountAsync` (L418-433) | — | `AuditService` | AuditRead L399 + DAC query L400 | **none** | `AuditReportServiceTests.cs` | — | 7 sequential counts per call — see `05` §5 |
| POST `:export` / `export/csv` (L453-454) | `ExportAuditLogsCommand` (L848) | `ValidateExportRequest` L478 (DataAnnotations + webhook URL) | `AuditExportCommandHandler` (`Commands/AuditExportCommands.cs:26`) | AuditExport L456 + tenant clamp L468 | **ExpensiveOperations L457** | `AuditExportProgressTrackerTests.cs`, `AuditLogExportBulkMonitoringTests.cs`, `AuditExportTransportContractTests.cs` | `AuditExportPostgreSqlHttpTests.cs` | covered |
| POST `export/json` (L544) | `ExportAuditLogsCommand` (L594) | shared | shared + `AuditJsonExportDocument` | AuditExport L546 | ExpensiveOperations L547 | `AuditExportTransportContractTests.cs` | `AuditExportPostgreSqlHttpTests.cs` | covered |
| POST `export/xml` (L557) | `ExportAuditLogsCommand` | shared | shared + `AuditXmlExporter.WriteDocumentAsync` L614 | AuditExport L559 | ExpensiveOperations L560 | `AuditXmlExporterTests.cs` | `AuditExportPostgreSqlHttpTests.cs` | covered |
| GET `export/{exportId}/progress` (L648) | `IAuditExportProgressTracker.GetAsync` L657 | — | tracker (in-memory per instance, `DistributedAuditExportProgressTracker`) | AuditExport L649 | **none** | `AuditExportProgressTrackerTests.cs` | — | covered |
| POST `scheduled-exports` (L667) | `CreateScheduledAuditExportCommand` (L690) | inline L685-686 + cron/tz exception mapping L695-703 | `ScheduledAuditExportService` | AuditExport L668 + tenant required L678 | ExpensiveOperations L669 | `ScheduledAuditExportServiceTests.cs`, `AuditExportCronScheduleTests.cs`, `AuditScheduledExportOptionsValidationTests.cs` | — | covered |
| GET `scheduled-exports` (L707) | `IScheduledAuditExportService.GetForTenantAsync` L720 | — | service | AuditExport L708 | **none** | `ScheduledAuditExportRepositoryTests.cs` | — | covered |
| DELETE `scheduled-exports/{exportId}` (L725) | `DisableScheduledAuditExportCommand` (L740) | — | service | AuditExport L726 | **none** | `ScheduledAuditExportServiceTests.cs` | — | covered |
| GET `scheduled-exports/{exportId}/history` (L747) | `GetHistoryAsync` L767 | — | service | AuditExport L748 | **none** | `ScheduledAuditExportRepositoryTests.cs` | — | covered |
| GET `scheduled-export-history/{historyId}/download` (L772) | `OpenDownloadAsync` L785 | — | service (range processing L788) | AuditExport L773 | **none** | — | — | **download unthrottled + no dedicated test found** |

## AuditQueryController — `v{version}/audit-queries` (`Audit:/Controllers/AuditQueryController.cs`, `[Authorize(Policy=AuditRead)]` L24)

| Route | Command / Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|-----------------|-----------|---------|------|------------|-----------|------------------|-------|
| POST `:execute` (L41) | `ExecuteAuditQueryCommand` (L57) | fail-closed DSL parser | `IAuditQueryDslExecutor` (row-scan cap + timeout) | class policy + DAC query L42 | **none** | `AuditQueryDslParserTests.cs`, `AuditQueryDslExecutorTests.cs`, `QueryDsl/AuditQueryControllerAuthorizationTests.cs` | — | covered |
| GET `:results` (L77) | text parse L104 → `AuditQueryDslExecutionRequest` (L110) | parser | executor | class policy + DAC search L78 | **none** | `AuditQueryDslEvaluatorTests.cs` | — | covered |
| POST `:validate` (L138) | `ValidateAuditQueryCommand` (L152) | parser | parser → canonical JSON | class policy + DAC query L139 | **none** | `AuditQueryDslParserTests.cs` | — | covered |
| GET `templates` (L164) | static `AuditQueryTemplateCatalog` | n/a | catalog L169 | class policy + DAC list L165 | **none** | catalog covered in DSL tests | — | covered |
| GET `saved` (L178) | `ISavedAuditQueryService.ListAsync` (L192) | — | service (own-or-shared in tenant) | class policy + DAC list L179 | **none** | `SavedAuditQueryServiceTests.cs` | — | covered |
| POST `saved` (L200) | `CreateSavedAuditQueryCommand` (L215) | query validated pre-persist | service + revision snapshot | class policy | **none** | `SavedAuditQueryServiceTests.cs` | — | covered |
| GET `saved/{id}` (L227) | `GetAsync` (L242) | — | service (owner-or-shared) | class policy | **none** | `SavedAuditQueryServiceTests.cs` | — | covered |
| GET `saved/{id}/revisions` (L250) | `GetRevisionsAsync` (L265) | — | service (revisions survive delete) | class policy | **none** | `SavedAuditQueryServiceTests.cs` | — | covered |
| PUT `saved/{id}` (L272) | `UpdateSavedAuditQueryCommand` (L289) | validated | service + new revision | class policy (owner-only in service) | **none** | `SavedAuditQueryServiceTests.cs` | — | covered |
| DELETE `saved/{id}` (L299) | `DeleteSavedAuditQueryCommand` (L314) | — | service (history retained) | class policy (owner-only in service) | **none** | `SavedAuditQueryServiceTests.cs` | — | covered |
| POST `saved/{id}:execute` (L328) | `ExecuteSavedAuditQueryCommand` (L346) | stored canonical query | executor + usage recording | class policy + DAC query L329 | **none** | `SavedAuditQueryServiceTests.cs`, `AuditQueryDslExecutorTests.cs` | — | covered |

## SecurityAuditController — `v{version}/admin/security-audit` (`Audit:/Controllers/SecurityAuditController.cs`, `[Authorize]` L31)

| Route | Command / Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|-----------------|-----------|---------|------|------------|-----------|------------------|-------|
| GET `` (L45) | `UnifiedSecurityAuditRequest` → `ISecurityAuditAggregator` (L73) | — | `SecurityAuditAggregator` (3-source union) | AuditRead L46 + DAC list L47 + search perm L48 + clamp L60 | **none** | `AuditLogQueryServiceTests.cs` | — | covered |
| GET `authentication` (L88) | `AuthenticationAuditRequest` (L104) | — | aggregator | **SystemAdmin L89** — deliberately stricter, request has no tenant filter (remarks L80-87) | **none** | — | — | **no dedicated unit test on the authentication surface** |
| GET `permissions` (L112) | `PermissionAuditRequest` (L133) | — | aggregator | AuditRead L113 + DAC list L114 + clamp L124 | **none** | — | — | covered |
| GET `dashboard` (L141) | aggregates (L163) | — | aggregator | AuditRead L142 + DAC query L143 + clamp L155 | **none** | `AuditReportServiceTests.cs` | — | covered |
| POST `:export` (L175) | `ExportSecurityAuditLogsCommand` (L190) | — | `AuditExportCommandHandler` CSV leg (`AuditExportCommands.cs:18`) | AuditExport L176 + clamp L186 | **none — inconsistent with AuditController export tier** | `AuditExportTransportContractTests.cs` | — | **security-audit export not on ExpensiveOperations** — N12 |

## SecurityEventController — `v{version}/audit/security-events` + `api/audit/security-events` (`Audit:/Controllers/SecurityEventController.cs`, `[Authorize]` L21 + **`EnableRateLimiting(Api)` L22**)

| Route | Command / Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|-----------------|-----------|---------|------|------------|-----------|------------------|-------|
| GET `taxonomy` (L34) | `ISecurityEventQueryService.GetTaxonomy` | n/a | static taxonomy | class + DAC list L35 | Api | `SecurityEventTaxonomyTests.cs` | — | covered |
| GET `delivery-status` (L44) | `GetDeliveryStatusAsync` | n/a | pipeline status tracker | class + DAC query L45 | Api | `SecurityEventQueryServiceTests.cs` | — | covered |
| GET `alerts` (L56) | `GetAlertsAsync` (L65) | service validation → 400 | `SecurityEventQueryService` (tenant clamped) | class + DAC list L57 | Api | `SecurityEventQueryServiceTests.cs` | `SecurityAlertLifecyclePostgreSqlHttpTests.cs` | covered |
| POST `alerts/{alertId}/acknowledge` (L79) | `AcknowledgeSecurityAlertCommand` (L91) | transition guard → 409 | command handler | class [Authorize] only — **no policy/DAC on mutation** | Api | `SecurityAlertLifecyclePostgreSqlHttpTests.cs`, `SecurityEventPipelineTests.cs` | `SecurityAlertLifecyclePostgreSqlHttpTests.cs` | covered via service guard (actor from context) |
| POST `alerts/{alertId}:resolve` (L110) | `ResolveSecurityAlertCommand` (L122) | transition guard → 409 | command handler | class only | Api | same family | same | covered |
| GET `retention/policy` (L137) | `ISecurityLogRetentionService.GetPolicyAsync` | — | service (`RequireAdministratorAsync` → 403) | class only + service guard | Api | `SecurityLogRetentionServiceTests.cs` | — | covered |
| PUT `retention/policy` (L155) | `ConfigureSecurityLogRetentionCommand` (L164) | `SecurityLogRetentionValidationException` → 400 | service (revision concurrency → 409) | class only + service guard | Api | `SecurityLogRetentionServiceTests.cs`, `AuditRetentionConcurrentValidationTests.cs` | — | covered |
| POST `retention/enforce` (L181) | `EnforceSecurityLogRetentionCommand` (L190) | — | service (legal-hold aware) | class only + service guard | Api | `SecurityLogRetentionServiceTests.cs` | — | covered |
| GET `retention/executions` (L201) | `GetExecutionsAsync` (L210) | — | service | class only | Api | `SecurityLogRetentionServiceTests.cs` | — | covered |
| GET `retention/archive` (L227) | `GetArchivedAsync` (L236) | — | service (issue #193 archive tier) | class only | Api | `SecurityLogRetentionServiceTests.cs` | — | covered |
| GET `retention/storage-tiers` (L254) | `GetStorageTierReportAsync` | — | service | class only | Api | — | — | covered |

## ChainOfCustodyController — `v{version}/audit/chain-of-custody` + api alias (`Audit:/Controllers/ChainOfCustodyController.cs`, `[Authorize]` + Api L23-24; all guards service-side → 403/400)

| Route | Command / Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|-----------------|-----------|---------|------|------------|-----------|------------------|-------|
| GET `evidence` (L34) | `IChainOfCustodyService.ListAsync` (L41) | take clamp 1-200 inline L41 | service (custody-gated; reads logged) | class only + service scope check | Api | `AuditCustodyServicesTests.cs` | — | covered |
| GET `evidence/{evidenceId}` (L50) | `GetAsync` purpose-tagged (L58) | — | service (access logged; broken seal needs forensic scope) | class only | Api | `AuditCustodyServicesTests.cs` | — | covered |
| POST `evidence` (L68) | `PreserveEvidenceCommand` (L76) | `ValueSnapshotValidationException` → 400 | command handler (SHA-256 content hash at preserve) | class only | Api | `AuditCustodyServicesTests.cs` | — | covered |
| POST `evidence/{id}:break-seal` (L90) | `BreakEvidenceSealCommand` (L99) | purpose required | handler (forensic scope required) | class only | Api | `AuditCustodyServicesTests.cs` | — | covered |
| POST `evidence/{id}:release` (L112) | `ReleaseEvidenceCommand` (L121) | reason required | handler (**blocked while legal hold active**) | class only | Api | `AuditCustodyServicesTests.cs` | — | covered |
| GET `evidence/{id}/integrity` (L134) | `VerifyIntegrityAsync` (L141) | — | service vs recorded hash | class only | Api | `AuditCustodyServicesTests.cs` | — | covered |
| GET `custodians` (L151) | `ListAsync` | — | `IEvidenceCustodianService` | class only | Api | `AuditCustodyServicesTests.cs` | — | covered |
| POST `custodians` (L166) | `RegisterCustodianCommand` (L174) | validation exception → 400 | handler (fail-closed registry) | class only | Api | `AuditCustodyServicesTests.cs` | — | covered |
| POST `custodians/{id}:revoke` (L188) | `RevokeCustodianCommand` (L196) | reason required | handler (immediate effect, logged) | class only | Api | `AuditCustodyServicesTests.cs` | — | covered |
| GET `transfers` (L205) | `ListAsync` (L215) | — | `IEvidenceTransferService` | class only | Api | `AuditCustodyServicesTests.cs` | — | covered |
| POST `transfers` (L224) | `InitiateEvidenceTransferCommand` (L232) | validation → 400 | handler (challenge nonce + ECDSA proof) | class only | Api | `AuditCustodyServicesTests.cs` | — | covered |
| POST `transfers/{id}:acknowledge` (L246) | `AcknowledgeEvidenceTransferCommand` (L255) | validation → 400 | handler (verify proof, counter-sign, custody handoff) | class only | Api | `AuditCustodyServicesTests.cs` | — | covered |
| GET `legal-holds` (L269) | `ListAsync` | — | `ILegalHoldService` | class only | Api | `AuditCustodyServicesTests.cs` | — | covered |
| GET `legal-holds/{holdId}` (L285) | `GetAsync` | — | service | class only | Api | `AuditCustodyServicesTests.cs` | — | covered |
| POST `legal-holds` (L302) | `CreateLegalHoldCommand` (L310) | validation → 400 | handler (immediate preservation + notifications) | class only | Api | `AuditCustodyServicesTests.cs` | — | covered |
| POST `legal-holds/{id}:release` (L324) | `ReleaseLegalHoldCommand` (L333) | reason required | handler (chains retained as records) | class only | Api | `AuditCustodyServicesTests.cs` | — | covered |
| POST `legal-holds/{id}:notify` (L346) | `DispatchLegalHoldNotificationsCommand` (L353) | — | handler | class only | Api | `AuditCustodyServicesTests.cs` | — | covered |
| POST `packages` (L366) | `BuildCustodyPackageCommand` (L374) | validation → 400 | handler (court-admissible + expert certification) | class only | Api | `AuditCustodyServicesTests.cs` | — | covered |

## Remaining audit controllers (class `[Authorize]` + `RateLimitPolicies.Api` unless noted)

### CompliancePackagingController — `v{version}/audit/compliance-packaging` (`Controllers/CompliancePackagingController.cs:14-18`)

| Route | Command / Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|-----------------|-----------|---------|------|------------|-----------|------------------|-------|
| GET `templates` (L25) | `GetTemplatesAsync` | n/a | framework catalog service | class only | Api | `ComplianceFrameworkCatalogTests.cs` | — | covered |
| POST `documents` (L30) | `UploadComplianceDocumentCommand` | 2 MiB `RequestSizeLimit` L31 | handler (validation → 400) | class only | Api | `CompliancePackagingCoreTests.cs` | — | covered |
| GET `documents` (L37) / `{id}` (L43) | service reads | — | service | class only | Api | `CompliancePackagingCoreTests.cs` | — | covered |
| POST `documents/{id}/review` (L48) | `ReviewComplianceDocumentCommand` | validation → 400 | handler | class only | Api | `CompliancePackagingCoreTests.cs` | — | covered |
| POST `` (L54) | `PrepareCompliancePackageCommand` | validation → 400; signing unavailable → 503 L113 | handler (seals + signs) | class only | Api | `ComplianceEvidencePackagingServiceTests.cs`, `EcdsaCryptographicSigningServiceTests.cs` | — | covered |
| GET `` (L61) / `{id}` (L67) | service reads | — | service | class only | Api | `CompliancePackagingCoreTests.cs` | — | covered |
| GET `{id}/verification` (L72) | `VerifyPackageAsync` | — | engine | class only | Api | `ComplianceFrameworkEvidenceTests.cs` | — | covered |
| GET `{id}/download` (L77) | `DownloadPackageAsync` | — | service (SHA-256 header L88, no-store) | class only | Api | `CompliancePackagingCoreTests.cs` | — | covered |

### EntityChangeAuditController — `v{version}/audit/entity-changes` (`Controllers/EntityChangeAuditController.cs:18-22`)

| Route | Command / Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|-----------------|-----------|---------|------|------------|-----------|------------------|-------|
| GET `` (L30) | `EntityChangeAuditListRequest` → `ListForCurrentTenantAsync` (L39) | service validation → 400 | `EntityChangeAuditQueryService` (tenant-admin guard → 403; host store bridge `API:/Core/Setup/InfrastructureLayerExtensions.cs:416`) | class only + service guard | Api | `EntityChangeAuditRedactionTests.cs` | — | covered |

### Coverage controllers (`Controllers/ComprehensiveCoverageControllers.cs`)

| Route | Command / Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|-----------------|-----------|---------|------|------------|-----------|------------------|-------|
| GET `v1/audit/comprehensive-coverage` (L18, L27) | `BuildAuditTrailCoverage` | n/a | `EventCoverageSweepService` | class only | Api | `EventCoverageSweepTests.cs` | — | covered |
| GET `v1/audit/events/comprehensive` (L39, L48) | `SweepEventCoverage` | n/a | same service | class only | Api | `EventCoverageSweepTests.cs` | — | covered |

### FieldLevelAuditController — `v{version}/audit/field-level` (`Controllers/FieldLevelAuditController.cs:20-24`)

| Route | Command / Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|-----------------|-----------|---------|------|------------|-----------|------------------|-------|
| GET `access-history/{entityId}` (L40) | `GetFieldAccessHistoryAsync` (L45) | — | `FieldAccessAuditService` | class only + service tenant scope | Api | `FieldLevelAuditFamilyTests.cs` | — | covered |
| POST `access` (L52) | `RecordFieldAccessCommand` (L57) | validation → 400 | handler (classification + masking) | class only | Api | `FieldLevelAuditFamilyTests.cs` | — | covered |
| GET `classifications` (L63) | `GetClassificationsAsync` (L68) | — | service | class only | Api | `FieldLevelAuditFamilyTests.cs` | — | covered |
| POST `classifications/scan` (L73) | `ScanSensitiveFieldsCommand` (L76) | — | handler | class only | Api | `FieldLevelAuditFamilyTests.cs` | — | covered |
| GET `anomalies` (L82) | `GetAnomaliesAsync` (L102) | enum parse → 400 L87-100 | `FieldAccessPatternAnalysisService` | class only | Api | `FieldLevelAuditFamilyTests.cs` | — | covered |
| POST `anomalies/analyze` (L107) | `AnalyzeFieldAccessPatternsCommand` | — | handler | class only | Api | `FieldLevelAuditFamilyTests.cs` | — | covered |
| POST `anomalies/{id}/acknowledge` (L117) | `AcknowledgeFieldAccessAnomalyCommand` | — | handler | class only | Api | `FieldLevelAuditFamilyTests.cs` | — | covered |
| POST `anomalies/{id}/resolve` (L124) | `ResolveFieldAccessAnomalyCommand` | — | handler | class only | Api | `FieldLevelAuditFamilyTests.cs` | — | covered |
| GET `bulk-exports` (L134) | `GetExportsAsync` (L139) | — | `BulkExportMonitoringService` | class only | Api | `AuditLogExportBulkMonitoringTests.cs` | — | covered |
| POST `bulk-exports/evaluate` (L144) | `EvaluateBulkExportCommand` | validation → 400; blocked → 413 L317-319 | handler (threshold alerting) | class only | Api | `AuditLogExportBulkMonitoringTests.cs` | — | covered |
| GET `gdpr/article-30` (L154) | `GetActivitiesAsync` + `GetCompletenessGapsAsync` | — | `GdprArticle30Service` | class only | Api | `FieldLevelAuditFamilyTests.cs` | — | covered |
| PUT `gdpr/article-30` (L167) | `SaveGdprActivityCommand` | validation → 400 | handler | class only | Api | `FieldLevelAuditFamilyTests.cs` | — | covered |
| DELETE `gdpr/article-30/{id}` (L175) | `DeleteGdprActivityCommand` | — | handler | class only | Api | `FieldLevelAuditFamilyTests.cs` | — | covered |
| POST `gdpr/article-30/seed` (L185) | `SeedGdprActivitiesCommand` | — | handler | class only | Api | `FieldLevelAuditFamilyTests.cs` | — | covered |
| POST `gdpr/article-30/{id}/review` (L192) | `MarkGdprActivityReviewedCommand` | — | handler | class only | Api | `FieldLevelAuditFamilyTests.cs` | — | covered |
| GET `cross-border-transfers` (L201) | `GetTransfersAsync` | enum parse → 400 | `CrossBorderTransferService` | class only | Api | `FieldLevelAuditFamilyTests.cs` | — | covered |
| POST `cross-border-transfers/validate` (L226) | `ValidateCrossBorderTransferCommand` | validation → 400 | handler (jurisdiction policy) | class only | Api | `FieldLevelAuditFamilyTests.cs` | — | covered |
| GET `data-subject-requests` (L236) | `GetRequestsAsync` | status parse → 400 | `DataSubjectAccessMonitoringService` | class only | Api | `FieldLevelAuditFamilyTests.cs` | — | covered |
| GET `data-subject-requests/subjects/{userId}` (L257) | `GetSubjectMonitoringAsync` | — | service | class only | Api | `FieldLevelAuditFamilyTests.cs` | — | covered |
| GET `data-subject-requests/summary` (L264) | `GetSummaryAsync` | — | service | class only | Api | `FieldLevelAuditFamilyTests.cs` | — | covered |
| GET `retention` (L273) | `GetPoliciesAsync` | — | `SensitiveDataRetentionService` | class only | Api | `FieldLevelAuditFamilyTests.cs` | — | covered |
| PUT `retention` (L283) | `ConfigureSensitiveDataRetentionCommand` | validation → 400 | handler | class only | Api | `FieldLevelAuditFamilyTests.cs` | — | covered |
| POST `retention/enforce` (L291) | `EnforceSensitiveDataRetentionCommand` (dry-run flag) | — | handler (purge or mask) | class only | Api | `FieldLevelAuditFamilyTests.cs` | — | covered |

### PiiRedactionPipelineController — `v{version}/audit/redaction-pipeline` (`Controllers/PiiRedactionPipelineController.cs:18-22`)

| Route | Command / Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|-----------------|-----------|---------|------|------------|-----------|------------------|-------|
| GET `` (L30) | `GetStatusAsync` | — | `PiiRedactionAdminService` | class only + service tenant-admin guard | Api | `PiiRedactionPipelineTests.cs` | — | covered |
| GET `rules` (L36) / `rules/{id}` (L43) | service reads | — | service | class only | Api | `PiiRedactionPipelineTests.cs` | — | covered |
| POST `rules` (L50) | `CreatePiiRedactionRuleCommand` | validation → 400 | handler (audited, cache invalidation) | class only | Api | `PiiRedactionPipelineTests.cs` | — | covered |
| PUT `rules/{id}` (L71) | `UpdatePiiRedactionRuleCommand` | expectedRevision → 409 on stale | handler (versioned) | class only | Api | `PiiRedactionPipelineTests.cs` | — | covered |
| DELETE `rules/{id}` (L94) | `DeletePiiRedactionRuleCommand` | same 409 semantics | handler (audited high risk) | class only | Api | `PiiRedactionPipelineTests.cs` | — | covered |
| POST `test` (L118) | `TestPiiRedactionPipelineCommand` | validation → 400 | handler (dry run) | class only | Api | `PiiRedactionPipelineTests.cs` | — | covered |
| GET `tokens/{token}` (L139) | `ResolvePiiTokenCommand` | reason required L144 | handler (**vault resolve, critical-risk audited**) | class only + service authorization | Api | `PiiRedactionPipelineTests.cs` | — | covered |

### ValueSnapshotController — `v{version}/audit/value-snapshots` (`Controllers/ValueSnapshotController.cs:20-24`)

| Route | Command / Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|-----------------|-----------|---------|------|------------|-----------|------------------|-------|
| GET `` (L34) | `SearchAsync` | validation → 400 | `ValueSnapshotQueryService` | class only + service tenant guard | Api | `ValueSnapshotQueryAnalysisTests.cs` | — | covered |
| GET `{id}` (L55) | `GetAsync` | — | service | class only | Api | `ValueSnapshotQueryAnalysisTests.cs` | — | covered |
| GET `history` (L72) | `GetHistoryAsync` | validation → 400 | service | class only | Api | `ValueSnapshotQueryAnalysisTests.cs` | — | covered |
| GET `trends` (L98) | `GetTrendAsync` | validation → 400 | `ValueSnapshotAnalysisService` | class only | Api | `ValueSnapshotQueryAnalysisTests.cs` | — | covered |
| GET `impact/{snapshotId}` (L126) | `GetImpactAsync` | — | service | class only | Api | `ValueSnapshotDiffEngineTests.cs` | — | covered |
| GET `storage` (L143) | `GetStorageOverviewAsync` | — | service | class only | Api | `ValueSnapshotMaintenanceServiceTests.cs` | — | covered |
| GET `retention/policy` (L158) | `GetPolicyAsync` | — | `ValueSnapshotRetentionService` | class only | Api | `ValueSnapshotMaintenanceServiceTests.cs` | — | covered |
| PUT `retention/policy` (L175) | `ConfigureValueSnapshotRetentionCommand` | validation → 400 | handler | class only | Api | `ValueSnapshotMaintenanceServiceTests.cs` | — | covered |
| POST `retention/enforce` (L196) | `EnforceValueSnapshotRetentionCommand` | — | handler (legal-hold aware, dry-run) | class only | Api | `ValueSnapshotMaintenanceServiceTests.cs` | — | covered |
| POST `rollback-plans` (L220) | `CreateValueSnapshotRollbackPlanCommand` | rejection → 409 | `ValueSnapshotRollbackService` | class only | Api | `ValueSnapshotMaintenanceServiceTests.cs` | — | covered |
| GET `rollback-plans/{planId}` (L248) | `GetPlanAsync` | — | service | class only | Api | `ValueSnapshotMaintenanceServiceTests.cs` | — | covered |
| POST `rollback-plans/{planId}:execute` (L265) | `ExecuteValueSnapshotRollbackCommand` | newer-changes guard → 409 unless accepted | handler | class only | Api | `ValueSnapshotMaintenanceServiceTests.cs` | — | covered |

### AuditReplayController — `v{version}/admin/audit-logs/replay` + `api/audit/replay` (`Controllers/AuditReplayController.cs:41-48`)

| Route | Command / Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|-----------------|-----------|---------|------|------------|-----------|------------------|-------|
| POST `snapshots` (L56) | `CaptureAuditReplayCommand` (L75) | bounds inline; `RequestSizeLimit` L57 | `AuditReplayCaptureService` (≤10 000 events, ≤16 MiB, SHA-256 header L78) | **`Policies.SystemBackupRestore` L47** (issue #386) | Api L48 | `AuditReplayControllerTests.cs`, `AuditReplayCaptureServiceTests.cs`, `AuditReplaySnapshotTests.cs` | `AuditReplayPostgreSqlHttpTests.cs` | covered |
| GET `snapshots` (L111) | same command (L117-119) | same | same | same | Api | `AuditReplayControllerTests.cs` | `AuditReplayPostgreSqlHttpTests.cs` | covered |
| POST `snapshots:reconstruct` (L127) | `ReconstructAuditReplayCommand` (L145) | digest must match L138-141 | `AuditReplayReconstructionService` | same | Api | `AuditReplayReconstructionControllerTests.cs`, `AuditReplayReconstructionServiceTests.cs` | `AuditReplayPostgreSqlHttpTests.cs` | covered |

### AuditRetentionSimulationController — `v{version}/audit/retention-simulation` (+ `retention-policies` alias) (`Controllers/AuditRetentionSimulationController.cs:17-23`)

| Route | Command / Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|-----------------|-----------|---------|------|------------|-----------|------------------|-------|
| GET `templates` (L31) | `GetPolicyTemplatesAsync` | n/a | catalog (validated at boot `AuditModule.cs:43`) | class only + service admin guard | Api | `AuditRetentionPolicyTemplatesTests.cs` | — | covered |
| GET `configuration` (L40) | `GetConfigurationAsync` | — | service | class only | Api | `AuditRetentionSimulationServiceTests.cs` | — | covered |
| PUT `configuration` (L48) | `ConfigureAuditRetentionCommand` | validation → 400; concurrency → 409 | handler | class only | Api | `AuditRetentionSimulationServiceTests.cs`, `AuditRetentionConcurrentValidationTests.cs` | — | covered |
| POST `` (L56) | `RunAuditRetentionSimulationCommand` | validation → 400 | simulation engine (what-if only) | class only | Api | `AuditRetentionSimulationEngineTests.cs` | — | covered |
| GET `{id}` (L64) | `GetRunAsync` | — | service | class only | Api | `AuditRetentionSimulationServiceTests.cs` | — | covered |
| GET `` (L70) | `GetRunsAsync` | — | service | class only | Api | `AuditRetentionSimulationServiceTests.cs` | — | covered |

## ConsentController — `api/compliance/consent` (`Consent:/Controllers/ConsentController.cs`, `[Authorize]` L10 — **service layer unregistered, gap N1**)

| Route | Command / Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|-----------------|-----------|---------|------|------------|-----------|------------------|-------|
| GET `policies` (L13) | `GetActivePoliciesQuery` | — | `GetActivePoliciesQueryHandler` (`Commands/ConsentCommands.cs:62`) | `[Authorize]` only | **none** | `ConsentCoverageCompletionTests.cs` (DI only) | — | **dead at runtime — N1; no ownership/tenant clamp** |
| POST `policies` (L17) | `CreateConsentPolicyCommand` | — | handler (`:19`) | `[Authorize]` only | **none** | — | — | **any user can create policies — N4** |
| POST `policies/{policyId}/versions` (L21) | `PublishPolicyVersionCommand` | — | handler (`:28`) | `[Authorize]` only | **none** | — | — | N4 |
| GET `users/{userId}` (L25) | `GetUserConsentsQuery` | — | handler (`:68`) | `[Authorize]` only | **none** | — | — | **any user reads any user's consents — N4** |
| POST `grant` (L29) | `GrantConsentCommand` | — | handler (`:34`) | `[Authorize]` only | **none** | — | — | N1 + N2 + N4 |
| POST `revoke` (L33) | `RevokeConsentCommand` | — | handler (`:40`) | `[Authorize]` only | **none** | — | — | N4 |
| POST `data-subject-requests` (L40) | `SubmitDataSubjectRequestCommand` | — | handler (`:49`) | `[Authorize]` only | **none** | — | — | N4 |
| POST `data-subject-requests/{id}/process` (L44) | `ProcessDataSubjectRequestCommand` | — | handler (`:55`) | `[Authorize]` only | **none** | — | — | **ProcessedByUserId client-supplied — N4** |
| GET `data-subject-requests/pending` (L48) | `GetPendingDataSubjectRequestsQuery` | — | handler (`:74`) | `[Authorize]` only | **none** | — | — | N4 |

## FerpaController — `api/compliance/ferpa` (`Ferpa:/Controllers/FerpaController.cs`, `[Authorize]` L10)

| Route | Command / Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|-----------------|-----------|---------|------|------------|-----------|------------------|-------|
| GET `students/{id}/records` (L13) | `GetStudentEducationRecordsQuery` | — | handler (`Commands/FerpaCommands.cs:105`) | `[Authorize]` only | **none** | `FerpaCommandHandlerTests.cs`, `FerpaServiceTests.cs` | — | **no scope check — N5** |
| GET `students/{id}/directory-information` (L17) | `GetStudentDirectoryInformationQuery` | — | handler (`:111`) | `[Authorize]` only | **none** | same | — | N5 |
| POST `records` (L21) | `RegisterEducationRecordCommand` | — | handler (`:63`) | `[Authorize]` only | **none** | same | — | N5 |
| GET `directory-policy` (L25) | `GetDirectoryInformationPolicyQuery` | — | handler (`:117`) | `[Authorize]` only | **none** | same | — | covered |
| PUT `directory-policy` (L29) | `UpsertDirectoryInformationPolicyCommand` | — | handler (`:69`) | `[Authorize]` only | **none** | same | — | **tenant policy writable by any user — N5** |
| GET `students/{id}/consents` (L33) | `GetStudentFerpaConsentsQuery` | — | handler (`:123`) | `[Authorize]` only | **none** | same | — | N5 |
| POST `consents` (L37) | `GrantFerpaDisclosureConsentCommand` | — | handler (`:75`) | `[Authorize]` only | **none** | same | — | **guardian unverified — N5** |
| POST `consents/{id}/revoke` (L41) | `RevokeFerpaDisclosureConsentCommand` | — | handler (`:81`) | `[Authorize]` only | **none** | same | — | N5 |
| POST `disclosures` (L47) | `RecordFerpaDisclosureCommand` | — | handler (`:87`; consent-or-basis gate in service) | `[Authorize]` only | **none** | same | — | N5 |
| GET `students/{id}/disclosures` (L51) | `GetStudentFerpaDisclosureLogsQuery` | — | handler (`:129`) | `[Authorize]` only | **none** | same | — | N5 |
| POST `inspection-requests` (L55) | `SubmitFerpaInspectionRequestCommand` | — | handler (`:93`) | `[Authorize]` only | **none** | same | — | N5 |
| POST `inspection-requests/{id}/complete` (L59) | `CompleteFerpaInspectionRequestCommand` | entity guard → 500 on terminal | handler (`:99`) | `[Authorize]` only | **none** | `FerpaEntityTests.cs` | — | **ProcessedByUserId client-supplied — N4** |
| GET `inspection-requests/pending` (L63) | `GetPendingFerpaInspectionRequestsQuery` | — | handler (`:135`) | `[Authorize]` only | **none** | same | — | N5 |

## KYC host controllers (`API:/Core/Controllers/EconomyKycController.cs`)

### EconomyKycController — `api/v{version}/economy/kyc` (`[Authorize]` L26)

| Route | Command / Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|-----------------|-----------|---------|------|------------|-----------|------------------|-------|
| POST `onboarding` (L33) | `StartKycOnboardingEndpointCommand` (L42) | idempotency key required in body record; orchestrator guards | `EconomyComplianceEndpointCommandHandler` (`API:/Core/Commands/EconomyComplianceEndpointCommands.cs:116`) → `SumSubKycAmlOrchestrator.StartAsync` | `[Authorize]` + authenticated tenant actor L40 | **none** | `SumSubKycAmlOrchestratorTests.cs` | — | covered |
| POST `access-token` (L52) | `CreateKycAccessTokenEndpointCommand` (L64) | lifetime > 0 enforced in orchestrator L130 | handler `:119` | `[Authorize]` + actor L59 | **none** | `SumSubKycAmlOrchestratorTests.cs` | — | covered |
| GET `status` (L77) | `IComplianceEvidenceReader.ReadLatestAsync` (L83) | — | host evidence reader (no CQRS) | `[Authorize]` + actor L82 | **none** | `SumSubKycAmlOrchestratorTests.cs` | — | covered |

### EconomySumSubWebhookController — `api/v{version}/integrations/economy/sumsub/webhook` (L114)

| Route | Command / Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|-----------------|-----------|---------|------|------------|-----------|------------------|-------|
| POST `webhook` (L121) | `IngestSumSubWebhookEndpointCommand` (L135) | digest headers required L127-131 | handler `:122` → `IngestWebhookAsync` (HMAC verify, inbox, replay guards) | **`[AllowAnonymous]` L122 — signature is the gate** | **none** | `SumSubKycAmlOrchestratorTests.cs`, `SumSubKycAmlProviderTests.cs` | — | **no Webhook rate policy — N8** |

## Matrix-level findings

1. **Zero validators in four modules** — the shared `ValidationBehavior` never runs here; every guard is inline or exception-mapped. Acceptable for query-heavy audit surfaces; *not* acceptable for Consent/FERPA where body fields flow straight into `Guid` identities of other users.
2. **Rate limiting is patchy on exactly the expensive surfaces**: `AuditController` listing/DSL/statistics, `AuditQueryController` (unbounded-ish scans bounded only by the DSL executor caps), `SecurityAuditController:export`, the action-type export, the scheduled-export download, Consent, FERPA and the SumSub webhook are unthrottled (N8/N12).
3. **Test skew**: the audit module is deeply unit-tested (72 files) with four PostgreSql HTTP integration suites for the export/replay/alert paths; the custody, packaging, PII, snapshot and DSL surfaces have no HTTP-level integration tests; Consent has only a DI-wiring test (which passes while the module is dead in the host — it never asks whether the *host* resolves `IConsentService`); FERPA has solid service/repo tests that all bypass the controller's missing authorization.
