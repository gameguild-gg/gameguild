using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace GameGuild.AI.UnitTests;

public class AiPromptTemplateServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldNormalizeKey_AndRenderPlaceholders()
    {
        await using var db = CreateDbContext();
        var service = new AiPromptTemplateService(db);
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var created = await service.CreateAsync(
            tenantId,
            userId,
            new CreateAiPromptTemplateRequest(
                "Listing Email!",
                "Listing Email",
                "Hello {{contactName}}, tour {{propertyTitle}}.",
                Category: "Sales",
                SystemPrompt: "Use a {{tone}} tone."),
            CancellationToken.None);

        created.IsSuccess.Should().BeTrue();
        created.Value.Key.Should().Be("listing-email");
        created.Value.TenantId.Should().Be(tenantId);
        created.Value.CreatedByUserId.Should().Be(userId);

        var rendered = await service.RenderAsync(
            tenantId,
            created.Value.Id,
            new Dictionary<string, string?>
            {
                ["contactName"] = "Morgan",
                ["propertyTitle"] = "Pine House",
                ["tone"] = "warm"
            },
            CancellationToken.None);

        rendered.IsSuccess.Should().BeTrue();
        rendered.Value.Prompt.Should().Be("Hello Morgan, tour Pine House.");
        rendered.Value.SystemPrompt.Should().Be("Use a warm tone.");
    }

    [Fact]
    public async Task ListAsync_ShouldReturnSystemAndTenantTemplates_AndExcludeOtherTenants()
    {
        await using var db = CreateDbContext();
        var service = new AiPromptTemplateService(db);
        var tenantId = Guid.NewGuid();

        db.Set<AiPromptTemplate>().AddRange(
            new AiPromptTemplate
            {
                TenantId = null,
                Key = "system-description",
                Name = "System Description",
                Category = "Listings",
                Prompt = "Write a description.",
                IsActive = true,
                IsSystemTemplate = true
            },
            new AiPromptTemplate
            {
                TenantId = tenantId,
                Key = "tenant-email",
                Name = "Tenant Email",
                Category = "Sales",
                Prompt = "Write an email.",
                IsActive = true
            },
            new AiPromptTemplate
            {
                TenantId = Guid.NewGuid(),
                Key = "other-tenant",
                Name = "Other Tenant",
                Category = "Sales",
                Prompt = "Hidden.",
                IsActive = true
            },
            new AiPromptTemplate
            {
                TenantId = tenantId,
                Key = "inactive",
                Name = "Inactive",
                Category = "Sales",
                Prompt = "Inactive.",
                IsActive = false
            });
        await db.SaveChangesAsync();

        var result = await service.ListAsync(tenantId, includeInactive: false, cancellationToken: CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(template => template.Key).Should().BeEquivalentTo("system-description", "tenant-email");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRejectSystemTemplates()
    {
        await using var db = CreateDbContext();
        var service = new AiPromptTemplateService(db);
        var tenantId = Guid.NewGuid();
        var template = new AiPromptTemplate
        {
            TenantId = tenantId,
            Key = "system",
            Name = "System",
            Category = "General",
            Prompt = "System.",
            IsActive = true,
            IsSystemTemplate = true
        };
        db.Set<AiPromptTemplate>().Add(template);
        await db.SaveChangesAsync();

        var result = await service.DeleteAsync(tenantId, template.Id, Guid.NewGuid(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.SystemPromptTemplateReadOnly");
    }

    private static TestAiDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TestAiDbContext>()
            .UseInMemoryDatabase($"ai-prompts-{Guid.NewGuid()}")
            .Options;

        return new TestAiDbContext(options);
    }

    private sealed class TestAiDbContext(DbContextOptions<TestAiDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new AiModelConfiguration().Configure(modelBuilder);
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
