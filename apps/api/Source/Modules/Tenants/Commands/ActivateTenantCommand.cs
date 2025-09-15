using GameGuild.CQRS;
﻿using GameGuild;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Command to activate a tenant
/// </summary>
public class ActivateTenantCommand(Guid id) : ICommand<Result<bool>> {
  public Guid Id { get; init; } = id;
}
