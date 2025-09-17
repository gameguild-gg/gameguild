using System.Security.Claims;
using System.Transactions;
using GameGuild.CQRS;


namespace GameGuild.Modules.Users;

/// <summary> Enhanced command to create a new user using Result<T> pattern for better error handling </summary>
public class CreateUserResultCommand : IResultCommand<User>, IAuthorizedRequest, ITransactionalRequest {
  [Required] [StringLength(100, MinimumLength = 1)] public string Name { get; init; } = string.Empty;

  [Required] [EmailAddress] [StringLength(255)] public string Email { get; init; } = string.Empty;

  public bool IsActive { get; init; } = true;

  [Range(0, double.MaxValue)] public decimal InitialBalance { get; init; }

  // Authorization implementation
  public string[ ]? RequiredRoles { get; } = ["Admin", "UserManager"];

  public string[ ]? RequiredPermissions { get; } = ["users.create"];

  public Task<bool> IsAuthorizedAsync(ClaimsPrincipal? user, CancellationToken cancellationToken) {
    // Custom authorization logic - example: check if user can create users in their tenant
    return Task.FromResult(true); // Simplified for example
  }

  // Transaction settings
  public IsolationLevel? IsolationLevel { get => System.Transactions.IsolationLevel.ReadCommitted; }
}
