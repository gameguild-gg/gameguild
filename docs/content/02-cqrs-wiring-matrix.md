# Content.Pages — CQRS Wiring Matrix

> Module `GameGuild.Content.Pages` · `develop` @ `ce31d5850c`. Every controller route → command → handler → service → auth → rate limit → tests.
> **Auth legend**: `A` = class-level `[Authorize]`; `AA` = `[AllowAnonymous]`; `CM` = code gate `CanViewUnpublished` (`content:read|write|admin`, checked for visibility only); `EDIT` = code gate `CanEditContent` (`content:edit|write|admin`, returns 403). Gates are **code-level `ActorContext.HasAnyPermission` checks, not policies** — there is no `[Authorize(Policy = ...)]` in this module. System admins bypass via `ActorContext` (`Controllers/PageController.cs:29-30`).
> **Rate limit**: no route carries `[EnableRateLimiting]`; all routes resolve the `global` policy (`API:/Core/Extensions/RateLimitingServiceCollectionExtensions.cs:121`).
> **Registration**: handlers via CQRS assembly scan (`API:/Core/Setup/ApplicationLayerExtensions.cs:142`); services via `AddContentPagesModule` (`API:/Core/Setup/InfrastructureLayerExtensions.cs:335`); audit sink via Compliance.Audit (`Audit:/Extensions/AuditModule.cs:142`).

## 1. PageController — `v{version}/pages` (`Controllers/PageController.cs:12`)

| # | Route | Command / path | Handler | Auth | Test |
|---|---|---|---|---|---|
| 1 | GET `v1/pages` | `IPageService.GetPagesAsync` (service-direct) | — | A + CM forces `status=Published` | `CoverageCompletionTests.cs` (service) |
| 2 | GET `v1/pages/{id}` | `IPageService.GetByIdAsync` | — | A + CM draft→404 | `ContentPagesTests.cs:251+` (mapping) |
| 3 | GET `v1/pages/by-slug/{slug}` | `IPageService.GetBySlugAsync(publishedOnly=!CM)` | — | **AA** (registry L98) | `ContentPagesTests.cs` |
| 4 | GET `v1/pages/sitemap` | `IPageService.GetPagesAsync(Published, take 5000)` | — | **AA** (registry L99) | — |
| 5 | POST `v1/pages` | `CreatePageCommand` (`ContentMutationCommands.cs:8`) | `CreatePageCommandHandler` (`:42`) → `PageService.CreateAsync` | A — **no permission gate** (C1) | `ContentPagesMutationAuditTests.cs:16` |
| 6 | PUT `v1/pages/{id}` | `UpdatePageCommand` (`:9`) | `UpdatePageCommandHandler` (`:48`) → `UpdateAsync` | A + **EDIT 403** | `EditorialPermissionGatingTests.cs:20,37,65,85` |
| 7 | DELETE `v1/pages/{id}` | `DeletePageCommand` (`:10`) | `DeletePageCommandHandler` (`:54`) → `DeleteAsync` (soft) | A — **no gate** (C1) | `ContentPagesMutationAuditTests.cs:16` |
| 8 | POST `v1/pages/{id}/publish` | `PublishPageCommand` (`PublishPageCommand.cs:5`) | `PublishPageCommandHandler` (guard empty GUIDs) → `PublishAsync` | A + subject-guid required (401) — **no content permission** (C1) | `CoverageCompletionTests.cs:336` |
| 9 | POST `v1/pages/{id}/unpublish` | `UnpublishPageCommand` (`:11`) | `UnpublishPageCommandHandler` (`:60`) → `UnpublishAsync` | A — **no gate** (C1) | `ContentPagesMutationAuditTests.cs:16` |
| 10 | GET `v1/pages/{pageId}/sections` | `IPageService.GetSectionsAsync` | — | A — **no page-status/CM check** (C4) | `ContentPagesTests.cs` |
| 11 | POST `v1/pages/{pageId}/sections` | `CreatePageSectionCommand` (`:12`) | `CreatePageSectionCommandHandler` (`:66`) → `CreateSectionAsync` | A — **no gate** (C1/C4) | `ContentPagesMutationAuditTests.cs:42` |
| 12 | GET `v1/pages/{pageId}/sections/{sectionId}` | `IPageService.GetSectionByIdAsync` + route PageId match | — | A — same as #10 | — |
| 13 | PUT `v1/pages/{pageId}/sections/{sectionId}` | `UpdatePageSectionCommand` (`:13`) | `UpdatePageSectionCommandHandler` (`:73`, re-checks PageId) → `UpdateSectionAsync` | A + **EDIT 403** | `EditorialPermissionGatingTests.cs` |
| 14 | DELETE `v1/pages/{pageId}/sections/{sectionId}` | `DeletePageSectionCommand` (`:14`) | `DeletePageSectionCommandHandler` (`:84`, PageId match) → `DeleteSectionAsync` (soft) | A — **no gate** (C1/C4) | `ContentPagesMutationAuditTests.cs:42` |
| 15 | POST `v1/pages/{pageId}/sections/reorder` | `ReorderPageSectionsCommand` (`:15`) | `ReorderPageSectionsCommandHandler` (`:95`) → `ReorderSectionsAsync` | A — **no gate** (C1/C4) | `ContentPagesMutationAuditTests.cs:42` |

## 2. ContentResourceController — `v{version}/content-resources` (`Controllers/ContentResourceController.cs:12`)

| # | Route | Command / path | Handler | Auth | Test |
|---|---|---|---|---|---|
| 16 | GET `v1/content-resources` | `IContentResourceService.ListAsync` | — | **AA** (registry L100) + CM forces Published | `ContentPagesTests.cs` |
| 17 | GET `v1/content-resources/{id}` | `GetByIdAsync` | — | A + CM draft/in-review→404 | — |
| 18 | GET `v1/content-resources/by-slug/{slug}` | `GetBySlugAsync(publishedOnly=!CM)` + `IncrementViewCountAsync` | — | **AA** (registry L101) | — |
| 19 | POST `v1/content-resources` | `CreateContentResourceCommand` (`ContentMutationCommands.cs:5`) | `CreateContentResourceCommandHandler` (`:21`) → `CreateAsync` | A — **no gate** (C1) | `ContentPagesMutationAuditTests.cs:69` |
| 20 | PUT `v1/content-resources/{id}` | `UpdateContentResourceCommand` (`:6`) | `UpdateContentResourceCommandHandler` (`:28`) → `UpdateAsync` | A + **EDIT 403** | `EditorialPermissionGatingTests.cs:20,65` |
| 21 | DELETE `v1/content-resources/{id}` | `DeleteContentResourceCommand` (`:7`) | `DeleteContentResourceCommandHandler` (`:35`) → `DeleteAsync` (soft) | A — **no gate** (C1) | `ContentPagesMutationAuditTests.cs:69` |
| 22 | POST `v1/content-resources/{id}/publish` | `PublishContentResourceCommand` (`PublishContentResourceCommand.cs`) | `PublishContentResourceCommandHandler` → `PublishAsync` | A + subject-guid (401) — **no content permission** (C1) | `CoverageCompletionTests.cs:357` |

## 3. CourseProposalsController — `v{version}/content/course-proposals` (`Controllers/CourseProposalsController.cs:16`)

| # | Route | Command / path | Handler | Auth | Test |
|---|---|---|---|---|---|
| 23 | GET `v1/content/course-proposals` | `ICourseProposalService.ListAsync` | — | A — **no content permission** (I2) | `CourseProposalTests.cs:180,252` |
| 24 | GET `v1/content/course-proposals/{id}` | `GetByIdAsync` | — | A — no permission key | `CourseProposalTests.cs:252` |
| 25 | GET `v1/content/course-proposals/by-slug/{slug}` | `GetPublishedBySlugAsync` | — | **AA** (registry L198) | `CourseProposalTests.cs:118` |
| 26 | POST `v1/content/course-proposals` | `CreateCourseProposalCommand` (`:17`) | `CreateCourseProposalCommandHandler` (`:114`) → `CreateAsync` | **AA** (registry L197) | `CourseProposalTests.cs:96,284,295` |
| 27 | PUT `v1/content/course-proposals/{id}` | `UpdateCourseProposalCommand` (`:18`) | `UpdateCourseProposalCommandHandler` (`:121`) → `UpdateAsync` | A + status-vocabulary 400 — **no transition/permission gate** | `CourseProposalTests.cs:149,323,334` |
| 28 | DELETE `v1/content/course-proposals/{id}` | `DeleteCourseProposalCommand` (`:19`) | `DeleteCourseProposalCommandHandler` (`:128`) → `DeleteAsync` (**hard**) | A — **no gate** (C1 family) | `CourseProposalTests.cs:207` |

## 4. MarketingLeadController — `v{version}/marketing/leads` (`Controllers/MarketingLeadController.cs:12`)

| # | Route | Command / path | Handler | Auth | Test |
|---|---|---|---|---|---|
| 29 | GET `v1/marketing/leads` | `IMarketingLeadService.ListAsync` | — | A — **no permission key** (I3) | `CoverageCompletionTests.cs:108` |
| 30 | GET `v1/marketing/leads/{id}` | `GetByIdAsync` | — | A — no permission key | `CoverageCompletionTests.cs:108` |
| 31 | POST `v1/marketing/leads` | `CreateMarketingLeadCommand` (`:16`) | `CreateMarketingLeadCommandHandler` (`:106`) → `CreateAsync` | **AA** (registry L196) + controller validation 400 | `CoverageCompletionTests.cs:129,140,165,185` |

## 5. OpenGraphController — `v{version}/og` (`Controllers/OpenGraphController.cs:14`)

| # | Route | Command / path | Handler | Auth | Test |
|---|---|---|---|---|---|
| 32 | GET `v1/og/{*slug}` | `IOpenGraphService.ResolveAsync` | — | **AA** + `OpenGraphController.*` wildcard registry entry (L102); `ResponseCache` 300 s | `CoverageCompletionTests.cs:315` |

## 6. Dead-surface reconciliation (G7/G8/E9 cross-check)

- **G7 (request types with no handler): zero.** All 17 command records have co-located handlers (`Commands/ContentMutationCommands.cs`, `Commands/PublishPageCommandHandler.cs`, `Commands/PublishContentResourceCommandHandler.cs`); none appear in `G7|` baseline entries (`apps/api/tests/GameGuild.API.UnitTests/Architecture/wiring-gap-baseline.txt:86-93` lists only Subscriptions/Ledgers types).
- **G8 (planned-422 advertised surface): zero** — no `IgnoreApi`/planned-unavailable commands dispatched here.
- **E9 (route smoke 5xx): zero entries** for `/v1/pages`, `/v1/content-resources`, `/v1/content/course-proposals`, `/v1/marketing/leads`, `/v1/og` in the baseline — the module's 32 routes answered without 5xx in the round-2 sweep (2026-11-04).
- **Queries: none declared.** Reads are service-direct from controllers (no `IQuery<,>` records) — so no dead-query class is *possible*; the trade-off is that read paths bypass the CQRS pipeline behaviors entirely.
- **Declared-but-unused surface inside the module**: `content:read`'s companion `content:edit-metadata` key (`Authz:/Models/TypedPermissions.cs:222`, issue #333) is not referenced by any Content.Pages gate; `MarketingLeadStatuses.Reviewed/Archived` (`Entities/MarketingLead.cs:21-31`) and `Page.ScheduledPublishAt` / `ContentResource.ScheduledPublishAt` (`Entities/Page.cs:166`, `Entities/ContentResource.cs:176`) are write-able but never consumed — see `06-gaps.md` C3/C6.
- **Client usage**: only 3 of the 32 routes have web callers — `POST /v1/marketing/leads` (`apps/web/src/lib/contact/actions.ts:82`, `apps/web/src/lib/content-creators/actions.ts:105`, `apps/web/src/lib/proposals/actions.ts:105`), `POST /v1/content/course-proposals` (`apps/web/src/lib/proposals/actions.ts:166`), `GET …/by-slug` (`apps/web/src/lib/proposals/actions.ts:69`); contract-matrix row "content (pages/versioning/marketing) — 32 endpoints, 1 gen, 31 dead" (`docs/architecture/api-contract-matrix.md:330`). The pages/OG/catalog surfaces are the marketing-site/SEO contract kept deliberately public.
