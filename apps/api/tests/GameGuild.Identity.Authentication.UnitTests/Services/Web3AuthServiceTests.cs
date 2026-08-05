using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GameGuild.Identity.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public class Web3AuthServiceTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepoMock = new();
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock = new();
    private readonly Mock<IWeb3Service> _web3ServiceMock = new();
    private readonly Mock<IAuthAttemptService> _authAttemptServiceMock = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();

    private readonly Web3AuthService _sut;

    public Web3AuthServiceTests()
    {
        var configData = new Dictionary<string, string?>
        {
            { "Jwt:RefreshTokenExpirationDays", "7" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.UserAgent = "TestAgent/1.0";
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        _authAttemptServiceMock.Setup(x => x.GetClientIpAddress(It.IsAny<HttpContext>())).Returns("127.0.0.1");

        _jwtTokenServiceMock
            .Setup(x => x.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string[]>()))
            .Returns("jwt-access-token");
        _jwtTokenServiceMock
            .Setup(x => x.GenerateRefreshTokenAsync(It.IsAny<Guid>(), It.IsAny<DeviceInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("jwt-refresh-token");
        _web3ServiceMock
            .Setup(x => x.VerifySignatureAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _sut = new Web3AuthService(
            _refreshTokenRepoMock.Object,
            _jwtTokenServiceMock.Object,
            _web3ServiceMock.Object,
            configuration,
            _authAttemptServiceMock.Object,
            _httpContextAccessorMock.Object,
            NullLogger<Web3AuthService>.Instance);
    }

    [Fact]
    public async Task VerifyWeb3SignatureAsync_ShouldReturnAccessTokenLifetime_InExpiresIn()
    {
        var request = new Web3VerificationRequest
        {
            WalletAddress = "0xabc123",
            Signature = "0xsignature",
            Challenge = "challenge-nonce"
        };

        var before = SystemClock.UtcNow;
        var result = await _sut.VerifyWeb3SignatureAsync(request);
        var after = SystemClock.UtcNow;

        // ponytail: Web3 sign-in must report access-token lifetime, not refresh-token lifetime
        result.ExpiresIn.Should().Be(3600, "default AccessTokenExpirationMinutes is 60");
        result.AccessTokenExpiresAt.Should().BeOnOrAfter(before.AddMinutes(59));
        result.AccessTokenExpiresAt.Should().BeOnOrBefore(after.AddMinutes(61));
    }

    [Fact]
    public async Task VerifyWeb3SignatureAsync_CustomAccessTokenExpiration_ParsedFromConfig()
    {
        var customConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:RefreshTokenExpirationDays", "7" },
                { "Jwt:AccessTokenExpirationMinutes", "10" }
            })
            .Build();

        var sut = new Web3AuthService(
            _refreshTokenRepoMock.Object,
            _jwtTokenServiceMock.Object,
            _web3ServiceMock.Object,
            customConfig,
            _authAttemptServiceMock.Object,
            _httpContextAccessorMock.Object,
            NullLogger<Web3AuthService>.Instance);

        var request = new Web3VerificationRequest
        {
            WalletAddress = "0xabc123",
            Signature = "0xsignature",
            Challenge = "challenge-nonce"
        };

        var result = await sut.VerifyWeb3SignatureAsync(request);

        result.ExpiresIn.Should().Be(600, "10 minutes * 60 seconds");
        result.AccessTokenExpiresAt.Should().BeOnOrAfter(SystemClock.UtcNow.AddMinutes(9));
        result.AccessTokenExpiresAt.Should().BeOnOrBefore(SystemClock.UtcNow.AddMinutes(11));
    }
}
