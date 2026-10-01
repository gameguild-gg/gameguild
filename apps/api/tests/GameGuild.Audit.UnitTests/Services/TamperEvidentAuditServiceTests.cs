using System.Security.Cryptography;
using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.Compliance.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace GameGuild.Tests.Audit.Unit.Services;

public sealed class TamperEvidentAuditServiceTests : IDisposable
{
    private readonly DbContextOptions<ApplicationDbContext> _contextOptions;
    private readonly ServiceProvider _serviceProvider;
    private readonly EcdsaCryptographicSigningService _signingService;

    public TamperEvidentAuditServiceTests()
    {
        _contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TamperEvidentAudit_{Guid.NewGuid()}", new InMemoryDatabaseRoot())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _signingService = new EcdsaCryptographicSigningService(Options.Create(CreateSigningOptions()));
        _serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddScoped<IApplicationDbContext>(_ => new TestApplicationDbContext(_contextOptions))
            .BuildServiceProvider();
    }

    [Fact]
    public async Task CreateAuditLogAsync_ShouldPersistSnapshotsInSignedSequence_AndVerifyTheChain()
    {
        var service = CreateService();
        var tenantId = Guid.NewGuid();

        var sessionId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString();
        var first = await service.CreateAuditLogAsync(
            tenantId,
            Guid.NewGuid(),
            "RoleAssigned",
            "Role",
            Guid.NewGuid(),
            """{"role":"viewer"}""",
            """{"role":"admin"}""",
            "Role changed from viewer to admin",
            "High",
            "192.0.2.1",
            "test-agent",
            new AuditEventMetadata { SessionId = sessionId, CorrelationId = correlationId },
            CancellationToken.None);
        var second = await service.CreateAuditLogAsync(
            tenantId,
            Guid.NewGuid(),
            "RoleRevoked",
            "Role",
            Guid.NewGuid(),
            null,
            """{"removed":true}""",
            "Role revoked",
            "High",
            "192.0.2.2",
            "test-agent");

        first.IsSuccess.Should().BeTrue(first.Error.Description);
        second.IsSuccess.Should().BeTrue(second.Error.Description);
        first.Value.SequenceNumber.Should().Be(1);
        second.Value.SequenceNumber.Should().Be(2);
        second.Value.PreviousHash.Should().Be(first.Value.ChainHash);
        first.Value.BeforeSnapshot.Should().Be("""{"role":"viewer"}""");
        first.Value.AfterSnapshot.Should().Be("""{"role":"admin"}""");
        first.Value.SessionId.Should().Be(sessionId);
        first.Value.CorrelationId.Should().Be(correlationId);
        first.Value.SigningKeyId.Should().Be("test-key");
        _signingService.VerifySignature(first.Value.ChainHash, first.Value.DigitalSignature, "test-key").Should().BeTrue();

        var integrity = await service.VerifyChainIntegrityAsync(tenantId);

        integrity.IsSuccess.Should().BeTrue();
        integrity.Value.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyChainIntegrityAsync_ShouldRejectChangedPersistedSnapshot()
    {
        var service = CreateService();
        var tenantId = Guid.NewGuid();
        var created = await service.CreateAuditLogAsync(
            tenantId,
            Guid.NewGuid(),
            "AdminUpdated",
            "Settings",
            Guid.NewGuid(),
            """{"mode":"old"}""",
            """{"mode":"new"}""",
            "Settings changed",
            "High",
            "192.0.2.5",
            "test-agent");

        created.IsSuccess.Should().BeTrue(created.Error.Description);

        await using (var scope = _serviceProvider.CreateAsyncScope())
        {
            var context = (TestApplicationDbContext)scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var entry = await context.Set<TamperEvidentAuditLog>().SingleAsync();
            context.Entry(entry).Property(nameof(TamperEvidentAuditLog.AfterSnapshot)).CurrentValue = """{"mode":"tampered"}""";
            await context.SaveChangesAsync();
        }

        var integrity = await service.VerifyChainIntegrityAsync(tenantId);

        integrity.IsSuccess.Should().BeTrue();
        integrity.Value.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAuditLogAsync_ShouldFailClosedWhenNoSigningKeyIsConfigured()
    {
        var options = Options.Create(new AuditSigningOptions());
        var service = new TamperEvidentAuditService(
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            new EcdsaCryptographicSigningService(options),
            NullLogger<TamperEvidentAuditService>.Instance);

        var result = await service.CreateAuditLogAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "AdminUpdated",
            "Settings",
            Guid.NewGuid(),
            null,
            null,
            "Settings changed",
            "High",
            "192.0.2.5",
            "test-agent");

        result.IsFailure.Should().BeTrue();
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        (await context.Set<TamperEvidentAuditLog>().CountAsync()).Should().Be(0);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    private TamperEvidentAuditService CreateService()
        => new(
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            _signingService,
            NullLogger<TamperEvidentAuditService>.Instance);

    private static AuditSigningOptions CreateSigningOptions()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return new AuditSigningOptions
        {
            ActiveKeyId = "test-key",
            Keys = new Dictionary<string, AuditSigningKeyOptions>(StringComparer.Ordinal)
            {
                ["test-key"] = new()
                {
                    PrivateKeyPem = key.ExportPkcs8PrivateKeyPem(),
                    PublicKeyPem = key.ExportSubjectPublicKeyInfoPem()
                }
            }
        };
    }
}
