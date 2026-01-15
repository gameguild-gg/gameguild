# Week 03 Readings - Utility AI

---

## Required Readings

| #   | Reading                                                                                                                                                                                           | Time   | Covers                                                                     |
| --- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------ | -------------------------------------------------------------------------- |
| 1   | Ian Millington, **AI for Games (3rd Ed.)**, Chapter 5.4 (Utility-Based Systems) - ISBN 9781138483972                                                                                              | 30 min | Utility functions, action selection, combining utilities, dual utility     |
| 2   | David "Rez" Graham, [An Introduction to Utility Theory](http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter09_An_Introduction_to_Utility_Theory.pdf) (PDF) - Game AI Pro                         | 35 min | Response curves, considerations, normalization, infinite axis architecture |
| 3   | Mike Lewis, [Choosing Effective Utility-Based Considerations](http://www.gameaipro.com/GameAIPro3/GameAIPro3_Chapter13_Choosing_Effective_Utility-Based_Considerations.pdf) (PDF) - Game AI Pro 3 | 25 min | Consideration design, input selection, curve tuning strategies             |

**Focus while reading:**

- Millington: utility fundamentals, expected utility, action selection mechanisms
- Graham: response curve types (linear, polynomial, logistic, sine), IAUS architecture, score combination
- Lewis: practical consideration design, avoiding common pitfalls, debugging approaches

---

## Videos

| #   | Video                                                                                                                                                        | Time   | Covers                                                         |
| --- | ------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------ | -------------------------------------------------------------- |
| A   | GDC 2010 - [Improving AI Decision Modeling Through Utility Theory](https://www.gdcvault.com/play/1012410/Improving-AI-Decision-Modeling-Through) (Dave Mark) | 55 min | Core utility concepts, response curves, The Sims as case study |
| B   | GDC 2012 - [AI in Skyrim: Utility-Based Systems for Dynamic Decision-Making](https://www.gdcvault.com/play/1015677/AI-in-Skyrim-Utility-Based) (Emil Pagliarulo) | 40 min | Skyrim AI architecture, utility for NPC behaviors               |

---

## Interactive Resources

| #   | Resource                                                               | Time   | Covers                                   |
| --- | ---------------------------------------------------------------------- | ------ | ---------------------------------------- |
| 1   | [Desmos: Response Curve Playground](https://www.desmos.com/calculator) | 15 min | Experiment with curve equations yourself |

**Hands-on task:** In Desmos, plot these and observe behavior:

- Linear: `y = x`
- Quadratic: `y = x^2` and `y = 1-(1-x)^2`
- Logistic: `y = 1/(1 + e^(-10*(x-0.5)))`
- Sine: `y = 0.5 * sin(2πx - π/2) + 0.5`

---

## Optional Deep Dive

| Resource                                                                                                                                                    | Time   | Focus                                                              |
| ----------------------------------------------------------------------------------------------------------------------------------------------------------- | ------ | ------------------------------------------------------------------ |
| GDC 2015 - [Building a Better Centaur: AI at Massive Scale](https://www.gdcvault.com/play/1021848/Building-a-Better-Centaur-AI) (Dave Mark & Mike Lewis)    | 60 min | Guild Wars 2 combat AI, scaling utility systems                    |
| GDC 2012 - [Embracing the Dark Art of Mathematical Modeling](https://www.gdcvault.com/play/1015683/Embracing-the-Dark-Art-of) (Dave Mark & Kevin Dill)      | 60 min | Advanced curve design, real-world modeling techniques              |
| GDC 2013 - [Architecture Tricks: Managing Behaviors](https://gdcvault.com/play/1018040/Architecture-Tricks-Managing-Behaviors-in) (Dave Mark)               | 60 min | Dual utility, bucket selection, complexity management              |
| Kevin Dill, [Dual-Utility Reasoning](http://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter03_Dual-Utility_Reasoning.pdf) (PDF) - Game AI Pro 2             | 15 min | Bucket systems, priority handling, action selection                |
| Richard Evans, [Modeling Individual Personalities in The Sims 3](https://www.gdcvault.com/play/1012450/Modeling-Individual-Personalities-in-The) - GDC 2010 | 60 min | The Sims 3 AI lead on needs systems, traits, and emergent behavior |

---

## Code Study (Optional)

| Repository                                                                | Language | Focus                                       |
| ------------------------------------------------------------------------- | -------- | ------------------------------------------- |
| [Curvature](https://github.com/apoch/curvature)                           | C#       | Mike Lewis's utility AI editor (IAUS-based) |
| [ECS-IAUS-system](https://github.com/DreamersIncStudios/ECS-IAUS-sytstem) | Unity/C# | IAUS implementation using Unity DOTS        |

::: note

Most production utility systems are proprietary. Focus on the concepts from readings rather than specific implementations.

:::

---

**Study order:** Millington 5.4 → Graham PDF → 2010 GDC Talk → Lewis PDF → Desmos curves

**Total required time:** ~2h 10min (readings: 1h 30min, video: 55min)

