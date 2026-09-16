using FluentAssertions;
using GameGuild.Identity.Tenants;
using GameGuild.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingLabSettingsServiceCoverageTests
{
    [Fact]
    public async Task GlobalSettings_AreCreatedMappedAndReportedAsExisting()
    {
        await using var context = CreateContext();
        var service = new TestingLabSettingsService(context);

        (await service.TestingLabSettingsExistAsync()).Should().BeFalse();
        var settings = await service.GetTestingLabSettingsAsync();
        settings.IsGlobal.Should().BeTrue();
        settings.LabName.Should().Be("Testing Lab");
        settings.Timezone.Should().Be("UTC");
        settings.DefaultSessionDuration.Should().Be(60);
        settings.AllowPublicSignups.Should().BeTrue();
        settings.RequireApproval.Should().BeTrue();
        settings.EnableNotifications.Should().BeTrue();
        settings.MaxSimultaneousSessions.Should().Be(10);
        settings.VersionSubmissionPolicy.Should().Be(VersionSubmissionPolicy.ReadyMutableUntilReview);
        (await service.TestingLabSettingsExistAsync()).Should().BeTrue();

        var dto = await service.GetTestingLabSettingsDtoAsync();
        dto.Should().BeEquivalentTo(settings, options => options.ExcludingMissingMembers());
        dto.TenantId.Should().BeNull();
    }

    [Fact]
    public async Task CreateOrUpdate_CreatesTenantSettingsThenUpdatesTheExistingRow()
    {
        await using var context = CreateContext();
        var tenant = NewTenant();
        context.Set<Tenant>().Add(tenant);
        await context.SaveChangesAsync();
        var service = new TestingLabSettingsService(context);
        var create = CompleteCreateDto("Creator Lab");

        var created = await service.CreateOrUpdateTestingLabSettingsAsync(tenant.Id, create);
        created.Tenant.Should().BeSameAs(tenant);
        created.LabName.Should().Be("Creator Lab");
        created.ReminderDaysBefore.Should().Be("7,1");

        var update = CompleteCreateDto("Updated Lab");
        update.DefaultSessionDuration = 120;
        var existing = await service.CreateOrUpdateTestingLabSettingsAsync(tenant.Id, update);
        existing.Should().BeSameAs(created);
        existing.LabName.Should().Be("Updated Lab");
        existing.DefaultSessionDuration.Should().Be(120);
        (await service.GetTestingLabSettingsDtoAsync(tenant.Id)).TenantId.Should().Be(tenant.Id);

        await FluentActions.Awaiting(() => service.CreateOrUpdateTestingLabSettingsAsync(tenant.Id, null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task PartialUpdate_AppliesOnlyProvidedValues()
    {
        await using var context = CreateContext();
        var service = new TestingLabSettingsService(context);
        var settings = await service.GetTestingLabSettingsAsync();
        var update = new UpdateTestingLabSettingsDto
        {
            LabName = "Focused Lab",
            Description = "Private beta",
            Timezone = "America/Sao_Paulo",
            DefaultSessionDuration = 45,
            AllowPublicSignups = false,
            RequireApproval = false,
            EnableNotifications = false,
            MaxSimultaneousSessions = 3,
            VersionSubmissionPolicy = VersionSubmissionPolicy.ReleasedImmutable
        };

        var updated = await service.UpdateTestingLabSettingsAsync(null, update);
        updated.LabName.Should().Be("Focused Lab");
        updated.Description.Should().Be("Private beta");
        updated.Timezone.Should().Be("America/Sao_Paulo");
        updated.DefaultSessionDuration.Should().Be(45);
        updated.AllowPublicSignups.Should().BeFalse();
        updated.RequireApproval.Should().BeFalse();
        updated.EnableNotifications.Should().BeFalse();
        updated.MaxSimultaneousSessions.Should().Be(3);
        updated.VersionSubmissionPolicy.Should().Be(VersionSubmissionPolicy.ReleasedImmutable);

        (await service.UpdateTestingLabSettingsAsync(null, new UpdateTestingLabSettingsDto()))
            .Should().BeSameAs(settings);
        await FluentActions.Awaiting(() => service.UpdateTestingLabSettingsAsync(null, null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Reset_RestoresExistingSettingsOrCreatesMissingDefaults()
    {
        await using var context = CreateContext();
        var service = new TestingLabSettingsService(context);
        var existing = await service.CreateOrUpdateTestingLabSettingsAsync(null, CompleteCreateDto("Changed"));

        var reset = await service.ResetTestingLabSettingsAsync();
        reset.Should().BeSameAs(existing);
        reset.LabName.Should().Be("Testing Lab");
        reset.Description.Should().Be("Default testing lab settings");
        reset.Timezone.Should().Be("UTC");
        reset.DefaultSessionDuration.Should().Be(60);
        reset.AllowPublicSignups.Should().BeTrue();
        reset.RequireApproval.Should().BeTrue();
        reset.EnableNotifications.Should().BeTrue();
        reset.MaxSimultaneousSessions.Should().Be(10);
        reset.VersionSubmissionPolicy.Should().Be(VersionSubmissionPolicy.ReadyMutableUntilReview);

        await using var emptyContext = CreateContext();
        var created = await new TestingLabSettingsService(emptyContext).ResetTestingLabSettingsAsync();
        created.LabName.Should().Be("Testing Lab");
    }

    private static CreateTestingLabSettingsDto CompleteCreateDto(string name) => new()
    {
        LabName = name,
        Description = "Description",
        Timezone = "UTC",
        DefaultSessionDuration = 90,
        AllowPublicSignups = false,
        RequireApproval = false,
        EnableNotifications = false,
        ReminderDaysBefore = "7,1",
        MaxSimultaneousSessions = 4,
        VersionSubmissionPolicy = VersionSubmissionPolicy.ReadyMutableUntilReview
    };

    private static Tenant NewTenant() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Creator workspace",
        Slug = $"creator-{Guid.NewGuid():N}"
    };

    private static SettingsContext CreateContext() => new(
        new DbContextOptionsBuilder<SettingsContext>()
            .UseInMemoryDatabase($"testing-lab-settings-{Guid.NewGuid():N}")
            .Options);

    private sealed class SettingsContext(DbContextOptions<SettingsContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<TestingLabSettings> TestingLabSettings => Set<TestingLabSettings>();

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
