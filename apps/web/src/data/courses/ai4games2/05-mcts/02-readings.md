# Week 05 Readings - Monte Carlo Tree Search

---

## Required Readings

| #   | Reading                                                                                                     | Time   | Covers                                                                                  |
| --- | ----------------------------------------------------------------------------------------------------------- | ------ | --------------------------------------------------------------------------------------- |
| 1   | Ian Millington, **AI for Games (3rd Ed.)**, Chapter 8.4 (Monte Carlo Search) - ISBN 9781138483972           | 30 min | MCTS fundamentals, rollout policies, selection strategies, convergence properties       |
| 2   | GeeksForGeeks, [Monte Carlo Tree Search](https://www.geeksforgeeks.org/ml-monte-carlo-tree-search-mcts/)    | 15 min | Step-by-step MCTS walkthrough with diagrams and pseudocode                              |
| 3   | Chess Programming Wiki, [Monte-Carlo Tree Search](https://www.chessprogramming.org/Monte-Carlo_Tree_Search) | 20 min | MCTS terminology, phases, UCT, practical considerations for board games                 |
| 4   | Chess Programming Wiki, [UCT](https://www.chessprogramming.org/UCT)                                         | 10 min | Upper Confidence bounds applied to Trees, mathematical foundation, exploration constant |

**Focus while reading:**

- Millington: How MCTS differs from minimax, when to prefer sampling over exhaustive search, rollout policies
- GeeksForGeeks: The four-phase loop (Selection → Expansion → Simulation → Backpropagation), worked example
- Chess Programming Wiki (MCTS): Terminology, tree policy vs default policy, enhancements
- Chess Programming Wiki (UCT): UCB1 formula derivation, the exploration-exploitation tradeoff, tuning $C$

---

## Videos

| #   | Video                                                                                                        | Time   | Covers                                                                       |
| --- | ------------------------------------------------------------------------------------------------------------ | ------ | ---------------------------------------------------------------------------- |
| A   | John Levine - [Monte Carlo Tree Search](https://www.youtube.com/watch?v=UXW2yZndl7U)                         | 14 min | Clear visual explanation of the four MCTS phases with game tree examples     |
| B   | Sebastian Lague - [Coding Adventure: Making a Better Chess Bot](https://www.youtube.com/watch?v=_vqlIPDR2TU) | 25 min | Practical MCTS implementation journey, debugging, and performance comparison |

---

## Interactive Resources

| #   | Resource                                                          | Time   | Covers                                                            |
| --- | ----------------------------------------------------------------- | ------ | ----------------------------------------------------------------- |
| 1   | [MCTS Visualizer](https://vgarciasc.github.io/mcts-viz/)          | 15 min | Watch MCTS build its tree in real-time on Tic-Tac-Toe             |
| 2   | [UCT Visualizer (GeoGebra)](https://www.geogebra.org/3d/qw9efwtf) | 10 min | 3D visualization of the UCB1 surface as a function of wins/visits |

**Hands-on task:** In the MCTS Visualizer:

- Run MCTS on an empty Tic-Tac-Toe board and observe the tree growth
- Compare trees after 100 vs 1000 iterations — notice how the tree becomes asymmetric
- Watch how UCB1 balances exploration (trying new nodes) and exploitation (revisiting good nodes)
- Try modifying the exploration constant and observe how it affects tree shape

---

## Optional Deep Dive

| Resource                                                                                                                                           | Time   | Focus                                                                       |
| -------------------------------------------------------------------------------------------------------------------------------------------------- | ------ | --------------------------------------------------------------------------- |
| Browne et al., [A Survey of Monte Carlo Tree Search Methods](https://ieeexplore.ieee.org/document/6145622) (IEEE, 2012)                            | 60 min | Comprehensive survey: variations, enhancements, applications, open problems |
| Silver et al., [Mastering the game of Go without Human Knowledge](http://discovery.ucl.ac.uk/10045895/1/agz_unformatted_nature.pdf) (Nature, 2017) | 45 min | AlphaGo Zero: self-play MCTS + neural networks, no human data               |
| Silver et al., [Mastering Chess and Shogi by Self-Play](https://arxiv.org/abs/1712.01815) (2017)                                                   | 30 min | AlphaZero generalized to Chess and Shogi, surpassing Stockfish              |
| Anthony et al., [Thinking Fast and Slow with Deep Learning and Tree Search](https://arxiv.org/pdf/1705.08439.pdf) (2017)                           | 30 min | Expert Iteration: combining MCTS with imitation learning                    |
| UQ Pressbooks, [Monte Carlo Tree Search Chapter](https://uq.pressbooks.pub/mastering-reinforcement-learning/chapter/monte-carlo-tree-search/)      | 25 min | MCTS in the context of reinforcement learning, policy improvement           |
| Roger Grosse & Jimmy Ba, [MCTS Lecture Slides](http://www.cs.toronto.edu/~rgrosse/courses/csc421_2019/slides/lec22.pdf) (U of Toronto)             | 20 min | Academic treatment of MCTS, convergence proofs, connections to RL           |
| David Duvenaud, [MCTS Introduction Slides](https://duvenaud.github.io/learning-to-search/slides/week3/MCTSintro.pdf)                               | 15 min | Compact slide deck covering core MCTS concepts                              |
| Ferenc Huszár, [AlphaGo Zero and Expert Iteration](https://www.inference.vc/alphago-zero-policy-improvement-and-vector-fields/) (Blog)             | 15 min | Intuitive explanation of policy improvement through MCTS                    |
| Chess Programming Wiki - [MCTS Enhancements](https://www.chessprogramming.org/Monte-Carlo_Tree_Search#Enhancements)                                | 20 min | RAVE, progressive widening, transposition tables in MCTS                    |

---

## Code Study (Optional)

| Repository                                                       | Language | Focus                                                            |
| ---------------------------------------------------------------- | -------- | ---------------------------------------------------------------- |
| [mcts](https://github.com/pbsinclair42/mcts)                     | Python   | Clean, minimal MCTS implementation — great for learning          |
| [python-mcts](https://github.com/int8/monte-carlo-tree-search)   | Python   | Well-documented MCTS with UCT for two-player games               |
| [Leela Chess Zero](https://github.com/LeelaChessZero/lc0)        | C++      | Production MCTS+neural network chess engine (AlphaZero approach) |
| [KataGo](https://github.com/lightvector/KataGo)                  | C++      | State-of-the-art Go engine using MCTS with neural networks       |
| [AlphaZero.jl](https://github.com/jonathan-laurent/AlphaZero.jl) | Julia    | Clean AlphaZero implementation for arbitrary games               |

---

## Key Concepts Summary

| Concept                  | Core Idea                                                                                     |
| ------------------------ | --------------------------------------------------------------------------------------------- |
| **MCTS**                 | Build a partial game tree using random simulations to estimate move values                    |
| **Selection (UCB1)**     | Balance exploitation (high win rate) with exploration (less visited nodes)                    |
| **Expansion**            | Add new child nodes when reaching a leaf                                                      |
| **Simulation (Rollout)** | Play random moves to a terminal state and use the outcome as a value estimate                 |
| **Backpropagation**      | Update visit counts and win counts along the path from leaf to root                           |
| **Exploration constant** | $C$ in UCB1 controls explore vs exploit tradeoff; $\sqrt{2}$ is theoretically optimal         |
| **Anytime property**     | MCTS can return a move at any time; more iterations yield better results                      |
| **Asymmetric tree**      | MCTS naturally searches deeper in promising lines and shallower in unpromising ones           |
| **Neural MCTS (PUCT)**   | Replace random rollouts with neural network evaluation; use policy prior to guide exploration |

---

**Study order:** John Levine MCTS video → GeeksForGeeks article → Chess Programming Wiki → MCTS Visualizer → Millington Ch. 8.4 → Sebastian Lague video

**Total required time:** ~2h 19min (readings: 1h 15min, videos: 39min, interactive: 25min)
