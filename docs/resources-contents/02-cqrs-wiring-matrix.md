# Resources.Contents — CQRS Wiring Matrix

> Module `GameGuild.Resources.Contents` · `develop` @ `ce31d5850c`. Route → command → handler → service → auth → rate limit → tests, then §3 dead-surface reconciliation.
> **Auth legend**: `A` = class `[Authorize]`; `AA` = `[AllowAnonymous]`; `DRAFT` = `content:draft|write|admin` code gate (403); `SCHED` = `content:schedule|write|admin`; `EDIT` = `content:edit|write|admin` (arrays at `Controllers/VersioningController.cs:22-24`). **Rate limit**: `global` policy everywhere (no `[EnableRateLimiting]`).
> **Registration**: services + 2 explicit handler registrations via `ContentsModule` (`API:/Core/Setup/InfrastructureLayerExtensions.cs:255-258`); the 10 versioning handlers register via the CQRS assembly scan (`API:/Core/Setup/ApplicationLayerExtensions.cs:142`; enabled per `API:/Core/Setup/ModuleConfiguration.cs:37`).

## 1. VersioningController — `api/contents/versioning` (`Controllers/VersioningController.cs:12`; legacy unversioned prefix, no `[ApiVersion]`)

| # | Route | Command | Handler → service | Auth | E9 | Test |
|---|---|---|---|---|---|---|
| 1 | POST `api/contents/versioning/drafts` | `CreateContentDraftCommand` (`Commands/ContentVersionMutationCommands.cs:5`) | `:16` → `ContentDraftService.CreateDraftAsync` | A + **DRAFT 403** (`:52`) | clean | `VersioningEditorialPermissionGatingTests.cs`; `Services/ContentDraftServiceTests.cs` |
| 2 | PUT `api/contents/versioning/drafts/{versionId}` | `UpdateContentDraftCommand` (`:6`) | `:35` → `UpdateDraftAsync` | A + **DRAFT 403** (`:74`) | **500** (Baseline:280) | `ContentDraftServiceTests.cs`; `VersioningController tests` in `CoverageCompletionTests.cs:547` |
| 3 | GET `api/contents/versioning/{versionId}` | service-direct `IContentVersioningService.GetVersionAsync` | facade → `ContentVersionQueryService.GetVersionAsync` | A | **500** (Baseline:170) | `ContentVersionQueryServiceTests.cs:104,120` |
| 4 | GET `…/entity/{entityType}/{entityId}/history` | service-direct `GetVersionHistoryAsync` | → `ContentVersionQueryService` | A | **500** (Baseline:174) | `ContentVersionQueryServiceTests.cs:37,59,82` |
| 5 | GET `…/entity/{entityType}/{entityId}/current` | service-direct `GetCurrentVersionAsync` | → `ContentVersionQueryService` | **AA** (`:122`; registry `API:/Security/AnonymousEndpointRegistry.cs:44` — misattributed text, see R9) | **500** anon+authed (Baseline:172-173) | `ContentVersionQueryServiceTests.cs:180,208` |
| 6 | GET `…/entity/{entityType}/{entityId}/version/{versionNumber}` | service-direct `GetVersionByNumberAsync` | → `ContentVersionQueryService` | A | **500** (Baseline:175) | `ContentVersionQueryServiceTests.cs:138,159` |
| 7 | POST `…/{versionId}/submit-for-review` | `SubmitContentForReviewCommand` (`:7`) | `:52` → `ContentReviewPublishingService.SubmitForReviewAsync` | A — **no gate** (R5) | **500** (Baseline:231) | `ContentReviewPublishingServiceTests.cs:63,81,98` |
| 8 | GET `…/pending-review` | service-direct `GetPendingReviewAsync` | → `ContentReviewPublishingService` | A | **500** (Baseline:176) | `ContentReviewPublishingServiceTests.cs:193,218` |
| 9 | POST `…/{versionId}/approve` | `ApproveContentVersionCommand` (`:8`) | `:59` → `ApproveAsync` | A — **no gate** (R5) | **500** (Baseline:225) | `ContentReviewPublishingServiceTests.cs:116,135` |
| 10 | POST `…/{versionId}/reject` | `RejectContentVersionCommand` (`:9`) | `:66` → `RejectAsync` | A — **no gate** (R5) | **500** (Baseline:228) | `ContentReviewPublishingServiceTests.cs:156,174` |
| 11 | POST `…/{versionId}/publish` | `PublishContentVersionCommand` (`:10`) | `:73` → `PublishAsync` | A — **no gate** (R5) | **500** (Baseline:227) | `ContentReviewPublishingServiceTests.cs:285,306,332` |
| 12 | POST `…/{versionId}/schedule` | `ScheduleContentPublishCommand` (`:11`) | `:80` → `SchedulePublishAsync` | A + **SCHED 403** (`:239`) | **500** (Baseline:230) | `ContentReviewPublishingServiceTests.cs:353,374` |
| 13 | POST `…/{versionId}/cancel-schedule` | `CancelContentPublishCommand` (`:12`) | `:87` → `CancelScheduledPublishAsync` | A + **SCHED 403** (`:260`) | **500** (Baseline:226) — doubly broken, see R2 | `ContentReviewPublishingServiceTests.cs:398,419` |
| 14 | GET `…/compare` | service-direct `CompareVersionsAsync` | → `ContentVersionQueryService` | A | **500** (Baseline:171) | `ContentVersionQueryServiceTests.cs:229,254,270,288` |
| 15 | POST `…/entity/{entityType}/{entityId}/rollback` | `RollbackContentVersionCommand` (`:13`) | `:94` → `ContentDraftService.RollbackAsync` | A + **EDIT 403** (`:304`) | **500** (Baseline:232) | `ContentDraftServiceTests.cs` (rollback suite) |
| 16 | POST `…/{versionId}/reviews` | `AddContentVersionReviewCommand` (`:14`) | `:106` → `AddReviewAsync` | A — **no gate** (R5) | **500** (Baseline:229) | `ContentReviewPublishingServiceTests.cs:243,267` |

## 2. ContractGenerationController — `v{version}/document-contracts` (`Controllers/ContractGenerationController.cs:10-12`)

| # | Route | Command | Handler → service | Auth | E9 | Test |
|---|---|---|---|---|---|---|
| 17 | POST `v1/document-contracts/generate` | `GenerateContractCommand` (`Commands/GenerateContractCommands.cs:5`) | `:36` → `ContractGenerationService.GenerateAsync` | A + actor-guid 401 | clean | `CoverageCompletionTests.cs:249,259,292,448,695` |
| 18 | POST `v1/document-contracts/generate:bulk` | `BulkGenerateContractsCommand` (`:9`) | `:47` → loop `GenerateAsync` | A + actor-guid 401 + non-empty 400 | clean | `CoverageCompletionTests.cs:310,320,330,467,495` |

## 3. Dead-surface reconciliation

### 3.1 E9 — the 16 baselined 5xx routes (KNOWN, #1184-family / #1278 class 2)

`Baseline:` lines 170-176, 225-232, 280 record every VersioningController route except `POST drafts` as `EF query translation failure (unmapped member) -> 500`, tagged *"content-versioning EF query translation failure (unmapped member)"* and attributed to the `#1184`-family inventory (round 2, 2026-11-04). Issue **#1278** ("E9 route-smoke sweep — 131 failing route/principal combos") classes them under **"EF translation failures at runtime — ratings `IsDeleted` predicate + content versioning queries throw on request"**.

Reconciliation of the failing set against the queries each route executes:

| Failing routes (baseline line) | Query | Common predicate |
|---|---|---|
| :170, :174, :175, :280 (`GetVersion`, `history`, `version/{n}`, `PUT drafts`) | `FirstOrDefaultAsync/ToListAsync(v.Id == … && … && !v.IsDeleted)` / `Where(!v.IsDeleted)` (`Services/ContentVersionQueryService.cs:18-58`; `ContentDraftService.cs:61-62`) | `!v.IsDeleted` |
| :171 (`compare`) | two `FirstOrDefaultAsync(… && !v.IsDeleted)` (`ContentVersionQueryService.cs:82-85`) | same |
| :172-173 (`current`, anon+authed) | `FirstOrDefaultAsync(… IsCurrentVersion && !v.IsDeleted)` (`:60-67`) | same |
| :176 (`pending-review`) | `Where(!v.IsDeleted).Where(Status == PendingReview)` (`ContentReviewPublishingService.cs:95-97`) | same |
| :225-232 (approve/reject/publish/schedule/submit/reviews/rollback/cancel) | the transition prologue `FirstOrDefaultAsync(v.Id == … && !v.IsDeleted)` (`:19-21,43-45,67-69,95,121,144,180,209,236`; `ContentDraftService.cs:105-110`) | same |

`IsDeleted` is the computed getter `DeletedAt.HasValue` (`SharedKernel/Entities/EntityBase.cs:40`) referenced in explicit predicates — the E9 diagnosis points at this member (mechanism per baseline comment; the InMemory-backed smoke host shares the translating pipeline, so unmapped-member predicates fail there too). `POST drafts` is absent from the failing set — its `{}` smoke body dies earlier on validation/gate paths, not on the predicate. **G7/G8 cross-check**: no `G7|GameGuild.Resources.Contents…` baseline entries — all 12 request types have handlers and register (10 via scan, 2 explicitly); no planned-422 leaks.

### 3.2 Service surface with no route (dead-by-absence)

| Member | Purpose | Reachable today |
|---|---|---|
| `IContentVersioningService.GetDraftAsync` (`Services/ContentVersioningService.cs:26-27`) | fetch the open draft for an entity | **no route** — authors must track the draft's versionId themselves |
| `IContentVersioningService.ArchiveOldVersionsAsync` (`:92-94`; impl `ContentDraftService.cs:139-164`) | prune published/archived history beyond keepCount | **no route, no worker** — unbounded version growth (gap R7) |
| `IContentVersioningService.ProcessScheduledPublishingAsync` (`:60-61`; impl `ContentReviewPublishingService.cs:230-272`) | auto-publish due Scheduled versions | **no route, no worker, no `IHostedService`** — gap R3 |
| `IDocumentTemplateService` create/update/list/get (`Services/DocumentTemplateService.cs:8-141`) | template lifecycle | **no controller, no seeder** — contract generation 404s outside tests (gap R4) |

### 3.3 Declared-but-unused framework surface

- `IVersionable` (`Entities/ContentVersion.cs:262-267`) has **zero implementors** in `apps/api/Source` — the intended "entities that support versioning" contract never landed.
- `RenderPublishedAsync(keepUnknownPlaceholders)` (`Services/DocumentTemplateService.cs:169`) — both branches return the raw match; the flag is dead.
- `ContentVersion.Metadata` JSONB is written by contract generation (`Services/ContractGenerationService.cs:57-66`) and compared as a blob (`ContentVersionQueryService.cs:104-105`) — never interpreted.
- Client usage: **zero web callers** — no `api/contents`, `document-contracts`, or versioning fetch exists in `apps/web/src`; the contract-matrix content row counts 31 of 32 operations dead (`docs/architecture/api-contract-matrix.md:330`), and the single "gen" caller is the course-proposals surface of Content.Pages, not this module.
