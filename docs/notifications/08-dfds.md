# Notifications — Data-flow diagrams

> Module `GameGuild.Notifications` · develop `ce31d5850c`. L0 context + L1 per family (numbering matches 01/07).

## L0 — Context (producer fan-in → dispatch fan-out)

```mermaid
flowchart LR
    subgraph Producers[Producer modules 13 families]
        ID[Identity-Authentication x10 handlers]
        IT[Identity-Tenants via outbox event]
        PAY[Commerce-Payments]
        BIL[Commerce-Billing]
        SUB[Commerce-Subscriptions]
        SEC[API Core security alerts x10]
        AUD[Compliance-Audit LegalHold]
        LRN[Learning Assessments-Courses]
        SLA[Monitoring-SLA]
        ANN[Social-Announcements]
        TST[TestingLab]
    end
    USER[Authenticated user] --> PREFS[Preferences types-catalog]
    ADM[Admin] --> CONSOLE[Deliverability console<br/>WhatsApp broadcast console]
    ANON[Anonymous internet] --> UNsub[Unsubscribe GET]
    SNS[AWS SNS] --> WB[Email events webhook]
    Producers --> SEND[INotificationService-SendAsync]
    SEND --> QUEUE[(Notifications table<br/>Pending rows per channel)]
    QUEUE --> WORKERS[6 background workers]
    WORKERS --> SES[SES email]
    WORKERS --> SMS[SMS sender]
    WORKERS --> PUSH[Push sender]
    WORKERS --> WA[WhatsApp Cloud API]
    SES --> INBOX[User inboxes]
    WB --> QUEUE
    UNsub --> PREFS
```

Trust boundaries: TB1 internet→API (anonymous endpoints verified by signature/token, not authN), TB2 producer modules→SendAsync (in-process, trusted code), TB3 API→DB, TB4 API→external providers (SES/SMS/push/WhatsApp), TB5 AWS→webhook (SNS signature).

## L1-F1 — Producer → Email row → renderer → provider → user

```mermaid
flowchart TD
    P[Producer module trusted TB2] --> S[SendAsync]
    S --> D{DecideDeliveryAsync chain<br/>steps 0-6}
    D -->|Drop| X[no row LogDebug]
    D -->|Digest| R1[(row HeldForDigest)]
    D -->|HoldUntil| R2[(row Pending + ScheduledAt)]
    D -->|Send| R3[(row Pending)]
    R3 --> W[EmailDispatcher sweep 30s batch 50 TB3]
    W --> V{renderer in registry?}
    V -->|no| DL[(DeadLettered no-renderer N2)]
    V -->|yes| RES[Resolve recipient email profile fallback]
    RES --> SUP{active suppression?}
    SUP -->|yes| DL2[(DeadLettered suppressed)]
    SUP -->|no| REN[renderer builds EmailMessage from protected metadata]
    REN --> SES[IEmailSender SES TB4]
    SES --> M[Mail]
    SES -.-> PID[ProviderMessageId on row]
    M --> U[User inbox]
    style DL fill:#fdd
    style DL2 fill:#fdd
```

## L1-F2 — SNS webhook ingest → suppression + timeline

```mermaid
flowchart TD
    A[AWS SNS TB5] --> B[EmailEventsController anonymous]
    B --> C{SnsMessageVerifier<br/>signature + cert pin + topic allow-list}
    C -->|fail| F1[400-401-403 generic]
    C -->|SubscriptionConfirmation| D[GET SubscribeURL host pinned sns-amazonaws]
    C -->|Notification| E[parse mail fields strip ip-ua]
    E --> G[(EmailDeliveryEvent unique SnsMessageId)]
    G --> H{hard bounce or complaint?}
    H -->|yes| I[(EmailSuppression upsert normalized)]
    H -->|yes| J[(dead-letter Pending rows to address N5 full-scan)]
    H -->|no| K[record only]
    I & J & G --> L[one atomic SaveChanges]
    L --> M[200 ack SNS no retry]
    G -.-> N[timeline join by ProviderMessageId]
```

## L1-F3 — Unsubscribe (public token mutator)

```mermaid
flowchart TD
    A[Anonymous GET token TB1] --> B{DataProtection validate}
    B -->|invalid| C[400 generic]
    B -->|scope type| D{transactional?}
    D -->|yes| E[422]
    D -->|no| F[(MutedTypes append)]
    B -->|scope category| G[(category toggle false)]
    B -->|scope all| H[(EmailEnabled false)]
    F & G & H --> I[200 + manage URL]
    style A stroke:orange
```

## L1-F4 — Digest engine

```mermaid
flowchart TD
    W[DigestDispatcher 15m] --> C[candidate users with held rows]
    C --> P{frequency still set?}
    P -->|no| RE[(rows → Pending individual)]
    P -->|yes window due local 08-00| CL[atomic claim Version token]
    CL --> R[DigestRenderer bundle]
    R --> SES[SES TB4]
    SES --> S[(rows MarkDeliverySent no provider id)]
    CL -.->|crash| RC[email dispatcher reclaim 10m]
```

## L1-F5 — WhatsApp broadcast campaign

```mermaid
flowchart TD
    ADM[Admin TB1 Policies-Admin] --> CR[(WhatsAppBroadcast Draft)]
    CR -->|schedule-send| SCH[(Scheduled → Sending)]
    SW[Scheduler worker] --> SCH
    SCH --> AUD[AudienceResolver opt-in only cap 500]
    AUD --> RC[(WhatsAppBroadcastRecipient rows)]
    RC --> WD[WhatsAppDispatcher template-or-freeform TB4]
    WD --> WA[Cloud API or NoOp decline]
    WA -->|ok| OK[(MarkSent provider ids)]
    WA -->|decline| FA[(MarkFailed error)]
```

## Data stores summary

| Store | Written by | Read by |
|---|---|---|
| `Notifications` | producers via SendAsync, dispatchers lifecycle, digest engine, processor dead-letters, admin requeue | user routes, dispatchers, admin console |
| `NotificationPreferences` | preferences routes, unsubscribe, digest requeue checks | decision chain |
| `NotificationTemplates` | (seed/admin) | SendFromTemplateAsync |
| `EmailDeliveryEvents` | webhook ingest | admin feed, timeline |
| `EmailSuppressions` | processor upsert, admin release | dispatcher pre-send, admin |
| `WhatsAppBroadcast(Recipient)` | admin console, scheduler | admin console |
