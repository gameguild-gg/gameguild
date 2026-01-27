# Assignment 03: UDP Echo Server & Client

Build a UDP echo server and client using Boost.Asio. The client discovers the server via broadcast, then exchanges messages.

## Learning Objectives

- Use `udp::socket` for datagram communication
- Implement the echo pattern: `receive_from()` → `send_to()`
- Use UDP broadcast for LAN discovery

## Project Structure

```
projects/03-udp/
├── src/
│   ├── server.h    # UdpEchoServer class (implement TODO sections)
│   ├── client.h    # UdpEchoClient class (implement TODO sections)
│   ├── server.cpp  # Server executable
│   └── client.cpp  # Client executable
└── tests/
    └── tests.cpp   # Automated tests
```

---

## Part 1: UDP Echo Server

Implement the `UdpEchoServer` class in `src/server.h`:

- Bind to a UDP port
- Receive datagrams and echo them back to the sender

---

## Part 2: UDP Echo Client with Discovery

Implement the `UdpEchoClient` class in `src/client.h`:

- Broadcast `"DISCOVER"` to find a server
- Save the responding server's endpoint
- Send messages to the server and receive echoes

---

## Grading Rubric (10 points)

| Component                       | Points | Criteria                                            |
| ------------------------------- | ------ | --------------------------------------------------- |
| **Server: Basic Echo**          | 4.0    | Server receives UDP datagrams and echoes them back  |
| **Client: Broadcast Discovery** | 2.5    | Client sends broadcast and receives server response |
| **Client: Interactive Echo**    | 2.0    | Client sends user input, displays echo response     |
| **Code Quality**                | 1.5    | Clean code, proper error handling, good structure   |
| **Total**                       | **10** |                                                     |

---

## Extra Credit: UDP Chat Room (+3 points)

Implement a chat room where the server broadcasts received messages to all "connected" clients.

### Requirements

1. **Server tracks clients**: Maintain a list of client endpoints
2. **Client registration**: First message from a client adds them to the list
3. **Broadcast messages**: When server receives a message, send it to ALL registered clients
4. **Client timeout**: Remove clients that haven't sent a message in 30 seconds
5. **Username support**: First message is the username, subsequent messages are chat

### Chat Protocol

```
Client → Server: "JOIN:username"     → Server adds client, broadcasts "username joined"
Client → Server: "MSG:hello world"   → Server broadcasts "username: hello world" to all
Client → Server: "LEAVE"             → Server removes client, broadcasts "username left"
```

### Submission

- **Record a video** (1-2 minutes) demonstrating:
  - Starting the server
  - Two or more clients connecting
  - Clients exchanging messages
  - Messages appearing on all clients
- **Submit the video** to Canvas along with your code

---

## Common Pitfalls

1. **Broadcast requires opt-in**: Call `set_option(broadcast(true))` before sending to broadcast address
2. **Buffer size matters**: UDP truncates if buffer is smaller than datagram — use 1200 bytes
3. **`receive_from()` blocks**: Server must be running before client tries discovery
4. **Use `sender_endpoint`**: Server needs it to know where to echo back

---

## Submission Checklist

- [ ] `UdpEchoServer` echoes messages correctly
- [ ] `UdpEchoClient` discovers server via broadcast
- [ ] All tests pass
- [ ] (Extra Credit) Chat room video submitted

## Server

```c++
/**
 * UDP Echo Server
 *
 * Assignment 03: UDP and Datagram Sockets
 *
 * A simple UDP echo server that:
 * 1. Binds to a UDP port
 * 2. Receives datagrams from clients
 * 3. Echoes back the received message (including DISCOVER requests)
 *
 * TODO: Implement the server methods following the pseudocode in README.md
 */

#ifndef UDP_ECHO_SERVER_H
#define UDP_ECHO_SERVER_H

#include <boost/asio.hpp>
#include <iostream>
#include <string>
#include <array>

using boost::asio::ip::udp;

constexpr size_t MAX_UDP_PAYLOAD = 1200;
constexpr std::string_view DISCOVER_MESSAGE = "DISCOVER";

/**
 * UDP Echo Server class
 *
 * Example usage:
 *   boost::asio::io_context io;
 *   UdpEchoServer server(io, 9999);
 *   while (true) {
 *       server.process_one();
 *   }
 */
class UdpEchoServer {
public:
    /**
     * Construct server bound to specified port
     * @param io_context The Boost.Asio io_context
     * @param port Port to listen on (default: 9999)
     */
    UdpEchoServer(boost::asio::io_context& io_context, uint16_t port = 9999)
        : m_socket(io_context)
        , m_port(port)
    {
        // TODO: Open socket and bind to port
    }

    /**
     * Get the port the server is bound to
     * This is useful when binding to port 0 (ephemeral port)
     */
    uint16_t port() const {
        // TODO: Return the actual bound port (use local_endpoint())
        return m_port;
    }

    /**
     * Process one message: receive, print, and echo back
     * This function blocks until a message is received
     * @return The message that was received
     */
    std::string process_one() {
        // TODO: Receive a message, print it, echo it back, return it
        // Use: receive_from(), send_to(), boost::asio::buffer()

        return "";  // Placeholder - implement this!
    }

private:
    udp::socket m_socket;
    uint16_t m_port;
};

// ============================================================================
// Main entry point function - DO NOT MODIFY
// ============================================================================

/**
 * Run the UDP echo server (called from main)
 * @param port Port to listen on
 * @return Exit code (0 = success)
 */
inline int run_echo_server(uint16_t port) {
    try {
        boost::asio::io_context io_context;
        UdpEchoServer server(io_context, port);

        std::cout << "UDP Echo Server listening on port " << server.port() << "...\n";
        std::cout << "Press Ctrl+C to stop.\n\n";

        while (true) {
            server.process_one();
        }

    } catch (const std::exception& e) {
        std::cerr << "Error: " << e.what() << "\n";
        return 1;
    }

    return 0;
}

#endif // UDP_ECHO_SERVER_H
```

## Client

```c++
/**
 * UDP Echo Client with Broadcast Discovery
 *
 * Assignment 03: UDP and Datagram Sockets
 *
 * A simple UDP echo client that:
 * 1. Broadcasts "DISCOVER" to find a server on the LAN
 * 2. Waits for the echo response from a server
 * 3. Sends messages and receives echoes
 *
 * TODO: Implement the client methods following the pseudocode in README.md
 */

#ifndef UDP_ECHO_CLIENT_H
#define UDP_ECHO_CLIENT_H

#include <boost/asio.hpp>
#include <iostream>
#include <string>
#include <array>
#include <optional>

#include "server.h"

using boost::asio::ip::udp;

/**
 * UDP Echo Client class
 *
 * Example usage:
 *   boost::asio::io_context io;
 *   UdpEchoClient client(io);
 *
 *   // Discover a server on LAN (blocks until found)
 *   auto server = client.discover(9999);
 *   client.connect(server);
 *   auto echo = client.send_and_receive("Hello!");
 */
class UdpEchoClient {
public:
    /**
     * Construct client
     * @param io_context The Boost.Asio io_context
     */
    explicit UdpEchoClient(boost::asio::io_context& io_context)
        : m_socket(io_context)
    {
        // TODO: Open socket and enable broadcast option
    }

    /**
     * Discover a server on the LAN via broadcast
     * Blocks until a server responds
     * @param port Port to broadcast to
     * @return The server endpoint that responded
     */
    udp::endpoint discover([[maybe_unused]] uint16_t port = 9999) {
        // TODO: Broadcast DISCOVER_MESSAGE to 255.255.255.255:port
        //       Wait for response, disable broadcast, return server endpoint
        // Use: address_v4::broadcast(), send_to(), receive_from()

        return {};  // Placeholder - implement this!
    }

    /**
     * Set the server endpoint to communicate with
     * @param server_endpoint The server's endpoint
     */
    void connect(const udp::endpoint& server_endpoint) {
        m_server_endpoint = server_endpoint;
    }

    /**
     * Check if connected to a server
     */
    bool is_connected() const {
        return m_server_endpoint.port() != 0;
    }

    /**
     * Get the server endpoint
     */
    udp::endpoint server_endpoint() const {
        return m_server_endpoint;
    }

    /**
     * Send a message and receive the echo
     * Blocks until response is received
     * @param message The message to send
     * @return The echoed message, or nullopt if not connected
     */
    std::optional<std::string> send_and_receive([[maybe_unused]] const std::string& message) {
        if (!is_connected()) {
            return std::nullopt;
        }

        // TODO: Send message to server, receive and return the echo
        // Use: send_to(), receive_from(), boost::asio::buffer()

        return std::nullopt;  // Placeholder - implement this!
    }

    /**
     * Get the underlying socket (for testing)
     */
    udp::socket& socket() {
        return m_socket;
    }

private:
    udp::socket m_socket;
    udp::endpoint m_server_endpoint;
};

// ============================================================================
// Main entry point function - DO NOT MODIFY
// ============================================================================

/**
 * Run the UDP echo client (called from main)
 * @param port Server port to discover/connect to
 * @return Exit code (0 = success)
 */
inline int run_echo_client(uint16_t port) {
    try {
        boost::asio::io_context io_context;
        UdpEchoClient client(io_context);

        // ==================== DISCOVERY PHASE ====================

        std::cout << "Searching for servers on LAN (port " << port << ")...\n";
        std::cout << "(Waiting for server response...)\n";

        auto server = client.discover(port);
        client.connect(server);

        std::cout << "Found server at " << client.server_endpoint() << "\n";

        // ==================== INTERACTIVE PHASE ====================

        std::cout << "\nConnected! Type messages (or 'quit' to exit):\n\n";

        std::string line;
        while (true) {
            std::cout << "> " << std::flush;

            if (!std::getline(std::cin, line)) {
                break;
            }

            if (line == "quit" || line == "exit") {
                std::cout << "Goodbye!\n";
                break;
            }

            if (line.empty()) {
                continue;
            }

            auto echo = client.send_and_receive(line);

            if (echo) {
                std::cout << "Echo: " << *echo << "\n\n";
            } else {
                std::cerr << "Error: not connected to server\n\n";
            }
        }

    } catch (const std::exception& e) {
        std::cerr << "Error: " << e.what() << "\n";
        return 1;
    }

    return 0;
}

#endif // UDP_ECHO_CLIENT_H
```
