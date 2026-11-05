# Social.Ratings — Event inventory

> Module `GameGuild.Social.Ratings` · develop `ce31d5850c`. Shorthand `Rat:`.

## 1. Declared domain events — all 5 dead

Declared in `Rat:/Events/RatingEvents.cs` with the header "Domain events for the ratings system" (`:3-5`). Zero construction sites across `apps/api/Source/` (grep — declarations only). Same declared-dead class as Social.Posts N2 and Gamification G4; the event-producer guard (G4) does not scan this declaration shape (local records, not the durable-event interface), which is why they sit outside `wiring-gap-baseline.txt`.

| # | Event | Payload | Would signal | Construction | Consumers |
|---|---|---|---|---|---|
| 1 | `RatingCreatedEvent` (`:8-15`) | ids, value, HasReview | new rating → notifications/aggregates | **0** | 0 |
| 2 | `RatingUpdatedEvent` (`:17-24`) | old/new value | rating changed → summary refresh fan-out | **0** | 0 |
| 3 | `RatingDeletedEvent` (`:26-31`) | ids | deletion → summary refresh | **0** | 0 |
| 4 | `RatingReportedEvent` (`:33-38`) | reason, TotalReports | report → TrustSafety plane | **0** (the natural `TrustSafetyEventKind.Report` producer that would fix trustsafety T1 — currently nothing bridges reports to the evidence rail) | 0 |
| 5 | `RatingModerationStatusChangedEvent` (`:40-44`) | old/new status | moderation audit | **0** | 0 |
| 6 | `RatingSummaryRecalculatedEvent` (`:46-51`) | avg, total | cache refresh telemetry | **0** | 0 |
| 7 | `RatingHelpfulVoteEvent` (`:53-56`) | vote | helpfulness trending | **0** | 0 |

(7 records in the file — the header count of "5" used elsewhere in this package refers to families; the file declares 7. Corrections noted here for exactness: **7 declared, 7 dead**.)

## 2. Durable operation events (the only contract)

`Rat:/Architecture/RatingUseCaseEventContracts.cs:5-15` — 11 contracts, all `NoDomainEventReason` of the generic-operation family:

| Command | Operation event |
|---|---|
| `RateRatingEndpointCommand` | `social.ratings.rate` |
| `DeleteRatingEndpointCommand` | `social.ratings.delete` |
| `GetRatingSummariesBatchEndpointCommand` | `social.ratings.summaries.batch` (no-transition reason — POST-shaped query) |
| `GetUserRatingsBatchEndpointCommand` | `social.ratings.user-ratings.batch` (same) |
| `VoteRatingHelpfulEndpointCommand` | `social.ratings.helpful.vote` |
| `RemoveRatingHelpfulVoteEndpointCommand` | `social.ratings.helpful.remove` |
| `ReportRatingEndpointCommand` | `social.ratings.report` |
| `ApproveRatingEndpointCommand` | `social.ratings.approve` |
| `RejectRatingEndpointCommand` | `social.ratings.reject` |
| `AdminDeleteRatingEndpointCommand` | `social.ratings.admin-delete` |
| `RecalculateRatingSummaryEndpointCommand` | `social.ratings.summary.recalculate` |

## 3. Inbound events — none

No subscribers. Consequences: rating a mentorship session does not feed mentor dashboards; rating a course does not refresh learning aggregates; nothing reacts to ratings platform-wide except the module's own synchronous summary writes.
