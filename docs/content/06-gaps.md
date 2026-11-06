# Content.Pages — Gaps

> Module `GameGuild.Content.Pages` · `develop` @ `ce31d5850c`. KNOWN = already tracked in a guard baseline / issue; NEW = first catalogued by this documentation pass. `Pages:` = `apps/api/Source/Modules/GameGuild.Content.Pages/`.

## Headline

| Id | Severity | One-liner |
|---|---|---|
| C1 | High | Editorial permission gate exists only on `PUT` — create/delete/publish/unpublish and all section mutations are `[Authorize]`-only |
| C2 | High | Soft-delete is invisible to reads — no query filter, no `DeletedAt` predicate on `Page`/`ContentResource` aggregates |
| C3 | Medium | `ScheduledPublishAt` is stored but nothing ever promotes a scheduled page (no worker, no command) |
| C4 | Medium | Section sub-resource reads/writes ignore parent-page publication status — draft-page blocks leak to any member |
| C5 | Medium | `UpdatePageDto.Status` raw patch performs arbitrary lifecycle transitions without publish bookkeeping |
| C6 | Medium | Marketing leads: `reviewed`/`archived` unreachable; no delete/erasure route for stored PII |
| C7 | Low | View-count increment is an unauthenticated, unthrottled, unaudited write-on-read |
| C8 | Low | Course proposals: reviewer surface is `[Authorize]`-only (no permission key) and hard delete has no erasure discipline for proposer PII |
| C9 | Low | No integration events — publication is invisible to other modules (web caches rely on TTL/no-store) |

## Detail

### C1 — Permission gates cover only `PUT` (NEW)

- `CanEditContent` (`content:edit|write|admin`) is checked in exactly three places: `UpdatePage` (`Controllers/PageController.cs:137`), `UpdateSection` (`:243`), `Update` resource (`Controllers/ContentResourceController.cs:122`).
- Ungated mutations beyond class `[Authorize]`: `CreatePage` (`Controllers/PageController.cs:119-129`), `DeletePage` (`:157-167`), `Publish` (`:170-186` — only requires a guid subject), `Unpublish` (`:189-199`), `CreateSection` (`:212-222`), `DeleteSection` (`:263-272`), `ReorderSections` (`:275-280`), `CreateContentResource` (`Controllers/ContentResourceController.cs:104-114`), `Delete` (`:142-152`), `Publish` (`:155-171`), `UpdateCourseProposal`/`DeleteCourseProposal` (`Controllers/CourseProposalsController.cs:80-125`).
- Effect: any authenticated member (any tenant, any role) can create pages, soft-delete the pricing page, publish a draft, or archive the proposal queue. The gating tests only pin the PUT family (`EditorialPermissionGatingTests.cs:20-85`).
- Fix direction: hoist `CanEditContent`/a `content:write` gate onto all mutating actions (or a controller-level policy attribute), keep `CanViewUnpublished` for reads.

### C2 — Soft-delete is cosmetic for reads (NEW)

- `DeleteAsync` sets `DeletedAt` via `SoftDelete()` (`Services/PageService.cs:126`, `Services/ContentResourceService.cs:133`; `EntityBase.SoftDelete`, `SharedKernel/Entities/EntityBase.cs:93-105`).
- No `HasQueryFilter` exists in `PageConfiguration`/`ContentResourceConfiguration` (`Pages:/Configuration/PageConfiguration.cs`, `ContentResourceConfiguration.cs`), and the read paths do not predicate `DeletedAt`: `GetByIdAsync` (`Services/PageService.cs:15-19`), `GetBySlugAsync` (`:21-26`), `GetPagesAsync` (`:28-45`), `ContentResourceService.GetByIdAsync/GetBySlugAsync/ListAsync` (`:12-50`), `OpenGraphService.ResolveAsync` (`Services/OpenGraphService.cs:14-26`).
- Effect: deleted pages keep serving at their slug, in listings, in the sitemap and in OG resolution; `DELETE` is not a takedown. Only `GetSectionsAsync` filters deleted *sections* (`Services/PageService.cs:169`), and the page-level includes filter deleted sections while the page itself still resolves (`:17,23`).
- Fix direction: add `HasQueryFilter(e => e.DeletedAt == null)` to both configurations (mind the orphaned-filter rule for inherited base filters) or predicate `DeletedAt` in every read.

### C3 — Scheduled publishing is inert (NEW)

- `Page.ScheduledPublishAt` (`Entities/Page.cs:166`) and `ContentResource.ScheduledPublishAt` (`Entities/ContentResource.cs:176`) are write-only via create/update (`Services/PageService.cs:111`, `Services/ContentResourceService.cs:117`).
- Nothing reads them: no `IHostedService`/background worker references them (checked `docs/operations/background-workers.md` inventory scope; no `ScheduledPublishAt` consumer in `apps/api/Source` outside these entities), no command, no query surface. Compare `Resources.Contents`, which at least has a `ProcessScheduledPublishingAsync` implementation — itself uncalled (its package's R4).

### C4 — Section surface ignores parent status (NEW)

- `GetSections`/`GetSection` (`Controllers/PageController.cs:204-235`) and the mutating section routes never check the parent page's `Status` or `CanViewUnpublished`; `CreateSectionAsync` does not even verify the page exists (FK violation → 500, `Services/PageService.cs:191`).
- Effect: a member who cannot see a draft page by id (404 via `:65-68`) can still enumerate its full block payload via `GET …/sections`, and mutate it per C1.

### C5 — Raw status patch bypasses publish semantics (NEW)

- `UpdatePageDto.Status` / `UpdateContentResourceDto.Status` are applied verbatim (`Services/PageService.cs:93`, `Services/ContentResourceService.cs:100`): a PUT can go Archived→Published without `PublishedBy`/`PublishedAt` (`Services/PageService.cs:140-142` sets those only in `PublishAsync`), or flip to `Archived` without any audit wording distinguishing it from a metadata edit (both log `…Updated` with a status metadata pair, `:113-117`).
- Fix direction: drop `Status` from the update DTOs (route all transitions through publish/unpublish) or replicate the publish bookkeeping in the patch path.

### C6 — Marketing-lead lifecycle and PII retention (NEW)

- No route mutates `MarketingLead` after creation (`Controllers/MarketingLeadController.cs` — GET/GET/POST only): `MarketingLeadStatuses.Reviewed/Archived` (`Entities/MarketingLead.cs:21-31`) are unreachable, making the status filter of the CRM list (`Services/MarketingLeadService.cs:69-71`) dead UI vocabulary.
- The table stores `Name`, `Email`, `Company`, `Topic`, `Message`, `UserAgent` (`Entities/MarketingLead.cs:65-93`) with no DELETE, no export, no retention job — indefinite PII retention with no erasure path (GDPR/FG-LPD exposure). Audit events are shape-only (good, `Services/MarketingLeadService.cs:29-35`); the row itself is the liability.

### C7 — View-count write-on-read (NEW)

- `IncrementViewCountAsync` (`Services/ContentResourceService.cs:158-165`) runs on every anonymous by-slug GET (`Controllers/ContentResourceController.cs:95-101`): no audit event, no per-route rate limit (global policy only), crawler-inflatable, and a failed write 500s a public read. Batched/atomic (`UPDATE … SET ViewCount = ViewCount + 1`) or event-driven counting would decouple it.

### C8 — Proposal reviewer surface and hard delete (NEW)

- CRM reads/writes (`Controllers/CourseProposalsController.cs:19-125`) require only authentication — no `content:*` key gates the review pipeline (contrast the EditorialEdit pattern two controllers over), so every member can list proposals including `ProposerName`/`ProposerEmail` and drive status transitions.
- `DeleteAsync` hard-deletes (`Services/CourseProposalService.cs:151`) with an audit row as the only trace; there is no erasure-vs-retention policy for the proposer PII that the row held.

### C9 — No integration events (NEW, design note)

- Publication/unpublication emit nothing on the platform outbox (`04-event-inventory.md` §3). Downstream consumers that would plausibly want a signal (web marketing-site cache purge, search indexing, analytics) currently rely on `ResponseCache` 300 s (`Controllers/OpenGraphController.cs:23`) and `no-store` fetches. Declared intentional by the use-case contracts (`NoDomainEventReason`), so catalogued as a boundary note rather than a defect.

## KNOWN cross-references (no action here)

- **E9 route smoke**: zero 5xx entries for this module's routes (baseline `apps/api/tests/GameGuild.API.UnitTests/Architecture/wiring-gap-baseline.txt` — no `pages`/`content-resources`/`marketing`/`course-proposals`/`og` keys); the module is clean on the wiring ratchet.
- **G7/G8**: zero dead request types / planned-422 leaks (see `02-cqrs-wiring-matrix.md` §6).
- **Anonymous registry**: the eight public actions are allowlisted with justifications (`API:/Security/AnonymousEndpointRegistry.cs:98-102,196-198`) — reviewed surface, not drift.
- **Contract matrix**: 31 of 32 generated client operations have no web caller (`docs/architecture/api-contract-matrix.md:330`) — the marketing-site contract is intentionally broader than today's frontend.
