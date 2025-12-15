# Game Network Programming with C++

## Course Overview
This 15-week course introduces students to network programming fundamentals through the lens of game development. Students will learn essential networking concepts, protocols, and programming techniques while building practical multiplayer game systems.

## Prerequisites

- Proficiency in C++ programming
- Basic understanding of computer systems and operating systems
- Familiarity with object-oriented programming concepts

---

## Weekly Schedule

### Week 1: Network Fundamentals
**Topics:**
- Network topologies and devices
- Understand the OSI model and TCP/IP stack
- Identify network devices and their roles
- Grasp basic network addressing concepts

**Hands-on Project:**

Build a network diagnostic tool that implements ping functionality and performs traceroute analysis. Research and diagram the network architecture of a popular online game (e.g., World of Warcraft, Counter-Strike).

**Assessment:**

Quiz covering OSI model layers, TCP/IP fundamentals, and network device identification.

---

### Week 2: IP Addressing and Routing
**Topics:**
- Master IPv4 and IPv6 addressing schemes
- Understand subnetting and CIDR notation
- Learn routing protocols and DNS resolution

**Hands-on Project:**
Create an IP address calculator and subnet analyzer. Design a network topology for a hypothetical game server infrastructure, including load balancers and regional servers.
todo: Use wireshark

**Assessment:**
- Quiz on IP addressing, subnetting calculations, routing protocol concepts.

---

### Week 3: UDP Protocol and Socket Programming
**Topics:**
- Understand UDP characteristics and use cases
- Learn socket programming fundamentals
- Implement connectionless communication patterns
- Datagram
- Reliable UDP introduction
- Explain that we will cover more how to abstract the package sending/receiving in future weeks with serialization

**Hands-on Project:**
Develop a real-time "asteroid field" game where ship positions are broadcast via UDP. Include basic chat functionality using UDP sockets.
Echo server.

**Assessment:**
Quiz on UDP protocol characteristics, socket programming basics, and connectionless communication patterns.

---

### Week 4: TCP Protocol and Reliable Communication
**Topics:**
- Understand TCP characteristics and reliability mechanisms
- Learn connection establishment and management
- Implement flow control and error correction
- Datagram
- STUN/TURN intro

**Hands-on Project:**
Build a turn-based strategy game (tic-tac-toe or chess) using TCP for move synchronization. Implement proper connection handling and error recovery.
Chat App.

**Assessment:**
Quiz on TCP protocol features, connection management, and reliability mechanisms.

---

### Week 5: HTTP and Modern Web Protocols
**Topics:**
- Understand HTTP/HTTPS fundamentals
- Learn REST API design principles
- Idempotency and statelessness
- Explore WebSockets/WebRTC and modern protocols (HTTP/2, QUIC)

**Hands-on Project:**
Create a lobby system using HTTP REST APIs and WebSocket connections. Include player statistics tracking and real-time chat functionality.

**Assessment:**
Quiz on HTTP methods, status codes, WebSocket protocol, and modern web protocol features.

---

### Week 6: State Management
**Topics:**
- Understand state synchronization concepts
- Learn client-server vs peer-to-peer architectures
- Implement state replication strategies
- Object replication

**Hands-on Project:**
Develop a multiplayer Pong game with shared game state management. Implement both real-time and turn-based synchronization modes.

**Assessment:**
Quiz on state management, synchronization strategies, and network architecture patterns.

---

### Week 7: Data Serialization and Formats
**Topics:**
- Compare serialization formats (JSON, XML, Protocol Buffers, MessagePack)
- Understand performance implications of different formats
- Implement custom serialization solutions

**Hands-on Project:**
Build a "ghost car" racing game with a multi-format serialization library. Store and transmit player car data using various serialization formats and compare performance.
Extra: build a serialization tool.

**Assessment:**
Quiz on serialization format comparison, data encoding methods, and performance considerations.

---

### Week 8: Remote Procedure Calls and APIs
**Topics:**
- Understand RPC concepts and implementations
- Design RESTful game APIs
- Explore GraphQL for game data queries
- Extras: gRPC

**Hands-on Project:**
Create a "remote robot controller" game with an RPC system for game actions (move, attack, chat commands). Implement both REST and RPC interfaces.

**Assessment:**
Quiz on RPC concepts, API design principles, and remote communication patterns.

---

### Week 9: Network Security and Cryptography
**Topics:**
- Learn game security fundamentals
- Understand encryption and authentication methods
- Implement anti-cheat measures

**Hands-on Project:**
Develop a secure multiplayer game with message encryption/decryption, player authentication system, and basic anti-cheat security layer.

**Assessment:**
Quiz on encryption algorithms, authentication methods, and common game security vulnerabilities.

---

### Week 10: Performance and Reliability
**Topics:**
- Understand network performance metrics (latency, jitter, packet loss)
- Implement acknowledgment and retransmission systems
- Optimize bandwidth usage

**Hands-on Project:**
Build a "rhythm game" with network performance monitoring, latency adaptation, packet loss recovery, and reliable UDP implementation.

**Assessment:**
Quiz on network performance metrics, reliability mechanisms, and optimization techniques.

---

### Week 11: Advanced Client Techniques
**Topics:**
- Implement client-side prediction
- Understand lag compensation strategies
- Learn rollback networking and interpolation
- Guest Lecturer: Someone from Photon Quantum (Photon Network)

**Hands-on Project:**
Create a real-time combat game with client prediction system, rollback networking, and accurate hit registration for fast-paced gameplay.

**Assessment:**
Quiz on client prediction concepts, rollback networking, and lag compensation strategies.

---

### Week 12: Server Architecture Design
**Topics:**
- Design authoritative server systems
- Compare dedicated vs listen server models
- Implement load balancing and scaling solutions

**Hands-on Project:**
Begin final project development - focus on server architecture design and implementation.

**Assessment:**
Quiz on server architecture patterns, load balancing strategies, and scalability concepts.
Which server architecture to choose for different game genres.

---

### Week 13: NAT Traversal and P2P Networking
**Topics:**
- Understand NAT punchthrough techniques
- Learn ICE/STUN/TURN protocols
- Implement peer-to-peer game architectures
- STEAM P2P networking overview

**Hands-on Project:**
Continue final project development - implement NAT traversal or P2P features as applicable.

**Assessment:**
Quiz on NAT traversal techniques, P2P networking protocols, and connection establishment methods.

---

### Week 14: Concurrency and Optimization
**Topics:**
- Master multithreading in networked games
- Ensure thread safety in concurrent systems
- Implement asynchronous programming patterns

**Hands-on Project:**
Finalize final project - focus on performance optimization and concurrent programming implementation.

**Assessment:**
Quiz on concurrency patterns, thread safety mechanisms, and performance optimization techniques.

---

### Week 15: Project Presentations and Review
**Topics:**
- Present technical projects effectively
- Conduct peer code reviews
- Evaluate and critique networking implementations

**Hands-on Project:**
Final project presentations with live demonstrations, comprehensive code walkthroughs, and complete technical documentation.

**Assessment:**
Final project presentation and peer evaluation.

---

## Final Project Options

Students must choose one of the following capstone projects:

### Option 1: Real-time Multiplayer Action Game
Develop a fast-paced multiplayer game featuring:
- Client-side prediction and lag compensation
- Authoritative server with cheat prevention
- Real-time state synchronization
- Performance optimization for low-latency gameplay
- Suggestion: Implement a 1x1 fighting game.

### Option 2: MMO-Style Persistent World
Create a persistent multiplayer environment including:
- Player management and authentication
- Real-time chat and social features
- Basic in-game economy system
- Database integration for persistent state

### Option 3: Peer-to-Peer Game Network
Build a P2P multiplayer game featuring:
- NAT traversal implementation
- Distributed game state management
- Conflict resolution mechanisms
- Network topology optimization

### Option 4: Scalable Game Backend Service
Design a robust server infrastructure with:
- Microservices architecture
- Database integration and optimization
- Load balancing and auto-scaling
- Monitoring and analytics systems

### Option 5: Custom Protocol Design
Develop a novel networking solution featuring:
- Custom protocol design for specific game genre
- Performance benchmarking against existing solutions
- Comprehensive documentation and specification
- Reference implementation with examples

---

## Assessment and Grading

| Component | Weight | Description |
|-----------|--------|-------------|
| Weekly Projects (Weeks 1-11) | 55% | Hands-on programming assignments |
| Weekly Quizzes | 20% | Knowledge assessment and concept understanding |
| Final Project | 20% | Capstone project demonstrating course concepts |
| Final Presentation | 5% | Project demonstration and technical communication |

### Grading Scale
- A: 90-100% - Exceptional understanding and implementation
- B: 80-89% - Good grasp of concepts with solid implementation
- C: 70-79% - Adequate understanding with basic implementation
- D: 60-69% - Minimal understanding with incomplete implementation
- F: Below 60% - Insufficient demonstration of course objectives

### Late Submission Policy
Late submissions will incur a penalty of **1% deduction per day** up to a maximum of **25% of the total grade**. For example, a submission that is 1 week (7 days) late will receive a 7% penalty, resulting in a maximum possible grade of 93%, which still falls within the A range. This policy encourages timely submission while allowing flexibility for unforeseen circumstances.

---

## Development Environment and Tools

### Required Software
- **Primary Language:** C++ (C++17 or later)
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

## Learning Outcomes

Upon successful completion of this course, students will be able to:

1. **Protocol Mastery:** Understand and implement major network protocols (TCP, UDP, HTTP, WebSocket) in game contexts

2. **Architecture Design:** Design and implement both client-server and peer-to-peer networking architectures for multiplayer games

3. **Problem Solving:** Handle real-world networking challenges including latency, packet loss, security threats, and scalability issues

4. **System Design:** Create scalable multiplayer game systems that can handle hundreds or thousands of concurrent players

5. **Optimization:** Apply performance optimization techniques appropriate for different game genres and network conditions

6. **Debugging:** Troubleshoot and debug complex network-related issues using appropriate tools and methodologies

7. **Security:** Implement security measures to protect against common game networking vulnerabilities and cheating

8. **Professional Skills:** Communicate technical networking concepts clearly and work effectively in team-based development environments

---

## Additional Resources

### Recommended Reading
- "Multiplayer Game Programming" by Josh Glazer and Sanjay Madhav
- "Real-Time Rendering" by Tomas Akenine-Möller (networking chapters)
- "Game Engine Architecture" by Jason Gregory (networking sections)

### Online Resources
- Gaffer On Games (https://gafferongames.com/) - Networking articles
- Valve Developer Community - Source Engine networking
- Unity Multiplayer Networking documentation
- Unreal Engine networking documentation

### Professional Communities
- Game Networking Discord servers
- Reddit r/gamedev networking discussions
- Stack Overflow game-networking tag
- GDC (Game Developers Conference) networking talks