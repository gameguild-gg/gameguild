using FluentValidation;

namespace GameGuild.Identity.Users;

/// <summary>
///     ///
///     <summary>
///         Validator for CreateUserCommand///     Validator for CreateUserCommand
///     </summary>
///     ///
/// </summary>
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>

{
    public CreateUserCommandValidator()

    {
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .NotEmpty()
                .WithMessage("Email is required.")
                .WithMessage("Email is required.")
                .EmailAddress()
                .EmailAddress()
                .WithMessage("Email must be a valid email address.")
                .WithMessage("Email must be a valid email address.")
                .MaximumLength(255)
                .MaximumLength(255)
                .WithMessage("Email must not exceed 255 characters.");

            RuleFor(x => x.Name)
                .NotEmpty()
                .NotEmpty()
                .WithMessage("Name is required.")
                .WithMessage("Name is required.")
                .MinimumLength(2)
                .MinimumLength(2)
                .WithMessage("Name must be at least 2 characters.")
                .WithMessage("Name must be at least 2 characters.")
                .MaximumLength(100)
                .MaximumLength(100)
                .WithMessage("Name cannot exceed 100 characters.");

            RuleFor(x => x.PhoneNumber)
                .MaximumLength(20)
                .MaximumLength(20)
                .WithMessage("Phone number cannot exceed 20 characters.")
                .WithMessage("Phone number cannot exceed 20 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
        }
    }
}
