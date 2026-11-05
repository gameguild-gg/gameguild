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
| 11 | Commerce.Products | ✅ 6 | ✅ 11 ctrl/92 endpoints (+ DI §0) | ✅ 45 contracts | ✅ 7 máquinas | ✅ 6 | ✅ 6 famílias C1-C6 (07) | ✅ L0+6 (08) | ✅ products/09 | **8/8** (P1-P9 residuais no 06-gaps) |
| 12 | Compliance.Audit | ✅ família 1 de 4 (01 compartilhado) | ✅ ~110 endpoints (matriz 149 total, 4 mód) | ✅ 3 sinks (AuthenticationAudit ~30 sites, ApiKey, RefreshToken) | ✅ 1 máquina (03 compartilhado) | ✅ 7 famílias (K2 payments ausente) | ✅ 8 famílias F1-F8 | ✅ L0+8 L1 | ✅ compliance/09 | **8/8** |
| 13 | Compliance.Consent | ✅ família 2 de 4 (01) | ✅ ~17 endpoints (02) | ✅ consent grant/withdraw + deadline events (04) | ✅ 1 máquina (03) | ✅ (compliance/10 §4) | ✅ F5 + #1268 aberto | ✅ L1 no 11-dfds | ✅ compliance/09 | **8/8** (N1/N2 residuais — #1268 pendente) |
| 14 | Compliance.FERPA | ✅ família 3 de 4 (01) | ✅ matriz compartilhada (02) | ✅ FERPA request events (04) | ✅ 1 máquina (03) | ✅ (compliance/10 §5) | ✅ F6 | ✅ L1 + examinador FERPA (11) | ✅ compliance/09 | **8/8** (N3/N4/N5/N6 residuais) |
| 15 | Compliance.KYC | ✅ família 4 de 4 (01) | ✅ matriz compartilhada (02) | ✅ KYC lifecycle events (04) | ✅ 1 máquina (03) | ✅ (compliance/10 §6) | ✅ F7+F8 | ✅ L1 + examinador AML (11) | ✅ compliance/09 | **8/8** (N7/N9/N10/N11 residuais) |
| 16 | Compliance.FinancialCrime | ✅ 5 (ingest FC1 designed-only) | ✅ 5 endpoints + 9 ops reconciled (02 §2) | ✅ evidence kind financial-crime | ✅ 5 máquinas | ✅ 5 | ✅ 6 famílias F1-F6 (examinador AML) | ✅ L0+6 (examinador) | ✅ financialcrime/09 | **8/8** (FC1-FC8 residuais no 06-gaps) |
| 17 | Finance.Economy (7 subs) | ✅ 11 famílias (01: wallet, payout, settlement, bounty, ad-reward, AI-credit, treasury) | ✅ 71 endpoints / 19 ctrl (02; N9: 19 unthrottled) | ✅ 5 outboxes + 10 streams (04) | ✅ 9 stateDiagrams (03) | ✅ (economy/05) | ✅ 9 famílias E1-E9 | ✅ L0+9 L1 (10) | ✅ economy/09 | **8/8** (N1-N10 residuais no 07-gaps) |
| 18 | Finance.Ledgers | ✅ 3 | ✅ 24 rotas + 20 dead types reconciliados (02 §3) | ✅ 1 durable + 12 dead | ✅ 4 máquinas (estados inalcançáveis mapeados) | ✅ 4 | ✅ 5 famílias F1-F5 | ✅ L0+4 | ✅ finance-ledgers/09 | **8/8** (L1-L8 residuais no 06-gaps) |
| 19 | Learning (12 subs) | ✅ 6 | ✅ 31 ctrl | ✅ | ✅ 5 | ✅ | ❌ | ❌ | ❌ | 5/8 |
| 20 | Social.Posts | ✅ 01 F1 (+C5 contadores) | ✅ 127 endpoints (família) | ✅ | ✅ 6 (família) | ✅ 10 (família) | ✅ C1 (07: injeção de conteúdo, verdade da moderação) | ✅ L1 C1 (08) | ✅ social-platform/09 | **8/8** |
| 21 | Social.Feed | ✅ 01 F2 | ✅ (02 família: 127 endpoints/9 sub) | ✅ (04 família) | ✅ (03 família: 6 máquinas) | ✅ 10 (família) | ✅ C2 (07: manipulação de feed, SP3/SP4) | ✅ L1 C2 (08) | ✅ 09 §3 (fronteira de ranking) | **8/8** |
| 22 | Social.Profiles | ✅ família (01 F2 leituras + 07 C6) | ✅ (02 família) | ✅ (04 família) | ✅ (03 família) | ✅ 10 (família) | ✅ C6 (07: exposição PII, **SP1**) | ✅ L1 C6 [PII] (08) | ✅ 09 | **8/8** |
| 23 | Social.Groups | ✅ 01 F3 | ✅ (02 família) | ✅ (04 família) | ✅ (03 família) | ✅ 10 (família) | ✅ C3 (07: takeover de grupo, N4) | ✅ L1 C3 (08) | ✅ 09 §4 (mapa de moderação) | **8/8** |
| 24 | Social.Follows | ✅ família (01 F2 exclusões) | ✅ (02 família) | ✅ (04 família) | ✅ (03 família) | ✅ 10 (família) | ✅ C2+C5 (07: evasão de block SP4) | ✅ L1 C2/C5 (08) | ✅ 09 | **8/8** |
| 25 | Social.Reactions | ✅ família (07 C5) | ✅ (02 família) | ✅ (04 família) | ✅ (03 família) | ✅ 10 (família) | ✅ C5 (07: identidade e verdade dos contadores) | ✅ L1 C5 (08) | ✅ 09 | **8/8** |
| 26 | Social.Blog | ✅ 01 F4 | ✅ (02 família) | ✅ (04 família) | ✅ (03 família) | ✅ 10 §1 (família) | ✅ C4 (07: injeção + gate de manifest) | ✅ L1 C4 (08) | ✅ 09 §2/§7 | **8/8** |
| 27 | Social.Announcements | ✅ família (01 F4 fan-out) | ✅ (02 família) | ✅ (04 família: bridge p/ Notifications) | ✅ (03 família) | ✅ 10 §1 (bridge c/ vereditos) | ✅ C4 (07: spam de anúncio, cap 50) | ✅ L1 C4 (08) | ✅ 09 §8 | **8/8** |
| 28 | Social.Assets-social | ✅ família (01 F1 gate) | ✅ (02 família) | ✅ (04 família) | ✅ (03 família) | ✅ 10 §5.1 (ready-gate) | ✅ C1/P1 (07: attach gate fail-closed) | ✅ L1 C1/P1 (08) | ✅ 09 §2 | **8/8** |
| 29 | Social.Mentorship | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | 0/8 |
| 30 | Social.Ratings | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | 0/8 |
| 31 | Platform.AI | ✅ 01 F9 | ✅ 383 endpoints (família) | ✅ | ✅ | ✅ 10 §2 (wallet c/ timeouts) | ✅ P5 (07: roubo de créditos, N12/SP5) | ✅ L1 P5 (08) | ✅ 09 §7 (fronteira do wallet) | **8/8** |
| 32 | Platform.Analytics | ✅ família (07 P3 §10) | ✅ (02 família: 383 endpoints/14 sub) | ✅ (04 família) | ✅ (03 família) | ✅ 10 (família) | ✅ P3 (07: **N10 cross-tenant**) | ✅ L1 P3 (08 §10) | ✅ 09 §6 (varredura de classe TB7) | **8/8** |
| 33 | Platform.Assets | ✅ 01 F5 (upload→moderação) | ✅ (02 família) | ✅ (04 família) | ✅ (03 família: asset moderation) | ✅ 10 §3-§5 (Openinary, ClamAV, tokens) | ✅ P1 (07: malware de asset, SP2) | ✅ L1 P1 (08 §8) | ✅ 09 §2/§5 | **8/8** |
| 34 | Platform.Features | ✅ família (07 §13) | ✅ (02 família) | ✅ (04 família) | ✅ (03 família) | ✅ 10 (família) | ✅ família (07 §13 cross-cutting) | ✅ L0 (08 §1) | ✅ 09 §4 (switches) | **8/8** |
| 35 | Platform.GameJams | ✅ 01 F6 (jam lifecycle) | ✅ (02 família) | ✅ (04 família) | ✅ (03 família: jam) | ✅ 10 (família) | ✅ P2 (07: manipulação de scoring, N5/N6) | ✅ L1 P2 (08 §9) | ✅ 09 §4 | **8/8** |
| 36 | Platform.Projects | ✅ 01 F7 (project lifecycle) | ✅ (02 família) | ✅ (04 família) | ✅ (03 família: project+version) | ✅ 10 §1 (announce ao publicar) | ✅ P3 (07: exfiltração, N10/N14) | ✅ L1 P3 (08 §10) | ✅ 09 §6 | **8/8** |
| 37 | Platform.ProjectWork | ✅ família (07 §13 entidade) | ✅ (02 família) | ✅ (04 família) | ✅ (03 família) | ✅ 10 (família) | ✅ família (07 §13; P3) | ✅ L1 P3 (08 §10) | ✅ 09 §1 | **8/8** |
| 38 | Platform.TestingLab | ✅ 01 F8 (session) | ✅ (02 família) | ✅ (04 família) | ✅ (03 família: event/session) | ✅ 10 (família) | ✅ P4 (07: abuso de sessão) | ✅ L1 P4 (08 §11) | ✅ 09 §4 | **8/8** |
| 39 | Platform.LaunchPad | ✅ família (07 P3 §10) | ✅ (02 família) | ✅ (04 família) | ✅ (03 família) | ✅ 10 (família) | ✅ família (07 §13; P3 participante) | ✅ L1 P3 (08 §10) | ✅ 09 §1 | **8/8** |
| 40 | Platform.Monitoring.SLA | ✅ família (07 §13) | ✅ (02 família) | ✅ (04 família) | ✅ (03 família) | ✅ 10 (família) | ✅ família (07 §13 cross-cutting) | ✅ L0 (08 §1) | ✅ 09 | **8/8** |
| 41 | Platform.Localization | ✅ família (07 §13) | ✅ (02 família) | ✅ (04 família) | ✅ (03 família) | ✅ 10 (família) | ✅ família (07 §13 cross-cutting) | ✅ L0 (08 §1) | ✅ 09 | **8/8** |
| 42 | Platform.Teams | ✅ família (07 §13) | ✅ (02 família) | ✅ (04 família) | ✅ (03 família) | ✅ 10 (família) | ✅ família (07 §13 cross-cutting) | ✅ L0 (08 §1) | ✅ 09 | **8/8** |
| 43 | Platform.Tags | ✅ família (07 §13) | ✅ (02 família) | ✅ (04 família) | ✅ (03 família) | ✅ 10 (família) | ✅ família (07 §13 cross-cutting) | ✅ L0 (08 §1) | ✅ 09 | **8/8** |
| 44 | Platform.Resources | ✅ família (07 §13) | ✅ (02 família) | ✅ (04 família) | ✅ (03 família) | ✅ 10 §2 (quota enforcer) | ✅ família (07 §13; P5 quota) | ✅ L1 P5 (08 §12) | ✅ 09 §7 | **8/8** |
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
| Módulos com package completo (8/8) | **40** (Authentication + Authorization + Users + Tenants + Provisioning + Payments + Billing + Orders + Subscriptions + Products + Compliance.Audit/Consent/FERPA/KYC/FinancialCrime + Finance.Economy + Finance.Ledgers + **Social 9 + Platform 14 — package família docs/social-platform/**) |
| Módulos ≥4/8 (kit funcional, faltam STRIDE/DFD/arq) | **1** (Learning 5/8 — recálculo pós-promoção das 23 linhas Social+Platform a 8/8) |
| Módulos 0/8 (sem nada) | **9** (+2 contracts-only n/a) |
| Endpoints com matriz de wiring | **~2.400** (base ~1.470 + 177 authz + ~125 users/tenants/provisioning + 92 products + 24 ledgers + 5 financial-crime + **510 social+platform**; o teto de 2.115 refere-se ao contrato OpenAPI do client — docs/architecture/api-contract-matrix.md — as matrizes cobrem a superfície de rotas do servidor) |
| Fluxos de usuário documentados | **~89** (+identity: 2 famílias cada de users/tenants/provisioning; +products 6, +financialcrime 5, +ledgers 3, **+social+platform 9 famílias críticas em docs/social-platform/01**) |
| Máquinas de estado | **~60** (+3 users, +3 tenants, +2 provisioning; +products 7, +financialcrime 5, +ledgers 4, **+6 social+platform**) |
| STRIDE | auth (12 fluxos) + commerce (23 famílias — payments 8 + billing/orders/subs 9 + products 6) + compliance (14 famílias — F1-F8 + FinancialCrime F1-F6 c/ examinador AML) + economy (E1-E9) + identity (authz 7 famílias + users + tenants + provisioning) + finance-ledgers (5 famílias) + **social+platform (12 famílias C1-C6 + P1-P5, docs/social-platform/07 — achaços SP1-SP5 além do #1250)** |
| DFDs | 17 (auth) + 23 (commerce — nível 0 + nível 1 por família, +products 6) + compliance (L0 + 14 famílias, +FinancialCrime 6 c/ examinador AML) + economy (L0 + 9) + 10 identity (authz L0+7×L1; users/tenants/provisioning L0+3×L1 cada) + finance-ledgers (L0 + 4) + **social+platform (L0 + 12 L1, fronteiras TB1-TB7, fluxos [PII] marcados — docs/social-platform/08)** |
| Sequências cross-module | auth + economy + compliance (audit-sink fan-in, KYC gates, FERPA-Learning) + authz cross-module-sequences (+ sets lite de users/tenants/provisioning) + commerce/products (checkout, promo-coordinator, fraud-ticket, price-truth pós-#1266) + finance-ledgers (outbox, fronteira Economy) + financialcrime (risk gates, 3 ledgers) + **social+platform (announcements bridge, AI credits, Openinary/ClamAV/tokens — inventário de timeouts e vereditos fail-open/closed, docs/social-platform/10)** |

## Próximas ondas (ordem de risco)

1. ~~**Payments + Billing/Orders/Subscriptions**: STRIDE + DFDs + arquitetura~~ — **entregue** (docs/commerce/payments/07-08, docs/commerce/billing-orders/07-08, docs/commerce/09-security-architecture; verificação pós-#1266/#1229 incluída nos docs 07)
2. ~~**Authorization completion + Identity.Users/Tenants/Provisioning + auth spot-audit**~~ — **entregue** (docs/authorization/ cross-module-sequences + 07/08/09; docs/identity/{users,tenants,provisioning}/ completos + docs/identity/context.md; spot-audit 29 citações em docs/auth/spot-audit-2026-11.md — 0 afirmações falsas). Achados críticos abertos: takeover de tenants (4 controllers sem política, docs/identity/tenants/gaps.md T1), permissão self nunca semeada (users 403, docs/identity/users/gaps.md U5), eventos AddDomainEvent descartados (tenants T8, repo-wide)
3. ~~**Compliance (4) + Economy**: STRIDE regulatório + DFDs + arquitetura + sequências do compliance~~ **ENTREGUE** — compliance/08-11 + economy/08-10; K4 economy (mint reversal) confirmado FIXED pós-#1266; N1 compliance re-verificado VIVO (PR #1268 aberto)
4. ~~**Learning + Social/Platform**: STRIDE + DFDs + arquitetura + sequências (3 famílias)~~ — **Social+Platform ENTREGUE** (docs/social-platform/07-10 + 09; linhas 20-28 + 31-44 a 8/8; novos achaços SP1-SP5 além do #1250 — destaque SP1: leitura anônima por handle ignora ProfileVisibility). **Learning permanece 5/8** (STRIDE/DFD/arquitetura pendentes — fora do pacote social+platform)
5. **Módulos 0/8 — hot-path primeiro**: Notifications + TrustSafety → ~~FinancialCrime (AML)~~ → ~~Products~~ + Content + ~~Ledgers~~/Achievements + Mentorship/Ratings — **FinancialCrime (docs/compliance-financialcrime/), Products (docs/commerce/products/) e Finance.Ledgers (docs/finance-ledgers/) entregues** (rows 16, 11, 18 — 8/8 cada; reconciliação dos 20 dead request types do Ledgers em finance-ledgers/02 §3; FC1 ingestão AML sem caller, P1 AddProductsModule nunca invocado)
