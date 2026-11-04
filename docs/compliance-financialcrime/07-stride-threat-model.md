# FinancialCrime (AML) — STRIDE threat model

> Module `GameGuild.Compliance.FinancialCrime` · Documented against `develop` commit `60100459e6`.
> Regulatory lens: the **AML examiner / FIU** and the **screening provider** are explicit external parties. For the examiner, *completeness* (every case dispositioned, versions contiguous) and *tamper-evidence* (hash chains on decisions/evidence) are not just security properties — they are examination findings waiting to happen. A missing pipe is itself a finding.

Families (aligned with `01-user-flows.md`):

- **F1** Screening ingestion (designed; ingress unwired — FC1)
- **F2** Transaction monitoring signals (designed; no producer — FC1)
- **F3** AML case management (live admin surface)
- **F4** Decisions & their consumption
- **F5** The financial-crime evidence ledger (kind `financial-crime`)
- **F6** Money-path gates consuming the evidence (risk issuer, conversion verifier, payout coordinator)

## F1 — Screening ingestion

| # | Threat (STRIDE) | Scenario | Mitigation (file:line) or residual |
|---|---|---|---|
| 1.1 | Spoofing | Provider identity forged to inject clean screenings for a sanctioned subject | Envelope requires `SignatureVerified=true` (`FC:/PostgreSqlFinancialCrimeControlPlane.cs:508-512` rejects unsigned) — **residual**: signature verification happens *upstream* (the connector is trusted to have verified); the control plane trusts the boolean. With no connector (FC1) this is a design-time note. |
| 1.2 | Tampering | Replay of a provider event id with a different payload | payload-hash conflict (`:99-102`) → `FinancialCrimeConflictException` |
| 1.3 | Tampering | Version-gap injection (post a v5 then v3 to shadow history) | contiguous-version guard + unique `(TenantId,SubjectHash,Version)` index (`:107-113`, `FC:/FinancialCrimePersistence.cs:130-131`) |
| 1.4 | Repudiation | Provider denies a screening was delivered | `(Provider,Environment,ProviderEventId)` unique row + `ReceivedAt` + evidence envelope with hash (`:128-129`, `:126`) |
| 1.5 | Information disclosure | Screening rows leak subject PII | `SubjectHash` only (≤128 chars, `FC:/FinancialCrimePersistence.cs:124`); no name/DOB/address columns exist — hashing enforced by schema |
| 1.6 | DoS | Flood ingestions to exhaust Serializable transactions | **residual**: no rate limit on any future ingress; each call takes `Serializable` (`:91-92`). Also *availability of the control itself*: with no caller, the DoS already happened (FC1) |
| 1.7 | Elevation | Non-provider source injects screenings | **residual**: once a connector exists, it must carry its own auth; today nothing exists — nothing to elevate through |

## F2 — Transaction monitoring signals

| # | Threat | Scenario | Mitigation or residual |
|---|---|---|---|
| 2.1 | Spoofing | Signals fabricated to hold-out a competitor's users | idempotency-key hash + input-match replay (`:142-158`); **residual**: no producer auth story yet (FC1) — the design assumes a trusted internal producer |
| 2.2 | Tampering | Score inflation/deflation after the fact | rows append-only through the API surface; score bounds check constraint (`FC:/FinancialCrimePersistence.cs:139`) |
| 2.3 | Repudiation | "That hold wasn't caused by a signal" | signal row carries `OperationFingerprint` + `EvidenceHash` + `RequestHash` (`:159-165`); case `Opened` event carries the evidence hash (`:417`) |
| 2.4 | DoS | Signal storm auto-opens cases | active-case reuse (`:402-407`) caps one case per subject — storm cannot multiply holds per subject; **residual** across many synthetic subject hashes |
| 2.5 | Info disclosure | Score/evidence rows mined for behavior patterns | subject + fingerprints hashed; tenant-scoped reads only (`API:/Core/Controllers/EconomyComplianceAdministrationController.cs:232-243`) |

## F3 — AML case management (live surface)

| # | Threat | Scenario | Mitigation or residual |
|---|---|---|---|
| 3.1 | Spoofing | Analyst credentials stolen | `[Authorize]` + `OperateCompliance` typed permission per request (`:53-65` etc. via `TryActor` `:232-243`); **residual**: no step-up on case decisions (FC2) — a stolen session can close cases |
| 3.2 | Tampering | Silent case mutation bypassing events | every mutation appends a `case_events` row with actor + hash inside the same transaction (`:194, :235, :417`); unique `(CaseId,Sequence)` (`FC:/FinancialCrimePersistence.cs:171`) |
| 3.3 | Repudiation | Analyst denies deciding | `DecidedBy` + assignee-must-decide guard (`:210`) + decision rows immutable |
| 3.4 | Info disclosure | Cross-tenant case enumeration | tenant-scoped reads (`ReadCasesAsync`/`ReadCaseDetailsAsync` filter `TenantId`, `:343-344, :370-372`); tenant comes from the actor, never the route |
| 3.5 | DoS | Unthrottled case reads (take=500) | **residual FC7** — no rate limit |
| 3.6 | Elevation | Single analyst self-assigns then self-approves | **residual FC2** — no two-person rule; compare step-up on hold releases in the same handler (`API:/Core/Commands/EconomyComplianceEndpointCommands.cs:79-90`) |
| 3.7 | Availability | Examiner cannot see the program | **residual FC1/FC4** — empty tables + no bulk export; the examiner-facing *completeness* property is structurally absent |

## F4 — Decisions & consumption

| # | Threat | Scenario | Mitigation or residual |
|---|---|---|---|
| 4.1 | Tampering | Decision edited after issue | immutable rows; corrections are new contiguous versions (`:215-224`) |
| 4.2 | Spoofing/Replay | One approval reused for many operations | single-use consumption: window + Approved-only + unique `DecisionId` consumption index (`:271-283`, `FC:/FinancialCrimePersistence.cs:194`) |
| 4.3 | Repudiation | "We never approved this release" | decision envelope published to the evidence ledger with hash + raw object reference (`:236-247`) |
| 4.4 | DoS | Burn the approval before the legitimate operation | consumption is first-wins; an attacker with API access could race — **residual** (low: requires `OperateCompliance`) |
| 4.5 | Availability | Consumption leg never wired | **residual FC1** — nothing calls `ConsumeDecisionAsync`; the integrity fence exists but the release flow that should burn it is dormant |

## F5 — The financial-crime evidence ledger

| # | Threat | Scenario | Mitigation or residual |
|---|---|---|---|
| 5.1 | Tampering | Evidence row edited to flip Deny→Allow | ledger owned by economy store (`Risk:/PostgreSqlComplianceEvidenceStore.cs`); reader demands `SignatureVerified && EvidenceHash` (`FC:/FinancialCrimeModule.cs:61`) — unadmissible otherwise |
| 5.2 | Spoofing | Forged `internal-financial-crime` envelope injected outside the control plane | store-side sequence enforcement: only `Published/Duplicate` accepted, else the whole decide transaction rolls back (`FC:/PostgreSqlFinancialCrimeControlPlane.cs:245-248`) — **residual**: direct DB writes bypass (deployment invariant: writer-function monopoly, economy package §6.1) |
| 5.3 | Repudiation | Evidence existence denied | durable rows + `economy.compliance-evidence.published.v1` outbox message (`Risk:/PostgreSqlComplianceEvidenceStore.cs:288-300`) — **residual**: the outbox has no consumer (K1/economy N2), so the *publication trail* beyond the row is unverified |
| 5.4 | Info disclosure | Evidence ledger joined back to identities | `SubjectHash` only; reader maps `EconomySubjectReference.ForUser(tenant, actor)` (`Risk:/HardToSoftConversionRiskEvidenceVerifier.cs:40-44`) — the join lives in calling context, not the ledger |
| 5.5 | Tampering | Cross-kind confusion (kyc-aml evidence presented as financial-crime) | kind is a column with three fixed values (`Risk:/PostgreSqlComplianceEvidenceStore.cs:119-123`) and the reader filters by kind (`FC:/FinancialCrimeModule.cs:51-52`) — **residual**: no cross-kind precedence rule (FC5); an examiner asking "which kind is authoritative" has no answer in code |

## F6 — Money-path gates (consumer side)

| # | Threat | Scenario | Mitigation or residual |
|---|---|---|---|
| 6.1 | Spoofing | Caller fakes the risk decision | decisions are server-issued rows with deterministic ids + replay binding (`Risk:/PostgreSqlEconomyProtectedOperationRiskDecisionIssuer.cs:115-126, 307-322`) |
| 6.2 | Tampering | Gate made fail-open | fail-closed by construction: absent/stale/unsigned → `ComplianceUnavailable`/`ComplianceStale` deny (`:82-87`); conversions throw on missing Allow (`Risk:/ExternalRiskEvidence.cs:67-71`) |
| 6.3 | Repudiation | Money moved with no AML trace | every decision writes `economy_risk_audit_evidence` rows incl. the financial-crime evidence hash (`:272-305`) — the *absence* of evidence is itself audited (`04-event-inventory.md` §4) |
| 6.4 | DoS | Evidence-check storms | `Serializable` per decision — **residual**: no caching of fresh evidence (every operation re-reads the ledger) |
| 6.5 | Elevation | Bypass the gate for a capability | gates are per-capability opt-in (`Risk:/EconomyRiskComposition.cs:36-64`) — **residual (policy)**: any capability *not* wired through the issuer (none known for value movement; the economy package documents the rail) would be an AML bypass; keep the writer-monopoly invariant |
| 6.6 | Availability | Legitimate commerce blocked | **structural (by FC1)**: enabling value movement denies everyone on the AML leg — deliberate fail-closed, but it must be a *decision*, not a surprise (`06-gaps.md` FC1 rollout trap) |

## Examiner-view summary

| Examiner question | Where the platform answers it today |
|---|---|
| Show me every AML case and its disposition | `ReadCasesAsync`/`ReadCaseDetailsAsync` — but the set is empty because nothing opens cases (FC1) |
| Prove decisions were not altered | immutable decision rows + evidence envelopes + case-event sequence (`03-state-machines.md` §3) |
| Prove a sanctioned match was actioned | not provable — the sanctions-match columns exist (`SanctionsMatch/PepMatch/AdverseMediaMatch`, `FC:/FinancialCrimePersistence.cs:16-18`) but nothing feeds them (FC1) |
| Show me your SAR filings | hash-only references, no export (FC4) — the narrative lives with the FIU by design, but the reference index is the platform's proof and it is unreachable in bulk |
| Who closed this case and under what authority | `DecidedBy` + reason code + policy version — no step-up evidence on the decision (FC2) |
| Why was this payment allowed | risk-decision audit rows — but on the AML leg the answer is always "no evidence" today (FC1) |
