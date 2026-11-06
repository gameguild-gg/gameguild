# Notifications — Cross-module sequences

> Module `GameGuild.Notifications` · develop `ce31d5850c`. Shorthand `Notif:` = module root.

## 1. Producer → row → renderer → provider → user (email happy path)

```mermaid
sequenceDiagram
    participant P as Producer module<br/>e.g. Identity handler
    participant NS as NotificationDeliveryService
    participant PS as NotificationPreferenceService
    participant DB as Notifications tables
    participant W as EmailDispatcherBackgroundService
    participant R as EmailRendererRegistry
    participant SES as IEmailSender SES
    participant U as User inbox

    P->>NS: SendAsync recipientId type title channel Email
    NS->>PS: DecideDeliveryAsync
    alt Drop
        NS-->>P: Failure Notification-Skipped no row
    else Digest
        NS->>DB: INSERT Pending then HeldForDigest
    else HoldUntil
        NS->>DB: INSERT Pending ScheduledAt quiet-end
    else Send
        NS->>DB: INSERT Pending
    end
    loop every 30s batch 50
        W->>DB: sweep Pending due + reclaim Sending older 10m
        W->>R: Resolve type
        alt no renderer
            W->>DB: MarkDeadLettered no-renderer N2
        else renderer found
            W->>W: render null means skip → Sent
            W->>SES: Send EmailMessage ToEmail resolver-overwritten
            SES-->>W: providerMessageId
            W->>DB: MarkDeliverySent id
        end
    end
    SES->>U: deliver
```

## 2. Tenant invite — the only outbox-driven producer

```mermaid
sequenceDiagram
    participant T as Identity-Tenants command handler
    participant E as member entity
    participant OB as Platform outbox
    participant H as TenantInviteRequestedHandler
    participant NS as NotificationDeliveryService
    participant W as Email dispatcher

    T->>E: AddIntegrationEvent TenantMemberInviteRequestedV1
    note over T,E: AddTenantMemberCommandHandler-cs-131 resend via UpdateTenantMemberInvite-cs-105
    E->>OB: SaveChanges drains IHasIntegrationEvents
    OB->>H: HandleAsync event durable retry
    H->>H: guards member live not cancelled has email tenant live
    H->>NS: SendAsync recipientId null RecipientEmail invitee<br/>type TenantInvite channel Email
    note over NS: transactional type bypasses prefs DeliveryService-cs-89-93
    NS->>W: row Pending → swept → TenantInviteRenderer → SES
```

## 3. SES/SNS webhook ingest with suppression side effects

```mermaid
sequenceDiagram
    participant SNS as AWS SNS
    participant C as EmailEventsController anonymous
    participant H as ReceiveEmailEventsCommandHandler
    participant V as SnsMessageVerifier
    participant PR as EmailEventProcessor
    participant DB as Notifications tables

    SNS->>C: POST raw body
    C->>H: ReceiveEmailEventsCommand body
    H->>V: ValidateRequest
    alt invalid signature
        H-->>C: 401 generic
    else topic mismatch
        H-->>C: 403
    else SubscriptionConfirmation
        H->>H: visit SubscribeURL only if host pinned sns-*.amazonaws-com
        H-->>C: 200 or 502
    else Notification
        H->>H: BuildEvent parse mail-messageId destination eventType<br/>strip ipAddress userAgent cap payload 4000
        H->>DB: exists SnsMessageId? idempotent skip
        H->>PR: ProcessAsync tracked-entities only no save
        alt hard bounce Permanent or Undetermined or complaint
            PR->>DB: upsert suppression normalized address<br/>re-suppress if released
            PR->>DB: dead-letter Pending rows to that address client-side normalized
        end
        H->>DB: single SaveChanges atomic event+suppression+deadletters
        note over H,DB: DbUpdateException race → verify duplicate → re-run processor → ack so SNS never retries a stored event
        H-->>C: 200
    end
```

## 4. One-click unsubscribe

```mermaid
sequenceDiagram
    participant U as Email client
    participant C as UnsubscribeController anonymous
    participant T as UnsubscribeTokenService
    participant PS as NotificationPreferenceService

    U->>C: GET unsubscribe-token
    C->>T: Validate DataProtect purpose notification-unsubscribe-v1 no expiry
    alt invalid
        C-->>U: 400 generic
    else scope type non-transactional
        C->>PS: SetMutedTypes append
    else scope category
        C->>PS: UpdatePreferences category false
    else scope all
        C->>PS: UpdatePreferences emailEnabled false
    end
    C-->>U: 200 unsubscribed + manage URL
```

## 5. Digest bundling window

```mermaid
sequenceDiagram
    participant W as DigestDispatcherService every 15m
    participant DB as tables
    participant SES as IEmailSender

    W->>DB: distinct users with HeldForDigest rows
    loop per user
        W->>DB: prefs EmailDigestFrequency?
        alt gone or off
            W->>DB: requeue rows → Pending individual delivery
        else Daily or Weekly Monday or BiWeekly ISO-even Monday local 08:00
            W->>DB: atomic claim HeldForDigest → Sending via Version concurrency
            W->>SES: one DigestRenderer email rows CreatedAt ≤ fire time
            SES-->>W: accepted
            W->>DB: rows MarkDeliverySent no single provider id
        end
    end
    note over W,DB: crash window claimed-Sending rows reclaimed by email dispatcher after 10m stale and delivered individually or dead-lettered — visible never lost DigestDispatcherService-cs-21-31
```

## 6. The WhatsApp saga end-to-end (fail-closed chain)

```mermaid
sequenceDiagram
    participant U as User
    participant API as preferences API
    participant D as DecideDeliveryAsync
    participant W as WhatsAppDispatcherService
    participant P as IWhatsAppProvider

    U->>API: PUT preferences whatsAppEnabled true explicit opt-in
    note over API: default false Entities-NotificationPreference-cs-46-118
    U->>U: something wants to notify them on WhatsApp
    D->>D: step 0 gate WhatsAppEnabled else Drop even urgent
    alt not opted in
        D-->>D: Drop whatsapp-not-opted-in
    else opted in
        D->>W: row Pending channel WhatsApp
        loop sweep
            W->>W: phone from metadata or profile else dead-letter
            W->>P: SendTemplate allowlisted name or SendMessage free-form
            alt NoOp unconfigured
                P-->>W: declined → throw → retry backoff → dead-letter observably
            else Business API configured
                P-->>W: providerMessageId → Sent
            end
        end
    end
    note over P: settings ValidateOnStart fails boot when Enabled without credentials WhatsAppSettings-cs-118-123
```
