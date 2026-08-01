using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingParticipantControllerAuthorizationTests
{
    [Fact]
    public void Self_Service_Registration_Endpoints_Should_Use_Authenticated_Actor_Identity()
    {
        typeof(TestingParticipantsController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Should().NotBeEmpty();

        foreach (var methodName in new[]
                 {
                     nameof(TestingParticipantsController.RegisterForSession),
                     nameof(TestingParticipantsController.UnregisterFromSession),
                     nameof(TestingParticipantsController.AddToWaitlist),
                     nameof(TestingParticipantsController.RemoveFromWaitlist)
                 })
        {
            var method = typeof(TestingParticipantsController).GetMethod(methodName)!;
            method.GetCustomAttributes(inherit: true)
                .Should().NotContain(attribute => attribute.GetType().Name.StartsWith("RequireResourcePermission", StringComparison.Ordinal));
        }
    }
}