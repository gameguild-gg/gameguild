using FluentValidation;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Credentials;

/// <summary> FluentValidation validator for HardDeleteCredentialCommand </summary>
public class HardDeleteCredentialCommandValidator : AbstractValidator<HardDeleteCredentialCommand>
{
    private readonly ApplicationDbContext _context;

    public HardDeleteCredentialCommandValidator(ApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Credential ID is required")
            .MustAsync(CredentialExists)
            .WithMessage("Credential not found");
    }

    private async Task<bool> CredentialExists(Guid credentialId, CancellationToken cancellationToken)
    {
        return await _context.Credentials.IgnoreQueryFilters().AnyAsync(credential => credential.Id == credentialId, cancellationToken);
    }
}
