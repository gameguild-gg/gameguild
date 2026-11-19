# 04 — Ownership & Cap Table

## Terminology

**Governors** = holders of governance shares. The initial three governors are Tolsta, Matheus, and Joel; the class extends to future members who join governance. "Founder" is reserved for legal artifacts (founder loans, founder shares at incorporation).

## Two share classes

1. **Revenue shares (economic class)** — receive quarterly dividends/payouts, bear pro-rata capital-call obligation for company bills/debts (see 07). The money class.
2. **Governance shares** — vote on product + treasury proposals. No economic rights: receive nothing, owe nothing.

The cap table (Delaware stock ledger — can be a signed spreadsheet) records each holder's position in **both** classes independently.

| Holder type | Revenue shares | Governance shares |
|---|---|---|
| Governors (initial three) | ✅ | ✅ |
| Advisor / future governor (vote-only) | ❌ | ✅ |
| Investor | ✅ | voting per deal — no standing cap; reviewed case-by-case per round |

Splitting into two classes buys:

- **Separate transfer rules per class** — both carry ROFR; economic and voting interests can still change hands independently of each other
- **Clean investor terms** — investors buy revenue shares; governance included (or not) per deal
- **Separation of work from ownership** — governors who work for the company do so under contract, paid as contractors; ownership and compensation never mix (see Compensation model, 05)
- **Simpler cap table reasoning** — payouts computed off one class, votes off the other

Implementation: Delaware supports this as two series of stock (or non-voting common + voting common) in the charter. Counsel picks the exact instruments.

## Governor allocation (agreed internally)

| Governor | Governance class | Revenue class (of initial pool) |
|---|---|---|
| Tolsta | 40% | 40% |
| Matheus | 30% | 30% |
| Joel | 30% | 30% |

Initial governors hold both classes at 40/30/30. Vesting applies to both classes together (an unvested governor has neither vote nor economics).

## Exit & transfer rules

**Vesting (agreed)**: 0% at 1 year, vesting progressively to 100% at 5 years — 1-year cliff, then linear (25%/yr, ~2.08%/month from month 12 to 60). Unvested shares reclaimable on exit — leaving at month 6 walks with 0%, at month 3y with 50%.

**Exit for cause** (misconduct / not working): same as voluntary (unvested reclaimed) **plus harsher treatment** — exact clause (e.g., vested shares buyable at fair value, forfeiture of unpaid cycle compensation) to be defined with counsel.

**Transfers (both classes)**:

1. **Only vested shares are transferable.** Unvested shares (either class) cannot be sold or transferred — they stay with the holder until vested or reclaimed.
2. **ROFR on both classes**: existing shareholders of that class may buy at the same terms before any outsider. No transfer to outsiders without the right clearing.
3. Rationale: prevents outsiders quietly acquiring a quarterly payout stream (revenue class) or voting power (governance class) — while vested holders can still exit.

## Allocation & reserves (proposed — awaiting sign-off)

| Allocation | % of total | Notes |
|---|---|---|
| Founders (vested 5y) | 70 | 40/30/30 internally |
| Investor pool (angels + seed) | 20 | released in tranches per round |
| Team/advisor pool | 10 | early hires + advisors; company-held, no vesting until granted |

**Community reserve: carve later, at seed** (5–10% of a future loyalty program, taken proportionally from everyone at that time). Pre-reserving community in equity now is unusual and confuses angels.

Round-by-round expectations:

| Round | Size | % sold | Investors cumulative |
|---|---|---|---|
| Angels | $150–500k | 8–12% | 8–12% |
| Seed | $1–3M | to 15–25% | 15–25% incl. angels |

## Dilution rules (to be written into the operating agreement)

1. **Everyone dilutes pro-rata.** New shares issued → all existing % drop proportionally; share counts never decrease.
2. **Anti-dilution: none by default.** Broad-based weighted average only where standard SAFEs include it. Never accept full-ratchet.
3. **Pro-rata rights granted** — investors may maintain their % by participating in next rounds.
4. **Vesting is the real dilution defense** — unvested founder shares reclaimed on exit return to the company, not to other founders.
5. **Team pool dilutes like everyone else** but doesn't vest as a pool; it vests per grant.
6. **Sanity line**: founders ≥50% after seed; ~45–50% after Series A. A term sheet below that pre-A = mispriced or oversized round.

Illustrative path (founders incl. internal 40/30/30):

| Stage | Founders | Team pool | Investors (cum.) |
|---|---|---|---|
| Incorporation | 100% | – | – |
| Post-reserve carve | 70% | 10% | 20% (escrowed/authorized) |
| Post-angels (~10% sold) | ~63% | ~9% | ~10% + remaining pool |
| Post-seed (~20% cum. sold) | ~56% | ~8% | ~20% + pool remainder |

## Open questions

- [ ] Investor instrument: SAFE
- [ ] Payout snapshot mechanics: cap table at vote date (default) vs. time-weighted
- [ ] Sign-off: 70/20/10 reserve split + dilution rules above
