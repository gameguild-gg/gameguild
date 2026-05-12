# Authority Models: Who Owns the Truth?

Every networked game must answer one fundamental question: **which machine's version of reality is correct?** When two players see different things — one says "I hit you," the other says "you missed" — the authority model decides who wins. This decision shapes every downstream design choice: latency feel, cheat resistance, infrastructure cost, and failure behavior.

---

## 1. The Authority Problem

### Why Authority Exists

In a single-player game, one machine simulates everything. There is exactly one version of reality and it is always self-consistent. Networked games break this: multiple machines simulate simultaneously, and network latency means they cannot be perfectly synchronized.

Without an authority:

- Player A's machine says A's bullet hit Player B at position (10, 5).
- Player B's machine says B dodged and is at position (10, 7).
- Both are "correct" on their local simulation.
- There is no way to resolve the conflict without someone being designated as the tiebreaker.

The authority is the machine (or rule) that produces the **canonical state** — the one true version that all other machines must eventually converge to.

### Authority Is Not Binary

Authority is often described as "server authoritative" vs "client authoritative," but real systems exist on a spectrum:

| Model                         | Who Decides                                 | Latency                     | Cheat Resistance | Complexity |
| ----------------------------- | ------------------------------------------- | --------------------------- | ---------------- | ---------- |
| Full client authority         | Each client trusts itself                   | Zero (local)                | None             | Low        |
| Client authority + validation | Client acts, server validates post-hoc      | Low (local + rollback risk) | Moderate         | Medium     |
| Server authoritative          | Server decides, clients predict and correct | RTT/2 base + prediction     | High             | High       |
| Distributed authority         | Multiple peers share authority by partition | Varies by partition         | Moderate         | Very high  |

Most production games use **server authoritative with client-side prediction** — the server is the source of truth, but clients predict locally to hide latency. The server then confirms or corrects the prediction.

---

## 2. Full Client Authority

### How It Works

Each client simulates its own actions and broadcasts results to other clients (or a relay server). Whatever the client says happened, happened.

```
Client A: "I moved to (10, 5)" → broadcast to all
Client B: "I moved to (3, 8)" → broadcast to all
```

No validation. No correction. Each client accepts what others report.

### Where It's Used

- **Cooperative games with trusted players**: LAN parties, small friend groups, game jams.
- **Low-stakes interactions**: chat, emotes, cosmetic effects where cheating has no gameplay impact.
- **Prototyping**: fastest path to "something working" — validate game feel before investing in authority.

### Why It Fails at Scale

With full client authority, any client can lie:

- Report impossible positions (teleporting).
- Report hits that never happened (aimbot broadcasting fake hit events).
- Report resource acquisition that didn't occur (inventory hacking).

There is no mechanism to detect or prevent this. The moment players have incentive to cheat (rankings, competitive play, real-money items), full client authority collapses.

### The Trust Boundary

Full client authority means the trust boundary is at the client. Since players control their own machines, the trust boundary is compromised by definition. This is why it only works when trust is social (friends) rather than technical (enforcement).

---

## 3. Server Authoritative Model

### The Core Principle

One machine — the **authoritative server** — runs the canonical simulation. Clients send inputs (not outcomes). The server applies inputs, advances the simulation, and sends authoritative state back to clients.

```
Client → Server:  "I pressed MOVE_RIGHT at tick 42"
Server:           Applies input, simulates tick 42, produces new state
Server → Client:  "At tick 42, your position is (10.5, 5.0)"
```

The client never tells the server what happened. The client tells the server what the player _intended_, and the server decides what _actually_ happened.

### Input Authority vs State Authority

A critical distinction:

- **Input authority**: the client is authoritative over _what the player pressed_. The server trusts that the player pressed MOVE_RIGHT. (It may validate timing and rate, but not the intent itself.)
- **State authority**: the server is authoritative over _what the press caused_. The server decides whether MOVE_RIGHT results in movement, collision, interaction, etc.

This separation is what makes server authority work: clients control their own intentions, but not the world's response to those intentions.

### The Latency Problem

Server authority introduces mandatory latency:

1. Player presses button (t=0).
2. Input travels to server (t=RTT/2).
3. Server simulates and produces result.
4. Result travels back to client (t=RTT).
5. Client renders the result.

At 80ms RTT, the player waits 80ms between pressing a button and seeing the result — _if the client does nothing locally_. This feels terrible for any action game. The solution is **client-side prediction** (covered in Week 13), where the client immediately simulates the input locally and later corrects if the server disagrees.

### Validation and Anti-Cheat

Because the server runs the simulation, it can validate everything:

- **Movement validation**: is the requested movement speed physically possible? Is the path clear of collisions?
- **Action validation**: does the player have the resources/cooldowns to perform this action?
- **Timing validation**: are inputs arriving at a reasonable rate? (A client sending 1000 inputs per second is suspicious.)
- **State consistency**: does the claimed client state match the server's record?

Invalid inputs are silently dropped or flagged. The client will be corrected by the next authoritative state update.

### Worked Example: Hit Detection

Player A fires at Player B:

1. Client A sends: "FIRE at angle 45° at tick 100."
2. Server receives at tick 102 (1 tick of network delay).
3. Server rewinds to tick 100 (lag compensation).
4. Server casts ray at angle 45° from A's tick-100 position against B's tick-100 position.
5. If hit: server applies damage, broadcasts event to all clients.
6. If miss: server sends no hit event. Client A's local "hit" prediction gets silently corrected.

The server is the only machine that decides whether the shot connected. Client A may see a local hit marker, but it's speculative until the server confirms.

---

## 4. Distributed Authority (Peer-to-Peer Partitioned)

### What It Means

Instead of one central authority, **authority is partitioned among peers**. Each peer is authoritative over specific entities or regions:

- Player A is authoritative over its own character and nearby objects.
- Player B is authoritative over its own character and its nearby objects.
- No single machine sees or validates everything.

### When It's Used

- **Cooperative games** where players don't need to directly affect each other's state frequently.
- **Large open-world games** where centralizing all authority would be too expensive.
- **Unity's Distributed Authority** topology and similar frameworks.

### Authority Transfer

When two players interact (collision, trade, combat), authority must temporarily converge:

- **Handoff**: one peer yields authority to the other for the duration of the interaction.
- **Arbitration**: a lightweight coordinator resolves conflicts without running the full simulation.
- **Merge**: both peers submit their view and a deterministic rule picks the outcome.

Authority transfer is the hardest part of distributed authority. Race conditions, network partitions during transfer, and disagreements about who "owns" an entity are all active failure modes.

### Comparison to Centralized

| Aspect                  | Centralized (Server) | Distributed (P2P Partitioned) |
| ----------------------- | -------------------- | ----------------------------- |
| Single point of failure | Yes (server)         | No (but partition failures)   |
| Cheat resistance        | High                 | Per-partition only            |
| Infrastructure cost     | Server hardware      | Player machines               |
| Latency (peer-to-peer)  | 2× (through server)  | 1× (direct)                   |
| Complexity              | Moderate             | Very high                     |
| Scalability             | Server-limited       | Peer-limited                  |

---

## 5. Authority and Game Genre Fit

Different genres tolerate different authority tradeoffs:

| Genre           | Typical Authority Model              | Why                                                              |
| --------------- | ------------------------------------ | ---------------------------------------------------------------- |
| Competitive FPS | Server authoritative + lag comp      | Cheat resistance and hit-reg fairness are critical               |
| Fighting games  | Lockstep / rollback (peer authority) | Both players need identical simulation; frame precision matters  |
| Cooperative PvE | Server or distributed authority      | Lower cheat incentive; can tolerate some inconsistency           |
| MMO             | Server authoritative (sharded)       | Massive scale requires centralized validation per shard          |
| Battle royale   | Server authoritative                 | 100 players with high cheat incentive                            |
| Turn-based      | Server authoritative (simple)        | Latency tolerance is high; validation is straightforward         |
| Racing          | Server authoritative + prediction    | Physics divergence requires authority, but input latency matters |

### The Genre-Authority Mismatch

Using the wrong authority model for a genre creates characteristic failures:

- **Client authority in competitive FPS**: rampant cheating destroys competitive integrity.
- **Full server authority in fighting games**: 80ms of input delay makes precise combos impossible.
- **Distributed authority in MMO**: cross-partition exploits, duplication bugs, inconsistent economies.

---

## 6. CSI vs GPR Framing

### CSI Perspective: Authority as a Consistency Model

The CSI engineer sees authority as a **consistency guarantee** in a distributed system:

- **Strong consistency** (server authoritative): all clients see the same state after each tick. Linearizable. High latency cost.
- **Eventual consistency** (client authority + correction): clients may temporarily disagree but converge. Lower latency, higher complexity.
- **Causal consistency** (distributed authority with ordering): events are processed in causal order, but concurrent events may be resolved differently on different peers.

The CAP theorem applies directly: in the presence of network partitions, you cannot have both perfect consistency and full availability. Server authority chooses consistency (correct state) over availability (immediate response). Client prediction adds availability back through optimistic speculation.

### GPR Perspective: Authority as Player Feel

The GPR engineer asks:

- **Input responsiveness**: does the player feel immediate control? (Prediction quality.)
- **Correction visibility**: when the server disagrees, how jarring is the correction? (Smoothing quality.)
- **Fairness perception**: when two players interact, does the outcome feel fair to both? (Lag compensation quality.)
- **Trust**: does the player trust that hits register, that cheaters are caught, that the game is honest? (Authority credibility.)

These are not technical metrics — they are **experiential metrics** that authority design directly determines.

### Reconciling Both Perspectives

The best authority design satisfies both:

- CSI: the system is provably consistent, recovers from all failure modes, and emits measurable metrics.
- GPR: the player feels responsive, corrections are invisible, and outcomes feel fair.

This is achieved through the layered approach covered in Weeks 12-13: authority provides correctness, prediction provides responsiveness, and interpolation/smoothing provides visual quality.
