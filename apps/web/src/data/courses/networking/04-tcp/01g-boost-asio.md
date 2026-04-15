## TCP Programming with Boost.Asio

### Client Connection

```cpp
#include <boost/asio.hpp>
#include <iostream>

using boost::asio::ip::tcp;

int main() {
    boost::asio::io_context io_context;

    // Create socket
    tcp::socket socket(io_context);

    // Resolve hostname to IP address
    tcp::resolver resolver(io_context);
    auto endpoints = resolver.resolve("localhost", "8080");

    // or use IP directly
    // auto endpoints = tcp::endpoint(boost::asio::ip::make_address("127.0.0.1"), 12345);

    // Connect (throws on failure)
    boost::asio::connect(socket, endpoints);

    std::cout << "Connected!" << std::endl;

    // ... send/receive data ...

    // Graceful shutdown
    socket.shutdown(tcp::socket::shutdown_both);
    socket.close();

    return 0;
}
```

### Server Setup

```cpp
#include <boost/asio.hpp>
#include <iostream>

using boost::asio::ip::tcp;

int main() {
    boost::asio::io_context io_context;

    // Create acceptor on port 12345
    tcp::acceptor acceptor(io_context, tcp::endpoint(tcp::v4(), 12345));

    // Enable port reuse (important for development)
    acceptor.set_option(tcp::acceptor::reuse_address(true));

    // Set listen backlog
    acceptor.listen(128);

    std::cout << "Server listening on port 12345..." << std::endl;

    while (true) {
        // Accept incoming connection
        tcp::socket socket(io_context);
        acceptor.accept(socket);

        std::cout << "Client connected: "
                  << socket.remote_endpoint() << std::endl;

        // Handle client...

        // Graceful close
        socket.shutdown(tcp::socket::shutdown_both);
        socket.close();
    }

    return 0;
}
```

### Important Socket Options

```cpp
// Allow binding to port in TIME_WAIT state
acceptor.set_option(tcp::acceptor::reuse_address(true));

// Enable TCP keepalive probes
socket.set_option(boost::asio::socket_base::keep_alive(true));

// Set linger behavior on close
socket.set_option(boost::asio::socket_base::linger(true, 30));
```

### Handling Partial Reads

Since TCP is a byte stream, you must handle partial data:

```cpp
// WRONG: Assumes all data arrives in one read
char buffer[1024];
size_t len = socket.read_some(boost::asio::buffer(buffer));

// CORRECT: Read until you have a complete message
std::string message;
boost::asio::streambuf buffer;

// Read until newline delimiter
boost::asio::read_until(socket, buffer, '\n');
std::istream is(&buffer);
std::getline(is, message);
```

### Graceful Shutdown Pattern

```cpp
void close_connection(tcp::socket& socket) {
    boost::system::error_code ec;

    // 1. Shutdown both directions (sends FIN)
    socket.shutdown(tcp::socket::shutdown_both, ec);
    if (ec) {
        std::cerr << "Shutdown error: " << ec.message() << std::endl;
    }

    // 2. Close the socket
    socket.close(ec);
    if (ec) {
        std::cerr << "Close error: " << ec.message() << std::endl;
    }
}
```

**Common mistake:** Calling `close()` without `shutdown()` may cause buffered data to be lost. Always shutdown first for graceful termination.

### Listen Backlog

The backlog parameter in `listen()` controls how many pending connections can queue:

```cpp
acceptor.listen(128);  // Up to 128 pending connections
```

If the application doesn't call `accept()` fast enough:

- Backlog fills up
- New connection attempts receive RST or are silently dropped
- Existing established connections are unaffected
