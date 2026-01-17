using FluentAssertions;
using GameGuild.Identity.Tenants.UnitTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Repositories;

public class TenantMetadataRepositoryTests
{
    [Fact]
    public async Task Add_Get_Update_Delete_Should_Work()
    {
        await using var context = CreateContext();
        var repo = new TenantMetadataRepository(context);

        var tenantId = Guid.NewGuid();
        var metadata = new TenantMetadata { TenantId = tenantId, Industry = "Gaming", Type = "Studio", Size = TenantSize.Small };
        metadata.SetTags(["pro"]);

        context.TenantMetadata.Add(metadata);
        await context.SaveChangesAsync();

        var fetched = await repo.GetByTenantIdAsync(tenantId);
        fetched.Should().NotBeNull();

        (await repo.GetByIdAsync(fetched!.Id)).Should().NotBeNull();

        fetched!.Industry.Should().Be("Gaming");
        fetched.UpdateNotes("note");
        await repo.UpdateAsync(fetched);
        await repo.SaveChangesAsync();

        await repo.DeleteAsync(fetched);
        await repo.SaveChangesAsync();

        fetched.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByFilters_Should_Return_Matches()
    {
        await using var context = CreateContext();
        var repo = new TenantMetadataRepository(context);

        var meta = new TenantMetadata { TenantId = Guid.NewGuid(), Industry = "Gaming", Type = "Studio", Size = TenantSize.Medium };
        meta.SetTags(["alpha", "beta"]);
        context.TenantMetadata.Add(meta);
        await context.SaveChangesAsync();

        (await repo.GetByIndustryAsync("Gaming")).Should().ContainSingle();
        (await repo.GetBySizeAsync(TenantSize.Medium)).Should().ContainSingle();
        (await repo.GetByTypeAsync("Studio")).Should().ContainSingle();
        (await repo.GetByTagsAsync(["alpha"]))
            .Should().ContainSingle();
    }

    private static TestTenantDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestTenantDbContext>()
            .UseInMemoryDatabase($"TenantMetadataRepo_{Guid.NewGuid()}")
            .Options;
        return new TestTenantDbContext(options);
    }
}
