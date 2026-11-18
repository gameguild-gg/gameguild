# 07 — Profit Sharing & Payouts

> Distribution shares = revenue (economic) shares, computed from cap table at snapshot. Crypto payout rail.

## Agreed model

- **Distribution is voted every quarter** at the quarterly governors' meeting (see 05): governors vote how much of the quarter's distributable profit goes to revenue shareholders.
- Payouts in crypto (stablecoins, e.g., USDC).
- **Only revenue shareholders receive funds and bear pro-rata capital-call obligation for company bills/debts. Governance-only shareholders receive nothing and owe nothing.**
- Revenue shares are transferable under ROFR; only vested shares transfer; whoever holds them at the quarterly snapshot receives that quarter's payout.

## Repayment waterfall (in the operating agreement)

```
Revenue in
  1. Operating costs (servers, payroll/contracts, tools)  ← keep the lights on
  2. Founder loans (principal)                             ← repaid before distributions
  3. Quarterly distribution pool ← voted each quarter by governors
  4. Reinvestment / reserve  ← remainder, allocation voted quarterly
```

## Cadence

```
Every 3 months (quarterly governors' meeting)
  ├─ close books: revenue, costs, debt service
  ├─ review: performance vs. plan
  ├─ vote: distribution % of distributable profit (0–100%)
  ├─ vote: next quarter's plan (becomes the planned-spend baseline, 05)
  ├─ vote: contract approvals for the quarter's work
  └─ execute: USDC transfers to revenue shareholders, pro-rata per cap-table snapshot
```

Runway floor: **6 months** of operating costs must remain after any distribution.

## What each quarterly vote decides

| Parameter | Options |
|---|---|
| Distribution % of quarterly distributable profit | 0–100% |
| Extra founder-loan repayment tranche | optional acceleration |
| Reinvestment allocation | product / growth / reserve |
| Special allocations | bounty pools, contributor grants |

## Founder loans (repaid before distributions)

Money initial governors personally spent building the platform before/around incorporation becomes formal company debt:

1. **Document every expense**: date, amount, what it paid for (servers, contractors, tools), who paid. Signed schedule attached to the loan agreement — retroactive documentation is fine **if done before the investor round**.
2. **Founder Loan Agreement** signed at/before incorporation: company acknowledges the documented amounts as debt. 0% interest (principal repayment is not income; interest would be taxable).
3. Repayment: **cash or convert to equity at a discount** if a SAFE/priced round closes — election made at the round.
4. Senior to all shareholder distributions; repaid quarterly alongside the payout cycle.

⚠️ Must be fully documented **before** signing any investor SAFE.

## Creator payouts (separate from shareholder distributions)

- Platform marketplace sales split creator-first (see 06): creator proceeds + GameGuild platform fee at settlement.
- **On-request payouts**: creators withdraw their balance whenever they want (minimum threshold TBD).
- Refunds: 14-day buyer window; creator's earned balance **clawed back automatically** on refund.

## Distribution mechanics

| Mechanism | Notes |
|---|---|
| **Quarterly USDC batch transfers** | default: company wallet → revenue shareholder wallets; per cap-table snapshot; recorded in decisions log |
| Buyback | optional: company repurchases shares instead of payouts; different tax treatment per holder jurisdiction (see 03) |

KYC'd wallet registry for all revenue shareholders; treasury diversification via rolling DCA, never lump dumps.

## Anti-abuse

- Profit-share computed at quarterly snapshot; wallets declared/KYC'd for compliance
- Transfers: ROFR + vested-only (04) — no buying a payout stream the week before a snapshot
- New investors joining mid-quarter: pro-rata from entry snapshot only

## Tax note

Each distribution is a taxable event per holder jurisdiction (BR holders: law 14.754/2023 treatment; US: ordinary income vs dividend characterization — see 03). Quarterly payouts mean quarterly filing obligations for US persons — budget for quarterly estimated payments.
