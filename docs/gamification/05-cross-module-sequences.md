# Gamification.Achievements — Cross-module sequences

> Module `GameGuild.Gamification.Achievements` · develop `ce31d5850c`. Shorthand `Ach:`, `Notif:` = `apps/api/Source/Modules/GameGuild.Notifications/`.

## 1. Manual award — the only live end-to-end path

```mermaid
sequenceDiagram
    participant Admin as Admin client
    participant API as AchievementsController
    participant H as AwardAchievementCommandHandler
    participant S as AchievementService
    participant DB as achievements schema

    Admin->>API: POST api-achievements-id-award
    API->>API: RequirePermission achievements-colon-award
    note over API: key never seeded G2 → 403 today
    API->>H: AwardAchievementCommand UserId-from-body Context
    H->>H: TenantId from ActorContext
    H->>S: AwardAchievementAsync userId achievementId context tenant
    S->>DB: load Achievement incl Levels and Prerequisites
    S->>DB: existing UserAchievement lookup non-repeatable
    S->>DB: load user awards for prerequisites
    S->>DB: INSERT user_achievements IsCompleted true
    note over S,DB: no transaction no unique index double-award race 07 §2.2
    S-->>H: Result-Success UserAchievement
    H-->>API: dto
    API-->>Admin: 200
    note over S: no event no notification emitted 04
```

## 2. The client-polled notify loop (what "notification" means here)

```mermaid
sequenceDiagram
    participant Client as User client
    participant API as AchievementsController
    participant S as AchievementService
    participant DB as achievements schema

    loop poll
        Client->>API: GET api-achievements-my-unnotified
        API->>S: GetUnnotifiedAchievementsAsync
        S->>DB: SELECT IsNotified false AND IsCompleted true
        S-->>API: rows
        API-->>Client: 200 list
    end
    Client->>Client: render toast
    Client->>API: POST my-userAchievementId-mark-notified
    note over API: no ownership check any authed user can ack 07 §4.3
    API->>S: MarkNotifiedAsync
    S->>DB: UPDATE IsNotified true
    API-->>Client: 204
```

Contrast with every other consumer of Notifications: Identity/Billing/Learning producers call `INotificationService.SendAsync` which persists a Notification row and lets the dispatchers deliver it (see docs/notifications/05). Achievements has **no such call site** — the bridge is the client itself.

## 3. The missing bridge, both directions

| Direction | Designed carrier | Reality |
|---|---|---|
| Achievements → Notifications (award notice) | `AchievementEarnedEvent` → hypothetical subscriber → `SendAsync(AchievementUnlocked)` | event never constructed (04 §1); `NotificationType.AchievementUnlocked` has zero producers — the preference toggle for the Achievements category guards nothing |
| Any module → Achievements (progress tick) | module would call `IAchievementService.UpdateProgressAsync` after e.g. post-created / course-completed | zero callers (G1); Posts' `PostCreatedEvent` family is itself dead (social-platform N2) — a dead emitter pointing at a dead receiver |

## 4. Tenant boundary contract

```mermaid
sequenceDiagram
    participant U as User tenant A
    participant API as Controller-Service
    participant DB as achievements schema

    U->>API: GET my achievements
    API->>DB: WHERE UserId actor AND TenantId eq actor-tenant OR TenantId IS NULL
    note over DB: definitions and awards are tenant-or-global AchievementService-cs-136-270-292-301
    U->>API: GET achievement-by-id
    API->>DB: WHERE Id no tenant predicate no IsSecret predicate
    note over API: cross-tenant read + secret leak G3
```

- Definitions created via API carry the creating actor's tenant (`Ach:/Commands/AchievementCommands.cs:60`); nothing stops a global definition being awarded in any tenant (award tenant = actor's).
- There is no per-tenant achievement catalog separation: the catalog route (`[AllowAnonymous]`, `AchievementsController.cs:163-178`) ignores tenant entirely.
