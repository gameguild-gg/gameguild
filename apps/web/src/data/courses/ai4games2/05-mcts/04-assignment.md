# Assignment: Monte Carlo Tree Search

## Objective

Build an AI engine that uses **Monte Carlo Tree Search (MCTS)** to make decisions in a turn-based game. Your engine must implement the four core MCTS phases (Selection, Expansion, Simulation, Backpropagation) and use UCB1 for tree policy selection.

## Requirements

- **No external AI/search libraries allowed.** You must implement the MCTS algorithm, UCB1 selection, rollout policy, and backpropagation yourself. I will evaluate your ability to create proper abstractions and apply the algorithm correctly.
- You may use any game engine or framework for rendering and input handling.
- You may reuse your game framework from Assignment 4 (MinMax) and replace the search algorithm with MCTS.

## Game Options

Choose one of the following (or propose another—check with me first):

| Game                     | Notes                                                            |
| ------------------------ | ---------------------------------------------------------------- |
| **Tic-Tac-Toe**          | Simplest option—good for verifying correctness vs known solution |
| **Connect 4**            | Medium complexity, good branching factor for MCTS                |
| **Hex**                  | MCTS excels here—no known good evaluation function               |
| **Othello / Reversi**    | Classic MCTS benchmark, well-studied                             |
| **Go (9×9)**             | The canonical MCTS game—smaller board for feasibility            |
| **Chess**                | Compare MCTS vs your MinMax from Assignment 4                    |
| **Ultimate Tic-Tac-Toe** | Nested game with interesting strategic depth                     |

**Bonus:** If you implement the same game as Assignment 4, you can pit your MinMax AI against your MCTS AI and compare their performance!

---

## Implementation Guide

### 1. Node Structure

Create an MCTS node class that stores:

- Game state
- Parent pointer and children list
- Visit count ($n$) and win count ($w$)
- UCB1 calculation method

```cpp
class MCTSNode {
    MCTSNode* parent;
    std::vector<MCTSNode*> children;
    double wins;
    int visits;
    GameState state;

    double ucb(double C = std::sqrt(2.0));
    bool isLeaf();
    bool isTerminal();
};
```

### 2. Selection (UCB1 Tree Policy)

Implement the selection phase using UCB1:

$$UCB1 = \frac{w_i}{n_i} + C \sqrt{\frac{\ln N}{n_i}}$$

Ensure unvisited nodes return UCB1 = ∞ to guarantee they're visited at least once.

### 3. Expansion

When reaching a leaf that is not terminal, generate child nodes for legal moves. You may expand all children at once or one at a time.

### 4. Simulation (Rollout)

Implement at minimum a **uniform random rollout** — play random legal moves until reaching a terminal state. Return the game outcome (win=1, loss=0, draw=0.5).

### 5. Backpropagation

Propagate the simulation result back to the root. **Remember to flip the result at each level** since alternating players have opposite goals.

### 6. Move Selection

After the iteration budget is exhausted, select the root's child with the **most visits** as the best move.

### 7. Iteration/Time Budget

Your MCTS must support either:

- A fixed number of iterations (e.g., 1000–10000), or
- A time limit (e.g., 1–5 seconds per move)

---

## Bonus Features (Optional)

For extra challenge, implement one or more of the following:

| Feature                   | Description                                                               |
| ------------------------- | ------------------------------------------------------------------------- |
| **Tree reuse**            | Reuse the subtree from the previous move instead of building from scratch |
| **Better rollout policy** | Use heuristics or domain knowledge instead of pure random play            |
| **MCTS vs MinMax**        | Pit your MCTS against your Assignment 4 MinMax and report results         |
| **Tunable exploration**   | Allow adjusting the exploration constant $C$ and analyze its effect       |
| **Parallelization**       | Implement leaf-parallel or root-parallel MCTS                             |
| **RAVE/AMAF**             | All-Moves-As-First heuristic to speed up convergence                      |
| **Progressive widening**  | Limit child expansion based on visit count for large branching factors    |

---

## Submission

**Deliverables:**

1. **Video** (max 5 minutes): Walk through the most important parts of your code and demonstrate your AI in action. Show the MCTS statistics (iterations, tree size, win rates) if possible.
2. **Source code**: Zip your project files

**Do not include:** Binary files, executables, debug folders, or build artifacts.

---

## Additional Resources

- [MCTS Visualizer](https://vgarciasc.github.io/mcts-viz/) — Watch MCTS build its tree step by step
- [Monte Carlo Tree Search (GeeksForGeeks)](https://www.geeksforgeeks.org/ml-monte-carlo-tree-search-mcts/) — Implementation walkthrough
- [pbsinclair42/mcts](https://github.com/pbsinclair42/mcts) — Clean Python MCTS reference implementation
- [Browne et al., A Survey of MCTS Methods (2012)](https://ieeexplore.ieee.org/document/6145622) — Comprehensive survey of MCTS variations
