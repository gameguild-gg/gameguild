# FinancialCrime (AML) — event and evidence inventory

> Module `GameGuild.Compliance.FinancialCrime` · Documented against `develop` commit `60100459e6`.
> Citation shorthand: `FC:` = `apps/api/Source/Modules/GameGuild.Compliance.FinancialCrime/`, `Risk:` = `apps/api/Source/Modules/GameGuild.Finance.Economy/Risk/`, `API:` = `apps/api/Source/GameGuild.API/`.

The module declares **no domain events, no MediatorR notifications, and no outbox of its own**. Its observable contract is two-shaped: (a) durable **evidence envelopes** published transactionally through the economy compliance-evidence store, and (b) the host's **generic durable operation events** on the three endpoint commands.

## 1. Evidence envelopes (kind `financial-crime`)

Producer: `PostgreSqlFinancialCrimeControlPlane` → `IComplianceEvidenceStore.IngestAsync`.

| Event | Producer call site | Payload of record | Transport | Consumers |
|---|---|---|---|---|
| Screening evidence | `IngestScreeningAsync` (`FC:/PostgreSqlFinancialCrimeControlPlane.cs:126`) — **no caller (FC1)** | provider, environment, providerEventId, tenant, subjectHash, version, result, policyVersion, payloadHash, signatureVerified, issued/expires, jurisdiction (`:444-450`) | `compliance_evidence` row + outbox message `economy.compliance-evidence.published.v1` (`Risk:/PostgreSqlComplianceEvidenceStore.cs:288-300`) | evidence-table read side (`ReadLatestAsync`) — **live**; the outbox fan-out has **NO consumer** (economy package gap N2, re-verified there) |
| Case-decision evidence | `DecideCaseAsync` (`:236-247`) — wired | provider `internal-financial-crime`, providerEventId `case-decision:{id:N}`, version = latest+1, decided result, evidenceHash, raw object reference (`:239-243`) | same | same read side; same N2 for the publish stream |

Properties that matter for the examiner:

- Envelope versions must be contiguous per `(tenant, subject, kind)` — the store rejects out-of-sequence ingestion (`FC:/PostgreSqlFinancialCrimeControlPlane.cs:127-130, 245-248` treat non-`Published/Duplicate` as conflict).
- `EvidenceKind = "financial-crime"` is one of exactly three kinds (`kyc-aml`, `financial-crime`, `trust-safety` — `Risk:/PostgreSqlComplianceEvidenceStore.cs:119-123`): the AML ledger is deliberately separate from KYC evidence; no cross-kind inference exists (see `05-cross-module-sequences.md` §5).
- Case events (`compliance_financial_crime_case_events`) are the internal audit trail: strictly increasing `Sequence` per case (unique index, `FC:/FinancialCrimePersistence.cs:171`), each row carrying kind/actor/reasonCode/evidenceHash (`FC:/PostgreSqlFinancialCrimeControlPlane.cs:435-442`). They are **not** published anywhere — examiner export does not exist (`06-gaps.md` FC4).

## 2. Generic durable operation events (host commands)

The three wired commands participate in the platform's use-case event pipeline (`UseCaseEventContractAttribute`):

| Command | Use case | Notes |
|---|---|---|
| `AssignFinancialCrimeCaseEndpointCommand` | — no dedicated contract in `API:/Architecture/EconomyControlPlaneUseCaseEventContracts.cs` | rides the generic durable operation event recorded by the CQRS pipeline |
| `DecideFinancialCrimeCaseEndpointCommand` | same | same |
| `RecordRegulatoryReferenceEndpointCommand` | same | same |

(Contrast: the economy control-plane file at `API:/Architecture/EconomyControlPlaneUseCaseEventContracts.cs:10` contracts `VerifyEconomyJournalEndpointCommand` — that is the economy journal verifier, unrelated to this module despite the "ledger" naming.)

## 3. What never arrives (the negative inventory)

| Expected AML stream | Why it is expected | Reality at `60100459e6` |
|---|---|---|
| Provider screening events (sanctions/PEP/adverse-media) | `IngestScreeningAsync` + `NextScreenAt` + `ReadDueRescreeningsAsync` machinery | **no webhook, no connector, no scheduler** — zero producers (FC1) |
| Transaction-monitoring signals | `RecordSignalAsync` + score window checks | **no producer** in Payments, Economy, or Billing (FC1) |
| Decision consumptions | `ConsumeDecisionAsync` + single-use index | **no consumer** at any value-movement site (FC1) |
| Rescreening sweeps | `ReadDueRescreeningsAsync` (limit 1..1000, `FC:/PostgreSqlFinancialCrimeControlPlane.cs:307-320`) | **no worker** — `NextScreenAt` is written (by the dormant ingest) and indexed, never read |
| Case-event / SAR exports for the examiner | `ReadCaseDetailsAsync` exists per case | no bulk export, no webhook, no report rendering (FC4) |

## 4. Audit logging (structured, best-effort)

The economy protected-operation path records every risk decision with the financial-crime evidence hash in `economy_risk_audit_evidence` rows (`Risk:/PostgreSqlEconomyProtectedOperationRiskDecisionIssuer.cs:272-305`, event kind `external-risk-evidence`) — so denials caused by missing AML evidence leave a durable trace with the `financial-crime-evidence-unavailable` evidence hash input. This is currently the **only** place the AML leg of a money decision is observable in production, because it logs the absence.

## 5. Retry / DLQ posture

| Stream | Attempts | Dead-letter | Replay |
|---|---|---|---|
| Evidence publish (inside ingest/decide transactions) | none needed — transactional; failure rolls back the whole operation | n/a | n/a |
| `economy.compliance-evidence.published.v1` outbox | n/a — **no consumer exists** (economy N2) | n/a | n/a |
| Generic operation events of the three wired commands | platform `OutboxDispatcher` 5-attempt / ≤900s backoff with `DeadLetteredAtUtc` + replay (`API:/Core/Eventing/OutboxDispatcher.cs`) | platform rail | platform rail |

Contrast (per the economy package): the Economy-internal outboxes run unbounded retries off the platform rail; the evidence envelope published here rides the transactional write but its fan-out leg is the documented N2 void — nothing retries because nothing consumes.
