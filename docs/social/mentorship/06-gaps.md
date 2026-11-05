# Social.Mentorship — Gaps

> Module `GameGuild.Social.Mentorship` · develop `ce31d5850c`. KNOWN vs NEW per repo convention.

| # | Severity | Gap | Evidence | Status |
|---|---|---|---|---|
| M1 | high | **Booking race double-books a slot** — Open-check → session insert → slot.Book all in memory, no transaction, no concurrency token, no unique index on `SessionId`; concurrent requests produce two sessions against one slot (one orphaned) | `Men:/Commands/MentorshipCommands.cs:289-365`; `Entities/MentorshipEntities.cs:231-294` (no Version token) | NEW |
| M2 | medium | **No notifications on booking/cancel/complete** — zero domain events, zero `INotificationService` calls; counterparties must poll | `Men:/Architecture/UseCaseEventContracts.g.cs` (operation events only); module grep | NEW (04 §3) |
| M3 | medium | **`AssignMeetingDetails` dead + meeting links unreachable** — `MeetingUrl` column write-orphaned; sessions carry a channel label and no join URL (calendar/Discord dispatch declared follow-up in #1159) | `Men:/Entities/MentorshipEntities.cs:404-409` — zero callers | NEW (02 §3 S1) |
| M4 | medium | **Matchmaker is a self-service privilege** — booking-for-others only checks "actor has a published profile with Role=Matchmaker"; anyone can upsert+publish that role; enables booking sessions on behalf of arbitrary advisee ids | `Men:/Commands/MentorshipCommands.cs:370-381`; `Controllers/MentorProfileController.cs:33-55` (no role gate) | NEW (07 family 2) |
| M5 | low | **No rate limiting anywhere in the module** — anonymous directory scraping + availability spam are unthrottled at the API layer | no `EnableRateLimiting` on the 3 controllers | NEW |
| M6 | low | **Invalid profile-status enum → 500** — `ArgumentOutOfRangeException` default branch instead of 400 | `Men:/Commands/MentorshipCommands.cs:129-142` | NEW |
| M7 | low | **No-show / reschedule absent** — session lifecycle is Booked→Cancelled/Completed only; expired-unattended sessions stay Booked forever (dashboard "upcoming" filters `StartsAt >= now`, so they silently age out of view but persist as Booked) | `Entities/MentorshipEntities.cs:39-44`; `Queries/MentorshipQueries.cs:247-252` | NEW (product decision needed) |
| M8 | info | **Dead query parameters** — `ListMentorAvailabilityQuery.IncludeBooked` and `GetMentorQuery.ViewerUserId` have no route-level setter | `Men:/Queries/MentorshipQueries.cs:92, 112` | NEW (02 §3 S2/S3) |
| M9 | info | **Tenant column is provenance, not isolation** — no tenant predicate on any query; directory is global | `Men:/Queries/MentorshipQueries.cs` (all queries); `05 §5` | NEW (documented) |
| — | info | Multi-advisee sessions + external calendar/Discord dispatch — **explicitly out of scope** in #1159 | PR #1159 notes | KNOWN (declared) |

## Recommended acceptance order

1. M1 — smallest correct fix: unique index on `MentorAvailabilitySlots.SessionId` (nullable-unique) + `Version` concurrency token, or wrap booking in a serializable transaction; then map the guard failure to 409.
2. M2 — subscribe Notifications to `social.mentorship.*` operation events (or upgrade to named events) — the rail already exists.
3. M4 — gate the Matchmaker role behind an admin grant or an approval state before `Publish` honors it.
4. M6/M8 — error-shape hygiene; M5 — rate limits on the anonymous trio before any public launch.
