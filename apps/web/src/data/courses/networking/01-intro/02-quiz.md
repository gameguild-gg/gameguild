## Quiz 01: Network Fundamentals

---

**1. A game client sends player position data to a server. As this data travels down through the network stack, headers are added at each layer. What is the correct order in which headers are added?**

A) Ethernet header → IP header → TCP header → Application header  
B) Application header → TCP header → IP header → Ethernet header  
C) TCP header → Application header → Ethernet header → IP header  
D) IP header → TCP header → Ethernet header → Application data

---

**2. You're debugging network issues in a multiplayer game. Using Wireshark, you capture a frame and see it contains: an Ethernet header, an IP header, a UDP header, and game state data. If you wanted to see ONLY the Ethernet and IP information (ignoring UDP and above), which OSI layers would you be examining?**

A) Network and Transport (Layers 3 & 4)  
B) Physical, Data Link, and Network (Layers 1, 2 & 3)  
C) Physical and Data Link (Layers 1 & 2)  
D) Data Link and Network (Layers 2 & 3)

---

**3. Why does the TCP/IP model combine the OSI Session, Presentation, and Application layers into a single Application layer?**

A) The OSI model was created after TCP/IP and added unnecessary complexity  
B) Because these distinctions are handled by individual applications rather than the network stack  
C) Because modern networks don't need session management or data formatting  
D) Because the TCP/IP model was designed to improve network performance

---

**4. A hub, switch, and router are connected in a network. A device sends a broadcast message. Which statement best describes what happens?**

A) All three devices block broadcast messages by default  
B) The hub floods it to all ports; the switch floods it to all ports; the router blocks it  
C) The hub floods it to all ports; the switch sends it only to the destination; the router forwards it to other networks  
D) The hub, switch, and router all forward the broadcast to all their ports

---

**5. You're designing a simple networked game. Your game data needs to reach a player on a different network (different subnet). At minimum, which network device is REQUIRED for this communication, and why?**

A) A repeater, because it can extend the signal to reach distant networks  
B) A switch, because it can forward frames between any connected devices  
C) A hub, because it broadcasts data to all connected networks  
D) A router, because it can forward packets between different networks using IP addresses

---

**6. Consider this scenario: Computer A (IP: 192.168.1.10) sends data to Computer B (IP: 192.168.1.20) on the same local network. Which address type does the switch use to deliver the frame to Computer B?**

A) Subnet mask  
B) Port number of the application  
C) MAC address of Computer B  
D) IP address (192.168.1.20)

---

**7. A game developer argues: "The OSI model is outdated and nobody uses all 7 layers, so there's no point learning it." What is the strongest counter-argument?**

A) The TCP/IP model is too simple to describe real networks  
B) The OSI model provides a common vocabulary for discussing where problems occur in a network stack  
C) The OSI model is legally required for network certification  
D) Modern games actually use all 7 layers explicitly

---

**8. When a packet travels from a game client to a game server across the internet, which of the following changes at each router hop?**

A) Source and destination MAC addresses  
B) The payload data  
C) Source and destination IP addresses  
D) Source and destination port numbers

---

**9. You capture two packets in Wireshark. Packet A has headers: [Ethernet][IP][TCP][HTTP]. Packet B has headers: [Ethernet][IP][UDP][Custom Game Protocol]. Both packets are the same total size. Which packet likely has MORE space available for actual game data, and why?**

A) Packet B, because UDP has a smaller header than TCP  
B) Packet A, because HTTP is more efficient than custom protocols  
C) Packet A, because TCP provides built-in compression  
D) They have the same space, because Ethernet and IP headers are identical

---

**10. In the context of game networking, why is understanding the OSI/TCP-IP layering important for a game programmer, even though they typically only write code at the Application layer?**

A) Certification exams require knowledge of all layers  
B) Game programmers must implement all layers from scratch  
C) Understanding lower layers helps diagnose issues like latency, packet loss, and connection failures  
D) Game engines require manual configuration of each OSI layer
