# Packet Budgets, Prioritization, and Degradation Strategy

Every tick, the server has a finite number of bytes it can send to each client. This is the **packet budget** — a hard constraint imposed by bandwidth, packet rate limits, and congestion state. When the game wants to send more data than the budget allows, something must be deferred or dropped. The question is: what?

---

## 1. Per-Packet Budget Planning

### Defining the Budget

The packet budget is the maximum payload bytes per tick per client. It comes from external constraints:

$$B_{\text{tick}} = \frac{B_{\text{max\_bps}}}{R_{\text{send\_hz}}} - H_{\text{overhead}}$$

Where:

- $B_{\text{max\_bps}}$ = maximum bytes per second (from bandwidth cap or congestion window)
- $R_{\text{send\_hz}}$ = send rate in Hz
- $H_{\text{overhead}}$ = per-packet overhead (IP header: 20B, UDP header: 8B, protocol header: 8-12B ≈ 36-40B total)

### Worked Example

Client bandwidth cap: 50 KB/s (50,000 B/s). Send rate: 20 Hz. Overhead: 40 bytes per packet.

$$B_{\text{tick}} = \frac{50{,}000}{20} - 40 = 2{,}500 - 40 = 2{,}460 \text{ bytes}$$

Each tick, you have 2,460 bytes of payload to work with. This must contain:

- ACK header and reliability data
- Reliable message retransmissions (if any)
- Fresh state data (positions, velocities, animations)
- Events and low-priority data

### Budget Is Not Constant

The budget changes when:

- Congestion controller reduces send rate (lower Hz → could mean larger packets, but usually means less total data).
- Path quality changes (loss increases → retransmissions consume budget).
- Congestion controller reduces bandwidth cap.

The budget system must recompute available bytes every tick, not assume a fixed number.

### MTU Considerations

The Maximum Transmission Unit (MTU) limits individual packet size. Standard internet MTU is 1500 bytes (Ethernet). After IP (20B) and UDP (8B) headers: 1472 bytes of UDP payload.

If the budget per tick is 2,460 bytes but MTU limits packets to ~1400 bytes of payload (accounting for protocol headers), you need 2 packets per tick. This is fine but doubles the packet-rate pressure.

**Practical guideline**: aim for 1 packet per tick per client. Keep payloads under ~1200 bytes to avoid fragmentation on paths with smaller MTUs (many paths have < 1500B effective MTU due to tunneling, VPN, etc.).

If more data is needed, either:

- Increase send rate slightly (more packets, smaller each).
- Use compression to fit within one packet.
- Defer low-priority data to the next tick.

### Budget Accounting

Track budget consumption explicitly:

```
budget_remaining = budget_per_tick

// Mandatory overhead
budget_remaining -= protocol_header_size
budget_remaining -= ack_data_size

// Reliable retransmissions (highest priority)
for msg in retransmission_queue:
    if budget_remaining >= msg.size:
        pack(msg)
        budget_remaining -= msg.size

// Fresh reliable events
for msg in reliable_event_queue:
    if budget_remaining >= msg.size:
        pack(msg)
        budget_remaining -= msg.size

// State updates (fill remaining budget)
state_data = buildStateDelta(budget_remaining)
pack(state_data)
```

The order matters: mandatory overhead first, retransmissions second, fresh events third, state data fills the rest.

---

## 2. Priority Classes

### Why Prioritize?

Not all game data has equal importance. Some data is mandatory for correct gameplay; some is cosmetic. When the budget is tight, the protocol should sacrifice cosmetic data before touching gameplay-critical data.

### Defining Priority Classes

A practical priority hierarchy for a typical multiplayer game:

| Priority    | Class                  | Examples                                       | Budget Behavior                            |
| ----------- | ---------------------- | ---------------------------------------------- | ------------------------------------------ |
| 0 (highest) | Protocol               | ACKs, keepalives, connection control           | Always sent, never deferred                |
| 1           | Critical input/events  | Player inputs, weapon fire, damage, spawns     | Reliable, retransmitted, sent immediately  |
| 2           | Authority corrections  | Position corrections, state reconciliation     | Sent every tick if budget allows           |
| 3           | High-frequency state   | Nearby entity positions, velocities            | Sent most ticks, can skip occasionally     |
| 4           | Medium-frequency state | Distant entities, animation state, health bars | Sent every 2-4 ticks, defer under pressure |
| 5           | Low-frequency state    | Score, team state, world events                | Sent every 5-10 ticks, defer freely        |
| 6 (lowest)  | Telemetry/debug        | Performance metrics, diagnostic data           | Only sent when budget allows               |

### Relevance-Based Priority

Within a priority class, individual items can be further ranked by **relevance** to the receiving client:

- **Distance**: entities closer to the player are higher priority than distant ones.
- **Visibility**: entities in the player's field of view are higher priority.
- **Interaction probability**: entities the player is targeting or can interact with are higher priority.
- **Rate of change**: fast-moving entities need more frequent updates than stationary ones.

This creates a two-dimensional priority: class (fundamental importance) × relevance (contextual importance).

### Worked Example: Priority Budget Allocation

Budget: 1,200 bytes. 20 nearby entities, 50 distant entities.

| Class                 | Bytes Needed      | Bytes Allocated | Notes               |
| --------------------- | ----------------- | --------------- | ------------------- |
| Protocol headers      | 40                | 40              | Mandatory           |
| Retransmissions       | 120 (2 msgs)      | 120             | High priority       |
| Critical events       | 60 (1 fire event) | 60              | Must send           |
| Authority corrections | 150               | 150             | Top 3 corrections   |
| Nearby entities (20)  | 20 × 40 = 800     | 600             | Top 15 entities fit |
| Distant entities (50) | 50 × 20 = 1000    | 230             | Top 11 entities fit |
| Telemetry             | 80                | 0               | Deferred            |
| **Total**             | **2,250**         | **1,200**       | Budget respected    |

5 nearby entities and 39 distant entities are deferred to the next tick. All critical data was sent.

---

## 3. Priority Accumulation and Starvation Prevention

### The Starvation Problem

If high-priority data always consumes the full budget, low-priority data never gets sent. A constantly-moving nearby entity generates enough position updates to fill every tick's budget, and distant entity positions, score updates, or telemetry are perpetually deferred.

This is **starvation**: lower-priority data accumulates indefinitely.

### Priority Accumulation (Aging)

Each deferred update accumulates priority over time. The longer it waits, the higher its effective priority becomes:

$$P_{\text{effective}} = P_{\text{base}} + k \times t_{\text{deferred}}$$

Where:

- $P_{\text{base}}$ is the item's static priority class
- $k$ is an accumulation rate (priority units per millisecond)
- $t_{\text{deferred}}$ is how long the item has been waiting

After enough time, a low-priority item's effective priority surpasses newer high-priority items, forcing it into the next packet.

### Reservation Strategy

An alternative to accumulation: reserve a portion of the budget for each priority class:

| Class                 | Minimum Reservation | Maximum Usage |
| --------------------- | ------------------- | ------------- |
| Protocol/Critical     | 200 bytes           | Unlimited     |
| Authority corrections | 200 bytes           | 500 bytes     |
| Nearby entities       | 400 bytes           | 800 bytes     |
| Distant entities      | 100 bytes           | 400 bytes     |
| Low-priority          | 50 bytes            | 200 bytes     |

Each class is guaranteed its minimum reservation. Unused budget flows to the next class. This prevents starvation while maintaining priority ordering.

### Bandwidth Sharing Over Time

Another anti-starvation approach: ensure every entity gets at least one update every N ticks:

- Nearby entities: at least once per tick (every 50ms at 20 Hz).
- Medium-distance entities: at least once per 4 ticks (200ms).
- Distant entities: at least once per 10 ticks (500ms).
- World state: at least once per 20 ticks (1000ms).

If an entity's last update was N ticks ago, it gets a priority boost proportional to its staleness. Fresh data for nearby entities gets high priority; a distant entity that hasn't been updated in 500ms also gets high priority because it's overdue.

### Worked Example: Priority Accumulation

Entity A (nearby, base priority 100) was sent last tick. Entity B (distant, base priority 30) was last sent 8 ticks ago. Accumulation rate k = 5 per tick.

$$P_A = 100 + 5 \times 1 = 105$$
$$P_B = 30 + 5 \times 8 = 70$$

Entity A still wins. But after 15+ ticks of deferral:

$$P_B = 30 + 5 \times 15 = 105$$

Entity B catches up and gets sent. The distant entity is never starved for more than ~15 ticks (750ms at 20 Hz).

---

## 4. Delta Encoding, Quantization, and Compression as Budget Multipliers

### Why Size Matters

If you can represent the same data in half the bytes, you can fit twice as many entities in the same budget. State compression techniques are direct multipliers on effective budget capacity.

### Delta Encoding

Instead of sending absolute state, send the difference from the last acknowledged state:

**Absolute**: `position = (1523.45, 267.89, -45.12)` → 12 bytes (3 × float32)
**Delta**: `delta = (+0.23, -0.05, +0.01)` → potentially 3-6 bytes (smaller values need fewer bits)

Delta encoding works because:

- Most entities change slowly between ticks.
- Small deltas can be represented with fewer bits.
- The baseline (last acknowledged state) is known to both sides.

**Requirement**: the sender must track what each client has acknowledged (the baseline). This is the "delta base" and couples delta encoding with the ACK system.

### Delta Encoding and Packet Loss

If the delta base refers to a state the client never received, the delta is meaningless. The sender must:

1. Only compute deltas against the client's last **ACK'd** baseline.
2. If no recent baseline exists (sustained loss), fall back to a full state snapshot.
3. If the baseline is very old, the "delta" may be larger than a full snapshot — send whichever is smaller.

### Quantization

Reduce the precision of values to fit in fewer bits:

| Data                  | Full Precision         | Quantized                  | Savings |
| --------------------- | ---------------------- | -------------------------- | ------- |
| Position (per axis)   | float32 (32 bits)      | fixed-point 16.8 (24 bits) | 25%     |
| Velocity (per axis)   | float32 (32 bits)      | 12-bit scaled int          | 62.5%   |
| Rotation (quaternion) | 4 × float32 (128 bits) | "smallest three" (29 bits) | 77%     |
| Health (0-100)        | int32 (32 bits)        | uint8 (8 bits)             | 75%     |

"Smallest three" quaternion encoding: store only the 3 smallest components (2 bits to indicate which is largest, 9 bits each for the 3 smallest). The fourth component is derived from the unit constraint $w^2 + x^2 + y^2 + z^2 = 1$.

### Quantization Error

Quantization introduces error. A 16-bit position with 8 fractional bits has resolution of $1/256 \approx 0.004$ units. If units are meters, that's 4mm — invisible in most games. If units are centimeters, that's 0.04mm — more than enough.

The acceptable quantization error depends on the game's visual scale and the data type:

- Positions: error < 1% of visual size at typical zoom.
- Rotations: error < 0.5° for characters, < 5° for distant objects.
- Health/score: exact (use integer types with sufficient range).

### Bitpacking

After quantization, individual fields can be packed into a bitstream without byte alignment:

```
Bits 0-15:   x delta (16 bits)
Bits 16-31:  y delta (16 bits)
Bits 32-43:  z delta (12 bits)
Bits 44-72:  rotation (29 bits)
Bits 73-84:  velocity_x (12 bits)
...
```

This avoids the padding waste of byte-aligned fields. A state update that would be 48 bytes with aligned fields might pack into 25 bytes.

### Compression

General-purpose compression (zlib, LZ4, zstd) can further reduce packet size:

| Technique      | Compression Ratio (typical state data) | CPU Cost | Latency   |
| -------------- | -------------------------------------- | -------- | --------- |
| None           | 1.0×                                   | None     | None      |
| LZ4 (fast)     | 1.5-2.0×                               | Very low | < 0.1ms   |
| zstd (level 1) | 2.0-3.0×                               | Low      | 0.1-0.5ms |
| zlib (level 6) | 2.5-3.5×                               | Moderate | 0.5-2ms   |

For game packets, LZ4 is usually the sweet spot: fast enough to not add meaningful latency, effective enough to provide meaningful compression.

**Caution**: compression works best on larger payloads. A 50-byte packet may not compress at all (overhead of compression framing exceeds savings). Apply compression only to packets above a minimum size (e.g., > 200 bytes).

### Combined Effect

Applying delta encoding + quantization + bitpacking to an entity state update:

| Technique                     | Entity Size |
| ----------------------------- | ----------- |
| Raw (float32 everything)      | 48 bytes    |
| Quantized (reduced precision) | 28 bytes    |
| Delta + quantized             | 14 bytes    |
| Delta + quantized + bitpacked | 10 bytes    |

From 48 bytes to 10 bytes: a 4.8× compression. The same budget that held 25 entities now holds 120.

---

## 5. Graceful Degradation: Drop or Defer Under Pressure

### The Principle

When the budget is exhausted and path quality degrades, the system must reduce what it sends. **Graceful degradation** means reducing quality smoothly rather than failing abruptly. The player should experience "a bit worse" before experiencing "unplayable."

### Degradation Hierarchy

The system should shed load in order from least to most important:

1. **Drop telemetry and debug data.** No player impact.
2. **Reduce distant entity update frequency.** Player unlikely to notice.
3. **Reduce quantization precision for non-critical state.** Subtle visual artifacts.
4. **Reduce nearby entity update frequency.** Visible but tolerable.
5. **Reduce update frequency for all state.** Clearly degraded experience.
6. **Drop cosmetic state entirely.** Functional but ugly.
7. **Reduce input/event rate.** Gameplay is impaired.
8. **Disconnect.** Can't maintain minimal quality.

Steps 1-4 should handle common congestion. Step 5 handles sustained degradation. Steps 6-7 are emergency measures. Step 8 is the last resort.

### Interest Management as Budget Tool

Interest management (also called relevance filtering or area of interest) determines which entities each client receives updates about. Under budget pressure, the interest area shrinks:

- Normal: updates for all entities within 100m radius.
- Constrained: updates for entities within 50m radius.
- Severe: updates for entities within 20m radius + all players (regardless of range).

This dramatically reduces the number of entities competing for budget space.

### Dynamic Level of Detail (Network LOD)

Similar to visual LOD in rendering, network LOD adjusts the detail level of state updates based on distance and budget:

| Distance | Update Rate    | Precision | Components Sent                                  |
| -------- | -------------- | --------- | ------------------------------------------------ |
| < 20m    | Every tick     | Full      | Position, velocity, rotation, animation, effects |
| 20-50m   | Every 2 ticks  | Reduced   | Position, velocity, rotation                     |
| 50-100m  | Every 5 ticks  | Minimal   | Position, rough rotation                         |
| > 100m   | Every 10 ticks | Coarse    | Position only                                    |

Under budget pressure, all distance bands shift one level coarser.

### Worked Example: Degradation in Action

Normal budget: 1,200 bytes. Congestion reduces effective budget to 600 bytes.

| Priority              | Normal (1,200B) | Degraded (600B) | Action                 |
| --------------------- | --------------- | --------------- | ---------------------- |
| Protocol              | 40              | 40              | Unchanged              |
| Retransmissions       | 100             | 100             | Unchanged              |
| Critical events       | 60              | 60              | Unchanged              |
| Nearby (20 entities)  | 600             | 350             | Update 15 → 9 entities |
| Distant (50 entities) | 300             | 50              | Update 11 → 2 entities |
| Telemetry             | 100             | 0               | Dropped entirely       |

All critical game data is preserved. Entity update frequency is reduced for non-critical entities. The player experiences slightly less smooth motion for distant entities but no gameplay impact.

### Signaling Degradation to the Client

When the server reduces update quality, the client should be informed:

- **Send rate changed**: client adjusts interpolation buffer.
- **Interest area reduced**: client knows not to expect updates for distant entities (avoids "frozen" entities in the world).
- **LOD reduced**: client may substitute local animations or interpolation to compensate.

This signaling can be a small header field in each packet (1-2 bytes for a quality level indicator).

---

## Code Example (C++): Priority Budget Packer

```cpp
#include <vector>
#include <algorithm>
#include <cstdint>

struct StateUpdate {
    uint32_t entityId;
    int priorityClass;        // 0 = highest
    float relevanceScore;     // 0.0 - 1.0
    int ticksSinceLastSend;
    int sizeBytes;
    const uint8_t* data;
};

class BudgetPacker {
    int budgetBytes;
    int overheadBytes;

public:
    explicit BudgetPacker(int budget, int overhead = 40)
        : budgetBytes(budget), overheadBytes(overhead) {}

    struct PackResult {
        std::vector<const StateUpdate*> included;
        std::vector<const StateUpdate*> deferred;
        int bytesUsed;
    };

    PackResult pack(std::vector<StateUpdate>& updates) {
        PackResult result;
        int remaining = budgetBytes - overheadBytes;

        // Compute effective priority with aging
        for (auto& u : updates) {
            u.relevanceScore = computeEffectivePriority(u);
        }

        // Sort by effective priority (highest first = lowest number)
        std::sort(updates.begin(), updates.end(),
            [](const StateUpdate& a, const StateUpdate& b) {
                return a.relevanceScore > b.relevanceScore;
            });

        for (auto& u : updates) {
            if (remaining >= u.sizeBytes) {
                result.included.push_back(&u);
                remaining -= u.sizeBytes;
            } else {
                result.deferred.push_back(&u);
            }
        }

        result.bytesUsed = budgetBytes - overheadBytes - remaining;
        return result;
    }

private:
    float computeEffectivePriority(const StateUpdate& u) const {
        // Base priority from class (invert so higher class = higher score)
        float base = 100.0f - u.priorityClass * 15.0f;

        // Relevance multiplier
        base *= (0.5f + 0.5f * u.relevanceScore);

        // Aging bonus: 3 points per tick deferred
        base += u.ticksSinceLastSend * 3.0f;

        return base;
    }
};
```

## Code Example (C#): Network LOD System

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

public enum NetworkLodLevel { Full, Reduced, Minimal, Coarse }

public class NetworkLodConfig
{
    public float MaxDistance { get; init; }
    public int UpdateEveryNTicks { get; init; }
    public NetworkLodLevel DetailLevel { get; init; }
}

public class NetworkLodSystem
{
    private readonly List<NetworkLodConfig> _bands = new()
    {
        new() { MaxDistance = 20, UpdateEveryNTicks = 1, DetailLevel = NetworkLodLevel.Full },
        new() { MaxDistance = 50, UpdateEveryNTicks = 2, DetailLevel = NetworkLodLevel.Reduced },
        new() { MaxDistance = 100, UpdateEveryNTicks = 5, DetailLevel = NetworkLodLevel.Minimal },
        new() { MaxDistance = float.MaxValue, UpdateEveryNTicks = 10, DetailLevel = NetworkLodLevel.Coarse }
    };

    private int _degradationLevel; // 0 = normal, 1 = constrained, 2 = severe

    public void SetDegradation(int level)
    {
        _degradationLevel = Math.Clamp(level, 0, 2);
    }

    public struct EntitySendDecision
    {
        public int EntityId;
        public bool ShouldSend;
        public NetworkLodLevel LodLevel;
    }

    public List<EntitySendDecision> EvaluateEntities(
        IEnumerable<(int EntityId, float Distance, int TicksSinceLastSend)> entities,
        int currentTick)
    {
        var decisions = new List<EntitySendDecision>();

        foreach (var (entityId, distance, ticksSince) in entities)
        {
            var band = GetBand(distance);
            int adjustedInterval = band.UpdateEveryNTicks * (1 + _degradationLevel);
            var adjustedLod = (NetworkLodLevel)Math.Min(
                (int)band.DetailLevel + _degradationLevel,
                (int)NetworkLodLevel.Coarse);

            bool shouldSend = ticksSince >= adjustedInterval;

            decisions.Add(new EntitySendDecision
            {
                EntityId = entityId,
                ShouldSend = shouldSend,
                LodLevel = adjustedLod
            });
        }

        return decisions;
    }

    private NetworkLodConfig GetBand(float distance)
    {
        foreach (var band in _bands)
        {
            if (distance <= band.MaxDistance)
                return band;
        }
        return _bands[^1];
    }

    public int EstimateEntitySize(NetworkLodLevel lod) => lod switch
    {
        NetworkLodLevel.Full => 48,
        NetworkLodLevel.Reduced => 28,
        NetworkLodLevel.Minimal => 16,
        NetworkLodLevel.Coarse => 8,
        _ => 48
    };
}
```

---

## Common Anti-Patterns

1. **No budget tracking**: serializing whatever data is ready without checking total size. Leads to oversized packets and fragmentation.
2. **Flat priority (everything is "important")**: when everything is equally prioritized, nothing can be deferred, and the system cannot degrade gracefully.
3. **No starvation prevention**: low-priority data accumulates indefinitely, creating a "stale data bomb" when it finally gets budget.
4. **Full state every tick**: sending complete entity state instead of deltas wastes most of the budget on unchanged data.
5. **Compression without quantization**: general compression is less effective than domain-specific quantization for game state. Do both.
6. **No network LOD**: every entity gets the same update resolution regardless of distance or importance.
7. **Abrupt degradation**: switching from "everything" to "nothing" instead of smoothly reducing quality. Players perceive a sudden drop much more than a gradual one.
8. **Not signaling quality changes to clients**: the client needs to know when send rate or LOD changes so it can adjust interpolation and presentation.
