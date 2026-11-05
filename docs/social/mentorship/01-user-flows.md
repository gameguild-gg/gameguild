# Social.Mentorship — User flows

> Module `GameGuild.Social.Mentorship` · develop `ce31d5850c`. Shorthand `Men:` = module root. Families M1-M5 match 07/08 numbering (M6+ reserved for gaps).

## F1 — Mentor profile self-service (upsert → publish → pause)

```mermaid
flowchart TD
    A[User PUT v1-mentorship-profile] --> B[UpsertMentorProfileCommand<br/>Controllers-MentorProfileController.cs-33-55]
    B --> C{profile exists for user?}
    C -->|no| D[MentorProfile-Create born Draft<br/>Entities-cs-81-114]
    C -->|yes| E[profile-Update in place]
    D & E --> F{Role + PricingModel}
    F -->|Free and no reason| G[ArgumentException free needs reason<br/>Entities-cs-162-170]
    F -->|ok| H[skills replace max 20 distinct<br/>Commands-cs-76-110]
    H --> I[SaveChanges 200 dto]
    J[POST profile-status Published] --> K[SetMentorProfileStatusCommand<br/>-119-167]
    K --> L{target}
    L -->|Draft| M[RevertToDraft]
    L -->|Published| N[Publish sets PublishedAt once<br/>Entities-cs-143-148]
    L -->|Paused| O[Pause]
    M & N & O --> P[200 dto]
    Q{no profile yet?} --> R[RequestValidationException create first<br/>-151-166]
```

Edge cases:

| Case | Behavior | Evidence |
|---|---|---|
| One profile per user | upsert keyed on `UserId` singleton | `Men:/Commands/MentorshipCommands.cs:32-34` |
| Free-contribution rule (issue #102) | `Free` without reason rejected at entity | `Men:/Entities/MentorshipEntities.cs:162-170` |
| Introductory minutes normalized | ≤0 → 15; else clamp 10-15 | `:172-180` |
| Skills normalized + capped | trim, distinct ignore-case, ≤20 (over-limit throws) | `Commands/MentorshipCommands.cs:76-96` |
| Role freely self-assigned | Mentor/Advisor/**Matchmaker** chosen by the user — no approval | `Entities/MentorshipEntities.cs:53` + no gate |
| Tenant stamp | actor tenant at create only | `Commands/MentorshipCommands.cs:60` |

## F2 — Availability publishing

```mermaid
flowchart TD
    A[POST profile-availability slots] --> B[PublishMentorAvailabilityCommand<br/>Commands-cs-171-245]
    B --> C{1-50 ranges?}
    C -->|no| D[RequestValidationException]
    C -->|yes| E[profile must exist create-first]
    E --> F[load existing Open slots]
    F --> G{per range guards}
    G -->|starts ge now+1h| H[reject lead time]
    G -->|10min-8h duration| I[reject out of band]
    G -->|overlaps existing-or-created| J[reject overlap]
    G -->|ok| K[MentorAvailabilitySlot-Create UTC-kind enforced<br/>Entities-cs-296-312]
    K --> L[SaveChanges 200 sorted slots]
    M[DELETE availability-slotId] --> N[RemoveMentorAvailabilitySlotCommand<br/>-247-272]
    N --> O{owner and not Booked and not Cancelled?}
    O -->|Booked| P[reject cancel session first]
    O -->|ok| Q[slot-Cancel]
```

Edge cases:

| Case | Behavior | Evidence |
|---|---|---|
| Draft/paused profiles can publish slots | only ownership checked, status not | `Commands/MentorshipCommands.cs:197-199` |
| UTC enforced at entity | non-UTC kind throws | `Entities/MentorshipEntities.cs:308-311` |
| Overlap check in-memory | existing Open + within-batch — no DB constraint backs it | `Commands/MentorshipCommands.cs:228-232` |

## F3 — Advisee books a session

```mermaid
flowchart TD
    A[User POST v1-mentorship-sessions] --> B[BookMentorshipSessionCommand<br/>Controllers-MentorshipSessionsController.cs-52-70]
    B --> C{slot exists and Open?}
    C -->|no| D[RequestValidationException no longer open]
    C -->|yes| E{mentor profile Published?}
    E -->|no| F[reject not accepting bookings]
    E -->|yes| G{slot started already?}
    G -->|yes| H[reject already started]
    G -->|no| I{BookingMode}
    I -->|MatchmakerOnly| J{actor is published Matchmaker?}
    J -->|no| K[reject ask a matchmaker]
    J -->|yes| L{MenteeUserId named?}
    L -->|no| M[reject must name advisee]
    I -->|Public| N{MenteeUserId for another?}
    N -->|yes and not matchmaker| O[reject only matchmaker books others]
    N -->|no or self| P[mentee = actor]
    L & P --> Q{duration le IntroductoryCallMinutes<br/>and OffersIntroductoryCall}
    Q -->|yes| R[session IsIntroductory true]
    Q -->|no| S[IsIntroductory false]
    R & S --> T[MentorshipSession-Book guards<br/>self-booking topic required UTC range<br/>Entities-cs-337-376]
    T --> U[slot-Book sessionId Open-only guard]
    U --> V[SaveChanges 201 session dto]
    style U fill:#fdd
```

The red node is the race (M1): `slot.Book` guards in memory only; concurrent requests can both pass `Open` and both insert sessions.

Edge cases:

| Case | Behavior | Evidence |
|---|---|---|
| Self-booking blocked | `mentorUserId == menteeUserId` throws at entity | `Entities/MentorshipEntities.cs:354-357` |
| Matchmaker booking self as mentee | blocked by the same rule | same |
| Matchmaker-only visibility | slots visible publicly but not self-bookable (issue #102) | `Entities/MentorshipEntities.cs:25-29` + flow |
| Topic required, ≤200 chars; notes ≤2000 | entity truncation/throw | `:366-369` |

## F4 — Cancel / complete

```mermaid
flowchart TD
    A[Participant POST sessions-id-cancel reason] --> B[CancelMentorshipSessionCommand<br/>Commands-cs-384-434]
    B --> C{actor is mentor or mentee?}
    C -->|no| D[RequestValidationException not found for user]
    C -->|yes| E{status Booked?}
    E -->|no| F[reject only booked can cancel]
    E -->|yes| G[session-Cancel reason 500 captured who when<br/>Entities-cs-378-390]
    G --> H{slot linked to this session?}
    H -->|yes| I[slot-Release back to Open]
    H -->|no| J[skip]
    I & J --> K[200 dto]
    L[Mentor POST sessions-id-complete] --> M[CompleteMentorshipSessionCommand<br/>-436-456]
    M --> N{actor is THE mentor?}
    N -->|no| O[reject only mentor completes]
    N -->|yes| P{status Booked?}
    P -->|no| F
    P -->|yes| Q[session-Complete CompletedAt now]
```

Edge cases:

| Case | Behavior | Evidence |
|---|---|---|
| Cancellation is symmetric | either side cancels; counterparty learns nothing (no notification) | `Commands/MentorshipCommands.cs:417-433` (M2) |
| Cancel releases the slot for re-booking | `slot.Release()` guarded to Booked | `Entities/MentorshipEntities.cs:272-283` |
| Mentee cannot complete | mentor-only | `Commands/MentorshipCommands.cs:447-450` |
| Completing a past session anytime | no time window check | `Entities/MentorshipEntities.cs:392-402` |

## F5 — Public directory + dashboard

```mermaid
flowchart TD
    A[Anonymous GET v1-mentorship-mentors] --> B[ListMentorsQuery skill role search intro skip take<br/>Queries-cs-16-88]
    B --> C[Published only ordered PublishedAt desc]
    C --> D{skill filter}
    D -->|given| E[NormalizedSkill exact match via subquery]
    D -->|search| F[DisplayName-Headline contains client-lower]
    F --> G[Take clamp 1-100 default 20]
    G --> H[200 with skills batch-loaded]
    I[Anonymous GET mentors-profileId] --> J[GetMentorQuery no viewer]
    J --> K{Published?}
    K -->|no| L[null → 404 even for owner]
    K -->|yes| M[200 profile]
    N[Anonymous GET mentors-profileId-availability] --> O[ListMentorAvailabilityQuery Open-only default 200 cap<br/>Queries-cs-112-160]
    P[Mentor GET profile-dashboard] --> Q[GetMentorDashboardQuery<br/>Queries-cs-233-286]
    Q --> R[profile + next-10 upcoming + open-slot count + completed-total counts]
```

Edge cases:

| Case | Behavior | Evidence |
|---|---|---|
| Directory search is case-insensitive contains | `ToLower()` in query — translatable (column-side, no custom method) | `Queries/MentorshipQueries.cs:55-58` |
| Availability window filter UTC-kind forced | `SpecifyKind` on from/to | `:139-148` |
| My sessions filter | mentor-or-mentee, or `asMentorOnly`; capped 200 | `:180-207` |
| Registry-listed anonymous surface | allow-list entry with published-only rationale | `API:/Security/AnonymousEndpointRegistry.cs:181-185` |
