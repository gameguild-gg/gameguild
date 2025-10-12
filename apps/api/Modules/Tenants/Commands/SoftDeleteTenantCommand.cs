using GameGuild.CQRS;

namespace GameGuild.Modules.Tenants;

/// <summary> Command to soft delete a tenant </summary>
public class SoftDeleteTenantCommand(Guid id) : ICommand<Result<bool>>
{
    public Guid Id { get; init; } = id;
}
