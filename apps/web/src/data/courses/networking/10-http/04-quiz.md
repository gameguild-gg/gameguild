# Quiz 10: HTTP — The Application-Layer Protocol

## Part 1: HTTP Fundamentals

!!! quiz
{
"title": "HTTP's Place in the Stack",
"question": "At which layer of the network stack does HTTP operate?",
"options": ["Network layer", "Transport layer", "Application layer", "Data link layer"],
"answers": ["Application layer"]
}
!!!

!!! quiz
{
"title": "HTTP's Transport Dependency",
"question": "HTTP relies on which underlying protocol to deliver a reliable byte stream (for HTTP/1.1 and HTTP/2)?",
"options": ["UDP", "TCP", "ICMP", "IP directly"],
"answers": ["TCP"]
}
!!!

!!! quiz
{
"title": "HTTP Statelessness",
"question": "What does it mean that HTTP is 'stateless'?",
"options": ["The server cannot send responses", "The client must reconnect for every request", "Cookies are not allowed", "The server does not remember anything about previous requests"],
"answers": ["The server does not remember anything about previous requests"]
}
!!!

!!! quiz
{
"title": "Statelessness Benefit",
"question": "Which of the following is a direct benefit of HTTP's statelessness?",
"options": ["Any server in a pool can handle any request without session affinity", "Lower latency for all requests", "Servers never need authentication tokens", "Responses are always smaller"],
"answers": ["Any server in a pool can handle any request without session affinity"]
}
!!!

!!! quiz
{
"title": "Carrying State in HTTP",
"question": "Since HTTP is stateless, how does an application typically carry authentication state across requests?",
"options": ["TCP keep-alive preserves the session automatically", "The server remembers the client's IP address", "The server stores the session in DNS", "The client sends an Authorization header with a token on each request"],
"answers": ["The client sends an Authorization header with a token on each request"]
}
!!!

## Part 2: HTTP Messages

!!! quiz
{
"title": "HTTP Message Structure",
"question": "What separates the headers from the body in an HTTP/1.1 message?",
"options": ["A Content-Length value of 0", "A null byte (\\0)", "An empty line (\\r\\n\\r\\n)", "A special END_HEADERS frame"],
"answers": ["An empty line (\\r\\n\\r\\n)"]
}
!!!

!!! quiz
{
"title": "Request Line Components",
"question": "An HTTP request line consists of which three parts?",
"options": ["Method, Request-Target (URI), HTTP Version", "Host, Port, Path", "Status Code, Reason Phrase, Body", "Scheme, Authority, Fragment"],
"answers": ["Method, Request-Target (URI), HTTP Version"]
}
!!!

!!! quiz
{
"title": "Response Status Line",
"question": "What is the correct order of components in an HTTP response status line?",
"options": ["Status Code, HTTP Version, Reason Phrase", "HTTP Version, Status Code, Reason Phrase", "Reason Phrase, Status Code, HTTP Version", "HTTP Version, Reason Phrase, Status Code"],
"answers": ["HTTP Version, Status Code, Reason Phrase"]
}
!!!

!!! quiz
{
"title": "Header Framing Strategy",
"question": "HTTP/1.1 headers use which framing strategy from Week 5?",
"options": ["Length-prefix framing", "Delimiter-based framing (CRLF)", "Fixed-size framing", "Sentinel value framing"],
"answers": ["Delimiter-based framing (CRLF)"]
}
!!!

!!! quiz
{
"title": "Body Length Detection",
"question": "When an HTTP/1.1 response has no Content-Length header and no chunked Transfer-Encoding, how does the receiver know when the body ends?",
"options": ["It reads exactly 1024 bytes", "It assumes there is no body", "It waits for a timeout", "It reads until the TCP connection closes"],
"answers": ["It reads until the TCP connection closes"]
}
!!!

## Part 3: HTTP Methods and Status Codes

!!! quiz
{
"title": "Chunked Transfer Encoding",
"question": "In HTTP/1.1 chunked transfer encoding, how does the receiver know that the body is complete?",
"options": ["The server closes the connection", "A special CRLF-only line appears", "A chunk with length 0 is sent", "The Content-Length header specifies the total"],
"answers": ["A chunk with length 0 is sent"]
}
!!!

!!! quiz
{
"title": "Safe HTTP Methods",
"question": "Which HTTP methods are considered 'safe' — meaning they do not change server state?",
"options": ["GET and HEAD", "POST and PATCH", "PUT and DELETE", "GET and POST"],
"answers": ["GET and HEAD"]
}
!!!

!!! quiz
{
"title": "Idempotent Methods",
"question": "Which method is idempotent, meaning calling it N times has the same effect as calling it once?",
"options": ["POST", "PUT", "PATCH", "None of the above"],
"answers": ["PUT"]
}
!!!

!!! quiz
{
"title": "POST Characteristics",
"question": "Why is POST considered neither safe nor idempotent?",
"options": ["It cannot send a body", "POST always returns 500", "Each POST can create a new resource, so repeating it creates duplicates", "POST is only used for deleting resources"],
"answers": ["Each POST can create a new resource, so repeating it creates duplicates"]
}
!!!

!!! quiz
{
"title": "201 Created",
"question": "When a server returns 201 Created after a POST, which header should it include to indicate where the new resource lives?",
"options": ["Location", "ETag", "Content-Type", "Host"],
"answers": ["Location"]
}
!!!

The difference between 401 and 403 is a common source of confusion. Despite its name, 401 means **unauthenticated** (identity unknown), while 403 means **unauthorized** (identity known, but access denied).

!!! quiz
{
"title": "401 vs 403",
"question": "A client sends a valid JWT token, but the user does not have permission to delete the resource. Which status code should the server return?",
"options": ["404 Not Found", "401 Unauthorized", "400 Bad Request", "403 Forbidden"],
"answers": ["403 Forbidden"]
}
!!!

!!! quiz
{
"title": "204 No Content",
"question": "After successfully deleting a resource, what is the most appropriate status code if the server has no body to return?",
"options": ["201 Created", "204 No Content", "200 OK", "304 Not Modified"],
"answers": ["204 No Content"]
}
!!!

!!! quiz
{
"title": "Status Code Families",
"question": "Which status code family indicates a client error?",
"options": ["3xx", "2xx", "4xx", "1xx"],
"answers": ["4xx"]
}
!!!

## Part 4: URLs, Headers, and Content Negotiation

!!! quiz
{
"title": "URL Fragment Behavior",
"question": "Which part of a URL is never sent to the server?",
"options": ["Query string", "Port", "Path", "Fragment"],
"answers": ["Fragment"]
}
!!!

!!! quiz
{
"title": "Content Negotiation",
"question": "Which HTTP header does the client use to tell the server what response format it prefers?",
"options": ["Accept", "Host", "Content-Type", "Authorization"],
"answers": ["Accept"]
}
!!!

!!! quiz
{
"title": "Percent Encoding",
"question": "In the URL `/search?q=C%2B%2B+games`, what does `%2B` decode to?",
"options": ["A space", "A plus sign (+)", "A forward slash", "An ampersand (&)"],
"answers": ["A plus sign (+)"]
}
!!!

!!! quiz
{
"title": "Host Header Requirement",
"question": "Why is the Host header required in HTTP/1.1?",
"options": ["It sets the TCP port", "It authenticates the client", "It specifies the response format", "It enables virtual hosting — multiple domains can share one IP address"],
"answers": ["It enables virtual hosting — multiple domains can share one IP address"]
}
!!!

## Part 5: REST Architectural Constraints

REST models **nouns** (resources), not **verbs** (actions). The HTTP method provides the verb. Putting actions in the URL path (`/api/deletePlayer`) violates the uniform interface constraint.

!!! quiz
{
"title": "REST Definition",
"question": "REST is best described as:",
"options": ["A database query language", "A specific HTTP library", "An architectural style defined by constraints", "A wire protocol like TCP"],
"answers": ["An architectural style defined by constraints"]
}
!!!

!!! quiz
{
"title": "REST Uniform Interface",
"question": "Which URL design follows the REST uniform interface constraint?",
"options": ["DELETE /api/players/42", "POST /api/deletePlayer", "GET /api/actions?type=delete&id=42", "GET /api/getPlayerById/42"],
"answers": ["DELETE /api/players/42"]
}
!!!

!!! quiz
{
"title": "REST Constraint Count",
"question": "How many constraints did Fielding define for REST?",
"options": ["3", "4", "6", "5"],
"answers": ["6"]
}
!!!

!!! quiz
{
"title": "Optional REST Constraint",
"question": "Which REST constraint is the only optional one?",
"options": ["Statelessness", "Code-on-Demand", "Cacheability", "Layered System"],
"answers": ["Code-on-Demand"]
}
!!!

!!! quiz
{
"title": "Richardson Maturity Level 2",
"question": "An API that uses different URIs for different resources AND uses proper HTTP methods (GET, POST, PUT, DELETE) is at which Richardson Maturity Level?",
"options": ["Level 2", "Level 3", "Level 1", "Level 0"],
"answers": ["Level 2"]
}
!!!

!!! quiz
{
"title": "HATEOAS",
"question": "What does HATEOAS (Richardson Level 3) add beyond Level 2?",
"options": ["Binary encoding of responses", "Server-side session storage", "Encryption of all headers", "Responses include links to available actions and related resources"],
"answers": ["Responses include links to available actions and related resources"]
}
!!!

## Part 6: HTTP Caching

!!! quiz
{
"title": "Cache-Control: no-cache",
"question": "What does `Cache-Control: no-cache` actually mean?",
"options": ["The response expires immediately and is discarded", "The cache may store it but must revalidate with the server before using it", "Only the browser may cache it", "Do not store the response at all"],
"answers": ["The cache may store it but must revalidate with the server before using it"]
}
!!!

!!! quiz
{
"title": "Preventing Caching Entirely",
"question": "Which Cache-Control directive truly prevents any cache from storing the response?",
"options": ["no-cache", "private", "must-revalidate", "no-store"],
"answers": ["no-store"]
}
!!!

!!! quiz
{
"title": "ETag Purpose",
"question": "What is an ETag?",
"options": ["A fingerprint of a resource's content used for conditional requests", "A session token stored in a cookie", "A timestamp of when the server started", "An encryption key for TLS"],
"answers": ["A fingerprint of a resource's content used for conditional requests"]
}
!!!

!!! quiz
{
"title": "304 Not Modified",
"question": "When a client sends `If-None-Match: \"abc123\"` and the server's current ETag matches, what does the server return?",
"options": ["200 OK with the full body", "204 No Content", "304 Not Modified with no body", "400 Bad Request"],
"answers": ["304 Not Modified with no body"]
}
!!!

!!! quiz
{
"title": "Freshness vs Validation",
"question": "Which caching phase avoids the network entirely (no request sent to server)?",
"options": ["Freshness", "Validation", "Revalidation", "Invalidation"],
"answers": ["Freshness"]
}
!!!

!!! quiz
{
"title": "Private vs Public Caching",
"question": "A response contains user-specific inventory data. Which Cache-Control directive is most appropriate?",
"options": ["public, max-age=3600", "no-cache", "private, max-age=30", "immutable"],
"answers": ["private, max-age=30"]
}
!!!

## Part 7: HTTP Evolution

!!! quiz
{
"title": "HTTP/1.0 Problem",
"question": "What was the main performance problem with HTTP/1.0?",
"options": ["It did not support status codes", "Every request required a new TCP connection", "It could only transfer HTML", "It did not support headers"],
"answers": ["Every request required a new TCP connection"]
}
!!!

!!! quiz
{
"title": "HTTP/1.1 Keep-Alive",
"question": "What key feature did HTTP/1.1 introduce to solve the connection-per-request problem?",
"options": ["Binary framing", "QUIC transport", "Server push", "Persistent connections (keep-alive by default)"],
"answers": ["Persistent connections (keep-alive by default)"]
}
!!!

!!! quiz
{
"title": "Head-of-Line Blocking in HTTP/1.1",
"question": "HTTP/1.1 pipelining failed in practice because of Head-of-Line (HoL) blocking. What causes HoL blocking?",
"options": ["The client can only open one TCP connection", "Headers are too large to parse", "The server must respond in order, so a slow response blocks subsequent ones", "TLS encryption slows down every request"],
"answers": ["The server must respond in order, so a slow response blocks subsequent ones"]
}
!!!

!!! quiz
{
"title": "HTTP/2 Wire Format",
"question": "HTTP/2 replaced HTTP/1.1's text-based format with what?",
"options": ["Protobuf serialization", "XML encoding", "CBOR encoding", "A binary framing layer"],
"answers": ["A binary framing layer"]
}
!!!

!!! quiz
{
"title": "HTTP/2 Multiplexing",
"question": "What does multiplexing in HTTP/2 allow?",
"options": ["Multiple request/response streams to share a single TCP connection", "Multiple servers to respond to one request", "Clients to skip the TLS handshake", "Servers to reject slow clients"],
"answers": ["Multiple request/response streams to share a single TCP connection"]
}
!!!

!!! quiz
{
"title": "HPACK Compression",
"question": "HTTP/2 uses HPACK to compress what?",
"options": ["TLS certificates", "HTTP headers", "The request body", "URL paths"],
"answers": ["HTTP headers"]
}
!!!

HTTP/2 solves HoL blocking at the HTTP level with multiplexing, but a lost TCP packet still stalls **all** streams on that connection. HTTP/3 fixes this by replacing TCP with QUIC.

!!! quiz
{
"title": "HTTP/3 Transport",
"question": "HTTP/3 replaces TCP with which transport protocol?",
"options": ["SCTP", "DCCP", "QUIC (built on UDP)", "Raw UDP"],
"answers": ["QUIC (built on UDP)"]
}
!!!

!!! quiz
{
"title": "QUIC 0-RTT",
"question": "For repeat connections, QUIC can establish a connection with how many round trips?",
"options": ["3 RTT", "0 RTT", "2 RTT", "1 RTT"],
"answers": ["0 RTT"]
}
!!!

!!! quiz
{
"title": "QUIC vs TCP HoL Blocking",
"question": "How does QUIC solve TCP-level Head-of-Line blocking?",
"options": ["It uses a bigger congestion window", "It retransmits packets faster", "It disables flow control", "Packet loss on one stream does not stall other streams"],
"answers": ["Packet loss on one stream does not stall other streams"]
}
!!!

## Part 8: HTTP in C++ with Boost.Beast

!!! quiz
{
"title": "Beast Architecture",
"question": "Boost.Beast provides HTTP message types and parsers on top of which library?",
"options": ["Boost.Asio", "Boost.Spirit", "Boost.Filesystem", "Boost.Serialization"],
"answers": ["Boost.Asio"]
}
!!!

!!! quiz
{
"title": "Beast Body Types",
"question": "In Boost.Beast, which body type would you use for a JSON response stored as a std::string?",
"options": ["http::empty_body", "http::file_body", "http::buffer_body", "http::string_body"],
"answers": ["http::string_body"]
}
!!!

!!! quiz
{
"title": "prepare_payload()",
"question": "What does calling `res.prepare_payload()` on a Beast HTTP message do?",
"options": ["Encrypts the body with TLS", "Compresses the body with gzip", "Automatically sets the Content-Length header based on the body size", "Parses JSON in the body"],
"answers": ["Automatically sets the Content-Length header based on the body size"]
}
!!!

!!! quiz
{
"title": "Beast vs Raw Asio",
"question": "Compared to raw Boost.Asio TCP, what does Boost.Beast add?",
"options": ["A completely new socket implementation", "HTTP message parsing and serialization on top of Asio streams", "Automatic database connectivity", "A built-in web browser"],
"answers": ["HTTP message parsing and serialization on top of Asio streams"]
}
!!!

!!! quiz
{
"title": "cpp-httplib Alternative",
"question": "What is the main advantage of cpp-httplib compared to Boost.Beast?",
"options": ["It is a single-header library that is much simpler to use", "It includes a built-in database", "It has better performance under load", "It supports HTTP/3"],
"answers": ["It is a single-header library that is much simpler to use"]
}
!!!

!!! quiz
{
"title": "REST for Games",
"question": "For a game that needs authentication, matchmaking, and leaderboards — but also real-time gameplay at 60 Hz — which combination is most appropriate?",
"options": ["REST for everything including real-time gameplay", "UDP/WebSocket for everything including authentication", "FTP for file transfer and SMTP for game state", "REST/HTTP for auth, matchmaking, leaderboards; UDP for real-time gameplay"],
"answers": ["REST/HTTP for auth, matchmaking, leaderboards; UDP for real-time gameplay"]
}
!!!
