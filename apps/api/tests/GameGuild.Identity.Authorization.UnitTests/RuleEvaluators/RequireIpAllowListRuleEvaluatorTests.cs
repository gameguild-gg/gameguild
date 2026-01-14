using System.Net;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.RuleEvaluators;

public class RequireIpAllowListRuleEvaluatorTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly RequireIpAllowListRuleEvaluator _evaluator;

    public RequireIpAllowListRuleEvaluatorTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _evaluator = new RequireIpAllowListRuleEvaluator(_httpContextAccessorMock.Object);
    }

    private void SetupHttpContext(string ipAddress)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(ipAddress);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);
    }

    [Fact]
    public async Task EvaluateAsync_NoCidrs_ReturnsSuccess()
    {
        // Arrange
        var context = new AuthorizationHandlerContext([], new ClaimsPrincipal(), null);
        var parameters = RuleParameters.FromJson("{}");

        // Act
        var result = await _evaluator.EvaluateAsync(context, parameters);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_AllowedIp_ReturnsSuccess()
    {
        // Arrange
        SetupHttpContext("192.168.1.10");
        var context = new AuthorizationHandlerContext([], new ClaimsPrincipal(), null);
        var parameters = RuleParameters.FromJson("{\"cidrs\": [\"192.168.1.0/24\"]}");

        // Act
        var result = await _evaluator.EvaluateAsync(context, parameters);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_DeniedIp_ReturnsFail()
    {
        // Arrange
        SetupHttpContext("10.0.0.1");
        var context = new AuthorizationHandlerContext([], new ClaimsPrincipal(), null);
        var parameters = RuleParameters.FromJson("{\"cidrs\": [\"192.168.1.0/24\"]}");

        // Act
        var result = await _evaluator.EvaluateAsync(context, parameters);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Contain("IP");
    }
}
