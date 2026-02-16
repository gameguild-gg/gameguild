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
    A[C++ Object<br/>position: {x: 1.5, y: 2.0, z: 3.7}] -->|Serialize| B[Bytes on Wire<br/>3F C0 00 00 40 00 00 00 ...]
    B -->|Deserialize| C[C++ Object<br/>position: {x: 1.5, y: 2.0, z: 3.7}]
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

## Endianness: The Byte Order Problem

The integer `0x12345678` in memory:

| Byte Order    | Address 0 | Address 1 | Address 2 | Address 3 |
| ------------- | --------- | --------- | --------- | --------- |
| Big-endian    | `12`      | `34`      | `56`      | `78`      |
| Little-endian | `78`      | `56`      | `34`      | `12`      |

x86/x64/ARM = little-endian

Network standard = big-endian

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

No more `htonl`/`ntohl`—type-safe, works with any integer type.

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

```cpp
struct GoodLayout {
    int32_t b;   // 4 bytes
    char a;      // 1 byte
    char c;      // 1 byte
    // 2 bytes padding
};
// sizeof = 8
```

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

- Varints compress small integers (1 byte for values < 128)
- Tag-Length-Value encoding
- Schema evolution (add fields without breaking old clients)
- ~2-10× smaller than JSON

---

## Custom Bitpacking: Maximum Compression

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

## Compression Techniques

| Technique      | Idea                                       | Use Case               |
| -------------- | ------------------------------------------ | ---------------------- |
| Delta encoding | Send differences, not absolutes            | Game state snapshots   |
| Quantization   | Reduce precision (float → fixed-point)     | Positions, rotations   |
| VLQ / Varints  | Small values use fewer bytes               | Protobuf, MIDI         |
| LZ4            | General-purpose, extremely fast decompress | Real-time game data    |
| Zstandard      | Better ratio, configurable speed           | Asset loading, replays |

---

## Format Comparison

| Format           | Size (Player) | Parse Speed | Schema? | Human-Readable? |
| ---------------- | ------------- | ----------- | ------- | --------------- |
| JSON             | ~80 bytes     | Slow        | No      | Yes             |
| Protocol Buffers | ~20 bytes     | Fast        | Yes     | No              |
| FlatBuffers      | ~24 bytes     | Zero-copy   | Yes     | No              |
| Custom bitpack   | ~4 bytes      | Fastest     | Manual  | No              |

**Rule of thumb:** Use the simplest format that meets your bandwidth and latency requirements.

---

## Key Takeaways

1. **Never `memcpy` structs** to the network—padding and endianness break portability
2. **Boost.Endian** for byte order conversion—replaces legacy `htonl`/`ntohl`
3. **JSON** for human-readable interchange; **binary** for performance
4. **Protocol Buffers** when you need schema evolution and industry tooling
5. **Custom bitpacking** when every bit counts (real-time games)
6. **Always validate** deserialized data—never trust the wire

---

## Next Class

**Friday:** Hands-on bitpacking implementation, compression deep-dive, performance benchmarks
