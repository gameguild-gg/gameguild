using FluentValidation;

namespace GameGuild.Identity.Users;

/// <summary>
///     Validator for UpdateUserRequest
/// </summary>
public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.PhoneNumber).MaximumLength(20).WithMessage("Phone number must not exceed 20 characters.").When(x => !string.IsNullOrEmpty(x.PhoneNumber));
    }
}
