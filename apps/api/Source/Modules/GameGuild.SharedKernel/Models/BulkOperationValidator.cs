using FluentValidation;

namespace GameGuild.Models;

/// <summary>
///     Base validator for bulk operations
/// </summary>
public abstract class BulkOperationValidator<T> : AbstractValidator<T> where T : class
{
    protected void AddCommonRules()
    {
        RuleFor(x => GetTenantIds(x))
            .NotEmpty()
            .WithMessage("At least one tenant ID is required")
            .OverridePropertyName("TenantIds");

        RuleFor(x => GetTenantIds(x))
            .Must(ids => ids.Count() <= 100)
            .WithMessage("Cannot process more than 100 tenants at once")
            .OverridePropertyName("TenantIds");

        RuleForEach(x => GetTenantIds(x))
            .NotEmpty()
            .WithMessage("Tenant ID cannot be empty")
            .OverridePropertyName("TenantIds");
    }

    protected abstract IEnumerable<Guid> GetTenantIds(T instance);
}
