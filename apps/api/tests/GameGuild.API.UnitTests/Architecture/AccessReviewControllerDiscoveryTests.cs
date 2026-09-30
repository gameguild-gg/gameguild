using FluentAssertions;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Authorization.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Xunit;

namespace GameGuild.API.UnitTests.Architecture;

public sealed class AccessReviewControllerDiscoveryTests
{
    [Fact]
    public void OnlyTheConsolidatedAccessReviewControllerIsDiscovered()
    {
        var manager = new ApplicationPartManager();
        manager.FeatureProviders.Add(new ControllerFeatureProvider());
        manager.ApplicationParts.Add(new AssemblyPart(typeof(AccessReviewCampaignController).Assembly));
        manager.ApplicationParts.Add(new AssemblyPart(typeof(AccessReviewsController).Assembly));
        var feature = new ControllerFeature();

        manager.PopulateFeature(feature);

        var controllerTypes = feature.Controllers.Select(controller => controller.AsType()).ToArray();
        controllerTypes.Should().Contain(typeof(AccessReviewsController));
        controllerTypes.Should().NotContain(typeof(AccessReviewCampaignController));
        controllerTypes.Should().NotContain(typeof(AccessReviewItemController));
        controllerTypes.Should().NotContain(typeof(AccessReviewAnalyticsController));
    }
}