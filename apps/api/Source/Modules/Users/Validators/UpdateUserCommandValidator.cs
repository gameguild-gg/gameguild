using FluentValidation;
using GameGuild.Database;

namespace GameGuild.Modules.Users;

/// <summary> FluentValidation validator for UpdateUserCommand </summary>
public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    private readonly ApplicationDbContext _context;

    public UpdateUserCommandValidator(ApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required").MustAsync(UserExists).WithMessage("User not found");

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
            .EmailAddress()
            .WithMessage("Invalid email format")
            .Length(1, 255)
            .WithMessage("Email must be between 1 and 255 characters")
            .MustAsync(BeUniqueEmailForUpdate)
            .WithMessage("Email address is already in use")
            .When(x => !string.IsNullOrEmpty(x.Email));
    }

    private async Task<bool> UserExists(Guid userId, CancellationToken cancellationToken) { return await _context.Users.AnyAsync(x => x.Id == userId && x.DeletedAt == null, cancellationToken); }

    private async Task<bool> BeUniqueEmailForUpdate(UpdateUserCommand command, string email, CancellationToken cancellationToken)
    {
        return !await _context.Users.AnyAsync(x => x.Email == email && x.Id != command.UserId && x.DeletedAt == null, cancellationToken);
    }
}
