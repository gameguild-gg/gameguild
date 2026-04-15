# Quiz 07: Distributed State and Synchronization

## Topic 1: CAP and P2P Basics

!!! quiz
{
"title": "CAP and Network Splits",
"question": "CAP states that a distributed system cannot guarantee all three of Consistency, Availability, and one other property. That third property means the system keeps operating even when the network is split and some nodes cannot reach others. What is it?",
"options": ["Parallelism", "Persistence", "Performance", "Partition tolerance"],
"answers": ["Partition tolerance"]
}
!!!

---

!!! quiz
{
"title": "What Lockstep Peers Send",
"question": "In a lockstep P2P game (e.g. RTS style), each peer runs the same simulation. What do peers actually transmit to each other every turn so that everyone can stay in sync?",
"options": ["Delta-compressed state", "Only player inputs", "Rendered frames", "Full game state snapshots"],
"answers": ["Only player inputs"]
}
!!!

---

!!! quiz
{
"title": "Desync from Nondeterminism",
"question": "In lockstep, if one peer's simulation produces even a tiny different result (e.g. one floating-point difference) from the same inputs, the game desyncs. Why must every peer produce identical results from the same input stream?",
"options": ["To enable delta compression", "To allow late-joining players", "Peers only exchange inputs, so each must derive the same state from them locally", "To reduce server CPU usage"],
"answers": ["Peers only exchange inputs, so each must derive the same state from them locally"]
}
!!!

---

!!! quiz
{
"title": "Full Mesh with 6 Peers",
"question": "Six players are in a full mesh P2P topology (every peer has a direct connection to every other peer). How many distinct connections are there in total?",
"options": ["6", "12", "15", "36"],
"answers": ["15"]
}
!!!

---

!!! quiz
{
"title": "Who Has Zero Latency to Authority",
"question": "In which P2P model does one peer have zero network latency to the authoritative simulation because that peer is also running the server?",
"options": ["Full mesh", "State broadcast", "Lockstep", "Host authority (listen server)"],
"answers": ["Host authority (listen server)"]
}
!!!

---

## Topic 2: State vs Input Sync and Security

!!! quiz
{
"title": "Deterministic Simulation Requirement",
"question": "You want to minimize bandwidth by not sending full game state every tick. Instead, you send only player actions and expect every machine to run the same simulation to derive state. Which approach is this?",
"options": ["State sync", "Input sync", "Delta broadcast", "Snapshot sync"],
"answers": ["Input sync"]
}
!!!

---

!!! quiz
{
"title": "Cheat: Client Reports Health",
"question": "A cheat allows a player to display 999 health while the server thinks they have 10. What server mistake most likely allowed this?",
"options": ["Accepting client-reported health (or other critical state) as authoritative", "Not encrypting packets", "Accepting input commands like \"move left\"", "Sending sequence numbers on input packets"],
"answers": ["Accepting client-reported health (or other critical state) as authoritative"]
}
!!!

---

!!! quiz
{
"title": "Preventing Teleportation Cheats",
"question": "A client sends \"I moved 500 units this tick.\" The server allows at most 100 units per tick. What should the server do to prevent teleportation or speed hacks?",
"options": ["Encrypt the packet and send it back", "Reject or clamp the move so it never exceeds the allowed maximum per tick", "Verify the client's IP address", "Check the client's system clock"],
"answers": ["Reject or clamp the move so it never exceeds the allowed maximum per tick"]
}
!!!

---

!!! quiz
{
"title": "Why Server Hides Invisible Entities",
"question": "The server only sends position updates for entities that are in the player's line of sight or area of interest. What cheat does this design help prevent?",
"options": ["Speed hacks", "Wallhacks (showing enemies through walls)", "Score manipulation", "Input replay attacks"],
"answers": ["Wallhacks (showing enemies through walls)"]
}
!!!

---

## Topic 3: Server Reconciliation

!!! quiz
{
"title": "What Client Does with Unprocessed Inputs",
"question": "In server reconciliation, the client receives the server's authoritative state and a \"last processed\" input sequence number. After discarding predictions for inputs the server has already processed, what does the client do with inputs the server has not yet processed?",
"options": ["Ignore them until the next tick", "Send them back to the server for comparison", "Reapply them on top of the server state to get a corrected prediction", "Drop them permanently"],
"answers": ["Reapply them on top of the server state to get a corrected prediction"]
}
!!!

---

!!! quiz
{
"title": "Purpose of Last Processed Seq",
"question": "The server includes a \"last processed sequence number\" in its state updates. What does the client use this for?",
"options": ["To control the server's tick rate", "To encrypt the next input", "To know which inputs are already applied on the server so it can discard those and reapply only unprocessed ones", "To determine the order in which players joined"],
"answers": ["To know which inputs are already applied on the server so it can discard those and reapply only unprocessed ones"]
}
!!!

---

!!! quiz
{
"title": "Client Predicted Through Doorway",
"question": "The client predicted that the player walked through a doorway. The server has a wall there (e.g. door closed) and rejected the move. When the client reconciles with the server's authoritative state, what does the player see?",
"options": ["The wall is removed and the prediction is accepted", "The player snaps back to the server's position (in front of the wall)", "The server sends the wall to the client and the client adds it", "The client is disconnected for cheating"],
"answers": ["The player snaps back to the server's position (in front of the wall)"]
}
!!!

---

!!! quiz
{
"title": "Snap vs Blend vs Ignore",
"question": "In threshold-based correction, the client may snap, blend, or ignore the correction depending on how large the error is. Which description matches this strategy?",
"options": ["The server only sends updates when error exceeds a threshold", "Large error: snap instantly; moderate: blend smoothly; small: keep prediction", "The client disconnects if error exceeds a threshold", "Sequence numbers are only sent when the threshold is exceeded"],
"answers": ["Large error: snap instantly; moderate: blend smoothly; small: keep prediction"]
}
!!!

---

## Topic 4: Delta Compression and Bandwidth

!!! quiz
{
"title": "Sending Only What Changed",
"question": "In many games only a small fraction of the full state changes each tick. What technique sends only the changed parts instead of the full state to reduce bandwidth?",
"options": ["Encryption", "Delta compression", "Deterministic simulation", "Hit detection"],
"answers": ["Delta compression"]
}
!!!

---

!!! quiz
{
"title": "Why XOR Old and New State",
"question": "In binary delta compression, the delta is often computed as (new_state XOR old_state). When only a few bits change between old and new, why is this representation useful?",
"options": ["It encrypts the state", "The result is mostly zeros and compresses very well", "It creates a checksum for error detection", "It detects which inputs were reordered"],
"answers": ["The result is mostly zeros and compresses very well"]
}
!!!

---

!!! quiz
{
"title": "Recovery After Lost Delta",
"question": "Delta encoding requires both sides to share the same baseline. A delta packet is lost over UDP so the client's baseline is wrong. Which of these is a valid way for the server to recover?",
"options": ["The client fills in missing data with zeros", "Resend a delta from the last acknowledged baseline, or send a full snapshot, or send redundant deltas", "The server retransmits the exact same lost packet only", "The client's game must reconnect"],
"answers": ["Resend a delta from the last acknowledged baseline, or send a full snapshot, or send redundant deltas"]
}
!!!

---

!!! quiz
{
"title": "Bandwidth Budget and Entity Selection",
"question": "The server has a fixed bandwidth budget per tick and cannot send every entity to every client every time. How does a priority accumulator help?",
"options": ["It decides which client connects first", "It assigns priority by distance/importance and sends high-priority entities often; others accumulate priority until sent", "It compresses high-priority packets more", "It sets the order in which inputs are processed"],
"answers": ["It assigns priority by distance/importance and sends high-priority entities often; others accumulate priority until sent"]
}
!!!

---

## Topic 5: Optimistic Concurrency and P2P Conflict Resolution

!!! quiz
{
"title": "Apply First, Reconcile Later",
"question": "The client applies its own move immediately without waiting for the server; if the server later disagrees, the client corrects. This pattern is the game-networking analogue of which distributed systems idea?",
"options": ["Strict locking", "Optimistic concurrency (assume success, then reconcile if authority disagrees)", "Two-phase commit", "Deterministic simulation on all nodes"],
"answers": ["Optimistic concurrency (assume success, then reconcile if authority disagrees)"]
}
!!!

---

!!! quiz
{
"title": "Two Peers Grab Same Item",
"question": "In a P2P game with a listen server, two peers both claim they picked up the same item. Which conflict resolution method lets the host (the peer running the server) decide who actually gets it?",
"options": ["Vector clocks", "Last-writer-wins", "CRDTs", "Host arbitration"],
"answers": ["Host arbitration"]
}
!!!

---

!!! quiz
{
"title": "P2P During a Partition",
"question": "When some peers are unreachable due to a network partition, many P2P games keep accepting local input and let state diverge temporarily, then reconcile when the network recovers. Which CAP trade-off does this reflect?",
"options": ["Favoring CP (consistency over availability)", "Favoring AP (availability over strong consistency; accept eventual consistency)", "P2P systems never partition", "P2P systems have unlimited bandwidth"],
"answers": ["Favoring AP (availability over strong consistency; accept eventual consistency)"]
}
!!!
