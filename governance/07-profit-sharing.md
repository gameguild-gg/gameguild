# 07 — Profit Sharing

> Revised 2026-11-02: no tokens. Distribution shares = equity stakes, computed from cap table at snapshot. Crypto payout rail unchanged.

## Agreed model

- Profit-share mandate is **re-decided by vote every 3 years**: distribution %, retention %, and distribution form.
- Payouts in crypto (stablecoins, e.g., USDC).
- Share of payouts = pro-rata equity (40/30/30 founders + investors per cap table at snapshot).

## What each 3-year vote decides

| Parameter | Options |
|---|---|
| Distribution % of accumulated pool | 0–100% |
| Form | lump sum (Merkle drop) / streaming (Superfluid, Sablier) / buyback (burn or treasury-retire) |
| Reinvestment allocation | product / growth / reserve |
| Special allocations | bounty pools, contributor grants |

## Distribution mechanics (no-token version)

With tokens gone, distribution is bookkeeping + transfers, no smart contracts needed:

| Mechanism | Fits | Notes |
|---|---|---|
| **USDC batch transfers per cycle** | ✅ default | company wallet/account → holder wallets; snapshot cap table at vote date; recorded in decisions log |
| **Scheduled tranche payments** | ✅ if voted pot pays out over time | e.g. monthly drips for 12 months post-vote — plain recurring transfers |
| Buyback | optional indirect | company repurchases shares instead of payouts; different tax treatment per holder jurisdiction (see 03) |

On-chain splitters (0xSplits/Sablier) become options again only if a crypto-native layer returns; research preserved in git history.

**Chosen default**: distribution pool accumulates → 3-year vote passes → swap to USDC → batch transfers per snapshot cap table (or scheduled tranches if the mandate votes for streaming). KYC'd wallet list per holder for compliance; treasury diversification via rolling DCA, never lump dumps.

## Anti-abuse

- Profit-share right computed at snapshot; wallets must be declared/KYC'd for legal payout compliance
- Minimum holding period before a transfer/secondary purchase counts for profit share (if transferability ever enabled)
- New investors joining mid-cycle: pro-rata from entry snapshot only

## Tax note

Each distribution is a taxable event per holder jurisdiction (BR: law 14.754/2023 treatment; US: ordinary income vs dividend characterization — see 03). Buyback&burn avoids per-holder payout events but has its own treatment.
