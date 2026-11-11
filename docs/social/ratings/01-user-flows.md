# Social.Ratings — User flows

> Module `GameGuild.Social.Ratings` · develop `ce31d5850c`. Shorthand `Rat:` = module root. Families match 07/08.

> **Reading note (R1)**: every read flow below is the *designed* behavior; at runtime against a real database each currently 500s on the `IsDeleted` translation failure until the E9 backlog item is fixed. The flows document the logic the route implements.

## F1 — Rate an entity (create or update)

```mermaid
flowchart TD
    A[User POST api-ratings] --> B[RateRatingEndpointCommand<br/>Controllers-RatingsController.cs-26-41]
    B --> C[RatingEndpointCommandHandler → RateAsync<br/>Services-RatingCrudService.cs-25-72]
    C --> D{existing row for user+entity not deleted?}
    D -->|yes| E[Rating-Update value text title sets EditedAt<br/>Entities-Rating.cs-77-88]
    D -->|no| F{Value 1-5?}
    F -->|out of range| G[ArgumentOutOfRangeException → 500 shape]
    F -->|ok| H[Rating-Create born Approved ModerationStatus-1<br/>Entities-cs-49-74]
    E & H --> I[SaveChanges]
    I --> J[RecalculateSummaryAsync synchronous full reload<br/>RatingQueryService.cs-120-146]
    J --> K[200 RatingDto]
```

Edge cases:

| Case | Behavior | Evidence |
|---|---|---|
| EntityType free text | any string ≤100; no existence check, no allowlist | `Rat:/Commands/RatingEndpointCommands.cs:5-10`; `Entities/Rating.cs:70` |
| One rating per user per entity | unique index; rate = update-in-place | `Entities/Rating.cs:22`; `Services/RatingCrudService.cs:33-45` |
| Value bounds enforced at entity | 1-5 or throw (controller passes through → error shape, not 400) | `Entities/Rating.cs:54-56, 77-80` |
| Review text/title unfiltered | no profanity/length service checks (MaxLength attributes only) — text moderation N/A platform-wide | `Entities/Rating.cs:36-43` |
| Summary updated post-save, no transaction | crash window leaves stale summary (R5) | `Services/RatingCrudService.cs:52-60` |

## F2 — Helpful vote

```mermaid
flowchart TD
    A[User POST ratings-id-helpful IsHelpful] --> B[VoteRatingHelpfulEndpointCommand]
    B --> C[VoteHelpfulAsync<br/>RatingModerationService.cs-18-77]
    C --> D{rating exists?}
    D -->|no| E[404 RatingErrors-NotFound]
    D -->|yes| F{own rating?}
    F -->|yes| G[400 CannotVoteOwnRating]
    F -->|no| H{existing vote row?}
    H -->|yes| I[UpdateVote flip<br/>adjust HelpfulCount on transition]
    H -->|no| J[Create vote IsHelpful captured]
    J -->|helpful true| K[IncrementHelpful]
    I --> L[SaveChanges 204]
    A2[DELETE ratings-id-helpful] --> B2[RemoveHelpfulVoteEndpointCommand]
    B2 --> C2[RemoveHelpfulVoteAsync<br/>-80-108]
    C2 --> D2{vote exists?}
    D2 -->|no| E2[404 VoteNotFound]
    D2 -->|yes and was helpful| F2[DecrementHelpful + vote SoftDelete]
```

Edge cases:

| Case | Behavior | Evidence |
|---|---|---|
| Flip helpful→not | HelpfulCount decremented (vote row kept, `IsHelpful=false`) | `Rat:/Services/RatingModerationService.cs:44-54` |
| Vote on already-deleted rating | filtered (`!r.IsDeleted`) → 404 | `:26-30` |
| Remove decrements only helpful votes | `vote.IsHelpful` checked | `:96-103` |
| No dedup window | re-vote toggling is free (no cooldown) | handler shape |

## F3 — Report a review → auto-flag → moderation

```mermaid
flowchart TD
    A[User POST ratings-id-report Reason] --> B[ReportRatingEndpointCommand]
    B --> C[ReportAsync<br/>RatingModerationService.cs-110-152]
    C --> D[IncrementReport]
    D --> E{ReportCount ge 3?}
    E -->|yes| F[SetModerationStatus Flagged]
    E -->|no| G[keep Approved]
    F & G --> H[SaveChanges 204]
    H --> I[LogWarning with Reason — no reporter stored R3]
    J[TenantAdmin GET ratings-admin-moderation] --> K[Pending-or-Flagged ordered by ReportCount<br/>-86-105 queue in practice Flagged-only R4]
    L[TenantAdmin POST admin-id-approve] --> M[Approved + RecalculateSummary]
    N[TenantAdmin POST admin-id-reject] --> O[Rejected hidden + RecalculateSummary]
    P[TenantAdmin DELETE admin-id] --> Q[SoftDelete + RecalculateSummary]
```

Edge cases:

| Case | Behavior | Evidence |
|---|---|---|
| Reporter identity | **not persisted** — only a log line | `Rat:/Services/RatingModerationService.cs:150-152` |
| Unbounded reports | no per-reporter or per-rating bound | handler shape (R3) |
| Reason free text | logged, max length enforced only by DTO shape | `Controllers/RatingsController.cs:397` |
| Flagged excluded from public reads immediately | Approved-only filters | `Services/RatingQueryService.cs` reads |

## F4 — Aggregate reads

```mermaid
flowchart TD
    A[GET summary-entityType-entityId anonymous] --> B[GetSummaryAsync reads RatingSummary row]
    C[POST summaries-batch anonymous] --> D[GetRatingSummariesBatchEndpointCommand]
    E[GET top-entityType minRatings take] --> F[GetTopRatedAsync summary table ordered avg desc]
    G[GET recent-reviews entityType optional] --> H[Approved reviews newest take default 20]
    I[GET entity-... filters] --> J[min-max value withReview verifiedOnly sort paging]
    K[GET count] --> L[count of Approved? count includes all non-deleted — see note]
```

Edge cases / honesty notes:

| Case | Behavior | Evidence |
|---|---|---|
| Summary counts Approved-only; **GetCountAsync counts non-deleted regardless of moderation** | read paths disagree under moderation | `Rat:/Services/RatingQueryService.cs:120-146` (Recalculate Approved filter) vs count implementation — verify at fix time (R6) |
| Distribution percentages | computed on the DTO from histogram | `Entities/Rating.cs` `GetDistributionPercentages` |
| Top requires `TotalRatings >= minRatings` default 5 | Wilson-style correction absent — avg desc with count tiebreak only | `Services/RatingQueryService.cs:158-172` |
| Batch POST-shaped reads | query commands carrying POST bodies (no state change — contracts say so) | `Architecture/RatingUseCaseEventContracts.cs:7-8` |

## F5 — Delete own rating

```mermaid
flowchart TD
    A[User DELETE ratings-id] --> B[DeleteRatingEndpointCommand]
    B --> C[DeleteAsync<br/>RatingCrudService.cs-100-119]
    C --> D{row owner eq actor and not deleted?}
    D -->|no| E[404]
    D -->|yes| F[SoftDelete]
    F --> G[RecalculateSummaryAsync]
    G --> H[204]
```

Note: the **admin** delete (F3) is the same soft-delete plus TenantAdmin gate; there is no hard delete anywhere in the module.
