# Measuring the Right Signals: Latency, Jitter, and Packet Loss

Most networking failures are first **measurement failures**. Teams optimize what they can see — usually average ping — while players suffer from what they cannot see: jitter spikes, burst loss, queue buildup, and tail-latency events that averages hide completely.

---

## 1. Latency vs RTT vs Throughput — Not Interchangeable

These three metrics describe different physical phenomena. Confusing them leads to incorrect tuning decisions.

### One-Way Latency

One-way latency is the time a packet takes from sender to receiver. It is the most directly relevant metric for "how stale is the state the other side sees?" but also the hardest to measure because it requires synchronized clocks on both ends.

Approaches to one-way measurement:

- **NTP-synchronized clocks**: practical but adds measurement noise from NTP drift. Accuracy is typically ±5-20ms on public internet, which is often comparable to the latency itself.
- **PTP (Precision Time Protocol)**: sub-microsecond in controlled LAN environments, impractical on public internet.
- **Estimated from RTT/2**: common approximation, but assumes symmetric paths. Real internet paths are frequently asymmetric — upload and download traverse different queues, different ISP peering, different buffer depths.

In practice, most game networking systems measure RTT and work with it directly, accepting the asymmetry limitation.

### Round-Trip Time (RTT)

RTT measures the full round trip: sender → receiver → acknowledgment back. It is the easiest reliable metric because it uses one clock.

Measurement approach:

1. Sender records send timestamp for packet with sequence number S.
2. Receiver acknowledges S (in its next outbound packet, via ACK field).
3. Sender computes RTT = now - send_timestamp(S).

Key subtlety: the receiver may not ACK immediately. If the receiver batches ACKs or only sends packets at its own tick rate, there is **processing delay** baked into the RTT that is not network delay. This matters when RTTs appear higher than expected — the extra time may be receiver-side scheduling, not path latency.

### Throughput

Throughput is bytes delivered per unit time. It measures capacity utilization, not responsiveness.

Critical distinction:

- A link can deliver 10 Mbit/s of throughput while individual packets experience 200ms of queueing delay.
- A game can receive all its state updates within budget while players feel "laggy" because of jitter-induced correction artifacts.

Throughput matters for capacity planning and bandwidth budgets (Topic 7), but it is **not a substitute for latency and jitter measurement** when diagnosing responsiveness problems.

### Worked Example: Why These Diverge

Consider a path with:

- 40ms base RTT
- 2ms jitter standard deviation
- 0.5% random loss
- 5 Mbit/s available throughput

This path has acceptable throughput for most games. But if the jitter occasionally spikes to 80ms (p99 event), interpolation buffers designed for 2ms jitter will underrun, causing visible hitching. Average RTT dashboards will show "40ms average — looks fine." The player experiences periodic teleporting.

---

## 2. Jitter as Delay Variation — Not Just "High Ping"

Jitter is the **variation** in packet arrival timing, not the absolute delay itself. A connection with 100ms stable RTT and zero jitter can feel better than a connection with 50ms average RTT and 40ms jitter range.

### What Jitter Causes

Jitter directly attacks the assumptions of interpolation and buffering systems:

- **Interpolation buffer underruns**: if the buffer expects packets every 50ms but one arrives 120ms late, the renderer runs out of data and must extrapolate or freeze.
- **Visible rubber-banding**: sudden arrival of delayed packets causes position corrections that snap entities.
- **Uneven input acknowledgment cadence**: player inputs get confirmed at irregular intervals, making responsiveness feel inconsistent.
- **Correction amplitude spikes**: when a delayed packet finally arrives, the accumulated correction can be large enough to be visually obvious.

### Measuring Jitter

RFC 3550 defines jitter as the mean deviation of inter-arrival times. A simpler practical approach:

For consecutive packets i and i+1:

$$J_i = |(\text{recv}_{i+1} - \text{recv}_i) - (\text{send}_{i+1} - \text{send}_i)|$$

This measures how much the spacing between received packets differs from the spacing between sent packets. If packets are sent every 50ms and received at 50ms intervals, jitter is zero. If they arrive at 30ms then 70ms intervals, jitter is non-zero even though average spacing is still 50ms.

### Short-Window vs Long-Window Jitter

Track both:

- **Short window (last 16-32 packets)**: reflects current path behavior, useful for real-time adaptation decisions.
- **Long window (last 256-512 packets)**: reflects session-level path character, useful for buffer sizing and profile classification.

When short-window jitter spikes but long-window stays stable, you are seeing transient events. When both rise, the path is degrading.

### Jitter Percentiles Matter More Than Mean

Just like latency, jitter mean hides tail behavior. A path with 2ms mean jitter and occasional 60ms jitter spikes will cause periodic buffer underruns that mean jitter does not predict.

Track p50/p90/p99 jitter alongside p50/p90/p99 RTT.

---

## 3. Packet Loss as Both Reliability and Congestion Signal

Loss has two distinct meanings for your system, and you should track them separately.

### Reliability Impact

When a packet is lost, any messages it carried must be either:

- retransmitted (if reliable class)
- abandoned (if unreliable class — the data is stale)
- reconstructed from redundancy (if FEC is used)

The reliability cost depends entirely on what was in the packet and its class. Losing an unreliable position update is fine — the next one supersedes it. Losing a reliable match-start event requires recovery.

### Path Health Signal

Rising loss rates indicate path stress:

- **Queue overflow**: routers/switches dropping packets when buffers fill (tail-drop or AQM).
- **Wireless contention**: especially on WiFi, where loss can be bursty and correlated with other traffic.
- **ISP throttling or deprioritization**: some paths treat UDP differently under load.

This is the signal that should feed your congestion and pacing logic (Topic 6).

### Random Isolated Loss vs Burst Loss

This distinction is critical and often overlooked:

- **Random isolated loss** (e.g., 1% uniform): generally tolerable. Interpolation bridges single-frame gaps. Reliable messages retransmit within one RTT. Players rarely notice.
- **Burst loss** (e.g., 5 consecutive packets dropped): devastating. Interpolation buffer drains completely. Multiple reliable messages need recovery simultaneously. Players see freezing, teleporting, or disconnection.

Burst-loss detection algorithm:

1. Track consecutive un-acked packets.
2. Track maximum burst length per window.
3. When burst length exceeds threshold, classify path as "burst-lossy" and activate conservative behavior.

### Distinguishing Loss from Late Delivery

A packet that arrives after its deadline is functionally lost for real-time purposes even though it technically "arrived." Your loss model should include late arrivals as effective losses for unreliable traffic.

---

## 4. Why Averages Hide Player-Visible Pain

This is one of the most common measurement failures in production systems.

### The Dashboard Problem

A monitoring dashboard showing "average RTT: 45ms, average loss: 0.8%" looks healthy. But if:

- p99 RTT is 350ms (once every ~100 packets, a massive delay spike)
- loss is concentrated in bursts of 3-5 packets every few seconds

Then players experience periodic freezing, rubber-banding, and hit-registration failures — while the dashboard stays green.

### What to Track Instead

| Metric               | What It Shows                  | Why It Matters             |
| -------------------- | ------------------------------ | -------------------------- |
| p50 RTT              | Typical experience             | Baseline feel              |
| p90 RTT              | Degraded-but-common experience | Correction frequency       |
| p99 RTT              | Worst-case tail events         | Teleport/freeze events     |
| p50 jitter           | Typical variation              | Buffer sizing baseline     |
| p99 jitter           | Worst-case variation           | Buffer underrun risk       |
| Mean loss %          | Long-term path character       | Capacity planning          |
| Max burst length     | Worst-case consecutive loss    | Freeze/disconnect risk     |
| Loss event frequency | How often bursts occur         | Session quality prediction |

### Percentile Computation in Practice

For real-time adaptation, you do not need exact percentiles. Approximate approaches:

- **Sorted window**: keep last N samples sorted; p99 ≈ sample at position 0.99×N. Works for small N.
- **Histogram buckets**: count samples in predefined ranges (0-10ms, 10-20ms, ...). Cheap and effective for dashboards.
- **Streaming quantile estimators**: algorithms like P² or t-digest that maintain approximate percentiles without storing all samples. Better for high-volume telemetry.

### Worked Example: Two Players, Same Average

Player A: stable 60ms RTT, 0.5% random loss, 1ms jitter.
Player B: average 40ms RTT, but p99 is 300ms, burst loss events every 8 seconds.

Player B has better average metrics but dramatically worse experience. Only tail metrics reveal this.

---

## 5. Instrumentation Basics: Sample Windows, Smoothing, and Confidence

Raw measurements are noisy. Feeding raw samples directly into adaptation logic causes oscillation. You need layered processing.

### Layer 1: Raw Samples

Every packet exchange produces raw timing data:

- send timestamp (monotonic clock)
- receive timestamp
- sequence number
- ACK feedback

Store these in a circular buffer for recent history. This is your ground truth.

### Layer 2: Rolling Windows

Aggregate raw samples into statistical windows:

- **per-interval stats**: mean, variance, min, max over last N samples or last T seconds
- **percentiles**: p50, p90, p99 from window
- **trend detection**: is RTT/jitter/loss rising, falling, or stable?

Window sizes should match your adaptation timescales:

- Fast adaptation (pacing, buffer adjustment): 16-64 samples
- Medium adaptation (send rate, tick decimation): 64-256 samples
- Slow adaptation (profile classification, alerting): 256-1024 samples

### Layer 3: Smoothed Estimates

For control-loop inputs (RTO calculation, adaptive buffer sizing), use smoothed estimators:

**Exponentially Weighted Moving Average (EWMA):**

$$\text{smoothed} = \alpha \cdot \text{new\_sample} + (1 - \alpha) \cdot \text{smoothed}$$

Common α values:

- RTT smoothing: α = 1/8 (matches TCP's SRTT approach from RFC 6298)
- Jitter smoothing: α = 1/16 (slower response, more stable)

**Smoothed RTT + Variance (TCP-style):**

$$\text{SRTT} = (1 - \alpha) \cdot \text{SRTT} + \alpha \cdot \text{RTT\_sample}$$
$$\text{RTTVAR} = (1 - \beta) \cdot \text{RTTVAR} + \beta \cdot |\text{SRTT} - \text{RTT\_sample}|$$

Where α = 1/8 and β = 1/4 are standard starting points.

### Layer 4: Confidence Checks

Do not make adaptation decisions from insufficient data:

- Require minimum sample count before trusting smoothed estimates (e.g., at least 8 RTT samples before first RTO computation).
- Flag "low confidence" states explicitly — new connections, post-reconnection, path change events.
- Distinguish "no data yet" from "data says things are fine."

### Clock Requirements

- **Always use monotonic clocks** (`steady_clock` in C++, `Stopwatch`/`Environment.TickCount64` in C#). Wall-clock time (`system_clock`, `DateTime.UtcNow`) can jump backwards during NTP adjustments.
- Record timestamps at packet boundaries (send/receive), not at processing time — delays in your own code should not pollute network measurements.
- Label measurement direction: uplink timing and downlink timing may behave differently.

---

## Practical Measurement Loop

Putting it all together into a per-tick measurement cycle:

```
every protocol tick:
    1. For each newly ACK'd packet:
         - compute RTT sample
         - feed into SRTT/RTTVAR estimator
         - feed into rolling window
    2. For each received packet:
         - compute inter-arrival delta
         - feed into jitter estimator
    3. For each expected-but-not-received packet past deadline:
         - increment loss counter
         - track burst length
    4. At window boundary (every N packets or T seconds):
         - compute percentiles from window
         - classify path quality tier (good / degraded / severe)
         - emit metrics for dashboards and adaptation inputs
    5. Feed path classification into:
         - interpolation buffer sizing (Topic 3)
         - retransmission timer (Topic 5)
         - pacing rate (Topic 6)
         - packet budget allocation (Topic 7)
```

---

## CSI vs GPR Lens

The measurement infrastructure is identical for both tracks. The difference is what you optimize:

### CSI-275 Perspective

- Emphasize statistical validity: do not adapt from small samples.
- Use formal smoothing (EWMA/variance) for control-loop stability.
- Track metrics per-client across sessions for capacity planning.
- Prioritize fairness: ensure one client's path problems do not degrade others.

### GPR-430 Perspective

- Emphasize player-visible outcomes: map metrics to correction frequency, buffer underruns, and perceived fairness.
- Tune thresholds by gameplay sensitivity: a fighting game cares about p99 RTT at 16ms granularity; a strategy game may tolerate 200ms p99.
- Measure input-to-visual-outcome latency end-to-end, not just network RTT.
- Track "bad experience episodes" as a metric, not just raw transport numbers.

---

## Common Measurement Anti-Patterns

1. **Treating throughput as responsiveness**: "we have plenty of bandwidth" does not mean "latency is acceptable."
2. **Tuning to average RTT only**: misses tail events that dominate player complaints.
3. **Triggering congestion logic from tiny sample sets**: a single high-RTT sample should not halve your send rate.
4. **Mixing clock domains**: using `system_clock` or wall time for RTT measurement introduces NTP-adjustment noise.
5. **Aggregating all traffic classes into one metric**: mixing reliable event timing with unreliable state timing produces uninterpretable numbers.
6. **Measuring only server-side**: client-side perception can differ dramatically due to last-mile behavior.
7. **No burst-loss tracking**: treating 1% random loss the same as 1% loss concentrated in bursts.
8. **Hardcoded thresholds without percentile context**: "if RTT > 100ms then degrade" misses that 100ms might be fine for one game genre and unplayable for another.

---

## Code Example (C++): Rolling RTT + Jitter + Loss Tracker

```cpp
#include <cstdint>
#include <cmath>
#include <deque>
#include <algorithm>
#include <numeric>

struct PathMetrics {
    // --- RTT ---
    std::deque<double> rttSamples;
    size_t rttCap = 128;
    double srtt = 0.0;
    double rttvar = 0.0;
    bool srttInitialized = false;

    void addRttSample(double rttMs) {
        rttSamples.push_back(rttMs);
        if (rttSamples.size() > rttCap) rttSamples.pop_front();

        if (!srttInitialized) {
            srtt = rttMs;
            rttvar = rttMs / 2.0;
            srttInitialized = true;
        } else {
            // RFC 6298 style
            rttvar = 0.75 * rttvar + 0.25 * std::abs(srtt - rttMs);
            srtt   = 0.875 * srtt + 0.125 * rttMs;
        }
    }

    double rttP50() const { return percentile(rttSamples, 0.50); }
    double rttP90() const { return percentile(rttSamples, 0.90); }
    double rttP99() const { return percentile(rttSamples, 0.99); }

    // --- Jitter ---
    double lastArrivalMs = 0.0;
    double lastSendMs = 0.0;
    std::deque<double> jitterSamples;
    size_t jitterCap = 128;

    void addArrivalSample(double sendMs, double recvMs) {
        if (lastArrivalMs > 0.0) {
            double sendDelta = sendMs - lastSendMs;
            double recvDelta = recvMs - lastArrivalMs;
            double j = std::abs(recvDelta - sendDelta);
            jitterSamples.push_back(j);
            if (jitterSamples.size() > jitterCap) jitterSamples.pop_front();
        }
        lastSendMs = sendMs;
        lastArrivalMs = recvMs;
    }

    double jitterP99() const { return percentile(jitterSamples, 0.99); }

    // --- Loss ---
    uint64_t packetsExpected = 0;
    uint64_t packetsReceived = 0;
    int consecutiveLoss = 0;
    int maxBurstLength = 0;

    void onPacketReceived() {
        packetsExpected++;
        packetsReceived++;
        consecutiveLoss = 0;
    }

    void onPacketLost() {
        packetsExpected++;
        consecutiveLoss++;
        if (consecutiveLoss > maxBurstLength)
            maxBurstLength = consecutiveLoss;
    }

    double lossRate() const {
        if (packetsExpected == 0) return 0.0;
        return 1.0 - static_cast<double>(packetsReceived) / packetsExpected;
    }

private:
    static double percentile(const std::deque<double>& samples, double p) {
        if (samples.empty()) return 0.0;
        std::vector<double> sorted(samples.begin(), samples.end());
        std::sort(sorted.begin(), sorted.end());
        size_t idx = static_cast<size_t>(p * (sorted.size() - 1));
        return sorted[idx];
    }
};
```

## Code Example (C#): Comprehensive Measurement State

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

public class PathMetrics
{
    // --- RTT ---
    private readonly Queue<double> _rttSamples = new();
    private const int RttCap = 128;
    public double Srtt { get; private set; }
    public double RttVar { get; private set; }
    private bool _srttInit;

    public void AddRttSample(double rttMs)
    {
        _rttSamples.Enqueue(rttMs);
        while (_rttSamples.Count > RttCap) _rttSamples.Dequeue();

        if (!_srttInit)
        {
            Srtt = rttMs;
            RttVar = rttMs / 2.0;
            _srttInit = true;
        }
        else
        {
            RttVar = 0.75 * RttVar + 0.25 * Math.Abs(Srtt - rttMs);
            Srtt   = 0.875 * Srtt + 0.125 * rttMs;
        }
    }

    public double RttP99 => Percentile(_rttSamples, 0.99);

    // --- Jitter ---
    private readonly Queue<double> _jitterSamples = new();
    private const int JitterCap = 128;
    private double _lastSendMs, _lastRecvMs;
    private bool _jitterInit;

    public void AddArrival(double sendMs, double recvMs)
    {
        if (_jitterInit)
        {
            double j = Math.Abs((recvMs - _lastRecvMs) - (sendMs - _lastSendMs));
            _jitterSamples.Enqueue(j);
            while (_jitterSamples.Count > JitterCap) _jitterSamples.Dequeue();
        }
        _lastSendMs = sendMs;
        _lastRecvMs = recvMs;
        _jitterInit = true;
    }

    public double JitterP99 => Percentile(_jitterSamples, 0.99);

    // --- Loss ---
    public long PacketsExpected { get; private set; }
    public long PacketsReceived { get; private set; }
    public int ConsecutiveLoss { get; private set; }
    public int MaxBurstLength { get; private set; }

    public void OnReceived() { PacketsExpected++; PacketsReceived++; ConsecutiveLoss = 0; }
    public void OnLost()
    {
        PacketsExpected++;
        ConsecutiveLoss++;
        MaxBurstLength = Math.Max(MaxBurstLength, ConsecutiveLoss);
    }

    public double LossRate => PacketsExpected == 0 ? 0 : 1.0 - (double)PacketsReceived / PacketsExpected;

    // --- Helpers ---
    private static double Percentile(IEnumerable<double> source, double p)
    {
        var sorted = source.OrderBy(x => x).ToArray();
        if (sorted.Length == 0) return 0;
        int idx = (int)(p * (sorted.Length - 1));
        return sorted[idx];
    }
}
```

