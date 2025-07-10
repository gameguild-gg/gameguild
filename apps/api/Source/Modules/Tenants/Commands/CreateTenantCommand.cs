using GameGuild.Common;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Command to create a new tenant
/// </summary>
public class CreateTenantCommand(string name, string? description = null, bool isActive = true, string? slug = null)
  : ICommand<Common.Result<Tenant>> {
  public string Name { get; init; } = name;

  public string? Description { get; init; } = description;

  public bool IsActive { get; init; } = isActive;

  public string Slug { get; init; } = slug ?? name.ToLowerInvariant().Replace(" ", "-");
}
