using System.Reflection;
using FluentAssertions;
using GameGuild.API.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.API.UnitTests.Endpoints;

public sealed class AuthenticationEndpointHandlerTests
{
    [Fact]
    public async Task GoogleSignIn_LegacyShell_ReturnsGoneInsteadOfMockTokens()
    {
        var method = typeof(AuthenticationEndpoint).GetMethod("GoogleSignIn", BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();

        var task = (Task<IResult>)method!.Invoke(
            null,
            [new GoogleSignInRequest("id-token"), Mock.Of<ILogger<Program>>()])!;

        var result = await task;

        result.Should().BeAssignableTo<IStatusCodeHttpResult>();
        ((IStatusCodeHttpResult)result).StatusCode.Should().Be(StatusCodes.Status410Gone);
        result.Should().NotBeOfType<SignInResponseDto>();
    }
}
