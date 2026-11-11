# Social.Mentorship — STRIDE threat model

> Module `GameGuild.Social.Mentorship` · develop `ce31d5850c`. Families match 01/08.

## Family 1 — Profile & availability self-service

| Threat | Story | Mitigations | Verdict |
|---|---|---|---|
| **S**poofing | upsert someone else's profile | singleton keyed on actor `UserId` (`Men:/Commands/MentorshipCommands.cs:32-34`) | pass |
| **T**ampering | inject oversized fields | entity truncation/normalization (name 120, bio 2000, skills 80×20) | pass |
| **R**epudiation | "I never offered that slot" | rows carry owner ids + timestamps; operation events durable | adequate |
| **I**nformation disclosure | read unpublished profiles | directory Published-only; by-id null for unpublished (`Queries:93-101`) | pass |
| **D**oS — availability spam | publish 50-slot waves repeatedly to bloat the directory | ≤50/request, overlap-checked — but no cap on total open slots, no rate limit (M5) | weak |
| **E**levation | self-assign Matchmaker role | **none** — honor-system (M4) | fail |

## Family 2 — Booking (forgery & squatting)

| Threat | Story | Mitigations | Verdict |
|---|---|---|---|
| **S**poofing — **booking forgery** | matchmaker-mode booking on behalf of a victim advisee (reputation damage, calendar poisoning) | requires published Matchmaker profile — but that is self-granted (M4); mentee id taken from body unvalidated | **fail** (compounds M4) |
| **T**ampering — **slot double-book** | concurrent bookings race the Open guard | in-memory guard only (M1) | **fail** |
| **R**epudiation | "I never booked this" | actor id not stored on the session (only mentor/mentee + audit on cancel); matchmaker-booked rows indistinguishable from self-booked | weak — provenance gap |
| **I**nformation disclosure | enumerate others' sessions | `ListMy...` participant-scoped; `GetParticipantSessionAsync` rejects non-participants (`Commands:417-433`) | pass |
| **D**oS — slot squatting | book every open slot then no-show (griefing mentors); cancel→rebook churn is free | no booking quota, no penalty state, no no-show tracking (M7) | fail (product-level) |
| **E**levation | mentee completes own session | mentor-only guard (`Commands:447-450`) | pass |

## Family 3 — Cancel / complete

| Threat | Story | Mitigations | Verdict |
|---|---|---|---|
| **S**/T | cancel someone else's session | participant check (`Commands:417-433`) | pass |
| **R**epudiation | cancel audit captured (who/when/reason) | `Entities:378-390` | pass |
| **I** | — | — | n/a |
| **D**oS | cancel-flood to churn slots | symmetric cancel is by-design; no cooldown | weak |
| **E** | — | — | n/a |

## Family 4 — Public directory (anonymous)

| Threat | Story | Mitigations | Verdict |
|---|---|---|---|
| **S** | — (no identity) | — | n/a |
| **T** | — (read-only) | — | n/a |
| **R** | — | — | n/a |
| **I**nformation disclosure | scrape mentor PII (display name, headline, timezone, availability calendar) | only Published profiles; fields are professional by design; allow-listed surface (`API:/Security/AnonymousEndpointRegistry.cs:181-185`) | accepted (product feature) |
| **D**oS — scraping + search abuse | unthrottled contains-search | no rate limit (M5) | weak |
| **E** | — | — | n/a |

## Family 5 — Meeting-channel metadata (**calendar-link injection**)

| Threat | Story | Mitigations | Verdict |
|---|---|---|---|
| **T**ampering | a future meeting-URL field could carry phishing links rendered as platform meetings | today `MeetingUrl` is **unwritable** (M3) — the dead surface is currently the mitigation; when S1 is wired, URLs must be validated/schemed-allowlisted | open (pre-registration of the threat) |
| **S**poofing | channel enum implies Discord/Calendar integrations exist | they do not (declared follow-up) — UI must not over-claim | documented |

## Aggregate verdict

The module's authorization spine is sound (actor-scoped everywhere, participant checks, published-only public reads). Its integrity risks are **concurrency (M1) and privilege-modeling (M4)** — both classic "trust the client" shapes that pre-launch social features inherit; fix before the program has real users, since both corrupt scheduling state that has no reconciliation tool (no admin surface exists for mentorship at all — there is no way to repair a double-booked slot or revoke a matchmaker except direct DB edits).
