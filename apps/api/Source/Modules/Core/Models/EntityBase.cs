// Re-export EntityBase from the core entities namespace for backward compatibility

namespace GameGuild.Core.Entities;

/// <summary>
/// Compatibility alias for EntityBase
/// </summary>
/// <typeparam name="TKey">The type of the entity's identifier</typeparam>
public abstract class EntityBase<TKey> : GameGuild.EntityBase<TKey> where TKey : IEquatable<TKey> {
    protected EntityBase() : base() { }
    protected EntityBase(object partial) : base(partial) { }
}

/// <summary>
/// Compatibility alias for EntityBase with Guid key
/// </summary>
public abstract class EntityBase : GameGuild.EntityBase<Guid> {
    protected EntityBase() : base() { }
    protected EntityBase(object partial) : base(partial) { }
}