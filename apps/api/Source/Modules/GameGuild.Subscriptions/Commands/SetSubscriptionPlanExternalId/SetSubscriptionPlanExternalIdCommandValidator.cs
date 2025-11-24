using FluentValidation;

namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Validator for SetSubscriptionPlanExternalIdCommand
/// </summary>
public class SetSubscriptionPlanExternalIdCommandValidator : AbstractValidator<SetSubscriptionPlanExternalIdCommand>
{
    public SetSubscriptionPlanExternalIdCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required");

        RuleFor(x => x.ExternalId).NotEmpty().WithMessage("ExternalId is required").MaximumLength(100).WithMessage("ExternalId cannot exceed 100 characters");
    }
}
