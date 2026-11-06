# Social.Mentorship — State machines

> Module `GameGuild.Social.Mentorship` · develop `ce31d5850c`. Shorthand `Men:`.

## 1. Mentor profile (`MentorProfileStatus`, `Men:/Entities/MentorshipEntities.cs:16-21`)

```mermaid
stateDiagram-v2
    [*] --> Draft : Create born Draft cs-107
    Draft --> Published : Publish sets PublishedAt once cs-143-148
    Published --> Paused : Pause cs-150-154
    Paused --> Published : Publish again PublishedAt kept
    Published --> Draft : RevertToDraft cs-156-160
    Paused --> Draft : RevertToDraft
    Draft --> Draft : Upsert in place no state change
```

| Guard | Note |
|---|---|
| free transitions, no approval step | self-service publish — by design (#102), and the root of M4 (matchmaker role is publishable by anyone) |
| PublishedAt sticky | `??=` keeps first publish time for directory ordering (`:146`) |

Booking-side coupling: only `Published` profiles accept bookings (`Men:/Commands/MentorshipCommands.cs:302-305`) — pausing stops new bookings without touching existing sessions.

## 2. Availability slot (`MentorAvailabilitySlotStatus`, `Entities:32-37`)

```mermaid
stateDiagram-v2
    [*] --> Open : Create guards UTC + range cs-242-255
    Open --> Booked : Book sessionId Open-only guard cs-260-270
    Booked --> Open : Release on session cancel clears SessionId cs-272-283
    Open --> Cancelled : Cancel by mentor remove-slot cs-284-294
    Booked --> Cancelled : Cancel idempotent no-op path cs-285-287
    Cancelled --> [*] : terminal
```

Honesty notes:

| # | Truth | Evidence |
|---|---|---|
| 1 | The Open→Booked guard is **in-memory only** — the double-book race bypasses it (`slot.Book` after both loaded Open) | `Commands/MentorshipCommands.cs:289-294` + `:363`; no concurrency token on the entity, no unique index on `SessionId` (M1) |
| 2 | Booked slots cannot be removed — "cancel the session first" — and cancelling the session auto-releases | `Commands:263-266`, `:402-411` |
| 3 | Cancelled is terminal (no re-open) | no transition |

## 3. Session (`MentorshipSessionStatus`, `Entities:39-44`)

```mermaid
stateDiagram-v2
    [*] --> Booked : Book guards self-booking topic range<br/>Entities-cs-337-376
    Booked --> Cancelled : Cancel by mentor or mentee<br/>reason who when captured cs-378-390
    Booked --> Completed : Complete mentor-only cs-392-402
    Cancelled --> [*]
    Completed --> [*]
```

| Guard | Note |
|---|---|
| terminal states have no exits | no reschedule, no no-show, no re-open — the lifecycle is intentionally minimal |
| Cancel captures audit (`CancelledByUserId`, `CancelledAt`, `CancellationReason` ≤500) | `Entities:378-390` |
| Complete records only timestamp | who completed is implicit (mentor-only guard) |
| `IsIntroductory` computed at booking | duration ≤ intro minutes and profile offers intro (`Commands:346-349`) — never recomputed |
