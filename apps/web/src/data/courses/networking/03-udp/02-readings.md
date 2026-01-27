# Week 03 Readings: UDP and Datagram Sockets

::: note "About Beej's Guide"

Beej's Guide uses C and POSIX/Linux system calls directly. In this course, we'll use **Boost.Asio** for cross-platform compatibility (Windows, macOS, Linux). The concepts are identical. Beej teaches the underlying socket API that Boost.Asio wraps. Understanding the raw API helps you debug issues and read documentation for any networking library.

:::

| #   | Reading                                                                                                                                                        | Time   | Covers                                                           |
| --- | -------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------ | ---------------------------------------------------------------- |
| 1   | [RFC 768 (UDP)](https://datatracker.ietf.org/doc/html/rfc768)                                                                                                  | 10 min | UDP header format, checksum, protocol specification (only 3 pgs) |
| 2   | Glenn Fiedler, ["UDP vs. TCP"](https://gafferongames.com/post/udp_vs_tcp/)                                                                                     | 15 min | Why games use UDP, latency vs reliability trade-offs             |
| 3   | Beej's Guide to Network Programming, [Ch. 2 "What is a socket?"](https://beej.us/guide/bgnet/html/split/what-is-a-socket.html)                                 | 15 min | Socket types (stream vs datagram), layered network model         |
| 4   | Beej's Guide to Network Programming, [Ch. 5.1–5.3 "System Calls"](https://beej.us/guide/bgnet/html/split/system-calls-or-bust.html)                            | 20 min | `getaddrinfo()`, `socket()`, `bind()` - core socket setup        |
| 5   | Beej's Guide to Network Programming, [Ch. 5.8 "sendto() and recvfrom()"](https://beej.us/guide/bgnet/html/split/system-calls-or-bust.html#sendtorecv)          | 15 min | UDP-specific send/receive, DGRAM-style communication             |
| 6   | Beej's Guide to Network Programming, [Ch. 6.3 "Datagram Sockets"](https://beej.us/guide/bgnet/html/split/client-server-background.html#datagram)               | 20 min | Complete UDP client/server example (listener + talker)           |
| 7   | Beej's Guide to Network Programming, [Ch. 7.7 "Broadcast Packets"](https://beej.us/guide/bgnet/html/split/slightly-advanced-techniques.html#broadcast-packets) | 15 min | `SO_BROADCAST`, broadcast addresses, LAN discovery               |

**Total reading time: ~110 minutes (~1.8 hours)**



---

## Videos (Pick One)

| Resource                                                                                                                  | Time   | What it covers                                           |
| ------------------------------------------------------------------------------------------------------------------------- | ------ | -------------------------------------------------------- |
| Ben Eater, ["Networking tutorial"](https://www.youtube.com/playlist?list=PLowKtXNTBypH19whXTVoG3oKSuOcw_XeW) (videos 1-5) | 45 min | Visual explanation of packets, UDP headers, port numbers |
| Jacob Sorber, ["How to write a UDP client/server"](https://www.youtube.com/watch?v=uIanSvWou1M)                           | 15 min | Hands-on C socket programming walkthrough                |
| Computerphile, ["UDP and TCP: Comparison of Transport Protocols"](https://www.youtube.com/watch?v=Vdc8TCESIg8)            | 10 min | Clear visual comparison of UDP vs TCP characteristics    |

---

## Interactive Practice

| Resource                                                                                    | Time   | What it does                                            |
| ------------------------------------------------------------------------------------------- | ------ | ------------------------------------------------------- |
| [Wireshark UDP capture lab](https://wiki.wireshark.org/SampleCaptures) (download `dns.cap`) | 20 min | Analyze real UDP packets, examine headers and checksums |
| [Kurose/Ross UDP Lab](https://gaia.cs.umass.edu/kurose_ross/interactive/UDP.php)            | 15 min | Self-quiz on UDP segment structure and checksum         |

---

## C++ / Boost.Asio Resources

| Resource                                                                                                                        | Time   | What it covers                                  |
| ------------------------------------------------------------------------------------------------------------------------------- | ------ | ----------------------------------------------- |
| Boost.Asio, [UDP Tutorial](https://www.boost.org/doc/libs/1_84_0/doc/html/boost_asio/tutorial/tutdaytime4.html)                 | 20 min | Synchronous UDP daytime client using Boost.Asio |
| Boost.Asio, [UDP Server Tutorial](https://www.boost.org/doc/libs/1_84_0/doc/html/boost_asio/tutorial/tutdaytime5.html)          | 20 min | Synchronous UDP daytime server using Boost.Asio |
| Boost.Asio, [Networking Overview](https://www.boost.org/doc/libs/1_84_0/doc/html/boost_asio/overview/networking/protocols.html) | 10 min | TCP, UDP, ICMP protocol support in Boost.Asio   |

---

## Optional Deep Dive

### RFCs & Standards

- [RFC 768 (UDP)](https://datatracker.ietf.org/doc/html/rfc768) - The complete UDP specification (essential, only 3 pages)
- [RFC 1122 "Requirements for Internet Hosts"](https://datatracker.ietf.org/doc/html/rfc1122#section-4) - Section 4 covers transport layer requirements
- [RFC 919 "Broadcasting Internet Datagrams"](https://datatracker.ietf.org/doc/html/rfc919) - IP broadcast address standards

### Game Networking Context (GPR students)

- Glenn Fiedler, ["Sending and Receiving Packets"](https://gafferongames.com/post/sending_and_receiving_packets/) - Cross-platform socket wrapper design in C++
- GDC Vault, ["1500 Archers on a 28.8: Network Programming in Age of Empires"](https://www.gamedeveloper.com/programming/1500-archers-on-a-28-8-network-programming-in-age-of-empires-and-beyond) - Classic paper on game UDP networking
- [Multiplayer Book, "UDP Socket Programming"](https://www.oreilly.com/library/view/multiplayer-game-programming/9780134034355/) - Ch. 3 covers socket abstractions for games

### Distributed Systems Context (CSI students)

- Peterson & Davie, [Ch. 5.1 "Simple Demultiplexer (UDP)"](https://book.systemsapproach.org/e2e/udp.html) - UDP in the context of end-to-end protocols
- [The C10K Problem](http://www.kegel.com/c10k.html) - Historical context on scaling network servers

### Socket API Deep Dive

- Beej's Guide, [Ch. 9 "Man Pages"](https://beej.us/guide/bgnet/html/split/man-pages.html) - Complete reference for socket functions
- POSIX, [`socket(2)` man page](https://man7.org/linux/man-pages/man2/socket.2.html) - Official Linux socket documentation
- POSIX, [`setsockopt(2)` man page](https://man7.org/linux/man-pages/man2/setsockopt.2.html) - Socket options including `SO_BROADCAST`, `SO_REUSEADDR`

### Boost.Asio Deep Dive

- [Boost.Asio Examples](https://www.boost.org/doc/libs/1_84_0/doc/html/boost_asio/examples.html) - Complete example programs
- [The BSD Socket API and Boost.Asio](https://www.boost.org/doc/libs/1_84_0/doc/html/boost_asio/overview/networking/bsd_sockets.html) - Mapping between BSD sockets and Asio

---

## Assignment Preparation

This week's assignment involves building a UDP echo client/server with broadcast-based server discovery. Key concepts to understand:

1. **Echo Server Pattern**: Server receives a datagram and sends back the same content to the sender
2. **Broadcast Discovery**: Client sends to broadcast address (e.g., `255.255.255.255` or subnet broadcast), server responds with its address
3. **`SO_BROADCAST` option**: Must be enabled on socket to send broadcast packets
4. **`recvfrom()` captures sender**: Returns the address of whoever sent the packet, enabling reply

**Recommended reading order for the assignment:**

1. RFC 768 → understand the protocol
2. Beej Ch. 5.1-5.3, 5.8 → learn the API
3. Beej Ch. 6.3 → see a working example
4. Beej Ch. 7.7 → understand broadcasting
5. Boost.Asio UDP tutorials → if using Boost for your wrapper
