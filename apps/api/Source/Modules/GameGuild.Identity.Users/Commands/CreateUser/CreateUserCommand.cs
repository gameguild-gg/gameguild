using GameGuild.CQRS;
using GameGuild.Resources;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command to create a new user
/// </summary>
/// <param name="Email">User's email address</param>
/// <param name="Name">User's full name</param>
/// <param name="PhoneNumber">Optional phone number</param>
[RequiresQuota(ResourceUsageType.Users, Source = "CreateUser")]
public sealed record CreateUserCommand(string Email, string Name, string? PhoneNumber = null) : ICommand<UserDto>;
