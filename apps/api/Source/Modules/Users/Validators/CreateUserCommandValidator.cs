using FluentValidation;

namespace GameGuild.Modules.Users;

/// <summary> FluentValidation validator for CreateUserCommand </summary>
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    private readonly IUserService _userService;

    public CreateUserCommandValidator(IUserService userService)
    {
        _userService = userService;

        RuleFor(x => x.GivenName)
            .Length(1, 100)
            .WithMessage("Given name must be between 1 and 100 characters")
            .Matches(@"^[a-zA-Z\s\-'\.]+$")
            .WithMessage("Given name can only contain letters, spaces, hyphens, apostrophes, and periods")
            .When(x => !string.IsNullOrEmpty(x.GivenName));

        RuleFor(x => x.FamilyName)
            .Length(1, 100)
            .WithMessage("Family name must be between 1 and 100 characters")
            .Matches(@"^[a-zA-Z\s\-'\.]+$")
            .WithMessage("Family name can only contain letters, spaces, hyphens, apostrophes, and periods")
            .When(x => !string.IsNullOrEmpty(x.FamilyName));

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email format")
            .Length(1, 255)
            .WithMessage("Email must be between 1 and 255 characters")
            .MustAsync(BeUniqueEmail)
            .WithMessage("Email address is already in use");
    }

    private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
    {
        User? existingUser = await _userService.GetByEmailAsync(email);

        return existingUser == null;
    }
}
