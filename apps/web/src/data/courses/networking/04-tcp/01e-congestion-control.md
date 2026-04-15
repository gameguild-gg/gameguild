## Congestion Control

While flow control protects the receiver, **congestion control** protects the network from being overwhelmed.

### Congestion Window (cwnd)

The sender maintains a **congestion window** that limits how much data can be in flight:

```
Effective Window = min(cwnd, receiver_advertised_window)
```

### The Two Phases of Congestion Control

TCP congestion control alternates between **two growth phases**, separated by a threshold called **ssthresh** (slow start threshold):

| Phase                    | Growth Type                         | Active When     | Purpose                                           |
| ------------------------ | ----------------------------------- | --------------- | ------------------------------------------------- |
| **Slow Start**           | **Exponential** (doubles every RTT) | cwnd < ssthresh | Ramp up quickly to find available capacity        |
| **Congestion Avoidance** | **Linear** (+1 MSS per RTT)         | cwnd ≥ ssthresh | Probe carefully to avoid overwhelming the network |

**ssthresh** starts at a high value (e.g., 65535 bytes). It is only changed when packet loss occurs, at which point it is set to half the current cwnd. This means the network "remembers" approximately where congestion happened and switches to cautious linear growth before reaching that point again.

### Phase 1: Slow Start (Exponential Growth)

Despite its name, slow start grows **exponentially**:

1. Start with cwnd = 1 MSS (Maximum Segment Size)
2. For each ACK received, increase cwnd by 1 MSS
3. Since each RTT acknowledges all segments in the window, this **doubles** cwnd every RTT

![Slow Start Graph](https://quickchart.io/chart?c=%7B%22type%22%3A%22line%22%2C%22data%22%3A%7B%22labels%22%3A%5B%22RTT%200%22%2C%22RTT%201%22%2C%22RTT%202%22%2C%22RTT%203%22%2C%22RTT%204%22%2C%22RTT%205%22%5D%2C%22datasets%22%3A%5B%7B%22label%22%3A%22cwnd%20%28segments%29%22%2C%22data%22%3A%5B1%2C2%2C4%2C8%2C16%2C32%5D%2C%22fill%22%3Afalse%2C%22borderColor%22%3A%22rgb%2875%2C%20192%2C%20192%29%22%2C%22tension%22%3A0%7D%5D%7D%2C%22options%22%3A%7B%22title%22%3A%7B%22display%22%3Atrue%2C%22text%22%3A%22TCP%20Slow%20Start%20%28Exponential%20Growth%29%22%7D%2C%22scales%22%3A%7B%22yAxes%22%3A%5B%7B%22scaleLabel%22%3A%7B%22display%22%3Atrue%2C%22labelString%22%3A%22Congestion%20Window%20%28segments%29%22%7D%7D%5D%7D%7D%7D)

**Slow Start ends when one of these happens:**

1. **cwnd reaches ssthresh** → TCP transitions to Congestion Avoidance (linear growth)
2. **Packet loss (timeout)** → ssthresh is set to cwnd/2, cwnd resets to 1, and Slow Start restarts
3. **3 duplicate ACKs** → ssthresh is set to cwnd/2, cwnd is set to ssthresh, and TCP enters Congestion Avoidance directly (fast recovery)

### Phase 2: Congestion Avoidance (Linear Growth)

Once cwnd ≥ ssthresh, TCP switches from exponential to **linear** growth. The idea is: "We're getting close to where congestion happened before, so let's be careful."

This phase uses **Additive Increase, Multiplicative Decrease (AIMD)**:

- **Additive Increase**: Increase cwnd by ~1 MSS per RTT (linear growth). So cwnd goes: ssthresh → ssthresh+1 → ssthresh+2 → ...
- **Multiplicative Decrease**: On packet loss, ssthresh is set to cwnd/2 and cwnd is reduced (see loss response below)

This creates the characteristic "sawtooth" pattern:

![AIMD Sawtooth](https://quickchart.io/chart?c=%7B%22type%22%3A%22line%22%2C%22data%22%3A%7B%22labels%22%3A%5B%220%22%2C%221%22%2C%222%22%2C%223%22%2C%224%22%2C%225%22%2C%226%22%2C%227%22%2C%228%22%2C%229%22%2C%2210%22%2C%2211%22%2C%2212%22%2C%2213%22%2C%2214%22%2C%2215%22%2C%2216%22%2C%2217%22%2C%2218%22%2C%2219%22%2C%2220%22%5D%2C%22datasets%22%3A%5B%7B%22label%22%3A%22cwnd%22%2C%22data%22%3A%5B1%2C2%2C4%2C8%2C16%2C17%2C18%2C19%2C20%2C21%2C22%2C11%2C12%2C13%2C14%2C15%2C16%2C17%2C18%2C9%2C10%5D%2C%22fill%22%3Afalse%2C%22borderColor%22%3A%22rgb%28255%2C%2099%2C%20132%29%22%2C%22tension%22%3A0%7D%2C%7B%22label%22%3A%22ssthresh%22%2C%22data%22%3A%5B64%2C64%2C64%2C64%2C16%2C16%2C16%2C16%2C16%2C16%2C16%2C11%2C11%2C11%2C11%2C11%2C11%2C11%2C11%2C9%2C9%5D%2C%22fill%22%3Afalse%2C%22borderColor%22%3A%22rgb%2854%2C%20162%2C%20235%29%22%2C%22borderDash%22%3A%5B5%2C5%5D%2C%22tension%22%3A0%7D%5D%7D%2C%22options%22%3A%7B%22title%22%3A%7B%22display%22%3Atrue%2C%22text%22%3A%22TCP%20Congestion%20Control%20%28AIMD%20Sawtooth%29%22%7D%2C%22scales%22%3A%7B%22xAxes%22%3A%5B%7B%22scaleLabel%22%3A%7B%22display%22%3Atrue%2C%22labelString%22%3A%22Time%20%28RTT%29%22%7D%7D%5D%2C%22yAxes%22%3A%5B%7B%22scaleLabel%22%3A%7B%22display%22%3Atrue%2C%22labelString%22%3A%22Window%20Size%20%28segments%29%22%7D%7D%5D%7D%7D%7D)

**Reading the graph above:** Notice how cwnd grows exponentially (1→2→4→8→16) during Slow Start, then switches to linear growth (16→17→18→...) once it hits ssthresh. When loss occurs at cwnd=22, ssthresh drops to 11, and the pattern repeats.

### The Transition Visualized

This chart shows a single connection's lifecycle — exponential growth during Slow Start, then linear growth after crossing ssthresh:

![Congestion Control Transition](<https://quickchart.io/chart?c=%7B%22type%22%3A%22line%22%2C%22data%22%3A%7B%22labels%22%3A%5B%220%22%2C%221%22%2C%222%22%2C%223%22%2C%224%22%2C%225%22%2C%226%22%2C%227%22%2C%228%22%2C%229%22%2C%2210%22%5D%2C%22datasets%22%3A%5B%7B%22label%22%3A%22cwnd%20(segments)%22%2C%22data%22%3A%5B1%2C2%2C4%2C8%2C16%2C17%2C18%2C19%2C20%2C21%2C22%5D%2C%22fill%22%3Afalse%2C%22borderColor%22%3A%22rgb(255%2C%2099%2C%20132)%22%2C%22backgroundColor%22%3A%22rgb(255%2C%2099%2C%20132)%22%2C%22tension%22%3A0%7D%2C%7B%22label%22%3A%22ssthresh%20%3D%2016%22%2C%22data%22%3A%5B16%2C16%2C16%2C16%2C16%2C16%2C16%2C16%2C16%2C16%2C16%5D%2C%22fill%22%3Afalse%2C%22borderColor%22%3A%22rgb(54%2C%20162%2C%20235)%22%2C%22borderDash%22%3A%5B5%2C5%5D%2C%22pointRadius%22%3A0%2C%22tension%22%3A0%7D%5D%7D%2C%22options%22%3A%7B%22title%22%3A%7B%22display%22%3Atrue%2C%22text%22%3A%22Slow%20Start%20(Exponential)%20%E2%86%92%20Congestion%20Avoidance%20(Linear)%22%7D%2C%22scales%22%3A%7B%22xAxes%22%3A%5B%7B%22scaleLabel%22%3A%7B%22display%22%3Atrue%2C%22labelString%22%3A%22Time%20(RTT)%22%7D%7D%5D%2C%22yAxes%22%3A%5B%7B%22scaleLabel%22%3A%7B%22display%22%3Atrue%2C%22labelString%22%3A%22cwnd%20(segments)%22%7D%2C%22ticks%22%3A%7B%22beginAtZero%22%3Atrue%7D%7D%5D%7D%2C%22annotation%22%3A%7B%22annotations%22%3A%5B%7B%22type%22%3A%22box%22%2C%22xScaleID%22%3A%22x-axis-0%22%2C%22yScaleID%22%3A%22y-axis-0%22%2C%22xMin%22%3A%220%22%2C%22xMax%22%3A%224%22%2C%22backgroundColor%22%3A%22rgba(75%2C%20192%2C%20192%2C%200.15)%22%2C%22borderColor%22%3A%22rgba(75%2C%20192%2C%20192%2C%200.4)%22%2C%22label%22%3A%7B%22enabled%22%3Atrue%2C%22content%22%3A%22Exponential%22%2C%22position%22%3A%22top%22%2C%22fontSize%22%3A11%7D%7D%2C%7B%22type%22%3A%22box%22%2C%22xScaleID%22%3A%22x-axis-0%22%2C%22yScaleID%22%3A%22y-axis-0%22%2C%22xMin%22%3A%224%22%2C%22xMax%22%3A%2210%22%2C%22backgroundColor%22%3A%22rgba(255%2C%20159%2C%2064%2C%200.15)%22%2C%22borderColor%22%3A%22rgba(255%2C%20159%2C%2064%2C%200.4)%22%2C%22label%22%3A%7B%22enabled%22%3Atrue%2C%22content%22%3A%22Linear%22%2C%22position%22%3A%22top%22%2C%22fontSize%22%3A11%7D%7D%5D%7D%7D%7D>)

### The Transition: A Step-by-Step Walkthrough

Let's trace a complete congestion control lifecycle to make the exponential → linear transition crystal clear:

```
Initial state: cwnd = 1, ssthresh = 16

--- PHASE 1: SLOW START (exponential) ---
RTT 0:  cwnd = 1   → send 1 segment   (cwnd < ssthresh, so exponential)
RTT 1:  cwnd = 2   → send 2 segments
RTT 2:  cwnd = 4   → send 4 segments
RTT 3:  cwnd = 8   → send 8 segments
RTT 4:  cwnd = 16  → cwnd has reached ssthresh!

--- TRANSITION: cwnd == ssthresh → switch to linear growth ---

--- PHASE 2: CONGESTION AVOIDANCE (linear) ---
RTT 5:  cwnd = 17  → +1 per RTT now (cwnd ≥ ssthresh, so linear)
RTT 6:  cwnd = 18
RTT 7:  cwnd = 19
RTT 8:  cwnd = 20
RTT 9:  cwnd = 21
RTT 10: cwnd = 22  → PACKET LOSS DETECTED (timeout)!

--- LOSS RESPONSE ---
New ssthresh = 22 / 2 = 11
New cwnd = 1

--- PHASE 1 AGAIN: SLOW START (exponential) ---
RTT 11: cwnd = 1
RTT 12: cwnd = 2
RTT 13: cwnd = 4
RTT 14: cwnd = 8
RTT 15: cwnd = 11  → reached new ssthresh!

--- PHASE 2 AGAIN: CONGESTION AVOIDANCE (linear) ---
RTT 16: cwnd = 12
RTT 17: cwnd = 13
... and so on
```

> **Think of it this way:** Slow Start is like accelerating in a car — you speed up quickly (exponential) until you approach the speed limit (ssthresh). Then you switch to Congestion Avoidance — gentle taps on the gas (linear) until you crash (packet loss). After a crash, the speed limit is lowered (new ssthresh = cwnd/2) and you start over.

### Response to Packet Loss

| Event            | Action                                                                            |
| ---------------- | --------------------------------------------------------------------------------- |
| Timeout          | ssthresh = cwnd/2, cwnd = 1, restart slow start                                   |
| 3 Duplicate ACKs | ssthresh = cwnd/2, cwnd = ssthresh, continue congestion avoidance (fast recovery) |

**Why the difference?** A timeout is a severe signal — the network may be very congested, so TCP resets to cwnd=1 and does a full Slow Start. Three duplicate ACKs are a milder signal — packets are still getting through, so TCP can skip Slow Start and jump directly to Congestion Avoidance at the new ssthresh (this is called **Fast Recovery**).

**Example:** If cwnd = 12 and timeout occurs:

- New ssthresh = 12 ÷ 2 = 6
- New cwnd = 1
- TCP restarts Slow Start: cwnd = 1 → 2 → 4 → reaches ssthresh (6)
- Then switches to Congestion Avoidance: cwnd = 6 → 7 → 8 → 9 → ...
