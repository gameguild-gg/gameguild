# FinancialCrime (AML) — cross-module sequences

> Module `GameGuild.Compliance.FinancialCrime` · Documented against `develop` commit `60100459e6`.
> Citation shorthand: `FC:` = `apps/api/Source/Modules/GameGuild.Compliance.FinancialCrime/`, `Risk:` = `apps/api/Source/Modules/GameGuild.Finance.Economy/Risk/`, `Payouts:` = `apps/api/Source/Modules/GameGuild.Finance.Economy.Payouts/`, `API:` = `apps/api/Source/GameGuild.API/`, `Kyc:` = `apps/api/Source/Modules/GameGuild.Compliance.KYC/`.

## 1. Protected operation → risk decision reading AML evidence (LIVE)

```mermaid
sequenceDiagram
    participant Caller as Economy protected operation
    participant Issuer as PostgreSqlEconomyProtectedOperationRiskDecisionIssuer
    participant FCInput as PostgreSqlFinancialCrimeRiskInputSource
    participant EvRd as IComplianceEvidenceReader Postgres
    participant Assess as EconomyProtectedRiskDecisionIssuerSupport
    Caller->>Issuer: IssueAsync request intent subject jurisdiction
    Issuer->>Issuer: signed policy + covered reserve checks L59-96
    Issuer->>FCInput: ReadAsync tenantId subjectRef requestedAt L186-187
    FCInput->>EvRd: ReadLatestAsync kind financial-crime L51-52
    EvRd-->>FCInput: null today - no ingestion ever FC1
    FCInput-->>Issuer: Unavailable version 1 +1min FC FinancialCrimeModule.cs L53-54 L64-70
    Issuer->>Issuer: trust-safety read same shape L188-189
    Issuer->>Assess: Assess evidence now L81
    alt any evidence Unavailable or Unknown
        Assess-->>Caller: ComplianceUnavailable deny L82-87
    else stale
        Assess-->>Caller: ComplianceStale deny
    else Deny or Blocked
        Assess-->>Caller: Denied
    else Review
        Assess-->>Caller: review case submitted L145-150
    else Allow
        Assess-->>Caller: counters reserved allow L166-171
    end
```

Fail-open/fail-closed verdict: **fail-closed** — absent, stale, or unsigned AML evidence blocks the operation. With FC1 (no ingestion), the Allow branch is unreachable for every subject.

## 2. HardCoin → SoftCoin conversion verification (LIVE)

```mermaid
sequenceDiagram
    participant Conv as Self-service conversion
    participant Verifier as HardToSoftConversionRiskEvidenceVerifier
    participant FCInput as PostgreSqlFinancialCrimeRiskInputSource
    participant Val as ExternalRiskEvidenceValidator
    Conv->>Verifier: VerifyAsync actorId tenantId L29-35
    Verifier->>Verifier: subjectReference ForUser tenant actor L40
    Verifier->>FCInput: ReadAsync tenantId subject now L41-44
    FCInput-->>Verifier: Unavailable - no evidence rows exist FC1
    Verifier->>Val: RequireFreshAllow financial-crime plus trust-safety L46-49
    Val->>Val: source loop - Allow fresh signed hash auditable L59-75
    Val-->>Conv: ExternalRiskEvidenceException Fresh Allow required<br/>Risk ExternalRiskEvidence.cs L67-71
```

Hard gate: conversions require **fresh explicit Allow from both external sources**; absence is an exception, not a downgrade.

## 3. Payout coordination (LIVE read side)

```mermaid
sequenceDiagram
    participant Pay as PayoutCoordinator
    participant FCInput as IFinancialCrimeRiskInputSource
    participant TS as ITrustSafetyRiskInputSource
    participant Assess as EconomyProtectedRiskDecisionIssuerSupport
    Pay->>FCInput: ReadAsync tenant subject requestedAt Payouts PayoutCoordinator.cs L109-111
    FCInput-->>Pay: Unavailable - evidence ledger empty FC1
    Pay->>TS: ReadAsync L112-114
    TS-->>Pay: trust-safety outcome
    Pay->>Assess: Assess both L115-116
    Assess-->>Pay: ComplianceUnavailable - payout denied fail-closed
```

The legacy KYC payout eligibility source (`Payouts:/ComplianceKycPayoutEligibilitySource.cs`) is a **separate, earlier** check against `user_kyc_verifications` — the two gates stack, and neither reads the other's evidence (see §5).

## 4. Case decision → evidence ledger → hold release (LIVE)

```mermaid
sequenceDiagram
    participant Analyst as Analyst with OperateCompliance
    participant Ctrl as API EconomyComplianceAdministrationController
    participant Handler as EconomyComplianceEndpointCommandHandler
    participant Plane as PostgreSqlFinancialCrimeControlPlane
    participant EvStore as IComplianceEvidenceStore
    participant Holds as IComplianceHoldStore
    Analyst->>Ctrl: POST cases caseId decisions
    Ctrl->>Plane: ReadCaseDetailsAsync subject hash L115-116
    Ctrl->>Handler: DecideFinancialCrimeCaseEndpointCommand L131-133
    Handler->>Plane: DecideCaseAsync decision expectedVersion L66-68
    Plane->>Plane: assignee + version + contiguous checks L210-219
    alt Approved
        Plane->>Holds: ReleaseAsync holdId decider evidence L226-229
        Plane->>Plane: case Closed ClosedAt L226-228
    else non-Approved
        Plane->>Plane: case NeedsReview hold stays L232-233
    end
    Plane->>EvStore: IngestAsync envelope version latest+1 L236-244
    EvStore-->>Plane: Published or Duplicate L245-248
    Plane-->>Ctrl: 201 decision
```

Timeouts/failure verdicts: every leg is one `Serializable` DB transaction — an evidence-store rejection rolls back the case mutation and the hold release together (no partial release).

## 5. The three evidence ledgers and the two gates that consult neither — N7 extended

```mermaid
flowchart TD
    SUMSUB["SumSub KYC AML pipeline<br/>Kyc SumSubKycAmlOrchestrator.cs L268-293"] -->|"kind kyc-aml"| EV[("compliance evidence ledger<br/>Risk PostgreSqlComplianceEvidenceStore.cs")]
    FCPLANE["FinancialCrime control plane<br/>FC PostgreSqlFinancialCrimeControlPlane.cs L126 L244"] -->|"kind financial-crime - dormant FC1"| EV
    TS["TrustSafety control plane"] -->|"kind trust-safety"| EV
    LEGACY["legacy KYC ledger<br/>user_kyc_verifications Kyc KycService.cs"] --> SIGNIN["sign-in compliance gate<br/>API Core Compliance KycAndHoldSignInCompliancePolicy.cs L53"]
    LEGACY --> KYCPAY["payout KYC eligibility<br/>Payouts ComplianceKycPayoutEligibilitySource.cs L26"]
    EV --> RISK["economy risk gates<br/>issuer L186-190 verifier L41-44 payouts L109-116"]
    SIGNIN -.->|"consults neither evidence kind - N7"| EV
    RISK -.->|"consults neither KYC ledger"| LEGACY
```

The compliance package's N7 finding (two KYC ledgers, no bridge) extends into three ledgers once AML is considered: a SumSub-verified user produces `kyc-aml` evidence but **no** `financial-crime` evidence, so the risk gates still deny; conversely a future screening provider producing `financial-crime` Allow evidence says nothing about the sign-in gate. The gates that protect money and the gates that protect access read disjoint evidence sets — an AML examiner will ask which one is authoritative (`07-stride-threat-model.md` F6; `06-gaps.md` FC5).
