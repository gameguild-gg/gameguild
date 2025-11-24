using GameGuild.CQRS;
using GameGuild.Resources.Attributes;
using GameGuild.Resources.Models;
using GameGuild.Users.Models;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command to create a new user
/// </summary>
/// <param name="Email">User's email address</param>
/// <param name="Name">User's full name</param>
/// <param name="PhoneNumber">Optional phone number</param>
[RequiresQuota(ResourceUsageType.Users, 1, Source = "CreateUser")]
public record CreateUserCommand(string Email, string Name, string? PhoneNumber = null) : ICommand<UserDto>;
