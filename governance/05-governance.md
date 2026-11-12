# 05 — Governance (Decision Structure)

## Terminology

**Governors** = governance shareholders. Initial: Tolsta, Matheus, Joel. Class extends to future members.

## Scope of votes

- **Product decisions**: roadmap priorities, feature bets, major pivots
- **Treasury decisions**: spending outside the quarterly plan, allocation changes
- **Quarterly cycle decisions**: distribution amounts, contractor compensation, unplanned spend (see Quarterly cycle)

NOT in scope (handled by legal agreements / supermajority): changing cap table, dissolving the company, governor expulsion terms.

## Quarterly cycle (every 3 months)

Each quarter closes with a governors' meeting (Loomio + call):

1. **Review**: performance vs. the quarter's plan, financials
2. **Plan next quarter**: budget, roadmap — approved by vote; this becomes the *planned spend baseline*
3. **Distribution vote**: how much of the quarter's distributable profit goes to revenue shareholders (waterfall in 07 applies: opex → founder loans → distributions)
4. **Compensation review**: contracted pay confirmed per contract; contracts for new/changed work approved by vote
5. Outcomes recorded in `appendix-decisions-log.md`

## Voting model

- **Equity-weighted votes** (40/30/30).
- **No simple majority anywhere money or structure is at stake.** All passing thresholds are qualified majorities set above 60%, because 30+30=60 must never override the 40% holder, and 40 alone must never force anything.
- Deadlock is the *intended* outcome when agreement fails on structural items — it forces negotiation, not domination.
- **Deadlock breaker**: on **product decisions only**, Tolsta holds the tie-breaking vote. Treasury and structural items have no tie-breaker — they follow the deadlock clause below.
- Proposal threshold: any governor may submit; formal votes require all three to cast.
- Quorum: 100% of governors for treasury decisions.

## Decision classes & thresholds

| Class | Examples | Threshold |
|---|---|---|
| Routine ops | spend within the approved quarterly plan | CEO (Tolsta) discretion |
| Product | roadmap changes, feature bets | qualified majority **≥66⅔%** (any 2 governors = 60% < 66⅔% → fails without consensus or Tolsta's tie-break) |
| Unplanned treasury spend | **anything not in the approved quarterly plan** | **dual condition: ≥2/3 (66⅔%) of voting weight AND a majority of governors (≥2 of 3) voting in favor** — see note |
| Structural | cap table changes, dissolution, governor exit terms | **unanimous (100%)** |
| Quarterly cycle votes | distribution %, plan approval, compensation | qualified majority **≥66⅔%** |

**Threshold note (unplanned treasury, agreed 2026-11-12)**: "2/3 + majority of governors." At exactly 40/30/30 this means 40+30 = 70% ≥ 66⅔% with 2 governors in favor → **passes over the third's objection**. That is the agreed rule: treasury spend can move with a strong coalition (Tolsta+1) but never with 60% (the two 30s alone, weight-failed) and never with one governor alone. The dual condition stays correct as the cap table changes: a future 51% holder alone still fails the governor-majority condition; three small holders totaling 66% pass only if ≥2 of them… i.e., the formula, not today's arithmetic, is the law. Counsel to draft precisely.

The quarterly plan is therefore the master control: planned spend flows freely; anything unplanned needs the dual-condition qualified majority.

## Coalition table (why these numbers)

| Coalition | Weight | Governors | Product (≥66⅔%)? | Treasury (⅔ + gov majority)? | Structural (100%)? |
|---|---|---|---|---|---|
| Tolsta + Matheus | 70% | 2 | ✅ | ✅ (70% ≥ ⅔ and 2 ≥ 2) | ❌ |
| Tolsta + Joel | 70% | 2 | ✅ | ✅ | ❌ |
| Matheus + Joel | 60% | 2 | ❌ | ❌ (weight fails) | ❌ |
| Any single governor | ≤40% | 1 | ❌ | ❌ | ❌ |
| All three | 100% | 3 | ✅ | ✅ | ✅ |

The two 30% governors together (60%) can never pass anything. Tolsta alone can never pass anything. Only Tolsta + at least one other can move treasury — and structural change requires everyone.

## Deadlock clause (agreed)

For treasury/structural items where the dual condition or unanimity fails:

1. **Cooling period**: 30 days from the failed vote; the item may not be re-proposed.
2. **Mediation**: mandatory mediation session (all governors + a neutral mediator).
3. **Shotgun buy-sell**: if mediation fails within 30 more days, any governor may trigger — offering to buy the others' shares at a stated per-share price; each recipient either sells at that price or buys the offeror's shares at the same price. Counsel to draft terms (valuation mechanics, funding period, tag of both share classes).

## Compensation model (agreed)

- Ownership and work are **fully separated**. Governors do not earn by owning; workers are paid for working.
- **All work for the company is done under contract** with agreed pay — governor or not. There is no voted-payment mechanism for uncontracted work: if work is worth paying, it is worth a contract first.

## Voting tool: Loomio

- Thread per proposal, 7-day close (cycle votes at the quarterly meeting), agree/abstain/block; decisions auto-archived
- Equity weights (40/30/30) stated in each proposal header; tally computed manually
- **Canonical ledger stays `appendix-decisions-log.md`** — every closed Loomio proposal outcome is copied there
- Operating agreement references the process, not the tool — tool swappable without legal rework

Informal temperature checks (chat) allowed before formal proposals; only Loomio outcomes count.

## Treasury execution & checks

- Bank + crypto treasury: dual signature (2-of-3 governors on the crypto wallet multisig)
- Emergency: 2-of-3 governors can freeze non-routine treasury actions pending formal vote
