# Midterm — Chess Engine Competition

## Overview

Your midterm is a **chess engine competition**. You will build a chess bot that goes beyond the baseline algorithms (plain Minimax or plain MCTS) by implementing **advanced techniques** covered in the lectures. We will run a class-wide tournament for fun to see which bot is the smartest!

- **Competition rules:** [github.com/gameguild-gg/chess-competition](https://github.com/gameguild-gg/chess-competition)
- **Live leaderboard:** [gameguild-gg.github.io/chess-competition](https://gameguild-gg.github.io/chess-competition/)

---

## Deliverables

### 1. Chess Bot Submission

Submit your bot following the competition repository rules. Your engine must implement `ChessSimulator::Move(std::string fen, int timeLimitMs)` and return a valid UCI move string within the time budget.

### 2. Video Presentation

Record a video (5–10 minutes) with the following structure:

1. **Text report intro (first frames):** The very first frames of your video must display a brief **plain-text report** (`.txt` file shown on screen) summarizing:
   - Which search algorithm you chose (Minimax or MCTS)
   - Which advanced techniques you implemented (bullet list)
   - Any additional features or experiments you tried
2. **Code walkthrough:** After the text report, walk through your code and explain:
   - Your overall engine architecture
   - How each advanced technique is implemented
   - How your engine manages time
   - Any design decisions, trade-offs, or bugs you encountered

---

## Requirements

You must choose **either Minimax or MCTS** as your base search algorithm, and then build **advanced improvements** on top of it. A plain implementation of Minimax with alpha-beta or a plain MCTS loop is **not sufficient** — you must go further.

### If you choose Minimax

**Required:**

- **Iterative Deepening** — your engine must be time-aware. Use iterative deepening so the search always has a best move ready and can stop when the time budget runs out. Never exceed the time limit.
- **At least one additional advanced technique** from the list below (beyond basic alpha-beta pruning and a simple heuristic evaluation):

| Technique                      | Description                                                                                                  |
| ------------------------------ | ------------------------------------------------------------------------------------------------------------ |
| **Quiescence Search**          | Extend search at leaf nodes with capture-only moves to eliminate the horizon effect                          |
| **Aspiration Windows**         | Narrow the alpha-beta window around the previous depth's score for faster pruning                            |
| **Move Ordering (MVV-LVA)**    | Order captures by Most Valuable Victim – Least Valuable Attacker for better cutoffs                          |
| **Transposition Tables**       | Cache evaluated positions using Zobrist hashing to avoid redundant work                                      |
| **Null-Move Pruning**          | Skip your turn and search at reduced depth — if you still get a cutoff, prune the branch                     |
| **Late Move Reductions (LMR)** | Reduce search depth for moves searched late in the move list (they are statistically less likely to be best) |
| **Killer Heuristic**           | Remember quiet moves that caused beta cutoffs at the same depth and try them early                           |
| **History Heuristic**          | Track which moves cause cutoffs globally and use that to improve move ordering                               |
| **Opening Book**               | Use a small table of known opening moves to save time in the first few moves                                 |
| **Piece-Square Tables**        | Position-dependent piece values that reward good piece placement (e.g., knights in the center)               |
| **Advanced Evaluation**        | Go beyond material count: mobility, king safety, pawn structure, passed pawns, rook on open files            |
| **Delta Pruning**              | In quiescence search, skip captures that cannot possibly raise alpha even with a safety margin               |

### If you choose MCTS

**Required:**

- **Time-aware search loop** — your MCTS must run iterations within the time budget and stop before the deadline. Never exceed the time limit.
- **At least one additional advanced technique** to make your search smarter than blind random rollouts:

| Technique                          | Description                                                                                                                 |
| ---------------------------------- | --------------------------------------------------------------------------------------------------------------------------- |
| **Smarter Rollout Policy**         | Instead of uniform random moves, bias rollouts toward captures, checks, or heuristic-guided moves (light or heavy playouts) |
| **Hybrid MCTS + Evaluation**       | Replace random rollouts entirely with a static evaluation function (similar to AlphaZero's value network approach)          |
| **Prioritized UCB1 / Priors**      | Add a prior bias term to UCB1 based on chess heuristics (captures, checks, central moves) to guide early exploration        |
| **PUCT Selection**                 | Use the Predictor + Upper Confidence Trees formula with move priors instead of plain UCB1                                   |
| **Tree Reuse**                     | After a move is played, promote the relevant subtree and discard siblings instead of building a new tree from scratch       |
| **Rollout Depth Limiting**         | Cap rollout length and use a material evaluation at the cutoff instead of playing to checkmate                              |
| **Exploration Constant Tuning**    | Experiment with the $C$ parameter in UCB1 — the default $\sqrt{2}$ may not be optimal for chess                             |
| **Parallelization (Virtual Loss)** | Run multiple MCTS iterations in parallel using virtual loss to prevent threads from duplicating work                        |

### Additional suggestions (either approach)

These are optional but can make your bot significantly stronger:

- Combine both approaches: use Minimax for tactical positions and MCTS when time management is tight
- Implement endgame-specific logic (e.g., king centralization, passed pawn promotion)
- Add check extensions (search deeper when the king is in check)
- Implement Principal Variation Search (PVS) / NegaScout for tighter windows on non-PV nodes
- Log and analyze your engine's search statistics (nodes/second, average depth, time usage) to find bottlenecks

---

## Competition Rules Summary

- Implement only inside the `chess-bot` folder (C++)
- Do **not** create subfolders inside `chess-bot`
- No external libraries beyond those already provided
- Per-move time **must** stay under the limit passed to your engine
- AI-assisted tools are allowed but must be disclosed (20% score penalty)
- Username must not reveal your real name (FERPA). Or you should follow the FERPA waiver instructions at https://gameguild.gg/ferpa-waiver

Full rules: [github.com/gameguild-gg/chess-competition](https://github.com/gameguild-gg/chess-competition)

---

## Grading

| Component                                                             | Weight    |
| --------------------------------------------------------------------- | --------- |
| **Advanced techniques** — correct implementation of required features | 50%       |
| **Video presentation** — clear text report + code explanation         | 30%       |
| **Code quality** — clean, readable, well-structured code              | 20%       |
| **Tournament performance** — top finishers in the class competition   | **Bonus** |

Tournament ranking does **not** count toward the base 100 points. Top finishers earn bonus points on top of their grade.

---

## Tips

- **Time management is non-negotiable.** More engines lose by timing out than by playing bad moves. Use 85% of the budget and leave margin for OS jitter.
- **Start simple, then optimize.** Get a working engine first (even if it only plays random legal moves), then layer in advanced features one at a time.
- **Test against the competition site.** Use the [live competition tool](https://gameguild-gg.github.io/chess-competition/) to run practice matches and check your engine's behavior under time pressure.
- **Debug with FEN strings.** When your engine makes a bad move, save the FEN and reproduce the issue in isolation.
