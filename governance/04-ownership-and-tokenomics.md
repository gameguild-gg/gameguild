# 04 — Ownership & Tokenomics

## Principle: three separate ledgers

1. **Equity** (legal wrapper) — founder shares, investor shares. Paper.
2. **Governance tokens** — vote on product + treasury. On-chain.
3. **Profit-share rights** — receive distributions. Can be same token, different function.

## Founder allocation (agreed internally)

| Founder | Governance/economic share (of founder pool) |
|---|---|
| Tolsta | 40% |
| Matheus | 30% |
| Joel | 30% |

**Mandatory before issuance**: vesting schedule (proposed: 4 years, 1-year cliff). Unvested founder tokens must be reclaimable — otherwise a co-founder exit at month 6 with 30% is fatal.

## Investor reserve

Reserve carved as its own allocation, not retroactive dilution of founders:

| Allocation | % of total | Notes |
|---|---|---|
| Founders (vested) | 60–70 | 40/30/30 internally |
| Investor pool (angels → seed) | 15–25 | tranches per round |
| Team/advisor pool | 10 | optional, decide early |

Dilution path (illustrative):

| Stage | Founders | Pool | Investors (cumulative) |
|---|---|---|---|
| Incorporation | 100% | – | – |
| Angels (~$100–300k) | ~85% | 10% | ~5–8% |
| Seed ($1–3M) | ~65–70% | 12% | ~20–25% |

## Token parameters (open)

- Total supply: fixed (e.g., 10,000,000) vs. mintable per round — fixed + reserve escrow recommended
- Transferability: **non-transferable initially** (internal governance only) — avoids securities-offering complications until legal clears transfer
- Investor tokens: vesting (1y cliff typical) + lockup
- One token, two functions (vote + profit-share right) — simplest for 3 founders

## Open questions

- [ ] Vesting terms sign-off from all three founders
- [ ] Investor instrument: SAFE + token warrant vs. direct token purchase (SAFT)
- [ ] Does profit-share right follow token balance at snapshot, or time-weighted holding?
