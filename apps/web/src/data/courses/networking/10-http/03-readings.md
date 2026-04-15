# Week 10 Readings: HTTP — The Application-Layer Protocol

::: tip "How to approach these readings"

Start with the MDN overview to see how HTTP requests and responses are structured, then read about status codes and messages. Move on to Fielding's REST dissertation chapter to understand the **design philosophy** behind HTTP APIs. Finish with the protocol evolution articles to see how HTTP/2 and HTTP/3 solve performance problems. Don't memorize headers—understand the request/response lifecycle.

:::

| #   | Reading                                                                                                                                        | Time   | Covers                                                                                      |
| --- | ---------------------------------------------------------------------------------------------------------------------------------------------- | ------ | ------------------------------------------------------------------------------------------- |
| 1   | MDN, ["An overview of HTTP"](https://developer.mozilla.org/en-US/docs/Web/HTTP/Overview)                                                       | 15 min | HTTP as a client-server protocol, statelessness, request/response cycle, headers and bodies |
| 2   | MDN, ["HTTP Messages"](https://developer.mozilla.org/en-US/docs/Web/HTTP/Messages)                                                             | 15 min | Request line, status line, headers, body structure for HTTP/1.1 and HTTP/2 framing          |
| 3   | MDN, ["HTTP response status codes"](https://developer.mozilla.org/en-US/docs/Web/HTTP/Status)                                                  | 15 min | 1xx–5xx categories, when to use 200 vs 201 vs 204, 301 vs 302, 400 vs 404 vs 422            |
| 4   | Fielding, [Dissertation Ch. 5 — "Representational State Transfer (REST)"](https://ics.uci.edu/~fielding/pubs/dissertation/rest_arch_style.htm) | 20 min | REST constraints: client-server, stateless, cacheable, uniform interface, layered system    |
| 5   | MDN, ["Evolution of HTTP"](https://developer.mozilla.org/en-US/docs/Web/HTTP/Evolution_of_HTTP)                                                | 15 min | HTTP/0.9 → 1.0 → 1.1 → 2 → 3 timeline, what each version fixed                              |
| 6   | Cloudflare, ["HTTP/2 vs HTTP/1.1"](https://www.cloudflare.com/learning/performance/http2-vs-http1.1/)                                          | 10 min | Binary framing, multiplexing, header compression (HPACK), server push                       |
| 7   | Cloudflare, ["What is HTTP/3?"](https://www.cloudflare.com/learning/performance/what-is-http3/)                                                | 10 min | QUIC transport, 0-RTT handshakes, no head-of-line blocking, UDP-based                       |
| 8   | MDN, ["HTTP caching"](https://developer.mozilla.org/en-US/docs/Web/HTTP/Caching)                                                               | 20 min | Cache-Control directives, ETags, conditional requests, freshness vs validation              |

**Total reading time: ~120 minutes (~2 hours)**

---

## Videos (Pick One or Two)

| Resource                                                                                               | Time   | What it covers                                                                   |
| ------------------------------------------------------------------------------------------------------ | ------ | -------------------------------------------------------------------------------- |
| Hussein Nasser, ["HTTP Crash Course"](https://www.youtube.com/watch?v=0OrmKCB0UrQ)                     | 30 min | Full request/response lifecycle, headers, methods, status codes, keep-alive      |
| Computerphile, ["Running a Web Server"](https://www.youtube.com/watch?v=JhpUch6lWMw)                   | 12 min | How a server listens, accepts, and responds to HTTP requests at the socket level |
| Hussein Nasser, ["HTTP/2 vs HTTP/3"](https://www.youtube.com/watch?v=GriONb4EfPY)                      | 25 min | Side-by-side comparison of multiplexing, head-of-line blocking, QUIC             |
| LiveOverflow, ["How a HTTP request becomes a TCP packet"](https://www.youtube.com/watch?v=PpsEaqJV_A0) | 10 min | Tracing an HTTP request through DNS, TCP, and the network stack                  |

---

## Interactive Practice

| Resource                                                                                                              | Time   | What it does                                                                          |
| --------------------------------------------------------------------------------------------------------------------- | ------ | ------------------------------------------------------------------------------------- |
| [httpbin.org](https://httpbin.org/) — try `curl -v httpbin.org/get` and `curl -v -X POST httpbin.org/post -d "hello"` | 20 min | See raw HTTP request/response headers, methods, and status codes in your terminal     |
| Wireshark: capture HTTP traffic with filter `http` while browsing a non-HTTPS site                                    | 20 min | Inspect HTTP/1.1 request/response structure, Content-Length, chunked encoding in wire |
| `curl -I https://www.google.com` then `curl -I -H "If-None-Match: <etag>" https://www.google.com`                     | 10 min | See caching headers (ETag, Cache-Control) and conditional 304 responses               |

---

## C++ / Boost Resources

| Resource                                                                                                                              | Time   | What it covers                                                              |
| ------------------------------------------------------------------------------------------------------------------------------------- | ------ | --------------------------------------------------------------------------- |
| Boost.Beast, [HTTP Examples](https://www.boost.org/doc/libs/latest/libs/beast/doc/html/beast/examples.html)                           | 20 min | Synchronous and async HTTP client/server examples using Boost.Beast         |
| Boost.Beast, [HTTP Message Model](https://www.boost.org/doc/libs/latest/libs/beast/doc/html/beast/using_http/message_containers.html) | 15 min | `request<>`, `response<>`, body types, field manipulation                   |
| [cpp-httplib](https://github.com/yhirose/cpp-httplib) (GitHub README)                                                                 | 10 min | Single-header C++ HTTP client/server library — simpler alternative to Beast |
| Boost.URL, [Overview](https://www.boost.org/doc/libs/latest/libs/url/doc/html/index.html)                                             | 10 min | Parsing and constructing URLs in C++ (scheme, host, path, query, fragment)  |

---

## Optional Deep Dive

### RFCs & Standards

- [RFC 9110 "HTTP Semantics"](https://datatracker.ietf.org/doc/html/rfc9110) — Definitive reference for methods, status codes, headers, content negotiation
- [RFC 9112 "HTTP/1.1"](https://datatracker.ietf.org/doc/html/rfc9112) — Wire format: request-line, header fields, message body, chunked transfer coding
- [RFC 9113 "HTTP/2"](https://datatracker.ietf.org/doc/html/rfc9113) — Binary framing layer, streams, flow control, HPACK header compression
- [RFC 9114 "HTTP/3"](https://datatracker.ietf.org/doc/html/rfc9114) — HTTP over QUIC, unidirectional streams, QPACK header compression
- [RFC 9111 "HTTP Caching"](https://datatracker.ietf.org/doc/html/rfc9111) — Cache-Control semantics, freshness calculation, validation model

### REST API Design (Both Tracks)

- Microsoft, ["RESTful web API design"](https://learn.microsoft.com/en-us/azure/architecture/best-practices/api-design) — Resource naming, HTTP methods mapping to CRUD, versioning strategies
- [Richardson Maturity Model](https://martinfowler.com/articles/richardsonMaturityModel.html) — Level 0 (RPC) → Level 3 (HATEOAS), understanding REST maturity

### Game Networking Context (GPR students)

- [Nakama Game Server](https://heroiclabs.com/docs/nakama/concepts/) — Example of a game backend exposing REST endpoints for matchmaking, leaderboards, storage
- [PlayFab REST API](https://learn.microsoft.com/en-us/rest/api/playfab/) — Microsoft's game backend-as-a-service HTTP API (auth, player data, economy)
- [Steam Web API](https://developer.valvesoftware.com/wiki/Steam_Web_API) — Valve's HTTP API for player stats, achievements, matchmaking queries
- Epic Online Services, ["Web API Overview"](https://dev.epicgames.com/docs/web-api-ref) — REST endpoints for multiplayer, social features, anti-cheat

### Distributed Systems Context (CSI students)

- [OpenAPI Specification 3.1](https://spec.openapis.org/oas/v3.1.0) — Industry standard for describing HTTP APIs, used by code generators
- [CNCF Cloud Native Glossary — "API Gateway"](https://glossary.cncf.io/api-gateway/) — How HTTP is used as the transport for microservices communication
- Kleppmann, [Designing Data-Intensive Applications (O'Reilly)](https://www.oreilly.com/library/view/designing-data-intensive-applications/9781491903063/) — Ch. 4 §"REST and RPC" covers HTTP-based service communication patterns
- [HTTP/2 in Infrastructure](https://www.nginx.com/blog/http2-module-nginx/) — How reverse proxies and load balancers use HTTP/2 multiplexing

### HTTP Debugging & Tools

- [Postman Learning Center](https://learning.postman.com/docs/getting-started/introduction/) — GUI tool for exploring and testing HTTP APIs
- [curl Documentation — "HTTP with curl"](https://everything.curl.dev/http) — Comprehensive guide to HTTP operations with curl
- [mitmproxy](https://mitmproxy.org/) — Interactive HTTPS proxy for inspecting encrypted HTTP traffic

### Boost.Beast Deep Dive

- [Boost.Beast Tutorial](https://www.boost.org/doc/libs/latest/libs/beast/doc/html/beast/quick_start.html) — Step-by-step HTTP client and server setup
- [Boost.Beast Advanced HTTP](https://www.boost.org/doc/libs/latest/libs/beast/doc/html/beast/using_http.html) — Custom body types, incremental reads, chunked encoding
- [Boost.Beast WebSocket](https://www.boost.org/doc/libs/latest/libs/beast/doc/html/beast/using_websocket.html) — Upgrading HTTP to WebSocket (preview of future weeks)

---

## Study Tips

::: warning "What to pay attention to"

1. **MDN HTTP Overview**: Understand the stateless request/response model—every HTTP interaction is independent
2. **HTTP Messages**: Know the difference between request lines (`GET /path HTTP/1.1`) and status lines (`HTTP/1.1 200 OK`)
3. **Status Codes**: Focus on the distinction between 2xx (success), 3xx (redirect), 4xx (client error), 5xx (server error)
4. **Fielding's REST**: Focus on the six constraints, not the academic formalism—understand WHY they exist

:::

**Recommended reading order:**

1. MDN "Overview of HTTP" → big picture of the protocol
2. MDN "HTTP Messages" → understand request/response structure
3. MDN "Status Codes" → learn the status code families
4. Fielding Ch. 5 → design philosophy behind REST
5. MDN "Evolution of HTTP" → how the protocol improved over time
6. Cloudflare HTTP/2 + HTTP/3 → modern performance improvements
7. MDN "HTTP Caching" → how caching reduces network load
8. httpbin + curl exercises → hands-on practice

**Common mistakes to avoid:**

- Confusing HTTP (application-layer protocol) with TCP (transport-layer protocol)
- Thinking HTTP is only for web browsers—it's the foundation of most API communication
- Ignoring caching headers—they are critical for performance in real systems
- Treating REST as "any API over HTTP"—REST has specific architectural constraints
