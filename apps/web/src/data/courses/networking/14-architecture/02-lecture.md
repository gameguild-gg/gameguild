# Lecture 14: Server Architecture and Session Management

## Overview

This lecture covers the **architectural decisions** that determine how a networked game is built: who has authority over game state, what kind of servers run the simulation, how players find and join games, and how the infrastructure scales to serve them. We also connect these practical decisions to **distributed systems theory** — consensus, replication, failure detection, and the CAP theorem — showing that every game networking problem has a formal foundation.

We frame decisions differently for **CSI (distributed systems, infrastructure, formal properties)** and **GPR (player experience, session flow, perceived fairness)**.

---

## Lecture Sections

This lecture is divided into the following sections for easier navigation:

### 1. Authority Models: Who Owns the Truth?

Who decides the "real" game state when clients disagree:

- Full client authority (simple but exploitable)
- Server authoritative model (input authority vs state authority)
- Distributed authority and authority transfer in P2P
- Genre fitness: which authority model fits which game type
- CSI framing: consistency models / GPR framing: player feel and responsiveness

### 2. Dedicated vs Listen Servers

The infrastructure decision that shapes cost, fairness, and session stability:

- Dedicated server properties: stateful, expensive, fair
- Listen server properties: host advantage, disconnect risk, free
- Host migration: algorithm, failure modes, and strategies
- Hybrid models (local dedicated, relay-backed, cloud-assisted listen)
- Decision framework by game requirements

### 3. Rollback Networking Concepts

How action games hide authority latency through speculative execution:

- The core rollback algorithm (rewind, replay with corrected inputs)
- Requirements: determinism, state snapshots, fast resimulation
- Fighting games vs shooters: different rollback tradeoffs
- Artifacts and mitigation (teleporting, animation pops)
- Lockstep as the predecessor and its blocking limitation

### 4. Session Management and Connection Lifecycle

How players find, join, play in, and leave game sessions:

- Session as a distributed resource with lifecycle states
- Session vs match vs lobby: the terminology distinction
- Connection lifecycle: creation, joining, disconnection, reconnection
- Session discovery: server browsers, matchmakers, invites
- Connection brokering and platform services (Steam, Epic, consoles)

### 5. Matchmaking: Finding Fair, Fast, Fun Games

The multi-objective optimization problem of grouping players:

- Queue time vs match quality: the fundamental tradeoff
- Skill rating systems (Elo, Glicko, TrueSkill, OpenSkill)
- Matchmaking architecture: queues, pools, match functions, assignment
- Latency and region constraints in player matching
- Party balancing and population health

### 6. Scaling Game Servers

How to serve a global player base without over-provisioning or under-serving:

- Stateful vs stateless: why game servers are hard to scale
- Horizontal scaling: fleet management, allocation, graceful draining
- Server orchestration: Agones (Kubernetes) and PlayFab (managed)
- Region management and global distribution strategies
- Monitoring, observability, and capacity planning

### 7. Distributed Systems Foundations for Game Networking

The theoretical backbone connecting all architecture decisions:

- Games as distributed systems: the mapping from theory to practice
- Consensus and authority (single-leader, multi-leader, Raft)
- Failure detection: timeouts, heartbeats, the uncertainty problem
- CAP theorem applied to games (consistency vs availability under partition)
- Replication strategies and coordination avoidance

### 8. Architecture Decision Patterns: Putting It All Together

Choosing the right architecture for your game:

- The multi-dimensional decision space (authority × server type × sync × scale)
- Genre-driven architecture patterns (fighting, FPS, battle royale, MMO, co-op)
- Decision flow: player count → competitive/co-op → latency sensitivity → budget
- Common architecture mistakes and how to avoid them
- CSI vs GPR summary across all Week 14 topics

---

## Quick Reference

| Topic                       | Key Takeaway                                                                       |
| --------------------------- | ---------------------------------------------------------------------------------- |
| Authority models            | Server authority provides consistency; client authority provides responsiveness    |
| Dedicated vs listen servers | Dedicated = fair and stable but costly; listen = free but has host advantage       |
| Rollback networking         | Speculative execution hides latency; requires deterministic simulation             |
| Session management          | Sessions are distributed resources with create/join/play/leave lifecycle           |
| Matchmaking                 | Multi-objective optimization balancing skill, latency, queue time, and party size  |
| Scaling                     | Game servers are stateful — scaling requires lifecycle-aware orchestration         |
| Distributed systems         | Every game networking concept maps to a distributed systems fundamental            |
| Architecture decisions      | Genre requirements drive authority, server type, sync strategy, and scale approach |
| CSI framing                 | Formal properties: consistency, availability, consensus, replication               |
| GPR framing                 | Player experience: responsiveness, fairness, session stability, perceived quality  |
