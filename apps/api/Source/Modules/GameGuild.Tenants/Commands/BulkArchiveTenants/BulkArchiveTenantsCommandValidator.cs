using GameGuild.SharedKernel.Validators;

namespace GameGuild.Tenants.Commands;

/// <summary>
///     Validator for BulkArchiveTenantsCommand
/// </summary>
public class BulkArchiveTenantsCommandValidator : BulkOperationValidator<BulkArchiveTenantsCommand>
{
    public BulkArchiveTenantsCommandValidator() { AddCommonRules(); }

    protected override IEnumerable<Guid> GetTenantIds(BulkArchiveTenantsCommand instance) { return instance.TenantIds; }
}
