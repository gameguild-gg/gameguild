# Quiz 04: TCP and Stream Sockets

!!! quiz
{
"title": "TCP Three-Way Handshake",
"question": "What is the correct sequence of the TCP three-way handshake?",
"options": ["SYN -> SYN-ACK -> ACK", "ACK -> SYN -> SYN-ACK", "SYN-ACK -> SYN -> ACK", "SYN -> ACK -> SYN-ACK"],
"answers": ["SYN -> SYN-ACK -> ACK"]
}
!!!

!!! quiz
{
"title": "TCP Connection State",
"question": "A TCP server has received a FIN from a client and sent an ACK, but has not yet sent its own FIN. What state is the server in?",
"options": ["FIN_WAIT_1", "TIME_WAIT", "FIN_WAIT_2", "CLOSE_WAIT"],
"answers": ["CLOSE_WAIT"]
}
!!!

!!! quiz
{
"title": "Flow Control vs Congestion Control",
"question": "Which TCP mechanism prevents a fast sender from overwhelming a slow receiver?",
"options": ["Congestion control", "Flow control (sliding window)", "Slow start", "Fast retransmit"],
"answers": ["Flow control (sliding window)"]
}
!!!

!!! quiz
{
"title": "TIME_WAIT Purpose",
"question": "Why does TCP use the TIME_WAIT state after closing a connection?",
"options": ["To allow the application to finish processing data", "To retry sending the final FIN packet", "To ensure delayed segments from the old connection don't corrupt a new connection on the same port", "To reduce CPU usage during connection teardown"],
"answers": ["To ensure delayed segments from the old connection don't corrupt a new connection on the same port"]
}
!!!

!!! quiz
{
"title": "Head-of-Line Blocking",
"question": "What problem does head-of-line blocking describe in TCP?",
"options": ["The first packet in a sequence is always dropped", "The TCP header is too large for efficient transmission", "A lost packet delays delivery of all subsequent packets until retransmitted", "Multiple clients cannot connect to the same server port"],
"answers": ["A lost packet delays delivery of all subsequent packets until retransmitted"]
}
!!!

!!! quiz
{
"title": "TCP Sequence Number",
"question": "What does the TCP sequence number represent?",
"options": ["The total number of packets sent in the connection", "The priority level of the segment", "The number of retransmissions attempted", "The byte position of the first byte of data in this segment"],
"answers": ["The byte position of the first byte of data in this segment"]
}
!!!

!!! quiz
{
"title": "Connection Refused",
"question": "In Boost.Asio, a client calls socket.connect(endpoint) to a server that is not running. What happens?",
"options": ["The call throws boost::system::system_error with connection refused", "The call blocks forever", "The connection enters ESTABLISHED state with an empty server", "The socket automatically retries until the server starts"],
"answers": ["The call throws boost::system::system_error with connection refused"]
}
!!!

!!! quiz
{
"title": "Listen Backlog",
"question": "What is the purpose of the backlog parameter in acceptor.listen(backlog)?",
"options": ["Maximum number of simultaneous established connections", "Maximum number of pending connections waiting to be accepted", "Timeout in seconds for incoming connections", "Size of the receive buffer in bytes"],
"answers": ["Maximum number of pending connections waiting to be accepted"]
}
!!!

!!! quiz
{
"title": "AIMD Algorithm",
"question": "During congestion avoidance, how does TCP's AIMD algorithm adjust the congestion window?",
"options": ["Additive increase on ACK, additive decrease on loss", "Additive increase on ACK, multiplicative decrease on loss", "Multiplicative increase on ACK, multiplicative decrease on loss", "Multiplicative increase on ACK, additive decrease on loss"],
"answers": ["Additive increase on ACK, multiplicative decrease on loss"]
}
!!!

!!! quiz
{
"title": "Reuse Address Option",
"question": "Which Boost.Asio socket option allows a server to bind to a port still in TIME_WAIT state?",
"options": ["tcp::socket::keep_alive", "tcp::socket::linger", "tcp::socket::broadcast", "tcp::acceptor::reuse_address"],
"answers": ["tcp::acceptor::reuse_address"]
}
!!!

!!! quiz
{
"title": "TCP Byte Stream",
"question": "A client sends 1000 bytes with one socket.send() call. How many socket.receive() calls might the server need?",
"options": ["One or more - TCP doesn't preserve message boundaries", "Exactly one", "Exactly two", "Zero - data is delivered automatically"],
"answers": ["One or more - TCP doesn't preserve message boundaries"]
}
!!!

!!! quiz
{
"title": "TCP Acknowledgment Number",
"question": "What does the TCP acknowledgment number indicate?",
"options": ["The sequence number of the next byte the receiver expects", "The sequence number of the last byte successfully received", "The total bytes received so far", "The number of segments acknowledged"],
"answers": ["The sequence number of the next byte the receiver expects"]
}
!!!

!!! quiz
{
"title": "TCP Slow Start",
"question": "How does TCP slow start behave?",
"options": ["Congestion window increases by 1 segment per RTT", "Congestion window starts at maximum and decreases", "Congestion window remains constant until loss", "Congestion window doubles each RTT (exponential growth)"],
"answers": ["Congestion window doubles each RTT (exponential growth)"]
}
!!!

!!! quiz
{
"title": "TCP Connection Identifier",
"question": "What uniquely identifies a TCP connection?",
"options": ["Source IP and source port only", "Destination IP and destination port only", "Source IP, source port, destination IP, and destination port (4-tuple)", "Source port and destination port only"],
"answers": ["Source IP, source port, destination IP, and destination port (4-tuple)"]
}
!!!

!!! quiz
{
"title": "Fast Retransmit",
"question": "When can TCP use fast retransmit instead of waiting for a timeout?",
"options": ["When the receiver requests it explicitly", "After receiving 3 duplicate ACKs for the same sequence number", "When the congestion window is full", "Only during slow start phase"],
"answers": ["After receiving 3 duplicate ACKs for the same sequence number"]
}
!!!

!!! quiz
{
"title": "Graceful TCP Close",
"question": "In Boost.Asio, what is the correct way to gracefully close a TCP connection after sending data?",
"options": ["Call socket.close() immediately", "Delete the socket object", "Call socket.shutdown(tcp::socket::shutdown_both) then socket.close()", "Set socket.set_option(tcp::socket::linger(false, 0))"],
"answers": ["Call socket.shutdown(tcp::socket::shutdown_both) then socket.close()"]
}
!!!

!!! quiz
{
"title": "Accept Loop Problem",
"question": "A server's accept loop stops and new clients cannot connect, but existing connections work fine. The server is not at its file descriptor limit. What is the most likely cause?",
"options": ["The network cable is unplugged", "TCP congestion control is blocking new connections", "The clients are using the wrong port number", "The listen backlog queue is full and the application isn't calling accept() fast enough"],
"answers": ["The listen backlog queue is full and the application isn't calling accept() fast enough"]
}
!!!

!!! quiz
{
"title": "Receive Buffer Full",
"question": "What happens when TCP's receive buffer fills up completely?",
"options": ["The receiver advertises window size 0, causing the sender to stop transmitting", "New packets are silently discarded by the receiver", "The connection is automatically closed", "The receiver sends RST to abort the connection"],
"answers": ["The receiver advertises window size 0, causing the sender to stop transmitting"]
}
!!!

!!! quiz
{
"title": "SYN_SENT State",
"question": "In the TCP state diagram, which state does a client enter immediately after sending a SYN?",
"options": ["ESTABLISHED", "SYN_SENT", "SYN_RECEIVED", "LISTEN"],
"answers": ["SYN_SENT"]
}
!!!

!!! quiz
{
"title": "Congestion Window After Timeout",
"question": "A TCP sender has cwnd = 4 segments and ssthresh = 16 segments. After receiving ACKs for all 4 segments, a timeout occurs when cwnd = 12. What are the new values?",
"options": ["cwnd = 6, ssthresh = 12", "cwnd = 12, ssthresh = 6", "cwnd = 1, ssthresh = 6", "cwnd = 1, ssthresh = 12"],
"answers": ["cwnd = 1, ssthresh = 6"]
}
!!!
