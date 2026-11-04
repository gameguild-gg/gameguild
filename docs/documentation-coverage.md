# Documentation Coverage Tracker

> **Living document** — updated by every documentation PR. One row per module (contracts-only projects footnoted).
> Legend per column: ✅ complete · 🟡 partial (noted) · ❌ missing · n/a not applicable
> Score = artifacts delivered of 8 (fluxos, matriz endpoints, eventos, estados, sequências, STRIDE, DFDs, arquitetura).

## Coverage matrix

| # | Módulo | Fluxos usuário | Matriz endpoints | Eventos | Estados | Sequências | STRIDE | DFDs | Arquitetura | Score |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | Identity.Authentication | ✅ 22 | ✅ 250 endpoints | ✅ 26 tipos | ✅ 9 máquinas | ✅ 6 | ✅ 12 famílias | ✅ 17 | ✅ | **8/8** |
| 2 | Identity.Authorization | ✅ 7 | ✅ 177 endpoints | ✅ | ✅ 5 máquinas | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 3 | Identity.Users | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | 0/8 |
| 4 | Identity.Tenants | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | 0/8 |
| 5 | Identity.Provisioning | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | 0/8 |
| 6 | Identity.Context | n/a | n/a | n/a | n/a | n/a | n/a | n/a | 🟡 abstrações | 0.5/8 |
| 7 | Commerce.Payments | ✅ 6 | ✅ 22 ctrl/~120 endpoints | ✅ c/ Stripe webhooks | ✅ 4 | ✅ 5 | ❌ | ❌ | ❌ | 5/8 |
| 8 | Commerce.Billing | ✅ 2+ | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | 5/8 |
| 9 | Commerce.Orders | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | 5/8 |
| 10 | Commerce.Subscriptions | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | 5/8 |
| 11 | Commerce.Products | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | 0/8 |
| 12 | Compliance.Audit | ✅ | ✅ 38 tabelas | ✅ 3 sinks | ✅ | ✅ 7 famílias (K2 payments ausente) | ✅ 8 famílias F1-F8 | ✅ L0+8 L1 | ✅ | **8/8** |
| 13 | Compliance.Consent | ✅ | ✅ | ✅ | ✅ | ✅ (compliance/10 §4) | ✅ F5 + #1268 aberto | ✅ | ✅ | **8/8** (N1/N2 residuais — #1268 pendente) |
| 14 | Compliance.FERPA | ✅ | ✅ | ✅ | ✅ | ✅ (compliance/10 §5) | ✅ F6 | ✅ + examinador FERPA | ✅ | **8/8** (N3/N4/N5/N6 residuais) |
| 15 | Compliance.KYC | ✅ | ✅ | ✅ | ✅ | ✅ (compliance/10 §6) | ✅ F7+F8 | ✅ + examinador AML | ✅ | **8/8** (N7/N9/N10/N11 residuais) |
| 16 | Compliance.FinancialCrime | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | 0/8 |
| 17 | Finance.Economy (7 subs) | ✅ | ✅ 108 tabelas | ✅ 5 outboxes | ✅ | ✅ (economy/05) | ✅ 9 famílias E1-E9 | ✅ L0+9 L1 | ✅ | **8/8** (N1-N10 residuais no 07-gaps) |
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
| Módulos com package completo (8/8) | **6** (Authentication + Compliance.Audit/Consent/FERPA/KYC + Finance.Economy — commerce money-path em entrega paralela) |
| Módulos ≥4/8 (kit funcional, faltam STRIDE/DFD/arq) | **29** |
| Módulos 0/8 (sem nada) | **15** (+2 contracts-only n/a) |
| Endpoints com matriz de wiring | **~1.470** de ~2.115 gerados |
| Fluxos de usuário documentados | **~60** |
| Máquinas de estado | **~30** |
| STRIDE | auth (12 fluxos) + compliance (F1-F8) + economy (E1-E9) — commerce money-path em entrega paralela |
| DFDs | 17 (auth) + compliance (L0 + 8 famílias) + economy (L0 + 9 famílias) |
| Sequências cross-module | auth + economy + compliance (audit-sink fan-in, KYC gates, FERPA-Learning) |

## Próximas ondas (ordem de risco)

1. **Payments + Billing/Orders/Subscriptions**: STRIDE + DFDs + arquitetura (dinheiro sem análise de ameaças) — em entrega paralela (docs/money-security-package)
2. ~~**Compliance (4) + Economy**: STRIDE regulatório + DFDs + arquitetura + sequências do compliance~~ **ENTREGUE** — compliance/08-11 + economy/08-10; K4 economy (mint reversal) confirmado FIXED pós-#1266; N1 compliance re-verificado VIVO (PR #1268 aberto)
3. **Authorization + Learning + Social/Platform**: STRIDE + DFDs + arquitetura + sequências (3 famílias)
4. **Módulos 0/8 — hot-path primeiro**: Identity.Users + Tenants + Provisioning → Notifications + TrustSafety → FinancialCrime (AML) → Products + Content + Ledgers/Achievements + Mentorship/Ratings
