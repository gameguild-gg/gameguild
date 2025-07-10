using DotNetEnv;
using GameGuild.Common;
using GameGuild.Common.Extensions;
using GameGuild.Common.GraphQL;
using GameGuild.Database;
using GameGuild.Modules.Authentication;
using GameGuild.Modules.Credentials;
using GameGuild.Modules.Programs.GraphQL;
using GameGuild.Modules.Projects;
using GameGuild.Modules.Tenants;
using GameGuild.Modules.TestingLab;
using GameGuild.Modules.UserProfiles;
using GameGuild.Modules.Users;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using ProgramContentType = GameGuild.Modules.Programs.GraphQL.ProgramContentType;
using ProjectType = GameGuild.Modules.Projects.ProjectType;
using ToKebabParameterTransformer = GameGuild.Common.ToKebabParameterTransformer;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));

// Load environment variables from .env file
Env.Load();

// Setup configuration sources
builder.Configuration.SetBasePath(AppContext.BaseDirectory)
       .AddJsonFile("appsettings.json", true, true)
       .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, true)
       .AddEnvironmentVariables();

// Add configuration services
builder.Services.AddAppConfiguration(builder.Configuration);

// Configure CORS options from appsettings
var corsOptions =
  builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();

// Add CORS services
builder.Services.AddCors(options => {
    if (builder.Environment.IsDevelopment())
      // Allow all origins in development for easier testing
      options.AddPolicy("Development", policy => { policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader(); });
    else
      // Use configured origins in production
      options.AddPolicy(
        "Production",
        policy => {
          policy.WithOrigins(corsOptions.AllowedOrigins)
                .WithMethods(corsOptions.AllowedMethods)
                .WithHeaders(corsOptions.AllowedHeaders);

          if (corsOptions.AllowCredentials) policy.AllowCredentials();
        }
      );

    // Default policy for specific origins (can be used in development too)
    options.AddPolicy(
      "Configured",
      policy => {
        policy.WithOrigins(corsOptions.AllowedOrigins)
              .WithMethods(corsOptions.AllowedMethods)
              .WithHeaders(corsOptions.AllowedHeaders);

        if (corsOptions.AllowCredentials) policy.AllowCredentials();
      }
    );
  }
);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services
       .AddControllers(opts => opts.Conventions.Add(new RouteTokenTransformerConvention(new ToKebabParameterTransformer())))
       .AddAuthFilters(); // Add authentication filters

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc(
      "v1",
      new OpenApiInfo { Title = "GameGuild CMS API", Version = "v1", Description = "A Content Management System API for GameGuild" }
    );

    // Add JWT authentication support to Swagger
    c.AddSecurityDefinition(
      "Bearer",
      new OpenApiSecurityScheme {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token in the format: Bearer {your token}",
      }
    );

    // Configure security requirements based on the [Public] attribute
    c.OperationFilter<AuthOperationFilter>();
  }
);

// Configure Clean Architecture layers
builder.Services.AddPresentation();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Get connection string from the environment
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ??
                       throw new InvalidOperationException(
                         "DB_CONNECTION_STRING environment variable is not set. Please check your .env file or environment configuration."
                       );

// Check if we should use the in-memory database (for tests)
var useInMemoryDb = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("USE_IN_MEMORY_DB")) &&
                    Environment.GetEnvironmentVariable("USE_IN_MEMORY_DB")!.Equals(
                      "true",
                      StringComparison.OrdinalIgnoreCase
                    );

// Add Entity Framework with the appropriate provider
builder.Services.AddDbContext<ApplicationDbContext>(options => {
    if (useInMemoryDb)
      // Use InMemory for tests
      options.UseInMemoryDatabase("TestDatabase_" + Guid.NewGuid().ToString());
    else
      // Use SQLite for regular development
      options.UseSqlite(connectionString);

    // Suppress SQLite pragma warnings
    options.ConfigureWarnings(warnings =>
                                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)
    );

    // Enable sensitive data logging in development
    if (!builder.Environment.IsDevelopment()) return;

    options.EnableSensitiveDataLogging();
    options.EnableDetailedErrors();
  }
);

// Add DAC authorization services for GraphQL
builder.Services.AddDACAuthorizationServices();

// Add GraphQL services
builder.Services.AddGraphQLServer()
       .AddQueryType<Query>()
       .AddMutationType<Mutation>()
       .AddDACAuthorization() // Add 3-layer DAC authorization
       .AddTypeExtension<TenantQueries>()
       // .AddTypeExtension<GameGuild.Modules.Tenants.GraphQL.TenantMutations>()
       .AddTypeExtension<UserQueries>()
       .AddTypeExtension<UserMutations>()
       .AddTypeExtension<UserProfileQueries>()
       .AddTypeExtension<UserProfileMutations>()
       .AddTypeExtension<AuthQueries>()
       .AddTypeExtension<AuthMutations>()
       .AddTypeExtension<ProjectQueries>()
       .AddTypeExtension<ProjectMutations>()
       .AddTypeExtension<TestingLabQueries>()
       .AddTypeExtension<TestingLabMutations>()
       .AddTypeExtension<ProgramContentQueries>()
       .AddTypeExtension<ProgramContentMutations>()
       .AddTypeExtension<ContentInteractionQueries>()
       .AddTypeExtension<ContentInteractionMutations>()
       .AddTypeExtension<ActivityGradeQueries>()
       .AddTypeExtension<ActivityGradeMutations>()
       .AddType<UserType>()
       .AddType<CredentialType>()
       .AddType<TenantType>()
       .AddType<TenantPermissionType>()
       .AddType<UserProfileType>()
       .AddType<ProjectType>()
       .AddType<ProgramContentType>()
       .AddType<ContentInteractionType>()
       .AddType<ActivityGradeType>()
       .AddType<TestingRequestType>()
       .AddType<TestingSessionType>()
       .AddType<TestingParticipantType>()
       .AddType<TestingLocationType>()
       .ModifyOptions(opt => { opt.RemoveUnreachableTypes = true; })
       .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = builder.Environment.IsDevelopment());

var app = builder.Build();

app.MapEndpoints();

app.UseRequestContextLogging();

app.UseExceptionHandler();

// Automatically apply pending migrations and create a database if it doesn't exist
using (var scope = app.Services.CreateScope()) {
  var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
  var logger = scope.ServiceProvider.GetRequiredService<ILogger<GameGuild.Program>>();

  try {
    // Only run migrations on relational databases (not on InMemory)
    if (!context.Database.IsInMemory()) {
      logger.LogInformation("Applying database migrations...");
      context.Database.Migrate();
      logger.LogInformation("Database migrations applied successfully");
    }
    else {
      logger.LogInformation("Using in-memory database, skipping migrations");
      // Create database schema for InMemory database
      context.Database.EnsureCreated();
    }

    // Seed initial data
    var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();
    await seeder.SeedAsync();
  }
  catch (Exception ex) {
    logger.LogError(ex, "An error occurred while applying database migrations or seeding data");

    throw; // Rethrow to fail startup if migrations fail
  }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
  app.MapOpenApi();
  app.UseSwagger();

  app.UseSwaggerUI(c => {
      c.SwaggerEndpoint("/swagger/v1/swagger.json", "GameGuild CMS API v1");
      c.RoutePrefix = "swagger"; // Serve Swagger UI at /swagger
    }
  );
}

app.UseHttpsRedirection();

// Add CORS middleware (must be before authentication and authorization)
app.UseCors(builder.Environment.IsDevelopment() ? "Development" : "Production");

// Add authentication middleware
app.UseAuthModule();

// Map GraphQL endpoint
app.MapGraphQL();

app.MapControllers();

await app.RunAsync();

// REMARK: Required for functional and integration tests to work.
namespace GameGuild {
  public partial class Program;
}
