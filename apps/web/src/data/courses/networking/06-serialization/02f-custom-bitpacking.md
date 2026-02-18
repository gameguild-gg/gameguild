# Custom Bitpacking

When every bit counts — sending game state at 60 Hz to dozens of players — general-purpose formats waste bandwidth. Custom bitpacking compresses data by using **exactly** the number of bits each value needs.

## The Core Insight: Range-Based Serialization

If you know a value's range, you can calculate the minimum bits required:

$$\text{bits\_required}(\min, \max) = \lceil \log_2(\max - \min + 1) \rceil$$

| Value               | Range       | Bits Required | vs `float` (32 bits) |
| ------------------- | ----------- | ------------- | -------------------- |
| Health (0–100)      | 101 values  | 7 bits        | 4.6× savings         |
| Position X (0–1023) | 1024 values | 10 bits       | 3.2× savings         |
| Heading (0–359°)    | 360 values  | 9 bits        | 3.6× savings         |
| Team (0–3)          | 4 values    | 2 bits        | 16× savings          |
| Alive (bool)        | 2 values    | 1 bit         | 32× savings          |

A player with position (10 bits × 3), health (7 bits), heading (9 bits), team (2 bits), and alive (1 bit) uses **40 bits (5 bytes)** instead of 20 bytes with raw types — **4× compression**.

## The Scratch Register Technique

Before looking at the code, let's understand the core algorithm. The challenge: we want to write arbitrary numbers of bits (10, 7, 9, 2, 1...) but we can only send **whole bytes**. A 10-bit value straddles two bytes. A 7-bit value leaves 1 bit of the current byte unused for the next field.

The solution is a **scratch register** — a 64-bit integer (`uint64_t`) used as a staging area for bits before they're committed to the output buffer.

### How It Works

The writer maintains two pieces of state:

- `scratch_` — a `uint64_t` holding pending bits, packed from the LSB (least significant bit) side
- `scratch_bits_` — an `int` counting how many bits in `scratch_` are live data

```
scratch_ (64 bits wide)
┌──────────────────────────────────────────────────────────────────┐
│     unused (zeros)            │    live pending bits            │
│  bits 63 ─── scratch_bits_    │    bits (scratch_bits_-1) ─── 0 │
└──────────────────────────────────────────────────────────────────┘
```

**Writing** `N` bits with value `V` follows three steps:

1. **Shift and merge:** `scratch_ |= uint64_t(V) << scratch_bits_` — the new value slots in above the existing pending bits
2. **Bump counter:** `scratch_bits_ += N`
3. **Drain full bytes:** while `scratch_bits_ >= 8`, extract the lowest byte, push it to the output buffer, then `scratch_ >>= 8` and `scratch_bits_ -= 8`

### Why 64 bits?

Between drains, we can have up to 7 leftover bits from a previous write. A single `write_bits` call can add up to 32 new bits. That's 7 + 32 = 39 bits maximum in flight — too many for a 32-bit register, but well within 64 bits.

### Step-by-Step Example

Write `player.health = 42` (7 bits) then `player.heading = 180` (9 bits):

| Step | Operation                                             | `scratch_` (binary, LSB→) | `scratch_bits_` | Buffer         |
| ---- | ----------------------------------------------------- | ------------------------- | --------------- | -------------- |
| 0    | Initial state                                         | `(empty)`                 | 0               | `[]`           |
| 1    | `write_bits(42, 7)` — OR `0b0101010` at position 0    | `...0_0101010`            | 7               | `[]`           |
| 1a   | 7 < 8 — no drain                                      | —                         | —               | —              |
| 2    | `write_bits(180, 9)` — OR `0b010110100` at position 7 | `...010110100_0101010`    | 16              | `[]`           |
| 2a   | 16 ≥ 8 — drain: emit `scratch_ & 0xFF` = `0x2A`       | `...0_10110100`           | 8               | `[0x2A]`       |
| 2b   | 8 ≥ 8 — drain: emit `scratch_ & 0xFF` = `0x5A`        | `(empty)`                 | 0               | `[0x2A, 0x5A]` |

Result: 16 bits packed into exactly 2 bytes with **zero** wasted bits between fields.

### Reading (BitReader) — The Mirror

The reader uses an identical scratch register, but in reverse:

1. **Fill:** while `scratch_bits_ < N`, load the next byte from the input buffer into `scratch_` at position `scratch_bits_`, then `scratch_bits_ += 8`
2. **Extract:** `value = scratch_ & ((1ULL << N) - 1)` — mask out the lowest N bits
3. **Consume:** `scratch_ >>= N` and `scratch_bits_ -= N`

The reader loads bytes on demand (not all at once), so it works identically to the writer but pulling bits out instead of pushing them in.

### The Flush Problem

After writing all fields, the scratch register may still hold 1–7 leftover bits that haven't been emitted as a complete byte. Calling `flush()` pushes that final partial byte (padded with zeros in the high bits). Forgetting to flush is the most common BitWriter bug — the last few bits silently vanish.

## BitWriter and BitReader Implementation

The fundamental abstractions for bitpacking are a `BitWriter` (serializer) and `BitReader` (deserializer) that work at the bit level:

```cpp
#include <cstdint>
#include <cassert>
#include <vector>

class BitWriter {
public:
    void write_bits(uint32_t value, int bits) {
        assert(bits > 0 && bits <= 32);
        assert(value < (1u << bits));  // value fits in requested bits

        // Pack bits into current word
        scratch_ |= static_cast<uint64_t>(value) << scratch_bits_;
        scratch_bits_ += bits;

        // Flush complete bytes
        while (scratch_bits_ >= 8) {
            buffer_.push_back(static_cast<uint8_t>(scratch_ & 0xFF));
            scratch_ >>= 8;
            scratch_bits_ -= 8;
        }
    }

    void write_bool(bool value) {
        write_bits(value ? 1 : 0, 1);
    }

    // Flush remaining bits (call when done writing)
    void flush() {
        if (scratch_bits_ > 0) {
            buffer_.push_back(static_cast<uint8_t>(scratch_ & 0xFF));
            scratch_ = 0;
            scratch_bits_ = 0;
        }
    }

    const std::vector<uint8_t>& data() const { return buffer_; }
    size_t bit_count() const { return buffer_.size() * 8 - (8 - scratch_bits_) % 8; }

private:
    std::vector<uint8_t> buffer_;
    uint64_t scratch_ = 0;
    int scratch_bits_ = 0;
};

class BitReader {
public:
    BitReader(const uint8_t* data, size_t size)
        : data_(data), size_(size) {}

    uint32_t read_bits(int bits) {
        assert(bits > 0 && bits <= 32);

        // Load more bytes into scratch
        while (scratch_bits_ < bits && byte_offset_ < size_) {
            scratch_ |= static_cast<uint64_t>(data_[byte_offset_++]) << scratch_bits_;
            scratch_bits_ += 8;
        }

        uint32_t value = scratch_ & ((1ull << bits) - 1);
        scratch_ >>= bits;
        scratch_bits_ -= bits;
        return value;
    }

    bool read_bool() {
        return read_bits(1) != 0;
    }

private:
    const uint8_t* data_;
    size_t size_;
    size_t byte_offset_ = 0;
    uint64_t scratch_ = 0;
    int scratch_bits_ = 0;
};
```

## Using the Bitpacker

### Basic Usage

```cpp
// Serialize
BitWriter writer;
writer.write_bits(player.x,      10);  // 0–1023
writer.write_bits(player.y,      10);  // 0–1023
writer.write_bits(player.z,      10);  // 0–1023
writer.write_bits(player.health,  7);  // 0–100
writer.write_bits(player.heading,  9);  // 0–359
writer.write_bits(player.team,    2);  // 0–3
writer.write_bool(player.alive);       // 1 bit
writer.flush();
// Total: 49 bits = 7 bytes

// Deserialize
BitReader reader(writer.data().data(), writer.data().size());
player.x       = reader.read_bits(10);
player.y       = reader.read_bits(10);
player.z       = reader.read_bits(10);
player.health  = reader.read_bits(7);
player.heading = reader.read_bits(9);
player.team    = reader.read_bits(2);
player.alive   = reader.read_bool();
```

### Glenn Fiedler's Unified Pattern

Use a template to write one function that works for both reading and writing:

```cpp
template <typename Stream>
bool serialize_player(Stream& stream, Player& player) {
    serialize_bits(stream, player.x,       10);
    serialize_bits(stream, player.y,       10);
    serialize_bits(stream, player.z,       10);
    serialize_bits(stream, player.health,   7);
    serialize_bits(stream, player.heading,  9);
    serialize_bits(stream, player.team,     2);
    serialize_bool(stream, player.alive);
    return true;
}

// One function, two uses:
WriteStream writer(buffer, size);
serialize_player(writer, player);

ReadStream reader(buffer, received);
serialize_player(reader, player);
```

This eliminates read/write mismatch bugs — the most common serialization error.

## Compressed Floats

Full IEEE-754 floats use 32 bits, but game values have known ranges. **Quantize** the float to an integer within a fixed range:

```cpp
// Compress a float in [min_val, max_val] to N bits
uint32_t compress_float(float value, float min_val, float max_val, int bits) {
    float normalized = (value - min_val) / (max_val - min_val);  // [0, 1]
    uint32_t max_int = (1u << bits) - 1;
    return static_cast<uint32_t>(normalized * max_int + 0.5f);   // round
}

// Decompress back to float
float decompress_float(uint32_t compressed, float min_val, float max_val, int bits) {
    uint32_t max_int = (1u << bits) - 1;
    float normalized = static_cast<float>(compressed) / max_int;
    return min_val + normalized * (max_val - min_val);
}
```

**Example: Position in a 100m × 100m map with 0.1m precision:**

```
Range: 0.0 to 100.0, precision 0.1m → 1000 steps → 10 bits
3 axes × 10 bits = 30 bits (3.75 bytes)
vs 3 × float = 96 bits (12 bytes)
Savings: 3.2×
```

## Compressed Quaternions (Smallest Three)

Quaternions represent rotations as `(w, x, y, z)` where `w² + x² + y² + z² = 1`. The **smallest-three** encoding exploits this constraint:

1. Find the component with the largest absolute value
2. Store which component it is (2 bits — one of `w`, `x`, `y`, `z`)
3. Store the other three components (they're in \[-0.707, 0.707\])
4. Reconstruct the largest component from `w² + x² + y² + z² = 1`

```cpp
// Quaternion smallest-three encoding
void serialize_quaternion(BitWriter& writer, const Quaternion& q, int bits_per_component) {
    // Find largest component
    float abs_vals[4] = {
        std::abs(q.w), std::abs(q.x), std::abs(q.y), std::abs(q.z)
    };
    int largest = 0;
    for (int i = 1; i < 4; i++) {
        if (abs_vals[i] > abs_vals[largest]) largest = i;
    }

    // Write which component is largest (2 bits)
    writer.write_bits(largest, 2);

    // Ensure the largest component is positive (negate quaternion if needed)
    float sign = ((&q.w)[largest] >= 0) ? 1.0f : -1.0f;

    // Write the three smallest components
    for (int i = 0; i < 4; i++) {
        if (i == largest) continue;
        float val = (&q.w)[i] * sign;
        // Range is [-1/sqrt(2), 1/sqrt(2)] ≈ [-0.707, 0.707]
        uint32_t compressed = compress_float(val, -0.707107f, 0.707107f, bits_per_component);
        writer.write_bits(compressed, bits_per_component);
    }
}
```

**Size: 2 + 3 × 9 = 29 bits** vs 4 × 32 = 128 bits. **4.4× compression.**

Overwatch uses 9 bits per component (29 bits total) for rotation. The precision error is ~0.06° — imperceptible in gameplay.

## CRC32 Checksums

Add integrity verification to your serialized data:

```cpp
#include <boost/crc.hpp>

// After serializing, append CRC32
void append_crc(std::vector<uint8_t>& buffer) {
    boost::crc_32_type crc;
    crc.process_bytes(buffer.data(), buffer.size());
    uint32_t checksum = crc.checksum();

    auto be = boost::endian::native_to_big(checksum);
    const auto* ptr = reinterpret_cast<const uint8_t*>(&be);
    buffer.insert(buffer.end(), ptr, ptr + 4);
}

// Before deserializing, verify CRC32
bool verify_crc(const uint8_t* data, size_t total_size) {
    if (total_size < 4) return false;

    size_t payload_size = total_size - 4;
    boost::crc_32_type crc;
    crc.process_bytes(data, payload_size);

    uint32_t expected;
    std::memcpy(&expected, data + payload_size, 4);
    expected = boost::endian::big_to_native(expected);

    return crc.checksum() == expected;
}
```

::: warning "CRC is not encryption"

CRC32 detects accidental corruption (bit flips, truncation) but does **not** protect against malicious tampering. An attacker can modify data and recompute the CRC. For security, use HMAC or digital signatures.

:::
