# Assignment: MinMax with Alpha-Beta Pruning

## Objective

Build an AI engine that explores game states using **minimax search with alpha-beta pruning**. Your engine must intelligently prune branches that cannot influence the final decision.

## Requirements

- **No external AI/search libraries allowed.** You must implement the minimax algorithm and pruning logic yourself. I will evaluate your ability to create proper abstractions and apply the algorithm correctly.
- You may use any game engine or framework for rendering and input handling.
- You may use my Chess boilerplate if you choose Chess: [github.com/gameguild-gg/chess-competition](https://github.com/gameguild-gg/chess-competition)

## Game Options

Choose one of the following (or propose another—check with me first):

| Game                            | Notes                                                           |
| ------------------------------- | --------------------------------------------------------------- |
| **Tic-Tac-Toe**                 | Simplest option, but you must implement the full search         |
| **Connect 4**                   | Medium complexity, good balance of depth and branching          |
| **Checkers**                    | See [rules](https://winning-moves.com/images/kingmerulesv2.pdf) |
| **Chess**                       | You may use a simplified ruleset or fewer pieces                |
| **Rubik's Cube**                | Large state space with many transpositions—challenging!         |
| **Match-3** (Candy Crush style) | Must be Match-3, not Match-1                                    |

**Important:** Whichever game you choose, you must implement minimax with alpha-beta pruning.

---

## Implementation Guide

### 1. Board Representation

Use a 2D array, bitboard, or other suitable structure to represent game state.

- Reference: [Chess Programming Wiki - Board Representation](https://www.chessprogramming.org/Board_Representation)

### 2. Legal Move Generation

Implement the rules of your chosen game to generate all valid moves from any position. For Chess, this includes handling checks, pins, and special moves.

### 3. Position Evaluation

Create a heuristic function that scores positions. Consider factors relevant to your game:

- Material balance
- Piece mobility
- Positional advantages
- Threat detection

Reference: [Chess Programming Wiki - Evaluation](https://www.chessprogramming.org/Evaluation)

### 4. Minimax Search with Alpha-Beta Pruning

Implement the core search algorithm:

- Minimax recursively evaluates positions
- Alpha-beta pruning eliminates branches that cannot affect the outcome
- Use your evaluation function at leaf nodes or depth limits

Reference: [Chess Programming Wiki - Search](https://www.chessprogramming.org/Search)

### 5. Move Ordering (Recommended)

Order moves so promising ones are searched first. This dramatically improves pruning efficiency.

### 6. Transposition Tables (Optional)

Cache evaluated positions to avoid redundant computation when the same state is reached via different move sequences.

### 7. Time Management (Optional)

Implement depth limits or time controls to ensure responsive play.

### 8. Advanced Features (Optional)

For extra challenge: quiescence search, iterative deepening, endgame tablebases, or multi-threading.

---

## Submission

**Deliverables:**

1. **Video** (max 5 minutes): Walk through the most important parts of your code and demonstrate your AI in action
2. **Source code**: Zip your project files

**Do not include:** Binary files, executables, debug folders, or build artifacts.

---

## Additional Resources

- [Building My Own Chess Engine](https://healeycodes.com/building-my-own-chess-engine) - Walkthrough of a chess engine implementation
- [python-chess Selected Projects](https://github.com/niklasf/python-chess#selected-projects) - Examples of chess projects
- [Red Blob Games - A\* Introduction](https://www.redblobgames.com/pathfinding/a-star/introduction.html) - Useful for understanding search concepts
