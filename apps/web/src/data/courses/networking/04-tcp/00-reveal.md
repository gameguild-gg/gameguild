# Week 04: TCP and Stream Sockets

---

## Today's Agenda

1. TCP Protocol Deep Dive
2. Connection Establishment & Termination
3. Reliability Mechanisms
4. Flow & Congestion Control
5. Boost.Asio Implementation

---

## TCP: Transmission Control Protocol

Defined in **RFC 793** (comprehensive!)

The reliable transport layer protocol powering the internet

---

## TCP Characteristics

- **Connection-oriented** - three-way handshake required
- **Reliable** - guaranteed delivery
- **Ordered** - bytes arrive in sequence
- **Error detection** - checksums
- **Flow control** - sliding window
- **Congestion control** - network awareness

---

## When to Use TCP

| Application      | Why TCP?                         |
| ---------------- | -------------------------------- |
| Chat             | Messages must arrive reliably    |
| File Transfer    | Every byte must be correct       |
| HTTP/HTTPS       | Complete, ordered content needed |
| Email (SMTP)     | Messages cannot be lost          |
| Database Queries | Results must be reliable         |

---

## The 4-Tuple Connection Identifier

```
(Source IP, Source Port, Destination IP, Destination Port)
```

Uniquely identifies each connection

- Server handles thousands of clients on one port
- Client can have multiple connections to same server
- NAT devices track connections via 4-tuple

---

## TCP Header Format

```mermaid
packet-beta
0-15: "Source Port"
16-31: "Destination Port"
32-63: "Sequence Number"
64-95: "Acknowledgment Number"
96-99: "Data Offset"
100-105: "Reserved"
106: "URG"
107: "ACK"
108: "PSH"
109: "RST"
110: "SYN"
111: "FIN"
112-127: "Window (Flow Control)"
128-143: "Checksum"
144-159: "Urgent Pointer"
160-191: "Options (if any)"
```

---

## Key Header Fields

| Field           | Size    | Purpose                                    |
| --------------- | ------- | ------------------------------------------ |
| Sequence Number | 32 bits | Byte position in stream                    |
| Acknowledgment  | 32 bits | Next byte expected (cumulative ACK)        |
| Window          | 16 bits | Receiver's available buffer (flow control) |
| Flags (6 bits)  | 6 bits  | SYN, ACK, FIN, RST, PSH, URG               |

---

## TCP Flags Explained

- **SYN** - Synchronize sequence numbers (connection start)
- **ACK** - Acknowledge received data
- **FIN** - Finish (close connection)
- **RST** - Reset (abort connection)
- **PSH** - Push data immediately
- **URG** - Urgent data follows

---

## The Three-Way Handshake

```mermaid
sequenceDiagram
    participant Client
    participant Server

    Note over Server: LISTEN
    Note over Client: CLOSED

    Client->>Server: SYN (seq=x)
    Note over Client: SYN_SENT

    Server->>Client: SYN-ACK (seq=y, ack=x+1)
    Note over Server: SYN_RECEIVED

    Client->>Server: ACK (ack=y+1)
    Note over Client: ESTABLISHED
    Note over Server: ESTABLISHED

    Note over Client,Server: Ready for Data Transfer
```

---

## Why Three Steps?

- **Two-way sync** - Both sides exchange initial sequence numbers
- **Prevents duplicates** - Sequence numbers prevent stale connections
- **Resource allocation** - Server only allocates resources after final ACK

---

## Connection Refused

Client attempts to connect to port with no listening server:

```
1. OS responds with RST (reset) packet
2. Boost.Asio throws boost::system::system_error
3. Error code: connection_refused
```

---

## TCP Connection States

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
    FIN_WAIT_2 --> TIME_WAIT: recv FIN, send ACK

    CLOSE_WAIT --> LAST_ACK: close, send FIN
    LAST_ACK --> CLOSED: recv ACK

    TIME_WAIT --> CLOSED: 2MSL timeout
```

---

## Important States

| State       | Meaning                                      |
| ----------- | -------------------------------------------- |
| LISTEN      | Server waiting for incoming connections      |
| SYN_SENT    | Client waiting for server's SYN-ACK          |
| ESTABLISHED | Connection open, data flowing                |
| CLOSE_WAIT  | Received FIN, waiting for app to close       |
| TIME_WAIT   | Final ACK sent, waiting for delayed segments |

---

## The TIME_WAIT State

After closing: wait **2×MSL** (Maximum Segment Lifetime)

Typically 60 seconds

**Purpose:**

- Ensures old packets don't corrupt new connections on same 4-tuple
- Allows retransmission of final ACK if lost

**Practical Impact:**

- Cannot rebind to same port immediately
- Use `reuse_address` option during development

---

## Sequence & Acknowledgment Numbers

```mermaid
sequenceDiagram
    participant Sender
    participant Receiver

    Sender->>Receiver: Data (seq=1000, 500 bytes)
    Note right of Receiver: Received bytes 1000-1499
    Receiver->>Sender: ACK (ack=1500)
    Note left of Sender: "Send byte 1500 next"
```

**Key Insight:** ACK number = next byte **expected**, not last byte received

This is called a **cumulative acknowledgment**

---

## TCP: Byte Stream vs Messages

TCP does **NOT** preserve message boundaries

```
One send():  "Hello" (5 bytes)
             "World" (5 bytes)

One receive() might get:
  "Hel" + "loWor" + "ld"
  or
  "HelloWorld"
  or any other split
```

---

## Message Framing Required

Applications must implement framing:

| Method        | Example                      |
| ------------- | ---------------------------- |
| Delimiter     | `"Hello\n"`, `"World\n"`     |
| Length prefix | `[5]Hello[5]World`           |
| Fixed size    | Pad all messages to 64 bytes |

---

## Reliable Delivery: Timeouts

1. Sender sets timer when sending data
2. If ACK doesn't arrive before timeout (RTO), retransmit
3. RTO calculated from measured round-trip times (RTT)

---

## Reliable Delivery: Fast Retransmit

```mermaid
sequenceDiagram
    participant Sender
    participant Receiver

    Sender->>Receiver: Segment 1 (seq=1000)
    Sender->>Receiver: Segment 2 [LOST]
    Sender->>Receiver: Segment 3
    Sender->>Receiver: Segment 4

    Receiver->>Sender: ACK 1500
    Receiver->>Sender: ACK 1500 (dup 1)
    Receiver->>Sender: ACK 1500 (dup 2)
    Receiver->>Sender: ACK 1500 (dup 3)

    Note over Sender: 3 dup ACKs → Fast Retransmit!
    Sender->>Receiver: Segment 2 [retransmit]
```

---

## Fast Retransmit

**Trigger:** Receive **3 duplicate ACKs** for same sequence number

**Effect:** Retransmit immediately without waiting for timeout

**Why it works:** Subsequent packets arrived = packet loss detected

---

## Flow Control: Sliding Window

Receiver advertises available buffer space in every ACK

Sender limits unacknowledged data to this amount

```
Effective Window = min(cwnd, receiver_advertised_window)
```

---

## Flow Control: Window Zero

When receiver buffer fills completely:

1. Receiver advertises **window = 0**
2. Sender stops transmitting
3. Sender sends **window probe** packets periodically
4. When receiver has space, it advertises window > 0
5. Sender resumes transmission

---

## Congestion Control: Two Phases

TCP congestion control has **two growth phases** separated by a threshold called **ssthresh** (slow start threshold):

| Phase                    | Growth      | When Active     |
| ------------------------ | ----------- | --------------- |
| **Slow Start**           | Exponential | cwnd < ssthresh |
| **Congestion Avoidance** | Linear      | cwnd ≥ ssthresh |

**ssthresh** starts high (e.g. 65535 bytes) and is updated on packet loss.

---

## Phase 1: Slow Start (Exponential)

Despite the name, grows **exponentially**:

1. Start with cwnd = 1 MSS
2. For each ACK received, increase cwnd by 1 MSS
3. Effect: cwnd **doubles** every RTT

```
RTT 0: cwnd = 1   (send 1 segment)
RTT 1: cwnd = 2   (send 2 segments)
RTT 2: cwnd = 4   (send 4 segments)
RTT 3: cwnd = 8   (send 8 segments)
RTT 4: cwnd = 16  (send 16 segments)
```

Slow Start **ends** when cwnd reaches ssthresh → switch to Congestion Avoidance.

---

## Phase 2: Congestion Avoidance (Linear)

Once cwnd ≥ ssthresh, growth **slows down** to avoid flooding the network:

- **Additive Increase:** Increase cwnd by ~1 MSS per RTT (linear growth)
- This means cwnd goes: ssthresh, ssthresh+1, ssthresh+2, ...

When packet loss is detected:

- **Multiplicative Decrease:** Cut cwnd in half

This is called **AIMD** (Additive Increase, Multiplicative Decrease).

---

## The Transition Visualized

![Congestion Control Transition](<https://quickchart.io/chart?c=%7B%22type%22%3A%22line%22%2C%22data%22%3A%7B%22labels%22%3A%5B%220%22%2C%221%22%2C%222%22%2C%223%22%2C%224%22%2C%225%22%2C%226%22%2C%227%22%2C%228%22%2C%229%22%2C%2210%22%5D%2C%22datasets%22%3A%5B%7B%22label%22%3A%22cwnd%20(segments)%22%2C%22data%22%3A%5B1%2C2%2C4%2C8%2C16%2C17%2C18%2C19%2C20%2C21%2C22%5D%2C%22fill%22%3Afalse%2C%22borderColor%22%3A%22rgb(255%2C%2099%2C%20132)%22%2C%22backgroundColor%22%3A%22rgb(255%2C%2099%2C%20132)%22%2C%22tension%22%3A0%7D%2C%7B%22label%22%3A%22ssthresh%20%3D%2016%22%2C%22data%22%3A%5B16%2C16%2C16%2C16%2C16%2C16%2C16%2C16%2C16%2C16%2C16%5D%2C%22fill%22%3Afalse%2C%22borderColor%22%3A%22rgb(54%2C%20162%2C%20235)%22%2C%22borderDash%22%3A%5B5%2C5%5D%2C%22pointRadius%22%3A0%2C%22tension%22%3A0%7D%5D%7D%2C%22options%22%3A%7B%22title%22%3A%7B%22display%22%3Atrue%2C%22text%22%3A%22Slow%20Start%20(Exponential)%20%E2%86%92%20Congestion%20Avoidance%20(Linear)%22%7D%2C%22scales%22%3A%7B%22xAxes%22%3A%5B%7B%22scaleLabel%22%3A%7B%22display%22%3Atrue%2C%22labelString%22%3A%22Time%20(RTT)%22%7D%7D%5D%2C%22yAxes%22%3A%5B%7B%22scaleLabel%22%3A%7B%22display%22%3Atrue%2C%22labelString%22%3A%22cwnd%20(segments)%22%7D%2C%22ticks%22%3A%7B%22beginAtZero%22%3Atrue%7D%7D%5D%7D%2C%22annotation%22%3A%7B%22annotations%22%3A%5B%7B%22type%22%3A%22box%22%2C%22xScaleID%22%3A%22x-axis-0%22%2C%22yScaleID%22%3A%22y-axis-0%22%2C%22xMin%22%3A%220%22%2C%22xMax%22%3A%224%22%2C%22backgroundColor%22%3A%22rgba(75%2C%20192%2C%20192%2C%200.15)%22%2C%22borderColor%22%3A%22rgba(75%2C%20192%2C%20192%2C%200.4)%22%2C%22label%22%3A%7B%22enabled%22%3Atrue%2C%22content%22%3A%22Exponential%22%2C%22position%22%3A%22top%22%2C%22fontSize%22%3A11%7D%7D%2C%7B%22type%22%3A%22box%22%2C%22xScaleID%22%3A%22x-axis-0%22%2C%22yScaleID%22%3A%22y-axis-0%22%2C%22xMin%22%3A%224%22%2C%22xMax%22%3A%2210%22%2C%22backgroundColor%22%3A%22rgba(255%2C%20159%2C%2064%2C%200.15)%22%2C%22borderColor%22%3A%22rgba(255%2C%20159%2C%2064%2C%200.4)%22%2C%22label%22%3A%7B%22enabled%22%3Atrue%2C%22content%22%3A%22Linear%22%2C%22position%22%3A%22top%22%2C%22fontSize%22%3A11%7D%7D%5D%7D%7D%7D>)

**Key insight:** The switch from exponential to linear happens at ssthresh — not from a loss event, but as a **preventive** measure to probe for capacity carefully.

---

## Response to Packet Loss

| Event            | Action                                             |
| ---------------- | -------------------------------------------------- |
| Timeout          | ssthresh = cwnd/2, cwnd = 1, restart slow start    |
| 3 Duplicate ACKs | ssthresh = cwnd/2, cwnd = ssthresh (fast recovery) |

After loss, the cycle restarts: slow start (exponential) up to the new ssthresh, then congestion avoidance (linear) again.

---

## Loss Example: Full Cycle

If cwnd = 12 and **timeout** occurs:

```
1. ssthresh = 12 ÷ 2 = 6
2. cwnd = 1  (restart slow start)
3. Slow start: cwnd = 1 → 2 → 4 → reaches ssthresh (6)
4. Switch to congestion avoidance: cwnd = 6 → 7 → 8 → 9 → ...
5. Eventually another loss → repeat
```

This creates the **sawtooth** pattern: rapid exponential ramp-up, gentle linear increase, then a sharp drop on loss.

---

## TCP Connection Termination

```mermaid
sequenceDiagram
    participant Client
    participant Server

    Note over Client,Server: ESTABLISHED

    Client->>Server: FIN
    Note over Client: FIN_WAIT_1

    Server->>Client: ACK
    Note over Server: CLOSE_WAIT
    Note over Client: FIN_WAIT_2

    Server->>Client: FIN
    Note over Server: LAST_ACK

    Client->>Server: ACK
    Note over Server: CLOSED
    Note over Client: TIME_WAIT
```

---

## Why Four Steps?

Unlike the handshake, termination is **asymmetric**:

- Each side independently closes its sending direction
- Connection can be **half-closed** (one direction closed, other open)
- One side can signal "done sending" while still receiving

---

## Graceful vs Abortive Close

| Type     | Mechanism    | Effect                                   |
| -------- | ------------ | ---------------------------------------- |
| Graceful | FIN exchange | All buffered data delivered before close |
| Abortive | RST packet   | Immediate close, data may be lost        |

---

## TCP vs UDP

| Aspect        | TCP        | UDP         |
| ------------- | ---------- | ----------- |
| Connection    | Required   | None        |
| Reliability   | Guaranteed | Best-effort |
| Ordering      | Yes        | No          |
| Flow Control  | Yes        | No          |
| Congestion CC | Yes        | No          |
| Byte Stream   | Yes        | Messages    |
| Latency       | Higher     | Lower       |

---

## Head-of-Line Blocking

TCP's critical limitation for real-time apps:

```
Packet 1: arrives
Packet 2: LOST
Packet 3: arrives, held in buffer (waiting for 2)
Packet 4: arrives, held in buffer (waiting for 2)

When Packet 2 finally retransmits and arrives:
Packets 3, 4 delivered → but now STALE!
```

**Games prefer UDP** for position updates

---

## When to Choose Each

**Use TCP For:**

- Chat messages
- File transfers
- HTTP/Web browsing
- Email
- Database queries

**Use UDP For:**

- Real-time game state
- Voice/video streaming
- DNS queries
- Live broadcasts
- Position updates

---

## Boost.Asio: Client Connection

```cpp
#include <boost/asio.hpp>
using boost::asio::ip::tcp;

int main() {
    boost::asio::io_context io_context;
    tcp::socket socket(io_context);

    tcp::resolver resolver(io_context);
    auto endpoints = resolver.resolve("example.com", "80");

    boost::asio::connect(socket, endpoints);

    // ... send/receive data ...

    socket.shutdown(tcp::socket::shutdown_both);
    socket.close();
}
```

---

## Boost.Asio: Server Setup

```cpp
#include <boost/asio.hpp>
using boost::asio::ip::tcp;

int main() {
    boost::asio::io_context io_context;

    tcp::acceptor acceptor(io_context,
        tcp::endpoint(tcp::v4(), 12345));

    acceptor.set_option(tcp::acceptor::reuse_address(true));
    acceptor.listen(128);

    while (true) {
        tcp::socket socket(io_context);
        acceptor.accept(socket);
        // Handle client...
        socket.shutdown(tcp::socket::shutdown_both);
        socket.close();
    }
}
```

---

## Socket Options

```cpp
// Allow binding to port in TIME_WAIT state
acceptor.set_option(tcp::acceptor::reuse_address(true));

// Enable TCP keepalive probes
socket.set_option(boost::asio::socket_base::keep_alive(true));

// Linger on close (30 seconds)
socket.set_option(boost::asio::socket_base::linger(true, 30));
```

---

## Handling Partial Reads

```cpp
// WRONG: Assumes all data arrives at once
char buffer[1024];
size_t len = socket.read_some(boost::asio::buffer(buffer));

// CORRECT: Read until newline delimiter
std::string message;
boost::asio::streambuf buffer;
boost::asio::read_until(socket, buffer, '\n');

std::istream is(&buffer);
std::getline(is, message);
```

---

## Graceful Shutdown Pattern

```cpp
void close_connection(tcp::socket& socket) {
    boost::system::error_code ec;

    // 1. Shutdown both directions (sends FIN)
    socket.shutdown(tcp::socket::shutdown_both, ec);

    // 2. Close the socket
    socket.close(ec);
}
```

**Never** call `close()` without `shutdown()` first!

---

## Listen Backlog

```cpp
acceptor.listen(128);  // Up to 128 pending connections
```

Controls how many connections can queue waiting to be accepted

If application doesn't call `accept()` fast enough:

- Backlog fills up
- New connections receive RST or dropped
- Existing connections unaffected

---

## Common TCP Issues

**"Address Already in Use"**

- Solution: Use `reuse_address(true)`

**Connection Refused**

- Server not running, firewall rule, wrong port

**Data Loss on Close**

- Always use graceful shutdown

**Hangs on Read**

- Implement proper message framing

---

## Debugging TCP Connections

View connection states on Linux/macOS:

```bash
# Show all TCP connections
ss -tan

# Show with process info
ss -tanp

# Filter by state
ss -tan state established
ss -tan state time-wait
```

---

## Key Takeaways

1. **Three-way handshake** establishes connection
2. **Sequence/ACK numbers** track every byte
3. **Sliding window** prevents receiver overflow
4. **Slow start + AIMD** manages congestion
5. **Four-way termination** closes gracefully

---

## For Next Time

**Readings:** RFC 793, Beej's Guide (Ch. 5-7), Peterson & Davie Ch. 5-6

**Assignment:** Build multi-client TCP chat server with Boost.Asio

**Remember:** TCP is byte stream—implement message framing!

---

## Questions?
