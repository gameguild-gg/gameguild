# Week 02: Network Addressing

---

## Why This Matters for Networked Applications

Every packet your application, service, or game sends needs a destination.

- Configure servers and services correctly
- Debug connectivity issues
- Design scalable network architectures
- Understand NAT traversal and address translation

---

## Why This Matters for Game Programmers (GPR)

Every packet your game sends needs a destination.

- Configure game servers correctly
- Debug "can't connect" issues
- Design network architectures that scale
- Understand NAT traversal

---

## Why This Matters for Computer Scientists (CSI)

Every distributed system, cloud service, or client/server app relies on addressing.

- Build robust distributed systems
- Debug network failures in any environment
- Design secure, scalable architectures
- Understand how the Internet routes data

---

## Part 1: IP Addressing

---

## IPv4 Addresses

**32 bits** written as four decimal octets:

```
192.168.1.100
```

Each octet = 8 bits = **0–255**

---

## IPv4 in Code

```cpp
// IPv4 address as a 32-bit integer
uint32_t ip = (192 << 24) | (168 << 16) | (1 << 8) | 100;
// ip = 0xC0A80164 = 3232235876
```

---

## Special IPv4 Addresses

| Address           | Purpose                                |
| ----------------- | -------------------------------------- |
| `127.0.0.1`       | Localhost (this machine)               |
| `0.0.0.0`         | "Any" address (bind to all interfaces) |
| `255.255.255.255` | Broadcast                              |
| `192.168.x.x`     | Private LAN (home/office)              |
| `10.x.x.x`        | Private LAN (corporate/cloud)          |

---

## Key Insight

> Your server binds to `0.0.0.0` to accept connections on any interface

> Most local testing happens on `127.0.0.1`
>
> Servers and services often bind to `0.0.0.0` to accept connections on any interface

---

## IPv6 Addresses

IPv4 has ~4.3 billion addresses. **We ran out.**

IPv6 fixes this with **128 bits**:

```
2001:0db8:85a3:0000:0000:8a2e:0370:7334
```

---

## IPv6 Shorthand Rules

1. Drop leading zeros: `0db8` → `db8`
2. Collapse consecutive zero groups with `::` (once only)

```
2001:db8:85a3::8a2e:370:7334
```

**Localhost in IPv6:** `::1`

---

## IPv4 vs IPv6 in Code

```cpp
#include <boost/asio.hpp>
using boost::asio::ip::address;

// IPv4
auto addr4 = address::from_string("192.168.1.100");

// IPv6
auto addr6 = address::from_string("::1");

// Works with both!
bool is_v6 = addr6.is_v6();  // true
```

---

## Industry Reality

Most applications and games still use IPv4.

Large platforms (cloud, gaming, enterprise) may handle IPv6 internally.

You'll encounter IPv6 in modern deployments and infrastructure.

---

## Part 2: Subnetting

---

## IP Address Structure

Every IP address has two parts:

```
[  Network portion  ][  Host portion  ]
```

**The subnet mask** tells you where the split happens.

---

## Subnet Masks

| Subnet Mask       | Binary                                | Network bits |
| ----------------- | ------------------------------------- | ------------ |
| `255.0.0.0`       | `11111111.00000000.00000000.00000000` | 8            |
| `255.255.0.0`     | `11111111.11111111.00000000.00000000` | 16           |
| `255.255.255.0`   | `11111111.11111111.11111111.00000000` | 24           |
| `255.255.255.240` | `11111111.11111111.11111111.11110000` | 28           |

---

## Finding the Network Address

**AND** the IP with the mask:

```cpp
uint32_t ip   = 0xC0A80164;  // 192.168.1.100
uint32_t mask = 0xFFFFFF00;  // 255.255.255.0
uint32_t network = ip & mask; // 192.168.1.0
```

---

## CIDR Notation

**Classless Inter-Domain Routing** - compact way to write subnet masks.

```
192.168.1.0/24
```

The `/24` means "24 bits for network, 8 bits for hosts"

---

## CIDR Reference Table

| CIDR  | Subnet Mask       | Hosts Available |
| ----- | ----------------- | --------------- |
| `/8`  | `255.0.0.0`       | 16,777,214      |
| `/16` | `255.255.0.0`     | 65,534          |
| `/24` | `255.255.255.0`   | 254             |
| `/28` | `255.255.255.240` | 14              |
| `/30` | `255.255.255.252` | 2               |
| `/31` | `255.255.255.254` | 2 (RFC 3021)    |
| `/32` | `255.255.255.255` | 1 (single host) |

---

## Host Count Formula

$$\text{Hosts} = 2^{(32 - \text{prefix})} - 2$$

The **-2** accounts for:

- Network address (all zeros)
- Broadcast address (all ones)

---

## Subnetting Example

Given `192.168.100.50/26`, find all addresses:

---

## Step 1: Subnet Mask

`/26` = 26 ones followed by 6 zeros

```
11111111.11111111.11111111.11000000
= 255.255.255.192
```

---

## Step 2: Network Address

IP AND mask:

```
192.168.100.00110010  (IP: .50)
255.255.255.11000000  (mask)
─────────────────────
192.168.100.00000000  = 192.168.100.0
```

---

## Step 3: Broadcast Address

Set all host bits to 1:

```
192.168.100.00111111 = 192.168.100.63
```

---

## Step 4: Host Range

- **First host:** `192.168.100.1`
- **Last host:** `192.168.100.62`
- **Total hosts:** $2^6 - 2 = 62$

---

## Powers of 2 Quick Reference

| 2^n | Value | Hosts (/n) |
| --- | ----- | ---------- |
| 2^1 | 2     | /31: 0     |
| 2^2 | 4     | /30: 2     |
| 2^3 | 8     | /29: 6     |
| 2^4 | 16    | /28: 14    |
| 2^5 | 32    | /27: 30    |
| 2^6 | 64    | /26: 62    |
| 2^7 | 128   | /25: 126   |
| 2^8 | 256   | /24: 254   |

---

## Part 3: DNS

---

## DNS: The Internet's Phone Book

**Problem:** Humans remember names. Computers use numbers.

**Solution:** Domain Name System (DNS)

```
service.example.com → 203.0.113.50
```

---

## DNS Resolution Process

```
1. Client → Local Resolver: "What's game.example.com?"
2. Resolver → Root Server: "Where's .com?"
3. Resolver → .com TLD: "Where's example.com?"
4. Resolver → example.com NS: "Where's game.example.com?"
5. Resolver → Client: "It's 203.0.113.50"
6. Client connects to 203.0.113.50:7777
```

---

## DNS Caching

Each step is cached.

**TTL (Time To Live)** controls how long.

Backend services and game servers typically cache DNS resolutions to reduce latency.

---

## DNS Record Types

| Type  | Purpose               | Example                              |
| ----- | --------------------- | ------------------------------------ |
| A     | IPv4 address          | `service.example.com → 203.0.113.50` |
| AAAA  | IPv6 address          | `service.example.com → 2001:db8::1`  |
| CNAME | Alias to another name | `www → service.example.com`          |
| MX    | Mail server           | `example.com → mail.example.com`     |
| NS    | Nameserver for domain | `example.com → ns1.example.com`      |

---

## DNS in Code

```cpp
#include <boost/asio.hpp>
using boost::asio::ip::tcp;

boost::asio::io_context io;
tcp::resolver resolver(io);

auto endpoints = resolver.resolve("service.example.com", "7777");

for (const auto& ep : endpoints) {
    std::cout << ep.endpoint().address() << std::endl;
}
```

---

## Game Dev Consideration

> DNS resolution is **blocking** and can take seconds.
>
> Always resolve **asynchronously** or **cache results**.

---

## Part 4: Routing

---

## How Packets Travel

**The question:** How does a packet get from your client, device, or application to a server across the world?

**The answer:** Hop by hop, using routing tables.

---

## Packet Path

```
Your PC → Home Router → ISP Router → ... → Data Center → Application Server
```

---

## What Each Router Does

1. Receives packet
2. Looks at destination IP
3. Checks routing table: "Which interface gets this packet closer?"
4. Forwards packet
5. Decrements TTL (Time To Live)

---

## Routing Table Example

| Destination      | Gateway       | Interface |
| ---------------- | ------------- | --------- |
| `192.168.1.0/24` | `0.0.0.0`     | LAN       |
| `0.0.0.0/0`      | `ISP Gateway` | WAN       |

- Packets for `192.168.1.x` stay on LAN
- Everything else (`0.0.0.0/0` = default route) goes to ISP

---

## View Your Routing Table

```bash
# Linux/macOS
ip route   # or: netstat -rn

# Windows
route print
```

---

## TTL (Time To Live)

Counter decremented at each hop. Packet dies at 0.

- Prevents infinite routing loops
- Typically starts at **64** or **128**

---

## Traceroute

Exploits TTL to discover the path:

```bash
# macOS / Linux
traceroute google.com

# Windows
tracert google.com
```

Uses: Diagnosing high latency.

---

## Traceroute Output

It should give output like this:

```
1  192.168.1.1      2.8 ms   (Your router)
2  10.0.0.1         8.4 ms   (ISP)
3  96.120.88.125   12.4 ms   (ISP backbone)
4  68.85.221.33    15.7 ms   (Transit)
...
9  142.250.185.46  25.1 ms   (Destination)
```

---

## Reading Traceroute

- Each hop shows 3 RTT measurements
- Latency increases with distance
- `* * *` = router doesn't respond to ICMP, but still forwards packets

---

## Part 5: Wireshark

---

## What is Wireshark?

A **packet analyzer** that captures and inspects network traffic in real-time.

---

## Why You Need Wireshark

- Debug protocol issues ("Why isn't my packet arriving?")
- Verify your implementation matches the spec
- Understand how existing protocols work
- Detect network problems (retransmissions, errors)

---

## First Capture Steps

1. **Select interface** (usually your main network adapter)
2. **Start capture** (blue shark fin button)
3. **Generate traffic** (browse a website, run your game)
4. **Stop capture** (red square)
5. **Analyze packets**

---

## The Wireshark Interface

```
┌────────────────────────────────────────────┐
│ Filter bar: ip.addr == 192.168.1.100       │
├────────────────────────────────────────────┤
│ Packet List (summary)                      │
├────────────────────────────────────────────┤
│ Packet Details (protocol tree)             │
├────────────────────────────────────────────┤
│ Packet Bytes (raw hex + ASCII)             │
└────────────────────────────────────────────┘
```

---

## Essential Display Filters

| Filter                        | Shows                                 |
| ----------------------------- | ------------------------------------- |
| `ip.addr == 192.168.1.100`    | Traffic to/from this IP               |
| `tcp.port == 7777`            | Traffic on your application/game port |
| `udp`                         | All UDP traffic                       |
| `dns`                         | DNS queries and responses             |
| `tcp.analysis.retransmission` | Retransmitted packets                 |

---

## Combining Filters

Use `&&` (and), `||` (or), `!` (not):

```
ip.addr == 192.168.1.100 && tcp.port == 7777 && !dns
```

---

## Capture vs Display Filters

| Type    | When Applied   | Use Case                        |
| ------- | -------------- | ------------------------------- |
| Capture | During capture | Reduce file size, focus capture |
| Display | After capture  | Explore existing capture        |

**Capture filter example:**

```
host 192.168.1.100 and port 7777
```

---

## Following TCP Streams

Right-click a TCP packet → **Follow → TCP Stream**

Shows the entire conversation as readable text.

Use case: Debug your custom protocol.
