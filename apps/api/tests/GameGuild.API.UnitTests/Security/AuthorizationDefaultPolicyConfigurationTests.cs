using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using GameGuild.API;
using GameGuild.Configuration.PresentationLayer.Authorization;
using PresentationAuthorizationOptions = GameGuild.Configuration.PresentationLayer.Authorization.AuthorizationOptions;
using RuntimeAuthorizationOptions = Microsoft.AspNetCore.Authorization.AuthorizationOptions;
using Xunit;

namespace GameGuild.API.UnitTests.Security;

public sealed class AuthorizationDefaultPolicyConfigurationTests
{
    [Fact]
    public void SetupAuthorization_UsesConfiguredPolicyNamedByDefaultPolicy()
    {
        var options = new PresentationAuthorizationOptions
        {
            DefaultPolicy = "CanModerate",
            RequireAuthenticatedUser = true,
            RoleHierarchy = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Administrator"] = ["Moderator"]
            },
            Policies = new Dictionary<string, ConfiguredAuthorizationPolicyOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["CanModerate"] = new()
                {
                    RequireAuthenticatedUser = false,
                    Roles = ["Moderator"],
                    Claims = [new AuthorizationClaimRequirementOptions
                    {
                        Type = "tenant_access",
                        AllowedValues = ["write"]
                    }],
                    AuthenticationSchemes = ["Bearer"]
                }
            }
        };
        var services = new ServiceCollection();
        services.AddLogging();
        services.SetupAuthorization(new ConfigurationBuilder().Build(), options);

        using var provider = services.BuildServiceProvider();
        var defaultPolicy = provider.GetRequiredService<IOptions<RuntimeAuthorizationOptions>>().Value.DefaultPolicy;

        defaultPolicy.Requirements.OfType<DenyAnonymousAuthorizationRequirement>().Should().ContainSingle();
        defaultPolicy.Requirements.OfType<RolesAuthorizationRequirement>().Single().AllowedRoles
            .Should().BeEquivalentTo("Moderator", "Administrator");
        var claimRequirement = defaultPolicy.Requirements.OfType<ClaimsAuthorizationRequirement>().Single();
        claimRequirement.ClaimType.Should().Be("tenant_access");
        claimRequirement.AllowedValues.Should().ContainSingle().Which.Should().Be("write");
        defaultPolicy.AuthenticationSchemes.Should().ContainSingle().Which.Should().Be("Bearer");
    }

    [Fact]
    public void SetupAuthorization_HonorsExplicitlyDisabledAuthenticationForConfiguredDefaultPolicy()
    {
        var options = new PresentationAuthorizationOptions
        {
            DefaultPolicy = "CanReadAudit",
            RequireAuthenticatedUser = false,
            Policies = new Dictionary<string, ConfiguredAuthorizationPolicyOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["CanReadAudit"] = new()
                {
                    RequireAuthenticatedUser = false,
                    Claims = [new AuthorizationClaimRequirementOptions
                    {
                        Type = "audit_access",
                        AllowedValues = ["read"]
                    }]
                }
            }
        };
        var services = new ServiceCollection();
        services.AddLogging();
        services.SetupAuthorization(new ConfigurationBuilder().Build(), options);

        using var provider = services.BuildServiceProvider();
        var defaultPolicy = provider.GetRequiredService<IOptions<RuntimeAuthorizationOptions>>().Value.DefaultPolicy;

        defaultPolicy.Requirements.OfType<DenyAnonymousAuthorizationRequirement>().Should().BeEmpty();
        defaultPolicy.Requirements.OfType<ClaimsAuthorizationRequirement>().Single().ClaimType.Should().Be("audit_access");
    }
}
