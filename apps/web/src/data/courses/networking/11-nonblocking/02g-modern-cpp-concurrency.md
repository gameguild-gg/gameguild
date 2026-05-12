# Modern C++ Concurrency: jthread, Stop Tokens, Coroutines

Modern C++ gives safer primitives for building custom thread managers.

## `std::jthread`

`std::jthread` improves `std::thread` ergonomics:

- joins automatically at scope end (RAII)
- integrates cooperative cancellation via stop tokens

This reduces shutdown bugs caused by detached or unjoined threads.

## Cooperative Cancellation

Use cancellation as a first-class design requirement:

- workers periodically check stop requests
- blocking waits should be interruptible or bounded by timeout
- shutdown path should be deterministic

## Coroutines (`co_await`)

Coroutines turn callback chains into linear async code:

- easier state-machine readability
- explicit suspension points
- compose naturally with async operations

They improve orchestration clarity but do not remove the need for thread safety, ownership, and backpressure.

## Combining with Boost.Asio

A common pattern:

- Asio event loop handles non-blocking I/O
- coroutine flows express protocol steps
- worker pool handles CPU-heavy transforms
- results return to owner context for final state mutation

This gives both scalability and maintainable control flow.

## Code Example (C++): `std::jthread` + Stop Token

```cpp
#include <thread>
#include <chrono>

std::jthread worker([](std::stop_token st) {
	while (!st.stop_requested()) {
		// periodic work
		std::this_thread::sleep_for(std::chrono::milliseconds(5));
	}
});

// Later on shutdown:
worker.request_stop();
```

## Code Example (C#): CancellationToken in Async Worker Loop

```csharp
using System.Threading;

var cts = new CancellationTokenSource();

var task = Task.Run(async () =>
{
	while (!cts.Token.IsCancellationRequested)
	{
		await Task.Delay(5, cts.Token);
		// periodic async work
	}
}, cts.Token);

// Shutdown:
cts.Cancel();
```
