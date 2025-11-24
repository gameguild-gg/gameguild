using FluentValidation;

namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Validator for UpdateSubscriptionMetadataCommand
/// </summary>
public class UpdateSubscriptionMetadataCommandValidator : AbstractValidator<UpdateSubscriptionMetadataCommand>
{
    public UpdateSubscriptionMetadataCommandValidator()
    {
        RuleFor(x => x.SubscriptionId).NotEmpty().WithMessage("SubscriptionId is required");

        RuleFor(x => x.Metadata).NotEmpty().WithMessage("Metadata is required").MaximumLength(2000).WithMessage("Metadata cannot exceed 2000 characters");
    }
}
