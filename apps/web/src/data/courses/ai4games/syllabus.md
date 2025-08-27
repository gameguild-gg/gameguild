# Artificial Intelligence

This course provides a technical introduction to the core concepts of artifical intelligence (AI). Students will be introduced to the history of AI, agents (agent architecture and multi-agent behavior), search (search space, uninformed and informed search, constraint satisfaction, game playing), knowledge representation (logical encoding of domain knowledge, logical reeasoning systems), planning (search over plan space, partial-order planning, practical planning), uncertainty and probability, learning (inductive learning, linear separators, decision trees, boosting, reinforcement learning), and perception and cognition (natural language, machine vision, robotics). [source](https://catalog.champlain.edu/preview_course_nopop.php?catoid=47&coid=33594)

## Requirements

- Required: [GPR-200 (Introduction to Modern Graphics Programming)](https://catalog.champlain.edu/preview_course_nopop.php?catoid=46&coid=31450) and [GPR-250 (Game Architecture)](https://catalog.champlain.edu/preview_course_nopop.php?catoid=37&coid=25336)
- Recommended: [CSI-281 (Data Structures and Algorithms)](https://classlist.champlain.edu/show/course/number/CSI_281)

### Textbook

- AI for Games, Third Edition: Millington, Ian. [amazon](https://a.co/d/3wjdn1T) [online via Champlain](https://research-ebsco-com.cobalt.champlain.edu/c/uomcmi/search/details/gokywrfgzb)

## Student-centered Learning Outcomes

[![Bloom's Taxonomy](https://cdn.vanderbilt.edu/vu-wp0/wp-content/uploads/sites/59/2019/03/27124326/Blooms-Taxonomy-650x366.jpg)](https://cft.vanderbilt.edu/guides-sub-pages/blooms-taxonomy/) [image source](https://cft.vanderbilt.edu/guides-sub-pages/blooms-taxonomy/)

Upon completion of the AI for Games, students should be able to:

### Objective Outcomes

- **Recall** AI algorithms used in games including search techniques (DFS, BFS, dijkstra), pathfinding A\*, behavioral systems and others;
- **Identify** appropriate AI techniques for specific game mechanics such as agent movement, maze generation, procedural content and other techniques to solve game problems;
- **Understand** core AI concepts including finite state machines, flocking behaviors, spatial partitioning, and noise functions;
- **Apply** basic search algorithms (DFS, BFS, A\*) to solve pathfinding and maze generation problems in 2D game environments;
- **Implement** behavioral agent systems using flocking algorithms and finite automata for game character AI;
- **Analyze** the performance trade-offs between different pathfinding approaches in continuous and grid-based spaces, and its common data structures;
- **Create** procedural content using noise functions and spatial quantization techniques for game world generation;
- **Integrate** multiple AI systems (movement, pathfinding, and procedural generation) into a cohesive game project demonstrating technical proficiency.

::: warning "Tentative Schedule"

This is a work in progress, and the schedule is subject to change. Every change will be communicated in class. Use the github repo as the source of truth for the schedule and materials. The materials provided in canvas are just a copy for archiving purposes and might be outdated.

:::

## Schedule for Fall 2025

College dates for the Fall 2025 semester:

| Event                                     | Date              |
| ----------------------------------------- | ----------------- |
| Classes Begin                             | Aug. 25           |
| Add/Drop                                  | Aug. 25 - 29      |
| No Classes - College remains open         | Sept. 19          |
| Indigenous Peoples Day Holiday Observance | Oct. 13           |
| Last Day to Withdraw                      | Nov. 07           |
| Thanksgiving Break                        | Nov. 24 - Nov. 28 |
| Last Day of Classes                       | Dec. 05           |
| Finals                                    | Dec. 08 - Dec. 12 |
| Winter Break                              | Dec. 15 - Jan. 09 |

## Weekly schedule

- Week 01. 2025/08/25 - 2025/08/29
  - Topics:
    - **Introduction**
    - **Game AI History**
  - Assignments:
    1. Read this Syllabus;
    2. [Read Notes on plagiarism](submissions)
    3. [Sign FERPA Form](ferpa)
    4. Read Text Chapters 1 & 2 from AI for Games book;
    5. Take the quiz on Canvas;
    6. [Setup your machine and repository](setup);
    7. Start the [Flocking Simulation](flocking);
- Week 02. 2025/09/01 - 2025/09/05
  - Topic: **Behavioral Agents**
  - Presentation: [Flocking](https://docs.google.com/presentation/d/1OBEY-tb_ubgoq6Mk9lEsCFaYLINni3oPwjH8iAXEQQM/edit?usp=sharing)
  - Formal Assignment: [Flocking](flocking)
  - Interactive Assignment: [Flocking](https://github.com/gameguild-gg/mobagen/tree/master/examples/flocking)
- Week 03. 2025/09/08 - 2025/09/12
  - Topic: **Finite Automata** and **2D Grids**
  - Formal Assignment: [Formal Game of Life](life)
  - Interactive Assignment: [Interactive Game of Life](https://github.com/gameguild-gg/mobagen/tree/master/examples/life)
- Week 04. 2025/09/15 - 2025/09/19
  - Topic: **Random Numbers**
  - Formal Assignment: [Formal](rng)
- Week 05. 2025/09/22 - 2025/09/26
  - Topics:
    - **Depth First Search**
    - **Random walk**
    - **Maze Generation**
  - Formal Assignment: [Formal Maze](maze)
  - Interactive Assignment: [Interactive Maze](https://github.com/gameguild-gg/mobagen/tree/master/examples/maze)
- Week 06. 2025/09/29 - 2025/10/03
  - Topics:
    - **Breadth First Search**
    - **Path Finding**
  - Interactive Assignment: [Catch the Cat](https://github.com/gameguild-gg/mobagen/tree/master/examples/catchthecat)
- Week 07. 2025/10/06 - 2025/10/10
  - Topic:
    - **MidTerms**
    - **Catch the Cat Challenge** and **Competition**
  - [Catch the Cat](https://github.com/gameguild-gg/mobagen/tree/master/examples/catchthecat)
- Week 8. 2025/10/13 - 2025/10/17
  - Topic:
    - **Spatial Quantization, Partitioning and Hashing**
  - Readings: [Spatial Quantization](spatial-quantization)
- Week 9. 2025/10/20 - 2025/10/24
  - Topic:
    - **Pathfinding on Continuous Space**
  - Formal Assignment: [PathFinding on continuous space](pathfinding-continuous)
- Week 10. 2025/10/27 - 2025/10/31
  - Topic: **Noise functions**
  - Formal Assignment: [Noise functions](noise)
- Week 11. 2025/11/03 - 2025/11/07
  - Topic:
    - **Procedural Content Generation**
  - Interactive Assignment: [Scenario Generation](https://github.com/gameguild-gg/mobagen/tree/master/examples/scenario)
- Week 12. 2025/11/10 - 2025/11/14
  - Topic:
    - **Procedural Content Generation**
  - Interactive Assignment: [Scenario Generation](https://github.com/gameguild-gg/mobagen/tree/master/examples/scenario)
- Week 13. 2025/11/17 - 2025/11/21
  - Topic: Work sessions for final project
  - Assignment: [Final Project](final-project)
- Week 14. 2025/11/24 - 2025/11/28
  - Topic: Thanksgiving Break
- Week 15. 2025/12/01 - 2025/12/05
  - Topic: Work sessions for final project
  - Assignment: [Final Project](final-project)
- Week 16. 2025/12/08 - 2025/12/12
  - Topic: Finals Presentation
  - Assignment: [Final Project](final-project)
