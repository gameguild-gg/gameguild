# Lecture 05: Message Framing, Buffering, and Concurrency

## Overview

This lecture covers how to reconstruct message boundaries from TCP's raw byte stream and the **concurrency models** used to handle network I/O efficiently.

We'll explore framing strategies, buffer management, partial read/write handling, deadlock prevention, and the spectrum from threads to coroutines to fibers.

---

## Lecture Sections

This lecture is divided into the following sections for easier navigation:

### [1. The TCP Framing Problem](lecture/framing-problem)

TCP is a byte stream protocol that doesn't preserve message boundaries. Learn why framing is essential and how message fragmentation occurs.

### [2. Framing Strategies](lecture/framing-strategies)

Compare different approaches to message framing:

- **Length-prefix framing**: `[4-byte length][payload]`
- **Delimiter-based framing**: `[payload][\n]`
- **TLV (Type-Length-Value)**: For extensible protocols
- **Fixed-length framing**: Simple but inflexible

### [3. Buffer Management](lecture/buffer-management)

Understand buffer types in Boost.Asio:

- `boost::asio::buffer()` - Non-owning view
- `boost::asio::streambuf` - Dynamic, owns memory
- Pre-allocated vectors for high performance

### [4. Handling Partial Reads and Writes](lecture/partial-io)

Learn why `read_some()` and `write_some()` may transfer fewer bytes than requested, and how to use composed operations like `boost::asio::read()` and `boost::asio::write()`.

### [5. Deadlock Prevention](lecture/deadlock-prevention)

Understand how TCP deadlock occurs when both ends block on write with full buffers, and solutions including:

- Async I/O
- Separate read/write threads
- Write queues with the async chain pattern

### [6. Concurrency Models: Threads vs Coroutines vs Fibers](lecture/concurrency-models)

Compare the three main concurrency approaches:

- **OS Threads**: Preemptive, kernel-scheduled, 1-8 MB stack each
- **Coroutines**: Cooperative, stackless, ~100-1000 bytes each
- **Fibers**: Cooperative, userspace threads, work stealing

### [7. C++ Concurrency Implementation](lecture/cpp-concurrency)

Practical implementation with:

- `std::jthread` (C++20) with stop tokens
- Boost.Asio callback-based async
- C++20 coroutines with `co_await`
- Boost.Fiber for userspace threads

### [8. Edge Cases and Summary](lecture/edge-cases)

Handle edge cases like connection termination mid-message, message interleaving, and byte order (endianness). Includes assignment preparation checklist.

---

## Quick Reference

| Topic         | Key Takeaway                                                       |
| ------------- | ------------------------------------------------------------------ |
| Framing       | TCP is a byte stream—implement message boundaries                  |
| Length-prefix | Best for binary protocols: `[4-byte len][payload]`                 |
| Partial I/O   | Use `boost::asio::read()` not `read_some()` for complete transfers |
| Deadlock      | Never block on write without reading—use async or write queues     |
| Threads       | Simple but expensive (1-8 MB/thread, context switch overhead)      |
| Coroutines    | Lightweight (100-1000 bytes), scale to millions, no blocking calls |
| Byte order    | Always use `native_to_big()` / `big_to_native()` for wire protocol |
