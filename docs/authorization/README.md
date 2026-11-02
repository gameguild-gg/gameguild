# GameGuild.Identity.Authorization — Documentation Package

> The permission/authorization module audit package · Documented against `develop` commit `74e9bba4c`.
> Conventions follow the authentication module's merged docs (`docs/auth/flows/*`, `docs/security/*`, `docs/auth/event-flows.md`, `docs/auth/state-machines.md`).

## Scope

`GameGuild.Identity.Authorization` (the engine: 19 controllers in 16 files, 118 endpoints) plus its authorization-surface controllers that physically live in `GameGuild.Identity.Authentication` (`AbacPolicyController`, `ConditionalPolicyCrudController`, `ConditionalPolicyEvaluationController`, `PermissionEvaluationController`, `RolesController` — 59 endpoints) — **24 controllers, 177 endpoints total**. The "~110 endpoints / 11 controllers" framing used when this package was commissioned undercounted the surface (#1180 counted the hidden policy controllers only).

## Contents

| Document | What it covers |
|---|---|
| [`flows/abac-policy-crud-evaluation-flow.md`](flows/abac-policy-crud-evaluation-flow.md) | ABAC policy management + evaluation attempt; the 15-of-18-dead-endpoint reality; the #1180 stub honestly documented |
| [`flows/conditional-policy-crud-evaluation-flow.md`](flows/conditional-policy-crud-evaluation-flow.md) | Conditional policy CRUD (dead) vs the working signed-bundle path (draft→sign→approve→deploy→rollback); version snapshots |
| [`flows/permission-grant-lifecycle-flow.md`](flows/permission-grant-lifecycle-flow.md) | Grant → deny → expire (lazy + worker + reminders) → revoke → extend; JIT elevation escalation |
| [`flows/roles-lifecycle-flow.md`](flows/roles-lifecycle-flow.md) | Role CRUD, single vs bulk assignment, removal, expiry, inheritance cycle guards, deletion-impact analysis |
| [`flows/access-review-campaign-flow.md`](flows/access-review-campaign-flow.md) | Campaign create→start→items→decisions→reminders→complete/cancel/expire; the remediation-is-a-status-flip finding |
| [`flows/permission-evaluation-decision-path-flow.md`](flows/permission-evaluation-decision-path-flow.md) | The four decision doors (policy provider + PermissionHandler, DAC filter, query surface, ruleset gates) + caching/throttle/audit layers |
| [`flows/orchestrated-permission-checks-flow.md`](flows/orchestrated-permission-checks-flow.md) | `AuthorizationBehavior` CQRS pipeline over `[AuthorizeRequest]` (112 platform declarations) + requirement handlers + rule evaluator chain |
| [`cqrs-wiring-matrix.md`](cqrs-wiring-matrix.md) | Every endpoint × command/query × validator × handler × auth attribute × rate limit × unit test × integration test × HOLE |
| [`state-machines.md`](state-machines.md) | stateDiagram-v2 for permission grant, JIT elevation, access-review campaign + item, conditional policy version (+bundle family), role assignment — with transitions audits |
| [`event-flows.md`](event-flows.md) | Every event/notification/webhook/audit stream with producer/consumer file:line and outbox durability |
| [`gaps.md`](gaps.md) | Consolidated gap register: already-filed (#1180) vs NEW, each with file:line, plus a suggested issue split |

## Headline findings

1. **31 of 177 endpoints are dead** — 16 answer 422 "planned authorization management API is not available" via the unavailable-contract mechanism, 15 answer 500 because their queries never got handlers at all. Mapping-only controller tests (mocked `IMediator`) keep CI green through all of it. (`gaps.md` B1-B3)
2. **Two more header-writing stub middlewares** besides #1180's `AbacPolicyMiddleware`: `AccessReviewMiddleware` and `PermissionCachingMiddleware` append diagnostics headers and do nothing else — both are registered in the live pipeline. (C1-C2)
3. **Access-review "revoke" revokes nothing** — it flips item status; the subject keeps every permission. `AutoRevokeOnNoResponse` is a dead flag. (D1-D2)
4. **Conditional policies are never enforced at resolution time** — the evaluator exists but is not registered as a resolution extension; only ABAC overlays decisions. (G2)
5. **Alerting/durability gaps**: orphan `AccessReviewReminderNotification`, best-effort post-commit expiration alerts, fire-and-forget revocation webhooks. (E1-E3)
6. **Role lifecycle asymmetries**: single assign/remove skip token-version bumps and audit rows that the bulk path performs. (E4, F1-F4)

## How to read the diagrams

All Mermaid diagrams are `flowchart TD` / `stateDiagram-v2` with no parentheses or colons in labels; `file:line` references use the `File.cs L123` form inside labels and `File.cs:123` in prose/tables. Paths are relative to `apps/api/Source/Modules/GameGuild.Identity.Authorization/` unless stated otherwise.
