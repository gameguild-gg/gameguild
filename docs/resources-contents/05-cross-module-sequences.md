# Resources.Contents — Cross-Module Sequences

> Module `GameGuild.Resources.Contents` · `develop` @ `ce31d5850c`. All interactions are synchronous request/response — the module emits no events (see `04-event-inventory.md`). E9-dead steps are flagged inline.

## 1. Editorial happy path — draft → review → publish (intended; currently E9-dead past step 2)

```mermaid
sequenceDiagram
    participant Author as Author content draft
    participant Rv as Reviewer any authenticated - R5
    participant VC as VersioningController api contents versioning
    participant Sender as ISender
    participant CRP as ContentReviewPublishingService
    participant CV as ContentVersion entity
    participant DB as content_versions table

    Author->>VC: POST drafts CreateDraftRequest
    VC->>Sender: CreateContentDraftCommand
    Sender->>CRP: via ContentDraftService CreateDraftAsync
    CRP->>DB: MaxAsync VersionNumber E9-failing predicate
    CRP->>CV: Create n plus 1 Draft
    CRP->>DB: INSERT - ILogger only no audit
    CRP-->>VC: Result ok
    VC-->>Author: 201 draft dto

    Author->>VC: PUT drafts versionId update body
    Note over VC: E9 500 today - Baseline L280
    VC->>CRP: UpdateDraftAsync - entity guard Draft only
    CRP->>DB: UPDATE

    Author->>VC: POST versionId submit-for-review
    Note over VC: E9 500 today - Baseline L231
    VC->>CRP: SubmitForReviewAsync
    CRP->>CV: SubmitForReview actor guid
    CV->>CV: Draft to PendingReview<br/>ContentVersion.cs L150-161
    CRP->>DB: UPDATE

    Rv->>VC: POST versionId approve ReviewNotes
    Note over VC: E9 500 today - Baseline L225
    VC->>CRP: ApproveAsync
    CRP->>CV: Approve requires PendingReview
    CRP->>DB: UPDATE Approved ReviewedBy

    Rv->>VC: POST versionId publish
    Note over VC: E9 500 today - Baseline L227
    VC->>CRP: PublishAsync
    CRP->>DB: SELECT siblings IsCurrentVersion
    CRP->>CV: SetAsCurrent false on siblings<br/>Publish on target L153-163
    CRP->>DB: UPDATE all
    CRP-->>VC: published dto
```

## 2. Contract generation — template render → content version (auto-publish chain)

```mermaid
sequenceDiagram
    participant Sys as Authenticated caller
    participant CGC as ContractGenerationController v1 document-contracts
    participant CGS as ContractGenerationService
    participant DTS as DocumentTemplateService
    participant CV as ContentVersion entity
    participant DB as content_versions + document_templates

    Sys->>CGC: POST generate GenerateContractRequest Publish true
    CGC->>CGC: actor guid else 401<br/>ContractGenerationController.cs L26-30
    CGC->>CGS: GenerateContractCommand
    CGS->>DTS: RenderPublishedAsync templateId variables
    DTS->>DB: template row + published version row<br/>DocumentTemplateService.cs L112-133
    alt template or published version missing - the only real-world outcome per R4
        DTS-->>CGS: NotFound error
        CGS-->>CGC: failure
        CGC-->>Sys: 404 DocumentTemplate NotFound or PublishedVersionNotFound
    else rendered
        DTS->>DTS: regex placeholder substitution<br/>missing set collected L155-170
        DTS-->>CGS: rendered body
        CGS->>DB: MaxAsync VersionNumber for entity
        CGS->>CV: Create next version metadata JSON snapshot<br/>ContractGenerationService.cs L57-77
        CGS->>DB: SELECT prior Published versions - archive each
        CGS->>CV: SubmitForReview then Approve then Publish same actor<br/>L93-95 - review bypassed R8
        CGS->>DB: INSERT version
        CGS-->>CGC: GeneratedContractResult
        CGC-->>Sys: 201 GeneratedContractResponse Published true
    end
```

Bulk variant loops the same service per item, `ContinueOnError` stops at the first failure when false (`Commands/GenerateContractCommands.cs:50-89`).

## 3. The scheduled-publish dead end (no worker)

```mermaid
sequenceDiagram
    participant Sch as Scheduler content schedule
    participant VC as VersioningController
    participant CRP as ContentReviewPublishingService
    participant DB as content_versions

    Sch->>VC: POST versionId schedule ScheduledAt future
    Note over VC: E9 500 today - Baseline L230
    VC->>CRP: SchedulePublishAsync
    CRP->>CRP: guard scheduledAt greater than now L187
    CRP->>DB: UPDATE Status Scheduled ScheduledPublishAt
    Note over DB: nothing ever reads ScheduledPublishAt again<br/>ProcessScheduledPublishingAsync has zero callers - R3

    alt operator cancels
        Sch->>VC: POST versionId cancel-schedule
        VC->>CRP: CancelScheduledPublishAsync
        CRP->>CRP: version Approve on a Scheduled row<br/>entity requires PendingReview - throws L222 vs ContentVersion.cs L166
        CRP-->>VC: uncaught InvalidOperationException
        VC-->>Sch: 500 always - R2 - doubly dead with E9 Baseline L226
    else operator rescues manually
        Sch->>VC: POST versionId publish
        Note over VC: E9 500 today - Baseline L227
        VC->>CRP: PublishAsync - entity accepts Scheduled
        CRP->>DB: published
    else nobody acts
        Note over DB: version stays Scheduled forever
    end
```

## 4. Boundary contract — Resources.Contents vs Content.Pages (do not merge)

```mermaid
flowchart TD
    subgraph CP["GameGuild.Content.Pages - CMS surface"]
        P["pages page_sections content_resources<br/>proposals leads"]
        PA["required audit sink fail-closed"]
        PR["routes v1 pages v1 content-resources v1 marketing v1 content course-proposals v1 og"]
    end
    subgraph RC["GameGuild.Resources.Contents - versioning + templates"]
        V["content_versions polymorphic EntityId EntityType<br/>content_version_reviews document_templates"]
        RA["no audit - ILogger only"]
        RR["routes api contents versioning legacy prefix<br/>v1 document-contracts"]
    end
    PR --> P
    P --> PA
    RR --> V
    V --> RA
    NO1["no shared tables"] -.-> P
    NO2["no shared events - neither emits any"] -.-> V
    NO3["no code reference between modules<br/>beyond EntityBase and CQRS rail"] -.-> PR
```

- **Two content models**: Content.Pages is the *rendered-website* model (slug-addressed, SEO, overwrite-in-place, published flag); Resources.Contents is the *revision-history* model (polymorphic `(EntityId, EntityType)` snapshots, review workflow, rollback). Nothing versions a `Page` and nothing slug-addresses a `ContentVersion`.
- **Two route conventions**: `v{version}/…` everywhere in Content.Pages; `api/contents/versioning` is the one legacy-prefix island (plus `v1/document-contracts` in this module).
- **Two audit postures**: fail-closed required audit (Pages) vs none (this module).
- **Both are platform-global** (no tenant predicates) and both emit zero integration events — publishing in either is invisible to the other.
