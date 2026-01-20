# Week 02: Network Addressing

---

## Part 1: IP Addressing (Tuesday)

### Why This Matters for Networked Games

Every packet your game sends needs a destination. Every server your players connect to has an address. Understanding IP addressing lets you:

- Configure game servers correctly
- Debug "can't connect" issues
- Design network architectures that scale
- Understand NAT traversal

---

### IPv4 Addresses

An IPv4 address is **32 bits** written as four decimal octets:

```
192.168.1.100
```

Each octet = 8 bits = 0–255

**In code:**

```cpp
// IPv4 address as a 32-bit integer
uint32_t ip = (192 << 24) | (168 << 16) | (1 << 8) | 100;
// ip = 0xC0A80164 = 3232235876
```

**Special addresses you'll use constantly:**

| Address           | Purpose                         |
| ----------------- | ------------------------------- |
| `127.0.0.1`       | Localhost (this machine)        |
| `0.0.0.0`         | "Any" address (server bind)     |
| `255.255.255.255` | Broadcast                       |
| `192.168.x.x`     | Private LAN (your home network) |
| `10.x.x.x`        | Private LAN (corporate/cloud)   |

::: note

Most of your testing happens on `127.0.0.1`. Your server binds to `0.0.0.0` to accept connections on any interface.

:::

## Practice Activity 1: IP Address Conversion

Convert decimal octets to their binary representation and back. Understanding bit patterns is essential for subnetting.

!!! code
{
"description": "Convert an IPv4 octet (0-255) to its 8-bit binary representation. For example, 192 should return '11000000'",
"language": "python",
"code": "def octet_to_binary(octet):\n # Convert the octet to 8-bit binary string\n # Your code here\n pass\n\n# do not modify the code below\nprint(octet_to_binary(192))\nprint(octet_to_binary(255))\nprint(octet_to_binary(1))",
"expectedOutput": "11000000\n11111111\n00000001"
}
!!!

---

### IPv6 Addresses

IPv4 has ~4.3 billion addresses. We ran out. IPv6 fixes this with **128 bits**:

```
2001:0db8:85a3:0000:0000:8a2e:0370:7334
```

**Shorthand rules:**

1. Drop leading zeros: `0db8` → `db8`
2. Collapse consecutive zero groups with `::` (once only)

```
2001:db8:85a3::8a2e:370:7334
```

**Localhost in IPv6:** `::1`

**In sockets code:**

```cpp
// IPv4
struct sockaddr_in addr4;
addr4.sin_family = AF_INET;
inet_pton(AF_INET, "192.168.1.100", &addr4.sin_addr);

// IPv6
struct sockaddr_in6 addr6;
addr6.sin6_family = AF_INET6;
inet_pton(AF_INET6, "::1", &addr6.sin6_addr);
```

**Game dev reality:** Most games still use IPv4. Xbox Live and PlayStation Network handle IPv6 internally. You'll encounter it in cloud deployments.

---

### Subnets: Dividing the Address Space

Every IP address has two parts:

```
[  Network portion  ][  Host portion  ]
```

**The subnet mask** tells you where the split happens.

| Subnet Mask       | Binary                                | Network bits |
| ----------------- | ------------------------------------- | ------------ |
| `255.0.0.0`       | `11111111.00000000.00000000.00000000` | 8            |
| `255.255.0.0`     | `11111111.11111111.00000000.00000000` | 16           |
| `255.255.255.0`   | `11111111.11111111.11111111.00000000` | 24           |
| `255.255.255.240` | `11111111.11111111.11111111.11110000` | 28           |

**To find the network address:** AND the IP with the mask

```cpp
uint32_t ip   = 0xC0A80164;  // 192.168.1.100
uint32_t mask = 0xFFFFFF00;  // 255.255.255.0
uint32_t network = ip & mask; // 192.168.1.0
```

---

## Practice Activity 2: Subnet Mask Calculation

Given a CIDR prefix length, calculate the corresponding subnet mask in dotted decimal notation.

!!! code
{
"description": "Convert a CIDR prefix length (e.g., 24) to a subnet mask string (e.g., '255.255.255.0'). The first 'prefix' bits should be 1, the rest 0.",
"language": "python",
"code": "def cidr_to_mask(prefix):\n # Create a 32-bit number with 'prefix' ones followed by zeros\n # Return as dotted decimal string\n # Example: cidr_to_mask(24) should return '255.255.255.0'\n pass\n\n# do not modify the code below\nprint(cidr_to_mask(8))\nprint(cidr_to_mask(16))\nprint(cidr_to_mask(24))\nprint(cidr_to_mask(26))",
"expectedOutput": "255.0.0.0\n255.255.0.0\n255.255.255.0\n255.255.255.192"
}
!!!

---

### CIDR Notation

**Classless Inter-Domain Routing** - a compact way to write subnet masks.

```
192.168.1.0/24
```

The `/24` means "24 bits for network, 8 bits for hosts."

| CIDR  | Subnet Mask       | Hosts Available |
| ----- | ----------------- | --------------- |
| `/8`  | `255.0.0.0`       | 16,777,214      |
| `/16` | `255.255.0.0`     | 65,534          |
| `/24` | `255.255.255.0`   | 254             |
| `/28` | `255.255.255.240` | 14              |
| `/30` | `255.255.255.252` | 2               |
| `/32` | `255.255.255.255` | 1 (single host) |

**Formula:** Hosts = 2^(32 - prefix) - 2

The `-2` accounts for network address (all zeros) and broadcast address (all ones).

---

## Practice Activity 3: Network Address Calculation

Apply a subnet mask to an IP address using bitwise AND to find the network address.

!!! code
{
"description": "Given an IP address string and a CIDR prefix, calculate the network address. For example, '192.168.1.100/24' should return '192.168.1.0'",
"language": "python",
"code": "def get_network_address(ip_str, prefix):\n # Parse the IP address string into octets\n # Apply bitwise AND with the subnet mask\n # Return the network address as a dotted decimal string\n pass\n\n# do not modify the code below\nprint(get_network_address('192.168.1.100', 24))\nprint(get_network_address('192.168.1.100', 26))\nprint(get_network_address('10.20.30.40', 16))",
"expectedOutput": "192.168.1.0\n192.168.1.64\n10.20.0.0"
}
!!!

---

### Subnetting Calculations (Assignment 02 Preview)

Given `192.168.100.50/26`, find:

**Step 1: Subnet mask**

- `/26` = 26 ones followed by 6 zeros
- `11111111.11111111.11111111.11000000` = `255.255.255.192`

**Step 2: Network address**

- IP in binary: `192.168.100.00110010`
- Mask: `255.255.255.11000000`
- AND result: `192.168.100.00000000` = `192.168.100.0`

**Step 3: Broadcast address**

- Set all host bits to 1: `192.168.100.00111111` = `192.168.100.63`

**Step 4: Host range**

- First host: `192.168.100.1`
- Last host: `192.168.100.62`
- Total hosts: 2^6 - 2 = 62

---

### Quick Reference: Powers of 2

Memorize this for subnetting speed:

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

## Practice Activity 4: Count Usable Hosts

Calculate how many usable host addresses exist in a given subnet.

!!! code
{
"description": "Given a CIDR prefix length, calculate the number of usable host addresses. Use the formula: 2^(32 - prefix) - 2",
"language": "python",
"code": "def count_usable_hosts(prefix):\n # Calculate usable hosts: 2^(32 - prefix) - 2\n # The -2 accounts for network and broadcast addresses\n pass\n\n# do not modify the code below\nprint(count_usable_hosts(24))\nprint(count_usable_hosts(26))\nprint(count_usable_hosts(30))\nprint(count_usable_hosts(32))",
"expectedOutput": "254\n62\n2\n0"
}
!!!

---

## Practice Activity 5: Complete Subnet Analysis

Combine everything: given an IP/CIDR, calculate network address, broadcast address, and host range.

!!! code
{
"description": "Analyze a subnet completely. Given an IP and CIDR prefix, return a tuple of (network, first_host, last_host, broadcast). For example, '192.168.100.50/26' should return ('192.168.100.0', '192.168.100.1', '192.168.100.62', '192.168.100.63')",
"language": "python",
"code": "def analyze_subnet(ip_str, prefix):\n # Parse the IP\n # Calculate network address (IP AND mask)\n # Calculate broadcast address (network with all host bits set to 1)\n # Calculate first host (network + 1)\n # Calculate last host (broadcast - 1)\n # Return all four as dotted decimal strings\n pass\n\n# do not modify the code below\nnetwork, first, last, broadcast = analyze_subnet('192.168.100.50', 26)\nprint(f\"{network},{first},{last},{broadcast}\")\nnetwork, first, last, broadcast = analyze_subnet('10.0.0.130', 25)\nprint(f\"{network},{first},{last},{broadcast}\")",
"expectedOutput": "192.168.100.0,192.168.100.1,192.168.100.62,192.168.100.63\n10.0.0.128,10.0.0.129,10.0.0.254,10.0.0.255"
}
!!!

---

## DNS and Routing

### DNS: The Internet's Phone Book

**Problem:** Humans remember names. Computers use numbers.

**Solution:** Domain Name System - a distributed database mapping names → IPs.

```
game.example.com → 203.0.113.50
```

---

## Practice Activity 6: DNS Name Parsing

Extract the subdomain, domain, and TLD from a fully qualified domain name.

!!! code
{
"description": "Parse a FQDN into its components. For 'game.example.com', return the parts as a tuple. For 'api.servers.example.org', return all parts.",
"language": "python",
"code": "def parse_fqdn(fqdn):\n # Split the FQDN by '.'\n # Return the parts as a tuple\n pass\n\n# do not modify the code below\nprint(parse_fqdn('game.example.com'))\nprint(parse_fqdn('auth.api.example.org'))",
"expectedOutput": "('game', 'example', 'com')\n('auth', 'api', 'example', 'org')"
}
!!!

---

### DNS Resolution Process

When your game client connects to `game.example.com:7777`:

```
1. Client → Local Resolver: "What's game.example.com?"
2. Resolver → Root Server: "Where's .com?"
3. Resolver → .com TLD: "Where's example.com?"
4. Resolver → example.com NS: "Where's game.example.com?"
5. Resolver → Client: "It's 203.0.113.50"
6. Client connects to 203.0.113.50:7777
```

**Caching:** Each step is cached. TTL (Time To Live) controls how long.

---

### DNS Record Types

| Type  | Purpose                       | Example                           |
| ----- | ----------------------------- | --------------------------------- |
| A     | IPv4 address                  | `game.example.com → 203.0.113.50` |
| AAAA  | IPv6 address                  | `game.example.com → 2001:db8::1`  |
| CNAME | Alias to another name         | `www → game.example.com`          |
| MX    | Mail server                   | `example.com → mail.example.com`  |
| TXT   | Arbitrary text (verification) | SPF, DKIM records                 |
| NS    | Nameserver for domain         | `example.com → ns1.example.com`   |

---

### DNS in Code

**Using `getaddrinfo()` (preferred, protocol-agnostic):**

```cpp
#include <netdb.h>

struct addrinfo hints{}, *result;
hints.ai_family = AF_UNSPEC;     // IPv4 or IPv6
hints.ai_socktype = SOCK_STREAM; // TCP

int status = getaddrinfo("game.example.com", "7777", &hints, &result);
if (status != 0) {
    std::cerr << "DNS failed: " << gai_strerror(status) << "\n";
    return;
}

// result is a linked list of addresses (may have multiple)
for (auto* p = result; p != nullptr; p = p->ai_next) {
    int sock = socket(p->ai_family, p->ai_socktype, p->ai_protocol);
    if (connect(sock, p->ai_addr, p->ai_addrlen) == 0) {
        // Connected!
        break;
    }
    close(sock);
}

freeaddrinfo(result);
```

**Game dev consideration:** DNS resolution is blocking and can take seconds. Always resolve asynchronously or cache results.

---

### Routing Basics

**The question:** How does a packet get from your game client to a server across the world?

**The answer:** Hop by hop, using routing tables.

```
Your PC → Home Router → ISP Router → ... → Data Center → Game Server
```

Each router:

1. Receives packet
2. Looks at destination IP
3. Checks routing table: "Which interface gets this packet closer?"
4. Forwards packet
5. Decrements TTL (Time To Live)

---

### Routing Table Example

Your home router's simplified routing table:

| Destination      | Gateway       | Interface |
| ---------------- | ------------- | --------- |
| `192.168.1.0/24` | `0.0.0.0`     | LAN       |
| `0.0.0.0/0`      | `ISP Gateway` | WAN       |

- Packets for `192.168.1.x` stay on LAN
- Everything else (`0.0.0.0/0` = default route) goes to ISP

**View your routing table:**

```bash
# Linux/macOS
ip route   # or: netstat -rn

# Windows
route print
```

---

### TTL and Traceroute

**TTL (Time To Live):** Counter decremented at each hop. Packet dies at 0.

- Prevents infinite routing loops
- Typically starts at 64 or 128

**Traceroute:** Exploits TTL to discover the path:

**Game dev use:** Diagnosing high latency. "Where is the lag coming from?"

---

### Try It Yourself: Run Traceroute

Let's trace the path from your computer to a real server. Open your terminal and run one of these commands based on your operating system:

**macOS / Linux:**

```bash
traceroute google.com
# or for faster results (3 probes per hop instead of default)
traceroute -q 3 google.com
```

**Windows:**

```cmd
tracert google.com
```

**Expected Output (example from macOS):**

```
traceroute to google.com (142.250.185.46), 64 hops max, 52 byte packets
 1  192.168.1.1 (192.168.1.1)  2.847 ms  1.234 ms  1.189 ms
 2  10.0.0.1 (10.0.0.1)  8.456 ms  7.892 ms  8.234 ms
 3  96.120.88.125 (96.120.88.125)  12.456 ms  11.892 ms  12.123 ms
 4  68.85.221.33 (68.85.221.33)  15.789 ms  14.234 ms  15.567 ms
 5  96.110.177.65 (96.110.177.65)  18.234 ms  17.890 ms  18.456 ms
 6  68.86.143.93 (68.86.143.93)  22.678 ms  21.234 ms  22.890 ms
 7  142.251.61.221 (142.251.61.221)  23.456 ms  22.789 ms  23.234 ms
 8  108.170.252.1 (108.170.252.1)  24.567 ms  23.890 ms  24.234 ms
 9  142.250.185.46 (142.250.185.46)  25.123 ms  24.456 ms  25.789 ms
```

**What you're seeing:**

- **Hop 1:** Your home router/gateway
- **Hop 2-3:** Your ISP's network
- **Hop 4-6:** Transit providers (backbone internet)
- **Hop 7-8:** Google's edge network
- **Hop 9:** Final destination (Google's server)

**Notice:**

- Each hop shows 3 RTT (Round Trip Time) measurements
- Latency increases with distance
- Some hops may show `* * *` (timeouts) - they're configured not to respond to ICMP, but still forward packets

**Try different destinations:**

```bash
# try us!
traceroute gameguild.gg

# Game servers
traceroute steampowered.com
traceroute epicgames.com

# International (notice the higher latency)
traceroute bbc.co.uk      # UK
traceroute www.japan.go.jp       # Japan
```

---

## Part 3: Wireshark Introduction

### What is Wireshark?

A **packet analyzer** that captures and inspects network traffic in real-time.

**Why you need it:**

- Debug protocol issues ("Why isn't my packet arriving?")
- Verify your implementation matches the spec
- Understand how existing protocols work
- Detect network problems (retransmissions, errors)

---

### First Capture

1. **Select interface** (usually your main network adapter)
2. **Start capture** (blue shark fin button)
3. **Generate traffic** (browse a website, run your game)
4. **Stop capture** (red square)
5. **Analyze packets**

---

### The Wireshark Interface

```
┌─────────────────────────────────────────────────────────┐
│ Filter bar: ip.addr == 192.168.1.100                    │
├─────────────────────────────────────────────────────────┤
│ Packet List (summary of each packet)                    │
│   No.  Time      Source        Dest          Protocol   │
│   1    0.000     192.168.1.100 8.8.8.8       DNS       │
│   2    0.045     8.8.8.8       192.168.1.100 DNS       │
├─────────────────────────────────────────────────────────┤
│ Packet Details (protocol tree)                          │
│   ▼ Ethernet II                                         │
│   ▼ Internet Protocol Version 4                         │
│   ▼ User Datagram Protocol                              │
│   ▼ Domain Name System (query)                          │
├─────────────────────────────────────────────────────────┤
│ Packet Bytes (raw hex + ASCII)                          │
│   0000   45 00 00 3c 1c 46 40 00 40 06 ...             │
└─────────────────────────────────────────────────────────┘
```

---

### Essential Display Filters

| Filter                        | Shows                             |
| ----------------------------- | --------------------------------- |
| `ip.addr == 192.168.1.100`    | Traffic to/from this IP           |
| `tcp.port == 7777`            | Traffic on your game port         |
| `udp`                         | All UDP traffic                   |
| `dns`                         | DNS queries and responses         |
| `tcp.analysis.retransmission` | Retransmitted packets (problems!) |
| `frame.len > 1000`            | Large packets                     |

**Combine with `&&` (and), `||` (or), `!` (not):**

```
ip.addr == 192.168.1.100 && tcp.port == 7777 && !dns
```

---

### Capture Filters vs Display Filters

| Type    | When Applied   | Syntax           | Use Case                        |
| ------- | -------------- | ---------------- | ------------------------------- |
| Capture | During capture | BPF syntax       | Reduce file size, focus capture |
| Display | After capture  | Wireshark syntax | Explore existing capture        |

**Capture filter example:**

```
host 192.168.1.100 and port 7777
```

---

### Exercise: Capture DNS Resolution

1. Start Wireshark, select your interface
2. Set display filter: `dns`
3. Open terminal: `nslookup google.com`
4. Stop capture
5. Find:
   - Query packet (your request)
   - Response packet (the answer)
   - The resolved IP address in the response

---

### Following TCP Streams

Right-click a TCP packet → **Follow → TCP Stream**

Shows the entire conversation as readable text:

```
GET / HTTP/1.1
Host: example.com
...

HTTP/1.1 200 OK
Content-Type: text/html
...
```

**Game dev use:** Debug your custom protocol. See exactly what bytes are exchanged.

---

## Summary

| Concept    | Key Point                                             |
| ---------- | ----------------------------------------------------- |
| IPv4       | 32-bit, dotted decimal, running out                   |
| IPv6       | 128-bit, hex with colons, the future                  |
| Subnetting | Network + Host portions, defined by mask              |
| CIDR       | `/24` = prefix length notation                        |
| DNS        | Name → IP mapping, hierarchical, cached               |
| Routing    | Hop-by-hop forwarding using routing tables            |
| Wireshark  | Capture and analyze packets, essential debugging tool |

---

## Coding Assignment 02: IP Calculator

Build a subnet analyzer that takes an IP address with CIDR notation and outputs:

- Network address
- Broadcast address
- First usable host
- Last usable host
- Total usable hosts
- Subnet mask (dotted decimal)

**Example:**

```
Input:  192.168.100.50/26
Output:
  Network:    192.168.100.0
  Broadcast:  192.168.100.63
  First Host: 192.168.100.1
  Last Host:  192.168.100.62
  Hosts:      62
  Mask:       255.255.255.192
```

**Hint:** Use bitwise operations. The mask is `0xFFFFFFFF << (32 - prefix)`.
