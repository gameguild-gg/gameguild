# Resources.Contents — Gaps

> Module `GameGuild.Resources.Contents` · `develop` @ `ce31d5850c`. KNOWN = tracked in a guard baseline or issue; NEW = first catalogued by this documentation pass. `Contents:` = `apps/api/Source/Modules/GameGuild.Resources.Contents/`, `Baseline:` = `apps/api/tests/GameGuild.API.UnitTests/Architecture/wiring-gap-baseline.txt`.

## Headline

| Id | Severity | One-liner |
|---|---|---|
| R1 | **Critical (KNOWN)** | 16 of 18 routes 500 at runtime — the whole `api/contents/versioning` read/transition surface fails EF translation (`IsDeleted` predicate), E9 baseline lines 170-176/225-232/280 (#1184-family, #1278 class 2) |
| R2 | High (NEW, latent under R1) | `cancel-schedule` can never succeed — calls `Approve` on a `Scheduled` row which the entity forbids; uncaught throw → 500 |
| R3 | High (NEW) | Scheduled publishing has no worker — `ProcessScheduledPublishingAsync` has zero callers |
| R4 | High (NEW) | `DocumentTemplate` has no API surface and no seeder — contract generation can only 404 in real environments |
| R5 | High (NEW) | submit/approve/reject/publish/reviews carry no permission gate — any authenticated member drives the editorial workflow |
| R6 | Medium (NEW) | Attribution spoofing — `CreateDraftRequest.CreatedBy` is client-supplied; actor guid falls back to `Guid.Empty` on non-guid subjects |
| R7 | Medium (NEW) | No uniqueness on `(EntityId, EntityType, VersionNumber)` nor on `IsCurrentVersion`; no pruning (`ArchiveOldVersionsAsync` unreachable) |
| R8 | Medium (NEW) | Contract auto-publish bypasses review (submit→approve→publish by the same actor, synchronously) |
| R9 | Medium (NEW) | `[AllowAnonymous]` current-version route is allowlisted with a misattributed justification ("build/version probe … exposes only the version") while it returns full published bodies |
| R10 | Medium (NEW) | Zero audit trail for the editorial workflow (no sink port, ILogger only) |
| R11 | Low (NEW) | Legacy route prefix `api/contents/versioning` off the `v{version}` convention; no `[ApiVersion]` on that controller |

## Detail

### R1 — The versioning surface is dead at runtime (KNOWN — E9 / #1278)

- Baseline entries (all tagged *"content-versioning EF query translation failure (unmapped member) -> 500"*, issue #1184-family, round 2 of 2026-11-04): `Baseline:170-176` (GET `{versionId}`, `compare`, `current` anon+authed, `history`, `version/{n}`, `pending-review`), `:225-232` (POST approve, cancel-schedule, publish, reject, reviews, schedule, submit-for-review, rollback), `:280` (PUT `drafts/{versionId}`). Issue #1278 classes them as *"EF translation failures at runtime — ratings `IsDeleted` predicate + content versioning queries throw on request"*.
- Common predicate across every failing query: `!v.IsDeleted` (e.g. `Services/ContentVersionQueryService.cs:18-21,28-30,45-50,62-67,82-85`; `Services/ContentDraftService.cs:61-62,85-87,105-110`; `Services/ContentReviewPublishingService.cs:19-21,43-45,67-69,95-97,120-122,143-145,179-181,209-211,234-237`) over the computed getter `EntityBase.IsDeleted => DeletedAt.HasValue` (`SharedKernel/Entities/EntityBase.cs:40`). The EF configurations install `HasQueryFilter(e => !e.IsDeleted)` on all three entities (`Configuration/ContentVersionEntityConfiguration.cs:34,72,100`) yet the explicit predicates restate it.
- Not failing: `POST drafts` (validation/gate short-circuits the `{}` smoke body) and the `v1/document-contracts` pair (no `IsDeleted` predicate — `Services/ContractGenerationService.cs:52-55,81-86` filters on `Status` only).
- Fix direction: predicate `DeletedAt == null` explicitly (as Content.Pages' section reads do) or map/ignore `IsDeleted` per entity; then remove the 16 baseline lines (the ratchet requires removal when fixed).

### R2 — cancel-schedule is structurally broken (NEW)

- `CancelScheduledPublishAsync` requires `Status == Scheduled` and then calls `version.Approve(GetCurrentUserId(), "Scheduled publishing cancelled")` (`Services/ContentReviewPublishingService.cs:207-228`).
- The entity's `Approve` throws unless `Status == PendingReview` (`Entities/ContentVersion.cs:163-169`) — a Scheduled row can never satisfy it. Unlike submit/approve/reject/publish, the cancel path has **no try/catch**, so the throw surfaces as 500.
- The unit suite documents the bug in its name and asserts the throw: `CancelScheduledPublishAsync_WhenScheduled_ThrowsDueToMissingEntityMethod` (`apps/api/tests/GameGuild.Resources.Contents.UnitTests/Services/ContentReviewPublishingServiceTests.cs:398`).
- Fix direction: add an entity transition `CancelSchedule()` (Scheduled→Approved) or call `SetAsCurrent`-style back-transition; wrap in the family's catch pattern.

### R3 — Scheduled publishing never fires (NEW)

- `ProcessScheduledPublishingAsync` (`Services/ContentReviewPublishingService.cs:230-272`) publishes due Scheduled versions (clearing siblings, attributing `PublishedBy ?? Guid.Empty` at `:256`) — and has **zero callers**: no route dispatches it, no `IHostedService`/`BackgroundService` references it, and it is absent from the background-workers inventory (`docs/operations/background-workers.md`). Grep across `apps/api/Source` finds references only in the interface, facade, and implementation.
- The scheduled date is therefore decoration; `ScheduledPublishAt` even has an index (`Configuration/ContentVersionEntityConfiguration.cs:44`) for a query nobody runs.

### R4 — DocumentTemplate lifecycle is unreachable (NEW)

- `IDocumentTemplateService` (`List/GetById/Create/Update/GetPublished/RenderPublishedAsync`, `Services/DocumentTemplateService.cs`) has no controller; `ContentsModule.MapEndpoints` is an empty pass-through (`ContentsModule.cs:35-39`). No seeder writes `document_templates` (`DatabaseSeeder.cs` has no reference).
- Therefore `POST v1/document-contracts/generate` in any non-test environment hits `DocumentTemplate.NotFound` or `PublishedVersionNotFound` (`Services/DocumentTemplateService.cs:183-188`) — and even hand-seeded templates lack a *published* content version because the versioning surface that would publish it is E9-dead (R1). The two gaps compound: contract generation is doubly unreachable.
- Fix direction: a minimal templates controller (create/update/list + publish-via-versioning) or a seeder for system templates.

### R5 — Editorial workflow gates only drafting/scheduling/rollback (NEW)

- Gated: `CreateDraft`/`UpdateDraft` (`content:draft|write|admin`, `Controllers/VersioningController.cs:52,74`), `SchedulePublish`/`CancelSchedule` (`content:schedule|write|admin`, `:239,260`), `Rollback` (`content:edit|write|admin`, `:304`).
- **Ungated beyond `[Authorize]`**: `SubmitForReview` (`:150`), `Approve` (`:187`), `Reject` (`:202`), `Publish` (`:217`), `AddReview` (`:321`) — and the whole read surface (`GetVersion`, `history`, `current`, `version/{n}`, `pending-review`, `compare`). Any authenticated member can read every entity's full version history (any tenant's — no tenant predicate, R6/STRIDE RS4) and drive transitions. The entity's status guards are the only workflow discipline.
- The permission keys exist in the catalog for this exact purpose (`content:draft`, `content:schedule` — `Authz:/Models/TypedPermissions.cs:216,219`); a `content:review`-style key for approve/reject/publish simply does not exist yet.

### R6 — Attribution is spoofable or empty (NEW)

- `CreateDraftRequest.CreatedBy` (`Controllers/VersioningController.cs:341`) is **client-supplied** and flows into `ContentVersion.Create` (`Services/ContentDraftService.cs:17-42`) — the draft's author is whatever the request says.
- All actor-sourced identities fall back to `Guid.Empty` when the subject is not a guid (`Services/ContentDraftService.cs:15`, `Services/ContentReviewPublishingService.cs:15`) — approvals/rejections/publishes attributable to the empty guid (tests pin the behavior: `CoverageCompletionTests.cs:664,680`).
- Contract generation is the exception — it takes `createdBy` from the actor context at the controller (`Controllers/ContractGenerationController.cs:26-34`).

### R7 — Version-number and current-version integrity (NEW)

- Index `(EntityId, EntityType, VersionNumber)` is **non-unique** (`Configuration/ContentVersionEntityConfiguration.cs:43`); `MaxAsync+1` under concurrency can mint duplicates (`Services/ContentDraftService.cs:28-30`, `Services/ContractGenerationService.cs:52-55`).
- Nothing enforces a single `IsCurrentVersion` per entity — the publish path clears siblings best-effort in one save (`Services/ContentReviewPublishingService.cs:153-161`); concurrent publishes (or the worker-less schedule path) can leave zero or multiple currents.
- History is unbounded: `ArchiveOldVersionsAsync` (`Services/ContentDraftService.cs:139-164`) has no route/worker (§3.2 of the wiring matrix).

### R8 — Contract generation bypasses review (NEW)

- With `Publish=true`, the service archives prior published versions and runs `SubmitForReview → Approve → Publish` synchronously as the *generating* actor (`Services/ContractGenerationService.cs:81-96`, approve note *"Generated contract auto-approved for publication."*). No reviewer participates; combined with R5 (publish ungated) the generated-contract path has no human gate at all. Deliberate shortcut (tests pin it), catalogued because the module's stated purpose is a *review workflow*.

### R9 — Misattributed anonymous-endpoint justification (NEW)

- `VersioningController.GetCurrentVersion` is `[AllowAnonymous]` (`Controllers/VersioningController.cs:121-122`) and is the only `VersioningController` action — the registry key `["VersioningController.GetCurrentVersion"]` (`API:/Security/AnonymousEndpointRegistry.cs:44`) therefore resolves to **this** endpoint, but its justification reads *"Build/version probe used by load balancers; exposes only the version"* — text written for a build-info controller of the same short name that no longer exists (only one `VersioningController` in `apps/api/Source`).
- The endpoint actually returns the full published version — `Title`, `Summary`, **`Body`**, `Metadata` (`Controllers/VersioningController.cs:121-131`) — for any `(entityType, entityId)`. Justification and surface disagree; either the allowlist entry should be rewritten or the route re-scoped. (Currently masked by R1's 500s — the anon E9 entry is `Baseline:172`.)

### R10 — No audit trail (NEW)

- See `04-event-inventory.md`: no sink port, bare `SaveChangesAsync`, ILogger only. Editorial decisions (approve/reject with notes, publishes, rollbacks, contract generation) produce no `AuditLog` rows — contrast the Content.Pages fail-closed pattern (`Pages:/Auditing/IContentPagesAuditEventSink.cs`).

### R11 — Route-style outlier (NEW, cosmetic)

- `Route("api/contents/[controller]")` (`Controllers/VersioningController.cs:12`) — no `v{version:apiVersion}` segment, no `[ApiVersion]`, `[controller]` token spelling; the module's other controller uses the standard `v{version}/document-contracts` + `[ApiVersion("1.0")]` (`Controllers/ContractGenerationController.cs:10-12`). OpenAPI tags still group it (`API:/Core/Extensions/OpenApiExtensions.cs:646`). Migration cost is client-breaking; recorded so the next surface audit doesn't treat the prefix as an error.
