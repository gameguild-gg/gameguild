# Week 11 Readings - Goal-Oriented Action Planning (GOAP)

---

## Required Readings

| #   | Reading                                                                                                                                                                              | Time   | Covers                                                                                                                             |
| --- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------ | ---------------------------------------------------------------------------------------------------------------------------------- |
| 1   | Tommy Thompson, [Building the AI of F.E.A.R. with Goal Oriented Action Planning](https://www.gamedeveloper.com/design/building-the-ai-of-f-e-a-r-with-goal-oriented-action-planning) | 20 min | Complete GOAP overview: automated planning history, STRIPS origins, 3-state FSM, goals, actions, preconditions/effects, replanning |
| 2   | Wikipedia, [Stanford Research Institute Problem Solver (STRIPS)](https://en.wikipedia.org/wiki/Stanford_Research_Institute_Problem_Solver)                                           | 10 min | STRIPS formalism ⟨P,O,I,G⟩, preconditions and postconditions, state representation, complexity (PSPACE-complete)                   |
| 3   | Wikipedia, [Automated Planning and Scheduling](https://en.wikipedia.org/wiki/Automated_planning_and_scheduling)                                                                      | 15 min | Forward (progressive) vs backward (regressive) search, state-space vs plan-space, PDDL language, classical planning assumptions    |
| 4   | CrashKonijn, [GOAP Theory](https://goap.crashkonijn.com/)                                                                                                                            | 15 min | GOAP concepts in practice: world state as key-value pairs, goals, actions, sensor system, planning cycle                           |
| 5   | Wikipedia, [F.E.A.R. (video game) § AI](https://en.wikipedia.org/wiki/F.E.A.R._(video_game)#AI)                                                                                    | 15 min | Case study: 70 goals, 120 actions, 3-state FSM (GoTo/Animate/UseSmartObject), A\* for action planning, squad coordination          |

**Focus while reading:**

- Tommy Thompson article: This is your primary reading. Understand the key insight — GOAP uses **A\* search through action space** rather than spatial space. Follow how the 3-state FSM (GoTo, Animate, UseSmartObject) replaces the 80+ state FSMs of earlier games. Pay close attention to how plans are validated, invalidated, and replanned at runtime.
- STRIPS: Understand the formal representation — a planning problem is a quadruple ⟨P,O,I,G⟩ where P is propositions, O is operators (actions), I is initial state, G is goal state. This is the theoretical foundation GOAP builds on.
- Automated Planning: Focus on the distinction between **forward (progressive) planning** (start → goal) and **backward (regressive) planning** (goal → start). GOAP in F.E.A.R. uses regressive search. Also note the classical planning assumptions: deterministic, fully observable, finite.
- CrashKonijn GOAP Theory: Focus on the practical implementation patterns — how world state is represented as key-value pairs, how actions declare preconditions and effects, and how the planner selects the cheapest valid plan.
- F.E.A.R. Wikipedia AI section: Read how the system works end-to-end in a real game. Notice how verbal squad commands are actually an illusion — the AI decides to flank first, then triggers the audio command, making it appear as if NPCs are following orders.

---

## Videos

### Core GOAP Concepts

| #   | Video                                                                                      | Time   | Covers                                                                                                                        |
| --- | ------------------------------------------------------------------------------------------ | ------ | ----------------------------------------------------------------------------------------------------------------------------- |
| A   | AI and Games - [Building the AI of F.E.A.R.](https://www.youtube.com/watch?v=PaOLBOuyswI)  | 30 min | Full GOAP walkthrough: automated planning, STRIPS, goals/actions/preconditions/effects, FEAR source code analysis, replanning |
| B   | Holistic 3D - [Goal Oriented Action Planning](https://www.youtube.com/watch?v=jUSrVF8mve4) | 15 min | Practical GOAP implementation tutorial with visual demonstrations of the planning process                                     |

**Focus while watching:**

- AI and Games (A): This is the video companion to Reading 1. Tommy Thompson walks through the actual F.E.A.R. C++ source code. Focus on how goals calculate priority, how actions are assigned to different enemy types via a database editor, and the discovery that rats cause unnecessary planning overhead because they keep replanning even after leaving the player's area.
- Holistic 3D (B): Watch for the practical implementation perspective — how to structure goals, actions, and world state in a game engine. This complements the theoretical readings with concrete visual examples.

---

### GOAP in Production

| #   | Video                                                                                                                                                                             | Time   | Covers                                                                                     |
| --- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------ | ------------------------------------------------------------------------------------------ |
| C   | GDC 2015 - [GOAP: Ten Years Old and No Fear!](https://www.youtube.com/watch?v=gm7K68663rA) ([GDC Vault](https://www.gdcvault.com/play/1022019/Goal-Oriented-Action-Planning-Ten)) | 60 min | AI Summit: GOAP in Shadow of Mordor, Tomb Raider debugging, data analytics for AI planning |

---

## Optional Deep Dive

| Resource                                                                                                                                                                                   | Time   | Focus                                                                                                         |
| ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------ | ------------------------------------------------------------------------------------------------------------- |
| AI and Games - [Facing Your F.E.A.R.](https://www.youtube.com/watch?v=BmOOrh5lq7o) (2014)                                                                                                  | 15 min | Earlier, more concise overview of the F.E.A.R. AI — good recap after studying the detailed version            |
| Wikipedia, [Hierarchical Task Network](https://en.wikipedia.org/wiki/Hierarchical_task_network)                                                                                            | 15 min | HTN planning: compound and primitive tasks, decomposition methods, SHOP2 planner, comparison with STRIPS/GOAP |
| Game AI Pro, [Exploring HTN Planners through Example](https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter12_Exploring_HTN_Planners_through_Example.pdf) (Ch. 12)                         | 30 min | Practical HTN walkthrough by example — how HTN decomposes high-level tasks into primitive actions             |
| Game AI Pro 2, [Optimizing Practical Planning for Game AI](https://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter13_Optimizing_Practical_Planning_for_Game_AI.pdf) (Ch. 13, Éric Jacopin) | 30 min | Performance analysis of the GOAP planner in F.E.A.R., including the rat overhead discovery and optimizations  |
| Jeff Orkin, [Three States and a Plan: The A.I. of F.E.A.R.](https://web.archive.org/web/20210809014746/http://alumni.media.mit.edu/~jorkin/gdc2006_orkin_jeff_fear.pdf) (GDC 2006, PDF)    | 30 min | The original GDC presentation by F.E.A.R.'s AI lead — primary source on the architecture                      |
| GDC 2015 - [GOAP: Ten Years Old and No Fear!](https://www.gdcvault.com/play/1022019/Goal-Oriented-Action-Planning-Ten) (Full talk)                                                         | 60 min | Full GDC AI Summit retrospective on GOAP by developers of Shadow of Mordor and Tomb Raider                    |

---

## Code Study (Optional)

| Repository                                                 | Language   | Focus                                                                                                       |
| ---------------------------------------------------------- | ---------- | ----------------------------------------------------------------------------------------------------------- |
| [crashkonijn/GOAP](https://github.com/crashkonijn/GOAP)    | C# (Unity) | Production GOAP library (1.7k+ stars), multi-threaded resolver, sensor system, well-documented              |
| [F.E.A.R. SDK 1.08](https://github.com/xfw5/Fear-SDK-1.08) | C++        | Original F.E.A.R. source code with GOAP implementation — goals, actions, planner, FSM (search for AI files) |

---

## Key Concepts Summary

| Concept                             | Core Idea                                                                                                                       |
| ----------------------------------- | ------------------------------------------------------------------------------------------------------------------------------- |
| **GOAP**                            | Goal-Oriented Action Planning — a STRIPS-based AI architecture where NPCs plan sequences of actions to satisfy goals at runtime |
| **World State**                     | A set of key-value pairs representing everything the planner needs to know about the current situation                          |
| **Action**                          | A discrete operation with preconditions (what must be true before), effects (how the world changes after), and a cost           |
| **Goal**                            | A desired world state condition with a priority; the planner searches for actions whose effects satisfy the goal                |
| **Planning as Search**              | A\* search through action space — actions are edges, world states are nodes, costs guide optimal plan selection                 |
| **Regressive Planning**             | Searching backward from the goal state toward the current state; F.E.A.R. uses this approach                                    |
| **Progressive Planning**            | Searching forward from the current state toward the goal; simpler but may explore more states                                   |
| **Cost Function**                   | Each action has a cost; the planner finds the cheapest sequence of actions that reaches the goal                                |
| **Runtime Replanning**              | When conditions change (enemy spotted, plan invalidated), the AI abandons its current plan and replans immediately              |
| **Three-State FSM**                 | F.E.A.R. reduced NPC behavior to 3 states (GoTo, Animate, UseSmartObject); A\* planning navigates between them                  |
| **STRIPS**                          | Stanford Research Institute Problem Solver (1971) — the formal planning system GOAP is derived from                             |
| **HTN (Hierarchical Task Network)** | An alternative planning approach that decomposes high-level tasks into subtasks; used in newer titles like Killzone 2           |
| **Preconditions/Effects**           | Preconditions gate when an action can execute; effects describe state changes — together they let A\* chain actions             |
| **Squad Coordination Illusion**     | In F.E.A.R., NPCs don't actually communicate — each plans independently; verbal commands are triggered after planning decisions |

---

**Study order:** STRIPS Wikipedia → Automated Planning Wikipedia → Tommy Thompson article (Reading 1) → AI and Games video (A) → CrashKonijn GOAP Theory → F.E.A.R. Wikipedia AI section → Holistic 3D video (B)

**Total required time:** ~2h (readings: 75 min, videos: 45 min)
