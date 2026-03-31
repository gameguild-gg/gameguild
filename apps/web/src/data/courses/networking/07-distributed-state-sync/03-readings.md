# Week 07 Readings: Distributed State and Synchronization

::: tip "How to approach these readings"

**CSI:** Start with the CAP Theorem and P2P vs Client-Server to ground distributed systems patterns. Then Gambetta and Fiedler show how these patterns apply to real-time applications. **GPR:** Start with Gambetta's articles to understand authoritative servers and reconciliation—they frame why games use this architecture. Fiedler covers state vs input sync and delta/selective updates. Both audiences: study "never trust the client" for server authority and input validation. Don't memorize protocols; understand the **patterns** that govern distributed state.

:::

| #   | Reading                                                                                                                                                                                                                                  | Time   | Covers                                                                                          |
| --- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------ | ----------------------------------------------------------------------------------------------- |
| 1   | ["An Illustrated Proof of the CAP Theorem"](https://mwhittaker.github.io/blog/an_illustrated_proof_of_the_cap_theorem/)                                                                                                                  | 15 min | Consistency, availability, partition tolerance—tradeoffs in distributed systems                 |
| 2   | [OrbitDB: P2P vs Client-Server](https://github.com/orbitdb/field-manual/blob/main/02_Thinking_Peer_to_Peer/01_P2P_vs_Client-Server.md)                                                                                                   | 10 min | Client-server vs P2P architectures from a distributed systems perspective                       |
| 3   | Gabriel Gambetta, ["Client-Server Game Architecture"](https://www.gabrielgambetta.com/client-server-game-architecture.html)                                                                                                              | 15 min | State sync models, authoritative server, client-server vs naive, "never trust the client" intro |
| 4   | Gabriel Gambetta, ["Client-Side Prediction and Server Reconciliation"](https://www.gabrielgambetta.com/client-side-prediction-server-reconciliation.html)                                                                                | 20 min | Server reconciliation, input sequences, correction flow, responsiveness vs authority            |
| 5   | Glenn Fiedler, ["State Synchronization"](https://gafferongames.com/post/state_synchronization/)                                                                                                                                          | 20 min | State vs input sync, delta/selective updates, bandwidth vs accuracy tradeoffs                   |
| 6   | [State Synchronization Pattern](https://www.eventhelix.com/design-patterns/state-synchronization/) + [Demofox: Compressing Networked State Data](https://blog.demofox.org/2018/06/04/a-neat-trick-for-compressing-networked-state-data/) | 10 min | What delta compression is—send changes vs full state; XOR trick for networked state             |
| 7   | ["Never Trust the Client"](https://www.gamedeveloper.com/business/never-trust-the-client-simple-techniques-against-cheating-in-multiplayer-and-spatialos) (Gamedeveloper.com)                                                            | 15 min | Anti-cheat, server authority, input validation, why clients cannot be trusted                   |

**Total reading time: ~105 minutes (~1h 45m)**

---

## Videos (Pick One or Two)

| Resource                                                                                                          | Time   | What it covers                                                          |
| ----------------------------------------------------------------------------------------------------------------- | ------ | ----------------------------------------------------------------------- |
| ["Understanding the CAP Theorem"](https://www.youtube.com/watch?v=Q6gYVkvcE3I)                                    | 10 min | CAP theorem explained—consistency vs availability under partition (CSI) |
| Martin Kleppmann, ["Turning the Database Inside Out"](https://www.youtube.com/watch?v=fU9hR3kiOK0) (first 20 min) | 20 min | Event sourcing, distributed state—CSI perspective on state sync         |
| GDC 2015, Glenn Fiedler, ["Physics for Game Programmers: Networking"](https://archive.org/details/GDC2015Fiedler) | 60 min | State sync, networked physics, extrapolation (optional, extra material) |

---

## Interactive Practice

| Resource                                                                                                                    | Time   | What it does                                                                                       |
| --------------------------------------------------------------------------------------------------------------------------- | ------ | -------------------------------------------------------------------------------------------------- |
| [Gabriel Gambetta: Client-Side Prediction Live Demo](https://www.gabrielgambetta.com/client-side-prediction-live-demo.html) | 15 min | See prediction + reconciliation in action; play with latency slider to observe correction behavior |
| Hands-on: Trace one "move" through client → server → reconciliation                                                         | 15 min | Draw message flow: input sent, server processes, authoritative update, client correction           |

---

## Optional Deep Dive

### Distributed Systems Context (CSI students)

- Peterson & Davie, [Computer Networks: A Systems Approach](https://book.systemsapproach.org/) — Distributed systems chapter on client-server vs P2P
- ["An Illustrated Proof of the CAP Theorem"](https://mwhittaker.github.io/blog/an_illustrated_proof_of_the_cap_theorem/) — Deeper dive into consistency tradeoffs
- Kleppmann, [Personal Site & Talks](https://martin.kleppmann.com/) — replication and consistency background context
- [OrbitDB: P2P vs Client-Server](https://github.com/orbitdb/field-manual/blob/main/02_Thinking_Peer_to_Peer/01_P2P_vs_Client-Server.md) — Architectural comparison

### P2P State Sync (Both audiences)

- ["The TRIBES Engine Networking Model"](https://www.gamedeveloper.com/programming/the-tribes-engine-networking-model) (Frohnmayer & Gift) — Classic paper on P2P game networking; lockstep, host authority, state broadcast
- [Gabriel Gambetta, "Lag Compensation"](https://www.gabrielgambetta.com/lag-compensation.html) — Practical server-side rewind and fairness tradeoffs for hit validation
- [OrbitDB: P2P Architecture](https://github.com/orbitdb/field-manual) — P2P data sync, eventual consistency (CSI)

### Game Networking Context (GPR students)

- Glenn Fiedler, ["Snapshot Interpolation"](https://gafferongames.com/post/snapshot_interpolation/) — Connects to Week 13; interpolation between snapshots
- [Valve Source Multiplayer Networking](https://developer.valvesoftware.com/wiki/Source_Multiplayer_Networking) — Skim for state sync and authority patterns
- [Conflict-free Replicated Data Types (CRDTs)](https://en.wikipedia.org/wiki/Conflict-free_replicated_data_type) — Conflict resolution patterns for eventually consistent shared state

### Unity / Unreal Documentation

- [Unity Netcode: Dealing with Latency](https://docs-multiplayer.unity3d.com/netcode/current/learn/dealing-with-latency/) — Skim authority and latency sections

---

## Study Tips

::: warning "What to pay attention to"

1. **CAP Theorem**: Consistency vs availability under partition—why distributed systems can't have all three; **P2P often chooses AP** (availability + partition tolerance)
2. **P2P vs Client-Server**: Trade-offs—single authority vs distributed, scalability vs consistency
3. **P2P state sync**: Lockstep (input-only, deterministic), host authority (listen server), state broadcast; conflict resolution when peers disagree
4. **Gambetta Part I**: Why authoritative server? Why send inputs not state? "Never trust the client" as design principle
5. **Gambetta Part II**: Server reconciliation flow—client predicts, server decides, client corrects. Input sequence numbers and when to accept authoritative state
6. **Fiedler State Sync**: State vs input sync trade-offs; selective updates (send only changed objects); bandwidth vs determinism
7. **Delta compression**: Send deltas (changes) instead of full state—reduces bandwidth when state changes incrementally
8. **Never trust the client**: Server validates all inputs; applies to APIs, microservices, and games; harder in full P2P (no central validator)

:::

**Recommended reading order:**

1. CAP Theorem → understand consistency tradeoffs in distributed systems
2. OrbitDB P2P vs Client-Server → architectural patterns
3. Gambetta Part I → understand authoritative server and "never trust the client"
4. Gambetta Part II → server reconciliation flow and input sequencing
5. Fiedler State Synchronization → state vs input sync, selective updates
6. Delta encoding + Demofox → what delta compression is and one practical trick
7. Never trust the client → anti-cheat, server authority, input validation

**Common mistakes to avoid:**

- Trusting client-reported position, health, scores, or critical state (server must validate)
- Assuming prediction and reconciliation apply only to games (optimistic concurrency is universal)
- Confusing state sync (send state) with input sync (send inputs; server simulates)
- Skipping delta compression and sending full state every tick (wastes bandwidth)
- Ignoring conflict resolution in P2P—when peers disagree, you need a strategy (host decides, last-writer-wins, etc.)
