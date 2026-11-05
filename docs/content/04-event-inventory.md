# Content.Pages — Event Inventory

> Module `GameGuild.Content.Pages` · `develop` @ `ce31d5850c`. This module publishes **no domain events and no durable integration events** — its entire observable event surface is (a) required in-transaction **audit events** funneled to the Compliance.Audit `AuditLog` table, and (b) the generated **use-case event contracts** that declare that absence explicitly.

## 1. Required audit events (the mutation contract)

Port: `IContentPagesAuditEventSink.RecordAsync(ContentPagesAuditEvent)` (`Pages:/Auditing/IContentPagesAuditEventSink.cs:9-16`; payload record `:22-31`). Implementation: `CentralContentPagesAuditEventSink` in Compliance.Audit (`Audit:/Services/CentralModuleAuditEventSinks.cs:56-86`), registered by `Audit:/Extensions/AuditModule.cs:142`. Persistence discipline (`Audit:/Services/CentralModuleAuditEventSinks.cs`, `RequiredModuleAuditRecorder.RecordAsync`):

- recorded **inside the owning command's transaction** — a missing ambient transaction throws (fail-closed, `:35-41`);
- mapped to `CreateAuditLogRequest` with `RiskLevel.Low` on success / `Medium` on failure and `AuditCategory.Data`;
- actor id/tenant id come from the ambient actor context when the payload omits them (`:30-33`);
- the mutation and the audit row commit or roll back together via `SaveWithRequiredAuditAsync` (`SharedKernel/Abstractions/AuditedMutationExtensions.cs:62`).

All 17 action types (each emitted from exactly one service site):

| # | ActionType | Emitter | Trigger route | PII discipline |
|---|---|---|---|---|
| 1 | `Content.Pages.PageCreated` | `Services/PageService.cs:76-80` | POST `v1/pages` | metadata slug/type/locale only |
| 2 | `Content.Pages.PageUpdated` | `Services/PageService.cs:113-117` | PUT `v1/pages/{id}` | slug + status |
| 3 | `Content.Pages.PageDeleted` | `Services/PageService.cs:127-131` | DELETE `v1/pages/{id}` | slug |
| 4 | `Content.Pages.PagePublished` | `Services/PageService.cs:143-147` | POST `…/{id}/publish` | slug + locale; actor = publisher |
| 5 | `Content.Pages.PageUnpublished` | `Services/PageService.cs:157-161` | POST `…/{id}/unpublish` | slug |
| 6 | `Content.Pages.PageSectionCreated` | `Services/PageService.cs:192-196` | POST `…/sections` | section type + page id |
| 7 | `Content.Pages.PageSectionUpdated` | `Services/PageService.cs:213-217` | PUT `…/sections/{id}` | page id |
| 8 | `Content.Pages.PageSectionDeleted` | `Services/PageService.cs:227-231` | DELETE `…/sections/{id}` | page id |
| 9 | `Content.Pages.PageSectionsReordered` | `Services/PageService.cs:248-252` | POST `…/sections/reorder` | count only |
| 10 | `Content.Pages.ContentResourceCreated` | `Services/ContentResourceService.cs:82-86` | POST `v1/content-resources` | slug/type/locale |
| 11 | `Content.Pages.ContentResourceUpdated` | `Services/ContentResourceService.cs:120-124` | PUT `v1/content-resources/{id}` | slug + status |
| 12 | `Content.Pages.ContentResourceDeleted` | `Services/ContentResourceService.cs:134-138` | DELETE `v1/content-resources/{id}` | slug |
| 13 | `Content.Pages.ContentResourcePublished` | `Services/ContentResourceService.cs:150-154` | POST `…/{id}/publish` | slug + locale; actor = publisher |
| 14 | `Content.Pages.MarketingLeadCreated` | `Services/MarketingLeadService.cs:31-35` | POST `v1/marketing/leads` | **shape only** — source/topic/plan/locale/pagePath, never name/email/message |
| 15 | `Content.Pages.CourseProposalCreated` | `Services/CourseProposalService.cs:29-34` | POST `v1/content/course-proposals` | **shape only** — slug + status, never proposer name/email |
| 16 | `Content.Pages.CourseProposalUpdated` | `Services/CourseProposalService.cs:132-137` | PUT `…/{id}` | slug + previous/current status |
| 17 | `Content.Pages.CourseProposalDeleted` | `Services/CourseProposalService.cs:152-157` | DELETE `…/{id}` | slug |

**Not audited** (write paths without an audit event): `IncrementViewCountAsync` (`Services/ContentResourceService.cs:158-165` — plain `SaveChangesAsync`, unauthenticated write-on-read) and every **read** path. Anonymous-submission audits carry `ActorUserId = null` by design (`Services/CourseProposalService.cs:31`, `Services/MarketingLeadService.cs:33`).

Test lock-in: `ContentPagesMutationAuditTests.cs` asserts every mutation family emits its event (`:16,42,69,89`) and that audit failure rolls the mutation back (`:112,125`).

## 2. Use-case event contracts (declared no-event surface)

`Architecture/UseCaseEventContracts.g.cs` (source-generated) declares one contract id per command, each with `NoDomainEventReason = "No named cross-module event is declared; the durable generic operation event remains the observable contract."` — i.e. the codebase's own registry states these use cases emit nothing beyond the generic operation/audit trail:

`content.pages.create-content-resource`, `…create-course-proposal`, `…create-marketing-lead`, `…create-page`, `…create-page-section`, `…delete-course-proposal`, `…delete-content-resource`, `…delete-page`, `…delete-page-section`, `…publish-content-resource`, `…publish-page`, `…reorder-page-sections`, `…unpublish-page`, `…update-course-proposal`, `…update-content-resource`, `…update-page`, `…update-page-section` (17 ids; `Pages:/Architecture/UseCaseEventContracts.g.cs:3-19`).

## 3. Integration events / outbox: none

- No record in this module implements `IDurableIntegrationEvent`; nothing adds to `IHasIntegrationEvents.IntegrationEvents`; the platform outbox drain in `ApplicationDbContext.SaveChangesAsync` (`API:/Database/ApplicationDbContext.cs`) therefore never carries a Content.Pages payload.
- Consequence: publishing a page or resource is **invisible to other modules** — no cache-invalidation event for the web marketing site (it relies on `ResponseCache` 300 s on `/og` and `no-store` fetches on the proposal detail), no search-index event, no webhook. Cross-module sequencing is purely synchronous request/response (see `05-cross-module-sequences.md`).
- G4 baseline cross-check: no `G4|GameGuild.Content.Pages…` entries exist (no dead domain events declared).

## 4. Write-on-read side effect

`GET v1/content-resources/by-slug/{slug}` increments `ViewCount` inside the read request (`Controllers/ContentResourceController.cs:95-98` — awaited to avoid disposing the scoped DbContext mid-protocol, per the inline comment). It is the module's only mutation outside the audit regime; because it is anonymous and per-request, counts are crawler-inflatable and the double-write widens the read path's failure surface (a failed increment 500s a public read; `Services/ContentResourceService.cs:158-165` has no try/catch). See STRIDE T4 and gap C7.
