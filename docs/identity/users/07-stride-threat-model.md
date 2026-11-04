# Identity.Users STRIDE Threat Model

> `develop` 7ae5a28a65 · Families F1 lifecycle, F2 preferences/metadata/profile, F3 per-user notifications. Focus: information disclosure (the module owns the platform's PII core) and elevation (self-service admin surface). Paths relative to `apps/api/Source/Modules/GameGuild.Identity.Users/` unless stated.

## 1. Trust boundaries

TB1 client → API (attribute-policy gate); TB2 module → PostgreSQL (user PII); TB3 module → durable outbox; TB4 user A ↔ user B (self-vs-any authorization).

## 2. F1 — lifecycle

| Threat | Vector | Control | Verdict |
|---|---|---|---|
| S/E — enumerate all users | `GET /users` carries controller-level `[Authorize]` only, no collection policy (`Controllers/UsersController.cs:14-17,58`) — contrast profiles list which requires `Policies.UsersRead` (`UserProfilesController.cs:22-27`) | none beyond authentication | **open (gap W2)** — any authenticated principal can page the user directory (emails, statuses) |
| E — suspend/deactivate/purge another user's record | single+bulk admin verbs gated by `UsersAdmin`/`UsersPurge` policies | attribute gate + permission lookup | covered *if* the policies resolve (see the 403 contract — same `SelfOrPermission` family; `UsersAdmin` requires `users:admin` grant) |
| T — purge a user with active tenant membership bypassing the census | `ValidatePurge` throws on active memberships (`Entities/User.cs:405-409`) | entity gate | covered |
| R/I — deny a purge (no error taxonomy) | `ValidatePurge` throws `InvalidOperationException` → HTTP 500, not 409/422 | none | open (wrong status class; leaks nothing but breaks clients) |
| E — suspended user signs in | `CanSignIn` ignores `IsSuspended` (`ValueObjects/UserStatus.cs:56`) | Authentication module's own lockout policy governs | **documented dependency** — the Users flag alone does not contain a user |

## 3. F2 — preferences / metadata / profile (the PII family)

| Threat | Vector | Control | Verdict |
|---|---|---|---|
| S — read another user's preferences | route policy `UsersReadSelf` + `SelfOrPermission` (tenant-membership check for cross-user, `GameGuild.Identity.Authorization/Rules/Evaluators/SelfOrPermissionRuleEvaluator.cs:109-133`) | attribute gate | covered for non-holders; note the inverse — **legitimate owners also denied** until `users:read:self` is granted (the 403 contract, `flows/preferences-and-notifications-flow.md` §2) |
| T — mass assignment via PATCH | request DTOs are family-scoped records; validators on 17 of 20 families | covered for validated families; Localization family has NO validators (`Replace/Reset/UpdateUserLocalizationPreferencesCommand`) | **partial (gap W3)** |
| I/T — preference tampering at rest | plain JSON columns, no per-field audit, no versioning (`Entities/UserPreferences.cs:42-70`) | none in-module | open (forensics gap) |
| S — PII in metadata custom fields | free-form `Dictionary<string, object?>` (`Entities/UserMetadata.cs`) with `DataMaskingResourceType("User")` on controllers (`UsersController.cs:16`) | data-masking pipeline applies | covered at the read path; raw JSON columns remain full-fidelity in the DB |
| E — deprecated notification-preferences verbs as a bypass to Notifications-module preferences | `[Obsolete]` but still routed and writable (`Controllers/UserPreferencesController.cs:92-104`) | none | open (split-brain — see gaps G-Users-3) |

## 4. F3 — per-user notifications

| Threat | Vector | Control | Verdict |
|---|---|---|---|
| S — read/archive another user's notifications | same `UsersEditSelf` contract per §3 | covered (same 403 caveat) |
| T — unarchive to resurrect a notification | unguarded flag writes (`Entities/UserNotification.cs:74-84`) | none needed (user-owned state) | accepted |
| DoS — unbounded bulk bodies | bulk verbs take id lists; no per-request bound in validators that exist | partially (paging defaults) | minor |

## 5. Cross-cutting

| Threat | Vector | Control | Verdict |
|---|---|---|---|
| E — request reaches handler without authorization re-check | no `[AuthorizeRequest]` in the module — single gate at the controller attribute | none | **design posture — documented** (if a route is ever added without an attribute, nothing else protects the request; the platform's anonymous-endpoint registry test guards only `[AllowAnonymous]`) |
| I — suspension/purge invisible to the platform | orphan `User*Notification` producers (`event-flows.md` §2) | none | open — no downstream audit/alert for suspension or purge |
| T — dead query types smuggled into a future route | the 11 dead types have DTOs ready to wire (`Queries/UserQueries.cs:8-32`) | none (would need G7 ratchet) | accepted (dead code) |

## 6. Threat-to-gap index

S-enumeration → gaps W2; unvalidated Localization/purge/notification commands → W3; split-brain preferences → G-Users-3; orphan producers → G-Users-1; 500-instead-of-4xx purge → G-Users-4 (see `gaps.md`).
