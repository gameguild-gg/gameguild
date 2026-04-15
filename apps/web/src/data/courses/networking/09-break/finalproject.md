# Final Project

The goal of this final project is to create 3 deliverables:

| Deliverable  | Description                                                                 | Grade | Composition |
| ------------ | --------------------------------------------------------------------------- | ----- | ----------- |
| Tech Demo    | A working networked application demonstrating your chosen networking topic. | 50%   | In group    |
| Presentation | A presentation of your project.                                             | 30%   | In group    |
| Writeup      | A writeup of your project.                                                  | 20%   | Individual  |

- A Tech Demo of your networked application. (In group)
- A Presentation of your project. (In group)
- A Writeup of your project. (Individual)

## Topics Suggestions

::: tip

You may select one or more topics from the list below, or propose your own (must be approved by the instructor first).

This is a cross-listed course (CSI-275 / GPR-430). Topics are organized into **Game Programming** and **Computer Science** tracks, but you may pick from either track regardless of your major.

:::

### Game Programming Track (GPR-430)

| #   | Topic                                       | Description                                                                                       | Complexity |
| --- | ------------------------------------------- | ------------------------------------------------------------------------------------------------- | :--------: |
| 1   | Real-Time Multiplayer Fighting Game         | 1v1 fighter with client-side prediction, rollback, and lag compensation                           |  ⭐⭐⭐⭐  |
| 2   | Multiplayer Racing with Dead Reckoning      | Networked racing game using prediction/extrapolation for smooth vehicle movement                  |   ⭐⭐⭐   |
| 3   | Co-op Action Game with Authoritative Server | 2–4 player co-op with authoritative server, entity interpolation, and delta compression           |   ⭐⭐⭐   |
| 4   | RTS Networking with Lockstep                | Deterministic lockstep simulation for real-time strategy (Age of Empires style)                   |  ⭐⭐⭐⭐  |
| 5   | Rollback Netcode Platformer / Action Game   | GGPO/rollback-style networking for a fast-paced 2D platformer or action game                      |  ⭐⭐⭐⭐  |
| 6   | FPS Hit Registration & Lag Compensation     | Server-side hit detection with lag compensation (Halo: Reach / Overwatch style)                   | ⭐⭐⭐⭐⭐ |
| 7   | Physics-Sync Multiplayer Game               | Networked physics simulation with authority handoff (Rocket League style)                         | ⭐⭐⭐⭐⭐ |
| 8   | Turn-Based Multiplayer with Reconnection    | Chess/card game server with session persistence, reconnection, and spectator mode                 |   ⭐⭐⭐   |
| 9   | Interest Management / Area of Interest      | Large-world multiplayer with spatial partitioning to reduce bandwidth (MMO / Battle Royale style) |  ⭐⭐⭐⭐  |

### Computer Science Track (CSI-275)

| #   | Topic                                 | Description                                                                                        | Complexity |
| --- | ------------------------------------- | -------------------------------------------------------------------------------------------------- | :--------: |
| 10  | Custom Reliable UDP Protocol          | Design and benchmark a reliable UDP protocol with ACKs, retransmission, and congestion control     |  ⭐⭐⭐⭐  |
| 11  | P2P Network with NAT Traversal        | Peer-to-peer overlay network with STUN/hole-punching, peer discovery, and distributed state        |  ⭐⭐⭐⭐  |
| 12  | Distributed Chat with Persistence     | Multi-room chat system with database-backed history, authentication, and WebSocket transport       |   ⭐⭐⭐   |
| 13  | Load-Balanced Server Cluster          | Horizontally scalable server cluster with session affinity, health checks, and failover            | ⭐⭐⭐⭐⭐ |
| 14  | Real-Time Collaborative Editor (CRDT) | Conflict-free replicated data type implementation for concurrent document editing                  | ⭐⭐⭐⭐⭐ |
| 15  | Network Monitoring & Analysis Tool    | Packet capture/analysis tool with latency, jitter, and loss metrics plus visualization dashboard   |   ⭐⭐⭐   |
| 16  | Matchmaking & Lobby Service           | REST + WebSocket matchmaking service with Elo/MMR ranking, lobby management, and session brokering |  ⭐⭐⭐⭐  |
| 17  | Custom Application-Layer Protocol     | Novel protocol for a specific use case with formal specification, benchmarks, and reference impl   |  ⭐⭐⭐⭐  |
| 18  | Distributed Key-Value Store           | Replicated KV store with consistency guarantees, leader election, and partition tolerance          | ⭐⭐⭐⭐⭐ |

### Open

| #   | Topic                    | Description                                                                                 | Complexity |
| --- | ------------------------ | ------------------------------------------------------------------------------------------- | :--------: |
| 19  | Guest Lecturer Challenge | Select one of the challenges proposed by guest lecturers (shared during the semester)       |   Varies   |
| 20  | Your Own Idea            | Propose a unique project aligned with course objectives. Must be approved by the instructor |   Varies   |

**Complexity key:** ⭐ trivial · ⭐⭐ straightforward · ⭐⭐⭐ moderate, multiple techniques · ⭐⭐⭐⭐ research-level integration · ⭐⭐⭐⭐⭐ cutting-edge challenge

::: note

Once you select your topic, check the signup spreadsheet to make sure it is not already taken.

:::

## Tech Demo

::: info

It is highly recommended to enroll into the game jam [here](https://itch.io/jam/networking-26) in order to submit your tech demo. But it is not required.

:::

You have to implement a tech demo featuring your networking solution. The tech demo should be a working, testable application that demonstrates the networking concepts from your chosen topic.

::: warning

Desktop builds are preferred — C++ is the recommended language since it aligns with the course assignments. However, any language is accepted with instructor approval.

Your demo must be **testable by others**. Provide clear build/run instructions or, when possible, deploy a hosted version. Recommended delivery formats:

- **Desktop executable** with a README explaining how to build and run (preferred)
- **Docker Compose setup** for easy multi-instance testing
- **Web-based demo** deployed to [itch.io](https://itch.io/) or [GitHub Pages](https://pages.github.com/)
- **Video demo** (only as a supplement — a testable build is always preferred)

:::

## Writeup

Your post should include the following:

- At least 3 references. These can be books, articles, RFCs, or websites;
- A description of the networking architecture and protocols used;
- What problems your solution addresses (latency, reliability, scalability, etc.);
- Design decisions and tradeoffs you made;
- Length should be around 600 words;
- Your post should reference your tech demo;
- Use diagrams to explain your architecture. Suggestions: [Code2flow](https://code2flow.com/), [Mermaid](https://mermaid.live/), [draw.io](https://draw.io/).

## Presentation

- The goal of the presentation is to teach the class that specific networking topic;
- Your audience is the class, so target the level of content to be around yours;
- Duration: 10 minutes + 5 minutes Q&A;
- You may use any presentation tool you like, but I recommend [Google Slides](https://www.google.com/slides/) for simplicity, easy sharing, and multi-user editing;
- This is a formal presentation — be prepared to answer questions from the audience;
- A **live demo** is required during the presentation. Have a backup video in case of technical issues;
- You may use your tech demo in your presentation.

## Submission

- The presentation slides (shared link);
- The link for the blog post (LinkedIn, Medium, etc.);
- The link for the tech demo (itch.io, GitHub repo, etc.);
- The code repository (GitHub link).

## Late Policy

Late submissions will incur a penalty of **1% deduction per day** up to a maximum of **25% of the total grade**. For example, a submission that is 1 week (7 days) late will receive a 7% penalty, resulting in a maximum possible grade of 93%.

## Milestones & Checkpoints

| Week | Date       | Milestone                         | Deliverable                                                                                                                                                                                                                                        | Status       |
| ---- | ---------- | --------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------ |
| 10   | 2026/03/22 | **Proposal**                      | Team composition + project proposal in slideshow format. Must include: chosen topic, team members & roles, scope description, tech stack, and preliminary network protocol sketch.                                                                 | Due Sunday   |
| 11   | 2026/03/29 | **Architecture Design**           | Network protocol design document + architecture diagram. Show major components, message formats, data flow, and how the networking integrates with the application. Use [Mermaid](https://mermaid.live/), [draw.io](https://draw.io/), or similar. | Due Sunday   |
| 12   | 2026/04/05 | **Networking Prototype**          | Core networking implemented and demonstrable. Publish a testable build **or** record a video showing the networking in action. The goal is to show basic client-server (or P2P) communication working.                                             | Due Sunday   |
| 13   | 2026/04/12 | **Alpha Build**                   | In-class testing session. Bring your build for classmates to test. Collect feedback. Core features should be functional.                                                                                                                           | In class     |
| 14   | 2026/04/19 | **Feature Freeze**                | No new features after this week. Second testing session in class. Focus on bug fixes, polish, and stress testing only.                                                                                                                             | In class     |
| 15   | 2026/04/26 | **Peer Evaluation & Code Freeze** | Teams exchange projects for structured code review and testing. Submit peer evaluation feedback by Sunday. Code freeze: project submission no later than Wednesday 2026/04/22. Technical essay draft due.                                          | Due Sunday   |
| 16   | 2026/04/30 | **Final Presentations**           | 10-minute presentation + 5-minute Q&A. Live demo required. Final code repository, technical essay, and peer evaluation reflection due.                                                                                                             | Due Thursday |
