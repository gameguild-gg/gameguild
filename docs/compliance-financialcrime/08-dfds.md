# FinancialCrime (AML) — data-flow diagrams

> Module `GameGuild.Compliance.FinancialCrime` · Documented against `develop` commit `60100459e6`.
> PII note: no direct personal identifiers cross any boundary — subjects move as `SubjectHash` (SHA-256, ≤128 hex chars) everywhere in this module.

## Level 0 — context

```mermaid
flowchart TD
    EXAM["AML examiner / FIU<br/>external party"]
    PROV["Screening provider<br/>sanctions PEP adverse media<br/>external - DOES NOT EXIST FC1"]
    MON["Transaction monitoring producer<br/>external internal - DOES NOT EXIST FC1"]
    ANA["AML analyst with OperateCompliance<br/>external party authenticated"]
    SYS["GameGuild API<br/>financial-crime control plane"]
    EV[("compliance evidence ledger<br/>kind financial-crime")]
    HOLD[("compliance hold store")]
    ECON["Economy money path<br/>risk issuer conversion verifier payouts"]
    KYC["KYC ledgers - disjoint see N7 FC5"]

    PROV -.->|"signed screening events - DESIGNED ONLY"| SYS
    MON -.->|"scored signals - DESIGNED ONLY"| SYS
    ANA -->|"case list details assign decide SAR STR"| SYS
    SYS --> EV
    SYS --> HOLD
    EV --> ECON
    SYS -->|"case exports - NONE FC4"| EXAM
    KYC -.->|"no bridge"| ECON
```

Trust boundaries: TB1 analyst→API (authenticated + `OperateCompliance`); TB2 provider→API (designed, absent); TB3 monitoring→API (designed, absent); TB4 API→evidence/hold stores (in-process, transactional); TB5 API→examiner (absent).

## F1 — Screening ingestion (level 1)

```mermaid
flowchart TD
    subgraph TB2["TB2 provider boundary - absent FC1"]
        P1(("screening provider"))
    end
    subgraph API["API process - Serializable transaction"]
        I1["validate signature lifetime<br/>FC PostgreSqlFinancialCrimeControlPlane.cs L493-513"] --> I2["replay + contiguous version checks<br/>L94-113"]
        I2 --> I3["screening row<br/>subjectHash outcome flags policyVersion"]
        I3 --> I4{"outcome not Approved"}
        I4 -- "yes" --> I5["open case + hold activation<br/>L398-423"]
        I4 -- "no" --> I6["no case"]
        I5 --> I7["evidence envelope IngestAsync<br/>L126"]
        I6 --> I7
    end
    P1 -.-> I1
    I7 --> EV[("compliance evidence ledger<br/>TB4")]
    I5 --> HOLD[("hold store TB4")]
    I3 --> T1[("compliance_financial_crime_screenings")]
    I5 --> T2[("compliance_financial_crime_cases + case_events")]
```

Flows marked PII: none — `SubjectHash` only. The dashed input edge is the FC1 void.

## F2 — Transaction monitoring (level 1)

```mermaid
flowchart TD
    subgraph TB3["TB3 monitoring boundary - absent FC1"]
        M1(("monitoring producer"))
    end
    subgraph API["API process"]
        S1["validate signal score lifetime<br/>L515-532"] --> S2["RequestHash idempotency<br/>L142-158"]
        S2 --> S3["open or reuse case + hold<br/>L166-168 L402-407"]
        S3 --> S4["signal row linked to case"]
    end
    M1 -.-> S1
    S4 --> T3[("compliance_financial_crime_transaction_signals")]
    S3 --> T2[("cases + case_events")]
    S3 --> HOLD[("hold store")]
```

## F3 — Case management (level 1, live)

```mermaid
flowchart TD
    subgraph TB1["TB1 analyst boundary"]
        A1(("analyst OperateCompliance"))
    end
    subgraph API["API process"]
        C1["TryActor per request<br/>EconomyComplianceAdministrationController.cs L232-243"] --> C2{"read or mutate"}
        C2 -- "read" --> C3["ReadCases L322-353<br/>ReadCaseDetails L355-396"]
        C2 -- "assign" --> C4["AssignCaseAsync version guard L176-198"]
        C2 -- "decide" --> C5["DecideCaseAsync L200-253<br/>assignee contiguous Approved closes"]
        C2 -- "SAR STR" --> C6["RecordRegulatoryReferenceAsync<br/>hash only L286-305"]
    end
    A1 --> C1
    C3 --> T2[("cases case_events decisions references")]
    C4 --> T2
    C5 --> EV[("evidence ledger - decision envelope")]
    C5 --> HOLD[("hold release on Approved")]
    C6 --> T4[("compliance_financial_crime_regulatory_references<br/>ReferenceHash only")]
```

Flows marked PII: none (subject hashes + hashes of external references).

## F4 — Decision consumption (level 1, designed)

```mermaid
flowchart TD
    subgraph MONEY["value movement path - absent caller FC1"]
        V1(("operation executor"))
    end
    subgraph API["API process"]
        D1["locate Approved decision in window<br/>L268-274"] --> D2["write unique consumption row<br/>L277-282"]
    end
    V1 -.-> D1
    D2 --> T5[("compliance_financial_crime_decision_consumptions")]
```

## F5/F6 — Evidence ledger and money-path gates (level 1, live read side)

```mermaid
flowchart TD
    subgraph ECAP["Economy capabilities - opt-in per jurisdiction"]
        E1["protected operations risk issuer<br/>Risk PostgreSqlEconomyProtectedOperationRiskDecisionIssuer.cs L182-191"]
        E2["HardCoin to SoftCoin conversion verifier<br/>Risk HardToSoftConversionRiskEvidenceVerifier.cs L29-52"]
        E3["payout coordination<br/>Payouts PayoutCoordinator.cs L109-116"]
    end
    RD["PostgreSqlFinancialCrimeRiskInputSource<br/>FC FinancialCrimeModule.cs L43-79"]
    EV[("compliance evidence ledger<br/>TB4")]
    E1 --> RD
    E2 --> RD
    E3 --> RD
    RD --> EV
    RD -->|"Unavailable today - empty ledger FC1"| E1
    EV -->|"ReadLatestAsync kind financial-crime"| RD
```

Flows marked PII: none; the subject reference is derived from actor id + tenant (`Risk:/EconomySubjectReference.cs:13`) and hashed before storage.

## Boundary inventory

| Boundary | Crossers | Control |
|---|---|---|
| TB1 analyst → API | case reads/writes | `[Authorize]` + `OperateCompliance` per request; no rate limit (FC7); no step-up on decisions (FC2) |
| TB2 provider → API (absent) | screening events | designed: signature-verified envelopes only (`FC:/PostgreSqlFinancialCrimeControlPlane.cs:508-512`) |
| TB3 monitoring → API (absent) | signals | designed: idempotency-key hashed, input-match replay guards (`:142-158`) |
| TB4 API ↔ stores | evidence, holds, 7 tables | in-process `Serializable` transactions; single-use and contiguity enforced by unique indexes |
| TB5 API → examiner (absent) | case exports | none exists (FC4) |
| TB6 evidence kinds ↔ KYC ledgers | none — disjoint (FC5) | no bridge, no precedence rule |
