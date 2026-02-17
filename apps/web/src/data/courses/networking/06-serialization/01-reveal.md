# Week 06: Serialization

---

## Today's Agenda

1. Why Serialization Matters
2. Endianness and Byte Order
3. Struct Packing and Alignment
4. Text Formats (JSON)
5. Binary Formats (Protobuf, FlatBuffers)
6. Custom Bitpacking
7. Compression Techniques
8. Performance Comparison

---

## Recap: Framing Tells Us Where Messages End

Last week we solved **message boundaries** with length-prefix framing.

Now we need to solve: **what goes inside the payload?**

How do we convert C++ objects into bytes and back?

---

## The Serialization Problem

```mermaid
flowchart LR
    A["C++ Object<br/>position: (x: 1.5, y: 2.0, z: 3.7)"] -->|Serialize| B["Bytes on Wire<br/>3F C0 00 00 40 00 00 00 ..."]
    B -->|Deserialize| C["C++ Object<br/>position: (x: 1.5, y: 2.0, z: 3.7)"]
```

**Serialization** = converting structured data to a flat byte sequence

**Deserialization** = reconstructing structured data from bytes

---

## Why Not Just memcpy the Struct?

```cpp
struct Player {
    uint32_t id;
    float x, y, z;
    uint16_t health;
};

// WRONG: Don't do this!
send(socket, &player, sizeof(Player));
```

Three problems:

1. **Endianness** differs between CPUs
2. **Padding** differs between compilers
3. **No versioning** — can't add fields later

---

## Problem 1: Endianness

Sender stores `uint32_t id = 42` as bytes `2A 00 00 00` (little-endian).

Receiver's CPU expects `00 00 00 2A` (big-endian).

**Receiver reads 704,643,072 instead of 42.**

Same bits, completely wrong number.

---

## Problem 2: Struct Padding

Compilers insert invisible padding bytes between struct fields.

Different compilers (or the same compiler with different flags) produce different layouts.

A 64-bit GCC sender writes 24 bytes. A 32-bit MSVC receiver expects 16.

**The receiver reads corrupted data.**

---

## Problem 3: No Versioning

You add a `uint8_t team;` field to `Player`.

Every existing client breaks immediately — they read the old format and get garbage.

There's no way to handle old-format messages gracefully with raw `memcpy`.

---

## The Serialization Spectrum

```mermaid
flowchart LR
    subgraph HumanReadable ["Human-Readable"]
        JSON["JSON<br/>~80 B/player"]
        XML["XML<br/>~150 B/player"]
    end

    subgraph SchemaBinary ["Schema-Based Binary"]
        PB["Protocol Buffers<br/>~20 B/player"]
        FB["FlatBuffers<br/>~24 B/player"]
    end

    subgraph Custom ["Hand-Tuned"]
        BP["Bitpacking<br/>~4 B/player"]
    end

    HumanReadable -->|"trade readability<br/>for size"| SchemaBinary
    SchemaBinary -->|"trade generality<br/>for compression"| Custom
```

---

## The Solution: Explicit Serialization

Instead of `memcpy`, write explicit serialize/deserialize functions:

```cpp
std::vector<uint8_t> serialize(const Player& p) {
    std::vector<uint8_t> buf;
    buf.reserve(18); // 4 + 4 + 4 + 4 + 2

    auto append = [&buf](auto value) {
        auto net = boost::endian::native_to_big(value);
        const auto* ptr = reinterpret_cast<const uint8_t*>(&net);
        buf.insert(buf.end(), ptr, ptr + sizeof(net));
    };

    append(p.id);
    // float → uint32_t via memcpy, then endian-swap
    uint32_t fx; std::memcpy(&fx, &p.x, 4); append(fx);
    uint32_t fy; std::memcpy(&fy, &p.y, 4); append(fy);
    uint32_t fz; std::memcpy(&fz, &p.z, 4); append(fz);
    append(p.health);
    return buf;
}
```

---

## The Read/Write Mismatch Problem

Separate serialize and deserialize functions must stay perfectly in sync:

```cpp
// serialize.cpp — writes id, x, y, z, health
void write(Buffer& buf, const Player& p) {
    write_u32(buf, p.id);
    write_f32(buf, p.x);
    write_f32(buf, p.y);
    write_f32(buf, p.z);
    write_u16(buf, p.health);
}

// deserialize.cpp — OOPS: forgot to read y!
void read(Buffer& buf, Player& p) {
    p.id     = read_u32(buf);
    p.x      = read_f32(buf);
    // p.y   = read_f32(buf);  ← MISSING!
    p.z      = read_f32(buf);  // reads y's bytes → wrong
    p.health = read_u16(buf);  // reads z's bytes → wrong
}
```

Every field you add, remove, or reorder must be mirrored **exactly** in both functions. This is the #1 source of serialization bugs.

---

## Glenn Fiedler's Unified Pattern

Write **one** function that works for both reading and writing using C++ templates:

```cpp
template <typename Stream>
bool serialize(Stream& stream, Player& player) {
    serialize_uint32(stream, player.id);
    serialize_float(stream, player.x);
    serialize_float(stream, player.y);
    serialize_float(stream, player.z);
    serialize_uint16(stream, player.health);
    return true;
}
```

The **same code path** handles both directions — mismatch is impossible:

```cpp
WriteStream writer(buffer, bufferSize);
serialize(writer, player);  // template instantiates write version

ReadStream reader(buffer, bytesReceived);
serialize(reader, player);  // template instantiates read version
```

---

## How Does One Function Do Both?

`WriteStream` and `ReadStream` implement the same interface but do opposite things:

```cpp
class WriteStream {
public:
    void serialize_uint32(uint32_t& value) {
        write_bytes(&value, 4);  // writes value to buffer
    }
};

class ReadStream {
public:
    void serialize_uint32(uint32_t& value) {
        read_bytes(&value, 4);   // reads buffer into value
    }
};
```

The `serialize()` function calls `stream.serialize_uint32(player.id)`. When `Stream = WriteStream`, it writes. When `Stream = ReadStream`, it reads. **Same field order, guaranteed.**

This is the core pattern behind netcode in Overwatch, Rocket League, and Glenn Fiedler's [gaffer.on.games](https://gafferongames.com/) articles.

---

## Endianness: The Byte Order Problem

The integer `0x12345678` in memory:

| Byte Order    | Address 0 | Address 1 | Address 2 | Address 3 |
| ------------- | --------- | --------- | --------- | --------- |
| Big-endian    | `12`      | `34`      | `56`      | `78`      |
| Little-endian | `78`      | `56`      | `34`      | `12`      |

x86/x64/ARM = little-endian

Network standard = big-endian

---

## Endianness: Visual Layout

```mermaid
packet-beta
title "Big-Endian (Network Byte Order)"
0-7: "0x12"
8-15: "0x34"
16-23: "0x56"
24-31: "0x78"
```

```mermaid
packet-beta
title "Little-Endian (x86/ARM)"
0-7: "0x78"
8-15: "0x56"
16-23: "0x34"
24-31: "0x12"
```

---

## Which Architectures Use What?

| Architecture       | Byte Order    | Examples                            |
| ------------------ | ------------- | ----------------------------------- |
| x86, x64           | Little-endian | Intel, AMD desktop/server CPUs      |
| ARM (default mode) | Little-endian | Mobile devices, Apple Silicon, RPi  |
| Network standard   | Big-endian    | TCP/IP headers (RFC 1700)           |
| PowerPC, SPARC     | Big-endian    | Older game consoles (PS3, Xbox 360) |

If a LE machine sends `uint32_t value = 1` as raw bytes, a BE machine reads **16,777,216**.

---

## Boost.Endian: Modern C++ Solution

```cpp
#include <boost/endian/conversion.hpp>

uint32_t value = 0x12345678;

// Convert to network byte order (big-endian)
uint32_t net = boost::endian::native_to_big(value);

// Convert back to host byte order
uint32_t host = boost::endian::big_to_native(net);
```

Alternatively, use `htonl`/`ntohl` for 32-bit integers, but Boost.Endian is more flexible and modern.

---

## Handling Floats: The Tricky Part

Floats also have endianness. Reinterpret as `uint32_t`, swap, transmit:

```cpp
void write_float(uint8_t* dest, float value) {
    uint32_t bits;
    std::memcpy(&bits, &value, sizeof(bits));
    bits = boost::endian::native_to_big(bits);
    std::memcpy(dest, &bits, sizeof(bits));
}

float read_float(const uint8_t* src) {
    uint32_t bits;
    std::memcpy(&bits, src, sizeof(bits));
    bits = boost::endian::big_to_native(bits);
    float value;
    std::memcpy(&value, &bits, sizeof(value));
    return value;
}
```

C++20 alternative: `std::bit_cast<uint32_t>(my_float)` — cleaner, constexpr.

---

## Endianness: Common Mistakes

| Mistake                                   | Consequence                            |
| ----------------------------------------- | -------------------------------------- |
| Forgetting to convert before sending      | Receiver reads garbage values          |
| Converting twice (send + receive swap)    | Double-swap = correct only by accident |
| Using `htonl` on a `float`                | Wrong — `htonl` takes `uint32_t`       |
| Assuming all platforms are little-endian  | Breaks on PowerPC, big-endian ARM mode |
| Not converting length-prefix header bytes | Framing logic reads wrong message size |

---

## Struct Padding: The Hidden Bytes

```cpp
struct BadLayout {
    char a;      // 1 byte
    // 3 bytes padding!
    int32_t b;   // 4 bytes
    char c;      // 1 byte
    // 3 bytes padding!
};
// sizeof = 12, not 6!
```

The compiler inserts invisible bytes to satisfy CPU alignment requirements.

---

## Struct Padding: Memory Layout

```mermaid
packet-beta
title "struct BadLayout — actual memory"
0-7: "a (1B)"
8-15: "pad"
16-23: "pad"
24-31: "pad"
32-39: "b byte 0"
40-47: "b byte 1"
48-55: "b byte 2"
56-63: "b byte 3"
64-71: "c (1B)"
72-79: "pad"
80-87: "pad"
88-95: "pad"
```

6 bytes of data, 6 bytes of invisible padding = 12 bytes total.

---

## Alignment Rules

| Type      | Size    | Alignment | Rule                                 |
| --------- | ------- | --------- | ------------------------------------ |
| `char`    | 1 byte  | 1         | Can be placed anywhere               |
| `int16_t` | 2 bytes | 2         | Must start at even address           |
| `int32_t` | 4 bytes | 4         | Must start at address divisible by 4 |
| `int64_t` | 8 bytes | 8         | Must start at address divisible by 8 |
| `float`   | 4 bytes | 4         | Must start at address divisible by 4 |
| `double`  | 8 bytes | 8         | Must start at address divisible by 8 |

Rule of thumb: order fields from **largest to smallest** to minimize padding.

---

## Padding Varies Across Platforms

```cpp
struct CrossPlatform {
    char a;
    double b;
    char c;
};
```

| Platform          | sizeof | Layout                               |
| ----------------- | ------ | ------------------------------------ |
| x86-64 GCC/Clang  | 24     | a(1) + pad(7) + b(8) + c(1) + pad(7) |
| x86 MSVC (32-bit) | 16     | a(1) + pad(3) + b(8) + c(1) + pad(3) |
| ARM (packed mode) | 10     | a(1) + b(8) + c(1)                   |

Same struct definition, three different sizes. **`memcpy` breaks.**

---

## Optimizing Struct Layout

```cpp
// BAD: 24 bytes (wasted padding)
struct Bad {
    char a;       // 1 + 7 pad
    double b;     // 8
    char c;       // 1 + 7 pad
};

// GOOD: 16 bytes (minimal padding)
struct Good {
    double b;     // 8
    char a;       // 1
    char c;       // 1 + 6 pad
};
```

Order fields from **largest alignment to smallest**: `double` → `int64_t` → `float`/`int32_t` → `int16_t` → `char`.

---

## Don't Use `#pragma pack` for Networking

```cpp
#pragma pack(push, 1)
struct Packed {
    char a;       // 1 byte
    int32_t b;    // 4 bytes (NO padding before)
    char c;       // 1 byte
};
#pragma pack(pop)
// sizeof(Packed) == 6
```

Problems:

- **Slower** on x86 (unaligned access penalty)
- **Hardware fault** on some ARM configurations
- **Still doesn't fix endianness**

Use explicit serialization functions instead.

**Never send raw structs over the network.**

---

## Text Formats: JSON

```json
{
  "id": 42,
  "position": { "x": 1.5, "y": 2.0, "z": 3.7 },
  "health": 100
}
```

**Pros:** Human-readable, self-describing, universal

**Cons:** Verbose (~2-10× larger than binary), slow to parse, no fixed schema

**Use for:** Config files, REST APIs, debugging

**Avoid for:** 60 Hz game state updates

---

## JSON Value Types

```mermaid
flowchart TD
    V[JSON Value] --> O["Object: curly braces with key:value pairs"]
    V --> A["Array: square brackets with values"]
    V --> S["String: double-quoted text"]
    V --> N["Number: integer or float"]
    V --> B["Boolean: true / false"]
    V --> NL["Null: null"]
```

Six types total. No comments, no trailing commas, must be UTF-8 (RFC 8259).

---

## JSON in C++: nlohmann/json

```cpp
#include <nlohmann/json.hpp>
using json = nlohmann::json;

// Serialize
json j;
j["id"] = 42;
j["position"] = {{"x", 1.5}, {"y", 2.0}, {"z", 3.7}};
j["health"] = 100;
std::string wire = j.dump();  // compact JSON

// Deserialize
json parsed = json::parse(wire);
uint32_t id = parsed["id"];
float x = parsed["position"]["x"];
```

---

## JSON: Size Problem

```
{"x":1.5,"y":2.0,"z":3.7}  = 27 bytes (text)
3 raw floats                = 12 bytes (binary)
3 × 10-bit bitpacked       =  4 bytes (bitpacked)
```

JSON is **7× larger** than bitpacked for the same data.

At 64 Hz with 20 players, that's the difference between 125 KB/s and 2 MB/s.

---

## Other Text Formats

| Format | Nesting | Types    | Readability | Use Case             |
| ------ | ------- | -------- | ----------- | -------------------- |
| JSON   | Yes     | 6 types  | Good        | APIs, interchange    |
| CSV    | No      | Strings  | Excellent   | Tabular data, export |
| XML    | Yes     | Strings  | Poor        | Legacy, SOAP         |
| YAML   | Yes     | Rich     | Excellent   | Config files         |
| TOML   | Limited | Explicit | Excellent   | Config files         |

- **Data interchange:** JSON
- **Config files:** TOML (simple) or YAML (complex)
- **Real-time network data:** None — use binary formats

---

## Binary Formats: Protocol Buffers

```protobuf
message Player {
    uint32 id = 1;
    float x = 2;
    float y = 3;
    float z = 4;
    uint32 health = 5;
}
```

- The numbers (`= 1`, `= 2`) are **field tags**, not default values
- They identify fields on the wire and must never change once deployed
- Schema evolution: add fields without breaking old clients

---

## Protobuf: Varints

Variable-length integer encoding — small values use fewer bytes:

```
Value 1:     0x01              → 1 byte
Value 127:   0x7F              → 1 byte
Value 128:   0x80 0x01         → 2 bytes
Value 300:   0xAC 0x02         → 2 bytes
Value 16384: 0x80 0x80 0x01    → 3 bytes
```

Each byte: 7 bits for data + 1 bit (MSB) as continuation flag.

**MSB** = **Most Significant Bit** — the highest-order (leftmost) bit in a byte.

MSB = 1 → "more bytes follow." MSB = 0 → "last byte."

---

## Protobuf: Encoding 300 as a Varint

```
300 in binary:   1 0010 1100

Split into 7-bit groups:
  0000010   0101100

Add continuation bits:
  00000010  10101100

Reverse (little-endian varint order):
  10101100  00000010

Wire bytes: 0xAC  0x02
```

Only 2 bytes instead of 4 for a `uint32_t`.

---

## Protobuf: Tag-Length-Value (TLV)

Each field on the wire:

```mermaid
packet-beta
title "Protobuf Field Encoding"
0-7: "Tag (varint)"
8-15: "Length (varint)"
16-47: "Value (variable)"
```

Tag encodes field number + wire type: `tag = (field_number << 3) | wire_type`

| Wire Type | Meaning | Used For                       |
| --------- | ------- | ------------------------------ |
| 0         | VARINT  | int32, uint32, bool, enum      |
| 1         | I64     | fixed64, double                |
| 2         | LEN     | string, bytes, nested messages |
| 5         | I32     | fixed32, float                 |

---

## The Problem: Signed Integers in Varints

Varints encode **unsigned** integers efficiently: small values use few bytes.

But `int32 = -1` in two's complement is `0xFFFFFFFF` (all bits set) — varint treats this as a huge unsigned number:

```
-1 as uint32 = 4,294,967,295 → varint needs 10 bytes!
-2 as uint32 = 4,294,967,294 → varint needs 10 bytes!
```

Every negative `int32` costs **10 bytes** because the MSB (sign bit) is always 1, so the upper bits are all set.

Varints are an encoding — they don't know about signs. We need a **preprocessing step**.

---

## ZigZag: Making Signed Values Varint-Friendly

**ZigZag is not a replacement for varints — it's a transform applied before varint encoding.**

It interleaves positive and negative values so small-magnitude numbers map to small unsigned numbers:

```
 0 → 0        (small magnitude → small varint)
-1 → 1
 1 → 2
-2 → 3
 2 → 4
-3 → 5
 3 → 6
```

Formula: `zigzag(n) = (n << 1) ^ (n >> 31)` (for 32-bit)

Reverse: `original(z) = (z >>> 1) ^ -(z & 1)`

---

## ZigZag + Varint: The Full Pipeline

```
Signed value → ZigZag transform → Varint encode → Wire bytes
```

| Signed Value | Without ZigZag (raw varint) | With ZigZag → Varint |
| ------------ | --------------------------- | -------------------- |
| `0`          | 1 byte                      | 1 byte               |
| `1`          | 1 byte                      | 1 byte               |
| `-1`         | **10 bytes**                | 1 byte               |
| `63`         | 1 byte                      | 1 byte               |
| `-64`        | **10 bytes**                | 1 byte               |
| `300`        | 2 bytes                     | 2 bytes              |
| `-300`       | **10 bytes**                | 2 bytes              |

ZigZag saves **8 bytes per negative value** when magnitudes are small.

Protobuf uses `sint32`/`sint64` types for ZigZag; plain `int32` uses raw varint (wasteful for negatives).

---

## Protobuf: Encoded Size Example

```
Player { id=42, x=1.5, y=2.0, z=3.7, health=100 }
```

| Field     | Tag  | Wire Type | Value Bytes | Total   |
| --------- | ---- | --------- | ----------- | ------- |
| id        | 0x08 | VARINT    | 0x2A (1B)   | 2B      |
| x         | 0x15 | I32       | 4 bytes     | 5B      |
| y         | 0x1D | I32       | 4 bytes     | 5B      |
| z         | 0x25 | I32       | 4 bytes     | 5B      |
| health    | 0x28 | VARINT    | 0x64 (1B)   | 2B      |
| **Total** |      |           |             | **19B** |

JSON `{"id":42,"x":1.5,"y":2.0,"z":3.7,"health":100}` = **49 bytes**. Protobuf = 2.6× smaller.

---

## Schema Evolution

Add fields without breaking old clients:

```protobuf
// Version 2
message Player {
    uint32 id     = 1;
    float  x      = 2;
    float  y      = 3;
    float  z      = 4;
    uint32 health = 5;
    string name   = 6;  // NEW — old clients skip unknown tag 6
}
```

Rules:

- **Never reuse** a field number
- **Never change** a field's wire type
- New fields must have default values (proto3: all do)

---

## FlatBuffers: Zero-Copy Reads

Protobuf requires a deserialization step. FlatBuffers reads **directly from the buffer**:

```mermaid
flowchart TD
    subgraph Protobuf
        PW["Wire Bytes"] -->|Parse| PM["In-Memory Object"]
        PM -->|Access| PV["field.x"]
    end

    subgraph FlatBuffers
        FW["Wire Bytes"] -->|"Cast pointer<br/>(no copy)"| FV["field.x"]
    end
```

- No allocation during deserialization
- No parse time — just a pointer cast
- Buffer can be memory-mapped from a file
- Slightly larger than Protobuf for small messages (~36B vs ~19B)

---

## Self-Describing Binary Formats

| Format      | Description                       | Size vs JSON |
| ----------- | --------------------------------- | ------------ |
| MessagePack | "JSON but binary," maps directly  | ~30% smaller |
| CBOR        | RFC 8949, for IoT/constrained     | ~40% smaller |
| BSON        | MongoDB wire format, extends JSON | Similar size |

These include field names in the data (like JSON) but encode values in binary.

No schema required, but less compact than Protobuf.

---

## Custom Bitpacking: The Core Insight

If you know a value's range, you can calculate the minimum bits required:

$$\text{bits\_required}(\min, \max) = \lceil \log_2(\max - \min + 1) \rceil$$

| Value               | Range       | Bits Required | vs `float` (32 bits) |
| ------------------- | ----------- | ------------- | -------------------- |
| Health (0–100)      | 101 values  | 7 bits        | 4.6× savings         |
| Position X (0–1023) | 1024 values | 10 bits       | 3.2× savings         |
| Heading (0–359°)    | 360 values  | 9 bits        | 3.6× savings         |
| Team (0–3)          | 4 values    | 2 bits        | 16× savings          |
| Alive (bool)        | 2 values    | 1 bit         | 32× savings          |

---

## Bitpacking: Maximum Compression

Instead of 3 × `float` (96 bits) for position:

```
Position range: 0–1023 → needs 10 bits each
Total: 30 bits vs 96 bits = 3.2× compression
```

```cpp
void serialize(BitWriter& writer, const Position& pos) {
    writer.write_bits(pos.x, 10);  // 0–1023
    writer.write_bits(pos.y, 10);  // 0–1023
    writer.write_bits(pos.z, 10);  // 0–1023
}
```

Used by AAA games: Overwatch, Rocket League, Mortal Kombat

---

## BitWriter: The Serializer

```cpp
class BitWriter {
public:
    void write_bits(uint32_t value, int bits) {
        assert(bits > 0 && bits <= 32);
        scratch_ |= (uint64_t(value) << scratch_bits_);
        scratch_bits_ += bits;
        while (scratch_bits_ >= 8) {
            buffer_.push_back(uint8_t(scratch_ & 0xFF));
            scratch_ >>= 8;
            scratch_bits_ -= 8;
        }
    }
    void write_bool(bool v) { write_bits(v ? 1 : 0, 1); }
    void flush() { /* push remaining scratch bits */ }

private:
    std::vector<uint8_t> buffer_;
    uint64_t scratch_ = 0;
    int scratch_bits_ = 0;
};
```

---

## BitReader: The Deserializer

```cpp
class BitReader {
public:
    BitReader(const uint8_t* data, size_t size)
        : data_(data), size_(size) {}

    uint32_t read_bits(int bits) {
        while (scratch_bits_ < bits && byte_offset_ < size_) {
            scratch_ |= uint64_t(data_[byte_offset_++])
                        << scratch_bits_;
            scratch_bits_ += 8;
        }
        uint32_t value = scratch_ & ((1ULL << bits) - 1);
        scratch_ >>= bits;
        scratch_bits_ -= bits;
        return value;
    }
    bool read_bool() { return read_bits(1) != 0; }

private:
    const uint8_t* data_;
    size_t size_, byte_offset_ = 0;
    uint64_t scratch_ = 0;
    int scratch_bits_ = 0;
};
```

---

## Bitpacking: Full Example

```cpp
// Serialize: 49 bits = 7 bytes
BitWriter writer;
writer.write_bits(player.x,       10);  // 0–1023
writer.write_bits(player.y,       10);  // 0–1023
writer.write_bits(player.z,       10);  // 0–1023
writer.write_bits(player.health,   7);  // 0–100
writer.write_bits(player.heading,  9);  // 0–359
writer.write_bits(player.team,     2);  // 0–3
writer.write_bool(player.alive);        // 1 bit
writer.flush();

// Deserialize: read in the SAME ORDER
BitReader reader(writer.data(), writer.size());
player.x       = reader.read_bits(10);
player.y       = reader.read_bits(10);
player.z       = reader.read_bits(10);
player.health  = reader.read_bits(7);
player.heading = reader.read_bits(9);
player.team    = reader.read_bits(2);
player.alive   = reader.read_bool();
```

---

## Glenn Fiedler's Unified Bitpacking

The same unified pattern, but now with **bit-level precision** — each field specifies its exact bit count:

```cpp
template <typename Stream>
bool serialize_player(Stream& stream, Player& player) {
    serialize_bits(stream, player.x,       10);  // 0–1023
    serialize_bits(stream, player.y,       10);  // 0–1023
    serialize_bits(stream, player.z,       10);  // 0–1023
    serialize_bits(stream, player.health,   7);  // 0–100
    serialize_bits(stream, player.heading,  9);  // 0–359
    serialize_bits(stream, player.team,     2);  // 0–3
    serialize_bool(stream, player.alive);         // 1 bit
    return true;  // total: 49 bits = 7 bytes
}
```

Usage is identical to the byte-level version:

```cpp
BitWriter writer(buffer, size);
serialize_player(writer, player);  // writes 49 bits

BitReader reader(buffer, received);
serialize_player(reader, player);  // reads 49 bits
```

Add a new field? **One line, one place.** No read/write mismatch possible.

---

## Compressed Floats

Full IEEE-754 floats use 32 bits, but game values have known ranges. **Quantize** the float to an integer:

```cpp
uint32_t compress_float(float value,
        float min_val, float max_val, int bits) {
    float normalized = (value - min_val)
                     / (max_val - min_val);  // [0, 1]
    uint32_t max_int = (1u << bits) - 1;
    return uint32_t(normalized * max_int + 0.5f);
}

float decompress_float(uint32_t compressed,
        float min_val, float max_val, int bits) {
    uint32_t max_int = (1u << bits) - 1;
    float normalized = float(compressed) / float(max_int);
    return min_val + normalized * (max_val - min_val);
}
```

Position in 100m × 100m with 0.1m precision: 10 bits per axis = 30 bits vs 96 bits.

---

## Compressed Quaternions: Smallest Three

Quaternions: `(w, x, y, z)` where `w² + x² + y² + z² = 1`. Exploit the constraint:

1. Find the component with the largest absolute value
2. Store which component (2 bits — one of w, x, y, z)
3. Store the other three (they're in \[-0.707, 0.707\])
4. Reconstruct the largest from `w² + x² + y² + z² = 1`

**Size: 2 + 3 × 9 = 29 bits** vs 4 × 32 = 128 bits. **4.4× compression.**

Overwatch uses 9 bits per component. Precision error: ~0.06° — imperceptible in gameplay.

---

## CRC32 Integrity Checksums

Append a 4-byte checksum after serialization to detect corruption:

```cpp
#include <boost/crc.hpp>

void append_crc(std::vector<uint8_t>& buffer) {
    boost::crc_32_type crc;
    crc.process_bytes(buffer.data(), buffer.size());
    uint32_t checksum = crc.checksum();
    const auto* p = reinterpret_cast<const uint8_t*>(&checksum);
    buffer.insert(buffer.end(), p, p + 4);
}

bool verify_crc(const uint8_t* data, size_t total_size) {
    if (total_size < 4) return false;
    boost::crc_32_type crc;
    crc.process_bytes(data, total_size - 4);
    uint32_t expected;
    std::memcpy(&expected, data + total_size - 4, 4);
    return crc.checksum() == expected;
}
```

CRC detects accidental corruption, **not** malicious tampering (use HMAC for that).

---

## Compression: The Pipeline

```mermaid
flowchart LR
    A["Full Game State"] -->|Delta Encoding| B["Only Changes"]
    B -->|Quantization| C["Reduced Precision"]
    C -->|Bitpacking| D["Minimum Bits"]
    D -->|"Optional: LZ4"| E["Wire Bytes"]
```

| Stage          | 100 players × 3 floats | Size   |
| -------------- | ---------------------- | ------ |
| Raw            | 100 × 12 bytes         | 1200 B |
| Delta encoding | ~30 changed × 12 bytes | 360 B  |
| Quantization   | 30 × 6 bytes           | 180 B  |
| Bitpacking     | 30 × 30 bits           | 113 B  |
| LZ4            | ~90 B                  | ~90 B  |

---

## Delta Encoding

Send the **difference** from the previous value, not the absolute:

```cpp
// Full state: position = {500, 300, 100}
// Previous:   position = {498, 300, 101}
// Delta:      position = {  2,   0,  -1}
```

Deltas are typically much smaller:

- Varints use fewer bytes (small deltas → 1-2 bytes)
- Many deltas are zero (unchanged → skip entirely)
- Overwatch: ~80% of fields unchanged per frame

---

## Delta Encoding: Code

```cpp
void serialize_delta(BitWriter& writer,
                     const Player& current,
                     const Player& previous) {
    uint32_t mask = 0;
    if (current.x != previous.x)           mask |= (1 << 0);
    if (current.y != previous.y)           mask |= (1 << 1);
    if (current.z != previous.z)           mask |= (1 << 2);
    if (current.health != previous.health) mask |= (1 << 3);

    writer.write_bits(mask, 4);  // which fields changed

    if (mask & (1 << 0)) writer.write_bits(current.x, 10);
    if (mask & (1 << 1)) writer.write_bits(current.y, 10);
    if (mask & (1 << 2)) writer.write_bits(current.z, 10);
    if (mask & (1 << 3)) writer.write_bits(current.health, 7);
}
```

If nothing changed: only 4 bits sent (the zero mask).

---

## Quantization

Reduce precision to use fewer bits. **Lossy** — you sacrifice precision for bandwidth:

```cpp
// 0.0–100.0 meters with 1cm precision
// 10,000 steps → 14 bits (vs 32 for float)
uint16_t quantize(float pos) {
    return uint16_t(pos * 100.0f + 0.5f);
}
float dequantize(uint16_t q) {
    return float(q) / 100.0f;
}
```

| Value        | Range      | Precision | Bits | vs Float |
| ------------ | ---------- | --------- | ---- | -------- |
| Map position | 0–4096m    | 10 cm     | 16   | 2×       |
| Velocity     | -50–50 m/s | 0.1 m/s   | 10   | 3.2×     |
| Angle        | 0–360°     | ~0.35°    | 10   | 3.2×     |
| Health       | 0–100      | 1 HP      | 7    | 4.6×     |

---

## Variable-Length Quantities (VLQ)

Same concept as protobuf varints, generalized:

| Value     | VLQ Bytes | uint32_t Bytes | Savings |
| --------- | --------- | -------------- | ------- |
| 0–127     | 1         | 4              | 75%     |
| 128–16383 | 2         | 4              | 50%     |
| 16384+    | 3+        | 4              | varies  |

Ideal for values that are **usually small** but occasionally large (array lengths, entity counts).

---

## General-Purpose Compressors

### LZ4: Speed-Optimized

```
Compression:   ~780 MB/s
Decompression: ~4,200 MB/s
Ratio:         ~2.1×
```

Decompression is nearly free. Ideal for real-time data.

### Zstandard (zstd): Ratio-Optimized

```
Compression:   ~500 MB/s (L1) – ~10 MB/s (L19)
Decompression: ~1,700 MB/s
Ratio:         ~2.8× (L1) – ~4.5× (L19)
```

Use lower levels for real-time data, higher for replays and asset bundles.

---

## When to Compress

| Use Case                     | Compressor    | Why                                       |
| ---------------------------- | ------------- | ----------------------------------------- |
| Real-time game packets       | LZ4 or none   | Decompression must be < 1ms               |
| Replay files                 | Zstandard     | Ratio > speed                             |
| Asset bundles / downloads    | Zstandard L19 | Best ratio, one-time decompression        |
| Already-bitpacked game state | Usually none  | Bitpacked data has little redundancy left |

If you've done thorough bitpacking, LZ4 may **increase** size (framing overhead on high-entropy data).

---

## Format Comparison

| Format           | Size (Player) | Parse Speed | Schema? | Human-Readable? |
| ---------------- | ------------- | ----------- | ------- | --------------- |
| JSON             | ~80 bytes     | Slow        | No      | Yes             |
| Protocol Buffers | ~20 bytes     | Fast        | Yes     | No              |
| FlatBuffers      | ~24 bytes     | Zero-copy   | Yes     | No              |
| Custom bitpack   | ~4 bytes      | Fastest     | Manual  | No              |

---

## Bandwidth at Scale

What does each format cost across 60-tick, 100-player server?

| Format    | Per-Player | Per-Tick (100p) | Per-Second (60 Hz) | Per-Minute |
| --------- | ---------- | --------------- | ------------------ | ---------- |
| JSON      | 80 B       | 8 KB            | 480 KB/s           | 28.1 MB    |
| Protobuf  | 20 B       | 2 KB            | 120 KB/s           | 7.0 MB     |
| FlatBuf   | 24 B       | 2.4 KB          | 144 KB/s           | 8.4 MB     |
| Bitpacked | 4 B        | 400 B           | 24 KB/s            | 1.4 MB     |

**20× bandwidth difference** between JSON and bitpacked. That difference defines your server costs.

---

## Decision Flowchart

```mermaid
flowchart TD
    A["Need to serialize data?"] --> B{"Human-readable<br/>required?"}
    B -->|Yes| C["JSON / YAML"]
    B -->|No| D{"Schema evolution<br/>needed?"}
    D -->|Yes| E{"Zero-copy<br/>important?"}
    D -->|No| F{"Every bit<br/>counts?"}
    E -->|Yes| G["FlatBuffers"]
    E -->|No| H["Protocol Buffers"]
    F -->|Yes| I["Custom Bitpacking"]
    F -->|No| J["Protobuf or<br/>MessagePack"]
```

---

## Common Serialization Mistakes

| Mistake                              | Consequence               | Fix                            |
| ------------------------------------ | ------------------------- | ------------------------------ |
| `memcpy` struct to socket            | Padding/endian mismatches | Explicit serialize each field  |
| Trust deserialized data              | Crash, exploit            | Validate after deserialize     |
| Ignore version field                 | Can't update protocol     | Always include version/magic   |
| Float equality for delta compression | Missed updates            | Use epsilon or integer compare |
| Over-compress already-bitpacked data | Size increases (overhead) | Measure before adding LZ4      |
| Send full state every tick           | Bandwidth explosion       | Delta + change mask            |

---

## Serialization Checklist

Before shipping your netcode, verify:

- [ ] **Byte order:** All multibyte values converted (Boost.Endian)
- [ ] **No struct padding:** Fields serialized individually
- [ ] **Version field:** Magic number + version in packet header
- [ ] **Validation:** Bounds-check every deserialized value
- [ ] **Delta encoding:** Only send what changed
- [ ] **Quantization:** Precision matched to gameplay needs
- [ ] **Bit budget:** Measured bits per entity per tick
- [ ] **Compression:** Measured whether LZ4 actually helps _your_ data
- [ ] **Checksums:** CRC32 on all packets
- [ ] **Fuzz testing:** Random bytes don't crash deserializer

---

## Key Takeaways

1. **Never `memcpy` structs** to the network — padding and endianness break portability
2. **Boost.Endian** for byte order conversion — replaces legacy `htonl`/`ntohl`
3. **JSON** for human-readable interchange; **binary** for performance
4. **Protocol Buffers** when you need schema evolution and industry tooling
5. **Custom bitpacking** when every bit counts (real-time games)
6. **Always validate** deserialized data — never trust the wire

---

## The Serialization Spectrum

```mermaid
flowchart LR
    JSON["JSON"] --> MP["MessagePack"] --> PB["Protobuf"] --> FB["FlatBuffers"] --> BP["Custom Bitpack"]

    style JSON fill:#4CAF50,color:#fff
    style MP fill:#8BC34A,color:#fff
    style PB fill:#FFC107,color:#000
    style FB fill:#FF9800,color:#fff
    style BP fill:#F44336,color:#fff
```

**Human-Friendly** &larr;&mdash;&mdash;&mdash;&mdash;&mdash;&mdash;&mdash;&mdash;&mdash;&rarr; **Machine-Friendly**

Choose the **simplest format** that meets your bandwidth and latency requirements.
Move right only when measurements prove you must.
