# Lecture 10: HTTP — The Application-Layer Protocol

## Overview

This lecture covers **HTTP (HyperText Transfer Protocol)**—the dominant application-layer protocol that powers the web and most API communication.

We'll explore how HTTP sits on top of TCP (and QUIC), the request/response lifecycle, message structure, methods and status codes, the evolution from HTTP/1.0 through HTTP/3, REST architectural constraints, caching mechanisms, and how to implement HTTP clients and servers in C++ using Boost.Beast.

---

## Lecture Sections

This lecture is divided into the following sections for easier navigation:

### [1. HTTP Fundamentals](./lecture/http-fundamentals)

HTTP is a stateless, text-based, client-server protocol at the application layer. Understand where HTTP fits in the network stack (on top of TCP/TLS), the request/response cycle, and why statelessness is both a constraint and a feature.

### [2. HTTP Messages: Requests and Responses](./lecture/http-messages)

Anatomy of an HTTP message:

- **Request line**: `GET /path HTTP/1.1` — method, target, version
- **Response status line**: `HTTP/1.1 200 OK` — version, status code, reason
- **Headers**: key-value metadata (`Host`, `Content-Type`, `Content-Length`, `Authorization`)
- **Body**: optional payload (JSON, HTML, binary data)

### [3. HTTP Methods and Status Codes](./lecture/methods-and-status-codes)

The standard methods and when to use each:

- **GET** (read), **POST** (create), **PUT** (replace), **PATCH** (partial update), **DELETE** (remove)
- **HEAD**, **OPTIONS**, **TRACE** — diagnostic methods
- Status code families: 1xx (informational), 2xx (success), 3xx (redirection), 4xx (client error), 5xx (server error)
- Key distinctions: 200 vs 201 vs 204, 301 vs 302, 400 vs 404 vs 422

### [4. URLs, Headers, and Content Negotiation](./lecture/urls-and-headers)

How URLs identify resources (scheme, authority, path, query, fragment). Key header categories:

- **Request headers**: `Host`, `Accept`, `Authorization`, `User-Agent`
- **Response headers**: `Content-Type`, `Content-Length`, `Set-Cookie`, `Location`
- **Content negotiation**: `Accept` / `Content-Type` for format selection (JSON, XML, HTML)

### [5. REST Architectural Constraints](./lecture/rest-constraints)

Fielding's six REST constraints and why they matter:

- **Client-server separation**: decouple UI from data storage
- **Statelessness**: every request contains all needed context
- **Cacheability**: responses declare whether they can be cached
- **Uniform interface**: resources identified by URIs, manipulated via representations
- **Layered system**: intermediaries (proxies, CDNs, load balancers) are transparent
- **Code-on-demand** (optional): servers can extend client functionality

Richardson Maturity Model: Level 0 (RPC) → Level 1 (Resources) → Level 2 (HTTP Verbs) → Level 3 (HATEOAS).

### [6. HTTP Caching](./lecture/http-caching)

How caching reduces latency and bandwidth:

- **Cache-Control** directives: `max-age`, `no-cache`, `no-store`, `public`, `private`
- **ETags** and conditional requests (`If-None-Match` → 304 Not Modified)
- **Last-Modified** / `If-Modified-Since` for time-based validation
- Freshness vs validation model — when to reuse vs when to revalidate

### [7. Evolution of HTTP: 1.0 → 1.1 → 2 → 3](./lecture/http-evolution)

What each version of HTTP solved:

- **HTTP/1.0**: one request per connection, no persistent connections
- **HTTP/1.1**: keep-alive, pipelining (broken in practice), chunked transfer encoding, `Host` header
- **HTTP/2**: binary framing, multiplexing (multiple streams over one connection), HPACK header compression, server push
- **HTTP/3**: QUIC transport (UDP-based), 0-RTT handshakes, no head-of-line blocking at the transport layer

### [8. HTTP in C++ with Boost.Beast](./lecture/http-cpp-boost-beast)

Implementing HTTP clients and servers using Boost.Beast:

- `beast::http::request<>` and `beast::http::response<>` message containers
- Synchronous client: connect → write request → read response
- `beast::http::field` for header manipulation
- Body types: `string_body`, `file_body`, `empty_body`
- URL parsing with Boost.URL (`scheme`, `host`, `path`, `query`)
- `cpp-httplib` as a simpler single-header alternative

---

## Quick Reference

| Topic        | Key Takeaway                                                                    |
| ------------ | ------------------------------------------------------------------------------- |
| HTTP Model   | Stateless request/response over TCP (HTTP/1.1, 2) or QUIC (HTTP/3)              |
| Request Line | `METHOD /path HTTP/version` — e.g. `GET /api/users HTTP/1.1`                    |
| Status Line  | `HTTP/version STATUS_CODE REASON` — e.g. `HTTP/1.1 200 OK`                      |
| Methods      | GET (read), POST (create), PUT (replace), PATCH (update), DELETE (remove)       |
| Status Codes | 2xx success, 3xx redirect, 4xx client error, 5xx server error                   |
| REST         | Resources + URIs + HTTP verbs + statelessness + cacheability                    |
| Caching      | `Cache-Control` + ETags + conditional requests = fewer round trips              |
| HTTP/2       | Binary framing, multiplexing, HPACK — one TCP connection, many streams          |
| HTTP/3       | QUIC (UDP), 0-RTT, no head-of-line blocking — faster than HTTP/2 on lossy links |
| Boost.Beast  | `http::request<string_body>`, `http::response<string_body>` for C++ HTTP I/O    |
