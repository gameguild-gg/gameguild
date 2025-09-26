using GameGuild.CQRS;

namespace GameGuild.Modules.Tenants;

/// <summary> Command to update an existing tenant </summary>
public class UpdateTenantCommand(Guid id, string? name = null, string? description = null, bool? isActive = null, string? slug = null) : ICommand<Result<Tenant>>
{
    public Guid Id { get; init; } = id;

    public string? Name { get; init; } = name;

    public string? Description { get; init; } = description;

    public bool? IsActive { get; init; } = isActive;

    public string? Slug { get; init; } = slug;
}
