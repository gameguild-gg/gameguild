# TrustSafety — STRIDE threat model

> Module `GameGuild.TrustSafety` + the content-safety surfaces it is conflated with · develop `ce31d5850c`. Families match 01/08.

## Family 1 — Evidence ingestion (designed; threats evaluated against the implementation that exists)

| Threat | Story | Mitigations | Verdict |
|---|---|---|---|
| **S**poofing — evidence forgery | fabricate a Clear verdict for a fraudulent subject | mandatory `KeyId`+`Signature` over the canonical payload, verified via the economy capability-policy verifier (`TS:/PostgreSqlTrustSafetyControlPlane.cs:118-127`); invalid → inbox row Rejected (kept as evidence of attempt) | pass (fail-closed) |
| **T**ampering — replay with different payload | re-send an EventId with a friendlier outcome | payload-hash compare → `TrustSafetyConflictException` (`:92-95`); (tenant, subject, version) unique | pass |
| **R**epudiation | "that verdict was never issued" | inbox rows retain signature + hashes + processed timestamps; evidence envelope versioned | pass |
| **I**nformation disclosure | subject identity leakage | subjects are hashes only (`SubjectHash` ≤128); raw reference capped 2048 opaque | pass |
| **D**oS | flood ingest to exhaust serializable transactions | **no rate limiter on the (nonexistent) ingest caller** — moot until T1 but must ship with the first producer | open |
| **E**levation | a producer self-clears by emitting Approved vN+1 | version monotonicity + policy signature (keyed) + hold release only on Approved **of the next version** (`:132-144`) | pass assuming key hygiene |

## Family 2 — Appeal workflow (the live console)

| Threat | Story | Mitigations | Verdict |
|---|---|---|---|
| **S**poofing / **E**levation | non-staff assign/decide appeals | **none beyond `[Authorize]`** — T3; empty table is the only thing exploited-today-proofing it | **fail** (gate must precede first data) |
| **T**ampering | decide without assignment, or re-decide | state+assignee+version triple check (`:237-243`); DB constraint couples decision fields | pass |
| **R**epudiation | reviewer denies the decision | `DecidedBy`, `DecidedAt`, `ReasonCode`, `DecisionEvidenceHash` mandatory (`:234-247`) | pass |
| **I**nformation disclosure | cross-tenant appeal reads | tenant-scoped queries from actor context (`EconomyComplianceAdministrationController.cs:170-178`) | pass |
| **D**oS | take capped 1-500 (`TS:/PostgreSqlTrustSafetyControlPlane.cs:268-270`) | pass |

## Family 3 — Report lifecycles (user-facing abuse-report surfaces)

| Threat | Story | Mitigations | Verdict |
|---|---|---|---|
| **S**poofing — **report-system abuse** | report-bomb a critic's review into hiding | ratings auto-flag at 3 (`Ratings:/Services/RatingModerationService.cs:145-151`) with **no reporter identity, no bound, admin-only restore** | **fail** (T6) — classic brigading lever |
| **T**ampering | alter a filed report | reports immutable after creation; review decision appends | pass |
| **R**epudiation | "nobody reviewed my report" | reviewer id + notes on review (`Assets:/Commands/ReviewReportCommand.cs:11-14`) | pass (assets lane) |
| **I**nformation disclosure | reporter identity exposure | assets keeps `ReportedByUserId`; expose-paths need checking when the queue API goes external | open |
| **D**oS | mass-report floods | no rate limits on report endpoints (assets/ratings) | weak |
| **E**levation | none (reporters have no decision power) | decisions admin-gated | pass |

## Family 4 — Auto-moderation lane (**moderation bypass** surface)

| Threat | Story | Mitigations | Verdict |
|---|---|---|---|
| **S**poofing | none (no identity in the lane) | — | n/a |
| **T**ampering — **classification gaming** | craft an image the vision model scores safe (adversarial), or smuggle content in frames of a whitelisted-looking MIME | thresholds 0.80/0.75 dual-sided; human-review MIME list (gif/mp4/webm); parse-failure → NeedsReview never approve (`API:/Security/LitellmModerationClassifier.cs:119-161`) | partial — adversarial robustness is model-dependent, accept + monitor |
| **R**epudiation | "we approved it" trail | classification result stored (labels + model) on the asset | pass |
| **I**nformation disclosure | image exfiltration via the proxy | single purpose-bound proxy call, key from config; images stay internal | pass |
| **D**oS | burn the LLM lane | timeout 120s, failure cap 3 → NeedsReview (`Assets:/Moderation/ModerationService.cs:35-44`) | pass |
| **E**levation — **bypass via composition** | `UnresolvedModerationClassifier` returns approved for everything when the real binder is absent | TryAdd-scoped fallback **defaults to approval** (`Assets:/Moderation/UnresolvedModerationClassifier.cs:8-13`) | **fail** (T4) — fallback must fail-closed |

## Family 5 — Economy enforcement coupling (the stakes of the missing producer)

| Threat | Story | Mitigations | Verdict |
|---|---|---|---|
| **T**ampering | abuse verdicts silently fail to block payouts | today NO verdicts flow (T1) — `Unavailable` outcome; economy policy defines the meaning of Unavailable (permissive or strict is economy's contract, see docs/economy) | **structural gap** — enforcement depends on the dead producer |
| **D**oS | hold-thrash a competitor (activate/release churn) | hold activation needs a signed event (family 1); system-actor releases are event-driven only | pass-if-wired |
| **E**levation | release own hold via console | no hold-release route exists anywhere in this module (holds released only by Approved events / financial-crime console which is policy-gated) | pass |

## Aggregate verdict

The control plane's *innards* are strong (fail-closed signature gate, replay/version defenses, hash-only subjects, constraint-backed lifecycle). Its *perimeter* is where the risk lives: an unauthenticated-by-policy console (T3), a producer-less rail whose absence turns all enforcement off (T1), and the adjacent moderation lane's fail-open fallback (T4) and bombable report counters (T6). None of these can be exploited for money movement today precisely because the rail is inert — which is also why fixing T1 without first fixing T3/T4 would be the wrong order.
