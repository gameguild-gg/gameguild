## Congestion Control

While flow control protects the receiver, **congestion control** protects the network from being overwhelmed.

### Congestion Window (cwnd)

The sender maintains a **congestion window** that limits how much data can be in flight:

```
Effective Window = min(cwnd, receiver_advertised_window)
```

### Slow Start

Despite its name, slow start grows **exponentially**:

1. Start with cwnd = 1 MSS (Maximum Segment Size)
2. For each ACK received, increase cwnd by 1 MSS
3. This doubles cwnd every RTT

![Slow Start Graph](https://quickchart.io/chart?c=%7B%22type%22%3A%22line%22%2C%22data%22%3A%7B%22labels%22%3A%5B%22RTT%200%22%2C%22RTT%201%22%2C%22RTT%202%22%2C%22RTT%203%22%2C%22RTT%204%22%2C%22RTT%205%22%5D%2C%22datasets%22%3A%5B%7B%22label%22%3A%22cwnd%20%28segments%29%22%2C%22data%22%3A%5B1%2C2%2C4%2C8%2C16%2C32%5D%2C%22fill%22%3Afalse%2C%22borderColor%22%3A%22rgb%2875%2C%20192%2C%20192%29%22%2C%22tension%22%3A0%7D%5D%7D%2C%22options%22%3A%7B%22title%22%3A%7B%22display%22%3Atrue%2C%22text%22%3A%22TCP%20Slow%20Start%20%28Exponential%20Growth%29%22%7D%2C%22scales%22%3A%7B%22yAxes%22%3A%5B%7B%22scaleLabel%22%3A%7B%22display%22%3Atrue%2C%22labelString%22%3A%22Congestion%20Window%20%28segments%29%22%7D%7D%5D%7D%7D%7D)

Slow start continues until:

- cwnd reaches **ssthresh** (slow start threshold)
- Packet loss is detected

### Congestion Avoidance (AIMD)

After cwnd reaches ssthresh, TCP switches to **Additive Increase, Multiplicative Decrease (AIMD)**:

- **Additive Increase**: Increase cwnd by ~1 MSS per RTT (linear growth)
- **Multiplicative Decrease**: On packet loss, cut cwnd in half

This creates the characteristic "sawtooth" pattern:

![AIMD Sawtooth](https://quickchart.io/chart?c=%7B%22type%22%3A%22line%22%2C%22data%22%3A%7B%22labels%22%3A%5B%220%22%2C%221%22%2C%222%22%2C%223%22%2C%224%22%2C%225%22%2C%226%22%2C%227%22%2C%228%22%2C%229%22%2C%2210%22%2C%2211%22%2C%2212%22%2C%2213%22%2C%2214%22%2C%2215%22%2C%2216%22%2C%2217%22%2C%2218%22%2C%2219%22%2C%2220%22%5D%2C%22datasets%22%3A%5B%7B%22label%22%3A%22cwnd%22%2C%22data%22%3A%5B1%2C2%2C4%2C8%2C16%2C17%2C18%2C19%2C20%2C21%2C22%2C11%2C12%2C13%2C14%2C15%2C16%2C17%2C18%2C9%2C10%5D%2C%22fill%22%3Afalse%2C%22borderColor%22%3A%22rgb%28255%2C%2099%2C%20132%29%22%2C%22tension%22%3A0%7D%2C%7B%22label%22%3A%22ssthresh%22%2C%22data%22%3A%5B64%2C64%2C64%2C64%2C16%2C16%2C16%2C16%2C16%2C16%2C16%2C11%2C11%2C11%2C11%2C11%2C11%2C11%2C11%2C9%2C9%5D%2C%22fill%22%3Afalse%2C%22borderColor%22%3A%22rgb%2854%2C%20162%2C%20235%29%22%2C%22borderDash%22%3A%5B5%2C5%5D%2C%22tension%22%3A0%7D%5D%7D%2C%22options%22%3A%7B%22title%22%3A%7B%22display%22%3Atrue%2C%22text%22%3A%22TCP%20Congestion%20Control%20%28AIMD%20Sawtooth%29%22%7D%2C%22scales%22%3A%7B%22xAxes%22%3A%5B%7B%22scaleLabel%22%3A%7B%22display%22%3Atrue%2C%22labelString%22%3A%22Time%20%28RTT%29%22%7D%7D%5D%2C%22yAxes%22%3A%5B%7B%22scaleLabel%22%3A%7B%22display%22%3Atrue%2C%22labelString%22%3A%22Window%20Size%20%28segments%29%22%7D%7D%5D%7D%7D%7D)

### Response to Packet Loss

| Event            | Action                                                                            |
| ---------------- | --------------------------------------------------------------------------------- |
| Timeout          | ssthresh = cwnd/2, cwnd = 1, restart slow start                                   |
| 3 Duplicate ACKs | ssthresh = cwnd/2, cwnd = ssthresh, continue congestion avoidance (fast recovery) |

**Example:** If cwnd = 12 and timeout occurs:

- New ssthresh = 12 ÷ 2 = 6
- New cwnd = 1
- TCP restarts slow start until cwnd reaches ssthresh (6)
