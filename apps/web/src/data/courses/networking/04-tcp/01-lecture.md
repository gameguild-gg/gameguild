# Lecture 04: TCP and Stream Sockets

## Overview

This lecture covers the Transmission Control Protocol (TCP), the reliable transport layer protocol that powers most internet applications. We'll explore how TCP establishes connections, ensures reliable delivery, manages flow and congestion, and how to implement TCP clients and servers using Boost.Asio.

---

## Lecture Sections

This lecture is divided into the following sections for easier navigation:

### [1. Introduction to TCP](lecture/introduction)

Introduction to TCP as a connection-oriented, reliable, byte-stream protocol. When to use TCP vs UDP.

### [2. Connection Establishment](lecture/connection-establishment)

The TCP three-way handshake and connection state machine:

- **SYN → SYN-ACK → ACK** handshake sequence
- Connection states (LISTEN, ESTABLISHED, TIME_WAIT, etc.)
- Why TIME_WAIT exists and its practical impact

### [3. Reliability Mechanisms](lecture/reliability)

How TCP ensures reliable delivery:

- Sequence and acknowledgment numbers
- Byte stream vs message protocol
- Retransmission: timeout-based and fast retransmit

### [4. Flow Control](lecture/flow-control)

Preventing receiver buffer overflow:

- Sliding window protocol
- Window advertisements
- Zero window and window probes

### [5. Congestion Control](lecture/congestion-control)

Preventing network congestion:

- Congestion window (cwnd)
- Slow start (exponential growth)
- AIMD (Additive Increase, Multiplicative Decrease)

### [6. Connection Termination and Protocol Comparison](lecture/termination-comparison)

Graceful connection close and TCP vs UDP:

- Four-way handshake (FIN exchange)
- Head-of-line blocking
- When to choose TCP vs UDP

### [7. TCP Programming with Boost.Asio](lecture/boost-asio)

Practical socket programming:

- Client connection setup
- Server setup with acceptor
- Socket options and graceful shutdown

### [8. Multi-Client Connection Management](lecture/multi-client)

Building a chat server:

- Server architecture: `start()` and `accept_connection()`
- `io_context.run()` and the async event loop
- User registry with modern C++ (`std::shared_mutex`, `std::jthread`)
- Processing chat commands (`/quit`, `/list`, `/msg`)
- Graceful disconnect flow

### [9. Alternative Concurrency Models](lecture/concurrency-models)

Beyond thread-per-client:

- Comparison: threads vs async I/O
- Full async chat server example with Boost.Asio

### [10. Common TCP Issues and Debugging](lecture/debugging)

Troubleshooting and summary:

- "Address Already in Use" error
- Connection refused, data loss, hangs
- Using `netstat` and `ss` to inspect connections
- Summary and assignment checklist

---

## Quick Reference

| Topic               | Key Takeaway                                           |
| ------------------- | ------------------------------------------------------ |
| TCP vs UDP          | TCP: reliable, ordered, connection-oriented            |
| Three-way handshake | SYN → SYN-ACK → ACK establishes connection             |
| Byte stream         | TCP doesn't preserve message boundaries—use framing    |
| Flow control        | Receiver advertises window size to prevent overflow    |
| Congestion control  | Slow start + AIMD prevents network congestion          |
| Graceful close      | Always `shutdown()` before `close()`                   |
| Multi-client        | Thread per client with `std::jthread` for simplicity   |
| Async I/O           | Single thread handles thousands via `io_context.run()` |

