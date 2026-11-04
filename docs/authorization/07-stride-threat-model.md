# Authorization STRIDE Threat Model — privilege escalation focus

> Module `GameGuild.Identity.Authorization` (+ its authorization-surface controllers in `GameGuild.Identity.Authentication`) · Documented against `develop` commit `7ae5a28a65`, i.e. **after** #1203 (dead-endpoint fixes) and #1230 (governance enforcement).
> Focus per commission: **elevation of privilege** across the grant lifecycle, JIT elevation, and the ABAC/conditional fail-open history — every #1230 fix is cited as a mitigation, and every still-open fail-open is called out. STRIDE categories are applied to the module's seven flow families; the families map 1:1 to the files in [`flows/`](flows/).
> Cross-reference: full gap register in [`gaps.md`](gaps.md) (IDs reused below); decision-path anatomy in [`flows/permission-evaluation-decision-path-flow.md`](flows/permission-evaluation-decision-path-flow.md); sequences in [`cross-module-sequences.md`](cross-module-sequences.md).

Path convention: `file:line` relative to `apps/api/Source/Modules/GameGuild.Identity.Authorization/` unless stated otherwise.

## 1. Methodology and trust boundaries

STRIDE per family, weighting **E** (elevation of privilege) and **T** (tampering) — the two categories a permission engine cannot afford. Trust boundaries crossed by every family:

- **TB1** client → API (authenticated principal claims vs. route/resource targets)
- **TB2** permission engine → persistence (grants, policies, ACLs — the tamper surface)
- **TB3** tenant A actor → tenant B data (tenant isolation inside one database)
- **TB4** engine → cache layers (L1 memory / L2 Redis — stale-permission surface)
- **TB5** engine → external consumers (revocation webhooks — TB-out with no receipt)

## 2. Family 1 — Grant lifecycle (grant → expire → revoke → extend → delegate)

**Assets**: `TenantPermission` grants (`Entities/TenantPermission.cs` — `ExpiresAt`, `GrantedBy`, fail-closed `ParsePermissionTypes` at `:71`), delegations (`Entities/PermissionDelegation.cs`).

| Threat | Vector | Current control (mitigation) | Verdict |
|---|---|---|---|
| E — mint a grant without going through the mutation contract | direct EF writes bypassing audit | all grant mutations flow through `PermissionMutationTransaction` (`Services/PermissionMutationTransaction.cs:10`) — same transaction as audit rows | covered |
| E — expired grant still honored | lazy expiration only; process asleep | lazy check at resolution + worker sweep (`Services/PermissionExpirationService.cs:69-104`) with `ProcessedStampKey` idempotency + system-admin authz guard at `:60-66` | covered; **alert about the expiration is best-effort (gap E2)** but the grant itself expires regardless |
| T — extend own grant forever | self-extension | extension caps (`:309`, issue #331) cap single extensions and the resulting distance; extensions require the mutation contract | covered (cap enforcement) |
| T — re-grant silently keeps earlier deadline | surprising merge | documented behavior, not a vuln (`Services/FocusedPermissionServices.cs:125-127`, gap G14) | accepted |
| E — delegation chain grants more than the delegator had | transitive delegation | delegation depth guards + DENY-WINS resolution (`Services/EffectivePermissionResolverService.cs:24-35`) | covered; hierarchy depth cap silently drops grants (gap A3) |
| I/D — revoke hides from audit | revoke path skips audit | revoke is a mutation-contract operation with audit row | covered |

## 3. Family 2 — JIT elevation (request → approve → activate → expire → revoke)

**Assets**: `JitElevationRequest` (`Entities/JitElevationRequest.cs`).

| Threat | Vector | Current control (mitigation) | Verdict |
|---|---|---|---|
| E — self-approval | requester approves own elevation | explicit guard `if (reviewerId == RequesterId) throw` (`Entities/JitElevationRequest.cs:111-113`) | covered |
| E — approve a non-pending request | state confusion | status guard `Status != Pending → throw` (`:107-112`) — post-#1230 the approve path auto-activates (`:123-133`), closing the formerly-unreachable `Active` state (gap G11 **fixed**) | covered |
| E — elevated permission outlives the window | expiry not enforced | `IsGrantInForce` window checks (`:75`); revoke bumps the **tenant** security version (`Services/JitElevationService.cs:280-303`) so cached views invalidate | **partial — user security version is NOT bumped (gap G10 open)**: a user-scoped cached decision can survive tenant-cache invalidation windows |
| T — approval without reviewer identity | anonymous approve | reviewer id + comments persisted (`:115-122`); audit trail via mutation contract | covered |
| DoS — elevation flooding | unauthenticated request spam | request path is an authenticated mutation; batch/throttle layers apply (see `09-security-architecture.md` §cache/throttle) | covered |

## 4. Family 3 — ABAC overlay (evaluation at resolution)

| Threat | Vector | History | Current control | Verdict |
|---|---|---|---|---|
| E — overlay failure silently permits | ABAC store outage → overlay skipped | **was the headline fail-open** (carried from #1110 docs, gap A2) | still permissive **for the ABAC layer**: evaluator failure logs and contributes nothing (`Services/AbacPolicyEvaluationExtension.cs:70-76`, "no ABAC contributions are applied") | **open fail-open (layer-scoped)** — a tenant whose denies live ONLY in ABAC loses them during an ABAC store outage. Compensating controls: conditional overlay (family 4) now enforces independently, and DENY-WINS means one surviving deny layer still subtracts |
| E — allow-overlay minting permissions | ABAC `Permit` treated as access grant | — | allow-effect ABAC policies **never mint** — only deny subtracts (`AbacPolicyEvaluationExtension.cs:81-90` context-wide deny is fail-closed) | covered |
| T — policy tampering at rest | direct table edits | — | version snapshots + signed bundles (family 5) + mutation-contract writes | covered |
| I — evaluation decisions unlogged | silent allow/deny | — | `PermissionEvaluationLogEntry` (`Entities/PermissionEvaluationLogEntry.cs`) | covered |

## 5. Family 4 — Conditional policies (draft → sign → approve → deploy → rollback; resolution-time veto)

The family #1230 was cut for.

| Threat | Vector | History | Current control (mitigation, PR #1230) | Verdict |
|---|---|---|---|---|
| E — deployed conditional DENY never enforced | evaluator existed but was never registered at resolution (gap G2) | **real until #1230** | `ConditionalPolicyEvaluationExtension` registered as `IPermissionEvaluationExtension` (`Extensions/AuthorizationModuleExtensions.cs:485`), `Order` 400 — runs before the ABAC overlay so ABAC stays the last veto (`Services/ConditionalPolicyEvaluationExtension.cs:42-43`) | **fixed in #1230** |
| E — evaluation failure silently permits | overlay exception → skip | fail-open at the resolver layer (still true, `Services/EffectivePermissionResolverService.cs:267-296`) | the extension catches its own failures and applies a **fail-closed guard**: with `PermissionEngine:ConditionalPolicy:FailClosedOnEvaluationError` (default `true`) it denies every permission of the resolution (`ConditionalPolicyEvaluationExtension.cs:66-80`; option `Abstractions/PermissionEngineOptions.cs:63-69`) | **fixed in #1230** (opt-out remains an explicit, logged configuration) |
| E — "no policy applies" defaults to allow | default-allow evaluation (gap G3) | documented acceptable default | unchanged by design: zero-policy tenants contribute nothing (`Services/ConditionalPolicyEvaluator.cs:183`) — acceptable because it is the *absence of policy*, not a failure; a tenant that wants deny-by-default deploys an unconstrained deny | **accepted (documented)**, not a failure mode |
| T — unsigned policy deployed | MITM/editorial tampering | — | bundles must be signed before deployment (`Commands/PolicyBundleCommands.cs` + `IPolicyBundleSignatureService`); `PolicyBundleStatus.Revoked` is checked but has no writer (gap G6 open) | **partial — key revocation flow unimplemented** |
| E — request-time attributes bypass resolution-time narrowing | IP/device/geo/MFA conditions absent at resolution | — | documented narrowing: such policies remain enforced at request-time gates only (`ConditionalPolicyEvaluationExtension.cs:50-56` remarks) — the narrowing is explicit, not silent | accepted (documented) |
| **Tooling** — CodeQL flags `ConditionalPolicyEvaluator` | **the CodeQL alert on `Services/ConditionalPolicyEvaluator.cs` is OPEN at the time of writing** (full-source evidence lane) | the #1230 rewrite touched exactly this file (+123 lines) — the alert must be triaged against the current code, not the pre-#1230 shape | **OPEN — remediation pending** |

## 6. Family 5 — Policy bundles and version history (signed bundles)

| Threat | Vector | Current control | Verdict |
|---|---|---|---|
| T — deploy an unreviewed bundle | draft straight to active | draft → sign → approve → deploy lifecycle enforced in `Commands/PolicyBundleCommands.cs` (handler takes `IPolicyBundleSignatureService`, `:48-54`) | covered |
| T — rollback to a tampered version | history rewrite | `Entities/PolicyVersionHistory.cs` snapshots definitions per deploy | covered; **no retention bound** — unbounded append (gap G7 open, integrity-safe but a resource-exhaustion path) |
| E — revoked bundle still served | revocation unimplemented | `PolicyBundleStatus.Revoked` checked at serve time but nothing writes it (gap G6) | **open** |

## 7. Family 6 — Access review campaigns (create → items → decisions → remediation)

| Threat | Vector | History | Current control (mitigation, PR #1230) | Verdict |
|---|---|---|---|---|
| E — "revoke" decision removes nothing | status-flip-only revoke (gap D1) | remediation was a no-op | revoke now performs grant revocation + role removal + audit row, guarded by state-machine checks added to `Entities/AccessReview.cs` (+67 lines) and `Services/AccessReviewAnalyticsServices.cs` (+189) | **fixed in #1230** |
| E — decision tampering after the fact | approved↔revoked overwrite (gap D3) | no Pending-state guard | state guards added in `Entities/AccessReview.cs` (post-#1230 `AccessReviewStateMachineTests` pins them) | **fixed in #1230** |
| T — review without audit trail | no durable audit (gap D8) | nothing written | review decisions now audit via the sinks (see `cross-module-sequences.md` §2) | **fixed in #1230** |
| E — `AutoRevokeOnNoResponse` promises auto-revocation | dead flag (gap D2) | persisted but read by nothing | still dead — the flag remains a documented dead promise | **open** |

## 8. Family 7 — Orchestrated checks (`[AuthorizeRequest]` CQRS pipeline)

| Threat | Vector | Current control | Verdict |
|---|---|---|---|
| E — role/group-scoped ACL entries silently unmatched | resource-level check builds only `AclSubject.ForUser` (`Behaviors/AuthorizationBehavior.cs:134-137`, comment admits it) | none — fail-closed direction but silent | **open (gap G8)** |
| E — misnamed `ResourceId` property → ACL check skipped | reflection extraction without startup validation (`Behaviors/AuthorizationBehavior.cs:116-121,166-190`) | fails closed (no id → deny) but silently | **open (gap G9)** |
| E — requirement handler omitted for a declared permission | DI gap | fail-closed by the requirement chain (unhandled requirement → deny); G1 guard ratchets handler registrations in CI | covered |

## 9. Cross-cutting threats

| Threat | Vector | Control | Verdict |
|---|---|---|---|
| E — policy name valid in `Policies.All` but missing from the store | deployment drift | fail-closed synthesized deny (`Providers/DbAuthorizationPolicyProvider.cs:86-93,125`) | covered (deny) — but see gap G13 for the *dangling DB policy* inverse (silent static fallback, no signal) |
| E — stale cached permissions after revocation | L1/L2 cache lag | security-version bump + invalidation fan-out (`Services/JitElevationService.cs:280-303`, `Caching/CacheInvalidationService.cs`, `Caching/RedisPermissionCacheInvalidationPublisher.cs`); JIT approval bumps tenant version only (**gap G10 open**) | **partial** |
| E — cross-tenant policy bleed | tenant override merge bug | `PolicyMerger` + tenant-scoped cache keys + fail-closed tenant guards on the #1203 query handlers | covered |
| T — audit logs forged via the notification path | non-durable alerts | required audit goes through `RequiredModuleAuditRecorder` (in-transaction), not notifications | covered |
| S — ABAC/conditional management surface advertised but dead | client trusts generated SDK | 16 planned commands answer designed 422 + controllers hidden from OpenAPI, pinned by `apps/api/tests/GameGuild.API.UnitTests/Architecture/PlannedAuthorizationSurfaceGuardTests.cs` | covered (truthful surface) |

## 10. Threat-to-issue index

| Threat family | Issue | State |
|---|---|---|
| Conditional not enforced / fail-open / self-approval / revoke-no-remediation / role audit | **#1204** | **fixed by #1230** (this model documents the closed state) |
| ABAC stub middleware + hidden planned surface | **#1180** | open (S1 adds: the registration path itself is dead code) |
| ABAC overlay permissive-skip (layer fail-open) | gaps.md A2/H1 | open |
| JIT user-version gap | gaps.md G10 | open |
| Bundle revocation / retention | gaps.md G6/G7 | open |
| Reflection/resource-subject gaps | gaps.md G8/G9 | open |
| ConditionalPolicyEvaluator CodeQL alert | CodeQL (full-source evidence lane) | **OPEN** — triage against the post-#1230 file |
