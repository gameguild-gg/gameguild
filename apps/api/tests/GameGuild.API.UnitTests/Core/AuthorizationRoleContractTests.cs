using FluentAssertions;
using GameGuild.Compliance.Audit;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace GameGuild.API.UnitTests.Core;

public sealed class AuthorizationRoleContractTests
{
    [Theory]
    [InlineData(typeof(KeyRotationController))]
    [InlineData(typeof(SecurityAuditController))]
    [InlineData(typeof(AuditController))]
    public void PlatformSecurityControllers_Should_Require_The_SystemAdminPolicy(Type controllerType)
    {
        var authorize = controllerType
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Should()
            .ContainSingle()
            .Subject;

        authorize.Policy.Should().Be(Policies.SystemAdmin);
        authorize.Roles.Should().BeNullOrEmpty();
    }
}
