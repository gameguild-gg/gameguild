# Gamification.Achievements — Event inventory

> Module `GameGuild.Gamification.Achievements` · develop `ce31d5850c`. Shorthand `Ach:` = module root.
> The module publishes **zero** durable integration events. Its 8 declared domain events are all declared-dead (never constructed). The only observable contract is the generic durable operation event emitted by the use-case pipeline.

## 1. Declared domain events — all 8 dead

Declared in `Ach:/Events/AchievementEvents.cs` with the comment "These events can be used to trigger cross-module actions and notifications" (`:4-6`). grep over `apps/api/Source/` finds **zero construction sites** for every record — the same class as Social.Posts N2 (12 orphan events) and tenants T8. The event-producer guard (G4) does not scan this shape (local declarations not deriving the durable-event interface), which is why they sit outside `wiring-gap-baseline.txt`.

| # | Event | Declared payload | Would signal | Construction sites | Consumers |
|---|---|---|---|---|---|
| 1 | `AchievementEarnedEvent` (`:11-18`) | UserId, AchievementId, name, points, level, EarnedAt, TenantId | award granted → notifications/leaderboards | **0** | 0 |
| 2 | `AchievementProgressUpdatedEvent` (`:23-29`) | progress vs target, IsCompleted | progress tick | **0** | 0 |
| 3 | `AchievementLevelUpEvent` (`:34-42`) | prev/new level, bonus points | tier promotion | **0** (and levels unreachable) | 0 |
| 4 | `AchievementPointsEarnedEvent` (`:47-52`) | points, total, source | leaderboard update | **0** | 0 |
| 5 | `AchievementUnlockedEvent` (`:57-62`) | name, reason | prerequisites now met | **0** | 0 |
| 6 | `AchievementCreatedEvent` (`:67-73`) | definition fields, CreatedBy | admin catalog change | **0** | 0 |
| 7 | `AchievementModifiedEvent` (`:78-83`) | ChangeDescription, ModifiedBy | definition change | **0** | 0 |
| 8 | `MilestoneAchievementEvent` (`:88-94`) | MilestoneRank, description | first-N-to-complete | **0** | 0 |

## 2. Durable operation events (the real contract)

`Ach:/Architecture/UseCaseEventContracts.g.cs:1-5` — auto-generated contracts binding each command to a generic durable operation event, all with `NoDomainEventReason = "No named cross-module event is declared; the durable generic operation event remains the observable contract."`:

| Use case | Operation event name |
|---|---|
| `MarkAchievementNotifiedCommand` | `gamification.achievements.mark-achievement-notified` |
| `CreateAchievementCommand` | `gamification.achievements.create-achievement` |
| `UpdateAchievementCommand` | `gamification.achievements.update-achievement` |
| `DeleteAchievementCommand` | `gamification.achievements.delete-achievement` |
| `AwardAchievementCommand` | `gamification.achievements.award-achievement` |

Reads (`my`, `eligible`, catalog) are service-direct and emit nothing.

## 3. The notifications that were promised and never sent

`NotificationType.AchievementUnlocked` and `NotificationType.ProgressMilestone` exist in the Notifications module's enum (`Notifications:/Entities/Notification.cs:373, 406`) and are wired into its preference machinery — category "Achievements" (`Notifications:/NotificationCategories.cs:41-44`), category toggle `AchievementsEnabled` (`NotificationPreferenceService.cs:210`). grep over `apps/api/Source/` for producers: **zero**. The only `AchievementUnlocked` matches outside Notifications are an unrelated enum member in Learning.Experience.Social and this module's dead domain event.

Net effect: the Achievements preference toggle guards traffic that can never arrive; users toggling it off are muting a channel that does not exist. This is the mirror image of the unnotified-handshake gap (05 §3): the server has an ack mechanism but no send mechanism.

## 4. Inbound events — none

The module subscribes to nothing (`grep IIntegrationEventHandler / IDomainEventHandler` in module — zero). Gamification triggers in other modules (post created, course completed, streak) have no path into Achievements because the entry point (`UpdateProgressAsync`) is caller-less (G1).
