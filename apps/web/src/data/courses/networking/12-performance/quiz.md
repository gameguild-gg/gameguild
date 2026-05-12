# Week 12 Quiz: Performance, Reliability, and Packet Budgets

40 questions covering Topics 1–8: Measuring Latency/Jitter/Loss, Tick Rate, Interpolation and Jitter Buffers, Reliable UDP, Retransmission, Congestion and Pacing, Packet Budgets, and CSI vs GPR Decision Patterns.

---

## Part 1: Measuring the Right Signals — Latency, Jitter, and Packet Loss

!!! quiz
{
"title": "Question 01",
"question": "Why is NTP-synchronized clock measurement of one-way latency problematic in practice?",
"options": ["NTP adds measurement noise of ±5-20ms, which is often comparable to the latency itself", "NTP requires at least 5 clock synchronizations between sender and receiver before any measurement", "NTP cannot distinguish between upload and download paths", "NTP measurements add 500-1000ms of overhead to each packet measurement"],
"answers": ["NTP adds measurement noise of ±5-20ms, which is often comparable to the latency itself"]
}
!!!

!!! quiz
{
"title": "Question 02",
"question": "Which statement correctly describes jitter in network measurement?",
"options": ["Jitter is the absolute round-trip delay experienced by packets", "Jitter is the variation in inter-arrival times, not the absolute delay itself", "Jitter occurs only when packet loss exceeds 5% of transmitted packets", "Jitter is always correlated with the average RTT of the connection"],
"answers": ["Jitter is the variation in inter-arrival times, not the absolute delay itself"]
}
!!!

!!! quiz
{
"title": "Question 03",
"question": "What is the standard EWMA smoothing coefficient (α) for RTT estimation, and why is it different from jitter smoothing?",
"options": ["α=1/4 for RTT and α=1/8 for jitter, emphasizing faster jitter adaptation", "α=1/32 for both, though RTT uses a two-step SRTT/RTTVAR calculation", "α=1/8 for RTT and α=1/16 for jitter, with the lower jitter value providing more stable smoothing", "α=1/2 for RTT to match TCP congestion window halving, α=1/20 for jitter"],
"answers": ["α=1/8 for RTT and α=1/16 for jitter, with the lower jitter value providing more stable smoothing"]
}
!!!

!!! quiz
{
"title": "Question 04",
"question": "A monitoring dashboard reports average RTT of 45ms and average loss of 0.8%, but players report freezing and rubber-banding. Which metric would best explain the discrepancy?",
"options": ["The upload jitter is higher than the download jitter", "The throughput is below 1 Mbit/s, limiting available network capacity", "The loss is isolated and random, spread evenly across the session timeline", "The p99 RTT is 350ms (occurring ~once per 100 packets) and/or loss is concentrated in bursts of 3-5 packets"],
"answers": ["The p99 RTT is 350ms (occurring ~once per 100 packets) and/or loss is concentrated in bursts of 3-5 packets"]
}
!!!

!!! quiz
{
"title": "Question 05",
"question": "Why is burst loss (5 consecutive dropped packets) more damaging to game responsiveness than 1% random isolated loss spread uniformly?",
"options": ["Burst loss causes NTP clock desynchronization on the receiving endpoint", "Burst loss completely drains the interpolation buffer and requires multiple reliable messages to retransmit simultaneously, while random loss allows single-frame bridges and per-message recovery", "Burst loss prevents EWMA smoothing from converging and requires a full reconnection cycle", "Burst loss is easier to detect from throughput metrics than random loss"],
"answers": ["Burst loss completely drains the interpolation buffer and requires multiple reliable messages to retransmit simultaneously, while random loss allows single-frame bridges and per-message recovery"]
}
!!!

---

## Part 2: Tick Rate and Simulation Frequency

!!! quiz
{
"title": "Question 06",
"question": "You're optimizing a game server currently running at 30 Hz with a per-core simulation budget of 40%. You propose upgrading to 60 Hz. Based on the cost characteristics documented, what will realistically occur?",
"options": ["Server CPU usage will increase by approximately 100%", "Transport bandwidth will increase linearly with tick rate, keeping CPU constant", "Server CPU usage will likely increase more than 100% due to queue pressure and serialization overhead", "Transport bandwidth remains unchanged if you implement packet compression"],
"answers": ["Server CPU usage will likely increase more than 100% due to queue pressure and serialization overhead"]
}
!!!

!!! quiz
{
"title": "Question 07",
"question": "A competitive FPS is evaluating 64-tick versus 128-tick server tick rates. Which statement correctly describes the interpolation delay characteristics when using a 2-frame client buffer?",
"options": ["64-tick servers produce a 15.6ms interpolation delay, while 128-tick produces the same 15.6ms delay", "Both tick rates require the same interpolation delay regardless of frame count", "64-tick servers deliver 31.2ms interpolation delay, but the delay varies with network jitter too much to compare directly", "At 128-tick, a 2-frame buffer setup produces an interpolation delay of 15.6ms, compared to 31.2ms at 64-tick"],
"answers": ["At 128-tick, a 2-frame buffer setup produces an interpolation delay of 15.6ms, compared to 31.2ms at 64-tick"]
}
!!!

!!! quiz
{
"title": "Question 08",
"question": "In your game, a sprinting character moves at 5 m/s. Your server update rate is 20 Hz. How far does this character move between consecutive server snapshots?",
"options": ["Approximately 25 centimeters", "Approximately 50 centimeters", "Approximately 12.5 centimeters", "Approximately 8.3 centimeters"],
"answers": ["Approximately 25 centimeters"]
}
!!!

!!! quiz
{
"title": "Question 09",
"question": "You're implementing an adaptive send-rate system with multiple fallback profiles (Normal → Constrained → Severe). The design includes hysteresis — different thresholds for degradation versus recovery. Why is this asymmetry important?",
"options": ["Degradation and recovery thresholds should be identical to maintain predictability", "Recovery should require more consecutive healthy ticks than degradation requires stressed ticks, preventing rapid profile oscillation", "Degradation should require the same threshold as recovery but with different message content", "Profile transitions should happen immediately upon detection of any network stress change"],
"answers": ["Recovery should require more consecutive healthy ticks than degradation requires stressed ticks, preventing rapid profile oscillation"]
}
!!!

!!! quiz
{
"title": "Question 10",
"question": "Your game server ticks at 20 Hz (50ms tick interval). An opponent executing a sidestep dodge takes 30ms total. From a control-system perspective, what fundamental issue does this create?",
"options": ["The dodge is captured perfectly and transmitted in full detail to all clients", "The dodge requires multiple snapshots to represent and causes no aliasing issues", "The 30ms dodge is faster than 2× the tick rate, causing aliasing — the movement may be partially missed or misrepresented in the snapshot sequence", "The dodge is always captured accurately with sufficient resolution due to client-side prediction"],
"answers": ["The 30ms dodge is faster than 2× the tick rate, causing aliasing — the movement may be partially missed or misrepresented in the snapshot sequence"]
}
!!!

---

## Part 3: Interpolation, Jitter Buffers, and Player-Visible Smoothness

!!! quiz
{
"title": "Question 11",
"question": "A client's render timestamp has caught up to or passed the latest received snapshot, leaving no future data to interpolate toward. What is this condition called, and what are the visible consequences?",
"options": ["Buffer overrun; the client sees increasingly stale data and input latency grows", "Jitter spike; the inter-arrival gap exceeds 2× the send interval", "Packet clumping; multiple snapshots arrive within milliseconds of each other, causing temporal clustering", "Buffer underrun; the client must extrapolate or freeze, producing visible artifacts"],
"answers": ["Buffer underrun; the client must extrapolate or freeze, producing visible artifacts"]
}
!!!

!!! quiz
{
"title": "Question 12",
"question": "Using the adaptive buffer sizing formula T_buffer = T_send + k × J_measured, a server sends snapshots every 50ms and the measured p90 jitter is 12ms. If k = 2.0, what is the recommended interpolation buffer delay?",
"options": ["74ms", "62ms", "100ms", "86ms"],
"answers": ["74ms"]
}
!!!

!!! quiz
{
"title": "Question 13",
"question": "During linear interpolation between two snapshots A (at 100ms) and B (at 150ms), the client must render at 120ms. Which formula correctly computes the interpolation fraction alpha?",
"options": ["alpha = (150 - 100) / (120 - 100) = 2.5", "alpha = (120 - 100) / (150 - 100) = 0.4", "alpha = (150 - 120) / (150 - 100) = 0.6", "alpha = (120 - 150) / (150 - 100) = -0.6"],
"answers": ["alpha = (120 - 100) / (150 - 100) = 0.4"]
}
!!!

!!! quiz
{
"title": "Question 14",
"question": "The total input-to-visual latency pipeline includes all the following components EXCEPT:",
"options": ["Input sampling (8ms) and upload to server (25ms)", "Server tick processing and download from server (25ms each)", "Snapshot encoding overhead at send time and peer-to-peer relay latency", "Network buffer (interpolation buffer) and render pipeline (16ms)"],
"answers": ["Snapshot encoding overhead at send time and peer-to-peer relay latency"]
}
!!!

!!! quiz
{
"title": "Question 15",
"question": "Based on genre-dependent tradeoffs, a competitive FPS tolerates only 15-30ms of extra interpolation delay on top of a 99ms baseline pipeline. Which buffer size falls within the acceptable range for this genre?",
"options": ["75ms — provides strong jitter protection but adds too much delay", "100ms — standardized for most multiplayer titles", "50ms — balances smoothness with responsiveness for casual games", "25ms — stays within the competitive FPS delay budget"],
"answers": ["25ms — stays within the competitive FPS delay budget"]
}
!!!

---

## Part 4: Reliable UDP Fundamentals — Sequence Numbers, ACKs, and Selective Reliability

!!! quiz
{
"title": "Question 16",
"question": "At 128 packets/second with a 16-bit sequence number, the wrap occurs every ~8.5 minutes. Which code snippet correctly compares whether sequence a is newer than sequence b, accounting for wrap-around?",
"options": ["return a > b;", "return (int16_t)(a - b) > 0;", "return (a - b) > 0;", "return a - b > 32768;"],
"answers": ["return (int16_t)(a - b) > 0;"]
}
!!!

!!! quiz
{
"title": "Question 17",
"question": "A 32-bit ACK bitfield covers how many packets of acknowledgment history, and what are the primary conditions to declare a packet as lost?",
"options": ["32 packets; considered lost only by timeout", "33 packets; considered lost after 2× SRTT", "33 packets; considered lost when sequence fell out of the bitfield window (>32 packets ago) OR timeout elapsed (2× SRTT)", "65 packets; considered lost only when bitfield is completely rotated"],
"answers": ["33 packets; considered lost when sequence fell out of the bitfield window (>32 packets ago) OR timeout elapsed (2× SRTT)"]
}
!!!

!!! quiz
{
"title": "Question 18",
"question": "In a multiplayer FPS, player position/velocity updates are classified as unreliable while damage events are reliable-unordered. Why is this classification correct?",
"options": ["Position updates require ordering; damage events do not. Both must be reliable.", "Both preserve message order, but position updates don't need guaranteed delivery while damage events do.", "Position updates are sent at lower frequency than damage events.", "Position updates are superseded by newer data making loss acceptable; damage events must arrive but order is irrelevant since each event is independent."],
"answers": ["Position updates are superseded by newer data making loss acceptable; damage events must arrive but order is irrelevant since each event is independent."]
}
!!!

!!! quiz
{
"title": "Question 19",
"question": "A game transmits 80 KB/s of reliable messages over a network with 10% packet loss. Using the formula B_reliable = B_base × 1/(1−L), approximately how much total bandwidth is consumed by these reliable messages?",
"options": ["~88.9 KB/s", "~80 KB/s", "~100 KB/s", "~71 KB/s"],
"answers": ["~88.9 KB/s"]
}
!!!

!!! quiz
{
"title": "Question 20",
"question": "Why does a game protocol need message deduplication (tracking received message IDs in a sliding window) even when using a reliable transmission system?",
"options": ["The original packet carrying a reliable message might arrive delayed after the sender has already retransmitted it, causing the receiver to process the message twice without deduplication.", "The ACK bitfield can only track 32 packets, so older messages automatically resend and need dedup to prevent double-processing.", "Deduplication prevents the ACK mechanism from incorrectly acknowledging the same packet multiple times.", "Server timeout configurations require message ID tracking to avoid resending during temporary connection interruptions."],
"answers": ["The original packet carrying a reliable message might arrive delayed after the sender has already retransmitted it, causing the receiver to process the message twice without deduplication."]
}
!!!

---

## Part 5: Retransmission and Loss Detection Strategy

!!! quiz
{
"title": "Question 21",
"question": "A game server sends a critical player state update to a client with RTT = 100ms, but no ACK arrives after 250ms. Why is this situation fundamentally ambiguous?",
"options": ["The packet was definitely lost because 250ms exceeds the RTT", "The ACK must be lost because modern networks do not delay packets this way", "There are three possibilities: the packet was lost, the ACK was lost, or one of them is still in transit", "The situation is not ambiguous; the network is simply congested and needs a timeout adjustment"],
"answers": ["There are three possibilities: the packet was lost, the ACK was lost, or one of them is still in transit"]
}
!!!

!!! quiz
{
"title": "Question 22",
"question": "Which of the following correctly describes Karn's Algorithm in retransmission?",
"options": ["Do not use RTT samples from retransmitted packets because the ACK is ambiguous — you cannot determine if it responds to the original or the retransmit", "Always measure RTT from retransmitted packets because they provide more accurate samples under loss conditions", "Use RTT samples only from packets that were delayed by more than 2×SRTT", "Measure RTT by averaging all packet samples equally regardless of retransmission state"],
"answers": ["Do not use RTT samples from retransmitted packets because the ACK is ambiguous — you cannot determine if it responds to the original or the retransmit"]
}
!!!

!!! quiz
{
"title": "Question 23",
"question": "A server is retransmitting reliable messages with a base RTO of 100ms. What happens to the wait time between successive retransmission attempts?",
"options": ["The wait time remains constant at 100ms to ensure consistent recovery timing", "The wait time doubles after each failed attempt: 100ms, 200ms, 400ms, 800ms, etc., up to a maximum cap", "The wait time decreases by half to accelerate recovery: 100ms, 50ms, 25ms, 12.5ms, etc.", "The wait time is recalculated from SRTT after each attempt, ignoring the base RTO value"],
"answers": ["The wait time doubles after each failed attempt: 100ms, 200ms, 400ms, 800ms, etc., up to a maximum cap"]
}
!!!

!!! quiz
{
"title": "Question 24",
"question": "What is the primary danger of immediately retransmitting all pending reliable messages after detecting loss, rather than pacing them across multiple send intervals?",
"options": ["The receiver does not have enough buffer space to handle multiple retransmissions in rapid succession", "The application layer will reject payloads that arrive too close together in time", "Pacing actually takes only a few milliseconds longer than bursting, so the performance difference is negligible", "All pending messages create a bandwidth spike that can overflow router buffers, causing additional loss and latency spikes for all traffic sharing that path"],
"answers": ["All pending messages create a bandwidth spike that can overflow router buffers, causing additional loss and latency spikes for all traffic sharing that path"]
}
!!!

!!! quiz
{
"title": "Question 25",
"question": "Using NACK inference with ACK bitfields to detect loss, why require that 3-4 subsequent packets be acknowledged before declaring an earlier packet lost?",
"options": ["To ensure the remote side has received the maximum amount of data before any retransmission is triggered", "To allow sufficient time for the original packet to complete transmission over very slow network links", "Because packet reordering is common in some networks (WiFi, mobile, multi-path routing), and requiring multiple confirmations tolerates mild reordering while still detecting genuine loss faster than timeouts", "To avoid retransmitting too frequently and wasting precious bandwidth on a potentially congested path"],
"answers": ["Because packet reordering is common in some networks (WiFi, mobile, multi-path routing), and requiring multiple confirmations tolerates mild reordering while still detecting genuine loss faster than timeouts"]
}
!!!

---

## Part 6: Congestion, Pacing, and Fairness Under Load

!!! quiz
{
"title": "Question 26",
"question": "According to the congestion chain, what is the correct sequence of events when a sender's rate exceeds link capacity?",
"options": ["Queue grows and latency rises before packet loss occurs", "Packet loss happens immediately without any delay increase", "All router buffers fill simultaneously, causing instant congestion", "Latency spikes only occur after the queue has been empty for a timeout period"],
"answers": ["Queue grows and latency rises before packet loss occurs"]
}
!!!

!!! quiz
{
"title": "Question 27",
"question": "Using AIMD rate control with α = 0.5 pkt/s increase and β = 0.7 decrease factor, starting at a send rate of 20 pkt/s, what is the rate after one second of no congestion signals?",
"options": ["19.5 pkt/s", "20.0 pkt/s", "20.35 pkt/s", "20.5 pkt/s"],
"answers": ["20.5 pkt/s"]
}
!!!

!!! quiz
{
"title": "Question 28",
"question": "A game server has 60 clients and sends state updates every 50ms. Using the pacing stagger formula stagger(i) = i × T_tick / N_clients, what is the approximate time gap between the send for client 0 and client 1?",
"options": ["Exactly 1.0ms", "Approximately 0.65ms", "Approximately 0.83ms", "Approximately 50ms"],
"answers": ["Approximately 0.83ms"]
}
!!!

!!! quiz
{
"title": "Question 29",
"question": "Bufferbloat is a problem in modern networks. Why is this phenomenon particularly harmful for real-time game applications?",
"options": ["Oversized buffers eliminate packet loss entirely, creating false confidence in path quality", "Massive buffer delays (500ms+) spike latency unpredictably even though packets arrive, making games unplayable", "It causes TCP flows to consume all available bandwidth, starving game traffic", "Bufferbloat only affects wired connections, not WiFi and mobile links"],
"answers": ["Massive buffer delays (500ms+) spike latency unpredictably even though packets arrive, making games unplayable"]
}
!!!

!!! quiz
{
"title": "Question 30",
"question": "UDP-based game protocols must implement congestion control and back off during network stress. Which of the following best explains the rationale?",
"options": ["Game protocols need to prevent other game instances from sharing the same network path", "Routers automatically prioritize game traffic over TCP flows like web browsing and streaming", "Congestion control is only necessary for multiplayer games with over 100 concurrent players", "Without congestion response, the game unfairly consumes bandwidth needed for the player's own TCP applications (browsing, video calls), degrading their usability"],
"answers": ["Without congestion response, the game unfairly consumes bandwidth needed for the player's own TCP applications (browsing, video calls), degrading their usability"]
}
!!!

---

## Part 7: Packet Budgets, Prioritization, and Degradation Strategy

!!! quiz
{
"title": "Question 31",
"question": "A game server had a 50 KB/s client budget at 20 Hz, resulting in a 2,460-byte payload budget (with 40B overhead). The congestion controller now reduces send rate to 10 Hz while maintaining the same 50 KB/s bandwidth cap. What is the approximate new payload budget per tick?",
"options": ["1,230 bytes", "2,460 bytes", "4,920 bytes", "4,960 bytes"],
"answers": ["4,960 bytes"]
}
!!!

!!! quiz
{
"title": "Question 32",
"question": "Why does best practice recommend limiting UDP payloads to approximately 1,200 bytes instead of using the full 1,472 bytes available in standard Ethernet packets?",
"options": ["To maintain consistent packet size across all network paths", "To account for MTU variations on Internet paths that may be smaller than 1,500 bytes due to tunneling or VPN", "To reduce the computational cost of serializing large packets", "To allow room for protocol version upgrades in the future"],
"answers": ["To account for MTU variations on Internet paths that may be smaller than 1,500 bytes due to tunneling or VPN"]
}
!!!

!!! quiz
{
"title": "Question 33",
"question": "A nearby entity with base priority 100 was sent last tick. A distant entity with base priority 30 was last sent 5 ticks ago, with accumulation rate k = 5 per tick. Which entity should be prioritized for sending in the current tick?",
"options": ["The nearby entity (effective priority ~105) because it remains higher than the distant entity (~55)", "The distant entity because it has lower base priority and needs to catch up over time", "Either entity because the priority difference is negligible", "The server should defer both until the next tick to save bandwidth"],
"answers": ["The nearby entity (effective priority ~105) because it remains higher than the distant entity (~55)"]
}
!!!

!!! quiz
{
"title": "Question 34",
"question": "An entity state update is currently 48 bytes using float32 for all components. After applying quantization with reduced precision appropriate for distant entities, the size drops to 18 bytes. Approximately what compression ratio was achieved?",
"options": ["1.5× compression", "2.0× compression", "2.67× compression", "3.5× compression"],
"answers": ["2.67× compression"]
}
!!!

!!! quiz
{
"title": "Question 35",
"question": "When using delta encoding for state transmission, why is it critical that deltas are always computed against the client's last acknowledged baseline?",
"options": ["If the delta base refers to a state the client never received, the delta is meaningless and will cause desynchronization", "To ensure the size of delta-encoded updates never exceeds the size of absolute updates", "To reduce the overhead of the ACK protocol itself", "To allow compression algorithms to work more effectively on delta values"],
"answers": ["If the delta base refers to a state the client never received, the delta is meaningless and will cause desynchronization"]
}
!!!

---

## Part 8: CSI vs GPR Decision Patterns

!!! quiz
{
"title": "Question 36",
"question": "In a competitive FPS with 40 KB/s (40,000 B/s) available bandwidth per client, 350B average payload, and 40B per-packet overhead, what is the approximate maximum sustainable tick rate using R_max = B_available / (S + H)?",
"options": ["81 Hz", "102 Hz", "145 Hz", "176 Hz"],
"answers": ["102 Hz"]
}
!!!

!!! quiz
{
"title": "Question 37",
"question": "Which statement correctly describes the CSI approach to achieving control-loop stability in networked games?",
"options": ["The gain-delay product (G × τ) must remain below a stability margin (typically 0.5-0.8) to prevent oscillation", "The correction gain should be set as high as possible to minimize player-perceived latency", "Stability is achieved by maximizing the tick rate and minimizing interpolation buffer size", "The stability margin increases proportionally with round-trip time to accommodate higher variance"],
"answers": ["The gain-delay product (G × τ) must remain below a stability margin (typically 0.5-0.8) to prevent oscillation"]
}
!!!

!!! quiz
{
"title": "Question 38",
"question": "A game developer observes that unreliable delivery works for entity position updates but not for kill events. According to the CSI-GPR reconciliation framework, why is this distinction valid?",
"options": ["Position messages can be ordered, but kill events must be sequenced for fairness", "Unreliable delivery is acceptable for position updates only when RTT exceeds 100ms", "Kill events occur more frequently than position updates, so they require higher reliability budgets", "Position updates are idempotent overwrites that eventually converge via later updates; kill events are one-time mutations that cannot be recovered by future state snapshots"],
"answers": ["Position updates are idempotent overwrites that eventually converge via later updates; kill events are one-time mutations that cannot be recovered by future state snapshots"]
}
!!!

!!! quiz
{
"title": "Question 39",
"question": "Which pair of hysteresis thresholds correctly implements the principle that systems should degrade quickly but recover slowly?",
"options": ["Degrade at 60 ticks, recover at 30 ticks (both equal)", "Degrade at 50 ticks, recover at 50 ticks (synchronized)", "Degrade at 30 ticks, recover at 60 ticks (recover 2× slower)", "Degrade at 80 ticks, recover at 40 ticks (degrade slower to be conservative)"],
"answers": ["Degrade at 30 ticks, recover at 60 ticks (recover 2× slower)"]
}
!!!

!!! quiz
{
"title": "Question 40",
"question": "A player with 150ms RTT fires at an enemy moving at 6 m/s. The interpolation buffer is 60ms and the server tick interval is 15ms. Using E_max ≈ v × (T_interp + RTT/2 + T_tick), approximately what is the maximum hit registration error?",
"options": ["0.30m", "0.90m", "1.20m", "1.80m"],
"answers": ["0.90m"]
}
!!!
