# Assignment 06: Serialization Library

## Overview

Build a **serialization library** in C++ that implements varint encoding with ZigZag, string serialization, a custom bitpacking read/write stream using Glenn Fiedler's unified pattern, nested struct serialization, and an RPC-style packet dispatch system. Your code will be graded by running a provided [doctest](https://github.com/doctest/doctest) test suite against your implementation.

You may create this on your preferred game engine such as Unity or Unreal, but the serialization code should support what is described below.

> **Endianness**: Use [Boost.Endian](https://www.boost.org/doc/libs/release/libs/endian/) for all byte-order conversions. Do **not** write your own byte-swapping functions. Include `<boost/endian/conversion.hpp>` and use `boost::endian::native_to_big()` / `boost::endian::big_to_native()` where needed (e.g., inside your varint or packet header code).

---

## Submission Requirements

Submit a **GitHub repository link**. The instructor will provide a boilerplate repository with the project structure, `CMakeLists.txt`, `doctest.h`, and `tests.cpp` already set up. You only need to implement the header files.

Requirements:

- Fork/clone the boilerplate repo and implement the required `.h` files
- Your code must compile cleanly with the provided `tests.cpp` (which `#include`s your headers)
- All provided tests must **pass**
- Your `main.cpp` should also print a hex dump demo of a full packet round-trip
- Submit the link to your GitHub repository

---

## Project Structure

```
projects/06-serialization/          # boilerplate repo (provided)
├── CMakeLists.txt                  # build config (provided)
├── src/
│   ├── varint.h        # Varint + ZigZag encoding/decoding (you implement)
│   ├── bitstream.h     # BitWriter / BitReader classes (you implement)
│   ├── serialize.h     # Unified serialize functions + PlayerState (you implement)
│   ├── gameobject.h    # Nested GameObject + Position structs (you implement)
│   ├── packet.h        # RPC packet header and message dispatch (you implement)
│   └── main.cpp        # Demo executable with hex dumps (you implement)
└── tests/
    ├── doctest.h       # doctest single-header (provided)
    └── tests.cpp       # Automated tests (provided — do not modify)
```

---

## Requirements (100 points)

### 1. Varint + ZigZag Encoding (20 points)

Implement variable-length integer encoding with ZigZag in `varint.h`:

```cpp
// Compute the minimum number of bits needed to represent values in [min, max]
constexpr int bits_required(uint32_t min, uint32_t max);

// Encode unsigned varint into buffer. Returns bytes written.
size_t encode_varint(uint32_t value, uint8_t* buffer);

// Decode unsigned varint from buffer. Returns bytes consumed.
size_t decode_varint(const uint8_t* buffer, uint32_t& out_value);

// ZigZag: convert signed to unsigned (small magnitude -> small value)
uint32_t zigzag_encode(int32_t value);
int32_t  zigzag_decode(uint32_t value);

// Convenience: encode/decode signed varint (ZigZag + varint)
size_t encode_signed_varint(int32_t value, uint8_t* buffer);
size_t decode_signed_varint(const uint8_t* buffer, int32_t& out_value);
```

Requirements:

- `bits_required(min, max)` returns the minimum number of bits to represent values in `[min, max]`. You must use this function later in your bitpacking and unified serialization code instead of hardcoding bit widths
- Small positive values (0-127) must encode in 1 byte
- Small negative values (-1, -2, ...) must also encode in 1-2 bytes (via ZigZag)
- `-1` encoded as signed varint must be 1 byte (not 10 bytes)

---

### 2. Bitpacking Stream (25 points)

Implement `BitWriter` and `BitReader` classes in `bitstream.h`:

```cpp
class BitWriter {
public:
    // Write `bits` number of bits from `value` (1-32 bits)
    void write_bits(uint32_t value, int bits);

    // Write a single boolean (1 bit)
    void write_bool(bool value);

    // Flush remaining bits in the scratch buffer to the byte buffer
    void flush();

    // Access the serialized buffer
    const uint8_t* data() const;
    size_t size() const;        // bytes written (after flush)
    size_t bits_written() const; // total bits written
};

class BitReader {
public:
    BitReader(const uint8_t* data, size_t size);

    // Read `bits` number of bits (1-32 bits)
    uint32_t read_bits(int bits);

    // Read a single boolean
    bool read_bool();

    // Total bits consumed
    size_t bits_read() const;
};
```

Requirements:

- Bits are packed contiguously with no padding between fields
- `write_bits(5, 3)` writes the value 5 using exactly 3 bits
- Reading must produce identical values in the same order as writing
- `flush()` pads the final byte with zeros
- `bits_written()` and `bits_read()` track bit-level position

---

### 3. Unified Serialization with Game State (20 points)

Implement Glenn Fiedler's unified serialize pattern and a game state struct in `serialize.h`:

```cpp
struct PlayerState {
    uint16_t    x;         // 0-1023 (10 bits)
    uint16_t    y;         // 0-1023 (10 bits)
    uint16_t    z;         // 0-1023 (10 bits)
    uint8_t     health;    // 0-100  (7 bits)
    uint16_t    heading;   // 0-359  (9 bits)
    uint8_t     team;      // 0-3    (2 bits)
    bool        alive;     //        (1 bit)
    std::string name;      // length-prefixed string (see Section 4)
    // Bitpacked fields: 49 bits = 7 bytes + name string
};

// Unified serialize function: works for both BitWriter and BitReader
template <typename Stream>
bool serialize_player(Stream& stream, PlayerState& player);
```

Requirements:

- **One function** handles both serialization and deserialization (template on stream type)
- `BitWriter` and `BitReader` must implement a compatible interface so the same `serialize_player` function works with both
- **Use `bits_required(min, max)` from Section 1** to compute all bit widths — do not hardcode magic numbers like `10` or `7`. For example: `stream.serialize_bits(player.health, bits_required(0, 100))`
- The `name` field must use your string serialization functions from Section 4
- Round-trip test: create a `PlayerState`, serialize it, deserialize into a new `PlayerState`, verify all fields match
- Include a `main.cpp` demo that:
  1. Creates a `PlayerState` with sample values
  2. Serializes it to a byte buffer
  3. Prints a hex dump of the buffer
  4. Deserializes it back
  5. Prints both states to verify they match

---

### 4. String Serialization (15 points)

Implement length-prefixed string read/write functions. These should work with your `BitWriter`/`BitReader` (byte-aligned writes are fine — flush partial bits, write string bytes, then continue bitpacking):

```cpp
// Write a string: length prefix (varint) followed by raw bytes
void write_string(BitWriter& writer, const std::string& str);

// Read a string: read length prefix (varint), then read that many bytes
std::string read_string(BitReader& reader);

// Unified version (template on stream type)
template <typename Stream>
bool serialize_string(Stream& stream, std::string& str);
```

Requirements:

- String length is encoded as a **varint** prefix (reuse your Section 1 code)
- After the length, write the raw UTF-8 bytes (no null terminator on the wire)
- Empty strings must be supported (length = 0, no data bytes)
- Maximum string length: 255 bytes. Reject longer strings with an error return
- The unified `serialize_string` template must work with both `BitWriter` and `BitReader`
- Strings must survive round-trip: write then read must produce the exact same string

---

### 5. Nested GameObject Serialization (10 points)

Implement serialization for a nested struct in `gameobject.h`. The struct contains a `Position` with float fields and an integer ID. **You are free to choose your own serialization strategy** — raw bytes, bitpacking with quantization, varint encoding, or any combination. The only requirement is a correct round-trip:

```cpp
struct Position {
    float x;
    float y;
    float z;
};

struct GameObject {
    uint32_t id;        // unique object identifier
    Position position;  // nested struct with floats
};

// You decide the serialization approach.
// Implement both a serializer and deserializer.
// A unified template is recommended but not required.

template <typename Stream>
bool serialize_position(Stream& stream, Position& pos);

template <typename Stream>
bool serialize_game_object(Stream& stream, GameObject& obj);
```

Requirements:

- `serialize_game_object` must serialize **both** the `id` and the nested `position` (all 3 floats)
- Round-trip: serialize a `GameObject`, deserialize into a new `GameObject`, verify all fields match
- Float comparison: use an epsilon check (e.g., `std::abs(a - b) < 0.001f`) if you quantize, or exact equality if you serialize raw bytes
- You may use `std::memcpy` to reinterpret floats as `uint32_t` for byte-level serialization, or use your compressed floats from the extra credit, or any other method

---

### 6. RPC — Remote Procedure Call (15 points)

Implement an RPC-style packet system in `packet.h` that frames serialized payloads with a message type and length:

```cpp
enum class MessageType : uint8_t {
    PLAYER_UPDATE   = 1,  // RPC: UpdatePlayer(PlayerState)
    CHAT_MESSAGE    = 2,  // RPC: SendChat(sender, text)
    OBJECT_UPDATE   = 3,  // RPC: UpdateObject(GameObject)
    PING            = 4   // RPC: Ping() — no payload
};

struct PacketHeader {
    MessageType type;       // 1 byte
    uint16_t    payload_len; // 2 bytes, big-endian (use Boost.Endian)
    // Total header: 3 bytes, followed by `payload_len` bytes of payload
};

struct ChatMessage {
    std::string sender;     // length-prefixed string
    std::string text;       // length-prefixed string
};

// Write a complete packet: header + serialized payload
void write_packet(std::vector<uint8_t>& buffer,
                  MessageType type,
                  const uint8_t* payload, size_t payload_len);

// Read a packet header from a buffer. Returns bytes consumed (3).
size_t read_packet_header(const uint8_t* buffer, PacketHeader& header);

// Dispatch: read header, then deserialize the appropriate message type
void dispatch_packet(const uint8_t* buffer, size_t buffer_len);
```

Requirements:

- `write_packet` writes: 1-byte message type + 2-byte big-endian payload length + raw payload bytes
- Use `boost::endian::native_to_big()` / `boost::endian::big_to_native()` for the 2-byte length field
- `dispatch_packet` reads the header, then based on `MessageType`:
  - `PLAYER_UPDATE`: deserialize a `PlayerState` using your unified `serialize_player`
  - `CHAT_MESSAGE`: deserialize sender and text using your `serialize_string`
  - `OBJECT_UPDATE`: deserialize a `GameObject` using your `serialize_game_object` from Section 5
  - `PING`: no payload (length = 0)
- Print what was received (e.g., "RPC PlayerUpdate: x=500, y=300 ..." or "RPC Chat from Alice: Hello!")
- Round-trip test: serialize a message into a packet, parse the packet back, verify the message contents match

---

## Extra Credit Features

### Multiple Player Serialization (+10 points)

Serialize an array of players with a count prefix:

```cpp
template <typename Stream>
bool serialize_game_state(Stream& stream, std::vector<PlayerState>& players);
```

- Write player count as a varint before the bitpacked player data
- Deserialize must reconstruct the vector with the correct number of players
- Print total bytes used and bytes-per-player

### Delta Encoding (+15 points)

Implement delta serialization that only sends changed fields:

```cpp
template <typename Stream>
bool serialize_player_delta(Stream& stream,
                            PlayerState& current,
                            const PlayerState& baseline);
```

- Write a change bitmask (7 bits, one per field)
- Only serialize fields that differ from the baseline
- Demonstrate bandwidth savings: serialize 10 players where only 2-3 fields changed per player, compare total bytes vs. full serialization

### Compressed Floats (+10 points)

Add float quantization utilities:

```cpp
uint32_t compress_float(float value, float min, float max, int bits);
float decompress_float(uint32_t compressed, float min, float max, int bits);
```

- Quantize a float range to N bits
- Demonstrate: position 0.0-100.0m with 0.1m precision using 10 bits
- Round-trip error must be less than the specified precision
- Integrate with the unified serialize pattern

### Packet Integrity with CRC32 (+5 points)

Add a CRC32 checksum:

- Compute CRC32 over the serialized buffer
- Append 4-byte checksum after the data
- Verify on deserialization; reject corrupted packets
- Demonstrate: flip one bit in the buffer, show CRC32 catches it

### Other Extra Features (+5 points each, up to +10)

Implement additional features beyond the base requirements. Examples:

- **Hex dump printer**: Pretty-print serialized bytes with bit annotations
- **Benchmark**: Measure serialize/deserialize throughput (ops/sec)
- **Varint size analyzer**: Print a table of values and their varint byte sizes
- **Protobuf interop**: Serialize/deserialize a `.proto` schema for the same `PlayerState`

---

## Recommended Implementation Order

1. **Implement `bits_required` + ZigZag** -- pure math, easy to test independently
2. **Implement varints** -- test with known values
3. **Build BitWriter** -- the scratch-buffer accumulator pattern
4. **Build BitReader** -- mirror of BitWriter
5. **Implement string serialization** -- length-prefixed with varint; test round-trip
6. **Create the unified serialize function** -- template on stream type, using `bits_required()` for all bit widths
7. **Implement nested GameObject serialization** -- Position with floats + ID
8. **Build the RPC packet system** -- header + payload framing and dispatch for all message types
9. **Write the demo main** -- hex dump and round-trip verification
10. **Add extra credit features** -- delta encoding, compressed floats, CRC32

---

## Grading Rubric

| Component                            | Points                  |
| ------------------------------------ | ----------------------- |
| **All provided tests pass**          | Required (0 if missing) |
| Varint + ZigZag + `bits_required`    | 15                      |
| Bitpacking stream (BitWriter/Reader) | 25                      |
| Unified serialization + PlayerState  | 20                      |
| String serialization                 | 10                      |
| Nested GameObject serialization      | 10                      |
| RPC packet header + dispatch         | 20                      |
| **Base Total**                       | **100**                 |
| Extra: Multiple player serialization | +10                     |
| Extra: Delta encoding                | +15                     |
| Extra: Compressed floats             | +10                     |
| Extra: CRC32 packet integrity        | +5                      |
| Extra: Other features (max 2)        | +5 each (max +10)       |
| **Maximum Total**                    | **150**                 |

---

## Common Pitfalls

1. **ZigZag arithmetic shift** -- `n >> 31` must be an **arithmetic** right shift (sign-extending). In C++ this is implementation-defined for signed types but works on all major compilers. Use `int32_t` to be safe

2. **Varint buffer overflow** -- A `uint32_t` needs at most 5 bytes as a varint. Always allocate enough buffer space

3. **BitWriter forgetting to flush** -- The last partial byte stays in the scratch register until you call `flush()`

4. **Read/write order mismatch** -- If you don't use the unified pattern, reading fields in a different order than writing produces garbage. This is exactly what the unified pattern prevents

5. **Off-by-one in bits_required** -- `ceil(log2(101))` = 7 bits for 0-100, not 6. The formula is `ceil(log2(max - min + 1))`. Also handle the edge case where `min == max` (return 1)

6. **Signed varint without ZigZag** -- Encoding `-1` as a raw unsigned varint produces 10 bytes. Always ZigZag-encode signed values first

7. **String length not varint-encoded** -- Using a fixed `uint32_t` for string length wastes 3 bytes on short strings. Use your varint encoder for the length prefix

8. **Packet header endianness** -- The 2-byte payload length in the packet header must be big-endian on the wire. Forgetting `native_to_big()` will silently produce wrong lengths on little-endian machines

9. **Forgetting to handle empty strings** -- A zero-length string is valid. Your read/write must handle length=0 without reading past the buffer

10. **Dispatch without validating payload length** -- Always check that the buffer has enough remaining bytes (from the header's `payload_len`) before deserializing. Reading past the buffer is undefined behavior

---

## Resources

- [doctest — C++ Testing Framework](https://github.com/doctest/doctest) (single-header, just download `doctest.h`)
- [Glenn Fiedler: Reading and Writing Packets](https://gafferongames.com/post/reading_and_writing_packets/)
- [Glenn Fiedler: Serialization Strategies](https://gafferongames.com/post/serialization_strategies/)
- [Google Protocol Buffers: Encoding](https://protobuf.dev/programming-guides/encoding/)
- [Boost.Endian Documentation](https://www.boost.org/doc/libs/release/libs/endian/)
- [Wikipedia: Variable-length quantity](https://en.wikipedia.org/wiki/Variable-length_quantity)

