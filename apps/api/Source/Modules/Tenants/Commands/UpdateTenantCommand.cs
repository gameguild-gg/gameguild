using GameGuild.Common;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Command to update an existing tenant
/// </summary>
public class UpdateTenantCommand : ICommand<Common.Result<Tenant>> {
  public Guid Id { get; init; }

  public string? Name { get; init; }

  public string? Description { get; init; }

  public bool? IsActive { get; init; }

  public string? Slug { get; init; }

  public UpdateTenantCommand(Guid id, string? name = null, string? description = null, bool? isActive = null, string? slug = null) {
    Id = id;
    Name = name;
    Description = description;
    IsActive = isActive;
    Slug = slug;
  }
}
