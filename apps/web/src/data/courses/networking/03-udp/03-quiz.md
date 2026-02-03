# Week 03 Quiz: UDP and Datagram Sockets

!!! quiz
{
"title": "UDP Header Size",
"question": "What is the size of the UDP header?",
"options": ["4 bytes", "8 bytes", "20 bytes", "40 bytes"],
"answers": ["8 bytes"]
}
!!!

!!! quiz
{
"title": "UDP Characteristics",
"question": "Which of the following is NOT a characteristic of UDP?",
"options": ["Connectionless", "Unreliable delivery", "Guaranteed in-order delivery", "Low overhead"],
"answers": ["Guaranteed in-order delivery"]
}
!!!

!!! quiz
{
"title": "Safe UDP Payload Size",
"question": "What is the recommended maximum UDP payload size for apps and game networking to avoid fragmentation across all networks?",
"options": ["576 bytes", "1472 bytes", "1200 bytes", "65535 bytes"],
"answers": ["1200 bytes"]
}
!!!

!!! quiz
{
"title": "IPv6 and Large UDP Datagrams",
"question": "In IPv6, what happens when a UDP datagram exceeds the path MTU?",
"options": ["The packet is fragmented by routers", "The packet is dropped and an ICMPv6 Packet Too Big message is sent", "The packet is silently discarded with no notification", "The packet is automatically retransmitted"],
"answers": ["The packet is dropped and an ICMPv6 Packet Too Big message is sent"]
}
!!!

!!! quiz
{
"title": "UDP Socket Type",
"question": "Which socket type constant is used for UDP (datagram) sockets?",
"options": ["SOCK_STREAM", "SOCK_DGRAM", "SOCK_RAW", "SOCK_UDP"],
"answers": ["SOCK_DGRAM"]
}
!!!

!!! quiz
{
"title": "UDP Server Response",
"question": "In Boost.Asio, how does a UDP server know where to send its response?",
"options": ["The client's address is stored in the socket object", "The server must query a lookup table", "The receive_from() function fills an endpoint parameter with the sender's address", "UDP servers cannot send responses"],
"answers": ["The receive_from() function fills an endpoint parameter with the sender's address"]
}
!!!

!!! quiz
{
"title": "UDP Broadcast",
"question": "What must you do before sending to a broadcast address in Boost.Asio?",
"options": ["Nothing, broadcast is enabled by default", "Call socket.connect() first", "Enable the broadcast option with socket.set_option(broadcast(true))", "Use a special broadcast socket type"],
"answers": ["Enable the broadcast option with socket.set_option(broadcast(true))"]
}
!!!

!!! quiz
{
"title": "Broadcast Address Scope",
"question": "Which broadcast address is limited to the local subnet and will NOT cross routers?",
"options": ["255.255.255.255", "0.0.0.0", "224.0.0.1", "127.255.255.255"],
"answers": ["255.255.255.255"]
}
!!!

!!! quiz
{
"title": "UDP Checksum Requirement",
"question": "Why is the UDP checksum mandatory in IPv6 but optional in IPv4?",
"options": ["IPv6 packets are larger and need more error checking", "IPv4 has its own header checksum that covers UDP data", "IPv6 removed the IP header checksum, so the transport layer must verify integrity", "IPv6 uses a different checksum algorithm"],
"answers": ["IPv6 removed the IP header checksum, so the transport layer must verify integrity"]
}
!!!

!!! quiz
{
"title": "UDP Echo Server Pattern",
"question": "In the UDP echo server pattern, what is the correct sequence of operations?",
"options": ["bind() -> send_to() -> receive_from() -> loop", "bind() -> receive_from() -> send_to() -> loop", "connect() -> receive_from() -> send_to() -> loop", "send_to() -> receive_from() -> bind() -> loop"],
"answers": ["bind() -> receive_from() -> send_to() -> loop"]
}
!!!
