using System.Text.Json;
using System.Reflection;
using FluentAssertions;
using GameGuild.API;
using GameGuild.CQRS.Models;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace GameGuild.API.UnitTests.Core;

public sealed class DataMaskingMvcIntegrationTests
{
    [Fact]
    public async Task MvcResponse_UsesConfiguredTenantRuleBeforeJsonSerialization()
    {
        var tenantId = Guid.NewGuid();
        var ruleRepository = new Mock<IDataMaskingRuleRepository>();
        ruleRepository
            .Setup(repository => repository.GetByResourceTypeAsync("User", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        ruleRepository
            .Setup(repository => repository.GetByResourceTypeAsync("User", tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new DataMaskingRule
                {
                    Id = Guid.NewGuid(),
                    TenantId = new TenantId(tenantId),
                    Name = "Mask user email",
                    ResourceType = "User",
                    FieldName = "Email",
                    MaskingType = MaskingType.Redact,
                    IsEnabled = true
                }
            ]);
        var actorAccessor = new Mock<IActorContextAccessor>();
        actorAccessor
            .Setup(accessor => accessor.ActorContext)
            .Returns(ActorContextBuilder.ForUser(Guid.NewGuid()).Build() with { TenantId = tenantId });

        await using var app = await CreateApplicationAsync(ruleRepository.Object, actorAccessor.Object);
        using var client = app.GetTestClient();

        using var response = await client.GetAsync("/data-masking-probe/users");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("email").GetString().Should().Be("[REDACTED]");
        body.RootElement.GetProperty("displayName").GetString().Should().Be("Example User");
        ruleRepository.Verify(repository => repository.GetByResourceTypeAsync(
            "User", tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static async Task<WebApplication> CreateApplicationAsync(
        IDataMaskingRuleRepository ruleRepository,
        IActorContextAccessor actorAccessor)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(ruleRepository);
        builder.Services.AddSingleton(actorAccessor);
        builder.Services.AddScoped<IDataMaskingService, DataMaskingService>();
        builder.Services.AddScoped<FieldMaskingResultFilter>();
        builder.Services
            .AddControllers(options => options.Filters.AddService<FieldMaskingResultFilter>())
            .ConfigureApplicationPartManager(manager =>
            {
                manager.ApplicationParts.Clear();
                manager.ApplicationParts.Add(new ProbeControllerApplicationPart());
            });

        var app = builder.Build();
        app.MapControllers();
        await app.StartAsync();
        return app;
    }

    private sealed class ProbeControllerApplicationPart : ApplicationPart, IApplicationPartTypeProvider
    {
        public override string Name => nameof(ProbeControllerApplicationPart);

        public IEnumerable<TypeInfo> Types { get; } = [typeof(DataMaskingProbeController).GetTypeInfo()];
    }
}

[ApiController]
[Route("data-masking-probe")]
public sealed class DataMaskingProbeController : ControllerBase
{
    [HttpGet("users")]
    public IActionResult GetUser() => Ok(new UserResponse
    {
        Email = "private@example.com",
        DisplayName = "Example User"
    });
}

public sealed class UserResponse
{
    public string Email { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;
}
