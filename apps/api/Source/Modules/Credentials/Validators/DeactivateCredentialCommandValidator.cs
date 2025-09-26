using FluentValidation;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Credentials;

/// <summary> FluentValidation validator for DeactivateCredentialCommand </summary>
public class DeactivateCredentialCommandValidator : AbstractValidator<DeactivateCredentialCommand>
{
    private readonly ApplicationDbContext _context;

    public DeactivateCredentialCommandValidator(ApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.Id).NotEmpty().WithMessage("Credential ID is required").MustAsync(CredentialExists).WithMessage("Credential not found").MustAsync(CredentialIsActive).WithMessage("Credential is already inactive");
    }

    private async Task<bool> CredentialExists(Guid credentialId, CancellationToken cancellationToken)
    {
        return await _context.Credentials.AnyAsync(credential => credential.Id == credentialId && credential.DeletedAt == null, cancellationToken);
    }

    private async Task<bool> CredentialIsActive(Guid credentialId, CancellationToken cancellationToken)
    {
        var credential = await _context.Credentials.FirstOrDefaultAsync(c => c.Id == credentialId && c.DeletedAt == null, cancellationToken);

        return credential is { IsActive: true };
    }
}
