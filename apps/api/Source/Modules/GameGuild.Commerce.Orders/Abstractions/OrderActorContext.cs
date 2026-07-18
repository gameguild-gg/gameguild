using GameGuild.Identity.Context.Actors;

namespace GameGuild.Commerce.Orders;

internal readonly record struct OrderActorIdentity(Guid UserId, Guid TenantId);

internal static class OrderActorContext
{
    public static bool TryResolve(
        IActorContextAccessor actorContextAccessor,
        out OrderActorIdentity identity,
        out Error error)
    {
        var actor = actorContextAccessor.ActorContext;
        if (actor is null || !actor.IsAuthenticated || !actor.SubjectIdAsGuid.HasValue || !actor.TenantId.HasValue)
        {
            identity = default;
            error = Error.Unauthorized("Orders.Unauthenticated", "An authenticated tenant user context is required.");
            return false;
        }

        if (actor.ActorKind != ActorKind.User)
        {
            identity = default;
            error = Error.Unauthorized("Orders.UserActorRequired", "Orders require an authenticated user actor context.");
            return false;
        }

        identity = new OrderActorIdentity(actor.SubjectIdAsGuid.Value, actor.TenantId.Value);
        error = Error.None;
        return true;
    }

    public static Error? Authorize(Order order, IActorContextAccessor actorContextAccessor)
    {
        if (!TryResolve(actorContextAccessor, out var actor, out var error))
            return error;

        return order.UserId == actor.UserId && order.TenantId == actor.TenantId
            ? null
            : Error.Forbidden("Orders.Forbidden", "The order is outside the current actor context.");
    }
}
