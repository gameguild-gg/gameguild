# Analytics Event Contract

Versioned measurement contract for the web client's analytics events (issues
#1210, #1221). The contract is implemented in
`apps/web/src/lib/analytics/events.ts` — event name constants, payload types,
and the `ANALYTICS_EVENT_CONTRACT_VERSION` stamped on every dispatched payload
as `event_version`.

## Contract versioning policy

Current contract version: **1** (`ANALYTICS_EVENT_CONTRACT_VERSION`).

- Bump the version whenever a param is added, renamed, or removed on any
  event, or a new event is added with a different shape than documented here.
- Changes are **additive-only within a version**: once a version ships, its
  param set is frozen; new params require a version bump.
- `event_version` lets downstream consumers (BigQuery export, dashboards)
  distinguish schema generations of the same event name.

## Events

| Event | Params | Type | Producer | Trigger | Purpose | Destinations |
|---|---|---|---|---|---|---|
| `web_vitals` | `metric_name`, `metric_value`, `metric_rating`, `metric_id`, `page_path`, `event_version` | string / int / string / string / string / int | `web-vitals-reporter.tsx` | web-vitals library callback, once per metric per navigation | analytics | GA4, Firebase |
| `page_load_timing` | `metric_name`, `response_start_ms`, `dom_content_loaded_ms`, `load_event_ms`, `transfer_size`, `page_path`, `event_version` | string / int ×4 / string / int | `web-vitals-reporter.tsx` | Navigation Timing 2 entry, once per document load | analytics | GA4, Firebase |
| `page_view` | `page_path`, `event_version` | string / int | `web-vitals-reporter.tsx` | initial mount + every SPA pathname change (once per path change) | analytics | GA4, Firebase |

All events are consent-gated on the analytics purpose (issue #1206): nothing
is dispatched while consent is denied or absent, and consent is re-checked
after async loader resolution (fail closed).

## Delivery ownership

- **GA4 gtag.js owns the `page_view` transport** (issue #1221 decision). The
  `config` call passes `send_page_view: false`; the explicit `page_view`
  event from `web-vitals-reporter.tsx` is the single source of page views.
- **GTM is not configured in production** (`NEXT_PUBLIC_GTM_CONTAINER_ID`
  unset). **Runbook:** when GTM is later enabled, the gtag.js built-in
  page_view must stay disabled (it already is) **and** exactly one of
  (explicit `page_view` event, GTM history-triggered page_view) must own
  transport — otherwise page views double-fire. Prefer keeping the explicit
  event and NOT configuring a GTM page_view trigger.

## Tag / SDK inventory

| Tag / SDK | Config | Loader |
|---|---|---|
| gtag.js — GA4 `G-DCKJKN8PPD` | `NEXT_PUBLIC_GA_MEASUREMENT_ID` | `google-analytics.tsx` (consent-gated) |
| Firebase Analytics (project `gamedev-guild`) | `NEXT_PUBLIC_FIREBASE_*` | `web-vitals-reporter.tsx` (env-gated; all unset = no-op) |
| Google Tag Manager | `NEXT_PUBLIC_GTM_CONTAINER_ID` | `google-tag-manager.tsx` (consent-gated; **unset in production**) |

## GA4 admin setup

Register these once per property (Admin → Custom definitions):

- **Event-scoped custom dimensions:** `metric_name`, `metric_rating`,
  `metric_id`, `page_path`
- **Custom metrics:** `metric_value`, `response_start_ms`,
  `dom_content_loaded_ms`, `load_event_ms`, `transfer_size`

Notes: custom definitions take 24–48h to appear in reports and are **not
retroactive** — events collected before registration keep raw params but they
won't show in standard reports. The BigQuery export carries the raw params
regardless of custom-definition registration.

## Firebase limits

- Event name ≤ 40 chars; all names above comply (`page_load_timing` = 16).
- ≤ 25 params per event (all events above ≤ 7).
- Param values ≤ 100 chars (`page_path`, `metric_id` are the realistic ones;
  paths must stay under the limit — extremely long query-less paths are
  acceptable, but avoid putting full URLs in `page_path`).
- SDK auto-collection is gated via `setAnalyticsCollectionEnabled`, re-applied
  on every load and on every consent change.

## Example dataLayer fixtures

Sanitized examples of what reaches `window.dataLayer` via the gtag shim
(`['event', name, payload]` entries):

```json
// web_vitals
["event", "web_vitals", {
  "metric_name": "LCP",
  "metric_value": 2451,
  "metric_rating": "good",
  "metric_id": "v3-abc123",
  "page_path": "/courses",
  "event_version": 1
}]

// page_load_timing
["event", "page_load_timing", {
  "metric_name": "page_load_timing",
  "response_start_ms": 120,
  "dom_content_loaded_ms": 901,
  "load_event_ms": 1500,
  "transfer_size": 4096,
  "page_path": "/play",
  "event_version": 1
}]

// page_view
["event", "page_view", {
  "page_path": "/courses",
  "event_version": 1
}]
```

## Environment destination matrix

| Environment | GA4 | Firebase | GTM | Notes |
|---|---|---|---|---|
| local / dev | unset | unset | unset | All `NEXT_PUBLIC_*` analytics env vars unset — zero tracking footprint, all loaders no-op |
| staging / test | distinct test GA4 property | test Firebase project (via env) | unset | Set through env only; **never** production property ids (`G-DCKJKN8PPD`) or the production Firebase project |
| production | GA4 `G-DCKJKN8PPD` | Firebase project `gamedev-guild` | unset | GTM intentionally unset in every environment today (see runbook below) |

## GTM operational runbook

Google Tag Manager is intentionally disabled until ownership for #1221
activates (`NEXT_PUBLIC_GTM_CONTAINER_ID` is unset in every environment).

- **Preview / debug:** open GTM → Preview, enter the site URL, connect via
  Google Tag Assistant. Verify the consent-mode signals gate every tag
  (nothing fires while analytics consent is denied) and that the explicit
  `page_view` event is the single page-view source (no GTM history-triggered
  page_view double-firing — see Delivery ownership above).
- **Publish:** Workspace → Submit → provide a version **name and description**
  summarizing the change. Never publish without a description; versions are
  the audit trail.
- **Rollback:** GTM → Workspace/Versions → select the previous version →
  restore/publish it. Confirm in Tag Assistant that the rollback removed the
  offending tags/triggers.
- **Ownership:** container changes require approval from the infra lead. GTM
  stays disabled until the #1221 ownership handoff activates it.

