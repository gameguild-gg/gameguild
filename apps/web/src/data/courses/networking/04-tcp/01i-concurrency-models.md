## Alternative Concurrency Models

While `std::jthread` is recommended for beginners, there are other approaches to handling multiple clients. Each has trade-offs in complexity, performance, and scalability.

### Comparison of Concurrency Models

| Model                              | Complexity | Scalability                | Best For                 |
| ---------------------------------- | ---------- | -------------------------- | ------------------------ |
| Thread per client (`std::jthread`) | Low        | Moderate (100s of clients) | Learning, simple servers |
| Async I/O (Boost.Asio)             | High       | Excellent (10,000s)        | Production servers       |

---

### Asynchronous I/O with Boost.Asio

Async I/O uses **callbacks** instead of threads. A single thread can handle thousands of connections by processing events as they occur.

**Key Concept:** Instead of blocking on `read()`, you say "call this function when data arrives."

```cpp
#include <boost/asio.hpp>
#include <memory>
#include <map>
#include <iostream>

using boost::asio::ip::tcp;

// Forward declarations
class ChatSession;
class ChatServer;

// Shared state for all sessions
class ChatRoom {
public:
    void join(std::shared_ptr<ChatSession> session) {
        sessions_[session->username()] = session;
    }

    void leave(const std::string& username) {
        sessions_.erase(username);
    }

    void broadcast(const std::string& from, const std::string& message);

private:
    std::map<std::string, std::shared_ptr<ChatSession>> sessions_;
};

// Represents one connected client
class ChatSession : public std::enable_shared_from_this<ChatSession> {
public:
    ChatSession(tcp::socket socket, ChatRoom& room)
        : socket_(std::move(socket)), room_(room) {}

    void start() {
        // First, read the username
        do_read_username();
    }

    void deliver(const std::string& message) {
        bool write_in_progress = !write_queue_.empty();
        write_queue_.push_back(message);
        if (!write_in_progress) {
            do_write();
        }
    }

    const std::string& username() const { return username_; }

private:
    void do_read_username() {
        auto self = shared_from_this();

        // Async read until newline - doesn't block!
        boost::asio::async_read_until(socket_, buffer_, '\n',
            [this, self](boost::system::error_code ec, std::size_t length) {
                if (!ec) {
                    std::istream is(&buffer_);
                    std::getline(is, username_);

                    std::cout << "User '" << username_ << "' connected\n";
                    room_.join(self);
                    room_.broadcast("SERVER", username_ + " joined the chat");

                    // Now start reading messages
                    do_read_message();
                }
            });
    }

    void do_read_message() {
        auto self = shared_from_this();

        boost::asio::async_read_until(socket_, buffer_, '\n',
            [this, self](boost::system::error_code ec, std::size_t length) {
                if (!ec) {
                    std::istream is(&buffer_);
                    std::string message;
                    std::getline(is, message);

                    if (message == "QUIT") {
                        room_.broadcast("SERVER", username_ + " left the chat");
                        room_.leave(username_);
                        socket_.close();
                        return;
                    }

                    room_.broadcast(username_, message);
                    do_read_message();  // Continue reading
                } else {
                    // Connection closed or error
                    room_.broadcast("SERVER", username_ + " disconnected");
                    room_.leave(username_);
                }
            });
    }

    void do_write() {
        auto self = shared_from_this();

        boost::asio::async_write(socket_,
            boost::asio::buffer(write_queue_.front()),
            [this, self](boost::system::error_code ec, std::size_t) {
                if (!ec) {
                    write_queue_.pop_front();
                    if (!write_queue_.empty()) {
                        do_write();  // Write next message
                    }
                }
            });
    }

    tcp::socket socket_;
    ChatRoom& room_;
    boost::asio::streambuf buffer_;
    std::string username_;
    std::deque<std::string> write_queue_;
};

// Broadcast implementation (needs ChatSession definition)
void ChatRoom::broadcast(const std::string& from, const std::string& message) {
    std::string formatted = "[" + from + "]: " + message + "\n";
    for (auto& [username, session] : sessions_) {
        if (username != from) {
            session->deliver(formatted);
        }
    }
}

// Accepts incoming connections
class ChatServer {
public:
    ChatServer(boost::asio::io_context& io_context, short port)
        : acceptor_(io_context, tcp::endpoint(tcp::v4(), port)) {
        acceptor_.set_option(tcp::acceptor::reuse_address(true));
        do_accept();
    }

private:
    void do_accept() {
        acceptor_.async_accept(
            [this](boost::system::error_code ec, tcp::socket socket) {
                if (!ec) {
                    std::make_shared<ChatSession>(
                        std::move(socket), room_)->start();
                }
                do_accept();  // Accept next connection
            });
    }

    tcp::acceptor acceptor_;
    ChatRoom room_;
};

int main() {
    boost::asio::io_context io_context;

    ChatServer server(io_context, 12345);

    std::cout << "Async chat server on port 12345\n";
    std::cout << "Single thread handling all clients!\n";

    // Run the event loop - processes all async operations
    io_context.run();

    return 0;
}
```

**Key Points:**

- **Single-threaded**: One thread handles ALL clients via event loop
- **Non-blocking**: `async_read_until` returns immediately
- **Callback chains**: Each operation triggers the next via lambdas
- **`shared_from_this()`**: Prevents session from being destroyed mid-operation
- **Scalable**: Can handle 10,000+ connections on one thread

---

### Which Model Should You Choose?

| Scenario                                 | Recommended Model         |
| ---------------------------------------- | ------------------------- |
| Learning / Assignment                    | `std::jthread` per client |
| Simple production server (< 100 clients) | `std::jthread` per client |
| Moderate scale (100-1000 clients)        | Boost.Asio async          |
| High scale (> 1000 clients)              | Boost.Asio async          |
| Game server (low latency)                | Boost.Asio async          |
