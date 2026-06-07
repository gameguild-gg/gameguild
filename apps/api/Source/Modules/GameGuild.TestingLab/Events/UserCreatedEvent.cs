using GameGuild.CQRS;

namespace GameGuild.TestingLab;

public sealed class UserCreatedEvent(Guid userId, string name) : DomainEvent {
  public Guid UserId { get; } = userId;
  public string Name { get; } = name;
}
