# Event Loops and Reactor-Style Architecture

The reactor pattern is the backbone of non-blocking networking.

## Reactor Loop Pattern

1. register interest in events (read/write/timer)
2. wait for readiness
3. dispatch associated handlers
4. return to wait

This keeps one orchestration loop responsive while handling many concurrent I/O operations.

## Boost.Asio Mapping

- `io_context` = event loop engine
- async socket operations = event registrations
- completion handlers = dispatch targets
- strands = serialized handler execution for shared state

## Handler Design Rules

- Keep handlers short
- Avoid blocking calls inside handlers
- Push expensive work to worker queues/pools
- Re-enter loop quickly to preserve responsiveness

## Backpressure and Cancellation

A mature reactor design also needs:

- bounded outgoing queues
- timeout handling
- cancellation/stop strategy

Without these, non-blocking systems can still fail under load (queue explosion, stale tasks, starvation).

## Pattern Benefit

Reactor architecture gives you high concurrency without requiring one thread per connection, while preserving clear control over ownership and scheduling.

## Code Example (C++): Minimal Reactor-Style Accept Loop

```cpp
#include <boost/asio.hpp>

using boost::asio::ip::tcp;

void do_accept(tcp::acceptor& acceptor) {
	acceptor.async_accept([&acceptor](boost::system::error_code ec, tcp::socket socket) {
		if (!ec) {
			// Register further async read/write handlers for this socket.
		}
		do_accept(acceptor); // Keep reactor loop alive.
	});
}
```

## Code Example (C#): Event Loop with `SocketAsyncEventArgs`

```csharp
using System.Net.Sockets;

static void StartReceive(Socket socket)
{
	var args = new SocketAsyncEventArgs();
	args.SetBuffer(new byte[2048], 0, 2048);
	args.Completed += (_, e) =>
	{
		if (e.BytesTransferred > 0)
		{
			// Handle data quickly, then re-arm receive.
			StartReceive((Socket)e.UserToken!);
		}
	};
	args.UserToken = socket;
	if (!socket.ReceiveAsync(args))
	{
		// Completed synchronously; handler pattern still applies.
	}
}
```
