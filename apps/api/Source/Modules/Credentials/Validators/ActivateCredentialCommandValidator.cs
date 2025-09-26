using FluentValidation;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Credentials;

/// <summary> FluentValidation validator for ActivateCredentialCommand </summary>
public class ActivateCredentialCommandValidator : AbstractValidator<ActivateCredentialCommand>
{
    private readonly ApplicationDbContext _context;

    public ActivateCredentialCommandValidator(ApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.Id).NotEmpty().WithMessage("Credential ID is required").MustAsync(CredentialExists).WithMessage("Credential not found").MustAsync(CredentialIsInactive).WithMessage("Credential is already active");
    }

    private async Task<bool> CredentialExists(Guid credentialId, CancellationToken cancellationToken)
    {
        return await _context.Credentials.AnyAsync(credential => credential.Id == credentialId && credential.DeletedAt == null, cancellationToken);
    }

    private async Task<bool> CredentialIsInactive(Guid credentialId, CancellationToken cancellationToken)
    {
        var credential = await _context.Credentials.FirstOrDefaultAsync(c => c.Id == credentialId && c.DeletedAt == null, cancellationToken);

        return credential is { IsActive: false };
    }
}
