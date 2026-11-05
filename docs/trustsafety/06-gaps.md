# TrustSafety — Gaps

> Module `GameGuild.TrustSafety` · develop `ce31d5850c`. KNOWN = tracked in an issue/baseline; NEW = first recorded here.

| # | Severity | Gap | Evidence | Status |
|---|---|---|---|---|
| T1 | high | **Both control-plane write surfaces are caller-less** — `IngestAsync` (evidence) and `SubmitAppealAsync` (appeals) invoked nowhere; the risk input source permanently answers `Unavailable` and the appeals console manages an empty table | `TS:/PostgreSqlTrustSafetyControlPlane.cs:80-187, 189-209`; grep `Source/`+`tests/` → 0 callers | NEW |
| T2 | high | **Zero text moderation platform-wide** (#1250 N1) — no text checks in TrustSafety, Social, Ratings, or Courses; only media is classified; user text persists unfiltered | `docs/social-platform/05-gaps.md:17-18`; `API:/Security/LitellmModerationClassifier.cs` (image-only prompt); module census | KNOWN (#1250 N1, product decision pending) |
| T3 | high | **Appeals routes are `[Authorize]`-only** — no `Policies.Admin`, no tenant permission; any authenticated user can assign/decide appeals (data-empty today, but the gate must precede the first real appeal) | `API:/Core/Controllers/EconomyComplianceAdministrationController.cs:36-38, 168-235` | NEW |
| T4 | medium | **`UnresolvedModerationClassifier` default-approves** — the module fallback returns `IsApproved=true` for anything it classifies; safe only while the host always binds the LiteLLM client; a test/focused host silently approves uploads | `Assets:/Moderation/UnresolvedModerationClassifier.cs:8-13` | NEW |
| T5 | medium | **Moderation-off is a silent freeze** (N13) — worker exits when `AutoModerationOptions.Enabled=false`; uploads accumulate `Pending`, queue never drains, nothing alerts | `Assets:/BackgroundServices/ModerationClassificationWorker.cs:17-21`; `docs/social-platform/05-gaps.md:53-54` | KNOWN (#1250 family) |
| T6 | medium | **Ratings report has no reporter identity or bound** — `ReportAsync` increments a counter with no per-reporter row; auto-flag at 3 is bombable; `Reason` only logged | `Ratings:/Services/RatingModerationService.cs:125-152` | NEW (cross-ref docs/social/ratings/ R6) |
| T7 | low | **`DisabledModules` entry is decorative** — nothing enforces it for this module (no handlers to skip); readers reasonably believe the module is off when its services and console are live | `API:/Core/Setup/ApiProductComposition.cs:113-116`; `EconomyCapabilityCompositionExtensions.cs:23` | NEW (naming/intent hazard) |
| T8 | low | **Deferred inbox rows have no re-driver** — out-of-order events wait forever unless the producer re-sends; no sweeper exists | `TS:/PostgreSqlTrustSafetyControlPlane.cs:132-137` (no re-drive path) | NEW (moot until T1) |
| T9 | info | **`TrustSafetyAppealState`/`TrustSafetyOutcome` surfaced raw by the console** — internal enum ints in query params and DTOs; fine for an ops surface, undocumented for external clients | `EconomyComplianceAdministrationController.cs:168-235` | NEW |
| T10 | info | **No E9/G-baseline entries** — the host-carried routes smoke clean (take/route shapes valid); the module itself is invisible to the guards (no controllers/requests) | `wiring-gap-baseline.txt` grep — only ratings/notifications/financial-crime entries | info |

## Recommended acceptance order

1. T1 is the fork in the road: either (a) wire the first producer — the Assets moderation verdicts and the Ratings flag outcomes are the natural `Moderation`/`Report`-kind emitters — or (b) mark the module `planned` in the tracker and stop carrying its console. The current half-state (live read path, dead write path) is the worst of both.
2. T3 — add `Policies.Admin` (or a compliance-officer policy) to the appeals routes before any producer lands (one-line change per route).
3. T2 — record the product decision (#1250 note 3) in this package when made: chosen plane, scope (posts/comments/reviews), and whether verdicts flow into the T1 evidence rail.
4. T4/T5 — flip the unresolved classifier to NeedsReview-on-unresolved and add a queue-depth alert; both are small and close real auto-approval/freezes.
