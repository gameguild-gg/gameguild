using FluentValidation;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Credentials;

/// <summary> FluentValidation validator for UpdateCredentialCommand </summary>
public class UpdateCredentialCommandValidator : AbstractValidator<UpdateCredentialCommand>
{
    private readonly ApplicationDbContext _context;

    public UpdateCredentialCommandValidator(ApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Credential ID is required")
            .MustAsync(CredentialExists)
            .WithMessage("Credential not found");

        RuleFor(x => x.Type)
            .NotEmpty()
            .WithMessage("Credential type is required")
            .MaximumLength(50)
            .WithMessage("Credential type must be 50 characters or fewer");

        RuleFor(x => x.Value)
            .NotEmpty()
            .WithMessage("Credential value is required")
            .MaximumLength(1000)
            .WithMessage("Credential value must be 1000 characters or fewer");

        RuleFor(x => x.Metadata)
            .MaximumLength(2000)
            .WithMessage("Metadata must be 2000 characters or fewer");

        RuleFor(x => x.ExpiresAt)
            .Must(BeInTheFuture)
            .WithMessage("Expiration date must be in the future");

        RuleFor(x => x)
            .MustAsync(HaveUniqueTypeForUser)
            .WithMessage("A credential with this type already exists for the user");
    }

    private async Task<bool> CredentialExists(Guid credentialId, CancellationToken cancellationToken)
    {
        return await _context.Credentials.AnyAsync(credential => credential.Id == credentialId && credential.DeletedAt == null, cancellationToken);
    }

    private static bool BeInTheFuture(DateTime? expiresAt)
    {
        return !expiresAt.HasValue || expiresAt.Value > DateTime.UtcNow;
    }

    private async Task<bool> HaveUniqueTypeForUser(UpdateCredentialCommand command, CancellationToken cancellationToken)
    {
        var credential = await _context.Credentials.AsNoTracking().FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

        if (credential == null) return false;

        if (string.Equals(credential.Type, command.Type, StringComparison.Ordinal)) return true;

        return !await _context.Credentials.AnyAsync(
            c => c.UserId == credential.UserId &&
                 c.Id != command.Id &&
                 c.Type == command.Type &&
                 c.DeletedAt == null,
            cancellationToken);
    }
}
