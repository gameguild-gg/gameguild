using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Economy.Treasury.UnitTests;

public sealed class TreasuryModuleTests
{
    [Fact]
    public void FailClosedProviderEvidenceVerifierRejectsEveryReceiptAndEvent()
    {
        var verifier = new FailClosedAdminWithdrawalProviderEvidenceVerifier();

        verifier.Verify((AdminWithdrawalProviderReceipt)null!).Should().BeFalse();
        verifier.Verify((AdminWithdrawalProviderEvent)null!).Should().BeFalse();
    }

    [Fact]
    public void ModuleAndCompositionHookRemainDisabledAndRegisterOnlyDurablePersistence()
    {
        var module = new TreasuryModule();
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        module.Name.Should().Be("Economy.Treasury");
        module.EnabledByDefault.Should().BeFalse();
        module.ConfigureServices(services, configuration).Should().BeSameAs(services);
        services.AddTreasuryComposition(configuration).Should().BeSameAs(services);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IAdminWithdrawalStore) &&
            descriptor.ImplementationType == typeof(PostgreSqlAdminWithdrawalStore) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IAdminWithdrawalAuditTrail) &&
            descriptor.ImplementationType == typeof(PostgreSqlAdminWithdrawalAuditTrail) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IAdminWithdrawalProviderEvidenceVerifier) &&
            descriptor.ImplementationType == typeof(FailClosedAdminWithdrawalProviderEvidenceVerifier) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IDurableAdminWithdrawalWorkflow) &&
            descriptor.ImplementationType == typeof(PostgreSqlDurableAdminWithdrawalWorkflow) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);        services.Should().NotContain(descriptor => descriptor.ServiceType == typeof(AdminWithdrawalCoordinator));
        services.Should().NotContain(descriptor => descriptor.ServiceType == typeof(DbContext));
    }
}
