using GameGuild.CQRS;
﻿using GameGuild;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Command to hard delete a tenant permanently
/// </summary>
public class HardDeleteTenantCommand(Guid id) : ICommand<Result<bool>> {
  public Guid Id { get; init; } = id;
}
