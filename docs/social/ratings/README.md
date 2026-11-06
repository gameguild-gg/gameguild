# GameGuild.Social.Ratings — Documentation Package

> Module `GameGuild.Social.Ratings` · Documented against `develop` commit `ce31d5850c` (merge of #1311).
> Scope: the polymorphic 1-5-star rating module — rate-or-update per (user, entity), helpful votes with own-vote blocking, report-driven auto-flagging at 3, admin moderation (approve/reject/admin-delete), and synchronous `RatingSummary` aggregate recomputation (star histogram + average + verified/review counts). Tables materialized via the #1169 fix. **The package's central finding is the #1278 E9 EF-translation failure**: the soft-delete predicate `IsDeleted` is an unmapped computed member of `EntityBase`, and every ratings read route 500s at runtime against a real database (19 E9 baseline entries).

## Document inventory

| File | Contents |
|------|----------|
| [`01-user-flows.md`](01-user-flows.md) | Mermaid `flowchart TD` flows — rate-or-update, helpful vote / remove, report → auto-flag, admin moderation, summary reads (single/batch/top/recent) — each with edge-case matrices and file:line |
| [`02-cqrs-wiring-matrix.md`](02-cqrs-wiring-matrix.md) | DI wiring (facade + 3 sub-services + one 11-interface command handler) + route → command → handler → auth matrix for the **21 live routes**, anonymous-route census, E9 baseline table |
| [`03-state-machines.md`](03-state-machines.md) | `stateDiagram-v2` — rating moderation lifecycle (with unreachable `Pending`), helpful-vote lifecycle, summary refresh semantics |
| [`04-event-inventory.md`](04-event-inventory.md) | 5 declared domain events **all never constructed** (declared-dead), 11 generic durable operation events (`social.ratings.*`) |
| [`05-cross-module-sequences.md`](05-cross-module-sequences.md) | `sequenceDiagram`s — rate→recalculate, report→flag→moderate→recalculate, helpful vote, the polymorphic EntityType boundary contract (no target validation), the E9 failure sequence |
| [`06-gaps.md`](06-gaps.md) | KNOWN vs NEW — headline R1: **the E9 IsDeleted translation failure breaks every read** (cross-ref #1278); R2: EntityType is unvalidated free text (ratings on anything, summaries table pollution); R3: report bombing auto-flags at 3 with no reporter identity |
| [`07-stride-threat-model.md`](07-stride-threat-model.md) | STRIDE per family — rating brigading, self-vote inflation, phantom-entity rating, summary skew via unvalidated EntityType, report abuse |
| [`08-dfds.md`](08-dfds.md) | L0 context + L1 per family with trust boundaries (9 anonymous reads → API → services → ratings schema) |
| [`09-security-architecture.md`](09-security-architecture.md) | Component view (facade + focused services pattern), permission map, integrity properties and non-properties (sync recalc, no verified-purchase proof), the E9 root-cause anatomy, deployment invariants |

## Conventions

- Follows `docs/auth/flows/*` / `docs/finance-ledgers/*` conventions; Mermaid labels without parentheses/colons.
- Citation shorthand: `Rat:` = `apps/api/Source/Modules/GameGuild.Social.Ratings/`, `API:` = `apps/api/Source/GameGuild.API/`, `SK:` = `apps/api/Source/Modules/GameGuild.SharedKernel/`, `Authz:` = `apps/api/Source/Modules/GameGuild.Identity.Authorization/`.
- Tests: `apps/api/tests/GameGuild.Social.Ratings.UnitTests/` (facade, entities, events/DTOs, configuration+module). Local `dotnet test` blocked (INC-PG hold).

## Module shape at a glance

- **15 files, ~1.9k LOC**: 1 controller (`RatingsController` — 21 routes on `api/ratings`, 9 of them `[AllowAnonymous]` reads, 5 admin with `TenantAdmin`), 11 endpoint commands handled by a single `RatingEndpointCommandHandler` implementing 11 `ICommandHandler` interfaces (`Rat:/Commands/RatingEndpointCommands.cs:23-87`), 4 services (`IRatingService` facade over `RatingCrudService` + `RatingQueryService` + `RatingModerationService`), 3 entities (`Rating`, `RatingHelpfulVote`, `RatingSummary`), 5 declared-dead domain events.
- **Tables (3)**: `ratings`, `rating_helpful_votes`, `rating_summaries` — materialized by `RatingsModelConfiguration : IModelConfiguration` (`Rat:/Configuration/RatingEntityConfiguration.cs:98-105`) in migration `20261102105545_AddLedgersRatingsAndAchievementsTables` (`:97, :116, :143`) — the #1169/#1158 materialization fix (same wave as Ledgers and Achievements).
- **Wiring**: `"Social.Ratings"` in `EnabledModules` (`API:/Core/Setup/ApiProductComposition.cs:78`); `AddRatingsModule` registers the 3 sub-services + facade (`Rat:/RatingsModule.cs:12-24`) and **is** called by the host (`ApiProductComposition.cs:187`).
- **Key semantics**: one rating per (EntityId, EntityType, UserId) — unique index (`Entities/Rating.cs:22`) with rate-or-update in the service; every mutation synchronously reloads all of the entity's ratings and recomputes `RatingSummary` (`Rat:/Services/RatingQueryService.cs:120-146`); aggregates count **Approved-only** (`Entities/Rating.cs` `Recalculate` filter); reports auto-flag at ≥3 (`Services/RatingModerationService.cs:145-151`); helpful votes blocked on own rating (`:26-31`).
- **Events**: 5 domain event records, zero construction sites (`Rat:/Events/RatingEvents.cs`); 11 operation-event contracts (`Architecture/RatingUseCaseEventContracts.cs:5-15`).

## Headline findings

1. **Every ratings read 500s at runtime — the #1278 E9 class** — `EntityBase.IsDeleted` is a get-only computed member (`DeletedAt.HasValue`, `SK:/Entities/EntityBase.cs:40`) with no column mapping; the module's global query filters (`Rat:/Configuration/RatingEntityConfiguration.cs:32, 57`) and explicit `!r.IsDeleted` predicates cannot translate to SQL → "unmapped member" → 500 on live reads. 19 route/principal combos sit in the E9 baseline (`apps/api/tests/GameGuild.API.UnitTests/Architecture/wiring-gap-baseline.txt:157-192, 235`). The unit-tested service layer passes because EF InMemory providers evaluate filters client-side (R1).
2. **`EntityType` is unvalidated free text** — any authenticated user can rate any (Guid, string) pair: `"Course"`, `"Post"`, or `"Foobar"`; nothing verifies the target exists or matches an allowlist (`Rat:/Commands/RatingEndpointCommands.cs:5-10` + service layer); `rating_summaries` fills with phantom entities and `GET top/{entityType}` surfaces them (R2, STRIDE family 5).
3. **`RatingModerationStatus.Pending` is unreachable** — every rating is born `Approved` (`Entities/Rating.cs:84`); the moderation queue therefore only ever sees auto-`Flagged` rows (`Services/RatingModerationService.cs:110-122`); a "pre-moderation" mode does not exist despite the enum and queue shape suggesting it (R4).
4. **Report bombing auto-hides reviews** — `ReportAsync` bumps a counter with **no reporter row, no bound, no identity**; the 3rd report flips the review to `Flagged` and admin `Reject` (or in-flight flag) removes it from all Approved-only reads (`Rat:/Services/RatingModerationService.cs:125-152`; cross-ref trustsafety T6) (R3).
5. **Summary recomputation is O(all ratings for entity) per mutation, in-line** — every rate/update/delete/helpful-adjacent mutation reloads the full set (`Rat:/Services/RatingQueryService.cs:120-146`); also `RateAsync`'s recalculate call happens **after** the save without a transaction, so a crash between leaves the summary stale until the next mutation or the admin recalculate route (R5).
