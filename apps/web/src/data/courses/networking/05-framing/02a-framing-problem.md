# The TCP Framing Problem

TCP is a **byte stream** protocol. It guarantees bytes arrive in order, but it does NOT preserve message boundaries.

```mermaid
sequenceDiagram
    participant App1 as Application (Sender)
    participant TCP1 as TCP Stack
    participant Net as Network
    participant TCP2 as TCP Stack
    participant App2 as Application (Receiver)

    App1->>TCP1: send("Hello")
    App1->>TCP1: send("World")

    TCP1->>Net: [Hel]
    TCP1->>Net: [loWor]
    TCP1->>Net: [ld]

    Net->>TCP2: [Hel]
    Net->>TCP2: [loWor]
    Net->>TCP2: [ld]

    TCP2->>App2: recv() → "Hel"
    TCP2->>App2: recv() → "loWorld"

    Note over App2: Where does "Hello" end<br/>and "World" begin?
```

::: warning "The fundamental problem"

If sender calls `send("Hello")` then `send("World")`, the receiver might get:

- `"HelloWorld"` (one chunk)
- `"Hel"` + `"loWorld"` (two chunks)
- `"H"` + `"e"` + `"l"` + `"l"` + `"o"` + `"W"` + `"o"` + `"r"` + `"l"` + `"d"` (ten chunks)

This is not a bug—it's how TCP works. **Your application must implement framing.**

:::

## Why Does This Happen?

1. **Nagle's algorithm** batches small writes into larger segments
2. **Network MTU** fragments large writes into multiple packets
3. **TCP segmentation** splits data based on congestion window
4. **Receiver buffering** coalesces arriving segments before `recv()`
