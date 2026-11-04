# 06 — Treasury & Money Flow

## All revenue streams (enumerate — platform-derived)

| Stream | Rail | Notes |
|---|---|---|
| Subscriptions (SaaS tiers) | fiat + crypto | main recurring |
| Transaction / marketplace fees | crypto-friendly | % take rate |
| Content sales rev-share | platform | split with creators |
| Sponsorships / ads | fiat | episodic |
| API licensing | fiat | later stage |
| Token-locked discounts | crypto | crypto-native perk |

## All outflows

| Outflow | Rail | Governance |
|---|---|---|
| Infra costs | fiat/card | ops budget, below vote threshold |
| Team compensation | fiat (BR/US payroll) + crypto bounties | below threshold |
| Investor distributions | stablecoin | per 3-year mandate |
| Founder distributions | stablecoin | per 3-year mandate |
| Treasury reserve | stablecoin + blue-chip crypto | allocation set by vote |

## Canonical flow

```
Revenue (fiat)
  → operational entity accounts (BR/US) → pay fiat costs
  → periodic sweep to treasury
Revenue (crypto)
  → direct to treasury contract (splitter)
Treasury (Safe multisig)
  → ops float (3–6 months runway)
  → reserve (vote-set allocation)
  → distribution pool (accumulates between 3-year cycles)
```

## Money-flow rules

1. Treasury actions above threshold X require a passed vote + timelock.
2. Every distribution pool movement is on-chain (auditable by all holders).
3. Fiat/crypto conversion policy: defined per entity for tax reasons (see 03-legal-structure).
4. Runway floor: distribution votes may not drain below N months of runway.

## Open questions

- [ ] Vote threshold X (treasury spend requiring vote)
- [ ] Runway floor N (months)
- [ ] Sweep cadence fiat→treasury (monthly? quarterly?)
- [ ] Which chains (Ethereum L2 / Solana / Base) — fee + tooling dependent
