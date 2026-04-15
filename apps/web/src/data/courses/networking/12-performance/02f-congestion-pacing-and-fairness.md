# Congestion, Pacing, and Fairness Under Load

Network performance is not just a local system property — it depends on the behavior of every other application sharing the same path. A game server that sends packets faster than the path can carry doesn't just hurt itself; it hurts every other user on the link. Congestion control is how you avoid being that bad neighbor while still delivering the best possible experience.

---

## 1. Congestion Response Principles for UDP-Based Systems

### What Is Congestion?

Congestion occurs when the aggregate traffic entering a network link exceeds its capacity. The excess packets queue in router buffers. When buffers fill, packets are dropped. Even before drops occur, queuing adds latency.

```
Sender rate > Link capacity → Queue grows → Latency rises → Drops begin
```

### Why UDP Must Care

TCP has built-in congestion control: it reduces its send rate when it detects loss or delay. UDP has none. A naive UDP-based game protocol can:

- Fill router buffers, spiking latency for all users on the link.
- Cause loss for itself and others.
- Trigger ISP quality-of-service policies that throttle or deprioritize the traffic.
- On shared home networks, make web browsing, video calls, and other TCP traffic crawl.

Not implementing congestion response doesn't mean you avoid congestion — it means you cause congestion without recovering from it.

### Core Principles

1. **Don't send faster than the path can carry.** Measure the path's capacity and respect it.
2. **When signals indicate congestion, reduce.** Loss and rising delay are congestion signals.
3. **When signals indicate recovery, increase cautiously.** Don't jump back to full rate — probe carefully.
4. **Account for all traffic, not just yours.** The path is shared. Your protocol's fair share is a fraction of total capacity.

### Detecting Congestion

Three primary signals:

| Signal                                 | How Detected                          | Reliability                                                      |
| -------------------------------------- | ------------------------------------- | ---------------------------------------------------------------- |
| Packet loss                            | ACK gaps, timeout, NACK inference     | High (loss almost always means congestion on modern networks)    |
| Delay increase                         | RTT measurement rising above baseline | Moderate (can be caused by routing changes, not just congestion) |
| Explicit Congestion Notification (ECN) | IP header bits set by routers         | High (direct signal, but not universally deployed)               |

For game protocols, **loss** and **delay increase** are the practical signals. ECN is theoretically ideal but rarely used in practice.

### Delay-Based vs Loss-Based Detection

**Loss-based**: reduce send rate when packets are dropped. This is reliable but reactive — congestion has already caused damage before you detect it. The queue was full, packets were lost, and latency already spiked.

**Delay-based**: reduce send rate when RTT increases above a baseline. This is proactive — you detect the growing queue before it overflows. But it's fragile — RTT can increase for reasons other than congestion (route changes, WiFi contention), leading to false positives.

Most practical game protocols use a **hybrid approach**:

- Use delay increase as an early warning to slow probing.
- Use loss as a definitive signal to cut send rate.
- Use sustained low delay and zero loss as evidence to increase rate.

### Worked Example: Congestion Detection

A game server tracks per-client path quality:

| Tick | SRTT (ms) | Baseline RTT (ms) | Loss Rate | Congestion Signal?                  |
| ---- | --------- | ----------------- | --------- | ----------------------------------- |
| 100  | 42        | 40                | 0%        | No                                  |
| 200  | 45        | 40                | 0%        | Mild delay increase                 |
| 300  | 58        | 40                | 0%        | Delay threshold exceeded (+45%)     |
| 400  | 65        | 40                | 2%        | Delay + loss → confirmed congestion |
| 500  | 50        | 40                | 0%        | Recovering                          |
| 600  | 43        | 40                | 0%        | Recovered                           |

At tick 300, the delay increase alone triggers a proactive rate reduction. At tick 400, loss confirms congestion and may trigger a more aggressive cut. By tick 600, conditions have stabilized and the rate can carefully increase.

---

## 2. Pacing vs Burst Sending

### The Burst Problem

A server sending at 20 Hz might prepare a packet every 50ms. But under load or batched processing, it might prepare 3 packets in quick succession and then wait 150ms:

```
Time:       |0ms  |1ms  |2ms  |          |150ms |151ms |152ms |
Packets:    P1    P2    P3               P4     P5     P6
```

This **burst** of 3 packets hits the network together. Even if the average rate is within budget, the instantaneous burst rate far exceeds it. Short bursts can:

- Overflow small router queues (many home routers have tiny buffers).
- Cause loss within the burst when a queue is nearly full.
- Interact badly with WiFi scheduling (WiFi transmits in time slots; a burst may miss the next slot).
- Spike latency for the burst packets and all packets queued behind them.

### Pacing: Spreading Packets Over Time

Pacing means sending packets at regular intervals rather than in bursts:

```
Time:       |0ms     |50ms    |100ms   |150ms   |200ms   |
Packets:    P1       P2       P3       P4       P5
```

Each packet is separated by the send interval. No bursts. Queue occupancy is minimal. Latency is predictable.

### Implementing Pacing

The simplest approach: use a **send timer** that fires at the desired send interval. The timer callback serializes and sends exactly one packet:

```
// Pseudocode
while (running) {
    waitUntil(nextSendTime);
    packet = buildPacket();
    send(packet);
    nextSendTime += sendInterval;
}
```

Under load, if the build step takes longer than the send interval, packets accumulate and create a burst on the next send. To prevent this:

- Drop or defer low-priority content if build time exceeds budget.
- Enforce a maximum of 1 packet per timer invocation (never send 2 to "catch up").
- If falling behind, skip content rather than bursting.

### Pacing with Multiple Clients

A server with 32 clients at 20 Hz must send 640 packets/second. If all 32 packets per tick are prepared in one batch and sent together, that's a 32-packet burst every 50ms.

Better: stagger sends across the tick interval. Client 1 is sent at tick + 0ms, client 2 at tick + 1.5ms, client 3 at tick + 3ms, etc. The 32 packets are spread over the full 50ms window.

$$\text{stagger}(i) = \frac{i \times T_{\text{tick}}}{N_{\text{clients}}}$$

For 32 clients over a 50ms tick: stagger ≈ 1.56ms between clients. This is smooth enough to avoid burst effects on most paths.

### When Pacing Matters Most

- **Upstream bottleneck**: home upload links are often 1-10 Mbit/s. A burst of 10 packets at 300 bytes each is only 3KB, but at 1 Mbit/s upload, that takes 24ms to transmit — nearly half a 50ms tick interval.
- **WiFi**: WiFi has per-packet overhead and scheduling delays. Bursts cause more contention and retransmissions at the MAC layer.
- **Mobile**: cellular schedulers allocate airtime. Bursts may require waiting for the next scheduling opportunity.

---

## 3. Send-Rate Adaptation from Measured Path Signals

### Why Fixed Rates Fail

A fixed send rate works when the path is stable. But paths degrade:

- A player's roommate starts streaming 4K video.
- WiFi interference spikes during peak hours.
- A routing change adds 50ms of latency.
- The player's ISP throttles UDP during congestion events.

A fixed rate either wastes capacity (set too low for normal conditions) or causes congestion (set right for normal but too high for degraded conditions).

### Adaptive Rate Control

The server monitors per-client path metrics and adjusts send behavior:

1. **Monitor**: track SRTT, jitter, loss rate, and throughput per client.
2. **Classify**: determine if the path is healthy, stressed, or severely degraded.
3. **Adapt**: change send rate, content per packet, or reliability class based on classification.
4. **Recover**: when metrics improve, cautiously increase back toward normal.

### Rate Adaptation Strategies

**Strategy 1: Step-Down Profiles**

Define 2-4 quality profiles and switch between them based on congestion signals (as described in the tick rate topic):

| Profile | Send Rate | Content                  | Trigger                    |
| ------- | --------- | ------------------------ | -------------------------- |
| Full    | 20 Hz     | All state                | Default                    |
| Reduced | 10 Hz     | Critical + high priority | RTT > 1.5× baseline for 2s |
| Minimal | 5 Hz      | Critical only            | Loss > 5% for 3s           |

**Strategy 2: Continuous Rate Adjustment**

Use an AIMD (Additive Increase, Multiplicative Decrease) approach similar to TCP:

- **Increase**: if no congestion signals for an interval, increase send rate by a small fixed amount (e.g., +0.5 pkt/s).
- **Decrease**: on congestion signal, multiply send rate by a reduction factor (e.g., ×0.7).

$$R_{new} = \begin{cases} R_{old} + \alpha & \text{if no congestion} \\ R_{old} \times \beta & \text{if congestion detected} \end{cases}$$

With $\alpha = 0.5$ pkt/s and $\beta = 0.7$.

**Strategy 3: Target Delay**

Set a target one-way delay (OWD) and adjust send rate to maintain it:

- If measured OWD < target: path has headroom, can increase rate.
- If measured OWD > target: path is loaded, reduce rate.
- If measured OWD >> target: path is congested, cut rate aggressively.

This is similar to how BBR (Google's congestion control) works — it targets a delay point rather than probing to loss.

### Worked Example: AIMD Rate Adaptation

Starting at 20 pkt/s, $\alpha = 0.5$, $\beta = 0.7$:

| Second | Event               | Rate (pkt/s) |
| ------ | ------------------- | ------------ |
| 0      | Start               | 20.0         |
| 1      | No congestion       | 20.5         |
| 2      | No congestion       | 21.0         |
| 3      | Congestion detected | 14.7         |
| 4      | No congestion       | 15.2         |
| 5      | No congestion       | 15.7         |
| 6      | No congestion       | 16.2         |
| 7      | Congestion detected | 11.3         |
| 8      | No congestion       | 11.8         |
| ...    | Recovery continues  | ...          |

The sawtooth pattern finds the sustainable rate. Over time, if the path's capacity is ~18 pkt/s, the rate oscillates between ~13 and ~18.

---

## 4. TCP-Friendliness and Coexistence

### The Fairness Problem

Most internet traffic is TCP. TCP reduces its rate during congestion. If your UDP game protocol doesn't reduce its rate, it effectively steals bandwidth from TCP flows — including the player's own web browsing, streaming, and downloads.

This is not just an abstract concern. On a typical home connection:

- Player has 10 Mbit/s upload.
- Game uses 1 Mbit/s (fixed rate, no congestion control).
- Player's family streams video (TCP), which needs 5 Mbit/s.
- Total demand: 6 Mbit/s — fits fine.
- ISP has a momentary congestion event, effective capacity drops to 4 Mbit/s.
- TCP backs off, getting 3 Mbit/s. Game still sends 1 Mbit/s.
- TCP further backs off to 2.5 Mbit/s. Video quality drops dramatically.
- If the game also backed off to 0.7 Mbit/s, TCP could hold 3.3 Mbit/s — video stays watchable.

### TCP-Friendly Rate Control (TFRC)

TFRC (RFC 5348) is a rate-based congestion control that achieves throughput roughly equal to a TCP flow under the same conditions. The steady-state rate equation:

$$R = \frac{s}{\text{RTT} \times \sqrt{\frac{2p}{3}} + T_{RTO} \times \left(3\sqrt{\frac{3p}{8}}\right) \times p \times \left(1 + 32p^2\right)}$$

Where:

- $s$ = packet size
- $p$ = loss event rate
- $T_{RTO}$ = retransmission timeout

This is complex, but the intuition is simple: **higher loss rate → lower allowed send rate**, in the same proportion as TCP would reduce.

### Practical Alternatives to TFRC

Full TFRC is rarely implemented in game protocols. Practical approaches:

1. **Bandwidth cap with congestion backoff**: set a maximum rate per client and reduce it by a fixed percentage when congestion is detected. Simpler than TFRC but achieves similar coexistence behavior.

2. **Delay-based rate limiting**: monitor one-way delay and reduce rate when delay exceeds a threshold. This backs off before loss occurs, leaving capacity for TCP flows.

3. **Budget ceiling**: calculate the maximum bytes/sec the game should ever use (e.g., 2% of the expected home uplink) and enforce it as a hard cap. Under congestion, TCP reduces within the remaining 98%.

### How Much Bandwidth Does a Game Actually Need?

Game traffic is often a tiny fraction of link capacity:

| Scenario                               | Bandwidth     |
| -------------------------------------- | ------------- |
| FPS, 60Hz state updates, 20 players    | ~100-300 KB/s |
| MMO, 10Hz state, 100 nearby entities   | ~50-150 KB/s  |
| Strategy, 5Hz state, bulk unit updates | ~30-80 KB/s   |
| Voice chat (Opus codec)                | ~6-10 KB/s    |

Even the highest case (300 KB/s = 2.4 Mbit/s) is modest compared to video streaming (5-25 Mbit/s). The challenge is not bulk bandwidth but **latency sensitivity** — games need low delay, which is orthogonal to throughput.

---

## 5. Throughput vs Latency Fairness Tradeoffs

### Two Kinds of Fairness

**Throughput fairness**: each flow gets an equal share of bottleneck bandwidth. TCP approximates this through AIMD.

**Latency fairness**: each flow experiences similar delay. This is harder to achieve because queuing delay depends on all flows' behavior.

### The Game's Priority

Games care far more about latency than throughput. A game would rather send 50 KB/s with 20ms latency than 200 KB/s with 100ms latency. This inverts the usual optimization target.

### Bufferbloat: When Throughput Optimization Hurts Latency

Modern networks often have oversized buffers (a problem called "bufferbloat"). When a large TCP download fills the buffer, latency spikes from 10ms to 500ms+ — even for small game packets sharing the queue.

The buffer absorbs bursts (preventing loss) but converts dropped packets into massive delay. From the game's perspective, this is worse than loss — at least with loss, the next packet might arrive quickly.

### Queue Management Interactions

Router algorithms that manage queues affect game traffic:

| Algorithm      | Behavior                           | Game Impact                           |
| -------------- | ---------------------------------- | ------------------------------------- |
| Drop-tail      | Drop newest packet when full       | Burst loss during congestion          |
| RED/AQM        | Probabilistically drop before full | Gradual degradation, better for games |
| CoDel/FQ-CoDel | Drop based on sojourn time         | Targets bufferbloat; great for games  |
| Fair queuing   | Each flow gets a queue             | Isolates game from bulk downloads     |

FQ-CoDel (fair queuing + controlled delay) is increasingly common in home routers and is particularly beneficial for games — it gives the game's small-packet low-rate flow a fair share of the link with minimal delay, even when bulk downloads are saturating the link.

### What the Game Can Control

The game cannot control router configuration, but it can:

1. **Keep packets small**: small packets are more likely to fit in fair-queue slots and less likely to be the "oversize" packet that gets dropped.
2. **Pace evenly**: smooth send patterns work better with all queue management schemes than bursts.
3. **Back off on delay increase**: if delay is rising, the path is buffering — reduce rate before loss occurs.
4. **Use DSCP marking**: set the Differentiated Services Code Point (DSCP) in the IP header to request low-latency treatment. Set EF (Expedited Forwarding, DSCP 46) for game traffic. Not all ISPs honor it, but it doesn't hurt.
5. **Minimize total traffic**: the less you send on the shared link, the less you contribute to queuing for everyone.

---

## 6. Practical Checklist

- [ ] Implement some form of send-rate adaptation (AIMD, profiles, or delay-based).
- [ ] Pace sends evenly across tick intervals — never burst multiple packets back-to-back.
- [ ] Stagger per-client sends across the tick to spread server-side bursts.
- [ ] Use delay increase as an early congestion warning before loss occurs.
- [ ] Set a per-client bandwidth ceiling that respects typical home uplink capacity.
- [ ] Test on constrained links (throttle to 1 Mbit/s) to verify graceful degradation.
- [ ] Monitor queue-induced latency (not just end-to-end RTT) if possible.
- [ ] Reduce reliable retransmission rate during congestion (don't escalate).
- [ ] Consider DSCP marking for low-latency treatment.
- [ ] Verify that congestion recovery doesn't overshoot (increase gradually).

---

## Code Example (C++): Delay-Based Congestion Controller

```cpp
#include <algorithm>
#include <cmath>
#include <cstdint>

class CongestionController {
    double sendRatePps = 20.0;      // packets per second
    double minRate = 5.0;
    double maxRate = 60.0;
    double baselineRtt = -1.0;      // ms, measured minimum
    double delayThreshold = 1.5;    // 50% above baseline
    double increaseStep = 0.5;      // pkt/s per second
    double decreaseFactor = 0.7;
    double lastUpdateTime = 0;

    // Track loss for secondary signal
    int packetsTotal = 0;
    int packetsLost = 0;

public:
    void onRttSample(double rttMs) {
        if (baselineRtt < 0 || rttMs < baselineRtt) {
            baselineRtt = rttMs;
        }
    }

    void onPacketAcked() { packetsTotal++; }
    void onPacketLost() { packetsTotal++; packetsLost++; }

    void update(double now, double currentRtt) {
        double dt = now - lastUpdateTime;
        if (dt < 1.0) return; // Update every second
        lastUpdateTime = now;

        bool delayCongestion = baselineRtt > 0 &&
                               currentRtt > baselineRtt * delayThreshold;
        double lossRate = packetsTotal > 0
            ? (double)packetsLost / packetsTotal
            : 0.0;
        bool lossCongestion = lossRate > 0.02; // 2%

        if (lossCongestion) {
            // Aggressive reduction on loss
            sendRatePps *= decreaseFactor;
        } else if (delayCongestion) {
            // Mild reduction on delay increase
            sendRatePps *= 0.9;
        } else {
            // Additive increase
            sendRatePps += increaseStep;
        }

        sendRatePps = std::clamp(sendRatePps, minRate, maxRate);

        // Reset counters
        packetsTotal = 0;
        packetsLost = 0;
    }

    double getSendInterval() const {
        return 1000.0 / sendRatePps; // milliseconds
    }

    double getSendRate() const { return sendRatePps; }
};
```

## Code Example (C#): Paced Sender with Staggered Client Sends

```csharp
using System;
using System.Collections.Generic;

public class PacedSender
{
    private double _tickIntervalMs;
    private double _lastTickTime;
    private readonly List<ClientSendState> _clients = new();

    public PacedSender(double tickHz)
    {
        _tickIntervalMs = 1000.0 / tickHz;
    }

    public void AddClient(int clientId)
    {
        _clients.Add(new ClientSendState { ClientId = clientId });
        RecalculateStagger();
    }

    public void RemoveClient(int clientId)
    {
        _clients.RemoveAll(c => c.ClientId == clientId);
        RecalculateStagger();
    }

    private void RecalculateStagger()
    {
        if (_clients.Count == 0) return;
        double gap = _tickIntervalMs / _clients.Count;
        for (int i = 0; i < _clients.Count; i++)
        {
            var c = _clients[i];
            c.StaggerOffsetMs = i * gap;
            _clients[i] = c;
        }
    }

    /// <summary>
    /// Called each tick. Returns ordered list of (clientId, sendTimeOffset)
    /// pairs. Caller should schedule sends at tickStart + offset.
    /// </summary>
    public List<(int ClientId, double OffsetMs)> GetSendSchedule()
    {
        var schedule = new List<(int, double)>();
        foreach (var c in _clients)
        {
            schedule.Add((c.ClientId, c.StaggerOffsetMs));
        }
        return schedule;
    }

    private struct ClientSendState
    {
        public int ClientId;
        public double StaggerOffsetMs;
    }
}

public class AdaptiveRateController
{
    private double _ratePps = 20.0;
    private double _baselineRtt = -1;
    private int _packetsAcked;
    private int _packetsLost;

    public double MinRate { get; set; } = 5.0;
    public double MaxRate { get; set; } = 60.0;
    public double DelayThresholdMultiplier { get; set; } = 1.5;

    public void OnRttSample(double rttMs)
    {
        if (_baselineRtt < 0 || rttMs < _baselineRtt)
            _baselineRtt = rttMs;
    }

    public void OnPacketAcked() => _packetsAcked++;
    public void OnPacketLost() => _packetsLost++;

    public void Update(double currentRttMs)
    {
        int total = _packetsAcked + _packetsLost;
        double lossRate = total > 0 ? (double)_packetsLost / total : 0;

        bool delayCongested = _baselineRtt > 0 &&
            currentRttMs > _baselineRtt * DelayThresholdMultiplier;
        bool lossCongested = lossRate > 0.02;

        if (lossCongested)
            _ratePps *= 0.7;
        else if (delayCongested)
            _ratePps *= 0.9;
        else
            _ratePps += 0.5;

        _ratePps = Math.Clamp(_ratePps, MinRate, MaxRate);
        _packetsAcked = 0;
        _packetsLost = 0;
    }

    public double SendIntervalMs => 1000.0 / _ratePps;
    public double CurrentRate => _ratePps;
}
```
