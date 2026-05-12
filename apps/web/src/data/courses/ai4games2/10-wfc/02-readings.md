# Week 10 Readings - Wave Function Collapse

---

## Required Readings

| #   | Reading                                                                                                                                            | Time   | Covers                                                                                                       |
| --- | -------------------------------------------------------------------------------------------------------------------------------------------------- | ------ | ------------------------------------------------------------------------------------------------------------ |
| 1   | Boris the Brave, [Wave Function Collapse Explained](https://www.boristhebrave.com/2020/04/13/wave-function-collapse-explained/)                    | 20 min | Constraint programming basics, WFC as constraint solving, Sudoku analogy, observation-propagation cycle      |
| 2   | Maxim Gumin, [WaveFunctionCollapse README](https://github.com/mxgmn/WaveFunctionCollapse)                                                         | 15 min | Original algorithm description, tiled model, overlapping model, entropy heuristic, local similarity          |
| 3   | Stephen Sherratt (gridbugs), [Procedural Generation with WFC](https://gridbugs.org/wave-function-collapse/)                                        | 30 min | Deep technical walkthrough in Rust: image processor, core internals, entropy, propagation, enabler counts    |
| 4   | Boris the Brave, [Wave Function Collapse Tips and Tricks](https://www.boristhebrave.com/2020/02/08/wave-function-collapse-tips-and-tricks/)        | 15 min | Practical tileset design, marching cubes, path constraints, variety, avoiding homogeneous output             |

**Focus while reading:**

- Boris WFC Explained: Understand the constraint satisfaction framing — WFC is a **constraint solver** where the constraints are adjacency rules, not a generative black box. Follow the full Sudoku analogy carefully; it explains everything about how observation + backtracking work.
- mxgmn README: Look at the visual examples first to build intuition about what the algorithm produces. Then read the algorithmic description paying attention to (C1) local patterns from input → output and the two models (simple tiled vs. overlapping).
- gridbugs deep dive: This is the most technically precise explanation available. Focus on the `CoreCell`, `FrequencyHints`, and the enabler-count propagation data structure. The entropy formula derivation (weighted variant) is key.
- Boris Tips and Tricks: Focus on tileset design principles (marching cubes strategy), avoiding disconnected regions (path constraint), and how real games (Bad North, Caves of Qud) solved these problems in production.

---

## Videos

### Core WFC Concepts

| #   | Video                                                                                                                  | Time   | Covers                                                                                                                   |
| --- | ---------------------------------------------------------------------------------------------------------------------- | ------ | ------------------------------------------------------------------------------------------------------------------------ |
| A   | Maxim Gumin - [WaveFunctionCollapse Demo](https://youtu.be/DOQTr2Xmlz0)                                                | 5 min  | Official visual demo of the algorithm running on multiple tilesets — watch this first to see what you're implementing   |
| B   | Martin Donald - [Superpositions, Sudoku, the Wave Function Collapse algorithm](https://www.youtube.com/watch?v=qRnUBiTJ66Y) | 15 min | Clear visual explanation using superposition and constraint propagation; connects to quantum mechanics name origin       |
| C   | Brian Bucklew - [End-to-End Procedural Generation in Caves of Qud](https://www.youtube.com/watch?v=AdCgi9E90jw)        | 25 min | GDC 2019: real production WFC pipeline, multi-pass generation, handling connectedness, biome variation, failure recovery |

**Focus while watching:**

- Demo video (A): Notice the wave of colored superpositions collapsing into definite tiles — the visual progression demonstrates entropy-guided selection perfectly.
- Martin Donald (B): The "superposition collapsing" metaphor + the Sudoku-like propagation — this video builds the best conceptual mental model before you read any code.
- Brian Bucklew (C): Focus on the practical engineering challenges: how WFC alone produces disconnected maps, why they run WFC in passes with different settings for different zones, and how they ensure path connectivity.

---

### WFC in Production Games

| #   | Video                                                                                                                              | Time   | Covers                                                                                         |
| --- | ---------------------------------------------------------------------------------------------------------------------------------- | ------ | ---------------------------------------------------------------------------------------------- |
| D   | Oskar Stålberg - [WFC Applied to Level Generation in Bad North](https://www.youtube.com/watch?v=0bcZb-SsnrA)                       | 30 min | Everything Procedural Conference 2018: WFC for 3D island generation, navigability heuristic    |
| E   | Oskar Stålberg - [Townscaper: Town Building at Konsoll 2021](https://www.youtube.com/watch?v=5xrRTOikBBg)                          | 45 min | WFC + marching cubes on irregular grids — the architecture behind Townscaper                   |
| F   | AI and Games - [Oskar Stålberg on Townscaper](https://www.youtube.com/watch?v=_1fvJ5sHh6A)                                         | 20 min | Accessible interview-style overview of how Townscaper uses WFC and mixed initiative generation |

---

## Interactive Resources

| #   | Resource                                                                                           | Time   | Covers                                                                                            |
| --- | -------------------------------------------------------------------------------------------------- | ------ | ------------------------------------------------------------------------------------------------- |
| 1   | Marian Kleineberg - [Infinite WFC City Generator](https://marian42.itch.io/wfc)                   | 15 min | Walk through a procedurally generated infinite city — see WFC in a real game context             |
| 2   | Maxim Gumin - [WFC GUI Tool](https://exutumno.itch.io/wavefunctioncollapse)                        | 15 min | Interactive desktop GUI; run the overlapping and tiled models on different example inputs         |

**Hands-on task:** Using the Infinite City Generator:

- Walk around for a few minutes and identify places where the generation is seamless vs. where you notice repetition
- Try to identify which constraints could produce the road network layout you observe
- Think about what a "contradiction" might look like in this context — what kind of configurations would be impossible to complete?
- After reading the gridbugs article, revisit: can you identify the Observation → Propagation cycle in the real-time generation as you teleport to new areas?

---

## Optional Deep Dive

| Resource                                                                                                                                                                  | Time   | Focus                                                                                                          |
| ------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------ | -------------------------------------------------------------------------------------------------------------- |
| Brian Bucklew - [WFC and Tile-Based Generation in Caves of Qud](https://www.youtube.com/watch?v=fnFj3dOKcIQ) (Roguelike Celebration 2019)                                 | 30 min | Companion talk to the GDC version — more detail on the multi-zone approach and failure mode analysis           |
| Oskar Stålberg - [Townscaper at EPC 2021](https://www.youtube.com/watch?v=NOJYZYqY6_M)                                                                                   | 35 min | Deep technical dive into WFC on irregular sphere grids and the mixed-initiative design workflow of Townscaper  |
| Oskar Stålberg - [Townscaper at SGC 2021](https://www.youtube.com/watch?v=Uxeo9c-PX-w)                                                                                   | 30 min | Player-facing perspective and design philosophy behind Townscaper's generative systems                         |
| Boris the Brave, [DeBroglie Documentation](https://boristhebrave.github.io/DeBroglie/)                                                                                    | 30 min | Full API and feature reference: non-local constraints, backtracking, hex/3D topology support                   |
| Paul Merrell, [Model Synthesis](https://paulmerrell.org/model-synthesis/)                                                                                                 | 20 min | The algorithm WFC is based on (2007) — understanding the genealogy clarifies the AC-4 constraint solver inside |
| Isaac Karth & Adam M. Smith, [WFC is Constraint Solving in the Wild](https://adamsmith.as/papers/wfc_is_constraint_solving_in_the_wild.pdf) (FDG 2017 Workshop Paper)    | 20 min | Academic framing of WFC as an ASP/constraint problem, formal analysis of heuristics and backtracking           |
| Marian Kleineberg - [WFC Infinite City: Implementation Article](https://marian42.de/article/wfc/)                                                                         | 15 min | Practical write-up on how the Infinite City Generator handles adjacency specification and backtracking         |

---

## Code Study (Optional)

| Repository                                                             | Language | Focus                                                                                           |
| ---------------------------------------------------------------------- | -------- | ----------------------------------------------------------------------------------------------- |
| [WaveFunctionCollapse](https://github.com/mxgmn/WaveFunctionCollapse) | C#       | Original reference implementation — clean, well-structured, two models in one codebase          |
| [DeBroglie](https://github.com/BorisTheBrave/DeBroglie)                | C#       | Production-quality library with backtracking, non-local constraints, hexagonal and 3D topologies |
| [fast-wfc](https://github.com/math-fehr/fast-wfc)                      | C++      | Optimized implementation (10× speedup over original) — study the enabler count data structure  |
| [wfc_python](https://github.com/ikarth/wfc_python)                     | Python   | Readable Python port — good for understanding algorithm flow without C++ noise                  |
| [infinite city generator](https://github.com/marian42/wavefunctioncollapse) | C# (Unity) | Full Unity game using WFC — study the runtime generation and adjacency setup                 |

---

## Key Concepts Summary

| Concept                        | Core Idea                                                                                                                     |
| ------------------------------ | ----------------------------------------------------------------------------------------------------------------------------- |
| **Wave Function Collapse**     | A constraint satisfaction algorithm that fills a grid with tiles such that adjacency rules are everywhere satisfied           |
| **Tiled Model**                | User-specifies which tile types may appear adjacent to each other (hand-crafted adjacency table)                              |
| **Overlapping Model**          | Adjacency rules are inferred automatically by extracting every NxN pattern from a sample input image                         |
| **Observation**                | Select the cell with lowest entropy and "collapse" it — assign it one tile chosen from its remaining possibilities            |
| **Propagation**                | After collapsing a cell, remove any tiles from neighboring cells that are no longer permitted by adjacency rules; cascade     |
| **Entropy (Shannon)**          | $H = -\sum p_i \log p_i$ — cells with fewer remaining options have lower entropy; collapse those first to reduce contradictions |
| **Minimum Entropy Heuristic**  | Always collapse the cell with the lowest entropy (most constrained); mimics how humans solve Sudoku                           |
| **Enabler Count**              | For each cell/tile/direction triple, count how many compatible tiles exist in the neighboring cell; drop to 0 → remove tile   |
| **Contradiction**              | A cell with zero remaining possibilities; WFC cannot continue and must restart (or backtrack)                                 |
| **Backtracking**               | Roll back to a saved state when a contradiction is reached; WFC implementations vary in whether they support this             |
| **Local Similarity (C1)**      | Every NxN patch in the output must appear somewhere in the input — this is the core generative guarantee                     |
| **Non-Local Constraints**      | WFC alone cannot ensure global structure (connected paths, reachable areas); must be added explicitly (DeBroglie, multi-pass) |

---

**Study order:** WFC Demo video (A) → Martin Donald video (B) → Boris "WFC Explained" → mxgmn README → Brian Bucklew GDC (C) → gridbugs deep dive → Boris "Tips and Tricks"

**Total required time:** ~2h 5 min (readings: 80 min, videos: 45 min, interactive: 30 min)
