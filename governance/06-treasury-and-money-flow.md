# 06 — Treasury & Money Flow

## All revenue streams (enumerate — platform-derived)

| Stream | Rail | Notes |
|---|---|---|
| Subscriptions (SaaS tiers) | fiat + crypto | main recurring |
| Marketplace sales (courses, assets, digital goods) | fiat (Stripe) + platform currencies | **creator-first split — see below** |
| Sponsorships / ads | fiat | episodic |
| API licensing | fiat | later stage |

## Marketplace split rule (creator-first)

Selling courses, assets, and other digital goods on the platform follows one rule:

**Sale proceeds go primarily to the content creator; the remainder (a platform fee) goes to GameGuild.**

- Platform fee target: **5–10%** (competitive vs. Udemy 63%/30%, Unity 30%, Fab 88%/12% rev-share tiers — being near the low end is the positioning)
- Per-product fee is configurable (the platform implements per-product fee policy in parts-per-million — see `GameGuild.Finance.Economy.Marketplace/ProductCurrencyPolicy.cs`), so premium/exclusive listings can deviate by agreement
- Settlement splits each order into seller proceeds + platform fee with conservation enforced at settlement time — the creator-first rule is structural, not a manual payout decision
- Refunds: **14-day buyer window**; creator earned balance clawed back automatically; fee and proceeds both reverse
- **GameGuild is the merchant of record** — the company is the legal seller: collects payment, remits sales tax/VAT, owns refund liability, then pays the creator. Creator payouts are **on request** (minimum threshold TBD).
- GameGuild's platform-fee income flows into the canonical treasury flow below and is the base for the quarterly distribution cycle (07)

Example: $100 course sale → creator receives $90–95, GameGuild receives $5–10 platform fee.

## Currency model (HardCoin / SoftCoin)

- **HardCoin** = fiat-backed credits: purchased with real money, spendable on the platform. Effectively prepaid store credit.
- **SoftCoin** = earned rewards or purchased **with HardCoin only**. **SoftCoin never converts back to HardCoin** (one-way gate) — this keeps SoftCoin outside money-transmission/e-money territory and makes earned rewards non-refundable.
- Implication: HardCoin float is a liability on the books (unearned revenue) — must be reserved 1:1 and reported; BR/US counsel confirm treatment at incorporation. Refund policy for purchased-but-unused HardCoin to define in ToS.

## All outflows

| Outflow | Rail | Governance |
|---|---|---|
| Infra costs | fiat/card | within approved quarterly plan — CEO discretion |
| Team/contractor compensation | fiat (BR/US) + crypto | contracts approved at quarterly meeting |
| Founder loans (principal) | stablecoin/fiat | repaid before distributions (07) |
| Governor/investor distributions | stablecoin | voted each quarter (07) |
| Treasury reserve | stablecoin + blue-chip crypto | allocation voted quarterly |

## Canonical flow

```
Revenue (fiat)
  → operational entity accounts (BR/US) → pay fiat costs
  → quarterly sweep to treasury (every 3 months, aligned with the governors' cycle)
Revenue (crypto / platform currencies)
  → direct to treasury wallet
Treasury (Safe multisig)
  → ops float (minimum 6-month runway floor)
  → reserve (vote-set allocation)
  → distribution pool (voted each quarter, 05/07)
```

## Money-flow rules

1. **Anything not in the approved quarterly plan requires the dual-condition qualified majority** (≥⅔ of voting weight AND a majority of governors — see 05). Planned spend flows freely under CEO discretion.
2. Every distribution pool movement is recorded in the decisions log (canonical record) and executed from the treasury wallet — auditable by all shareholders.
3. Fiat/crypto conversion policy: defined per entity for tax reasons (see 03-legal-structure).
4. **Runway floor: 6 months** — distribution votes may not drain below it.
5. HardCoin float: reserved 1:1 as unearned-revenue liability; never spent as if it were income.
6. **Emergency freeze**: any 2-of-3 governors may freeze non-routine treasury actions pending a formal vote (freezing ≠ passing — deliberately easier than spending).
