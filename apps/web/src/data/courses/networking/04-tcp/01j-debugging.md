## Common TCP Issues and Debugging

### "Address Already in Use" Error

**Cause:** Trying to bind to a port still in TIME_WAIT state

**Solution:**

```cpp
acceptor.set_option(tcp::acceptor::reuse_address(true));
```

### Connection Refused

**Cause:** No process listening on the target port

**Debug:** Verify server is running, check firewall rules, confirm port number

### Data Loss on Close

**Cause:** Calling `close()` immediately without `shutdown()`

**Solution:** Always use graceful shutdown sequence

### Application Hangs on Read

**Cause:** TCP byte stream - waiting for more data that won't arrive

**Solution:** Implement proper message framing (length prefix or delimiter)

### Viewing Connection States

Use `netstat` or `ss` to inspect TCP connections:

**Linux/macOS:**

```bash
# Show all TCP connections
ss -tan

# Show connections with process info
ss -tanp

# Filter by state
ss -tan state established
ss -tan state time-wait
```

**Windows (PowerShell or Command Prompt):**

```powershell
# Show all TCP connections
netstat -an -p tcp

# Show connections with process IDs
netstat -ano -p tcp

# Show connections with process names (PowerShell)
Get-NetTCPConnection | Select-Object LocalAddress, LocalPort, RemoteAddress, RemotePort, State, OwningProcess

# Filter by state (PowerShell)
Get-NetTCPConnection -State Established
Get-NetTCPConnection -State TimeWait
```

---

## Summary

TCP provides reliable, ordered, byte-stream communication through:

1. **Three-way handshake** - Establishes connection and synchronizes sequence numbers
2. **Sequence/ACK numbers** - Track every byte for reliability and ordering
3. **Flow control** - Sliding window prevents receiver buffer overflow
4. **Congestion control** - Slow start and AIMD prevent network congestion
5. **Four-way termination** - Graceful connection close

**Key implementation points for Boost.Asio:**

- Use `reuse_address` option for development
- Handle partial reads - TCP doesn't preserve message boundaries
- Always `shutdown()` before `close()` for graceful termination
- Set appropriate listen backlog for your expected connection rate
- Maintain a user registry (map) keyed by username for multi-client servers
- Use `std::jthread` (C++20) for automatic thread cleanup - no `.detach()` needed
- Use `std::shared_mutex` for read-heavy workloads (multiple readers, one writer)
- Use `std::atomic<bool>` for simple lock-free status flags
- Use `std::shared_ptr` for safe socket ownership across threads

**For the chatroom assignment:**

- Design a user registry that stores connected clients by username
- Implement broadcast functionality to send messages to all users
- Process commands (messages starting with `/`) separately from regular chat
- For `/quit`: return from handler loop, clean up, announce departure
- For `/list`, `/help`: send response only to the requesting client
- For `/msg`: look up target user, send only to their socket
- Use `std::jthread` for automatic thread cleanup
- Use `std::shared_lock` for reads, `std::unique_lock` for writes (or simpler `std::mutex` with `std::lock_guard`)
- Clean up resources when users disconnect
- Compile with `-std=c++20` to access modern threading features
