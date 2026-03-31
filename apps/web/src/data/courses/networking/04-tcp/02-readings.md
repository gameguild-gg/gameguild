# Week 04 Readings: TCP and Stream Sockets

::: note "About Beej's Guide"

Beej's Guide uses C and POSIX/Linux system calls directly. In this course, we'll use **Boost.Asio** for cross-platform compatibility (Windows, macOS, Linux). The concepts are identical. Beej teaches the underlying socket API that Boost.Asio wraps. Understanding the raw API helps you debug issues and read documentation for any networking library.

:::

| #   | Reading                                                                                                                              | Time   | Covers                                                                                        |
| --- | ------------------------------------------------------------------------------------------------------------------------------------ | ------ | --------------------------------------------------------------------------------------------- |
| 1   | [RFC 793 (TCP)](https://datatracker.ietf.org/doc/html/rfc793) — Sections 1-3.4 only                                                  | 25 min | TCP header format, connection states, three-way handshake, termination                        |
| 2   | Glenn Fiedler, ["Client/Server Connection over UDP"](https://gafferongames.com/post/client_server_connection/)                       | 15 min | head-of-line blocking motivation, connection semantics, and time-critical transport tradeoffs |
| 3   | Beej's Guide, [Ch. 5.4–5.7 "System Calls"](https://beej.us/guide/bgnet/html/split/system-calls-or-bust.html#connect)                 | 25 min | `connect()`, `listen()`, `accept()`, `send()`, `recv()`                                       |
| 4   | Beej's Guide, [Ch. 6.1–6.2 "Client-Server Background"](https://beej.us/guide/bgnet/html/split/client-server-background.html)         | 20 min | Complete TCP client/server example, connection flow                                           |
| 5   | Beej's Guide, [Ch. 7.3 "Handling Partial send()s"](https://beej.us/guide/bgnet/html/split/slightly-advanced-techniques.html#sendall) | 10 min | Stream semantics, message boundaries, `sendall()` pattern                                     |
| 6   | Peterson & Davie, [Ch. 5.2 "Reliable Byte Stream (TCP)"](https://book.systemsapproach.org/e2e/tcp.html)                              | 30 min | Sliding window, sequence numbers, retransmission, flow control                                |
| 7   | Peterson & Davie, [Ch. 6.3 "TCP Congestion Control"](https://book.systemsapproach.org/congestion/tcpcc.html)                         | 25 min | Slow start, congestion avoidance, AIMD, fast retransmit/recovery                              |

**Total reading time: ~150 minutes (~2.5 hours)**

---

## Videos (Pick One or Two)

I couldn't find a working link for that specific Hussein Nasser video on TCP connection states. I'll replace it with a more reliable alternative. Here's the updated Videos section:

---

## Videos (Pick One or Two)

| Resource                                                                                                                        | Time   | What it covers                                                 |
| ------------------------------------------------------------------------------------------------------------------------------- | ------ | -------------------------------------------------------------- |
| Ben Eater, ["TCP handshake"](https://www.youtube.com/watch?v=F27PLin3TV0)                                                       | 12 min | Visual walkthrough of SYN/SYN-ACK/ACK with packet captures     |
| Computerphile, ["TCP Meltdown"](https://www.youtube.com/watch?v=AAssk2N_oPk)                                                    | 12 min | Why TCP-over-TCP fails, reliability trade-offs                 |
| javidx9, ["Networking in C++"](https://www.youtube.com/watch?v=2hNdkYInj4g&list=PLIXt8mu2KcUJOwdLMp-Z-cDIZA1aZfVTN) (Parts 1-2) | 60 min | TCP client/server with custom framework, connection management |
| Sunny Classroom, ["TCP Connection States Explained"](https://www.youtube.com/watch?v=jE0mfLB-NEA)                               | 18 min | All TCP states with state diagram walkthrough                  |

---

## Interactive Practice

| Resource                                                                                               | Time   | What it does                                                                    |
| ------------------------------------------------------------------------------------------------------ | ------ | ------------------------------------------------------------------------------- |
| [Wireshark TCP analysis guide](https://www.wireshark.org/docs/wsug_html_chunked/ChAdvTCPAnalysis.html) | 25 min | Analyze handshake, retransmissions, sequence behavior, and flow-control signals |
| [Kurose/Ross TCP Lab](https://gaia.cs.umass.edu/kurose_ross/interactive/)                              | 15 min | Self-quiz on TCP segment structure, sequence/ack numbers                        |
| [TCP State Diagram Practice](https://www.cs.umd.edu/~shankar/417-F01/Slides/chapter3b/sld010.htm)      | 10 min | Trace through connection states manually                                        |

---

## C++ / Boost.Asio Resources

| Resource                                                                                                                                  | Time   | What it covers                                  |
| ----------------------------------------------------------------------------------------------------------------------------------------- | ------ | ----------------------------------------------- |
| Boost.Asio, [TCP Daytime Client](https://www.boost.org/doc/libs/latest/doc/html/boost_asio/tutorial/tutdaytime1.html)                     | 15 min | Synchronous TCP client using `tcp::socket`      |
| Boost.Asio, [TCP Daytime Server](https://www.boost.org/doc/libs/latest/doc/html/boost_asio/tutorial/tutdaytime2.html)                     | 20 min | Synchronous TCP server using `tcp::acceptor`    |
| Boost.Asio, [TCP Echo Example](https://www.boost.org/doc/libs/latest/doc/html/boost_asio/example/cpp11/echo/blocking_tcp_echo_server.cpp) | 15 min | Complete echo server with connection handling   |
| Boost.Asio, [Socket Options](https://www.boost.org/doc/libs/latest/doc/html/boost_asio/reference/socket_base.html)                        | 10 min | `keep_alive`, `reuse_address`, `linger` options |

---

## Optional Deep Dive

### RFCs & Standards

- [RFC 793 (TCP)](https://datatracker.ietf.org/doc/html/rfc793) — The original TCP specification (read sections 3.5-3.9 for state machine details)
- [RFC 1122 "Host Requirements"](https://datatracker.ietf.org/doc/html/rfc1122#section-4.2) — Section 4.2 clarifies TCP implementation requirements
- [RFC 5681 "TCP Congestion Control"](https://datatracker.ietf.org/doc/html/rfc5681) — Modern congestion control algorithms (slow start, congestion avoidance)
- [RFC 6298 "Computing TCP Retransmission Timer"](https://datatracker.ietf.org/doc/html/rfc6298) — RTO calculation with smoothed RTT

### Game Networking Context (GPR students)

- Glenn Fiedler, ["Reliability and Flow Control"](https://gafferongames.com/post/reliability_ordering_and_congestion_avoidance_over_udp/) — Building TCP-like features over UDP for games
- [Multiplayer Game Programming](https://www.oreilly.com/library/view/multiplayer-game-programming/9780134034355/#toc) — Ch. 4 covers when TCP is acceptable (lobby, login, chat)

### Distributed Systems Context (CSI students)

- Peterson & Davie, [Ch. 5.2.6 "TCP Extensions"](https://book.systemsapproach.org/e2e/tcp.html#tcp-extensions) — Large windows, timestamps, SACK
- [The C10K Problem](http://www.kegel.com/c10k.html#simul) — Managing many TCP connections efficiently
- Kleppmann, [Designing Data-Intensive Applications: notes and resources](https://martin.kleppmann.com/2015/05/27/logs-for-data-infrastructure.html) — distributed-systems context for transport guarantees and system behavior

### Socket API Deep Dive

- Beej's Guide, [Ch. 7.2 "select()"](https://beej.us/guide/bgnet/html/split/slightly-advanced-techniques.html#select) — Handling multiple connections
- POSIX, [`listen(2)` man page](https://man7.org/linux/man-pages/man2/listen.2.html) — Backlog queue for pending connections
- POSIX, [`shutdown(2)` man page](https://man7.org/linux/man-pages/man2/shutdown.2.html) — Graceful vs abrupt connection termination
- POSIX, [`setsockopt(2)` SO_LINGER](https://man7.org/linux/man-pages/man7/socket.7.html) — Controlling close behavior

### Boost.Asio Deep Dive

- [Boost.Asio Chat Example](https://www.boost.org/doc/libs/1_84_0/doc/html/boost_asio/example/cpp11/chat/) — Multi-client chat server (reference for assignment)
- [Boost.Asio TCP Reference](https://www.boost.org/doc/libs/1_84_0/doc/html/boost_asio/reference/ip__tcp.html) — Complete API documentation
- [Graceful Shutdown Pattern](https://www.boost.org/doc/libs/1_84_0/doc/html/boost_asio/overview/networking/other_protocols.html) — `shutdown()` before `close()`

---

## Key Concepts Summary

### TCP Connection Lifecycle

```
Client                              Server
  |                                   |
  |  -------- SYN (seq=x) -------->   |   (LISTEN → SYN_RECEIVED)
  |                                   |
  |  <--- SYN-ACK (seq=y, ack=x+1) -- |
  |                                   |
  |  -------- ACK (ack=y+1) ------->  |   (ESTABLISHED)
  |                                   |
  |        ... data exchange ...      |
  |                                   |
  |  -------- FIN --------------->    |   (FIN_WAIT_1)
  |  <------- ACK ----------------    |   (CLOSE_WAIT)
  |  <------- FIN ----------------    |   (LAST_ACK)
  |  -------- ACK --------------->    |   (TIME_WAIT → CLOSED)
```

### TCP vs UDP Quick Reference

| Aspect             | TCP                          | UDP                       |
| ------------------ | ---------------------------- | ------------------------- |
| Connection         | Required (handshake)         | None                      |
| Reliability        | Guaranteed delivery, ordered | Best-effort, unordered    |
| Flow control       | Yes (sliding window)         | None                      |
| Congestion control | Yes (slow start, AIMD)       | None                      |
| Head-of-line block | Yes (problematic for games)  | No                        |
| Message boundaries | No (byte stream)             | Yes (datagrams preserved) |
| Use case           | Chat, file transfer, HTTP    | Voice, video, game state  |

---

## Assignment Preparation

This week's assignment involves building a **TCP chat application** with multiple clients, proper connection handling, and graceful shutdown. Key concepts to understand:

1. **Acceptor Pattern**: Server uses `tcp::acceptor` to listen and accept incoming connections in a loop
2. **Stream Semantics**: TCP is a byte stream—you must handle message framing (e.g., newline-delimited or length-prefixed)
3. **Graceful Shutdown**: Use `socket.shutdown(tcp::socket::shutdown_both)` before `close()` to ensure FIN is sent
4. **Connection Tracking**: Server must track connected clients to broadcast messages to all participants
5. **`reuse_address` Option**: Set `acceptor.set_option(tcp::acceptor::reuse_address(true))` to avoid "address already in use" errors during development

**Recommended reading order for the assignment:**

1. RFC 793 §3.1-3.4 → understand the protocol and states
2. Beej Ch. 5.4-5.7 → learn connect/listen/accept/send/recv
3. Beej Ch. 6.1-6.2 → see working TCP client/server
4. Beej Ch. 7.3 → understand partial sends and stream handling
5. Boost.Asio TCP tutorials → translate to Boost.Asio
6. Boost.Asio chat example (optional deep dive) → reference architecture

**Common pitfalls:**

- Forgetting TCP doesn't preserve message boundaries (one `send()` ≠ one `recv()`)
- Not handling partial reads/writes
- Ignoring `TIME_WAIT` state causing bind failures
- Abrupt `close()` without `shutdown()` can lose buffered data
