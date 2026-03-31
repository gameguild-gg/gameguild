# Lecture 12: Performance, Reliability, and Packet Budgets

## Overview

This lecture covers how to design networked systems that stay responsive under real-world conditions by balancing **latency, jitter, packet loss, simulation rate, and bandwidth limits**.

We’ll move from measurement fundamentals (what players actually feel) into transport and update strategy: reliable UDP patterns, acknowledgment schemes, retransmission timing, pacing/congestion behavior, and packet-budget prioritization. We’ll also frame decisions differently for **CSI (systems/update intervals/control loops)** and **GPR (tick rates/game feel/fairness)**.

---

## Lecture Sections

This lecture is divided into the following sections for easier navigation:

### 1. Measuring the Right Signals: Latency, Jitter, and Packet Loss

Establish a measurement-first mindset:

- Latency vs RTT vs throughput (not interchangeable)
- Jitter as delay variation (not just “high ping”)
- Packet loss as both reliability and congestion signal
- Why averages hide player-visible pain (tail latency/jitter spread)
- Instrumentation basics (sample windows, smoothing, and confidence)

### 2. Tick Rate and Simulation Frequency as Budget Decisions

Simulation frequency is not just a quality knob — it is a budget tradeoff:

- Higher tick/update rates improve freshness but increase send pressure
- Lower rates reduce bandwidth but require stronger interpolation/correction strategies
- CSI framing: update intervals and control-loop stability
- GPR framing: server tick rates, responsiveness, and hit-reg trust
- Aligning tick, snapshot cadence, and interpolation delay as one system

### 3. Interpolation, Jitter Buffers, and Player-Visible Smoothness

How clients turn irregular packet arrivals into smooth presentation:

- Interpolation buffers as jitter absorbers
- Delay-for-smoothness tradeoff
- Handling packet clumping and occasional loss without hitching
- Why extrapolation works in some motion models and fails in others
- Choosing interpolation window based on send rate and loss environment

### 4. Reliable UDP Fundamentals: Sequence Numbers, ACKs, and Selective Reliability

Reliable UDP is about selective guarantees, not TCP emulation:

- Sequence numbers for ordering and freshness checks
- ACK + ACK-bitfield patterns for robust packet-level acknowledgment
- Message-level reliability on top of packet acks
- Reliable-ordered vs unreliable classes by gameplay/system importance
- Designing for partial reliability instead of one global reliability mode

### 5. Retransmission and Loss Detection Strategy

How to detect and respond to loss without overreacting:

- Time-based loss detection and ambiguity tradeoffs
- RTT estimation and retransmission timeout (RTO/PTO) intuition
- Spurious retransmit risks vs delayed recovery risks
- Exponential backoff and safety under uncertain conditions
- Why “resend everything immediately” destabilizes both latency and fairness

### 6. Congestion, Pacing, and Fairness Under Load

Performance is constrained by path behavior, not just local code:

- Congestion response principles for UDP-based systems
- Pacing vs burst sending
- Send-rate adaptation from measured path signals
- TCP-friendliness and coexistence concerns
- Throughput vs latency fairness tradeoffs at system level

### 7. Packet Budgets, Prioritization, and Degradation Strategy

What to send first when bandwidth is constrained:

- Per-packet budget planning (bytes per tick/update)
- Priority classes (inputs/events/state/chat/telemetry)
- Priority accumulation and starvation prevention
- Delta/quantization/compression as budget multipliers
- Graceful degradation: drop or defer low-priority updates first

### 8. CSI vs GPR Decision Patterns

Applying the same principles to different goals:

- **CSI:** stable update intervals, measurable control loops, protocol correctness under loss
- **GPR:** player feel, correction smoothness, fairness perception, and hit confidence
- Choosing reliability classes per message type
- Choosing tick/update rates based on both path quality and UX targets
- Building feedback loops that tune send behavior over time

---

## Quick Reference

| Topic                       | Key Takeaway                                                                        |
| --------------------------- | ----------------------------------------------------------------------------------- |
| Latency vs jitter vs loss   | Measure separately; each demands different mitigation                               |
| Tick rate / update interval | Higher freshness costs bandwidth and packet pressure                                |
| Interpolation buffer        | Trades delay for smoothness and jitter resistance                                   |
| Reliable UDP                | Use selective reliability classes, not one-size-fits-all retransmission             |
| ACK strategy                | Packet-level ACK + bitfield gives robust feedback under loss                        |
| Retransmission timing       | Too aggressive causes spurious retransmits and congestion                           |
| Congestion and pacing       | Pace sends and adapt rate from path signals to avoid self-inflicted latency         |
| Packet budget management    | Prioritize gameplay-critical data first; defer/omit low-priority updates under load |
| CSI framing                 | Optimize control-loop stability and protocol behavior under uncertainty             |
| GPR framing                 | Optimize responsiveness, smooth corrections, and perceived fairness                 |
