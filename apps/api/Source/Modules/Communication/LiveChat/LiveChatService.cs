namespace GameGuild.Modules.Communication.LiveChat;

/// <summary>
/// Represents a chat room/conversation.
/// </summary>
public sealed class ChatRoom
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public ChatRoomType Type { get; set; }
    public List<Guid> ParticipantIds { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}

/// <summary>
/// Type of chat room.
/// </summary>
public enum ChatRoomType
{
    DirectMessage,
    GroupChat,
    SupportSession
}

/// <summary>
/// Represents a chat message.
/// </summary>
public sealed class ChatMessage
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public Guid SenderId { get; set; }
    public required string Content { get; set; }
    public MessageType Type { get; set; }
    public List<string> Attachments { get; set; } = new();
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }
}

/// <summary>
/// Type of message content.
/// </summary>
public enum MessageType
{
    Text,
    Image,
    File,
    SystemNotification
}

/// <summary>
/// Represents user presence status.
/// </summary>
public sealed class UserPresence
{
    public Guid UserId { get; set; }
    public PresenceStatus Status { get; set; }
    public string? CustomStatus { get; set; }
    public DateTime LastSeen { get; set; }
    public DateTime? LastActivityAt { get; set; }
}

/// <summary>
/// User presence status.
/// </summary>
public enum PresenceStatus
{
    Online,
    Away,
    Busy,
    Offline
}

/// <summary>
/// Represents a typing indicator.
/// </summary>
public sealed class TypingIndicator
{
    public Guid RoomId { get; set; }
    public Guid UserId { get; set; }
    public DateTime StartedAt { get; set; }
}

/// <summary>
/// Result of file sharing operation.
/// </summary>
public sealed class FileShareResult
{
    public required string Url { get; set; }
    public required string FileName { get; set; }
    public long FileSize { get; set; }
    public required string ContentType { get; set; }
}

/// <summary>
/// Service interface for live chat operations.
/// </summary>
public interface ILiveChatService
{
    /// <summary>
    /// Creates a new chat room.
    /// </summary>
    Task<ChatRoom> CreateRoomAsync(
        string name,
        ChatRoomType type,
        List<Guid> participantIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message to a chat room.
    /// </summary>
    Task<ChatMessage> SendMessageAsync(
        Guid roomId,
        Guid senderId,
        string content,
        MessageType type = MessageType.Text,
        List<string>? attachments = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets message history for a room.
    /// </summary>
    Task<IReadOnlyList<ChatMessage>> GetMessageHistoryAsync(
        Guid roomId,
        int limit = 100,
        DateTime? before = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks messages as read.
    /// </summary>
    Task MarkAsReadAsync(
        Guid roomId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates user presence status.
    /// </summary>
    Task<UserPresence> UpdatePresenceAsync(
        Guid userId,
        PresenceStatus status,
        string? customStatus = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user presence information.
    /// </summary>
    Task<UserPresence?> GetPresenceAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts typing indicator.
    /// </summary>
    Task StartTypingAsync(
        Guid roomId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops typing indicator.
    /// </summary>
    Task StopTypingAsync(
        Guid roomId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active typing indicators for a room.
    /// </summary>
    Task<IReadOnlyList<TypingIndicator>> GetTypingUsersAsync(
        Guid roomId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Shares a file in a chat room.
    /// </summary>
    Task<FileShareResult> ShareFileAsync(
        Guid roomId,
        Guid userId,
        Stream fileContent,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a participant to a room.
    /// </summary>
    Task AddParticipantAsync(
        Guid roomId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a participant from a room.
    /// </summary>
    Task RemoveParticipantAsync(
        Guid roomId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes a chat room.
    /// </summary>
    Task CloseRoomAsync(
        Guid roomId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active rooms for a user.
    /// </summary>
    Task<IReadOnlyList<ChatRoom>> GetUserRoomsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of live chat service with real-time messaging support.
/// </summary>
public sealed class LiveChatService : ILiveChatService
{
    private readonly ILogger<LiveChatService> _logger;
    private readonly Dictionary<Guid, ChatRoom> _rooms = new();
    private readonly Dictionary<Guid, List<ChatMessage>> _messages = new();
    private readonly Dictionary<Guid, UserPresence> _presence = new();
    private readonly Dictionary<Guid, List<TypingIndicator>> _typingIndicators = new();

    public LiveChatService(ILogger<LiveChatService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<ChatRoom> CreateRoomAsync(
        string name,
        ChatRoomType type,
        List<Guid> participantIds,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating chat room: {Name} with {Count} participants", name, participantIds.Count);

        var room = new ChatRoom
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            ParticipantIds = participantIds,
            CreatedAt = DateTime.UtcNow
        };

        _rooms[room.Id] = room;
        _messages[room.Id] = new List<ChatMessage>();

        return Task.FromResult(room);
    }

    public Task<ChatMessage> SendMessageAsync(
        Guid roomId,
        Guid senderId,
        string content,
        MessageType type = MessageType.Text,
        List<string>? attachments = null,
        CancellationToken cancellationToken = default)
    {
        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            SenderId = senderId,
            Content = content,
            Type = type,
            Attachments = attachments ?? new List<string>(),
            SentAt = DateTime.UtcNow,
            IsRead = false
        };

        if (!_messages.ContainsKey(roomId))
        {
            _messages[roomId] = new List<ChatMessage>();
        }

        _messages[roomId].Add(message);
        _logger.LogInformation("Message sent to room {RoomId}", roomId);

        return Task.FromResult(message);
    }

    public Task<IReadOnlyList<ChatMessage>> GetMessageHistoryAsync(
        Guid roomId,
        int limit = 100,
        DateTime? before = null,
        CancellationToken cancellationToken = default)
    {
        if (!_messages.TryGetValue(roomId, out var messages))
        {
            return Task.FromResult<IReadOnlyList<ChatMessage>>(Array.Empty<ChatMessage>());
        }

        var query = messages.AsEnumerable();
        if (before.HasValue)
        {
            query = query.Where(m => m.SentAt < before.Value);
        }

        var history = query
            .OrderByDescending(m => m.SentAt)
            .Take(limit)
            .Reverse()
            .ToList();

        return Task.FromResult<IReadOnlyList<ChatMessage>>(history);
    }

    public Task MarkAsReadAsync(
        Guid roomId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (_messages.TryGetValue(roomId, out var messages))
        {
            foreach (var message in messages.Where(m => m.SenderId != userId && !m.IsRead))
            {
                message.IsRead = true;
            }

            _logger.LogInformation("Marked messages as read in room {RoomId} for user {UserId}", roomId, userId);
        }

        return Task.CompletedTask;
    }

    public Task<UserPresence> UpdatePresenceAsync(
        Guid userId,
        PresenceStatus status,
        string? customStatus = null,
        CancellationToken cancellationToken = default)
    {
        var presence = new UserPresence
        {
            UserId = userId,
            Status = status,
            CustomStatus = customStatus,
            LastSeen = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };

        _presence[userId] = presence;
        _logger.LogInformation("Updated presence for user {UserId}: {Status}", userId, status);

        return Task.FromResult(presence);
    }

    public Task<UserPresence?> GetPresenceAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        _presence.TryGetValue(userId, out var presence);
        return Task.FromResult(presence);
    }

    public Task StartTypingAsync(
        Guid roomId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (!_typingIndicators.ContainsKey(roomId))
        {
            _typingIndicators[roomId] = new List<TypingIndicator>();
        }

        var indicator = new TypingIndicator
        {
            RoomId = roomId,
            UserId = userId,
            StartedAt = DateTime.UtcNow
        };

        _typingIndicators[roomId].RemoveAll(t => t.UserId == userId);
        _typingIndicators[roomId].Add(indicator);

        return Task.CompletedTask;
    }

    public Task StopTypingAsync(
        Guid roomId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (_typingIndicators.TryGetValue(roomId, out var indicators))
        {
            indicators.RemoveAll(t => t.UserId == userId);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TypingIndicator>> GetTypingUsersAsync(
        Guid roomId,
        CancellationToken cancellationToken = default)
    {
        if (!_typingIndicators.TryGetValue(roomId, out var indicators))
        {
            return Task.FromResult<IReadOnlyList<TypingIndicator>>(Array.Empty<TypingIndicator>());
        }

        var activeIndicators = indicators
            .Where(t => (DateTime.UtcNow - t.StartedAt).TotalSeconds < 5)
            .ToList();

        return Task.FromResult<IReadOnlyList<TypingIndicator>>(activeIndicators);
    }

    public async Task<FileShareResult> ShareFileAsync(
        Guid roomId,
        Guid userId,
        Stream fileContent,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sharing file {FileName} in room {RoomId}", fileName, roomId);

        await Task.Delay(100, cancellationToken);

        var result = new FileShareResult
        {
            Url = $"https://cdn.example.com/chat-files/{Guid.NewGuid()}/{fileName}",
            FileName = fileName,
            FileSize = fileContent.Length,
            ContentType = contentType
        };

        await SendMessageAsync(roomId, userId, $"Shared file: {fileName}", MessageType.File,
            new List<string> { result.Url }, cancellationToken);

        return result;
    }

    public Task AddParticipantAsync(
        Guid roomId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (_rooms.TryGetValue(roomId, out var room))
        {
            if (!room.ParticipantIds.Contains(userId))
            {
                room.ParticipantIds.Add(userId);
                _logger.LogInformation("Added participant {UserId} to room {RoomId}", userId, roomId);
            }
        }

        return Task.CompletedTask;
    }

    public Task RemoveParticipantAsync(
        Guid roomId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (_rooms.TryGetValue(roomId, out var room))
        {
            room.ParticipantIds.Remove(userId);
            _logger.LogInformation("Removed participant {UserId} from room {RoomId}", userId, roomId);
        }

        return Task.CompletedTask;
    }

    public Task CloseRoomAsync(
        Guid roomId,
        CancellationToken cancellationToken = default)
    {
        if (_rooms.TryGetValue(roomId, out var room))
        {
            room.ClosedAt = DateTime.UtcNow;
            _logger.LogInformation("Closed chat room {RoomId}", roomId);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ChatRoom>> GetUserRoomsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var rooms = _rooms.Values
            .Where(r => r.ParticipantIds.Contains(userId) && r.ClosedAt == null)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<ChatRoom>>(rooms);
    }
}
