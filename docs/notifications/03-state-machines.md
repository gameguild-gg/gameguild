# Notifications — State machines

> Module `GameGuild.Notifications` · develop `ce31d5850c`. Shorthand `Notif:` = module root.

## 1. Notification row — delivery lifecycle (`NotificationDeliveryStatus`, `Entities/Notification.cs:501-523`)

The cross-channel queue→render→dispatch machine. InApp short-circuits it (born Sent); email/push/SMS/WhatsApp ride the full graph.

```mermaid
stateDiagram-v2
    [*] --> Pending : Notification-Create at SendAsync<br/>ScheduledAt when held DeliveryService-cs-105
    [*] --> Sent : InApp rows marked at creation<br/>DeliveryService-cs-121-125
    Pending --> HeldForDigest : preference decision Digest<br/>MarkHeldForDigest cs-292-299
    HeldForDigest --> Sending : digest engine atomic claim Version token<br/>DigestDispatcherService-cs-21-26
    HeldForDigest --> Pending : digest off while rows held → requeue orphans<br/>DigestDispatcherService-cs-83-87
    Pending --> Sending : dispatcher claims ClaimForSending<br/>Notification-cs-337-349 persisted pre-send
    Sending --> Sent : provider accepted MarkDeliverySent providerMessageId<br/>Notification-cs-262-290
    Sending --> Pending : attempt failed MarkDeliveryAttemptFailed<br/>AttemptCount++ NextAttemptAt backoff cs-292-301
    Pending --> DeadLettered : attempts exhausted or TTL stale<br/>EmailDispatcherService-cs-259-271
    Sending --> DeadLettered : permanent errors renderer-missing recipient-unresolved suppressed<br/>cs-137-172
    DeadLettered --> Pending : admin requeue MarkRequeued keeps audit cs-312-325
    Sent --> Read : user POST read InApp only<br/>MarkAsRead cs-219-227
    Read --> Sent : mark unread clears ReadAt
    Sent --> Deleted : soft delete Delete<br/>Notification-cs-327-330
```

Machine honesty notes:

| # | Truth | Evidence |
|---|---|---|
| 1 | `Failed` enum value (3) is **never assigned** — failure writes return to `Pending` with `AttemptCount++`; the value is display-only | grep `NotificationDeliveryStatus.Failed` → declarations only (`Entities/Notification.cs:515-517`) |
| 2 | The dispatcher's quiet-hours hold writes `Pending` + `NextAttemptAt` through the EF change API (no domain method) because the entity has no Hold transition — interim, documented in-code | `Notif:/Services/Email/EmailDispatcherService.cs:273-289` |
| 3 | `Sending` rows older than 10 minutes are reclaimed into the sweep (crash between claim and send) | `EmailDispatcherService.cs:23-24, 53-54` |
| 4 | Holds must not consume retry attempts — hence the change-API bypass instead of `MarkDeliveryAttemptFailed` | `:275-277` doc |
| 5 | Requeue keeps `LastError`/`AttemptCount` deliberately (audit of the failure that dead-lettered) | `Entities/Notification.cs:308-311` |

## 2. Read/unread (InApp projection)

```mermaid
stateDiagram-v2
    [*] --> Unread
    Unread --> Read : MarkAsRead sets ReadAt idempotent
    Read --> Unread : MarkAsUnread clears ReadAt
    Read --> Deleted : DELETE read cleanup
    Unread --> Deleted : DELETE single
```

## 3. WhatsApp broadcast campaign (`WhatsAppBroadcastStatus`, `Entities/WhatsAppBroadcast.cs:166-173`)

```mermaid
stateDiagram-v2
    [*] --> Draft : Create
    Draft --> Scheduled : Schedule guarded only-from-Draft cs-97-109
    Scheduled --> Sending : MarkSending scheduler or manual send<br/>also Draft → Sending allowed cs-118-127
    Draft --> Sending : manual send now
    Sending --> Sent : MarkSent RecordRecipients cs-130-143
    Sending --> Failed : MarkFailed error kept cs-145-151
    Draft --> Cancelled : Cancel guarded
    Scheduled --> Cancelled : Cancel guarded only Draft-Scheduled cs-153-162
    Sent --> [*]
    Failed --> [*]
    Cancelled --> [*]
```

Guards are real (InvalidOperationException on illegal transitions) — unlike several Social blind setters (N4 class).

## 4. Preference decision chain — the effective per-notification "state selector"

Not a persisted machine; `DecideDeliveryAsync` (`Notif:/Services/NotificationPreferenceService.cs:150-244`) picks the row's fate in fixed order:

| Step | Gate | Outcome |
|---|---|---|
| 0 | WhatsApp channel and (no pref row or WhatsAppEnabled false) | **Drop whatsapp-not-opted-in** — even Urgent/transactional |
| 1 | Transactional type set or priority Urgent | Send (bypass) |
| 2 | Type in muted set | Drop muted |
| 3 | Channel toggle off | Drop channel-disabled |
| 4 | Category toggle off (Marketing/Social/Learning/Achievements) | Drop category-disabled |
| 5 | Email and digest frequency set and priority < bypass | **Digest** (row HeldForDigest) |
| 6 | In quiet hours and priority < bypass | InApp → Drop quiet-hours; Email → **HoldUntil** window end; others Send |
| — | none hit | Send |

The **whatsAppEnabled saga** (#54/#55): explicit opt-in default-off at the preference (`Entities/NotificationPreference.cs:46,118,147-151`), fail-closed decision step 0, provider fail-closed (NoOp declines visibly, `NotificationsModule.cs:63-75`), settings `ValidateOnStart` refuses Enabled-without-credentials (`WhatsAppSettings.cs:118-123`), and the broadcast audience resolver filters opt-ins only — the channel cannot send a single message by accident or default.

## 5. Email suppression lifecycle (`EmailSuppression`, `Entities/EmailSuppression.cs`)

```mermaid
stateDiagram-v2
    [*] --> Active : hard bounce Permanent-Undetermined or complaint<br/>EmailEventProcessor-cs-24-34
    Active --> Active : refresh BounceType-SourceEventId change-API upsert cs-100-113
    Active --> Released : admin DELETE suppression<br/>ReleaseEmailSuppressionCommand
    Released --> Active : re-suppress on new event clears ReleasedAt<br/>cs-116-123
```

- "Permanent" and **"Undetermined"** bounces both suppress (`EmailEventProcessor.cs:64-67`) — conservative by design.
- Complaint suppression is the sender-reputation guard; release is admin-only and idempotent.
