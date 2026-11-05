using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

/// <summary>Server-owned delivery request; never returned in a public flow response.</summary>
public sealed record OrchestratedRiskCodeRequestedNotification(Guid UserId, string Email, string Code,
    int ExpiresInSeconds, DateTimeOffset ExpiresAt) : INotification;
