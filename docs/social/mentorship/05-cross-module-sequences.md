# Social.Mentorship — Cross-module sequences

> Module `GameGuild.Social.Mentorship` · develop `ce31d5850c`. Shorthand `Men:`.

## 1. Booking end-to-end (advisee self-service)

```mermaid
sequenceDiagram
    participant M as Advisee client
    participant MC as MentorshipSessionsController
    participant H as BookMentorshipSessionCommandHandler
    participant DB as mentorship schema

    M->>MC: POST v1-mentorship-sessions slotId topic channel
    MC->>H: BookMentorshipSessionCommand actingUser menteeUser?
    H->>DB: load slot
    H->>DB: load mentor profile
    H->>H: slot Open profile Published not-started
    alt MatchmakerOnly
        H->>DB: actor is published Matchmaker?
        H->>H: mentee must be named
    else booking for another
        H->>DB: actor is published Matchmaker?
    else self
        H->>H: mentee = actor
    end
    H->>H: introductory check duration le intro-minutes
    H->>DB: INSERT MentorshipSession Booked
    H->>DB: UPDATE slot Open → Booked sessionId
    note over H,DB: in-memory guard only — race double-books M1
    H-->>MC: session dto
    MC-->>M: 201
    note over M,DB: no notification no event beyond operation event M2
```

## 2. Cancel with slot release

```mermaid
sequenceDiagram
    participant P as Mentor or mentee
    participant H as CancelMentorshipSessionCommandHandler
    participant DB as mentorship schema

    P->>H: POST sessions-id-cancel reason
    H->>DB: load session participant check
    H->>H: Booked-only guard
    H->>DB: UPDATE session Cancelled + audit fields
    H->>DB: load linked slot
    alt slot-SessionId eq session
        H->>DB: slot Release → Open
    end
    H-->>P: 200 dto
    note over DB: released slot immediately re-bookable by anyone
```

## 3. Dashboard aggregation

```mermaid
sequenceDiagram
    participant T as Mentor
    participant Q as GetMentorDashboardQueryHandler
    participant DB as mentorship schema

    T->>Q: GET profile-dashboard
    Q->>DB: profile + skills
    Q->>DB: next 10 Booked sessions StartsAt ge now
    Q->>DB: count Open future slots
    Q->>DB: count all sessions count Completed sessions
    Q-->>T: counters + upcoming list
    note over Q,DB: 5 sequential queries no caching fine at current scale
```

## 4. The #1159 creator-process boundary contract

PR #1159 delivered two product tracks together — the content-creator pipeline (issue #105: public `POST /v1/content/course-proposals`, wizard at `apps/web/src/app/[locale]/(public)/content-creators/*`, actions in `apps/web/src/lib/content-creators/actions.ts`) and this module (issue #102). The relationship is **product-level and deliberately uncoupled at code level**:

```mermaid
flowchart LR
    A[Creator applies #105<br/>course-proposals + wizard] --> B[Creator publishes courses]
    B -.grows into.-> C[Mentor or Advisor profile<br/>Role self-assigned]
    D[Matchmaker profile<br/>self-service publish] -.onboards.-> A
    C --> E[Program mentorship sessions]
    D -.may book on behalf of.-> E
```

- No module references `CourseProposals` from mentorship or vice versa (grep both ways — zero).
- No shared events; the tracks meet only in the UI journey and the issue text.
- Consequence for auditors: creator-pipeline changes cannot break mentorship and vice versa; but there is also **no enforcement** that a "Mentor" ever was a creator — the role is self-asserted (see M4).

## 5. Tenant boundary

Profiles stamp the creating actor's tenant (`Men:/Commands/MentorshipCommands.cs:60`); **no query filters by tenant** — the directory and bookings are platform-global by construction (anonymous directory lists across tenants). The tenant column is provenance, not isolation; documented so nobody assumes tenant scoping that does not exist.
