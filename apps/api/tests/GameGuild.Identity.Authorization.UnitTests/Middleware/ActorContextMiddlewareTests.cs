using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameGuild.Identity.Authorization.UnitTests.Middleware;

public sealed class ActorContextMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_PreservesSecurityClaimsInTypedAttributes()
    {
        var subjectId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var authenticatedAt = DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds();
        var tokenId = Guid.NewGuid().ToString();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(JwtRegisteredClaimNames.Sub, subjectId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, tokenId),
            new Claim("auth_time", authenticatedAt.ToString()),
            new Claim(JwtRegisteredClaimNames.Exp, expiresAt.ToString()),
            new Claim("mfa_verified", "true")
        ], "Bearer"));
        var claimsAccessor = new StaticClaimsPrincipalAccessor(principal);
        var tenantResolver = new Mock<IAuthorizationTenantResolver>();
        tenantResolver
            .Setup(resolver => resolver.ResolveTenantIdAsync(It.IsAny<HttpContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId.ToString());
        var actorAccessor = new ActorContextAccessor();
        ActorContext? capturedActor = null;
        var middleware = new ActorContextMiddleware(
            _ =>
            {
                capturedActor = actorAccessor.ActorContext;
                return Task.CompletedTask;
            },
            Mock.Of<ILogger<ActorContextMiddleware>>());

        await middleware.InvokeAsync(
            new DefaultHttpContext(),
            actorAccessor,
            tenantResolver.Object,
            claimsAccessor,
            null);

        capturedActor.Should().NotBeNull();
        capturedActor!.TypedAttributes.AuthenticatedAt.Should().Be(DateTimeOffset.FromUnixTimeSeconds(authenticatedAt));
        capturedActor.TypedAttributes.TokenExpiresAt.Should().Be(DateTimeOffset.FromUnixTimeSeconds(expiresAt));
        capturedActor.TypedAttributes.TokenId.Should().Be(tokenId);
        capturedActor.TypedAttributes.MfaVerified.Should().BeTrue();
    }
}
