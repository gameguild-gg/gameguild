# Week 02: Network Addressing Quiz

---

## Section A: IPv4 & IPv6 Fundamentals

### Question 1: IPv4 Bits (Multiple Choice)

How many bits are in an IPv4 address?

- [ ] A) 16 bits
- [ ] B) 32 bits
- [ ] C) 64 bits
- [ ] D) 128 bits


---

### Question 2: IPv4 Range (Multiple Choice)

What is the minimum and maximum value for a single octet in an IPv4 address?

- [ ] A) 0–100
- [ ] B) 0–128
- [ ] C) 0–255
- [ ] D) 1–254


---

### Question 3: IPv6 Address Length (True/False)

IPv6 addresses are 128 bits long.

- [ ] True
- [ ] False


---

### Question 4: IPv6 Localhost (Fill the Text)

The IPv6 equivalent of `127.0.0.1` (localhost) is: ___________

---

### Question 5: Special IPv4 Addresses (Multiple Choice)

Which address is used when a server wants to accept connections on any available network interface?

- [ ] A) `127.0.0.1`
- [ ] B) `0.0.0.0`
- [ ] C) `255.255.255.255`
- [ ] D) `192.168.1.1`


---

## Section B: Subnetting & CIDR

### Question 6: Subnet Mask Binary (Exact Number)

Convert the subnet mask `255.255.255.0` to CIDR notation. Answer: ___________


---

### Question 7: Network Address Calculation (Multiple Choice)

Given the IP address `192.168.100.50` with subnet mask `255.255.255.0`, what is the network address?

- [ ] A) `192.168.100.0`
- [ ] B) `192.168.100.50`
- [ ] C) `192.168.100.255`
- [ ] D) `192.168.101.0`


---

### Question 8: Broadcast Address (Fill the Text)

For the subnet `192.168.1.0/24`, the broadcast address is: ___________


---

### Question 9: Usable Hosts in /25 (Exact Number)

How many usable host addresses are available in a `/25` subnet?

Answer: ___________


---

### Question 10: CIDR Subnet Size (Multiple Choice)

Which CIDR notation provides the most host addresses?

- [ ] A) `/30`
- [ ] B) `/25`
- [ ] C) `/16`
- [ ] D) `/8`


---

### Question 11: Private IP Ranges (True/False)

The range `172.16.0.0` to `172.31.255.255` is reserved for private networks.

- [ ] True
- [ ] False


---

### Question 12: Subnet Mask Binary (Exact Number)

Convert `/26` to a dotted decimal subnet mask: ___________


---

## Section C: DNS

### Question 13: DNS Purpose (Multiple Choice)

What is the primary function of DNS?

- [ ] A) Route packets between routers
- [ ] B) Encrypt network traffic
- [ ] C) Map domain names to IP addresses
- [ ] D) Manage server hardware


---

### Question 14: DNS Query Type (Fill the Text)

A DNS record type that maps a domain name to an IPv4 address is called an: `A record` (or just `A`)

---

### Question 15: DNS TTL (Multiple Choice)

What does TTL stand for in DNS?

- [ ] A) Transmission Time Limit
- [ ] B) Time To Live
- [ ] C) Transfer Table Layer
- [ ] D) TCP Time Log


---

### Question 16: FQDN Components (Fill the Text)

In the FQDN `api.example.com`, the subdomain is `api`, the domain is `example`, and the TLD is `com`

---

## Section D: Routing & Traceroute

### Question 17: TTL Definition (True/False)

TTL (Time To Live) prevents infinite routing loops by decrementing at each hop.

- [ ] True
- [ ] False


---

### Question 18: Traceroute Purpose (Multiple Choice)

What does the `traceroute` command do?

- [ ] A) Measures packet loss
- [ ] B) Discovers the path packets take to reach a destination
- [ ] C) Encrypts traffic between hops
- [ ] D) Blocks packets at firewalls


---

### Question 19: Typical Initial TTL (Multiple Choice)

When a packet leaves your computer, what is a typical starting TTL value?

- [ ] A) 1
- [ ] B) 32
- [ ] C) 64 or 128
- [ ] D) 256


---

### Question 20: Routing Table Default Route (Fill the Text)

The default route in a routing table that matches all destinations is: ___________


---

## Section E: Wireshark

### Question 21: Wireshark Function (Multiple Choice)

What is Wireshark primarily used for?

- [ ] A) Writing network code
- [ ] Capturing and analyzing network packets
- [ ] C) Managing DNS servers
- [ ] D) Configuring firewall rules


---

### Question 22: Display Filter (Fill the Text)

To filter Wireshark to show only DNS traffic, use the display filter: ___________

---

### Question 23: Wireshark TCP Stream (True/False)

You can view the human-readable content of a TCP conversation in Wireshark using "Follow TCP Stream."

- [ ] True
- [ ] False


---

### Question 24: Capture vs Display Filter (Multiple Choice)

Which type of filter reduces the amount of data captured from the network card?

- [ ] A) Display filter
- [ ] Capture filter
- [ ] C) Both equally
- [ ] D) Neither; they only affect display


---

## Section F: Integration & Scenarios

### Question 25: Game Server Setup (Multiple Choice)

Your game server needs to accept connections from players on the local network (`192.168.1.0/24`). What address should it bind to?

- [ ] A) `192.168.1.1`
- [ ] B) `127.0.0.1`
- [ ] C) `0.0.0.0`
- [ ] D) `255.255.255.255`


---

### Question 26: Network Diagnosis (True/False)

If a player reports high latency to your game server, running `traceroute` would help you identify where in the network path the delay is occurring.

- [ ] True
- [ ] False


---

### Question 27: DNS Caching (Multiple Choice)

Why do game servers typically cache DNS resolutions instead of looking them up every time?

- [ ] A) To improve security
- [ ] To reduce latency and server load
- [ ] C) To prevent IP spoofing
- [ ] D) To comply with networking standards


---

### Question 28: Subnet Planning (Exact Number)

You're setting up a LAN for a game development studio with 50 computers. You want some growth room. What minimum subnet would you use?

Answer: ___________


---

### Question 29: Wireshark Debugging (Fill the Text)

You want to see only traffic to/from your game server (IP `203.0.113.50`). The Wireshark display filter would be: ___________

---

### Question 30: IPv6 Adoption (True/False)

Most modern games already use IPv6 exclusively because IPv4 addresses are depleted.

- [ ] False
- [ ] True
