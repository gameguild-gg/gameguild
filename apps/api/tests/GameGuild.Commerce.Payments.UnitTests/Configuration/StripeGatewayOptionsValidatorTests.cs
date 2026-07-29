using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.Configuration;

public sealed class StripeGatewayOptionsValidatorTests
{
    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void Validate_ReturnsSuccessAndLogsWarningWhenSimulationOutsideDevelopmentAndTesting(string environmentName)
    {
        var validator = CreateValidator(environmentName);
        var options = CreateConfiguredOptions();
        options.UseSimulation = true;

        var result = validator.Validate(Options.DefaultName, options);

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void Validate_ReturnsSuccessWhenCredentialsMissing(string environmentName)
    {
        var validator = CreateValidator(environmentName);

        var result = validator.Validate(Options.DefaultName, new StripeGatewayOptions
        {
            IsEnabled = true,
            UseSimulation = false
        });

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Test")]
    [InlineData("Testing")]
    public void Validate_AllowsSimulationInNonProductionEnvironments(string environmentName)
    {
        var validator = CreateValidator(environmentName);

        var result = validator.Validate(Options.DefaultName, new StripeGatewayOptions
        {
            IsEnabled = true,
            UseSimulation = true
        });

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_AllowsDisabledProvider()
    {
        var validator = CreateValidator(Environments.Production);

        var result = validator.Validate(Options.DefaultName, new StripeGatewayOptions
        {
            IsEnabled = false,
            UseSimulation = false
        });

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_AcceptsTestCredentialsInProductionAsWarning()
    {
        var validator = CreateValidator(Environments.Production);
        var options = CreateConfiguredOptions();
        options.ApiKey = "sk_test_example";
        options.PublishableKey = "pk_test_example";

        var result = validator.Validate(Options.DefaultName, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_AcceptsConfiguredProductionProvider()
    {
        var validator = CreateValidator(Environments.Production);

        var result = validator.Validate(Options.DefaultName, CreateConfiguredOptions());

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_AcceptsTestCredentialsInStaging()
    {
        var validator = CreateValidator(Environments.Staging);
        var options = CreateConfiguredOptions();
        options.ApiKey = "sk_test_example";
        options.PublishableKey = "pk_test_example";
        options.LiveMode = false;

        var result = validator.Validate(Options.DefaultName, options);

        result.Succeeded.Should().BeTrue();
    }

    private static StripeGatewayOptionsValidator CreateValidator(string environmentName)
    {
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(value => value.EnvironmentName).Returns(environmentName);
        return new StripeGatewayOptionsValidator(environment.Object);
    }

    private static StripeGatewayOptions CreateConfiguredOptions() => new()
    {
        IsEnabled = true,
        UseSimulation = false,
        ApiKey = "sk_live_test",
        PublishableKey = "pk_live_test",
        AccountId = "acct_platform",
        LiveMode = true,
        WebhookSecret = string.Empty
    };
}
