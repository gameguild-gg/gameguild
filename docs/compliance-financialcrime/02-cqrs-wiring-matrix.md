# FinancialCrime (AML) — CQRS / wiring matrix

> Module `GameGuild.Compliance.FinancialCrime` + host composition · Documented against `develop` commit `60100459e6`.
> The module owns **no controller and no request types of its own**; its write surface is reached through three host endpoint commands, its read surface through two direct control-plane calls in the controller, and its remaining four operations have no caller at all.

## 0. DI wiring (who can resolve what)

| Service | Registration | Evidence | Reachable at runtime? |
|---|---|---|---|
| `IFinancialCrimeControlPlane` → `PostgreSqlFinancialCrimeControlPlane` | `TryAddScoped` in `AddFinancialCrimeComposition` | `FC:/FinancialCrimeModule.cs:22` | **YES** — composition invoked by `AddEconomyCapabilityComposition` (`API:/Core/Setup/EconomyCapabilityCompositionExtensions.cs:22`), itself invoked by `ApiProductComposition.ConfigureServices` (`API:/Core/Setup/ApiProductComposition.cs:123`) |
| `IFinancialCrimeRiskInputSource` → `PostgreSqlFinancialCrimeRiskInputSource` | `TryAddScoped` same composition | `FC:/FinancialCrimeModule.cs:21` | **YES** — injected into `PostgreSqlEconomyProtectedOperationRiskDecisionIssuer` (`Risk:/PostgreSqlEconomyProtectedOperationRiskDecisionIssuer.cs:16,27`), `HardToSoftConversionRiskEvidenceVerifier` (`Risk:/HardToSoftConversionRiskEvidenceVerifier.cs:17-19`), `PayoutCoordinator` (`Payouts:/PayoutCoordinator.cs:46`) |
| `IComplianceEvidenceReader` | two registrations: `TryAddSingleton<UnavailableComplianceEvidenceReader>` (composition, first) then `AddScoped` factory → `PostgreSqlComplianceEvidenceStore` (`Risk:/EconomyCoreModule.cs:87-88`, second) | last registration wins for single resolution | **YES — real Postgres reader** serves the risk input source; the `Unavailable` singleton is a standalone-fallback only. Order: capability composition (`ApiProductComposition.cs:123`) runs before core composition (`:124`) |
| `IComplianceEvidenceStore` → `PostgreSqlComplianceEvidenceStore` | `AddScoped` factory | `Risk:/EconomyCoreModule.cs:84-86` | **YES** — control-plane constructor (`FC:/PostgreSqlFinancialCrimeControlPlane.cs:73-77`) |
| `IComplianceHoldStore` → `PostgreSqlComplianceHoldStore` | `AddScoped` | `Risk:/EconomyCoreModule.cs:89` | **YES** — case opening activates holds (`FC:/PostgreSqlFinancialCrimeControlPlane.cs:418-421`) |
| `FinancialCrimeModelConfiguration` (7 tables) | assembly-scanned `IModelConfiguration` | `API:/Database/ApplicationDbContext.cs:866-883`; snapshot `:10411-10752` | **YES — tables materialized** (all 7 `compliance_financial_crime_*`) |
| `FinancialCrimeModule : ModuleBase` | class exists, `EnabledByDefault => true` | `FC:/FinancialCrimeModule.cs:8-14` | **NO host instantiates it**; the name sits in `DisabledModules` (`API:/Core/Setup/ApiProductComposition.cs:114-117`) which only affects assembly scanning — inert here (gap FC6) |

**Auth legend** — `[Authorize]` = any authenticated principal; `OperateCompliance` = `EconomyPermission.Keys.OperateCompliance` checked in-controller per request (`EconomyComplianceAdministrationController.cs:232-243`). **Rate limit legend** — “—” = no `EnableRateLimiting` anywhere on the controller (global limiter only, if any).

## 1. Live HTTP surface (5 endpoints, one controller)

Controller: `EconomyComplianceAdministrationController` (`API:/Core/Controllers/EconomyComplianceAdministrationController.cs`), route `api/v{v}/admin/economy/compliance`, `[Authorize]` controller-level, no rate-limit policy. (It also hosts the TrustSafety appeal surface — documented in the TrustSafety package when that row is written.)

| Route | Request | Validator | Handler / callee | Auth | Rate limit | Unit tests | Integration tests | HOLE? |
|---|---|---|---|---|---|---|---|---|
| GET `financial-crime/cases` | — (direct call) | — | `IFinancialCrimeControlPlane.ReadCasesAsync` (`:48-55`, `FC:...ControlPlane.cs:322-353`) | `[Authorize]` + `OperateCompliance` | — | `PostgreSqlFinancialCrimeControlPlaneTests.cs` (ReadCases guards) | — | no rate limit; **reads a table nothing can populate (FC1)** |
| GET `financial-crime/cases/{caseId}` | — (direct call) | — | `ReadCaseDetailsAsync` (`:58-75`, `FC:...:355-396`) | same | — | same | — | same |
| POST `financial-crime/cases/{caseId}/assignment` | `AssignFinancialCrimeCaseEndpointCommand` (`API:/Core/Commands/EconomyComplianceEndpointCommands.cs:12-14`) | — (controller binds `ExpectedVersion`) | `EconomyComplianceEndpointCommandHandler` (`:61-64`) → `AssignCaseAsync` (`FC:...:176-198`) | same | — | `PostgreSqlFinancialCrimeControlPlaneTests.cs`; handler dispatch in `FinancialCrimeCoverageCompletionTests.cs` | — | no validator, no rate limit |
| POST `financial-crime/cases/{caseId}/decisions` | `DecideFinancialCrimeCaseEndpointCommand` (`:16-17`) | — (controller composes the decision record `:115-130`) | handler (`:66-68`) → `DecideCaseAsync` (`FC:...:200-253`) | same | — | same | — | request `Id`/`Version`/`ExpiresAt`/`PolicyVersion` are client-supplied — no server-side sanity beyond entity validation |
| POST `financial-crime/cases/{caseId}/regulatory-references` | `RecordRegulatoryReferenceEndpointCommand` (`:18-20`) | — | handler (`:70-76`) → `RecordRegulatoryReferenceAsync` (`FC:...:286-305`) | same | — | same | — | SAR/STR kind whitelist server-side (`:291-294`); no rate limit |

## 2. Control-plane operations × callers (the reconciliation)

All 9 `IFinancialCrimeControlPlane` operations (`FC:/PostgreSqlFinancialCrimeControlPlane.cs:53-64`):

| Operation | Intended producer | Actual caller at `60100459e6` | Status |
|---|---|---|---|
| `IngestScreeningAsync` | screening provider connector / webhook | **NONE** | **hole — FC1**: no HTTP ingress, no worker, no scheduled puller; `ReadDueRescreeningsAsync` output also unconsumed |
| `RecordSignalAsync` | transaction-monitoring producer (economy postings, payments) | **NONE** | **hole — FC1**: no monitoring producer exists |
| `AssignCaseAsync` | analyst assignment | controller via endpoint command (`API:...Controller.cs:89-91`) | **wired** |
| `DecideCaseAsync` | analyst decision | controller via endpoint command (`:131-133`) | **wired** |
| `ConsumeDecisionAsync` | value-movement path burning an approval | **NONE** | **hole — FC1**: the single-use consumption leg is dormant |
| `RecordRegulatoryReferenceAsync` | compliance officer SAR/STR log | controller via endpoint command (`:157-159`) | **wired** |
| `ReadDueRescreeningsAsync` | rescreening scheduler sweep | **NONE** | **hole — FC1**: `NextScreenAt` is stored and indexed but nothing sweeps it |
| `ReadCasesAsync` | analyst queue | controller direct (`:54`) | **wired** |
| `ReadCaseDetailsAsync` | analyst case file | controller direct (`:68`, `:115`) | **wired** |

## 3. Persistence wiring

| Table | Written by | Read by | Constraints of note |
|---|---|---|---|
| `compliance_financial_crime_screenings` | `IngestScreeningAsync` (no caller) | replay lookup, rescreening query, case linkage | unique `(Provider,Environment,ProviderEventId)`; unique `(TenantId,SubjectHash,Version)`; check `Version>0 AND PolicyVersion>0`; check `ExpiresAt>IssuedAt AND NextScreenAt>IssuedAt` (`FC:/FinancialCrimePersistence.cs:113-135`) |
| `compliance_financial_crime_transaction_signals` | `RecordSignalAsync` (no caller) | replay lookup | unique `RequestHash`; check `Score BETWEEN 0 AND 1000000` (`:136-150`) |
| `compliance_financial_crime_cases` | open-on-ingest/signal (no caller), assign/decide (wired) | admin reads | check `Version>0`; check closed ⇒ `ClosedAt` present (`:151-162`) |
| `compliance_financial_crime_case_events` | every case mutation | details read | unique `(CaseId,Sequence)`; check `Sequence>0` (`:163-173`) |
| `compliance_financial_crime_decisions` | `DecideCaseAsync` (wired) | details read, consumption check | unique `(CaseId,Version)`; check versions/lifetime (`:174-188`) |
| `compliance_financial_crime_decision_consumptions` | `ConsumeDecisionAsync` (no caller) | consumability check | unique `DecisionId` — single-use enforced by index (`:189-196`) |
| `compliance_financial_crime_regulatory_references` | `RecordRegulatoryReferenceAsync` (wired) | details read | unique `(CaseId,Kind,ReferenceHash)` — same SAR hash on one case is idempotent (`:197-206`) |

Evidence ledger (economy-owned, financial-crime kind): `compliance_evidence` rows written through `IComplianceEvidenceStore.IngestAsync` (`Risk:/PostgreSqlComplianceEvidenceStore.cs:160-188`), read via `ReadLatestAsync` (`:190+`) — see `04-event-inventory.md`.

## 4. Wiring verdict

**The read/decide half of the AML program is fully wired and hardened; the ingest/monitor/consume half is entirely unwired.** This is the campaign's recurring "registered composition, missing producer" class (cf. Consent N1, economy N1/N2 no-consumer outboxes) — but inverted: here the consumer (risk gates) is live and the producer does not exist, which converts the gap into a universal fail-closed denial rather than silent dead endpoints. Consequence for rollout: enabling any Economy value-moving capability (`Risk:/EconomyRiskComposition.cs:36-64` opt-in + jurisdiction allowlist) without first building a screening connector yields `ComplianceUnavailable` for every subject (`Risk:/PostgreSqlEconomyProtectedOperationRiskDecisionIssuer.cs:82-87`).
