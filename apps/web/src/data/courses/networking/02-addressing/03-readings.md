# Week 02 Readings: Addressing and DNS

| #   | Reading                                                                                                                                                                              | Time   | Covers                                                    |
| --- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------ | --------------------------------------------------------- |
| 1   | Beej's Guide to Network Concepts, [Ch. 6–8 "IP, IPv4, IPv6"](https://beej.us/guide/bgnet0/html/#the-internet-protocol-ip)                                                            | 25 min | IPv4/IPv6 structure, address notation, differences        |
| 2   | Beej's Guide to Network Concepts, [Ch. 17 "IP Subnets and Subnet Masks"](https://beej.us/guide/bgnet0/html/#ip-subnets-and-subnet-masks)                                             | 15 min | Subnet masks, CIDR notation, network/host portions        |
| 3   | Practical Networking, ["Subnetting Mastery"](https://www.practicalnetworking.net/stand-alone/subnetting-mastery/) (7 videos)                                                         | 45 min | Subnet calculations, cheat-sheet method, speed techniques |
| 4   | Cloudflare Learning Center, ["What is DNS?"](https://www.cloudflare.com/learning/dns/what-is-dns/) + ["DNS Server Types"](https://www.cloudflare.com/learning/dns/dns-server-types/) | 20 min | DNS resolution process, recursive/authoritative servers   |
| 5   | Wireshark User's Guide, [Ch. 1 Introduction](https://www.wireshark.org/docs/wsug_html_chunked/ChapterIntroduction.html)                                                              | 30 min | Wireshark UI, capturing packets, basic display filters    |

**Total reading time: ~135 minutes (~2.25 hours)**

---

## Interactive (Pick One)

| Resource                                                                              | Time   | What it does                                          |
| ------------------------------------------------------------------------------------- | ------ | ----------------------------------------------------- |
| [SubnetIPv4.com](https://subnetipv4.com/)                                             | 15 min | Random subnetting problems with instant feedback      |
| [Kurose/Ross DNS Problems](https://gaia.cs.umass.edu/kurose_ross/interactive/dns.php) | 15 min | Self-quiz on DNS resolution and query types           |
| [Wireshark Sample Captures](https://wiki.wireshark.org/SampleCaptures)                | 20 min | Analyze real packet captures (dns, http) in Wireshark |

---

## Optional Deep Dive

- RFC 1918, ["Address Allocation for Private Internets"](https://www.rfc-editor.org/rfc/rfc1918) - The original standard defining 10.x.x.x, 172.16.x.x, 192.168.x.x private ranges
- Beej's Guide, [Ch. 31 "Domain Name System (DNS)"](https://beej.us/guide/bgnet0/html/#domain-name-system-dns) - More detailed DNS coverage with Python examples
- [RFC 791 (IPv4)](https://datatracker.ietf.org/doc/html/rfc791) | [RFC 8200 (IPv6)](https://datatracker.ietf.org/doc/html/rfc8200) | [RFC 1035 (DNS)](https://datatracker.ietf.org/doc/html/rfc1035)
- [Wireshark User's Guide](https://www.wireshark.org/docs/wsug_html_chunked/) + [Kurose/Ross Wireshark Labs](https://gaia.cs.umass.edu/kurose_ross/wireshark.php)
