# Resources.Contents — User Flows

> Module `GameGuild.Resources.Contents` · `develop` @ `ce31d5850c`. Shorthand `Contents:` = `apps/api/Source/Modules/GameGuild.Resources.Contents/`. Actors: **Author** (`content:draft` family), **Scheduler** (`content:schedule` family), **Editor** (`content:edit` family), **Reviewer/Member** (authenticated, no content permission — per the wiring, they can still drive several transitions), **System** (contract-generation caller).
> Reality check carried through every flow: 16 of these routes are **baselined as 500-at-runtime** (E9 / #1278) — the flows document intended behavior, with the runtime-dead steps flagged `E9`.

## F1 — Draft authoring (create, update, rollback)

```mermaid
flowchart TD
    A1["Author POST api contents versioning drafts<br/>VersioningController.cs L46"] --> A2{"content draft write admin<br/>VersioningController.cs L52"}
    A2 -- "no key" --> A403["403 Forbid<br/>VersioningEditorialPermissionGatingTests"]
    A2 -- "holds" --> A3["CreateContentDraftCommand<br/>ContentVersionMutationCommands.cs L5"]
    A3 --> A4["ContentDraftService CreateDraftAsync<br/>ContentDraftService.cs L17"]
    A4 --> A5["MaxAsync VersionNumber filter IsDeleted E9-failing predicate<br/>ContentDraftService.cs L28-30"]
    A5 --> A6["ContentVersion Create n plus 1 Status Draft<br/>Entities ContentVersion.cs L87"]
    A6 --> A7["SaveChangesAsync - ILogger only no audit<br/>ContentDraftService.cs L44"]
    A7 --> A8["201 CreatedAtAction GetVersion E9 route"]

    U1["Author PUT api contents versioning drafts versionId<br/>VersioningController.cs L67"] --> U2{"content draft write admin<br/>VersioningController.cs L74"}
    U2 -- "no key" --> U403["403"]
    U2 -- "holds" --> U3["UpdateContentDraftCommand<br/>ContentVersionMutationCommands.cs L6"]
    U3 --> U4["ContentDraftService UpdateDraftAsync<br/>ContentDraftService.cs L52"]
    U4 --> U5{"version found and Status Draft<br/>ContentDraftService.cs L69"}
    U5 -- "missing" --> U404["mapped 404 ContentVersioning.NotFound"]
    U5 -- "not Draft" --> U400["400 CanOnlyUpdateDrafts"]
    U5 -- "Draft" --> U6["entity UpdateDraft patch + guard throws if not Draft<br/>Entities ContentVersion.cs L115-148"]
    U6 --> U7["200 ContentVersionDto E9 route"]

    RB1["Editor POST entity entityType entityId rollback<br/>VersioningController.cs L292"] --> RB2{"content edit write admin<br/>VersioningController.cs L304"}
    RB2 -- "no key" --> RB403["403"]
    RB2 -- "holds" --> RB3["RollbackContentVersionCommand target reason<br/>ContentVersionMutationCommands.cs L13"]
    RB3 --> RB4["ContentDraftService RollbackAsync<br/>ContentDraftService.cs L98"]
    RB4 --> RB5{"target version exists<br/>ContentDraftService.cs L105-113"}
    RB5 -- "no" --> RB404["404"]
    RB5 -- "yes" --> RB6["CreateDraftAsync from target snapshot<br/>change notes Rollback to vN reason<br/>ContentDraftService.cs L117-126"]
    RB6 --> RB7["201 new draft - E9 route"]
```

### F1 edge-case matrix

| Case | Behavior | Citation |
|---|---|---|
| Concurrent draft creation | two racing `MaxAsync` reads can mint the same `VersionNumber` (index is non-unique) | `Services/ContentDraftService.cs:28-30`; `Configuration/ContentVersionEntityConfiguration.cs:43`; gap R7 |
| Draft for unknown entity | no existence check on `(EntityId, EntityType)` — drafts can attach to anything, including nonexistent entities | `Services/ContentDraftService.cs:17-50` |
| `CreatedBy` from payload | draft authorship is the **request body's** `CreatedBy` (`CreateDraftRequest`, `Controllers/VersioningController.cs:341`), not the actor — spoofable attribution | gap R6 |
| Update of a non-draft | service returns `CanOnlyUpdateDrafts` 400 (entity would also throw) | `Services/ContentDraftService.cs:69-72`; `Entities/ContentVersion.cs:117-119` |
| Rollback semantics | rollback = new draft from snapshot; the published current version is untouched until the new draft clears review | `Services/ContentDraftService.cs:98-137` |
| Rollback to a draft/rejected version | allowed — target need only exist | `Services/ContentDraftService.cs:105-110` |
| Get the draft for an entity | `GetDraftAsync` exists in the facade but has **no route** (`02-cqrs-wiring-matrix.md` §3) | `Services/ContentVersioningService.cs:26-27` |

## F2 — Review workflow (submit, pending queue, approve/reject, review comments)

```mermaid
flowchart TD
    S1["Author POST versionId submit-for-review<br/>VersioningController.cs L150"] --> S2{"gate - none beyond Authorize<br/>VersioningController.cs L150-160"}
    S2 --> S3["SubmitContentForReviewCommand<br/>ContentVersionMutationCommands.cs L7"]
    S3 --> S4["ContentReviewPublishingService SubmitForReviewAsync<br/>ContentReviewPublishingService.cs L17"]
    S4 --> S5{"found and Status Draft<br/>entity guard throws otherwise"}
    S5 -- "not Draft" --> S400["400 InvalidOperation caught<br/>ContentReviewPublishingService.cs L35-38"]
    S5 -- "Draft" --> S6["entity SubmitForReview actor guid<br/>PendingReview SubmittedForReviewAt<br/>Entities ContentVersion.cs L150-161"]
    S6 --> S7["200 - E9 route"]

    P1["Reviewer GET pending-review entityType skip take<br/>VersioningController.cs L165"] --> P2["GetPendingReviewAsync oldest first<br/>ContentReviewPublishingService.cs L89-111"]
    P2 --> P3["200 list - E9 route"]

    AP1["Reviewer POST versionId approve ReviewNotes<br/>VersioningController.cs L187"] --> AP2["ApproveContentVersionCommand<br/>ContentVersionMutationCommands.cs L8"]
    AP2 --> AP3["ApproveAsync entity Approve requires PendingReview<br/>ContentReviewPublishingService.cs L41-63"]
    AP3 -- "not pending" --> AP400["400 InvalidOperation"]
    AP3 -- "pending" --> AP4["Approved ReviewedBy ReviewedAt ReviewNotes<br/>Entities ContentVersion.cs L163-175"]
    AP4 --> AP5["200 - E9 route"]

    RJ1["Reviewer POST versionId reject ReviewNotes<br/>VersioningController.cs L202"] --> RJ2["RejectContentVersionCommand<br/>ContentVersionMutationCommands.cs L9"]
    RJ2 --> RJ3["RejectAsync entity Reject requires PendingReview<br/>ContentReviewPublishingService.cs L65-87"]
    RJ3 --> RJ4["Rejected terminal for this version - resume via new draft or rollback<br/>03-state-machines.md"]

    RV1["Reviewer POST versionId reviews decision feedback suggestions<br/>VersioningController.cs L321"] --> RV2["AddContentVersionReviewCommand<br/>ContentVersionMutationCommands.cs L14"]
    RV2 --> RV3["AddReviewAsync appends ContentVersionReview row<br/>any decision on any status - no guard<br/>ContentReviewPublishingService.cs L113-139"]
    RV3 --> RV4["201 review dto - E9 route"]
```

### F2 edge-case matrix

| Case | Behavior | Citation |
|---|---|---|
| Who may submit/approve/reject | **any authenticated caller** — no editorial key on submit/approve/reject/reviews (only drafts/schedule/rollback are gated) | `Controllers/VersioningController.cs:150-212,321-332`; gap R5 |
| Self-review | submitter and approver can be the same guid — no separation check | `Services/ContentReviewPublishingService.cs:29,53` |
| Actor fallback | actor guid defaults to `Guid.Empty` when the subject is not a guid (`:15`) — approvals attributable to the empty guid | gap R6 |
| Review rows vs state | `ContentVersionReview` rows are advisory annotations; only `Approve`/`Reject` move state — an Approve-decision review row does not approve | `Entities/ContentVersion.cs:272-307` |
| Review on any status | `AddReviewAsync` has no status precondition | `Services/ContentReviewPublishingService.cs:113-139` |
| Reviewer attribution | `ReviewerId` = actor context guid (not payload) | `:128-133` |

## F3 — Publishing (publish, current-version swap, schedule, cancel)

```mermaid
flowchart TD
    B1["Member POST versionId publish<br/>VersioningController.cs L217"] --> B2{"gate - none beyond Authorize<br/>VersioningController.cs L217-227"}
    B2 --> B3["PublishContentVersionCommand<br/>ContentVersionMutationCommands.cs L10"]
    B3 --> B4["ContentReviewPublishingService PublishAsync<br/>ContentReviewPublishingService.cs L141"]
    B4 --> B5{"entity Publish requires Approved or Scheduled<br/>Entities ContentVersion.cs L193"}
    B5 -- "other status" --> B400["400 InvalidOperation caught"]
    B5 -- "ok" --> B6["clear IsCurrentVersion on siblings then Publish<br/>ContentReviewPublishingService.cs L153-163"]
    B6 --> B7["Published IsCurrentVersion true PublishedAt PublishedBy<br/>200 - E9 route"]

    SC1["Scheduler POST versionId schedule ScheduledAt<br/>VersioningController.cs L232"] --> SC2{"content schedule write admin<br/>VersioningController.cs L239"}
    SC2 -- "no key" --> SC403["403"]
    SC2 -- "holds" --> SC3["ScheduleContentPublishCommand<br/>ContentVersionMutationCommands.cs L11"]
    SC3 --> SC4["SchedulePublishAsync requires future date and Approved<br/>ContentReviewPublishingService.cs L177-205"]
    SC4 -- "past date" --> SC400b["400 ScheduleDateMustBeFuture"]
    SC4 -- "not Approved" --> SC400["400 InvalidOperation"]
    SC4 -- "ok" --> SC5["Scheduled ScheduledPublishAt PublishedBy scheduler<br/>Entities ContentVersion.cs L205-216"]
    SC5 --> SC6["200 - E9 route"]
    SC6 --> SC7["no worker - stays Scheduled forever gap R3<br/>only manual publish can fire it"]

    CC1["Scheduler POST versionId cancel-schedule<br/>VersioningController.cs L253"] --> CC2{"content schedule write admin<br/>VersioningController.cs L260"}
    CC2 -- "no key" --> CC403["403"]
    CC2 -- "holds" --> CC3["CancelScheduledPublishAsync<br/>ContentReviewPublishingService.cs L207"]
    CC3 --> CC4{"Status Scheduled<br/>ContentReviewPublishingService.cs L217"}
    CC4 -- "no" --> CC400["400 NotScheduled"]
    CC4 -- "yes" --> CC5["calls version Approve - but entity requires PendingReview<br/>ContentReviewPublishingService.cs L222 and Entities ContentVersion.cs L166"]
    CC5 --> CC500["InvalidOperationException UNCAUGHT - 500 always<br/>gap R2 - test names it<br/>ContentReviewPublishingServiceTests.cs L398"]

    A1["GET compare versionId1 versionId2<br/>VersioningController.cs L274"] --> A2["CompareVersionsAsync same-entity check field-level booleans<br/>ContentVersionQueryService.cs L77-115"]
    A2 -- "cross-entity pair" --> A400c["400 VersionsMustBeSameEntity"]
    A2 -- "ok" --> A3["200 ContentVersionDiff - E9 route"]
```

### F3 edge-case matrix

| Case | Behavior | Citation |
|---|---|---|
| Publish without approval | entity guard blocks (Draft/PendingReview/Rejected → 400) — the review gate is the entity, not a permission | `Entities/ContentVersion.cs:193-195` |
| Current-version swap | siblings' `IsCurrentVersion` cleared in the same save; no uniqueness constraint enforces single-current (concurrent publishes can leave none/dupes) | `Services/ContentReviewPublishingService.cs:153-161`; gap R7 |
| Schedule→publish path | entity allows Scheduled→Published directly (manual rescue of the worker-less state) | `Entities/ContentVersion.cs:193` |
| Cancel-schedule | **broken by construction** — see R2 | `Services/ContentReviewPublishingService.cs:222` |
| Scheduled publish execution | `ProcessScheduledPublishingAsync` exists, publishes due versions and attributes `PublishedBy ?? Guid.Empty` — but has **no caller** (R3) | `:230-272,256` |
| Diff fidelity | `BodyDiff` is the literal string "Body changed" — no line-level diff; `TitleDiff` interpolates old→new | `Services/ContentVersionQueryService.cs:106-108` |

## F4 — Contract generation (single + bulk from document templates)

```mermaid
flowchart TD
    G1["System POST v1 document-contracts generate<br/>ContractGenerationController.cs L18"] --> G2{"actor guid present<br/>ContractGenerationController.cs L26-30"}
    G2 -- "absent" --> G401["401 Unauthorized"]
    G2 -- "present" --> G3["GenerateContractCommand input createdBy<br/>GenerateContractCommands.cs L5"]
    G3 --> G4["ContractGenerationService GenerateAsync<br/>ContractGenerationService.cs L12"]
    G4 --> G5{"EntityType and Title non-empty<br/>ContractGenerationService.cs L19-29"}
    G5 -- "blank" --> G400["400 Validation errors"]
    G5 -- "ok" --> G6["DocumentTemplateService RenderPublishedAsync<br/>DocumentTemplateService.cs L143"]
    G6 --> G7{"template found AND published version exists<br/>DocumentTemplateService.cs L112-141"}
    G7 -- "no template" --> G404a["404 DocumentTemplate.NotFound"]
    G7 -- "no published version" --> G404b["404 PublishedVersionNotFound"]
    G7 -- "ok" --> G8["regex double-brace placeholder substitution<br/>missing set collected<br/>DocumentTemplateService.cs L155-170"]
    G8 --> G9{"missing variables and not AllowMissingVariables<br/>ContractGenerationService.cs L42-48"}
    G9 -- "yes" --> G400b["400 MissingVariables listed"]
    G9 -- "no" --> G10["ContentVersion Create next version metadata JSON<br/>template id variables snapshot<br/>ContractGenerationService.cs L52-77"]
    G10 --> G11{"request Publish flag<br/>ContractGenerationService.cs L79"}
    G11 -- "false" --> G12["draft saved 201 Published false"]
    G11 -- "true" --> G13["archive prior Published then<br/>SubmitForReview Approve Publish same actor<br/>ContractGenerationService.cs L81-96"]
    G13 --> G14["generated contract live immediately - review bypassed gap R8"]
    G12 --> G15["201 GeneratedContractResponse"]
    G14 --> G15

    B1["System POST v1 document-contracts generate-colon-bulk<br/>ContractGenerationController.cs L50"] --> B2{"actor guid and non-empty list<br/>ContractGenerationController.cs L57-68"}
    B2 -- "empty list" --> B400b["400 ContractGeneration.BulkEmpty"]
    B2 -- "ok" --> B3["BulkGenerateContractsCommand ContinueOnError<br/>GenerateContractCommands.cs L9"]
    B3 --> B4["loop GenerateAsync per item<br/>stop on first failure when not ContinueOnError<br/>GenerateContractCommands.cs L50-89"]
    B4 --> B5["200 BulkGeneratedContractsResponse per-item success error"]
```

### F4 edge-case matrix

| Case | Behavior | Citation |
|---|---|---|
| Templates in real environments | no create/update/list API, no seeder — the 404 branches are the only production outcome (R4) | `Services/DocumentTemplateService.cs`; gap R4 |
| Placeholder syntax | `{{ key }}` letters/digits/dot/dash/underscore, case-insensitive variable match | `Services/DocumentTemplateService.cs:155-159,179-180` |
| `keepUnknownPlaceholders` | parameter exists but both branches return the raw match — dead flag | `Services/DocumentTemplateService.cs:169` |
| `AllowMissingVariables` | validation moves from 400 to a `MissingVariables` list on the 201 | `Services/ContractGenerationService.cs:42-48`; `Controllers/ContractGenerationController.cs:100` |
| Auto-publish chain | submit→approve→publish executed synchronously by the generating actor — no reviewer involvement; prior published versions archived | `Services/ContractGenerationService.cs:93-95`; gap R8 |
| `EntityId` omitted | a fresh random GUID is minted — the contract becomes its own entity | `Services/ContractGenerationService.cs:51` |
| Bulk partial failure | `ContinueOnError=false` stops at first error; response always 200 with counts | `Commands/GenerateContractCommands.cs:62-81` |
| Duplicate `TemplateKey` | unique index + pre-check → conflict error (`DocumentTemplate.DuplicateKey`) — service-only path | `Services/DocumentTemplateService.cs:49-54,183-188` |
