using FluentValidation;

namespace GameGuild.Modules.Users;

/// <summary> FluentValidation validator for CreateUserCommand </summary>
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    private readonly IUserService _userService;

    public CreateUserCommandValidator(IUserService userService)
    {
        _userService = userService;

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .Length(1, 100)
            .WithMessage("Name must be between 1 and 100 characters")
            .Matches(@"^[a-zA-Z\s\-'\.]+$")
            .WithMessage("Name can only contain letters, spaces, hyphens, apostrophes, and periods");

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
        var existingUser = await _userService.GetByEmailAsync(email);

        return existingUser == null;
    }
}
