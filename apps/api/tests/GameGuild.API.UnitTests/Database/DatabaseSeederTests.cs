using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using GameGuild.LaunchPad;
using GameGuild.Projects;
using GameGuild.TestingLab;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameGuild.API.UnitTests.Database;

public sealed class DatabaseSeederTests
{
    [Fact]
    public async Task SeedAsync_Should_Not_Log_Warnings_When_LegacyIdentityManagers_Are_Not_Registered()
    {
        var services = new ServiceCollection();
        var logger = new CapturingLogger<ApplicationDbContext>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Seed:AdminPassword"] = "UnitTestAdmin123!"
            })
            .Build();

        _ = typeof(GameGuild.Identity.Users.User);

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<ILogger<ApplicationDbContext>>(logger);
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        await using var provider = services.BuildServiceProvider();

        await DatabaseSeeder.SeedAsync(provider);

        logger.Messages.Where(message => message.Level >= LogLevel.Warning).Should().BeEmpty();
    }

    [Fact]
    public async Task SeedAsync_Should_Create_Default_Tenant_And_Admin_SuperAdmin_Membership()
    {
        var services = CreateSeederServices("UnitTestAdmin123!");
        await using var provider = services.BuildServiceProvider();

        await DatabaseSeeder.SeedAsync(provider);
        await DatabaseSeeder.SeedAsync(provider);

        var dbContext = provider.GetRequiredService<ApplicationDbContext>();
        var adminUser = await dbContext.Set<User>().SingleAsync(user => user.Email == "admin@game-guild.com");
        var tenant = await dbContext.Set<Tenant>().SingleAsync(tenant => tenant.IsDefault);
        var membership = await dbContext.Set<TenantMember>()
            .SingleAsync(member => member.UserId == adminUser.Id && member.TenantId == tenant.Id);

        tenant.Slug.Should().Be("gameguild-platform");
        tenant.IsActive.Should().BeTrue();
        membership.Role.Should().Be("SystemAdmin");
        membership.IsActive.Should().BeTrue();

        var settingsCount = await dbContext.Set<TenantSettings>()
            .CountAsync(settings => settings.TenantId == tenant.Id);
        var statisticsCount = await dbContext.Set<TenantStatistics>()
            .CountAsync(statistics => statistics.TenantId == tenant.Id);

        settingsCount.Should().Be(1);
        statisticsCount.Should().Be(1);
    }

    [Fact]
    public async Task SeedAsync_Should_Create_ProjectBacked_TestingLab_And_LaunchPad_Content()
    {
        var services = CreateSeederServices("UnitTestAdmin123!");
        await using var provider = services.BuildServiceProvider();

        await DatabaseSeeder.SeedAsync(provider);
        await DatabaseSeeder.SeedAsync(provider);

        var dbContext = provider.GetRequiredService<ApplicationDbContext>();

        var projects = await dbContext.Set<Project>()
            .Include(project => project.Versions)
            .Where(project => project.Slug.StartsWith("gameguild-showcase-"))
            .ToListAsync();
        projects.Should().HaveCount(3);
        projects.Should().OnlyContain(project => project.Status == ContentStatus.Published);
        projects.Should().OnlyContain(project => project.Visibility == ContentVisibility.Public);
        projects.Should().OnlyContain(project => project.Versions.Count > 0);

        var launchPlans = await dbContext.Set<LaunchPlan>()
            .Include(plan => plan.Project)
            .Include(plan => plan.ChecklistItems)
            .ToListAsync();
        launchPlans.Should().HaveCount(3);
        launchPlans.Should().OnlyContain(plan => plan.Project != null && plan.Project.Slug.StartsWith("gameguild-showcase-"));
        launchPlans.Should().OnlyContain(plan => plan.ChecklistItems.Count >= 5);

        var testingRequests = await dbContext.Set<TestingRequest>()
            .Include(request => request.ProjectVersion)
            .ThenInclude(version => version!.Project)
            .ToListAsync();
        testingRequests.Should().HaveCount(3);
        testingRequests.Should().OnlyContain(request => request.ProjectVersionId.HasValue);
        testingRequests.Should().OnlyContain(request => request.ProjectVersion != null);
        testingRequests.Should().OnlyContain(request => request.ProjectVersion!.Project.Slug.StartsWith("gameguild-showcase-"));

        var locations = await dbContext.Set<TestingLocation>().ToListAsync();
        locations.Should().HaveCountGreaterThanOrEqualTo(2);

        var sessions = await dbContext.Set<TestingSession>()
            .Include(session => session.TestingRequest)
            .Include(session => session.Location)
            .ToListAsync();
        sessions.Should().HaveCount(3);
        sessions.Should().OnlyContain(session => session.TestingRequest != null && session.Location != null);
    }

    private static ServiceCollection CreateSeederServices(string adminPassword)
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Seed:AdminPassword"] = adminPassword
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<ILogger<ApplicationDbContext>, CapturingLogger<ApplicationDbContext>>();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        return services;
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Text)> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add((logLevel, formatter(state, exception)));
        }

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose() { }
        }
    }
}
