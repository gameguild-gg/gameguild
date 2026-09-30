using System.Globalization;
using System.Text.Json;
using FluentAssertions;
using GameGuild;
using GameGuild.API;
using GameGuild.API.Database;
using GameGuild.API.UnitTests.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProblemDetailsDetailLevel = GameGuild.Configuration.PresentationLayer.ProblemDetails.ProblemDetailsDetailLevel;
using ProblemDetailsExceptionMapping = GameGuild.Configuration.PresentationLayer.ProblemDetails.ProblemDetailsExceptionMapping;
using ProblemDetailsLocalizedText = GameGuild.Configuration.PresentationLayer.ProblemDetails.ProblemDetailsLocalizedText;
using ProblemDetailsOptions = GameGuild.Configuration.PresentationLayer.ProblemDetails.ProblemDetailsOptions;

namespace GameGuild.API.UnitTests.Core;

public sealed class SchemaNotReadyProblemDetailsTests
{
    [Fact]
    public void IsDatabaseSchemaNotReadyException_ShouldDetectPostgresMissingRelationSqlState()
    {
        InfrastructureServiceCollectionExtensions
            .IsDatabaseSchemaNotReadyException(new FakePostgresException("42P01", "relation \"Users\" does not exist"))
            .Should()
            .BeTrue();
    }

    [Fact]
    public void IsDatabaseSchemaNotReadyException_ShouldDetectMissingRelationMessage()
    {
        InfrastructureServiceCollectionExtensions
            .IsDatabaseSchemaNotReadyException(new InvalidOperationException("relation \"TenantDomains\" does not exist"))
            .Should()
            .BeTrue();
    }

    [Fact]
    public void IsDatabaseSchemaNotReadyException_ShouldDetectNestedMissingRelation()
    {
        var exception = new InvalidOperationException(
            "Outer wrapper",
            new FakePostgresException("42P01", "relation \"AspNetUsers\" does not exist"));

        InfrastructureServiceCollectionExtensions
            .IsDatabaseSchemaNotReadyException(exception)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void IsDatabaseSchemaNotReadyException_ShouldIgnoreOtherExceptions()
    {
        InfrastructureServiceCollectionExtensions
            .IsDatabaseSchemaNotReadyException(new InvalidOperationException("Invalid credentials"))
            .Should()
            .BeFalse();
    }

    [Fact]
    public void SetupProblemDetails_ShouldTranslateMissingSchemaFailuresToServiceUnavailable()
    {
        var services = new ServiceCollection();
        services.SetupProblemDetails(new ConfigurationBuilder().Build(), ProblemDetailsOptions.CreateDefault());
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<Microsoft.AspNetCore.Http.ProblemDetailsOptions>>().Value;
        var httpContext = new DefaultHttpContext { TraceIdentifier = "trace-42" };
        httpContext.Request.Path = "/api/users";
        var context = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails(),
            Exception = new InvalidOperationException("relation users does not exist")
        };

        options.CustomizeProblemDetails!(context);

        context.ProblemDetails.Status.Should().Be(StatusCodes.Status503ServiceUnavailable);
        context.ProblemDetails.Type.Should().Be("urn:problem-type:database-schema-not-ready");
        context.ProblemDetails.Extensions["traceId"].Should().Be("trace-42");
    }

    [Fact]
    public void SetupProblemDetails_ShouldApplyExceptionMappingLocalizationAndCorrelation()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("pt-BR");
            var problemOptions = ProblemDetailsOptions.CreateDefault();
            problemOptions.ExceptionMappings[typeof(ArgumentException).FullName!] = new ProblemDetailsExceptionMapping
            {
                StatusCode = StatusCodes.Status422UnprocessableEntity,
                Type = "urn:problem-type:invalid-input",
                Title = "Invalid input",
                LocalizedMessageKey = "invalid-input",
            };
            problemOptions.LocalizedMessages["pt-BR"] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["invalid-input"] = new ProblemDetailsLocalizedText
                {
                    Title = "Dados inválidos",
                    Detail = "Revise os campos enviados.",
                },
            };
            problemOptions.CustomExtensions["service"] = "gameguild-api";

            var services = new ServiceCollection();
            services.SetupProblemDetails(new ConfigurationBuilder().Build(), problemOptions);
            problemOptions.CustomExtensions["service"] = "changed-after-registration";
            using var provider = services.BuildServiceProvider();
            var configured = provider.GetRequiredService<IOptions<Microsoft.AspNetCore.Http.ProblemDetailsOptions>>().Value;
            var httpContext = new DefaultHttpContext { TraceIdentifier = "trace-91" };
            httpContext.Request.Headers["X-Correlation-ID"] = "request-91";
            var context = new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = new ProblemDetails(),
                Exception = new InvalidOperationException("outer", new ArgumentException("private input")),
            };

            configured.CustomizeProblemDetails!(context);

            context.ProblemDetails.Status.Should().Be(StatusCodes.Status422UnprocessableEntity);
            context.ProblemDetails.Type.Should().Be("urn:problem-type:invalid-input");
            context.ProblemDetails.Title.Should().Be("Dados inválidos");
            context.ProblemDetails.Detail.Should().Be("Revise os campos enviados.");
            context.ProblemDetails.Extensions["traceId"].Should().Be("trace-91");
            context.ProblemDetails.Extensions["correlationId"].Should().Be("request-91");
            context.ProblemDetails.Extensions["service"].Should().Be("gameguild-api");
            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
            httpContext.Response.Headers["X-Correlation-ID"].ToString().Should().Be("request-91");
            context.ProblemDetails.Extensions.Should().NotContainKey("exception");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void SetupProblemDetails_MinimalLevel_ShouldOmitDetailsAndUseTraceIdForInvalidCorrelationHeader()
    {
        var problemOptions = ProblemDetailsOptions.CreateDefault();
        problemOptions.DetailLevel = ProblemDetailsDetailLevel.Minimal;
        var services = new ServiceCollection();
        services.SetupProblemDetails(new ConfigurationBuilder().Build(), problemOptions);
        using var provider = services.BuildServiceProvider();
        var configured = provider.GetRequiredService<IOptions<Microsoft.AspNetCore.Http.ProblemDetailsOptions>>().Value;
        var httpContext = new DefaultHttpContext { TraceIdentifier = "trace-fallback" };
        httpContext.Request.Headers["X-Correlation-ID"] = "first, second";
        var context = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails { Status = StatusCodes.Status500InternalServerError },
            Exception = new InvalidOperationException("secret exception text"),
        };

        configured.CustomizeProblemDetails!(context);

        context.ProblemDetails.Detail.Should().BeNull();
        context.ProblemDetails.Extensions["correlationId"].Should().Be("trace-fallback");
        context.ProblemDetails.Extensions.Should().NotContainKey("exception");
    }

    [Theory]
    [InlineData("Production", false)]
    [InlineData("Development", true)]
    public void SetupProblemDetails_ShouldOnlyIncludeExceptionDetailsInDevelopment(
        string environmentName,
        bool expectedToIncludeException)
    {
        var problemOptions = ProblemDetailsOptions.CreateDefault();
        problemOptions.DetailLevel = ProblemDetailsDetailLevel.Detailed;
        problemOptions.IncludeExceptionDetails = true;

        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(value => value.EnvironmentName).Returns(environmentName);
        var services = new ServiceCollection();
        services.AddSingleton(environment.Object);
        services.SetupProblemDetails(new ConfigurationBuilder().Build(), problemOptions);
        using var provider = services.BuildServiceProvider();
        var configured = provider.GetRequiredService<IOptions<Microsoft.AspNetCore.Http.ProblemDetailsOptions>>().Value;
        var httpContext = new DefaultHttpContext { RequestServices = provider };
        var context = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails { Status = StatusCodes.Status500InternalServerError },
            Exception = new InvalidOperationException("private exception detail"),
        };

        configured.CustomizeProblemDetails!(context);

        context.ProblemDetails.Extensions.ContainsKey("exception").Should().Be(expectedToIncludeException);
    }

    [Fact]
    public void SetupProblemDetails_ShouldMapCommonClientInputExceptions()
    {
        var services = new ServiceCollection();
        services.SetupProblemDetails(new ConfigurationBuilder().Build(), ProblemDetailsOptions.CreateDefault());
        using var provider = services.BuildServiceProvider();
        var configured = provider.GetRequiredService<IOptions<Microsoft.AspNetCore.Http.ProblemDetailsOptions>>().Value;

        var argumentContext = new ProblemDetailsContext
        {
            HttpContext = new DefaultHttpContext(),
            ProblemDetails = new ProblemDetails { Status = StatusCodes.Status500InternalServerError },
            Exception = new ArgumentNullException("name"),
        };
        configured.CustomizeProblemDetails!(argumentContext);

        argumentContext.ProblemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
        argumentContext.ProblemDetails.Type.Should().Be("https://api.gameguild.gg/problems/invalid-argument");
        argumentContext.ProblemDetails.Detail.Should().Be("The request contains an invalid argument.");

        var jsonContext = new ProblemDetailsContext
        {
            HttpContext = new DefaultHttpContext(),
            ProblemDetails = new ProblemDetails { Status = StatusCodes.Status500InternalServerError },
            Exception = new JsonException("private parser detail"),
        };
        configured.CustomizeProblemDetails!(jsonContext);

        jsonContext.ProblemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
        jsonContext.ProblemDetails.Type.Should().Be("https://api.gameguild.gg/problems/invalid-json");
        jsonContext.ProblemDetails.Detail.Should().Be("The request body contains invalid JSON.");
    }

    [Fact]
    public async Task ExceptionHandlingMiddleware_ShouldUseConfiguredExceptionMappings()
    {
        var problemOptions = ProblemDetailsOptions.CreateDefault();
        problemOptions.ExceptionMappings[typeof(InvalidOperationException).FullName!] = new ProblemDetailsExceptionMapping
        {
            StatusCode = StatusCodes.Status503ServiceUnavailable,
            Type = "urn:problem-type:service-unavailable",
            Title = "Service unavailable",
            Detail = "The service is temporarily unavailable.",
        };
        var services = new ServiceCollection();
        services.AddLogging();
        services.SetupProblemDetails(new ConfigurationBuilder().Build(), problemOptions);
        using var provider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = provider,
            TraceIdentifier = "trace-middleware-1",
        };
        httpContext.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("private exception detail"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(httpContext);

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        httpContext.Response.ContentType.Should().StartWith("application/problem+json");
        httpContext.Response.Body.Position = 0;
        using var json = await JsonDocument.ParseAsync(httpContext.Response.Body);
        json.RootElement.GetProperty("type").GetString().Should().Be("urn:problem-type:service-unavailable");
        json.RootElement.GetProperty("title").GetString().Should().Be("Service unavailable");
        json.RootElement.GetProperty("detail").GetString().Should().Be("The service is temporarily unavailable.");
        json.RootElement.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status503ServiceUnavailable);
        json.RootElement.GetProperty("traceId").GetString().Should().Be("trace-middleware-1");
    }

    [Fact]
    public async Task DatabaseReadinessHealthCheck_WithInMemoryDatabase_ShouldReportHealthy()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(options);

        var result = await new DatabaseReadinessHealthCheck(context)
            .CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data["pendingMigrations"].Should().Be(0);
    }

    [Fact]
    public async Task DatabaseReadinessHealthCheck_WithPendingRelationalMigration_ShouldReportDegradedThenHealthy()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .ReplaceService<IMigrationsAssembly, CoverageMigrationsAssembly>()
            .Options;
        await using var context = new ApplicationDbContext(options);
        var healthCheck = new DatabaseReadinessHealthCheck(context);

        var pending = await healthCheck.CheckHealthAsync(new HealthCheckContext());
        await context.Database.MigrateAsync();
        var current = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        pending.Status.Should().Be(HealthStatus.Degraded);
        pending.Data["pendingMigrations"].Should().Be(1);
        current.Status.Should().Be(HealthStatus.Healthy);
        current.Data["appliedMigrations"].Should().Be(1);
    }

    [Fact]
    public async Task DatabaseReadinessHealthCheck_WhenDatabaseCannotBeOpened_ShouldReportUnreachable()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.db");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={path};Mode=ReadOnly")
            .Options;
        await using var context = new ApplicationDbContext(options);

        var result = await new DatabaseReadinessHealthCheck(context)
            .CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Application database is unreachable.");
    }

    [Fact]
    public async Task DatabaseReadinessHealthCheck_WhenCheckThrows_ShouldReportFailure()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options);
        await context.DisposeAsync();

        var result = await new DatabaseReadinessHealthCheck(context)
            .CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Application database health check failed.");
        result.Exception.Should().BeOfType<ObjectDisposedException>();
    }

    private sealed class FakePostgresException(string sqlState, string message) : Exception(message)
    {
        public string SqlState { get; } = sqlState;
    }
}
