using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.Resources;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.API.UnitTests.Database;

public sealed class ApplicationDbContextModuleConfigurationTests
{
    [Fact]
    public void Model_Includes_Resources_Quota_And_Usage_Entities()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.Model.FindEntityType(typeof(ResourceQuota)).Should().NotBeNull();
        context.Model.FindEntityType(typeof(UsageRecord)).Should().NotBeNull();
    }
}
