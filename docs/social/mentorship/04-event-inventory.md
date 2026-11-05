# Social.Mentorship — Event inventory

> Module `GameGuild.Social.Mentorship` · develop `ce31d5850c`. Shorthand `Men:`.

## 1. Named domain events — none declared

The module declares **zero** domain event records (no `Events/` file — file census in the README). Contrast the Social.Posts 12-orphan class (`docs/social-platform/05-gaps.md` N2): mentorship chose to declare nothing rather than declare-dead.

## 2. Durable operation events (the only contract)

`Men:/Architecture/UseCaseEventContracts.g.cs:1-8` — all 7 commands bound to generic durable operation events with `NoDomainEventReason = "No named cross-module event is declared; the durable generic operation event remains the observable contract."`:

| Command | Operation event |
|---|---|
| `UpsertMentorProfileCommand` | `social.mentorship.upsert-mentor-profile` |
| `SetMentorProfileStatusCommand` | `social.mentorship.set-mentor-profile-status` |
| `PublishMentorAvailabilityCommand` | `social.mentorship.publish-mentor-availability` |
| `RemoveMentorAvailabilitySlotCommand` | `social.mentorship.remove-mentor-availability-slot` |
| `BookMentorshipSessionCommand` | `social.mentorship.book-mentorship-session` |
| `CancelMentorshipSessionCommand` | `social.mentorship.cancel-mentorship-session` |
| `CompleteMentorshipSessionCommand` | `social.mentorship.complete-mentorship-session` |

Queries (6) emit nothing.

## 3. The notification consequence (M2)

No `INotificationService` call exists in the module (grep — zero). Combined with §1: **a booking, cancellation, or completion produces no signal to the other participant**. The dashboard query is pull-only. If notifications are wanted, the operation events of §2 are the natural join point for a future subscriber — the platform outbox already carries them, so a Notifications-side `IIntegrationEventHandler` on the operation stream (or a named event upgrade) needs no mentorship changes.

## 4. Inbound events — none

No `IIntegrationEventHandler` / `IDomainEventHandler` implementations in the module. The #1159 creator-process linkage is product-level only (05 §4).
