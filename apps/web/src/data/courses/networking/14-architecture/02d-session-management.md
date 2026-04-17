# Session Management and Connection Lifecycle

A "session" is the container for a multiplayer experience: from the moment players are grouped together to the moment the last one leaves. Session management handles creating, joining, finding, maintaining, and ending these containers. It is the glue between matchmaking ("find me a game") and gameplay ("play the game").

---

## 1. What Is a Session?

### Session as a Resource

A session is a managed resource with a lifecycle:

```
Create → Open (accepting players) → Active (in progress) → Closing → Destroyed
```

At any point in its lifecycle, the session has:

- **Identity**: a unique session ID.
- **State**: open, active, closing, etc.
- **Membership**: list of connected players and their roles (host, player, spectator).
- **Configuration**: game mode, map, rules, visibility (public/private), max players.
- **Metadata**: region, creation time, average skill, tags for matchmaker queries.

### Session vs Match vs Lobby

These terms are related but distinct:

| Concept     | Duration          | What Happens                                      |
| ----------- | ----------------- | ------------------------------------------------- |
| **Lobby**   | Pre-match waiting | Players gather, configure settings, ready up      |
| **Match**   | Active gameplay   | The game runs with locked (or managed) player set |
| **Session** | Full lifecycle    | Encompasses lobby + match + post-match            |

A session may contain multiple matches (best-of-3, map rotation) or just one. The session persists across lobby-to-match transitions and potentially across disconnection/reconnection.

---

## 2. Session Lifecycle

### Creation

Sessions are created by:

- **Matchmaker**: the matchmaking system groups players and creates a session for them.
- **Player (host)**: a player creates a custom game and shares the join code or makes it publicly browsable.
- **Server allocation**: the matchmaker requests a game server instance, and the session is created once the server is ready.

Creation involves:

1. Allocating a session ID.
2. Registering the session in a session directory (for discovery).
3. Provisioning a game server (dedicated) or designating a host (listen).
4. Setting initial configuration (mode, map, max players, region).

### Joining

Players join sessions through:

- **Matchmaker assignment**: "You've been placed in session ABC-123."
- **Invite/join code**: "Enter code XYZ to join your friend."
- **Server browser**: player browses a list of open sessions and picks one.
- **Quick match**: matchmaker finds an in-progress session with open slots.

Joining involves:

1. Client contacts the session (via IP:port or through a relay).
2. Session validates the join request (is there room? is the player authorized? is the session still accepting?).
3. Session registers the player in its membership list.
4. Session sends the current game state to the new player (full state sync).
5. Client begins simulation.

### Connection Establishment

After matchmaking or session discovery, the client must establish a network connection to the game server. This involves:

1. **Address resolution**: the client obtains the server's IP address and port (from matchmaker response, session directory, or relay service).
2. **NAT traversal** (if needed): for listen servers or P2P, hole punching or relay setup.
3. **Protocol handshake**: custom UDP handshake with challenge-response (anti-spoofing).
4. **Authentication**: the client presents a token (from the platform's auth service) that the server validates.
5. **Initial state transfer**: the server sends the client the current world state.

### Disconnection and Reconnection

Players disconnect for many reasons: network failure, game crash, rage quit, power outage, mobile backgrounding. The session must handle each gracefully.

**Disconnection detection**:

- **Heartbeat timeout**: no packets received within a threshold (e.g., 10 seconds).
- **Connection close**: explicit disconnect message from the client.
- **Transport failure**: underlying transport reports connection lost.

**Reconnection policy options**:

| Policy             | Behavior                                                 | Use Case                 |
| ------------------ | -------------------------------------------------------- | ------------------------ |
| No reconnect       | Player is removed immediately; slot is freed             | Fast-paced short matches |
| Grace period       | Slot is held for N seconds; player can rejoin            | Competitive matches      |
| Persistent slot    | Player's state is preserved indefinitely; rejoin anytime | MMO, persistent worlds   |
| Spectator fallback | Reconnected player joins as spectator                    | Tournaments, broadcast   |

Reconnection requires:

1. Client authenticates again (token may have expired).
2. Server checks that the slot is still held.
3. Server sends a state snapshot (the world has changed since disconnect).
4. Client fast-forwards or receives delta updates.

---

## 3. Session Discovery

### The Server Browser Model

The classic model: a list of active sessions, filterable by map, mode, player count, region, ping.

Components:

- **Session directory service**: maintains a registry of all active sessions and their metadata.
- **Heartbeat/health updates**: each session periodically reports its status (player count, state, region).
- **Query interface**: clients query the directory with filters and receive a sorted list.

Advantages:

- Player has full control over which session to join.
- Supports niche communities (specific maps, rule sets, mods).

Disadvantages:

- Players must make a choice (friction).
- Sessions may be full by the time the player tries to join (stale data).
- Uneven distribution: popular sessions fill instantly, unpopular sessions stay empty.

### The Matchmaker Model

The matchmaker removes player choice: the player requests a match, and the system assigns them to a session. This is the dominant model for competitive and casual matchmaking.

The matchmaker is a separate service that:

1. Receives player requests ("I want to play ranked 5v5").
2. Evaluates players against each other (skill, latency, party, region).
3. Forms groups and creates sessions for them.
4. Assigns players to sessions and provides connection details.

We'll cover matchmaking algorithms in detail in the next section.

### Hybrid: Browse + Quick Match

Many games offer both:

- **Quick Match**: matchmaker assigns you for speed and fairness.
- **Server Browser**: browse custom games or community servers.
- **Custom Game**: create your own session and invite friends.

---

## 4. Connection Brokering

### What Connection Brokering Solves

Even after matchmaking groups players and allocates a server, the clients still need to establish actual network connections. Connection brokering is the process that gets clients and servers connected.

### The Brokering Flow

```
1. Matchmaker creates session → assigns server address
2. Matchmaker sends session details to each client (server IP:port, auth token)
3. Client connects to server using provided details
4. Server validates client's auth token
5. Connection established → client joins session
```

For dedicated servers, this is straightforward: the server has a public IP or is behind a well-known relay.

For listen servers, connection brokering is harder:

- The host may be behind NAT.
- Clients need the host's NAT-traversed address or a relay endpoint.
- The brokering service may need to facilitate STUN/TURN (covered in Week 15).

### Platform Services for Brokering

Most platforms provide session and brokering services:

| Platform    | Service                    | Capabilities                          |
| ----------- | -------------------------- | ------------------------------------- |
| Steam       | Steamworks Networking      | Relay, NAT traversal, matchmaking     |
| Epic        | Epic Online Services (EOS) | Lobbies, sessions, P2P relay          |
| PlayStation | PSN Matching               | Session management, relay             |
| Xbox        | Xbox Live Multiplayer      | Session directory, Smart Match, relay |
| PlayFab     | PlayFab Multiplayer        | Server hosting, matchmaking, party    |

These services handle the low-level connection problems (NAT, relay, auth) so the game developer focuses on session logic.

---

## 5. CSI vs GPR Framing

### CSI Perspective: Sessions as Distributed Resources

The CSI engineer models sessions as resources in a distributed system:

- **Consistency**: all participants must agree on session membership and state. A player who appears "in the session" on one client but not another causes bugs.
- **Availability**: session operations (join, leave, create) must succeed within latency budgets even under load.
- **Partition tolerance**: if the session directory is temporarily unreachable, can clients still connect to known servers? (Caching, local state.)
- **Idempotency**: join and leave operations must be idempotent — retrying a join should not create duplicate memberships.

### GPR Perspective: Sessions as Player Journeys

The GPR engineer models sessions as player experience flows:

- **Time to play**: how quickly does the player go from "I want to play" to "I am playing"? Every second in a lobby, loading screen, or matchmaking queue is friction.
- **Social continuity**: can friends stay together across matches? Can the party persist through map changes?
- **Graceful degradation**: when a player disconnects, do the remaining players have a good experience? (Backfill, AI substitution, or graceful match end.)
- **Transparency**: does the player understand what's happening? (Queue position, estimated wait time, server region, connection quality.)
