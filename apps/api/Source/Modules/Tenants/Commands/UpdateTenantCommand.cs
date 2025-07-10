using GameGuild.Common;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Command to update an existing tenant
/// </summary>
public class UpdateTenantCommand : ICommand<Common.Result<Tenant>>
{
  public Guid Id { get; init; }
  public string Name { get; init; } = string.Empty;
  public string? Description { get; init; }
  public bool IsActive { get; init; }
  public string Slug { get; init; } = string.Empty;

  public UpdateTenantCommand(Guid id, string name, string? description = null, bool isActive = true, string? slug = null)
  {
    Id = id;
    Name = name;
    Description = description;
    IsActive = isActive;
    Slug = slug ?? name.ToLowerInvariant().Replace(" ", "-");
  }
}
