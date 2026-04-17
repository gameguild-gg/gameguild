# Distributed Systems Foundations for Game Networking

Games are distributed systems. Every concept from distributed systems theory — consensus, failure detection, replication, consistency models — has a direct analog in game networking. This section connects the theoretical foundations (from the Hodges and Raft readings) to the concrete game architecture decisions covered in this week's other sections.

---

## 1. Games as Distributed Systems

### The Mapping

| Distributed Systems Concept | Game Networking Analog                                    |
| --------------------------- | --------------------------------------------------------- |
| Node                        | Game client or server instance                            |
| Leader                      | Authoritative server or host                              |
| Follower / Replica          | Client (replicates server state)                          |
| Consensus                   | Authority decision (who is right?)                        |
| Leader election             | Host migration (choosing a new host)                      |
| State machine replication   | Game state synchronization                                |
| Failure detection           | Disconnect/timeout detection                              |
| Partition                   | Player disconnected, in-flight packets                    |
| Eventual consistency        | Client prediction (temporarily wrong, eventually correct) |
| Optimistic replication      | Rollback (speculate locally, correct on conflict)         |

### Why This Framing Matters

Understanding the distributed systems foundations lets you:

- **Predict failure modes**: if you know your authority model is equivalent to single-leader replication, you know leader failure (server crash) will cause an availability gap.
- **Apply proven solutions**: host migration is leader election. The same algorithms (Raft, Bully, Ring) and the same failure cases apply.
- **Reason about tradeoffs**: the CAP theorem tells you that in the presence of network partitions, you must choose between consistency (server authority) and availability (client authority). Client prediction is a technique for adding availability back after choosing consistency.

---

## 2. Consensus and Authority

### What Consensus Means

In distributed systems, **consensus** is the problem of getting multiple nodes to agree on a single value. In game networking, the "value" is the game state:

- What is the player's position?
- Did the bullet hit?
- Who won the round?

### Single-Leader Consensus (Server Authority)

The simplest consensus model: one node (the server) is the **leader**. It decides unilaterally. All other nodes (clients) accept its decisions. There is no voting, no quorum — the leader's word is final.

This is equivalent to **single-leader replication** in database terms:

- Writes go to the leader (client inputs go to the server).
- The leader processes writes and produces a new state.
- The leader replicates the new state to followers (server sends state to clients).
- Followers are read-only replicas (clients render state but don't modify the authoritative version).

Failure mode: if the leader dies, the system stops (server crash = game ends). Recovery requires either restarting the leader or electing a new one (host migration).

### Multi-Leader / Peer Consensus (P2P)

In a P2P game (especially with rollback), there is no single leader. Both peers run the simulation and must agree on the outcome. This is a **consensus problem**:

- Both peers have the same inputs (exchanged over the network).
- Both peers run the same deterministic simulation.
- Both peers arrive at the same state.

If one peer has a different input set (due to lost packets), they temporarily disagree. Rollback resolves this by replaying with the correct inputs once they arrive.

This is analogous to **state machine replication**: given the same sequence of inputs, all replicas produce the same state.

### Connection to Raft

The Raft consensus algorithm solves leader election and log replication:

1. **Leader election**: nodes vote to choose a leader. Only one leader at a time.
2. **Log replication**: the leader appends entries to a log and replicates them to followers. An entry is committed when a majority acknowledge it.
3. **Safety**: committed entries are never lost, even if the leader fails.

In game networking terms:

- **Leader election → host migration**: when the server/host fails, the remaining nodes must agree on a new leader.
- **Log replication → input distribution**: the leader distributes inputs (or state) to all followers.
- **Commit → confirmation**: a state update is "committed" when the server sends the authoritative snapshot and the client applies it.

---

## 3. Failure Detection

### The Two Generals' Problem (Informally)

You can never be 100% certain that a remote node has received your message. The network might drop the message, or the acknowledgment. This fundamental uncertainty applies to every game networking interaction:

- The server sends a state update — did the client receive it?
- The client sends an input — did the server process it?
- The host seems to have disconnected — is it actually down, or just experiencing a temporary network hiccup?

### Timeout-Based Detection

Practical failure detection uses timeouts:

1. Expect regular heartbeats (or data packets) from the remote side.
2. If no packet arrives within a timeout window, declare the connection "suspected failed."
3. After a longer timeout, declare it "dead" and clean up.

The timeout must balance:

- **Too short**: false positives (declaring a connection dead during a temporary spike). This causes unnecessary disconnects.
- **Too long**: slow detection (a crashed client continues to occupy a slot for many seconds). This wastes resources and blocks backfill.

Typical game values:

| Detection stage | Timeout | Action                                      |
| --------------- | ------- | ------------------------------------------- |
| Suspected       | 3-5s    | Notify other players, prepare for migration |
| Confirmed dead  | 10-15s  | Remove from session, free slot              |

### Heartbeats vs Data as Liveness Signal

If the remote side is actively sending game data, no separate heartbeat is needed — the data packets serve as liveness signals. Heartbeats are only needed when data flow may pause (spectators, idle players, pre-match lobby).

---

## 4. The CAP Theorem Applied to Games

### CAP in One Sentence

In the presence of a network **Partition**, a distributed system must choose between **Consistency** (all nodes see the same data) and **Availability** (all nodes can continue operating).

### Game Networking Translation

- **Consistency** = all players see the same game state (server authoritative).
- **Availability** = all players can continue playing without waiting (client prediction, local simulation).
- **Partition** = network latency, packet loss, or disconnection.

Every online game experiences partitions (latency is a partial partition). The authority model determines the CAP tradeoff:

| Model                    | CAP Choice                            | Behavior During Partition              |
| ------------------------ | ------------------------------------- | -------------------------------------- |
| Pure server authority    | Consistency over availability         | Client waits (input delay)             |
| Server auth + prediction | Consistency + optimistic availability | Client speculates, corrects later      |
| Pure client authority    | Availability over consistency         | Clients diverge, no correction         |
| P2P lockstep             | Consistency (blocks)                  | Game pauses until all inputs arrive    |
| P2P rollback             | Consistency + optimistic availability | Peers speculate, roll back on conflict |

Client-side prediction and rollback are techniques for achieving **both** consistency and availability under partitions — with the caveat that availability is _optimistic_ (the client may need to correct).

### Eventual Consistency in Games

Client prediction is a form of **eventual consistency**: the client's state is temporarily different from the server's, but will converge once the server's authoritative update arrives. The convergence mechanism is rollback, smooth correction, or snap correction.

---

## 5. Replication and State Synchronization

### State Synchronization as Replication

Game state synchronization is **state machine replication**:

- The server (leader) produces a sequence of state updates.
- Clients (followers) apply these updates to maintain a replica of the game state.
- The replica may lag behind the leader (by the network delay).

### Replication Strategies in Games

| Strategy            | How It Works                                      | Bandwidth | Consistency          |
| ------------------- | ------------------------------------------------- | --------- | -------------------- |
| Full state snapshot | Send the entire game state every tick             | Very high | Strong               |
| Delta compression   | Send only what changed since the last ack'd state | Low       | Strong               |
| Interest management | Only replicate entities relevant to each client   | Low       | Eventually           |
| Event-based         | Send events (not state); client computes state    | Very low  | Requires determinism |

All of these are forms of replication with different consistency/bandwidth tradeoffs — the same tradeoffs that distributed databases make.

---

## 6. Coordination Avoidance

### Hodges' Insight

Jeff Hodges' "Notes on Distributed Systems for Young Bloods" emphasizes: **coordination is expensive; avoid it when possible.**

In game networking, coordination means waiting for agreement before proceeding:

- **Lockstep** is maximum coordination: every peer waits for every other peer's input before advancing.
- **Server authority with prediction** reduces coordination: the client proceeds without waiting for the server, then corrects.
- **Interest management** avoids coordination: entities that don't interact don't need to synchronize.

### Practical Coordination Avoidance in Games

- **Partition the world**: entities in different zones don't need mutual consistency. A player in Zone A doesn't need to agree on the state of Zone B.
- **Defer non-critical updates**: cosmetic state (particle effects, ambient animations) can be eventually consistent or even inconsistent — nobody cares if your client shows a different weather effect.
- **Batch confirmations**: instead of confirming every input individually, confirm batches (via ACK bitfields, covered in Week 12).

---

## 7. CSI vs GPR Framing

### CSI Perspective: Formal Properties

The CSI engineer applies distributed systems theory directly:

- **Linearizability**: can we guarantee that all clients see events in the same order? (Server authority provides this per shard; cross-shard is weaker.)
- **Causal ordering**: do causally related events appear in the right order? (Input → action → result must be ordered; unrelated events may be reordered.)
- **Liveness**: does the system always make progress? (Lockstep can deadlock if a peer is unreachable.)
- **Safety**: does the system never produce an incorrect state? (Rollback guarantees this by replaying with authoritative data.)
- **FLP impossibility**: in an asynchronous system, consensus is impossible if even one node can fail. Real systems work around this with timeouts and probabilistic guarantees.

### GPR Perspective: Invisible Theory

The GPR engineer doesn't want players to know distributed systems exist:

- **Consensus should be invisible**: the player presses a button and sees a result. They don't care that 5 machines had to agree.
- **Failure detection should be fast**: when a player disconnects, the other players shouldn't wait 30 seconds to find out.
- **Replication should feel real-time**: the player sees other players moving smoothly, not teleporting between replication epochs.
- **Partitions should degrade gracefully**: if the connection degrades, the game should feel sluggish before it disconnects — not snap from "perfect" to "kicked."
