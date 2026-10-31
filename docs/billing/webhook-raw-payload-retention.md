# Billing Webhook Raw-Payload Retention (Opt-In)

> Issue #408 — "Capture raw payload and all headers". This feature is **off by
> default**; production behavior is unchanged until an operator explicitly enables it.

## Why this exists

The durable webhook inbox (`billing_webhook_events`) stores a **minimized** record of
every accepted delivery: the privacy-reduced routing payload plus a headers stub with a
payload hash, not the raw HTTP bytes. That is the right default — provider payloads can
carry customer PII, and signature headers are secrets.

Debugging occasionally needs more: an unhandled event type whose routing projection lost
the field that mattered, a provider dispute about what was actually delivered, or a
forensic request to prove what headers arrived. For those cases the platform can retain
the **raw delivery** — the exact body plus the full header set — in a redacted,
time-boxed, opt-in form.

## Configuration

Section `Billing:Webhook:Retention` (binds to `WebhookRetentionPolicy`):

| Key             | Default | Meaning                                                                              |
| --------------- | ------- | ------------------------------------------------------------------------------------ |
| `Enabled`       | `false` | Master switch. When off, no raw byte is captured or stored anywhere.                 |
| `RetentionDays` | `30`    | TTL before the cleanup worker purges the row. Bounded to **1–365** (validated).      |
| `RedactPii`     | `true`  | Masks email addresses in the retained payload. Secrets are hashed **unconditionally**. |

```json
{
  "Billing": {
    "Webhook": {
      "Retention": {
        "Enabled": true,
        "RetentionDays": 30,
        "RedactPii": true
      }
    }
  }
}
```

## How it works

1. **Capture (opt-in gate at the edge).** Each provider callback action in
   `BillingWebhooksController` snapshots the raw body and the full header set into the
   scoped `IWebhookDeliveryContext` — a strict no-op when the policy is disabled.
   Braintree reads its urlencoded body via request buffering before form parsing so the
   true bytes are captured. Captures are bounded (256 KB payload, 100 headers, 8 KB per
   header value) so a hostile delivery cannot balloon memory.
2. **Verify first, then store.** The redacted raw delivery is persisted by
   `BillingWebhookRepository` only when the accompanying inbox row carries
   `IsSignatureVerified = true`. Providers that verify before persisting (Stripe,
   Braintree, Apple, Google) retain at row creation; PayPal — which persists its inbox
   row before calling the verification API — retains on the first update after
   `MarkSignatureVerified()`. A delivery that never passes signature verification is
   **never** retained raw.
3. **Redaction pass (`WebhookRawDeliveryRedactor`).** One-way, applied at persistence
   time:
   - Secret-bearing header values (`Authorization`, `Stripe-Signature`,
     `PayPal-Transmission-Sig`, `Apple-Pay-Signature`, cookies, API keys, plus any value
     carrying a `Bearer`/`Basic` credential or a provider secret key like `sk_live_…` /
     `whsec_…`) become `sha256:<hex>`. They stay **verifiable-by-hash**: recompute
     SHA-256 over a candidate raw value and compare (constant-time).
   - Email addresses become deterministic `email:<short-hash>` tokens when `RedactPii`
     is on — the address is destroyed but repeated occurrences stay correlatable.
   - Everything else (user agents, request ids, timestamps, JSON structure) is kept
     verbatim — that is its forensic value.
   - Non-JSON bodies (Braintree forms, IPN query strings) get the same treatment via a
     plain-text fallback.
4. **Storage.** One `billing_webhook_raw_deliveries` row per inbox row (unique FK,
   cascade delete), holding the redacted payload, redacted headers JSON, the SHA-256 of
   the *original* payload bytes (intake evidence computed before redaction), the
   redaction flags, and `ReceivedAt` / `ExpiresAt`.
5. **TTL purge.** The hosted webhook worker purges rows with `ExpiresAt <= now` on every
   cycle (30 s default), independently of the retry loop. A purge failure is logged and
   retried on the next cycle; it never aborts retry processing.

## Privacy rationale

- **Off by default.** Raw billing payloads are personal data; nothing is retained unless
  an operator makes an explicit, configured choice. Enabling retention is a documented
  data-processing decision that belongs to the deployment, not the codebase.
- **Verify first, then store.** Signature material is needed for verification, so
  verification always completes before the (redacted) form is written. Unverified —
  potentially hostile — deliveries are never retained raw.
- **Never store secrets literally.** Tokens, authorization headers and signature headers
  are one-way hashed even when PII redaction is turned off; `RedactPii = false` only
  keeps non-secret identity fields readable for debugging.
- **Bounded lifetime.** `RetentionDays` is hard-bounded to 1–365 and enforced by the
  cleanup worker, so an accidental configuration cannot retain raw payloads forever.

## Operations

- Check retained rows: `SELECT * FROM billing_webhook_raw_deliveries WHERE ...` (join
  `billing_webhook_events` on `BillingWebhookEventId` for processing status).
- Re-verify a stored signature hash against a candidate raw value using
  `WebhookRawDeliveryRedactor.VerifyHash(candidate, stored)`.
- Turning the policy off stops new retention immediately; previously retained rows age
  out on their `ExpiresAt` (purge only runs while the policy is enabled — run a one-off
  `DELETE FROM billing_webhook_raw_deliveries` if immediate erasure is required).
