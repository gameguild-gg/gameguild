# 10 — Risks & Open Questions

## Risks

| Risk | Severity | Mitigation |
|---|---|---|
| HardCoin = prepaid credits → e-money-ish regulation + float liability | HIGH | reserve 1:1 as unearned revenue; counsel confirm treatment (US state prepaid rules + BR); ToS refund policy for unused credits |
| Creator payout compliance (money-moving platform) | HIGH | creator KYC/W-9/W-8 before payouts; payout ledger; clawback rights in ToS |
| Sales tax/VAT as merchant of record (US state nexus + BR) | HIGH | tax engine at launch; nexus monitoring as sales grow |
| Quarterly distributions to BR residents (Lei 14.754/2023) | HIGH | BR tax counsel + per-holder modeling before first distribution; BR service entities for Matheus/Joel |
| Securities/regulatory classification of crypto dividend payouts | MED | counsel review of payout mechanics per holder jurisdiction |
| Governor deadlock on structural items (unanimous + one blocker) | MED | deadlock clause agreed (05): cooling → mediation → shotgun buy-sell; counsel drafting pending |
| Governor exit unvested | MED | vesting from day one; for-cause clause |
| Investor vote dominance | MED | no standing cap by design; case-by-case review per round if >20% governance |
| Regulatory drift (BR/US crypto rules) | MED | annual legal review cadence |

## Open questions

### Legal / structural
- [ ] Counsel engagement (US + BR) — Matheus/Joel BR-resident: BR service-entity decision + Lei 14.754 modeling now confirmed needed
- [ ] **IP assignment**: all platform IP (repo, brand) assigned to company at incorporation
- [ ] Platform ToS + creator agreement: payout terms (on-request, min threshold TBD), 14-day refund clawback, HardCoin ToS (purchased-credit refund policy), SoftCoin earn rules
- [ ] Sales tax engine (GameGuild is merchant of record → company remits): Stripe Tax / Anrok / manual
- [ ] For-cause exit clause details (what "harsher" means — counsel)
- [ ] Deadlock clause drafting (agreed in 05: cooling → mediation → shotgun) — counsel terms: valuation, funding, class handling
- [ ] Unplanned-treasury dual condition (⅔ weight + governor majority) — counsel drafts precise legal formula (05)
- [ ] Drag-along/tag-along for future rounds

### Financial / operational
- [ ] Creator payout minimum threshold
- [ ] Founder loans: compile expense schedule + amounts per governor; cash-or-convert election at round
- [ ] Bookkeeping owner + quarterly close process
- [ ] HardCoin float accounting treatment (unearned revenue liability; 1:1 reserve) — confirm with accountants
- [ ] KYC scope for creator + shareholder payout wallets
- [ ] Matheus/Joel sign-off: dilution path + 1-year zero-cliff vesting

## Settled (moved from open)

- [x] Jurisdiction: Delaware DevCo-led; Cayman deferred (2026-11-02)
- [x] Vesting: 0% at 1y → 100% at 5y, linear, reclaimable (2026-11-02)
- [x] Voting platform: Loomio (2026-11-02)
- [x] No tokens / no on-chain governance (2026-11-02)
- [x] Unplanned treasury threshold: dual condition — ≥⅔ weight AND majority of governors (2026-11-12)
- [x] Transfers: ROFR both classes; only vested shares transferable (2026-11-12)
- [x] Deadlock: cooling 30d → mediation 30d → shotgun buy-sell (2026-11-12)
- [x] Investor voting: no standing cap, case-by-case per round (2026-11-12)
- [x] Compensation: contracts only — no voted-payment mechanism (2026-11-12)
- [x] Emergency freeze: 2-of-3 governors (2026-11-12)
