# Week 02 Readings - Advanced Behavior Trees & Decision Trees

---

## Required Readings

| #   | Reading                                                                                                                                                                                                       | Time   | Covers                                                                                 |
| --- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------ | -------------------------------------------------------------------------------------- |
| 1   | Ian Millington, **AI for Games (3rd Ed.)**, Chapter 5 (Decision-Making Systems: FSMs, Behavior Trees, Decision Trees, Utility AI) - ISBN 9781138483972                                                        | 60 min | Core BT concepts, composition patterns, FSM vs BT trade-offs, practical implementation |
| 2   | Anthony Francis, [Overcoming Pitfalls in BT Design](http://www.gameaipro.com/GameAIPro3/GameAIPro3_Chapter09_Overcoming_Pitfalls_in_Behavior_Tree_Design.pdf) (PDF) - Game AI Pro 3                           | 30 min | Production patterns, blackboard decoupling, architecture anti-patterns                 |
| 3   | Bill Merrill, [Building Utility Decisions into Your Existing BT](https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter10_Building_Utility_Decisions_into_Your_Existing_Behavior_Tree.pdf) (PDF) - Game AI Pro | 25 min | Hybrid BT/Utility architecture, dynamic priority selection                             |

**Focus while reading:**

- Millington Ch. 5: Sequence/Selector semantics, Running state, why BTs stay flat
- Francis: anti-patterns (god node, deep nesting), decoupling game state, subtrees/reuse
- Merrill: mixing utility with BT priorities (reinforces Selector/fallback patterns)

## Videos

| #   | Video                                                                                                                                      | Time   | Covers                                                     |
| --- | ------------------------------------------------------------------------------------------------------------------------------------------ | ------ | ---------------------------------------------------------- |
| A   | GDC 2006 - [Three States and a Plan: The AI of F.E.A.R.](https://www.gdcvault.com/play/1013282/Three-States-and-a-Plan) (Jeff Orkin)       | 45 min | GOAP vs BT trade-offs, action planning, tactical behaviors |
| B   | GDC 2018 - [Beyond Killzone: Creating New AI for Horizon Zero Dawn](https://www.gdcvault.com/play/1025010/Beyond-Killzone-Creating-New-AI) | 60 min | Scaling BTs for 25+ character types, open-world AI         |
| C   | GDC 2016 - [AI Behavior Editing and Debugging in The Division](https://www.gdcvault.com/play/1023382/AI-Behavior-Editing-and-Debugging)    | 30 min | Production BT debugging, Snowdrop engine tools             |

## Documentation & Tutorials

| #   | Resource                                                                                                                                       | Time   | Covers                                                         |
| --- | ---------------------------------------------------------------------------------------------------------------------------------------------- | ------ | -------------------------------------------------------------- |
| 1   | [Unreal Engine: Behavior Tree Overview](https://dev.epicgames.com/documentation/en-us/unreal-engine/behavior-tree-in-unreal-engine---overview) | 20 min | **Event-driven BTs**, blackboard observers, **abort modes**    |
| 2   | [BehaviorTree.CPP: Tutorial Basics](https://www.behaviortree.dev/docs/tutorial-basics/tutorial_01_first_tree)                                  | 25 min | Type-safe blackboards, input/output ports, subtree composition |
| 3   | [BehaviorTree.CPP: ReactiveSequence vs Sequence](https://www.behaviortree.dev/docs/tutorial-basics/tutorial_04_sequence)                       | 15 min | Reactive evaluation vs remembering running child index         |

**Quiz focus while reading:**

- Unreal BT Overview: event-driven BTs, observers, abort modes
- BehaviorTree.CPP: decorators (inverter/repeat/cooldown), composing subtrees
- Reactive vs standard Sequence: why and how to resume a running child

## Optional Deep Dive

| Resource                                                                                                                                                                                    | Time   | Focus                                                               |
| ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------ | ------------------------------------------------------------------- |
| Ian Millington, **AI for Games (3rd Ed.)**, Chapter 7.6 (Decision Tree Learning) - ISBN 9781138483972                                                                                       | 40 min | ID3 algorithm, information gain, entropy, automatic tree generation |
| GDC 2014 - [The Last of Us: Human Enemy AI](https://www.gdcvault.com/play/1020338/The-Last-of-Us-Human) (Travis McIntosh)                                                                   | 60 min | Behavior stacks, post selectors, real-time level analysis           |
| GDC 2023 - [Preparing AI Systems for God of War Ragnarok](https://www.gdcvault.com/play/1028840/Preparing-AI-Systems-for-God)                                                               | 55 min | Migration from Lua to BTs, enhanced awareness systems               |
| Sagredo-Olivenza et al., [Trained Behavior Trees](https://ieeexplore.ieee.org/document/8116624) - IEEE Transactions on Games                                                                | -      | ML-generated BTs from player demonstrations                         |
| Robertson & Watson, [Building BTs from Observations in RTS Games](https://www.researchgate.net/publication/308809078_Building_behavior_trees_from_observations_in_real-time_strategy_games) | -      | Motif-finding for automatic BT extraction                           |

## Code Study (Optional)

| Repository                                                           | Language | Focus                                               |
| -------------------------------------------------------------------- | -------- | --------------------------------------------------- |
| [BehaviorTree.CPP](https://github.com/BehaviorTree/BehaviorTree.CPP) | C++      | Production-grade implementation, Groot2 editor      |
| [NPBehave](https://github.com/meniku/NPBehave)                       | C#/Unity | Event-driven architecture, shared blackboards       |
| [py_trees](https://github.com/splintered-reality/py_trees)           | Python   | Rigorous blackboard design, debugging introspection |

---

- **Study order (Required):** Millington Ch. 5 → Pitfalls PDF → F.E.A.R. video → Utility Decisions PDF → Unreal docs → Division video
- **Optional ML Deep Dive:** Millington Ch. 7.6 → Trained BTs paper → RTS BT extraction paper
