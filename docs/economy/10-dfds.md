# Economy Data Flow Diagrams — level 0 and level 1 per family

Status: living document, current as of 2026-11-04 against `origin/develop` (21b58ac6a7).

Companion documents: [`08-stride-threat-model.md`](08-stride-threat-model.md) (families E1-E9), [`09-security-architecture.md`](09-security-architecture.md) (trust boundaries E-B1..E-B7). Citation shorthand as in [`README.md`](README.md). No parentheses or colons in labels; `L###` is file:line.

This family moves money, not personal data — **PII markers below flag subject identity, wallet ownership, or KYC-linked references**, the identifiers an auditor treats as personal.

## 1. Level 0 — the Economy family as one process

```mermaid
flowchart TD
    subgraph EXT["External parties"]
        USER["Wallet owner"]
        ADMIN["Economy administrators"]
        STRIPE["Stripe - funding Connect treasury"]
        ADNET["Ad networks"]
        SUMSUB["SumSub KYC provider - identity PII at ingress"]
        AI["Internal AI providers"]
        AUDITOR["Financial auditor - artifact consumer"]
    end

    subgraph ZONE["API process - boundaries E-B1 to E-B7"]
        ECO(["Finance.Economy - 7 sub modules<br/>double-entry core fenced payouts"])
    end

    subgraph STORES["Data stores"]
        PG[("PostgreSQL economy schema - 108 tables")]
        LEDGER[("double-entry journal credit lots")]
    end

    USER -->|"transfers conversions top-ups payout requests PII subject ids"| ECO
    ADMIN -->|"review execute treasury control plane"| ECO
    STRIPE -->|"signed webhooks fiat events"| ECO
    ADNET -->|"verified completions"| ECO
    SUMSUB -->|"signed KYC decisions PII"| ECO
    AI -->|"token counts only"| ECO
    ECO -->|"provider payouts"| STRIPE
    ECO -->|"journal legs"| LEDGER
    ECO -->|"state"| PG
    ECO -.->|"exported evidence"| AUDITOR
```

## 2. E1 — top-up mint and reversal, level 1

```mermaid
flowchart TD
    subgraph EXT["Boundary E-B3 - anonymous HMAC"]
        S1["Stripe"]
    end
    subgraph APIZ["API process"]
        A1(["webhook consumer verify then identity L150-186"])
        A2(["funding gateway durable confirm L120-135"])
        A3(["reversal path charge refunded L158-266"])
        A4(["protected operations - ReverseProviderFunding idempotent"])
    end
    subgraph DBZ["PostgreSQL - boundary E-B4"]
        D1[("top-up intents claims")]
        D2[("journal legs via SECURITY DEFINER writers")]
        D3[("wallet debt partition for spent units L246-251")]
    end

    S1 -->|"signed event"| A1
    A1 -->|"payment_intent succeeded"| A2 --> D2
    A1 -->|"charge refunded cumulative"| A3 --> A4 --> D2
    A4 --> D3
    A2 --> D1
    A3 --> D1
```

## 3. E2 — conversion and transfer, level 1

```mermaid
flowchart TD
    subgraph EXT["Boundary E-B1"]
        U1["Wallet owner PII subject id"]
    end
    subgraph APIZ["API process"]
        B1(["EconomyWalletController L67 L84<br/>server resolves wallets only"])
        B2(["protected operation intent<br/>risk policy reserve jurisdiction"])
        B3(["writer post transfer post conversion"])
    end
    D1[("wallets")]
    D2[("journal legs")]
    D3[("FIFO credit lot fragments")]

    U1 -->|"business intent only"| B1 --> B2 --> B3
    B3 --> D2
    B3 --> D3
    B1 -.->|"reads own wallet PII subject scoped"| D1
```

## 4. E3 — payout dual-control and fenced execution, level 1

```mermaid
flowchart TD
    subgraph EXT["Boundaries E-B1 E-B2"]
        U1["Requester"]
        R1["Reviewer - economy review payouts"]
        X1["Executor - economy operate payouts plus step-up"]
        P1["Stripe Connect"]
    end
    subgraph APIZ["API process"]
        C1(["payout request L267 self service"])
        C2(["review trail L47 immutable L123-128 permission"])
        C3(["step-up executor Serializable L65"])
        C4(["settlement workflow receipt equality L400-410"])
        C5(["fencing token allocator PayoutsModule L16"])
        C6(["dispatch outbox processor - no gate recheck N6"])
    end
    subgraph DBZ["PostgreSQL"]
        D1[("payout requests operations")]
        D2[("journal + reserve fragments - Cancelled unreachable N4")]
        D3[("payout dispatch outbox - uncapped N3")]
    end

    U1 --> C1 --> D1
    R1 --> C2 --> D1
    X1 --> C3 --> C4 --> C5 --> C6 --> D3
    C4 --> D2
    C6 -->|"provider send"| P1
    P1 -->|"signed outcome webhook"| C4
```

## 5. E4 — treasury withdrawal, level 1

```mermaid
flowchart TD
    subgraph EXT["Boundary E-B2 E-B5"]
        T1["Treasury admin - operate treasury plus fresh reauth"]
        S1["Stripe platform account"]
    end
    subgraph APIZ["API process"]
        E1(["propose run - reserve at proposal L82-100"])
        E2(["durable workflow reserve FIFO fragments"])
        E3(["admin withdrawal dispatch outbox - default OFF"])
    end
    subgraph DBZ["PostgreSQL"]
        D1[("withdrawal runs - no cancel path N5")]
        D2[("journal + platform fee reserve fragments")]
        D3[("overlap guard L236-240")]
    end

    T1 -->|"reauth evidence bound to period amount destination"| E1
    E1 --> E2 --> D2
    E1 --> D1
    D1 --> D3
    E3 -->|"provider send"| S1
    E1 --> E3
```

## 6. E5 — marketplace settlement and refund, level 1

```mermaid
flowchart TD
    subgraph EXT["Boundary E-B1 E-B2"]
        M1["Buyer seller platform admins"]
        O1["Commerce order decision"]
    end
    subgraph APIZ["API process"]
        F1(["settlement authority CommerceOrderMarketplaceSettlementAuthority L10-15"])
        F2(["durable settlement service born Settled L92 K8"])
        F3(["deterministic entitlement ids L150"])
        F4(["marketplace outbox processor N3 uncapped L96-110"])
    end
    D1[("settlements buyer seller platform legs")]
    D2[("entitlements receipts L164-175")]
    D3[("marketplace outbox")]

    O1 --> F1 --> F2 --> D1
    F2 --> F3 --> D2
    F2 --> D3
    F4 --> D3
```

## 7. E6 — bounty escrow, level 1

```mermaid
flowchart TD
    subgraph EXT["Boundary E-B1"]
        B1["Bounty poster and claimers"]
    end
    subgraph APIZ["API process"]
        G1(["bounty create - escrow posts FIFO fragments"])
        G2(["completion award - writer serialized"])
        G3(["expiration sweep - safety cycle default ON L72-75"])
    end
    D1[("bounties + work queue")]
    D2[("journal escrow legs")]

    B1 --> G1 --> D2
    B1 --> G2 --> D2
    G3 --> D1
    G3 --> D2
```

## 8. E7 — ad rewards, level 1

```mermaid
flowchart TD
    subgraph EXT["Boundary E-B3"]
        P1["Ad networks"]
        U1["Viewer"]
    end
    subgraph APIZ["API process"]
        H1(["session milestones"])
        H2(["provider adapters verify completion"])
        H3(["issuance writer post ad reward issuance v1"])
    end
    D1[("sessions completions pending claims")]
    D2[("journal legs")]

    U1 --> H1
    P1 -->|"verified completions"| H2 --> H3
    H1 --> D1
    H3 --> D1
    H3 --> D2
```

## 9. E8 — AI credits, level 1

```mermaid
flowchart TD
    subgraph INT["In process only - boundary E-B6"]
        I1["AI provider calls"]
    end
    subgraph APIZ["API process"]
        J1(["reserve - argument guarded L105-115"])
        J2(["settle or release by idempotency key"])
        J3(["balance contributor - outside journal K6 L250-264"])
    end
    D1[("ai credit reservations")]
    D2[("wallet view merges contributors EconomyWalletQueries L120-133")]

    I1 --> J1 --> D1
    I1 --> J2 --> D1
    J3 --> D1
    J3 --> D2
    D1 -.->|"no expiry sweep N8"| D1
```

## 10. E9 — control plane, level 1

```mermaid
flowchart TD
    subgraph EXT["Boundaries E-B2 E-B3"]
        A1["Control plane admin - typed manage permissions"]
        S1["SumSub signed webhook"]
    end
    subgraph APIZ["API process"]
        K1(["kill switches policies reserves holds risk L445 re-check"])
        K2(["KYC evidence envelopes L7-22"])
        K3(["capability policy activation - safety cycle L66-68"])
    end
    D1[("capability policies kill switches")]
    D2[("economy compliance evidence ledger - outbox N2")]
    D3[("legacy KYC ledger - read by payout gate N7 ComplianceKycPayoutEligibility L22")]

    A1 --> K1 --> D1
    S1 --> K2 --> D2
    K3 --> D1
    K2 -.->|"not bridged"| D3
```

## 11. Cross-boundary value summary

| DFD | Value or identity crossing | Boundary | Control |
|-----|---------------------------|----------|---------|
| E1 | fiat event → coin mint/reversal | E-B3/E-B4 | HMAC + writer monopoly + reversal idempotency |
| E2 | subject-scoped movements | E-B1 | server-resolved wallets; no client ids |
| E3 | provider money out | E-B2/E-B5 | dual-control + step-up + fencing + hash binding |
| E4 | platform fees out | E-B2/E-B5 | reauth evidence + overlap guard |
| E5 | counterparty splits | E-B4 | atomic writer; deterministic entitlement ids |
| E8 | spendable-soft reduction | E-B6 | guards + idempotency; no expiry (N8) |
| E9 | compliance decisions → money gate | E-B7 | versioned evidence; legacy-ledger divergence N7 |
