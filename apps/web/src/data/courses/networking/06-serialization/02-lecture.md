# Lecture 06: Serialization

## Overview

This lecture covers how to convert C++ objects into bytes for network transmission and reconstruct them on the receiving end—**serialization** and **deserialization**.

We'll explore why naive approaches like `memcpy` fail, how endianness and struct padding create portability bugs, the spectrum from human-readable text formats (JSON) to schema-based binary formats (Protocol Buffers) to hand-tuned bitpacking for real-time games.

---

## Lecture Sections

This lecture is divided into the following sections for easier navigation:

### [1. Why Serialization Matters](./lecture/why-serialization)

Serialization converts structured data to a flat byte sequence for the wire. Learn why `memcpy` of structs is broken and the three problems it causes: endianness, padding, and versioning.

### [2. Endianness and Byte Order](./lecture/endianness)

Different CPUs store multi-byte integers in different orders. Understand big-endian vs little-endian, network byte order, and how to use Boost.Endian for portable conversion.

### [3. Struct Packing and Alignment](./lecture/struct-packing)

Compilers insert invisible padding bytes to satisfy alignment requirements. Learn why `sizeof(struct)` isn't what you expect and why raw struct I/O is non-portable.

### [4. Text Formats: JSON and Beyond](./lecture/text-formats)

JSON (RFC 8259) is the universal text interchange format. Understand its grammar, trade-offs against binary formats, and when text serialization is appropriate.

### [5. Binary Serialization Formats](./lecture/binary-formats)

Compare schema-based formats (Protocol Buffers, FlatBuffers) with self-describing formats (MessagePack, CBOR). Understand varints, TLV encoding, and zero-copy deserialization.

### [6. Custom Bitpacking](./lecture/custom-bitpacking)

When every bit counts—game state at 60 Hz—hand-rolled bitpackers compress positions, rotations, and game state far beyond any general-purpose format. Learn the `BitWriter`/`BitReader` pattern.

### [7. Compression Techniques](./lecture/compression)

Delta encoding, quantization, variable-length quantities, and general-purpose compressors (LZ4, Zstandard). Techniques that pair with serialization to minimize bandwidth.

### [8. Performance Comparison and Summary](./lecture/performance-comparison)

Benchmark the formats head-to-head: JSON vs Protobuf vs FlatBuffers vs custom bitpacking. Guidelines for choosing the right serialization strategy for your application.

---

## Quick Reference

| Topic            | Key Takeaway                                                                |
| ---------------- | --------------------------------------------------------------------------- |
| Serialization    | Convert objects to bytes; never `memcpy` raw structs                        |
| Endianness       | Use `boost::endian::native_to_big()` / `big_to_native()` for the wire       |
| Struct padding   | Compiler adds hidden bytes; `sizeof` varies across platforms                |
| JSON             | Human-readable, self-describing, but 2-10× larger than binary               |
| Protocol Buffers | Schema-based, compact varints, forward/backward compatible                  |
| FlatBuffers      | Zero-copy reads via vtables; no deserialization step                        |
| Bitpacking       | Range-based: `bits_required(min, max)` = ⌈log₂(max − min + 1)⌉ bits         |
| Compression      | Delta + quantization + bitpacking = typical game state compression pipeline |
