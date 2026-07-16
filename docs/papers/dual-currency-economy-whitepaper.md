# GameGuild Dual-Currency Economy: Ad-Rewarded Soft Coins & Marketplace Hard Coins

**Internal white paper — Engineering & Product**
**Status:** Draft for review
**Author:** [Alexandre Tolstenko Nogueira]
**Date:** July 2026

---

## 1. Purpose

GameGuild needs a token economy that lets students earn value by watching ad
interstitials, spend that value on low-stakes actions (AI grading, peer-review
fees, small purchases), and lets creators, mentors, and other sellers earn
and cash out real money from marketplace activity — courses, downloadable
software, certificate/exam tests, mentorship sessions, game-launch campaign
support packages, and other digital goods and services. This paper defines
the two currencies, their flows, the ad-reward mechanism, and the compliance
posture required before real money moves through the system.

This is a concept and economics document, not an implementation spec. Schema,
API design, and detailed fraud tooling are follow-on work once the model here
is agreed.

---

## 2. The two currencies

| | **Soft coins** | **Hard coins** |
|---|---|---|
| Backed by | Ad revenue, or platform-subsidized conversion | Real money (Stripe) or resale of hard-coin-priced goods |
| Cash-out to real money | **Never** | Yes, under conditions (Section 12) |
| Convertible to the other | Hard → Soft only, one direction | N/A |
| Primary holder | Students | Creators, mentors, and other sellers of digital goods/services |
| Primary use | Low-stakes spend: AI grading, bounty fees, some digital goods | Marketplace purchases, bounty rewards, payout |

The one-way conversion (hard → soft, never back) is the load-bearing rule of
the whole system. It's what keeps soft coins from becoming a shadow currency
that lets someone launder ad-reward value into real cash. Every other design
decision downstream exists to protect this rule.

---

## 3. Why hard coins need internal source-tagging

Not all hard coins are the same, even though the user sees one balance. Two
origins exist, and they must be tracked separately:

- **Purchased hard coins** — bought directly with a card via Stripe.
- **Earned hard coins** — received from selling a digital good or service
  (course, downloadable software, certificate/exam test, mentorship
  session, campaign support package, etc.), fulfilling a bounty, or
  otherwise earning income on the platform.

**Why this matters:** if a user could buy hard coins with a credit card and
immediately cash them out, that's a direct chargeback-fraud path — buy $100 in
coins, cash out, then dispute the original charge with their bank. The
platform eats the loss twice: once paying out, once absorbing the chargeback.

This is not a hypothetical risk unique to GameGuild — every marketplace with a
payout side (Upwork, Gumroad, Patreon, Steam) enforces the same separation:
money coming *in* from a buyer and money going *out* to a payee are never the
same bucket, even when the currency is nominally identical. The rule:

> **Only `earned` hard coins are cash-out eligible. `Purchased` hard coins can
> be spent freely inside the platform but never withdrawn.**

This single distinction is what makes cash-out possible without inheriting
unbounded chargeback exposure.

---

## 4. Currency flows

### 4.1 Soft coin inflows
- **Ad rewards** — verified ad view, converted from estimated ad revenue (Section 10).
- **Bounty transfers** — one user pays another in soft coins for completed work.
- **System-issued soft coins (rare)** — if the platform ever grants soft coins
  directly (promotions, compensation), it must debit its *own* hard-coin
  account for the equivalent conversion cost. Soft coins are never created
  from nothing — every soft coin in existence is backed by either ad revenue
  or a real hard-coin debit somewhere.

### 4.2 Soft coin outflows
- **Fees** — burned. Removed from circulation, no counterparty credited.
- **Bounty transfers** — debited from poster, credited to grader.
- **AI inference cost** — burned against internal token cost accounting.
- **Digital goods** (where the seller accepts soft coins) — credited to the
  seller. The seller accepts this knowingly; soft coins received this way are
  still non-convertible to hard coins or cash. That's the seller's decision
  to price in soft coins, and the platform does not shield them from that
  choice after the fact.

### 4.3 Hard coin inflows
- **Stripe purchase** → tagged `purchased`.
- **Sale of digital goods and services** (courses, downloadable software,
  certificate/exam tests, mentorship sessions, game-launch campaign support
  packages, etc.) → tagged `earned`.
- **Bounty fulfillment** (posted in hard coins) → tagged `earned`.

### 4.4 Hard coin outflows
- **Cash out** — only from `earned` balance, via Stripe Connect.
- **Spend on digital goods** — either bucket, spend doesn't care about tag.
- **Bounty posting** — escrowed, tag preserved through escrow lifecycle.
- **Conversion to soft coins** — either bucket, one-way.

---

## 5. Escrow and bounties

Bounties (e.g., "grade my submission," "review my project") lock funds in
escrow until claimed, with two resolution paths:

- **Claimed** — grader/reviewer receives the coins as fresh `earned` income
  (soft or hard, matching how it was posted), regardless of the poster's
  original source tag.
- **Reclaimed** (expired, unclaimed) — refunded to the poster **minus a
  reclaim fee**, with the **original source tag restored**. If a `purchased`
  hard coin went into escrow, a `purchased` hard coin comes back out — not
  `earned`. Without this, escrow-and-reclaim becomes a laundering path that
  converts non-cashable purchased coins into cashable earned coins for free.

Grader eligibility (prerequisite completion, reputation threshold, instructor
verification) is checked at claim time, not at posting time, since
eligibility can change between when a bounty is posted and when it's claimed.

---

## 6. Ledger design: hash-chained, append-only

The ledger is modeled as an append-only chain, not a mutable balance table.
No entry is ever updated or deleted. Every entry is cryptographically linked
to the one before it, so any retroactive tampering — someone editing a past
entry directly in the database — breaks the hash chain and is detectable.

### 6.1 Entry structure

```
ledger_entries:
  id                  bigserial, monotonic, one global sequence
  prev_entry_hash     hash of the previous entry
  entry_hash          hash of (id, wallet_id, currency, source_tag, amount,
                       ref_type, ref_id, created_at, prev_entry_hash)
  wallet_id
  currency            hard | soft
  source_tag          purchased | earned | fee | reserve | n/a
  amount              signed
  ref_type            ad_reward | bounty | purchase | conversion | fee |
                       cashout | reclaim | refund | dispute_lost |
                       ai_grading | admin_adjustment
  ref_id
  matures_at          nullable — set on any credit that could still be
                       reverted by a refund, dispute, or reclaim: user
                       earned_hard credits and admin-wallet fee credits
                       alike (Section 7.2). The hold deadline (Section
                       12.1) after which the entry counts toward
                       withdrawable balance (Section 13) or, for the admin
                       wallet, toward the next monthly withdrawal run
                       (Section 7.2)
  counterparty_wallet_id   nullable — every transaction posts at least two
                            entries (double-entry), this links them
  created_at
  memo
```

Every real-world event (ad reward, purchase, bounty claim, cash-out) produces
**two or more linked entries** — a debit somewhere, a credit somewhere else
— never a single one-sided mutation. This is standard double-entry
accounting, just made immutable and chain-verified.

### 6.2 Balance as a derived value

A user's balance is never stored as a field that gets directly incremented
or decremented. It's the **sum of all entries** for that wallet/currency/tag.
Two layers:

1. **Materialized balance cache** — a `wallet_balances` table recomputed on
   every write (or near-real-time), used for fast reads (checkout, spend
   checks). This is what the app actually queries.
2. **Periodic full recompute** — a background job re-sums the entire ledger
   from entry #1 and compares against the cached balance. Any mismatch is a
   bug or tampering signal, and gets flagged for audit before it's trusted
   again. This is the safety net that makes the cache trustworthy.

### 6.3 Refunds and card-network disputes: holds, not entry edits

Refunds and disputes are the one place the ledger's append-only rule is
easiest to get wrong — the instinct is to "correct" a past entry, which
Section 6.1's invariant forbids. Both are handled the same way as any other
correction: a **hold** on the affected entry while the outcome is pending,
resolved by either releasing the hold (nothing changes) or posting a new
**offsetting entry** (value moves back) — never by editing or deleting the
original entry.

A hold is administrative state, not a value movement, so it lives in its own
table rather than as a zero-amount ledger entry:

```
holds:
  id
  wallet_id
  ledger_entry_id     which earned_hard (or other) entry this hold applies to
  amount               usually mirrors the entry; can be partial
  reason               refund_pending | dispute_open | risk_review
  status               active | released | consumed_by_reversal
  opened_at
  resolved_at
```

**Refund flow:** a refund request opens a hold (`refund_pending`) on the
seller's credited entry. If approved, a new ledger entry reverses the
original amount (Section 11.1) and the hold is marked `consumed_by_reversal`.
If denied, the hold is `released` and the balance is untouched.

**Card-network dispute flow:** GameGuild is notified via a Stripe webhook
when a cardholder disputes a charge. Two cases:

- **The disputed entry hasn't matured yet** (Section 13) — a `dispute_open`
  hold blocks it from ever becoming withdrawable while the dispute is
  active. If GameGuild/the connected account loses the dispute, a new
  `dispute_lost` ledger entry reverses the value — nothing was ever paid
  out, so this is a clean reversal, no different from a refund. If the
  dispute is won, the hold is released and the entry matures on its normal
  schedule.
- **The disputed entry already matured and was paid out before the dispute
  was raised.** This should be rare — dispute windows and the payout hold
  are meant to overlap — but isn't impossible for reason codes with longer
  filing windows. Here a `dispute_lost` reversal still posts, but there's no
  balance left to take it from: the wallet goes negative, tracked explicitly
  as a debt against that account's future earnings, and backstopped by
  GameGuild's own Stripe Connect reserve exactly as described in Section
  12.2 — GameGuild is responsible for a connected account's negative
  balance under the standard marketplace integration, and this is precisely
  the scenario that exposure is for.

**Safety principle for all of the above:** if a hold's status is ever
ambiguous — a webhook hasn't been confirmed, a reconciliation job disagrees
with the cached state — the amount is treated as **not withdrawable** until
the ambiguity resolves. When the periodic recompute (Section 6.2) and the
live cache disagree on whether a hold is active, the lower, more
conservative balance is what's shown and enforced, never the higher one.

### 6.4 Open item

Hash-chaining can run as **one global chain** (every entry across every
wallet, strict total ordering, simplest to reason about, but a single point
of write contention at scale) or **one chain per wallet** (parallelizable,
but cross-wallet ordering/audit requires stitching chains back together via
`ref_id` links). Given GameGuild's expected transaction volume, a global
chain is almost certainly fine to start — this only needs revisiting if
ledger writes ever become a throughput bottleneck.

---

## 7. Platform (admin) wallet

A single admin wallet holds all platform-side value, distinguished by
`source_tag`/`ref_type` on each entry rather than by separate wallet
accounts. This keeps the wallet model uniform (admin is "just another
wallet" the ledger already knows how to handle) while still giving clean
audit slices:

- **Platform fees** — every fee entry (Section 8) posts a credit to the
  admin wallet tagged `fee`.
- **Reserve** — hard coins held 1:1 against outstanding `earned` balances
  (Section 12.3) live in the admin wallet tagged `reserve`, distinct from
  fees actually earned as revenue.
- **Cash-out tracking** — when a user cashes out, the debit from their
  `earned_hard` balance is mirrored by an entry against the admin wallet
  tagged `cashout`, which is what gets reconciled against Stripe Connect's
  own transfer records. This is the audit trail that answers "how much have
  we paid out, to whom, and does it match what Stripe says left our
  account."

Because everything is tag-filtered from one wallet rather than split across
several, auditing "how much fee revenue did we earn this quarter" or "what's
our current reserve liability" is a `SUM() WHERE source_tag = X` query, not a
cross-wallet reconciliation exercise.

### 7.1 Custody vs. ownership: the withdrawal event

Every credit posted to the admin wallet — a fee, a reserve allocation — is
an internal ledger claim, not yet GameGuild's own money to spend. The actual
cash backing every wallet in the system, user or admin, sits externally: at
Stripe (card purchases, pending payouts under the 120-day hold, Section
12.1) or with ad networks (collected or still-owed ad revenue, Section
10.3). None of that external cash becomes GameGuild's own, freely usable
revenue by virtue of an internal ledger entry alone.

**GameGuild only owns money at the moment it withdraws that specific amount
from Stripe (or from collected ad-network revenue) into the company's own
operating bank account.** Until that withdrawal happens, the admin wallet's
accrued fee revenue — like every other balance in the system — is treated
as still belonging to the same custodial pool as client funds, not as
spendable company cash.

This gives one testable invariant across the whole system:

```
external custodial cash (Stripe balance + collected/receivable
  ad revenue)
    =
sum of all internal claims (every user wallet + admin wallet's
  accrued-but-unwithdrawn fees + the earned_hard reserve + the
  soft-coin reserve, Section 12.3)
```

The two sides must always match. A mismatch means a ledger bug or a
reconciliation gap, not a rounding error to shrug off — this is the
mechanism that keeps GameGuild's own finances and its users' funds strictly
1:1, rather than something enforced by convention alone.

A withdrawal transaction (admin wallet → company bank account) is the only
event that removes value from this equation entirely: the withdrawn amount
leaves both the external custodial balance and the internal ledger claim at
the same moment, and only then is it GameGuild's own to spend, distribute,
or otherwise use outside the platform. This applies uniformly regardless of
funding source — hard-coin fee revenue collected via Stripe and soft-coin-
related revenue collected via ad networks are both subject to the same
rule.

### 7.2 Fee-revenue maturity and monthly withdrawal cadence

**GameGuild withdraws its own accrued fee revenue from Stripe to the
company bank account on a monthly cadence.** A withdrawal is not simply "all
admin-wallet hard-coin revenue as of today" — it's gated by the same
maturity rule already applied to user payouts (Section 12.1), applied to
GameGuild's own revenue as well: **any fee entry generated by a transaction
that could still be refunded, disputed, or reclaimed is not eligible for
withdrawal until that transaction's own hold window has fully elapsed** —
even though it's GameGuild's own money that ultimately results.

Mechanically, this reuses the exact same fields already defined for user
balances, not a separate concept: every admin-wallet fee entry carries a
`matures_at` (Section 6.1) and is subject to the same `holds` table (Section
6.3) as any other entry. A platform fee taken on a marketplace purchase
matures on the same schedule as the purchase itself would need to clear
before its proceeds are safe to withdraw; a fee taken from a reclaimed
escrow or a hard-coin cash-out follows the maturity of whatever funded it
upstream.

**At launch, this uses one uniform window rather than a per-transaction-type
matrix:** every admin-wallet fee entry matures on the same 120-day schedule
already adopted for user payouts (Section 12.1), regardless of whether the
specific transaction it came from was purchased-hard-coin-funded (genuine
card-dispute exposure) or entirely earned-hard-coin-funded (arguably lower
risk, since no card transaction is directly in play). Distinguishing those
cases to shorten the hold for lower-risk fee revenue is a real future
optimization, but building an accurate per-transaction-type maturity matrix
before GameGuild has any operational data to calibrate it against would be
guessing at exactly the kind of thing this whole model is trying not to
guess at. One conservative window, uniformly applied, is the safer starting
position (Section 15).

The monthly withdrawal run, in short:

```
withdrawal_amount = SUM(admin wallet earned/fee entries
                        WHERE matures_at <= run_date
                        AND no active hold)
                     − amount already withdrawn in prior runs
```

Because the cadence is monthly rather than continuous, matured revenue
doesn't move to the company bank account the instant it matures — it sits
in the custodial pool (Section 7.1's invariant still holds) until the next
scheduled run sweeps it. A fee entry that matures on day 121 but whose
previous withdrawal run was on day 100 simply waits for the next monthly
run; this is expected, not a bug.

**Residual risk, matching the user-payout case (Section 6.3):** if a
dispute arrives after revenue has already been withdrawn to the company
bank account — rare, since the hold window is designed to cover the
standard dispute period, but possible for reason codes with extended
filing windows — this is ordinary merchant chargeback liability against
GameGuild's own Stripe account, not a connected-account issue, and is
outside the scope of the connected-account reserve tooling described in
Section 12.1.

---

## 8. Fees: platform, creator, and reclaim

### 8.1 Creator fee — layered on top, not carved out of, existing costs

For AI grading and peer-review transactions, the creator's fee is **added on
top of the AI/inference cost and the platform fee**, not taken as a cut of
them. This matters for solvency: if creator fees were carved out of the
platform's own margin, a popular offering with many creator-fee-taking
transactions could push the platform's take toward zero or negative once
inference costs are accounted for. Layering the creator fee on top means the
platform's margin is never eroded by a creator's pricing choice.

```
total charged to student = ai_inference_cost + platform_fee + creator_fee
```

- `ai_inference_cost` — covers the actual LLM API cost, credited to an
  internal cost-tracking tag (not creator or platform revenue — this is
  what GameGuild owes its own inference provider).
- `platform_fee` — GameGuild's take, credited to the admin wallet.
- `creator_fee` — the creator's or seller's take, credited to their wallet
  as `earned` (soft or hard, matching what the student paid with).

The same layering applies to peer-review bounty fees where a creator opts
to monetize peer-review activity around their content.

### 8.1.1 Creator currency preference — protecting cashable income

A structural tension: soft coins exist specifically so students can pay for
grading and peer review without spending real money — which means, by
design, a large share of grading transactions will likely be settled in
soft coins. If a creator's fee is simply paid "in whatever currency the
student used," a creator monetizing grading could end up with most of that
income as non-cashable `earned_soft`, undermining the premise that creators
earn *real* income from this system.

**Fix: extend the same seller-currency-preference pattern already used for
marketplace products (Section 11) to creator fees.** A creator sets an
accepted-currency preference for their creator fee — `hard_only`,
`soft_only`, or `either` — independent of what currency the student uses for
the rest of the transaction (inference cost and platform fee still follow
whatever the student paid with).

- **`either` (recommended default):** creator receives whatever the student
  paid with, no guarantee — the simplest option to build.
- **`hard_only`:** if a student pays in soft coins, the creator_fee leg for
  that specific transaction either (a) doesn't apply for that transaction —
  the creator earns nothing on soft-coin-paid instances of their own
  service, which is a real trade-off they're accepting in exchange for every
  fee they *do* collect being cashable, or (b) is fronted by the platform in
  hard coins, with the platform separately retaining the student's soft-coin
  payment as its own — effectively the platform choosing to subsidize the
  creator's guarantee of real income, at the platform's own cost and
  discretion, not as a general user-facing soft→hard conversion path. Option
  (b) is a deliberate platform business decision (it costs GameGuild money
  per transaction) and should be scoped and priced before being offered, not
  assumed as a default behavior.

This keeps the core invariant intact — soft coins still never convert to
hard coins through any *general* mechanism — while giving creators who want
guaranteed cashable income a way to opt into it, at a cost/trade-off they
choose knowingly, consistent with how sellers already handle soft-coin
product pricing in Section 11.

### 8.2 Fee configuration — tunable, versioned

All fee rates (platform fee, reclaim fee, conversion buffer) are configured
by GameGuild, not hardcoded, and versioned so historical transactions remain
auditable even after rates change:

```
fee_config:
  id
  fee_type        platform_fee | creator_fee_cap | reclaim_fee | refund_fee |
                   ad_yield_buffer | hard_to_soft_fee | cashout_fee |
                   payout_hold_days
  scope           global | product_id-specific override | network-specific
                   (ad_yield_buffer only)
  value           percentage, fixed amount, or day-count (payout_hold_days)
  effective_from
  effective_to
  updated_by
  updated_at
```

Two fee types are worth distinguishing precisely, since they're easy to
conflate: **`ad_yield_buffer`** protects the ad-reward conversion rate
against revenue volatility and payment lag (Section 10.3) — it applies at
the moment a soft coin is minted from an ad view. **`hard_to_soft_fee`**
applies when a user manually converts their own already-held hard coins into
soft coins — a different transaction with no ad-market risk, so it should
carry a different (likely smaller) rate.

Every ledger entry that involves a fee stores the **rate actually applied**
at that moment (not just a reference to the current config), so a rate
change next month doesn't retroactively alter how last month's transactions
are interpreted during an audit.

---

## 9. Ad-fraud: vectors and mitigations

| Fraud vector | Mitigation | Residual risk |
|---|---|---|
| Bot/headless browser fakes playback events without real rendering | Require real `<video>` element decode events (not just JS-fired fakes); pair with invisible bot-detection (Cloudflare Turnstile / reCAPTCHA v3) before ad starts | Sophisticated headless-Chrome-with-real-rendering setups are still possible; this raises cost, doesn't eliminate it |
| Multiple accounts farming the same device/IP | Device fingerprinting + IP-based rate limits on reward claims; email/phone verification gating reward eligibility after N claims | Residential proxy farms can still spread accounts across IPs |
| Replay attack — resending a valid signed ad-session token | Single-use, short-expiry (minutes) tokens; server invalidates token after first `complete` event | None significant if expiry/single-use is enforced correctly |
| Playback-rate manipulation (dev tools speed up "watched" video) | Server checks elapsed wall-clock time against reported video duration; reject if quartile timestamps arrive faster than physically possible | Can't detect a *slowed-down* replay used to pre-stage fake quartile events; combine with token expiry |
| Tab backgrounded/hidden during "playback" | Page Visibility API — pause reward tracking when hidden, fail verification if hidden-time ratio exceeds a threshold | Some legitimate users alt-tab briefly; needs a tolerance window, not zero-tolerance |
| Reward velocity abuse (one user/device farming rapid rewards) | Daily/weekly reward caps per user, per device, per IP; anomaly detection on claim velocity | Determined abuse can stay just under caps; caps need periodic tuning |
| Ad network itself over-reports fill/inflates eCPM | Async reconciliation (Section 10.4) compares actual vs. estimated revenue per network; networks with persistent negative variance get deprioritized in ranking | Reconciliation lag (often 24–72h) means some fraud loss is absorbed before it's caught |
| Account collusion (real humans splitting reward-farming across many real accounts) | Same device-fingerprint + IP heuristics as above, plus flagging clusters of accounts with correlated claim timing | Hardest vector to fully solve — real humans behind real devices behave like real users |

Because soft coins carry real backing cost (ad revenue or a platform hard-coin
debit), ad-reward fraud here is a direct financial loss to GameGuild, not
just an engagement-metric abuse case. That raises the bar for how much
mitigation effort is justified compared to, say, fraud on a vanity metric.

---

## 10. Ad-reward mechanism

### 10.1 Selecting the ad to show

True per-impression real-time pricing isn't available from most VAST ad
networks — even legacy mobile mediation platforms (MoPub, AppLovin MAX,
ironSource) worked primarily on **trailing average eCPM per network**, not
live price discovery, except where genuine real-time bidding (RTB) was
integrated. Given GameGuild's traffic scale, a full Prebid.js/RTB stack isn't
justified — that infrastructure needs volume in the millions of impressions
to pay for its own integration cost.

**Recommended approach:** rank a small set of direct VAST-integrated networks
(2–4) by rolling 7/30-day average eCPM, serve the highest-ranked network,
update rankings periodically. This mirrors the outcome of a MoPub-style
waterfall honestly, without pretending to have real-time price data the
platform doesn't actually have.

**Candidate networks** (an illustrative shortlist to start commercial
due diligence from, Section 15 — not a commitment):

- **NitroPay** — built specifically for gaming and developer-community
  publishers, which lines up with GameGuild's own user base.
- **Playwire** — strong in high-impact video ad monetization for gaming and
  entertainment publishers, relevant given the ad format here is video
  interstitials specifically.
- **Snigel** — a full-service header-bidding/yield-management platform
  geared toward larger, technical/reference-heavy sites, closer in kind to
  an LMS/documentation-style traffic profile than to gaming alone.
- **MonetizeMore** and **Setupad** — broad-market header-bidding platforms,
  useful as a general-market baseline against the more niche options above.

The deliberate spread — gaming-audience specialists, a technical-content
specialist, and general-market platforms — reflects that GameGuild's
traffic sits at the intersection of gaming and technical/educational
content; no single category above is obviously the best fit without live
eCPM data across all of them to compare.

**Cold start (new network, or no trailing history yet):** use the ad
network's own self-reported average eCPM or rate-card estimate as the
initial ranking input and reward basis, clearly flagged internally as
`unverified — provider-supplied` until GameGuild's own reconciliation data
(Section 10.4) accumulates enough history (a set number of impressions or
days) to replace it with an internally-measured trailing average. Because a
provider's own estimate is self-reported and typically optimistic, a network
in this unverified state should carry a wider ad-yield buffer (Section 10.3)
than a reconciled network — the extra margin is what protects against the
estimate being inflated before GameGuild has real data to check it against.

### 10.2 Unskippable, verified playback

- Custom playback UI — no skip button, no seek bar, controls disabled for the
  ad's duration.
- **Verification is the structurally weak point.** Web has no equivalent to
  mobile's server-side-verified rewarded ad callbacks. Mitigation, not
  elimination:
  - Backend issues a signed, time-boxed session token when the ad starts.
  - Client reports quartile events (start/25/50/75/complete) against that token.
  - Backend rejects impossible timing (quartiles arriving faster than the
    video's actual duration allows) or tab-backgrounding during playback.
  - Per-user/day reward rate limits and anomaly detection (shared IP farming,
    implausibly fast completions).
  - This raises the cost of abuse; it does not make fraud impossible. Decide
    an acceptable fraud-tolerance threshold rather than over-investing in a
    perfect solution that doesn't exist on web.

### 10.3 Revenue → soft coin conversion

Actual ad revenue per impression is not known at the moment of reward:

1. **Estimated eCPM** (the same trailing average from 10.1) computes the
   reward instantly, for a responsive user experience.
2. **Async reconciliation** replaces that estimate with the real number once
   the network's revenue report lands — the full mechanism, including what
   updates and what never does, is in Section 10.4.
3. **Target rate: as close to 1:1 as possible** (e.g., $1.00 eCPM ≈ 100 soft
   coins), rather than a large built-in margin. The platform's protection
   against volatility is a small, separately-tracked **ad-yield buffer**
   (`ad_yield_buffer` in `fee_config`, Section 8.2) rather than a wide
   spread baked into the headline rate. This buffer absorbs:
   - estimate-vs-actual variance between reward time and reconciliation,
   - ad network payment lag (networks often pay 30–60–90 days after the
     impression, while the reward is granted instantly — the buffer is
     working-capital insurance against that gap),
   - currency conversion risk if a network settles in a currency other than
     the platform's operating currency,
   - **invalid traffic (IVT) clawbacks** — networks audit for bot traffic,
     scraper activity, and repeated/accidental clicks at month-end and void
     that impression's revenue retroactively, even after firing a real-time
     auction price. This is genuine uncertainty at reward time, unlike the
     next point, and is exactly the kind of thing the buffer exists for —
     it's also the financial echo of Section 9's ad-fraud vectors: fraud
     that slips past those defenses shows up here as revenue GameGuild
     never actually collects, discovered only at the audit.

   **One thing the buffer should *not* have to absorb: Gross vs. Net CPM.**
   A network's real-time callback (if GameGuild integrates one, Section
   10.1) may report the Gross CPM — the full amount the advertiser paid —
   while the actual settlement reflects Net CPM after the network's own
   wrapper cut (commonly 15–20%, set by contract). That percentage is known
   in advance, not a surprise discovered at reconciliation, so it belongs in
   the estimate calculation itself (`estimated_revenue = callback_price ×
   contracted_revenue_share_pct`, Section 10.4), not folded into the buffer
   as if it were unexplained variance. Treating a deterministic contractual
   fact the same as genuine IVT uncertainty would hide a known number behind
   a mechanism meant for the unknown one.

   The buffer starts conservative (wider for cold-start networks, Section
   10.1) and tightens over time as reconciliation data proves the estimate
   is reliable — but it should never go fully to zero. IVT variance in
   particular is structural, not a symptom of an immature estimate: even a
   fully reconciled, real-time-bid-integrated network still gets audited for
   invalid traffic every month, so a residual 2–5% gap between a real-time
   auction price and what actually settles is expected indefinitely, not
   something better pricing infrastructure eventually eliminates.

   **This is distinct from the fee on a user-initiated hard→soft
   conversion.** `ad_yield_buffer` exists specifically because ad revenue is
   uncertain and delayed — none of that risk is present when a user
   converts hard coins they already hold into soft coins; there's no
   external counterparty, no revenue lag, nothing to hedge against. That
   conversion instead uses a separate `hard_to_soft_fee` (Section 8.2),
   which only needs to cover GameGuild's own processing/operational cost,
   not ad-market volatility. Charging a user the ad-yield buffer on their
   own internal conversion would be justifying a fee with a risk that
   doesn't apply to that transaction.

### 10.4 Reconciliation loop: from actual revenue back to future estimates

Every reward granted under Section 10.3 is sized against a guess — the
trailing or cold-start eCPM estimate. That guess has to be replaced with the
real number the moment the real number exists, not left to drift out of
date. This is two distinct steps, not one, and they touch different things:

**1. Record the actual, against the batch — not against the ledger.** When
a network's revenue report lands (with the payment lag described above),
GameGuild records the actual revenue against the specific batch of
impressions it covers. This isn't a `ledger_entries` record — it doesn't
move value between wallets — it's an operational fact about how much a past
batch of ad views actually generated:

```
ad_revenue_batches:
  id
  network
  period_start, period_end     the window of ad views this batch covers
  contracted_revenue_share_pct known in advance from the network contract;
                                 applied to any real-time callback price to
                                 get a Net estimate before it's ever treated
                                 as `estimated_revenue` — never left for
                                 reconciliation to discover (Section 10.3)
  estimated_revenue            sum of (eCPM estimate × impressions ×
                                 contracted_revenue_share_pct) used to size
                                 rewards at the time
  soft_coins_granted           total soft coins minted against this batch
  actual_revenue                nullable until the network reports it
  actual_reported_at
  variance                      actual_revenue − estimated_revenue —
                                 primarily IVT clawbacks at this point,
                                 since revenue share is already priced in
  status                        pending | reconciled
```

**This loop doesn't go away if GameGuild later adopts real-time bidding.**
A Prebid-style auction callback (Section 10.1) is a much better estimate
than a trailing average — it's the actual clearing price at the moment the
ad was shown, not a historical guess — but it is still not the final
settled number. Expect roughly a 2–5% gap between that callback price and
what actually lands in the bank account at month-end, because invalid
traffic (IVT) is only discovered in the network's own end-of-month audit,
after the callback already fired. Better pricing infrastructure narrows the
gap between estimate and actual; it does not close it. The reconciliation
mechanism above is structural — necessary at every tier of pricing
sophistication GameGuild might adopt, not a stopgap for the absence of
real-time bidding.

**2. Feed the actual back into the guess — never into the past.** Once a
batch reconciles, its variance updates three things going forward:

- **The network's trailing eCPM** (Section 10.1) — the input to *future*
  reward sizing for that network shifts toward what actually happened.
- **That network's `ad_yield_buffer`** (Section 10.3) — a network whose
  actual revenue consistently lands close to its estimate earns a tighter
  buffer over time; one with persistent negative variance keeps a wider
  buffer or gets deprioritized in the ranking entirely (Section 9).
- **The "collected/receivable ad revenue" figure in the custody equation**
  (Section 7.1) — that figure is only ever an estimate until a batch
  reconciles; once it does, the real number replaces the estimate there,
  keeping the 1:1 custodial match honest rather than perpetually resting on
  a guess.

**What never changes: the soft coins already granted to a student for a
specific ad view.** That reward was final the moment it was granted —
ledger entries are never edited (Section 6) — regardless of what the
reconciled actual revenue turns out to be. If a batch reconciles below
estimate, GameGuild absorbs the gap; that's exactly what `ad_yield_buffer`
exists to cover, not a reason to claw back rewards after the fact, which
would violate the append-only ledger invariant and be hostile to the
student besides. Reconciliation corrects GameGuild's model of the future —
its eCPM estimate, its buffer sizing, its own custodial accounting — never
a specific reward already paid.

---

## 11. Marketplace purchase flow

Sellers, not buyers, decide what currency a product accepts:

- **Hard only**
- **Soft only**
- **Either** (buyer picks one at checkout — a toggle, not a split)
- **Fixed mix** (seller sets an exact hard/soft ratio; both legs debit
  atomically — if the buyer lacks either currency, the purchase fails
  entirely, no partial debit)

Sellers who accept soft coins do so knowingly, understanding they cannot
convert that portion back to hard coins or cash. The platform does not owe
them a bailout path after the fact — this is a pricing decision made at
listing time.

The platform takes a fee on every transaction, proportional across both legs
in a fixed-mix purchase.

### 11.1 Refunds

Ordinary marketplace purchases use the same hold-then-reverse mechanism as
escrow reclaim (Section 5) and disputes (Section 6.3), not a separate
mechanism:

- A refund request opens a `refund_pending` hold on the seller's credited
  entry (Section 6.3). If approved within the product's refund window, a new
  `refund` ledger entry reverses the transaction: the seller's `earned`
  balance is debited, and the buyer's balance is restored **with its
  original source tag** — a `fixed_mix` purchase reverses both legs
  atomically, same as the original debit. If denied, the hold releases and
  nothing changes.
- A `refund_fee` (Section 8.2) may be retained to cover processing cost,
  mirroring the escrow reclaim fee.
- **The refund is only cleanly reversible while the seller's earned balance
  from that sale hasn't been cashed out yet.** This is exactly why the
  payout hold period (Section 12.1) matters beyond fraud prevention — it's
  also what keeps refunds simple. Within the hold window, a refund is a
  ledger reversal against a balance that's still sitting in the system. Once
  a payout has already reached a seller's bank account, the balance to
  reverse no longer exists inside GameGuild, and the refund becomes a debt
  against the seller's future earnings rather than an instant reversal —
  the same residual risk every marketplace with instant seller payouts
  carries, and the reason Stripe's own Connect tooling supports per-account
  reserve plans specifically for sellers with a high refund rate.

---

## 12. Cash-out & compliance posture

### 12.1 Payout holding period — closing the two-hop chargeback gap

The `purchased`/`earned` split (Section 3) blocks the *direct* version of
the chargeback exploit (buy hard coins, cash out immediately, dispute the
charge) but not the two-hop version: attacker buys `purchased_hard` with a
stolen or soon-to-be-disputed card, immediately spends it on a colluding
second account (a marketplace "sale," or more cheaply, a bounty claim with
no deliverable at all), that second account receives fresh `earned_hard`
with no memory of the original card, and cashes out before the chargeback
lands. The source tag alone doesn't stop this — it only tracks *how* money
was obtained, not *how recently* it entered the system.

**Fix: a payout holding period on newly earned balances, independent of the
purchased/earned split.** Stripe Connect already provides this mechanism
rather than requiring it to be built from scratch: a **rolling reserve
plan** withholds a percentage of each transaction for a connected account
and releases it after a set number of days, and platforms commonly run a
**probationary reserve specifically for new connected accounts** — funds
from a new account's transactions are held and released only at the end of
that probation window. Reserve holds can run up to 180 days, and the
platform's Terms of Service must clearly state the reserve policy for it to
be enforceable.

**Decided policy:**
- **New connected accounts run under a 120-day probationary reserve**,
  matching the typical card-network dispute window, rather than a shorter
  risk-tiered hold. This closes the two-hop gap by default rather than
  relying on risk-scoring to catch every case — the trade-off is that every
  legitimate new creator's first payouts are also held for the full window,
  not just flagged-risk ones.
  - **Tapering exception, to avoid a 120-day wall for every new creator
    regardless of trust signal:** an account that clears a defined set of
    risk checks early (KYC verified, no refund/dispute activity, no
    device/IP/payment-fingerprint overlap with other accounts it's
    transacted with) can have its *remaining* held transactions released on
    an accelerated schedule rather than waiting out the full 120 days on
    every one. This needs to be an explicit, documented exception path — not
    an ad hoc override — so the 120-day default stays the enforceable
    baseline in the Terms of Service while still giving trustworthy
    creators a way out of the full wait. Exact clearance criteria and
    accelerated schedule are a business decision (Section 15).
- **Step-up KYC (Sumsub or similar) auto-triggers by risk score/velocity**,
  not a flat withdrawal-amount threshold or blanket first-withdrawal rule.
  Signals to score on: transaction velocity between poster/claimer
  accounts, device/IP/payment-fingerprint overlap, account age relative to
  payout size, and refund/dispute history. Exact thresholds are a business
  decision (Section 15) — the mechanism is risk-score-driven, not amount-
  driven, so a large first payout from a long-established, clean-history
  account doesn't need the same friction as a small payout from a
  brand-new account showing collusion signals.
- Radar for Platforms (Stripe's fraud-scanning tool for Connect
  marketplaces) is a third, complementary layer on the original charge side,
  and per-account reserve tuning based on refund rate also covers the
  refund-risk case in Section 11.1.

**Launch posture:** the 120-day hold is intentionally the maximally
conservative option, not a middle-ground compromise — at launch, GameGuild
has zero internal dispute-rate and fraud data, so there's no honest basis
yet for a shorter default. This is expected to change once real operational
history accumulates: as GameGuild builds its own fraud/dispute-rate data,
the default hold can shorten, the tapering exception's clearance criteria
can loosen, and step-up KYC thresholds can be tuned against real outcomes
rather than a first-principles guess. Treat this as the deliberately strict
starting position, not the permanent one.

### 12.2 Processor and entity

GameGuild's operating entity (formed via Stripe Atlas) is a **Delaware
C-corp**. Delaware incorporation governs corporate-law questions (equity,
governance) but does **not** by itself resolve money-transmission
compliance — that analysis runs per-state and per-country based on **where
users are located**, not where the company is incorporated. Being a Delaware
entity doesn't grant an exemption from other states' or countries' money
transmitter laws.

Stripe Atlas is incorporation only. Cash-out requires **Stripe Connect**
separately, which:

- Handles payee KYC/identity verification and risk-based screening during
  connected-account onboarding.
- Handles 1099 tax reporting (US) and international payout compliance.
- Acts as the licensed money transmitter, so GameGuild doesn't need its own
  license as long as it stays structured as the software/marketplace layer
  and Stripe Connect is the actual funds custodian and mover.
- In the standard marketplace (indirect charge) integration, GameGuild —
  not Stripe — is typically responsible for a connected account's negative
  balance, which is precisely the exposure the reserve plan in Section 12.1
  manages. This is not automatic protection Stripe provides by default; it
  requires GameGuild to actively configure the reserve plan.

This is a real compliance question, not just a technical one — recommend
actual legal counsel review the wallet/stored-value model specifically
(some jurisdictions treat "stored value redeemable for cash later" as its
own regulated category regardless of which processor moves the money)
before cash-out goes live with real users.

### 12.3 Reserve accounting

**Decision: fully segregated reserve, no revenue-funded float, for now.**
This is a specific application of the custody principle in Section 7.1:
`earned_hard` and outstanding soft coins are both internal claims against
external custodial cash, and neither is exempt from the 1:1 matching rule
just because it's labeled "reserve" rather than "user wallet."

Every `earned` hard coin sitting in a user's wallet is a liability — money
GameGuild owes out, whether or not it's been withdrawn yet. The segregated
model holds real cash 1:1 against every `earned_hard` unit the moment it's
earned, not the moment it's cashed out. This matches how regulated
stored-value and prepaid-access programs operate (PayPal balances, gift card
float, prepaid cards) and is the model regulators already recognize.

**Soft coins are a second, different kind of liability, not addressed by the
`earned_hard` reserve above.** Soft coins are never cash-redeemable, but
they aren't free for GameGuild to honor either: per Section 4.2, AI
inference cost is burned against internal token cost accounting when a
student pays for grading in soft coins — meaning GameGuild still owes real
money to its LLM provider regardless of what currency the student paid
with. An unspent soft-coin balance is therefore a standing promise of a
real-money-costing service, not a cash promise, but a promise with a real
cost nonetheless.

**Decision: soft coins get their own explicit reserve, separate from the
`earned_hard` reserve.** GameGuild holds cash against the sum of all
outstanding (unspent) soft-coin balances, sized at GameGuild's expected
internal redemption cost — not at the ad-eCPM value the coin was minted at.
This distinguishes it clearly from `ad_yield_buffer` (which protects the
*reward-minting* rate against ad-market volatility) and from the
`earned_hard` reserve (which backs a cash obligation, not a service
obligation).

**Sizing formula, fully conservative for launch:**

```
soft_coin_reserve = outstanding_soft_coin_supply × max(
                       current_provider_rate_card_cost_per_grading_call,
                       trailing_actual_average_cost_per_grading_call
                     )
```

Three decisions fix this formula:

1. **Scope — worst case, not probability-weighted.** Every outstanding soft
   coin is assumed to eventually get spent on AI grading, even though in
   practice most will go toward p2p transfers, fee burns, or soft-payable
   goods that cost GameGuild nothing to honor. This overstates the true
   expected liability, deliberately — consistent with GameGuild's stated
   launch posture (Section 12.1) of erring safe with zero operational
   history to lean on.
2. **Cost input — sourced from real metered data, not a fixed ceiling or a
   periodic guess.** Every AI grading call returns an exact, metered cost
   from the provider — there's no need to estimate the cost of a
   *completed* transaction. The open question is only what rate to assume
   for coins *not yet spent*: use whichever is higher, the current live
   provider rate card or the trailing actual average of real per-call
   costs, recalculated continuously as new metered data arrives. Using the
   higher of the two — rather than an average — matters because it's
   consistent with the worst-case scope in (1): if grading ever spans
   multiple model tiers at different price points, sizing off an average
   would quietly break the worst-case assumption the moment an outstanding
   coin lands on the pricier tier. Use the most expensive currently-offered
   grading tier's rate as the floor for this calculation, not a blended
   average across tiers.
3. **Breakage — 0% for now.** No discount for soft coins that historically
   go unredeemed (abandoned accounts, etc.), since there's no internal data
   yet to justify assuming any. Revisit once GameGuild has its own
   redemption-rate history (Section 15) — this, like the payout hold in
   Section 12.1, is the deliberately strict starting position, not the
   permanent one.

Whether soft coins should expire after a period of inactivity — which
would bound this liability's long-tail size independent of the breakage
question above — remains open (Section 15).

**Future consideration (explicitly not designed yet):** once reserve
balances are large enough to matter, the `earned_hard` reserve could be
placed in low-risk, liquid securities that earn a yield for GameGuild (e.g.,
short-term treasuries or a money-market fund) instead of sitting idle in
cash. That's a meaningfully different regulatory posture than pure 1:1
cash — most stored-value frameworks that permit reserve investment impose
liquidity and risk-profile restrictions, and require the reserve to remain
redeemable on demand regardless of the investment vehicle's own liquidity.
This needs its own legal/compliance review when it becomes relevant. For
now: uninvested cash, held 1:1. The soft-coin reserve (above) stays
uninvested cash too, for the same reason.

---

## 13. User-facing balance: total vs. withdrawable

Users need to see two different numbers, not one, and the difference
between them has to be exactly this precise given the payout hold (Section
12.1) and the hold/reversal mechanism (Section 6.3) — showing a single
ambiguous "balance" risks a user believing money is available when it's
actually still inside the 120-day window or frozen against an open dispute.

- **Current total** — every `earned_hard`/`earned_soft` credit to the
  wallet, minus everything spent, cashed out, or reversed. This is the
  standard ledger-derived balance from Section 6.2: it includes money still
  maturing under the payout hold, so it is *not* the amount a user can
  withdraw right now.
- **Withdrawable** — the subset of `current total` (hard coins only; soft
  coins are never withdrawable, Section 2) where every contributing entry
  has passed its `matures_at` date (Section 6.1) **and** carries no active
  hold (Section 6.3: no open dispute, no pending refund, no risk-review
  freeze on the entry or account). Computed the same way as total — summed
  from the ledger and holds table at query time, cached for fast reads, and
  checked against the periodic full recompute (Section 6.2) like every
  other balance figure.

**Conservative-by-default rule:** if the cached withdrawable figure and a
fresh recompute ever disagree, or a hold's status can't be confirmed against
its source of truth (e.g., a Stripe dispute webhook hasn't been reconciled
yet), the **lower of the two numbers is what's shown and what's enforced**
at cash-out time — never the higher. Given GameGuild has no operational
track record yet to lean on, an accidental early payout is a far worse
outcome than a user seeing "withdrawable" tick up a day later than it
technically could have. This is the same conservative-launch posture as the
120-day hold itself (Section 12.1), applied to the balance calculation
specifically: default to safe, loosen once real data justifies it.

The UI should show both numbers plainly (e.g., "Total: $X · Withdrawable
now: $Y") rather than collapsing them into one figure with a tooltip — the
gap between the two is meaningful information (it tells a creator how much
is still maturing), not just an implementation detail to hide.

---

## 14. Summary of hard invariants

These rules must be enforced at the data layer (constraints or stored
procedures), not just in application logic, because a client or API bypass
that violates any of these turns a design safeguard into an actual exploit:

1. Soft coins can never convert to hard coins or cash, under any path,
   including via escrow reclaim or product resale.
2. `Purchased` hard coins can never be cashed out — only `earned` hard coins
   can.
3. Escrow reclaim must restore the original source tag, not launder
   `purchased` into `earned`.
4. Every soft coin in existence must trace back to either ad revenue or a
   real hard-coin debit — none are created from nothing.
5. Fixed-mix purchases debit both currencies atomically or not at all.
6. Ledger entries are append-only and hash-chained — no entry is ever
   updated or deleted; corrections are new offsetting entries.
7. Creator fees on AI grading/peer review are layered on top of inference
   cost and platform fee, never carved out of them.
8. Newly earned balances are subject to a 120-day payout holding period
   before cash-out by default, independent of the purchased/earned source
   tag — the tag alone does not stop money laundered through a second,
   colluding account. Accelerated release requires clearing a documented
   risk exception, not an ad hoc override.
9. `ad_yield_buffer` (ad-revenue conversion risk) and `hard_to_soft_fee`
   (user-initiated conversion) are distinct fees and must never be
   collapsed into one line item.
10. Unspent soft-coin balances are backed by their own reserve, sized at
    100% of outstanding supply against the higher of current provider rate
    card or trailing actual cost, with 0% breakage assumed for now —
    separate from both the `earned_hard` reserve and the `ad_yield_buffer`.
11. Refunds and disputes are never handled by editing or deleting a past
    entry — only by a hold (Section 6.3) resolved into either a release or
    a new offsetting entry.
12. A wallet's withdrawable balance never includes an entry that hasn't
    matured or carries an active hold, and never shows a higher figure than
    the most recent verified computation when the cache and a fresh
    recompute disagree.
13. Admin wallet fee revenue is not GameGuild's own usable money until
    physically withdrawn from Stripe (or collected ad-network revenue) to
    the company's own bank account — an internal ledger credit is a claim,
    not possession. External custodial cash and the sum of all internal
    claims must match 1:1 at all times (Section 7.1).
14. Admin wallet fee revenue is withdrawn on a monthly cadence, and only the
    portion that has both matured and carries no active hold is eligible —
    the same maturity rule applied to user payouts applies to GameGuild's
    own revenue, with no exception for the platform's own money (Section
    7.2).
15. Ad revenue reconciliation only ever corrects forward-looking parameters
    (trailing eCPM, `ad_yield_buffer`, network ranking, the custody
    equation's receivable figure) — it never retroactively alters a soft
    coin already granted for a specific ad view (Section 10.4).
16. A network's contracted revenue-share percentage (Gross vs. Net CPM) is
    applied to any callback price before it's used as `estimated_revenue`
    — it is never treated as reconciliation variance, since it's known in
    advance and isn't the same kind of uncertainty as an IVT clawback
    (Section 10.3, 10.4).

---

## 15. Open items for follow-up (not covered in this paper)

- Database schema and API endpoint design.
- Detailed ad-fraud detection thresholds and rate limits (tune the caps
  and windows in Section 9).
- Legal review of the cash-out and reserve model with counsel (jurisdictional
  variations, tax withholding specifics, stored-value classification).
- Exact platform fee, creator fee caps, reclaim fee, refund fee, and both
  `ad_yield_buffer`/`hard_to_soft_fee` values (structure defined in Section
  8.2; values are a business/finance decision, tunable via `fee_config`).
- Exact accelerated-release clearance criteria and schedule for the 120-day
  hold tapering exception (Section 12.1), and the specific risk-score
  thresholds that trigger step-up KYC — the policy is decided, the
  parameters are not.
- Stripe webhook integration for dispute status (Section 6.3) — mapping
  Stripe's dispute lifecycle events to the `holds` table's `dispute_open` /
  `dispute_lost` / `dispute_won` states reliably, including what happens if
  a webhook is delayed or missed (the conservative-by-default rule in
  Section 13 covers the principle; the actual reconciliation job against
  Stripe is implementation work).
- Per-transaction-type fee-revenue maturity windows (Section 7.2) — once
  GameGuild has operational data, shortening the uniform 120-day admin-
  wallet hold for fee revenue with lower or no card-dispute exposure (e.g.,
  fees generated entirely from already-matured earned-hard-coin spend,
  where no card transaction is directly in play).
- Exact thresholds for tightening or widening a network's `ad_yield_buffer`
  based on reconciled variance history, and the impression-count or day
  count a cold-start network needs before it graduates from the
  provider-supplied estimate to an internally-measured trailing average
  (Section 10.1, 10.4) — the mechanism is specified, the numbers are not.
- Commercial due diligence on the candidate ad networks (Section 10.1) —
  actual contracted revenue-share percentage, minimum traffic requirements,
  payout terms, and real eCPM data for GameGuild's specific traffic profile
  before committing to any of them.
- Soft-coin expiration policy (Section 12.3) — whether unspent soft coins
  should expire after a period of inactivity, to bound the reserve's
  long-tail size independent of the 0%-breakage assumption already decided.
  The reserve *sizing formula* is now settled; this is the remaining
  open question around it.
- Default creator currency preference for creator fees (`either` vs.
  `hard_only`) and whether GameGuild will offer the platform-subsidized
  hard-coin front described in Section 8.1.1, given it costs GameGuild
  money per transaction if offered.
- **Charity/nonprofit revenue-share (Humble Bundle model):** a
  suggested-percentage slider that routes part of a real-money transaction
  to a nonprofit, via a donation-payments intermediary (e.g., Every.org,
  Pledge, Percent) rather than a direct-to-nonprofit transfer, so GameGuild
  doesn't take on nonprofit vetting or tax-receipt issuance itself. Needs a
  decision on where it plugs in — at hard-coin purchase (wallet top-up
  split) vs. a future direct real-money product checkout — before design.
- Global vs. per-wallet hash-chain structure if ledger write throughput ever
  becomes a bottleneck (Section 6.4).
