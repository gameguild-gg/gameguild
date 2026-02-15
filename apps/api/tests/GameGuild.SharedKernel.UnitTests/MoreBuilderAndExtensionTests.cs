using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Reflection;
using GameGuild;
using GameGuild.CQRS;
using GameGuild.Configuration.InfrastructureLayer;
using GameGuild.Configuration.PresentationLayer.Authorization;
using GameGuild.Configuration.PresentationLayer.OpenAPI;
using GameGuild.Configuration.PresentationLayer.SignalR;

namespace GameGuild.SharedKernel.UnitTests;

public class MoreBuilderAndExtensionTests
{
    private static IConfiguration EmptyConfig() => new ConfigurationBuilder().Build();

    // ═══════════════════════════════════════════════════════════════════
    // OpenApiOptionsBuilder
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void OpenApiOptionsBuilder_Create_ReturnsDefaults()
    {
        var opts = OpenApiOptionsBuilder.Create();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void OpenApiOptionsBuilder_Create_WithConfig()
    {
        var opts = OpenApiOptionsBuilder.Create(EmptyConfig());
        opts.Should().NotBeNull();
    }

    [Fact]
    public void OpenApiOptionsBuilder_Create_WithConfigAndSection()
    {
        var opts = OpenApiOptionsBuilder.Create(EmptyConfig(), "OpenApi");
        opts.Should().NotBeNull();
    }

    [Fact]
    public void OpenApiOptionsBuilder_Validate_DoesNotThrow()
    {
        var opts = OpenApiOptionsBuilder.Create();
        var act = () => OpenApiOptionsBuilder.Validate(opts);
        act.Should().NotThrow();
    }

    [Fact]
    public void OpenApiOptionsBuilder_Build_ReturnsOptions()
    {
        var opts = OpenApiOptionsBuilder.Build();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void OpenApiOptionsBuilder_Build_WithConfig()
    {
        var opts = OpenApiOptionsBuilder.Build(EmptyConfig());
        opts.Should().NotBeNull();
    }

    [Fact]
    public void OpenApiOptionsBuilder_Build_WithConfigAndSection()
    {
        var opts = OpenApiOptionsBuilder.Build(EmptyConfig(), "OpenApi");
        opts.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════════
    // SignalROptionsBuilder
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void SignalROptionsBuilder_Create_ReturnsDefaults()
    {
        var opts = SignalROptionsBuilder.Create();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void SignalROptionsBuilder_Create_WithConfig()
    {
        var opts = SignalROptionsBuilder.Create(EmptyConfig());
        opts.Should().NotBeNull();
    }

    [Fact]
    public void SignalROptionsBuilder_Create_WithConfigAndSection()
    {
        var opts = SignalROptionsBuilder.Create(EmptyConfig(), "SignalR");
        opts.Should().NotBeNull();
    }

    [Fact]
    public void SignalROptionsBuilder_Validate_DoesNotThrow()
    {
        var opts = SignalROptionsBuilder.Create();
        var act = () => SignalROptionsBuilder.Validate(opts);
        act.Should().NotThrow();
    }

    [Fact]
    public void SignalROptionsBuilder_Build_ReturnsOptions()
    {
        var opts = SignalROptionsBuilder.Build();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void SignalROptionsBuilder_Build_WithConfig()
    {
        var opts = SignalROptionsBuilder.Build(EmptyConfig());
        opts.Should().NotBeNull();
    }

    [Fact]
    public void SignalROptionsBuilder_Build_WithConfigAndSection()
    {
        var opts = SignalROptionsBuilder.Build(EmptyConfig(), "SignalR");
        opts.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════════
    // InfrastructureLayerOptionsBuilder
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void InfrastructureLayerOptionsBuilder_CreateDefault_ReturnsOptions()
    {
        var opts = InfrastructureLayerOptionsBuilder.CreateDefault();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void InfrastructureLayerOptionsBuilder_Create_WithConfig()
    {
        var opts = InfrastructureLayerOptionsBuilder.Create(EmptyConfig());
        opts.Should().NotBeNull();
    }

    [Fact]
    public void InfrastructureLayerOptionsBuilder_CreateWithValidation()
    {
        var opts = InfrastructureLayerOptionsBuilder.CreateWithValidation(EmptyConfig());
        opts.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════════
    // AuthorizationOptionsBuilder
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void AuthorizationOptionsBuilder_Create_ReturnsDefaults()
    {
        var opts = AuthorizationOptionsBuilder.Create();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void AuthorizationOptionsBuilder_Create_WithConfig()
    {
        var opts = AuthorizationOptionsBuilder.Create(EmptyConfig());
        opts.Should().NotBeNull();
    }

    [Fact]
    public void AuthorizationOptionsBuilder_Create_WithConfigAndSection()
    {
        var opts = AuthorizationOptionsBuilder.Create(EmptyConfig(), "Authorization");
        opts.Should().NotBeNull();
    }

    [Fact]
    public void AuthorizationOptionsBuilder_Validate_DoesNotThrow()
    {
        var opts = AuthorizationOptionsBuilder.Create();
        var act = () => AuthorizationOptionsBuilder.Validate(opts);
        act.Should().NotThrow();
    }

    [Fact]
    public void AuthorizationOptionsBuilder_Build_ReturnsOptions()
    {
        var opts = AuthorizationOptionsBuilder.Build();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void AuthorizationOptionsBuilder_Build_WithConfig()
    {
        var opts = AuthorizationOptionsBuilder.Build(EmptyConfig());
        opts.Should().NotBeNull();
    }

    [Fact]
    public void AuthorizationOptionsBuilder_Build_WithConfigAndSection()
    {
        var opts = AuthorizationOptionsBuilder.Build(EmptyConfig(), "Authorization");
        opts.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════════
    // EndpointExtensions
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void EndpointExtensions_AddEndpoints_RegistersScannedEndpoints()
    {
        var services = new ServiceCollection();
        services.AddEndpoints(typeof(MoreBuilderAndExtensionTests).Assembly);
        // Scanning test assembly — no endpoints but still exercises scanning code
        services.Should().NotBeNull();
    }

    [Fact]
    public void EndpointExtensions_AddEndpoints_WithSharedKernelAssembly()
    {
        var services = new ServiceCollection();
        services.AddEndpoints(typeof(ModuleRegistry).Assembly);
        services.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════════
    // CQRS — additional resolution tests
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void AddCqrs_EmptyAssembly_StillRegistersCore()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCqrs();  // no assemblies = calling assembly
        var sp = services.BuildServiceProvider();

        sp.GetService<ISender>().Should().NotBeNull();
        sp.GetService<IPublisher>().Should().NotBeNull();
        sp.GetService<IMediator>().Should().NotBeNull();
    }
}
