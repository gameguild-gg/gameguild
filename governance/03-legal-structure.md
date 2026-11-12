# 03 — Legal Structure & Jurisdiction

> Status: RESEARCH COMPLETE (2026-11-02). Recommendation: Delaware DevCo-led structure; offshore layer deferred.

## Simplified structure

```
Delaware DevCo (C-Corp)   ← everything: ops, equity, investor SAFEs, treasury, USDC payouts
Cayman Foundation         ← DEFERRED until: real treasury scale / international structure needed
BR service entity         ← only if BR-resident holders bill for services
```

A single Delaware C-Corp covers the whole plan: equity-weighted governance by contract, stablecoin payouts to holders, SAFE-based investor rounds. Offshore layer only adds value later (treasury custody, tax-optimized distributions at scale) — revisit at seed/Series A.

## Constraints (fixed)

- Founders resident in: Brazil + US
- Operations initially: Brazil and US markets
- Requirement: crypto-friendly wrapper, founder-favorable tax, low maintenance, can hold treasury + sign contracts

## Candidate structures

| Structure | Setup (Otonomos) | Annual | Entity tax | Recognition | Verdict |
|---|---|---|---|---|---|
| Wyoming DAO LLC | $485 yr-1 | ~$316 | US pass-through (US exposure) | Statutory but criticized | ❌ primary wrapper — US court/tax exposure, auto-dissolution after 1y inaction, must publish contract addresses |
| Wyoming DUNA | – | – | unincorporated nonprofit | New (2024), Uniswap DAO pattern | ❌ niche, US-centric |
| Marshall Islands DAO LLC | $2,990 | ~$1,000–1,500 | 0% | Direct DAO legal personhood; thin case law | ⚠️ budget option, weak institutional recognition |
| **Cayman Foundation Company** | $7,357 (starter ~$14.5k) | ~$7,123 | 0% (no income/capital/withholding) | **Industry default** (Maker EGF, dYdX, Aavegotchi, API3, 1inch, KlimaDAO) | ✅ **RECOMMENDED** |
| Swiss Stiftung | ~CHF 50k endowment | high (supervision, accounts, Swiss board) | ~11.9% unless exempt | Ethereum/Cardano pedigree | ❌ overkill + expensive + not auto-exempt |
| Swiss Verein (daoSuisse-style) | cheaper, member-based | medium | varies | emerging | ⚠️ watch, not productized by Otonomos |
| BVI BC | $2,616 | ~$2,310+ | 0% | Good for token SPVs | ✅ budget alternative (~90% of Cayman mechanics, less banking prestige) |
| Panama Foundation | $1,495–5,405 | ~$2,410 | territorial 0% | weak Web3 track record | ❌ |

## Shareholder residency map (confirmed 2026-11-02)

| Person | Residency | Key exposure |
|---|---|---|
| Tolsta | **US resident** | worldwide income; US filings on all payouts; if ever ≥10% of a *foreign* entity → Form 5471 + 8938 + FBAR (avoided by holding only DevCo equity) |
| Matheus | **Brazil** | Lei 14.754 accrual tax 15% on pro-rata *offshore* entity profits + CBE — mitigated because current structure has no offshore entity; DevCo dividends taxed per BR rules on receipt |
| Joel | **Brazil** | same as Matheus |

**Structural consequences:**

- **Tolsta (US)**: holds only DevCo equity — no foreign-entity stakes, no 5471 burden, no ETBUS footprint (single US entity does everything).
- **Matheus/Joel (BR)**: with a Delaware-only structure there is no Lei 14.754 *offshore accrual* problem (Delaware C-Corp is a foreign entity from BR's view — 14.754 DOES apply to profits of controlled foreign entities accrual-basis, Delaware included). ⚠️ This needs BR counsel modeling: each BR-resident governor is taxed annually on their pro-rata share of the DevCo's profits **whether or not distributed**, plus CBE. Mitigations to model with counsel:
  1. BR service entity per BR-resident governor billing the DevCo for work (services income, deductible at DevCo level) — compensation-heavy, distribution-light personal economics
  2. Accept the 15% accrual + R$22k exemption and budget annual BR filings
  3. A future offshore layer changes this math — re-evaluate at seed

## Recommendation

**Delaware C-Corp only, now.** The single DevCo carries ops, equity, treasury, payouts. The comparison table below is the research that supports the deferred offshore option — relevant when treasury scale or international structure justifies ~$7–15k/yr:

```
IF offshore layer added later (seed/Series A):
Cayman Foundation (0% tax, memberless)
  ├─ holds treasury (deferred decision)
  ├─ council majority NON-US residents
US DevCo (Delaware)   ← ops, equity, investors
BR service entities   ← Matheus/Joel bill for services
```

## Otonomous assessment

- Legitimate: since 2015, 20 jurisdictions, ~1,166 live entities, crypto checkout, SumSub KYC; real clients (Powerloom, ApeBond, API3 logos).
- ✅ Use them for: formation + registered agent + renewals in one dashboard.
- ❌ Don't rely on them for: legal/tax advice (they refer out to Andersen, Darien, CipherLaw etc.), founder-level tax planning, securities analysis of our profit-share token.
- Pricing 2026: Cayman Web3 Foundation $7,357 setup / ~$7,123/yr; BVI $2,616 setup / ~$2,310/yr.

## Brazil — the hard constraint (Lei 14.754/2023)

⚠️ **Offshore deferral is dead for Brazilian residents.** Since FY2024:

- Profits of controlled foreign entities (including a US Delaware C-Corp) taxed **annually on accrual** in the BR-resident governor's PIT return — **whether or not distributed**. Applies to Matheus and Joel.
- Rate: 15% flat; first ~R$ 22.2k/yr of foreign profits exempt.
- Anti-avoidance: profits routed into a Brazilian company to claim dividend exemption are recharacterized as taxed income.
- Crypto payouts: taxed at FMV on receipt; later disposal = capital gains 15–22.5% (IN RFB 1888/2019).
- **CBE**: annual Central Bank declaration of assets abroad (penalties up to R$250k). Individuals → Dec 31.

**Design consequence**: BR-resident governors owe BR tax on the DevCo's annual profits pro-rata even in years with zero distribution. Model the mitigations in the residency map above with BR counsel (Machado Meyer / Mattos Filho / KPMG BR) before incorporation.

## US

- US founder taxed on worldwide income regardless of wrapper — same trap, both directions.
- RMI LLC / BVI can check-the-box (Form 8832): single-owner → disregarded, multi-member → partnership (K-1s).
- ETBUS triggers to keep OUT of the foundation: US-based managers doing core revenue work, regular/continuous US services, US office. → Cayman council majority non-US; US founder works via DevCo.
- Governance-token profit rights = securities risk category (Howey) — irrelevant to current structure; only if a token layer is ever added.
- Crypto dividends to US founder = ordinary income at FMV on receipt.
- Compliance: Form 5471 / 8938 / FBAR — $10k+ penalties for missing. Non-negotiable budget line.

## Sources

- Wyoming: SF 38 (2021), SF 68 (2022), SoS DAO FAQ, MetaLeX "Wyoming's Legal DAO-saster" critique
- Cayman: Foundation Companies Act 2017; Cayman Finance Web3 registrations report
- Brazil: Lei 14.754/2023 (Planalto) — verify article numbers with BR counsel
- Otonomos: otonomos.com order pages + helpdesk guides (2026)
- Pattern references: Ethereum (Swiss), Uniswap (DE DevCo + DUNA 2024), dYdX, Maker (Cayman), 1inch/API3/Powerloom (Cayman via Otonomos)
