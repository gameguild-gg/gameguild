using FluentAssertions;

namespace GameGuild.API.UnitTests.Core;

public sealed class DockerDeploymentContractTests
{
    [Fact]
    public void ApiImage_UsesMinimalBuildContextAndHardenedRuntime()
    {
        var repositoryRoot = FindRepositoryRoot();
        var dockerfile = File.ReadAllText(Path.Combine(repositoryRoot, "apps", "api", "Dockerfile"));

        dockerfile.Should().Contain("COPY apps/api/GameGuild.sln apps/api/Directory.Build.props apps/api/Directory.Packages.props apps/api/global.json ./");
        dockerfile.Should().Contain("COPY apps/api/Source/ Source/");
        dockerfile.Should().NotContain("COPY . .");
        dockerfile.Should().Contain("--disable-parallel");
        dockerfile.Should().Contain("-m:1");
        dockerfile.Should().Contain("USER appuser");
        dockerfile.Should().Contain("EXPOSE 8080");
        dockerfile.Should().Contain("ASPNETCORE_HTTP_PORTS=8080");
        dockerfile.Should().Contain("DATAPROTECTION_KEYS_PATH=/app/.aspnet/DataProtection-Keys");
        dockerfile.Should().Contain("http://127.0.0.1:8080/live");
        dockerfile.Should().NotContain("EXPOSE 3000");
    }

    [Fact]
    public void LocalCompose_ProvidesDevelopmentInfrastructure()
    {
        var repositoryRoot = FindRepositoryRoot();
        var compose = File.ReadAllText(Path.Combine(repositoryRoot, "compose.yaml")).ReplaceLineEndings("\n");
        var apiSection = SliceService(compose, "api", "web");

        compose.Should().Contain("name: web-development-public");
        compose.Should().Contain("external: true");
        compose.Should().Contain("  postgres:\n");
        compose.Should().Contain("  redis:\n");
        compose.Should().Contain("  garage:\n");
        compose.Should().Contain("  garage-init:\n");
        compose.Should().NotContain("mailhog");
        apiSection.Should().Contain("context: .");
        apiSection.Should().Contain("dockerfile: apps/api/Dockerfile");
        apiSection.Should().Contain("depends_on:");
        apiSection.Should().Contain("ConnectionStrings__MigrationConnection");
        apiSection.Should().Contain("Database__GrantRuntimeRoleAfterMigrations");
        apiSection.Should().Contain("Database__FailStartupOnMigrationFailure");
        apiSection.Should().Contain("Database__FailStartupOnSeedFailure");
        apiSection.Should().Contain("Database__FailStartupOnGrantFailure");
        apiSection.Should().Contain("Redis__ConnectionString");
        apiSection.Should().Contain("EmailDelivery__Ses__Region");
        apiSection.Should().Contain("Assets__Storage__ServiceUrl");
        apiSection.Should().Contain(":/app/.aspnet/DataProtection-Keys");
        apiSection.Should().Contain("no-new-privileges:true");
        apiSection.Should().Contain("http://localhost:8080/live");
    }

    [Fact]
    public void CoolifyCompose_FailsClosedAndRequiresOperationalProviders()
    {
        var repositoryRoot = FindRepositoryRoot();
        var compose = File.ReadAllText(Path.Combine(repositoryRoot, "compose.coolify.yaml"));
        var apiSection = SliceService(compose, "backend", "web");

        apiSection.Should().Contain("- '8080'");
        apiSection.Should().Contain("ASPNETCORE_HTTP_PORTS=8080");
        apiSection.Should().Contain("Database__GrantRuntimeRoleAfterMigrations=true");
        apiSection.Should().Contain("Database__FailStartupOnMigrationFailure=true");
        apiSection.Should().Contain("Database__FailStartupOnSeedFailure=true");
        apiSection.Should().Contain("Database__FailStartupOnGrantFailure=true");
        apiSection.Should().Contain("REDIS_CONNECTION_STRING:?set REDIS_CONNECTION_STRING");
        apiSection.Should().Contain("EMAILDELIVERY__FROMEMAIL:?set EMAILDELIVERY__FROMEMAIL");
        apiSection.Should().Contain("EMAILDELIVERY__SES__REGION:?set EMAILDELIVERY__SES__REGION");
        apiSection.Should().Contain("S3_SERVICE_URL:?set S3_SERVICE_URL");
        apiSection.Should().Contain(":/app/.aspnet/DataProtection-Keys");
        apiSection.Should().Contain("http://127.0.0.1:8080/live");
        compose.Should().NotContain("backend:3000");
    }

    private static string SliceService(string compose, string service, string nextService)
    {
        compose = compose.ReplaceLineEndings("\n");
        var start = compose.IndexOf($"  {service}:\n", StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0);

        var end = compose.IndexOf($"  {nextService}:\n", start, StringComparison.Ordinal);
        end.Should().BeGreaterThan(start);

        return compose[start..end];
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory != null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "package.json")) &&
                Directory.Exists(Path.Combine(directory.FullName, "apps", "api")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
