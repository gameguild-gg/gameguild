# Week 11 Readings: Non-Blocking I/O and Concurrency

::: tip "How to approach these readings"

This week builds on prior socket fundamentals and framing work. Focus on **Boost.Asio design patterns** for non-blocking sockets (reactor-style waiting, handler execution, strands, and thread pools), then map those ideas to **Unity/.NET worker orchestration** and **modern C++ thread-manager patterns** (`std::jthread`, cancellation, and coroutines). Keep your attention on architecture and orchestration—not low-level socket basics already covered in earlier weeks.

:::

| #   | Reading / Watching                                                                                                                                                                                                                                                                                            | Time   | Covers                                                                                                                                                 |
| --- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 1   | Boost.Asio, ["Reactor-Style Operations"](https://www.boost.org/doc/libs/latest/doc/html/boost_asio/overview/core/reactor.html)                                                                                                                                                                                | 15 min | Readiness-based non-blocking I/O pattern (`wait_read`/`wait_write`) in a cross-platform abstraction                                                    |
| 2   | Boost.Asio Reference, [`basic_socket::non_blocking`](https://www.boost.org/doc/libs/latest/doc/html/boost_asio/reference/basic_socket/non_blocking.html) + [`async_wait`](https://www.boost.org/doc/libs/latest/doc/html/boost_asio/reference/basic_socket/async_wait.html)                                   | 15 min | Practical API surface for non-blocking sockets, immediate-return async waits, cancellation support                                                     |
| 3   | Boost.Asio Examples, [C++11 Nonblocking (`third_party_lib.cpp`)](https://www.boost.org/doc/libs/latest/doc/html/boost_asio/examples/cpp11_examples.html#boost_asio.examples.cpp11_examples.nonblocking)                                                                                                       | 15 min | Integrating non-blocking socket readiness into an existing loop; useful for both CSI event loops and GPR main/game loops                               |
| 4   | Boost.Asio Tutorial, [Timer.5 — Strands in Multithreaded Programs](https://www.boost.org/doc/libs/latest/doc/html/boost_asio/tutorial/tuttimer5.html)                                                                                                                                                         | 15 min | Handler serialization without explicit locking; safe multithreaded `io_context::run()` usage                                                           |
| 5   | Boost.Asio, [`thread_pool`](https://www.boost.org/doc/libs/latest/doc/html/boost_asio/reference/thread_pool.html) + [Strands Overview](https://www.boost.org/doc/libs/latest/doc/html/boost_asio/overview/core/strands.html)                                                                                  | 15 min | `post/dispatch/defer`, pool lifecycle, and serialized execution strategy for shared state                                                              |
| 6   | Unity Manual, [C# Job System Overview](https://docs.unity3d.com/Manual/JobSystemOverview.html) + Unity Scripting API, [`JobHandle`](https://docs.unity3d.com/ScriptReference/Unity.Jobs.JobHandle.html)                                                                                                       | 15 min | Unity-side worker execution model and completion polling patterns (`IsCompleted`/`Complete`) for moving data between gameplay flow and background work |
| 7   | .NET Docs, [`Thread`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.thread) + [Thread-safe collections overview](https://learn.microsoft.com/en-us/dotnet/standard/collections/thread-safe/) + [System.Threading.Channels](https://learn.microsoft.com/en-us/dotnet/core/extensions/channels) | 15 min | Raw worker-thread lifecycle + producer/consumer queues/channels for bidirectional polling between main loop and background workers                     |
| 8   | C++ Reference, [`std::jthread` and cooperative cancellation](https://en.cppreference.com/w/cpp/thread/jthread) + Boost.Asio, [C++20 Coroutines Support (`co_spawn`/`awaitable`)](https://www.boost.org/doc/libs/latest/doc/html/boost_asio/overview/composition/cpp20_coroutines.html)                        | 15 min | Modern C++ thread manager building blocks: RAII thread ownership, stop tokens, and coroutine-based async flows                                         |

**Total required reading/watching time: ~120 minutes (~2 hours)**

---

## Cross-Track Focus (CSI vs GPR)

- **CSI-275 focus:**
  - Compare event-driven reactor patterns vs worker-pool architectures
  - Understand how Boost abstracts readiness polling backends behind a consistent API
  - Design safe handler execution models (strand vs mutex-heavy code) and cancellation-friendly thread managers

- **GPR-430 focus:**
  - Integrate non-blocking socket readiness into a single-thread game loop
  - Offload expensive work to workers and poll/collect results without stalling frames
  - Model Unity worker communication with queue/channel handoff patterns between gameplay code and background processing
  - Use strands or single-owner data flow to avoid race conditions in gameplay/network state

---

## Optional Deep Dive

- [Boost.Asio Overview](https://www.boost.org/doc/libs/latest/doc/html/boost_asio/overview.html) — broader async model, execution contexts, composed operations
- [Boost.Asio C++11 Examples Index](https://www.boost.org/doc/libs/latest/doc/html/boost_asio/examples/cpp11_examples.html) — reference implementations for nonblocking, chat, server threading patterns
- Boost.Asio Examples: [HTTP Server 3 (thread-pool + single `io_context`)](https://www.boost.org/doc/libs/latest/doc/html/boost_asio/examples/cpp11_examples.html#boost_asio.examples.cpp11_examples.http_server_3)
- [Unity Learn: Job system, Burst, and ECS for performance](https://learn.unity.com/) — practical context for worker-style workloads in Unity pipelines
- [.NET docs: Task Parallel Library (TPL)](https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/task-parallel-library-tpl) — task schedulers, continuations, composition

---

## Study Tips

::: warning "What to pay attention to"

1. **Boost socket pattern first:** model your code around `io_context`, async ops, and completion handlers—not platform-specific syscalls
2. **Multiplexing conceptually:** understand readiness-based loops (`select`/`poll`/`epoll`) as backend strategies, not separate app architectures
3. **Unity worker handoff:** define a strict boundary between gameplay/main-thread responsibilities and worker-owned data processing
4. **Modern C++ manager style:** prefer `std::jthread` + cooperative stop over manual detached-thread lifecycles
5. **Coroutines are orchestration, not magic:** use coroutine flows to simplify async composition, but keep ownership/cancellation explicit

:::

**Recommended reading order:**

1. Boost Reactor-Style Operations
2. `non_blocking` + `async_wait` reference
3. Nonblocking example (`third_party_lib.cpp`)
4. Strand tutorial (Timer.5)
5. Boost `thread_pool`
6. Unity Job System + `JobHandle` polling
7. .NET raw thread + queue/channel handoff
8. `std::jthread` + Boost.Asio C++20 coroutines

**Common mistakes to avoid:**

- Mixing raw thread APIs and async callbacks without a clear ownership model
- Assuming "non-blocking" means "no coordination needed" (you still need readiness, backpressure, and cancellation rules)
- Running multithreaded handlers on shared objects without strand/mutex protection
- Accessing Unity-engine-owned state from background workers instead of passing immutable snapshots / queued commands
- Blocking the game/main thread while waiting for worker results instead of polling or callback-based completion
