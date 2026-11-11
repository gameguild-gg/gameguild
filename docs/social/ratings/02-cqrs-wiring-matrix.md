# Social.Ratings — CQRS / wiring matrix

> Module `GameGuild.Social.Ratings` · develop `ce31d5850c`. Shorthand `Rat:`, `API:`, `Authz:`.

## 0. DI wiring

| Piece | Registration | Evidence | Live? |
|---|---|---|---|
| Crud + query + moderation services | `AddRatingsModule` | `Rat:/RatingsModule.cs:12-20` | **yes** |
| Facade `IRatingService` | same (backward compat for controllers) | `:23` | **yes** |
| Handler (1 class, 11 interfaces) | module scan — `"Social.Ratings"` in `EnabledModules` | `Rat:/Commands/RatingEndpointCommands.cs:23-87`; `API:/Core/Setup/ApiProductComposition.cs:78`; `ApplicationLayerExtensions.cs:110-145` | **yes** |
| Tables ×3 | `RatingsModelConfiguration : IModelConfiguration` | `Rat:/Configuration/RatingEntityConfiguration.cs:98-105`; migration `20261102105545_AddLedgersRatingsAndAchievementsTables` (`:97, :116, :143`) | **yes** (the #1169 materialization) |
| Host call | `AddRatingsModule` | `API:/Core/Setup/ApiProductComposition.cs:187` | **yes** |
| G-baseline | no ratings entries (handler fully wired) | baseline grep | **clean** |
| **E9 baseline** | **19 route/principal combos** — EF translation failure | `apps/api/tests/GameGuild.API.UnitTests/Architecture/wiring-gap-baseline.txt:157-192, 235` | **failing backlog** (#1278, R1) |

**Auth legend** — class `[Authorize]`; 9 read routes opt out with `[AllowAnonymous]`; 5 admin routes add `[Authorize(Policy = "TenantAdmin")]` (`Authz:/Policies.cs:33`). **Rate limit** — none.

## 1. Live HTTP surface (21 routes, `api/ratings` — `Rat:/Controllers/RatingsController.cs`)

| Route | Request | Handler iface | Auth | HOLE? |
|---|---|---|---|---|
| POST `` | `RateRatingEndpointCommand` | `ICommandHandler<..., Result<Rating>>` (`RatingEndpointCommands.cs:24`) | authed | EntityType unvalidated (R2); value-range error shape |
| GET `{ratingId}` | service `GetByIdAsync` | — | **anon** | E9 500 (R1) |
| GET `my/{entityType}/{entityId}` | service `GetUserRatingAsync` | — | authed | E9 500 |
| GET `has-rated/{entityType}/{entityId}` | service | — | authed | E9 500 |
| DELETE `{ratingId}` | `DeleteRatingEndpointCommand` | `:29` | authed | owner-scoped |
| GET `entity/{entityType}/{entityId}` | service `GetRatingsAsync` | — | **anon** | E9 500 |
| GET `summary/{entityType}/{entityId}` | service `GetSummaryAsync` | — | **anon** | E9 500 |
| POST `summaries/batch` | `GetRatingSummariesBatchEndpointCommand` | `:31` | **anon** | E9 500 |
| POST `my/batch` | `GetUserRatingsBatchEndpointCommand` | `:37` | authed | E9 500 |
| GET `user/{userId}` | service `GetUserRatingsAsync` | — | **anon** | E9 500; **exposes any user's reviews platform-wide** |
| GET `count/{entityType}/{entityId}` | service `GetCountAsync` | — | **anon** | E9 500; count-vs-summary moderation mismatch (R6) |
| POST `{ratingId}/helpful` | `VoteRatingHelpfulEndpointCommand` | `:44` | authed | own-vote blocked |
| DELETE `{ratingId}/helpful` | `RemoveRatingHelpfulVoteEndpointCommand` | `:48` | authed | E9 500 |
| POST `{ratingId}/report` | `ReportRatingEndpointCommand` | `:52` | authed | no reporter identity (R3) |
| GET `top/{entityType}` | service `GetTopRatedAsync` | — | **anon** | E9 500; phantom entities rankable (R2) |
| GET `recent-reviews` | service `GetRecentReviewsAsync` | — | **anon** | E9 500 |
| GET `admin/moderation` | service `GetPendingModerationAsync` | — | TenantAdmin | queue is Flagged-only in practice (R4) |
| POST `admin/{ratingId}/approve` | `ApproveRatingEndpointCommand` | `:62` | TenantAdmin | |
| POST `admin/{ratingId}/reject` | `RejectRatingEndpointCommand` | `:66` | TenantAdmin | |
| DELETE `admin/{ratingId}` | `AdminDeleteRatingEndpointCommand` | `:70` | TenantAdmin | |
| POST `admin/recalculate/{entityType}/{entityId}` | `RecalculateRatingSummaryEndpointCommand` | `:74` | TenantAdmin | manual heal for R5 staleness |

## 2. Request-type census

| Category | Count | With handler | Without handler |
|---|---|---|---|
| Endpoint commands (`Rat:/Commands/RatingEndpointCommands.cs`) | 11 | 11 (one class) | 0 |
| Queries | 0 (service-direct reads, like Gamification/Achievements) | — | — |
| **Total** | **11** | **11** | **0 dead request types** |

## 3. Dead-surface reconciliation (the G7-class check)

| # | Surface | Meant to do | Deepest implementation | Verdict |
|---|---|---|---|---|
| S1 | 5 domain events (`Rat:/Events/RatingEvents.cs:8-56`) | rating lifecycle telemetry (created/updated/deleted/reported/moderation/summary/vote) | declarations only — zero construction sites | **declared-dead** (04) |
| S2 | `RatingModerationStatus.Pending` | pre-moderation queue for new reviews | enum value + queue query includes it; nothing ever sets it (born Approved) | **unreachable state** (R4) |
| S3 | `Rating.IsVerified` / `MarkAsVerified` | verified-purchase badge | setter exists; **zero callers** — `IsVerified` is always false; the `verifiedOnly` filter and DTO flag are decoration | **write-orphaned** (R7) |
| S4 | `RatingHelpfulVote.IsHelpful=false` rows in aggregate | "not helpful" signals | rows stored; nothing aggregates them (only helpful-true increments a counter) | decoration |

No Ledger-class dead request types — the module is handler-complete; deadness is in states/events/flags.

## 4. The E9 baseline table (#1278 cross-ref)

19 entries, all one root cause — `apps/api/tests/GameGuild.API.UnitTests/Architecture/wiring-gap-baseline.txt`:

| Baseline lines | Routes |
|---|---|
| `:157-158` | DELETE `{id}/helpful`, DELETE `{id}` (authed) |
| `:177-192` | GET `{id}`, `count/1/{id}`, `entity/1/{id}`, `has-rated/1/{id}`, `my/1/{id}`, `recent-reviews`, `summary/1/{id}`, `top/1`, `user/{id}` (anon + authed) |
| `:235` | POST `{id}/helpful` (authed) |

Each comment: "ratings EF query translation failure (unmapped member) → 500 on live reads" — anatomy in 09 §4.
