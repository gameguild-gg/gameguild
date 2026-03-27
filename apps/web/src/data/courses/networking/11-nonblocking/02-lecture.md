# Lecture 11: Non-Blocking I/O, Parallelism, and Concurrency

## Overview

This lecture covers how to build responsive networked systems using **non-blocking I/O, parallelism, and concurrency**.

We'll explore blocking vs non-blocking sockets, readiness multiplexing (`select`/`poll`/`epoll`) as concepts, event-loop architecture, worker-thread patterns, thread safety, and modern async orchestration. We’ll connect these ideas to both **CSI (event-driven server architecture)** and **GPR (game loop integration)**, including practical worker patterns in Unity/.NET and modern C++ (`std::jthread`, cooperative cancellation, coroutines).

---

## Lecture Sections

This lecture is divided into the following sections for easier navigation:

### 1. Parallelism vs Concurrency Fundamentals

Concurrency is about **managing many in-flight tasks**; parallelism is about **executing tasks simultaneously**. Learn where each model applies in networking and game systems, and why architecture choices matter more than raw thread count.

### 2. Blocking vs Non-Blocking Sockets

Understand the behavioral contract of blocking vs non-blocking calls:

- Blocking calls wait until work can proceed
- Non-blocking calls return immediately (`would_block` / retry later)
- Throughput, latency, and responsiveness trade-offs
- Why non-blocking design needs explicit readiness and backpressure strategy

### 3. I/O Multiplexing Concepts: select, poll, epoll

How readiness multiplexing works conceptually:

- Watch many sockets, act only when they become ready
- Event loop + readiness set + dispatch step
- `select`/`poll`/`epoll` as backend strategies behind higher-level abstractions
- Mapping the same architecture to both server loops and game networking loops

### 4. Event Loops and Reactor-Style Architecture

Build the mental model for reactor-style systems:

- Register interest (read/write/timer)
- Wait for readiness
- Dispatch handlers
- Keep handlers short and non-blocking

How Boost.Asio expresses this via `io_context`, async operations, and composed handlers.

### 5. Worker Threads and Thread Managers

Designing a worker model that scales:

- Main loop/event loop owns orchestration
- Worker pool owns expensive/background tasks
- Queue or channel handoff for bidirectional communication
- Poll/collect completion without stalling the primary loop

Includes Unity-oriented patterns (`JobHandle` polling, main-thread boundaries) and .NET/C++ queue-based manager design.

### 6. Thread Safety and Shared-State Ownership

Thread safety is a design property, not a patch:

- Data ownership and immutability boundaries
- Strands/serialized execution vs mutex-based protection
- Producer/consumer queues to reduce lock contention
- Common race-condition failure modes in networking code

### 7. Modern C++ Concurrency: jthread, Stop Tokens, Coroutines

Modern C++ tools for maintainable concurrency:

- `std::jthread` for RAII thread lifecycle management
- Cooperative cancellation with stop tokens
- Coroutines (`co_await`) for structured async flow
- Combining coroutine orchestration with event-loop I/O for cleaner state machines

### 8. CSI vs GPR Architecture Patterns

Apply the same primitives in different domains:

- **CSI:** event-driven services, bounded worker pools, safe async pipelines
- **GPR:** game-loop-safe polling, frame-budget-aware background processing
- Choosing between callback, polling, and coroutine flows based on system constraints

---

## Quick Reference

| Topic                      | Key Takeaway                                                                 |
| -------------------------- | ---------------------------------------------------------------------------- |
| Concurrency vs parallelism | Concurrency structures many tasks; parallelism executes tasks simultaneously |
| Blocking socket            | Call waits; simpler flow, can stall responsiveness                           |
| Non-blocking socket        | Call returns immediately; requires readiness + retry strategy                |
| Multiplexing               | One loop can manage many sockets via readiness events                        |
| Reactor pattern            | Register interest → wait → dispatch handler                                  |
| Worker thread manager      | Main loop orchestrates; workers process; queue/channel handoff               |
| Thread safety              | Prefer ownership boundaries/serialization over ad-hoc locking                |
| `std::jthread`             | Safer thread lifecycle with cooperative cancellation support                 |
| Coroutines                 | Simplify async control flow while preserving non-blocking execution          |
| CSI vs GPR framing         | Same primitives, different performance and responsiveness priorities         |
