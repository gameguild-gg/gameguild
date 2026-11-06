# Notifications — User flows

> Module `GameGuild.Notifications` · develop `ce31d5850c`. Shorthand `Notif:` = module root. Family numbering F1-F6 matches 07/08.

## F1 — Read and manage in-app notifications

```mermaid
flowchart TD
    A[User GET api-notifications skip take isRead] --> B[GetUserNotificationsAsync<br/>InApp channel only<br/>NotificationDeliveryService.cs-30-52]
    B --> C[200 newest-first page]
    D[GET unread-count] --> E[Count Unread InApp<br/>NotificationDeliveryService.cs-54-60]
    F[POST id-read] --> G{row exists?<br/>Controller-121-129}
    G -->|404| Z1[404]
    G --> H{RecipientId eq actor?<br/>Controller-131-135}
    H -->|no| Z2[403 Forbid]
    H -->|yes| I[MarkNotificationReadCommand<br/>NotificationMutationCommands]
    I --> J[MarkAsRead sets ReadAt<br/>Entities-Notification.cs-219-233]
    K[POST read-all] --> L[MarkAllNotificationsReadCommand user-scoped]
    M[POST id-unread] --> N[MarkAsUnread clears ReadAt]
    O[DELETE id] --> P[DeleteNotificationCommand SoftDelete]
    Q[DELETE read] --> R[DeleteReadNotificationsCommand returns count]
```

Edge cases:

| Case | Behavior | Evidence |
|---|---|---|
| Non-InApp rows invisible to the list | `Channel == InApp` filter — email/push/SMS rows never appear in-app | `Notif:/Services/NotificationDeliveryService.cs:38, 57` |
| Ownership enforced per-row on read/unread/delete | explicit `RecipientId != userId → Forbid()` | `Notif:/Controllers/NotificationsController.cs:106-111, 131-135, 179-211` |
| `read-all` / `delete-read` need no row check | scoped by userId in the command | `:149-162, 225-238` |
| Unread of an already-read row | allowed (state flip back) | `Entities/Notification.cs:225-233` |

## F2 — Preference management (the self-service settings page)

```mermaid
flowchart TD
    A[GET api-notifications-preferences] --> B[GetPreferencesAsync get-or-create default row<br/>NotificationPreferenceService.cs-11-24]
    C[PUT preferences] --> D[UpdateNotificationPreferencesCommand<br/>channels categories whatsapp<br/>Controller-261-287]
    D --> E[UpdateChannelPreferences SetWhatsAppEnabled UpdateCategoryPreferences<br/>Entities-NotificationPreference.cs-147-160]
    E --> F[SaveChanges 200 dto]
    G[PUT preferences-quiet-hours] --> H[SetQuietHoursCommand start end timezone<br/>null start clears]
    I[PUT preferences-muted-types] --> J[Enum-parse each name unknown → 400<br/>full-replace semantics empty clears<br/>Controller-317-349]
    K[PUT preferences-digest-frequency] --> L[null Daily Weekly BiWeekly parse → 400<br/>Controller-354-381]
    M[GET types-catalog] --> N[43 types minus Custom with category + suppressible flag<br/>Controller-386-398]
```

Edge cases:

| Case | Behavior | Evidence |
|---|---|---|
| Preference row lazily created on first read | default: all channels on, WhatsApp **off**, quiet-hours bypass = Urgent | `Notif:/Entities/NotificationPreference.cs:118-123`; `Services/NotificationPreferenceService.cs:16-21` |
| Muted types stored as JSON names, case-insensitive | malformed JSON treated as empty (no crash) | `Entities/NotificationPreference.cs:214-227` |
| Transactional types flagged non-suppressible in the catalog | drives the UI; enforcement lives in the decision chain + unsubscribe guard | `Notif:/NotificationCategories.cs:14-27`; `Controller:397` |
| WhatsApp toggle is **explicit opt-in only** | default false; nothing auto-enables it | `Entities/NotificationPreference.cs:46`; 03 §4 |

## F3 — One-click unsubscribe (anonymous, signed token)

```mermaid
flowchart TD
    A[Email client GET api-v1-notifications-unsubscribe-token] --> B[AllowAnonymous endpoint<br/>NotificationUnsubscribeController.cs-27-35]
    B --> C[UnsubscribeNotificationCommand<br/>UnsubscribeNotificationCommand.cs-35-57]
    C --> D{token present and valid?<br/>DataProtection purpose notification-unsubscribe-v1}
    D -->|no| E[400 generic message no oracle]
    D -->|scope type| F{type is Transactional?<br/>-59-65}
    F -->|yes| G[422 cannot unsubscribe]
    F -->|no| H[append to MutedTypes<br/>SetMutedTypesAsync]
    D -->|scope category| I{one of marketing social learning achievements?}
    I -->|no| J[422 unknown or non-suppressible]
    I -->|yes| K[category toggle to false]
    D -->|scope all| L[EmailEnabled false]
    H --> M[200 unsubscribed + ManageUrl]
    K --> M
    L --> M
```

Edge cases:

| Case | Behavior | Evidence |
|---|---|---|
| Tokens never expire | recorded decision A5 — no expiry field at all | `Notif:/Services/Email/UnsubscribeTokenService.cs:10-12` |
| Tampered/malformed indistinguishable | single generic 400; catch-all | `:54-59` |
| Idempotent repeat clicks | re-applying the same toggle is a no-op write | handler shape `:59-116` |
| ManageUrl from config | `App:BaseUrl` + `/workspace/settings/notifications` | `:138-142` |

## F4 — Admin email deliverability console

```mermaid
flowchart TD
    A[Admin Policies-Admin] --> B[GET v1-email-delivery-email-events<br/>filters eventType email providerMessageId]
    C[GET v1-email-delivery-suppressions includeReleased] --> D[suppression rows newest-first]
    E[DELETE v1-email-delivery-suppressions-email] --> F[ReleaseEmailSuppressionCommand<br/>idempotent 200]
    G[GET v1-email-delivery-deadletters type email] --> H[Notification rows DeadLettered]
    I[POST v1-email-delivery-notifications-id-colon-requeue] --> J[RequeueEmailNotificationCommand<br/>MarkRequeued Pending RequeueCount++]
    K[GET v1-email-delivery-notifications-id-timeline] --> L[EmailDeliveryEvent join on ProviderMessageId]
```

Edge cases:

| Case | Behavior | Evidence |
|---|---|---|
| Payload never exposed raw | DTO carries parsed fields + 500-char preview | `Notif:/Controllers/EmailDeliveryAdminController.cs:218-221` |
| Requeue only from DeadLettered | entity guard; otherwise 409 shape via `FromError` | `Entities/Notification.cs:312-325`; `Controller:128-141` |
| Requeue keeps audit | `LastError`/`AttemptCount` deliberately retained | `Entities/Notification.cs:308-311` doc comment |
| Admin = platform-wide | `Policies.Admin` (`Authz:/Policies.cs:111`), no tenant scoping on the console | controller class attribute `:18` |

## F5 — WhatsApp broadcast campaign (admin author + scheduler)

```mermaid
flowchart TD
    A[Admin GET v1-whatsapp-status] --> B[provider enabled template allowlist]
    C[POST v1-whatsapp-broadcasts] --> D[CreateWhatsAppBroadcastCommand Draft]
    D --> E[201 campaign]
    F[POST id-colon-schedule] --> G[Schedule only from Draft<br/>Entities-WhatsAppBroadcast.cs-97-109]
    H[POST id-colon-send] --> I[MarkSending now<br/>-118-127]
    J[POST id-colon-cancel] --> K[only Draft or Scheduled<br/>-153-162]
    L[Scheduler worker] --> M[due Scheduled campaigns<br/>WhatsAppBroadcastSchedulerBackgroundService.cs-55-56]
    M --> N[audience resolve opt-in only cap 500<br/>WhatsAppBroadcastOptions.cs-15]
    N --> O[per-recipient WhatsAppBroadcastRecipient rows]
    O --> P[WhatsAppDispatcherService sends template sends]
    P --> Q[MarkSent RecordRecipients or MarkFailed]
```

Edge cases:

| Case | Behavior | Evidence |
|---|---|---|
| Campaign audience is opt-in filtered | resolver honors `WhatsAppEnabled` preferences (the #54/#55 saga) | `Notif:/Services/WhatsApp/WhatsAppBroadcastAudienceResolver.cs` |
| Bounded blast | `MaxRecipientsPerBroadcast = 500` default | `WhatsAppBroadcastOptions.cs:15` |
| Template allowlist | free-form send only when row lacks allowlisted template name | `WhatsAppDispatcherService.cs:109-116`; `WhatsAppSettings.cs:60` |
| Unconfigured provider | NoOp declines → campaign MarkFailed observably | `NotificationsModule.cs:63-75`; broadcast entity `:145-151` |

## F6 — Consumer-side flows (documented from the user's chair)

- **Receive an identity email** (verification / reset / magic-link / codes): producer handler → `SendAsync` → Email row → sweep → renderer (registered by Identity.Authentication) → SES → inbox; delivery events return via the webhook and build the timeline (F4).
- **Digest subscriber**: preference `EmailDigestFrequency` set → suppressible email rows quarantine `HeldForDigest` → digest engine fires daily / Monday-weekly / ISO-week-even Monday at user-local 08:00 → one bundled email; rows marked Sent with no single provider id (`Entities/Notification.cs:271-282`).
- **Quiet-hours user**: Email rows hold until window end (`NotificationDeliveryService.cs:105`); InApp drops (`NotificationPreferenceService.cs:229-241`).
