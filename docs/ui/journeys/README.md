# UI journeys — the five critical user click-paths

The API surface is documented exhaustively (see
[api-contract-matrix.md](../architecture/api-contract-matrix.md) and the module
wiring matrices); these pages document the other half: **what the user clicks,
which component handles it, which API call it makes, and every state the screen
can be in**. Audited against `develop` (`630eb8c6d7`).

| # | Journey | File |
| --- | --- | --- |
| 1 | Sign-up, email verification, first sign-in (Turnstile #1082, backup codes #1195) | [01-sign-up-email-verification-first-sign-in.md](./01-sign-up-email-verification-first-sign-in.md) |
| 2 | MFA enrollment and recovery (TOTP/SMS settings, sign-in-bound enrollment #1231, email-MFA #1168, password re-verification #1164) | [02-mfa-enrollment-and-recovery.md](./02-mfa-enrollment-and-recovery.md) |
| 3 | Checkout — browse, cart, economy/Stripe payment, course checkout, promo/subscription reality (#1290) | [03-checkout-subscription-promo-payment.md](./03-checkout-subscription-promo-payment.md) |
| 4 | Account and settings — preferences, sessions list and revoke, sign-out (#1196, #1287) | [04-account-settings-sessions-sign-out.md](./04-account-settings-sessions-sign-out.md) |
| 5 | Creator and mentorship onboarding (#1159) | [05-creator-mentorship-onboarding.md](./05-creator-mentorship-onboarding.md) |

## Conventions used by these pages

- **References** are `file:line`, relative to the repo root (`apps/web/...`,
  `packages/...`), verified at the audit commit.
- **API calls** name the generated client method where one is used
  (e.g. `postAuthMfaTotpSetup`) or flag the raw-fetch transport (the matrix's
  F3/F4 classes) where the UI hand-fetches the URL.
- **Error states** follow the repo's two patterns: inline `FieldError` /
  `MarketplaceActionNotice` alerts for action results, and the
  **destructive-alert fallback** convention for failed *loads* — established by
  the #1217 fix (branch `fix/client-broken-calls`, pending merge at audit time):
  a failed load renders `Alert variant="destructive"` in place of the component,
  never a silently empty screen. Pages that predate it are flagged in their
  Gaps section.
- **Gaps** are split into *Known* (matrix finding, open issue, pending PR) and
  *NEW* (first observed while writing these pages).
- **Diagrams** are Mermaid `flowchart TD`; user actions/screens are rectangles,
  API calls are double-square nodes, decisions are diamonds. Labels avoid
  parentheses and colons for renderer compatibility.

## Cross-journey gap digest

The NEW gaps worth triaging first, collected from all five pages:

1. Email verification is unreachable by navigation and unenforced at sign-in
   (Journey 1).
2. Email-MFA (#1168) and MFA account-recovery have API surface but no UI
   (Journey 2).
3. No buyer-facing promo entry, no subscription purchase path, no
   PastDue/dunning surface anywhere (Journey 3).
4. `safeLoad` degrades security-page loads to silent empties; session-action
   statuses collapse into one generic toast (Journey 4).
5. The api-contract-matrix predates the mentorship module — 12+ raw-fetch call
   sites are missing from its F3 inventory (Journey 5).
