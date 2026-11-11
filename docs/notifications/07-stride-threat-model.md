# Notifications — STRIDE threat model

> Module `GameGuild.Notifications` · develop `ce31d5850c`. Family numbering matches 01/08.

## Family 1 — In-app notification access (user surface)

| Threat | Story | Mitigations | Verdict |
|---|---|---|---|
| **S**poofing | read another user's rows | SubjectId from ActorContext; per-row `RecipientId != actor → Forbid()` on id-addressed mutations | pass |
| **T**ampering | mark someone else's row read | same ownership checks (`NotificationsController.cs:121-135, 172-190, 199-219`) | pass |
| **R**epudiation | "I never saw it" | `ReadAt` timestamp only; no read-receipt event | acceptable (product choice) |
| **I**nformation disclosure | pull other tenants' rows | queries user-scoped; **no tenant predicate though** — ownership is per-user, rows of users from other tenants unreachable via user id | pass |
| **D**oS | unbounded pagination | `take` unbounded (default 20, no cap) — mild | weak |
| **E**levation | reach admin console via user routes | separate controllers + `Policies.Admin` | pass |

## Family 2 — Preference management (the delivery-control plane)

| Threat | Story | Mitigations | Verdict |
|---|---|---|---|
| **S**poofing | flip another user's preferences | every preference route is actor-scoped (`GetRequiredUserId`, `Controller:38-44`) | pass |
| **T**ampering — **notification hijack** | attacker who can call the API as the user disables security mail (EmailEnabled=false) | preferences are authenticated writes; **transactional types bypass preferences** at decision step 1 (`NotificationPreferenceService.cs:177-180`) — password resets still deliver | pass by design |
| **R**epudiation | "I never turned email off" | preference rows have UpdatedAt only — no per-change audit event beyond the generic operation event `notifications.update-notification-preferences` | weak |
| **I**nformation disclosure | enumerate someone's mute list | self-scoped reads only | pass |
| **D**oS | none meaningful | — | n/a |
| **E**levation | none | — | n/a |

## Family 3 — Email dispatch pipeline (injection / integrity)

| Threat | Story | Mitigations | Verdict |
|---|---|---|---|
| **S**poofing — **notification injection** | a producer-side attacker injects content that renders as a platform email (phishing relay) | Title/Message originate from producer modules; renderers build `EmailMessage` from `Notification.Metadata` (protected for identity credentials, `NotificationMetadataProtector`); **no HTML sanitization story documented at the EmailMessage boundary** — trust rests on producers | weak-by-contract — the registry pattern centralizes the trust boundary but nothing enforces producer hygiene (N-inj; 09 §3) |
| **T**ampering | modify a Pending row mid-flight | claim-then-send with persisted `Sending` + 10m reclaim (`EmailDispatcherService.cs:23-24, 129-134`); Version token on digest claim | pass |
| **R**epudiation | "we never sent it" | `SentAt`, `ProviderMessageId`, `EmailDeliveryEvent` timeline join | pass (best in repo) |
| **I**nformation disclosure | recipient addresses in logs | `LogRedaction.MaskEmail` at every address log site (`:169, 212`); webhook payload privacy-stripped (`ReceiveEmailEventsCommand.cs:262-279`); metadata protected at rest + legacy backfill (`:216-244`) | pass |
| **D**oS | flood the queue via a chatty producer | batch 50 per sweep, no per-producer quota — a runaway worker can starve delivery (FIFO by CreatedAt) | weak |
| **E**levation | renderer registration poisoning | renderers are compile-time DI registrations; a producer module registering a duplicate type crashes at boot (`ToDictionary`) — fail-fast | pass |

## Family 4 — Public endpoints (unsubscribe + SNS webhook) — the subscription-bomb surface

| Threat | Story | Mitigations | Verdict |
|---|---|---|---|
| **S**poofing — **forged SNS messages** | POST fake bounces to suppress victims / poison timelines | `SnsMessageVerifier` (signature, signing-cert host pinned, topic allow-list); failures are generic 400/401/403 with no oracle (`ReceiveEmailEventsCommand.cs:65-79, 148-159`); SSRF guard on SubscribeURL host pinning (`:81-86`) | pass |
| **T**ampering — **subscription bomb via unsubscribe** | replay/forge tokens to mute a victim's channel | DataProtection-signed payload (tamper → single generic Invalid); **no expiry + no replay bound** (N6) — a legitimately-issued link is a permanent mute lever if leaked | partial (N6) |
| **R**epudiation | "I never unsubscribed" | generic operation event `notifications.unsubscribe` without the token; log line with UserId/scope | weak |
| **I**nformation disclosure | token oracle / user enumeration | indistinguishable failure shapes; no existence signal (`UnsubscribeNotificationCommand.cs:124-127`) | pass |
| **D**oS — **subscription bomb / webhook flood** | hammer the anonymous webhook or unsubscribe to drain worker/DB | no rate limiting on either endpoint; webhook does signature verify (cheap) + one idempotent transaction; unsubscribe is one indexed preference write | weak — rely on edge WAF today |
| **E**levation | webhook turns into suppression-admin | suppressions only *created* by verified events; **release** requires `Policies.Admin` (`EmailDeliveryAdminController.cs:18, 86-97`) | pass |

## Family 5 — Deliverability admin (suppression/dead-letter console)

| Threat | Story | Mitigations | Verdict |
|---|---|---|---|
| **S**/|E** | non-admin requeues/releases | `Policies.Admin` (`Authz:/Policies.cs:111`) | pass |
| **T**ampering | malicious requeue loop to re-mail a suppressed victim | requeue is bounded to DeadLettered rows; re-sends re-hit the pre-send suppression check (`EmailDispatcherService.cs:157-172`) — a released address is a deliberate admin act with audit counters (`RequeueCount`) | pass |
| **R**epudiation | admin actions traceable | `RequeueCount`, `ReleasedAt`, operation events | pass |
| **I**nformation disclosure | raw SNS payloads in the feed | DTO preview capped 500 chars; parsed fields only (`EmailDeliveryAdminController.cs:177-221`) | pass |

## Family 6 — WhatsApp pipeline (the paid externally-metered channel)

| Threat | Story | Mitigations | Verdict |
|---|---|---|---|
| **S**poofing | send to numbers the user never gave | phone from protected metadata or profile resolver only (`WhatsAppDispatcherService.cs:96-105`) | pass |
| **T**ampering | free-form spam through template gate | allowlist check before template send (`:109-116`; `WhatsAppSettings.cs:60`); audience opt-in filter + 500-recipient cap (05 §6) | pass |
| **R**epudiation | campaign blast proof | per-recipient rows + provider ids (`WhatsAppBroadcastRecipient`) | pass |
| **I**nformation disclosure | phone numbers in logs/events | operation events explicitly exclude audience numbers (`WhatsAppUseCaseEventContracts.cs:7`) | pass |
| **D**oS / cost bomb | runaway campaign | cap 500 default; scheduler single-pass; fail-closed provider | pass with cap |
| **E**levation | broadcast authoring by non-admin | `Policies.Admin` on the controller | pass |

## Aggregate verdict

The module is the most defensively engineered in the repo (fail-closed channels, signature-verified webhook, idempotent ingest, redaction discipline). Residual risk concentrates in: the **anonymous public endpoints' missing rate limiting** (family 4 DoS), the **no-expiry unsubscribe token** (family 4 T), the **producer-trust email-injection contract** (family 3 S), and the **N1 legacy gate** that can delay transactional mail (operational integrity).
