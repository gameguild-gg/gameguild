# Week 06 Readings: Serialization

::: tip "How to approach these readings"

Start with Glenn Fiedler's two articles, they perfectly frame **why** game programmers hand-roll binary formats and what bitpacking buys you. Then move to the RFC/spec material to understand the text and binary formats you'll encounter in the industry. Finally, study the endianness and struct-packing references so you understand the pitfalls of raw memory serialization. Don't memorize wire formats; understand the **trade-offs** between convenience, size, speed, and safety.

:::

| #   | Reading                                                                                                                                          | Time   | Covers                                                                                                   |
| --- | ------------------------------------------------------------------------------------------------------------------------------------------------ | ------ | -------------------------------------------------------------------------------------------------------- |
| 1   | Glenn Fiedler, ["Reading and Writing Packets"](https://gafferongames.com/post/reading_and_writing_packets/)                                      | 19 min | Why text formats fail for games, hand-coded binary formats, struct packing pitfalls, bitpacker design    |
| 2   | Glenn Fiedler, ["Serialization Strategies"](https://gafferongames.com/post/serialization_strategies/)                                            | 19 min | Unified read/write serialize functions, compressed floats, vectors, quaternions, arrays, CRC32 checksums |
| 3   | [RFC 8259 (JSON)](https://datatracker.ietf.org/doc/html/rfc8259)                                                                                 | 15 min | The JSON spec—understand the canonical text interchange format and its limitations                       |
| 4   | [Protocol Buffers Encoding Guide](https://protobuf.dev/programming-guides/encoding/)                                                             | 20 min | Varints, wire types, tag-length-value encoding, ZigZag signed integers                                   |
| 5   | [Boost.Endian Documentation](https://www.boost.org/doc/libs/latest/libs/endian/doc/html/endian.html)                                             | 10 min | `endian_buffer`, `endian_arithmetic`, `native_to_big`/`big_to_native` conversions in modern C++          |
| 6   | Beej's Guide, [Ch. 7.4 "Serialization—How to Pack Data"](https://beej.us/guide/bgnet/html/split/slightly-advanced-techniques.html#serialization) | 15 min | Packing integers/floats for the wire, `pack()`/`unpack()` patterns, IEEE-754 encoding                    |

**Total reading time: ~100 minutes (~1h 40m)**

---

## Videos (Pick One or Two)

| Resource                                                                                                              | Time   | What it covers                                                                                        |
| --------------------------------------------------------------------------------------------------------------------- | ------ | ----------------------------------------------------------------------------------------------------- |
| GDC 2017, ["Overwatch Gameplay Architecture and Netcode"](https://www.youtube.com/watch?v=W3aieHjyNvw) (first 25 min) | 25 min | Blizzard's ECS architecture, how they serialize game state with deterministic fixed-point, bitpacking |
| CppCon 2017, ["A Practical Guide to C++ Serialization"](https://www.youtube.com/watch?v=G7-GQhCw8eE)                  | 60 min | Survey of C++ serialization approaches: raw structs, Boost.Serialization, cereal, Protocol Buffers    |
| javidx9, ["Networking in C++"](https://youtu.be/hHowZ3bWsio?si=Pitg5K4B2aH2Te7K) (Part 3)                             | 30 min | Custom message headers, body packing, variable-length messages in C++                                 |
| Computerphile, ["Endianness Explained"](https://www.youtube.com/watch?v=NcaiHcBvDR4)                                  | 10 min | Visual explanation of big-endian vs little-endian byte ordering                                       |

---

## Interactive Practice

| Resource                                                                                                    | Time   | What it does                                                                                      |
| ----------------------------------------------------------------------------------------------------------- | ------ | ------------------------------------------------------------------------------------------------- |
| Hands-on: Use `xxd` to inspect binary representations of integers                                           | 15 min | Write a C++ program that outputs a struct to a file, then use `xxd` to see byte order and padding |
| [Protobuf Playground](https://protobuf-decoder.netlify.app/)                                                | 15 min | Paste raw protobuf bytes and see decoded fields—great for understanding varint/TLV encoding       |
| Hands-on: Write a bitpacker that serializes a position (x, y, z) with 10 bits each vs. 3× `float` (96 bits) | 20 min | Compare 30 bits (bitpacked, range 0–1023) vs 96 bits (full float)—measure the savings             |

---

## C++ / Boost Resources

| Resource                                                                                                   | Time   | What it covers                                                                         |
| ---------------------------------------------------------------------------------------------------------- | ------ | -------------------------------------------------------------------------------------- |
| [Boost.Endian](https://www.boost.org/doc/libs/latest/libs/endian/doc/html/endian.html)                     | 15 min | Type-safe endian conversions, endian-aware arithmetic types, buffer types              |
| [Boost.Serialization Tutorial](https://www.boost.org/doc/libs/latest/libs/serialization/doc/tutorial.html) | 20 min | Archive-based serialization, intrusive vs non-intrusive, versioning support            |
| [Boost.JSON](https://www.boost.org/doc/libs/latest/libs/json/doc/html/index.html) (overview + quick look)  | 10 min | Modern C++ JSON parsing/generation, DOM and SAX models, `value_from`/`value_to`        |
| [nlohmann/json](https://github.com/nlohmann/json) (README)                                                 | 10 min | The most popular C++ JSON library—intuitive API, SAX parser, BSON/CBOR/MessagePack I/O |
| [`std::bit_cast` (C++20)](https://en.cppreference.com/w/cpp/numeric/bit_cast)                              | 5 min  | Type-safe reinterpretation of bits—replaces `reinterpret_cast` and union tricks        |
| [`std::byteswap` (C++23)](https://en.cppreference.com/w/cpp/numeric/byteswap)                              | 5 min  | Standard library byte-swap for endian conversion                                       |
| [`std::endian` (C++20)](https://en.cppreference.com/w/cpp/types/endian)                                    | 5 min  | Compile-time endianness detection (`std::endian::native`)                              |

---

## Optional Deep Dive

### Binary Serialization Formats — Comparison

| Format                                                    | Schema?      | Zero-copy? | C++ support | Use case                                   |
| --------------------------------------------------------- | ------------ | ---------- | ----------- | ------------------------------------------ |
| [Protocol Buffers](https://protobuf.dev/)                 | Yes (.proto) | No         | Excellent   | RPC, distributed systems, config           |
| [FlatBuffers](https://flatbuffers.dev/)                   | Yes (.fbs)   | **Yes**    | Excellent   | Game engines, mobile, performance-critical |
| [Cap'n Proto](https://capnproto.org/)                     | Yes          | **Yes**    | Good        | High-perf IPC, zero-copy RPC               |
| [MessagePack](https://msgpack.org/)                       | No           | No         | Good        | JSON-like but binary, compact              |
| [BSON](https://bsonspec.org/)                             | No           | No         | Good        | MongoDB wire format, JSON superset         |
| [CBOR (RFC 8949)](https://www.rfc-editor.org/rfc/rfc8949) | No           | No         | Good        | IoT, constrained environments              |
| Custom bitpacking                                         | No           | **Yes**    | Manual      | AAA games, real-time state replication     |

### Text Formats

- [JSON.org](https://www.json.org/) — The official JSON homepage with interactive railroad diagrams of the grammar; great visual complement to the RFC
- [CSV (RFC 4180)](https://datatracker.ietf.org/doc/html/rfc4180) — The simplest structured text format; no types, no nesting, delimiter hell
- [XML (W3C Spec)](https://www.w3.org/TR/xml/) — Self-describing, verbose, supports schemas (XSD); mostly legacy in games
- [YAML Spec](https://yaml.org/spec/) — Human-friendly superset of JSON; used in configs (Unity, Unreal metadata)
- [TOML](https://toml.io/) — Minimal config format; gaining popularity as a simpler alternative to YAML

### Struct Packing & Alignment

- [The Lost Art of Structure Packing (Eric S. Raymond)](http://www.catb.org/esr/structure-packing/) — Why `sizeof(struct)` isn't what you think, alignment rules, `#pragma pack`
- [C++ Data Alignment (cppreference)](https://en.cppreference.com/w/cpp/language/object#Alignment) — `alignof`, `alignas`, natural alignment rules
- [`offsetof` macro](https://en.cppreference.com/w/cpp/types/offsetof) — Inspecting struct layout at compile time

### Game Networking Context (GPR students)

- Glenn Fiedler, ["Snapshot Compression"](https://gafferongames.com/post/snapshot_compression/) — Delta encoding + quantization + bitpacking for game state snapshots
- GDC 2018, ["8 Frames in 16ms: Rollback Networking in Mortal Kombat"](https://www.youtube.com/watch?v=7jb0FOcImdg) — NetherRealm's deterministic serialization for rollback (input serialization is key)
- GDC 2015, ["Networking for Physics Programmers"](https://www.gdcvault.com/play/1022195/Physics-for-Game-Programmers-Networking) (Glenn Fiedler) — Quantizing physics state: position, orientation (smallest-three quaternion encoding), velocity
- [FlatBuffers C++ Tutorial](https://flatbuffers.dev/tutorial/) — Step-by-step: define schema → generate C++ → serialize/deserialize with zero-copy access
- [FlatBuffers Internals / White Paper](https://flatbuffers.dev/white_paper/) — How FlatBuffers achieves zero-copy reads via vtables and offset-based access
- Unreal Engine, ["Network Serialization and RPCs"](https://dev.epicgames.com/documentation/en-us/unreal-engine/rpcs) — How Unreal bitpacks replicated properties with `NetSerialize()` and `NetQuantize()`

### Distributed Systems Context (CSI students)

- [Protocol Buffers C++ Tutorial](https://protobuf.dev/getting-started/cpptutorial/) — Define `.proto` → compile → use generated classes in C++
- [gRPC over HTTP/2 Wire Format](https://github.com/grpc/grpc/blob/master/doc/PROTOCOL-HTTP2.md) — 5-byte header (compressed-flag + 4-byte length), protobuf payload
- [CBOR (RFC 8949)](https://www.rfc-editor.org/rfc/rfc8949) — Concise Binary Object Representation: self-describing binary like JSON but compact
- [Apache Avro](https://avro.apache.org/docs/) — Schema-based serialization with schema evolution, used in Kafka and big-data pipelines
- Kleppmann, [Designing Data-Intensive Applications](https://dataintensive.net/) — Ch. 4 "Encoding and Evolution": JSON vs binary formats, schema evolution, forward/backward compatibility

### Compression Techniques

- [LZ4 (extremely fast compression)](https://github.com/lz4/lz4) — Often used for real-time game data; decompression at >4 GB/s
- [Zstandard (zstd)](https://facebook.github.io/zstd/) — Facebook's modern compressor; configurable speed/ratio tradeoff
- [Delta Encoding (Wikipedia)](https://en.wikipedia.org/wiki/Delta_encoding) — Send differences instead of absolutes; foundational for game state compression
- [Variable-Length Quantity (Wikipedia)](https://en.wikipedia.org/wiki/Variable-length_quantity) — The general concept behind protobuf varints, MIDI encoding, etc.

---

## Study Tips

::: warning "What to pay attention to"

1. **Glenn Fiedler's two articles**: Understand the progression from text → binary → bitpacking. Notice how each step trades readability/debugging for bandwidth efficiency
2. **Endianness**: Know the difference between big-endian (network byte order) and little-endian (most modern CPUs). Understand when you must convert and when you can skip it (same-platform optimizations)
3. **Struct packing**: Never `memcpy` a struct to the network. Padding, alignment, and endianness make this non-portable. Use explicit serialization instead
4. **Protobuf encoding**: Focus on varints and the tag-length-value structure. This is the most widely used binary format in industry
5. **Bitpacking**: Understand `bits_required(min, max)` and why range-based serialization saves bandwidth vs. fixed-width types

:::

**Recommended reading order:**

1. Glenn Fiedler, "Reading and Writing Packets" → understand text vs binary tradeoffs, struct packing pitfalls, bitpacker intro
2. Glenn Fiedler, "Serialization Strategies" → unified serialize functions, compressed floats, CRC32
3. RFC 8259 (JSON) → skim for structure; understand why it's inefficient for high-frequency data
4. Boost.Endian + Beej's serialization chapter → endianness and portable packing in modern C++
5. Protocol Buffers Encoding Guide → industry-standard binary format internals
6. (If time permits) FlatBuffers white paper → zero-copy design for performance-critical applications

**Common mistakes to avoid:**

- Assuming `sizeof(MyStruct)` is the same across compilers and platforms (it isn't, padding differs)
- Forgetting endianness conversion when serializing multi-byte integers
- Using `reinterpret_cast<float*>(&intValue)` instead of `std::bit_cast` (undefined behavior pre-C++20)
- Sending JSON or XML at 60Hz for game state (text overhead kills bandwidth)
- Not validating deserialized values are within expected ranges (security vulnerability)
- Confusing **schema-based** formats (protobuf, FlatBuffers) with **self-describing** formats (JSON, MessagePack, CBOR)
