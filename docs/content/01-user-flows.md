# Content.Pages — User Flows

> Module `GameGuild.Content.Pages` · `develop` @ `ce31d5850c`. Shorthand `Pages:` = `apps/api/Source/Modules/GameGuild.Content.Pages/`. Actors: **Editor** (holds `content:draft`/`content:edit` family), **Content manager** (`content:write`/`content:admin`), **Member** (authenticated, no content permission), **Visitor** (anonymous), **Crawler** (SEO bot).
> Permission keys are defined in `Authz:/Models/TypedPermissions.cs:202-226` (`content:read`, `content:write`, `content:admin`, `content:edit`, `content:draft`, `content:schedule`, `content:edit-metadata`). This module uses read/write/admin/edit only — `content:draft` and `content:schedule` belong to `Resources.Contents`, and `content:edit-metadata` (#333) is used by neither.

## F1 — Page lifecycle (editor creates, edits, publishes, unpublishes, archives)

```mermaid
flowchart TD
    E1["Editor POST v1 pages with CreatePageDto<br/>PageController.cs L119"] --> E2{"ModelState valid<br/>PageController.cs L122"}
    E2 -- "invalid" --> E400["400 ValidationProblemDetails"]
    E2 -- "valid" --> C1["CreatePageCommand<br/>ContentMutationCommands.cs L8"]
    C1 --> C2["CreatePageCommandHandler<br/>delegates to PageService CreateAsync<br/>ContentMutationCommands.cs L42"]
    C2 --> C3["new Page Status Draft always<br/>PageService.cs L47 and L55"]
    C3 --> C4["unique slug constraint pages Slug<br/>PageConfiguration.cs L11"]
    C4 -- "slug collision" --> C500["DbUpdateException 500<br/>no pre-check no 409 mapping"]
    C4 -- "saved" --> C5["SaveWithRequiredAuditAsync<br/>audit Content.Pages.PageCreated<br/>PageService.cs L76"]
    C5 -- "audit write fails" --> C5x["transaction rolls back - fail closed<br/>AuditedMutationExtensions.cs L62"]
    C5 -- "committed" --> C6["201 CreatedAtAction GetPage"]

    E10["Editor PUT v1 pages by id<br/>PageController.cs L132"] --> E11{"CanEditContent<br/>content edit write admin<br/>PageController.cs L137"}
    E11 -- "no permission" --> E403["403 Forbid<br/>tested by EditorialPermissionGatingTests.cs L20"]
    E11 -- "holds" --> E12["UpdatePageCommand id dto<br/>PageController.cs L147"]
    E12 --> E13["PageService UpdateAsync patch-if-not-null<br/>including Status field directly<br/>PageService.cs L84-119"]
    E13 --> E14["audit Content.Pages.PageUpdated<br/>PageService.cs L113"]
    E14 --> E15["200 PageDto or 404 when missing<br/>PageController.cs L148-153"]

    P1["Editor POST v1 pages by id publish<br/>PageController.cs L170"] --> P2{"actor has guid SubjectId<br/>PageController.cs L173"}
    P2 -- "no guid" --> P401["401 Unauthorized"]
    P2 -- "has" --> P3["PublishPageCommand id userId<br/>PublishPageCommand.cs L5"]
    P3 --> P4["PublishPageCommandHandler validates empty guids<br/>PublishPageCommandHandler.cs L11-19"]
    P4 --> P5["PageService PublishAsync<br/>Status Published PublishedAt kept PublishedBy userId<br/>PageService.cs L135-149"]
    P5 --> P6["audit Content.Pages.PagePublished actor userId<br/>PageService.cs L143-147"]

    U1["Editor POST v1 pages by id unpublish<br/>PageController.cs L189"] --> U2["UnpublishPageCommand<br/>ContentMutationCommands.cs L11"]
    U2 --> U3["Status back to Draft<br/>PublishedAt and PublishedBy NOT cleared<br/>PageService.cs L151-163"]
    U3 --> U4["audit Content.Pages.PageUnpublished<br/>PageService.cs L157-161"]

    A1["Editor DELETE v1 pages by id<br/>PageController.cs L157"] --> A2{"any permission gate<br/>PageController.cs L157-167"}
    A2 -- "none - only class Authorize" --> A3["DeletePageCommand<br/>PageService.cs L121"]
    A3 --> A4["page SoftDelete sets DeletedAt<br/>PageService.cs L126"]
    A4 --> A5["row still readable - no query filter<br/>see edge cases"]
```

### F1 edge-case matrix

| Case | Behavior | Citation |
|---|---|---|
| Create with duplicate slug | `pages.Slug` unique index → `DbUpdateException` → 500 (no 409 mapping, no pre-check) | `Pages:/Configuration/PageConfiguration.cs:11`; `Services/PageService.cs:75-81` |
| Create enforces Draft | `Status = PageStatus.Draft` regardless of payload (CreatePageDto has no status field) | `Services/PageService.cs:55`; `Dtos/Dtos.cs` (CreatePageDto) |
| Update can set `Status` directly | `UpdatePageDto.Status` patch bypasses publish/unpublish endpoints — an editor can flip Draft→Archived→Published in one PUT without audit wording "published" | `Services/PageService.cs:93`; gap C1-adjacent, see `06-gaps.md` C5 |
| Publish of missing page | 404 (`page is null`) | `Controllers/PageController.cs:180-184` |
| Publish keeps original `PublishedAt` | `PublishedAt ??= UtcNow` — republish keeps first-publish timestamp | `Services/PageService.cs:141` |
| Unpublish does not clear publish stamps | `PublishedAt`/`PublishedBy` survive unpublish; sitemap/OG stop serving (status-gated) but stale metadata remains | `Services/PageService.cs:151-163` |
| Delete of missing page | 404 | `Controllers/PageController.cs:160-164` |
| Deleted page still resolvable | `GetByIdAsync`/`GetBySlugAsync`/`GetPagesAsync` have no `DeletedAt` predicate and `PageConfiguration` installs no query filter | `Services/PageService.cs:15-45`; `Configuration/PageConfiguration.cs` (absent filter); gap C2 |
| Self-parenting / cycles | `ParentPageId` is not validated — a page can parent itself or form a cycle (FK only ensures existence, `DeleteBehavior.Restrict`) | `Services/PageService.cs:71,109`; `Configuration/PageConfiguration.cs:26-29` |
| Scheduled publish | `ScheduledPublishAt` settable via create/update but never consumed — see C3 | `Entities/Page.cs:166`; `Services/PageService.cs:111` |

## F2 — Public page consumption (marketing site, visitors, crawlers)

```mermaid
flowchart TD
    V1["Visitor GET v1 pages by-slug slug - AllowAnonymous<br/>PageController.cs L74"] --> V2{"caller CanViewUnpublished<br/>content read write admin<br/>PageController.cs L80"}
    V2 -- "anonymous or plain member" --> V3["publishedOnly true<br/>PageService.cs L21-26"]
    V2 -- "content manager" --> V4["publishedOnly false - drafts visible"]
    V3 -- "no published page" --> V404["404"]
    V3 -- "found" --> V5["200 PageDto with live sections ordered by SortOrder"]
    V4 -- "found" --> V5

    S1["Crawler GET v1 pages sitemap - AllowAnonymous<br/>PageController.cs L100"] --> S2["GetPagesAsync status Published<br/>skip 0 take 5000 hard cap<br/>PageController.cs L107"]
    S2 --> S3["200 SitemapEntryDto list slug UpdatedAt locale only<br/>PageController.cs L110-113"]

    O1["Crawler GET v1 og catch-all slug - AllowAnonymous<br/>OpenGraphController.cs L22"] --> O2{"slug blank<br/>OpenGraphController.cs L26"}
    O2 -- "blank" --> O400["400 Slug is required"]
    O2 -- "present" --> O3["ResponseCache 300s VaryByQueryKeys all<br/>OpenGraphController.cs L23"]
    O3 --> O4["OpenGraphService ResolveAsync<br/>OpenGraphService.cs L11"]
    O4 --> O5{"published Page with slug<br/>OpenGraphService.cs L14-17"}
    O5 -- "yes" --> O6["200 Page ToOpenGraphDto"]
    O5 -- "no" --> O7{"published ContentResource with slug<br/>OpenGraphService.cs L23-26"}
    O7 -- "yes" --> O8["200 resource OG dto"]
    O7 -- "no" --> O404["404"]
```

### F2 edge-case matrix

| Case | Behavior | Citation |
|---|---|---|
| Anonymous slug probe of a draft | 404 — slugs of unpublished pages do not confirm existence | `Controllers/PageController.cs:80-86` |
| Member (no content permission) by-id probe of a draft | 404, not 403 — draft existence is not disclosed | `Controllers/PageController.cs:65-68` |
| Member listing non-published | requested `status` silently forced to `Published` (no error) | `Controllers/PageController.cs:45-48` |
| Sitemap size | capped at `take: 5000`; larger sites silently truncate | `Controllers/PageController.cs:107` |
| Sitemap of soft-deleted pages | deleted-but-published pages still appear (C2) | `Services/PageService.cs:28-45` |
| OG cache | 5-minute output cache keyed by all query strings — draft→publish flips lag up to 5 min | `Controllers/OpenGraphController.cs:23` |
| OG lookup order | pages shadow content resources with the same slug (unique only per table) | `Services/OpenGraphService.cs:13-28` |
| Catch-all slug | `{*slug}` allows slashes — `GET v1/og/blog/my-post` works | `Controllers/OpenGraphController.cs:22` |

## F3 — Section authoring (page blocks)

```mermaid
flowchart TD
    L1["Editor GET v1 pages pageId sections<br/>PageController.cs L204"] --> L2["GetSectionsAsync DeletedAt null ordered<br/>PageService.cs L167-172"]
    L2 --> L3["200 list - no page status check no CanViewUnpublished<br/>gap C4"]

    C1s["Editor POST v1 pages pageId sections<br/>PageController.cs L212"] --> C2s["CreatePageSectionCommand pageId dto<br/>ContentMutationCommands.cs L12"]
    C2s --> C3s["PageService CreateSectionAsync<br/>PageService.cs L177-198"]
    C3s --> C4s["FK page_sections PageId - no existence pre-check<br/>EF FK violation 500 on unknown page"]
    C4s --> C5s["audit Content.Pages.PageSectionCreated<br/>PageService.cs L192-196"]

    R1["Editor POST v1 pages pageId sections reorder<br/>PageController.cs L275"] --> R2["ReorderPageSectionsCommand orderedIds<br/>ContentMutationCommands.cs L15"]
    R2 --> R3["ReorderSectionsAsync loads live sections<br/>assigns SortOrder by list index<br/>PageService.cs L235-253"]
    R3 --> R4{"orderedIds contains foreign section id<br/>PageService.cs L244"}
    R4 -- "foreign id" --> R5["silently ignored - no 400"]
    R4 -- "section omitted from list" --> R6["keeps old SortOrder - partial reorder"]
    R3 --> R7["audit Content.Pages.PageSectionsReordered count only<br/>PageService.cs L248-252"]

    D1["Editor DELETE v1 pages pageId sections sectionId<br/>PageController.cs L263"] --> D2["DeletePageSectionCommand checks PageId match<br/>ContentMutationCommands.cs L84-93"]
    D2 --> D3["SoftDelete then audit PageSectionDeleted<br/>PageService.cs L221-233"]
```

### F3 edge-case matrix

| Case | Behavior | Citation |
|---|---|---|
| Sections of an unpublished page | returned to any authenticated caller — no status gate (C4) | `Controllers/PageController.cs:204-209` |
| Create section on unknown page | no pre-check; FK `page_sections.PageId` violation → 500 | `Services/PageService.cs:191`; `Configuration/PageSectionConfiguration.cs`; migration FK `20260210113401_AddContentPagesModule.cs:129` |
| Update section route guard | `CanEditContent` checked, then `UpdateSectionAsync` re-checks `section.PageId == route pageId` → 404 on mismatch | `Controllers/PageController.cs:243`; `Commands/ContentMutationCommands.cs:76-81` |
| Reorder with foreign/missing ids | foreign ids ignored silently; omitted sections keep old order (partial state) | `Services/PageService.cs:242-246` |
| Cascade delete of page | DB-level `DeleteBehavior.Cascade` on sections — but pages soft-delete, so cascade never fires from the API | `Configuration/PageConfiguration.cs:32-35` |
| Hide vs delete | `IsVisible` toggle available via update only; `GetBySlugAsync` include does not filter `IsVisible` — hiding is a frontend concern | `Entities/PageSection.cs:99`; `Services/PageService.cs:23` |

## F4 — Content resource lifecycle + public catalog

```mermaid
flowchart TD
    M1["Content manager POST v1 content-resources<br/>ContentResourceController.cs L104"] --> M2["CreateContentResourceCommand<br/>ContentMutationCommands.cs L5"]
    M2 --> M3["ContentResourceService CreateAsync Status Draft always<br/>ContentResourceService.cs L52-88"]
    M3 --> M4["audit Content.Pages.ContentResourceCreated<br/>ContentResourceService.cs L82-86"]

    Q1["Visitor GET v1 content-resources - AllowAnonymous<br/>ContentResourceController.cs L37"] --> Q2{"status filter and CanViewUnpublished<br/>ContentResourceController.cs L51"}
    Q2 -- "no content permission" --> Q3["status forced Published<br/>drafts and in-review unlisted"]
    Q2 -- "manager" --> Q4["requested status honored incl InReview"]
    Q3 --> Q5["ListAsync filters type locale category featured q<br/>ContentResourceService.cs L21-50"]
    Q5 --> Q6["order Featured desc then SortOrder then PublishedAt desc<br/>ContentResourceService.cs L43-49"]

    G1["Visitor GET v1 content-resources by-slug slug - AllowAnonymous<br/>ContentResourceController.cs L82"] --> G2["GetBySlugAsync publishedOnly unless manager<br/>ContentResourceController.cs L88"]
    G2 -- "not found or unpublished" --> G404["404"]
    G2 -- "published" --> G3["IncrementViewCountAsync awaited in-request<br/>ContentResourceController.cs L95-98"]
    G3 --> G4["ViewCount plus plus then SaveChanges<br/>no audit event unauthenticated write<br/>ContentResourceService.cs L158-165"]
    G4 --> G5["200 ContentResourceDto"]

    MP["Content manager POST by id publish<br/>ContentResourceController.cs L155"] --> MP2["PublishContentResourceCommand id userId<br/>PublishContentResourceCommandHandler.cs L5"]
    MP2 --> MP3["Status Published PublishedAt kept PublishedBy userId<br/>ContentResourceService.cs L142-156"]
    MP3 --> MP4["audit Content.Pages.ContentResourcePublished<br/>ContentResourceService.cs L150-154"]
```

### F4 edge-case matrix

| Case | Behavior | Citation |
|---|---|---|
| Catalog search `q` | server-side `ToLower().Contains` over Title/Summary/Tags — no index, full scan | `Services/ContentResourceService.cs:34-41` |
| In-review visibility | `InReview` exists only for ContentResource (Pages have no review state); only content managers can list it | `Entities/ContentResource.cs:40-46`; `Controllers/ContentResourceController.cs:51-54` |
| View-count inflation | anonymous, unthrottled-per-route (global policy only), write-on-read — every crawl increments; two SaveChanges per GET | `Controllers/ContentResourceController.cs:98`; `Services/ContentResourceService.cs:158-165`; STRIDE T4 |
| By-id draft probe | 404 for non-managers (existence not disclosed) | `Controllers/ContentResourceController.cs:73-77` |
| Delete | soft, same read-visibility caveat as pages (C2) | `Services/ContentResourceService.cs:128-140` |
| Course cross-link | `LinkedEntityId`/`LinkedEntityType` are free-form GUID+string; no integrity link to Learning.Courses | `Entities/ContentResource.cs:129-136` |

## F5 — Course-proposal pipeline (public submit → review → publish → detail page)

```mermaid
flowchart TD
    subgraph WEB["Next.js marketing site"]
        W1["Creator fills proposal form<br/>apps web src lib proposals actions.ts L166"] --> W2["POST v1 content course-proposals<br/>AllowAnonymous"]
    end
    W2 --> P1["CreateCourseProposalCommand<br/>CourseProposalsController.cs L65-78"]
    P1 --> P2{"ModelState valid<br/>CourseProposalsController.cs L71"}
    P2 -- "invalid" --> P400["400"]
    P2 -- "valid" --> P3["CourseProposalService CreateAsync<br/>CourseProposalService.cs L12-36"]
    P3 --> P4["Slug GenerateSlug title plus 8-char random suffix<br/>CourseProposalService.cs L16 and L165-196"]
    P4 --> P5["Status submitted ProposerEmail lowercased<br/>CourseProposalService.cs L17-24"]
    P5 --> P6["audit Content.Pages.CourseProposalCreated shape only no PII values<br/>CourseProposalService.cs L27-34"]
    P6 --> P7["201 CourseProposalDto"]

    R1["Reviewer GET v1 content course-proposals status search<br/>Authorize - no permission key<br/>CourseProposalsController.cs L19-33"] --> R2["ListAsync filters status search over title name email<br/>CourseProposalService.cs L55-88"]
    R2 --> R3["200 CRM list"]

    U1["Reviewer PUT v1 content course-proposals by id<br/>CourseProposalsController.cs L80"] --> U2{"dto.Status present and valid<br/>CourseProposalsController.cs L92-103"}
    U2 -- "unknown status string" --> U400["400 ValidationProblemDetails with allowed values"]
    U2 -- "valid or absent" --> U3["UpdateCourseProposalCommand<br/>CourseProposalService.cs L90-139"]
    U3 --> U4{"transition to published<br/>CourseProposalService.cs L123-126"}
    U4 -- "yes and PublishedAt null" --> U5["PublishedAt UtcNow"]
    U4 -- "leaving published" --> U6["PublishedAt cleared<br/>CourseProposalService.cs L127-130"]
    U3 --> U7["audit CourseProposalUpdated with PreviousStatus<br/>CourseProposalService.cs L132-137"]

    D1["Reviewer GET by-slug slug - AllowAnonymous<br/>CourseProposalsController.cs L52"] --> D2["GetPublishedBySlugAsync published only latest<br/>CourseProposalService.cs L44-53"]
    D2 -- "unpublished or missing" --> D404["404 - drafts and rejections unlisted"]
    D2 -- "published" --> D3["200 public detail for /proposals slash slug page"]

    I1["Visitor expresses interest on proposal page<br/>apps web src lib proposals actions.ts L105"] --> I2["POST v1 marketing leads source course-proposal<br/>see F6"]
```

### F5 edge-case matrix

| Case | Behavior | Citation |
|---|---|---|
| Any-status transition | `UpdateAsync` accepts any valid status string from any valid one — no guarded transition table (submitted→published legal, rejected→published legal) | `Services/CourseProposalService.cs:102-105`; `03-state-machines.md` |
| Slug uniqueness | random suffix makes collisions practically impossible; DB index is non-unique | `Services/CourseProposalService.cs:194`; `Configuration/CourseProposalConfiguration.cs:30` |
| Anonymous CRM surface? | list/get-by-id are `[Authorize]` but with no content permission — any member can read proposals incl. proposer name/email | `Controllers/CourseProposalsController.cs:19-46`; STRIDE I2 |
| Delete | **hard** `Remove` (audit records it) — contrast with soft-delete elsewhere in the module | `Services/CourseProposalService.cs:141-159` |
| Duplicate submissions | no dedupe/rate policy per route (global only); proposal spam lands in `course_proposals` with PII | `Controllers/CourseProposalsController.cs:65-78` |
| Metadata payload | free-form JSON up to 4000 chars, documented "never secrets", stored as text not jsonb | `Entities/CourseProposal.cs:63-65` |

## F6 — Marketing-lead capture (contact form / newsletter / proposal interest)

```mermaid
flowchart TD
    subgraph WEB2["Next.js forms"]
        C1["Contact form<br/>apps web src lib contact actions.ts L82"] --> C2["POST v1 marketing leads source contact"]
        N1["Newsletter signup<br/>apps web src lib content-creators actions.ts L105"] --> N2["POST v1 marketing leads source newsletter"]
        P2["Proposal interest<br/>apps web src lib proposals actions.ts L105"] --> N3["POST v1 marketing leads source course-proposal"]
    end
    C2 --> L1 & N2 --> L1 & N3 --> L1
    L1["CreateLead AllowAnonymous<br/>MarketingLeadController.cs L46"] --> L2{"ModelState valid<br/>MarketingLeadController.cs L51"}
    L2 -- "invalid" --> L400["400"]
    L2 -- "valid" --> L3["controller-side Validate rules<br/>MarketingLeadController.cs L57-106"]
    L3 -- "unknown source or topic" --> L400b["400 ValidationProblemDetails"]
    L3 -- "contact without name topic message min 10" --> L400b
    L3 -- "passes" --> L4["CreateMarketingLeadCommand<br/>MarketingLeadController.cs L67"]
    L4 --> L5["MarketingLeadService CreateAsync Status new normalized<br/>MarketingLeadService.cs L10-37"]
    L5 --> L6["audit MarketingLeadCreated shape only source topic plan locale pagePath<br/>MarketingLeadService.cs L29-35"]
    L6 --> L7["201 MarketingLeadDto"]

    S1["Sales GET v1 marketing leads source status topic search<br/>Authorize only<br/>MarketingLeadController.cs L15-31"] --> S2["ListAsync search over Email Name Company Plan<br/>MarketingLeadService.cs L45-94"]
    S2 --> S3["200 PII-bearing list"]
```

### F6 edge-case matrix

| Case | Behavior | Citation |
|---|---|---|
| Source vocabulary | `contact | newsletter | course-proposal` only; anything else 400 | `Entities/MarketingLead.cs:7-18`; `Controllers/MarketingLeadController.cs:77-80` |
| Topic vocabulary | `sales | support | partnership | other`; required for contact source | `Entities/MarketingLead.cs:33-45`; `Controllers/MarketingLeadController.cs:82-102` |
| Status mutation | **no route exists** — `new` is terminal via API; `reviewed`/`archived` unreachable (C6) | `Controllers/MarketingLeadController.cs` (absent); `03-state-machines.md` |
| Delete / erasure | no DELETE route; PII (name/email/company/message) retained indefinitely | gap C6 |
| Search on Email | CRM search substring-matches PII with `[Authorize]` only — no permission key | `Services/MarketingLeadService.cs:81-86`; STRIDE I3 |
| Duplicate newsletter signups | no dedupe — every submit inserts a row (`Email` index is non-unique) | `Configuration/MarketingLeadConfiguration.cs:31` |
