# Resources.Contents — Event Inventory

> Module `GameGuild.Resources.Contents` · `develop` @ `ce31d5850c`. This is a **zero-event module**: no domain events, no durable integration events, and — unlike Content.Pages, Learning, and the money modules — **no required-audit port**. The observable contract is the database row itself plus `ILogger` lines.

## 1. Audit events: none

- No `I…AuditEventSink` port exists in the module (contrast `Pages:/Auditing/IContentPagesAuditEventSink.cs`); no `SaveWithRequiredAuditAsync` call (the module uses bare `SaveChangesAsync` — `Services/ContentDraftService.cs:44,75,158`, `ContentReviewPublishingService.cs:30,54,77,136,164,195,224,269`, `ContentVersionQueryService.cs` (none — read-only), `ContractGenerationService.cs:99`, `DocumentTemplateService.cs:83,107`).
- Consequently **publishing, approving, rejecting, scheduling, rolling back and generating contracts leave no `AuditLog` row**: no who/when beyond the version columns (`ReviewedBy`, `PublishedBy`), no tamper-evident trail for the editorial workflow, no fail-closed coupling. See gap R10 and STRIDE family RS6.

## 2. Domain / integration events: none

- `ContentVersion` inherits `IHasDomainEvents`/`IHasIntegrationEvents` via `EntityBase` but nothing ever adds to those collections (no `AddDomainEvent`/integration-event construction anywhere in the module). The platform outbox drain in `ApplicationDbContext.SaveChangesAsync` never carries a Resources.Contents payload.
- G4 baseline cross-check: no `G4|GameGuild.Resources.Contents…` entries (nothing dead declared — nothing is declared at all).

## 3. Use-case event contracts (declared no-event surface)

`Architecture/UseCaseEventContracts.g.cs` declares 12 ids, each with `NoDomainEventReason = "No named cross-module event is declared; the durable generic operation event remains the observable contract."`:

`resources.contents.add-content-version-review`, `…approve-content-version`, `…bulk-generate-contracts`, `…cancel-content-publish`, `…create-content-draft`, `…generate-contract`, `…publish-content-version`, `…reject-content-version`, `…rollback-content-version`, `…schedule-content-publish`, `…submit-content-for-review`, `…update-content-draft` (`Contents:/Architecture/UseCaseEventContracts.g.cs:3-14`).

Note the declaration covers only the 12 dispatched commands — the service-only operations (`archive-old-versions`, `process-scheduled-publishing`, template CRUD) have neither events nor contracts.

## 4. Logger lines (the only emission)

| Site | Level | Event |
|---|---|---|
| `Services/ContentDraftService.cs:46` | Information | `Created draft version {VersionNumber} for {EntityType}:{EntityId}` |
| `Services/ContentDraftService.cs:77` | Information | `Updated draft version {VersionId}` |
| `Services/ContentDraftService.cs:133` | Information | `Created rollback version from v{TargetVersion} …` |
| `Services/ContentDraftService.cs:160` | Information | `Archived {Count} old versions …` |
| `Services/ContentReviewPublishingService.cs:32,56,80` | Information | submitted/approved/rejected |
| `Services/ContentReviewPublishingService.cs:166,197,225,259` | Information | published / scheduled / cancelled / auto-published |
| `Services/ContentReviewPublishingService.cs:264` | **Error** | auto-publish failure (rethrows) |
| `Services/ContentVersionQueryService.cs:111` | Debug | compared versions |

Structured logs identify versions by id/number — no user identifiers beyond what the version columns carry, and no correlation id. Treat as operational telemetry, not an audit trail.

## 5. What an event surface would owe (design note)

For parity with the Content.Pages pattern, the mutation set that would require in-transaction required-audit events is: create/update draft, submit, approve, reject, publish, schedule, cancel, rollback, add-review, generate contract (single/bulk), archive-versions, and template create/update. None exists today — flagged R10 rather than implemented here (docs-only scope).
