# Identity.Provisioning STRIDE Threat Model

> `develop` 7ae5a28a65 · Families F1 tokens, F2 SCIM users, F3 SCIM groups (=roles), F4 discovery/bulk. Focus: **bearer-token compromise** and **scope/tenant isolation** — this module hands an external system write access to users AND authorization roles. Paths relative to `apps/api/Source/Modules/GameGuild.Identity.Provisioning/` unless stated.

## 1. Trust boundaries

TB1 IdP → API (bearer token over TLS); TB2 token → tenant scope (claims-derived); TB3 SCIM write → `Identity.Users` records; TB4 SCIM group write → tenant roles (authorization surface); TB5 module → audit sink.

## 2. F1 — tokens

| Threat | Vector | Control | Verdict |
|---|---|---|---|
| S — token theft at rest | plaintext never stored: SHA-256 `KeyHash` (unique index) + `KeyPrefix` only | `Entities/ScimProvisioningToken.cs:15,51,126` | covered |
| S — token disclosure in responses | plaintext returned exactly once at create; list endpoint returns metadata only | create handler; `Controllers/ScimProvisioningTokensAdminController.cs:34` | covered |
| T — token forgery | 256-bit RNG body (`RandomNumberGenerator`, 48 bytes → 32-char base64url body) | `Entities/…Token.cs:105-121` | covered |
| E — scope escalation by token | scopes fixed at create, checked per request (`ScimScopes.cs:21,29`); only `ProvisioningManageTokens` holders can mint/rotate | covered |
| R — repudiation of IdP actions | every auth outcome audited with token id + tenant (`Authentication/…Handler.cs:85,102-110`); operation events carry `scim_token:{id}` actor | covered **iff** the real audit sink is bound — Null default is the hole (gap P1) |
| DoS — token spray | unknown-token path is a hash lookup + audit per attempt; no module-level rate limit on `/scim` (host-level limiters apply) | partial (host rate limiting; no dedicated SCIM limiter) |

## 3. F2 — SCIM users

| Threat | Vector | Control | Verdict |
|---|---|---|---|
| E — provision an admin account into the tenant | SCIM creates plain users; role assignment is the Groups surface (F4) with its own scope checks | covered (separation) |
| T — cross-tenant user writes | request context pins tenant from the token claims; mapping lookups are tenant-scoped | covered |
| I — PII overprovision | SCIM schema fields map to the Users records; emails required (`Services/ScimUserService.cs:109-116`) | accepted (protocol-required) |
| DoS — bulk abuse | `failOnErrors` stops runaway loops; no hard operation-count cap | partial (gap P3-adjacent) |

## 4. F3 — SCIM groups = tenant roles (the sharpest edge)

| Threat | Vector | Control | Verdict |
|---|---|---|---|
| **E — a write-scoped token reshapes authorization**: SCIM Group PATCH mutates role membership; a compromised IdP token can add itself to privileged roles | scope `scim:write` + tenant pinning only — **no role-sensitive-operation gate** (e.g. protecting Owner/Admin-equivalent groups) | **documented risk** — mitigations: token scope minimization at create, rotation grace, full audit trail; consider a sensitive-roles deny-list (gap P6) |
| T — role identity confusion | Group id ↔ `roleId` mapping rows keep associations stable | covered (`Scim/ScimResourceMappers.cs:92-108`) |

## 5. F4 — discovery + bulk

| Threat | Vector | Control | Verdict |
|---|---|---|---|
| S — anonymous schema probing | discovery requires the token scheme (stricter than RFC's optional anon) | covered (decision documented, gap P2 for interop) |
| I — feature probing | disabled feature → 404 before auth, no scheme leak | covered |
| T — bulk partial-failure confusion | per-operation status envelope; `failOnErrors` honored | covered |

## 6. Threat-to-gap index

Audit-loss-on-miscompose → P1; sensitive-role provisioning → P6; discovery interop → P2; no dedicated rate limit → P4 (see `gaps.md`).
