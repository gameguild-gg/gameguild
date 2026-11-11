# Content.Pages — Cross-Module Sequences

> Module `GameGuild.Content.Pages` · `develop` @ `ce31d5850c`. All cross-module interaction is synchronous in-request (no integration events — see `04-event-inventory.md` §3).

## 1. Mutation → compliance audit log (fail-closed, same transaction)

Applies to all 17 mutation routes of `02-cqrs-wiring-matrix.md`.

```mermaid
sequenceDiagram
    participant Editor as Editor or Member
    participant PC as PageController
    participant Sender as ISender CQRS pipeline
    participant PS as PageService
    participant DB as ApplicationDbContext pages table
    participant Sink as CentralContentPagesAuditEventSink Compliance.Audit
    participant AL as AuditLog table

    Editor->>PC: POST v1 pages create
    PC->>Sender: CreatePageCommand
    Sender->>PS: CreateAsync dto
    PS->>DB: Add page Status Draft
    PS->>Sink: RecordAsync Content.Pages.PageCreated
    Sink->>Sink: require ambient transaction else throw<br/>CentralModuleAuditEventSinks.cs L35
    Sink->>AL: Add AuditLog row Risk Low Category Data
    Sink-->>PS: done
    alt audit write fails
        PS-->>Sender: exception
        Note over PS,DB: SaveWithRequiredAuditAsync rolls back<br/>page row AND audit row together
        Sender-->>PC: 500 via ExceptionHandlingMiddleware
    else committed
        PS-->>Sender: Page entity
        Sender-->>PC: entity
        PC-->>Editor: 201 CreatedAtAction GetPage
```

The same shape runs for `PUT` (PageUpdated), `DELETE` (PageDeleted), publish/unpublish, sections, resources, proposals and leads — only the ActionType and emitter site change (`04-event-inventory.md` §1).

## 2. Public render path — marketing site page vs OG resolution

```mermaid
sequenceDiagram
    participant Visitor as Visitor or Crawler
    participant Web as Next.js marketing site
    participant OG as OpenGraphController v1 og
    participant OGS as OpenGraphService
    participant DB as pages and content_resources tables

    Visitor->>Web: GET /about or social preview scrape
    Web->>OG: GET v1 og about slug anonymous
    Note over OG: ResponseCache 300 s VaryByQueryKeys all<br/>OpenGraphController.cs L23
    OG->>OGS: ResolveAsync slug
    OGS->>DB: first published Page by slug AsNoTracking<br/>OpenGraphService.cs L14
    alt page found
        OGS-->>OG: Page.ToOpenGraphDto
    else no page
        OGS->>DB: published ContentResource by slug<br/>OpenGraphService.cs L23
        alt resource found
            OGS-->>OG: resource OG dto
        else nothing
            OGS-->>OG: null
            OG-->>Visitor: 404
        end
    end
    OG-->>Web: 200 OG metadata
    Web-->>Visitor: head meta tags rendered
```

Sibling read path: `GET v1/pages/by-slug/{slug}` returns the full page + ordered live sections (`Services/PageService.cs:21-26`); `GET v1/pages/sitemap` feeds `sitemap.xml` (`Controllers/PageController.cs:100-116`). Proposal detail: `GET v1/content/course-proposals/by-slug/{slug}` with `cache: 'no-store'` (`apps/web/src/lib/proposals/actions.ts:63-88`).

## 3. Course-proposal funnel — submission, publication, and downstream lead

```mermaid
sequenceDiagram
    participant Creator as Anonymous creator
    participant Web as Next.js proposals actions
    participant CPC as CourseProposalsController
    participant CPS as CourseProposalService
    participant DB as course_proposals table
    participant Sink as CentralContentPagesAuditEventSink
    participant Reviewer as Reviewer any authenticated
    participant Visitor as Visitor
    participant MLC as MarketingLeadController

    Creator->>Web: submit course proposal form
    Web->>CPC: POST v1 content course-proposals AllowAnonymous
    CPC->>CPS: CreateCourseProposalCommand
    CPS->>CPS: GenerateSlug title plus random suffix<br/>CourseProposalService.cs L165
    CPS->>DB: insert Status submitted
    CPS->>Sink: CourseProposalCreated shape only
    CPS-->>Web: 201 proposal dto
    Web-->>Creator: confirmation page

    Reviewer->>CPC: PUT v1 content course-proposals by id Status published
    CPC->>CPS: UpdateCourseProposalCommand
    CPS->>DB: Status published PublishedAt UtcNow<br/>CourseProposalService.cs L123-126
    CPS->>Sink: CourseProposalUpdated previous submitted current published
    CPS-->>Reviewer: 200

    Visitor->>Web: open /proposals by slug
    Web->>CPC: GET by-slug slug anonymous no-store
    CPC->>CPS: GetPublishedBySlugAsync
    CPS->>DB: latest published by slug
    CPS-->>Web: 200 public dto

    Visitor->>Web: express interest on proposal page
    Web->>MLC: POST v1 marketing leads source course-proposal<br/>proposals actions.ts L105
    Note over MLC: MarketingLeadService CreateAsync Status new<br/>audit MarketingLeadCreated shape only
    MLC-->>Web: 201 lead captured
```

Note the funnel's two PII stores: the proposal row keeps `ProposerName`/`ProposerEmail` (`Entities/CourseProposal.cs:58-61`, no erasure route, hard delete only) and the lead row keeps contact fields (`Entities/MarketingLead.cs:65-93`, no route at all to remove) — see gaps C6/C8.

## 4. Boundary statement — what this module is NOT

- **Not `Resources.Contents`**: Content.Pages has no versioning, no review workflow, no scheduling worker. A `Page` body edit overwrites in place; history exists only as audit log entries (`AuditLog` rows with `Content.Pages.PageUpdated`). Anything needing drafts-with-review or rollback is a `Resources.Contents` `ContentVersion` on a polymorphic entity — the two modules share no tables, no events, and no code beyond `EntityBase` and the CQRS rail (boundary contract in `docs/resources-contents/05-cross-module-sequences.md` §4).
- **Not `GameGuild.Resources`**: the Platform-family resources module (uploads/assets) is unrelated despite the similar name; its package is `docs/social-platform/`.
- **Not Learning.Courses**: `ContentResourceType.Course` + `LinkedEntityId` (`Entities/ContentResource.cs:129-136`) is a dangling cross-reference — nothing validates the linked program exists, and Learning emits nothing back.
