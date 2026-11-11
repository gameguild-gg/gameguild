# Gamification.Achievements — Data-flow diagrams

> Module `GameGuild.Gamification.Achievements` · develop `ce31d5850c`. L0 context + L1 per flow family (numbering matches 01/07).

## L0 — Context

```mermaid
flowchart LR
    Anon[Anonymous internet user] --> L0A[GET catalog and by-id]
    User[Authenticated user] --> L0B[my earned eligible points unnotified ack]
    Admin[Permission-granted admin<br/>keys unseeded G2] --> L0C[create update delete award]
    L0A --> PROC((Achievements module))
    L0B --> PROC
    L0C --> PROC
    PROC --> DB[(achievements schema<br/>5 tables)]
    PROC --> LOG[Structured logs]
    PROC --> OPE[Durable operation events<br/>gamification-achievements-]
    style Admin stroke-dasharray: 5 5
```

Trust boundaries crossed: TB1 internet→API (authN via Identity), TB2 API→DB (EF Core), TB3 API→operation-event rail (platform outbox infra).

## L1-F1 — Definition CRUD

```mermaid
flowchart TD
    A[Admin client] --> TB1{{TB1 authN plus RequirePermission}}
    TB1 --> B[AchievementsController]
    B --> C[CreateAchievementCommandHandler]
    C --> D[Achievement-Create factory]
    D --> E[(achievements table)]
    C -.-> F[operation event create-achievement]
    note1[no validators at edge G7] -.-> B
    E --> G[cascade target user_achievements on delete G9]
    style TB1 stroke:orange
```

## L1-F2 — Manual award

```mermaid
flowchart TD
    A[Admin client body carries target UserId] --> TB1{{TB1 RequirePermission achievements-award}}
    TB1 --> B[AchievementsController award route]
    B --> C[AwardAchievementCommandHandler<br/>tenant from actor]
    C --> D[AchievementService-AwardAchievementAsync]
    D --> E[(achievements lookup)]
    D --> F[(user_achievements check-then-insert<br/>race G8)]
    F --> G[200 dto to admin]
    D -.-> H[log line only no event 04]
    style TB1 stroke:orange
```

## L1-F3 — User reads

```mermaid
flowchart TD
    U1[Authenticated user] --> TB1{{TB1 [Authorize]}}
    U2[Anonymous] --> TB1
    TB1 --> C[AchievementsController reads]
    C --> D[AchievementService queries]
    D --> E[(achievements incl IsSecret leak paths G3)]
    D --> F[(user_achievements scoped by actor)]
    D --> G[200 responses with secret names on two paths]
    style TB1 stroke:orange
```

## L1-F4 — Unnotified handshake

```mermaid
flowchart TD
    A[User client] --> TB1{{TB1 [Authorize] no ownership check G5}}
    TB1 --> B[GET my-unnotified]
    B --> C[(user_achievements IsNotified false)]
    C --> D[200 list]
    A --> E[POST my-id-mark-notified]
    E --> F[MarkAchievementNotifiedCommand]
    F --> G[(UPDATE IsNotified true any id)]
    G -.-> H[no transport exists client ack only]
    style TB1 stroke:orange
```

## L1-F5 — Dead progress pipeline (context only)

```mermaid
flowchart TD
    NOBODY[No producer exists G1] -.-> S[AchievementService-UpdateProgressAsync]
    S -.-> P[(achievement_progress)]
    S -.-> A[(user_achievements auto-award tail)]
    style NOBODY stroke-dasharray: 5 5
    style S stroke-dasharray: 5 5
    style P stroke-dasharray: 5 5
```

## Data stores summary

| Store | Table | Written by | Read by |
|---|---|---|---|
| definitions | `achievements` | create/update/delete handlers | all reads, award |
| awards | `user_achievements` | award handler, mark-notified | my/points/unnotified, prerequisite checks |
| tiers | `achievement_levels` | **nothing** (S2) | progress target derivation (dead) |
| prereqs | `achievement_prerequisites` | **nothing** (S3) | award/eligible checks |
| progress | `achievement_progress` | **nothing** (S1) | nothing |
