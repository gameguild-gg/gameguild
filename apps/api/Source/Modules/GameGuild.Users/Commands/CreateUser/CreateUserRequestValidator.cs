using FluentValidation;

namespace GameGuild.Users.Commands;

/// <summary>
///     Validator for CreateUserRequest
/// </summary>
public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required.").EmailAddress().WithMessage("Email must be a valid email address.").MaximumLength(255).WithMessage("Email must not exceed 255 characters.");

        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.PhoneNumber).MaximumLength(20).WithMessage("Phone number must not exceed 20 characters.").When(x => !string.IsNullOrEmpty(x.PhoneNumber));
    }
}
