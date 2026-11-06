# Compliance Data Flow Diagrams — level 0 and level 1 per family

Status: living document, current as of 2026-11-04 against `origin/develop` (21b58ac6a7).

Companion documents: [`08-stride-threat-model.md`](08-stride-threat-model.md) (families F1-F8), [`09-security-architecture.md`](09-security-architecture.md) (trust boundaries C1-C7). Citation shorthand as in [`README.md`](README.md).

## 1. Notation

Mermaid `flowchart TD` with DFD shapes — external party rectangle, process stadium, data store cylinder. **PII-bearing flows are marked `PII` in the edge label.** Regulators and data-subject authorities appear as external parties where the family's evidence is ultimately aimed (FERPA examiner, data-protection authority, AML examiner). Trust boundary subgraphs match C1-C7 of the architecture document. No parentheses or colons appear inside node labels; `L###` is file:line shorthand.

## 2. Level 0 — the compliance family as one process

```mermaid
flowchart TD
    subgraph EXT["External parties"]
        USER["Authenticated platform user"]
        ADMIN["Tenant auditor or compliance officer"]
        DPO["Data protection authority - GDPR"]
        ED["FERPA examiner - eligible student guardian school official"]
        AMLR["AML examiner"]
        SUMSUB["SumSub KYC provider"]
        TENWH["Tenant HTTPS webhook endpoint"]
    end

    subgraph ZONE["API process - boundaries C1 to C6"]
        COMP(["Compliance family<br/>Audit Consent FERPA KYC"])
    end

    subgraph STORES["Data stores"]
        PG[("PostgreSQL compliance schema<br/>38 audit tables + consent ferpa kyc")]
        S3[("S3 raw evidence archive")]
        SPOOLF[("Local disk spool JSONL")]
    end

    USER -->|"consent FERPA KYC self-service PII"| COMP
    ADMIN -->|"query export DSL custody PII"| COMP
    SUMSUB -->|"signed webhook PII"| COMP
    COMP -->|"export notifications"| TENWH
    COMP -->|"evidence packs PII"| ADMIN
    ADMIN -.->|"artifacts produced for"| DPO
    ADMIN -.->|"records and disclosure logs PII"| ED
    ADMIN -.->|"KYC evidence and reports PII"| AMLR
    COMP -->|"audit rows PII"| PG
    COMP -->|"raw payloads PII"| S3
    COMP -->|"sanitized events"| SPOOLF
```

The regulator parties are dashed because no automated egress targets them — they consume artifacts humans produce from the export/packaging surfaces (boundary C7).

## 3. F1 — audit event ingestion, level 1

```mermaid
flowchart TD
    subgraph PRODZ["Producer modules - in process"]
        P1(["sink bridges authn apikey scim clt consent L135-147"])
    end
    subgraph AUDZ["Compliance.Audit"]
        P2(["taxonomy classify + 3 DB attempts SecurityEventLogger"])
        P3(["PiiRedactionService HMAC tokenize"])
        P4(["SecurityEventFileSpool append sanitized"])
        P5(["SpoolDrainer replay by event id L125-129"])
    end
    D1[("AuditLogs")]
    D2[("SecurityAlerts")]
    D3[("local spool JSONL")]

    P1 -->|"raw event PII"| P2
    P2 --> P3
    P3 -->|"redacted row PII residual"| D1
    P2 -->|"alerts same context"| D2
    P2 -->|"db down sanitized PII"| P4 --> D3
    P5 --> D3
    P5 -->|"replay persist"| D1
```

## 4. F2 — query and export, level 1

```mermaid
flowchart TD
    subgraph EXT["Boundary C1"]
        E1["Auditor"]
    end
    subgraph APIZ["Compliance.Audit"]
        Q1(["list search DSL statistics AuditController AuditQueryController"])
        Q2(["CSV JSON XML export column whitelist L481-489"])
        Q3(["scheduled export worker"])
        Q4(["webhook notifier allow-list HMAC L63-173"])
    end
    D1[("AuditLogs")]
    D2[("AuditExportHistories")]
    E2["Tenant webhook endpoint"]

    E1 -->|"query"| Q1 -->|"rows PII"| D1
    E1 -->|"export request"| Q2 -->|"up to caps PII"| D1
    Q2 -->|"file checksum"| D2
    Q3 -->|"runs"| D2
    Q3 -->|"completed notification"| Q4 --> E2
    E1 -.->|"evidence artifacts PII"| E3["Regulator or DPA - human handoff"]
```

## 5. F3 — retention and purge, level 1

```mermaid
flowchart TD
    subgraph APIZ["Compliance.Audit"]
        R1(["retention template resolution"])
        R2(["SecurityLogRetentionBackgroundService 24h AuditModule L125"])
        R3(["archive tier move"])
    end
    D1[("AuditLogs")]
    D2[("legal holds")]
    D3[("SecurityLogRetentionExecutions")]
    D4[("archive destination PII")]

    R1 --> R2
    D2 -->|"veto"| R2
    R2 -->|"purge"| D1
    R2 -->|"archive PII"| R3 --> D4
    R2 -->|"execution history"| D3
```

## 6. F4 — tamper evidence custody packaging replay, level 1

```mermaid
flowchart TD
    subgraph EXT["Boundary C1"]
        E1["Admin"]
    end
    subgraph APIZ["Compliance.Audit"]
        T1(["TamperEvidentAuditService append chain L95-159"])
        T2(["ECDSA signing versioned keys AuditModule L160-175"])
        T3(["custody preserve transfer SHA256 + challenges"])
        T4(["compliance packaging seal sign verify L72-92"])
        T5(["replay snapshot canonical bytes digest L78 127-141"])
        T6(["chain verifier recompute both hashes + signature L10-40"])
    end
    D1[("TamperEvidentAuditLogs unique tenant sequence L131")]
    D2[("custody evidence rows L555")]

    E1 -->|"high-risk append"| T1 --> T2 --> D1
    E1 -->|"preserve transfer"| T3 --> D2
    E1 -->|"package"| T4
    E1 -->|"snapshot verify"| T5
    T6 --> D1
```

## 7. F5 — consent lifecycle, level 1 (dead at runtime — N1)

```mermaid
flowchart TD
    subgraph EXT["Boundary C1"]
        E1["Any authenticated user - no ownership check N5"]
    end
    subgraph APIZ["Compliance.Consent"]
        C1(["ConsentController - DI dead N1 - would 500"])
        C2(["ConsentService grant revoke L53-67"])
        C3(["DSAR lifecycle 30-day deadline decorative N3"])
    end
    D1[("user_consents unique userId policyVersion L88")]
    D2[("consent policies versions dsar rows")]

    E1 -->|"grant revoke PII"| C1 --> C2 --> D1
    E1 -->|"policy admin"| C1 --> D2
    C2 -->|"audit events fail-soft"| C4(["CentralConsentAuditEventSink"])
    C3 --> D2
```

## 8. F6 — FERPA, level 1

```mermaid
flowchart TD
    subgraph EXT["Boundary C1"]
        E1["Any authenticated user - need-to-know inverted N5"]
        E2["Eligible student guardian school official"]
        E3["FERPA examiner - human artifact consumer"]
    end
    subgraph APIZ["Compliance.FERPA - host registered ApiProductComposition L120"]
        F1(["FerpaController records consents disclosures inspection L13-63"])
        F2(["disclosure gate caller-asserted basis L110-123"])
        F3(["directory policy stored never consulted N6"])
    end
    D1[("education records PII")]
    D2[("disclosure logs append-only PII")]
    D3[("inspection requests 45-day clock N3")]

    E1 -->|"read any student PII"| F1 --> D1
    E1 -->|"record disclosure"| F2 --> D2
    E1 -->|"inspection"| F1 --> D3
    E2 -.->|"statutory right"| F1
    E3 -.->|"logs and packs PII"| F1
```

## 9. F7 — KYC legacy ledger and sign-in gate, level 1

```mermaid
flowchart TD
    subgraph APIZ["In process only - no controller"]
        K1(["KycService status writes no transition table N9 L70-72 L232-241"])
        K2(["document upload stub N11 L191-204"])
        K3(["compliance report dead Expired N15 L270"])
    end
    D1[("user_kyc_verifications PII incl ProviderData jsonb L73-74")]
    G1(["KycAndHoldSignInCompliancePolicy deny challenge approve L68-77"])
    PAY(["ComplianceKycPayoutEligibilitySource L22 36-37"])

    K1 --> D1
    K2 --> D1
    K3 -->|"reads"| D1
    D1 -->|"status PII"| G1
    D1 -->|"status expiry PII"| PAY
```

## 10. F8 — SumSub pipeline and economy evidence, level 1

```mermaid
flowchart TD
    subgraph EXT["Boundary C2 - anonymous HMAC"]
        S1["SumSub"]
    end
    subgraph APIZ["Compliance.KYC via host"]
        W1(["EconomySumSubWebhookController allow anonymous L121-143"])
        W2(["orchestrator verify archive dedupe order L158-205"])
        W3(["publish versioned evidence L268-293"])
        W4(["jurisdiction pin on approve L295-324"])
    end
    subgraph ECO["Finance.Economy"]
        EV1(["EconomyKycEvidenceStore envelope L7-22"])
        PAY(["payout eligibility reads the OTHER ledger - N7"])
    end
    D1[("sumsub webhook inbox")]
    D2[("S3 raw payloads PII")]
    D3[("economy compliance evidence ledger")]
    D4[("user_kyc_verifications legacy - not written here")]

    S1 -->|"signed webhook PII"| W1 --> W2
    W2 --> D1
    W2 -->|"raw before state change PII"| D2
    W2 --> W3 --> W4 --> EV1 --> D3
    PAY -.->|"reads legacy"| D4
```

## 11. PII flow summary

| DFD | PII crossing a boundary | Boundary | Control at the crossing |
|-----|------------------------|----------|------------------------|
| F1 | raw event fields → disk spool | C4 | sanitize fail-closed before write |
| F2 | up-to-cap row sets → operator + webhook | C1/C6 | column whitelist, tenant clamp, HMAC egress |
| F3 | archived rows → archive store | C3 | execution history; no extra at-rest redaction |
| F5 | consent attributes IP UA method | C1 | **none beyond authentication — N5** |
| F6 | education records to any authenticated user | C1 | **none — N5** |
| F7 | provider jsonb to internal readers | C3 | none documented |
| F8 | subject PII to anonymous-signed ingress + S3 | C2/C5 | HMAC fail-closed; S3 pre-state archival |
