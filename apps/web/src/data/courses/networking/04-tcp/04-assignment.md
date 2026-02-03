# Assignment 04: TCP Chatroom

## Overview

Build a multi-client TCP chatroom using **Boost.Asio** in C++. Your server must handle multiple simultaneous connections without blocking, broadcast messages to all connected clients, and support graceful disconnection.

You may create this on your preferred game engine such as Unity or Unreal, but the networking code should support what is described below.

---

## Submission Requirements

### Video Demonstration (5 minutes maximum)

Your submission is a **video recording** (use OBS Studio or any other recording tools) demonstrating your chatroom working. The video structure is mandatory:

1. **First 30-60 seconds**: Show a text editor (Notepad, VS Code, etc.) with a clear list of:
   - All features you implemented
   - Which requirements are complete vs. partial
   - Any extra features beyond the base requirements

   ⚠️ **If you skip this step, your submission will not be graded.**

2. **Remaining time**: Demonstrate your chatroom working:
   - Show the server starting
   - Connect multiple clients (at least 2-3)
   - Send messages and show broadcast to all clients
   - Demonstrate `/quit` command
   - Show any extra features you implemented

**Upload** your video to YouTube (unlisted), Google Drive, or similar. Submit the link.

---

## Requirements (100 points)

### 1. Multi-Client Support (25 points)

- Server accepts **multiple simultaneous client connections**
- Each client can send and receive messages independently
- Messages from one client are **broadcast to all other connected clients**
- Use the `UserRegistry` pattern from the lecture with username as key

### 2. Non-Blocking Server (25 points)

- Server must **not block** while waiting for client input
- Use one of these approaches:
  - Multi-threaded with `std::jthread` (recommended - see lecture)
  - Asynchronous I/O with Boost.Asio async operations
- One slow or unresponsive client must not affect others

### 3. Single Chatroom (25 points)

- All connected clients are in the **same chatroom**
- When a client sends a message, all other clients receive it
- Format: `[username]: message`
- Server should announce when users join or leave

### 4. Graceful Exit with `/quit` (25 points)

- Client sends `/quit` command to disconnect
- Server acknowledges and closes connection gracefully
- Server announces to other clients: `[Server]: username has left the chat`
- Use proper TCP shutdown sequence (`shutdown()` before `close()`)

---

## Extra Credit Features

### Private Messages: `/msg username message` (+10 points)

- Client can send private message to specific user
- Syntax: `/msg alice Hello, how are you?`
- Only the target user receives the message
- Sender sees confirmation: `[PM to alice]: Hello, how are you?`
- If user doesn't exist: `[Server]: User 'alice' not found`

### Change Username: `/nick newname` (+5 points)

- Client can change their display name
- Syntax: `/nick newusername`
- Server announces: `[Server]: oldname is now known as newname`
- Update the `UserRegistry` to reflect the change
- Reject if username already taken

### Zombie Socket Detection (+10 points)

- Detect clients that disconnect without sending `/quit`
- Handle `SIGPIPE` / broken pipe errors gracefully
- Use TCP keepalive or application-level heartbeat
- Clean up disconnected clients from `UserRegistry`
- Announce: `[Server]: username disconnected unexpectedly`

### Other Extra Features (+5 points each, up to +15)

Implement additional features and document them in your video. Examples:

- **User list command**: `/list` shows all connected users
- **Help command**: `/help` shows available commands
- **Server timestamp**: Messages include `[HH:MM:SS]` timestamp
- **Rate limiting**: Prevent message spam
- **Kick user** (server-side): Admin can disconnect a user
- **Connection logging**: Server logs connections to file
- **Colored output**: ANSI colors for different message types

---

## Technical Specifications

### Server Requirements

```
- Listen on configurable port (default: 12345)
- Accept TCP connections using tcp::acceptor
- Track connected clients with UserRegistry (username as key)
- Broadcast messages to all clients except sender
- Handle commands starting with /
- Clean shutdown on Ctrl+C (optional but recommended)
```

### Client Requirements

```
- Connect to server IP and port
- Prompt user for username on connection
- Send typed messages to server
- Display received messages from server
- Handle /quit command locally and notify server
```

### Message Protocol

Use **newline-delimited** messages for framing:

```
Client → Server: "Hello everyone!\n"
Server → Clients: "[alice]: Hello everyone!\n"
Server → All: "[Server]: bob has joined the chat\n"
```

Each message ends with `\n`. Use `boost::asio::read_until()` with `'\n'` delimiter.

---

## Recommended Architecture

### Server Structure

```cpp
class UserRegistry {
    std::shared_mutex mutex_;
    std::unordered_map<std::string, std::shared_ptr<tcp::socket>> users_;
public:
    bool add_user(const std::string& username, std::shared_ptr<tcp::socket> socket);
    bool remove_user(const std::string& username);
    std::shared_ptr<tcp::socket> get_user(const std::string& username);
    void broadcast(const std::string& message, const std::string& exclude = "");
    std::vector<std::string> list_users();
};
```

### Thread Model (Recommended)

```cpp
// Main thread: accept connections
while (running) {
    auto socket = std::make_shared<tcp::socket>(io_context);
    acceptor.accept(*socket);

    // Spawn handler thread for each client
    std::jthread([socket, &registry](std::stop_token stop) {
        handle_client(socket, registry, stop);
    }).detach();  // or store in vector for clean shutdown
}
```

### Client Handler

```cpp
void handle_client(std::shared_ptr<tcp::socket> socket,
                   UserRegistry& registry,
                   std::stop_token stop) {
    // 1. Receive username
    // 2. Add to registry
    // 3. Announce join
    // 4. Loop: read messages, broadcast or handle commands
    // 5. On /quit or disconnect: remove from registry, announce leave
}
```

---

## Grading Rubric

| Component                          | Points                  |
| ---------------------------------- | ----------------------- |
| **Video starts with feature list** | Required (0 if missing) |
| Multi-client support               | 20                      |
| Non-blocking server                | 20                      |
| Single chatroom broadcast          | 20                      |
| Graceful `/quit` exit              | 20                      |
| **Base Total**                     | **80**                  |
| Extra: `/msg` private messages     | +10                     |
| Extra: `/nick` username change     | +5                      |
| Extra: Zombie socket detection     | +10                     |
| Extra: Other features (max 3)      | +5 each (max +15)       |
| **Maximum Total**                  | **120**                 |

---

## Getting Started

### 1. Review Lecture Material

Read **Section 11: Multi-Client Connection Management** in the lecture notes. It contains:

- `UserRegistry` class with thread-safe operations
- Modern C++ threading with `std::jthread` and `std::stop_token`
- Reader-writer locks with `std::shared_mutex`
- Complete server example code

### 2. Start Simple

1. First, build a single-client echo server
2. Add multi-client support with threads
3. Implement `UserRegistry` and broadcast
4. Add `/quit` command handling
5. Then add extra features

### 3. Testing Tips

- Open multiple terminal windows to simulate multiple clients
- Test what happens when you close a terminal abruptly (zombie detection)
- Test with 3+ simultaneous clients

---

## Common Pitfalls

1. **Forgetting TCP is a byte stream** — Always use `read_until('\n')` for message framing

2. **Deadlocks with mutex** — Use `std::shared_lock` for reads, `std::unique_lock` for writes

3. **Race condition on disconnect** — Client might disconnect while you're iterating users

4. **Blocking accept loop** — Don't process messages in accept thread; spawn handlers

5. **Memory leaks** — Use `std::shared_ptr<tcp::socket>` for proper cleanup

6. **Forgetting graceful shutdown** — Call `shutdown()` before `close()`

---

## Resources

- [Boost.Asio TCP Tutorial](https://www.boost.org/doc/libs/latest/doc/html/boost_asio/tutorial.html)
- [Boost.Asio Chat Example](https://www.boost.org/doc/libs/latest/doc/html/boost_asio/example/cpp11/chat/)
- [std::jthread Reference](https://en.cppreference.com/w/cpp/thread/jthread)
- [std::shared_mutex Reference](https://en.cppreference.com/w/cpp/thread/shared_mutex)

---

## Questions?

Post questions in the google chat or ask during office hours.
