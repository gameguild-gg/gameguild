# TCP Connection Termination and Protocol Comparison

## Connection Termination

TCP uses a four-way handshake to close connections gracefully:

```mermaid
sequenceDiagram
    participant Client
    participant Server

    Note over Client,Server: State: ESTABLISHED

    Client->>Server: FIN
    Note over Client: State: FIN_WAIT_1

    Server->>Client: ACK
    Note over Server: State: CLOSE_WAIT
    Note over Client: State: FIN_WAIT_2

    Note over Server: Application closes socket
    Server->>Client: FIN
    Note over Server: State: LAST_ACK

    Client->>Server: ACK
    Note over Server: State: CLOSED
    Note over Client: State: TIME_WAIT

    Note over Client: Wait 2×MSL
    Note over Client: State: CLOSED
```

### Why Four Steps?

Unlike the handshake, termination is **asymmetric**:

- Each side independently closes its sending direction
- A connection can be **half-closed** (one direction closed, other open)
- This allows one side to signal "I'm done sending" while still receiving

### Graceful vs Abortive Close

| Close Type | Mechanism    | Effect                                     |
| ---------- | ------------ | ------------------------------------------ |
| Graceful   | FIN exchange | All buffered data delivered before close   |
| Abortive   | RST packet   | Immediate close, buffered data may be lost |

---

## TCP vs UDP Comparison

```mermaid
flowchart TD
    subgraph TCP["TCP Characteristics"]
        T1[Connection-oriented]
        T2[Reliable delivery]
        T3[Ordered delivery]
        T4[Flow control]
        T5[Congestion control]
        T6[Byte stream]
        T7[Higher latency]
    end

    subgraph UDP["UDP Characteristics"]
        U1[Connectionless]
        U2[Best-effort delivery]
        U3[No ordering]
        U4[No flow control]
        U5[No congestion control]
        U6[Message boundaries preserved]
        U7[Lower latency]
    end
```

### Head-of-Line Blocking

A critical TCP limitation for real-time applications:

```mermaid
sequenceDiagram
    participant Sender
    participant Network
    participant Receiver
    participant App as Application

    Sender->>Network: Packet 1 (position update)
    Sender->>Network: Packet 2 (position update) [LOST]
    Sender->>Network: Packet 3 (position update)
    Sender->>Network: Packet 4 (position update)

    Network->>Receiver: Packet 1
    Receiver->>App: Deliver Packet 1

    Network->>Receiver: Packet 3
    Note over Receiver: Hold Packet 3 (waiting for 2)

    Network->>Receiver: Packet 4
    Note over Receiver: Hold Packet 4 (waiting for 2)

    Note over Sender: Timeout, retransmit Packet 2
    Sender->>Network: Packet 2 (retransmit)
    Network->>Receiver: Packet 2

    Receiver->>App: Deliver Packets 2, 3, 4
    Note over App: Packets 3, 4 are now STALE!
```

In games, stale position updates are worse than missing ones. This is why game state updates often use UDP.

### When to Choose Each Protocol

| TCP              | UDP                               |
| ---------------- | --------------------------------- |
| Chat messages    | Real-time game state              |
| File transfers   | Voice/video streaming             |
| Web browsing     | DNS queries                       |
| Email            | Live broadcasts                   |
| Database queries | Multiplayer game position updates |
