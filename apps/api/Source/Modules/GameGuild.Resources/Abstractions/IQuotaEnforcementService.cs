namespace GameGuild.Resources;

/// <summary>
///     Sub-service handling quota limit checking, atomic consumption, and decrement.
///     Implements <see cref="IResourceQuotaEnforcer"/>.
/// </summary>
public interface IQuotaEnforcementService : IResourceQuotaEnforcer;
