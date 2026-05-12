# CSI vs GPR Architecture Patterns

Week 11 concepts are shared across both tracks, but optimization targets differ.

## CSI-275 Emphasis

- event-driven service architecture
- bounded worker pools for background tasks
- throughput, fairness, and predictable latency
- explicit cancellation and fault handling for long-running services

## GPR-430 Emphasis

- frame-time stability in the game loop
- non-blocking networking integrated into update ticks
- worker offload for expensive tasks without touching engine-owned state directly
- result polling/application at safe points in the frame lifecycle

## Same Primitives, Different Priorities

| Primitive             | CSI Priority                    | GPR Priority                            |
| --------------------- | ------------------------------- | --------------------------------------- |
| Event loop/reactor    | Scale to many clients           | Keep frame loop responsive              |
| Worker pool           | Throughput + service isolation  | Offload expensive work from main thread |
| Queue/channel handoff | Reliability and observability   | Deterministic integration per frame     |
| Cancellation          | Service shutdown and resilience | Scene/state transition safety           |

## Decision Heuristic

If a task can block or is CPU-heavy, it should not run on your orchestrator loop (server loop or game main loop). Move it to a managed worker path and reintegrate results through explicit handoff.

## Code Example (C++ / CSI): Event Loop + Worker Offload

```cpp
// Pseudocode shape for a service:
// 1) async read request
// 2) offload CPU-heavy transform
// 3) post result back to io_context thread for response write

boost::asio::post(worker_pool, [payload, &io]() {
	auto result = transform(payload);
	boost::asio::post(io, [result]() {
		// write response on owner/event-loop context
	});
});
```

## Code Example (C# / GPR): Frame-Safe Result Apply

```csharp
using System.Collections.Concurrent;

ConcurrentQueue<Action> mainThreadApply = new();

// Worker thread:
Task.Run(() =>
{
	var netDelta = ComputeDelta();
	mainThreadApply.Enqueue(() => ApplyDeltaToGameState(netDelta));
});

// Game Update() loop:
while (mainThreadApply.TryDequeue(out var apply))
{
	apply(); // only main thread mutates engine-owned state
}
```
