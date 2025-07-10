using GameGuild.Common;

namespace GameGuild.Modules.Users.Events;

/// <summary>
/// Event raised when a user is created
/// </summary>
public sealed class UserCreatedEvent : DomainEventBase
{
    public UserCreatedEvent(Guid userId, string email, string name, DateTime createdAt)
        : base(userId, nameof(User))
    {
        UserId = userId;
        Email = email;
        Name = name;
        CreatedAt = createdAt;
    }

    public Guid UserId { get; }
    public string Email { get; }
    public string Name { get; }
    public DateTime CreatedAt { get; }
}

/// <summary>
/// Event raised when a user is updated
/// </summary>
public sealed class UserUpdatedEvent : DomainEventBase
{
    public UserUpdatedEvent(Guid userId, Dictionary<string, object> changes)
        : base(userId, nameof(User))
    {
        UserId = userId;
        Changes = changes;
    }

    public Guid UserId { get; }
    public Dictionary<string, object> Changes { get; }
}

/// <summary>
/// Event raised when a user is deleted
/// </summary>
public sealed class UserDeletedEvent : DomainEventBase
{
    public UserDeletedEvent(Guid userId, bool isSoftDelete)
        : base(userId, nameof(User))
    {
        UserId = userId;
        IsSoftDelete = isSoftDelete;
    }

    public Guid UserId { get; }
    public bool IsSoftDelete { get; }
}

/// <summary>
/// Event raised when a user is restored
/// </summary>
public sealed class UserRestoredEvent : DomainEventBase
{
    public UserRestoredEvent(Guid userId)
        : base(userId, nameof(User))
    {
        UserId = userId;
    }

    public Guid UserId { get; }
}

/// <summary>
/// Event raised when a user's balance is updated
/// </summary>
public sealed class UserBalanceUpdatedEvent : DomainEventBase
{
    public UserBalanceUpdatedEvent(Guid userId, decimal previousBalance, decimal newBalance, 
        decimal previousAvailableBalance, decimal newAvailableBalance, string? reason)
        : base(userId, nameof(User))
    {
        UserId = userId;
        PreviousBalance = previousBalance;
        NewBalance = newBalance;
        PreviousAvailableBalance = previousAvailableBalance;
        NewAvailableBalance = newAvailableBalance;
        Reason = reason;
    }

    public Guid UserId { get; }
    public decimal PreviousBalance { get; }
    public decimal NewBalance { get; }
    public decimal PreviousAvailableBalance { get; }
    public decimal NewAvailableBalance { get; }
    public string? Reason { get; }
}

/// <summary>
/// Event raised when a user is activated
/// </summary>
public sealed class UserActivatedEvent : DomainEventBase
{
    public UserActivatedEvent(Guid userId)
        : base(userId, nameof(User))
    {
        UserId = userId;
    }

    public Guid UserId { get; }
}

/// <summary>
/// Event raised when a user is deactivated
/// </summary>
public sealed class UserDeactivatedEvent : DomainEventBase
{
    public UserDeactivatedEvent(Guid userId)
        : base(userId, nameof(User))
    {
        UserId = userId;
    }

    public Guid UserId { get; }
}
