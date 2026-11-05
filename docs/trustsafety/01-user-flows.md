# TrustSafety — User flows

> Module `GameGuild.TrustSafety` + adjacent content-safety surfaces · develop `ce31d5850c`. Family numbering F1-F5 matches 07/08.

## F1 — Trust/safety evidence ingestion (DESIGNED — no producer exists, T1)

```mermaid
flowchart TD
    NO[No producer exists for TrustSafetyEvent] -.-> A[ITrustSafetyControlPlane-IngestAsync<br/>PostgreSqlTrustSafetyControlPlane.cs-80-187]
    A --> B[ValidateEvent ids hashes enums lifetime TS-cs-328-348]
    B --> C[Serializable transaction]
    C --> D{EventId replayed?}
    D -->|same payload hash| E[Duplicate or Deferred or Rejected]
    D -->|different payload| F[ConflictException replay-with-different-payload]
    D -->|new| G{tenant-subject-Version already bound?}
    G -->|yes| H[ConflictException]
    G -->|no| I[Canonicalize + signature verify<br/>EconomyTrustSafetyEventSignatureVerifier cs-39-48]
    I -->|invalid| J[inbox row invalid-signature Rejected]
    I -->|valid| K{Version exactly state-Version+1<br/>and IssuedAt monotonic? cs-132-137}
    K -->|no| L[inbox row out-of-order-event Deferred]
    K -->|yes| M{Outcome}
    M -->|Approved and hold active| N[release compliance hold SystemActor]
    M -->|not Approved and no active hold| O[activate compliance hold<br/>scope tenant+subjectHash kind]
    M --> M2[upsert subject state row versioned]
    M2 --> P[publish ComplianceEvidenceEnvelope<br/>kind TrustSafety cs-171-177]
    P --> Q[inbox ProcessedAt set]
    Q --> R[Published or Duplicate]
    style NO stroke-dasharray: 5 5
    style A stroke-dasharray: 5 5
```

Edge cases:

| Case | Behavior | Evidence |
|---|---|---|
| Replay same id + same payload + processed | `Duplicate` | `TS:/PostgreSqlTrustSafetyControlPlane.cs:88-104` |
| Replay same id + same payload + previously deferred | `Deferred` (retryable) | `:100-102` |
| Replay same id, different payload | hard conflict | `:92-95` |
| Version gap | deferred (`out-of-order-event`), not rejected — ordering tolerance | `:132-137` |
| Non-Approved outcome on subject with expired hold | new hold activated | `:145-155` |
| DB check constraints back the guards | version>0, lifetime, policyVersion>0 | `TS:/TrustSafetyPersistence.cs:22-24, 43-45` |

## F2 — Appeal review console (live surface, empty table by construction)

```mermaid
flowchart TD
    A[Authenticated staff GET admin-economy-compliance-trust-safety-appeals<br/>EconomyComplianceAdministrationController.cs-168-179] --> B{TryActor tenant from context}
    B -->|no tenant| C[403]
    B --> D[ReadAppealsAsync tenant state take cap 500<br/>TS-cs-252-283]
    D --> E[200 list newest SubmittedAt first]
    F[POST appeals-appealId-assignment ExpectedVersion] --> G[AssignTrustSafetyAppealEndpointCommand<br/>API-Core-Commands-EconomyComplianceEndpointCommands.cs-83-85]
    G --> H{state Submitted AND version matches? TS-cs-211-227}
    H -->|no| I[409 Conflict]
    H -->|yes| J[Assigned to actor Version++]
    K[POST appeals-appealId-deisions Overturn ReasonCode EvidenceHash] --> L{state Assigned AND AssignedTo eq actor AND version? TS-cs-229-250}
    L -->|no| I
    L -->|yes| M[Upheld or Overturned DecidedBy DecidedAt Version++]
    M --> N[200 appeal dto]
    O[SubmitAppealAsync] -.->|ZERO CALLERS T1| P[no route no handler no worker]
    style O stroke-dasharray: 5 5
    style P stroke-dasharray: 5 5
```

Edge cases:

| Case | Behavior | Evidence |
|---|---|---|
| Concurrent assignment | expectedVersion optimistic check → 409 | `TS:/PostgreSqlTrustSafetyControlPlane.cs:217-220, 237-239` |
| Decide by non-assignee | refused (`AssignedTo != actorId`) | `:237` |
| Decision evidence | `reasonCode` + `evidenceHash` mandatory, 422-shaped validation | `:234-235` |
| DB-enforced decision consistency | check constraint couples State∈{1,2}↔null decider / State∈{3,4}↔decider+timestamp | `TrustSafetyPersistence.cs:70-72` |

## F3 — Asset report → review → action (the real report lifecycle, in Assets)

```mermaid
flowchart TD
    A[User POST report asset] --> B[ReportAssetCommand<br/>Assets-Commands-ReportAssetCommand.cs-11-14]
    B --> C{asset exists?}
    C -->|no| D[null → 404 shape]
    C --> E{reporter is creator?}
    E -->|yes| D
    E -->|no| F[CreateReportAsync report row Pending]
    G[Admin GET moderation queue<br/>GetModerationQueueQuery] --> H[queue with ReportCount]
    I[Admin POST review report<br/>ReviewReportCommand] --> J{decision}
    J -->|NoAction| K[Dismissed]
    J -->|ContentRemoved or ContentHidden or UserWarned or ..| L[Resolved + action on asset]
    F --> M[ReportStatus Pending]
    M --> N[UnderReview]
    N --> O[Resolved or Dismissed]
```

Edge cases:

| Case | Behavior | Evidence |
|---|---|---|
| Self-report blocked | creator cannot report own asset | `Assets:/Commands/ReportAssetCommand.cs:51-58` |
| Report reasons enum | Inappropriate Copyright Spam Violence … | `Assets:/Models/Enums.cs:111-136` |
| Review decisions enum | NoAction ContentRemoved ContentHidden UserWarned … | `Enums.cs:156-168` |
| Report statuses | Pending UnderReview Resolved Dismissed | `Enums.cs:138-150` |
| Validators present (unlike Gamification) | FluentValidation on both commands | `ReportAssetCommand.cs:21-28`; `ReviewReportCommand.cs:19-26` |

## F4 — Auto-moderation classification lane (images only)

```mermaid
flowchart TD
    A[Asset upload] --> B{AutoModerationOptions.Enabled?<br/>ModerationService.cs-19-58}
    B -->|false| C[worker exits queue frozen silently N13]
    B -->|true| D[ModerationClassificationWorker polls 5s]
    D --> E[IAssetModerationClassifier]
    E -->|host default| F[LitellmModerationClassifier<br/>API-Security-LitellmModerationClassifier.cs-32]
    E -->|module fallback| G[UnresolvedModerationClassifier<br/>approved true unresolved T4]
    F --> H[LiteLLM vision proxy qwen<br/>8-label taxonomy cs-34-39]
    H --> I{confidence vs thresholds}
    I -->|score ge 0-80 safe| J[Approved]
    I -->|unsafe score ge 0-75| K[Rejected]
    I -->|middle or gif-mp4-webm| L[NeedsReview human queue]
    F -->|error x3| L
    F -->|unparseable| L[never auto-approve on failure cs-119-161]
```

Edge cases:

| Case | Behavior | Evidence |
|---|---|---|
| API key missing | throws `ModerationClassificationException` → NeedsReview path | `LitellmModerationClassifier.cs:57-61` |
| Model output garbage | parse failure throws → callers route to NeedsReview, never approval | `:119-161` doc |
| Human-review MIME types | gif/mp4/webm always human | `Assets:/Moderation/ModerationService.cs:50-58` |
| Disabled = silent freeze | worker exits; uploads pile Pending; nothing alerts | `docs/social-platform/05-gaps.md:53-54` (N13) |

## F5 — Zero text moderation (the #1250 N1 truth)

```mermaid
flowchart TD
    TXT[User text post comment blog review] --> P[(persistence unfiltered)]
    P --> Q[NO text analyzer exists anywhere]
    R[TrustSafety module] -.named as future host.-> S[3 files zero text checks]
    T[Media] -->|only gated path| U[Assets classification F4 + attach-safe-to-serve]
    style Q fill:#fdd
    style S fill:#fdd
```

The flowchart documents an absence: every text surface (`Social.Posts` 26 endpoints, comments, blog, ratings review text) persists unfiltered (`docs/social-platform/05-gaps.md:17-18`); TrustSafety contains no text APIs; the only policy vocabulary that exists is the image label taxonomy (F4). Product decision pending (#1250 note 3).
