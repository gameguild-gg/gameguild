using System.Net;
using FluentAssertions;
using GameGuild.API.Setup;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace GameGuild.API.UnitTests.Core;

public sealed class PipelineExtensionsTests
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    [InlineData("/ready")]
    [InlineData("/live")]
    public void ShouldRedirectToHttps_DoesNotRedirectHealthProbes(string path)
    {
        var context = CreateContext(path, IPAddress.Parse("203.0.113.10"));

        PipelineExtensions.ShouldRedirectToHttps(context).Should().BeFalse();
    }

    [Fact]
    public void ShouldRedirectToHttps_DoesNotRedirectLoopbackTraffic()
    {
        var context = CreateContext("/api/users", IPAddress.Loopback);

        PipelineExtensions.ShouldRedirectToHttps(context).Should().BeFalse();
    }

    [Fact]
    public void ShouldRedirectToHttps_RedirectsExternalApplicationTraffic()
    {
        var context = CreateContext("/api/users", IPAddress.Parse("203.0.113.10"));

        PipelineExtensions.ShouldRedirectToHttps(context).Should().BeTrue();
    }

    private static DefaultHttpContext CreateContext(string path, IPAddress remoteAddress)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Connection.RemoteIpAddress = remoteAddress;
        return context;
    }
}
