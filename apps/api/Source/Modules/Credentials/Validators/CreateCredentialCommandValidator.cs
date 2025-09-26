using FluentValidation;
using GameGuild.Database;

namespace GameGuild.Modules.Credentials;

/// <summary> FluentValidation validator for CreateCredentialCommand </summary>
public class CreateCredentialCommandValidator : AbstractValidator<CreateCredentialCommand>
{
    private readonly ApplicationDbContext _context;

    public CreateCredentialCommandValidator(ApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required").MustAsync(UserExists).WithMessage("User not found");

        RuleFor(x => x.Type).NotEmpty().WithMessage("Credential type is required").MaximumLength(50).WithMessage("Credential type must be 50 characters or fewer");

        RuleFor(x => x.Value).NotEmpty().WithMessage("Credential value is required").MaximumLength(1000).WithMessage("Credential value must be 1000 characters or fewer");

        RuleFor(x => x.Metadata).MaximumLength(2000).WithMessage("Metadata must be 2000 characters or fewer");

        RuleFor(x => x.ExpiresAt).Must(BeInTheFuture).WithMessage("Expiration date must be in the future");

        RuleFor(x => x).MustAsync(BeUniqueCredentialForUser).WithMessage("A credential with this type already exists for the user");
    }

    private async Task<bool> UserExists(Guid userId, CancellationToken cancellationToken) { return await _context.Users.AnyAsync(user => user.Id == userId && user.DeletedAt == null, cancellationToken); }

    private static bool BeInTheFuture(DateTime? expiresAt) { return !expiresAt.HasValue || expiresAt.Value > DateTime.UtcNow; }

    private async Task<bool> BeUniqueCredentialForUser(CreateCredentialCommand command, CancellationToken cancellationToken)
    {
        return !await _context.Credentials.AnyAsync(credential => credential.UserId == command.UserId && credential.Type == command.Type && credential.DeletedAt == null, cancellationToken);
    }
}
