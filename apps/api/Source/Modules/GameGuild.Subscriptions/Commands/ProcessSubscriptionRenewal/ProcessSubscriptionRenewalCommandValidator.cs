using FluentValidation;

namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Validator for ProcessSubscriptionRenewalCommand
/// </summary>
public class ProcessSubscriptionRenewalCommandValidator : AbstractValidator<ProcessSubscriptionRenewalCommand>
{
    public ProcessSubscriptionRenewalCommandValidator() { RuleFor(x => x.SubscriptionId).NotEmpty().WithMessage("SubscriptionId is required"); }
}
