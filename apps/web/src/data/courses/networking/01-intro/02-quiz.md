## Quiz 01: Network Fundamentals

!!! quiz
{
"title": "Network Stack Header Order",
"question": "A game client sends player position data to a server. As this data travels down through the network stack, headers are added at each layer. What is the correct order in which headers are added?",
"options": [
"Ethernet header → IP header → TCP header → Application header",
"Application header → TCP header → IP header → Ethernet header",
"TCP header → Application header → Ethernet header → IP header",
"IP header → TCP header → Ethernet header → Application data"
],
"answers": ["Application header → TCP header → IP header → Ethernet header"]
}
!!!

!!! quiz
{
"title": "OSI Layer Analysis in Wireshark",
"question": "You're debugging network issues in a game/app. Using Wireshark, you capture a frame and see it contains: an Ethernet header, an IP header, a UDP header, and game/app state data. If you wanted to see ONLY the Ethernet and IP information (ignoring UDP and above), which OSI layers would you be examining?",
"options": [
"Network and Transport (Layers 3 & 4)",
"Physical, Data Link, and Network (Layers 1, 2 & 3)",
"Physical and Data Link (Layers 1 & 2)",
"Data Link and Network (Layers 2 & 3)"
],
"answers": ["Data Link and Network (Layers 2 & 3)"]
}
!!!

!!! quiz
{
"title": "TCP/IP Model Design",
"question": "Why does the TCP/IP model combine the OSI Session, Presentation, and Application layers into a single Application layer?",
"options": [
"The OSI model was created after TCP/IP and added unnecessary complexity",
"Because these distinctions are handled by individual applications rather than the network stack",
"Because modern networks don't need session management or data formatting",
"Because the TCP/IP model was designed to improve network performance"
],
"answers": ["Because these distinctions are handled by individual applications rather than the network stack"]
}
!!!

!!! quiz
{
"title": "Broadcast Message Handling",
"question": "A hub, switch, and router are connected in a network. A device sends a broadcast message. Which statement best describes what happens?",
"options": [
"All three devices block broadcast messages by default",
"The hub floods it to all ports; the switch floods it to all ports; the router blocks it",
"The hub floods it to all ports; the switch sends it only to the destination; the router forwards it to other networks",
"The hub, switch, and router all forward the broadcast to all their ports"
],
"answers": ["The hub floods it to all ports; the switch floods it to all ports; the router blocks it"]
}
!!!

!!! quiz
{
"title": "Cross-Network Communication",
"question": "You're designing a simple networked app. Your app data needs to reach a player on a different network (different subnet). At minimum, which network device is REQUIRED for this communication, and why?",
"options": [
"A repeater, because it can extend the signal to reach distant networks",
"A switch, because it can forward frames between any connected devices",
"A hub, because it broadcasts data to all connected networks",
"A router, because it can forward packets between different networks using IP addresses"
],
"answers": ["A router, because it can forward packets between different networks using IP addresses"]
}
!!!

!!! quiz
{
"title": "Switch Addressing",
"question": "Consider this scenario: Computer A (IP: 192.168.1.10) sends data to Computer B (IP: 192.168.1.20) on the same local network. Which address type does the switch use to deliver the frame to Computer B?",
"options": [
"Subnet mask",
"Port number of the application",
"MAC address of Computer B",
"IP address (192.168.1.20)"
],
"answers": ["MAC address of Computer B"]
}
!!!

!!! quiz
{
"title": "OSI Model Relevance",
"question": "A developer argues: \"The OSI model is outdated and nobody uses all 7 layers, so there's no point learning it.\" What is the strongest counter-argument?",
"options": [
"The TCP/IP model is too simple to describe real networks",
"The OSI model provides a common vocabulary for discussing where problems occur in a network stack",
"The OSI model is legally required for network certification",
"Modern games actually use all 7 layers explicitly"
],
"answers": ["The OSI model provides a common vocabulary for discussing where problems occur in a network stack"]
}
!!!

!!! quiz
{
"title": "Packet Header Changes",
"question": "When a packet travels from a game/app client to a server across the internet, which of the following changes at each router hop?",
"options": [
"Source and destination MAC addresses",
"The payload data",
"Source and destination IP addresses",
"Source and destination port numbers"
],
"answers": ["Source and destination MAC addresses"]
}
!!!

!!! quiz
{
"title": "UDP vs TCP Header Overhead",
"question": "You capture two packets in Wireshark. Packet A has headers: [Ethernet][IP][TCP][HTTP]. Packet B has headers: [Ethernet][IP][UDP][Custom Game/app Protocol]. Both packets are the same total size. Which packet likely has MORE space available for actual data, and why?",
"options": [
"Packet B, because UDP has a smaller header than TCP",
"Packet A, because HTTP is more efficient than custom protocols",
"Packet A, because TCP provides built-in compression",
"They have the same space, because Ethernet and IP headers are identical"
],
"answers": ["Packet B, because UDP has a smaller header than TCP"]
}
!!!

!!! quiz
{
"title": "OSI/TCP-IP Understanding for Programmers",
"question": "In the context of networking, why is understanding the OSI/TCP-IP layering important for a programmer, even though they typically only write code at the Application layer?",
"options": [
"Certification exams require knowledge of all layers",
"Game programmers must implement all layers from scratch",
"Understanding lower layers helps diagnose issues like latency, packet loss, and connection failures",
"Game engines require manual configuration of each OSI layer"
],
"answers": ["Understanding lower layers helps diagnose issues like latency, packet loss, and connection failures"]
}
!!!
