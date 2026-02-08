using FluentValidation;
using GameGuild.CQRS;

namespace GameGuild.Identity.Authorization.Commands;

// ============================================================================
// Delegated Administration Commands
// ============================================================================

/// <summary>
///     Command to grant delegated admin scope
/// </summary>
public record GrantDelegatedAdminCommand(
    Guid AdminUserId,
    Guid? TenantId,
    string Name,
    string Description,
    string[] ManagedResourceTypes,
    Guid[] ManagedUserIds,
    string[] AllowedOperations,
    Guid? OrganizationalUnitId = null
) : ICommand<DelegatedAdminScope>;

public class GrantDelegatedAdminValidator : AbstractValidator<GrantDelegatedAdminCommand>
{
    public GrantDelegatedAdminValidator()
    {
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.ManagedResourceTypes)
            .NotEmpty()
            .When(x => x.ManagedUserIds == null || x.ManagedUserIds.Length == 0)
            .WithMessage("Either managed resource types or managed user IDs must be provided");
        RuleFor(x => x.AllowedOperations).NotEmpty();
    }
}

public class GrantDelegatedAdminHandler(IDelegatedAdminService service)
    : ICommandHandler<GrantDelegatedAdminCommand, DelegatedAdminScope>
{
    public async Task<DelegatedAdminScope> Handle(
        GrantDelegatedAdminCommand request,
        CancellationToken cancellationToken
    )
    {
        var scope = new DelegatedAdminScope
        {
            AdminUserId = request.AdminUserId,
            TenantId = request.TenantId,
            Name = request.Name,
            Description = request.Description,
            AllowedResourceTypes = System.Text.Json.JsonSerializer.Serialize(request.ManagedResourceTypes),
            AllowedUserIds = System.Text.Json.JsonSerializer.Serialize(request.ManagedUserIds),
            GrantablePermissions = System.Text.Json.JsonSerializer.Serialize(request.AllowedOperations),
            AllowedDepartments = request.OrganizationalUnitId?.ToString(),
            IsActive = true
        };

        return await service.GrantDelegatedAdminAsync(scope, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
///     Command to revoke delegated admin scope
/// </summary>
public record RevokeDelegatedAdminCommand(Guid ScopeId) : ICommand<bool>;

public class RevokeDelegatedAdminValidator : AbstractValidator<RevokeDelegatedAdminCommand>
{
    public RevokeDelegatedAdminValidator()
    {
        RuleFor(x => x.ScopeId).NotEmpty();
    }
}

public class RevokeDelegatedAdminHandler(IDelegatedAdminService service)
    : ICommandHandler<RevokeDelegatedAdminCommand, bool>
{
    public async Task<bool> Handle(
        RevokeDelegatedAdminCommand request,
        CancellationToken cancellationToken
    )
    {
        return await service.RevokeDelegatedAdminAsync(request.ScopeId, cancellationToken).ConfigureAwait(false);
    }
}
