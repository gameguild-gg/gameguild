using GameGuild.Identity.Authorization;
﻿using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record EvaluateConditionalPoliciesCommand : ICommand<ConditionalPolicyResult>
{
    public Guid UserId { get; init; }

    public Guid TenantId { get; init; }

    public PermissionType? PermissionType { get; init; }

    public string? ResourceType { get; init; }

    public Dictionary<string, object> Context { get; init; } = new Dictionary<string, object>();
}
