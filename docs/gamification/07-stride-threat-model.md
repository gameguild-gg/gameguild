# Gamification.Achievements — STRIDE threat model

> Module `GameGuild.Gamification.Achievements` · develop `ce31d5850c`. Family numbering matches 01 flows / 08 DFDs.
> Per family: the STRIDE table lists threats with attacker/story → mitigations present → verdict. "Mitigation absent" rows map to 06-gaps entries.

## Family 1 — Achievement definition CRUD (admin surface)

| Threat | Story | Mitigations present | Verdict |
|---|---|---|---|
| **S**poofing | forge admin identity to create definitions | `RequirePermission(achievements:create/update/delete)` engine-backed (`ResourcePermissionAuthorizationFilter.cs:527-536`); class `[Authorize]` | partial — the check is sound, but the **keys are unseeded** (G2): nobody can pass it, so today the surface denies all (fail-closed) |
| **T**ampering | rename/re-point an existing achievement to inflate its meaning | update handler is the only mutation path; same permission gate | ok-if-granted |
| **R**epudiation | admin denies having created a definition | creation logs `Achievement created` with id+name (`AchievementService.cs:312`); generic operation event `gamification.achievements.create-achievement` (04 §2) | adequate (operation event is durable) |
| **I**nformation disclosure | read secret definitions | catalog honors `includeSecrets:false` (`AchievementService.cs:263-266`) | **fail** — by-id and eligible paths leak secrets (G3) |
| **D**oS | flood create/delete | no rate limiting on controller; no validators (G7) | fail (low impact — gated route) |
| **E**levation | non-admin reaches mutation | permission engine only; no policy fallback | pass (fail-closed today) |

## Family 2 — Manual award (points forgery / spoofing)

| Threat | Story | Mitigations present | Verdict |
|---|---|---|---|
| **S**poofing | award to self by calling award route | `achievements:award` permission (unseeded → denied) | fail-closed but dead (G2) |
| **T**ampering — **achievement forgery** | grant a definition the target never earned, or grant a high-points definition repeatedly | repeatable guard only for non-repeatables; **no audit trail of who awarded beyond logs**; target user comes from body | **partial** — award carries actor tenant + Context string but no durable who-awarded field on the row |
| **R**epudiation | "I never got that" / "nobody granted that" | `EarnedAt`, `Context` jsonb on row; log line | partial — Context is attacker-chosen free text |
| **I**nformation disclosure | enumerate other users' awards | `my*` routes scope to actor (`Controller:37-44`) | pass |
| **D**oS — **double-award race** | concurrent award requests double-grant points on a non-repeatable | check-then-insert, no transaction, non-unique index (`AchievementService.cs:40-49`; `UserAchievement.cs:17`) | **fail** (G8) |
| **E**levation | points lead to economy value elsewhere | **no bridge** — points sum is informational only; Economy wallet is a separate guarded ledger | pass by isolation (verify before any future points→currency bridge) |

## Family 3 — User reads (earned / eligible / points / catalog)

| Threat | Story | Mitigations present | Verdict |
|---|---|---|---|
| **S**poofing | read another user's achievements | SubjectId from ActorContext only | pass |
| **T**ampering | none (read-only family) | — | n/a |
| **R**epudiation | none | — | n/a |
| **I**nformation disclosure | catalog/by-id anonymous; secrets by id; eligible leaks secrets; by-id ignores tenant | only catalog list filters | **fail** (G3) |
| **D**oS | expensive eligible query (N+1 over all active definitions) | no rate limit, no cap on definition count | weak (G11) |
| **E**levation | none | — | n/a |

## Family 4 — The unnotified handshake (spoofed acks)

| Threat | Story | Mitigations present | Verdict |
|---|---|---|---|
| **S**poofing — **ack forgery** | any authenticated user acks someone else's award (suppresses their toast) | **none** — command takes only the award id | **fail** (G5) |
| **T**ampering | re-ack after undo? no reverse transition exists | idempotent flag only | pass (no harm) |
| **R**epudiation | "the platform notified nobody" — the flag proves nothing about delivery | none — no transport, no delivery record | **fail** (04 §3; documented truth) |
| **I**nformation disclosure | unnotified list scoped to actor | `UserId == actor` (`AchievementService.cs:194-199`) | pass |
| **D**oS | none meaningful | — | n/a |
| **E**levation | none | — | n/a |

## Family 5 — Persistence boundary (tables)

| Threat | Story | Mitigations present | Verdict |
|---|---|---|---|
| **T**ampering | direct DB insert of award rows | out of module scope; no checksums | noted (accepted platform-wide) |
| **D**oS | cascade delete wipes awards en masse (one admin delete → N rows gone) | `DeleteBehavior.Cascade` with no guard | fail (G9) |
| **I** | jsonb `Context`/`Conditions` columns readable by anyone with DB access | no encryption; Context may carry caller free text | noted |

## Aggregate verdict

The module's security posture is **fail-closed today** (admin keys unseeded → the risky surface denies everyone), with the real exposure in the *read* paths (secret leakage G3, ack forgery G5) and the *integrity* of awards once the admin surface is eventually unlocked (double-award G8, context-free award trail). Nothing here touches money — the moment a future story converts achievement points into Economy value, families 2 and 3 must be re-run against the Economy package's STRIDE (docs/economy/).
