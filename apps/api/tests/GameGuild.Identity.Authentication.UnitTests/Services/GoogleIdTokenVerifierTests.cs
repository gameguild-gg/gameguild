using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public class GoogleIdTokenVerifierTests
{
    // ───────────────────────────────────────────────────────────────────────────
    // Seam test: proves a fake IGoogleIdTokenVerifier returns the canned
    // VerifiedGoogleUser. This is the contract Todo 3 will depend on — it must
    // pass the moment the interface + record exist.
    // ───────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task FakeVerifier_ReturnsMappedVerifiedGoogleUser()
    {
        // Arrange
        var canned = new VerifiedGoogleUser
        {
            Sub = "google-sub-123",
            Email = "user@example.com",
            EmailVerified = true,
            Name = "Jane Doe",
            Picture = "https://accounts.google.com/photo.png"
        };
        IGoogleIdTokenVerifier verifier = new FakeGoogleIdTokenVerifier(canned);

        // Act
        var result = await verifier.VerifyAsync("any-id-token", CancellationToken.None);

        // Assert — every field round-trips unchanged.
        result.Should().BeEquivalentTo(canned);
        result.Sub.Should().Be("google-sub-123");
        result.EmailVerified.Should().BeTrue();
    }

    // ───────────────────────────────────────────────────────────────────────────
    // Real-verifier rejection test: a malformed token string must throw
    // UnauthorizedAccessException. The token is rejected BEFORE any network
    // JWKS fetch (the JWT shape is invalid), so this stays a pure unit test.
    // ───────────────────────────────────────────────────────────────────────────
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-jwt")]
    [InlineData("only.two")]                                  // missing third segment
    [InlineData("a.b.c")]                                     // three parts but not valid base64url JSON
    public async Task RealVerifier_RejectsMalformedToken(string? idToken)
    {
        var configuration = new Mock<IConfiguration>();
        configuration.Setup(c => c["OAuth:Google:ClientId"]).Returns("test-client-id.apps.googleusercontent.com");
        var logger = new Mock<ILogger<GoogleIdTokenVerifier>>();
        var verifier = new GoogleIdTokenVerifier(configuration.Object, logger.Object);

        // Act & Assert
        await verifier
            .Invoking(v => v.VerifyAsync(idToken!, CancellationToken.None))
            .Should()
            .ThrowAsync<UnauthorizedAccessException>();
    }

    // ───────────────────────────────────────────────────────────────────────────
    // Empty ClientId fails CLOSED: a token cannot pass aud validation against
    // an empty audience list. Verifier must reject before calling Google.
    // ───────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task RealVerifier_MissingClientId_ThrowsUnauthorizedAccess()
    {
        var configuration = new Mock<IConfiguration>();
        configuration.Setup(c => c["OAuth:Google:ClientId"]).Returns((string?)null);
        var logger = new Mock<ILogger<GoogleIdTokenVerifier>>();
        var verifier = new GoogleIdTokenVerifier(configuration.Object, logger.Object);

        await verifier
            .Invoking(v => v.VerifyAsync("header.body.sig", CancellationToken.None))
            .Should()
            .ThrowAsync<UnauthorizedAccessException>();
    }

    private sealed class FakeGoogleIdTokenVerifier : IGoogleIdTokenVerifier
    {
        private readonly VerifiedGoogleUser _user;
        public FakeGoogleIdTokenVerifier(VerifiedGoogleUser user) => _user = user;
        public Task<VerifiedGoogleUser> VerifyAsync(string idToken, CancellationToken ct)
            => Task.FromResult(_user);
    }
}
