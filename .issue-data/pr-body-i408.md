## Summary

Delivers the final acceptance box of #408 — **"Capture raw payload and all headers"** — as owner-approved OPTION A: opt-in raw-payload retention with redaction and TTL. References #408.

The durable webhook inbox keeps a minimized record today (routing payload + headers stub). This PR adds an **opt-in** raw-delivery retention: when enabled, the exact raw body and full header set of each accepted provider webhook are persisted alongside the minimized record — through a one-way redaction pass, only after signature verification, purged on a TTL.

### Configuration (`Billing:Webhook:Retention`)

| Key | Default | Notes |
| --- | --- | --- |
| `Enabled` | `false` | Strict no-op in production until explicitly enabled. |
| `RetentionDays` | `30` | Bounded 1–365, validated in `BillingConfiguration.Validate`. |
| `RedactPii` | `true` | Masks emails; secrets are hashed **unconditionally**. |

### How it works

- **Capture**: each provider callback snapshots the raw body + full headers into the scoped `WebhookDeliveryContext` (bounded: 256 KB payload / 100 headers / 8 KB per value). Braintree reads via request buffering before form parsing. No-op when disabled.
- **Verify FIRST, then store**: `BillingWebhookRepository` persists the redacted raw delivery only when the inbox row carries `IsSignatureVerified = true` — at creation for verify-first providers (Stripe, Braintree, Apple, Google), and on the first update after `MarkSignatureVerified()` for PayPal (whose inbox row precedes its verification call). Unverified deliveries are never retained raw.
- **Redaction** (`WebhookRawDeliveryRedactor`): signature/authorization/token header values and credential-shaped payload strings become `sha256:<hex>` (re-verifiable via `VerifyHash`, constant-time compare); emails become deterministic `email:<hash>` tokens when `RedactPii` is on; everything else stays verbatim. Non-JSON bodies get the same treatment via a plain-text fallback.
- **Storage**: one `billing_webhook_raw_deliveries` row per inbox row (unique FK, cascade), plus the SHA-256 of the original payload bytes as intake evidence. Migration `20261031203907_AddBillingWebhookRawDeliveryRetention`, snapshot hand-checked, `docs/schema.dbml` regenerated.
- **TTL purge**: the hosted webhook worker purges expired rows every cycle, independent of the retry loop; failures are logged and retried, never aborting retries.

### Privacy rationale

Off by default (raw billing payloads are personal data — retention is a deployment decision); verification precedes storage; secrets are never stored literally even with PII redaction off; the retention window is hard-bounded and enforced by the cleanup worker. Documented in `docs/billing/webhook-raw-payload-retention.md`.

## Test plan

- [x] Redaction pass unit tests: header hashing + verbatim retention, verifiable-by-hash, deterministic email tokens, `RedactPii=false` keeps email but still hashes secrets, JSON structure preservation, plain-text fallback, oversized-value hashing.
- [x] Repository retention tests: disabled-by-default no-op, verify-first gating (unverified row stores nothing), redacted-at-rest assertions, post-verification single-write (PayPal path), no-capture no-op, TTL purge (expired removed / live kept / disabled no-op).
- [x] Worker purge tests: purge runs when enabled, never touched when disabled, purge failure does not abort the cycle.
- [x] Delivery-context raw capture bounds + first-capture-wins.
- [x] Full `GameGuild.Commerce.Billing.UnitTests` suite: 834/834 passing locally; full API solution compiles clean (0 warnings).
