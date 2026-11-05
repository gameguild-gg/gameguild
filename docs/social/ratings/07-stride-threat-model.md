# Social.Ratings — STRIDE threat model

> Module `GameGuild.Social.Ratings` · develop `ce31d5850c`. Families match 01/08.

## Family 1 — Rate an entity (reputation forgery)

| Threat | Story | Mitigations | Verdict |
|---|---|---|---|
| **S**poofing | rate as another user | actor id from ActorContext (`Rat:/Services/RatingCrudService.cs:22`) | pass |
| **T**ampering — **rating manipulation** | update own rating repeatedly to whatever the wind says | one-row-per-user rate-or-update is by design; no cooldown on updates | accepted (product) |
| **R**epudiation | "I never rated that" | `CreatedAt`/`EditedAt` on the row; operation event `social.ratings.rate` | adequate |
| **I**nformation disclosure | see others' private votes | helpful-vote rows never exposed; ratings themselves are public by design | pass |
| **D**oS | mass-create phantom-entity ratings | unvalidated EntityType = unbounded row creation with 3 summary tables to pollute (R2) | **fail** |
| **E**levation | self-verify (`IsVerified`) | `MarkAsVerified` has no caller — cannot be invoked through the API at all | pass-by-deadness (R7) |

## Family 2 — Helpful votes (inflation)

| Threat | Story | Mitigations | Verdict |
|---|---|---|---|
| **S**poofing | vote as someone else | actor-scoped | pass |
| **T**ampering — **self-promotion** | author helpful-votes own review | blocked (`CannotVoteOwnRating`, `Rat:/Services/RatingModerationService.cs:26-31`) | pass |
| **R**epudiation | audit of counter drift | vote rows persist; counter is denormalized — a skew is detectable by recount | weak (no reconciliation job) |
| **I** | — | votes not exposed | pass |
| **D**oS — **sybil helpful-bombing** | many accounts helpful-vote one review to pin it atop sorts | unique (rating,user) index is per-account only — no velocity or graph checks; HelpfulCount drives nothing sensitive yet | accepted risk (nothing consumes the count — decoration today) |
| **E** | — | — | n/a |

## Family 3 — Reports (abuse of the report system)

| Threat | Story | Mitigations | Verdict |
|---|---|---|---|
| **S**poofing | — (reporter not even recorded) | — | n/a |
| **T**ampering — **brigading** | 3 reports from one actor auto-flag → review hidden | no identity, no bound, threshold 3 (`RatingModerationService.cs:148-151`) | **fail** (R3) |
| **R**epudiation | "who reported me" | nothing persisted (log line only) | fail-by-design (privacy choice, but blocks anti-abuse too) |
| **I**nformation disclosure | reason text | logged only | pass |
| **D**oS | report-flood any review | same as T | fail (R3) |
| **E**levation | report → delete without admin | flag hides from public reads (soft effect); delete requires TenantAdmin | partial |

## Family 4 — Admin moderation

| Threat | Story | Mitigations | Verdict |
|---|---|---|---|
| **S**/E | non-admin moderates | `TenantAdmin` policy on 5 routes (`Rat:/Controllers/RatingsController.cs:309-377`) | pass |
| **T**ampering | moderator bias — reject all negative reviews of a tenant's product | no dual-control, no audit event (dead `RatingModerationStatusChangedEvent`); operation events exist per command | weak (audit gap R8) |
| **R**epudiation | "nobody decided this" | status changes + `UpdatedAt` only — no who/when on the decision | **fail** (no DecidedBy field) |
| **I** | queue exposes reporters? | no reporter data exists to leak | pass |
| **D**oS | recalculate spam | admin-gated | pass |

## Family 5 — Aggregates and the phantom-entity surface

| Threat | Story | Mitigations | Verdict |
|---|---|---|---|
| **S**poofing — **phantom-entity ratings** | rate a nonexistent "Course" id to launder reputation or squat summaries | no target validation (R2) | **fail** |
| **T**ampering — **summary skew** | keep mutation+recalc separated (crash) or exploit count-vs-summary mismatch to display inconsistent numbers | synchronous recalc mostly; no transaction (R5), count mismatch (R6) | weak |
| **R**epudiation | aggregates reproducible from rows | deterministic recompute route (admin) | pass |
| **I**nformation disclosure | `GET user/{userId}` anonymous full history (R9) | none | fail (product call needed) |
| **D**oS | top/recent unthrottled anonymous reads | none | weak (R1 makes them 500 today — incidental protection) |
| **E**levation | — | — | n/a |

## Aggregate verdict

The module's authorization is correct but its **integrity model is soft on three fronts**: unvalidated targets (family 5 S), bombable reports (family 3), and moderation without decision provenance (family 4 R). The E9 outage (R1) currently masks most of this — nothing works against a real database — which makes R1 the prerequisite both for product use and for meaningful penetration testing of these families.
