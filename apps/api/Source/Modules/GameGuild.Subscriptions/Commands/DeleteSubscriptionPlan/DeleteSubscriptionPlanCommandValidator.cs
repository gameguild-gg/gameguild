using FluentValidation;

namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Validator for DeleteSubscriptionPlanCommand
/// </summary>
public class DeleteSubscriptionPlanCommandValidator : AbstractValidator<DeleteSubscriptionPlanCommand>
{
    public DeleteSubscriptionPlanCommandValidator() { RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required"); }
}
