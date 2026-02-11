# Quiz 05: Message Framing, Buffering, and Concurrency

## Topic 1: The TCP Framing Problem

!!! quiz
{
"title": "TCP Byte Stream Semantics",
"question": "Why does TCP require an application-level framing protocol?",
"options": ["TCP is a byte stream that does not preserve message boundaries", "TCP packets are always split at 1024-byte intervals", "TCP automatically adds frame headers to each message", "TCP only delivers complete messages to the application"],
"answers": ["TCP is a byte stream that does not preserve message boundaries"]
}
!!!

!!! quiz
{
"title": "Partial Delivery",
"question": "A client sends a 5000-byte message with a single `boost::asio::write()` call. The receiver calls `socket.read_some()` once and gets only 1460 bytes. What is the most likely explanation?",
"options": ["TCP segments data based on MSS, so read_some() may return less than the full message", "boost::asio::write() only sends 1460 bytes maximum", "The message was corrupted during transmission", "The sender's Nagle algorithm truncated the message"],
"answers": ["TCP segments data based on MSS, so read_some() may return less than the full message"]
}
!!!

!!! quiz
{
"title": "Nagle's Algorithm",
"question": "What does Nagle's algorithm do that complicates message framing?",
"options": ["It combines small consecutive writes into fewer, larger TCP segments", "It splits large messages into fixed-size chunks", "It adds delimiter characters between messages automatically", "It reorders messages for optimal delivery"],
"answers": ["It combines small consecutive writes into fewer, larger TCP segments"]
}
!!!

## Topic 2: Framing Strategies

!!! quiz
{
"title": "Length-Prefix Complexity",
"question": "What is the time complexity of determining the message length with length-prefix framing?",
"options": ["O(1) — the length is in the fixed-size header", "O(N) — must scan the entire payload", "O(log N) — uses binary search on the buffer", "O(N²) — depends on message nesting depth"],
"answers": ["O(1) — the length is in the fixed-size header"]
}
!!!

!!! quiz
{
"title": "Binary Safety",
"question": "Which framing strategy is NOT binary-safe (cannot handle arbitrary byte values in payloads)?",
"options": ["Length-prefix", "Delimiter-based", "TLV (Type-Length-Value)", "Fixed-length"],
"answers": ["Delimiter-based"]
}
!!!

!!! quiz
{
"title": "Length-Prefix Protocol",
"question": "In a length-prefix framing scheme, what does the sender transmit first?",
"options": ["A fixed-size integer indicating the payload size in bytes", "The payload data followed by a length trailer", "A delimiter character marking the start of the message", "The message type identifier"],
"answers": ["A fixed-size integer indicating the payload size in bytes"]
}
!!!

!!! quiz
{
"title": "Choosing a Framing Strategy",
"question": "Which framing strategy is best suited for a binary game protocol that needs to support variable-length messages?",
"options": ["Delimiter-based with '\\n' separator", "Fixed-length messages with padding", "Length-prefix framing", "No framing — TCP handles message boundaries"],
"answers": ["Length-prefix framing"]
}
!!!

## Topic 3: Buffer Management

!!! quiz
{
"title": "Async Buffer Lifetime",
"question": "In Boost.Asio, what is the critical rule about buffer lifetime when using asynchronous operations like `async_read()`?",
"options": ["The buffer must remain valid until the completion handler is called", "Buffers can be freed immediately after calling async_read()", "Buffers are automatically managed by io_context", "Buffers should always be allocated on the stack for best performance"],
"answers": ["The buffer must remain valid until the completion handler is called"]
}
!!!

!!! quiz
{
"title": "Dynamic Buffer Type",
"question": "Which Boost.Asio buffer type dynamically grows as data is received and is commonly used with `read_until()`?",
"options": ["boost::asio::streambuf", "boost::asio::const_buffer", "boost::asio::mutable_buffer", "std::array<char, 1024>"],
"answers": ["boost::asio::streambuf"]
}
!!!

!!! quiz
{
"title": "Length Validation",
"question": "Why should a receiver validate the length field before allocating a buffer for an incoming message?",
"options": ["To prevent a malicious sender from causing excessive memory allocation", "To improve network throughput", "To ensure the message fits in a single TCP segment", "To comply with the TCP specification"],
"answers": ["To prevent a malicious sender from causing excessive memory allocation"]
}
!!!

## Topic 4: Partial Reads & Writes

!!! quiz
{
"title": "write_some vs boost::asio::write",
"question": "What is the difference between `socket.write_some()` and `boost::asio::write()`?",
"options": ["write_some() may send fewer bytes than requested; boost::asio::write() loops until all bytes are sent", "write_some() guarantees all bytes are sent; boost::asio::write() may send partial data", "They are identical in behavior", "write_some() is asynchronous; boost::asio::write() is synchronous"],
"answers": ["write_some() may send fewer bytes than requested; boost::asio::write() loops until all bytes are sent"]
}
!!!

!!! quiz
{
"title": "Composed Operations",
"question": "Which Boost.Asio composed operation reads data until a specific delimiter is found in the stream?",
"options": ["boost::asio::read_until()", "boost::asio::read()", "socket.read_some()", "boost::asio::write()"],
"answers": ["boost::asio::read_until()"]
}
!!!

!!! quiz
{
"title": "Receiving a Length-Prefixed Message",
"question": "When implementing length-prefix framing, what is the correct sequence of operations for receiving a complete message?",
"options": ["Read exactly 4 bytes for the length header, then read exactly that many bytes for the payload", "Read the entire buffer at once and parse the length afterward", "Read bytes one at a time until a delimiter is found", "Call read_some() once and assume the complete message arrived"],
"answers": ["Read exactly 4 bytes for the length header, then read exactly that many bytes for the payload"]
}
!!!

## Topic 5: Deadlock Prevention

!!! quiz
{
"title": "TCP Deadlock Cause",
"question": "What causes a TCP deadlock between two peers that are both sending large messages simultaneously?",
"options": ["Both peers' send buffers fill up and both block on write, with neither reading incoming data", "The TCP three-way handshake times out", "Nagle's algorithm prevents data from being sent", "The network switch drops all packets between the two peers"],
"answers": ["Both peers' send buffers fill up and both block on write, with neither reading incoming data"]
}
!!!

!!! quiz
{
"title": "Deadlock Solution",
"question": "Which of the following is a valid solution to prevent TCP write deadlock?",
"options": ["Use async I/O so reads and writes can progress independently without blocking each other", "Increase the TCP window size to infinity", "Disable Nagle's algorithm on both ends", "Send all messages in fixed 64-byte chunks"],
"answers": ["Use async I/O so reads and writes can progress independently without blocking each other"]
}
!!!

!!! quiz
{
"title": "Write Queue Pattern",
"question": "In the write queue pattern using Boost.Asio, what happens when a new message needs to be sent while a previous `async_write` is still in progress?",
"options": ["The new message is queued and sent after the current write completes", "The new message is dropped silently", "The current write is cancelled and replaced with the new message", "Both messages are sent simultaneously via separate async_write calls"],
"answers": ["The new message is queued and sent after the current write completes"]
}
!!!

## Topic 6: Concurrency Models

!!! quiz
{
"title": "Coroutine vs Thread Memory",
"question": "How much memory does a C++20 coroutine frame typically use compared to an OS thread stack?",
"options": ["Coroutines use ~100–1000 bytes; OS threads use 1–8 MB", "Both use approximately the same amount (1–8 MB)", "Coroutines use 4–64 KB; OS threads use ~100 bytes", "Both use approximately 4 KB"],
"answers": ["Coroutines use ~100–1000 bytes; OS threads use 1–8 MB"]
}
!!!

!!! quiz
{
"title": "std::jthread vs std::thread",
"question": "What is the primary advantage of `std::jthread` (C++20) over `std::thread`?",
"options": ["std::jthread automatically joins on destruction and supports cooperative cancellation via stop_token", "std::jthread runs faster than std::thread", "std::jthread can run on multiple cores while std::thread cannot", "std::jthread uses coroutines internally for better performance"],
"answers": ["std::jthread automatically joins on destruction and supports cooperative cancellation via stop_token"]
}
!!!

!!! quiz
{
"title": "shared_from_this Pattern",
"question": "In a Boost.Asio async server, what is the purpose of capturing `shared_from_this()` in async completion handlers?",
"options": ["To keep the Session object alive until all pending async handlers complete", "To share the socket between multiple threads", "To enable zero-copy I/O operations", "To prevent data races in the write queue"],
"answers": ["To keep the Session object alive until all pending async handlers complete"]
}
!!!

!!! quiz
{
"title": "Network Byte Order",
"question": "When sending a `uint32_t` length header over the network, which Boost function converts from native byte order to network byte order (big-endian)?",
"options": ["boost::endian::native_to_big()", "htonl() from <arpa/inet.h>", "static_cast<uint32_t>()", "boost::asio::buffer()"],
"answers": ["boost::endian::native_to_big()"]
}
!!!
