## Flow Control

Flow control prevents a fast sender from overwhelming a slow receiver's buffer.

### Sliding Window Protocol

The receiver advertises its available buffer space in the **window** field of every ACK. The sender limits unacknowledged data to this amount.

```mermaid
sequenceDiagram
    participant Sender
    participant Receiver

    Note over Receiver: Buffer: 4000 bytes free
    Receiver->>Sender: ACK (ack=1000, window=4000)

    Sender->>Receiver: Data (1000 bytes)
    Sender->>Receiver: Data (1000 bytes)
    Sender->>Receiver: Data (1000 bytes)

    Note over Receiver: Buffer: 1000 bytes free
    Receiver->>Sender: ACK (ack=4000, window=1000)

    Note over Sender: Can only send 1000 more bytes

    Note over Receiver: Application reads data
    Note over Receiver: Buffer: 4000 bytes free
    Receiver->>Sender: ACK (ack=4000, window=4000)

    Note over Sender: Window opened, can send more
```

### Window Size Zero

When the receiver's buffer fills completely:

1. Receiver advertises **window = 0**
2. Sender stops transmitting data
3. Sender periodically sends **window probe** packets
4. When receiver has space, it advertises window > 0
5. Sender resumes transmission

This mechanism prevents buffer overflow and data loss at the receiver.
