# Quiz 06: Serialization

## Topic 1: Serialization Fundamentals

!!! quiz
{
"title": "What is Serialization?",
"question": "What is serialization?",
"options": ["Compiling C++ code into machine instructions", "Converting in-memory data structures into a flat byte sequence", "Encrypting data before sending it over a network", "Sorting data fields alphabetically for storage"],
"answers": ["Converting in-memory data structures into a flat byte sequence"]
}
!!!

---

!!! quiz
{
"title": "Raw Struct Serialization",
"question": "Why is \u0060memcpy\u0060 of a struct directly onto the network socket a bad idea?",
"options": ["It only works with strings, not integers", "It requires the cstring header which is deprecated", "Endianness, struct padding, and lack of versioning make it non-portable", "It is too slow compared to JSON serialization"],
"answers": ["Endianness, struct padding, and lack of versioning make it non-portable"]
}
!!!

---

!!! quiz
{
"title": "Endianness Mismatch",
"question": "What happens when a little-endian machine sends uint32_t id = 42 as raw bytes to a big-endian machine?",
"options": ["The receiver reads 42 correctly", "The receiver reads 704,643,072 instead of 42", "The receiver gets a connection error", "The value is truncated to 0"],
"answers": ["The receiver reads 704,643,072 instead of 42"]
}
!!!

---

!!! quiz
{
"title": "Unified Serialize Pattern",
"question": "What is the main advantage of Glenn Fiedler's unified serialize pattern?",
"options": ["It makes code run twice as fast by using SIMD", "It compresses data automatically using LZ4", "One function handles both reading and writing, preventing read/write mismatch bugs", "It eliminates the need for byte-order conversion"],
"answers": ["One function handles both reading and writing, preventing read/write mismatch bugs"]
}
!!!

---

!!! quiz
{
"title": "Unified Pattern Implementation",
"question": "In the unified serialize pattern, how does the same function produce both read and write behavior?",
"options": ["It uses runtime \u0060if\u0060 checks on a direction flag", "It calls different virtual methods at runtime", "\u0060WriteStream\u0060 and \u0060ReadStream\u0060 implement the same interface but do opposite things; the template instantiates the correct version", "The compiler automatically detects whether to read or write"],
"answers": ["\u0060WriteStream\u0060 and \u0060ReadStream\u0060 implement the same interface but do opposite things; the template instantiates the correct version"]
}
!!!

---

!!! quiz
{
"title": "Most Common Serialization Bug",
"question": "What is the most common serialization bug that the unified pattern prevents?",
"options": ["Forgetting to include header files", "Reading fields in a different order than they were written", "Using the wrong integer type for the buffer", "Allocating too much memory for the output buffer"],
"answers": ["Reading fields in a different order than they were written"]
}
!!!

---

## Topic 2: Endianness and Byte Order

!!! quiz
{
"title": "Three Problems with Raw Structs",
"question": "Which of the following is NOT one of the three problems with sending raw structs over the network?",
"options": ["Endianness differs between CPUs", "Padding differs between compilers", "Raw structs cannot represent floating-point numbers", "There is no versioning — adding fields breaks old clients"],
"answers": ["Raw structs cannot represent floating-point numbers"]
}
!!!

---

!!! quiz
{
"title": "Wire Byte Order",
"question": "In the explicit serialization pattern, what byte order should be used on the wire?",
"options": ["Little-endian (x86 native order)", "Big-endian (network byte order)", "Mixed-endian (alternating per field)", "Platform-native order for maximum speed"],
"answers": ["Big-endian (network byte order)"]
}
!!!

---

!!! quiz
{
"title": "x86/ARM Byte Order",
"question": "What byte order do x86/x64 and modern ARM CPUs use?",
"options": ["Little-endian", "Big-endian", "Mixed-endian", "It depends on the operating system"],
"answers": ["Little-endian"]
}
!!!

---

!!! quiz
{
"title": "Boost.Endian Conversion",
"question": "Which Boost.Endian function converts a host-native integer to big-endian for sending on the wire?",
"options": ["\u0060boost::endian::big_to_native()\u0060", "\u0060boost::endian::native_to_big()\u0060", "\u0060boost::endian::little_to_big()\u0060", "\u0060boost::endian::swap_bytes()\u0060"],
"answers": ["\u0060boost::endian::native_to_big()\u0060"]
}
!!!

---

!!! quiz
{
"title": "Float Endianness",
"question": "How should you handle endianness when serializing a \u0060float\u0060 for network transmission?",
"options": ["Floats don't have endianness — send them raw", "Use \u0060htonl()\u0060 directly on the \u0060float\u0060 variable", "Reinterpret the float's bits as a \u0060uint32_t\u0060, endian-swap the integer, then transmit", "Convert the float to a string first"],
"answers": ["Reinterpret the float's bits as a \u0060uint32_t\u0060, endian-swap the integer, then transmit"]
}
!!!

---

!!! quiz
{
"title": "std::bit_cast",
"question": "What does \u0060std::bit_cast\u0060 do when converting float to uint32_t in C++20?",
"options": ["Rounds the float to the nearest integer", "Converts the float to its string representation", "Reinterprets the float's raw bit pattern as a \u0060uint32_t\u0060 without modifying any bits", "Truncates the float to an integer value"],
"answers": ["Reinterprets the float's raw bit pattern as a \u0060uint32_t\u0060 without modifying any bits"]
}
!!!

---

!!! quiz
{
"title": "Double Endian-Swap",
"question": "What happens if you accidentally endian-swap twice (once on send, once on receive)?",
"options": ["The data is corrupted and unrecoverable", "The value appears correct only by coincidence — it only works if both sides are the same endianness", "A compile-time error prevents this", "The value is always correct because the swaps cancel out"],
"answers": ["The value appears correct only by coincidence — it only works if both sides are the same endianness"]
}
!!!

---

!!! quiz
{
"title": "Console Generations",
"question": "Which older game consoles used big-endian byte order?",
"options": ["Nintendo Switch and PS5", "Game Boy and Sega Genesis", "PS3 (Cell/PowerPC) and Xbox 360 (PowerPC)", "All consoles before 2000 used big-endian"],
"answers": ["PS3 (Cell/PowerPC) and Xbox 360 (PowerPC)"]
}
!!!

---

## Topic 3: Struct Padding and Alignment

!!! quiz
{
"title": "Purpose of Padding",
"question": "Why do compilers insert padding bytes into structs?",
"options": ["To make structs take exactly a power-of-two number of bytes", "To protect against buffer overflows", "To align fields to their natural alignment boundaries for efficient CPU access", "To reserve space for future fields"],
"answers": ["To align fields to their natural alignment boundaries for efficient CPU access"]
}
!!!

---

!!! quiz
{
"title": "Struct Padding Calculation",
"question": "Given a struct S with char a, int32_t b, char c, what is the likely sizeof(S) on x86-64?",
"options": ["6 bytes (1 + 4 + 1)", "8 bytes", "12 bytes (1 + 3 pad + 4 + 1 + 3 pad)", "16 bytes"],
"answers": ["12 bytes (1 + 3 pad + 4 + 1 + 3 pad)"]
}
!!!

---

!!! quiz
{
"title": "Minimizing Struct Padding",
"question": "What is the rule of thumb for minimizing struct padding?",
"options": ["Use only \u0060char\u0060 fields", "Always use \u0060#pragma pack(1)\u0060", "Order fields from largest alignment to smallest (e.g., \u0060double\u0060 → \u0060int32_t\u0060 → \u0060char\u0060)", "Place all fields in alphabetical order"],
"answers": ["Order fields from largest alignment to smallest (e.g., \u0060double\u0060 → \u0060int32_t\u0060 → \u0060char\u0060)"]
}
!!!

---

!!! quiz
{
"title": "Disadvantages of pragma pack",
"question": "Why is \u0060#pragma pack(1)\u0060 a bad solution for networking?",
"options": ["It is not supported by any compiler", "It causes unaligned memory access (slower on x86, hardware fault on some ARM), and still does not fix endianness", "It makes the struct take more memory", "It prevents the struct from being copied"],
"answers": ["It causes unaligned memory access (slower on x86, hardware fault on some ARM), and still does not fix endianness"]
}
!!!

---

!!! quiz
{
"title": "Tail Padding",
"question": "What is 'tail padding' in a struct?",
"options": ["Extra bytes added before the first field", "Bytes inserted between every pair of fields", "Padding added after the last field so that arrays of the struct maintain alignment", "A debugging marker appended by the compiler"],
"answers": ["Padding added after the last field so that arrays of the struct maintain alignment"]
}
!!!

---

## Topic 4: JSON and Text Encoding

!!! quiz
{
"title": "JSON Value Types",
"question": "Which of the following is a valid JSON value type?",
"options": ["undefined", "Date", "null", "NaN"],
"answers": ["null"]
}
!!!

---

!!! quiz
{
"title": "JSON for High-Frequency Updates",
"question": "Why is JSON unsuitable for 60 Hz game state updates?",
"options": ["JSON does not support numbers", "JSON requires a schema file to parse", "It is 2-10× larger than binary and CPU-intensive to parse (string→number conversion, key lookups)", "JSON cannot represent boolean values"],
"answers": ["It is 2-10× larger than binary and CPU-intensive to parse (string→number conversion, key lookups)"]
}
!!!

---

!!! quiz
{
"title": "JSON vs Binary Size",
"question": "How many bytes does {\"x\":1.5,\"y\":2.0,\"z\":3.7} take as JSON text vs. 3 raw floats (binary)?",
"options": ["JSON: 12 bytes, binary: 27 bytes", "Both are 12 bytes", "JSON: 27 bytes, binary: 12 bytes", "JSON: 27 bytes, binary: 24 bytes"],
"answers": ["JSON: 27 bytes, binary: 12 bytes"]
}
!!!

---

!!! quiz
{
"title": "JSON RFC Encoding",
"question": "What encoding must JSON documents use according to RFC 8259?",
"options": ["ASCII", "UTF-16", "UTF-8", "Latin-1"],
"answers": ["UTF-8"]
}
!!!

---

## Topic 5: Protocol Buffers and Varints

!!! quiz
{
"title": "Protobuf Field Tags",
"question": "In a protobuf schema, what do the numbers = 1, = 2, = 3 after field declarations represent?",
"options": ["Default values for the fields", "The order fields appear in the struct's memory layout", "Field tags that identify each field on the wire (must never change once deployed)", "The maximum allowed value for the field"],
"answers": ["Field tags that identify each field on the wire (must never change once deployed)"]
}
!!!

---

!!! quiz
{
"title": "Varint Encoding Small Values",
"question": "How does a protobuf varint encode the integer value 1?",
"options": ["0x01 0x00 0x00 0x00 (4 bytes, little-endian)", "0x01 (1 byte — MSB=0 means last byte, lower 7 bits = 1)", "0x81 (1 byte with continuation bit set)", "0x00 0x01 (2 bytes, big-endian)"],
"answers": ["0x01 (1 byte — MSB=0 means last byte, lower 7 bits = 1)"]
}
!!!

---

!!! quiz
{
"title": "Varint Max Bytes",
"question": "What is the maximum number of bytes a \u0060uint32_t\u0060 needs when encoded as a varint?",
"options": ["4 bytes", "5 bytes (ceil(32/7) = 5)", "10 bytes", "8 bytes"],
"answers": ["5 bytes (ceil(32/7) = 5)"]
}
!!!

---

!!! quiz
{
"title": "Negative Integers as Varints",
"question": "Why does encoding a -1 value as a raw varint (without ZigZag) cost 5 bytes?",
"options": ["Negative numbers are always encoded as strings in protobuf", "The sign bit requires an extra byte", "In two's complement, -1 has all bits set, so there are no leading zeros for the varint to skip", "Varints cannot encode negative numbers at all"],
"answers": ["In two's complement, -1 has all bits set, so there are no leading zeros for the varint to skip"]
}
!!!

---

!!! quiz
{
"title": "ZigZag Encoding",
"question": "What does ZigZag encoding do?",
"options": ["Compresses strings by removing duplicate characters", "Reverses the byte order of an integer", "Maps signed integers so that small magnitudes (positive or negative) produce small unsigned values with many leading zeros", "Encrypts integers with a zigzag substitution cipher"],
"answers": ["Maps signed integers so that small magnitudes (positive or negative) produce small unsigned values with many leading zeros"]
}
!!!

---

!!! quiz
{
"title": "ZigZag Formula",
"question": "What is the formula for ZigZag encoding a 32-bit signed integer \u0060n\u0060?",
"options": ["\u0060abs(n) * 2\u0060", "\u0060n ^ 0xFFFFFFFF\u0060", "\u0060(n << 1) ^ (n >> 31)\u0060", "\u0060(n >> 1) ^ (n << 31)\u0060"],
"answers": ["\u0060(n << 1) ^ (n >> 31)\u0060"]
}
!!!

---

!!! quiz
{
"title": "Protobuf Wire Encoding Tag",
"question": "In protobuf wire encoding, what does the tag (field_number << 3) | wire_type encode?",
"options": ["The field's default value and maximum size", "The field's name as a hash code", "Both the field number and the wire type (VARINT, I32, I64, or LEN) in a single varint", "The byte offset where the field starts in the buffer"],
"answers": ["Both the field number and the wire type (VARINT, I32, I64, or LEN) in a single varint"]
}
!!!

---

!!! quiz
{
"title": "Protobuf Wire Type 2",
"question": "In the protobuf wire format, what does wire type 2 (LEN) encode?",
"options": ["A 2-byte fixed-width integer", "A variable-length integer (varint)", "A length-prefixed payload (string, bytes, or nested message) preceded by a varint byte count", "A 64-bit floating-point number"],
"answers": ["A length-prefixed payload (string, bytes, or nested message) preceded by a varint byte count"]
}
!!!

---

## Topic 6: Bitpacking, Quantization, and Safety

!!! quiz
{
"title": "Scratch Register",
"question": "What is a 'scratch register' in the context of a BitWriter?",
"options": ["A CPU register reserved for network operations", "A 64-bit integer used as a staging area to accumulate bits before emitting complete bytes", "A temporary file where serialized data is cached", "A debug register that tracks bit errors"],
"answers": ["A 64-bit integer used as a staging area to accumulate bits before emitting complete bytes"]
}
!!!

---

!!! quiz
{
"title": "Scratch Register Size",
"question": "Why does the BitWriter use a uint64_t scratch register instead of uint32_t?",
"options": ["For compatibility with 64-bit operating systems", "Because all game values require 64 bits", "Up to 7 leftover bits + 32 new bits = 39 bits maximum in flight, which exceeds 32 bits", "To support double-precision floating-point values"],
"answers": ["Up to 7 leftover bits + 32 new bits = 39 bits maximum in flight, which exceeds 32 bits"]
}
!!!

---

!!! quiz
{
"title": "Forgotten flush()",
"question": "What happens if you forget to call flush() on a BitWriter after writing all fields?",
"options": ["The buffer is automatically flushed when the object is destroyed", "A compile-time error is raised", "The last 1–7 bits remain in the scratch register and are silently lost — the data is truncated", "The entire buffer is discarded"],
"answers": ["The last 1–7 bits remain in the scratch register and are silently lost — the data is truncated"]
}
!!!

---

!!! quiz
{
"title": "bits_required Calculation",
"question": "Given bits_required(0, 100), what is the correct result?",
"options": ["6 bits", "7 bits (ceil(log2(101)) = 7)", "8 bits", "100 bits"],
"answers": ["7 bits (ceil(log2(101)) = 7)"]
}
!!!

---

!!! quiz
{
"title": "BitReader Mask",
"question": "In the BitReader, what is the purpose of the mask (1ULL << bits) - 1?",
"options": ["It sets all bits in the scratch register to 1", "It clears the scratch register after reading", "It extracts exactly the lowest N bits from the scratch register, discarding higher bits", "It inverts the bit order for endianness conversion"],
"answers": ["It extracts exactly the lowest N bits from the scratch register, discarding higher bits"]
}
!!!

---

!!! quiz
{
"title": "Delta Encoding",
"question": "What is delta encoding in the context of game networking?",
"options": ["Encoding the absolute position of every entity every frame", "Sending only the difference from the previous frame's values, so small changes use fewer bytes", "Compressing data using the LZ4 algorithm", "Encoding timestamps as offsets from Unix epoch"],
"answers": ["Sending only the difference from the previous frame's values, so small changes use fewer bytes"]
}
!!!

---

!!! quiz
{
"title": "Quantization",
"question": "What is quantization in serialization?",
"options": ["Counting the number of packets sent per second", "Splitting data into fixed-size quantum packets", "Reducing the precision of a value (e.g., float) to use fewer bits by mapping a known range to a fixed-point integer", "Measuring the quality of a compressed stream"],
"answers": ["Reducing the precision of a value (e.g., float) to use fewer bits by mapping a known range to a fixed-point integer"]
}
!!!

---

!!! quiz
{
"title": "Compression Pipeline Order",
"question": "In the game compression pipeline, what is the typical order of techniques applied?",
"options": ["LZ4 → Bitpacking → Delta → Quantization", "Quantization → LZ4 → Delta → Bitpacking", "Delta encoding → Quantization → Bitpacking → (optional) LZ4", "Bitpacking → Delta → LZ4 → Quantization"],
"answers": ["Delta encoding → Quantization → Bitpacking → (optional) LZ4"]
}
!!!

---

!!! quiz
{
"title": "Bandwidth Savings",
"question": "Approximately how much bandwidth does custom bitpacking save compared to JSON for 20 players at 64 Hz?",
"options": ["About 2× savings", "About 5× savings", "About 16× savings (JSON: ~2 MB/s vs bitpacking: ~125 KB/s)", "They use roughly the same bandwidth"],
"answers": ["About 16× savings (JSON: ~2 MB/s vs bitpacking: ~125 KB/s)"]
}
!!!

---

!!! quiz
{
"title": "Protobuf vs Bitpacking",
"question": "When is Protocol Buffers a better choice than custom bitpacking?",
"options": ["Always — protobuf is strictly superior in all cases", "When you need schema evolution, cross-language support, and the data rate is moderate (not 60 Hz competitive game state)", "When you need the absolute minimum bandwidth", "When you are writing a single-player offline game"],
"answers": ["When you need schema evolution, cross-language support, and the data rate is moderate (not 60 Hz competitive game state)"]
}
!!!

---

!!! quiz
{
"title": "Deserialization Safety",
"question": "Which of the following is the most critical rule for deserialization safety?",
"options": ["Always use JSON because it is safer than binary", "Assume the sender is trustworthy if they authenticated", "Validate all deserialized values against expected bounds before using them", "Use \u0060memcpy\u0060 to avoid parser bugs"],
"answers": ["Validate all deserialized values against expected bounds before using them"]
}
!!!
