using System.Reflection;

namespace GameGuild.CQRS;

/// <summary>
/// Handler metadata for optimized dispatch
/// </summary>
internal sealed class HandlerMetadata
{
    public Type HandlerType { get; init; } = null!;

    public MethodInfo HandleMethod { get; init; } = null!;

    public Func<object, object[], Task<object?>>? CompiledInvoker { get; init; }
}
