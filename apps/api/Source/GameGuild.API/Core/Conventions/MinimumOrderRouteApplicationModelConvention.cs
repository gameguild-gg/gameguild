using System.Reflection;
using GameGuild.Commerce.Orders;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace GameGuild.API;

/// <summary>
/// Exposes only the order operations that have completed the production safety review.
/// </summary>
internal sealed class MinimumOrderRouteApplicationModelConvention : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        var controller = application.Controllers.SingleOrDefault(model => model.ControllerType == typeof(OrdersController));
        if (controller is null)
        {
            return;
        }

        var verifiedActions = controller.Actions
            .Where(action => action.ActionMethod.GetCustomAttribute<MinimumOrderRouteAttribute>(inherit: true) is not null)
            .ToHashSet();

        if (verifiedActions.Count == 0)
        {
            throw new InvalidOperationException("OrdersController does not expose any verified minimum routes.");
        }

        for (var index = controller.Actions.Count - 1; index >= 0; index--)
        {
            if (!verifiedActions.Contains(controller.Actions[index]))
            {
                controller.Actions.RemoveAt(index);
            }
        }
    }
}
