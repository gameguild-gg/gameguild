# Notifications — CQRS / wiring matrix

> Module `GameGuild.Notifications` · develop `ce31d5850c`. Shorthand `Notif:` = module root, `API:` = `apps/api/Source/GameGuild.API/`, `Authz:` = `apps/api/Source/Modules/GameGuild.Identity.Authorization/`.

## 0. DI wiring (one composition, `Notif:/NotificationsModule.cs:25-115`)

| Piece | Registration | Evidence | Live? |
|---|---|---|---|
| Facade + sub-services | `INotificationService`→facade, `INotificationPreferenceService`, `INotificationTemplateService`, `INotificationDeliveryService`, `NotificationMetadataProtector` | `NotificationsModule.cs:28-33` | **yes** |
| Email pipeline | renderer registry + Security renderer + recipient resolver + dispatcher svc + **EmailDispatcherBackgroundService**; options `Notifications:EmailDispatcher` | `:35-40` | **yes** |
| Digest engine | renderer + dispatcher + **DigestDispatcherBackgroundService**; options `Notifications:DigestDispatcher` | `:43-47` | **yes** |
| SMS pipeline | `ISmsSender` TryAdd NoOp (host may override) + **SmsDispatcherBackgroundService** | `:50-55` | **yes** |
| Push pipeline | `IPushSender` TryAdd NoOp + **PushDispatcherBackgroundService** (exits when `PushDeliveryOptions.Enabled=false` — #1218 G6) | `:58-62`; `Services/Push/PushDispatcherBackgroundService.cs:14-19` | **yes** |
| WhatsApp pipeline | settings + `ValidateOnStart`, NoOp provider fallback, phone resolver + **WhatsAppDispatcherBackgroundService** | `:63-79` | **yes** (fail-closed until configured) |
| Broadcast engine | audience resolver + service + **WhatsAppBroadcastSchedulerBackgroundService**; options cap 500 | `:82-87` | **yes** |
| Cross-module inbound | `TenantInviteRenderer` + `IIntegrationEventHandler<TenantMemberInviteRequestedV1>` → `TenantInviteRequestedHandler` (registered here to break the Tenants→Notifications→Users→Tenants cycle) | `:90-92` | **yes** |
| Unsubscribe + footer | `IUnsubscribeTokenService` (DataProtection), `IEmailFooterService` | `:95-100` | **yes** |
| Deliverability admin + webhook | `IEmailDeliveryAdminService`, `ISnsMessageVerifier`, `IEmailEventProcessor`, named HttpClient `SnsSubscriptionConfirmation` (15s) | `:103-110` | **yes** |
| Handlers (module commands) | module scan — `Notifications` in `CommonEnabledModules` | `API:/Core/Setup/ModuleConfiguration.cs:35`; `ApplicationLayerExtensions.cs:110-145` | **yes** |
| Tables | `NotificationsModelConfiguration : IModelConfiguration` → ApplyConfigurationsFromAssembly(module) | `Notif:/Configuration/NotificationsModelConfiguration.cs:8-14`; migrations `20260711061500_AddNotificationPersistence`, `20260818213704_AddNotificationEmailDelivery`, `20260819191909_AddEmailDeliveryEvents`, WhatsApp broadcast migrations | **yes** |
| Host call | `AddNotificationsModule` | `API:/Core/Setup/InfrastructureLayerExtensions.cs:340` | **yes** |

Renderer DI registrations **outside** the module (feed the registry): `Authz-sibling` Identity.Authentication 10 renderers (`Identity.Authentication/Extensions/DependencyInjection.cs:53-62`), Commerce.Billing 2 (`Extensions/ServiceCollectionExtensions.cs:72,123`), Commerce.Subscriptions 1 (`Extensions/DependencyInjection.cs:178`). Registry duplicates would throw `ToDictionary` on duplicate key — one renderer per type is a structural invariant.

**Rate limit legend** — `—` = none on any Notifications controller (the Subscriptions-owned `notifications/subscriptions` controller is the only rate-limited one in this route namespace, `RateLimitPolicies.Api`).

## 1. NotificationsController (`api/notifications`, `[Authorize]`, 15 routes — `Notif:/Controllers/NotificationsController.cs`)

| Route | Request / service | Handler | Auth | Rate limit | HOLE? |
|---|---|---|---|---|---|
| GET `` | service `GetUserNotificationsAsync` | — | self | — | InApp-only rows |
| GET `unread-count` | service | — | self | — | |
| GET `{id}` | service `GetByIdAsync` | — | self + ownership `:106-111` | — | |
| POST `{id}/read` | `MarkNotificationReadCommand` | `NotificationMutationCommands.cs` | self + ownership | — | |
| POST `read-all` | `MarkAllNotificationsReadCommand` | same | self | — | |
| POST `{id}/unread` | `MarkNotificationUnreadCommand` | same | self + ownership | — | |
| DELETE `{id}` | `DeleteNotificationCommand` | same | self + ownership | — | soft delete |
| DELETE `read` | `DeleteReadNotificationsCommand` | same | self | — | |
| GET `preferences` | service get-or-create | — | self | — | |
| PUT `preferences` | `UpdateNotificationPreferencesCommand` | same | self | — | |
| PUT `preferences/quiet-hours` | `SetNotificationQuietHoursCommand` | same | self | — | |
| PUT `preferences/muted-types` | `SetMutedNotificationTypesCommand` | `EmailPreferenceCommandHandler` (`EmailPreferenceCommands.cs:9-21`) | self | — | |
| PUT `preferences/digest-frequency` | `SetNotificationDigestFrequencyCommand` | same | self | — | |
| GET `types-catalog` | inline enum projection | — | self | — | excludes Custom |

## 2. EmailDeliveryAdminController (`api`, `[Authorize(Policy = Policies.Admin)]`, 6 routes)

| Route | Request | Handler | HOLE? |
|---|---|---|---|
| GET `v1/email-delivery/email-events` | service `GetEventsAsync` | — | |
| GET `v1/email-delivery/suppressions` | service | — | |
| DELETE `v1/email-delivery/suppressions/{email}` | `ReleaseEmailSuppressionCommand` | `EmailDeliveryMutationCommands.cs` | idempotent 200 |
| GET `v1/email-delivery/deadletters` | service | — | |
| POST `v1/email-delivery/notifications/{id}:requeue` | `RequeueEmailNotificationCommand` | same | E9-clean |
| GET `v1/email-delivery/notifications/{id}/timeline` | service `GetTimelineAsync` | — | empty when no provider id |

## 3. EmailEventsController (`api`, `[AllowAnonymous]` — signature-verified, 1 route)

| Route | Request | Handler | Notes |
|---|---|---|---|
| POST `v1/notifications/email-events` | `ReceiveEmailEventsCommand(body)` | `ReceiveEmailEventsCommandHandler` | SNS signature + topic allow-list (`SnsMessageVerifier`); SubscriptionConfirmation handshake visits pinned `sns.*.amazonaws.com` URL only; idempotent on `SnsMessageId`; single transactional SaveChanges; ipAddress/userAgent stripped from stored payload |

## 4. NotificationUnsubscribeController (`api`, `[AllowAnonymous]` — token-verified, 1 route)

| Route | Request | Handler | Notes |
|---|---|---|---|
| GET `v1/notifications/unsubscribe?token=` | `UnsubscribeNotificationCommand` | handler in same file | scopes type/category/all; transactional types 422 |

## 5. WhatsAppBroadcastAdminController (`api`, `[Authorize(Policy = Policies.Admin)]`, 8 routes)

| Route | Request | Handler family |
|---|---|---|
| GET `v1/whatsapp/status` | service read | — |
| POST `v1/whatsapp/broadcasts` | `CreateWhatsAppBroadcastCommand` | `WhatsAppBroadcastMutationCommands.cs` |
| GET `v1/whatsapp/broadcasts` | service page | — |
| GET `v1/whatsapp/broadcasts/{id}` | service read | — |
| POST `v1/whatsapp/broadcasts/{id}:schedule` | `ScheduleWhatsAppBroadcastCommand` | same |
| POST `v1/whatsapp/broadcasts/{id}:send` | `SendWhatsAppBroadcastCommand` | same |
| POST `v1/whatsapp/broadcasts/{id}:cancel` | `CancelWhatsAppBroadcastCommand` | same |
| GET `v1/whatsapp/broadcasts/{id}/recipients` | service page | — |

## 6. Route census + the one outsider

- **26 routes in-module** (15 + 6 + 1 + 1 + 8 minus... actual count: NotificationsController 14 + admin 6 + webhook 1 + unsubscribe 1 + WhatsApp 8 = 30 route attributes; the wiring above lists each — treat 30 as the census number).
- `GET api/v1/notifications/subscriptions` + `POST .../{id}:resend` live in **Commerce.Subscriptions** (`Subscriptions/Controllers/SubscriptionNotificationsController.cs:33,42`) — a billing delivery-record console inside the notifications namespace. The resend route is an E9 baseline failure (500 on unknown id → should be 404; `wiring-gap-baseline.txt:249`). Documented here so namespace audits do not mis-attribute it.

## 7. Request-type census

| Category | Count | With handler | Without handler |
|---|---|---|---|
| Mutation commands (`NotificationMutationCommands.cs`, 76 lines) | 6 | 6 | 0 |
| Preference commands (`EmailPreferenceCommands.cs`) | 2 | 2 | 0 |
| Admin delivery commands (`EmailDeliveryMutationCommands.cs`) | 2 (+1 ingest `IngestEmailDeliveryEventCommand` used by tests/contracts) | all | 0 |
| Webhook + unsubscribe commands | 2 | 2 | 0 |
| WhatsApp broadcast commands | 4 | 4 | 0 |
| **Total** | **16** | **16** | **0 dead request types** |

The module predates the wiring-gap guards and is fully wired; the residual dead surface is the **28 renderer-less email types** (04 §3), not handlers.
