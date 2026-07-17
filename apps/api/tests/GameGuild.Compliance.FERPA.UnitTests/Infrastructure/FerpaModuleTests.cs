using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace GameGuild.Compliance.FERPA.UnitTests.Infrastructure;

public sealed class FerpaModuleTests
{
    [Fact]
    public void AddFerpaModule_RegistersCompleteScopedObjectGraph()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IApplicationDbContext>());

        services.AddFerpaModule().Should().BeSameAs(services);

        var expectedRegistrations = new Dictionary<Type, Type>
        {
            [typeof(IFerpaEducationRecordRepository)] = typeof(FerpaEducationRecordRepository),
            [typeof(IFerpaDirectoryInformationPolicyRepository)] = typeof(FerpaDirectoryInformationPolicyRepository),
            [typeof(IFerpaDisclosureConsentRepository)] = typeof(FerpaDisclosureConsentRepository),
            [typeof(IFerpaDisclosureLogRepository)] = typeof(FerpaDisclosureLogRepository),
            [typeof(IFerpaInspectionRequestRepository)] = typeof(FerpaInspectionRequestRepository),
            [typeof(IFerpaService)] = typeof(FerpaService)
        };

        foreach (var (serviceType, implementationType) in expectedRegistrations)
        {
            services.Should().ContainSingle(descriptor =>
                descriptor.ServiceType == serviceType &&
                descriptor.ImplementationType == implementationType &&
                descriptor.Lifetime == ServiceLifetime.Scoped);
        }

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IFerpaService>().Should().BeOfType<FerpaService>();
    }

    [Fact]
    public void ModelConfiguration_DefinesPersistenceConstraintsAndIndexes()
    {
        using var context = FerpaTestDbContext.Create();
        var record = context.Model.FindEntityType(typeof(FerpaEducationRecord))!;
        var policy = context.Model.FindEntityType(typeof(FerpaDirectoryInformationPolicy))!;
        var consent = context.Model.FindEntityType(typeof(FerpaDisclosureConsent))!;
        var disclosure = context.Model.FindEntityType(typeof(FerpaDisclosureLog))!;
        var request = context.Model.FindEntityType(typeof(FerpaInspectionRequest))!;

        record.FindPrimaryKey()!.Properties.Should().ContainSingle(property => property.Name == nameof(FerpaEducationRecord.Id));
        record.GetIndexes().Should().Contain(index => index.Properties.Single().Name == nameof(FerpaEducationRecord.StudentUserId));
        record.FindProperty(nameof(FerpaEducationRecord.Title))!.GetMaxLength().Should().Be(300);
        record.FindProperty(nameof(FerpaEducationRecord.RecordKind))!.GetMaxLength().Should().Be(80);
        record.FindProperty(nameof(FerpaEducationRecord.MetadataJson))!
            .FindAnnotation("Relational:ColumnType")!.Value.Should().Be("jsonb");
        policy.GetIndexes().Single(index => index.Properties.Single().Name == nameof(FerpaDirectoryInformationPolicy.TenantId)).IsUnique.Should().BeTrue();
        policy.FindProperty(nameof(FerpaDirectoryInformationPolicy.NoticeUrl))!.GetMaxLength().Should().Be(500);
        consent.FindProperty(nameof(FerpaDisclosureConsent.Recipient))!.GetMaxLength().Should().Be(250);
        disclosure.FindProperty(nameof(FerpaDisclosureLog.Basis))!.GetMaxLength().Should().Be(80);
        request.FindProperty(nameof(FerpaInspectionRequest.Status))!.GetMaxLength().Should().Be(80);
        request.FindProperty(nameof(FerpaInspectionRequest.ProcessingNotes))!.GetMaxLength().Should().Be(2000);
    }

    [Fact]
    public void Module_ExposesStableIdentityAndDelegatesRegistration()
    {
        var module = new FerpaModule();
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        var endpoints = Mock.Of<Microsoft.AspNetCore.Routing.IEndpointRouteBuilder>();

        var configured = module.ConfigureServices(services, configuration);
        var mapped = module.MapEndpoints(endpoints);

        module.Name.Should().Be("FERPA");
        module.Order.Should().Be(95);
        configured.Should().BeSameAs(services);
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IFerpaService));
        mapped.Should().BeSameAs(endpoints);
    }
}
