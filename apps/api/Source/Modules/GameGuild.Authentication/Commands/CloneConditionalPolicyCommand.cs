using GameGuild.Authorization;
using GameGuild.CQRS;

namespace GameGuild.Authentication;

public record CloneConditionalPolicyCommand : ICommand<ConditionalPolicy>
{
    public Guid SourcePolicyId { get; set; }

    public string NewName { get; init; } = string.Empty;

    public string? NewDescription { get; init; }

    public Guid? NewTenantId { get; init; }
}
