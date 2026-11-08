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
4. **Compensation votes**: contractors/employees paid per contract; any governor who worked without a contract gets a **voted performance-based payment** — limited mechanism, discretionary, not a salary entitlement
5. Outcomes recorded in `appendix-decisions-log.md`

## Voting model

- **Equity-weighted votes** (40/30/30). With 40/30/30 no two governors can outvote the third: built-in veto triangle.
- **Deadlock breaker (agreed)**: on **product decisions only**, Tolsta holds the tie-breaking vote. Treasury/structural items have no tie-breaker — they escalate per the deadlock clause.
- Proposal threshold: any governor may submit; formal votes require all three to cast.
- Quorum: 100% of governors for treasury decisions.

## Decision classes & thresholds

| Class | Examples | Threshold |
|---|---|---|
| Routine ops | spend within the approved quarterly plan | CEO (Tolsta) discretion |
| Product | roadmap changes, feature bets | simple majority; Tolsta tie-breaks |
| Unplanned treasury spend | **anything not in the approved quarterly plan** | supermajority (≥70% — Tolsta alone insufficient) |
| Structural | cap table changes, dissolution, governor exit terms | unanimous |
| Quarterly cycle votes | distribution %, plan approval, compensation | simple majority |

The quarterly plan is therefore the master control: planned spend flows freely; anything unplanned needs a supermajority vote.

## Compensation model (agreed)

- Ownership and work are **fully separated**. Governors do not earn by owning; workers are paid for working.
- Anyone (governor or not) doing meaningful work for the company does so **under a contract** with agreed pay.
- A governor who worked without a contract: at cycle end, performance is measured and compensation is **voted** by governors. Limited mechanism — discretionary bonus, not an entitlement, and it should stay rare (the contract route is the norm).

## Voting tool: Loomio

- Thread per proposal, 7-day close (cycle votes at the quarterly meeting), agree/abstain/block; decisions auto-archived
- Equity weights (40/30/30) stated in each proposal header; tally computed manually
- **Canonical ledger stays `appendix-decisions-log.md`** — every closed Loomio proposal outcome is copied there
- Operating agreement references the process, not the tool — tool swappable without legal rework

Informal temperature checks (chat) allowed before formal proposals; only Loomio outcomes count.

## Treasury execution & checks

- Bank + crypto treasury: dual signature (2-of-3 governors on the crypto wallet multisig)
- Emergency: 2-of-3 governors can freeze non-routine treasury actions pending formal vote
