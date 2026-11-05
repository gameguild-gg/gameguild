# TrustSafety — Cross-module sequences

> Module `GameGuild.TrustSafety` · develop `ce31d5850c`. Shorthand `TS:`, `Econ:`, `Assets:`, `API:`.

## 1. Economy risk consult (the live consumer path)

```mermaid
sequenceDiagram
    participant OP as Economy protected operation<br/>payout policy consult
    participant RI as PostgreSqlTrustSafetyRiskInputSource
    participant EV as IComplianceEvidenceReader
    participant DB as trust_safety + evidence tables

    OP->>RI: ReadAsync tenantId subjectHash observedAt
    note over RI: no-tenant overload always Unavailable trust-safety-tenant-context-required TS-TrustSafetyModule-cs-27-35
    RI->>EV: ReadLatestAsync kind TrustSafety
    alt no evidence exists permanent today T1
        RI-->>OP: Unavailable trust-safety-evidence-unavailable 1min TTL
    else evidence found and signature-verified
        RI-->>OP: TrustSafetyRiskInput version issued expires Allow-Deny-Review
    end
    OP->>OP: evidence folds into policy decision<br/>Econ-Risk-PostgreSqlEconomyProtectedOperationRiskDecisionIssuer-cs-188-190
    note over OP: payout authorization consumes the same inputs PayoutCoordinator-cs-116
```

## 2. Evidence ingestion — as designed, with the truth flag

```mermaid
sequenceDiagram
    participant PR as Producer someday NONE TODAY
    participant CP as PostgreSqlTrustSafetyControlPlane
    participant SV as SignatureVerifier
    participant HZ as IComplianceHoldStore
    participant ES as IComplianceEvidenceStore
    participant DB as 3 tables serializable tx

    PR->>CP: IngestAsync TrustSafetyEvent signed
    CP->>DB: replay checks EventId + tenant-subject-version
    CP->>SV: Verify canonical payload keyId signature
    alt invalid
        CP->>DB: inbox invalid-signature Rejected
    else valid ordered
        CP->>HZ: Approved → Release hold OR non-Approved → Activate hold
        CP->>DB: subject state upsert version++
        CP->>ES: IngestAsync envelope kind TrustSafety
        CP->>DB: inbox ProcessedAt
    end
```

The sequence is complete and unit-tested in isolation — only the left-most participant is missing (04 §1).

## 3. Appeal decide — the live console path

```mermaid
sequenceDiagram
    participant ST as Authenticated staff
    participant AC as EconomyComplianceAdministrationController
    participant CMD as Assign-Decide EndpointCommands
    participant CP as PostgreSqlTrustSafetyControlPlane
    participant DB as trust_safety_appeals

    ST->>AC: GET admin-economy-compliance-trust-safety-appeals
    AC->>CP: ReadAppealsAsync tenant state limit
    CP->>DB: SELECT tenant-scoped
    AC-->>ST: 200 list
    ST->>AC: POST assignment ExpectedVersion
    AC->>CMD: AssignTrustSafetyAppealEndpointCommand
    CMD->>CP: AssignAppealAsync
    CP->>DB: Submitted-only Version-checked → Assigned
    ST->>AC: POST decisions Overturn ReasonCode EvidenceHash
    AC->>CMD: DecideTrustSafetyAppealEndpointCommand
    CMD->>CP: DecideAppealAsync
    CP->>DB: Assigned-to-actor-only → Upheld-Overturned
    note over ST,DB: SubmitAppealAsync has no caller — the table is empty by construction T1
```

## 4. Asset report → moderation action (the content-safety cross-module path that exists)

```mermaid
sequenceDiagram
    participant U as Reporter
    participant A as Assets module
    participant ML as ModerationClassificationWorker
    participant LLM as LiteLLM vision proxy
    participant ADM as Admin reviewer

    U->>A: POST report asset reason
    A->>A: guard not-own-asset create report Pending
    loop polling 5s
        ML->>LLM: classify image bytes 8-label prompt
        LLM-->>ML: labels+scores strict JSON
        ML->>ML: threshold 0-80 approve / 0-75 reject / else NeedsReview
    end
    ADM->>A: GET moderation queue report-count ordered
    ADM->>A: POST review report decision
    A->>A: ReportStatus Resolved-Dismissed + asset action<br/>ContentRemoved-ContentHidden-UserWarned
    note over A,ML: text content of any kind never passes any classifier 01 F5
```

## 5. Boundary contract — TrustSafety vs. FinancialCrime vs. Assets moderation

| Plane | Scope | Evidence kind | Enforcement |
|---|---|---|---|
| **TrustSafety (this module)** | behavioral risk verdicts on hashed subjects (8 kinds), appeals | `TrustSafety` | economy compliance holds + Allow/Deny/Review on protected ops |
| FinancialCrime (docs/compliance-financialcrime/) | AML cases, examiners, regulatory refs | `financial-crime` | same evidence rail, own case lifecycle |
| Assets moderation | uploaded media content safety | none (rows only) | asset visibility (Rejected hidden, NeedsReview queued) |

The three are frequently conflated in feature requests ("add moderation to TrustSafety") — the intended split: **content** → Assets lane; **behavior/abuse verdicts** → this module's evidence rail; **regulated financial crime** → FinancialCrime. Text moderation belongs to whichever plane the pending #1250 product decision picks; today it exists in none (01 F5).
