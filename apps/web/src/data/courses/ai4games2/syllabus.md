# Advanced Artificial Intelligence for Games

![Adv. AI for Games Banner](https://i.imgur.com/cooKXbw.jpeg)

Students with a firm foundation in the basic techniques of artificial intelligence for games will apply their skills to program advanced pathfinding algorithms, artificial opponents, scripting tools and other real-time drivers for non-playable agents. The goal of the course is to provide finely-tuned artificial competition for players using all the rules followed by a human. [Source](https://classliststaging.champlain.edu/show/course/number/GPR_440)

## Instructors

Feel free to add us to your professional network!

- (main) [Alexandre Tolstenko](https://www.linkedin.com/in/aletolstenko/) 🔗 - [Book a meeting with me](https://calendar.app.google/EU42UnUSyTwyhryL9)
- (external) [Matheus Martins](https://www.linkedin.com/in/mathrmartins/) 🔗

## Requirements

- [Artificial Intelligence for Games](https://classliststaging.champlain.edu/show/course/number/GPR_340). minimum grade of C.

## Textbook

- AI for Games, Third Edition: 9781138483972: Millington, Ian

## Learning Outcomes and Competencies

Using the [Bloom's Taxonomy](https://cft.vanderbilt.edu/guides-sub-pages/blooms-taxonomy/) and the [Champlain College Competency Framework](https://competencies.champlain.edu/), the following learning outcomes have been developed for this course:

### Objective Outcomes

By the end of this course, students will be able to:

1. **Compare** decision architectures (FSMs, behavior trees, decision trees, utility systems), **defending** appropriate choices for different game scenarios. _(Analysis)_
2. **Implement** game tree search algorithms (MinMax, alpha-beta, MCTS) for adversarial game AI. _(Technology Literacy)_
3. **Design** evaluation functions and heuristics that guide AI decision-making in complex game states. _(Creativity)_
4. **Analyze** algorithm performance through competitive evaluation, measuring search depth, node expansions, and playing strength. _(Quantitative Literacy)_
5. **Apply** constraint-based procedural generation (Wave Function Collapse) to create game content. _(Technology Literacy)_
6. **Construct** planning systems (GOAP) by modeling actions, preconditions, effects, and world states. _(Technology Literacy)_
7. **Evaluate** multi-agent coordination patterns (blackboard, scheduling, tokens), **justifying** architectural decisions. _(Analysis)_
8. **Synthesize** tactical reasoning systems using influence maps and spatial queries for NPC positioning. _(Integration)_
9. **Collaborate** in teams to deliver a complete AI system addressing real-world game development challenges. _(Collaboration)_
10. **Formulate** design questions about AI behavior requirements that guide implementation decisions. _(Inquiry)_

### Assessment Outcomes

| Outcome                             | Assessment Method                        | Week      |
| ----------------------------------- | ---------------------------------------- | --------- |
| Decision architecture comparison    | Quiz 1, Assignment 1                     | 2         |
| Game tree search implementation     | Quizzes 3-6, Assignments 3-6, Midterm    | 4-8       |
| Evaluation function design          | Assignment 5-6, Midterm                  | 6-8       |
| Algorithm performance analysis      | Midterm (competition ranking + analysis) | 8         |
| Constraint-based PCG                | Quiz 7, Assignment 7                     | 10        |
| Planning system construction        | Quiz 8, Assignment 8                     | 11        |
| Multi-agent architecture evaluation | Quiz 9, Final Project                    | 12, 15-16 |
| Tactical reasoning synthesis        | Quiz 10, Final Project                   | 13, 15-16 |
| Team collaboration                  | Final Project, Peer Evaluations          | 10-16     |
| Design requirement inquiry          | Project Proposal, Checkpoints            | 10-14     |

### Champlain Competencies Addressed

| Competency                | Course Coverage                                                                                                 | Primary Assessments                     |
| ------------------------- | --------------------------------------------------------------------------------------------------------------- | --------------------------------------- |
| **Analysis**              | Decision architecture trade-offs, algorithm comparison, multi-agent pattern evaluation, competitive post-mortem | Quizzes, Midterm Analysis, Assignments  |
| **Technology Literacy**   | C++ implementation, GitHub Actions CI/CD, game tree search, GOAP planners, WFC generators                       | All Assignments, Midterm, Final Project |
| **Creativity**            | Evaluation function design, believable agent behaviors, novel AI solutions for final project                    | Assignments 5-6, Final Project          |
| **Quantitative Literacy** | Search depth analysis, node expansion metrics, Elo ratings, performance benchmarking                            | Midterm Competition, Assignment Tests   |
| **Collaboration**         | Team-based final project with defined roles, peer code review, milestone accountability                         | Final Project, Peer Evaluations         |
| **Integration**           | Combining decision systems, planning, multi-agent coordination, and tactical reasoning into complete game AI    | Final Project                           |
| **Inquiry**               | Framing AI behavior requirements, debugging agent decisions, architectural trade-off questions                  | Project Proposal, Checkpoints           |

## Philosophy and Approach

This course focuses on **Experiential Learning**, where students learn by building functional AI systems. The course is structured around hands-on coding assignments automatically tested via GitHub Actions, culminating in a competitive chess AI tournament and a comprehensive final project. Students will engage in:

- **Competitive Evaluation**: The chess competition provides measurable feedback on algorithm effectiveness through Elo ratings and tournament rankings
- **Iterative Development**: Weekly assignments build incrementally toward complex systems
- **Industry Case Studies**: Analysis of shipped game AI (Halo 2, F.E.A.R., Killzone, Alien Isolation) grounds theory in practice
- **Real-World Challenges**: Final projects may address problems proposed by industry partners
- **Peer Learning**: Code reviews and peer evaluations develop professional collaboration skills

## Course Overview

| Phase                         | Weeks | Focus                                              | Assessment                                |
| ----------------------------- | ----- | -------------------------------------------------- | ----------------------------------------- |
| Decision & Search Foundations | 1-7   | FSM, BTs, Utility AI, MinMax, MCTS, Chess Engine   | 7 Coding Assignments + Weekly Quizzes     |
| Midterm                       | 8     | Chess AI Competition                               | Tournament + Code Review + Analysis Essay |
| Spring Break                  | 9     | -                                                  | -                                         |
| Advanced Topics               | 10-11 | Wave Function Collapse, GOAP Planning              | 2 Coding Assignments + Quizzes            |
| Project Focus                 | 12-14 | Multi-Agent, Influence Maps, Stealth/Believability | Quizzes + Project Checkpoints             |
| Finalization                  | 15-16 | Peer Evaluation, Presentations                     | Final Project (Code + Essay + Demo)       |

## Holiday and Important Dates

| Date         | Reason             |
| ------------ | ------------------ |
| Monday, 1/19 | MLK Day (No Class) |
| 3/9–3/13     | Spring Break       |
| Friday, 4/10 | Day off            |

## Schedule for Spring 2026

::: warning

This is a work in progress schedule. It is subject to change. Every change will be communicated in class.

:::

---

### WEEK 01 - Course Setup & FSM Refresher

**2026/01/12 – 2026/01/16**

**Monday 2026/01/12** - Course Introduction

- Instructor introduction
- Syllabus review, course outcomes, grading breakdown
- **Final project overview** (introduced now, work begins after midterm)
- GitHub setup walkthrough

**Thursday 2026/01/15** - FSM Deep Dive

- Clone course repository
- CMake + doctest environment verification
- GitHub Actions auto-grading demonstration
- FSM architecture: from naive switch/if-else to State Pattern
- Transition management: separation of states and transitions
- Stack-based FSMs (pushdown automata)
- **Hierarchical State Machines (HSMs)**: nested substates, parent-child relationships

**Reading**: Millington Ch. 5 (Decision Making - FSM sections)

**Quiz 0** (2026/01/15): Course policies, syllabus, FSM concepts  
**Assignment 0** (due 2026/01/18): **FSM Implementation with State Pattern**

- Implement a clean FSM framework separating states from transitions
- 3-state system (IDLE → ALERT → COMBAT) with condition-based transitions
- Data-driven transitions configurable at runtime
- Auto-graded: state lifecycle verification (onEnter/execute/onExit) across multiple scenarios

---

### WEEK 02 - Advanced Decision Architectures

**2026/01/19 – 2026/01/23**

**Monday 2026/01/19** - NO CLASS (Martin Luther King Jr. Day)

**Thursday 2026/01/22** - Decision Systems Deep Dive

- **Behavior Trees**: selectors, sequences, parallels, decorators
- **Decision Trees**: runtime evaluation, pruning strategies
- When to use what: trade-offs and industry patterns
- Case study: Halo 2's behavior architecture

**Reading**: Millington Ch. 5 (Decision Making)

**Quiz 1** (2026/01/22): BT node types, decision tree concepts  
**Assignment 1** (due 2026/01/25): TBD

- _Assignment details to be determined_

---

### WEEK 03 - Utility AI

**2026/01/26 – 2026/01/30**

**Monday 2026/01/26** - Utility AI Fundamentals

- Scoring-based decision making
- Response curves: linear, quadratic, logistic, sine
- Consideration architecture: inputs → normalization → curves → scores
- Combining considerations: multiplication vs. averaging
- Infinite Axis Utility System (IAUS) overview

**Thursday 2026/01/29** - Utility AI in Practice

- Tuning response curves for desired behavior
- Debugging utility systems: score visualization
- Case study: The Sims needs system, Guild Wars 2 combat AI
- Comparison with BTs: when utility wins, when it doesn't

**Reading**: Dave Mark's GDC talks on Utility AI (2010, 2012)

**Quiz 2** (2026/01/29): Response curves, utility scoring  
**Assignment 2** (due 2026/02/01): **Utility-Based NPC**

- Implement utility system for survival game NPC
- Actions: Eat, Sleep, Gather, Craft, Fight, Flee
- 6+ considerations with different response curves
- Auto-graded: behavior validation across hunger/energy/threat scenarios
- Visualization output of scores over time (bonus)

---

### WEEK 04 - MinMax and Alpha-Beta Pruning

**2026/02/02 – 2026/02/06**

**Monday 2026/02/02** - Game Tree Search Foundations

- Two-player zero-sum games
- MinMax algorithm: theory and recursive implementation
- Evaluation functions: static board assessment
- Terminal vs. non-terminal nodes

**Thursday 2026/02/05** - Alpha-Beta Pruning

- Pruning principle: why it works
- Alpha-beta implementation
- Move ordering: killer moves, history heuristic
- Transposition tables and Zobrist hashing introduction

**Reading**: Millington Ch. 8 (Board Games)

**Quiz 3** (2026/02/05): MinMax algorithm, pruning conditions  
**Assignment 3** (due 2026/02/08): **Tic-Tac-Toe Perfect Player**

- Implement MinMax with alpha-beta pruning
- Must achieve perfect play (never loses)
- Auto-graded:
  - Correctness: plays optimally in all positions
  - Pruning efficiency: node count reduction measured
  - Transposition table correctness (bonus)

---

### WEEK 05 - Monte Carlo Tree Search

**2026/02/09 – 2026/02/13**

**Monday 2026/02/09** - MCTS Fundamentals

- Four phases: Selection, Expansion, Simulation, Backpropagation
- UCB1 formula: exploration vs. exploitation
- When MCTS beats MinMax: large branching factors, hard-to-evaluate positions
- Case study: AlphaGo's approach

**Thursday 2026/02/12** - MCTS Implementation

- Tree policy vs. default policy
- Simulation strategies: random vs. heuristic rollouts
- Parallelization strategies (root, leaf, tree)
- Time management: iterations vs. wall clock

**Reading**: Browne et al. "A Survey of MCTS Methods" (2012)

**Quiz 4** (2026/02/12): UCB1 formula, MCTS phases  
**Assignment 4** (due 2026/02/15): **Connect Four MCTS**

- Implement complete MCTS player
- UCB1 selection with configurable exploration constant
- Auto-graded:
  - UCB1 correctness: selection matches expected nodes
  - Backpropagation: win/visit counts verified
  - Competitive play: must beat random player 95%+, beat simple heuristic 70%+

---

### WEEK 06 - Chess Engine Core

**2026/02/16 – 2026/02/20**

**Monday 2026/02/16** - Chess AI Architecture

- UCI protocol overview (library provided)
- Board representation strategies
- Move generation (library provided)
- Evaluation function design: material, position, mobility, king safety

**Thursday 2026/02/19** - Search Implementation for Chess

- Iterative deepening: anytime algorithm benefits
- Aspiration windows
- Quiescence search: handling tactical sequences
- Time management for tournament play

**Provided**: UCI wrapper library, legal move generator, board representation

**Quiz 5** (2026/02/19): Chess evaluation concepts, iterative deepening  
**Assignment 5** (due 2026/02/22): **Chess Engine Core**

- Implement search algorithm (MinMax or MCTS, your choice)
- Implement evaluation function (minimum: material + piece-square tables)
- UCI compliance verification
- Auto-graded:
  - UCI protocol tests: responds correctly to all commands
  - Finds forced mates in test positions (mate-in-1, mate-in-2)
  - Baseline Elo estimate against reference engine

---

### WEEK 07 - Chess Engine Optimization

**2026/02/23 – 2026/02/27**

**Monday 2026/02/23** - Advanced Chess Techniques

- Null-move pruning, late move reductions
- Killer move heuristic, history tables
- Opening book integration
- Endgame considerations

**Thursday 2026/02/26** - Competition Preparation Workshop

- In-class practice tournament (preliminary rankings)
- Debugging session with instructor support
- cutechess-cli usage demonstration
- Competition rules review

**Quiz 6** (2026/02/26): Search optimizations, pruning techniques  
**Assignment 6** (due 2026/03/01): **Chess Engine Final Submission**

- Polished engine for competition
- Documentation: search strategy, evaluation design, optimizations
- Self-play testing results
- Auto-graded: final UCI compliance check, performance benchmarks

---

### WEEK 08 - MIDTERM: Chess AI Competition

**2026/03/02 – 2026/03/06**

**Monday 2026/03/02** - Competition Warm-up

- Final engine testing
- Last-minute questions
- **Final Project detailed introduction and team formation**

**Thursday 2026/03/05** - Chess Competition Day

**Tournament Format:**

- Swiss system (7 rounds) → Double elimination playoff (top 8)
- Time control: 5 minutes + 0.5 second increment
- Infrastructure: cutechess-cli on standardized runners

**Midterm Grading:**
| Component | Weight |
|-----------|--------|
| Tournament Performance (Elo-based) | 50% |
| Code Review (algorithm implementation) | 30% |
| Written Analysis (design decisions) | 20% |

---

### WEEK 09 - SPRING BREAK

**2026/03/09 – 2026/03/13**

No classes or assignments. Optional: continue engine development for post-mortem analysis.

---

### WEEK 10 - Wave Function Collapse

**2026/03/16 – 2026/03/20**

**Monday 2026/03/16** - WFC Fundamentals

- Constraint-based procedural generation
- Tiled vs. overlapping models
- Entropy-based cell selection
- Propagation algorithm

**Thursday 2026/03/19** - WFC Implementation

- Adjacency constraint definition
- Failure recovery: backtracking vs. restart
- Input exemplar design
- Combining WFC with rule-based post-processing

**Reading**: Maxim Gumin's WFC documentation

**Quiz 7** (2026/03/19): WFC algorithm, constraint propagation  
**Assignment 7** (due 2026/03/22): **WFC Dungeon Generator**

- Implement tiled WFC for 2D dungeon generation
- Minimum 8 tile types with adjacency rules
- Auto-graded:
  - Constraint satisfaction: no invalid adjacencies
  - Connectivity: all rooms reachable (flood fill verification)
  - Reproducibility: same seed = identical output
  - Performance: tiles/second benchmark

**Final Project**: Proposal due

---

### WEEK 11 - Goal-Oriented Action Planning (GOAP)

**2026/03/23 – 2026/03/27**

**Monday 2026/03/23** - GOAP as A\* Through Action Space

- From spatial pathfinding to planning
- World state representation
- Actions: preconditions and effects
- Planning as search: start state → goal state

**Thursday 2026/03/26** - GOAP Implementation

- Regressive vs. progressive planning
- Cost functions and action preferences
- Runtime replanning
- **HTN Overview** (15 min): alternative planning approach, when to use each
- Case study: F.E.A.R. AI (source code available)

**Reading**: F.E.A.R. SDK GOAP documentation

**Quiz 8** (2026/03/26): GOAP concepts, action modeling  
**Assignment 8** (due 2026/03/29): **GOAP Planner** _(LAST CODING ASSIGNMENT)_

- Implement GOAP for RTS-style unit
- Actions: Gather Wood, Gather Stone, Build House, Build Barracks, Train Unit, Attack
- Auto-graded:
  - Plan generation: correct action sequences for 20+ goal scenarios
  - Optimality: plans within 10% of optimal cost
  - Replanning: handles world state changes mid-execution

**Final Project**: Checkpoint #1 (architecture design)

---

### WEEK 12 - Multi-Agent Systems

**2026/03/30 – 2026/04/03**

**Monday 2026/03/30** - Multi-Agent Coordination

- Blackboard architecture: shared knowledge base
- Publisher/subscriber patterns
- Knowledge queries and posting

**Thursday 2026/04/02** - Scheduling and Resource Management

- Time-slot scheduling for coordinated actions
- Token systems: attack tokens, speak tokens
- Role assignment and squad behaviors
- Case study: Killzone 2/3 hierarchical AI

**Reading**: Millington Ch. 6 (Tactical and Strategic AI)

**Quiz 9** (2026/04/02): Blackboard architecture, token systems

**Final Project**: Checkpoint #2 (core implementation)

---

### WEEK 13 - Influence Maps and Tactical AI

**2026/04/06 – 2026/04/10**

**Monday 2026/04/06** - Influence Maps

- Spatial reasoning through value propagation
- Decay functions and update frequencies
- Layered maps: threat, territory, resources
- Queries: "safest path", "best attack position"

**Thursday 2026/04/09** - Tactical Position Evaluation

- Cover point generation and quality scoring
- Flanking opportunity detection
- Combining influence maps with pathfinding
- Case study: Company of Heroes tactical AI

**Quiz 10** (2026/04/09): Influence map calculations, tactical queries

**Final Project**: Testing session (in-class)

---

### WEEK 14 - Stealth AI and Believable Agents

**2026/04/13 – 2026/04/17**

**Monday 2026/04/13** - Stealth Game AI

- Perception systems: vision cones, audio detection
- Awareness states: Unaware → Suspicious → Alert → Combat → Search
- Last known position tracking
- Alert propagation and coordinated search

**Thursday 2026/04/16** - Believable Agent Design

- Imperfection: intentional mistakes, reaction delays
- Personality systems: aggression, caution, curiosity
- Emotional state and behavior modulation
- Case studies: Alien Isolation, The Last of Us companion AI

**Quiz 11** (2026/04/16): Perception systems, awareness states

**Final Project**: Feature freeze

---

### WEEK 15 - Peer Evaluation and Code Freeze

**2026/04/20 – 2026/04/24**

**Monday 2026/04/20** - Peer Evaluation Session

- Teams exchange projects for code review
- Structured feedback using provided rubric
- Bug identification and UX feedback

**Thursday 2026/04/23** - Code Freeze and Polish

- Final bug fixes only (no new features)
- Documentation completion
- Presentation preparation workshop

**Final Project Deliverables** (due 2026/04/26):

- Code freeze and project submission no later than wednesday 2026/04/22
- Technical essay draft
- Peer evaluation done by sunday 2026/04/26

---

### WEEK 16 - Final Presentations

**2026/04/27 – 2026/05/01**

**Monday 2026/04/27** - Presentations (Group A)

- 10-minute presentation + 5-minute Q&A per team
- Live demonstration required

**Thursday 2026/04/30** - Presentations (Group B)

- 10-minute presentation + 5-minute Q&A per team
- Live demonstration required

**Final Project Submission** (due 2026/04/30):

- Final code repository
- Technical essay (2000–3000 words)
- Peer evaluation reflection

---

## Final Project Options

Students select or propose a project demonstrating multiple course techniques:

1. **AI Director System** - L4D-style pacing manager with stress tracking and dynamic spawning
2. **Tactical Squad Coordinator** - GOAP-based squad with role assignment, cover usage, flanking
3. **Predator AI** - Single intelligent enemy with multi-sensory perception, 20+ behavior tree branches
4. **Open World NPC Scheduler** - NPCs with daily routines, need-driven behavior (utility AI), player interaction
5. **Procedural Dungeon with Encounters** - WFC generation + enemy placement using influence maps
6. **Stealth Game AI System** - Complete guard AI with vision, audio, awareness states, coordinated search
7. **Industry Partnership Project** - Real challenge from partner company (proposals provided before Week 8)
8. **Custom Proposal** - Approved alternative demonstrating 3+ course techniques

---

## Assessment Summary

| Component          | Weight | Items                       |
| ------------------ | ------ | --------------------------- |
| Coding Assignments | 30%    | 9 assignments (A0–A8)       |
| Chess Competition  | 20%    | Midterm tournament          |
| Quizzes            | 20%    | 12 quizzes (Q0–Q11)         |
| Final Project      | 20%    | Demo + Essay + Presentation |
| Attendance         | 10%    | Class participation         |

---

## Tech Stack

- **Language**: C++ with CMake (final project: language of choice)
- **Testing**: doctest
- **CI/CD**: GitHub Actions
- **Chess**: UCI and moves library provided
- **Textbook**: AI for Games, Third Edition (Millington)

---

### Late Submission Policy

Late submissions will incur a penalty of **1% deduction per day** up to a maximum of **25% of the total grade**. For example, a submission that is 1 week (7 days) late will receive a 7% penalty, resulting in a maximum possible grade of 93%, which still falls within the A range. This policy encourages timely submission while allowing flexibility for unforeseen circumstances. Please does not make my life miserable by submitting assignments on the finals week. I beg you.
