# Dedicated vs Listen Servers

The choice between **dedicated servers** and **listen servers** is one of the most consequential infrastructure decisions in game networking. It determines who pays for compute, where authority lives, what happens when a player disconnects, and how matchmaking and scaling work. Both models are valid — the choice depends on genre, budget, player count, and cheat tolerance.

---

## 1. Dedicated Servers

### What They Are

A dedicated server is a process (usually headless — no rendering) that runs the authoritative game simulation on infrastructure the developer controls. Players connect to it as clients. The server exists solely to run the game; no player "is" the server.

```
Player A ──┐
Player B ──┼── Dedicated Server (cloud/datacenter) ── authoritative simulation
Player C ──┘
```

### Properties

| Property          | Dedicated Server                                              |
| ----------------- | ------------------------------------------------------------- |
| Authority         | Server is authoritative; no player has special trust          |
| Failure mode      | Server crash ends game for everyone                           |
| Network topology  | Star (all clients connect to one endpoint)                    |
| Latency symmetry  | All players have similar latency to the same server           |
| Cheat resistance  | High (server validates all inputs)                            |
| Cost              | Developer/publisher pays for compute                          |
| Scaling           | Add more server instances (horizontal scaling)                |
| Player disconnect | Game continues; player slot is freed or held for reconnection |

### When Dedicated Servers Are Preferred

- **Competitive multiplayer**: ranked modes, esports, tournaments where fairness and anti-cheat are non-negotiable.
- **Large player counts**: 64-player shooters, battle royales with 100+ players, MMO shards.
- **Persistent worlds**: the game state must survive any individual player disconnecting.
- **Cross-platform play**: a neutral server avoids giving any platform an advantage.

### The Cost Problem

Dedicated servers cost money. Each active game instance consumes CPU, memory, and bandwidth on infrastructure the developer operates. For a game with 100,000 concurrent players in 4-player matches:

- 25,000 active game instances.
- At $0.03/hour per small VM: $750/hour = $18,000/day = $540,000/month.
- Plus bandwidth, monitoring, matchmaking infrastructure, and operational overhead.

This cost scales linearly with concurrent players and is the primary reason smaller studios cannot always offer dedicated servers for every mode.

### Stateless vs Stateful Considerations

Dedicated game servers are **stateful** — each instance holds live player state, match progress, and simulation state. This has important implications:

- **Cannot round-robin**: a load balancer cannot redirect a client to a different server instance mid-match.
- **Cannot kill pods freely**: terminating a server instance kills a live match.
- **Graceful draining**: when scaling down, servers must finish their current match before terminating.
- **Session affinity**: once a player joins a server, they must stay connected to that specific instance.

This statefulness makes game server orchestration fundamentally different from web server orchestration (where any request can go to any instance).

---

## 2. Listen Servers (Player-Hosted)

### What They Are

A listen server is a game instance where **one player's machine acts as both client and server**. That player (the "host") runs the authoritative simulation while also playing the game. Other players connect to the host as clients.

```
Player A (host + server) ──┐
Player B (client) ─────────┼── Host machine runs simulation
Player C (client) ─────────┘
```

### Properties

| Property          | Listen Server                                          |
| ----------------- | ------------------------------------------------------ |
| Authority         | Host player has authoritative control                  |
| Failure mode      | Host disconnect ends game unless host migration exists |
| Network topology  | Star (all connect to host) or mesh for some data       |
| Latency symmetry  | Host has 0ms latency; other players have RTT to host   |
| Cheat resistance  | Low (host can manipulate simulation directly)          |
| Cost              | Zero infrastructure cost to developer                  |
| Scaling           | Scales with player count (each match hosts itself)     |
| Player disconnect | If host disconnects, game ends (without migration)     |

### When Listen Servers Are Preferred

- **Small session sizes**: 2-8 player games where a host can handle the simulation.
- **Low competitive stakes**: cooperative games, casual modes, friend groups.
- **Budget constraints**: indie studios, free-to-play titles without server revenue.
- **LAN play**: the host is physically close to all players; latency is negligible.
- **Platform restrictions**: console platforms where custom server infrastructure is difficult.

### The Host Advantage Problem

The host player has **zero network latency** to the authoritative simulation. In a competitive game, this creates a measurable advantage:

- Host's inputs are applied instantly. Other players' inputs are delayed by RTT.
- Host sees authoritative state with no interpolation delay.
- Host's hit detection is perfectly accurate; remote players rely on lag compensation.

This advantage can be partially mitigated:

- **Artificial host delay**: add fake latency to the host's inputs to equalize. This makes the host's experience worse but the match fairer.
- **Host-only cosmetic mode**: host runs the server but uses a separate client connection like everyone else (essentially a "local dedicated server"). This eliminates the advantage but requires more processing power.

### The Disconnect Problem

If the host disconnects (rage quit, crash, network failure), the game state is lost. Without **host migration**, all other players are kicked. This is the single biggest reliability problem with listen servers.

---

## 3. Host Migration

### What It Solves

Host migration transfers the authoritative simulation from the departing host to another player's machine, allowing the game to continue.

### How It Works (High Level)

1. **Detection**: clients detect host disconnection (timeout, heartbeat failure).
2. **Election**: remaining clients agree on a new host (deterministic rule or negotiation).
3. **State Transfer**: the new host must reconstruct the game state. Two approaches:
   - **Pre-distributed state**: all clients maintain a near-complete copy of the game state (from state sync). The new host promotes its local state to authoritative.
   - **State snapshot**: the old host periodically broadcasts a full state snapshot that clients cache. The new host loads from the most recent snapshot.
4. **Reconnection**: clients disconnect from the old host and connect to the new host.
5. **Reconciliation**: the new host resolves any state discrepancies from the transition period.

### Why It's Hard

Host migration has several failure modes:

- **State divergence**: different clients may have slightly different views of the game state. Which one becomes authoritative?
- **In-flight packets**: inputs and state updates that were in transit when the host died are lost.
- **Election conflicts**: if two clients both think they're the new host (network partition during migration), the game forks.
- **Timing gap**: the migration takes real time (1-5 seconds typically). What happens to the game during this pause? Players see a freeze, a loading screen, or rubber-banding.
- **NAT traversal**: the new host may not be reachable by all other clients (they could reach the old host via NAT hole punching, but the new host requires new connections).

### Practical Host Migration Strategies

| Strategy                      | Complexity | Recovery Time | State Accuracy     |
| ----------------------------- | ---------- | ------------- | ------------------ |
| Full state replication to all | High       | Fast (<1s)    | High (all have it) |
| Periodic snapshots from host  | Medium     | Medium (1-3s) | Moderate (stale)   |
| No migration (game ends)      | Zero       | N/A           | N/A                |
| Backup host (shadow server)   | Very high  | Very fast     | Very high          |

Most commercial games that support host migration use the **full state replication** approach: clients already receive enough state data (from normal state sync) that the new host can reconstruct the world by promoting its local view.

---

## 4. Hybrid Models

### Local Dedicated Server

The player's machine runs a dedicated server process separate from the client process. The player connects to their own machine as a regular client. This eliminates the host advantage while keeping the zero-infrastructure-cost benefit.

Trade-off: requires more CPU/memory on the host's machine (running both server and client).

### Relay + Player Host

Players connect through a relay server (developer-operated) that forwards packets but does not run the simulation. One player still hosts the authoritative simulation. The relay provides:

- NAT traversal (all players connect to the relay; no direct peer connections needed).
- Packet routing and optional packet inspection.
- Host anonymity (other players don't know the host's IP address).

The relay adds one hop of latency but solves the NAT and privacy problems.

### Cloud-Backed Listen Server

The game starts as a listen server. If the host disconnects, the game migrates to a cloud-hosted dedicated server. This gives zero-cost operation during normal play and high reliability at the cost of keeping standby server capacity.

---

## 5. Decision Framework

### Choosing Between Models

| Factor                 | Dedicated Server                  | Listen Server                  |
| ---------------------- | --------------------------------- | ------------------------------ |
| Player count per match | Any (scales with server hardware) | 2-16 (limited by host machine) |
| Competitive integrity  | High                              | Low-medium                     |
| Infrastructure budget  | Required                          | Zero                           |
| Reliability            | High (server doesn't rage quit)   | Low without host migration     |
| Latency fairness       | Equal for all players             | Host has advantage             |
| Deployment complexity  | High (orchestration, scaling)     | Low (clients are servers)      |
| Long-running sessions  | Ideal (persistent worlds)         | Fragile (host may leave)       |

### Mixed Strategies

Many games use both:

- **Ranked/competitive**: dedicated servers for fairness.
- **Casual/custom**: listen servers to reduce cost.
- **LAN/private**: listen servers for simplicity and zero latency.

This is a common pattern in games like Halo, Call of Duty, and Rocket League — competitive playlists use dedicated servers, while custom games and local play use listen servers.

---

## 6. CSI vs GPR Framing

### CSI Perspective: Infrastructure Architecture

The CSI engineer evaluates:

- **Availability**: what is the uptime guarantee? Dedicated servers can be monitored, restarted, and load-balanced. Listen servers depend on player hardware and network quality.
- **Fault tolerance**: dedicated servers can be replicated or backed by standby instances. Listen servers have no redundancy without host migration.
- **Capacity planning**: dedicated servers require provisioning (how many instances, in which regions, at what times). Listen servers scale automatically with player count.
- **Observability**: dedicated servers emit server-side metrics (tick time, bandwidth, player counts). Listen servers make server metrics difficult to collect.

### GPR Perspective: Player Experience

The GPR engineer evaluates:

- **Time to play**: dedicated servers require matchmaking + server allocation (5-30 seconds). Listen servers start instantly (host creates game, friends join).
- **Session continuity**: dedicated servers survive player disconnects. Listen servers require host migration to survive host disconnect.
- **Perceived fairness**: dedicated servers feel fair (equal latency). Listen servers feel unfair when the host has better performance.
- **Feature parity**: some features (spectating, replay recording, server-side anti-cheat) are much easier with dedicated servers.
