# Assignment 07: State Synchronization System

## Overview

Build an **authoritative game server** and **multiple clients** in C++ that synchronize shared state over UDP. Your implementation will demonstrate client-server state sync, server reconciliation (client-side prediction + correction), delta compression, and the "never trust the client" principle. Reuse your serialization library from Assignment 06 (`BitWriter`, `BitReader`, `serialize_player`, varint) for packet encoding.

You may create this on your preferred game engine such as Unity or Unreal, but the networking and synchronization logic should support what is described below.

---

## Submission Requirements

### If you use the course boilerplate ([gameguild-gg/network](https://github.com/gameguild-gg/network))

- Your code must compile cleanly with the provided `tests.cpp` (which `#include`s your headers)
- All provided [doctest](https://github.com/doctest/doctest) tests must **pass**
- Submit the link to your GitHub repository (private repo; see course README for duplication instructions)
- **No video is required** — automated tests verify your implementation

### If you submit via a game engine (Unity, Unreal, etc.)

- **Video demonstration (5 minutes maximum)** is required. The video structure is mandatory:
  1. **First 30–60 seconds**: Show a text editor with a clear list of:
     - All features you implemented
     - Which requirements are complete vs. partial
     - Any extra features beyond the base requirements

     ⚠️ **If you skip this step, your submission will not be graded.**

  2. **Remaining time**: Demonstrate your system:
     - Start the server
     - Connect 2–3 clients (separate terminal windows or instances)
     - Show movement appearing on all clients (normal prediction)
     - Demonstrate server rejection (e.g., move into a wall; client snaps back)
     - Show delta compression savings (bytes per tick: full vs delta)

- **Upload** your video to YouTube (unlisted), Google Drive, or similar. Submit the video link along with your repository or project link.

---

## Project Structure

```
projects/07-state-sync/               # boilerplate repo (provided)
├── CMakeLists.txt                    # build config (provided)
├── src/
│   ├── game_state.h      # GameState, PlayerState (or include from Assignment 06)
│   ├── server.h          # AuthoritativeServer class (UDP, tick loop, validation)
│   ├── client.h          # GameClient class (prediction, reconciliation, input buffer)
│   ├── delta.h           # serialize_state_delta, deserialize_state_delta
│   ├── server_main.cpp   # Server executable
│   └── client_main.cpp   # Client executable
└── tests/
    ├── doctest.h         # doctest single-header (provided)
    └── tests.cpp         # Unit tests for validation, reconciliation, delta (provided)
```

---

## Requirements (100 points)

### 1. Shared State and Authoritative Server (25 points)

Implement an authoritative server that owns the game state and validates all client inputs.

#### Game State

Use or define a `PlayerState` compatible with Assignment 06:

```cpp
struct PlayerState {
    uint16_t x;         // 0-1023 (10 bits) — grid position
    uint16_t y;         // 0-1023 (10 bits)
    uint8_t  health;    // 0-100  (7 bits)
    uint16_t heading;   // 0-359  (9 bits)
    uint8_t  team;      // 0-3    (2 bits)
    bool     alive;     //        (1 bit)
    std::string name;   // length-prefixed string
};

struct GameState {
    std::vector<PlayerState> players;  // indexed by player_id
};
```

#### Server Tick Loop

The server runs at 20 Hz. Each tick:

1. Receive UDP packets (inputs from clients)
2. Validate each input
3. Apply valid inputs to authoritative state
4. Broadcast authoritative state (or delta) to all clients

```cpp
class AuthoritativeServer {
public:
    AuthoritativeServer(boost::asio::io_context& io, uint16_t port);
    void tick();  // called 20 times per second
private:
    void receive_inputs();
    void validate_and_apply(PlayerId id, const PlayerInput& input);
    void broadcast_state();
    // ...
};
```

#### Input Validation ("Never Trust the Client")

The server must **reject** invalid inputs. Do **not** accept client-reported position, health, or any critical state.

| Check         | Example                            | Reject if                    |
| ------------- | ---------------------------------- | ---------------------------- |
| **Bounds**    | x, y in [0, 1023]                  | Out of grid                  |
| **Speed**     | Move ≤ 1 cell per tick             | Client reports move > 1 cell |
| **Alive**     | Player must be alive to act        | Dead player sends input      |
| **Ownership** | Input is for the correct player_id | Mismatched id                |

```cpp
// Pseudocode: server validation
bool validate_input(PlayerId id, const PlayerInput& input) {
    PlayerState& p = state.players[id];
    if (!p.alive) return false;

    int dx = 0, dy = 0;
    if (input.move_left)  dx = -1;
    if (input.move_right) dx =  1;
    if (input.move_up)    dy = -1;
    if (input.move_down)  dy =  1;

    if (dx != 0 && dy != 0) return false;  // at most one axis
    if (dx == 0 && dy == 0) return true;   // no move is valid

    int nx = (int)p.x + dx, ny = (int)p.y + dy;
    if (nx < 0 || nx > 1023 || ny < 0 || ny > 1023) return false;
    if (!is_walkable(nx, ny)) return false;  // e.g., wall at (512, 512)
    return true;
}
```

#### Join Protocol

- Client sends `JOIN:username` (or a small binary packet)
- Server assigns a player ID, adds player to `GameState`, sends `JOIN_ACK:player_id`
- Client stores `player_id` for all subsequent packets

---

### 2. Client-Side Prediction and Server Reconciliation (30 points)

Clients predict movement locally for responsiveness, then reconcile when the server replies.

#### Input with Sequence Numbers

Every client input is tagged with a sequence number:

```cpp
struct ClientInput {
    uint32_t seq;        // monotonically increasing
    bool move_left;
    bool move_right;
    bool move_up;
    bool move_down;
};
```

#### Client Algorithm

```
inputQueue = []
seqNum = 0

every tick:
    input = get_player_input()
    seqNum += 1
    input.seq = seqNum

    // 1. Predict locally (optimistic)
    local_state = apply_input(local_state, input)

    // 2. Save for possible replay
    inputQueue.push(input)

    // 3. Send to server
    send_to_server(input)

on server_update(server_state, last_processed_seq):
    // 4. Discard acknowledged inputs
    while inputQueue.not_empty() and inputQueue.front().seq <= last_processed_seq:
        inputQueue.pop_front()

    // 5. Accept server truth
    local_state = server_state

    // 6. Reapply unprocessed inputs
    for each input in inputQueue:
        local_state = apply_input(local_state, input)
```

#### Server Response Format

The server includes `lastProcessedSeq` in each state update so the client knows which inputs were applied:

```
Packet: [message_type][tick][last_processed_seq][state_or_delta]
```

#### Test: Server Rejection

Create a wall on the server (e.g., cells at x=512 are blocked). The client does not know about the wall. When the client predicts moving through it and the server rejects the move, the client must **snap back** to the server state after reconciliation. If using the boilerplate, the provided tests verify this; if using a game engine, your video must show this behavior.

---

### 3. Delta Compression (25 points)

Instead of sending the full game state every tick, send only **changed** fields (deltas).

#### Change Bitmask

For each player, encode a bitmask indicating which fields changed:

```cpp
// Bit positions for change mask (6 bits per player)
enum class FieldBit : uint8_t {
    X = 0, Y = 1, HEALTH = 2, HEADING = 3, TEAM = 4, ALIVE = 5
};

uint8_t compute_change_mask(const PlayerState& current, const PlayerState& baseline);
// Returns a 6-bit mask: bit i set if current.field[i] != baseline.field[i]
```

#### Delta Serialization

```cpp
// Serialize only changed fields for each player
size_t serialize_state_delta(BitWriter& writer,
                             const GameState& current,
                             const GameState& baseline);

// Deserialize delta and apply to baseline (mutates output)
bool deserialize_state_delta(BitReader& reader,
                             const GameState& baseline,
                             GameState& output);
```

#### Baseline Management

- Server tracks `last_acked_state` per client (the state the client confirmed receiving)
- Client sends ACK with tick number when it receives a state update
- Server sends delta from `last_acked_state`; if no ACK yet, send full state for new clients

#### Bandwidth Savings Test

Serialize 10 players where only 2–3 fields changed per player. Compare:

- Full state size: `10 * sizeof(PlayerState)` (or your bitpacked size)
- Delta size: only changed fields + 6-bit mask per player

The delta must be **at least 3× smaller** for this scenario. Print both sizes in your demo.

---

### 4. Integration Demo (20 points)

**Boilerplate users:** The provided doctest suite includes integration tests that verify server + clients, reconciliation, and delta behavior. All tests must pass.

**Game-engine users:** Your video must demonstrate:

1. **Normal movement**: 2–3 clients moving around; movements appear on all clients; prediction feels smooth (no visible lag)
2. **Server rejection**: One client moves toward a wall; client predicts through it, then snaps back when server rejects
3. **Delta savings**: Server prints (or logs) bytes per tick: "Full: X bytes, Delta: Y bytes" for a few ticks

#### Demo Checklist

- [ ] Server starts and binds to UDP port
- [ ] Clients connect and receive join acknowledgment
- [ ] All clients see each other's positions update
- [ ] Client-side prediction: movement feels instant
- [ ] Reconciliation: invalid move causes snap-back
- [ ] Delta compression: logged bytes show clear savings

---

## Extra Credit Features

### P2P Lockstep Mode (+15 points)

Implement a second mode (toggle via `--lockstep` flag) where peers exchange **inputs only** and run a deterministic simulation.

- No server; each peer sends its input to all others
- All peers wait for all inputs before advancing the tick
- Use fixed-point math for determinism (or document floating-point caveats)
- Same `PlayerState` and input format as client-server mode

### Priority Accumulator (+10 points)

When there are more than 20 players, the server uses distance-based priority to fit updates into a 1200-byte packet budget per client.

- Each entity has a priority (inverse distance to the client's player)
- Accumulate priority for entities not sent last tick
- Sort by accumulated priority; pack top N entities that fit
- Reset accumulator for sent entities
- Print which entities were sent vs deferred each tick

### Snapshot + Keyframe (+10 points)

- Send full snapshot every N ticks (e.g., every 50 ticks = every 2.5 seconds) as a keyframe
- Deltas reference the last keyframe (not the last tick)
- Handle packet loss: if client misses a delta, it waits for the next keyframe
- Enables late join: new client requests latest keyframe

### Smooth Correction (+5 points)

Instead of snapping to server state on mismatch, **blend** over several frames:

```cpp
display_state = lerp(display_state, server_state, blend_factor);
```

Use a configurable blend factor (e.g., 0.2). Demonstrate: correction is visually smooth instead of a sudden teleport (video if game engine; test or demo if boilerplate).

### State Hash Verification (+5 points)

- Client and server compute CRC32 (or similar) of the game state each tick
- Client sends its hash with the ACK
- Server compares; if hashes differ, server logs "Desync detected for client X"
- Demonstrates detection of divergent state (e.g., due to packet loss or bug)

### Other Features (+5 points each, up to +10)

Implement and document (in your video if game engine, or in README / test output if boilerplate). Examples:

- **Hex dump of delta packets**: Pretty-print the delta byte stream
- **Bandwidth graph**: Plot bytes per second over time
- **Spectator mode**: Client that receives state but does not send inputs
- **Configurable tick rate**: Server accepts `--tick-rate 30` etc.

---

## Recommended Implementation Order

1. **Game state + server skeleton** — Define `GameState`, `PlayerState`; server binds UDP, receives packets
2. **Input validation** — Implement `validate_input`; add a wall; reject invalid moves
3. **Full state broadcast** — Server serializes full state and sends to all clients (no deltas yet)
4. **Client prediction** — Client applies input locally before server reply
5. **Input buffer + sequence numbers** — Client tags inputs with seq; server echoes lastProcessedSeq
6. **Reconciliation** — On server update: discard acked inputs, accept server state, reapply unacked
7. **Delta serialization** — Implement change bitmask + `serialize_state_delta` / `deserialize_state_delta`
8. **Baseline management** — Server tracks last acked state per client; client sends ACK
9. **Integration** — Run server + clients; for boilerplate run provided tests; for game engine record demo video
10. **Extra credit** — Add lockstep, priority, keyframes, etc.

---

## Grading Rubric

| Component                                                        | Points                  |
| ---------------------------------------------------------------- | ----------------------- |
| **Video (game-engine only)** or **all tests pass (boilerplate)** | Required (0 if missing) |
| Authoritative server + input validation                          | 25                      |
| Client-side prediction + reconciliation                          | 30                      |
| Delta compression (bitmask + baseline)                           | 25                      |
| Integration demo (movement, rejection, delta stats)              | 20                      |
| **Base Total**                                                   | **100**                 |
| Extra: P2P lockstep mode                                         | +15                     |
| Extra: Priority accumulator                                      | +10                     |
| Extra: Snapshot + keyframe                                       | +10                     |
| Extra: Smooth correction                                         | +5                      |
| Extra: State hash verification                                   | +5                      |
| Extra: Other features (max 2)                                    | +5 each (max +10)       |
| **Maximum Total**                                                | **155**                 |

---

## Common Pitfalls

1. **Trusting client state** — Never use client-reported position, health, or score. The server must derive all critical state from validated inputs.

2. **Forgetting to reapply inputs** — After accepting server state, you must reapply all unprocessed inputs from the buffer. If you forget, the player will "teleport back" every server update.

3. **Wrong reconciliation order** — Discard acked inputs first, then set state to server state, then reapply remaining inputs. Doing it in the wrong order breaks prediction.

4. **Delta without baseline** — The receiver needs the baseline to apply a delta. New clients must receive a full snapshot first; the server must track which baseline each client has.

5. **Change mask bit order** — Encoder and decoder must agree on which bit corresponds to which field. Document it and use named constants.

6. **UDP packet loss** — Delta assumes the client received the previous state. If a packet is lost, the next delta may be invalid. Options: periodic full snapshots, redundant deltas, or ACK/retransmit (simplified).

7. **Floating-point in lockstep** — For P2P lockstep, use fixed-point or document that floating-point can cause desync across platforms. Same compiler and CPU family helps but is not guaranteed.

8. **Input buffer overflow** — At 20 Hz and 100 ms RTT, the buffer holds ~2–3 inputs. Cap the buffer size; if it grows too large, drop oldest inputs or throttle.

9. **Server not simulating** — The server must run the same movement logic as the client. If the server uses different rules, reconciliation will constantly correct.

10. **Wall only on server** — For the rejection demo, the wall must exist only in server logic. The client predicts through it; the server rejects; the client reconciles and snaps back.

---

## Resources

- [Gabriel Gambetta: Client-Server Game Architecture](https://www.gabrielgambetta.com/client-server-game-architecture.html)
- [Gabriel Gambetta: Client-Side Prediction and Server Reconciliation](https://www.gabrielgambetta.com/client-side-prediction-server-reconciliation.html)
- [Client-Side Prediction Live Demo](https://www.gabrielgambetta.com/client-side-prediction-live-demo.html) — Interactive demo with latency slider
- [Glenn Fiedler: State Synchronization](https://gafferongames.com/post/state_synchronization/)
- [Boost.Asio UDP Documentation](https://www.boost.org/doc/libs/release/doc/html/boost_asio/reference/ip__udp.html)
- [doctest — C++ Testing Framework](https://github.com/doctest/doctest)
