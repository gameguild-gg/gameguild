# Notifications — Gaps

> Module `GameGuild.Notifications` · develop `ce31d5850c`. KNOWN = tracked in an issue/baseline; NEW = first recorded here.

| # | Severity | Gap | Evidence | Status |
|---|---|---|---|---|
| N1 | high | **Legacy boolean preference gate in the email dispatcher** — `DispatchAsync` re-checks `[Obsolete] ShouldSendNotificationAsync` and holds *any* false for 30 minutes (`PreferenceHoldRecheckInterval`); the modern `DecideDeliveryAsync` (with transactional/urgent bypass) already ran at row creation. A row created under a bypass can be re-held by the legacy gate that lacks the bypass — the in-code "INTERIM ... Reconcile" debt | `Notif:/Services/Email/EmailDispatcherService.cs:26-31, 108-127`; `Services/NotificationPreferenceService.cs:246-256` | NEW |
| N2 | high | **28 of 43 types have no email renderer** — guaranteed dead-letter "No email renderer registered" for any Email-channel row of an uncovered type (producers currently only send covered types; any new email producer silently builds a dead-letter generator) | registry census 04 §3; `EmailDispatcherService.cs:137-144`; `Entities/Notification.cs:366-523` | NEW |
| N3 | medium | **Hold writes bypass the domain model** — quiet-hours/preferences hold sets `NextAttemptAt`+`DeliveryStatus` through `dbContext.Entry(...).CurrentValue` because the entity has no `Hold(nextAttemptAt)` (schema-frozen lane); same pattern re-used by the suppression upsert refresh | `EmailDispatcherService.cs:273-289`; `EmailEventProcessor.cs:100-113` | NEW (in-code acknowledged) |
| N4 | medium | **`NotificationDeliveryStatus.Failed` is dead enum value** — failure paths always return to `Pending`; any UI/baseline distinguishing Failed from retrying-Pending is wrong | `Notif:/Entities/Notification.cs:515-517`; grep — zero assignments | NEW |
| N5 | medium | **Dead-letter sweeps load all Pending email rows** — `DeadLetterPendingSendsAsync` fetches every Pending email row then normalizes client-side (`EmailAddressNormalizer` not SQL-translatable — same EF class as #1278 E9); O(all-pending) per bounce event | `Notif:/Services/Email/EmailEventProcessor.cs:118-135` | NEW |
| N6 | medium | **Unsubscribe tokens never expire and replays are unbounded** — decision A5; a leaked token link (e.g. forwarded mail) permanently mutes a user's channel/category; no rate limit on the endpoint | `Notif:/Services/Email/UnsubscribeTokenService.cs:10-12, 40-60`; controller has no `EnableRateLimiting` | NEW (accepted risk? needs explicit confirmation) |
| N7 | low | **`notifications/subscriptions/:resend` 500s on unknown id** — should be 404; the route lives in Commerce.Subscriptions but sits in the notifications namespace | `Subscriptions/Controllers/SubscriptionNotificationsController.cs:42-60`; `wiring-gap-baseline.txt:249` | KNOWN (#1278 E9 backlog) |
| N8 | low | **InApp quiet-hours drops are silent data loss by design** — decision chain step 6 drops InApp rows during quiet hours (no hold); users lose those notifications entirely | `Notif:/Services/NotificationPreferenceService.cs:229-235` | NEW (documented decision — confirm intended) |
| N9 | low | **`SendBulkAsync` N preference roundtrips + per-row Decide** — bulk fan-out is sequential per recipient with a DB read each; fine at current scale, no batching | `Notif:/Services/NotificationDeliveryService.cs:175-236` | NEW |
| N10 | low | **Slack/Discord/Webhook channels have no dispatcher** — enum values 4-6 (`Entities/Notification.cs:537-548`) with no worker and no renderer; rows of those channels would sit Pending forever | module file census — only Email/Digest/Sms/Push/WhatsApp workers exist | NEW |
| N11 | info | **Renderer registry duplicate-type would crash DI** — `renderers.ToDictionary(r => r.Type)` throws on duplicate key; safe today (15 distinct types) but the invariant is implicit | `Notif:/Services/Email/EmailRendererRegistry.cs:11-13` | NEW (guard candidate) |
| N12 | info | **Legacy metadata backfill runs every sweep** — `ProtectLegacyMetadataAsync` scans up to 1000 unprotected legacy rows each 30s pass with `IgnoreQueryFilters` across tenants; amortizes history but is a standing cost until backlog drains | `Notif:/Services/Email/EmailDispatcherService.cs:216-244` | NEW |
| — | info | Historical: push dispatcher polling while disabled (#1218 G6) — **fixed** (worker exits when `PushDeliveryOptions.Enabled=false`, `PushDispatcherBackgroundService.cs:14-19`) | | KNOWN (fixed) |

## Recommended acceptance order

1. N1 — delete the legacy re-check (rows already carry the decision) or make the re-check call `DecideDeliveryAsync` and honor Drop-vs-Hold; this is the only gap that can *delay transactional mail*.
2. N2 — add a startup/architecture guard: every NotificationType used with `NotificationChannel.Email` by any producer must have a registered renderer (the registry knows; a test can assert coverage).
3. N6 — confirm A5 (no expiry) with security sign-off, or add a validity window + re-issue tokens per send.
4. N5 — persist a normalized `RecipientEmail` column (or SQL function) so suppression matches in SQL.
5. N7/N4/N10 — cheap hygiene: 404 on resend, remove or implement Failed, document-or-implement the three channel orphans.
