# Week 12 Readings — Multi-Agent Coordination

---

## Required Readings

| #   | Reading                                                                                                                                                                                                                                                                       | Time   | Covers                                                                                                                                      |
| --- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------ | ------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | Game AI Pro 3 Ch. 8, Kevin Dill, Christopher Dragert, [Modular AI](http://www.gameaipro.com/GameAIPro3/GameAIPro3_Chapter08_Modular_AI.pdf)                                                                                                                                   | 15 min | Modular agent architecture: composable AI modules, separation of concerns, inter-module communication, practical multi-agent patterns       |
| 2   | Game AI Pro Online 2021 Ch. 4, [Knowledge is Power: An Overview of AI Knowledge Representation in Games](http://www.gameaipro.com/GameAIProOnlineEdition2021/GameAIProOnlineEdition2021_Chapter04_Knowledge_is_Power_an_Overview_of_AI_Knowledge_Representation_in_Games.pdf) | 20 min | Knowledge representation survey: blackboard systems, knowledge queries, world state models, sensory systems, game AI coordination patterns  |
| 3   | Robert Nystrom, Game Programming Patterns, [Event Queue](https://gameprogrammingpatterns.com/event-queue.html)                                                                                                                                                                | 15 min | Event queue pattern: decoupling senders from receivers, central event bus, aggregation, pub/sub in game engines, ring buffer implementation |
| 4   | Game AI Pro Ch. 5, Kevin Dill, [Structural Architecture — Common Tricks of the Trade](http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter05_Structural_Architecture_Common_Tricks_of_the_Trade.pdf)                                                                          | 15 min | Blackboard as a standard game AI tool, polling vs event-driven systems, structural patterns for agent communication                         |
| 5   | Game AI Pro Ch. 29, Straatman et al., [Hierarchical AI for Multiplayer Bots in Killzone 3](http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter29_Hierarchical_AI_for_Multiplayer_Bots_in_Killzone_3.pdf)                                                                     | 20 min | Case study: hierarchical AI layers (strategic → tactical → individual), squad coordination, role assignment, Killzone 3 bot architecture    |

**Focus while reading:**

- Modular AI (Reading 1): This chapter introduces how to decompose game AI into **composable modules** that can be mixed, matched, and reused. Focus on how modules communicate — this is the game AI take on multi-agent systems. Understand how separation of concerns lets each AI module (perception, decision-making, action) operate independently while still coordinating through shared interfaces.
- Knowledge Representation (Reading 2): This is your deep dive into how game agents **share knowledge**. Focus on the blackboard architecture — a shared data store where specialist modules (knowledge sources) read and write, coordinated by a control shell. Understand how knowledge queries ("where is nearest cover?") and knowledge posting ("enemy spotted at X") enable agent coordination without direct coupling. This survey covers the patterns used in production game AI.
- Event Queue (Reading 3): Robert Nystrom walks through **event-driven communication** in game engines — central event buses, decoupling senders from receivers in time and identity, and aggregating requests. Focus on the pub/sub relationship: publishers emit events ("enemy died"), subscribers react to them, and a queue sits between them. This is the practical implementation of how game agents communicate asynchronously.
- Structural Architecture (Reading 4): This chapter by Kevin Dill covers the practical game AI perspective on blackboards, polling systems, and event-driven architectures. Focus on the **trade-offs** between polling (simple, predictable) and event-driven (responsive, complex) approaches. This reading connects the theoretical patterns from Readings 2–3 to actual game implementations.
- Killzone 3 Hierarchical AI (Reading 5): This is your primary case study. Follow the **three-layer hierarchy**: the strategic layer assigns objectives, the tactical layer coordinates squads, and the individual layer handles NPC-level decisions. Notice how role assignment dynamically adapts — an NPC might switch from "suppressor" to "flanker" based on the tactical situation. This is multi-agent coordination in a shipped AAA title.

---

## Videos

### Core Multi-Agent Concepts

| #   | Video                                                                                        | Time   | Covers                                                                                  |
| --- | -------------------------------------------------------------------------------------------- | ------ | --------------------------------------------------------------------------------------- |
| A   | Guerrilla Games — [Killzone 2 AI Demo](https://www.youtube.com/watch?v=7oWKCLdsGTE)                              | 5 min  | Killzone 2 hierarchical AI system in action: squad coordination, tactical behaviors     |
| B   | AI and Games — [The AI of The Last of Us Part II](https://www.youtube.com/watch?v=BghECmeLda0) | 20 min | Companion AI coordination, buddy positioning, shared knowledge, role-based behaviors    |

**Focus while watching:**

- The AI of Killzone (A): Watch how the hierarchical AI layers communicate — the strategic layer doesn't micromanage individual NPCs; it sets goals that the tactical layer decomposes into squad-level actions. Pay attention to how attack tokens and time-slot scheduling prevent unrealistic "everyone rush the player" behavior. This is the visual companion to Reading 5.
- Buddy AI of The Last of Us (B): Focus on how Ellie's AI uses a **knowledge-sharing** system — she needs to know where Joel (the player) is looking, what enemies he's engaged with, and where safe positions are. Notice the role assignment: sometimes Ellie provides cover fire, sometimes she hides. This is multi-agent coordination from the companion perspective rather than the enemy squad perspective.

---

### Production Deep Dive

| #   | Video                                                                                                                                                                                                         | Time   | Covers                                                                           |
| --- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------ | -------------------------------------------------------------------------------- |
| C   | Game AI Pro 2 Ch. 2, Jeff Orkin, [Combat Dialogue in FEAR: The Illusion of Communication](http://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter02_Combat_Dialogue_in_FEAR_The_Illusion_of_Communication.pdf) | 15 min | How F.E.A.R. creates the illusion of squad communication using dialogue triggers |

---

## Optional Deep Dive

| Resource                                                                                                                                                                                                                                   | Time   | Focus                                                                                                               |
| ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------ | ------------------------------------------------------------------------------------------------------------------- |
| Game AI Pro Ch. 28, Michael Dawe, [Beyond the Kung-Fu Circle: A Flexible System for Managing NPC Attacks](http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter28_Beyond_the_Kung-Fu_Circle_A_Flexible_System_for_Managing_NPC_Attacks.pdf) | 20 min | Token/slot-based attack coordination: attack tokens, speak tokens, the Kung-Fu Circle problem, flexible systems     |
| Game AI Pro Ch. 34, Phil Carlisle, [A Simple and Robust Knowledge Representation System](http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter34_A_Simple_and_Robust_Knowledge_Representation_System.pdf)                                   | 20 min | Knowledge queries and posting in game AI, structured knowledge representation for agent communication               |
| Game AI Pro 2 Ch. 35, Max Dyckhoff, [Ellie: Buddy AI in The Last of Us](http://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter35_Ellie_Buddy_AI_in_The_Last_of_Us.pdf)                                                                     | 25 min | Companion AI in production: buddy positioning, player-awareness, role assignment, shared world model                |
| Refactoring Guru, [Observer Pattern](https://refactoring.guru/design-patterns/observer)                                                                                                                                                    | 10 min | Observer/event-subscriber pattern: subscription mechanism, publisher-subscriber relationships, runtime registration |
| Millington, _AI for Games_ (3rd ed.), Chapter 6: Tactical and Strategic AI                                                                                                                                                                 | 60 min | Book reference — comprehensive coverage of blackboards, coordination, influence maps, tactical reasoning            |

---

## Key Concepts Summary

| Concept                      | Core Idea                                                                                                                                           |
| ---------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Multi-Agent System (MAS)** | Multiple autonomous agents with local views that coordinate to solve problems no single agent could solve alone                                     |
| **Blackboard Architecture**  | A shared knowledge base (blackboard) read/written by specialist knowledge sources, with a control shell moderating access                           |
| **Knowledge Source**         | A specialist module that monitors the blackboard and contributes partial solutions when its expertise matches the current state                     |
| **Control Shell**            | The moderator component that determines which knowledge source acts next, preventing conflicts and ensuring coherent problem-solving                |
| **Publish-Subscribe**        | A messaging pattern where publishers categorize messages by topic; subscribers receive only messages matching their registered interests            |
| **Token System**             | A resource-limiting coordination pattern — attack tokens, speak tokens, etc. — that controls how many agents can perform a specific action at once  |
| **Kung-Fu Circle**           | The unrealistic scenario where enemies politely attack one at a time; solved by token/slot systems that manage concurrent agent actions             |
| **Hierarchical AI**          | A multi-layer architecture where higher layers (strategic) set goals that lower layers (tactical → individual) decompose and execute                |
| **Squad Coordination**       | Higher-level AI that assigns roles and coordinates tactics for groups of agents, as seen in Killzone 2/3's tactical layer                           |
| **Role Assignment**          | Dynamic allocation of tactical roles (flanker, suppressor, scout, medic) to squad members based on the current situation                            |
| **Time-Slot Scheduling**     | Coordinating agent actions across time so that NPCs take turns performing visible actions (attacking, speaking) to create natural-looking behavior  |
| **Knowledge Query**          | An agent requesting specific information from the shared knowledge base — e.g., "where is the nearest cover position?"                              |
| **Knowledge Posting**        | An agent publishing observations or facts to the shared knowledge base for other agents to read — e.g., "enemy spotted at position X"               |
| **Buddy AI**                 | A companion NPC that must coordinate closely with the player — sharing knowledge, avoiding blocking, and adapting its role to complement the player |

---

**Study order:** Modular AI (Reading 1) → Knowledge Representation (Reading 2) → Event Queue (Reading 3) → Structural Architecture (Reading 4) → Killzone 3 Hierarchical AI (Reading 5) → AI and Games Killzone video (A) → Buddy AI video (B)

**Total required time:** ~2h (readings: 85 min, videos: 35 min, combat dialogue reading: 15 min)
