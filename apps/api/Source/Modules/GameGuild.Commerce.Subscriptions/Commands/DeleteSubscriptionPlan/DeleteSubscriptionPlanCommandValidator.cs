using FluentValidation;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Validator for DeleteSubscriptionPlanCommand
/// </summary>
public sealed class DeleteSubscriptionPlanCommandValidator : AbstractValidator<DeleteSubscriptionPlanCommand>
{
    public DeleteSubscriptionPlanCommandValidator() { RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required"); }
}
