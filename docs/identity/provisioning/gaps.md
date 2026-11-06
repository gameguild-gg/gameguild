# Identity.Provisioning — Consolidated Gaps

> `develop` 7ae5a28a65 · NEW gaps; the module is otherwise the cleanest-wired identity package audited in this campaign. Paths relative to `apps/api/Source/Modules/GameGuild.Identity.Provisioning/` unless stated.

| # | Gap | Evidence | Suggested severity |
|---|-----|----------|--------------------|
| P1 | **Audit sink defaults to Null** — `TryAddScoped<IScimProvisioningAuditSink>(NullScimProvisioningAuditSink.Instance)` silently drops all SCIM auth/operation audit when the Audit module is not composed; prefer a startup check or a logging fallback that warns once | `ProvisioningModuleExtensions.cs:57`; Null impl `Auditing/ScimProvisioningAudit.cs:42-52`; real binding `GameGuild.Compliance.Audit/Extensions/AuditModule.cs:137` | medium |
| P2 | Discovery endpoints require authentication though RFC 7644 §4 permits anonymous ServiceProviderConfig discovery — IdPs that probe before configuring tokens will fail | `Controllers/ScimDiscoveryControllers.cs:16,34,62,78` | low (decision; revisit for interop) |
| P3 | List `count`/pagination caps not documented/enforced beyond processor defaults | `Scim/ScimListResponse.cs` + service list paths | low |
| P4 | No dedicated rate-limit policy on the token-admin controller or `/scim` paths (host-level limiters only; contrast `[EnableRateLimiting]` on Authorization's `PolicyBundlesController`) | absence on `Controllers/ScimProvisioningTokensAdminController.cs` | low |
| P5 | SCIM user create does not emit `UserCreatedEvent` if it bypasses `CreateUserCommand` — quota/sign-up consumers may miss IdP-provisioned users; needs a pinning test | mapping path `Services/ScimUserService.cs` create branch; contract note `event-flows.md` §3 | medium (verify + pin) |
| P6 | No sensitive-role guard on SCIM Group writes — a `scim:write` token can mutate privileged role membership; consider a deny-list of protected roles | `Scim/ScimGroupPatchApplier.cs` + `Services/ScimGroupService.cs` | medium (hardening) |

## Suggested issue split

1. `chore(provisioning): fail loudly when the SCIM audit sink is the Null default` (P1).
2. `feat(provisioning): sensitive-role guard for SCIM group operations` (P6).
3. `test(provisioning): pin UserCreatedEvent behavior for SCIM-created users` (P5).
4. Optional interop/hardening batch (P2+P3+P4).
