using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace GameGuild.Tests.SharedKernel.Unit.Middlewares;

public class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_AddsDefaultHeaders_ForRegularPath()
    {
        var options = new SecurityHeadersOptions();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask, options);
        var (context, responseFeature) = CreateContext("/api/properties");

        await middleware.InvokeAsync(context);
        await responseFeature.FireOnStartingAsync();

        context.Response.Headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
        context.Response.Headers["X-Frame-Options"].ToString().Should().Be(options.XFrameOptionsValue);
        context.Response.Headers["Referrer-Policy"].ToString().Should().Be(options.ReferrerPolicyValue);
        context.Response.Headers["X-XSS-Protection"].ToString().Should().Be("0");
        context.Response.Headers["Content-Security-Policy"].ToString().Should().Be(options.ContentSecurityPolicyValue);
        context.Response.Headers["Permissions-Policy"].ToString().Should().Be(options.PermissionsPolicyValue);
        context.Response.Headers.ContainsKey("Cache-Control").Should().BeFalse();
        context.Response.Headers.ContainsKey("Pragma").Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_UsesSwaggerPolicy_ForSwaggerPath()
    {
        var options = new SecurityHeadersOptions();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask, options);
        var (context, responseFeature) = CreateContext("/swagger/index.html");

        await middleware.InvokeAsync(context);
        await responseFeature.FireOnStartingAsync();

        context.Response.Headers["Content-Security-Policy"].ToString().Should().Be(options.SwaggerContentSecurityPolicyValue);
    }

    [Fact]
    public async Task InvokeAsync_AddsNoCacheHeaders_ForSensitivePath()
    {
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask, new SecurityHeadersOptions());
        var (context, responseFeature) = CreateContext("/auth/token");

        await middleware.InvokeAsync(context);
        await responseFeature.FireOnStartingAsync();

        context.Response.Headers["Cache-Control"].ToString().Should().Be("no-store, no-cache, must-revalidate");
        context.Response.Headers["Pragma"].ToString().Should().Be("no-cache");
    }

    [Fact]
    public async Task InvokeAsync_DoesNotAddDisabledHeaders()
    {
        var options = new SecurityHeadersOptions
        {
            EnableXContentTypeOptions = false,
            EnableXFrameOptions = false,
            EnableReferrerPolicy = false,
            EnableXXssProtection = false,
            EnableContentSecurityPolicy = false,
            EnablePermissionsPolicy = false,
            EnableNoCacheForSensitiveEndpoints = false
        };
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask, options);
        var (context, responseFeature) = CreateContext("/auth/token");

        await middleware.InvokeAsync(context);
        await responseFeature.FireOnStartingAsync();

        context.Response.Headers.ContainsKey("X-Content-Type-Options").Should().BeFalse();
        context.Response.Headers.ContainsKey("X-Frame-Options").Should().BeFalse();
        context.Response.Headers.ContainsKey("Referrer-Policy").Should().BeFalse();
        context.Response.Headers.ContainsKey("X-XSS-Protection").Should().BeFalse();
        context.Response.Headers.ContainsKey("Content-Security-Policy").Should().BeFalse();
        context.Response.Headers.ContainsKey("Permissions-Policy").Should().BeFalse();
        context.Response.Headers.ContainsKey("Cache-Control").Should().BeFalse();
        context.Response.Headers.ContainsKey("Pragma").Should().BeFalse();
    }

    private static (DefaultHttpContext Context, TestResponseFeature ResponseFeature) CreateContext(string path)
    {
        var responseFeature = new TestResponseFeature();
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(responseFeature);
        context.Request.Path = path;
        return (context, responseFeature);
    }

    private sealed class TestResponseFeature : IHttpResponseFeature
    {
        private readonly List<(Func<object, Task> Callback, object State)> _onStarting = [];
        private readonly List<(Func<object, Task> Callback, object State)> _onCompleted = [];

        public int StatusCode { get; set; } = StatusCodes.Status200OK;

        public string? ReasonPhrase { get; set; }

        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();

        public Stream Body { get; set; } = new MemoryStream();

        public bool HasStarted { get; private set; }

        public void OnStarting(Func<object, Task> callback, object state)
        {
            _onStarting.Add((callback, state));
        }

        public void OnCompleted(Func<object, Task> callback, object state)
        {
            _onCompleted.Add((callback, state));
        }

        public async Task FireOnStartingAsync()
        {
            for (var index = _onStarting.Count - 1; index >= 0; index--)
            {
                var (callback, state) = _onStarting[index];
                await callback(state);
            }

            HasStarted = true;
        }
    }
}
