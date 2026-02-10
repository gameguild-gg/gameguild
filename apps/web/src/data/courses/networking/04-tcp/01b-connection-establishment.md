## TCP Three-Way Handshake

TCP uses a three-way handshake to establish a connection and synchronize sequence numbers between client and server.

```mermaid
sequenceDiagram
    participant Client
    participant Server

    Note over Server: State: LISTEN
    Note over Client: State: CLOSED

    Client->>Server: SYN (seq=x)
    Note over Client: State: SYN_SENT

    Server->>Client: SYN-ACK (seq=y, ack=x+1)
    Note over Server: State: SYN_RECEIVED

    Client->>Server: ACK (ack=y+1)
    Note over Client: State: ESTABLISHED
    Note over Server: State: ESTABLISHED

    Note over Client,Server: Connection Ready for Data Transfer
```

### Handshake Steps

1. **SYN (Synchronize)**: Client sends a segment with SYN flag set and an initial sequence number (ISN)
2. **SYN-ACK**: Server acknowledges client's SYN and sends its own SYN with its ISN
3. **ACK**: Client acknowledges server's SYN, completing the handshake

### Why Three Steps?

- **Two-way synchronization**: Both sides must agree on initial sequence numbers
- **Prevents old duplicates**: Sequence numbers prevent stale connection attempts from being accepted
- **Resource allocation**: Server allocates resources only after receiving final ACK

### Connection Refused

When a client attempts to connect to a port with no listening server:

- The server's OS responds with a **RST** (reset) packet
- In Boost.Asio, `socket.connect()` throws `boost::system::system_error` with `connection_refused`

---

## 4. TCP Connection States

TCP connections progress through a series of states during their lifetime:

```mermaid
stateDiagram-v2
    [*] --> CLOSED
    CLOSED --> LISTEN: passive open
    CLOSED --> SYN_SENT: active open, send SYN

    LISTEN --> SYN_RECEIVED: recv SYN, send SYN+ACK
    SYN_SENT --> ESTABLISHED: recv SYN+ACK, send ACK
    SYN_RECEIVED --> ESTABLISHED: recv ACK

    ESTABLISHED --> FIN_WAIT_1: close, send FIN
    ESTABLISHED --> CLOSE_WAIT: recv FIN, send ACK

    FIN_WAIT_1 --> FIN_WAIT_2: recv ACK
    FIN_WAIT_1 --> CLOSING: recv FIN, send ACK
    FIN_WAIT_1 --> TIME_WAIT: recv FIN+ACK, send ACK

    FIN_WAIT_2 --> TIME_WAIT: recv FIN, send ACK
    CLOSING --> TIME_WAIT: recv ACK

    CLOSE_WAIT --> LAST_ACK: close, send FIN
    LAST_ACK --> CLOSED: recv ACK

    TIME_WAIT --> CLOSED: 2MSL timeout
```

### Important States

| State        | Description                                              |
| ------------ | -------------------------------------------------------- |
| LISTEN       | Server waiting for incoming connections                  |
| SYN_SENT     | Client has sent SYN, waiting for SYN-ACK                 |
| SYN_RECEIVED | Server received SYN, sent SYN-ACK, waiting for ACK       |
| ESTABLISHED  | Connection open, data can flow both directions           |
| CLOSE_WAIT   | Received FIN from peer, waiting for application to close |
| TIME_WAIT    | Sent final ACK, waiting for delayed segments to expire   |

### The TIME_WAIT State

After closing a connection, the endpoint that sent the final ACK enters TIME_WAIT for **2×MSL** (Maximum Segment Lifetime, typically 60 seconds).

**Purpose:**

- Ensures delayed packets from the old connection don't corrupt a new connection using the same 4-tuple
- Allows retransmission of final ACK if lost

**Practical Impact:**

- Cannot immediately rebind to the same port
- Use `reuse_address` option during development to bypass this
