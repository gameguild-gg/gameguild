# Week 13 Readings — Influence Maps & Tactical Position Evaluation

---

## Required Readings

| #   | Reading                                                                                                                                                                                                                        | Time   | Covers                                                                                                                                   |
| --- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------ | ---------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | Game AI Pro 2 Ch. 29, Mike Lewis, [Escaping the Grid: Infinite-Resolution Influence Mapping](http://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter29_Escaping_the_Grid_Infinite-Resolution_Influence_Mapping.pdf)             | 15 min | Core influence map concepts: value propagation, moving beyond fixed grids, continuous-space influence, resolution trade-offs             |
| 2   | Game AI Pro 2 Ch. 30, Dave Mark, [Modular Tactical Influence Maps](http://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter30_Modular_Tactical_Influence_Maps.pdf)                                                               | 20 min | Modular influence map architecture: composable map layers, decay functions, update frequencies, combining layers for tactical decisions  |
| 3   | Game AI Pro Ch. 26, Matthew Jack, [Tactical Position Selection: An Architecture and Query Language](http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter26_Tactical_Position_Selection_An_Architecture_and_Query_Language.pdf) | 15 min | Query architecture for evaluating tactical positions: cover quality scoring, sight-line analysis, position ranking by weighted criteria  |
| 4   | Game AI Pro Ch. 27, Daniel Brewer, [Tactical Pathfinding on a NavMesh](http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter27_Tactical_Pathfinding_on_a_NavMesh.pdf)                                                           | 15 min | Combining pathfinding with tactical awareness: threat-weighted routes, exposure minimization, navmesh annotation for tactical properties |
| 5   | Game AI Pro 2 Ch. 31, [Spatial Reasoning for Strategic Decision Making](http://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter31_Spatial_Reasoning_for_Strategic_Decision_Making.pdf)                                          | 15 min | Strategic-level spatial queries: territory evaluation, resource control assessment, front-line detection, strategic map analysis         |

**Focus while reading:**

- Influence Mapping Foundations (Reading 1): This chapter challenges the assumption that influence maps must be grid-based. Focus on how **value propagation** works — influence radiates outward from sources (units, buildings, objectives) and decays with distance. Understand the trade-off between grid resolution and computational cost, and how continuous-space approaches can eliminate grid artifacts. This is your conceptual foundation for everything that follows.
- Modular Influence Maps (Reading 2): Dave Mark presents the key architectural insight: influence maps should be **composable layers**, not a single monolithic grid. Focus on how separate layers (threat, territory, resources, visibility) can be combined through weighted sums, products, or thresholds to answer complex tactical questions like "where is safe AND has good line of sight?" Pay special attention to **decay functions** (linear, exponential, inverse-square) and how update frequency affects responsiveness vs. performance.
- Tactical Position Selection (Reading 3): This chapter introduces a **query language** for evaluating positions — instead of hardcoding "find cover," you express queries like "find a position within 10m that has cover from the target AND line of sight to allies." Focus on how positions are scored by weighted criteria and how this architecture lets designers create new tactical behaviors without code changes. This connects influence maps to actionable AI decisions.
- Tactical Pathfinding (Reading 4): Standard A\* finds the shortest path; tactical pathfinding finds the **safest or most advantageous** path. Focus on how navmesh edges and polygons can be annotated with threat values from influence maps, so paths avoid exposed areas, prefer cover corridors, and account for enemy sight lines. Notice how this combines the spatial data from Readings 1–2 with the position evaluation from Reading 3.
- Strategic Spatial Reasoning (Reading 5): This reading zooms out from individual positions to **territory-level** reasoning. Focus on how influence maps answer strategic questions: "which regions do we control?", "where is the front line?", "which resource points are contested?" This is where influence maps become a strategic decision-making tool — the AI equivalent of a commander reading a battle map.

---

## Videos

### Core Influence Map Concepts

| #   | Video                                                                                                                          | Time   | Covers                                                                                                                                    |
| --- | ------------------------------------------------------------------------------------------------------------------------------ | ------ | ----------------------------------------------------------------------------------------------------------------------------------------- |
| A   | IADaveMark — [Modular Influence Map System (Imap)](https://www.youtube.com/watch?v=6RGquWxNock)                                | 8 min  | Live demonstration of Dave Mark's modular influence map implementation: layered maps, real-time propagation, visual debugging             |
| B   | AI and Games — [How AI Helps Players Achieve 'Tactical Clarity' in Gears Tactics](https://www.youtube.com/watch?v=63N4RCLmb5Y) | 15 min | Tactical AI in a turn-based game: cover evaluation, flanking detection, position scoring, threat assessment, AI-assisted player decisions |

**Focus while watching:**

- Modular Influence Map System (A): This is the visual companion to Reading 2. Watch how individual influence layers (threat, ownership, resource value) are rendered as heat maps, and how **combining layers** produces emergent tactical intelligence. Notice how changes propagate in real time — when a unit moves, the influence map updates and the AI's spatial awareness shifts accordingly. Pay attention to how modular design lets you add new map layers without rewriting existing logic.
- Gears Tactics Tactical Clarity (B): Focus on how the AI evaluates **tactical positions** for both enemies and player guidance. Notice the cover quality scoring system — full cover vs. half cover vs. exposed — and how the AI evaluates flanking opportunities by comparing the angle between an attacker's approach and the defender's cover orientation. This video demonstrates how influence maps and tactical queries combine to create intelligent combat positioning in a shipped title.

---

### Production Deep Dive

| #   | Video                                                                                                                      | Time   | Covers                                                                                                                                                                   |
| --- | -------------------------------------------------------------------------------------------------------------------------- | ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| C   | GDC — [Authored vs. Systemic: Finding a Balance for Combat AI in Uncharted 4](https://www.youtube.com/watch?v=G8W7EQKBgcg) | 25 min | Cover system architecture, spatial position evaluation, authored setpieces vs. systemic AI, Naughty Dog's approach to tactical combat positioning (watch the first half) |

**Focus while watching:**

- Uncharted 4 Combat AI (C): This GDC talk reveals how Naughty Dog balances **authored level design** with **systemic spatial reasoning**. Focus on how cover points are generated and scored — each position is evaluated for protection quality, sight lines to enemies, distance to the player, and flanking angles. Notice the tension between letting AI find optimal positions (systemic) and having designers control dramatic moments (authored). The first 25 minutes cover the spatial evaluation system; the rest addresses authored behaviors. This is tactical position evaluation in a production AAA game.

---

## Optional Deep Dive

| Resource                                                                                                                                                                                                | Time   | Focus                                                                                                                  |
| ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------ | ---------------------------------------------------------------------------------------------------------------------- |
| Game AI Pro 3 Ch. 24, [Being Where It Counts: Telling Paragon Bots Where to Go](http://www.gameaipro.com/GameAIPro3/GameAIPro3_Chapter24_Being_Where_It_Counts_Telling_Paragon_Bots_Where_to_Go.pdf)    | 20 min | MOBA bot spatial awareness: lane evaluation, objective prioritization, strategic positioning in Paragon                |
| Game AI Pro 3 Ch. 26, Eric Johnson, [Guide to Effective Auto-Generated Spatial Queries](http://www.gameaipro.com/GameAIPro3/GameAIPro3_Chapter26_Guide_to_Effective_Auto-Generated_Spatial_Queries.pdf) | 20 min | Automating spatial query generation: reducing designer burden, query optimization, scalable tactical AI                |
| LlamAcademy — [Hiding AI: Take Cover Behind World Geometry](https://www.youtube.com/watch?v=t9e2XBQY4Og)                                                                                                | 22 min | Practical Unity tutorial on cover system implementation: raycasting for cover detection, NavMesh integration, scoring  |
| Millington, _AI for Games_ (3rd ed.), Chapter 6: Tactical and Strategic AI                                                                                                                              | 60 min | Book reference — comprehensive coverage of influence maps, tactical waypoints, terrain analysis, strategic AI patterns |

---

## Key Concepts Summary

| Concept                  | Core Idea                                                                                                                                                     |
| ------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Influence Map**        | A spatial data structure overlaid on the game world that stores propagated values (threat, territory, resources) — the AI's "mental model" of the battlefield |
| **Value Propagation**    | Spreading influence values outward from sources (units, buildings, objectives) across map cells, creating gradients that encode spatial relationships         |
| **Decay Function**       | How influence diminishes with distance from its source — linear (constant falloff), exponential (rapid falloff), or inverse-square (physically realistic)     |
| **Update Frequency**     | How often influence values are recalculated; high frequency = responsive but expensive, low frequency = efficient but stale data                              |
| **Layered Maps**         | Multiple independent influence map layers (threat, territory, visibility, resources) that can be combined to answer complex tactical questions                |
| **Map Query**            | Asking spatial questions of influence data — "where is the safest path?", "what is the best attack position?", "which region is most contested?"              |
| **Tactical Position**    | A location evaluated for combat utility by scoring cover quality, sight lines, distance to objectives, flanking angles, and escape routes                     |
| **Cover Point**          | A position providing protection from enemy fire, scored by quality (full/half/none), directional protection, and accessibility                                |
| **Flanking Detection**   | Identifying positions that expose an enemy's unprotected side or rear by comparing the attacker's approach angle to the defender's cover orientation          |
| **Spatial Reasoning**    | AI using geometric and topological relationships between game-world positions to make informed tactical and strategic decisions                               |
| **Territory Control**    | Influence-based ownership of map regions — tracked by propagating friendly vs. enemy influence and comparing values at each point                             |
| **Tactical Pathfinding** | Finding routes weighted by tactical factors (threat exposure, cover availability, sight-line risk) rather than just distance or traversal cost                |

---

**Study order:** Influence Map Foundations (Reading 1) → Modular Influence Maps (Reading 2) → Imap Demo video (A) → Tactical Position Selection (Reading 3) → Tactical Pathfinding (Reading 4) → Gears Tactics video (B) → Strategic Spatial Reasoning (Reading 5) → Uncharted 4 Deep Dive (C)

**Total required time:** ~2h (readings: 80 min, videos: 23 min, production deep dive: 25 min)
