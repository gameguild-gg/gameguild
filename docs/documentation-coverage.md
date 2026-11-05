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
| 20 | Social.Posts | 🟡 críticos | ✅ 127 endpoints (família) | ✅ | ✅ 6 (família) | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 21 | Social.Feed | 🟡 críticos (família) | ✅ (02 família: 127 endpoints/9 sub) | ✅ (04 família) | ✅ (03 família: 6 máquinas) | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 22 | Social.Profiles | 🟡 críticos (família) | ✅ (02 família) | ✅ (04 família) | ✅ (03 família) | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 23 | Social.Groups | 🟡 críticos (família) | ✅ (02 família) | ✅ (04 família) | ✅ (03 família) | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 24 | Social.Follows | 🟡 críticos (família) | ✅ (02 família) | ✅ (04 família) | ✅ (03 família) | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 25 | Social.Reactions | 🟡 críticos (família) | ✅ (02 família) | ✅ (04 família) | ✅ (03 família) | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 26 | Social.Blog | 🟡 críticos (família) | ✅ (02 família) | ✅ (04 família) | ✅ (03 família) | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 27 | Social.Announcements | 🟡 críticos (família) | ✅ (02 família) | ✅ (04 família: bridge p/ Notifications) | ✅ (03 família) | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 28 | Social.Assets-social | 🟡 críticos (família) | ✅ (02 família) | ✅ (04 família) | ✅ (03 família) | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 29 | Social.Mentorship | ✅ 5 (01: perfil, disponibilidade, booking, cancel/complete, diretório+dashboard) | ✅ 13 rotas / 3 ctrl (02; anon registry ×3) | ✅ 7 op events (04; zero named) | ✅ 3 máquinas (03: profile, slot, session) | ✅ 5 (05, incl. fronteira #1159 creator-process) | ✅ 5 famílias (07) | ✅ L0+5 (08) | ✅ 09 | **8/8** (docs/social/mentorship/) |
| 30 | Social.Ratings | ✅ 5 (01: rate, helpful, report→flag, agregados, delete) | ✅ 21 rotas / 1 ctrl (02; 9 anônimas, 5 TenantAdmin) | ✅ 7 mortos + 11 op events (04) | ✅ 3 máquinas (03: moderação c/ Pending inalcançável, voto, summary) | ✅ 5 (05, incl. sequência da falha E9) | ✅ 5 famílias (07) | ✅ L0+4 (08) | ✅ 09 (anatomia E9 §4) | **8/8** (docs/social/ratings/; R1 = #1278 E9: 19 combos 500 — IsDeleted não traduzível) |
| 31 | Platform.AI | 🟡 críticos | ✅ 383 endpoints (família) | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 32 | Platform.Analytics | 🟡 críticos (família) | ✅ (02 família: 383 endpoints/14 sub) | ✅ (04 família) | ✅ (03 família) | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 33 | Platform.Assets | 🟡 críticos (família: upload→moderação) | ✅ (02 família) | ✅ (04 família) | ✅ (03 família: asset moderation) | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 34 | Platform.Features | 🟡 críticos (família) | ✅ (02 família) | ✅ (04 família) | ✅ (03 família) | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 35 | Platform.GameJams | 🟡 críticos (família: jam lifecycle) | ✅ (02 família) | ✅ (04 família) | ✅ (03 família: jam) | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 36 | Platform.Projects | 🟡 críticos (família: project lifecycle) | ✅ (02 família) | ✅ (04 família) | ✅ (03 família: project+version) | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 37 | Platform.ProjectWork | 🟡 críticos (família) | ✅ (02 família) | ✅ (04 família) | ✅ (03 família) | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 38 | Platform.TestingLab | 🟡 críticos (família: session) | ✅ (02 família) | ✅ (04 família) | ✅ (03 família: event/session) | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 39 | Platform.LaunchPad | 🟡 críticos (família) | ✅ (02 família) | ✅ (04 família) | ✅ (03 família) | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 40 | Platform.Monitoring.SLA | 🟡 críticos (família) | ✅ (02 família) | ✅ (04 família) | ✅ (03 família) | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 41 | Platform.Localization | 🟡 críticos (família) | ✅ (02 família) | ✅ (04 família) | ✅ (03 família) | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 42 | Platform.Teams | 🟡 críticos (família) | ✅ (02 família) | ✅ (04 família) | ✅ (03 família) | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 43 | Platform.Tags | 🟡 críticos (família) | ✅ (02 família) | ✅ (04 família) | ✅ (03 família) | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 44 | Platform.Resources | 🟡 críticos (família) | ✅ (02 família) | ✅ (04 família) | ✅ (03 família) | ❌ | ❌ | ❌ | ❌ | 4/8 |
| 45 | Content.Pages | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | 0/8 |
| 46 | Resources.Contents | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | 0/8 |
| 47 | Gamification.Achievements | ✅ 5 (01: CRUD def., award manual, leituras, handshake unnotified, progresso-morto) | ✅ 10 rotas / 1 ctrl (02; §3 reconcilia dead-surfaces — não repete a classe Ledgers) | ✅ 8 mortos + 5 op events (04) | ✅ 4 (03: definição, award, progresso inalcançável, invariantes IsNotified) | ✅ 4 (05: award, poll-loop, ponte ausente p/ Notifications, tenant) | ✅ 4 famílias (07: forgery/spoofing) | ✅ L0+4 (08) | ✅ 09 | **8/8** (docs/gamification/; G1 progresso sem caller, G2 permissões não semeadas) |
| 48 | Notifications | ✅ 6 (01: in-app, preferências, unsubscribe, console deliverability, broadcast WhatsApp, consumidor) | ✅ 30 rotas / 5 ctrl (02; +1 outsider em Commerce.Subscriptions N7) | ✅ censo produtores ×13 + renderers 15/43 (04) | ✅ 5 (03: fila-entrega c/ Failed-morto, broadcast, supressão, cadeia de decisão) | ✅ 6 (05: producer→renderer→canal, webhook SNS, digest, saga WhatsApp) | ✅ 6 famílias (07: injeção/bomba de inscrição) | ✅ L0+5 (08) | ✅ 09 (registry pattern, topologia workers, cadeia de preferências) | **8/8** (docs/notifications/; N1 gate legado, N2 28 tipos sem renderer) |
| 49 | TrustSafety | ✅ 5 (01: ingest-desenhado, apelações, reportes reais Assets/Ratings, classificação, zero-texto #1250-N1) | ✅ 3 rotas host-carregadas (02; §2 Ingest/SubmitAppeal = zero callers) | ✅ TrustSafetyEvent 8 kinds sem produtor (04) | ✅ 5 (03: inbox, subject-state, apelação, reporte, moderação) | ✅ 5 (05: consulta de risco Economy, fronteira vs FinancialCrime/Assets) | ✅ 5 famílias (07: bypass moderação, abuso de reporte) | ✅ L0+5 (08) | ✅ 09 | **8/8** (docs/trustsafety/; T1 ambas escritas sem caller, T3 console [Authorize]-only) |
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
| Módulos com package completo (8/8) | **22** (Authentication + Authorization + Users + Tenants + Provisioning + Payments + Billing + Orders + Subscriptions + Products + Compliance.Audit/Consent/FERPA/KYC/FinancialCrime + Finance.Economy + Finance.Ledgers + **Gamification.Achievements + Notifications + TrustSafety + Social.Mentorship + Social.Ratings**) |
| Módulos ≥4/8 (kit funcional, faltam STRIDE/DFD/arq) | **34** (+5 pacotes from-zero promovidos a 8/8) |
| Módulos 0/8 (sem nada) | **4** (+2 contracts-only n/a) (restam Content.Pages, Resources.Contents e os 2 contracts-only; os 5 antigos 0/8 das rows 29-30/47-49 agora 8/8) |
| Endpoints com matriz de wiring | **~1.967** de ~2.115 gerados (base ~1.470 + 177 authz + ~125 users/tenants/provisioning + 92 products + 24 ledgers + 5 financial-crime + **77 novos: 10 achievements + 30 notifications + 3 trustsafety (host) + 13 mentorship + 21 ratings**) |
| Fluxos de usuário documentados | **~106** (+identity: 2 famílias cada de users/tenants/provisioning; +products 6, +financialcrime 5, +ledgers 3; **+achievements 5, +notifications 6, +trustsafety 5, +mentorship 5, +ratings 5**) |
| Máquinas de estado | **~75** (+3 users, +3 tenants, +2 provisioning; +products 7, +financialcrime 5, +ledgers 4; **+achievements 4, +notifications 5, +trustsafety 5, +mentorship 3, +ratings 4**) |
| STRIDE | auth (12 fluxos) + commerce (23 famílias — payments 8 + billing/orders/subs 9 + products 6) + compliance (14 famílias — F1-F8 + FinancialCrime F1-F6 c/ examinador AML) + economy (E1-E9) + identity (authz 7 famílias + users + tenants + provisioning) + finance-ledgers (5 famílias) + **achievements (4 famílias — forgery/spoofing) + notifications (6 famílias — injeção de notificação, subscription bomb) + trustsafety (5 famílias — bypass de moderação, abuso de reporte) + mentorship (5 famílias) + ratings (5 famílias)** |
| DFDs | 17 (auth) + 23 (commerce — nível 0 + nível 1 por família, +products 6) + compliance (L0 + 14 famílias, +FinancialCrime 6 c/ examinador AML) + economy (L0 + 9) + 10 identity (authz L0+7×L1; users/tenants/provisioning L0+3×L1 cada) + finance-ledgers (L0 + 4) + **achievements (L0+4) + notifications (L0+5: producer→renderer→canal→usuário, webhook, unsubscribe, digest, broadcast) + trustsafety (L0+5) + mentorship (L0+5) + ratings (L0+4)** |
| Sequências cross-module | auth + economy + compliance (audit-sink fan-in, KYC gates, FERPA-Learning) + authz cross-module-sequences (+ sets lite de users/tenants/provisioning) + commerce/products (checkout, promo-coordinator, fraud-ticket, price-truth pós-#1266) + finance-ledgers (outbox, fronteira Economy) + financialcrime (risk gates, 3 ledgers) + **achievements (award, poll-loop, ponte Notifications ausente) + notifications (producer→outbox→renderer→canal, webhook SNS c/ supressão, digest, saga whatsAppEnabled #54/#55) + trustsafety (consulta de risco Economy, apelações, fronteira FinancialCrime/Assets) + mentorship (booking, cancel/release, fronteira #1159 creator-process) + ratings (rate→summary, E9 #1278)** |

## Próximas ondas (ordem de risco)

1. ~~**Payments + Billing/Orders/Subscriptions**: STRIDE + DFDs + arquitetura~~ — **entregue** (docs/commerce/payments/07-08, docs/commerce/billing-orders/07-08, docs/commerce/09-security-architecture; verificação pós-#1266/#1229 incluída nos docs 07)
2. ~~**Authorization completion + Identity.Users/Tenants/Provisioning + auth spot-audit**~~ — **entregue** (docs/authorization/ cross-module-sequences + 07/08/09; docs/identity/{users,tenants,provisioning}/ completos + docs/identity/context.md; spot-audit 29 citações em docs/auth/spot-audit-2026-11.md — 0 afirmações falsas). Achados críticos abertos: takeover de tenants (4 controllers sem política, docs/identity/tenants/gaps.md T1), permissão self nunca semeada (users 403, docs/identity/users/gaps.md U5), eventos AddDomainEvent descartados (tenants T8, repo-wide)
3. ~~**Compliance (4) + Economy**: STRIDE regulatório + DFDs + arquitetura + sequências do compliance~~ **ENTREGUE** — compliance/08-11 + economy/08-10; K4 economy (mint reversal) confirmado FIXED pós-#1266; N1 compliance re-verificado VIVO (PR #1268 aberto)
4. **Learning + Social/Platform**: STRIDE + DFDs + arquitetura + sequências (3 famílias)
5. **Módulos 0/8 — hot-path primeiro**: ~~Notifications + TrustSafety → FinancialCrime (AML) → Products + Content + Ledgers/Achievements + Mentorship/Ratings~~ — **entregues**: FinancialCrime (docs/compliance-financialcrime/), Products (docs/commerce/products/), Finance.Ledgers (docs/finance-ledgers/) e o lote from-zero de 2026-11 **Gamification.Achievements + Notifications + TrustSafety + Social.Mentorship + Social.Ratings** (rows 47, 48, 49, 29, 30 — 8/8 cada). Achados críticos abertos desse lote: ratings E9 (#1278 — 19 rotas 500, IsDeleted não traduzível, docs/social/ratings/06 R1), achievements sem permissões semeadas (docs/gamification/06 G2), TrustSafety com ambas as escritas sem caller (docs/trustsafety/06 T1), text moderation zero confirmado #1250-N1. Restam 0/8: Content.Pages + Resources.Contents.
