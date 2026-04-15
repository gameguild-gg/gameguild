# Compression Techniques

Compression reduces the number of bytes sent over the network. For real-time applications, the techniques that matter most are **domain-specific** — they exploit knowledge about your data rather than treating it as an opaque byte stream.

## The Compression Pipeline

In game networking, these techniques are typically combined in a pipeline:

```mermaid
flowchart LR
    A[Full Game State] -->|Delta Encoding| B[Only Changes]
    B -->|Quantization| C[Reduced Precision]
    C -->|Bitpacking| D[Minimum Bits]
    D -->|Optional: LZ4| E[Wire Bytes]
```

Each stage reduces the data further:

| Stage          | Example: 100 players × 3 floats | Size   |
| -------------- | ------------------------------- | ------ |
| Raw            | 100 × 12 bytes                  | 1200 B |
| Delta encoding | ~30 changed × 12 bytes          | 360 B  |
| Quantization   | 30 × 6 bytes (16-bit per axis)  | 180 B  |
| Bitpacking     | 30 × 30 bits (10 bits/axis)     | 113 B  |
| LZ4            | ~90 B (repeated patterns)       | ~90 B  |

## Delta Encoding

Instead of sending the **absolute** value every frame, send the **difference** from the previous value:

```cpp
// Full state: position = {500, 300, 100}
// Previous:   position = {498, 300, 101}
// Delta:      position = {  2,   0,  -1}
```

Deltas are typically much smaller than absolute values, meaning:

- Varints use fewer bytes (small deltas → 1-2 bytes instead of 4)
- Bitpacking uses fewer bits (small range → fewer bits)
- Many deltas are zero (unchanged values can be skipped entirely)

```cpp
struct DeltaState {
    // Bitmask: which fields changed since last snapshot
    uint32_t changed_mask;
    // Only changed fields follow
};

void serialize_delta(BitWriter& writer,
                     const Player& current,
                     const Player& previous) {
    uint32_t mask = 0;
    if (current.x != previous.x)      mask |= (1 << 0);
    if (current.y != previous.y)      mask |= (1 << 1);
    if (current.z != previous.z)      mask |= (1 << 2);
    if (current.health != previous.health) mask |= (1 << 3);

    writer.write_bits(mask, 4);

    if (mask & (1 << 0)) writer.write_bits(current.x, 10);
    if (mask & (1 << 1)) writer.write_bits(current.y, 10);
    if (mask & (1 << 2)) writer.write_bits(current.z, 10);
    if (mask & (1 << 3)) writer.write_bits(current.health, 7);
}
```

::: tip "Overwatch uses delta compression extensively"

Blizzard's Overwatch sends full snapshots infrequently and delta updates at 63 Hz. Most frames, only a few players have moved, so most deltas are zero — the changed-bitmask alone is enough to skip 80%+ of the data.

:::

## Quantization

Reduce the precision of values to use fewer bits. This is a **lossy** technique — you sacrifice precision for bandwidth:

### Float to Fixed-Point

```cpp
// Quantize position: 0.0–100.0 meters with 1cm precision
// 10,000 steps → 14 bits (vs 32 bits for float)
uint16_t quantize_position(float pos) {
    return static_cast<uint16_t>(pos * 100.0f + 0.5f);
}

float dequantize_position(uint16_t quantized) {
    return static_cast<float>(quantized) / 100.0f;
}
```

### Common Quantization Strategies

| Value          | Range      | Precision | Bits | vs Float |
| -------------- | ---------- | --------- | ---- | -------- |
| Map position   | 0–4096m    | 1 cm      | 19   | 1.7×     |
| Map position   | 0–4096m    | 10 cm     | 16   | 2×       |
| Velocity       | -50–50 m/s | 0.1 m/s   | 10   | 3.2×     |
| Angle          | 0–360°     | ~0.35°    | 10   | 3.2×     |
| Health         | 0–100      | 1 HP      | 7    | 4.6×     |
| Normalized vec | -1.0–1.0   | ~0.001    | 11   | 2.9×     |

## Variable-Length Quantities (VLQ)

The same concept behind protobuf varints, generalized. Small values use fewer bytes:

```cpp
// Encode: small values use 1 byte, larger values use more
void write_vlq(std::vector<uint8_t>& buffer, uint32_t value) {
    do {
        uint8_t byte = value & 0x7F;
        value >>= 7;
        if (value > 0) byte |= 0x80;  // continuation bit
        buffer.push_back(byte);
    } while (value > 0);
}

// Decode
uint32_t read_vlq(const uint8_t*& data) {
    uint32_t result = 0;
    int shift = 0;
    uint8_t byte;
    do {
        byte = *data++;
        result |= (byte & 0x7F) << shift;
        shift += 7;
    } while (byte & 0x80);
    return result;
}
```

| Value     | VLQ Bytes | uint32_t Bytes | Savings |
| --------- | --------- | -------------- | ------- |
| 0–127     | 1         | 4              | 75%     |
| 128–16383 | 2         | 4              | 50%     |
| 16384+    | 3+        | 4              | varies  |

VLQ is ideal for values that are **usually small** but occasionally large (array lengths, entity counts, timestamps).

## General-Purpose Compressors

After domain-specific compression (delta + quantization + bitpacking), you can optionally apply a general-purpose compressor to catch remaining redundancy:

### LZ4: Speed-Optimized

```
Compression:   ~780 MB/s
Decompression: ~4,200 MB/s (!!!)
Ratio:         ~2.1× on typical data
```

LZ4 is designed for minimal decompression latency — critical for real-time data. Decompression is nearly free compared to the network transfer time.

```cpp
#include <lz4.h>

// Compress
std::vector<char> compressed(LZ4_compressBound(source_size));
int compressed_size = LZ4_compress_default(
    source_data, compressed.data(),
    source_size, compressed.size());
compressed.resize(compressed_size);

// Decompress
std::vector<char> decompressed(original_size);
LZ4_decompress_safe(
    compressed.data(), decompressed.data(),
    compressed_size, original_size);
```

### Zstandard (zstd): Ratio-Optimized

```
Compression:   ~500 MB/s (level 1), ~10 MB/s (level 19)
Decompression: ~1,700 MB/s
Ratio:         ~2.8× (level 1) to ~4.5× (level 19)
```

Zstandard offers a tunable speed/ratio tradeoff. Use lower levels for real-time data, higher levels for asset bundles or replays.

### When to Use General-Purpose Compression

| Use Case                     | Compressor    | Why                                         |
| ---------------------------- | ------------- | ------------------------------------------- |
| Real-time game packets       | LZ4 or none   | Decompression must be < 1ms                 |
| Replay files                 | Zstandard     | Ratio matters more than decompression speed |
| Asset bundles / downloads    | Zstandard L19 | Best ratio, decompression is one-time       |
| Already-bitpacked game state | Usually none  | Bitpacked data has little redundancy left   |

::: warning "Don't compress already-compressed data"

If you've done thorough bitpacking, LZ4 may actually **increase** the size (it adds framing overhead). Bitpacked data has high entropy — there are no patterns for LZ4 to exploit. Only apply general-purpose compression to data with remaining redundancy.

:::
