using FluentValidation;

namespace GameGuild.Identity.Tenants;

public class BulkCreateTenantsCommandValidator : AbstractValidator<BulkCreateTenantsCommand>
{
    public BulkCreateTenantsCommandValidator()
    {
        RuleFor(x => x.Tenants).NotEmpty().WithMessage("At least one tenant is required");
        RuleFor(x => x.Tenants).Must(t => t.Count() <= 100).WithMessage("Cannot create more than 100 tenants at once");
        RuleForEach(x => x.Tenants).ChildRules(item =>
        {
            item.RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            item.RuleFor(x => x.Slug).NotEmpty().MaximumLength(255).Matches(@"^[a-z0-9-]+$");
            item.RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress().MaximumLength(255);
            item.RuleFor(x => x.Description).MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Description));
        });
    }
}
