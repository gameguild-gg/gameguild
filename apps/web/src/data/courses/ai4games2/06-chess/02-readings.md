# Week 06 Readings - Chess Engine Core

---

## Required Readings

| #   | Reading                                                                                               | Time   | Covers                                                                                      |
| --- | ----------------------------------------------------------------------------------------------------- | ------ | ------------------------------------------------------------------------------------------- |
| 1   | Ian Millington, **AI for Games (3rd Ed.)**, Chapter 8.1–8.3 (Board Games) - ISBN 9781138483972        | 30 min | Game tree search for board games, evaluation functions, iterative deepening, time budgeting |
| 2   | Chess Programming Wiki, [UCI Protocol](https://www.chessprogramming.org/UCI)                          | 15 min | Universal Chess Interface commands, engine-GUI communication, protocol flow                 |
| 3   | Chess Programming Wiki, [Board Representation](https://www.chessprogramming.org/Board_Representation) | 15 min | Mailbox, 0x88, bitboards, trade-offs between simplicity and performance                    |
| 4   | Chess Programming Wiki, [Evaluation](https://www.chessprogramming.org/Evaluation)                     | 20 min | Evaluation function anatomy: material balance, positional factors, combined scoring         |
| 5   | Chess Programming Wiki, [Piece-Square Tables](https://www.chessprogramming.org/Piece-Square_Tables)   | 10 min | Static positional bonuses per piece per square, opening vs endgame tables                   |
| 6   | Chess Programming Wiki, [King Safety](https://www.chessprogramming.org/King_Safety)                   | 10 min | Pawn shield, king tropism, attack units, castling considerations                            |
| 7   | Chess Programming Wiki, [Iterative Deepening](https://www.chessprogramming.org/Iterative_Deepening)   | 10 min | Depth-first search with increasing depth limits, anytime property, move ordering benefits   |
| 8   | Chess Programming Wiki, [Time Management](https://www.chessprogramming.org/Time_Management)           | 10 min | Allocating time per move in tournament play, sudden death vs increment, panic time          |

**Focus while reading:**

- Millington: How evaluation functions drive search quality, the role of iterative deepening as an anytime algorithm, balancing depth vs breadth
- UCI Protocol: The command flow (`uci` → `isready` → `position` → `go` → `bestmove`), how engines communicate with GUIs and tournament managers
- Board Representation: Why bitboards are fast for move generation but mailbox is simpler to implement, you'll use a provided library, but understanding internals helps debugging
- Evaluation: Material is necessary but insufficient, positional terms (mobility, king safety, pawn structure) separate strong engines from weak ones
- Piece-Square Tables: The simplest way to add positional knowledge; understand how to combine opening and endgame tables via tapered evaluation
- King Safety: Why king safety is disproportionately important in evaluation, a single overlooked attack can lose immediately
- Iterative Deepening: Why re-searching from depth 1 each time is not wasteful (exponential growth means deeper levels dominate cost)
- Time Management: How to budget your thinking time across a game, critical for the upcoming tournament

---

## Videos

### Chess Engine Architecture

| #   | Video                                                                                                        | Time   | Covers                                                                                           |
| --- | ------------------------------------------------------------------------------------------------------------ | ------ | ------------------------------------------------------------------------------------------------ |
| A   | Sebastian Lague - [Coding Adventure: Chess](https://www.youtube.com/watch?v=U4ogK0MIzqk)                     | 21 min | Building a chess engine from scratch, board representation, move generation, search, evaluation |
| B   | Sebastian Lague - [Coding Adventure: Making a Better Chess Bot](https://www.youtube.com/watch?v=_vqlIPDR2TU) | 25 min | Improving a chess engine with better evaluation, search optimizations, and iterative deepening   |
| C   | Logic Crazy Chess - [How Chess Engines Work](https://www.youtube.com/watch?v=w4FFX_otR-4)                    | 16 min | High-level overview of chess engine components: evaluation, search, and time control             |
| D   | Barış Kaya - [Writing a Chess Engine in C](https://www.youtube.com/watch?v=bGAfaepBco4)                      | 28 min | Practical walkthrough of building a UCI-compatible chess engine from scratch                     |

### Evaluation & Search Concepts

| #   | Video                                                                                       | Time   | Covers                                                                                     |
| --- | ------------------------------------------------------------------------------------------- | ------ | ------------------------------------------------------------------------------------------ |
| E   | Tom 7 - [30 Weird Chess Algorithms: Elo World](https://www.youtube.com/watch?v=DpXy041BIlA) | 57 min | Creative exploration of chess evaluation functions, from absurd to surprisingly effective |
| F   | GothamChess - [How Do Chess Engines Work?](https://www.youtube.com/watch?v=BXQHP5he-S0)     | 12 min | Accessible explanation of engine evaluation and search for a broad audience                |
| G   | Disservin - [Writing a Chess Engine](https://www.youtube.com/watch?v=eECMVMb9pTs)           | 22 min | Modern approach to chess engine development, UCI implementation, and testing               |

---

## Interactive Resources

| #   | Resource                                                                                                    | Time   | Covers                                                                                       |
| --- | ----------------------------------------------------------------------------------------------------------- | ------ | -------------------------------------------------------------------------------------------- |
| 1   | [Lichess Board Editor](https://lichess.org/editor)                                                          | 10 min | Set up custom positions and export FEN strings, useful for testing your evaluation function |
| 2   | [Chess Programming Wiki - Simplified Eval](https://www.chessprogramming.org/Simplified_Evaluation_Function) | 15 min | Minimal but complete evaluation function with piece-square tables, a great starting point   |

**Hands-on task:** Using the Simplified Evaluation Function page:

- Study the piece values and piece-square tables provided
- Manually evaluate 3 positions from Lichess Board Editor using the material + PST formula
- Compare your manual evaluation to Stockfish's evaluation (visible on Lichess analysis board)
- Notice where simple material + PST evaluation disagrees with Stockfish, these gaps motivate more advanced evaluation terms (mobility, king safety, pawn structure)

---

## Optional Deep Dive

| Resource                                                                                                                                                                       | Time   | Focus                                                                                          |
| ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------ | ---------------------------------------------------------------------------------------------- |
| Chess Programming Wiki, [Mobility](https://www.chessprogramming.org/Mobility)                                                                                                  | 15 min | Counting legal moves as an evaluation term, trapped pieces, mobility area definitions          |
| Chess Programming Wiki, [Pawn Structure](https://www.chessprogramming.org/Pawn_Structure)                                                                                      | 15 min | Doubled, isolated, backward, passed pawns, critical long-term positional factors              |
| Chess Programming Wiki, [Tapered Eval](https://www.chessprogramming.org/Tapered_Eval)                                                                                          | 10 min | Smoothly interpolating between opening and endgame evaluation using game phase detection       |
| Chess Programming Wiki, [Quiescence Search](https://www.chessprogramming.org/Quiescence_Search)                                                                                | 15 min | Extending search at leaf nodes to resolve tactical sequences (captures, checks)                |
| Chess Programming Wiki, [Aspiration Windows](https://www.chessprogramming.org/Aspiration_Windows)                                                                              | 10 min | Narrowing the alpha-beta window around expected score for faster searches                      |
| Chess Programming Wiki, [Transposition Table](https://www.chessprogramming.org/Transposition_Table)                                                                            | 20 min | Caching evaluated positions with Zobrist hashing, replacement schemes, hash collisions         |
| François Dominic Laramée, [Chess Programming Part I–VI](https://www.gamedev.net/tutorials/programming/artificial-intelligence/chess-programming-part-i-getting-started-r1014/) | 30 min | Classic tutorial series covering chess engine basics from board representation to search       |
| Chess Programming Wiki, [Move Ordering](https://www.chessprogramming.org/Move_Ordering)                                                                                        | 15 min | Why move ordering dramatically affects alpha-beta efficiency: MVV-LVA, killer moves, hash move |
| Bruce Moreland, [Programming Topics](https://web.archive.org/web/20071026090003/http://www.brucemo.com/compchess/programming/)                                                 | 25 min | Classic reference on rotated bitboards, search, and evaluation implementation details          |

---

## Code Study (Optional)

| Repository                                                   | Language | Focus                                                                            |
| ------------------------------------------------------------ | -------- | -------------------------------------------------------------------------------- |
| [Sunfish](https://github.com/thomasahle/sunfish)             | Python   | Complete chess engine in 111 lines, excellent for understanding core concepts   |
| [Chess-Engine](https://github.com/lhartikk/simple-chess-ai)  | JS       | Simple chess AI with minimax and evaluation, easy to read and modify            |
| [Stockfish](https://github.com/official-stockfish/Stockfish) | C++      | World's strongest open-source engine, study evaluation and search architecture  |
| [Rustic](https://github.com/mvanthoor/rustic)                | Rust     | Well-documented didactic engine with accompanying tutorial book                  |
| [Vice](https://github.com/bluefeversoft/vice)                | C        | Chess engine built on YouTube tutorial series, great learn-by-watching resource |

---

## Key Concepts Summary

| Concept                  | Core Idea                                                                                                             |
| ------------------------ | --------------------------------------------------------------------------------------------------------------------- |
| **UCI Protocol**         | Standard text-based interface for engine ↔ GUI communication (`position`, `go`, `bestmove`)                           |
| **Board Representation** | Data structure encoding piece placement, mailbox (simple) vs bitboards (fast) trade-off                              |
| **Move Generation**      | Enumerating all legal moves from a position; performance-critical since it runs millions of times per search          |
| **Material Evaluation**  | Counting piece values (P=100, N=320, B=330, R=500, Q=900), the foundation of any evaluation function                 |
| **Piece-Square Tables**  | Static bonuses/penalties per piece per square encoding positional knowledge (e.g., knights are good in the center)    |
| **Mobility**             | Number of legal moves available, more mobility generally means more active, better-placed pieces                     |
| **King Safety**          | Evaluating how exposed the king is to attack, pawn shield, open files near king, attacker proximity                  |
| **Iterative Deepening**  | Search depth 1, then depth 2, then depth 3…, provides anytime results and improves move ordering via previous depths |
| **Time Management**      | Deciding how long to think per move, allocate more time in complex/critical positions, less in forced/simple ones    |

---

**Study order:** Sebastian Lague Chess video (A) → Millington Ch. 8.1–8.3 → UCI Protocol article → Board Representation → Simplified Evaluation Function → Evaluation + Piece-Square Tables → King Safety → Iterative Deepening → Time Management → Sebastian Lague Better Bot (B)

**Total required time:** ~2h (readings: 2h, videos: recommended ~45min on top)
