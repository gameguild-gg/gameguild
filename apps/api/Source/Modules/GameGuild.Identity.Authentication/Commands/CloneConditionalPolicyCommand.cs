using GameGuild.Identity.Authorization;
using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record CloneConditionalPolicyCommand : ICommand<ConditionalPolicy>
{
    public Guid SourcePolicyId { get; set; }

    public string NewName { get; init; } = string.Empty;

    public string? NewDescription { get; init; }

    public Guid? NewTenantId { get; init; }
}
