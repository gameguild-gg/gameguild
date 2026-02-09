using FluentValidation;

namespace GameGuild.Identity.Users;

/// <summary>
///     Validator for UpdateUserRequestItem
/// </summary>
public sealed class UpdateUserRequestItemValidator : AbstractValidator<UpdateUserRequestItem>
{
    public UpdateUserRequestItemValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MinimumLength(2).WithMessage("Name must be at least 2 characters.").MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

        RuleFor(x => x.PhoneNumber).MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters.").When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
    }
}
