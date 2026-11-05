# Gamification.Achievements — CQRS / wiring matrix

> Module `GameGuild.Gamification.Achievements` · develop `ce31d5850c`. Shorthand `Ach:` = module root, `API:` = `apps/api/Source/GameGuild.API/`, `Authz:` = `apps/api/Source/Modules/GameGuild.Identity.Authorization/`.

## 0. DI wiring

| Piece | Registration | Evidence | Live? |
|---|---|---|---|
| `IAchievementService` → `AchievementService` | `AddAchievementsModule` | `Ach:/AchievementsModule.cs:14-21`; called at `API:/Core/Setup/ApiProductComposition.cs:193` (the #1158 fix comment `:189-192`) | **yes** |
| Handlers (5 command handlers) | module scan | `API:/Core/Setup/ApplicationLayerExtensions.cs:110-145`; `"Gamification.Achievements"` in `EnabledModules` (`ApiProductComposition.cs:81`) | **yes** |
| Tables ×5 | `AchievementsModelConfiguration : IModelConfiguration` | `Ach:/Configuration/AchievementEntityConfiguration.cs:139-149` via `API:/Database/ApplicationDbContext.cs:870-883`; migration `20261102105545_AddLedgersRatingsAndAchievementsTables` | **yes** (the #1169 materialization) |
| Wiring-gap baseline | Achievements entries removed when the #1158 gap was fixed | `apps/api/tests/GameGuild.API.UnitTests/Architecture/wiring-gap-baseline.txt:23` ("empty — the #1158 Achievements module gap was fixed") | **clean** |
| Permission keys `achievements:*` | **nothing** — no seeder, no grant anywhere | grep `achievements:create|update|delete|award` over `Source/` → only `Ach:/AchievementsPermission.cs:6-11` | **dead-until-granted** (G2) |

**Auth legend** — class `[Authorize]` (`Ach:/Controllers/AchievementsController.cs:13`); admin mutations additionally `RequirePermission(...)` (`:201,229,261,280`) resolved by the tenant permission engine (`Authz:/Handlers/ResourcePermissionAuthorizationFilter.cs:527-536`). **Rate limit legend** — `—` = none (no `EnableRateLimiting` on the controller).

**Handler census** — 5 commands, 5 handlers, 0 queries (reads go through `IAchievementService` directly, not CQRS). Unlike Finance.Ledgers' 20 dead request types, **no declared request type lacks a handler**; the dead surface is a service method and two entity write paths (see §3).

## 1. Live HTTP surface (10 routes, `api/achievements` — `Ach:/Controllers/AchievementsController.cs`)

| Route | Request | Validator | Handler / service | Auth | Rate limit | Unit tests | HOLE? |
|---|---|---|---|---|---|---|---|
| GET `my` | — (service call) | — | `GetUserAchievementsAsync` (`AchievementService.cs:276`) | `[Authorize]` | — | `Achievements.UnitTests` controller/entity suites | category filter client-side safe |
| GET `my/points` | — | — | `GetUserTotalPointsAsync` (`:298`) | `[Authorize]` | — | same | |
| GET `my/unnotified` | — | — | `GetUnnotifiedAchievementsAsync` (`:190`) | `[Authorize]` | — | same | service failure → 500 raw error (`Controller:97-100`) |
| POST `my/{userAchievementId}/mark-notified` | `MarkAchievementNotifiedCommand` | — | handler `AchievementCommands.cs:40` → `MarkNotifiedAsync` | `[Authorize]` | — | same | **no ownership check — any user can ack any award** (07 §4.3) |
| GET `eligible` | — | — | `GetEligibleAchievementsAsync` (`:130`) | `[Authorize]` | — | same | **leaks secret definitions** (G3); N+1 prerequisite queries |
| GET (catalog) | — | — | `GetAchievementsAsync` (`:243`, `includeSecrets:false`) | `[AllowAnonymous]` | — | same | |
| GET `{achievementId}` | — | — | `GetAchievementByIdAsync` (`:235`) | `[AllowAnonymous]` | — | same | **secret + no tenant filter by id** (G3) |
| POST (create) | `CreateAchievementCommand` | — none | handler `AchievementCommands.cs:47` → `CreateAchievementAsync` | `RequirePermission achievements-colon-create` | — | same | **unseeded key → 403** (G2); no validator; no level/prerequisite authoring |
| PUT `{achievementId}` | `UpdateAchievementCommand` | — | handler `:80` → `UpdateAchievementAsync` | `RequirePermission achievements-colon-update` | — | same | same |
| DELETE `{achievementId}` | `DeleteAchievementCommand` | — | handler `:135` → `DeleteAchievementAsync` | `RequirePermission achievements-colon-delete` | — | same | same; cascade deletes user history (`Configuration/AchievementEntityConfiguration.cs:45-48`) |
| POST `{achievementId}/award` | `AwardAchievementCommand` | — | handler `:142` → `AwardAchievementAsync` | `RequirePermission achievements-colon-award` | — | same | same; E9: 500 on synthetic principal (`wiring-gap-baseline.txt:156,223,224`) |

Use-case event contracts cover exactly the 5 commands (`Ach:/Architecture/UseCaseEventContracts.g.cs:1-5`): all `gamification.achievements.*` with `NoDomainEventReason` "generic durable operation event only".

## 2. Request-type census

| Category | Count | With handler | Without handler |
|---|---|---|---|
| Commands (`Ach:/Commands/AchievementCommands.cs`) | 5 | 5 | 0 |
| Queries | 0 (service-direct reads) | — | — |
| **Total** | **5** | **5** | **0 dead request types** |

## 3. Dead-surface reconciliation (the G7-class check, post-#1169)

The Finance.Ledgers failure class (declared request types without handlers) does **not** repeat here — the module was re-wired in #1158/#1169 and its baseline entries were removed. The dead surface is one layer deeper:

| # | Surface | Meant to do | Deepest implementation present | Verdict |
|---|---|---|---|---|
| S1 | `AchievementService.UpdateProgressAsync` (`Ach:/Services/AchievementService.cs:76-128`) | incremental progress + auto-award (the "gamified" engine) | fully implemented incl. get-or-create + auto-award tail — **zero callers** | **hole** (G1) |
| S2 | `AchievementLevel` write surface | author Bronze/Silver/Gold tiers with per-tier points | entity + config + unique index (`Entities/AchievementRelatedEntities.cs:15-71`) — no create path in any handler | **hole** |
| S3 | `AchievementPrerequisite` write surface | dependency chains between achievements | entity + config (`:74-102`) — rows can only exist via direct DB insert; the prerequisite check (`AchievementService.cs:365-387`) guards a set nothing can build | **hole** |
| S4 | `Achievement.Conditions` jsonb | declarative earn conditions `{"posts_count":10}` | column + doc comment (`Entities/Achievement.cs:64-69`) — never read by any code path | **decoration** |
| S5 | `UserAchievement.Level/Progress/MaxProgress/IncrementProgress/IncrementEarnCount` | level/progress bookkeeping on awards | setters/methods exist (`Entities/UserAchievement.cs:34-44,100-114`); every API award is born `IsCompleted=true, Level=null, Progress=0, MaxProgress=1, EarnCount=1` (`:72-91`) | **write-orphaned** |
| S6 | 8 domain events (`Ach:/Events/AchievementEvents.cs:11-94`) | cross-module triggers (leaderboards, notifications) | declarations only — zero construction sites | **declared-dead** (04 §1) |

Arithmetic: 0 dead request types; 6 dead surfaces (3 holes, 1 decoration, 1 write-orphaned, 1 declared-dead family).

## 4. Baseline verdicts

- **G-baselines (wiring gaps)**: clean — the module's entries were removed after #1158 (`wiring-gap-baseline.txt:23`).
- **E9 (route smoke)**: 3 live routes fail on the synthetic principal (`wiring-gap-baseline.txt:156,223,224`) — `DELETE /{id}`, `POST /{id}/award`, `POST /my/{id}/mark-notified` — all one family: "gamification award/notification mutations 500 on synthetic principal" (the error-path `StatusCode(500, result.Error)` shape; see 06 G6).
