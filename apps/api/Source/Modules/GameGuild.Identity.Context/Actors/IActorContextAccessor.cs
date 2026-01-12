namespace GameGuild.Identity.Context.Actors;

/// <summary>
///     Provides access to the current <see cref="ActorContext"/> for the executing request or operation.
/// </summary>
/// <remarks>
///     <para>
///         This interface allows core business logic to access the current security context
///         without depending on ASP.NET Core's HttpContext or ClaimsPrincipal.
///     </para>
///     <para>
///         In HTTP request scenarios, middleware populates the ActorContext early in the pipeline.
///         In background job scenarios, the job infrastructure sets up the appropriate context.
///         In tests, you can set the context directly via SetActorContext.
///     </para>
/// </remarks>
public interface IActorContextAccessor
{
    /// <summary>
    ///     Gets the current actor context, or <see cref="ActorContext.Anonymous"/> if none is set.
    /// </summary>
    ActorContext ActorContext { get; }

    /// <summary>
    ///     Sets the actor context for the current async execution flow.
    /// </summary>
    /// <param name="context">The actor context to set.</param>
    /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
    void SetActorContext(ActorContext context);

    /// <summary>
    ///     Clears the current actor context, reverting to anonymous.
    /// </summary>
    void ClearActorContext();
}
