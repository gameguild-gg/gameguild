# Week 02: Network Addressing Quiz

## Section A: IPv4 & IPv6 Fundamentals

!!! quiz
{
"title": "IPv4 Bits",
"question": "How many bits are in an IPv4 address?",
"options": ["128 bits", "32 bits", "16 bits", "64 bits"],
"answers": ["32 bits"]
}
!!!

!!! quiz
{
"title": "IPv4 Octet Range",
"question": "What is the minimum and maximum value for a single octet in an IPv4 address?",
"options": ["1–254", "0–255", "0–128", "0–100"],
"answers": ["0–255"]
}
!!!

!!! quiz
{
"title": "IPv6 Address Length",
"question": "True or False: IPv6 addresses are 128 bits long.",
"options": ["False", "True"],
"answers": ["True"]
}
!!!

!!! quiz
{
"title": "IPv6 Localhost",
"question": "The IPv6 equivalent of 127.0.0.1 (localhost) is:",
"options": ["fe80::1", "::1", "2001:db8::1", "::"],
"answers": ["::1"]
}
!!!

!!! quiz
{
"title": "Special IPv4 Addresses",
"question": "Which address is used when a server wants to accept connections on any available network interface?",
"options": ["192.168.1.1", "0.0.0.0", "255.255.255.255", "127.0.0.1"],
"answers": ["0.0.0.0"]
}
!!!

## Section B: Subnetting & CIDR

!!! quiz
{
"title": "Subnet Mask to CIDR",
"question": "Convert the subnet mask 255.255.255.0 to CIDR notation.",
"options": ["/25", "/23", "/24", "/26"],
"answers": ["/24"]
}
!!!

!!! quiz
{
"title": "Network Address Calculation",
"question": "Given the IP address 192.168.100.50 with subnet mask 255.255.255.0, what is the network address?",
"options": ["192.168.100.255", "192.168.100.0", "192.168.101.0", "192.168.100.50"],
"answers": ["192.168.100.0"]
}
!!!

!!! quiz
{
"title": "Broadcast Address",
"question": "For the subnet 192.168.1.0/24, the broadcast address is:",
"options": ["192.168.1.0", "192.168.1.255", "192.168.1.254", "192.168.1.1"],
"answers": ["192.168.1.255"]
}
!!!

!!! quiz
{
"title": "Usable Hosts in /25",
"question": "How many usable host addresses are available in a /25 subnet?",
"options": ["128", "126", "62", "254"],
"answers": ["126"]
}
!!!

!!! quiz
{
"title": "CIDR Subnet Size",
"question": "Which CIDR notation provides the most host addresses?",
"options": ["/30", "/8", "/25", "/16"],
"answers": ["/8"]
}
!!!

!!! quiz
{
"title": "Private IP Ranges",
"question": "True or False: The range 172.16.0.0 to 172.31.255.255 is reserved for private networks.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

!!! quiz
{
"title": "CIDR to Dotted Decimal",
"question": "Convert /26 to a dotted decimal subnet mask.",
"options": ["255.255.255.128", "255.255.255.192", "255.255.255.0", "255.255.255.224"],
"answers": ["255.255.255.192"]
}
!!!

## Section C: DNS

!!! quiz
{
"title": "DNS Purpose",
"question": "What is the primary function of DNS?",
"options": ["Encrypt network traffic", "Route packets between routers", "Map domain names to IP addresses", "Manage server hardware"],
"answers": ["Map domain names to IP addresses"]
}
!!!

!!! quiz
{
"title": "DNS Record Type",
"question": "A DNS record type that maps a domain name to an IPv4 address is called:",
"options": ["CNAME", "TXT", "A", "AAAA"],
"answers": ["A"]
}
!!!

!!! quiz
{
"title": "DNS TTL Meaning",
"question": "What does TTL stand for in DNS?",
"options": ["Time To Live", "Transfer Table Layer", "Transmission Time Limit", "TCP Time Log"],
"answers": ["Time To Live"]
}
!!!

!!! quiz
{
"title": "FQDN Components",
"question": "In the FQDN api.example.com, identify the subdomain, domain, and TLD.",
"options": ["subdomain=example, domain=api, TLD=com", "subdomain=api, domain=example, TLD=com", "subdomain=com, domain=example, TLD=api", "subdomain=api, domain=com, TLD=example"],
"answers": ["subdomain=api, domain=example, TLD=com"]
}
!!!

## Section D: Routing & Traceroute

!!! quiz
{
"title": "TTL Definition",
"question": "True or False: TTL (Time To Live) prevents infinite routing loops by decrementing at each hop.",
"options": ["False", "True"],
"answers": ["True"]
}
!!!

!!! quiz
{
"title": "Traceroute Purpose",
"question": "What does the traceroute command do?",
"options": ["Encrypts traffic between hops", "Discovers the path packets take to reach a destination", "Blocks packets at firewalls", "Measures packet loss"],
"answers": ["Discovers the path packets take to reach a destination"]
}
!!!

!!! quiz
{
"title": "Typical Initial TTL",
"question": "When a packet leaves your computer, what is a typical starting TTL value?",
"options": ["256", "32", "64 or 128", "1"],
"answers": ["64 or 128"]
}
!!!

!!! quiz
{
"title": "Default Route",
"question": "The default route in a routing table that matches all destinations is:",
"options": ["255.255.255.255/32", "0.0.0.0/0", "192.168.0.0/16", "127.0.0.0/8"],
"answers": ["0.0.0.0/0"]
}
!!!

## Section E: Wireshark

!!! quiz
{
"title": "Wireshark Function",
"question": "What is Wireshark primarily used for?",
"options": ["Configuring firewall rules", "Writing network code", "Capturing and analyzing network packets", "Managing DNS servers"],
"answers": ["Capturing and analyzing network packets"]
}
!!!

!!! quiz
{
"title": "Wireshark DNS Filter",
"question": "To filter Wireshark to show only DNS traffic, use the display filter:",
"options": ["tcp.port == 80", "dns", "ip.addr == 8.8.8.8", "udp.port == 53"],
"answers": ["dns"]
}
!!!

!!! quiz
{
"title": "Wireshark TCP Stream",
"question": "True or False: You can view the human-readable content of a TCP conversation in Wireshark using 'Follow TCP Stream.'",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

!!! quiz
{
"title": "Capture vs Display Filter",
"question": "Which type of filter reduces the amount of data captured from the network card?",
"options": ["Neither; they only affect display", "Capture filter", "Display filter", "Both equally"],
"answers": ["Capture filter"]
}
!!!

## Section F: Integration & Scenarios

!!! quiz
{
"title": "Game Server Setup",
"question": "Your game server needs to accept connections from players on the local network (192.168.1.0/24). What address should it bind to?",
"options": ["127.0.0.1", "255.255.255.255", "0.0.0.0", "192.168.1.1"],
"answers": ["0.0.0.0"]
}
!!!

!!! quiz
{
"title": "Network Diagnosis",
"question": "True or False: If a player reports high latency to your game server, running traceroute would help you identify where in the network path the delay is occurring.",
"options": ["False", "True"],
"answers": ["True"]
}
!!!

!!! quiz
{
"title": "DNS Caching",
"question": "Why do game servers typically cache DNS resolutions instead of looking them up every time?",
"options": ["To comply with networking standards", "To reduce latency and server load", "To prevent IP spoofing", "To improve security"],
"answers": ["To reduce latency and server load"]
}
!!!

!!! quiz
{
"title": "Subnet Planning",
"question": "You're setting up a LAN for a game development studio with 50 computers. You want some growth room. What minimum subnet would you use?",
"options": ["/26", "/24", "/25", "/27"],
"answers": ["/25"]
}
!!!

!!! quiz
{
"title": "Wireshark Host Filter",
"question": "You want to see only traffic to/from your game server (IP 203.0.113.50). The Wireshark display filter would be:",
"options": ["host 203.0.113.50", "ip.addr == 203.0.113.50", "tcp.port == 203", "ip.dst == 203.0.113.50"],
"answers": ["ip.addr == 203.0.113.50"]
}
!!!

!!! quiz
{
"title": "IPv6 Adoption",
"question": "True or False: Most modern games already use IPv6 exclusively because IPv4 addresses are depleted.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!
