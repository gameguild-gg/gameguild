using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using GameGuild;
using GameGuild.Configuration.PresentationLayer.ApiExplorer;
using GameGuild.Configuration.PresentationLayer.ApiVersioning;
using GameGuild.Configuration.PresentationLayer.FeatureFlags;
using GameGuild.Configuration.PresentationLayer.GraphQL;
using GameGuild.Configuration.PresentationLayer.Localization;
using GameGuild.Configuration.PresentationLayer.ModelValidation;
using GameGuild.Configuration.PresentationLayer.ProblemDetails;
using GameGuild.Configuration.PresentationLayer.RequestContext;
using GameGuild.Configuration.PresentationLayer.ResponseCaching;
using GameGuild.SharedKernel;

namespace GameGuild.SharedKernel.UnitTests;

public class AdditionalBuilderAndMiddlewareTests
{
    private static IConfiguration EmptyConfig() => new ConfigurationBuilder().Build();

    // ═══════════════════════════════════════════════════════════════════
    // GraphQLOptionsBuilder
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void GraphQLOptionsBuilder_Create()
    {
        var opts = GraphQLOptionsBuilder.Create();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void GraphQLOptionsBuilder_Create_WithConfig()
    {
        var opts = GraphQLOptionsBuilder.Create(EmptyConfig());
        opts.Should().NotBeNull();
    }

    [Fact]
    public void GraphQLOptionsBuilder_Build()
    {
        var opts = GraphQLOptionsBuilder.Create().Build();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void GraphQLOptionsBuilder_Build_WithConfig()
    {
        var opts = GraphQLOptionsBuilder.Create(EmptyConfig()).Build();
        opts.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════════
    // FeatureFlagsOptionsBuilder
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void FeatureFlagsOptionsBuilder_Create()
    {
        var opts = FeatureFlagsOptionsBuilder.Create();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void FeatureFlagsOptionsBuilder_Create_WithConfig()
    {
        var opts = FeatureFlagsOptionsBuilder.Create(EmptyConfig());
        opts.Should().NotBeNull();
    }

    [Fact]
    public void FeatureFlagsOptionsBuilder_Build()
    {
        var opts = FeatureFlagsOptionsBuilder.Create().Build();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void FeatureFlagsOptionsBuilder_Build_WithConfig()
    {
        var opts = FeatureFlagsOptionsBuilder.Create(EmptyConfig()).Build();
        opts.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════════
    // ApiVersioningOptionsBuilder
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void ApiVersioningOptionsBuilder_Create()
    {
        var opts = ApiVersioningOptionsBuilder.Create();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void ApiVersioningOptionsBuilder_Create_WithConfig()
    {
        var opts = ApiVersioningOptionsBuilder.Create(EmptyConfig());
        opts.Should().NotBeNull();
    }

    [Fact]
    public void ApiVersioningOptionsBuilder_Build()
    {
        var opts = ApiVersioningOptionsBuilder.Build();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void ApiVersioningOptionsBuilder_Build_WithConfig()
    {
        var opts = ApiVersioningOptionsBuilder.Build(EmptyConfig());
        opts.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════════
    // ResponseCachingOptionsBuilder
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void ResponseCachingOptionsBuilder_Create()
    {
        var opts = ResponseCachingOptionsBuilder.Create();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void ResponseCachingOptionsBuilder_Create_WithConfig()
    {
        var opts = ResponseCachingOptionsBuilder.Create(EmptyConfig());
        opts.Should().NotBeNull();
    }

    [Fact]
    public void ResponseCachingOptionsBuilder_Build()
    {
        var opts = ResponseCachingOptionsBuilder.Build();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void ResponseCachingOptionsBuilder_Build_WithConfig()
    {
        var opts = ResponseCachingOptionsBuilder.Build(EmptyConfig());
        opts.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════════
    // RequestContextOptionsBuilder
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void RequestContextOptionsBuilder_Create()
    {
        var opts = RequestContextOptionsBuilder.Create();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void RequestContextOptionsBuilder_Create_WithConfig()
    {
        var opts = RequestContextOptionsBuilder.Create(EmptyConfig());
        opts.Should().NotBeNull();
    }

    [Fact]
    public void RequestContextOptionsBuilder_Build()
    {
        var opts = RequestContextOptionsBuilder.Build();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void RequestContextOptionsBuilder_Build_WithConfig()
    {
        var opts = RequestContextOptionsBuilder.Build(EmptyConfig());
        opts.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════════
    // ProblemDetailsOptionsBuilder
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void ProblemDetailsOptionsBuilder_Create()
    {
        var opts = ProblemDetailsOptionsBuilder.Create();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void ProblemDetailsOptionsBuilder_Create_WithConfig()
    {
        var opts = ProblemDetailsOptionsBuilder.Create(EmptyConfig());
        opts.Should().NotBeNull();
    }

    [Fact]
    public void ProblemDetailsOptionsBuilder_Build()
    {
        var opts = ProblemDetailsOptionsBuilder.Build();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void ProblemDetailsOptionsBuilder_Build_WithConfig()
    {
        var opts = ProblemDetailsOptionsBuilder.Build(EmptyConfig());
        opts.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════════
    // ModelValidationOptionsBuilder
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void ModelValidationOptionsBuilder_Create()
    {
        var opts = ModelValidationOptionsBuilder.Create();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void ModelValidationOptionsBuilder_Create_WithConfig()
    {
        var opts = ModelValidationOptionsBuilder.Create(EmptyConfig());
        opts.Should().NotBeNull();
    }

    [Fact]
    public void ModelValidationOptionsBuilder_Build()
    {
        var opts = ModelValidationOptionsBuilder.Build();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void ModelValidationOptionsBuilder_Build_WithConfig()
    {
        var opts = ModelValidationOptionsBuilder.Build(EmptyConfig());
        opts.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════════
    // LocalizationOptionsBuilder
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void LocalizationOptionsBuilder_Create()
    {
        var opts = LocalizationOptionsBuilder.Create();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void LocalizationOptionsBuilder_Create_WithConfig()
    {
        var opts = LocalizationOptionsBuilder.Create(EmptyConfig());
        opts.Should().NotBeNull();
    }

    [Fact]
    public void LocalizationOptionsBuilder_Build()
    {
        var opts = LocalizationOptionsBuilder.Build();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void LocalizationOptionsBuilder_Build_WithConfig()
    {
        var opts = LocalizationOptionsBuilder.Build(EmptyConfig());
        opts.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════════
    // ApiExplorerOptionsBuilder
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void ApiExplorerOptionsBuilder_Create()
    {
        var opts = ApiExplorerOptionsBuilder.Create();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void ApiExplorerOptionsBuilder_Create_WithConfig()
    {
        var opts = ApiExplorerOptionsBuilder.Create(EmptyConfig());
        opts.Should().NotBeNull();
    }

    [Fact]
    public void ApiExplorerOptionsBuilder_Build()
    {
        var opts = ApiExplorerOptionsBuilder.Build();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void ApiExplorerOptionsBuilder_Build_WithConfig()
    {
        var opts = ApiExplorerOptionsBuilder.Build(EmptyConfig());
        opts.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════════
    // Middleware constructors
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void ExceptionHandlingMiddleware_CanBeConstructed()
    {
        var mw = new ExceptionHandlingMiddleware(
            ctx => Task.CompletedTask,
            NullLogger<ExceptionHandlingMiddleware>.Instance);
        mw.Should().NotBeNull();
    }

    [Fact]
    public void SecurityHeadersMiddleware_CanBeConstructed()
    {
        var mw = new SecurityHeadersMiddleware(
            ctx => Task.CompletedTask);
        mw.Should().NotBeNull();
    }

    [Fact]
    public void SecurityHeadersMiddleware_WithOptions()
    {
        var mw = new SecurityHeadersMiddleware(
            ctx => Task.CompletedTask,
            new SecurityHeadersOptions());
        mw.Should().NotBeNull();
    }

    [Fact]
    public void InMemoryIntegrationEventBus_CanBeConstructed()
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        var bus = new InMemoryIntegrationEventBus(
            sp,
            NullLogger<InMemoryIntegrationEventBus>.Instance);
        bus.Should().NotBeNull();
    }
}
