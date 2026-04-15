# Thread Safety and Shared-State Ownership

Thread safety is easiest when ownership is explicit.

## Ownership-First Strategy

Prefer these patterns:

1. **Single owner**: one thread owns mutable state
2. **Message passing**: other threads send requests/results
3. **Immutable snapshots**: workers read copies, not live mutable state

This reduces lock complexity and race risk.

## Synchronization Options

- **Serialized execution** (e.g., strands): handlers execute one-at-a-time for a protected context
- **Mutexes/locks**: useful but easy to misuse (deadlocks, contention)
- **Lock-free/thread-safe queues**: great for producer/consumer handoff

## Common Failure Modes

- read/write races on shared containers
- check-then-act races (TOCTOU)
- shutdown races (threads reading freed resources)
- mixing callback and thread paths with unclear ownership

## Practical Checklist

- Define owner for each mutable structure
- Document which thread may read/write each resource
- Keep critical sections small
- Make shutdown order explicit (stop signal → drain queue → join workers)

## Code Example (C++): Serialized Access with `strand`

```cpp
#include <boost/asio.hpp>

boost::asio::io_context io;
boost::asio::strand<boost::asio::io_context::executor_type> strand(io.get_executor());
int shared_counter = 0;

void safe_increment() {
	boost::asio::post(strand, [] {
		// Runs serialized relative to other strand handlers.
		++shared_counter;
	});
}
```

## Code Example (C#): Single-Owner Mutation via Mailbox Queue

```csharp
using System.Collections.Concurrent;

var mailbox = new ConcurrentQueue<Action>();
int sharedState = 0;

// Worker thread enqueues mutation request:
mailbox.Enqueue(() => sharedState++);

// Owner thread (main loop) drains mailbox:
while (mailbox.TryDequeue(out var op))
{
	op(); // Only owner thread mutates sharedState
}
```
