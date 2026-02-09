using System.Reflection;

namespace GameGuild.CQRS.Implementation;

/// <summary>
///     Cached handler metadata for optimized dispatch.
///     Stores the handler type, the Handle method reference, and a cached delegate
///     that wraps reflection-based invocation for O(1) lookup after first resolution.
/// </summary>
internal sealed class HandlerMetadata
{
    public Type HandlerType { get; init; } = null!;

    public MethodInfo HandleMethod { get; init; } = null!;

    /// <summary>
    ///     A cached delegate compiled from an expression tree for fast handler dispatch.
    ///     The expression tree is compiled once per handler method and cached, providing
    ///     ~100× faster invocation compared to <c>MethodInfo.Invoke</c> after the
    ///     one-time compilation cost.
    /// </summary>
    public Func<object, object[], Task<object?>>? CachedInvoker { get; init; }
}
