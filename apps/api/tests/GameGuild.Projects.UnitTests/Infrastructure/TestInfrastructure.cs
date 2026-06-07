using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using GameGuild.Projects;
using GameGuild.Identity.Users;
using GameGuild.API.Database;

namespace GameGuild.Projects.UnitTests.Infrastructure;

/// <summary>
/// Test database context for Projects unit tests - uses simplified in-memory context
/// </summary>
public class TestProjectsDbContext : DbContext, IApplicationDbContext
{
    public TestProjectsDbContext(DbContextOptions<TestProjectsDbContext> options) : base(options)
    {
    }

    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<ProjectCategory> ProjectCategories { get; set; } = null!;
    public DbSet<ProjectCollaborator> ProjectCollaborators { get; set; } = null!;
    public DbSet<ProjectFeedback> ProjectFeedbacks { get; set; } = null!;
    public DbSet<ProjectFollower> ProjectFollowers { get; set; } = null!;
    public DbSet<ProjectMetadata> ProjectMetadata { get; set; } = null!;
    public DbSet<ProjectRelease> ProjectReleases { get; set; } = null!;
    public DbSet<ProjectTeam> ProjectTeams { get; set; } = null!;
    public DbSet<ProjectVersion> ProjectVersions { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => Database.BeginTransactionAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply configurations for testing
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Slug).HasMaxLength(500).IsRequired();
            entity.Property(e => e.ShortDescription).HasMaxLength(500);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
        });

        modelBuilder.Entity<ProjectCategory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<ProjectCollaborator>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<ProjectMetadata>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
        });

        // Add other entity configurations as needed
        base.OnModelCreating(modelBuilder);
    }
}

/// <summary>
/// Factory for creating test database contexts
/// </summary>
public static class TestDbContextFactory
{
    public static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}

/// <summary>
/// Test data builder for creating test entities
/// </summary>
public class TestDataBuilder
{
    private readonly Fixture _fixture;

    public TestDataBuilder()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    public User CreateUser(string? email = null, string? firstName = null, string? lastName = null)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email ?? _fixture.Create<string>() + "@test.com",
            Name = $"{firstName ?? _fixture.Create<string>()} {lastName ?? _fixture.Create<string>()}".Trim()
        };
    }

    public Project CreateProject(Guid? createdById = null, string? title = null, ProjectType? type = null)
    {
        var userId = createdById ?? Guid.NewGuid();
        return new Project
        {
            Id = Guid.NewGuid(),
            Title = title ?? _fixture.Create<string>(),
            Slug = (title ?? _fixture.Create<string>()).ToLowerInvariant().Replace(" ", "-"),
            ShortDescription = _fixture.Create<string>(),
            Description = _fixture.Create<string>(),
            Type = type ?? ProjectType.Game,
            DevelopmentStatus = DevelopmentStatus.InDevelopment,
            Status = ContentStatus.Published,
            Visibility = ContentVisibility.Public,
            CreatedById = userId
        };
    }

    public ProjectCategory CreateProjectCategory(string? name = null)
    {
        return new ProjectCategory
        {
            Id = Guid.NewGuid(),
            Name = name ?? _fixture.Create<string>()
        };
    }
}
