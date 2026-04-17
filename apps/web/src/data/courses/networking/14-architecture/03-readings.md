# Week 14 Readings: Server Architecture and Session Management. Distributed Systems

::: tip "How to approach these readings"

This week is about **who owns the truth** and **how players find each other**. Read in order: first understand the authority models (who validates gameplay), then study rollback as the key technique for hiding authority latency, then move to session lifecycle (matchmaking, lobbies, scaling). The GDC talks are especially important this week — they show how production teams solved these problems under real constraints.

:::

| #   | Reading / Watching                                                                                                                                                  | Time   | Covers                                                                                                                                                                       |
| --- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | Jeff Hodges, ["Notes on Distributed Systems for Young Bloods"](https://www.somethingsimilar.com/2013/01/14/notes-on-distributed-systems-for-young-bloods/)                | 15 min | Practical distributed systems principles: design for failure, coordination avoidance, partial availability, backpressure, CAP theorem — the mindset for game server architecture |
| 2   | Raft Consensus, ["The Secret Lives of Data"](http://thesecretlivesofdata.com/raft/) + ["Overview & Paper"](https://raft.github.io/)                                        | 20 min | Distributed consensus and leader election: how nodes agree on state, elect leaders, and handle failures — directly maps to authority assignment and host migration              |
| 3   | Video, NetherRealm GDC: ["8 Frames in 16ms: Rollback Networking in Mortal Kombat and Injustice 2"](https://www.youtube.com/watch?v=7jb0FOcImdg)                     | 20 min | Rollback networking in production: input delay vs rollback tradeoffs, state save/restore, visual correction strategies                                                       |
| 4   | Infil, ["Fighting Game Networking"](https://words.infil.net/w02-netcode.html) (sections on delay-based vs rollback)                                                 | 15 min | Accessible comparison of delay-based vs rollback netcode, why rollback wins for responsiveness, and player-visible artifacts                                                 |
| 5   | Google, ["Open Match Documentation — Overview and Concepts"](https://open-match.dev/site/docs/overview/)                                                            | 15 min | Open-source matchmaking framework: match functions, tickets, assignments, and the separation of matchmaking logic from game servers                                          |
| 6   | Agones Documentation, ["What is Agones?"](https://agones.dev/site/docs/overview/) + ["Quickstart"](https://agones.dev/site/docs/getting-started/create-gameserver/) | 12 min | Kubernetes-native game server orchestration: dedicated server lifecycle, allocation, scaling, and fleet management                                                           |
| 7   | Glenn Fiedler, ["Deterministic Lockstep"](https://gafferongames.com/post/deterministic_lockstep/)                                                                   | 15 min | The lockstep authority model: why determinism matters, floating-point pitfalls, playout delay buffers, and how rollback evolved from lockstep                                |
| 8   | Microsoft PlayFab, ["Multiplayer Servers"](https://learn.microsoft.com/en-us/gaming/playfab/multiplayer/servers/)                                                   | 12 min | Managed dedicated server hosting: session lifecycle, server allocation, scaling policies, and matchmaking integration in production                                          |

**Total required reading/watching time: ~124 minutes (~2 hours 4 minutes)**

---

## Cross-Track Focus (CSI vs GPR)

- **CSI-275 focus:**
  - Compare authority models as distributed-systems problems: single-writer (dedicated), elected-leader (listen/host), and replicated-state (lockstep)
  - Analyze matchmaking as a service-discovery and load-balancing pattern with constraints (skill, latency, region)
  - Reason about scaling strategies: horizontal (fleet autoscaling) vs vertical (bigger servers), stateless vs stateful services

- **GPR-430 focus:**
  - Map authority choices to player feel: input delay, correction visibility, hit-registration trust, and fairness perception
  - Understand rollback as the primary technique for hiding authority latency in action games
  - Design session flows from the player perspective: queue → match → play → disconnect, with graceful degradation (host migration, reconnection)

---

## Optional Deep Dive

### Authority models and server architecture

- "1500 Archers on a 28.8: Network Programming in Age of Empires" (Bettner & Terrano) — classic lockstep architecture, deterministic simulation, and the tradeoffs that led to modern authority models
- "The TRIBES Engine Networking Model" (Frohnmayer & Gift) — early client-server with ghosting/scoping, priority-based updates, and the origin of many modern patterns
- Unreal Engine, ["Replicate Actor Properties"](https://dev.epicgames.com/documentation/en-us/unreal-engine/replicate-actor-properties-in-unreal-engine) — detailed look at property replication, RPCs, and authority roles in a production engine
- Unity, ["Netcode for GameObjects — Network Topologies"](https://docs-multiplayer.unity3d.com/netcode/current/terms-concepts/network-topologies/) — practical framework comparison of client-server, distributed authority, and relay-based topologies

### Rollback networking deep dive

- GGPO, ["How GGPO Works"](https://www.ggpo.net/) — the reference rollback library: input prediction, state snapshots, resimulation, and spectator delay
- Tony Cannon, ["GGPO: Good Game, Peace Out"](https://www.youtube.com/watch?v=k9JTIn1SVQ4) — rollback inventor's talk on the algorithm's design and real-world testing
- Infil, ["How Rollback Netcode Works" (Part 4)](https://words.infil.net/w02-netcode-p4.html) — step-by-step visual walkthrough of rollback frame-by-frame
- Glenn Fiedler, ["Introduction to Networked Physics"](https://gafferongames.com/post/introduction_to_networked_physics/) — the physics simulation that motivates lockstep and rollback approaches

### Matchmaking and session management

- Google Open Match, ["Match Function Guide"](https://open-match.dev/site/docs/guides/matchmaker/matchfunction/) — writing custom match functions: skill rating, latency constraints, party grouping
- Epic Online Services, ["Lobbies and Sessions"](https://dev.epicgames.com/docs/game-services/lobbies-and-sessions) — production lobby/session management: creation, search, invites, and presence
- IETF RFC 8445, ["Interactive Connectivity Establishment (ICE)"](https://datatracker.ietf.org/doc/html/rfc8445) — connection establishment patterns relevant to session setup (preview for Week 15)

### Scaling and infrastructure

- Agones, ["Scheduling and Autoscaling"](https://agones.dev/site/docs/advanced/scheduling-and-autoscaling/) — fleet scheduling strategies (packed vs distributed), autoscaling policies, and cluster-level scaling
- Kubernetes Documentation, ["Horizontal Pod Autoscaling"](https://kubernetes.io/docs/tasks/run-application/horizontal-pod-autoscale/) — the general autoscaling model that Agones builds upon
- CNCF, ["Service Discovery"](https://glossary.cncf.io/service-discovery/) — how game clients and matchmakers find available server instances
- Microsoft PlayFab, ["Using PlayFab Multiplayer Servers"](https://learn.microsoft.com/en-us/gaming/playfab/multiplayer/servers/using-playfab-servers-to-host-games) — detailed walkthrough: build authoring, deployment, scaling, latency measurement, and server requests

### Videos / talks

- YouTube, ["Explaining Rollback Netcode and How It Helps Fighting Games"](https://www.youtube.com/watch?v=0NLe4IpdS1w) (Core-A Gaming) — highly visual 15-minute explainer of delay vs rollback for non-technical audiences
- YouTube, ["Rocket League Networking Explained"](https://www.youtube.com/watch?v=ueEmiDM94IE) (GDC, Jared Cone) — hybrid authority model in a physics-heavy game with client prediction

### Optional exploration path (~105 minutes total)

Pick **any 5–7** from the list above to go deeper while staying near 2 hours:

1. "1500 Archers" paper (20 min)
2. GGPO talk by Tony Cannon (20 min)
3. Core-A Gaming rollback explainer (15 min)
4. Agones Scheduling and Autoscaling (15 min)
5. Unreal Engine Replication docs (20 min)
6. Epic Online Services Lobbies (15 min)
7. PlayFab detailed walkthrough (15 min)

---

## Study Tips

::: warning "What to pay attention to"

1. **Authority is a spectrum, not a binary:** pure client-authority (fast, cheatable), pure server-authority (safe, laggy), and hybrid models (predicted client + authoritative server) each fit different game genres.
2. **Rollback is not free:** it requires deterministic simulation, efficient state save/restore, and visual smoothing for corrections — but it makes authority nearly invisible to players.
3. **Dedicated ≠ always better:** listen servers reduce infrastructure cost and latency for small sessions; the key is robust host migration when the host disconnects.
4. **Matchmaking is a multi-objective optimization:** skill fairness, latency bounds, queue time, party sizes, and population health all compete — no single metric is sufficient.
5. **Scaling game servers is stateful, not stateless:** unlike web servers, each game instance holds live player state, so you cannot simply round-robin or kill pods — graceful draining and session-aware scheduling are required.

:::

**Recommended reading order:**

1. Hodges "Notes on Distributed Systems for Young Bloods"
2. Raft Consensus (interactive visualization + overview)
3. NetherRealm rollback GDC talk
4. Infil "Fighting Game Networking"
5. Fiedler "Deterministic Lockstep"
6. Open Match overview
7. Agones overview + quickstart
8. PlayFab "Multiplayer Servers"

**Common mistakes to avoid:**

- Assuming "authoritative server" means the client does nothing — client prediction is what makes authority feel responsive
- Implementing rollback without deterministic simulation — non-determinism causes desyncs that rollback cannot fix
- Treating matchmaking as pure skill sorting — ignoring latency, region, and queue health leads to long waits or unfair matches
- Designing listen-server games without host migration — a single player disconnect should not end the session for everyone
- Scaling game servers like web servers (stateless autoscaling) — game instances are stateful; killing a pod kills a live match
- Coupling matchmaking logic to game server code — keep them separate so match rules can evolve without redeploying servers
