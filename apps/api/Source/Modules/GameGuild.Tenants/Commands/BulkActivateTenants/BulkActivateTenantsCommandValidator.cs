using GameGuild.SharedKernel.Validators;

namespace GameGuild.Tenants.Commands;

/// <summary>
///     Validator for BulkActivateTenantsCommand
/// </summary>
public class BulkActivateTenantsCommandValidator : BulkOperationValidator<BulkActivateTenantsCommand>
{
    public BulkActivateTenantsCommandValidator() { AddCommonRules(); }

    protected override IEnumerable<Guid> GetTenantIds(BulkActivateTenantsCommand instance) { return instance.TenantIds; }
}
