# Week 03 Quiz: UDP and Datagram Sockets

## Question 1

What is the size of the UDP header?

- [ ] 4 bytes
- [x] 8 bytes
- [ ] 20 bytes
- [ ] 40 bytes

> UDP has a minimal 8-byte header containing only: Source Port (2), Destination Port (2), Length (2), and Checksum (2). This is much smaller than TCP's 20+ byte header.

## Question 2

Which of the following is **NOT** a characteristic of UDP?

- [ ] Connectionless
- [ ] Unreliable delivery
- [x] Guaranteed in-order delivery
- [ ] Low overhead

> UDP provides no ordering guarantees. Packets may arrive out of sequence, and it's up to the application to handle reordering if needed. TCP provides ordered delivery, not UDP.

## Question 3

What is the recommended maximum UDP payload size for apps and game networking to avoid fragmentation across all networks?

- [ ] 576 bytes
- [ ] 1472 bytes
- [x] 1200 bytes
- [ ] 65535 bytes

> Glenn Fiedler and the QUIC protocol both recommend ~1200 bytes as the safe maximum. This works across virtually all network paths including VPNs, tunnels, and mobile networks without fragmentation.

## Question 4

In IPv6, what happens when a UDP datagram exceeds the path MTU?

- [ ] The packet is fragmented by routers
- [x] The packet is dropped and an ICMPv6 "Packet Too Big" message is sent
- [ ] The packet is silently discarded with no notification
- [ ] The packet is automatically retransmitted

> Unlike IPv4, IPv6 does not allow fragmentation by intermediate routers. If a packet is too large, it's dropped and the sender receives an ICMPv6 "Packet Too Big" message.

## Question 5

Which socket type constant is used for UDP (datagram) sockets?

- [ ] `SOCK_STREAM`
- [x] `SOCK_DGRAM`
- [ ] `SOCK_RAW`
- [ ] `SOCK_UDP`

> `SOCK_DGRAM` indicates a datagram socket (UDP), while `SOCK_STREAM` indicates a stream socket (TCP). These constants are used when creating sockets in both BSD sockets and Boost.Asio.

## Question 6

In Boost.Asio, how does a UDP server know where to send its response?

- [ ] The client's address is stored in the socket object
- [ ] The server must query a lookup table
- [x] The `receive_from()` function fills an endpoint parameter with the sender's address
- [ ] UDP servers cannot send responses

> `receive_from()` takes an endpoint reference parameter that gets filled with the sender's IP address and port. The server uses this endpoint to send the response back.

## Question 7

What must you do before sending to a broadcast address in Boost.Asio?

- [ ] Nothing, broadcast is enabled by default
- [ ] Call `socket.connect()` first
- [x] Enable the broadcast option with `socket.set_option(broadcast(true))`
- [ ] Use a special broadcast socket type

> By default, sockets cannot send to broadcast addresses. You must explicitly enable the `SO_BROADCAST` option (or `broadcast(true)` in Boost.Asio) before sending to addresses like `255.255.255.255`.

## Question 8

Which broadcast address is limited to the local subnet and will NOT cross routers?

- [x] `255.255.255.255`
- [ ] `0.0.0.0`
- [ ] `224.0.0.1`
- [ ] `127.255.255.255`

> `255.255.255.255` is the "limited broadcast" address that only reaches hosts on the same subnet. Routers do not forward these packets. Directed broadcasts (like `192.168.1.255`) can theoretically cross routers but are often blocked.

## Question 9

Why is the UDP checksum mandatory in IPv6 but optional in IPv4?

- [ ] IPv6 packets are larger and need more error checking
- [ ] IPv4 has its own header checksum that covers UDP data
- [x] IPv6 removed the IP header checksum, so the transport layer must verify integrity
- [ ] IPv6 uses a different checksum algorithm

> IPv4 includes a checksum in the IP header itself, providing some error detection. IPv6 removed this IP header checksum for efficiency (relying on link-layer checks), so the transport layer (UDP/TCP) must ensure data integrity.

## Question 10

In the UDP echo server pattern, what is the correct sequence of operations?

- [ ] `bind()` → `send_to()` → `receive_from()` → loop
- [x] `bind()` → `receive_from()` → `send_to()` → loop
- [ ] `connect()` → `receive_from()` → `send_to()` → loop
- [ ] `send_to()` → `receive_from()` → `bind()` → loop

> A UDP echo server first binds to a port, then enters a loop where it receives a datagram (which also captures the sender's address), and sends the same data back to that sender. No `connect()` is needed for UDP servers.
