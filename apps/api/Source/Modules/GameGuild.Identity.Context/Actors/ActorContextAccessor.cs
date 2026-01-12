namespace GameGuild.Identity.Context.Actors;

/// <summary>
///     Default implementation of <see cref="IActorContextAccessor"/> using <see cref="AsyncLocal{T}"/>.
/// </summary>
/// <remarks>
///     <para>
///         Uses AsyncLocal to maintain the actor context across async/await boundaries.
///         This ensures the context flows correctly through the entire request processing pipeline.
///     </para>
///     <para>
///         This implementation has no dependencies on ASP.NET Core and can be used in any
///         .NET application (console apps, background workers, tests, etc.).
///     </para>
/// </remarks>
public sealed class ActorContextAccessor : IActorContextAccessor
{
    private static readonly AsyncLocal<ActorContextHolder> ActorContextCurrent = new();

    /// <inheritdoc />
    public ActorContext ActorContext
    {
        get => ActorContextCurrent.Value?.Context ?? ActorContext.Anonymous;
    }

    /// <inheritdoc />
    public void SetActorContext(ActorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Use a holder object to ensure the value flows correctly across async boundaries
        // and avoids issues with value types being copied
        var holder = ActorContextCurrent.Value;
        if (holder != null)
        {
            // Clear the old context to prevent leaking
            holder.Context = null;
        }

        ActorContextCurrent.Value = new ActorContextHolder { Context = context };
    }

    /// <inheritdoc />
    public void ClearActorContext()
    {
        var holder = ActorContextCurrent.Value;
        if (holder != null)
        {
            holder.Context = null;
        }
    }

    /// <summary>
    ///     Holder class to ensure proper AsyncLocal behavior with reference semantics.
    /// </summary>
    private sealed class ActorContextHolder
    {
        public ActorContext? Context { get; set; }
    }
}
