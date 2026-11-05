# Social.Mentorship — Data-flow diagrams

> Module `GameGuild.Social.Mentorship` · develop `ce31d5850c`. Families match 01/07.

## L0 — Context

```mermaid
flowchart LR
    ANON[Anonymous visitor] --> DIR[Public mentor directory]
    USER[Authenticated user<br/>mentor advisor matchmaker advisee] --> SELF[Profile availability sessions dashboard]
    DIR --> PROC((Social-Mentorship module))
    SELF --> PROC
    PROC --> DB[(mentorship schema 4 tables)]
    PROC -.-> OPE[Durable operation events social-mentorship-]
    style OPE stroke-dasharray: 4 4
```

Trust boundaries: TB1 internet→API (authN; directory anonymous + allow-listed), TB2 API→DB (EF Core). No external providers (calendar/Discord dispatch not built — declared in #1159).

## L1-F1 — Profile self-service

```mermaid
flowchart TD
    U[User TB1 Authorize] --> C[MentorProfileController]
    C --> H[UpsertMentorProfileCommandHandler]
    H --> E[MentorProfile entity guards<br/>free-reason intro-clamp UTC tz]
    E --> T1[(MentorProfiles)]
    E --> T2[(MentorSkills replace-set)]
    H -.-> O1[operation event upsert-mentor-profile]
```

## L1-F2 — Availability

```mermaid
flowchart TD
    U[Mentor TB1] --> C[POST availability]
    C --> H[PublishMentorAvailabilityCommandHandler<br/>lead-time duration overlap guards]
    H --> T[(MentorAvailabilitySlots Open)]
    U --> D[DELETE availability-slotId]
    D --> H2[Remove slot Booked-guard]
    H2 --> T
```

## L1-F3 — Booking

```mermaid
flowchart TD
    A[Advisee TB1] --> C[MentorshipSessionsController POST]
    C --> H[BookMentorshipSessionCommandHandler]
    H --> Q1[(slot Open check in-memory)]
    H --> Q2[(profile Published check)]
    H --> Q3[(matchmaker privilege self-service M4)]
    H --> S[(MentorshipSessions INSERT Booked)]
    H --> SL[(slot UPDATE Booked race M1)]
    S -.-> O[operation event book-mentorship-session]
    style SL stroke:orange
```

## L1-F4 — Cancel / complete

```mermaid
flowchart TD
    P[Participant TB1] --> C[POST cancel reason]
    C --> H[CancelMentorshipSessionCommandHandler<br/>participant + Booked guards]
    H --> S1[(session Cancelled + audit)]
    H --> S2[(slot Release → Open)]
    M[Mentor TB1] --> C2[POST complete]
    C2 --> H2[CompleteMentorshipSessionCommandHandler mentor-only]
    H2 --> S3[(session Completed)]
```

## L1-F5 — Public directory + dashboard

```mermaid
flowchart TD
    A[Anonymous TB1 allow-listed] --> Q[ListMentorsQuery Published-only<br/>skill search filters]
    Q --> T1[(MentorProfiles + Skills read)]
    A --> Q2[GetMentorQuery unpublished → null]
    A --> Q3[ListMentorAvailabilityQuery Open-only cap 200]
    M[Mentor TB1] --> D[GetMentorDashboardQuery]
    D --> T2[(5 aggregate reads)]
```

## Data stores summary

| Store | Written by | Read by |
|---|---|---|
| `MentorProfiles` | upsert/status handlers | directory, booking, dashboard |
| `MentorSkills` | upsert (replace-set) | directory/batch load, mappers |
| `MentorAvailabilitySlots` | publish/remove handlers, booking (Book/Release) | public availability, booking, dashboard count |
| `MentorshipSessions` | booking/cancel/complete | my-sessions, participant checks, dashboard |
