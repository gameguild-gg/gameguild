# 05 — Governance (Decision Structure, no tokens)

> Rewritten 2026-11-02: DAO token mechanics dropped. Same decision architecture, enforced by operating/shareholder agreement + off-chain voting.

## Scope of votes (agreed)

- **Product decisions**: roadmap priorities, feature bets, major pivots
- **Treasury decisions**: spending above threshold, allocation changes

NOT in scope (handled by legal agreements / supermajority): firing co-founders, changing cap table, dissolving the company.

## Voting model

- **Equity-weighted votes** (40/30/30) — the off-chain equivalent of token-weighted. With 40/30/30 no two founders can outvote the third: built-in veto triangle, same as the DAO design intended.
- Proposal threshold: any founder may submit; formal votes require all three to cast.
- Quorum: 100% of founders for treasury decisions (3 people — anything less is a side conversation, not a vote).
- **3-year mandate votes**: fixed cycle, auto-triggers: "how do we distribute accumulated earnings?" (see 07). Mandatory, cannot be skipped.

## Decision classes & thresholds

| Class | Examples | Threshold |
|---|---|---|
| Routine ops | infra spend below monthly budget, hiring within plan | CEO (Tolsta) discretion |
| Product | roadmap changes, major feature bets | simple majority (equity-weighted) |
| Treasury > threshold X | spend above X, allocation shifts | supermajority (≥70% equity — i.e., Tolsta alone insufficient) |
| Structural | cap table changes, dissolution, founder exit terms | unanimous |
| 3-year mandate | profit distribution model | simple majority, mandatory every 3 years |

Threshold X and exact classes get written into the operating agreement.

## Voting tool: Loomio

- Thread per proposal, 7-day close, agree/abstain/block; decisions auto-archived in Loomio
- Equity weights (40/30/30) stated in each proposal header; tally computed manually
- **Canonical ledger stays `appendix-decisions-log.md`** — every closed Loomio proposal outcome is copied there. Tool is swappable; the repo is the source of truth
- 3-year mandate votes: run in Loomio AND collect signed written consent from all shareholders (corporate act — lawyers want the paper)
- Operating agreement references the process, not the tool ("votes conducted via the designated voting platform with 7-day windows, outcomes recorded in the decisions log constitute binding consent") — keeps tool choice swappable without legal rework

Informal temperature checks (chat) allowed before formal proposals; only Loomio outcomes count.

## Treasury execution & checks

- Bank + crypto treasury: dual signature where possible (2-of-3 on crypto wallet multisig — a Safe works fine WITHOUT governance tokens; it's just a wallet).
- Emergency: 2-of-3 founders can freeze non-routine treasury actions pending formal vote.

## If DAO ever returns

This structure maps 1:1: equity weights → token weights, decisions log → Snapshot, operating agreement → Governor+Timelock. Research preserved in git history + `04` deprecated sections.
