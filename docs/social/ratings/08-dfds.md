# Social.Ratings — Data-flow diagrams

> Module `GameGuild.Social.Ratings` · develop `ce31d5850c`. Families match 01/07.

## L0 — Context

```mermaid
flowchart LR
    ANON[Anonymous reader] --> READS[9 anonymous read routes]
    USER[Authenticated user] --> WRITE[rate helpful report delete-own]
    ADM[TenantAdmin] --> MOD[moderation console 5 routes]
    READS & WRITE & MOD --> PROC((Social-Ratings module))
    PROC --> DB[(ratings schema 3 tables)]
    PROC -.-> OPE[Durable operation events social-ratings-]
    TARGET[Target entities courses posts anything] -.NO reference checked R2.-> PROC
    style TARGET stroke-dasharray: 5 5
```

Trust boundaries: TB1 internet→API (9 anonymous reads; writes authenticated; admin policy-gated), TB2 API→DB (EF Core — currently failing on translation, R1). Note the dashed input: the "target" of a rating is caller-asserted text+id, never verified against any store.

## L1-F1 — Rate

```mermaid
flowchart TD
    U[User TB1] --> C[RatingsController POST]
    C --> H[RatingEndpointCommandHandler]
    H --> S[RatingCrudService-RateAsync]
    S --> T1[(ratings upsert owner-scoped)]
    S --> T2[(rating_summaries synchronous recompute Approved-only)]
    H -.-> O[operation event social-ratings-rate]
```

## L1-F2 — Helpful vote

```mermaid
flowchart TD
    V[Voter TB1] --> C[POST id-helpful]
    C --> M[RatingModerationService-VoteHelpfulAsync]
    M --> G{own rating?}
    G -->|yes| E[400]
    G -->|no| T1[(rating_helpful_votes upsert unique per user)]
    M --> T2[(ratings-HelpfulCount ±)]
```

## L1-F3 — Report → flag → moderate

```mermaid
flowchart TD
    R[Reporter TB1 identity NOT stored R3] --> C[POST id-report]
    C --> M[ReportAsync count++]
    M --> F{ge 3 → Flagged}
    F --> T1[(ratings ModerationStatus)]
    ADM[Admin TB1 TenantAdmin] --> Q[queue Flagged by ReportCount]
    ADM --> D[approve or reject or delete or recalculate]
    D --> T1
    D --> T2[(summary recompute]]
```

## L1-F4 — Reads and aggregates

```mermaid
flowchart TD
    A[Anonymous TB1] --> R1[GET entity-id filters]
    A --> R2[GET summary single-batch]
    A --> R3[GET top entityType]
    A --> R4[GET recent-reviews]
    A --> R5[GET user-userId history R9]
    R1 & R2 & R3 & R4 & R5 --> Q[RatingQueryService]
    Q --> T1[(ratings Approved-only filters)]
    Q --> T2[(rating_summaries)]
    X[EF translation<br/>IsDeleted unmapped] -.->|500 today R1| Q
    style X fill:#fdd
```

## Data stores summary

| Store | Written by | Read by |
|---|---|---|
| `ratings` | rate/report/moderation handlers, soft deletes | entity reads, user history, recalc, moderation queue |
| `rating_helpful_votes` | vote/remove handlers | vote-existence checks (never exposed) |
| `rating_summaries` | synchronous recalc, admin recalculate | summary/batch/top reads |
