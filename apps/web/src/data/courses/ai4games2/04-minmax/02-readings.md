# Week 04 Readings - MinMax & Alpha-Beta Pruning

---

## Required Readings

| #   | Reading                                                                                        | Time   | Covers                                                                             |
| --- | ---------------------------------------------------------------------------------------------- | ------ | ---------------------------------------------------------------------------------- |
| 1   | Ian Millington, **AI for Games (3rd Ed.)**, Chapter 8.1-8.3 (Board Games) - ISBN 9781138483972 | 40 min | Game trees, minimax theory, static evaluation, terminal states, alpha-beta pruning |
| 2   | Chess Programming Wiki, [Alpha-Beta](https://www.chessprogramming.org/Alpha-Beta)              | 20 min | Zero-sum games, minimax recursion, alpha-beta pruning theory and implementation    |
| 3   | Chess Programming Wiki, [Minimax](https://www.chessprogramming.org/Minimax)                    | 15 min | Minimax implementation, negamax framework, practical patterns                      |

**Focus while reading:**

- Millington: Two-player zero-sum game theory, minimax recursion, why alpha-beta works, move ordering strategies
- Chess Programming Wiki (Alpha-Beta): History, implementation variants (fail-soft vs fail-hard), enhancements
- Chess Programming Wiki (Minimax): Negamax simplification, recursive structure, integration with alpha-beta

---

## Videos

| #   | Video                                                                                                                 | Time   | Covers                                                            |
| --- | --------------------------------------------------------------------------------------------------------------------- | ------ | ----------------------------------------------------------------- |
| A   | Sebastian Lague - [Algorithms Explained: minimax and alpha-beta pruning](https://www.youtube.com/watch?v=l-hh51ncgDI) | 10 min | Visual explanation of minimax tree traversal and pruning          |
| B   | Sebastian Lague - [Coding Adventure: Chess](https://www.youtube.com/watch?v=U4ogK0MIzqk)                              | 30 min | Full chess AI implementation: move generation, evaluation, search |

---

## Interactive Resources

| #   | Resource                                                                                 | Time   | Covers                                                           |
| --- | ---------------------------------------------------------------------------------------- | ------ | ---------------------------------------------------------------- |
| 1   | [Minimax Algorithm Visualizer](https://raphsilva.github.io/utilities/minimax_simulator/) | 10 min | Step through minimax on custom game trees                        |
| 2   | [Alpha-Beta Pruning Demo](http://homepage.ufp.pt/jtorres/ensino/ia/alfabeta.html) (UFP)  | 10 min | Interactive alpha-beta visualization with step-by-step execution |

**Hands-on task:** In the visualizers:

- Create a game tree with depth 4 and observe minimax propagation
- Enable alpha-beta and count how many nodes are pruned
- Try different move orderings and observe pruning efficiency changes

---

## Optional Deep Dive

| Resource                                                                                                                                                         | Time   | Focus                                                                     |
| ---------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------ | ------------------------------------------------------------------------- |
| Bruce Moreland, [Alpha-Beta with Move Ordering](https://web.archive.org/web/20071031095933/http://www.brucemo.com/compchess/programming/alphabeta.htm) (Archive) | 20 min | Killer moves, history heuristic, principal variation search               |
| Chess Programming Wiki - [Transposition Table](https://www.chessprogramming.org/Transposition_Table)                                                             | 25 min | Zobrist hashing, replacement schemes, hash collisions                     |
| Chess Programming Wiki - [Evaluation](https://www.chessprogramming.org/Evaluation)                                                                               | 30 min | Material balance, piece-square tables, mobility, king safety              |
| Chess Programming Wiki - [Move Ordering](https://www.chessprogramming.org/Move_Ordering)                                                                         | 25 min | Killer moves, history heuristic, MVV-LVA, principal variation             |
| Jonathan Schaeffer, [Checkers is Solved](https://www.science.org/doi/10.1126/science.1144079) (Science, 2007)                                                    | 15 min | How perfect play was computed for checkers using alpha-beta + endgame DBs |

---

## Code Study (Optional)

| Repository                                                                 | Language   | Focus                                                       |
| -------------------------------------------------------------------------- | ---------- | ----------------------------------------------------------- |
| [Sunfish](https://github.com/thomasahle/sunfish)                           | Python     | 111-line chess engine - incredibly readable minimax         |
| [Chess.js + Minimax Tutorial](https://github.com/lhartikk/simple-chess-ai) | JavaScript | Simple chess AI with minimax and alpha-beta implementation  |
| [Stockfish](https://github.com/official-stockfish/Stockfish)               | C++        | World-class engine - study evaluation and search (advanced) |

::: note

The Chess Programming Wiki (chessprogramming.org) is an invaluable reference for all board game AI topics. Bookmark it for deep dives into specific techniques.

:::

---

## Key Concepts Summary

| Concept                 | Core Idea                                                                               |
| ----------------------- | --------------------------------------------------------------------------------------- |
| **Zero-sum game**       | One player's gain = other player's loss; utilities sum to zero                          |
| **Minimax**             | Max player maximizes score; Min player minimizes; recurse to leaves                     |
| **Evaluation function** | Heuristic score for non-terminal positions (material, position, mobility)               |
| **Alpha-beta pruning**  | Skip branches that can't affect the final decision (α = best for Max, β = best for Min) |
| **Move ordering**       | Search likely-best moves first to maximize pruning efficiency                           |
| **Killer moves**        | Remember moves that caused cutoffs at same depth                                        |
| **History heuristic**   | Track which moves caused cutoffs across the entire search                               |
| **Transposition table** | Cache evaluated positions to avoid re-computation (uses Zobrist hashing)                |

---

**Study order:** Sebastian Lague minimax video → Millington Ch. 8 → Chess Programming Wiki articles → Interactive visualizers → Sebastian Lague Chess video

**Total required time:** ~2h 15min (readings: 1h 15min, videos: 40min, interactive: 20min)
