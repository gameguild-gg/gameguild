# Week 07 Readings - Advanced Chess Techniques

---

## Required Readings

| #   | Reading                                                                                                                                                                 | Time   | Covers                                                                                                      |
| --- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------ | ----------------------------------------------------------------------------------------------------------- |
| 1   | Ian Millington, **AI for Games (3rd Ed.)**, Chapter 8.5 (Search Enhancements) - ISBN 9781138483972                                                                      | 25 min | Pruning heuristics, move ordering strategies, transposition tables, search extensions in board games        |
| 2   | Chess Programming Wiki, [Zobrist Hashing](https://www.chessprogramming.org/Zobrist_Hashing)                                                                             | 15 min | Random number–based position hashing, incremental XOR updates, collision probability                        |
| 3   | Chess Programming Wiki, [Transposition Table](https://www.chessprogramming.org/Transposition_Table)                                                                     | 20 min | Storing and retrieving cached positions, replacement schemes, depth-preferred vs always-replace             |
| 4   | Chess Programming Wiki, [Null Move Pruning](https://www.chessprogramming.org/Null_Move_Pruning)                                                                         | 15 min | Skipping a turn to prove a position is too good to explore, reduction depth R, zugzwang pitfall             |
| 5   | Chess Programming Wiki, [Late Move Reductions](https://www.chessprogramming.org/Late_Move_Reductions)                                                                   | 10 min | Reducing search depth for moves ordered late, conditions that trigger a full-depth re-search                |
| 6   | Chess Programming Wiki, [Killer Heuristic](https://www.chessprogramming.org/Killer_Heuristic) + [History Heuristic](https://www.chessprogramming.org/History_Heuristic) | 15 min | Two complementary move-ordering techniques: position-independent killer slots and cumulative history scores |
| 7   | Chess Programming Wiki, [Opening Book](https://www.chessprogramming.org/Opening_Book)                                                                                   | 10 min | Polyglot format, book moves vs engine search, when to leave the book                                        |
| 8   | Chess Programming Wiki, [Endgame](https://www.chessprogramming.org/Endgame)                                                                                             | 10 min | Phase detection, endgame-specific evaluation adjustments, insufficient material, tablebases overview        |

**Focus while reading:**

- Millington: How pruning heuristics interact with each other; why move ordering is the single most important factor in alpha-beta efficiency; how transposition tables turn a tree search into a graph search
- Zobrist Hashing: The key insight is _incremental_ hashing — you XOR in/out only the moved pieces instead of rehashing the entire board, making it O(1) per move; understand why random 64-bit numbers make collisions extremely unlikely
- Transposition Table: How the hash key indexes into a fixed-size table; the difference between `EXACT`, `ALPHA`, and `BETA` bound types; why replacement policy matters (depth-preferred keeps deeper results, always-replace keeps fresher results)
- Null Move Pruning: The intuition — "if I skip my turn and still have a beta cutoff, my real position must be at least as good"; understand the zugzwang exception (positions where passing is worse than any move) and why null-move pruning is disabled in endgames with few pieces
- Late Move Reductions: Moves searched later in the move list are statistically less likely to be good, so searching them at reduced depth saves time; if a reduced search surprises us, we re-search at full depth — this makes LMR safe
- Killer + History Heuristics: Killer moves are "quiet moves that caused a beta cutoff at the same depth" — they are tried early because sibling positions often share refutations; the history heuristic generalizes this across the entire search by accumulating depth² bonuses for moves that cause cutoffs
- Opening Book: Using a book avoids spending compute on well-known theory; understand the trade-off between book depth (more memorized moves) and book width (more variations) and when the engine should "leave book" and think for itself
- Endgame: Why evaluation functions need to change in the endgame — king centralization becomes important, passed pawns gain value, and piece-square tables should shift; how tablebases provide perfect play for positions with ≤7 pieces

---

## Videos

### Search Optimizations

| #   | Video                                                                                                    | Time   | Covers                                                                                 |
| --- | -------------------------------------------------------------------------------------------------------- | ------ | -------------------------------------------------------------------------------------- |
| A   | Chesscoder - [Alpha Beta Pruning Enhancements](https://www.youtube.com/watch?v=DRutSmc3Oqo)              | 18 min | Null-move pruning, late move reductions, killer moves explained with code              |
| B   | Logic Crazy Chess - [How Transposition Tables Work](https://www.youtube.com/watch?v=WR_a7kKIRlc)         | 12 min | Visual explanation of Zobrist hashing and transposition table lookups in chess engines |
| C   | Code Monkey King - [Zobrist Hashing & Transposition Tables](https://www.youtube.com/watch?v=AXShPMCnRwE) | 20 min | Step-by-step implementation of Zobrist key generation and transposition table probing  |

### Opening Books & Endgames

| #   | Video                                                                                                | Time   | Covers                                                                                            |
| --- | ---------------------------------------------------------------------------------------------------- | ------ | ------------------------------------------------------------------------------------------------- |
| D   | GothamChess - [Opening Principles Everyone Should Know](https://www.youtube.com/watch?v=7VdfDJ9WIAM) | 15 min | Why openings matter, core principles that opening books encode, transitioning to the middlegame   |
| E   | ChessNetwork - [Understanding Endgame Tablebases](https://www.youtube.com/watch?v=5sYj8MkFEn4)       | 14 min | What tablebases are, how they achieve perfect endgame play, practical examples with Syzygy tables |

---

## Interactive Resources

| #   | Resource                                                 | Time   | Covers                                                                                   |
| --- | -------------------------------------------------------- | ------ | ---------------------------------------------------------------------------------------- |
| 1   | [Syzygy Endgame Tablebases](https://syzygy-tables.info/) | 10 min | Look up any ≤7-piece endgame position and see the theoretically perfect result and moves |
| 2   | [Lichess Analysis Board](https://lichess.org/analysis)   | 15 min | Test opening book lines, compare your engine evaluation vs Stockfish, explore endgames   |

**Hands-on task:** Using the resources above:

- **Zobrist hashing exercise:** Take the starting position. Write down which Zobrist keys you would XOR together (one per piece-square, plus side-to-move, castling rights, and en passant file). Now play 1.e2e4 — verify that the new hash can be computed incrementally: XOR out `white_pawn_on_e2`, XOR in `white_pawn_on_e4`, XOR the side-to-move key, XOR the en passant file key for e3. That's 4 XOR operations instead of rehashing 32+ pieces.
- **Null-move pruning intuition:** On Lichess, set up a position where White has a large material advantage (e.g., queen + rook vs rook). Think about what happens if White "passes" (does nothing): Black still can't improve the position enough. This is why null-move pruning works — if the null move still produces a cutoff, the position is clearly winning.
- **Endgame tablebases:** On Syzygy Tables, look up KQK (King + Queen vs King). Note that every position is either a forced win in N moves or a draw (stalemate). Then try KBNK (King + Bishop + Knight vs King) — notice the maximum distance-to-mate is 33 moves, which is why this is notoriously difficult for engines without tablebase access.

---

## Optional Deep Dive

| Resource                                                                                                          | Time   | Focus                                                                                      |
| ----------------------------------------------------------------------------------------------------------------- | ------ | ------------------------------------------------------------------------------------------ |
| Chess Programming Wiki, [Razoring](https://www.chessprogramming.org/Razoring)                                     | 10 min | Pre-frontier pruning: skip full search if static evaluation is far below alpha             |
| Chess Programming Wiki, [Futility Pruning](https://www.chessprogramming.org/Futility_Pruning)                     | 10 min | Skip quiet moves near leaf nodes when static eval + margin can't reach alpha               |
| Chess Programming Wiki, [Principal Variation Search](https://www.chessprogramming.org/Principal_Variation_Search) | 15 min | Search the first move with a full window, then use a zero-width window for the rest        |
| Chess Programming Wiki, [Syzygy Bases](https://www.chessprogramming.org/Syzygy_Bases)                             | 15 min | WDL and DTZ tables, probing during search, root vs non-root usage                          |
| Chess Programming Wiki, [NNUE](https://www.chessprogramming.org/NNUE)                                             | 20 min | Efficiently updatable neural network evaluation, Stockfish's breakthrough, halfkp features |
| Chess Programming Wiki, [Texel's Tuning Method](https://www.chessprogramming.org/Texel%27s_Tuning_Method)         | 15 min | Automated evaluation parameter tuning via logistic regression on game outcomes             |
| Chess Programming Wiki, [Extensions](https://www.chessprogramming.org/Extensions)                                 | 10 min | Check extensions, singular extensions, recapture extensions — when to search deeper        |

---

## Code Study (Optional)

| Repository                                        | Language | Focus for This Week                                                                          |
| ------------------------------------------------- | -------- | -------------------------------------------------------------------------------------------- |
| [Ethereal](https://github.com/AndyGrant/Ethereal) | C        | Clean implementation of null-move pruning, LMR, and killer/history move ordering             |
| [Demolito](https://github.com/lucasart/Demolito)  | C        | Compact engine with clear Zobrist hashing, transposition table, and opening book integration |
| [Weiss](https://github.com/TerjeKir/weiss)        | C        | Didactic engine with well-commented search enhancements: null-move, LMR, history tables      |

---

## Key Concepts Summary

| Concept                    | Core Idea                                                                                                                 |
| -------------------------- | ------------------------------------------------------------------------------------------------------------------------- |
| **Zobrist Hashing**        | Assign random 64-bit numbers to each piece-square combination; XOR them together to hash a position; update incrementally |
| **Transposition Table**    | Cache evaluated positions keyed by Zobrist hash; avoid re-searching the same position reached via different move orders   |
| **Null-Move Pruning**      | "If I skip my turn and still get a beta cutoff, the position is so good I don't need to search further"                   |
| **Late Move Reductions**   | Search moves ordered late at reduced depth; if they surprise us with a good score, re-search at full depth                |
| **Killer Heuristic**       | Store quiet moves that caused beta cutoffs at each depth; try them early in sibling nodes                                 |
| **History Heuristic**      | Accumulate depth² bonuses for quiet moves that cause cutoffs across the entire search; use for global move ordering       |
| **Opening Book**           | Pre-computed database of strong opening moves; saves compute and avoids known theoretical traps                           |
| **Endgame Considerations** | Adjust evaluation for the endgame phase: centralize the king, promote passed pawns, use tablebases for ≤7-piece endings   |

---

**Study order:** Zobrist Hashing → Transposition Table → Millington Ch. 8.5 → Killer + History Heuristics → Null-Move Pruning → Late Move Reductions → Opening Book → Endgame

**Total required time:** ~2h (readings: 2h, videos: recommended ~1h 25min on top)
