# Scaling Game Servers

Scaling game servers is fundamentally different from scaling web servers. Web servers are stateless: any request can go to any instance, and load balancers distribute freely. Game servers are **stateful**: each instance holds a live match with active player connections, simulation state, and in-flight data. You cannot redirect a player mid-match, and you cannot kill a server without ending the game.

---

## 1. Why Game Server Scaling Is Hard

### Stateful vs Stateless

| Property             | Web Server (Stateless)           | Game Server (Stateful)               |
| -------------------- | -------------------------------- | ------------------------------------ |
| Request routing      | Any instance handles any request | Player is bound to one instance      |
| Instance termination | Safe at any time                 | Kills a live match                   |
| Load balancing       | Round-robin, least-connections   | Must respect session affinity        |
| Scale-down           | Remove instances freely          | Must wait for matches to end (drain) |
| Health check         | HTTP 200 = healthy               | "Healthy" depends on match state     |
| State recovery       | Reload from database             | Match state is ephemeral, not in DB  |

### The Lifecycle Problem

A game server instance goes through a lifecycle:

```
Allocated → Initializing → Waiting for Players → Active (match in progress) → Draining → Terminated
```

During **Active**, the instance cannot be moved, replicated, or terminated. This phase can last from 5 minutes (short match) to hours (persistent session). The scaling system must respect this lifecycle.

### Peak vs Average Load

Player populations vary dramatically:

- **Daily cycle**: 3x-10x difference between peak (evening) and trough (early morning).
- **Weekly cycle**: weekends are 1.5x-2x of weekdays.
- **Events**: game launches, patches, and in-game events can spike 5x-20x.

If you provision for peak, you waste money during off-peak. If you provision for average, peak players get long queue times or no servers.

---

## 2. Horizontal Scaling: Adding More Instances

### Fleet Management

A **fleet** is a group of identical game server instances that can host the same game mode. Fleet management involves:

- **Provisioning**: starting new instances when demand increases.
- **Allocation**: assigning incoming matches to available instances.
- **Draining**: marking instances as "no new matches" before terminating them.
- **Health monitoring**: detecting crashed or stuck instances and replacing them.

### Allocation Strategies

When a matchmaker creates a match, it needs a server instance. Two approaches:

**Pre-warmed pool**: maintain a pool of idle server instances, ready to accept matches immediately.

- Pro: zero allocation latency (server is already running).
- Con: idle servers cost money.
- Tuning: how many idle servers to keep? Too many = waste; too few = allocation delay.

**On-demand allocation**: start a new instance when a match is created.

- Pro: no idle cost.
- Con: instance startup time (10-60 seconds for container, 1-5 minutes for VM) adds to queue time.
- Mitigation: use pre-warmed pools with on-demand overflow.

### Scheduling: Packed vs Distributed

When placing instances on physical machines (nodes):

- **Packed scheduling**: fill each node with as many instances as possible before using the next node. Maximizes utilization, minimizes cost. Risk: if a node fails, many matches are affected.
- **Distributed scheduling**: spread instances across nodes evenly. Lower utilization, higher resilience. Each node failure affects fewer matches.

Agones (Kubernetes-native game server orchestration) supports both strategies:

```yaml
# Agones Fleet scheduling
scheduling: Packed # or Distributed
```

### Graceful Scale-Down

When demand decreases, the system must remove instances without killing active matches:

1. Mark instances as **draining** (accept no new matches).
2. Wait for active matches to complete naturally.
3. Terminate the drained instance.

The drain period can be long (if matches take 30 minutes, you might wait 30 minutes before freeing the resource). This is why game server scaling has significant hysteresis — the system responds slowly to demand decreases.

---

## 3. Vertical Scaling: Bigger Instances

### When Vertical Scaling Applies

Some games have very large per-match player counts (64-128 players, or MMO shards with thousands). A single server instance must handle more load, which means a bigger machine.

Vertical scaling limits:

- **CPU**: single-threaded game simulations hit single-core limits. Multi-threading helps but adds complexity.
- **Memory**: large world state, player data, and physics state require more RAM.
- **Network**: high player counts produce high packet rates that can saturate NICs or CPU interrupt handling.

### CPU Budget Per Tick

The server must complete one tick of simulation before the next tick deadline:

$$T_{\text{tick\_budget}} = \frac{1}{R_{\text{tick\_hz}}}$$

At 60 Hz: 16.7ms per tick. At 20 Hz: 50ms per tick.

If the simulation takes longer than the tick budget, the server falls behind. This manifests as:

- Delayed state updates to clients.
- Inconsistent tick pacing (some ticks take longer, causing jitter on the client).
- Eventually, the server "hitches" and players experience rubber-banding or freezing.

Monitoring tick time as a percentage of tick budget is critical:

| Tick Time / Budget | Status                                       |
| ------------------ | -------------------------------------------- |
| < 50%              | Healthy, headroom for spikes                 |
| 50-80%             | Normal operation, limited headroom           |
| 80-95%             | At risk, occasional spikes may exceed budget |
| > 95%              | Overloaded, visible degradation              |

---

## 4. Game Server Orchestration

### What Orchestration Does

Orchestration automates the fleet management tasks:

- **Auto-scaling**: adjust fleet size based on demand signals (queue depth, allocation rate, time of day).
- **Health management**: detect unhealthy instances and replace them.
- **Rolling updates**: deploy new game server versions without disrupting active matches.
- **Multi-region**: manage fleets across geographic regions to serve global players.

### Kubernetes + Agones

Agones extends Kubernetes for game server workloads:

- **GameServer**: a Kubernetes custom resource representing a single game server instance.
- **Fleet**: a set of GameServers with desired replica count.
- **FleetAutoscaler**: adjusts Fleet size based on buffer count (how many idle servers to maintain) or custom webhooks.
- **Allocation**: assigns an idle GameServer to a match, transitioning it from "Ready" to "Allocated."

Agones lifecycle:

```
Pod Created → PortAllocated → Creating → Starting → Scheduled →
  RequestReady → Ready → Allocated → (match runs) → Shutdown → Deleted
```

### PlayFab Multiplayer Servers

Microsoft PlayFab offers managed game server hosting:

- **Builds**: upload game server builds (containers or VMs).
- **Regions**: deploy to multiple Azure regions.
- **Standby targets**: configure how many idle servers to maintain per region.
- **Auto-scaling**: PlayFab manages scaling based on allocation requests.
- **Server allocation API**: matchmaker requests a server; PlayFab returns connection details.

PlayFab abstracts away Kubernetes, VMs, and infrastructure — the developer focuses on the game server binary and scaling policy.

### Comparison

| Feature                | Agones (self-managed)          | PlayFab (managed)      |
| ---------------------- | ------------------------------ | ---------------------- |
| Infrastructure control | Full (your Kubernetes cluster) | Limited (Azure-hosted) |
| Cost model             | Pay for infrastructure         | Pay per server-hour    |
| Customization          | Full (custom schedulers, etc.) | Configuration-based    |
| Operational complexity | High (you manage K8s)          | Low (managed service)  |
| Multi-cloud            | Yes (any K8s cluster)          | Azure only             |
| Learning curve         | Steep (K8s + Agones)           | Moderate (PlayFab SDK) |

---

## 5. Region Management and Global Distribution

### Why Regions Matter

Players distributed globally need servers near them. A player in Tokyo connecting to a server in Virginia has ~150ms RTT from geography alone. Regional deployment ensures:

- **Low latency**: servers in or near the player's region.
- **Compliance**: data residency requirements (player data stays in-region).
- **Resilience**: regional failure doesn't affect the entire service.

### Multi-Region Fleet Strategy

| Strategy                   | Description                                      | Cost   | Complexity |
| -------------------------- | ------------------------------------------------ | ------ | ---------- |
| Single region              | All servers in one location                      | Lowest | Lowest     |
| Fixed multi-region         | Pre-provisioned fleets in each supported region  | High   | Medium     |
| Demand-driven multi-region | Provision fleets in regions where demand exists  | Medium | High       |
| Follow-the-sun             | Shift capacity to regions approaching peak hours | Medium | Very high  |

### Cross-Region Matchmaking

The matchmaker must decide which region's servers to use:

- **Player proximity**: pick the region closest to the players in the match.
- **Party distribution**: if party members span regions, pick the region that minimizes maximum RTT.
- **Server availability**: prefer regions with available idle servers to avoid allocation delay.
- **Cost**: some regions are more expensive; factor cost into routing decisions during off-peak.

---

## 6. Monitoring and Observability

### What to Monitor

Game server infrastructure needs specific metrics beyond standard web server monitoring:

| Metric                      | What It Tells You                 | Alert Threshold                     |
| --------------------------- | --------------------------------- | ----------------------------------- |
| Active matches              | Current demand                    | Trending toward capacity            |
| Idle server count           | Buffer for new matches            | < minimum buffer                    |
| Allocation latency          | Time from request to server ready | > 5 seconds                         |
| Server tick time (p95)      | Simulation performance            | > 80% of tick budget                |
| Player count per server     | Load per instance                 | > max capacity                      |
| Crash rate                  | Stability                         | > 1% of instances                   |
| Match duration distribution | Lifecycle planning for draining   | Unusually long matches              |
| Queue depth                 | Unmet demand                      | Growing faster than matches created |

### Capacity Planning

Predict future demand from historical patterns:

- **Time-series forecasting**: predict player counts from day-of-week and time-of-day patterns.
- **Event-based scaling**: pre-provision extra capacity before known events (patch day, tournaments, free weekends).
- **Reactive scaling**: scale up when queue depth or allocation latency exceeds thresholds.

A good strategy combines all three: baseline from forecasting, pre-provisioned burst from events, and reactive safety net.

---

## 7. CSI vs GPR Framing

### CSI Perspective: Infrastructure as Distributed System

The CSI engineer sees game server scaling as a distributed systems problem:

- **Service discovery**: how do matchmakers find available servers? (Registry, DNS, API.)
- **Load balancing**: how are matches distributed across instances and regions?
- **Fault tolerance**: what happens when an instance, node, or region fails?
- **Capacity planning**: queuing theory models predict wait times from arrival rate and service rate.
- **Cost optimization**: right-sizing instances, spot/preemptible instances for cost savings, reserved capacity for baseline.

### GPR Perspective: Scaling as Player Experience

The GPR engineer asks:

- **Queue time**: does the player wait too long because there aren't enough servers?
- **Match quality**: are matches degraded because the matchmaker had to compromise on region/latency to find available servers?
- **Session continuity**: can players stay in a session across matches, or does server churn force re-queueing?
- **Event readiness**: when a new season launches and 3x players log in, does the system handle it or does the game become unplayable?
