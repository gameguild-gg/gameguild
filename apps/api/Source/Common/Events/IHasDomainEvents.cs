namespace GameGuild.Common;

/// <summary>
/// Interface for entities that can raise domain events
/// </summary>
public interface IHasDomainEvents {
  IReadOnlyList<IDomainEvent> DomainEvents { get; }

  void ClearDomainEvents();
}
