using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Learning.Certificates.Tests;

public class CertificatesModuleAndServiceTests
{
    [Fact]
    public void AddCertificatesModule_RegistersServices()
    {
        var services = new ServiceCollection();
        services.AddScoped<IApplicationDbContext>(_ => Mock.Of<IApplicationDbContext>());
        services.AddScoped(typeof(ILogger<>), typeof(NullLogger<>));

        services.AddCertificatesModule();

        var provider = services.BuildServiceProvider();
        provider.GetService<ICertificateService>().Should().NotBeNull();
        provider.GetService<ICertificateTemplateService>().Should().NotBeNull();
    }

    [Fact]
    public void CertificateService_CanBeInstantiated()
    {
        var service = new CertificateService(
            Mock.Of<IApplicationDbContext>(),
            NullLogger<CertificateService>.Instance);

        service.Should().NotBeNull();
    }

    [Fact]
    public void CertificateTemplateService_CanBeInstantiated()
    {
        var service = new CertificateTemplateService(
            Mock.Of<IApplicationDbContext>(),
            NullLogger<CertificateTemplateService>.Instance);

        service.Should().NotBeNull();
    }
}
