# Reliable UDP Fundamentals: Sequence Numbers, ACKs, and Selective Reliability

Games use UDP because TCP's head-of-line blocking and mandatory retransmission are incompatible with real-time state delivery. But raw UDP provides no ordering, no acknowledgment, and no reliability — packets may arrive out of order, duplicated, or not at all. Building a usable transport on top of UDP means adding just enough reliability machinery without inadvertently recreating TCP's worst properties.

---

## 1. Sequence Numbers for Ordering and Freshness Checks

### Why Order Matters Even for "Unreliable" Data

Even when data does not need guaranteed delivery, it needs ordering. Consider position updates: if the client receives snapshot 10, then snapshot 8 arrives late, rendering snapshot 8 would move the entity backward in time. Sequence numbers let the receiver discard stale data.

### Basic Sequence Number Design

Each outbound packet gets a monotonically increasing sequence number:

```
Packet 1: seq=1, payload=[snapshot data]
Packet 2: seq=2, payload=[snapshot data]
Packet 3: seq=3, payload=[snapshot data]
...
```

The receiver tracks the highest sequence number seen. Any packet with `seq <= highest_seen` is either old or a duplicate and can be discarded (for unreliable data) or checked against a history window (for reliable data).

### Sequence Number Width and Wrapping

A 16-bit sequence number wraps at 65,536. At 60 packets/second, this wraps every ~18 minutes. At 128 packets/sec, every ~8.5 minutes.

Comparison must handle wrapping correctly:

```
// Is seq_a "more recent" than seq_b (with 16-bit wrapping)?
bool isMoreRecent(uint16_t a, uint16_t b) {
    return (int16_t)(a - b) > 0;
}
```

This works because signed arithmetic on the difference handles the wrap-around correctly for sequences within half the range (32,768 packets) of each other. At 128 pkt/s, that's ~4 minutes — more than enough for any reasonable comparison window.

A 32-bit sequence number wraps every ~33 million packets. At 128 pkt/s, this is ~3 days. For most protocols, 16-bit is sufficient with the wrapping comparison; 32-bit is used when simplicity is preferred over saving 2 bytes per packet.

### Freshness vs Reliability

Sequence numbers serve two distinct purposes:

1. **Freshness**: "Is this data newer than what I already have?" — used for unreliable state data like position updates.
2. **Reliability tracking**: "Which specific packets has the remote side received?" — used for reliable message delivery.

Both use the same sequence number, but the receiver's response differs:

- For freshness: discard old sequences silently.
- For reliability: track which sequences arrived and report gaps.

### Worked Example: Out-of-Order Arrival

Server sends packets with sequences 10, 11, 12, 13. Client receives them as: 10, 12, 11, 13.

| Arrival | Seq | Action (unreliable state data)    | Action (reliable messages)      |
| ------- | --- | --------------------------------- | ------------------------------- |
| 1st     | 10  | Process, set highest=10           | Record received, process        |
| 2nd     | 12  | Process (12 > 10), set highest=12 | Record received, note gap at 11 |
| 3rd     | 11  | Discard (11 < 12)                 | Record received, gap filled     |
| 4th     | 13  | Process (13 > 12), set highest=13 | Record received, process        |

For unreliable data, packet 11 is simply discarded — it's stale. For reliable data, packet 11 is still processed because it contains a message that must be delivered exactly once.

---

## 2. ACK + ACK-Bitfield Patterns for Robust Acknowledgment

### The Basic ACK Problem

The receiver needs to tell the sender which packets arrived. The simplest approach — one ACK per packet — doubles the packet rate and fails when ACKs themselves are lost.

### Cumulative ACK (TCP-Style)

TCP uses cumulative ACKs: "I've received everything up to sequence N." This is efficient but creates head-of-line blocking — if packet 5 is lost, the receiver can only ACK up to 4 even if 6, 7, 8 arrived.

### The ACK + Bitfield Pattern

Game protocols typically use a compact ACK structure in every outgoing packet:

```
struct PacketHeader {
    uint16_t sequence;        // This packet's sequence number
    uint16_t ack;             // Highest remote sequence received
    uint32_t ackBitfield;     // Which of the 32 packets before 'ack' were received
};
```

The `ack` field says "I received your packet #N." The `ackBitfield` is a 32-bit mask where bit 0 = packet (N-1), bit 1 = packet (N-2), ... bit 31 = packet (N-32). A set bit means that packet was received; a cleared bit means it was not (yet).

### How it Works in Practice

Client sends packets to server. Server tracks which client packets it received. On every outbound packet, the server includes:

- `ack = 47` (highest client sequence received)
- `ackBitfield = 0xFFFFFBFF` (all bits set except bit 10)

The client reads this and knows:

- Packet 47: received
- Packets 46 down to 16: all received except packet 37 (bit 10 is 47-37=10)
- Packet 37: not yet acknowledged

### Why This Is Robust

1. **Redundancy**: Every outbound packet repeats the full ACK state. If one ACK-bearing packet is lost, the next one carries the same information plus any updates.
2. **No ACK-of-ACK needed**: The sender never needs to know if its ACK was received by the remote side, because ACK state is repeated continuously.
3. **Low overhead**: 8 bytes per packet (2+2+4) covers 33 packets of acknowledgment history.
4. **Loss-tolerant**: Even under 30% loss, ACK information gets through because every surviving packet carries it.

### Adjusting Bitfield Width

32 bits covers 33 packets of history (ack + 32 previous). At 60 pkt/s, that's ~550ms of history. At 20 pkt/s, it's ~1650ms. This is usually sufficient.

For very high packet rates or very high loss, a 64-bit bitfield covers 65 packets of history. The tradeoff is 4 extra bytes per packet.

### Worked Example: ACK Processing

Server sends packets 40-50. Client receives all except 44 and 47:

Client's ACK state after receiving packet 50:

- `ack = 50`
- `ackBitfield`: bit 0 (seq 49) = 1, bit 1 (seq 48) = 1, bit 2 (seq 47) = 0, bit 3 (seq 46) = 1, bit 4 (seq 45) = 1, bit 5 (seq 44) = 0, bit 6 (seq 43) = 1, ... = `0x...F7DF`

When the server reads this, it knows packets 44 and 47 are unacknowledged. It can decide whether to retransmit messages from those packets based on the reliability class.

---

## 3. Message-Level Reliability on Top of Packet ACKs

### Packets vs Messages

A **packet** is a UDP datagram — it either arrives completely or not at all. A **message** is a logical unit of game data (a command, an event, a state update). Multiple messages can be packed into one packet, and one message can span multiple packets (fragmentation, though best avoided).

The ACK system operates at the **packet** level. Reliability decisions are made at the **message** level.

### How Message Reliability Works

1. Each message is assigned a reliability class (reliable-ordered, reliable-unordered, unreliable).
2. Messages are packed into outgoing packets.
3. The packet gets a sequence number and is sent.
4. When the remote ACK indicates the packet was received, all messages in that packet are considered delivered.
5. When the remote ACK indicates the packet was lost (bit not set after sufficient time), reliable messages from that packet are queued for retransmission in the next outgoing packet.

### Tracking Pending Reliable Messages

The sender maintains a list of reliable messages that have been sent but not yet acknowledged:

```
struct PendingMessage {
    uint16_t messageId;
    uint16_t packetSeq;      // Which packet carried this message
    double sendTime;          // When it was first sent
    int retransmitCount;      // How many times resent
    ReliabilityClass rclass;
    byte[] payload;
};
```

When the ACK bitfield confirms the packet was received, the pending message is removed. When the packet is declared lost (not acknowledged after sufficient bitfield rotations), the message is retransmitted.

### Loss Detection Timing

A packet is considered lost when:

- Its sequence number has fallen out of the ACK bitfield window (more than 32 packets ago without acknowledgment), OR
- A configurable timeout has elapsed since send time (e.g., 2× smoothed RTT)

The first condition is reliable at high packet rates. The second is a safety net for low packet rates where 32 packets might represent a very long time.

### Message Deduplication

If a reliable message is retransmitted and the original packet also eventually arrives (delayed, not lost), the receiver may see the message twice. The message-level protocol must deduplicate:

- Assign each message a unique ID.
- The receiver tracks which message IDs it has processed.
- Duplicate IDs are silently dropped.

A sliding window of the last N message IDs (e.g., 256) is usually sufficient.

---

## 4. Reliable-Ordered vs Unreliable Classes by Gameplay Importance

### Why One Reliability Mode Is Not Enough

Different game data has different delivery requirements:

| Data Type       | Must Arrive?             | Must Be In Order?        | Stale OK?        |
| --------------- | ------------------------ | ------------------------ | ---------------- |
| Player position | No (next one supersedes) | No (use latest)          | Yes, discard old |
| Chat message    | Yes                      | Yes (conversation order) | N/A              |
| Player input    | Yes                      | Yes (simulation order)   | No               |
| Damage event    | Yes                      | No (order irrelevant)    | No               |
| Voice audio     | No (real-time stream)    | Yes (playout order)      | Yes, discard old |
| Score update    | Yes                      | No                       | N/A              |

A single global reliability mode forces everything to the most restrictive common denominator. This is either too expensive (all data is reliable-ordered → head-of-line blocking) or too risky (all data is unreliable → events are lost).

### Common Reliability Classes

**1. Unreliable (fire-and-forget)**

- No retransmission, no ordering guarantee.
- Used for: position/velocity updates, heartbeats, telemetry.
- If lost, the next update supersedes it.

**2. Unreliable-Sequenced**

- No retransmission, but stale packets are discarded.
- Used for: animation state, priority-coded state, voice audio.
- Sequence number ensures only the latest data is used.

**3. Reliable-Unordered**

- Retransmitted until acknowledged, but delivered as soon as received (no ordering).
- Used for: damage events, score updates, object spawns, achievements.
- No head-of-line blocking because order doesn't matter.

**4. Reliable-Ordered**

- Retransmitted until acknowledged AND delivered in sequence order.
- Used for: chat, command sequences, transaction-like operations.
- Creates head-of-line blocking within this channel — if message 5 is lost, messages 6 and 7 are buffered until 5 arrives.

### Channel Isolation

Head-of-line blocking in reliable-ordered only affects messages in the same **channel**. A well-designed protocol supports multiple independent channels:

- Channel 0: reliable-ordered chat
- Channel 1: reliable-ordered player commands
- Channel 2: reliable-unordered game events
- Channel 3: unreliable state updates

A lost packet on channel 0 blocks channel 0 but does not affect channels 1-3. This is a critical difference from TCP, where all data shares one ordered stream.

### Worked Example: Message Classification in an FPS

| Message                  | Class                | Channel | Rationale                         |
| ------------------------ | -------------------- | ------- | --------------------------------- |
| Player position/velocity | Unreliable           | 3       | Superseded by next update         |
| Weapon fire event        | Reliable-unordered   | 2       | Must arrive but order irrelevant  |
| Kill notification        | Reliable-unordered   | 2       | Must arrive, no ordering need     |
| Chat text                | Reliable-ordered     | 0       | Must arrive in conversation order |
| Loadout change           | Reliable-ordered     | 1       | Sequence matters for state        |
| Voice audio frame        | Unreliable-sequenced | 4       | Real-time, discard stale          |
| Server config            | Reliable-ordered     | 1       | Must arrive in order              |

---

## 5. Designing for Partial Reliability

### The Mindset Shift

TCP programmers expect "send and forget" — the transport ensures delivery. UDP programmers must think about **what happens when data is lost** for every message type.

Partial reliability means explicitly choosing, for each message type, whether loss is acceptable and what the recovery strategy is.

### Design Process

For each message type in your protocol:

1. **If lost, is the data still useful later?** If no (e.g., old position), use unreliable.
2. **If lost, will the game state be inconsistent?** If yes (e.g., damage event), use reliable.
3. **If delivered out of order, is the result incorrect?** If yes (e.g., chat), use reliable-ordered.
4. **Does head-of-line blocking on this channel affect gameplay?** If yes, consider reliable-unordered instead.

### Implicit Reliability Through Redundancy

Some data doesn't need explicit retransmission because it's sent repeatedly:

- **Full state snapshots**: every snapshot contains all entity positions. Losing one snapshot is harmless because the next one contains the same data (updated).
- **Delta states with periodic keyframes**: deltas may depend on previous deltas (must arrive), but if a keyframe is sent every N snapshots, the receiver can resync from the keyframe.
- **Priority accumulation**: low-priority data that was deferred (not sent) accumulates priority and is eventually included in a future packet.

This implicit reliability avoids retransmission overhead entirely but requires the protocol to handle gaps gracefully.

### Delta Encoding and Reliability Interaction

Delta-encoded state (sending only what changed since the last acknowledged snapshot) interacts with reliability:

1. Server sends snapshot delta based on client's last ACK'd baseline.
2. If the delta packet is lost, the server detects this from the ACK bitfield.
3. The next delta is computed against the **same baseline** (since the client didn't advance).
4. This naturally retransmits lost changes without explicit retransmission.

This is an elegant pattern: the delta protocol self-corrects on loss without any additional retransmission mechanism.

### Reliability Budget

Reliable messages consume bandwidth twice: once when first sent, and again when retransmitted. Under loss, reliable message bandwidth grows:

$$B_{\text{reliable}} = B_{\text{base}} \times \frac{1}{1 - L}$$

Where $L$ is the loss rate. At 5% loss: $B = B_{\text{base}} \times 1.053$. At 20% loss: $B = B_{\text{base}} \times 1.25$.

This extra bandwidth comes from the overall budget. Too many reliable messages under high loss can crowd out unreliable state updates, causing the game to stutter even as events are delivered perfectly.

**Design guideline**: keep reliable message volume small relative to total bandwidth. Reliable messages should be compact events and commands, not bulk state.

---

## 6. Common Failure Modes and Anti-Patterns

1. **Making everything reliable**: recreates TCP head-of-line blocking. Position updates don't need reliability — they are superseded by newer data.

2. **Not deduplicating reliable messages**: retransmitted messages arrive twice if the original was delayed (not lost). Without dedup, effects apply twice (double damage, double score).

3. **Using 8-bit sequence numbers**: wraps at 256. At 60 pkt/s, that's 4 seconds. Two packets separated by >4 seconds become ambiguous. Use at least 16 bits.

4. **Not handling sequence wrap**: comparing `seq > highest` without signed subtraction fails at wrap boundaries. Sequence 0 is "newer" than 65535 but naive comparison says otherwise.

5. **Acking only in response packets**: if the receiver has nothing to send, ACKs are never sent. Always piggyback ACKs on outgoing packets; if no data to send, send empty ACK-only packets at a minimum rate.

6. **Unbounded retransmission queue**: if reliable messages accumulate faster than they can be delivered (sustained congestion), the queue grows without bound. Set a maximum queue size and drop/reject new reliable messages when full.

7. **Single reliability channel for all reliable data**: chat blocking game commands because both are reliable-ordered on the same channel. Use separate channels.

8. **No timeout on reliable delivery**: a message pending retransmission forever wastes memory and bandwidth. Set a maximum delivery attempt count or timeout (e.g., 10 seconds) after which the message is abandoned and the failure is reported to the application.

---

## Code Example (C++): Packet Header and ACK Processing

```cpp
#include <cstdint>
#include <array>
#include <vector>
#include <algorithm>

struct PacketHeader {
    uint16_t sequence;
    uint16_t ack;
    uint32_t ackBitfield;
};

class AckTracker {
    uint16_t localSequence = 0;
    uint16_t remoteSequence = 0;
    uint32_t receivedBitfield = 0;

    // Track which of our packets have been ACK'd
    static constexpr int HistorySize = 256;
    std::array<bool, HistorySize> acked{};
    std::array<double, HistorySize> sendTimes{};

public:
    uint16_t nextSequence() {
        return localSequence++;
    }

    void recordSend(uint16_t seq, double time) {
        sendTimes[seq % HistorySize] = time;
        acked[seq % HistorySize] = false;
    }

    // Called when we receive a packet from remote
    void onReceive(uint16_t remoteSeq) {
        if (isMoreRecent(remoteSeq, remoteSequence)) {
            // Shift bitfield to account for the gap
            int shift = (int16_t)(remoteSeq - remoteSequence);
            if (shift > 32) {
                receivedBitfield = 0;
            } else {
                receivedBitfield <<= shift;
                // Mark previous remoteSequence in bitfield
                receivedBitfield |= (1u << (shift - 1));
            }
            remoteSequence = remoteSeq;
        } else {
            // Older packet: set its bit in the bitfield
            int offset = (int16_t)(remoteSequence - remoteSeq);
            if (offset > 0 && offset <= 32) {
                receivedBitfield |= (1u << (offset - 1));
            }
        }
    }

    // Process ACK info from a received packet to mark our packets as ACK'd
    void processAck(uint16_t ack, uint32_t ackBitfield) {
        markAcked(ack);
        for (int i = 0; i < 32; i++) {
            if (ackBitfield & (1u << i)) {
                markAcked(ack - 1 - i);
            }
        }
    }

    PacketHeader buildHeader(uint16_t seq) const {
        return {seq, remoteSequence, receivedBitfield};
    }

    bool isPacketAcked(uint16_t seq) const {
        return acked[seq % HistorySize];
    }

    double getSendTime(uint16_t seq) const {
        return sendTimes[seq % HistorySize];
    }

    // Collect un-acked packet sequences older than timeout
    std::vector<uint16_t> getLostPackets(double now, double timeout) const {
        std::vector<uint16_t> lost;
        for (int i = 0; i < HistorySize; i++) {
            if (!acked[i] && sendTimes[i] > 0 &&
                (now - sendTimes[i]) > timeout) {
                lost.push_back(static_cast<uint16_t>(i));
            }
        }
        return lost;
    }

private:
    void markAcked(uint16_t seq) {
        acked[seq % HistorySize] = true;
    }

    static bool isMoreRecent(uint16_t a, uint16_t b) {
        return (int16_t)(a - b) > 0;
    }
};
```

## Code Example (C#): Message Reliability Manager

```csharp
using System;
using System.Collections.Generic;

public enum ReliabilityClass
{
    Unreliable,
    UnreliableSequenced,
    ReliableUnordered,
    ReliableOrdered
}

public struct GameMessage
{
    public ushort Id;
    public byte Channel;
    public ReliabilityClass Reliability;
    public byte[] Payload;
}

public class ReliabilityManager
{
    private ushort _nextMessageId;
    private readonly Dictionary<ushort, PendingMessage> _pending = new();
    private readonly HashSet<ushort> _receivedIds = new();
    private readonly Queue<ushort> _receivedOrder = new();
    private const int MaxReceivedHistory = 512;
    private const int MaxRetransmits = 8;
    private const double MaxPendingSeconds = 10.0;

    private struct PendingMessage
    {
        public ushort MessageId;
        public ushort PacketSeq;
        public double FirstSendTime;
        public double LastSendTime;
        public int RetransmitCount;
        public GameMessage Message;
    }

    public ushort AssignId(ref GameMessage msg)
    {
        msg.Id = _nextMessageId++;
        return msg.Id;
    }

    public void OnMessageSent(GameMessage msg, ushort packetSeq, double now)
    {
        if (msg.Reliability == ReliabilityClass.ReliableUnordered ||
            msg.Reliability == ReliabilityClass.ReliableOrdered)
        {
            _pending[msg.Id] = new PendingMessage
            {
                MessageId = msg.Id,
                PacketSeq = packetSeq,
                FirstSendTime = now,
                LastSendTime = now,
                RetransmitCount = 0,
                Message = msg
            };
        }
    }

    public void OnPacketAcked(ushort packetSeq)
    {
        // Remove all pending messages that were carried by this packet
        var toRemove = new List<ushort>();
        foreach (var kvp in _pending)
        {
            if (kvp.Value.PacketSeq == packetSeq)
                toRemove.Add(kvp.Key);
        }
        foreach (var id in toRemove)
            _pending.Remove(id);
    }

    public List<GameMessage> GetRetransmissions(double now, double rttEstimate)
    {
        var retransmits = new List<GameMessage>();
        var expired = new List<ushort>();

        foreach (var kvp in _pending)
        {
            var p = kvp.Value;

            // Expire old messages
            if (now - p.FirstSendTime > MaxPendingSeconds ||
                p.RetransmitCount >= MaxRetransmits)
            {
                expired.Add(kvp.Key);
                continue;
            }

            // Retransmit if enough time has passed
            double rto = Math.Max(rttEstimate * 2.0, 100.0);
            if (now - p.LastSendTime > rto)
            {
                retransmits.Add(p.Message);
                var updated = p;
                updated.RetransmitCount++;
                updated.LastSendTime = now;
                _pending[kvp.Key] = updated;
            }
        }

        foreach (var id in expired)
            _pending.Remove(id);

        return retransmits;
    }

    // Returns true if this message should be processed (not a duplicate)
    public bool ShouldProcess(ushort messageId)
    {
        if (_receivedIds.Contains(messageId))
            return false;

        _receivedIds.Add(messageId);
        _receivedOrder.Enqueue(messageId);

        while (_receivedOrder.Count > MaxReceivedHistory)
        {
            var old = _receivedOrder.Dequeue();
            _receivedIds.Remove(old);
        }

        return true;
    }
}
```
