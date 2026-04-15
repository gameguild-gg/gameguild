# Blocking vs Non-Blocking Sockets

A blocking socket call waits until it can complete. A non-blocking call returns immediately and tells you to try again later if the operation cannot proceed.

## Blocking Behavior

With blocking I/O, control flow is simple:

1. call `read()`
2. thread pauses until bytes arrive
3. continue

This is easy to reason about, but dangerous when one blocked call can stall an entire loop.

## Non-Blocking Behavior

With non-blocking I/O:

1. call `read()` / `write()`
2. if not ready, receive `would_block` (or equivalent)
3. wait for readiness notification, then retry

You gain responsiveness, but must handle state progression explicitly.

## Trade-Offs

| Approach     | Strengths                              | Risks                                               |
| ------------ | -------------------------------------- | --------------------------------------------------- |
| Blocking     | Simple control flow                    | Poor scalability if many connections/tasks wait     |
| Non-blocking | High responsiveness, scalable patterns | More state management, error handling, coordination |

## Practical Guidance

- Use non-blocking + event-loop style for networking core paths
- Keep callbacks/handlers short and non-blocking
- Use workers for CPU-heavy tasks; don’t turn I/O callbacks into mini batch jobs

## Code Example (C++): Non-Blocking Read with Retry Signal

```cpp
#include <boost/asio.hpp>

void try_read_once(boost::asio::ip::tcp::socket& socket) {
	socket.non_blocking(true);
	std::array<char, 1200> buf{};
	boost::system::error_code ec;

	std::size_t n = socket.read_some(boost::asio::buffer(buf), ec);
	if (ec == boost::asio::error::would_block || ec == boost::asio::error::try_again) {
		// Not ready yet: return to event loop.
		return;
	}
	if (ec) throw boost::system::system_error(ec);

	// Process n bytes...
}
```

## Code Example (C#): Blocking vs Non-Blocking Socket Mode

```csharp
using System;
using System.Net.Sockets;

static void ReadOnce(Socket socket)
{
	socket.Blocking = false; // non-blocking mode
	byte[] buffer = new byte[1200];

	try
	{
		int read = socket.Receive(buffer);
		// Process read bytes...
	}
	catch (SocketException ex) when (ex.SocketErrorCode == SocketError.WouldBlock)
	{
		// Not ready yet; poll/select loop will retry later.
	}
}
```

## Common Mistake

Switching sockets to non-blocking mode without redesigning the loop usually creates busy-waiting and CPU spikes. Non-blocking requires readiness-based orchestration, not tight retry loops.
