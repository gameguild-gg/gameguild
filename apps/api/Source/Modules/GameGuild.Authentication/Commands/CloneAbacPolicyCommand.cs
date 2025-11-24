using GameGuild.Authentication.Entities;
using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

public record CloneAbacPolicyCommand : ICommand<AbacPolicy>
{
    public Guid SourcePolicyId { get; set; }

    public string NewName { get; init; } = string.Empty;

    public string? NewDescription { get; init; }

    public Guid? NewTenantId { get; init; }
}
