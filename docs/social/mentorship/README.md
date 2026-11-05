# GameGuild.Social.Mentorship — Documentation Package

> Module `GameGuild.Social.Mentorship` · Documented against `develop` commit `ce31d5850c` (merge of #1311).
> Scope: the mentorship program delivered by PR #1159 for issue #102 — mentor/advisor/matchmaker profiles with the issue-required free-contribution rule, published availability slots, the booking lifecycle (book/cancel/complete with matchmaker-mode guards), the public mentor directory, and the mentor dashboard. Includes the **#1159 creator-process relationship**: co-delivered with the content-creator pipeline (#105), product-linked (creators grow into mentors/advisors; matchmakers onboard them) but with **zero code-level coupling** — documented as an explicit boundary contract.

## Document inventory

| File | Contents |
|------|----------|
| [`01-user-flows.md`](01-user-flows.md) | Mermaid `flowchart TD` flows — profile upsert/publish/pause, availability publish/remove, advisee booking (incl. matchmaker-only schedules + introductory calls), cancel/complete, public directory browse, dashboard — each with edge-case matrices and file:line |
| [`02-cqrs-wiring-matrix.md`](02-cqrs-wiring-matrix.md) | DI wiring + route → command/query → handler → auth matrix for the **13 live routes across 3 controllers**, anonymous-route registry entries, and the dead-surface reconciliation (`AssignMeetingDetails`, multi-advisee, calendar integrations) |
| [`03-state-machines.md`](03-state-machines.md) | `stateDiagram-v2` — profile (Draft/Published/Paused), availability slot (Open/Booked/Cancelled with Release), session (Booked/Cancelled/Completed) — with the real transition guards |
| [`04-event-inventory.md`](04-event-inventory.md) | Zero named domain events; 7 generic durable operation events (`social.mentorship.*`); the no-notification consequence (bookings never notify the other side) |
| [`05-cross-module-sequences.md`](05-cross-module-sequences.md) | `sequenceDiagram`s — booking end-to-end, matchmaker-mediated booking, cancel-with-slot-release, dashboard aggregation; the #1159 creator-process boundary contract |
| [`06-gaps.md`](06-gaps.md) | KNOWN vs NEW — headline M1: booking race can double-book a slot (no concurrency token); M2: no notifications on booking/cancel; M3: `AssignMeetingDetails` dead (meeting URLs unreachable) |
| [`07-stride-threat-model.md`](07-stride-threat-model.md) | STRIDE per family — booking forgery/slot squatting, availability spam, directory scraping, matchmaker privilege abuse, calendar-link injection |
| [`08-dfds.md`](08-dfds.md) | L0 context + L1 per family with trust boundaries (anonymous directory vs authenticated participants → API → mentorship schema) |
| [`09-security-architecture.md`](09-security-architecture.md) | Component view, permission map (self-service + anonymous registry), integrity properties (booking-slot invariant and its race caveat), deployment invariants |

## Conventions

- Follows `docs/auth/flows/*` / `docs/finance-ledgers/*` conventions; Mermaid labels without parentheses/colons; `id-colon-action` spelling for custom route actions.
- Citation shorthand: `Men:` = `apps/api/Source/Modules/GameGuild.Social.Mentorship/`, `API:` = `apps/api/Source/GameGuild.API/`.
- Tests: `apps/api/tests/GameGuild.Social.Mentorship.UnitTests/MentorshipTests.cs` (12 tests per #1159 — entity lifecycle rules + endpoint authorization surface). Local `dotnet test` blocked (INC-PG hold).

## Module shape at a glance

- **10 files, ~1.7k LOC**: 3 controllers (`MentorProfileController` self-service 6 routes, `MentorsController` public 3 routes, `MentorshipSessionsController` 4 routes — all under `v{version}/mentorship/*`, no `api/` prefix), 7 commands + 6 queries **all with handlers** (co-located in `Men:/Commands/MentorshipCommands.cs` and `Men:/Queries/MentorshipQueries.cs`), 4 entities, mappers/DTOs, `MentorshipModelConfiguration : IModelConfiguration` (`Men:/MentorshipModelConfiguration.cs:8`).
- **Tables (4)**: `MentorProfiles`, `MentorSkills`, `MentorAvailabilitySlots`, `MentorshipSessions` — migration `20261101152304_AddSocialMentorshipModule` (#1159), clean snapshot verified in the PR.
- **Wiring**: `"Social.Mentorship"` in `EnabledModules` (`API:/Core/Setup/ApiProductComposition.cs:77`); `AddMentorshipModule` is a no-op by design (handlers discovered by the host CQRS scan; module descriptor `MentorshipSocialModule : ModuleBase` order 165, `Men:/MentorshipModuleRegistration.cs:9-31`) and **is** called (`ApiProductComposition.cs:185`).
- **Auth**: everything self-service is `[Authorize]` + actor-scoped (`GetRequiredUserId` throws on missing subject); the 3 directory routes are `[AllowAnonymous]` and registered in the anonymous-endpoint allow-list (`API:/Security/AnonymousEndpointRegistry.cs:181-185`). **No rate limiting; no FluentValidation** — validation is inline `RequestValidationException` in handlers/entities.
- **Domain rules of note** (all issue #102 requirements): free pricing requires a `FreeContributionReason` (`Men:/Entities/MentorshipEntities.cs:162-170`); introductory call clamped to 10-15 minutes (`:172-180`); availability 10min-8h, ≥1h lead time, non-overlapping (commands `Men:/Commands/MentorshipCommands.cs:178-232`); matchmaker-only schedules require a published matchmaker to book (`:314-344`); skills capped at 20 (`:87-93`).

## Headline findings

1. **The booking race can double-book a slot** — the handler loads the slot, checks `Open`, books in memory, saves; two concurrent bookings both read `Open`, both create sessions, the slot ends up pointing at one while the other session dangles (no concurrency token on `MentorAvailabilitySlot`, no unique constraint on `SessionId`, no transaction) (`Men:/Commands/MentorshipCommands.cs:289-365`) (M1).
2. **Bookings never notify anyone** — zero named domain events; 7 operation events only (`Men:/Architecture/UseCaseEventContracts.g.cs:1-8`); no `INotificationService` call anywhere in the module — a mentor learns of a booking only by polling the dashboard (M2).
3. **`AssignMeetingDetails` is dead** — the session entity can carry `MeetingChannel` + `MeetingUrl` (`Men:/Entities/MentorshipEntities.cs:404-409`) but no route/handler/worker ever calls it: meetings are scheduled with a channel *field* and no joinable URL (calendar/Discord dispatch was declared follow-up infrastructure in #1159) (M3).
4. **Matchmaker is an honor-system privilege** — booking for another advisee requires "a published matchmaker profile," which is **self-service**: anyone can upsert their own profile with `Role = Matchmaker` and publish it (`Men:/Commands/MentorshipCommands.cs:370-381` checks only that the profile exists and is published — no grant, no approval) (M4, STRIDE family 2).
5. **Public directory by-id leaks nothing unpublished, with one nuance** — `GetMentorQuery(ProfileId, ViewerUserId?)` would show unpublished profiles to their owner, but the public controller always passes no viewer (`Men:/Controllers/MentorsController.cs:38-46` + `Queries/MentorshipQueries.cs:93-101`) → unpublished profiles 404 even for the owner via that route (owner reads via `/profile` self route) (documented behavior, not a leak).
