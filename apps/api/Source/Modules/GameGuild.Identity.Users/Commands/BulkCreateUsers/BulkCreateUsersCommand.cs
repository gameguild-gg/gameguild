using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command to create multiple users in bulk.
///     Quota is calculated dynamically based on the number of users.
/// </summary>
/// <param name="Users">Collection of user creation data</param>
/// <remarks>
///     Note: Since bulk operations have dynamic amounts, quota checking is handled
///     in the handler using IResourceQuotaService.CheckLimitsAsync() directly.
/// </remarks>
public sealed record BulkCreateUsersCommand(IEnumerable<CreateUserRequestItem> Users) : ICommand<BulkCreateUsersResponse>;
