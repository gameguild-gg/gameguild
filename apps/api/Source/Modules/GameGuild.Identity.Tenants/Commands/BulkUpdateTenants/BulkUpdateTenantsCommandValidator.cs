using FluentValidation;

namespace GameGuild.Identity.Tenants;

public sealed class BulkUpdateTenantsCommandValidator : AbstractValidator<BulkUpdateTenantsCommand>
{
    public BulkUpdateTenantsCommandValidator()
    {
        RuleFor(x => x.Updates).NotEmpty().WithMessage("At least one update is required");
        RuleFor(x => x.Updates).Must(u => u.Count() <= 100).WithMessage("Cannot update more than 100 tenants at once");
        RuleForEach(x => x.Updates).ChildRules(item =>
        {
            item.RuleFor(x => x.TenantId).NotEmpty();
            item.RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            item.RuleFor(x => x.Description).MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Description));
        });
    }
}
