# Interpolation, Jitter Buffers, and Player-Visible Smoothness

Clients receive state updates at irregular intervals over a lossy, jittery network. The player sees a screen refreshing at 60+ fps. Bridging these two realities — bursty network data in, smooth visual motion out — is the job of the **interpolation buffer** (also called a jitter buffer or playout buffer).


---

## 1. Interpolation Buffers as Jitter Absorbers

### The Core Problem

Network packets do not arrive at perfectly regular intervals. A server sending snapshots every 50ms (20 Hz) might produce arrivals like:

| Snapshot | Send Time | Arrival Time | Inter-Arrival Gap |
| -------- | --------- | ------------ | ----------------- |
| S1       | 0ms       | 22ms         | —                 |
| S2       | 50ms      | 74ms         | 52ms              |
| S3       | 100ms     | 118ms        | 44ms              |
| S4       | 150ms     | 178ms        | 60ms              |
| S5       | 200ms     | 221ms        | 43ms              |
| S6       | 250ms     | 289ms        | 68ms              |

The inter-arrival gaps vary from 43ms to 68ms despite the server sending at exactly 50ms intervals. If the client renders each snapshot immediately upon arrival, entity motion will stutter visibly — fast, slow, fast, slow — even though the server simulation is perfectly smooth.

### How the Buffer Fixes This

The interpolation buffer introduces deliberate delay. Instead of rendering the most recent snapshot immediately, the client renders a point in time that is **behind** the latest received data:

```
Real time (what server computed):  ──────────────────────>
Render time (what player sees):    ──────────────>
                                   ← buffer delay →
```

This delay creates a window of buffered snapshots. The client always has at least one confirmed snapshot ahead of its render point, so it can interpolate between known states rather than guessing.

### Buffer Mechanics

At each render frame, the client:

1. Determines its **render timestamp**: `t_render = t_now - buffer_delay`
2. Finds the two snapshots that bracket `t_render`: snapshot A (before) and snapshot B (after)
3. Computes the interpolation fraction: `alpha = (t_render - t_A) / (t_B - t_A)`
4. Lerps (or otherwise blends) between A and B for each entity

If the buffer is large enough to absorb the jitter, there will always be a valid A-B pair available. If the buffer is too small, the client runs out of data — a **buffer underrun**.

### Buffer Underrun vs Overrun

- **Underrun**: the render timestamp has caught up to or passed the latest received snapshot. The client has no future data to interpolate toward. It must extrapolate (guess) or freeze. Both are visible artifacts.
- **Overrun**: the buffer accumulates too many snapshots. The render timestamp falls further and further behind real time. The player sees increasingly stale data. Input-to-response latency grows.

A well-tuned buffer avoids both extremes.

### Adaptive Buffer Sizing

Static buffer sizes are fragile. A fixed 100ms buffer wastes latency on a clean connection and still underruns on a severely jittery one.

Adaptive approaches measure jitter in real time and adjust:

$$T_{\text{buffer}} = T_{\text{send}} + k \cdot J_{\text{measured}}$$

Where:

- $T_{\text{send}}$ is the expected send interval
- $J_{\text{measured}}$ is a running estimate of jitter (e.g., EWMA or percentile)
- $k$ is a safety multiplier (typically 1.5-3.0)

Higher $k$ reduces underrun risk but increases latency. Lower $k$ is more responsive but more fragile.

### Worked Example: Adaptive Buffer

Given:

- Send interval: 50ms (20 Hz)
- Measured p90 jitter: 12ms
- Safety multiplier k = 2.0

$$T_{\text{buffer}} = 50 + 2.0 \times 12 = 74\text{ms}$$

The client renders 74ms behind real time. Under normal jitter, this provides enough margin. During a jitter spike to 30ms, the client may briefly underrun but recovers quickly.

---

## 2. The Delay-for-Smoothness Tradeoff

### More Delay = Smoother Motion

Every millisecond added to the interpolation buffer is one more millisecond of insurance against jitter. A 200ms buffer can absorb dramatic network instability and still render buttery-smooth motion.

But the player pays for this in **responsiveness**. Their inputs are processed by the server and reflected back through a pipeline that already includes network RTT, server processing, and render delay. The interpolation buffer adds directly to this chain.

### Responsiveness Budget

Total input-to-visual latency:

$$T_{\text{total}} = T_{\text{input}} + T_{\text{upload}} + T_{\text{server}} + T_{\text{download}} + T_{\text{buffer}} + T_{\text{render}}$$

For a typical setup:

| Component             | Time     |
| --------------------- | -------- |
| Input sampling        | 8ms      |
| Upload                | 25ms     |
| Server tick (avg)     | 25ms     |
| Download              | 25ms     |
| Interp buffer         | **?**    |
| Render pipeline       | 16ms     |
| **Total (no buffer)** | **99ms** |

Adding a 75ms buffer pushes total latency to 174ms — noticeable in competitive play. Adding a 150ms buffer reaches 249ms — uncomfortable for action games. Adding a 30ms buffer holds latency at 129ms but risks underruns on jittery connections.

### Genre-Dependent Tradeoffs

| Genre                  | Acceptable Extra Delay | Smoothness Requirement                          |
| ---------------------- | ---------------------- | ----------------------------------------------- |
| Competitive FPS        | 15-30ms                | Moderate (corrections acceptable)               |
| Action RPG             | 50-80ms                | High (smooth world, less input-sensitive)       |
| Strategy/MOBA          | 80-120ms               | Very high (smooth units, click-based input)     |
| MMO with many entities | 100-200ms              | Critical (hundreds of smoothly moving entities) |

### Player Perception Thresholds

Research and industry experience suggest:

- < 100ms total latency: feels "instant" for most players
- 100-150ms: noticeable but acceptable for most genres
- 150-250ms: clearly delayed, acceptable only for slow-paced games
- > 250ms: frustrating for almost any interactive gameplay

The interpolation buffer must be sized to stay within the genre's acceptable latency budget while providing adequate smoothness.

---

## 3. Handling Packet Clumping and Occasional Loss

### Packet Clumping

Network queues, WiFi contention, and OS scheduling can cause packets to arrive in bursts. Three snapshots arrive within 5ms, then nothing for 140ms. If the client naively stamps arrival times, its interpolation will speed up during bursts and stall during gaps.

**Mitigation**: use **server-stamped send times** (or sequence-derived times) as the interpolation timeline, not client arrival times. The client interpolates along the server's timeline regardless of when packets physically arrived.

### Single Packet Loss

When one snapshot is lost:

```
S1 ─── S2 ─── [S3 lost] ─── S4 ─── S5
```

The client has S2 and S4 but no S3. It can:

1. **Interpolate over the gap**: treat S2→S4 as a single longer interpolation span. Motion is smooth but the interpolation covers 100ms instead of 50ms, potentially at different velocity.
2. **Extrapolate from S2 until S4 arrives**: use S2's velocity to predict S3's state. Risky if the entity changed direction.
3. **Hold S2 and snap to S4**: the simplest approach but produces a visible hitch.

Option 1 is usually best: the motion stays smooth and bounded by known states. The client should track that it's interpolating over a wider interval and potentially adjust alpha computation accordingly.

### Burst Loss (Multiple Consecutive Packets)

Losing 2-3 consecutive snapshots (150ms gap at 20 Hz) exhausts the buffer. The client must extrapolate or freeze. Recovery strategies:

- **Continue extrapolating** with velocity damping — progressively slow down extrapolated motion so entities don't fly off in wrong directions.
- **Freeze at last known state** — visually jarring but safe.
- **Blend to freeze** — smoothly decelerate to a stop over 50-100ms, then hold until new data arrives.

After new data arrives, **don't snap** to the new position. Instead, blend from the current (extrapolated/frozen) position to the new interpolation target over 100-200ms. This hides the correction.

### Adaptive Response to Loss Rate

When the running loss rate increases, the buffer should proactively widen:

```
if (loss_rate > loss_threshold):
    buffer_target = send_interval + 3 * jitter_estimate  # wider margin
else:
    buffer_target = send_interval + 1.5 * jitter_estimate  # normal margin
```

This pre-emptive widening reduces the probability of underruns during degraded periods.

---

## 4. Interpolation vs Extrapolation Failure Modes

### Interpolation: Safe but Delayed

Interpolation works between two known states. It is guaranteed to produce a position that lies on the segment between A and B (for linear interpolation) or on the curve through A and B (for higher-order interpolation).

**Failure modes**:

- Abrupt direction changes between A and B are not captured by linear interpolation — the entity takes a straight-line shortcut.
- If A and B have very different states (e.g., entity teleported), linear interpolation produces motion through empty space.

**Mitigations**:

- Use Hermite or cubic interpolation when velocity data is available — this curves through A and B respecting their velocities.
- Detect teleports (distance > threshold) and snap instead of interpolating.
- For rotational data, use slerp (spherical linear interpolation) instead of lerp to avoid gimbal artifacts.

### Extrapolation: Responsive but Risky

Extrapolation predicts future state from current state plus derivatives (velocity, acceleration). It is **not bounded** by known data — it can diverge arbitrarily from reality.

**When extrapolation works**:

- Entity is moving in a straight line at constant velocity (projectiles, vehicles on highways).
- The extrapolation window is short (< 1 send interval).
- Velocity is reliable and not about to change.

**When extrapolation fails**:

- Entity is controlled by another player (unpredictable inputs).
- Entity is near a collision boundary (wall, floor, obstacle).
- Entity has high angular velocity or is turning.
- The extrapolation window is long (missed multiple snapshots).

### The Correction Problem

When a real snapshot arrives after extrapolation, the entity's extrapolated position is almost certainly wrong. The correction from extrapolated → authoritative position is:

$$\Delta = P_{\text{authority}} - P_{\text{extrapolated}}$$

Large $\Delta$ is visible as a "snap" or "rubber band." The longer the extrapolation ran, the larger $\Delta$ likely is.

### Blended Correction

Instead of snapping, blend the correction over time:

```
P_visual = P_authority + correction_offset * decay
```

Where `correction_offset` starts at $\Delta$ and decays toward zero over a blend period (100-300ms). The entity visually glides to the correct position rather than teleporting.

### Worked Example: Correction Magnitudes

Entity moving at 5 m/s. Server sends at 20 Hz (50ms intervals).

| Missed Snapshots | Extrapolation Time | Max Position Error (straight line) | Max Error (90° turn at miss) |
| ---------------- | ------------------ | ---------------------------------- | ---------------------------- |
| 0                | 0ms                | 0cm                                | 0cm                          |
| 1                | 50ms               | ~0cm (just interpolating)          | ~25cm                        |
| 2                | 100ms              | ~7cm (velocity drift)              | ~50cm                        |
| 3                | 150ms              | ~15cm                              | ~75cm                        |

At 75cm of correction for a 3-snapshot loss with a direction change, the snap is easily visible. This is why extrapolation duration must be minimized and corrections must be smoothed.

---

## 5. Choosing Interpolation Window from Send Rate and Loss Environment

### Baseline: Two-Snapshot Buffer

The minimum viable interpolation buffer holds two snapshots — the pair being interpolated between. This means the buffer delay equals one send interval:

$$T_{\text{buffer, min}} = T_{\text{send}}$$

At 20 Hz: 50ms. At 60 Hz: 16.7ms. This provides zero jitter margin — any late packet causes an underrun.

### Practical Minimum: Two Intervals Plus Jitter

$$T_{\text{buffer}} = 2 \times T_{\text{send}} + J_{p90}$$

This ensures the client has one complete snapshot pair for interpolation plus margin for jitter. At 20 Hz with 15ms p90 jitter: $2 \times 50 + 15 = 115$ms.

### Aggressive (Competitive): One Interval Plus Small Margin

$$T_{\text{buffer}} = T_{\text{send}} + J_{p75}$$

Accepts occasional underruns in exchange for lower latency. At 60 Hz with 5ms p75 jitter: $16.7 + 5 = 21.7$ms. Underruns happen roughly 25% of the time at jitter boundaries — smoothing handles these with brief extrapolation.

### Conservative (MMO/Strategy): Three Intervals Plus Jitter

$$T_{\text{buffer}} = 3 \times T_{\text{send}} + J_{p95}$$

Prioritizes smoothness over responsiveness. Many entities, slow-paced gameplay, player tolerance for delay. At 10 Hz with 30ms p95 jitter: $3 \times 100 + 30 = 330$ms.

### Decision Matrix

| Send Rate | Network Quality       | Suggested Buffer Formula | Typical Buffer |
| --------- | --------------------- | ------------------------ | -------------- |
| 60 Hz     | Good (< 5ms jitter)   | $T_s + J_{p75}$          | 20-25ms        |
| 60 Hz     | Fair (10-20ms jitter) | $1.5T_s + J_{p90}$       | 35-45ms        |
| 20 Hz     | Good                  | $2T_s + J_{p90}$         | 110-120ms      |
| 20 Hz     | Poor (> 25ms jitter)  | $2.5T_s + J_{p95}$       | 150-170ms      |
| 10 Hz     | Any                   | $3T_s + J_{p95}$         | 330-400ms      |

### Adapting at Runtime

The buffer should not be static. Measure jitter continuously and adjust:

1. Every N seconds (e.g., every 2s), recompute $J_{p90}$ from recent samples.
2. Compute new target buffer size using the appropriate formula.
3. Move toward the target gradually (e.g., adjust by 1-2ms per frame).
4. Never shrink faster than you grow — hysteresis prevents oscillation.

Growing the buffer is safe (just adds delay). Shrinking risks underrun, so shrink slowly and only after sustained improvement.

---

## 6. CSI vs GPR Perspectives on Interpolation

### CSI Perspective

From a CSI viewpoint, the interpolation buffer is a **low-pass filter** in a discrete control system:

- It removes high-frequency jitter from the observation signal.
- It introduces phase delay (group delay) proportional to buffer size.
- The filter cutoff frequency is approximately $1 / (2 \times T_{\text{buffer}})$.
- Stability analysis must account for this additional delay in the control loop.

The CSI practitioner sizes the buffer analytically: measure the jitter spectrum, choose a filter that rejects >90% of jitter energy while keeping phase delay below the system's stability margin.

### GPR Perspective

From a GPR viewpoint, the interpolation buffer is a **smoothness-vs-responsiveness dial**:

- Players feel interpolation delay as sluggishness in other entities' motion.
- Under-buffered entities visibly stutter, breaking immersion.
- Over-buffered entities feel like they're moving through molasses.
- The "right" buffer size is the one where players stop noticing artifacts without complaining about delay.

The GPR practitioner sizes the buffer by playtesting: try different values, observe when players report "laggy" vs "stuttery" motion, and find the sweet spot.

### Reconciling Both Views

In practice, use CSI analysis to establish a reasonable range, then GPR playtesting to fine-tune within that range. The analytical approach prevents gross errors; the experiential approach catches subjective factors that analysis misses.

---

## 7. Implementation Patterns

### Anti-Patterns

1. **Using arrival time as interpolation timeline**: produces stuttery motion that follows network jitter instead of smooth server time.
2. **Fixed buffer with no adaptation**: works on the developer's LAN, fails on real player connections.
3. **Snapping on every correction**: makes the game look broken even under mild jitter.
4. **Extrapolating indefinitely**: entities fly through walls and across maps during packet loss.
5. **Same buffer for all entity types**: a distant background NPC does not need the same latency budget as the player's opponent.
6. **Resizing buffer based on instantaneous jitter**: one spike should not immediately resize the buffer — use windowed statistics.

### Practical Checklist

- [ ] Use server-stamped timestamps for interpolation timeline.
- [ ] Size buffer from send interval + measured jitter, not from tick rate.
- [ ] Implement adaptive buffer sizing with hysteresis.
- [ ] Smooth corrections over 100-300ms instead of snapping.
- [ ] Cap extrapolation duration (e.g., 2× send interval max).
- [ ] Dampen extrapolation velocity for player-controlled entities.
- [ ] Detect teleport events and skip interpolation for them.
- [ ] Use slerp for rotational interpolation.
- [ ] Monitor underrun rate and log it for diagnostics.

---

## Code Example (C++): Interpolation Buffer

```cpp
#include <deque>
#include <cstdint>
#include <algorithm>
#include <cmath>

struct EntitySnapshot {
    float x, y, z;
    float vx, vy, vz;
    double serverTime;
};

struct InterpolationBuffer {
    std::deque<EntitySnapshot> buffer;
    double bufferDelayMs = 100.0;
    double jitterEstimate = 0.0;
    double sendInterval = 50.0;  // 20 Hz
    static constexpr double JitterAlpha = 0.1;
    static constexpr double JitterSafetyK = 2.0;
    static constexpr double MaxExtrapolateMs = 100.0;

    void addSnapshot(const EntitySnapshot& snap, double arrivalTime) {
        // Update jitter estimate from inter-arrival variance
        if (!buffer.empty()) {
            double expectedGap = sendInterval;
            double actualGap = arrivalTime - lastArrivalTime;
            double deviation = std::abs(actualGap - expectedGap);
            jitterEstimate += JitterAlpha * (deviation - jitterEstimate);

            // Adapt buffer delay
            double target = sendInterval + JitterSafetyK * jitterEstimate;
            target = std::max(target, sendInterval);
            // Grow fast, shrink slow (hysteresis)
            if (target > bufferDelayMs)
                bufferDelayMs += 0.3 * (target - bufferDelayMs);
            else
                bufferDelayMs += 0.05 * (target - bufferDelayMs);
        }
        lastArrivalTime = arrivalTime;
        buffer.push_back(snap);

        // Prune old snapshots (keep last 10)
        while (buffer.size() > 10)
            buffer.pop_front();
    }

    struct InterpolatedState {
        float x, y, z;
        bool extrapolated;
    };

    InterpolatedState sample(double currentServerTime) const {
        double renderTime = currentServerTime - bufferDelayMs;

        // Find bracketing snapshots
        const EntitySnapshot* before = nullptr;
        const EntitySnapshot* after = nullptr;
        for (size_t i = 0; i + 1 < buffer.size(); i++) {
            if (buffer[i].serverTime <= renderTime &&
                buffer[i + 1].serverTime >= renderTime) {
                before = &buffer[i];
                after = &buffer[i + 1];
                break;
            }
        }

        if (before && after) {
            // Normal interpolation
            double span = after->serverTime - before->serverTime;
            double alpha = (span > 0)
                ? (renderTime - before->serverTime) / span
                : 0.0;
            alpha = std::clamp(alpha, 0.0, 1.0);
            return {
                static_cast<float>(before->x + alpha * (after->x - before->x)),
                static_cast<float>(before->y + alpha * (after->y - before->y)),
                static_cast<float>(before->z + alpha * (after->z - before->z)),
                false
            };
        }

        // Extrapolation fallback: use last snapshot + velocity
        if (!buffer.empty()) {
            auto& last = buffer.back();
            double dt = renderTime - last.serverTime;
            dt = std::min(dt, MaxExtrapolateMs); // cap extrapolation
            double dtSec = dt / 1000.0;
            // Dampen velocity to reduce divergence
            double damping = std::max(0.0, 1.0 - dt / (MaxExtrapolateMs * 2));
            return {
                static_cast<float>(last.x + last.vx * dtSec * damping),
                static_cast<float>(last.y + last.vy * dtSec * damping),
                static_cast<float>(last.z + last.vz * dtSec * damping),
                true
            };
        }

        return {0, 0, 0, true};
    }

private:
    double lastArrivalTime = 0;
};
```

## Code Example (C#): Interpolation Buffer with Correction Smoothing

```csharp
using System;
using System.Collections.Generic;

public struct EntitySnapshot
{
    public float X, Y, Z;
    public float Vx, Vy, Vz;
    public double ServerTime;
}

public class InterpolationBuffer
{
    private readonly List<EntitySnapshot> _buffer = new();
    private double _bufferDelayMs = 100.0;
    private double _jitterEstimate;
    private double _lastArrivalTime;

    // Correction smoothing state
    private float _correctionX, _correctionY, _correctionZ;
    private const double CorrectionDecayRate = 0.92; // per frame

    public double SendIntervalMs { get; set; } = 50.0;
    public double JitterSafetyK { get; set; } = 2.0;
    public double MaxExtrapolateMs { get; set; } = 100.0;

    public double CurrentBufferDelay => _bufferDelayMs;
    public double CurrentJitter => _jitterEstimate;

    public void AddSnapshot(EntitySnapshot snap, double arrivalTime)
    {
        if (_buffer.Count > 0)
        {
            double expectedGap = SendIntervalMs;
            double actualGap = arrivalTime - _lastArrivalTime;
            double deviation = Math.Abs(actualGap - expectedGap);
            _jitterEstimate += 0.1 * (deviation - _jitterEstimate);

            double target = SendIntervalMs + JitterSafetyK * _jitterEstimate;
            target = Math.Max(target, SendIntervalMs);

            if (target > _bufferDelayMs)
                _bufferDelayMs += 0.3 * (target - _bufferDelayMs);
            else
                _bufferDelayMs += 0.05 * (target - _bufferDelayMs);
        }

        _lastArrivalTime = arrivalTime;
        _buffer.Add(snap);

        // Keep last 10 snapshots
        while (_buffer.Count > 10)
            _buffer.RemoveAt(0);
    }

    public (float X, float Y, float Z, bool Extrapolated) Sample(
        double currentServerTime)
    {
        double renderTime = currentServerTime - _bufferDelayMs;

        // Find bracketing pair
        EntitySnapshot? before = null, after = null;
        for (int i = 0; i + 1 < _buffer.Count; i++)
        {
            if (_buffer[i].ServerTime <= renderTime &&
                _buffer[i + 1].ServerTime >= renderTime)
            {
                before = _buffer[i];
                after = _buffer[i + 1];
                break;
            }
        }

        float rawX, rawY, rawZ;
        bool extrapolated;

        if (before.HasValue && after.HasValue)
        {
            var a = before.Value;
            var b = after.Value;
            double span = b.ServerTime - a.ServerTime;
            double alpha = span > 0
                ? Math.Clamp((renderTime - a.ServerTime) / span, 0, 1)
                : 0;

            rawX = (float)(a.X + alpha * (b.X - a.X));
            rawY = (float)(a.Y + alpha * (b.Y - a.Y));
            rawZ = (float)(a.Z + alpha * (b.Z - a.Z));
            extrapolated = false;
        }
        else if (_buffer.Count > 0)
        {
            var last = _buffer[^1];
            double dt = Math.Min(renderTime - last.ServerTime, MaxExtrapolateMs);
            double dtSec = dt / 1000.0;
            double damping = Math.Max(0, 1.0 - dt / (MaxExtrapolateMs * 2));

            rawX = (float)(last.X + last.Vx * dtSec * damping);
            rawY = (float)(last.Y + last.Vy * dtSec * damping);
            rawZ = (float)(last.Z + last.Vz * dtSec * damping);
            extrapolated = true;
        }
        else
        {
            return (0, 0, 0, true);
        }

        // Apply and decay correction offset
        float finalX = rawX + _correctionX;
        float finalY = rawY + _correctionY;
        float finalZ = rawZ + _correctionZ;

        _correctionX *= (float)CorrectionDecayRate;
        _correctionY *= (float)CorrectionDecayRate;
        _correctionZ *= (float)CorrectionDecayRate;

        return (finalX, finalY, finalZ, extrapolated);
    }

    /// <summary>
    /// When authority corrects the position, add a visual offset
    /// that decays to zero over several frames.
    /// </summary>
    public void ApplyCorrection(float dx, float dy, float dz)
    {
        _correctionX += dx;
        _correctionY += dy;
        _correctionZ += dz;
    }
}
```

