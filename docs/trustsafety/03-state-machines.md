# TrustSafety — State machines

> Module `GameGuild.TrustSafety` + adjacent report lifecycles · develop `ce31d5850c`. Shorthand `TS:`, `Assets:`, `Ratings:`.

## 1. Evidence event inbox row (`trust_safety_event_inbox`)

Not a classic state machine — a processing-outcome column (`ProcessedAt`, `ProcessingError`) with three terminal shapes:

```mermaid
stateDiagram-v2
    [*] --> Received : inbox row inserted with signature outcome<br/>TS-PostgreSqlTrustSafetyControlPlane-cs-121-127
    Received --> Processed : ProcessedAt set evidence published state advanced<br/>cs-183-185
    Received --> Deferred : ProcessingError out-of-order-event<br/>re-ingest same EventId returns Deferred cs-100-102-133-137
    Received --> Rejected : ProcessingError invalid-signature<br/>cs-123-127
    Processed --> Duplicate : replay same EventId same payload<br/>cs-96-99
    Deferred --> Processed : predecessor arrives then re-ingest<br/>no automatic reordering worker
    Rejected --> [*] : terminal signature failure
```

| Guard | Enforcement |
|---|---|
| exactly-once per EventId | unique index (`TrustSafetyPersistence.cs:33`) |
| one event per (tenant, subject, version) | unique composite index (`:34-35`) |
| replay with different payload | `TrustSafetyConflictException` (`PostgreSqlTrustSafetyControlPlane.cs:92-95`) |
| no automatic retry of Deferred | only re-ingestion of the same event re-evaluates (design: caller retries) |

## 2. Subject state (`trust_safety_subject_states`, keyed tenant+subjectHash)

```mermaid
stateDiagram-v2
    [*] --> Constrained : event Outcome Rejected or NeedsReview<br/>hold activated cs-145-155
    [*] --> Clear : first event Approved no hold
    Constrained --> Constrained : next version non-Approved<br/>hold already active stays
    Constrained --> Clear : Approved releases hold SystemActor cs-139-144
    Clear --> Constrained : later non-Approved event
    note right of Constrained : Version is a monotonic counter Version-concurrency-token<br/>ExpiresAt bounds evidence freshness for readers
```

The economy **reads** this via `PostgreSqlTrustSafetyRiskInputSource` (evidence `ExpiresAt` honored — `TS:/TrustSafetyModule.cs:40-58`); expired evidence reads as `Unavailable` → economy treats as no-objection-by-default? No — `ExternalRiskOutcome.Unavailable` propagates and the **economy policy** decides what Unavailable means (see docs/economy risk chain).

## 3. Appeal (`trust_safety_appeals`)

```mermaid
stateDiagram-v2
    [*] --> Submitted : SubmitAppealAsync v1 unassigned<br/>ZERO CALLERS T1
    Submitted --> Assigned : AssignAppealAsync reviewer Version++<br/>cs-211-227
    Assigned --> Upheld : DecideAppealAsync overturn false<br/>reasonCode evidenceHash Version++ cs-229-250
    Assigned --> Overturned : overturn true
    Submitted --> Submitted : idempotent replay same id+hash+submitter cs-194-204
    Upheld --> [*]
    Overturned --> [*]
```

| Guard | Enforcement |
|---|---|
| new appeal must be `Submitted`, v1, unassigned, undecided | `ValidateAppeal` (`:350-367`) |
| only Submitted can be assigned, version-checked | `:217-220` |
| only Assigned-to-this-reviewer can decide, version-checked | `:237-239` |
| decision fields coupled to state | DB check constraint (`TrustSafetyPersistence.cs:70-72`) |
| optimistic concurrency | `Version` concurrency token on the row (`:80`) |

Note the deliberate hard stop: **no Un-assign, no reopen, no re-appeal transition** — the lifecycle is one-shot by design.

## 4. Content report lifecycles (the surfaces users actually touch)

**Assets report** (`ReportStatus`, `Assets:/Models/Enums.cs:138-150`):

```mermaid
stateDiagram-v2
    [*] --> Pending : ReportAssetCommand
    Pending --> UnderReview : reviewer picks from queue
    UnderReview --> Resolved : ReviewReportCommand decision ContentRemoved-ContentHidden-UserWarned-..
    UnderReview --> Dismissed : decision NoAction
    Resolved --> [*]
    Dismissed --> [*]
```

**Ratings report → moderation** (`Ratings:/Entities/Rating.cs` + `Services/RatingModerationService.cs`):

```mermaid
stateDiagram-v2
    [*] --> Approved : rating created default Approved<br/>Rating-cs-84
    Approved --> Flagged : ReportCount reaches 3 auto-flag<br/>RatingModerationService-cs-145-151
    Pending --> Approved : admin approve
    Flagged --> Approved : admin approve
    Approved --> Rejected : admin reject hidden from queries
    Rejected --> Approved : admin re-approve
    note right of Flagged : Pending 0 is unreachable through the API<br/>every rating is born Approved see ratings 03
```

**Asset moderation status** (`ModerationStatus`, `Assets:/Models/Enums.cs:84-96`): Pending → Processing → Approved / Rejected / NeedsReview, driven by the classification lane (01 F4).
