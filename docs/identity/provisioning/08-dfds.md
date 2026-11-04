# Identity.Provisioning Data Flow Diagrams

> `develop` 7ae5a28a65 · Level-0 + level-1 per family. Notation per `docs/security/auth-dfds.md` §1. Paths relative to `apps/api/Source/Modules/GameGuild.Identity.Provisioning/` unless stated.

## 1. Level 0 — context

```mermaid
flowchart LR
    IDP["External IdP - SCIM client with bearer token"]
    ADMIN["Platform admin - token management"]

    PROV(["0.0 GameGuild Identity.Provisioning module"])

    USERS["Identity.Users records"]
    ROLES["Tenant roles - Authorization module"]
    PG[("PostgreSQL tokens mappings")]
    AUDIT["Compliance.Audit central log"]

    IDP -->|"SCIM requests with ggsp token"| PROV
    ADMIN -->|"create rotate revoke - ProvisioningManageTokens"| PROV
    PROV -->|"user records via ScimUserMapping"| USERS
    PROV -->|"group membership = role assignments"| ROLES
    PROV -->|"token hashes + mappings"| PG
    PROV -->|"auth and operation audit"| AUDIT
```

Boundaries: B1 IdP→API (TLS + bearer); B2 token→tenant scope; B3 SCIM→Users; B4 SCIM→roles; B5 module→audit.

## 2. Level 1 — F1 tokens

```mermaid
flowchart TD
    subgraph API["B1 B2"]
        K1(["1.1 Create - 256 bit RNG - hash at rest - plaintext once"]) --> K2(["1.2 Active with scopes and expiry"])
        K2 --> K3(["1.3 Rotate - overlap grace window"])
        K3 --> K4(["1.4 Lazy finalize after grace - fail closed on next use"])
        K2 --> K5(["1.5 Revoke - reason recorded"])
        K4 & K5 --> K6(["1.6 Auth rejects - 401 audited"])
        K2 --> K7(["1.7 Auth accepts - claims tenant_id token_id scopes - usage stamped"])
        K6 & K7 --> K8(["1.8 Audit every outcome via sink"])
    end
    subgraph STORES["B3 B5"]
        DK[("ScimProvisioningToken - KeyHash unique")]
        DA2[("Central audit log - or Null sink gap P1")]
    end
    K1 & K3 & K5 --> DK
    K8 --> DA2
```

## 3. Level 1 — F2 SCIM users

```mermaid
flowchart TD
    subgraph API["B1 B2 - token scheme plus scope checks"]
        U1(["2.1 Create - userName and email required - uniqueness enforced"]) --> U2(["2.2 Replace - full resource"])
        U1 --> U3(["2.3 Patch - RFC 7644 path ops via ScimUserPatchApplier"])
        U1 --> U4(["2.4 Delete as deactivate per RFC 3.5.3"])
        U5(["2.5 List with filter - parser plus evaluator whitelist"])
        U1 & U2 & U3 & U4 --> U6(["2.6 Map to Identity.Users record via ScimUserMapping"])
        U1 --> U7(["2.7 ScimException to RFC 7644 error body - ScimExceptionFilter"])
    end
    DU[("Identity.Users + ScimUserMapping")]
    U6 --> DU
```

## 4. Level 1 — F3 SCIM groups and F4 discovery/bulk

```mermaid
flowchart TD
    subgraph API["B1 B2 B4"]
        G1(["3.1 Group CRUD - mapped to tenant roles"]) --> G2(["3.2 Group patch mutates role membership - STRIDE section 4 elevation surface"])
        D1(["4.1 Discovery - config schemas resourceTypes - token gated"]) --> D2(["4.2 Bulk - failOnErrors envelope"])
    end
    DG[("ScimGroupMapping + roles")]
    G1 & G2 --> DG
```

## 5. Coverage matrix

| Family | DFD | Highest-risk flow | STRIDE |
|---|---|---|---|
| F1 tokens | §2 | 1.8 Null-sink audit loss (P1) | §2 |
| F2 users | §3 | 2.6 external writes to user records | §3 |
| F3 groups | §4 | 3.2 role-membership writes by token (P6) | §4 |
| F4 discovery/bulk | §4 | 4.2 partial-failure envelopes | §5 |
