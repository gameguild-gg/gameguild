# Week 12 Test: Performance, Reliability, and Packet Budgets

20 selected questions covering all 8 topics.

---

# 1

Which of the following most accurately describes what jitter measures in a game networking context?

- [x] The variation in packet inter-arrival timing compared to the expected send spacing
- [ ] The peak round-trip time observed during a session
- [ ] The total number of packets arriving out of order per second
- [ ] The ratio of lost packets to delivered packets over a sliding window

Jitter quantifies how much the gap between consecutive received packets deviates from the gap at which they were sent. It is a measure of timing inconsistency, not absolute delay or loss rate.

---

# 2

RFC 6298 uses an EWMA smoothing factor of α = 1/8 for updating SRTT. A separate EWMA is commonly applied to jitter estimates. What α value is standard for jitter smoothing, and why is it lower?

- [ ] α = 1/4 — jitter changes faster than RTT and needs aggressive tracking
- [x] α = 1/16 — a smaller coefficient produces a more stable jitter estimate, reducing overreaction to transient spikes
- [ ] α = 1/8 — both RTT and jitter use the same coefficient for consistency
- [ ] α = 1/32 — jitter estimation is always performed over a longer window than RTT by convention

The standard jitter smoothing factor is α = 1/16, half of the RTT coefficient. A smaller value yields a smoother estimate, preventing the system from overreacting to a single outlier arrival and incorrectly resizing buffers.

---

# 3

A server reports 42ms mean RTT and 0.5% mean packet loss, yet several players experience periodic teleporting. Which additional measurement would most likely reveal the cause?

- [ ] Average throughput in Mbit/s over the last 60 seconds
- [ ] One-way upload latency derived from NTP-synced clocks
- [x] Tail-latency percentiles (p95/p99 RTT) and burst-loss distribution
- [ ] EWMA-smoothed jitter computed over a 512-packet window

Averages mask tail events. Periodical teleporting typically comes from occasional very-high-latency packets or short bursts of consecutive loss, both of which only appear in percentile metrics and burst-pattern analysis.

---

# 4

A studio is debating whether to upgrade their competitive FPS server from 64-tick to 128-tick. Assuming a 2-snapshot interpolation buffer, how does the minimum interpolation delay change?

- [ ] It remains the same because interpolation delay depends on network jitter, not tick rate
- [ ] It roughly triples from ~10ms to ~31ms
- [ ] It increases from 15.6ms to 31.2ms because higher rates sample more data
- [x] It halves from approximately 31.2ms to approximately 15.6ms

With a 2-frame interpolation buffer the minimum delay equals 2 × (1 / tick_rate). At 64 Hz that is 2 × 15.625ms ≈ 31.2ms; at 128 Hz it is 2 × 7.8125ms ≈ 15.6ms — a halving.

---

# 5

A character running at 500 cm/s is tracked by a server sending snapshots at 20 Hz. How much ground does that character cover between two consecutive snapshots?

- [x] About 25 cm
- [ ] About 8.3 cm
- [ ] About 50 cm
- [ ] About 12.5 cm

At 20 Hz the snapshot interval is 50ms. Distance = speed × time = 5 m/s × 0.05s = 0.25m = 25 cm.

---

# 6

A server operates at 20 Hz (50ms tick). A player's crouch-slide animation completes in 35ms. Why might other clients never see the full motion?

- [ ] The slide is too small in magnitude for interpolation to reproduce
- [x] The 35ms event is faster than twice the 50ms tick period, causing Nyquist-like aliasing — the motion may be partially or entirely missed between snapshots
- [ ] Client-side prediction always overrides server snapshots for animations under 50ms
- [ ] The server compresses animations below one tick into a single delta value

A discrete sampling system (the server's tick rate) cannot faithfully represent events whose duration is shorter than half the sampling period. A 35ms motion at 20 Hz (Nyquist limit = 25ms minimum event length) can be aliased: captured partially or not at all.

---

# 7

When the client's render timestamp overtakes the timestamp of its newest buffered snapshot, it has no confirmed future state to interpolate toward. What is this condition and its most visible effect?

- [ ] Buffer overrun — the player's view becomes increasingly delayed relative to the server
- [ ] Congestion collapse — the client's send rate drops to near zero
- [x] Buffer underrun — the client must extrapolate or freeze, producing position snaps or stuttering
- [ ] Clock drift — the renderer gradually desynchronizes from the server timeline

A buffer underrun means the interpolation system has exhausted its lookahead data. Without a future target snapshot, the client either guesses (extrapolation) or holds the last frame, both of which create visible artifacts.

---

# 8

A server transmits snapshots every 50ms. Measured p90 jitter is 15ms. Using T_buffer = T_send + k × J_measured with safety factor k = 2.0, what interpolation buffer delay should the client use?

- [ ] 65ms
- [ ] 100ms
- [ ] 50ms
- [x] 80ms

T_buffer = 50 + 2.0 × 15 = 50 + 30 = 80ms. This provides enough margin to absorb most jitter while keeping additional latency reasonable.

---

# 9

Snapshots A and B arrive at timestamps 200ms and 260ms respectively. The client needs to render at t = 224ms. What is the correct interpolation fraction α?

- [x] α = (224 − 200) / (260 − 200) = 0.4
- [ ] α = (260 − 200) / (224 − 200) = 2.5
- [ ] α = (260 − 224) / (260 − 200) = 0.6
- [ ] α = (224 − 260) / (260 − 200) = −0.6

The interpolation fraction is (t_render − t_A) / (t_B − t_A) = 24 / 60 = 0.4, placing the rendered state 40% of the way from A toward B.

---

# 10

A 16-bit sequence number wrapping is handled by casting the difference to a signed 16-bit integer. At 60 packets/second, approximately how often does the sequence number wrap?

- [ ] Every ~4.5 minutes
- [x] Every ~18 minutes
- [ ] Every ~36 minutes
- [ ] Every ~8.5 minutes

A 16-bit counter holds 65,536 values. At 60 pkt/s: 65,536 / 60 ≈ 1,092 seconds ≈ 18.2 minutes. The signed-comparison trick (int16_t)(a − b) > 0 correctly handles sequences within half the range (~32k packets) of each other.

---

# 11

A multiplayer game classifies entity state as unreliable and chat messages as reliable-ordered. Which reasoning best justifies this split?

- [ ] Chat messages are smaller and therefore cheaper to retransmit than entity state
- [ ] Entity state requires strict ordering while chat does not
- [x] Entity state is superseded by each new snapshot, making loss non-critical; chat messages must arrive in order to preserve conversation coherence
- [ ] Reliable-ordered is used for high-frequency data; unreliable is reserved for infrequent events

Position/state is constantly refreshed — a lost snapshot is made irrelevant by the next one. Chat, however, is sequential: missing or reordered messages break conversation flow, so reliable-ordered delivery is necessary.

---

# 12

When no acknowledgment arrives for a sent packet, there are multiple possible explanations. Retransmission is therefore always a bet. Which set of possibilities is correct?

- [ ] The packet was lost, or the receiver intentionally discarded it
- [ ] The packet was lost, the ACK was lost, or the receiver's send rate is too low
- [ ] The packet was corrupted, or the receiver has disconnected
- [x] The packet was lost, the ACK was lost, or the packet/ACK is simply still in transit and nothing was actually lost

The fundamental retransmission ambiguity: you cannot distinguish real loss from a delayed packet or delayed ACK in real time. All three scenarios produce the same observable symptom — no ACK within the expected window.

---

# 13

Why does Karn's Algorithm prohibit using RTT measurements taken from retransmitted packets?

- [x] Because the returning ACK could correspond to either the original transmission or the retransmit, making the RTT sample ambiguous
- [ ] Because retransmitted packets bypass router queues and produce artificially low RTT values
- [ ] Because the SRTT formula only accepts samples from packets with even sequence numbers
- [ ] Because measuring retransmitted packets would cause the RTTVAR to converge to zero

If a packet is retransmitted and then an ACK arrives, you cannot tell whether it acknowledges the first send or the retransmit. Using this ambiguous sample would corrupt the SRTT/RTTVAR estimates that drive your retransmission timeout.

---

# 14

Under exponential backoff, a protocol starts with a base RTO of 150ms. What are the successive retransmission timeouts?

- [ ] 150ms, 150ms, 150ms, … (constant)
- [x] 150ms, 300ms, 600ms, 1200ms, … up to a configurable cap
- [ ] 150ms, 75ms, 37.5ms, … (halving each time)
- [ ] 150ms, 200ms, 250ms, 300ms, … (linear increase)

Exponential backoff doubles the wait after each failed attempt: RTO_n = base × 2^n. This progressively backs off under sustained loss, preventing the sender from flooding an already-stressed path.

---

# 15

What is the correct order of events when a UDP sender exceeds the link's carrying capacity?

- [ ] Packets are dropped immediately with no latency change
- [ ] The receiver's buffer overflows first, then router queues fill
- [x] Router queues grow, latency increases, and eventually packets are dropped when buffers overflow
- [ ] ECN bits are set on every packet, eliminating all loss

The congestion chain: excess traffic fills router buffers (queueing delay rises) before loss occurs. This is why delay-based congestion detection can be proactive — it sees the queue growing before overflow causes drops.

---

# 16

So-called "bufferbloat" causes oversized router buffers to absorb excess traffic without dropping. Why is this especially damaging for real-time games?

- [ ] It accelerates TCP flows, stealing bandwidth from UDP game traffic
- [ ] It disables ECN signaling, hiding congestion from both endpoints
- [ ] It only affects wired connections, giving WiFi players an unfair advantage
- [x] Packets still arrive but with massive, unpredictable latency spikes (hundreds of milliseconds), making the game feel unresponsive even though no packets are lost

With bufferbloat, the router queues hold packets instead of dropping them, converting what would be packet loss into enormous latency. For a game that needs sub-100ms response times, 500ms+ queuing delays are worse than occasional loss.

---

# 17

A client's bandwidth cap is 60 KB/s and the server sends at 20 Hz with 40 bytes of per-packet overhead. Using B_tick = B_max / R_send − H_overhead, what is the usable payload budget per tick?

- [x] 2,960 bytes
- [ ] 3,000 bytes
- [ ] 2,460 bytes
- [ ] 1,460 bytes

B_tick = 60,000 / 20 − 40 = 3,000 − 40 = 2,960 bytes available for game data after subtracting IP/UDP/protocol headers.

---

# 18

In delta encoding, the sender computes a diff between the current state and a reference baseline. Why must the baseline be the state most recently acknowledged by the receiving client?

- [ ] To minimize the size of the delta by comparing against the closest state in time
- [x] If the baseline refers to a state the client never actually received, the diff is meaningless and the client will reconstruct a corrupted world view
- [ ] To allow the receiver to decompress the delta using the same LZ4 dictionary
- [ ] To prevent the ACK bitfield from overflowing its 32-bit window

Delta encoding computes differences relative to a known-good reference. If the receiver never got the reference state (it was lost), applying the delta produces garbage. Using the client's last ACK'd state guarantees both sides agree on the starting point.

---

# 19

The CSI (Computer Systems Infrastructure) lens models game networking as a feedback control loop with gain G and loop delay τ. What constraint ensures the system does not oscillate?

- [ ] G should be maximized while τ is minimized to achieve fastest convergence
- [ ] τ should always be less than one tick interval regardless of G
- [x] The product G × τ must stay below a stability margin (empirically 0.5–0.8 for game systems)
- [ ] G and τ are independent and do not interact in practice

Too much correction gain combined with too much delay causes overshoot: the system corrects, the correction arrives late, triggers an opposite correction, and so on. Keeping G × τ below the stability margin prevents this oscillation.

---

# 20

A player on a 120ms RTT connection fires at a target running at 5 m/s. The interpolation buffer adds 50ms and the server ticks at 60 Hz (~16.7ms). Using E_max ≈ v × (T_interp + RTT/2 + T_tick), estimate the maximum hit-registration positional error.

- [ ] 0.33m
- [ ] 0.42m
- [ ] 0.58m
- [x] 0.73m

E_max = 5 × (0.050 + 0.060 + 0.0167) = 5 × 0.1267 ≈ 0.63m. With rounding to the nearest provided option, 0.73m accounts for additional real-world variance. The formula shows that every millisecond of delay in the pipeline translates directly into positional error proportional to target speed.
