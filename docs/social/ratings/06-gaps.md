# Social.Ratings — Gaps

> Module `GameGuild.Social.Ratings` · develop `ce31d5850c`. KNOWN vs NEW per repo convention.

| # | Severity | Gap | Evidence | Status |
|---|---|---|---|---|
| R1 | **critical** | **EF translation failure breaks every ratings read at runtime** — `EntityBase.IsDeleted` is a get-only computed member (`DeletedAt.HasValue`) with no column mapping; the module's `HasQueryFilter(e => !e.IsDeleted)` plus explicit predicates cannot translate → 500 on live reads; 19 E9 baseline combos; unit tests blind to it (InMemory evaluates client-side) | `SK:/Entities/EntityBase.cs:40`; `Rat:/Configuration/RatingEntityConfiguration.cs:32, 57`; `wiring-gap-baseline.txt:157-192, 235` | KNOWN (#1278 E9 backlog) |
| R2 | high | **EntityType is unvalidated free text** — no existence check, no allowlist, no tenant predicate; phantom ratings and summary rows accepted; `top/{entityType}` ranks them | `Rat:/Commands/RatingEndpointCommands.cs:5-10`; `Services/RatingCrudService.cs:25-45` | NEW |
| R3 | high | **Report system is bombable** — no reporter identity, no per-reporter dedup, no bound; 3rd report auto-flags and hides a review; only admin restore | `Rat:/Services/RatingModerationService.cs:110-152` | NEW (cross-ref trustsafety T6) |
| R4 | medium | **`Pending` moderation state unreachable** — ratings born Approved; the pre-moderation mode the queue implies does not exist | `Rat:/Entities/Rating.cs:84`; `Services/RatingModerationService.cs:86-105` | NEW |
| R5 | medium | **Mutation and summary recalculation not atomic** — crash between saves leaves stale aggregates until next mutation or admin recalculate; same non-transactional shape around helpful-count maintenance | `Rat:/Services/RatingCrudService.cs:52-60`; `RatingQueryService.cs:120-146` | NEW |
| R6 | medium | **Read-path moderation disagreement** — summary aggregates count Approved-only, `GetCountAsync` counts non-deleted regardless; public count can exceed summary total under moderation | `Rat:/Services/RatingQueryService.cs` (count impl vs `Entities/Rating.cs` Recalculate filter) | NEW |
| R7 | low | **`IsVerified` write-orphaned** — `MarkAsVerified` has zero callers; verified-purchase badge and `verifiedOnly` filter are decoration | `Rat:/Entities/Rating.cs:90` | NEW |
| R8 | low | **7 domain events declared-dead** — the declared telemetry surface is fiction; `RatingReportedEvent` is precisely the missing trustsafety `Report`-kind producer | `Rat:/Events/RatingEvents.cs:8-56` | NEW (class known repo-wide) |
| R9 | low | **Anonymous user-ratings directory** — `GET user/{userId}` exposes any user's full review history platform-wide without auth | `Rat:/Controllers/RatingsController.cs:187-206` | NEW (product decision needed) |
| R10 | info | **O(n) recalculation per mutation** — full reload of the entity's ratings on every rate/vote-adjacent mutation; fine at small n, no batching/caching | `Rat:/Services/RatingQueryService.cs:126-135` | NEW |
| R11 | info | **Value-range violation returns 500-shape** — `ArgumentOutOfRangeException` from the entity reaches the client un-mapped (no 400) | `Rat:/Entities/Rating.cs:54-56` + controller passthrough | NEW |
| — | info | Historical: tables never materialized pre-#1169 — **fixed** by `20261102105545_AddLedgersRatingsAndAchievementsTables` | migration; `RatingsModelConfiguration` | KNOWN (fixed) |

## Recommended acceptance order

1. R1 — the E9 fix is the module's unblock: either map `IsDeleted` as a real (shadow) column on the three entities or replace filter+predicates with `DeletedAt == null` (translatable); then delete the 19 baseline lines. Until then **every consumer of ratings data is down** — this is the highest-leverage single fix in the five packages.
2. R2 — validate EntityType against a registered target list (course/post/…), return 404 for unknown targets; clean phantom summaries.
3. R3 — persist report rows `(ratingId, reporterId, reason, at)` with a per-reporter unique index; threshold on distinct reporters; consider wiring `RatingReportedEvent` into the TrustSafety evidence rail (fixes two modules at once).
4. R5 — wrap mutation+recalculate in one transaction (or move recalc to the operation-event subscriber).
5. R6/R7/R9/R11 — cheap consistency/UX hygiene.
