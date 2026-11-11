# TrustSafety — CQRS / wiring matrix

> Module `GameGuild.TrustSafety` · develop `ce31d5850c`. Shorthand `TS:`, `API:`, `Assets:`, `Ratings:`, `Courses:` per README.

## 0. DI wiring — "disabled" in name, live in fact

| Piece | Registration | Evidence | Live? |
|---|---|---|---|
| Evidence reader fallback | `TryAddSingleton<IComplianceEvidenceReader, UnavailableComplianceEvidenceReader>` | `TS:/TrustSafetyModule.cs:20` | **yes** (host overrides with the real store) |
| Risk input source | `TryAddScoped<ITrustSafetyRiskInputSource, PostgreSqlTrustSafetyRiskInputSource>` | `:21` | **yes** — consumed by economy risk issuer (`Econ:/Risk/PostgreSqlEconomyProtectedOperationRiskDecisionIssuer.cs:188-190`) and payouts (`Finance.Economy.Payouts/PayoutCoordinator.cs:116`) |
| Signature verifier adapter | `TryAddScoped<ITrustSafetyEventSignatureVerifier, EconomyTrustSafetyEventSignatureVerifier>` (wraps `ICapabilityPolicySignatureVerifier`) | `:22`; `TS:/PostgreSqlTrustSafetyControlPlane.cs:39-48` | **yes** |
| Control plane | `TryAddScoped<ITrustSafetyControlPlane, PostgreSqlTrustSafetyControlPlane>` | `:23` | **yes** |
| Composition call site | `AddTrustSafetyComposition` via economy capability host | `API:/Core/Setup/EconomyCapabilityCompositionExtensions.cs:23` | **yes** |
| Tables ×3 | `TrustSafetyModelConfiguration : IModelConfiguration` (assembly scan loads every `GameGuild.*.dll`, no module filter — `API:/Database/ApplicationDbContext.cs:902-920, 950-972`); created by migration `20260824180312_AddEconomyProductionPlatform` (`:1166-1220` + subject states + appeals) | `TS:/TrustSafetyPersistence.cs:18-87` | **yes** |
| CQRS handlers | none exist — module has zero request types | file census | n/a (the `DisabledModules` entry `API:/Core/Setup/ApiProductComposition.cs:113-116` only affects the handler scan → **no behavioral effect**) |
| G-baseline | TrustSafety not in `wiring-gap-baseline.txt` | baseline grep | clean (nothing declared → nothing dead) |

**Net**: every service the module declares is resolved at runtime; the DisabledModules flag is documentation, not enforcement. This is the inverse of Consent N1 (services missing, handlers live).

## 1. Live HTTP surface (3 routes, carried by the API host — the module has no controllers)

`EconomyComplianceAdministrationController`, `api/v{version}/apiVersion}/admin/economy/compliance`, class `[Authorize]` **only** (`API:/Core/Controllers/EconomyComplianceAdministrationController.cs:36-38`):

| Route | Command (host `Core/Commands/EconomyComplianceEndpointCommands.cs`) | Control-plane call | Auth | Rate limit | HOLE? |
|---|---|---|---|---|---|
| GET `trust-safety/appeals?state=&take=` | — (service direct) | `ReadAppealsAsync` (`TS:/PostgreSqlTrustSafetyControlPlane.cs:252-283`, take 1-500) | `[Authorize]` | — | **no admin policy** (T3) |
| POST `trust-safety/appeals/{id}/assignment` | `AssignTrustSafetyAppealEndpointCommand` (`:19-21, 83-85`) | `AssignAppealAsync` (`:211-227`) | `[Authorize]` | — | **no admin policy** (T3) |
| POST `trust-safety/appeals/{id}/decisions` | `DecideTrustSafetyAppealEndpointCommand` (`:22-24, 87-89`) | `DecideAppealAsync` (`:229-250`) | `[Authorize]` | — | **no admin policy** (T3) |

Sibling surface on the same controller (FinancialCrime, out of scope here — see docs/compliance-financialcrime/).

## 2. Dead write surfaces — the reconciliation

| # | Surface | Meant to do | Deepest implementation | Callers | Verdict |
|---|---|---|---|---|---|
| S1 | `ITrustSafetyControlPlane.IngestAsync` (`TS:/PostgreSqlTrustSafetyControlPlane.cs:80-187`) | admit signed evidence events (8 kinds) into inbox/subject-state/evidence/holds | fully implemented: serializable tx, replay guards, signature verify, hold activate/release, evidence publish | **0** in `Source/` and `tests/` (grep — only interface + impl) | **hole** (T1) — the entire evidence half of the control plane is unreachable |
| S2 | `ITrustSafetyControlPlane.SubmitAppealAsync` (`:189-209`) | create appeal rows (idempotent by id + evidence hash) | fully implemented incl. replay guard | **0** | **hole** (T1) — the appeals console (§1) manages a table nothing populates |
| S3 | `TrustSafetyEvent`/`TrustSafetyAppeal` records as cross-module contracts | producers (moderation pipeline, abuse detectors) emit them | records + canonicalization + signature envelope | 0 | **planned-only** |

## 3. The actual content-moderation surface census (where "report" really lives)

| Module | Surface | Routes/commands | Ownership check | Admin gate |
|---|---|---|---|---|
| `Assets:` | report asset / review / queue | `ReportAssetCommand`, `ReviewReportCommand`, `GetModerationQueueQuery` (`Assets:/Commands/*`), admin controller `AssetsAdminController` | self-report blocked (`ReportAssetCommand.cs:51-58`) | admin policy on review (`AssetsAdminController`) |
| `Ratings:` | review reports | `POST /api/ratings/{id}/report` → `ReportRatingEndpointCommand` (`Ratings:/Controllers/RatingsController.cs:248-256`) | **none** (any authed user, unlimited) | moderation via `TenantAdmin` policy routes (`:309-377`) |
| `Courses:` | content reports | `IContentReportService` + `ContentReport` model (`Courses:/Abstractions/IContentReportService.cs`; `Models/ContentReport.cs`) | module-internal | module-internal |
| `Social.*` | none — no report endpoint on any of the 127 Social endpoints | `docs/social-platform/05-gaps.md:17-18` | — | — |
| `TS:` (this module) | none for content — governance events only | §1 | n/a | `[Authorize]`-only (T3) |

## 4. Request-type census

| Category | Count | With handler | Without handler |
|---|---|---|---|
| Module CQRS requests | 0 | — | — |
| Host endpoint commands (appeals) | 2 | 2 (host assembly scan) | 0 |
| Control-plane service methods | 5 (`Ingest`, `SubmitAppeal`, `AssignAppeal`, `DecideAppeal`, `ReadAppeals`) | n/a (not CQRS) | **2 dead** (S1, S2) |
