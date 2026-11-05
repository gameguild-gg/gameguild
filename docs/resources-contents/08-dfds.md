# Resources.Contents — Data-Flow Diagrams

> Module `GameGuild.Resources.Contents` · `develop` @ `ce31d5850c`. Trust boundaries: **TB1** anonymous internet → API (current-version route only, allowlisted); **TB2** authenticated actors (author/editor/scheduler/reviewer-or-member) → API; **TB3** API services → PostgreSQL tables `content_versions`, `content_version_reviews`, `document_templates`. **There is no audit store boundary** — mutations flow to ILogger only (R10).

## L0 — Context

```mermaid
flowchart TD
    subgraph TB2["TB2 - authenticated"]
        AU["Author content draft key"]
        SC["Scheduler content schedule key"]
        ED["Editor content edit key"]
        MEM["Member - no content key - R5 can drive transitions and read all"]
        SYS["Contract caller - any authenticated"]
    end
    subgraph TB1["TB1 - anonymous"]
        ANON["Anonymous current-version reader - R9"]
    end
    API["GameGuild API<br/>Resources.Contents - 18 routes<br/>16 E9-dead at runtime - R1"]
    subgraph TB3["TB3 - PostgreSQL"]
        CV[("content_versions")]
        CR[("content_version_reviews")]
        DT[("document_templates")]
    end
    LOG["ILogger telemetry - the only emission"]
    AU -->|"drafts create update rollback"| API
    SC -->|"schedule cancel - broken R2"| API
    ED -->|"rollback"| API
    MEM -->|"submit approve reject publish reviews reads - ungated R5"| API
    SYS -->|"v1 document-contracts generate generate-colon-bulk"| API
    ANON -->|"GET current published version body - R9"| API
    API --> CV
    API --> CR
    API --> DT
    API -.->|"no audit rows - R10"| LOG
```

## L1-F1 — Draft authoring

```mermaid
flowchart TD
    AU["Author at TB2"] -->|"POST PUT api contents versioning drafts - DRAFT gate"| VC["VersioningController"]
    VC -->|"Create Update Rollback commands"| DS["ContentDraftService"]
    DS -->|"MaxAsync + INSERT UPDATE content_versions<br/>IsDeleted predicate - E9 failing R1"| CV[("content_versions TB3")]
    DS -.->|"ILogger created updated rollback - no audit R10"| LOG["Logs"]
```

## L1-F2 — Review workflow

```mermaid
flowchart TD
    MEM2["Reviewer or Member at TB2 - no gate R5"] -->|"submit approve reject reviews pending-review"| VC["VersioningController"]
    VC -->|"review commands"| RPS["ContentReviewPublishingService"]
    RPS -->|"entity guarded transitions E9 failing prologue R1"| CV[("content_versions TB3")]
    RPS -->|"INSERT content_version_reviews append only"| CR[("content_version_reviews TB3")]
    RPS -.->|"ILogger submitted approved rejected - no audit R10"| LOG["Logs"]
```

## L1-F3 — Publishing and scheduling

```mermaid
flowchart TD
    subgraph TB2F["TB2"]
        MEM3["Member - publish ungated R5"]
        SCF["Scheduler - gated"]
    end
    MEM3 -->|"POST publish"| VC["VersioningController"]
    SCF -->|"POST schedule POST cancel-schedule throws R2"| VC
    VC -->|"publish schedule cancel commands"| RPS["ContentReviewPublishingService"]
    RPS -->|"clear IsCurrentVersion siblings then Publish<br/>single save - E9 failing R1"| CV[("content_versions TB3")]
    NOW["Worker that would read ScheduledPublishAt<br/>DOES NOT EXIST - R3"] -.->|"never"| CV
```

## L1-F4 — Contract generation

```mermaid
flowchart TD
    SYS["Authenticated caller at TB2"] -->|"POST v1 document-contracts generate or generate-colon-bulk"| CGC["ContractGenerationController"]
    CGC -->|"Generate BulkGenerate commands - actor createdBy"| CGS["ContractGenerationService"]
    CGS -->|"RenderPublishedAsync templateId variables"| DTS["DocumentTemplateService"]
    DTS -->|"SELECT document_templates + published ContentVersion<br/>no IsDeleted predicate - E9 clean"| DT[("document_templates TB3")]
    DTS -->|"SELECT published version body"| CV[("content_versions TB3")]
    DTS -->|"regex substitution of double-brace placeholders<br/>values interpolated verbatim - RS5-T"| CGS
    CGS -->|"INSERT ContentVersion auto chain submit approve publish when Publish true - R8"| CV
    CGS -.->|"no audit R10"| LOG["Logs"]
    SEED["Template creator - DOES NOT EXIST no controller no seeder R4"] -.->|"never"| DT
```

## L1-RS4 — Reads (incl. anonymous current)

```mermaid
flowchart TD
    subgraph TB2R["TB2 any member - no ownership or tenant check"]
        R1q["GET versionId history version-n pending-review compare"]
    end
    ANONR["Anonymous at TB1 - R9 misattributed allowlist"] -->|"GET entity type entity id current"| VCR["VersioningController"]
    R1q --> VCR
    VCR --> VQS["ContentVersionQueryService"]
    VQS -->|"SELECT content_versions by EntityId EntityType<br/>IsDeleted predicate - E9 failing R1<br/>no tenant predicate - IDOR"| CV[("content_versions TB3")]
    VQS --> VQS2["CompareVersionsAsync field-level diff"]
```

## Data-store sensitivity summary

| Store | Sensitivity | Notes |
|---|---|---|
| `content_versions` | polymorphic content bodies (`Body` text, `Metadata` jsonb incl. contract variables snapshot, `ReviewNotes`, actor guids) | readable by any member and — for current published — anonymously (RS4); no tenant scoping |
| `content_version_reviews` | reviewer feedback + suggestions jsonb | append-only annotations, never consumed |
| `document_templates` | template bodies with `{{placeholder}}` holes + `PlaceholderSchema` jsonb | unreachable lifecycle (R4); rendered output must be treated as untrusted (RS5-T) |
| logs | version ids/numbers only | telemetry, not audit (R10) |
