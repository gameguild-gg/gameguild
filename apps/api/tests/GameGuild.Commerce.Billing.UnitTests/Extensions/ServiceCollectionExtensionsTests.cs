using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddBillingModule_Should_Register_Services()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Billing:Stripe:SecretKey"] = "sk"
            })
            .Build();

        services.AddBillingModule(config);

        services.Should().Contain(d => d.ServiceType == typeof(IBillingWebhookRepository));
        services.Should().Contain(d => d.ServiceType == typeof(StripeBillingWebhookService));
    }

    [Fact]
    public void AddBillingWebhooks_Should_Register_Webhook_Services()
    {
        var services = new ServiceCollection();

        services.AddBillingWebhooks();

        services.Should().Contain(d => d.ServiceType == typeof(IBillingWebhookRepository));
        services.Should().Contain(d => d.ServiceType == typeof(ApplePayBillingWebhookService));
    }
}