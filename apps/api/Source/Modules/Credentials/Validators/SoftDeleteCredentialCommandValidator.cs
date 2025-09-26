using FluentValidation;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Credentials;

/// <summary> FluentValidation validator for SoftDeleteCredentialCommand </summary>
public class SoftDeleteCredentialCommandValidator : AbstractValidator<SoftDeleteCredentialCommand>
{
    private readonly ApplicationDbContext _context;

    public SoftDeleteCredentialCommandValidator(ApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Credential ID is required")
            .MustAsync(CredentialExists)
            .WithMessage("Credential not found")
            .MustAsync(IsNotSoftDeleted)
            .WithMessage("Credential is already soft-deleted");
    }

    private async Task<bool> CredentialExists(Guid credentialId, CancellationToken cancellationToken)
    {
        return await _context.Credentials.IgnoreQueryFilters().AnyAsync(credential => credential.Id == credentialId, cancellationToken);
    }

    private async Task<bool> IsNotSoftDeleted(Guid credentialId, CancellationToken cancellationToken)
    {
        var credential = await _context.Credentials.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == credentialId, cancellationToken);

        return credential is { DeletedAt: null };
    }
}
