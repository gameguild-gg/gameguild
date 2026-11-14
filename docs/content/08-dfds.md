# Content.Pages — Data-Flow Diagrams

> Module `GameGuild.Content.Pages` · `develop` @ `ce31d5850c`. Trust boundaries: **TB1** anonymous internet → API gateway (global rate limit, anonymous-endpoint allowlist); **TB2** authenticated member/editor → API (JWT claims → `ActorContext`); **TB3** API services → PostgreSQL `public` schema; **TB4** mutation services → compliance `AuditLog` (same transaction, fail-closed). Flows numbered to match `01-user-flows.md`.

## L0 — Context

```mermaid
flowchart TD
    subgraph TB1["TB1 - anonymous internet"]
        V["Visitor"]
        CR["SEO crawler / social preview bot"]
    end
    subgraph TB2["TB2 - authenticated actors"]
        ED["Editor with content edit"]
        CM["Content manager with content write or admin"]
        MEM["Member - no content permission"]
    end
    API["GameGuild API<br/>Content.Pages module - 32 routes"]
    subgraph TB3["TB3 - PostgreSQL public schema"]
        T1[("pages page_sections")]
        T2[("content_resources")]
        T3[("course_proposals")]
        T4[("marketing_leads")]
    end
    subgraph TB4["TB4 - compliance store"]
    AUD[("AuditLog rows<br/>Content.Pages dot asterisk")]
    end
    WEB["Next.js marketing site<br/>proposals contact newsletter forms"]
    V -->|"reads published pages OG sitemap"| API
    CR -->|"crawls sitemap OG catalogs"| API
    ED -->|"authors pages sections"| API
    CM -->|"manages resources proposals leads"| API
    MEM -->|"CRM-style reads and - per C1 - ungated mutations"| API
    WEB -->|"submits proposals and leads"| API
    API --> T1
    API --> T2
    API --> T3
    API --> T4
    API -->|"required audit per mutation"| AUD
```

## L1-F1 — Page lifecycle

```mermaid
flowchart TD
    E["Editor or Member at TB2"] -->|"POST PUT DELETE publish unpublish v1 pages"| C["PageController"]
    C -->|"CreatePageCommand etc"| S["PageService"]
    S -->|"INSERT UPDATE pages - soft delete sets DeletedAt"| T1[("pages TB3")]
    S -->|"RecordAsync Content.Pages Page event"| K["CentralContentPagesAuditEventSink TB4"]
    K -->|"INSERT AuditLog same transaction fail-closed"| AUD[("AuditLog")]
    K -.->|"audit failure rolls back mutation"| S
```

## L1-F2 — Public consumption

```mermaid
flowchart TD
    subgraph INTERNET["TB1 anonymous"]
        VIS["Visitor"]
        BOT["Crawler"]
    end
    VIS -->|"GET by-slug"| C1["PageController anon allowlist"]
    BOT -->|"GET sitemap GET og catch-all"| C1
    C1 -->|"publishedOnly enforced unless content permission"| PS["PageService"]
    C1 --> OG["OpenGraphController 300 s response cache"]
    OG --> OGS["OpenGraphService"]
    PS -->|"SELECT pages where Status Published<br/>C2 - DeletedAt not filtered"| T1[("pages TB3")]
    OGS -->|"page first then resource"| T1
    OGS --> T2[("content_resources TB3")]
    C2R["GET content-resources by-slug anon"] -->|"read plus ViewCount plus plus write - C7"| T2
```

## L1-F3 — Section authoring

```mermaid
flowchart TD
    ED["Editor or Member at TB2"] -->|"sections CRUD reorder - C4 no parent status check"| C["PageController"]
    C -->|"Section commands"| S["PageService"]
    S -->|"INSERT UPDATE page_sections - cascade config never fires from soft delete"| T[("page_sections TB3")]
    S -->|"PageSection audit events"| K["CentralContentPagesAuditEventSink TB4"]
```

## L1-F4 — Content-resource lifecycle + catalog

```mermaid
flowchart TD
    subgraph TB2b["TB2"]
        CM["Content manager"]
        MEM2["Member - per C1 can mutate"]
    end
    subgraph TB1b["TB1"]
        VIS2["Visitor catalog and by-slug"]
    end
    CM -->|"create update delete publish"| C["ContentResourceController"]
    MEM2 -->|"same routes - ungated C1"| C
    VIS2 -->|"list by-slug - published forced"| C
    C --> S["ContentResourceService"]
    S --> T[("content_resources TB3")]
    S -->|"resource audit events"| K["CentralContentPagesAuditEventSink TB4"]
    S -->|"ViewCount increment - no audit C7"| T
```

## L1-F5+F6 — Proposal and lead intake

```mermaid
flowchart TD
    subgraph TB1c["TB1 anonymous intake"]
        CR2["Creator proposal form"]
        VI["Visitor interest contact newsletter"]
    end
    WEB["Next.js forms at TB1"]
    CR2 --> WEB
    VI --> WEB
    WEB -->|"POST course-proposals"| CP["CourseProposalsController"]
    WEB -->|"POST marketing leads"| ML["MarketingLeadController"]
    CP -->|"CreateCourseProposalCommand"| CPS["CourseProposalService"]
    ML -->|"CreateMarketingLeadCommand"| MLS["MarketingLeadService"]
    CPS --> T3[("course_proposals TB3 - PII name email")]
    MLS --> T4[("marketing_leads TB3 - PII contact fields C6 no erasure")]
    CPS -->|"shape-only audit"| K["CentralContentPagesAuditEventSink TB4"]
    MLS -->|"shape-only audit"| K
    REV["Reviewer or any member at TB2 - C8 no permission key"] -->|"GET list PUT status DELETE hard"| CP
    SALES["Sales or any member at TB2 - I3"] -->|"GET leads PII list"| ML
    PUB["Visitor detail page"] -->|"GET by-slug published only no-store"| CP
```

## Data-store sensitivity summary

| Store | Sensitivity | Notes |
|---|---|---|
| `pages` / `page_sections` / `content_resources` | platform content + free-form JSONB (`CustomData`, `Data`, `StructuredData`) | operator-trusted input; draft confidentiality depends on gates catalogued in `07-stride-threat-model.md` |
| `course_proposals` | PII (`ProposerName`, `ProposerEmail`) + free-form `Metadata` | no erasure path (hard delete only, C8) |
| `marketing_leads` | PII (name/email/company/message + `UserAgent`) | indefinite retention, no mutation surface (C6) |
| `AuditLog` rows `Content.Pages.*` | operational metadata, shape-only for intake | fail-closed in-transaction writes |
