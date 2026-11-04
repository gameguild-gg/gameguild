# Documentation Coverage Tracker

> **Living document** — updated by every documentation PR. One row per module (contracts-only projects footnoted).
> Legend per column: ✅ complete · 🟡 partial (noted) · ❌ missing · n/a not applicable
> Score = artifacts delivered of 8 (fluxos, matriz endpoints, eventos, estados, sequências, STRIDE, DFDs, arquitetura).

## Coverage matrix

| # | Módulo | Fluxos usuário | Matriz endpoints | Eventos | Estados | Sequências | STRIDE | DFDs | Arquitetura | Score |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | Identity.Authentication | ✅ 22 | ✅ 250 endpoints | ✅ 26 tipos | ✅ 9 máquinas | ✅ 6 | ✅ 12 famílias | ✅ 17 | ✅ | **8/8** (spot-audit 2026-11: 29 citações vs develop 7ae5a28a65 — 21 exatas, 8 com drift +N, 0 afirmação falsa; ver docs/auth/spot-audit-2026-11.md) |
| 2 | Identity.Authorization | ✅ 7 | ✅ 177 endpoints | ✅ | ✅ 5 máquinas | ✅ cross-module-sequences | ✅ 07-stride (7 famílias, pós-#1230) | ✅ 08-dfds (L0 + 7×L1) | ✅ 09-security-architecture | **8/8** |
| 3 | Identity.Users | ✅ 2 famílias c/ matrizes de borda | ✅ 53 endpoints (matriz completa × 11 tipos mortos G7) | ✅ 2 duráveis + 9 in-process (censos) | ✅ 3 máquinas | ✅ lite | ✅ 07-stride | ✅ 08-dfds | ✅ 09-architecture | **8/8** (docs/identity/users/) |
| 4 | Identity.Tenants | ✅ 2 famílias c/ matrizes de borda | ✅ 46 endpoints (T1: 4 controllers só `[Authorize]`) | ✅ censos (7 eventos no vazio + 3 mortos G4) | ✅ 3 máquinas | ✅ lite | ✅ 07-stride (takeover) | ✅ 08-dfds | ✅ 09-architecture | **8/8** (docs/identity/tenants/) |
| 5 | Identity.Provisioning | ✅ 2 famílias | ✅ 26 endpoints / 12 contratos | ✅ audit streams (Null-sink caveat) | ✅ 2 máquinas (token c/ grace) | ✅ lite | ✅ 07-stride | ✅ 08-dfds | ✅ 09-architecture | **8/8** (docs/identity/provisioning/) |
| 6 | Identity.Context | n/a | n/a | n/a | n/a | n/a | n/a | n/a | ✅ inventário de abstrações × implementações (docs/identity/context.md) | 1/8 (7×n/a) |
| 7 | Commerce.Payments | ✅ 6 | ✅ 22 ctrl/~120 endpoints | ✅ c/ Stripe webhooks | ✅ 4 | ✅ 5 | ✅ 8 famílias (07-stride) | ✅ 7 (08-dfds) | ✅ commerce/09 | **8/8** |
| 8 | Commerce.Billing | ✅ 2+ | ✅ | ✅ | ✅ | ✅ | ✅ 9 famílias (billing-orders/07) | ✅ 9 (billing-orders/08) | ✅ commerce/09 | **8/8** |
| 9 | Commerce.Orders | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ (billing-orders/07 §6-7 pós-#1229) | ✅ (billing-orders/08 F6-F7) | ✅ commerce/09 | **8/8** |
| 10 | Commerce.Subscriptions | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ (billing-orders/07 §1-5 pós-#1229) | ✅ (billing-orders/08 F1-F5) | ✅ commerce/09 | **8/8** |
| 11 | Commerce.Products | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | 0/8 |
| 12 | Compliance.Audit | ✅ | ✅ 38 tabelas | ✅ 3 sinks | ✅ | ❌ | 🟡 lite | ❌ | 🟡 | 4.5/8 |
| 13 | Compliance.Consent | ✅ | ✅ | ✅ | ✅ | ❌ | 🟡 lite | ❌ | 🟡 | 4.5/8 |
| 14 | Compliance.FERPA | ✅ | ✅ | ✅ | ✅ | ❌ | 🟡 lite | ❌ | 🟡 | 4.5/8 |
| 15 | Compliance.KYC | ✅ | ✅ | ✅ | ✅ | ❌ | 🟡 lite | ❌ | 🟡 | 4.5/8 |
| 16 | Compliance.FinancialCrime | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | 0/8 |
| 17 | Finance.Economy (7 subs) | ✅ | ✅ 108 tabelas | ✅ 5 outboxes | ✅ | ✅ | ❌ | ❌ | ❌ | 5/8 |
| 18 | Finance.Ledgers | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | 0/8 |
| 19 | Learning (12 subs) | ✅ 6 | ✅ 31 ctrl | ✅ | ✅ 5 | ✅ | ❌ | ❌ | ❌ | 5/8 |
| 20 | Social.Posts | 🟡 críticos | ✅ 127 endpoints (família) | ✅ | ✅ 6 (família) | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 21 | Social.Feed | 🟡 | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 22 | Social.Profiles | 🟡 | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 23 | Social.Groups | 🟡 | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 24 | Social.Follows | 🟡 | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 25 | Social.Reactions | 🟡 | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 26 | Social.Blog | 🟡 | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 27 | Social.Announcements | 🟡 | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 28 | Social.Assets-social | 🟡 | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 29 | Social.Mentorship | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | 0/8 |
| 30 | Social.Ratings | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | 0/8 |
| 31 | Platform.AI | 🟡 críticos | ✅ 383 endpoints (família) | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 32 | Platform.Analytics | 🟡 | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 33 | Platform.Assets | 🟡 | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 34 | Platform.Features | 🟡 | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 35 | Platform.GameJams | 🟡 | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 36 | Platform.Projects | 🟡 | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 37 | Platform.ProjectWork | 🟡 | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 38 | Platform.TestingLab | 🟡 | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 39 | Platform.LaunchPad | 🟡 | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 40 | Platform.Monitoring.SLA | 🟡 | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 41 | Platform.Localization | 🟡 | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 42 | Platform.Teams | 🟡 | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 43 | Platform.Tags | 🟡 | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 44 | Platform.Resources | 🟡 | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 45 | Content.Pages | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | 0/8 |
| 46 | Resources.Contents | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | 0/8 |
| 47 | Gamification.Achievements | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | 0/8 |
| 48 | Notifications | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | 0/8 |
| 49 | TrustSafety | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | 0/8 |
| 50 | SharedKernel + host core | n/a (anatomia) | n/a | ✅ outbox core | n/a | n/a | 🟡 primitives | ❌ | ✅ 6 docs | 3.5/8 |

**Footnote**: Finance.Contracts e Announcements.Contracts são projects de contratos sem endpoints/fluxo — cobertura n/a por natureza.

## Repo-wide (transversais)

| Documento | Estado | Onde |
|---|---|---|
| Inventário de background workers (47 serviços) | ✅ | docs/operations/background-workers.md |
| Contrato OpenAPI × client (2.115 endpoints, 19,9% usados) | ✅ | docs/architecture/api-contract-matrix.md |
| Veredito de homologação | ✅ | docs/release/READINESS-2026-11.md |

## Números consolidados

| Métrica | Valor |
|---|---|
| Módulos com package completo (8/8) | **9** (Authentication + Authorization + Users + Tenants + Provisioning + Payments + Billing + Orders + Subscriptions) |
| Módulos ≥4/8 (kit funcional, faltam STRIDE/DFD/arq) | **29** |
| Módulos 0/8 (sem nada) | **10** (+2 contracts-only n/a) |
| Endpoints com matriz de wiring | **~1.770** de ~2.115 gerados (+125 users/tenants/provisioning +177 authz re-matrizado) |
| Fluxos de usuário documentados | **~67** (+7 authz já contados; +2+2+2 famílias users/tenants/provisioning) |
| Máquinas de estado | **~36** (+3 users, +3 tenants, +2 provisioning, +1 context n/a) |
| STRIDE | auth + commerce + **identity (4 novos: authz 7 famílias + users + tenants + provisioning)** — 6 docs, 23 famílias |
| DFDs | 17 (auth) + 16 (commerce) + **10 identity (authz L0+7×L1; users/tenants/provisioning L0+3×L1 cada)** |

## Próximas ondas (ordem de risco)

1. ~~**Payments + Billing/Orders/Subscriptions**: STRIDE + DFDs + arquitetura~~ — **entregue** (docs/commerce/payments/07-08, docs/commerce/billing-orders/07-08, docs/commerce/09-security-architecture; verificação pós-#1266/#1229 incluída nos docs 07)
2. ~~**Authorization completion + Identity.Users/Tenants/Provisioning + auth spot-audit**~~ — **entregue** (docs/authorization/ cross-module-sequences + 07/08/09; docs/identity/{users,tenants,provisioning}/ completos + docs/identity/context.md; spot-audit 29 citações em docs/auth/spot-audit-2026-11.md — 0 afirmações falsas). Achados críticos abertos: takeover de tenants (4 controllers sem política, docs/identity/tenants/gaps.md T1), permissão self nunca semeada (users 403, docs/identity/users/gaps.md U5), eventos AddDomainEvent descartados (tenants T8, repo-wide)
3. **Compliance (4) + Economy**: STRIDE regulatório + DFDs + arquitetura + sequências do compliance
4. **Learning + Social/Platform**: STRIDE + DFDs + arquitetura + sequências (3 famílias)
5. **Módulos 0/8 — hot-path primeiro**: Notifications + TrustSafety → FinancialCrime (AML) → Products + Content + Ledgers/Achievements + Mentorship/Ratings
