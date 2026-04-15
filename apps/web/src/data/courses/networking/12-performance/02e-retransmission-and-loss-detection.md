# Retransmission and Loss Detection Strategy

When a packet is lost, how do you know it's gone? And once you decide to retransmit, how do you do it without making things worse? These are deceptively hard questions. The answers determine whether your protocol recovers gracefully from loss or spirals into self-inflicted congestion.

---

## 1. Time-Based Loss Detection and Ambiguity Tradeoffs

### The Fundamental Ambiguity

When you send a packet and don't receive an ACK within a certain time, there are three possibilities:

1. **The packet was lost.** Correct to retransmit.
2. **The ACK was lost.** The remote side received the data but you don't know it. Retransmission is wasteful but not harmful (if deduplication works).
3. **The packet or ACK is still in transit.** Nothing was lost — it's just slow. Retransmission is wasteful AND adds congestion.

You cannot distinguish these cases in real time. Every retransmission decision is a bet.

### The Cost of Wrong Bets

**Retransmit too early (false positive)**:

- Wastes bandwidth on data the remote already received.
- Adds packets to an already congested path.
- Can trigger congestion detection, causing send rate reduction.
- "Spurious retransmit" — the most common retransmission failure mode.

**Retransmit too late (false negative)**:

- The remote side waits longer for data it needs.
- For reliable-ordered channels, head-of-line blocking persists.
- For game events, the player experiences delayed effects.
- Recovery latency increases by the excess wait time.

### The Detection Window

Loss detection typically works through one of two signals:

1. **Timeout**: enough time has passed since send that the packet is "probably" lost. The timeout is derived from RTT estimates.
2. **NACK inference**: the ACK bitfield shows that later packets were received but this one was not. Three subsequent packets acknowledged without this one is strong evidence of loss (similar to TCP's "triple duplicate ACK").

Both signals have false-positive rates that depend on network conditions:

| Signal                    | False Positive Rate Under Jitter               | Detection Speed                           |
| ------------------------- | ---------------------------------------------- | ----------------------------------------- |
| Timeout only              | Low (if timeout is conservative)               | Slow (waits full timeout)                 |
| NACK inference only       | Moderate (reordering triggers false positives) | Fast (a few packet intervals)             |
| Combined (NACK + timeout) | Lowest                                         | Fast for obvious loss, slow for ambiguous |

### NACK Inference with the ACK Bitfield

Recall from the reliable UDP topic that each packet carries an ACK bitfield showing which recent remote packets were received. When you send packets 20, 21, 22, 23, and the remote ACKs show 20=yes, 21=no, 22=yes, 23=yes — that's strong evidence that 21 was lost (not just reordered, since packets on both sides of it arrived).

This is faster than timeout-based detection. At 60 pkt/s, three subsequent packets arrive in ~50ms versus a typical timeout of 200-500ms.

However, NACK inference fails when reordering is common. Some network paths (especially multi-path routing, WiFi, and mobile) routinely reorder packets by 1-3 positions. If you trigger retransmission on any gap, you'll spuriously retransmit constantly.

**Practical NACK threshold**: require 3-4 subsequent packets to arrive before declaring an earlier packet lost. This tolerates mild reordering while still detecting genuine loss faster than timeouts.

### Worked Example: Detection Timing

Server sends at 20 Hz (50ms intervals). RTT is 80ms. Jitter p90 is 15ms.

Packet 15 is lost. Packets 16, 17, 18 arrive normally.

- **NACK detection**: after packet 18 arrives (150ms after packet 15 was sent), the server sees 16, 17, 18 ACK'd but not 15. Detection time: ~150ms.
- **Timeout detection** (RTO = 2 × SRTT): with SRTT ≈ 80ms, RTO = 160ms. Detection at 160ms after send.

Both trigger at approximately the same time in this case. For faster send rates (60 Hz), NACK inference detects in ~50ms versus 160ms for timeout — a significant advantage.

---

## 2. RTT Estimation and Retransmission Timeout (RTO/PTO) Intuition

### Why RTT Estimation Matters

The retransmission timeout (RTO) is fundamentally an RTT-derived value. If you know the round-trip time precisely, you can set the timeout to just slightly above RTT: any packet not acknowledged within RTT + small margin is probably lost.

But RTT varies. It's not a fixed number — it's a distribution with mean, variance, and occasional spikes.

### Smoothed RTT (SRTT) and RTT Variance (RTTVAR)

The classic approach (RFC 6298, adapted for game protocols):

On each RTT sample $R$:

$$\text{RTTVAR} = (1 - \beta) \times \text{RTTVAR} + \beta \times |R - \text{SRTT}|$$
$$\text{SRTT} = (1 - \alpha) \times \text{SRTT} + \alpha \times R$$
$$\text{RTO} = \text{SRTT} + K \times \text{RTTVAR}$$

Standard values: $\alpha = 0.125$, $\beta = 0.25$, $K = 4$.

This makes RTO responsive to both the average RTT and its variability. A stable 50ms connection gets RTO ≈ 50 + 4×(small) ≈ 60-70ms. A variable connection with SRTT=50ms and RTTVAR=20ms gets RTO ≈ 50 + 80 = 130ms.

### Minimum RTO

TCP mandates a minimum RTO of 1 second. For game protocols this is far too conservative. Practical minimums:

- **Competitive games**: 50-100ms minimum RTO
- **Casual games**: 100-200ms minimum RTO
- **Low-rate protocols**: 200-500ms minimum RTO

The minimum prevents pathologically aggressive retransmission on very low-latency LAN connections where even small measurement errors could trigger spurious retransmits.

### PTO: Probe Timeout

A **Probe Timeout (PTO)** is a lighter alternative to RTO. Instead of retransmitting the lost message, the sender sends a small probe packet (or a packet containing only ACK data) to elicit an ACK from the remote side.

If the probe is ACKed along with the original packet, no retransmission was needed. If the probe is ACKed but the original packet still isn't, genuine loss is confirmed.

PTO is typically shorter than RTO (e.g., 1.5× SRTT instead of SRTT + 4×RTTVAR) because it's low-cost and exploratory. QUIC uses PTO as its primary loss detection trigger rather than RTO.

### Worked Example: SRTT Computation

Starting with initial RTT guess = 100ms:

| Sample  | R (ms) | SRTT (ms) | RTTVAR (ms) | RTO (ms) |
| ------- | ------ | --------- | ----------- | -------- |
| Initial | —      | 100       | 50          | 300      |
| 1       | 85     | 98.1      | 41.3        | 263      |
| 2       | 90     | 97.1      | 33.7        | 232      |
| 3       | 120    | 100.0     | 31.0        | 224      |
| 4       | 95     | 99.4      | 24.4        | 197      |
| 5       | 200    | 112.0     | 43.5        | 286      |

Sample 5 is a spike (200ms). Note how RTTVAR jumps and RTO increases to accommodate the new variability. This is the correct behavior — the system becomes more conservative after observing instability.

---

## 3. Spurious Retransmit Risks vs Delayed Recovery Risks

### Spurious Retransmits

A retransmit is "spurious" when the original packet (or its ACK) was merely delayed, not lost. The retransmitted copy arrives as a duplicate.

**Consequences**:

- Wasted bandwidth (the duplicate is discarded).
- Incorrect RTT samples: if you measure RTT from the retransmit (not the original), you get an artificially short RTT sample. This makes RTO shrink, causing more spurious retransmits — a positive feedback loop.
- Congestion signals may trigger if the protocol interprets retransmitted traffic as congestion feedback.
- Under high jitter, spurious retransmits can consume 10-30% of available bandwidth.

**Mitigation — Karn's Algorithm**: do not use RTT samples from retransmitted packets. Since you don't know whether the ACK corresponds to the original send or the retransmit, the sample is ambiguous. Only update SRTT from packets that were sent once and ACKed once.

**Mitigation — Eifel Detection**: if the original packet is eventually ACK'd (via a later ACK that covers it), detect that the retransmit was spurious. Revert any congestion response and adjust RTO upward to avoid future spurious retransmits.

### Delayed Recovery

The opposite problem: you wait too long to retransmit. The remote side has a gap in its data for the full delay.

For unreliable data this barely matters (the next snapshot fills the gap). For reliable-ordered data, it's painful — every message behind the gap is blocked.

**Consequence trade-offs**:

| Metric                     | Spurious Retransmit        | Delayed Recovery        |
| -------------------------- | -------------------------- | ----------------------- |
| Bandwidth waste            | High                       | None                    |
| Latency impact             | Indirect (congestion risk) | Direct (recovery delay) |
| Player impact (unreliable) | Minimal                    | Minimal                 |
| Player impact (reliable)   | Minimal                    | Significant             |
| Congestion risk            | Increases                  | None                    |

### Finding the Balance

- For unreliable-heavy protocols (most game state): err toward slower detection. False positives (spurious retransmits) waste bandwidth that could carry fresh state. False negatives (missed loss) are harmless because fresh data supersedes.
- For reliable-heavy protocols (chat, commands): err toward faster detection. The cost of delayed recovery (blocked messages, delayed effects) is worse than occasional spurious retransmits.

Most game protocols use NACK inference for reliable messages (fast detection) and don't retransmit unreliable messages at all.

---

## 4. Exponential Backoff and Safety Under Uncertain Conditions

### Why Backoff Exists

When a retransmission itself is not acknowledged, the natural instinct is to retransmit again immediately. This is dangerous: if the path is congested, adding more packets makes congestion worse.

Exponential backoff addresses this by doubling the wait time between successive retransmissions:

$$\text{RTO}_n = \text{RTO}_{\text{base}} \times 2^n$$

Where $n$ is the retransmission attempt number.

### Backoff Schedule

Starting with base RTO = 200ms:

| Attempt | Wait Time | Cumulative Time |
| ------- | --------- | --------------- |
| 1       | 200ms     | 200ms           |
| 2       | 400ms     | 600ms           |
| 3       | 800ms     | 1.4s            |
| 4       | 1600ms    | 3.0s            |
| 5       | 3200ms    | 6.2s            |
| 6 (cap) | 5000ms    | 11.2s           |

After attempt 6, the timeout is capped (typically at 5-10 seconds for game protocols). Beyond this, the connection is likely dead or severely impaired.

### When to Reset Backoff

Backoff resets when fresh evidence of connectivity arrives:

- A new ACK is received (for any packet, not just the retransmitted one).
- A new packet is received from the remote side.

This evidence tells you the path is (at least partially) working, so the aggressive backoff is no longer needed.

### Backoff in Practice for Game Protocols

Game protocols rarely reach high backoff levels because:

1. Most data is unreliable and not retransmitted at all.
2. Reliable messages are small and infrequent.
3. If the connection is so degraded that backoff reaches level 4+, the game usually disconnects or switches to a degraded mode.

The main value of backoff is preventing pathological behavior during brief congestion episodes — the "thundering herd" where multiple retransmissions hit the network simultaneously.

### Interaction with Game Disconnection

Game clients typically have a disconnect timeout (5-15 seconds). If no data is received from the server within this window, the client considers the connection lost. Exponential backoff must stay within this window — if backoff reaches a point where the next retransmit would be after the disconnect timeout, there's no point retransmitting.

```
MaxRetransmitAttempts = floor(log2(disconnectTimeout / baseRTO))
```

For disconnect timeout = 10s and base RTO = 200ms: `floor(log2(50)) = 5` attempts maximum.

---

## 5. Why "Resend Everything Immediately" Destabilizes the System

### The Tempting Failure Mode

A developer notices packet loss is causing game state gaps. The "obvious" fix: detect loss immediately and retransmit all pending reliable messages right away. This feels proactive and responsible.

In reality, it's one of the most damaging things you can do.

### What Happens When You Resend Everything

1. **Bandwidth spike**: all pending reliable messages are packed into one or more packets and sent simultaneously. This burst may exceed the path's capacity.
2. **Queue overload**: the router queue (typically 1-10 packets) fills. Subsequent packets are dropped — including fresh unreliable state that players need more than the retransmitted old data.
3. **Retransmission cascade**: the burst itself causes loss. New loss triggers new retransmissions. Each cycle adds more packets.
4. **Latency spike**: queue filling causes all packets (including freshly sent ones) to wait behind the burst. RTT jumps for every client sharing the path.
5. **Fairness impact**: other clients sharing the server's uplink experience degraded quality because one client's retransmission burst consumed shared capacity.

### Worked Example: Retransmission Burst

Server has 5 pending reliable messages at 200 bytes each. Loss event triggers immediate retransmit of all 5.

**Without pacing**:

- 5 packets sent in < 1ms (back-to-back).
- Router buffer holds 3 packets. Packets 4 and 5 are dropped.
- Packets 4 and 5 are detected as lost → retransmitted → some are dropped again.
- Fresh unreliable state for other clients is delayed behind the queue.

**With pacing** (one retransmit per send interval):

- 1 retransmit per 50ms, spreads over 250ms.
- No buffer overflow. Fresh data interleaved normally.
- Recovery takes longer but doesn't harm other traffic.

### The Correct Approach

1. **Pace retransmissions**: spread them across multiple send intervals instead of bursting.
2. **Prioritize critical retransmissions**: if 5 messages are pending, retransmit the most important ones first.
3. **Respect the congestion window**: retransmissions count toward the send budget, not in addition to it.
4. **Combine with fresh data**: pack one retransmission per packet alongside fresh state, rather than dedicated retransmission-only packets.

### Retransmission as Budget, Not Emergency

Think of retransmission as a line item in the per-tick send budget:

$$B_{\text{tick}} = B_{\text{fresh}} + B_{\text{retransmit}} + B_{\text{overhead}}$$

If the total budget is 400 bytes per tick:

- Fresh state: 300 bytes
- Retransmit: 80 bytes (one message)
- Overhead (headers, ACK): 20 bytes

This means recovery from 5 lost messages takes 5 ticks (250ms at 20 Hz). That's acceptable. Trying to recover in 1 tick would require a 1000-byte burst — 2.5× the normal budget.

---

## 6. Advanced Topics

### Fast Retransmit Without Timeout

Using the ACK bitfield, detect loss faster than RTO:

```
for each unacked packet P:
    if (3 or more subsequent packets have been ACK'd after P):
        declare P lost
        schedule retransmission
```

This is analogous to TCP's fast retransmit but adapted for the bitfield pattern. It triggers in ~3 packet intervals instead of waiting for RTO.

### Tail Loss Probes

Loss of the last few packets in a burst is hard to detect via NACK inference (no subsequent packets to provide feedback). A **Tail Loss Probe (TLP)** sends a small packet after a brief timeout (1-2× SRTT) to elicit ACK feedback:

If the TLP is ACK'd and the pending packets still aren't, they were lost. If the TLP triggers an ACK that covers the pending packets, they were just delayed.

TLP is especially useful for low-rate protocols where timeouts would otherwise be very long.

### Selective Retransmission

Instead of retransmitting the entire original packet, retransmit only the reliable messages that were in the lost packet. This is more efficient when:

- The original packet also contained unreliable data (which is now stale and shouldn't be resent).
- Multiple reliable messages can be combined into one retransmission packet.

### RTT Measurement for Retransmitted Packets

As mentioned (Karn's Algorithm), don't use RTT samples from retransmitted packets. But you can:

- Measure RTT from the PTO/TLP probe packets (which are fresh, not retransmits).
- Use the ACK bitfield to identify packets that were sent once and use those for RTT samples.
- Timestamp outbound packets and have the remote echo the timestamp back — this gives per-packet RTT measurement independent of retransmission state.

---

## Common Anti-Patterns

1. **No RTO minimum**: allows pathologically short timeouts on LAN, causing constant spurious retransmits when any jitter occurs.
2. **Using RTT samples from retransmits**: creates feedback loop of shrinking RTO → more spurious retransmits → more wrong samples.
3. **Not backing off**: constant-interval retransmission hammers a congested path instead of giving it time to recover.
4. **Retransmitting unreliable data**: position updates should never be retransmitted — the next update supersedes the lost one.
5. **Bursting all retransmissions together**: causes queue overflow and loss cascade as described above.
6. **Disconnecting on first retransmission failure**: single-digit loss rates are normal on the internet. Only disconnect after sustained inability to communicate.
7. **Ignoring loss for too long**: the opposite extreme — waiting 5+ seconds before any retransmission because "maybe it's just slow." Use NACK inference for fast detection of genuine loss.

---

## Code Example (C++): Loss Detector with NACK and Timeout

```cpp
#include <cstdint>
#include <vector>
#include <cmath>
#include <algorithm>

struct SentPacketRecord {
    uint16_t sequence;
    double sendTime;
    bool acked;
    bool lossDetected;
    int subsequentAcks;  // How many later packets have been ACK'd
};

class LossDetector {
    double srtt = 100.0;       // ms
    double rttvar = 50.0;      // ms
    double rto = 300.0;        // ms
    double minRto = 50.0;      // ms
    double maxRto = 5000.0;    // ms

    static constexpr double Alpha = 0.125;
    static constexpr double Beta = 0.25;
    static constexpr double K = 4.0;
    static constexpr int NackThreshold = 3;

    std::vector<SentPacketRecord> sent;

public:
    void recordSend(uint16_t seq, double time) {
        sent.push_back({seq, time, false, false, 0});
    }

    void onAck(uint16_t seq, double now) {
        for (auto& p : sent) {
            if (p.sequence == seq && !p.acked) {
                p.acked = true;

                // Update RTT only for non-retransmitted packets
                double sample = now - p.sendTime;
                updateRtt(sample);

                // Increment subsequentAcks for all earlier unacked packets
                for (auto& older : sent) {
                    if (!older.acked && !older.lossDetected &&
                        isOlder(older.sequence, seq)) {
                        older.subsequentAcks++;
                    }
                }
                break;
            }
        }
    }

    struct LossEvent {
        uint16_t sequence;
        double sendTime;
    };

    std::vector<LossEvent> detectLosses(double now) {
        std::vector<LossEvent> losses;

        for (auto& p : sent) {
            if (p.acked || p.lossDetected)
                continue;

            bool lost = false;

            // NACK inference: 3+ subsequent packets ACK'd
            if (p.subsequentAcks >= NackThreshold) {
                lost = true;
            }

            // Timeout: RTO exceeded
            if ((now - p.sendTime) > rto) {
                lost = true;
            }

            if (lost) {
                p.lossDetected = true;
                losses.push_back({p.sequence, p.sendTime});
            }
        }

        return losses;
    }

    double getCurrentRto() const { return rto; }
    double getSrtt() const { return srtt; }
    double getRttvar() const { return rttvar; }

private:
    void updateRtt(double sample) {
        rttvar = (1 - Beta) * rttvar + Beta * std::abs(sample - srtt);
        srtt = (1 - Alpha) * srtt + Alpha * sample;
        rto = srtt + K * rttvar;
        rto = std::clamp(rto, minRto, maxRto);
    }

    static bool isOlder(uint16_t a, uint16_t b) {
        return (int16_t)(a - b) < 0;
    }
};
```

## Code Example (C#): Retransmission Scheduler with Backoff

```csharp
using System;
using System.Collections.Generic;

public class RetransmissionScheduler
{
    private double _srtt = 100;
    private double _rttvar = 50;
    private double _rto = 300;
    private const double MinRto = 50;
    private const double MaxRto = 5000;
    private const int MaxAttempts = 6;

    private readonly List<PendingRetransmit> _pending = new();

    private struct PendingRetransmit
    {
        public ushort MessageId;
        public byte[] Payload;
        public double FirstSendTime;
        public double NextRetransmitTime;
        public int Attempt;
    }

    public void UpdateRtt(double sampleMs)
    {
        _rttvar = 0.75 * _rttvar + 0.25 * Math.Abs(sampleMs - _srtt);
        _srtt = 0.875 * _srtt + 0.125 * sampleMs;
        _rto = Math.Clamp(_srtt + 4.0 * _rttvar, MinRto, MaxRto);
    }

    public void ScheduleRetransmit(ushort messageId, byte[] payload, double now)
    {
        _pending.Add(new PendingRetransmit
        {
            MessageId = messageId,
            Payload = payload,
            FirstSendTime = now,
            NextRetransmitTime = now + _rto,
            Attempt = 0
        });
    }

    public void OnAcked(ushort messageId)
    {
        _pending.RemoveAll(p => p.MessageId == messageId);
    }

    /// <summary>
    /// Returns messages ready for retransmission. Caller should pace these
    /// (one per send interval) rather than sending all at once.
    /// </summary>
    public List<(ushort Id, byte[] Payload)> GetDueRetransmissions(double now)
    {
        var due = new List<(ushort, byte[])>();
        var expired = new List<int>();

        for (int i = 0; i < _pending.Count; i++)
        {
            var p = _pending[i];

            if (p.Attempt >= MaxAttempts)
            {
                expired.Add(i);
                continue;
            }

            if (now >= p.NextRetransmitTime)
            {
                due.Add((p.MessageId, p.Payload));

                // Exponential backoff
                p.Attempt++;
                double backoffRto = _rto * Math.Pow(2, p.Attempt);
                backoffRto = Math.Min(backoffRto, MaxRto);
                p.NextRetransmitTime = now + backoffRto;
                _pending[i] = p;
            }
        }

        // Remove expired (iterate in reverse to preserve indices)
        for (int i = expired.Count - 1; i >= 0; i--)
            _pending.RemoveAt(expired[i]);

        return due;
    }

    public int PendingCount => _pending.Count;
    public double CurrentRto => _rto;
}
```
