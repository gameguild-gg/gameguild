# TrustSafety — Data-flow diagrams

> Module `GameGuild.TrustSafety` + adjacent content-safety surfaces · develop `ce31d5850c`. Families match 01/07.

## L0 — Context

```mermaid
flowchart LR
    subgraph Intended[intended - absent today]
        PROD[Signed evidence producer NONE T1] -.-> IN[IngestAsync]
    end
    ST[Authenticated staff] --> APPEALS[Appeals console]
    EC[Economy risk chain<br/>payouts protected ops] --> READ[ITrustSafetyRiskInputSource]
    READ --> IN
    IN --> DB[(trust_safety tables + evidence + holds)]
    APPEALS --> DB
    DB --> EC
    subgraph Adjacent[actual content safety - other modules]
        R[Reporter] --> AR[Assets report flow]
        UP[Uploader] --> ML[Assets classification lane]
        RR[Rating reporter] --> RF[Ratings report flow]
    end
    ML --> LLM[LiteLLM vision proxy]
    style PROD stroke-dasharray: 5 5
    style IN stroke-dasharray: 5 5
```

Trust boundaries: TB1 internet→API (staff console `[Authorize]` only — gap T3), TB2 module→DB (EF/raw via Economy persistence), TB3 future producers→ingest (signature boundary), TB4 API→LiteLLM proxy (external LLM, service-network).

## L1-F1 — Evidence ingestion (designed)

```mermaid
flowchart TD
    P[Producer TB3] --> V{signature verify TB2}
    V -->|invalid| RJ[(inbox Rejected)]
    V -->|valid| O{version ordered?}
    O -->|no| DF[(inbox Deferred no re-driver T8)]
    O -->|yes| HZ{outcome}
    HZ -->|Approved| REL[release compliance hold]
    HZ -->|Rejected-NeedsReview| ACT[activate compliance hold scope tenant+subject]
    HZ --> SS[(subject state version++ outcome)]
    SS --> EV[(evidence envelope kind TrustSafety)]
    EV --> PR[ProcessedAt inbox]
```

## L1-F2 — Appeals console (live)

```mermaid
flowchart TD
    ST[Staff TB1 Authorize only T3] --> L[GET appeals tenant-scoped]
    L --> Q[(trust_safety_appeals read)]
    ST --> AS[POST assignment ExpectedVersion]
    AS --> Q2[(Submitted → Assigned Version++)]
    ST --> DE[POST decisions Overturn Reason EvidenceHash]
    DE --> Q3[(Assigned-to-actor → Upheld-Overturned)]
    SUB[SubmitAppealAsync] -.->|no caller T1| Q
    style SUB stroke-dasharray: 5 5
    style ST stroke:orange
```

## L1-F3 — Asset report → review lane

```mermaid
flowchart TD
    U[Reporter TB1 authed] --> RC{not own asset?}
    RC --> RP[(AssetReport Pending)]
    ADM[Admin reviewer TB1 admin policy] --> MQ[moderation queue by ReportCount]
    MQ --> RV[review decision]
    RV --> |NoAction| DS[(Dismissed)]
    RV -->|ContentRemoved-etc| RS[(Resolved + asset action)]
```

## L1-F4 — Classification lane (the only automated safety)

```mermaid
flowchart TD
    UP[Upload TB1] --> P[(asset ModerationStatus Pending)]
    W[Worker 5s poll] --> CL{classifier bound?}
    CL -->|host default| LLM[LiteLLM vision TB4 8-label JSON]
    CL -->|fallback| UNRES[Unresolved → APPROVED T4 fail-open]
    LLM --> TH{0-80 safe / 0-75 unsafe / middle}
    TH --> A2[(Approved)]
    TH --> R2[(Rejected)]
    TH --> N2[(NeedsReview)]
    LLM -->|error x3| N2
    style UNRES fill:#fdd
```

## L1-F5 — Text surfaces (the documented absence)

```mermaid
flowchart TD
    T[User text posts comments reviews] --> S[(stored unfiltered)]
    S --> X[NO analyzer NO policy NO queue]
    style X fill:#fdd
```

## Data stores summary

| Store | Written by | Read by | Live data? |
|---|---|---|---|
| `trust_safety_event_inbox` | IngestAsync (no caller) | replay checks | **empty by construction** |
| `trust_safety_subject_states` | IngestAsync | (via evidence reader) | **empty** |
| `trust_safety_appeals` | SubmitAppealAsync (no caller) | console | **empty** |
| compliance evidence (kind TrustSafety) | IngestAsync | risk input source, economy | **empty** |
| compliance holds | IngestAsync | economy gates | none from this module |
| Assets reports/moderation | report + review + worker | queue, asset visibility | live |
| Ratings report counters | ReportAsync | moderation console | live (bombable) |
