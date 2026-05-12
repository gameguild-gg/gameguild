# Game Network Programming with C++

![Game Network Programming with C++](https://i.imgur.com/Do3392o.jpeg)

::: note "Cross-Listed Course"

This is a cross-listed course between Computer Science and Game Programming majors.

**CSI-275**: Students will gain a solid understanding of network concepts, models, protocols, and applications. Emphasis is placed on the design and implementation of socket-based network programs, comprising both client and server architectures, and including advanced concepts such as non-blocking sockets, multiplexing, threads, asynchronous programming, and multicasting. Practical skills are developed through hands-on exercises and assignments using selected programming languages. [Course Catalog](https://classlist.champlain.edu/show/course/number/CSI_275)

**GPR-430**: Students learn the architectural, design and implementation strategies used to develop online games. They develop and stress test reliable and efficient protocols to address network latency (game lag), security and scalability requirements. Students will utilize distributed object caching along with these protocols to implement registration, authentication, server discovery and game lobby systems. [Course Catalog](https://classlist.champlain.edu/show/course/number/GPR_430)

:::

## Instructors

Feel free to add us to your professional network!

- (main) [Alexandre Tolstenko](https://www.linkedin.com/in/aletolstenko/) 🔗 - [Book a meeting with me](https://calendar.app.google/EU42UnUSyTwyhryL9)
- (external) [Matheus Martins](https://www.linkedin.com/in/mathrmartins/) 🔗
- (guest lecturers) [Eric Passos](https://www.linkedin.com/in/erick-passos-63039513/) 🔗

## Requirements

- Flavored by major:
  - **GPR**: [GPR-200: Introduction to Modern Graphics Programming](https://classlist.champlain.edu/show/course/number/GPR_200) and [GPR-250: Game Architecture](https://classlist.champlain.edu/show/course/number/GPR_250)
  - **CSI**: [CSI-240: Advanced Programming](https://classlist.champlain.edu/show/course/number/CSI_240) or [CSI-260: Advanced Python](https://classlist.champlain.edu/show/course/number/CSI_260)

- Proficiency in C++ programming
- Basic understanding of computer systems and operating systems
- Familiarity with object-oriented programming concepts

### Textbook

- No textbook required; all readings provided either publicly here or internal links.

---

## Learning Outcomes and Competencies

Using the [Bloom's Taxonomy](https://cft.vanderbilt.edu/guides-sub-pages/blooms-taxonomy/) and the [Champlain College Competency Framework](https://competencies.champlain.edu/), the following learning outcomes have been developed for this course:

### Objective Outcomes

By the end of this course, students will be able to:

1. **Explain** the OSI model and TCP/IP stack, **differentiating** between transport protocols. _(Analysis)_
2. **Implement** UDP and TCP client-server applications using Berkeley sockets API. _(Technology Literacy)_
3. **Design** message framing protocols and **apply** serialization strategies for network data. _(Technology Literacy)_
4. **Analyze** network performance by measuring latency, jitter, and packet loss. _(Analysis)_
5. **Compare** synchronization strategies (client-server vs P2P, prediction vs interpolation), **defending** appropriate choices. _(Analysis)_
6. **Evaluate** server architecture patterns and session management approaches for different application requirements. _(Technology Literacy)_
7. **Apply** NAT traversal concepts and security principles to networked applications. _(Technology Literacy)_
8. **Collaborate** in teams to deliver a networked application addressing real-time communication challenges. _(Collaboration)_
9. **Formulate** questions about performance requirements that guide architectural decisions. _(Inquiry)_
10. **Synthesize** networking concepts into a cohesive final project with documented design rationale. _(Integration)_

### Assessment Outcomes

| Outcome                    | Assessment Method                  | Weeks    |
| -------------------------- | ---------------------------------- | -------- |
| Protocol understanding     | Quizzes 1-4, Midterm               | 1-4, 8   |
| Socket implementation      | Assignments 3-4                    | 3-4      |
| Framing & serialization    | Quizzes 5-6, Assignments 5-6       | 5-6      |
| Performance analysis       | Quiz 10, Final Project             | 12       |
| Synchronization trade-offs | Quiz 7, 11, Midterm, Final Project | 7, 8, 13 |
| Architecture evaluation    | Quizzes 12, Final Project          | 14       |
| NAT & security             | Final Project                      | 15       |
| Team collaboration         | Final Project, Peer Evaluations    | 10-16    |
| Requirements inquiry       | Project Proposal, Milestones       | 10-13    |
| Multi-concept synthesis    | Final Project                      | 15-16    |

### Champlain Competencies Addressed

| Competency              | Course Coverage                                                                                          | Primary Assessments             |
| ----------------------- | -------------------------------------------------------------------------------------------------------- | ------------------------------- |
| **Analysis**            | Protocol evaluation, performance measurement, synchronization trade-offs, architectural decisions        | Quizzes, Midterm, Assignments   |
| **Technology Literacy** | C++ sockets, Wireshark, serialization formats, client-server systems, debugging distributed applications | All Assignments, Final Project  |
| **Collaboration**       | Team-based final project with defined roles, milestones, and accountability                              | Final Project, Peer Evaluations |
| **Integration**         | Combining transport protocols, serialization, state sync, and security into complete applications        | Final Project                   |
| **Inquiry**             | Performance requirements analysis, architecture decisions, protocol design questions                     | Project Proposal, Milestones    |

## Philosophy

This course takes a experiential learning, fundamentals-first, implementation-focused approach to network programming. Students learn by building. Every concept is reinforced through hands-on coding assignments automatically tested via GitHub Actions. The course emphasizes:

- _From Scratch_: No external networking libraries - students implement fundamentals using socket wrappers (Boost.Asio allowed for final projects only)
- _Continuous Integration_: Assignments auto-graded via GitHub Actions CI/CD
- _Dual Perspective_: Content framed for both CS distributed systems and game programming real-time applications
- _Industry Practices_: Wireshark analysis, Docker, protocol design, performance measurement under realistic network conditions

## Course Overview

| Weeks             | Focus                                           | Assessment                          |
| ----------------- | ----------------------------------------------- | ----------------------------------- |
| Fundamentals: 1-7 | Protocols, Sockets, Serialization, State Sync   | Weekly Quizzes + Coding Assignments |
| Midterm: 8        | Comprehensive Assessment                        | Midterm Exam                        |
| Advanced: 10-15   | Performance, Prediction, Architecture, Security | Weekly Quizzes + Project Milestones |
| Final: 16         | Project Delivery                                | Essay + Code + Demo                 |

---

## Spring 2026 - Week-by-Week Schedule

**Schedule:** Tuesday & Friday, 1h15m each  
**Audience:** Computer Science (2nd year) and Game Programming (4th year) majors

---

## Course Resources

**Primary Free Textbook:**

- Peterson & Davie, _Computer Networks: A Systems Approach_ - [book.systemsapproach.org](https://book.systemsapproach.org/) (CC BY 4.0, free)

**Essential Reading (All Students):**

- [Beej's Guide to Network Programming](https://beej.us/guide/bgnet/)
- [Glenn Fiedler's Gaffer on Games](https://gafferongames.com)
- [Gabriel Gambetta's Fast-Paced Multiplayer](https://www.gabrielgambetta.com/client-server-game-architecture.html)

**Cloud-Native Reference:**

- [CNCF Landscape](https://landscape.cncf.io)
- [CNCF Glossary](https://glossary.cncf.io)

**Classic Papers:**

- "1500 Archers on a 28.8: Network Programming in Age of Empires" (Bettner & Terrano)
- "The TRIBES Engine Networking Model" (Frohnmayer & Gift)
- [Valve's Source Multiplayer Networking documentation](https://developer.valvesoftware.com/wiki/Source_Multiplayer_Networking)

**GDC Talks (Assign throughout semester):**

- "I Shot You First: Networking the Gameplay of Halo: Reach" (Bungie, 2011)
- "Overwatch Gameplay Architecture and Netcode" (Blizzard, 2017)
- "8 Frames in 16ms: Rollback Networking in Mortal Kombat" (NetherRealm, 2018)

---

## Pre-Midterm (Weeks 1–7)

_Focus: Fundamentals + Weekly Coding Assignments_

---

### Week 01: Jan 12–16 - Network Fundamentals

**Tuesday, Jan 13:** Course intro, repo setup, OSI model, TCP/IP stack overview  
**Friday, Jan 16:** Network devices, packets, basic addressing concepts

**Quiz 01:** OSI layers, TCP/IP model, network device roles  
**Coding Assignment 01:** _Packet Inspector_ - Parse raw packet data from a file, identify headers and payload

**Readings:**

- _Academic:_ [Peterson & Davie, Chapter 1](https://book.systemsapproach.org/)
- _Practical:_ Glenn Fiedler, ["What Every Programmer Needs to Know About Game Networking"](https://gafferongames.com/post/what_every_programmer_needs_to_know_about_game_networking/)

---

### Week 02: Jan 19–23 - IP Addressing and DNS

**Tuesday, Jan 20:** IPv4/IPv6 addressing, subnetting, CIDR notation  
**Friday, Jan 23:** DNS resolution, routing basics, Wireshark intro

**Quiz 02:** IP addressing, subnetting calculations, DNS resolution  
**Coding Assignment 02:** _IP Calculator_ - Subnet analyzer that computes network address, broadcast, and host range

**Readings:**

- _Academic:_ [RFC 791 (IPv4)](https://datatracker.ietf.org/doc/html/rfc791) | [RFC 8200 (IPv6)](https://datatracker.ietf.org/doc/html/rfc8200) | [RFC 1035 (DNS)](https://datatracker.ietf.org/doc/html/rfc1035)
- _Practical:_ [Wireshark User's Guide](https://www.wireshark.org/docs/wsug_html_chunked/) + [Kurose/Ross Wireshark Labs](https://gaia.cs.umass.edu/kurose_ross/wireshark.php)

---

### Week 03: Jan 26–30 - UDP and Socket Basics

**Tuesday, Jan 27:** UDP characteristics, datagrams, when to use UDP  
**Friday, Jan 30:** Berkeley sockets API, socket wrapper design

**Quiz 03:** UDP protocol, datagram structure, socket fundamentals  
**Coding Assignment 03:** _UDP Echo_ - Build a UDP echo client/server using provided socket wrapper boilerplate

**Readings:**

- _Academic:_ [RFC 768 (UDP)](https://datatracker.ietf.org/doc/html/rfc768) (only 3 pages)
- _Practical:_ [Beej's Guide to Network Programming, UDP sections](https://beej.us/guide/bgnet/)

---

### Week 04: Feb 2–6 - TCP and Reliable Communication

**Tuesday, Feb 3:** TCP handshake, connection lifecycle, reliability mechanisms  
**Friday, Feb 6:** Flow control, congestion control, TCP vs UDP tradeoffs

**Quiz 04:** TCP connection states, reliability mechanisms, flow control  
**Coding Assignment 04:** _TCP Chat_ - Simple chat application with proper connection handling and graceful shutdown

**Readings:**

- _Academic:_ [RFC 9293 (TCP, updated 2022)](https://datatracker.ietf.org/doc/html/rfc9293) + [RFC 5681 (Congestion Control)](https://datatracker.ietf.org/doc/html/rfc5681)
- _Practical:_ [TCP State Diagram](https://users.cs.northwestern.edu/~aguMDT/cs340/project2/TCPIP_State_Transition_Diagram.pdf) | [Beej's TCP Guide](https://beej.us/guide/bgnet/html/split/what-is-a-socket.html)

---

### Week 05: Feb 9–13 - Framing and Data Transmission

**Tuesday, Feb 10:** Message framing (length-prefix, delimiters), buffering  
**Friday, Feb 13:** Handling partial reads/writes, deadlock prevention

**Quiz 05:** Framing strategies, buffer management, transmission edge cases  
**Coding Assignment 05:** _Framed Messenger_ - Length-prefixed message protocol with multi-message handling

**Readings:**

- _Academic:_ Stephen Cleary, ["TCP/IP Protocol Design: Message Framing"](https://www.codeproject.com/Articles/37496/TCP-IP-Protocol-Design-Message-Framing)
- _Practical:_ [Beej's Guide, "Handling Partial send()"](https://beej.us/guide/bgnet/html/split/slightly-advanced-techniques.html#sendall)

---

### Week 06: Feb 16–20 - Serialization

**Tuesday, Feb 17:** JSON, binary formats, struct packing, endianness  
**Friday, Feb 20:** Custom bitpacking, compression techniques, performance comparison

**Quiz 06:** Serialization formats, endianness, binary encoding  
**Coding Assignment 06:** _Object Streamer_ - Serialize/deserialize custom objects using binary format with bitpacking

**Readings:**

- _Academic:_ [RFC 8259 (JSON)](https://datatracker.ietf.org/doc/html/rfc8259) + [GNU C Library Byte Order](https://www.gnu.org/software/libc/manual/html_node/Byte-Order.html)
- _Practical:_ Glenn Fiedler, ["Reading and Writing Packets"](https://gafferongames.com/post/reading_and_writing_packets/)
- _Supplemental:_ [FlatBuffers documentation (Google's zero-copy serialization)](https://flatbuffers.dev/)

---

### Week 07: Feb 23–27 - Distributed State and Synchronization

**Tuesday, Feb 24:** State synchronization models (CS: distributed systems patterns / GPR: client-server vs P2P), P2P state sync (lockstep, host authority, state broadcast), delta compression  
**Friday, Feb 27:** Server reconciliation, "never trust the client", host authority in P2P (listen server), P2P conflict resolution

**Quiz 07:** Synchronization patterns, reconciliation strategies, P2P vs client-server  
**Coding Assignment 07:** _State Sync_ - Replicate shared state between multiple clients with delta updates and basic reconciliation

**Readings:**

- _Academic:_ ["An Illustrated Proof of the CAP Theorem"](https://mwhittaker.github.io/blog/an_illustrated_proof_of_the_cap_theorem/)
- _Practical:_ Gabriel Gambetta, ["Fast-Paced Multiplayer" Parts I-II](https://www.gabrielgambetta.com/client-server-game-architecture.html), [OrbitDB: P2P vs Client-Server](https://github.com/orbitdb/field-manual/blob/main/02_Thinking_Peer_to_Peer/01_P2P_vs_Client-Server.md)
- _Interactive Demo:_ [Client-side prediction live demo](https://www.gabrielgambetta.com/client-side-prediction-live-demo.html)

---

## Midterm (Week 8)

---

### Week 08: Mar 2–6 - Midterm

**Tuesday, Mar 3:** Midterm review, Q&A, practice problems  
**Friday, Mar 6:** **MIDTERM EXAM**

_No quiz this week_

---

## Spring Break (Week 9)

---

### Week 09: Mar 9–13 - SPRING BREAK

_No classes_

---

## Post-Midterm (Weeks 10–15)

_Focus: Advanced Topics + Final Project Milestones_

---

### Week 10: Mar 16–20 - HTTP, REST APIs, and Real-Time Web Protocols

**Tuesday, Mar 17:** HTTP fundamentals, methods, status codes, statelessness  
**Friday, Mar 20:** REST API design principles, WebSockets for bidirectional communication

**Quiz 08:** HTTP protocol, REST principles, WebSocket handshake  
**Project Milestone 01:** Team formation + project proposal (game/application concept, tech stack)

**Readings:**

- _Academic:_ [Roy Fielding's REST Dissertation, Chapter 5](https://www.ics.uci.edu/~fielding/pubs/dissertation/rest_arch_style.htm)
- _Practical:_ [RFC 6455 (WebSocket Protocol)](https://datatracker.ietf.org/doc/html/rfc6455)
- _Cloud-Native:_ [CNCF Glossary: Service Mesh, API Gateway](https://glossary.cncf.io/)

---

### Week 11: Mar 23–27 - Non-Blocking I/O and Concurrency

**Tuesday, Mar 24:** Blocking vs non-blocking sockets, select/poll/epoll (CS: event-driven servers / GPR: game loop integration)  
**Friday, Mar 27:** Multithreading basics, thread safety, async patterns

**Quiz 09:** Non-blocking I/O, select/poll, threading fundamentals  
**Project Milestone 02:** Architecture document (network protocol design, message formats)

**Readings:**

- _Academic:_ [epoll(7) Linux Manual Page](https://man7.org/linux/man-pages/man7/epoll.7.html)
- _Practical:_ Julia Evans, ["Async IO on Linux: select, poll, and epoll"](https://jvns.ca/blog/2017/06/03/async-io-on-linux--select--poll--and-epoll/)
- _Implementation:_ [Beej's Guide, "poll()"](https://beej.us/guide/bgnet/html/split/slightly-advanced-techniques.html#poll)

---

### Week 12: Mar 30 – Apr 3 - Performance, Simulation Frequency, and Reliability

**Tuesday, Mar 31:** Latency, jitter, packet loss measurement, tick rates / simulation frequency (CS: update intervals / GPR: server tick rates), bandwidth tradeoffs  
**Friday, Apr 3:** Reliable UDP implementation, acknowledgments, retransmission strategies, bandwidth management and prioritization

**Quiz 10:** Network metrics, tick rates / simulation frequency, reliability patterns, acknowledgment schemes  
**Project Milestone 03:** Networking prototype (basic client-server connection working)

**Readings:**

- _Academic:_ [RFC 9000 (QUIC)](https://datatracker.ietf.org/doc/html/rfc9000) + [QUIC Working Group](https://quicwg.org/)
- _Practical:_ Valve, ["Source Multiplayer Networking"](https://developer.valvesoftware.com/wiki/Source_Multiplayer_Networking)
- _Supplemental:_ Glenn Fiedler, ["Snapshot Interpolation"](https://gafferongames.com/post/snapshot_interpolation/)

---

### Week 13: Apr 6–10 - Client Prediction and Interpolation / Smoothing

**Tuesday, Apr 7:** Client-side prediction, entity interpolation / smoothing (CS: optimistic updates / GPR: dead reckoning), input handling

**Friday, Apr 10:** _NO CLASS - Day Off_

**Quiz 11:** Prediction, interpolation / smoothing, input delay concepts  
**Project Milestone 04:** Alpha build (core networking features functional)

**Readings:**

- _Academic:_ Yahn Bernier (Valve), ["Latency Compensating Methods in Client/Server In-game Protocol Design"](https://developer.valvesoftware.com/wiki/Latency_Compensating_Methods_in_Client/Server_In-game_Protocol_Design_and_Optimization)
- _Practical:_ GDC 2017, ["Overwatch Gameplay Architecture and Netcode"](https://www.youtube.com/watch?v=W3aieHjyNvw)
- _Supplemental:_ Gabriel Gambetta, ["Entity Interpolation"](https://www.gabrielgambetta.com/entity-interpolation.html)

---

### Week 14: Apr 13–17 - Server Architecture and Session Management

**Tuesday, Apr 14:** Authoritative servers, dedicated vs listen servers (CS: centralized vs distributed authority / GPR: host migration), rollback networking concepts  
**Friday, Apr 17:** Session management, connection brokering / matchmaking (CS: service discovery / GPR: lobby systems), scaling considerations

**Quiz 12:** Server models, authority patterns, session management / matchmaking, scalability  
**Project Milestone 05:** Beta build (feature complete, testing phase)

**Readings:**

- _Academic:_ [Google Open Match Documentation](https://open-match.dev/site/docs/)
- _Practical:_ GDC 2011, ["I Shot You First: Networking the Gameplay of Halo: Reach"](https://www.youtube.com/watch?v=h47zZrqjgLc)
- _Cloud-Native:_ [CNCF Projects: Service Discovery patterns](https://landscape.cncf.io/)

---

### Week 15: Apr 20–24 - NAT Traversal and Security

**Tuesday, Apr 21:** NAT types, hole punching, STUN/TURN/ICE concepts (CS: peer connectivity / GPR: P2P game connections)  
**Friday, Apr 24:** Network security, encryption basics, authentication, anti-cheat principles (fog of war, server authority, input validation)

_No quiz this week (last week before finals)_  
**Project Milestone 06:** Polish + documentation (bug fixes, code cleanup, README)

**Readings:**

- _Academic:_ [RFC 8489 (STUN)](https://datatracker.ietf.org/doc/html/rfc8489) | [RFC 8656 (TURN)](https://datatracker.ietf.org/doc/html/rfc8656) | [RFC 8445 (ICE)](https://datatracker.ietf.org/doc/html/rfc8445)
- _Practical:_ [WebRTC for the Curious, "Connecting" Chapter](https://webrtcforthecurious.com/docs/03-connecting/)

---

## Finals (Week 16)

---

### Week 16: Apr 27 – May 1 - Final Project Delivery

**Project Milestone 07 (Final):** Complete submission

- Essay: Technical writeup explaining architecture decisions
- Code: Full source with documentation
- Demo: Live demonstration or recorded video

---

## Summary

- Quizzes: 12 total (Weeks 1–7, 10–14)
- Coding Assignments: 7 total (Weeks 1–7)
- Midterm: 1 (Week 8)
- Project Milestones: 7 total (Weeks 10–16)

---

## Dual-Framing & Crosslisting Glossary

This course serves both CS and GPR students. The following terms are equivalent:

- Client-server ↔ Centralized authority
- P2P / Peer-to-peer ↔ Distributed authority (listen server = host authority in P2P)
- Tick rate ↔ Simulation frequency
- Matchmaking ↔ Connection brokering
- Lobby system ↔ Session management
- Dead reckoning ↔ Extrapolation / Prediction
- Entity interpolation ↔ Smoothing / Temporal interpolation
- Game loop ↔ Event loop / Main loop
- Authoritative server ↔ Centralized authority
- Listen server ↔ Player-hosted server

---

## Notes

- No external networking libraries beyond socket wrappers (Boost.Asio allowed for final projects). Talk to instructor for exceptions.
- Students implement fundamentals from scratch to understand underlying concepts
- Wireshark used throughout for packet analysis
- Network condition testing recommended: 200ms latency, 5% packet loss, 50ms jitter
- CI/CD autograding via GitHub Actions

---

## Final Project Options

Students must choose one of the following capstone projects:

### Option 1: Select one challenge from our guest lecturers

The challenges will be shared later in the semester.

### Option 2: Real-time Multiplayer Action Game

Develop a fast-paced multiplayer game featuring:

- (Required) Client-side prediction and lag compensation
- (Required) Real-time state synchronization
- (Extra) Cheat prevention, authoritative server
- (Extra) Performance optimization for low-latency gameplay
- Suggestion: Implement a 1x1 fighting game.

### Option 3: MMO-Style Persistence and Social Features

Create a persistent multiplayer environment including:

- (Required) Player management and authentication
- (Required) Real-time chat and social features
- (Required) Database integration for persistent state
- (Extra) Basic in-game economy system
- (Extra) Dashboard for player statistics and analytics
- Suggestion: Implement the framwork and demo for an Match Making lobby with chat and player profiles, not the actual game.

### Option 4: Peer-to-Peer Game Network

Build a P2P multiplayer game featuring:

- (Required) NAT traversal implementation
- (Required) Distributed game state management
- (Extra) Master client election and failover
- (Extra) Conflict resolution mechanisms
- (Extra) Network topology optimization

### Option 5: Scalable Game Backend Service

Design a robust server infrastructure with:

- (Required) Microservices architecture
- (Required) Database integration and optimization
- (Extra) Load balancing and auto-scaling
- (Extra) Monitoring and analytics systems

### Option 6: Custom Protocol Design

Develop a novel networking solution featuring:

- (Required) Custom protocol design for specific game genre
- (Required) Performance benchmarking against existing solutions
- (Required) Comprehensive documentation and specification
- (Required) Reference implementation with examples

### Option 7: Your Own Idea!

Propose a unique project that aligns with course objectives. Must be approved by the instructor.

---

## Assessment and Grading

| Component          | Weight | Assessment Method                       |
| ------------------ | ------ | --------------------------------------- |
| Readings           | 5%     | Forum discussion evaluation             |
| Coding Assignments | 25%    | Automatically tested via GitHub Actions |
| Midterm Exam       | 20%    | Paper-based exam                        |
| Quizzes            | 20%    | Canvas quizzes                          |
| Final Project      | 20%    | Essay, Demo, and Presentation           |
| Attendance         | 10%    | Rollcall attendance tracking            |

### Late Submission Policy

Late submissions will incur a penalty of **1% deduction per day** up to a maximum of **25% of the total grade**. For example, a submission that is 1 week (7 days) late will receive a 7% penalty, resulting in a maximum possible grade of 93%, which still falls within the A range. This policy encourages timely submission while allowing flexibility for unforeseen circumstances. Please does not make my life miserable by submitting assignments on the finals week. I beg you.

---

## Development Environment and Tools

### Required Software

- **Primary Language:** C++ (C++17 or later), final projects may use any language;
- **Compiler:** GCC 9+ or Clang 10+ or MSVC 2019+
- **Build System:** CMake 3.16+
- **Version Control:** Git

### Recommended Tools

- **Network Analysis:** Wireshark for packet inspection
- **API Testing:** Postman or curl for HTTP/REST testing
- **Containerization:** Docker for server deployment
- **IDE:** Visual Studio Code, CLion, or Visual Studio

### Required Libraries

- **Networking:** Boost.Asio or native socket libraries
- **Serialization:** nlohmann/json, Protocol Buffers
- **Threading:** std::thread, std::async
- **Testing:** Google Test framework

---

## Additional Resources

### Recommended Reading

- "Multiplayer Game Programming" by Josh Glazer and Sanjay Madhav
- "Real-Time Rendering" by Tomas Akenine-Möller (networking chapters)
- "Game Engine Architecture" by Jason Gregory (networking sections)

### Online Resources

- [Gaffer On Games](https://gafferongames.com/) - Networking articles by Glenn Fiedler
- [Valve Developer Community](https://developer.valvesoftware.com/wiki/Source_Multiplayer_Networking) - Source Engine networking
- [Unity Netcode for GameObjects](https://docs-multiplayer.unity3d.com/netcode/current/about/)
- [Unreal Engine Networking](https://dev.epicgames.com/documentation/en-us/unreal-engine/networking-and-multiplayer-in-unreal-engine)

### Professional Communities

- Game Networking Discord servers
- Reddit r/gamedev networking discussions
- Stack Overflow game-networking tag
- GDC (Game Developers Conference) networking talks
