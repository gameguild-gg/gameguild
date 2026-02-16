# Endianness and Byte Order

## What Is Endianness?

**Endianness** is the order in which bytes of a multi-byte value are stored in memory. The two conventions are:

- **Big-endian (BE):** Most significant byte at the lowest address
- **Little-endian (LE):** Least significant byte at the lowest address

Consider the 32-bit integer `0x12345678`:

```mermaid
packet-beta
title Big-Endian (Network Byte Order)
0-7: "0x12"
8-15: "0x34"
16-23: "0x56"
24-31: "0x78"
```

```mermaid
packet-beta
title Little-Endian (x86/ARM)
0-7: "0x78"
8-15: "0x56"
16-23: "0x34"
24-31: "0x12"
```

| Architecture       | Byte Order    | Examples                            |
| ------------------ | ------------- | ----------------------------------- |
| x86, x64           | Little-endian | Intel, AMD desktop/server CPUs      |
| ARM (default mode) | Little-endian | Mobile devices, Apple Silicon, RPi  |
| Network standard   | Big-endian    | TCP/IP headers (RFC 1700)           |
| PowerPC, SPARC     | Big-endian    | Older game consoles (PS3, Xbox 360) |

::: warning "Why this matters for networking"

If a little-endian machine sends `uint32_t value = 1` as raw bytes, a big-endian machine reads `16777216` (0x01000000). Even between two little-endian machines, using network byte order is best practice — your code must work if the receiver changes architecture.

:::

## Detecting Endianness at Compile Time (C++20)

C++20 introduced `std::endian` for compile-time endianness detection:

```cpp
#include <bit>

if constexpr (std::endian::native == std::endian::little) {
    // x86, ARM — most modern systems
} else if constexpr (std::endian::native == std::endian::big) {
    // PowerPC, SPARC, network devices
} else {
    // Mixed endianness (extremely rare, PDP-11)
}
```

## Byte Swapping with Boost.Endian

The modern C++ approach to endian conversion uses `boost::endian`:

### Conversion Functions

```cpp
#include <boost/endian/conversion.hpp>

uint32_t host_value = 0x12345678;

// Convert host → network (big-endian)
uint32_t network_value = boost::endian::native_to_big(host_value);

// Convert network → host
uint32_t back = boost::endian::big_to_native(network_value);

// Works with any integer type
uint16_t port = 8080;
uint16_t net_port = boost::endian::native_to_big(port);

int64_t timestamp = 1234567890;
int64_t net_ts = boost::endian::native_to_big(timestamp);
```

### Endian Buffer Types

For wire-format structs, use `boost::endian::big_uint32_buf_t` and friends — they store data in big-endian format and convert automatically on access:

```cpp
#include <boost/endian/buffers.hpp>

using namespace boost::endian;

// This struct has deterministic layout regardless of platform
struct WireHeader {
    big_uint16_buf_t message_type;
    big_uint32_buf_t payload_length;
    big_uint32_buf_t sequence_number;
};

// Write
WireHeader header;
header.message_type = 1;      // automatically stored as big-endian
header.payload_length = 1024;
header.sequence_number = 42;

// Send raw bytes — layout is guaranteed
send(socket, &header, sizeof(header));

// Read
WireHeader received;
recv(socket, &received, sizeof(received));
uint32_t len = received.payload_length.value(); // automatically converts
```

### Endian Arithmetic Types

For calculations in a specific endian format:

```cpp
#include <boost/endian/arithmetic.hpp>

using namespace boost::endian;

big_uint32_t counter = 0;
counter++;           // Increment works directly
counter += 10;       // Arithmetic works
uint32_t val = counter; // Implicit conversion to native
```

## Handling Floats

Floating-point numbers (IEEE 754) also have endianness. The standard approach is to reinterpret the float as a `uint32_t` (same bit pattern), endian-swap the integer, then transmit:

```cpp
#include <boost/endian/conversion.hpp>
#include <cstring>

// Serialize a float to network byte order
void write_float(uint8_t* dest, float value) {
    uint32_t bits;
    std::memcpy(&bits, &value, sizeof(bits));  // type-punning via memcpy
    bits = boost::endian::native_to_big(bits);
    std::memcpy(dest, &bits, sizeof(bits));
}

// Deserialize a float from network byte order
float read_float(const uint8_t* src) {
    uint32_t bits;
    std::memcpy(&bits, src, sizeof(bits));
    bits = boost::endian::big_to_native(bits);
    float value;
    std::memcpy(&value, &bits, sizeof(value));
    return value;
}
```

::: tip "C++20: Use std::bit_cast instead of memcpy for type-punning"

```cpp
#include <bit>

uint32_t bits = std::bit_cast<uint32_t>(my_float);
float value = std::bit_cast<float>(bits);
```

`std::bit_cast` is constexpr-capable and clearer in intent than `memcpy`. Both are well-defined — `reinterpret_cast<uint32_t*>(&my_float)` is **not** (it violates strict aliasing).

:::

## C++23: std::byteswap

C++23 adds `std::byteswap` for raw byte reversal:

```cpp
#include <bit>

uint32_t swapped = std::byteswap(0x12345678u);
// swapped == 0x78563412
```

This is a low-level primitive — prefer Boost.Endian's `native_to_big` / `big_to_native` which are clearer about intent and no-ops on big-endian platforms.

## Common Mistakes

| Mistake                                     | Consequence                            |
| ------------------------------------------- | -------------------------------------- |
| Forgetting to convert before sending        | Receiver reads garbage values          |
| Converting twice (send + receive both swap) | Double-swap = correct only by accident |
| Using `htonl` on a `float`                  | Wrong — `htonl` takes `uint32_t`       |
| Assuming all platforms are little-endian    | Breaks on PowerPC, big-endian ARM mode |
| Not converting length-prefix header bytes   | Framing logic reads wrong message size |
