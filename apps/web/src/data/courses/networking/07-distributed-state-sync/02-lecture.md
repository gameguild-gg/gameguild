# Lecture 07: Distributed State and Synchronization

## Overview

This lecture covers how to keep shared state consistent across clients and servers—**distributed state and synchronization**.

We'll explore state sync models (client-server vs P2P) from both distributed systems and game networking perspectives, why authoritative servers and "never trust the client" matter, how server reconciliation keeps applications responsive, and delta compression for bandwidth efficiency.

---

## Lecture Sections

This lecture is divided into the following sections for easier navigation:

### [1. State Synchronization Models](./lecture/state-sync-models)

Client-server vs P2P architectures. State sync (send state) vs input sync (send inputs; server simulates). **CSI:** Distributed systems patterns, replication, consistency. **GPR:** When to use each model in games.

### [2. Authoritative Server and Never Trust the Client](./lecture/authoritative-server)

Why the server must be the source of truth. **CSI:** Zero-trust principle, input validation in APIs and microservices. **GPR:** Anti-cheat, server authority as the foundation of secure multiplayer. Never trust client-reported position, scores, or critical state.

### [3. Server Reconciliation](./lecture/server-reconciliation)

Client-side prediction and server reconciliation. **CSI:** Optimistic concurrency control, conflict resolution. **GPR:** Input sequences, correction flow—client predicts, server decides, client corrects. Keeping applications responsive despite latency.

### [4. Delta Compression](./lecture/delta-compression)

Send deltas (changes) instead of full state. **CSI:** Incremental replication, log-based sync. **GPR:** Selective updates—only changed objects. Bandwidth vs accuracy tradeoffs. XOR trick and other techniques for networked state compression.

### [5. Summary and Quick Reference](./lecture/summary)

Recap of state sync patterns, authority, reconciliation, and delta compression. Guidelines for choosing the right approach for your application.

---

## Quick Reference

| Topic                    | Key Takeaway                                                                               |
| ------------------------ | ------------------------------------------------------------------------------------------ |
| State sync vs input sync | State sync: send state; Input sync: send inputs, server simulates                          |
| Client-server vs P2P     | Client-server: single authority; P2P: distributed, harder to secure                        |
| Never trust the client   | Server validates all inputs; never accept client-reported position or scores               |
| Server reconciliation    | Client predicts locally; server decides; client corrects when authoritative update arrives |
| Delta compression        | Send only changes (deltas), not full state; reduces bandwidth                              |
