# Week 12 Readings: Performance, Reliability, and Packet Budgets

::: tip "How to approach these readings"

This week is about **measuring what players actually feel** (latency, jitter, loss), then designing a transport/update strategy that stays responsive under imperfect networks. Read in order: first build the measurement mental model, then study simulation-rate tradeoffs, then reliability-over-UDP patterns, and finally bandwidth prioritization.

:::

| #   | Reading / Watching                                                                                                                                    | Time   | Covers                                                                                                                              |
| --- | ----------------------------------------------------------------------------------------------------------------------------------------------------- | ------ | ----------------------------------------------------------------------------------------------------------------------------------- |
| 1   | Cloudflare, ["What is latency?"](https://www.cloudflare.com/learning/performance/glossary/what-is-latency/)                                           | 12 min | Latency vs RTT vs throughput/bandwidth, path distance effects, and why low latency is a UX constraint                               |
| 2   | Gaffer On Games, ["Fix Your Timestep!"](https://gafferongames.com/post/fix_your_timestep/)                                                            | 18 min | Simulation/update-rate design, fixed vs variable timestep, and stability/performance tradeoffs tied to tick rate choices            |
| 3   | Gabriel Gambetta, ["Entity Interpolation"](https://www.gabrielgambetta.com/entity-interpolation.html)                                                 | 20 min | Jitter handling, interpolation buffers, update-rate smoothing, and packet-loss-tolerant presentation                                |
| 4   | Gaffer On Games, ["Reliable Ordered Messages"](https://gafferongames.com/post/reliable_ordered_messages/)                                             | 20 min | Reliable-UDP design primitives: sequence numbers, ACKs, selective reliability, retransmission strategy, and send-rate control       |
| 5   | IETF RFC 9002, ["QUIC Loss Detection and Congestion Control"](https://datatracker.ietf.org/doc/html/rfc9002) (focus on sections 5, 6, and 7)          | 20 min | RTT estimation, loss detection thresholds, probe timeout (PTO), congestion window behavior, pacing, and persistent congestion       |
| 6   | IETF RFC 8085, ["UDP Usage Guidelines"](https://datatracker.ietf.org/doc/html/rfc8085) (focus on 3.1, 3.1.6, 3.3, and 7 summary)                      | 15 min | Congestion-control requirements for UDP apps, burst mitigation/pacing, retransmission implications, and practical fairness guidance |
| 7   | Glenn Fiedler, ["XDP for Game Programmers"](https://mas-bandwidth.com/xdp-for-game-programmers) (skim intro + architecture sections)                  | 8 min  | Real production perspective on packet processing latency budgets and transport design tradeoffs                                     |
| 8   | Video, Bungie GDC: ["I Shot You First: Networking the Gameplay of Halo: Reach"](https://www.youtube.com/watch?v=h47zZrqjgLc) (watch selected segment) | 7 min  | Hit validation, lag compensation tradeoffs, and practical fairness under packet delay/loss                                          |

**Total required reading/watching time: ~120 minutes (~2 hours)**

---

## Cross-Track Focus (CSI vs GPR)

- **CSI-275 focus:**
  - Build a measurement-first model (RTT sample quality, jitter/variation, loss signals)
  - Compare reliability strategies (ack windows, resend timing, congestion responses)
  - Reason about system-level tradeoffs: throughput vs latency, fairness vs aggressiveness, and control-loop stability

- **GPR-430 focus:**
  - Translate metrics to feel: input responsiveness, correction smoothness, and hit-registration trust
  - Balance simulation tick, snapshot rate, interpolation delay, and packet budget per entity/event
  - Prioritize gameplay-critical data first (inputs, hit events, state deltas) and degrade gracefully under loss

---

## Optional Deep Dive

- Boost.Asio Reference: [`ip::udp::socket`](https://www.boost.org/doc/libs/latest/doc/html/boost_asio/reference/ip__udp/socket.html) + [`steady_timer`](https://www.boost.org/doc/libs/latest/doc/html/boost_asio/reference/steady_timer.html) — practical building blocks for resend timers and paced send loops
- .NET Docs: [`Stopwatch`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.stopwatch) — high-resolution timing for RTT and jitter instrumentation
- .NET Docs: [`Socket.SendTo`](https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.socket.sendto) + [`Socket.ReceiveFrom`](https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.socket.receivefrom) — low-level UDP send/receive control points
- IETF RFC 3550 §6.4.4, ["Analyzing Sender and Receiver Reports"](https://datatracker.ietf.org/doc/html/rfc3550#section-6.4.4) — practical jitter/loss analysis and round-trip interpretation
- Glenn Fiedler, ["Networked Physics"](https://gafferongames.com/post/networked_physics_2004/) — authoritative simulation synchronization tradeoffs

---

## Study Tips

::: warning "What to pay attention to"

1. **Keep metrics separate:** latency is average delay, jitter is delay variation, loss is delivery failure rate; each needs a different mitigation strategy.
2. **Tick/update is a budget decision:** higher rates improve freshness but increase bandwidth and packet-pressure risk.
3. **Reliability is selective, not binary:** not every message should be resent; reliability classes should match gameplay/system importance.
4. **Retransmission timing can hurt if naive:** too fast amplifies congestion, too slow hurts responsiveness.
5. **Prioritization beats raw throughput:** send the most player-visible data first when constrained.

:::

**Recommended reading order:**

1. Cloudflare latency primer
2. Gaffer "Fix Your Timestep!"
3. Gaffer "Snapshot Interpolation"
4. Gaffer "Reliability over UDP"
5. RFC 9002 (selected sections)
6. RFC 8085 (selected sections)
7. Overwatch talk segment
8. Halo Reach talk segment

**Common mistakes to avoid:**

- Treating packet loss as only a bandwidth issue instead of a gameplay-correction and reliability-class issue
- Using one global resend policy for all message types (inputs, events, snapshots, chat, etc.)
- Cranking tick rates without reevaluating interpolation windows and packet budgets
- Ignoring pacing and burst behavior, then blaming random jitter for self-inflicted congestion
- Measuring only average ping while ignoring jitter spread and tail latency
