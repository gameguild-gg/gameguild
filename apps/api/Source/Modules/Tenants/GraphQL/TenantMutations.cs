using GameGuild.Common;
using GameGuild.Modules.Permissions.Models;
using GameGuild.Modules.Tenants.Inputs;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// GraphQL mutations for Tenant management
/// </summary>
[ExtendObjectType<Mutation>]
public class TenantMutations {
  /// <summary>
  /// Create a new tenant with proper permission checks
  /// </summary>
  [RequireTenantPermission(PermissionType.Create)]
  public async Task<Tenant> CreateTenant(
    CreateTenantInput input,
    [Service] ICommandHandler<CreateTenantCommand, Common.Result<Tenant>> createTenantHandler
  ) {
    var command = new CreateTenantCommand(input.Name, input.Description, input.IsActive, null);
    var result = await createTenantHandler.Handle(command, CancellationToken.None);

    if (!result.IsSuccess) {
      throw new GraphQLException("Failed to create tenant");
    }

    return result.Value;
  }
}