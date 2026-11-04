# Identity.Provisioning — Sequences (lite)

> `develop` 7ae5a28a65 · Cross-module sequences only. Paths relative to `apps/api/Source/Modules/GameGuild.Identity.Provisioning/` unless stated.

## 1. IdP request → gate → token auth → SCIM controller

```mermaid
sequenceDiagram
    participant IdP as External IdP with bearer token
    participant GATE as ScimFeatureGateMiddleware - first in host pipeline
    participant AUTH as ScimProvisioningAuthenticationHandler
    participant AUD as Audit sink
    participant CTRL as SCIM controller
    participant USERS as Identity.Users via ScimUserService

    IdP->>GATE: request to scim v2 path
    alt feature disabled
        GATE-->>IdP: 404 - before any authentication - ScimFeatureGateMiddleware.cs 19-27
    end
    GATE->>AUTH: scheme selected by Authorize attribute
    AUTH->>AUTH: hash lookup then IsValid then lazy rotation finalize - Handler.cs 40-62
    AUTH->>AUD: RecordAuditAsync success or failure - Handler.cs 85 102-110
    AUTH->>CTRL: principal with tenant_id scim_token_id scope claims
    CTRL->>CTRL: scope check read or write - ScimScopes.cs 21 29
    CTRL->>USERS: command via services - maps to Identity.Users records
    CTRL-->>IdP: RFC 7644 response or error body via ScimExceptionFilter
```

Verdicts: feature gate **fail-closed (404)**; auth **fail-closed (401)**; scope **fail-closed (403)**; audit best-effort only in the Null-sink misconfiguration (gap P1).

## 2. Token rotation overlap (zero-downtime IdP cutover)

```mermaid
sequenceDiagram
    participant ADM as Admin with ProvisioningManageTokens
    participant ROT as RotateScimProvisioningTokenCommand
    participant OLD as Old token
    participant NEW as New token
    participant IDP as IdP still holding OLD

    ADM->>ROT: rotate with GracePeriodMinutes
    ROT->>NEW: create replacement
    ROT->>OLD: BeginRotationGrace graceEndsAt - Entities 188-191
    IDP->>OLD: requests keep succeeding during grace
    Note over OLD: once grace expires the NEXT request fails 401 AND finalizes the revocation - lazy - Entities 195-206
    IDP->>NEW: cutover
```

## 3. SCIM write → central audit fan-out

```mermaid
sequenceDiagram
    participant SCIM as SCIM operation
    participant SINK as IScimProvisioningAuditSink
    participant CENTRAL as CentralScimProvisioningAuditEventSink - Audit module
    participant LOG as Central audit log

    SCIM->>SINK: RecordAsync ScimProvisioningAuditEvent
    alt Audit module composed
        SINK->>CENTRAL: bound by AuditModule.cs 137
        CENTRAL->>LOG: required in-transaction row
    else not composed
        SINK->>SINK: NullScimProvisioningAuditSink - completed task - ProvisioningModuleExtensions.cs 57
        Note over SINK: silent drop - gap P1
    end
```
