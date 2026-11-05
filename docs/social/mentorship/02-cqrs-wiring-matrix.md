# Social.Mentorship — CQRS / wiring matrix

> Module `GameGuild.Social.Mentorship` · develop `ce31d5850c`. Shorthand `Men:`, `API:`.

## 0. DI wiring

| Piece | Registration | Evidence | Live? |
|---|---|---|---|
| Module DI | `AddMentorshipModule` — deliberately empty (handlers via host scan; comment says so) | `Men:/MentorshipModuleRegistration.cs:16-24` | **yes** (called `API:/Core/Setup/ApiProductComposition.cs:185`) |
| Module descriptor | `MentorshipSocialModule : ModuleBase`, order 165 | `Men:/MentorshipModuleRegistration.cs:28-32` | yes |
| Handlers (7 command + 6 query) | module scan — `"Social.Mentorship"` in `EnabledModules` | `API:/Core/Setup/ApiProductComposition.cs:77`; `API:/Core/Setup/ApplicationLayerExtensions.cs:110-145` | **yes** |
| Tables ×4 | `MentorshipModelConfiguration : IModelConfiguration` | `Men:/MentorshipModelConfiguration.cs:8-…`; migration `20261101152304_AddSocialMentorshipModule` (#1159) | **yes** |
| Anonymous allow-list | 3 directory actions registered | `API:/Security/AnonymousEndpointRegistry.cs:181-185` | **yes** |
| G-baseline / E9 baseline | no entries | baseline grep | **clean** |

**Rate limit legend** — none on any controller (no `EnableRateLimiting`).

## 1. Live HTTP surface (13 routes)

### MentorProfileController (`v{version}/mentorship/profile`, `[Authorize]` — `Men:/Controllers/MentorProfileController.cs:20-27`)

| Route | Request | Handler | Auth | HOLE? |
|---|---|---|---|---|
| GET `` | `GetMyMentorProfileQuery` | `Queries/MentorshipQueries.cs:168-185` | self | 404 when none |
| PUT `` | `UpsertMentorProfileCommand` | `Commands/MentorshipCommands.cs:23-117` | self | no FluentValidation (entity-level guards only); Role freely self-assigned (M4) |
| POST `status` | `SetMentorProfileStatusCommand` | `:122-167` | self | Draft/Published/Paused only — invalid enum → 500 (`ArgumentOutOfRangeException`, `:129-142`) |
| POST `availability` | `PublishMentorAvailabilityCommand` | `:175-245` | self | strong inline guards |
| DELETE `availability/{slotId}` | `RemoveMentorAvailabilitySlotCommand` | `:249-272` | self | 404-vs-204 by bool |
| GET `dashboard` | `GetMentorDashboardQuery` | `Queries/MentorshipQueries.cs:233-286` | self | |

### MentorsController (`v{version}/mentorship/mentors`, `[AllowAnonymous]` ×3 — `Men:/Controllers/MentorsController.cs:16-58`)

| Route | Request | Handler | HOLE? |
|---|---|---|---|
| GET `` | `ListMentorsQuery` | `Queries:20-88` | search contains — fine; take clamped |
| GET `{profileId}` | `GetMentorQuery` | `:91-130` | unpublished 404 even for owner (documented) |
| GET `{profileId}/availability` | `ListMentorAvailabilityQuery` | `:134-160` | Open-only default; `IncludeBooked` param unreachable from this controller |

### MentorshipSessionsController (`v{version}/mentorship/sessions`, `[Authorize]` — `Men:/Controllers/MentorshipSessionsController.cs:17-82`)

| Route | Request | Handler | HOLE? |
|---|---|---|---|
| GET `` | `ListMyMentorshipSessionsQuery` | `Queries:164-207` | cap 200 |
| POST `` | `BookMentorshipSessionCommand` | `Commands:284-382` | **booking race (M1)**; self-booking blocked |
| POST `{sessionId}/cancel` | `CancelMentorshipSessionCommand` | `:387-434` | participant check; releases slot |
| POST `{sessionId}/complete` | `CompleteMentorshipSessionCommand` | `:438-456` | mentor-only |

## 2. Request-type census

| Category | Count | With handler | Without handler |
|---|---|---|---|
| Commands (`Men:/Commands/MentorshipCommands.cs`) | 7 | 7 | 0 |
| Queries (`Men:/Queries/MentorshipQueries.cs`) | 6 | 6 | 0 |
| **Total** | **13** | **13** | **0 dead request types** |

## 3. Dead-surface reconciliation (the G7-class check)

| # | Surface | Meant to do | Deepest implementation | Verdict |
|---|---|---|---|---|
| S1 | `MentorshipSession.AssignMeetingDetails` (`Entities/MentorshipEntities.cs:404-409`) | attach Discord/Calendar join URL after booking | entity method only — **zero callers** (route/handler/worker grep) | **hole** (M3) — `MeetingUrl` column is write-orphaned; meetings carry a channel label and no link |
| S2 | `ListMentorAvailabilityQuery.IncludeBooked` (`Queries:112-160`) | expose booked slots too | parameter exists; the only caller (public controller) never sets it | planned-redundant (dead parameter) |
| S3 | `GetMentorQuery.ViewerUserId` (`Queries:92-101`) | owner-preview of unpublished profile | parameter exists; controller passes default null | planned-redundant |
| S4 | Multi-advisee sessions | issue #102 marked optional | not modeled (session is 1 mentor + 1 mentee) | **not-built** (explicitly out of scope per #1159) |
| S5 | Calendar/Discord invite dispatch | meeting channel field promises integrations | enum only (`Entities:46-51`); #1159 notes "external invite dispatch left as follow-up infrastructure" | **not-built** (declared) |

No Finance.Ledgers-class dead request types; the module ships fully wired — deadness is confined to S1-S3 and declared non-goals S4-S5.

## 4. Baseline verdicts

- **G-baselines**: clean.
- **E9 route smoke**: no mentorship entries — the inline-`RequestValidationException` error path maps to 400-family via the host's exception handling, and anonymous routes serve empty-200; nothing 500s on the synthetic principal.
