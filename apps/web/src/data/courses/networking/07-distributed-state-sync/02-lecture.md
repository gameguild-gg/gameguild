# Lecture 07: Distributed State and Synchronization

## Overview

This lecture covers how to keep shared state consistent across clients and servers—**distributed state and synchronization**.

We'll explore state sync models (client-server vs P2P) from both distributed systems and game networking perspectives, P2P state sync (lockstep, host authority, state broadcast), why authoritative servers and "never trust the client" matter, host authority in P2P (listen server), how server reconciliation keeps applications responsive, and delta compression for bandwidth efficiency.

---

## Lecture Sections

This lecture is divided into the following sections for easier navigation:

### [1. State Synchronization Models](./lecture/state-sync-models)

Client-server vs P2P architectures. State sync (send state) vs input sync (send inputs; server simulates). **P2P state sync:** Lockstep (deterministic, input-only; e.g., RTS), host authority (one peer acts as server), state broadcast (no single source of truth). **CSI:** Distributed systems patterns, replication, consistency; CAP theorem—P2P often favors availability (AP). **GPR:** When to use each model in games.

### [2. Authoritative Server and Never Trust the Client](./lecture/authoritative-server)

Why the server must be the source of truth. **Host authority in P2P:** Listen server (one peer hosts)—host migration when host leaves; "never trust the client" is harder in full P2P (no central validator). **CSI:** Zero-trust principle, input validation in APIs and microservices. **GPR:** Anti-cheat, server authority as the foundation of secure multiplayer. Never trust client-reported position, scores, or critical state.

### [3. Server Reconciliation](./lecture/server-reconciliation)

Client-side prediction and server reconciliation. **CSI:** Optimistic concurrency control, conflict resolution. **GPR:** Input sequences, correction flow—client predicts, server decides, client corrects. **P2P conflict resolution:** When peers disagree—host decides, last-writer-wins, or basic merge strategies. Keeping applications responsive despite latency.

### [4. Delta Compression](./lecture/delta-compression)

Send deltas (changes) instead of full state. **CSI:** Incremental replication, log-based sync. **GPR:** Selective updates—only changed objects. Bandwidth vs accuracy tradeoffs. XOR trick and other techniques for networked state compression.

---

## Quick Reference

| Topic                    | Key Takeaway                                                                               |
| ------------------------ | ------------------------------------------------------------------------------------------ |
| State sync vs input sync | State sync: send state; Input sync: send inputs, server simulates                          |
| Client-server vs P2P     | Client-server: single authority; P2P: distributed, harder to secure                        |
| P2P lockstep             | Deterministic sim; peers send only inputs; common in RTS                                   |
| Host authority in P2P    | Listen server: one peer hosts; host migration when host leaves                             |
| CAP and P2P              | P2P often favors availability (AP); eventual consistency when partitioned                  |
| Never trust the client   | Server validates all inputs; harder in full P2P (no central validator)                     |
| Server reconciliation    | Client predicts locally; server decides; client corrects when authoritative update arrives |
| Delta compression        | Send only changes (deltas), not full state; reduces bandwidth                              |
