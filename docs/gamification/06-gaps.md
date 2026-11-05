# Gamification.Achievements — Gaps

> Module `GameGuild.Gamification.Achievements` · develop `ce31d5850c`. KNOWN = already tracked in an issue/baseline; NEW = first recorded here.

| # | Severity | Gap | Evidence | Status |
|---|---|---|---|---|
| G1 | high | **The progress/auto-award engine has no caller** — `UpdateProgressAsync` (+ `AchievementProgress`, level tiers, prerequisite gates) is fully implemented and unreachable; the module can only grant badges by manual admin award | `Ach:/Services/AchievementService.cs:76-128`; grep `UpdateProgressAsync` over `Source/` → only interface + impl | NEW |
| G2 | high | **`achievements:create/update/delete/award` permission keys are never seeded or granted** — the 4 admin mutations 403 for every caller until out-of-band grants exist; combined with G1 the module has **no functioning write path at all** | `Ach:/AchievementsPermission.cs:6-11`; `Authz:/Handlers/ResourcePermissionAuthorizationFilter.cs:527-536`; grep over `PolicyDefinitionSeeder.cs` → nothing | NEW |
| G3 | medium | **Secret achievements leak** — by-id read (anonymous) has no `IsSecret` filter, and the eligible list loads secrets alongside normal definitions; only the catalog list honors `includeSecrets:false` | `Ach:/Services/AchievementService.cs:235-241` vs `:243-274`; eligible `:134-137`; `Controllers/AchievementsController.cs:183-195` | NEW |
| G4 | medium | **8 domain events never constructed** — the declared cross-module surface is fiction (posts-N2 class); the G4 event-producer guard does not scan this declaration shape | `Ach:/Events/AchievementEvents.cs:11-94` — zero construction sites | NEW (class known repo-wide) |
| G5 | medium | **`mark-notified` has no ownership check** — any authenticated user can ack any `userAchievementId`; also unnotified award ids are sequential GUIDs but unauthenticated enumeration is blocked by `[Authorize]` | `Ach:/Controllers/AchievementsController.cs:122-135`; `Commands/AchievementCommands.cs:40-45` | NEW |
| G6 | low | **E9 route-smoke failures (3)** — `DELETE /{id}`, `POST /{id}/award`, `POST /my/{id}/mark-notified` return 500 on the synthetic principal (raw `result.Error` → `StatusCode(500, ...)` shape) | `apps/api/tests/GameGuild.API.UnitTests/Architecture/wiring-gap-baseline.txt:156,223,224` | KNOWN (#1278 E9 backlog) |
| G7 | low | **No request validators** — Name/Points/Category arrive unvalidated at the API edge (entity clamps Points ≥ 0 only on update path); no FluentValidation in module | `Ach:/Commands/AchievementCommands.cs` (no validators); `Controllers:202-223` | NEW |
| G8 | low | **Double-award race on non-repeatables** — check-then-insert without transaction; `(UserId, AchievementId)` index deliberately non-unique | `Ach:/Services/AchievementService.cs:40-49`; `Entities/UserAchievement.cs:17` | NEW (also STRIDE 2.2) |
| G9 | low | **Definition delete hard-cascades user history** — deleting an achievement erases every user's award rows for it; no soft-delete, no guard | `Ach:/Configuration/AchievementEntityConfiguration.cs:45-48`; `Services/AchievementService.cs:341-363` | NEW |
| G10 | info | **`NotificationType.AchievementUnlocked` preference machinery guards zero traffic** — the Achievements category toggle and mute path in Notifications can never fire | `Notif:/NotificationCategories.cs:41-44`; `Notif:/Services/NotificationPreferenceService.cs:210`; producer grep → 0 | NEW |
| G11 | info | **Eligible-list N+1** — per-candidate prerequisite check re-loads the user's full award list | `Ach:/Services/AchievementService.cs:146-156, 370-372` | NEW |
| — | info | Historical: tables never materialized pre-#1169/#1158 (module referenced-nowhere) — **fixed**; baseline entries removed | `wiring-gap-baseline.txt:23`; migration `20261102105545_AddLedgersRatingsAndAchievementsTables` | KNOWN (fixed) |

## Recommended acceptance order

1. Decide the product truth (G1): either wire one producer (e.g. Posts/Learning event → `UpdateProgressAsync`) or delete the progress surface and the dead events (G4) — a half-wired gamification module invites exactly the Ledger-class audit confusion.
2. Seed + grant the `achievements:*` permission keys (G2) or the admin surface stays decorative.
3. Close the two secret leaks (G3) — single-line filters in `GetAchievementByIdAsync` (reject `IsSecret` for non-admin) and `GetEligibleAchievementsAsync` (exclude secrets).
4. Add the ownership check to `mark-notified` (G5) and a unique partial index for non-repeatable awards (G8).
