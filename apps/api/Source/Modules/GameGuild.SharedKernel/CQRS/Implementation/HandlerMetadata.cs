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
    ///     A cached delegate wrapping <c>MethodInfo.Invoke</c>.
    ///     This is <b>not</b> a truly compiled expression tree — it caches the reflection
    ///     call inside a delegate to avoid repeated method lookup while keeping O(1) dispatch.
    /// </summary>
    public Func<object, object[], Task<object?>>? CachedInvoker { get; init; }
}
