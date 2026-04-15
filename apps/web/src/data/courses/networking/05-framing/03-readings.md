# Week 05 Readings: Message Framing and Buffering

::: tip "How to approach these readings"

Focus on **understanding the problem** before the solutions. The Stephen Cleary article is the best starting point—it clearly explains WHY framing is needed. Then read Beej to see the raw socket API, and finally Boost.Asio to see how modern C++ abstracts it. Don't memorize code; understand the patterns.

:::

| #   | Reading                                                                                                                                                                           | Time   | Covers                                                                          |
| --- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------ | ------------------------------------------------------------------------------- |
| 1   | Stephen Cleary, ["TCP/IP Protocol Design: Message Framing"](https://www.codeproject.com/Articles/37496/TCP-IP-Protocol-Design-Message-Framing)                                    | 25 min | Three framing strategies: length-prefix, delimiter-based, and combined          |
| 2   | Beej's Guide, [Ch. 7 "Slightly Advanced Techniques"](https://beej.us/guide/bgnet/html/split/slightly-advanced-techniques.html)                                                    | 10 min | `sendall()` wrapper, why one `send()` call may not transmit all bytes           |
| 3   | Beej's Guide, [Ch. 7 "Slightly Advanced Techniques"](https://beej.us/guide/bgnet/html/split/slightly-advanced-techniques.html#slightly-advanced-techniques)                       | 20 min | Packing structs for the wire, byte order, length-prefixed encapsulation pattern |
| 4   | Beej's Guide, [Ch. 7 "Slightly Advanced Techniques"](https://beej.us/guide/bgnet/html/split/slightly-advanced-techniques.html#blocking)                                           | 15 min | Multiplexing reads and writes on one thread, preventing blocking deadlock       |
| 5   | Boost.Asio, [Buffers Overview](https://www.boost.org/doc/libs/latest/doc/html/boost_asio/overview/core/buffers.html)                                                              | 15 min | `mutable_buffer`, `const_buffer`, `streambuf`, dynamic buffers, lifetime rules  |
| 6   | Boost.Asio, [Chat Example (`chat_server`)](https://www.boost.org/doc/libs/latest/doc/html/boost_asio/examples/cpp11_examples.html#boost_asio.examples.cpp11_examples.chat_server) | 20 min | Length-prefixed message protocol in practice, async write queue                 |
| 7   | Glenn Fiedler, ["Packet Fragmentation and Reassembly"](https://gafferongames.com/post/packet_fragmentation_and_reassembly/)                                                       | 15 min | Framing large payloads, fragmentation boundaries, and reassembly concerns       |

**Total reading time: ~120 minutes (~2 hours)**

---

## Videos (Pick One or Two)

| Resource                                                                                                                      | Time   | What it covers                                                                     |
| ----------------------------------------------------------------------------------------------------------------------------- | ------ | ---------------------------------------------------------------------------------- |
| javidx9, ["Networking in C++" playlist](https://www.youtube.com/playlist?list=PLIXt8mu2KcUJOwdLMp-Z-cDIZA1aZfVTN) (Parts 3–4) | 60 min | Message headers, body packing, variable-length messages, async write queues in C++ |
| Computerphile, ["TCP vs UDP"](https://www.youtube.com/watch?v=uwoD5YsGACg)                                                    | 14 min | Stream vs datagram semantics—motivates why framing is only a TCP problem           |

---

## Interactive Practice

| Resource                                                                                | Time   | What it does                                                                     |
| --------------------------------------------------------------------------------------- | ------ | -------------------------------------------------------------------------------- |
| [Wireshark HTTP protocol page](https://wiki.wireshark.org/HTTP)                         | 25 min | Find HTTP `Content-Length` headers and `\r\n\r\n` delimiters in real TCP streams |
| [Kurose/Ross Interactive Exercises (alternate portal)](https://gaia.cs.umass.edu/kurose_ross/index.php?page=interactive) | 15 min | Self-quiz on message structure, encapsulation, and protocol headers              |
| Hands-on: Write a hex-dump of a length-prefixed message using `xxd` or a hex editor     | 15 min | Manually construct a `[4-byte length][payload]` frame and verify byte order      |

---

## C++ / Boost.Asio Resources

| Resource                                                                                                                                       | Time   | What it covers                                                       |
| ---------------------------------------------------------------------------------------------------------------------------------------------- | ------ | -------------------------------------------------------------------- |
| Boost.Asio, [async_read](https://www.boost.org/doc/libs/latest/doc/html/boost_asio/reference/async_read.html)                                  | 15 min | Reading exact byte counts with completion conditions (length-prefix) |
| Boost.Asio, [async_read_until](https://www.boost.org/doc/libs/latest/doc/html/boost_asio/reference/async_read_until.html)                      | 15 min | Reading until a delimiter sequence (delimiter-based framing)         |
| Boost.Asio, [streambuf](https://www.boost.org/doc/libs/latest/doc/html/boost_asio/reference/streambuf.html)                                    | 10 min | Dynamic input buffer for accumulating partial reads                  |
| Boost.Asio, [Chat Message Header](https://www.boost.org/doc/libs/latest/doc/html/boost_asio/examples/cpp11_examples.html) (`chat_message.hpp`) | 15 min | Reference implementation: fixed 4-byte header + variable-length body |

---

## Optional Deep Dive

### RFCs & Standards

- [RFC 4571 "Framing RTP/RTCP over Connection-Oriented Transport"](https://datatracker.ietf.org/doc/html/rfc4571) — Clean example of 2-byte length-prefix framing standardized in an RFC
- [RFC 7230 §3.3 "Message Body" (HTTP/1.1)](https://datatracker.ietf.org/doc/html/rfc7230#section-3.3) — How HTTP frames messages: `Content-Length` vs chunked `Transfer-Encoding`
- [RFC 9000 §19 "Frame Types" (QUIC)](https://datatracker.ietf.org/doc/html/rfc9000#section-19) — Modern framing design with variable-length integer encoding
- [RFC 9113 §4 "Frame Format" (HTTP/2)](https://datatracker.ietf.org/doc/html/rfc9113#section-4) — Binary framing layer: 9-byte frame header with length, type, flags, stream ID

### Game Networking Context (GPR students)

- Glenn Fiedler, ["Sending Large Blocks of Data"](https://gafferongames.com/post/sending_large_blocks_of_data/) — Chunking and transfer reliability considerations for large payloads
- Glenn Fiedler, ["Packet Fragmentation and Reassembly"](https://gafferongames.com/post/packet_fragmentation_and_reassembly/#fragmentation-overview) — Splitting large messages across multiple packets (framing at a higher level)
- [Multiplayer Game Programming](https://www.oreilly.com/library/view/multiplayer-game-programming/9780134034355/#ch05) — Ch. 5 covers message serialization, framing, and type-dispatching for game objects
- [Cap'n Proto Encoding](https://capnproto.org/encoding.html) — Zero-copy schema layout and framing-friendly message format

### Distributed Systems Context (CSI students)

- [Protocol Buffers Overview](https://protobuf.dev/overview/) — Typed schema workflow and cross-language serialization model
- [gRPC Core Concepts](https://grpc.io/docs/what-is-grpc/core-concepts/) — RPC framing, streaming, and message lifecycle over HTTP/2
- [The C10K Problem](http://www.kegel.com/c10k.html#resources) — Non-blocking I/O strategies that prevent deadlock at scale
- Kleppmann, [Technical writing archive](https://martin.kleppmann.com/archive.html) — practical deep dives on distributed systems and data architecture

### Boost.Asio Deep Dive

- [Boost.Asio Composed Operations](https://www.boost.org/doc/libs/latest/doc/html/boost_asio/overview/composition.html) — Building custom async framing operations from primitives
- [Boost.Asio Custom Completion Conditions](https://www.boost.org/doc/libs/latest/doc/html/boost_asio/reference/async_read/overload2.html) — Read exactly N bytes or until a condition is met
- [Boost.CircularBuffer](https://www.boost.org/doc/libs/latest/doc/html/circular_buffer.html) — Ring buffer for efficient buffering of incoming framed data
- [Boost.Asio Strands](https://www.boost.org/doc/libs/latest/doc/html/boost_asio/overview/core/strands.html) — Serializing async handlers to prevent write interleaving (deadlock-free writes)

### Concurrency Models (Threading vs Coroutines vs Fibers)

- CppCon, [Dmitry Nesteruk, "Introduction to Coroutines"](https://www.youtube.com/watch?v=ZTqHjjm86Bw) — C++20 coroutines basics
- Lewis Baker, ["Asymmetric Transfer"](https://lewissbaker.github.io/) — Deep dive into C++ coroutines mechanics
- [Boost.Fiber Documentation](https://www.boost.org/doc/libs/latest/libs/fiber/doc/html/index.html) — Userspace threads (fibers) for cooperative multitasking
- [Boost.Asio Coroutines](https://www.boost.org/doc/libs/latest/doc/html/boost_asio/overview/composition/cpp20_coroutines.html) — Using C++20 coroutines with Asio
- Christian Gyrling, ["Parallelizing the Naughty Dog Engine Using Fibers"](https://www.gdcvault.com/play/1022186/parallelizing-the-naughty-dog-engine) (GDC 2015) — How Naughty Dog replaced threads with fibers for job scheduling in game engines

---

## Study Tips

::: warning "What to pay attention to"

1. **Stephen Cleary article**: Understand the trade-offs table—when to use length-prefix vs delimiter
2. **Beej's sendall()**: Notice how it loops until all bytes are sent—this is the core pattern
3. **Boost.Asio Buffers**: Focus on `streambuf` and buffer lifetime—common source of bugs
4. **Chat Example**: Study `chat_message.hpp`—it's almost identical to what you'll build

:::

**Recommended reading order:**

1. Stephen Cleary article → understand the problem and strategies
2. Beej Ch. 7.3 → see the raw sendall() loop
3. Beej Ch. 7.4–7.5 → learn encapsulation patterns
4. Boost.Asio Buffers → understand buffer types
5. Boost.Asio Chat Example → reference implementation
6. Beej Ch. 7.2 select() → deadlock prevention (if time permits)

**Common mistakes to avoid:**

- Assuming one `send()` = one `recv()` (it doesn't!)
- Forgetting byte order conversion (`native_to_big`/`big_to_native`)
- Not handling partial reads in your receive loop
- Trusting untrusted length headers without size limits
