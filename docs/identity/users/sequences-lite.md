# Identity.Users — Sequences (lite)

> `develop` 7ae5a28a65 · Cross-module sequences only, per the campaign's sequences-lite convention. Paths relative to `apps/api/Source/Modules/GameGuild.Identity.Users/` unless stated.

## 1. Sign-up / admin-create → durable UserCreatedEvent → consumers

```mermaid
sequenceDiagram
    participant SU as LocalAuthService sign-up OR CreateUserCommandHandler
    participant U as User entity
    participant DB as ApplicationDbContext SaveChanges
    participant OB as OutboxMessage durable
    participant OD as OutboxDispatcher host
    participant H as IIntegrationEventHandler consumers

    SU->>U: AddIntegrationEvent new UserCreatedEvent - LocalAuthService.cs 861 or CreateUserCommandHandler.cs 20
    U->>DB: SaveChangesAsync
    DB->>OB: captured in same transaction - ApplicationDbContext.cs 184-230
    DB-->>SU: 201 committed
    OB-->>OD: relay picks up row
    OD->>H: dispatch by IIntegrationEventHandler of T - OutboxDispatcher.cs 153 283
    Note over H: consumers registered in host composition e g quota accounting via QuotaImpact plus one Users
```

Fallback: none needed — outbox is transactional; redelivery is the dispatcher's inbox/retry ladder.

## 2. User delete vs active sessions / refresh tokens

```mermaid
sequenceDiagram
    participant A as Admin or self
    participant UC as DeleteUserCommandHandler
    participant OB as OutboxMessage
    participant AUTHN as Authentication module session state

    A->>UC: DELETE users id - soft delete
    UC->>UC: user AddIntegrationEvent UserDeletedEvent - DeleteUserCommandHandler.cs 25
    UC->>OB: durable row committed
    Note over AUTHN: the Authentication module owns session revocation on user deletion - covered by docs auth cross-module-sequences.md section 3 - not re-derived here
```

## 3. Preference write → Notifications module (absent flow)

```mermaid
sequenceDiagram
    participant C as Client
    participant PC as UserPreferencesController
    participant PH as Preferences handler
    participant N as Notifications module

    C->>PC: PATCH users id preferences notifications - deprecated verbs
    PC->>PH: UpdateUserNotificationPreferencesCommand
    PH-->>C: 200
    Note over N: no event no notification crosses this boundary - the Notifications module keeps its OWN preferences at api notifications preferences - the users copy is deprecated - UserPreferencesController.cs 92-104
```

Verdict: **no integration exists** — the deprecated users-side notification preferences drift independently of the Notifications module's live copy (gap G-Users-3 in `gaps.md`).

## 4. Purge → tenant membership coupling

```mermaid
sequenceDiagram
    participant A as Admin with UsersPurge
    participant PU as PurgeUserCommandHandler
    participant E as User ValidatePurge
    participant NOTIF as IPublisher

    A->>PU: POST users id purge with strategy
    PU->>E: ValidatePurge - Entities User.cs 395-409
    E-->>PU: throws InvalidOperationException if not soft deleted OR active memberships remain - maps to 500
    PU->>NOTIF: publish UserPurgedNotification - PurgeUserCommandHandler.cs 36
    Note over NOTIF: zero consumers - the purge strategy string rides a notification nobody receives
    PU-->>A: 200 hard deleted
```
