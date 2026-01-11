# Assignment 02: Network Utilities Library

**Estimated Time:** 1–2 hours
**Grading:** Automatic via doctest + manual code review

## Overview

Build a **NetworkUtils C++ class** implementing IP addressing, subnet calculations, CIDR operations, IPv6 validation, and FQDN parsing. This assignment directly reinforces **Week 02 lecture topics** and provides a reusable networking utility library for future assignments.

## Tasks & Functions to Implement

### Task 1: IPv4 Address Validation & Conversion (30 min)

**Functions:**

- `bool is_valid_ipv4(const std::string& ip_str)`
  - Validate dotted decimal format (4 octets, each 0-255)
  - Return `false` for "256.1.1.1", "192.168.1", etc.
  - Use regex pattern: `^(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.(\d{1,3})$`
- `std::optional<uint32_t> ipv4_to_uint32(const std::string& ip_str)`
  - Convert "192.168.1.100" to `0xC0A80164`
  - Shift octets: `(octet1 << 24) | (octet2 << 16) | (octet3 << 8) | octet4`
  - Return `std::nullopt` if invalid
- `std::string uint32_to_ipv4(uint32_t ip_int)`
  - Reverse operation: `0xC0A80164` becomes "192.168.1.100"
  - Extract octets using shifts and masks: `(ip >> 24) & 0xFF`
- `std::string octet_to_binary(uint8_t octet)`
  - Convert 192 to "11000000", 255 to "11111111"
  - Loop through bits and build string: `for (int i = 7; i >= 0; --i)`

### Task 2: CIDR & Subnet Mask Conversion (20 min)

**Functions:**

- `std::string cidr_to_subnet_mask(uint8_t prefix)`
  - Convert CIDR prefix to dotted decimal: `/24` becomes "255.255.255.0"
  - Build 32-bit mask: prefix ones, then zeros
  - Formula: `mask = (prefix == 0) ? 0 : (0xFFFFFFFFU << (32 - prefix))`
  - Convert mask to dotted decimal
- `std::optional<uint8_t> subnet_mask_to_cidr(const std::string& mask)`
  - Reverse: "255.255.255.192" becomes `/26`
  - Count leading ones, verify contiguous (no gaps)
  - Return `std::nullopt` if invalid mask pattern

### Task 3: Subnet Calculations (45 min)

**Functions:**

- `std::string get_network_address(const std::string& ip_str, uint8_t prefix)`
  - Apply subnet mask with bitwise AND: `ip & mask`
  - From lecture: "192.168.1.100"/24 gives "192.168.1.0"
- `std::string get_broadcast_address(const std::string& ip_str, uint8_t prefix)`
  - Set all host bits to 1
  - Formula: `broadcast = network | (~mask)`
  - Example: "192.168.1.0"/24 gives "192.168.1.255"
- `std::string get_first_host(const std::string& ip_str, uint8_t prefix)`
  - Network address + 1
  - Special case: `/31` (point-to-point) has 2 usable hosts
- `std::string get_last_host(const std::string& ip_str, uint8_t prefix)`
  - Broadcast address - 1
  - Special case: `/31` returns broadcast address
- `uint32_t count_usable_hosts(uint8_t prefix)`
  - Formula: `2^(32 - prefix) - 2` (exclude network & broadcast)
  - Special cases: `/32` returns 0, `/31` returns 2 (RFC 3021)
  - From lecture: `/24` returns 254, `/26` returns 62
- `bool is_in_subnet(const std::string& ip_str, const std::string& network_str, uint8_t prefix)`
  - Check if IP belongs to subnet: `(ip & mask) == (network & mask)`
- `SubnetInfo analyze_subnet(const std::string& ip_str, uint8_t prefix)`
  - Return struct with: network, first_host, last_host, broadcast
  - Lecture example: "192.168.100.50"/26 returns ("192.168.100.0", "192.168.100.1", "192.168.100.62", "192.168.100.63")

### Task 4: IPv6 Address Handling (25 min) **EXTRA CREDIT**

**Functions:**

- `bool is_valid_ipv6(const std::string& ip_str)`
  - Accept formats: "::1", "2001:db8:85a3::8a2e:370:7334", "::ffff:192.0.2.1"
  - Reject: "::1::2" (multiple ::), "gggg::1" (invalid hex)
  - Check for valid hex characters and :: rules
- `std::string expand_ipv6(const std::string& ip_str)`
  - Expand "::1" to "0000:0000:0000:0000:0000:0000:0000:0001"
  - Expand "2001:db8::1" to "2001:0db8:0000:0000:0000:0000:0000:0001"
  - Pad each group to 4 characters with leading zeros

### Task 5: FQDN Parsing & DNS (30 min)

**Functions:**

- `bool is_valid_hostname(const std::string& hostname)`
  - RFC 1123 rules:
    - Each label: alphanumeric + hyphens (not start/end)
    - Max 63 chars per label, 253 chars total
    - Not purely numeric (distinguish from IPs)
  - Invalid: "256.1.1.1" (looks like IP), "-game.com" (starts with hyphen), "a"\*64".com" (too long)
- `std::vector<std::string> parse_fqdn(const std::string& fqdn)`
  - Split by '.': "game.example.com" returns ["game", "example", "com"]
  - From lecture: "api.servers.example.org" returns ["api", "servers", "example", "org"]
  - Return empty vector if invalid
- `std::string get_tld(const std::string& fqdn)`
  - Return last label: "game.example.com" returns "com"
- `std::string get_domain(const std::string& fqdn)`
  - Return second-to-last label: "game.example.com" returns "example"

### Task 6: IP Classification (20 min)

**Functions:**

- `bool is_private_ip(const std::string& ip_str)`
  - RFC 1918 private ranges:
    - 10.0.0.0/8: `(ip & 0xFF000000) == 0x0A000000`
    - 172.16.0.0/12: `(ip & 0xFFF00000) == 0xAC100000`
    - 192.168.0.0/16: `(ip & 0xFFFF0000) == 0xC0A80000`
  - Also include:
    - 127.0.0.0/8 (loopback)
    - 169.254.0.0/16 (link-local)
- `bool is_reserved_ip(const std::string& ip_str)`
  - 0.0.0.0/8, 255.255.255.255, 240.0.0.0/4 (future use)

## Testing & Grading

### Automatic Tests

- **IPv4 Validation** (valid/invalid addresses)
- **Subnet Calculations** (lecture examples: 192.168.100.50/26, 10.0.0.130/25)
- **CIDR Operations** (all common prefixes: /8, /16, /24, /25, /26, /28, /30, /32)
- **FQDN Parsing** (lecture examples: game.example.com, api.servers.example.org)
- **IP Classification** (private, reserved, public ranges)
- **Quiz Question Scenarios** (Questions 7, 8, 9, 12, 25, 28)
- **Edge Cases** (boundary values, invalid inputs)

### Grading Rubric (10 points)

| Component                    | Points | Test Cases | Points per Test |
| ---------------------------- | ------ | ---------- | --------------- |
| IPv4 Validation & Conversion | 2.0    | 5          | 0.4             |
| CIDR & Subnet Masks          | 0.8    | 2          | 0.4             |
| Subnet Calculations          | 4.0    | 10         | 0.4             |
| FQDN & DNS                   | 1.6    | 4          | 0.4             |
| IP Classification            | 1.6    | 4          | 0.4             |
| **Total**                    | **10** | **25**     | **0.4**         |
| IPv6 Handling (Extra Credit) | +1.2   | 3          | +0.4            |

## Demo Executable

The `main.cpp` includes **9 networking scenarios**:

1. **Server Configuration** - Validate bind addresses (0.0.0.0, 127.0.0.1, 192.168.x.x)
2. **LAN Planning** - Design subnet for 50-computer studio (growth planning)
3. **Public Subnet Analysis** - Lecture example breakdown (192.168.100.50/26)
4. **CIDR Operations** - Common network subnets reference table
5. **IPv6 Support** - Check readiness for IPv6 deployment
6. **DNS Configuration** - Validate server hostnames
7. **Network Classification** - Decide which IPs suitable for public servers
8. **Player IP Validation** - Check if player IP in region-locked subnet
9. **Binary Representation** - Understand octet-to-binary for subnetting math

## Implementation Tips

1. **Start with IPv4 (Tasks 1-3)** - These are the most used and map directly to quiz questions
2. **Use bitwise operations efficiently:**
   ```cpp
   // Create mask with 'prefix' ones: (0xFFFFFFFFU << (32 - prefix))
   // Apply mask: (ip & mask)
   // Set all host bits: (network | ~mask)
   ```
3. **String manipulation with `std::istringstream` and `std::sstream`:**
   ```cpp
   std::istringstream iss("192.168.1.100");
   int octet1, octet2, octet3, octet4;
   char dot;
   iss >> octet1 >> dot >> octet2 >> dot >> octet3 >> dot >> octet4;
   ```
4. **Use `std::optional<T>`** to signal failure gracefully
5. **Validate edge cases:** `/0`, `/32`, `/31` (point-to-point), invalid CIDR values
6. **Test against lecture examples first** before running full test suite

## References

- **Lecture:** Week 02 - IP Addressing and DNS
- **Textbook:** Beej's Guide Ch. 6-8 (IPv4/IPv6), Ch. 17 (Subnets)
- **RFCs:**
  - RFC 791: IPv4 (32-bit address space)
  - RFC 8200: IPv6 (128-bit address space)
  - RFC 1918: Private address ranges (10.x, 172.16-31.x, 192.168.x)
  - RFC 1123: Hostname requirements
  - RFC 3021: /31 subnet (point-to-point)

```c++
#pragma once

#include <string>
#include <vector>
#include <cstdint>
#include <optional>

/**
 * @class NetworkUtils
 * @brief Utilities for IP addressing, subnetting, CIDR operations, and DNS hostname parsing.
 *
 * This class provides static methods for:
 * - IPv4 address validation and conversion
 * - Subnet calculations (network, broadcast, host range)
 * - CIDR notation operations
 * - IPv6 address validation and expansion
 * - FQDN parsing and validation
 *
 */
class NetworkUtils {
public:
    // ============ IPv4 Address Validation & Conversion ============

    /**
     * Validate IPv4 address format (dotted decimal notation).
     * Valid: "192.168.1.100", "127.0.0.1", "0.0.0.0"
     * Invalid: "256.1.1.1", "192.168.1", "abc.def.ghi.jkl"
     *
     * @param ip_str The IPv4 address as dotted decimal string
     * @return true if valid, false otherwise
     */
    static bool is_valid_ipv4(const std::string& ip_str);

    /**
     * Convert dotted decimal IPv4 string to 32-bit unsigned integer.
     * Example: "192.168.1.100" → 0xC0A80164 (3232235876)
     *
     * @param ip_str Dotted decimal IPv4 address
     * @return 32-bit integer representation, or std::nullopt if invalid
     */
    static std::optional<uint32_t> ipv4_to_uint32(const std::string& ip_str);

    /**
     * Convert 32-bit unsigned integer to dotted decimal IPv4 string.
     * Example: 3232235876 → "192.168.1.100"
     *
     * @param ip_int 32-bit integer representation
     * @return Dotted decimal string representation
     */
    static std::string uint32_to_ipv4(uint32_t ip_int);

    /**
     * Convert single octet (0-255) to 8-bit binary string.
     * Example: 192 → "11000000", 255 → "11111111"
     *
     * @param octet Value 0-255
     * @return 8-character binary string
     */
    static std::string octet_to_binary(uint8_t octet);

    // ============ Subnet Calculations ============

    /**
     * Convert CIDR prefix length to dotted decimal subnet mask.
     * Example: 24 → "255.255.255.0", 26 → "255.255.255.192"
     *
     * @param prefix CIDR prefix length (0-32)
     * @return Dotted decimal subnet mask string
     */
    static std::string cidr_to_subnet_mask(uint8_t prefix);

    /**
     * Convert dotted decimal subnet mask to CIDR prefix length.
     * Example: "255.255.255.0" → 24, "255.255.255.192" → 26
     *
     * @param mask Dotted decimal subnet mask
     * @return CIDR prefix (0-32), or std::nullopt if invalid mask
     */
    static std::optional<uint8_t> subnet_mask_to_cidr(const std::string& mask);

    /**
     * Calculate network address given IP and CIDR prefix.
     * Example: ("192.168.1.100", 24) → "192.168.1.0"
     *
     * @param ip_str IPv4 address string
     * @param prefix CIDR prefix length
     * @return Network address string, or empty string if invalid
     */
    static std::string get_network_address(const std::string& ip_str, uint8_t prefix);

    /**
     * Calculate broadcast address given IP and CIDR prefix.
     * Example: ("192.168.1.0", 24) → "192.168.1.255"
     *
     * @param ip_str IPv4 address string
     * @param prefix CIDR prefix length
     * @return Broadcast address string, or empty string if invalid
     */
    static std::string get_broadcast_address(const std::string& ip_str, uint8_t prefix);

    /**
     * Calculate first usable host address in a subnet.
     * Example: ("192.168.1.0", 24) → "192.168.1.1"
     * (Network address + 1)
     *
     * @param ip_str IPv4 address in subnet
     * @param prefix CIDR prefix length
     * @return First host address, or empty string if invalid
     */
    static std::string get_first_host(const std::string& ip_str, uint8_t prefix);

    /**
     * Calculate last usable host address in a subnet.
     * Example: ("192.168.1.0", 24) → "192.168.1.254"
     * (Broadcast address - 1)
     *
     * @param ip_str IPv4 address in subnet
     * @param prefix CIDR prefix length
     * @return Last host address, or empty string if invalid
     */
    static std::string get_last_host(const std::string& ip_str, uint8_t prefix);

    /**
     * Calculate number of usable host addresses in a subnet.
     * Formula: 2^(32 - prefix) - 2
     * Examples: /24 → 254 hosts, /25 → 126 hosts, /30 → 2 hosts
     *
     * @param prefix CIDR prefix length
     * @return Number of usable hosts, or 0 for invalid input
     */
    static uint32_t count_usable_hosts(uint8_t prefix);

    /**
     * Check if an IP address belongs to a given subnet.
     * Example: is_in_subnet("192.168.1.50", "192.168.1.0", 24) → true
     *
     * @param ip_str The IP address to check
     * @param network_str The network address
     * @param prefix CIDR prefix length
     * @return true if IP is in subnet (including network and broadcast), false otherwise
     */
    static bool is_in_subnet(const std::string& ip_str, const std::string& network_str, uint8_t prefix);

    /**
     * Complete subnet analysis: return network, first host, last host, broadcast.
     * Example: analyze_subnet("192.168.100.50", 26) →
     *   ("192.168.100.0", "192.168.100.1", "192.168.100.62", "192.168.100.63")
     *
     * @param ip_str IPv4 address in subnet
     * @param prefix CIDR prefix length
     * @return Tuple of (network, first_host, last_host, broadcast), or empty strings if invalid
     */
    struct SubnetInfo {
        std::string network;
        std::string first_host;
        std::string last_host;
        std::string broadcast;
    };

    static SubnetInfo analyze_subnet(const std::string& ip_str, uint8_t prefix);

    // ============ IPv6 Address Handling ============

    /**
     * Validate IPv6 address format.
     * Valid: "2001:db8:85a3::8a2e:370:7334", "::1", "::ffff:192.0.2.1" (IPv4-mapped)
     * Invalid: "gggg::1", "::1::2" (multiple ::)
     *
     * @param ip_str The IPv6 address string
     * @return true if valid, false otherwise
     */
    static bool is_valid_ipv6(const std::string& ip_str);

    /**
     * Expand compressed IPv6 address (remove ::, add leading zeros).
     * Example: "::1" → "0000:0000:0000:0000:0000:0000:0000:0001"
     * Example: "2001:db8::1" → "2001:0db8:0000:0000:0000:0000:0000:0001"
     *
     * @param ip_str The IPv6 address (may be compressed)
     * @return Expanded IPv6 address, or empty string if invalid
     */
    static std::string expand_ipv6(const std::string& ip_str);

    // ============ DNS & FQDN Handling ============

    /**
     * Validate hostname according to RFC 1123.
     * Rules:
     * - Each label: alphanumeric + hyphen (not start/end)
     * - Max 63 chars per label
     * - Max 253 chars total
     * - Not purely numeric (to distinguish from IPs)
     *
     * Valid: "game.example.com", "api-v2.servers.example.org"
     * Invalid: "256.1.1.1" (IP), "-game.com" (starts with hyphen), "a"*64".com" (label too long)
     *
     * @param hostname The hostname string
     * @return true if valid, false otherwise
     */
    static bool is_valid_hostname(const std::string& hostname);

    /**
     * Parse FQDN into components (labels).
     * Example: "game.example.com" → ["game", "example", "com"]
     * Example: "api.v2.servers.example.org" → ["api", "v2", "servers", "example", "org"]
     *
     * @param fqdn Fully qualified domain name
     * @return Vector of domain labels, or empty vector if invalid
     */
    static std::vector<std::string> parse_fqdn(const std::string& fqdn);

    /**
     * Get TLD (top-level domain) from FQDN.
     * Example: "game.example.com" → "com"
     * Example: "api.servers.example.org" → "org"
     *
     * @param fqdn Fully qualified domain name
     * @return TLD string, or empty string if invalid
     */
    static std::string get_tld(const std::string& fqdn);

    /**
     * Get domain name (second-level domain).
     * Example: "game.example.com" → "example"
     * Example: "api.servers.example.org" → "example"
     *
     * @param fqdn Fully qualified domain name
     * @return Domain string, or empty string if invalid
     */
    static std::string get_domain(const std::string& fqdn);

    /**
     * Check if IP is in private ranges (RFC 1918 + link-local + loopback).
     * Private ranges: 10.0.0.0/8, 172.16.0.0/12, 192.168.0.0/16
     * Link-local: 169.254.0.0/16
     * Loopback: 127.0.0.0/8
     *
     * @param ip_str IPv4 address string
     * @return true if private, false otherwise
     */
    static bool is_private_ip(const std::string& ip_str);

    /**
     * Check if IP is in reserved ranges (0.0.0.0/8, 255.255.255.255, broadcast, etc).
     *
     * @param ip_str IPv4 address string
     * @return true if reserved, false otherwise
     */
    static bool is_reserved_ip(const std::string& ip_str);
};
```

```c++
#include "NetworkUtils.h"
#include <sstream>
#include <regex>
#include <algorithm>
#include <cmath>
#include <cctype>

// ============ IPv4 Address Validation & Conversion ============

bool NetworkUtils::is_valid_ipv4(const std::string& ip_str) {
    // TODO: Validate IPv4 address format (4 octets, 0-255 each, separated by dots)
    //
    // HINT 1 - Using Regex (Recommended):
    // Use std::regex to match pattern: "number.number.number.number"
    // Pattern example: R"(^(\d{1,3})\.(\d{1,3})$)" this captures 2 matches, continue that for 4 octets :)
    // - \d{1,3} matches 1-3 digits
    // - \. matches literal dot
    // - () creates capture groups for each octet
    // Learn regex at: https://regexr.com/ (paste pattern and test with examples!)
    // Use std::regex_match() to check if string matches pattern
    // Use std::smatch to capture groups, then validate each octet is 0-255
    // Use std::stoi to convert string to integer for range check
    //
    // HINT 2 - Without Regex:
    // 1. Count dots (should be exactly 3)
    // 2. Use std::istringstream to parse: "iss >> octet1 >> dot >> octet2 >> dot >> ..."
    // 3. Check each parsed number is in range [0, 255]
    // 4. Ensure no extra characters after last octet

    return false; // Replace with your implementation
}

std::optional<uint32_t> NetworkUtils::ipv4_to_uint32(const std::string& ip_str) {
    // TODO: Convert IPv4 string to 32-bit unsigned integer
    //
    // HINT - std::optional Usage:
    // std::optional is like a "box" that may or may not contain a value.
    // Use it when a function might fail (invalid input, not found, etc.)
    // - Return std::nullopt if validation fails (indicates "no value")
    // - Return the actual value if successful
    // Example: if (!is_valid_ipv4(ip_str)) return std::nullopt;
    //
    // HINT - Bit Shifting Refresher:
    // IPv4: 192.168.1.1 → 0xC0A80101 (32 bits = 4 bytes)
    // - Left shift (<<): moves bits left, fills with zeros
    //   Example: 0x0A << 8 = 0x0A00 (moved 8 bits left)
    // - OR (|): combines bits (0|0=0, 0|1=1, 1|0=1, 1|1=1)
    //   Example: 0xFF00 | 0x00AB = 0xFFAB
    // - AND (&): masks bits (0&0=0, 0&1=0, 1&0=0, 1&1=1)
    //   Example: 0x1234 & 0x00FF = 0x0034 (keeps only last byte)
    //
    // Algorithm:
    // 1. Parse octets from string (use std::istringstream)
    // 2. Build result: result = (result << 8) | octet
    //    - Shift existing bits left by 8 to make room
    //    - OR with new octet to add it
    // Example: 192.168.1.1
    //   - Start: result = 0
    //   - After 192: result = 192 (0x000000C0)
    //   - After 168: result = (192 << 8) | 168 = 49320 (0x0000C0A8)
    //   - After 1:   result = (49320 << 8) | 1 = 12625921 (0x00C0A801)
    //   - After 1:   result = (12625921 << 8) | 1 = 3232235777 (0xC0A80101)

    return std::nullopt; // Replace with your implementation
}

std::string NetworkUtils::uint32_to_ipv4(uint32_t ip_int) {
    // TODO: Convert 32-bit integer back to IPv4 string (inverse of ipv4_to_uint32)
    //
    // HINT - Extracting Bytes:
    // Given: 0xC0A80101 (3232235777) → need "192.168.1.1"
    // - Right shift (>>): moves bits right, discards rightmost bits
    // - Mask with 0xFF: extracts only the last byte (8 bits)
    //
    // Extract each octet:
    // - 1st octet: (ip_int >> 24) & 0xFF  // Shift right 24 bits, keep last 8
    // - 2nd octet: (ip_int >> 16) & 0xFF  // Shift right 16 bits, keep last 8
    // - 3rd octet: (ip_int >> 8) & 0xFF   // Shift right 8 bits, keep last 8
    // - 4th octet: ip_int & 0xFF          // Keep last 8 bits
    //
    // Use std::ostringstream to build string: "octet1.octet2.octet3.octet4"

    return ""; // Replace with your implementation
}

std::string NetworkUtils::octet_to_binary(uint8_t octet) {
    // TODO: Convert octet (0-255) to 8-bit binary string
    //
    // HINT - Octet to Binary:
    // An octet is just an 8-bit integer (uint8_t). Each bit represents a power of 2.
    // Example: 192 = 128 + 64 = 2^7 + 2^6 = 11000000 in binary
    //
    // Algorithm - Iterate Bit by Bit (from MSB to LSB):
    // 1. Loop from bit position 7 down to 0 (most significant to least)
    // 2. For each position i:
    //    - Shift octet right by i positions: (octet >> i)
    //    - AND with 1 to extract that bit: (octet >> i) & 1
    //    - Append '1' or '0' to result string
    //
    // Example: octet = 192 (decimal)
    //   - Bit 7: (192 >> 7) & 1 = 1 → '1'
    //   - Bit 6: (192 >> 6) & 1 = 1 → '1'
    //   - Bit 5: (192 >> 5) & 1 = 0 → '0'
    //   - ... → "11000000"

    return ""; // Replace with your implementation
}

// ============ Subnet Calculations ============

std::string NetworkUtils::cidr_to_subnet_mask(uint8_t prefix) {
    // TODO: Convert CIDR prefix length to subnet mask
    //
    // HINT - Subnet Mask Concept:
    // CIDR /24 means first 24 bits are 1s, remaining 8 bits are 0s
    // /24 → 11111111.11111111.11111111.00000000 → 255.255.255.0
    //
    // HINT - Creating the Mask:
    // 1. Start with all 1s: 0xFFFFFFFF (32 bits of 1s)
    // 2. Shift left by (32 - prefix) positions
    //    - This moves 1s to the left, fills right with 0s
    //    - Example: /24 → shift left by 8 → 0xFFFFFF00
    // 3. Convert result to IPv4 string using uint32_to_ipv4()
    //
    // Edge case: prefix=0 means all 0s (0.0.0.0)

    return ""; // Replace with your implementation
}

std::optional<uint8_t> NetworkUtils::subnet_mask_to_cidr(const std::string& mask) {
    // TODO: Convert subnet mask to CIDR prefix (inverse of cidr_to_subnet_mask)
    //
    // HINT:
    // 1. Convert mask string to uint32_t using ipv4_to_uint32()
    // 2. Count leading 1 bits (from MSB side)
    //    - Loop from bit 31 down to 0
    //    - Check if bit is set: (mask_val & (1U << i))
    //    - Stop counting when you hit first 0
    // 3. IMPORTANT: Verify mask is valid (all 1s must be contiguous)
    //    - Recreate expected mask from counted prefix
    //    - Compare with actual mask
    //    - Return std::nullopt if invalid (e.g., 255.255.0.255 is invalid)

    return std::nullopt; // Replace with your implementation
}

std::string NetworkUtils::get_network_address(const std::string& ip_str, uint8_t prefix) {
    // TODO: Calculate network address from IP and CIDR prefix
    //
    // HINT - What is Network Address?
    // The network address is the first address in a subnet (all host bits = 0).
    // Example: 192.168.1.50/24
    //   - Network portion: first 24 bits
    //   - Host portion: last 8 bits
    //   - Network address: 192.168.1.0 (set all host bits to 0)
    //
    // HINT - Calculation Using Bitwise AND:
    // 1. Convert IP to uint32_t
    // 2. Create subnet mask: 0xFFFFFFFF << (32 - prefix)
    // 3. Apply mask using AND: network = ip & mask
    //    - AND with 1 keeps bit, AND with 0 clears bit
    //    - This zeros out all host bits
    // 4. Convert back to string
    //
    // Example: 192.168.1.50/24
    //   - IP:   0xC0A80132 (11000000.10101000.00000001.00110010)
    //   - Mask: 0xFFFFFF00 (11111111.11111111.11111111.00000000)
    //   - AND:  0xC0A80100 (11000000.10101000.00000001.00000000) = 192.168.1.0

    return ""; // Replace with your implementation
}

std::string NetworkUtils::get_broadcast_address(const std::string& ip_str, uint8_t prefix) {
    // TODO: Calculate broadcast address from IP and CIDR prefix
    //
    // HINT - What is Broadcast Address?
    // The broadcast address is the last address in a subnet (all host bits = 1).
    // Used to send packets to all hosts in the subnet.
    // Example: 192.168.1.0/24
    //   - Network: 192.168.1.0
    //   - Broadcast: 192.168.1.255 (set all host bits to 1)
    //
    // HINT - Calculation Using Bitwise OR:
    // 1. Get network address first
    // 2. Calculate host bits mask: (1 << (32 - prefix)) - 1
    //    - This creates a mask with only host bits set to 1
    //    - Example /24: (1 << 8) - 1 = 255 = 0x000000FF
    // 3. Apply using OR: broadcast = network | host_bits
    //    - OR with 1 sets bit to 1, OR with 0 keeps bit unchanged
    //    - This sets all host bits to 1
    //
    // Example: 192.168.1.0/24
    //   - Network:    0xC0A80100 (192.168.1.0)
    //   - Host mask:  0x000000FF (255)
    //   - OR:         0xC0A801FF (192.168.1.255)

    return ""; // Replace with your implementation
}

std::string NetworkUtils::get_first_host(const std::string& ip_str, uint8_t prefix) {
    // TODO: Get first usable host IP in subnet
    //
    // HINT - Usable Host Range:
    // Hosts cannot use network or broadcast addresses.
    // First host = network address + 1
    // Last host = broadcast address - 1
    //
    // Example: 192.168.1.0/24
    //   - Network: 192.168.1.0 (not usable)
    //   - First host: 192.168.1.1
    //   - Last host: 192.168.1.254
    //   - Broadcast: 192.168.1.255 (not usable)
    //
    // SPECIAL CASE - /32 (Single Host):
    // Only one address, it IS the host (return empty or the IP itself)
    //
    // SPECIAL CASE - /31 (Point-to-Point Link):
    // RFC 3021: No network/broadcast in /31
    // Both addresses are usable (used for router-to-router links)
    // First = network address, Last = broadcast address

    return ""; // Replace with your implementation
}

std::string NetworkUtils::get_last_host(const std::string& ip_str, uint8_t prefix) {
    // TODO: Get last usable host IP in subnet
    //
    // HINT: See get_first_host() for explanation
    // Last host = broadcast - 1 (in normal subnets)
    // Exception: /31 point-to-point (RFC 3021)

    return ""; // Replace with your implementation
}

uint32_t NetworkUtils::count_usable_hosts(uint8_t prefix) {
    // TODO: Calculate number of usable host addresses in subnet
    //
    // HINT - Formula:
    // General case: 2^(host_bits) - 2
    //   - host_bits = 32 - prefix
    //   - Subtract 2 for network and broadcast addresses
    //   - Use bit shift: (1 << host_bits) - 2
    //
    // Example: /24 has 8 host bits
    //   - Total: 2^8 = 256 addresses
    //   - Usable: 256 - 2 = 254 hosts
    //
    // SPECIAL CASES:
    // - /32: 0 usable hosts (single address, the host itself)
    // - /31: 2 usable hosts (RFC 3021 point-to-point, no network/broadcast)
    //        Remember RFC 3021! /31 is special for router links.

    return 0; // Replace with your implementation
}

bool NetworkUtils::is_in_subnet(const std::string& ip_str, const std::string& network_str, uint8_t prefix) {
    // TODO: Check if an IP belongs to a given subnet
    //
    // HINT:
    // Apply the same subnet mask to both IP and network address.
    // If results match, they're in the same subnet.
    //
    // Algorithm:
    // 1. Convert both IP and network to uint32_t
    // 2. Create subnet mask
    // 3. Apply mask to both: ip & mask, network & mask
    // 4. Compare results

    return false; // Replace with your implementation
}

NetworkUtils::SubnetInfo NetworkUtils::analyze_subnet(const std::string& ip_str, uint8_t prefix) {
    // TODO: Analyze subnet and return all key addresses
    //
    // HINT:
    // This is a convenience function that combines previous functions:
    // 1. Calculate network address
    // 2. Calculate broadcast address
    // 3. Calculate first usable host
    // 4. Calculate last usable host
    //
    // Return SubnetInfo struct with all four values.
    // Handle special cases (/31, /32) appropriately.

    SubnetInfo info{"", "", "", ""}; // Replace with your implementation
    return info;
}

// ============ IPv6 Address Handling (EXTRA CREDIT) ============

bool NetworkUtils::is_valid_ipv6(const std::string& ip_str) {
    // TODO (EXTRA CREDIT): Validate IPv6 address format
    //
    // IPv6 is much more complex than IPv4:
    // - 8 groups of 4 hexadecimal digits separated by colons
    // - Example: 2001:0db8:85a3:0000:0000:8a2e:0370:7334
    // - Can compress zeros: 2001:db8:85a3::8a2e:370:7334
    // - Only ONE :: allowed (represents one or more groups of zeros)
    //
    // HINT: This is challenging! Consider:
    // - Count colons (should be 7, or less if :: is used)
    // - Verify only hex digits and colons
    // - Check :: appears at most once
    // - Validate each group is 1-4 hex digits

    return false; // Replace if attempting extra credit
}

std::string NetworkUtils::expand_ipv6(const std::string& ip_str) {
    // TODO (EXTRA CREDIT): Expand compressed IPv6 to full form
    //
    // Convert: 2001:db8::1 → 2001:0db8:0000:0000:0000:0000:0000:0001
    //
    // HINT:
    // 1. Find :: location
    // 2. Calculate how many zero groups it represents (8 - existing groups)
    // 3. Replace :: with appropriate number of :0000: groups
    // 4. Pad each group to 4 digits with leading zeros

    return ""; // Replace if attempting extra credit
}

// ============ DNS & FQDN Handling ============

bool NetworkUtils::is_valid_hostname(const std::string& hostname) {
    // TODO: Validate hostname/FQDN format
    //
    // Rules:
    // - Total length ≤ 253 characters
    // - Each label (part between dots) ≤ 63 characters
    // - Labels contain only alphanumeric and hyphen
    // - Labels cannot start or end with hyphen
    // - Cannot be purely numeric (to distinguish from IP addresses)
    //
    // HINT - Useful std::functions for Character Checking:
    // - std::isdigit(c): returns true if c is '0'-'9'
    // - std::isalpha(c): returns true if c is 'a'-'z' or 'A'-'Z'
    // - std::isalnum(c): returns true if c is alphanumeric (digit or letter)
    // - std::isxdigit(c): returns true if c is hexadecimal digit (0-9, a-f, A-F)
    //
    // Algorithm:
    // 1. Check total length
    // 2. Split by dots using std::getline(stream, str, '.')
    // 3. Validate each label
    // 4. Reject if looks like IP (all digits and dots)

    return false; // Replace with your implementation
}

std::vector<std::string> NetworkUtils::parse_fqdn(const std::string& fqdn) {
    // TODO: Split FQDN into labels (parts between dots)
    //
    // Example: "gameserver.example.com" → ["gameserver", "example", "com"]
    //
    // HINT:
    // 1. Validate hostname first
    // 2. Use std::istringstream and std::getline(stream, str, '.') to split
    // 3. Store each label in vector

    std::vector<std::string> labels; // Replace with your implementation
    return labels;
}

std::string NetworkUtils::get_tld(const std::string& fqdn) {
    // TODO: Extract Top-Level Domain (last label)
    //
    // Example: "gameserver.example.com" → "com"
    //
    // HINT: Parse FQDN and return last element of vector (.back())

    return ""; // Replace with your implementation
}

std::string NetworkUtils::get_domain(const std::string& fqdn) {
    // TODO: Extract domain name (second-to-last label)
    //
    // Example: "gameserver.example.com" → "example"
    //
    // HINT: Parse FQDN and return second-to-last element (labels[size-2])

    return ""; // Replace with your implementation
}

bool NetworkUtils::is_private_ip(const std::string& ip_str) {
    // TODO: Check if IP is in private/special-use ranges
    //
    // HINT - Private IP Ranges (RFC 1918):
    // These are NOT routable on public internet, used for local networks:
    // - 10.0.0.0/8        (10.0.0.0 - 10.255.255.255)
    // - 172.16.0.0/12     (172.16.0.0 - 172.31.255.255)
    // - 192.168.0.0/16    (192.168.0.0 - 192.168.255.255)
    //
    // HINT - Special Ranges:
    // - 127.0.0.0/8       (Loopback: localhost)
    // - 169.254.0.0/16    (Link-local: APIPA/auto-configuration)
    //
    // HINT - Detection Using Masking:
    // 1. Convert IP to uint32_t
    // 2. For each range, create appropriate mask and network
    // 3. Check: (ip & mask) == network
    //
    // Example: Check if IP is in 10.0.0.0/8
    //   - Mask: 0xFF000000 (first 8 bits)
    //   - Network: 0x0A000000 (10.0.0.0)
    //   - Test: (ip & 0xFF000000) == 0x0A000000
    //
    // Example: Check if IP is in 192.168.0.0/16
    //   - Mask: 0xFFFF0000 (first 16 bits)
    //   - Network: 0xC0A80000 (192.168.0.0)
    //   - Test: (ip & 0xFFFF0000) == 0xC0A80000

    return false; // Replace with your implementation
}

bool NetworkUtils::is_reserved_ip(const std::string& ip_str) {
    // TODO: Check if IP is in reserved/special ranges
    //
    // HINT - Reserved IP Ranges:
    // These addresses have special meanings and cannot be assigned to hosts:
    // - 0.0.0.0/8           (0.0.0.0 - 0.255.255.255) - "This network"
    // - 255.255.255.255     (Limited broadcast address)
    // - 240.0.0.0/4         (240.0.0.0 - 255.255.255.255) - Reserved for future use
    //
    // HINT - Detection:
    // Similar to is_private_ip(), use masking:
    // 1. Convert to uint32_t
    // 2. Check each range with appropriate mask
    //
    // Example: Check 0.0.0.0/8
    //   - Test: (ip & 0xFF000000) == 0x00000000
    //
    // Example: Check 240.0.0.0/4 (first 4 bits = 1111)
    //   - Mask: 0xF0000000 (first 4 bits)
    //   - Network: 0xF0000000
    //   - Test: (ip & 0xF0000000) == 0xF0000000
    //
    // Example: Check exact match for 255.255.255.255
    //   - Test: ip == 0xFFFFFFFF

    return false; // Replace with your implementation
}
```
