# Social.Ratings — Cross-module sequences

> Module `GameGuild.Social.Ratings` · develop `ce31d5850c`. Shorthand `Rat:`.

## 1. Rate → synchronous summary recomputation

```mermaid
sequenceDiagram
    participant U as Authenticated user
    participant C as RatingsController
    participant H as RatingEndpointCommandHandler
    participant S as RatingCrudService
    participant Q as RatingQueryService
    participant DB as ratings schema

    U->>C: POST api-ratings entityId entityType value review?
    C->>H: RateRatingEndpointCommand
    H->>S: RateAsync
    S->>DB: SELECT existing user-entity row
    alt exists
        S->>DB: UPDATE value text EditedAt
    else new
        S->>DB: INSERT Rating born Approved
    end
    S->>Q: RecalculateSummaryAsync
    Q->>DB: SELECT all ratings for entity
    Q->>DB: UPDATE-INSERT RatingSummary histogram Approved-only
    note over S,Q: no transaction — crash between writes leaves stale summary R5
    H-->>C: dto
    C-->>U: 200
```

## 2. Report → auto-flag → admin decision

```mermaid
sequenceDiagram
    participant R as Reporter any authed
    participant M as RatingModerationService
    participant DB as ratings schema
    participant A as TenantAdmin

    R->>M: ReportAsync ratingId reason
    M->>DB: ReportCount++
    alt ReportCount ge 3
        M->>DB: ModerationStatus Flagged
        note over DB: review vanishes from all public reads Approved-only
    end
    note over M,DB: no reporter row no bound R3 — third report always flips
    A->>M: GET admin-moderation Flagged ordered by ReportCount
    A->>M: POST reject or approve
    M->>DB: status change + summary recalculate
```

## 3. Helpful vote with counter maintenance

```mermaid
sequenceDiagram
    participant V as Voter
    participant M as RatingModerationService
    participant DB as ratings schema

    V->>M: POST helpful IsHelpful
    M->>DB: load rating not-deleted
    M->>M: own-rating guard → 400
    M->>DB: existing vote?
    alt new
        M->>DB: INSERT vote row
        M->>DB: HelpfulCount++ if helpful
    else flip
        M->>DB: UPDATE IsHelpful
        M->>DB: counter ± on transition
    end
```

## 4. The polymorphic EntityType boundary contract

```mermaid
sequenceDiagram
    participant U as Rater
    participant S as RatingCrudService
    participant DB as ratings schema
    participant T as Target module Course-Post-anything

    U->>S: RateAsync entityId "Course" 5
    note over S,DB: NO existence check NO type registry NO tenant predicate
    S->>DB: INSERT accepted as-is
    note over T: target module never asked never notified<br/>R2 — phantom entities accepted summary rows created
    U->>S: GET top "Course"
    S->>DB: summaries ordered avg desc
    DB-->>U: includes phantoms
```

The module is a **free-floating reputation store**: correctness of (EntityId, EntityType) pairs is entirely the caller's responsibility. Consumers today must treat EntityType strings as a convention, not a contract (grep for producer modules calling `RateAsync` — none in-repo yet; the surface is client-driven).

## 5. The E9 failure sequence (R1 — what actually happens at runtime)

```mermaid
sequenceDiagram
    participant C as Client
    participant API as RatingsController
    participant S as RatingQueryService
    participant EF as EF Core Npgsql
    participant DB as PostgreSQL

    C->>API: GET summary entity-type entity-id anonymous
    API->>S: GetSummaryAsync
    S->>EF: query with global filter not-IsDeleted
    EF->>EF: translate IsDeleted → EntityBase computed member no column mapping
    EF-->>S: InvalidOperationException could not be translated unmapped member
    S-->>API: exception
    API-->>C: 500
    note over EF,DB: unit tests pass — InMemory provider evaluates filters client-side<br/>19 baseline entries wiring-gap-baseline-157-192-235 #1278 E9
```

Root cause and fix directions in 09 §4.
