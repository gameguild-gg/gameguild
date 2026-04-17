# Architecture Decision Patterns: Putting It All Together

This final section ties together every topic from Week 14 — authority models, server types, rollback, sessions, matchmaking, scaling, and distributed systems foundations — into decision frameworks you can use when designing a networked game.

The core question is always: **given your game's requirements, which architecture choices produce the best player experience at acceptable cost and complexity?**

---

## 1. The Architecture Decision Space

Every networked game sits at a point in a multi-dimensional decision space:

| Decision                 | Options                                                 |
| ------------------------ | ------------------------------------------------------- |
| Authority model          | Server authoritative, client authoritative, distributed |
| Server type              | Dedicated, listen (P2P with host), pure P2P, hybrid     |
| State sync strategy      | Snapshots, delta, events, lockstep, rollback            |
| Session model            | Match-based, persistent, drop-in/drop-out               |
| Matchmaking              | Skill-based, quick-play, server browser, invite-only    |
| Scaling                  | Fixed fleet, auto-scaled, player-hosted, serverless     |
| Tick rate                | 10-128 Hz depending on genre                            |
| Player count per session | 2, 4-10, 16-64, 100+, thousands (MMO)                   |

These decisions are not independent — choosing one constrains others. Let's trace the dependency chains.

---

## 2. Genre-Driven Architecture Patterns

### Fighting Games (2 players, frame-perfect input)

| Decision      | Typical Choice                   | Why                                      |
| ------------- | -------------------------------- | ---------------------------------------- |
| Authority     | Distributed (both peers equal)   | No server = no server latency            |
| Server type   | Pure P2P (direct connection)     | Only 2 players, minimize hops            |
| Sync strategy | Rollback                         | Frame-perfect responsiveness required    |
| Session model | Match-based                      | Short matches (60-180 seconds)           |
| Matchmaking   | Skill-based (ranked), quick-play | 1v1 means precise skill matching matters |
| Scaling       | No game servers needed           | P2P; only matchmaking service scales     |
| Tick rate     | 60 Hz (frame-locked)             | Fighting games run at display refresh    |

The critical tradeoff: P2P rollback gives zero input latency but requires deterministic simulation and limits player count to 2-4.

### Competitive FPS (5v5 to 6v6)

| Decision      | Typical Choice               | Why                                      |
| ------------- | ---------------------------- | ---------------------------------------- |
| Authority     | Server authoritative         | Anti-cheat, consistent hit registration  |
| Server type   | Dedicated server             | No host advantage, stable performance    |
| Sync strategy | Server-reconciled prediction | Players need instant feedback (shooting) |
| Session model | Match-based (competitive)    | Structured rounds with clear outcomes    |
| Matchmaking   | Skill-based with ranking     | Competitive integrity is paramount       |
| Scaling       | Auto-scaled dedicated fleet  | Global player base, regional servers     |
| Tick rate     | 64-128 Hz                    | High precision needed for hit detection  |

The critical tradeoff: dedicated servers cost money but provide fair, cheat-resistant gameplay. The tick rate directly affects bandwidth and server cost.

### Battle Royale (60-100 players)

| Decision      | Typical Choice                          | Why                                               |
| ------------- | --------------------------------------- | ------------------------------------------------- |
| Authority     | Server authoritative                    | Anti-cheat at scale, consistency                  |
| Server type   | Dedicated server                        | 100 players can't be P2P                          |
| Sync strategy | Interest management + delta             | Too many entities for full state broadcast        |
| Session model | Match-based, drop-in at start           | Fixed match lifecycle with elimination            |
| Matchmaking   | Quick-play with optional SBMM           | Fill lobbies fast; 100 players is hard to balance |
| Scaling       | Pre-warmed fleet, aggressive auto-scale | Matches are large; each needs a full server       |
| Tick rate     | 20-30 Hz                                | Lower tick rate to handle entity count            |

The critical tradeoff: 100 players means aggressive interest management (don't replicate the entire map to every player). Tick rate drops to manage bandwidth and CPU.

### MMO (thousands of players, persistent world)

| Decision      | Typical Choice                   | Why                                      |
| ------------- | -------------------------------- | ---------------------------------------- |
| Authority     | Server authoritative (sharded)   | Economy integrity, persistent state      |
| Server type   | Dedicated server cluster         | Multiple servers per world               |
| Sync strategy | Interest management + zones      | Each server handles a world region       |
| Session model | Persistent (hours-long sessions) | Players connect and stay                 |
| Matchmaking   | Server/world selection           | Players choose a "realm" or are assigned |
| Scaling       | Sharding, instancing, layering   | Overflow players go to new shard/layer   |
| Tick rate     | 10-20 Hz                         | Lower tick rate, higher player count     |

The critical tradeoff: sharding trades global consistency for scalability. Players on different shards can't interact directly.

### Co-op PvE (4 players, casual)

| Decision      | Typical Choice                    | Why                                        |
| ------------- | --------------------------------- | ------------------------------------------ |
| Authority     | Host authoritative                | Simple, cheap, no dedicated infrastructure |
| Server type   | Listen server (one player hosts)  | Small group, friends playing together      |
| Sync strategy | State replication from host       | Host runs simulation, replicates to guests |
| Session model | Invite-based, drop-in/drop-out    | Friends join and leave freely              |
| Matchmaking   | Invite/lobby, optional quick-play | Social group formation                     |
| Scaling       | Player-hosted (no server cost)    | Players provide the compute                |
| Tick rate     | 30-60 Hz                          | Moderate precision needs                   |

The critical tradeoff: host advantage and host disconnection are accepted because the social dynamic (friends playing together) reduces their impact.

---

## 3. Decision Flow: Choosing Your Architecture

Here is a decision flow for selecting core architecture components:

### Step 1: How Many Players Per Session?

- **2 players**: P2P is viable. Consider rollback for action games.
- **2-8 players**: listen server is viable. Consider dedicated if competitive.
- **8-64 players**: dedicated server strongly preferred. Interest management needed above ~20.
- **64+ players**: dedicated server required. Aggressive interest management and potentially sharding.

### Step 2: Competitive or Cooperative?

- **Competitive (PvP)**: cheating matters → server authoritative. Fairness matters → dedicated servers + SBMM. Integrity matters → higher tick rate.
- **Cooperative (PvE)**: cheating matters less → host authority acceptable. Social matters → invite/lobby system. Cost matters → player-hosted.
- **Mixed**: competitive modes get dedicated servers; cooperative modes can use listen servers. Many games support both.

### Step 3: How Latency-Sensitive Is the Core Mechanic?

- **Frame-precise** (fighting games): rollback or lockstep. No server in the input path.
- **Twitch-precise** (FPS, racing): server authority + client prediction + lag compensation.
- **Turn-based or slow-paced** (strategy, RPG): server authority without prediction is fine. Higher latency tolerable.

### Step 4: Budget and Scale

- **Indie with small player base**: listen servers (free), P2P, or minimal dedicated fleet.
- **Mid-tier with growing player base**: managed services (PlayFab, GameLift), auto-scaling.
- **AAA with global player base**: custom infrastructure, Agones on Kubernetes, multi-region, full observability.

---

## 4. Common Architecture Mistakes

### Mistake 1: Over-Engineering for Scale

Building a Kubernetes-orchestrated, multi-region, auto-scaling fleet for a game with 200 concurrent players. The operational cost of the infrastructure exceeds the cost of just running 10 fixed servers.

**Fix**: start simple, scale when needed. A fixed set of dedicated servers handles many indie and mid-tier games. Add orchestration when you actually need auto-scaling.

### Mistake 2: Wrong Authority Model for the Genre

Using server authority with no prediction for a fast-paced action game (players feel 100ms+ input delay). Or using client authority for a competitive game (cheating is trivial).

**Fix**: match authority model to genre requirements (see Step 3 above).

### Mistake 3: Ignoring Host Migration

Building on listen servers without implementing host migration. When the host disconnects, the entire session ends — all other players lose their progress.

**Fix**: implement host migration, or use dedicated servers if session continuity matters.

### Mistake 4: Matchmaking Without Population Awareness

Strict skill-based matchmaking with a small player population. Players wait 10+ minutes, get frustrated, and leave — further shrinking the population in a death spiral.

**Fix**: adaptive criteria expansion, population-aware matchmaking, cross-mode pooling. See the matchmaking section.

### Mistake 5: Not Planning for Launch Day

The game launches, 10x expected players show up, the fixed server fleet is overwhelmed, players can't connect. First impressions are ruined.

**Fix**: pre-provision burst capacity, use auto-scaling with aggressive scale-up policies, load test before launch.

---

## 5. Cost Estimation Framework

### Server Cost Model

For dedicated servers, estimate monthly cost:

$$C_{\text{monthly}} = N_{\text{peak}} \times C_{\text{instance}} \times H_{\text{hours}} \times F_{\text{utilization}}$$

Where:

- $N_{\text{peak}}$ = peak concurrent match count
- $C_{\text{instance}}$ = cost per server-hour
- $H_{\text{hours}}$ = hours per month the service operates
- $F_{\text{utilization}}$ = utilization factor (1.0 = all instances fully used; typically 0.3-0.7)

Example: 1,000 peak concurrent matches, $0.05/server-hour, 730 hours/month, 50% utilization:

$$C = 1000 \times 0.05 \times 730 \times \frac{1}{0.5} = \$73,000/\text{month}$$

Compare to listen server (free compute) or managed services (different pricing model).

### Listen Server Cost Model

Listen servers shift compute cost to players. Infrastructure cost is only:

- Matchmaking/lobby service (handles ~1000x connections per instance).
- STUN/TURN relay servers (for players behind NAT). TURN bandwidth is the main cost.

---

## 6. CSI vs GPR Summary Table

This table summarizes how CSI and GPR perspectives apply across all Week 14 topics:

| Topic               | CSI Framing                                          | GPR Framing                                         |
| ------------------- | ---------------------------------------------------- | --------------------------------------------------- |
| Authority models    | Consistency model (single-leader, multi-leader)      | Who controls the feel? (responsive vs correct)      |
| Dedicated vs listen | Infrastructure architecture (managed vs unmanaged)   | Host advantage, fairness, session stability         |
| Rollback            | Optimistic replication with conflict resolution      | Invisible latency hiding for action games           |
| Session management  | Distributed resource lifecycle (states, transitions) | Player journey (finding, joining, staying in games) |
| Matchmaking         | Constraint optimization, queue theory                | Fairness perception, flow state, retention          |
| Scaling             | Fleet management, auto-scaling, capacity planning    | "Can I play?" (availability during peak/launch)     |
| Distributed systems | CAP theorem, consensus, replication theory           | Why the game "just works" (or doesn't)              |

### The Unifying Insight

CSI and GPR are not opposing approaches — they are complementary perspectives on the same problems:

- **CSI provides the theory**: why certain architectures fail, what the fundamental limits are, how to analyze tradeoffs formally.
- **GPR provides the requirements**: what the player needs to feel, what "good enough" consistency looks like, where to spend engineering budget for maximum experience impact.

A well-architected game network is designed by engineers who can think in both frames: use CSI to understand what is possible and what will break, then use GPR to decide what matters and what to optimize.
