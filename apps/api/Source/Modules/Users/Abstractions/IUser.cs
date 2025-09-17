namespace GameGuild.Modules.Users;

/// <summary> Interface representing a user in the system </summary>
public interface IUser {
  Guid Id { get; }

  string Name { get; }

  string Username { get; }

  string Email { get; }
}
