using MediatR;


namespace GameGuild.Modules.Users;

/// <summary>
/// Legacy notification sent when a user is created
/// </summary>
public class UserCreatedNotification : INotification
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Legacy notification sent when a user is updated
/// </summary>
public class UserUpdatedNotification : INotification
{
    public Guid UserId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Changes { get; set; } = new();
}

/// <summary>
/// Legacy notification sent when a user is deleted
/// </summary>
public class UserDeletedNotification : INotification
{
    public Guid UserId { get; set; }
    public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
    public bool SoftDelete { get; set; } = true;
}

/// <summary>
/// Legacy notification sent when a user is restored
/// </summary>
public class UserRestoredNotification : INotification
{
    public Guid UserId { get; set; }
    public DateTime RestoredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Legacy notification sent when a user's balance is updated
/// </summary>
public class UserBalanceUpdatedNotification : INotification
{
    public Guid UserId { get; set; }
    public decimal OldBalance { get; set; }
    public decimal NewBalance { get; set; }
    public decimal OldAvailableBalance { get; set; }
    public decimal NewAvailableBalance { get; set; }
    public string? Reason { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
