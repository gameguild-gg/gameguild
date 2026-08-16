using FluentAssertions;
using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.API.UnitTests.Database;

public sealed class MigrationDeploymentContractTests
{
    [Fact]
    public void MigrationCatalog_PreservesTheDeployedReconciliationIdentity()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused")
            .Options;

        using var context = new ApplicationDbContext(options);
        var migrations = context.Database.GetMigrations().ToArray();

        migrations.Should().Contain("20260814193823_ReconcileApplicationModel");
        migrations.Should().NotContain("20260815011232_ReconcileApplicationModel");
        migrations.Should().OnlyHaveUniqueItems();
    }
}
