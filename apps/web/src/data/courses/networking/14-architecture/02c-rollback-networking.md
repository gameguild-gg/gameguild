# Rollback Networking Concepts

Rollback networking is the technique that lets authority and responsiveness coexist. The client simulates locally (responsiveness) and rewinds when the server disagrees (authority). When implemented well, the player never notices the corrections. When implemented poorly, characters teleport, hits un-register, and players lose trust in the game.

---

## 1. The Problem Rollback Solves

### Authority Latency

Server authoritative games have a fundamental latency floor: the player presses a button, the input travels to the server, the server simulates, and the result travels back. At 80ms RTT, this is 80ms of delay between press and visual confirmation.

Client-side prediction (Week 13) hides this delay by speculatively simulating the input locally. But prediction is a guess — the server may produce a different result. When it does, the client must correct.

The question is: **how does the client correct?**

### Correction Approaches

| Approach          | How It Works                                  | Artifact                                   |
| ----------------- | --------------------------------------------- | ------------------------------------------ |
| Snap correction   | Jump to server state instantly                | Visible teleport, rubber-banding           |
| Smooth correction | Blend toward server state over several frames | Delayed convergence, mushy feel            |
| **Rollback**      | Rewind to server tick, replay inputs forward  | Corrections are small and nearly invisible |

Rollback is the most sophisticated approach because it corrects the **root cause** of the divergence (wrong state at the server tick) rather than just the **symptom** (wrong position at the current tick).

---

## 2. How Rollback Works

### The Core Algorithm

1. **Client receives authoritative state** for tick $T$ from the server.
2. **Client compares** its predicted state at tick $T$ to the server's authoritative state at tick $T$.
3. **If they match**: prediction was correct. No correction needed.
4. **If they differ**:
   a. Client **rewinds** its simulation to tick $T$.
   b. Client **applies the server's authoritative state** at tick $T$.
   c. Client **replays all local inputs** from tick $T+1$ to the current tick $T+N$, re-simulating each tick.
   d. Client **arrives at a corrected current state** that incorporates the server's authority plus all recent local inputs.

### Why Replay Matters

Without replay, the client would jump to the server's state at tick $T$ — which is in the past. The player would see their character snap backward. With replay, the client fast-forwards from the corrected past to the corrected present, producing a state that is both authoritative and current.

The replay must include all local inputs that were sent but not yet confirmed by the server. These inputs are still speculative, but they maintain the player's sense of control.

### Visual Timeline

```
Server confirms tick 100.  Client is at tick 108.

Without rollback:
  Client at tick 108 → snap to server's tick 100 state → visible teleport backward

With rollback:
  Client at tick 108 → rewind to tick 100 (server state)
                      → replay tick 101 (local input)
                      → replay tick 102 (local input)
                      → ...
                      → replay tick 108 (local input)
                      → arrive at corrected tick 108 → minimal visual change
```

If the prediction was close (which it usually is for the local player's own character), the corrected tick 108 state is nearly identical to the predicted tick 108 state. The correction is invisible.

---

## 3. Requirements for Rollback

### Deterministic Simulation

Rollback requires that replaying the same inputs from the same state produces the same output — **determinism**. If the simulation is non-deterministic (floating-point order dependence, random number generators not synchronized, frame-rate-dependent physics), replay produces different results and corrections oscillate.

Sources of non-determinism to control:

| Source                   | Problem                                      | Solution                                               |
| ------------------------ | -------------------------------------------- | ------------------------------------------------------ |
| Floating-point order     | Different addition order → different results | Fixed-point math or deterministic operation order      |
| Random numbers           | Different seeds → different outcomes         | Synchronized seed, deterministic RNG per tick          |
| Physics engine           | Variable-step integration → drift            | Fixed-step simulation, deterministic solver            |
| Hash map iteration order | Platform-dependent                           | Ordered containers or deterministic iteration          |
| Thread scheduling        | Non-deterministic interleaving               | Single-threaded simulation or deterministic scheduling |

Fighting games (where rollback is most mature) typically use fixed-point arithmetic and single-threaded, fixed-step simulation to guarantee determinism.

### Efficient State Save and Restore

The client must be able to:

- **Save** the complete simulation state at any tick (for rollback target).
- **Restore** a previously saved state instantly (to begin replay).
- **Keep a window** of saved states (typically RTT / tick_interval states deep).

State save approaches:

| Approach                       | Speed     | Memory Cost                | Implementation Complexity |
| ------------------------------ | --------- | -------------------------- | ------------------------- |
| Full state copy                | Fast copy | State size × window depth  | Low (memcpy)              |
| Copy-on-write                  | Very fast | Only changed data per tick | Medium                    |
| Delta snapshots                | Medium    | Low                        | High                      |
| Component-level dirty tracking | Fast      | Only dirty components      | Medium-high               |

For games with small state (fighting games: 2 players, limited objects), full state copy is practical. For games with large state (100-player shooters), delta or dirty-tracking approaches are necessary.

### Fast Resimulation

Rollback requires re-simulating N ticks per frame, where N is roughly the round-trip time in ticks:

$$N_{\text{rollback}} = \lceil \frac{RTT}{T_{\text{tick}}} \rceil$$

At 80ms RTT and 60 Hz (16.7ms ticks): $N = \lceil 80/16.7 \rceil = 5$ ticks of resimulation per frame.

The simulation must be fast enough that 5 extra ticks per frame fit within the frame budget. If the simulation takes 8ms per tick, rollback adds 40ms — exceeding a 16.7ms frame budget. This means the simulation must be highly optimized, or rollback is impractical.

### Input Buffer

The client must maintain a buffer of all local inputs that have been sent to the server but not yet confirmed:

```
Unconfirmed inputs: [tick 103: RIGHT, tick 104: RIGHT+JUMP, tick 105: RIGHT, ...]
Last confirmed tick: 102
Current tick: 108
```

When rollback occurs, these inputs are replayed from the confirmed tick forward. As the server confirms ticks, old inputs are removed from the buffer.

---

## 4. Rollback in Fighting Games vs Shooters

### Fighting Games: Pure Rollback

Fighting games are the natural home of rollback because:

- **Small state**: 2 players, limited game objects, compact simulation state (typically < 10 KB).
- **Frame precision matters**: a 1-frame advantage is competitively significant; input delay is unacceptable.
- **Determinism is achievable**: fixed-point math, simple physics, no complex AI.
- **Peer-to-peer**: fighting games typically use P2P with both peers running rollback, not client-server.

In P2P rollback:

- Both peers simulate locally with their own inputs.
- Each peer sends its inputs to the other.
- When a peer receives the other's input for a past tick, it rolls back to that tick, applies the correct input, and replays forward.
- Both peers converge to the same state because the simulation is deterministic and both have the same inputs.

GGPO (Good Game, Peace Out) is the reference library for P2P rollback, used in most modern fighting games.

### Shooters: Server Authority + Rollback Correction

Shooters use rollback differently:

- **Server authoritative**: the server is the source of truth.
- **Client predicts**: the client simulates locally.
- **Server confirms or corrects**: when the server's state differs, the client rolls back and replays.
- **More state**: 64 players, complex physics, many entities — state save is expensive.
- **Partial rollback**: typically only the local player's state is rolled back; other players' corrections use interpolation/smoothing.

### Comparison

| Aspect               | Fighting Game Rollback  | Shooter Rollback                |
| -------------------- | ----------------------- | ------------------------------- |
| Topology             | P2P (2 peers)           | Client-server                   |
| State size           | Small (< 10 KB)         | Large (hundreds of KB)          |
| Rollback scope       | Full simulation         | Local player only (usually)     |
| Determinism required | Strict                  | For local player's view         |
| Visual correction    | Rare (small divergence) | More frequent, smoothing needed |

---

## 5. Rollback Artifacts and Mitigation

### What Goes Wrong

Even well-implemented rollback has visible artifacts under bad network conditions:

- **One-frame teleports**: when the correction is large enough to be visible in a single frame. Mitigated by smoothing the correction over 2-3 frames.
- **Hit/hurt desync**: a player sees their attack connect locally, but rollback reveals it didn't. The hit effect plays and then the damage doesn't appear.
- **Animation pops**: the character's animation state jumps when the rolled-back state has a different animation than the predicted state.
- **Sound replays**: if sound effects trigger on prediction, rollback may cause the same sound to play twice or a hit sound to play for a miss.

### Mitigation Strategies

| Artifact          | Mitigation                                                                          |
| ----------------- | ----------------------------------------------------------------------------------- |
| Position teleport | Visual smoothing: render position blends toward simulation position over 2-4 frames |
| Animation pop     | Keep visual animation separate from simulation state; blend transitions             |
| Sound replay      | Defer sound effects until confirmed, or accept minor audio artifacts                |
| Hit desync        | Visual hit markers are speculative; damage numbers wait for confirmation            |
| Input display     | Show input response immediately; correct outcome quietly                            |

### The Rollback Window Budget

The maximum rollback depth (in ticks) determines the maximum RTT the system can handle without degradation:

$$RTT_{\text{max}} = N_{\text{window}} \times T_{\text{tick}}$$

A 10-frame window at 60 Hz supports RTT up to 167ms. Beyond that, the system falls back to input delay (waiting for the remote input before simulating).

GGPO-style systems use a hybrid: rollback up to the window limit, then add input delay frames for the remainder. This keeps the system responsive for most connections while gracefully degrading for very high-latency ones.

---

## 6. Lockstep: Rollback's Predecessor

### How Lockstep Works

In a lockstep model, all peers wait for all inputs before advancing each tick. No peer simulates ahead.

```
Tick 100: Wait for all inputs → simulate → advance
Tick 101: Wait for all inputs → simulate → advance
```

This guarantees deterministic agreement (everyone has the same inputs at the same tick) but adds latency equal to the slowest peer's round-trip time.

### Why Rollback Replaced Lockstep

Lockstep adds input delay equal to network latency. At 80ms RTT between peers, every input has 80ms of delay. This is acceptable for slow-paced games (strategy, card games) but unacceptable for action games.

Rollback decouples simulation from network confirmation: the client simulates immediately and corrects later, eliminating the perceived delay.

### When Lockstep Is Still Used

- **RTS games**: many units, deterministic simulation required, slow input cadence (commands per second, not frames per second).
- **Turn-based games**: natural lockstep — each turn waits for all players.
- **Deterministic replay**: lockstep logs are compact (only inputs), enabling efficient replay and spectating.

---

## 7. CSI vs GPR Framing

### CSI Perspective: Rollback as Optimistic Replication

The CSI engineer recognizes rollback as an instance of **optimistic replication** (or optimistic concurrency control):

- **Speculate**: apply changes locally without coordination (local simulation).
- **Detect conflict**: compare local state against authoritative state (server correction).
- **Resolve conflict**: rewind and replay with authoritative data (rollback + replay).

This is the same pattern used in distributed databases (optimistic transactions), version control systems (merge conflicts), and collaborative editing (OT/CRDT conflict resolution).

The tradeoff is the same everywhere: optimistic approaches reduce latency but increase the cost of conflict resolution when speculation fails.

### GPR Perspective: Rollback as Invisible Authority

The GPR engineer's goal is to make rollback invisible. The player should never notice corrections. Success metrics:

- **Correction frequency**: what percentage of frames require rollback? (Target: < 5% on a typical connection.)
- **Correction magnitude**: when rollback occurs, how many pixels/units does the player's character move? (Target: < character width.)
- **Correction visibility**: can the player distinguish a rolled-back frame from a non-rolled-back frame? (Target: no.)

When these metrics are met, rollback achieves the ideal: the responsiveness of client authority with the correctness of server authority.
