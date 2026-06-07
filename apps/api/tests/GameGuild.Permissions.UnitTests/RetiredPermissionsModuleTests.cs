using FluentAssertions;
using GameGuild.Identity.Authorization;
using Xunit;

namespace GameGuild.Tests.Permissions.Unit;

public sealed class RetiredPermissionsModuleTests
{
    [Fact]
    public void PermissionsMicroserviceScope_IsConsolidatedIntoIdentityAuthorizationModule()
    {
        typeof(TenantPermission).Namespace.Should().Be("GameGuild.Identity.Authorization");
    }
}
