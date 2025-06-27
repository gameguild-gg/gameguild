using GameGuild.Data;
using GameGuild.Modules.User.GraphQL;
using GameGuild.Common.Extensions;
using GameGuild.Common.Middleware;
using GameGuild.Common.Transformers;
using GameGuild.Modules.Auth.Configuration;
using GameGuild.Config;
using GameGuild.Common.GraphQL.Extensions;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using MediatR;


var builder = WebApplication.CreateBuilder(args);

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
  }
);

// Add common services and modules
builder.Services.AddCommonServices();
builder.Services.AddUserModule();
builder.Services.AddTenantModule();
builder.Services.AddUserProfileModule(); // Register the UserProfile module
builder.Services.AddProgramModule(); // Register the Program module
builder.Services.AddProjectModule(); // Register the Project module
builder.Services.AddTestModule(); // Register the Test module
builder.Services.AddAuthModule(builder.Configuration); // Register the Auth module

// Add database seeder
builder.Services.AddScoped<GameGuild.Common.Services.IDatabaseSeeder, GameGuild.Common.Services.DatabaseSeeder>();

// Add HTTP context accessor for GraphQL authorization
builder.Services.AddHttpContextAccessor();

// Add MediatR for CQRS
builder.Services.AddMediatR(typeof(Program));

// Add MediatR pipeline behaviors
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(GameGuild.Common.Behaviors.LoggingBehavior<,>));

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
       .AddTypeExtension<GameGuild.Modules.Tenant.GraphQL.TenantQueries>()
       .AddTypeExtension<GameGuild.Modules.Tenant.GraphQL.TenantMutations>()
       .AddTypeExtension<GameGuild.Modules.UserProfile.GraphQL.UserProfileQueries>()
       .AddTypeExtension<GameGuild.Modules.UserProfile.GraphQL.UserProfileMutations>()
       .AddTypeExtension<GameGuild.Modules.Auth.GraphQL.AuthQueries>()
       .AddTypeExtension<GameGuild.Modules.Auth.GraphQL.AuthMutations>()
       .AddTypeExtension<GameGuild.Modules.Project.GraphQL.ProjectQueries>()
       .AddTypeExtension<GameGuild.Modules.Project.GraphQL.ProjectMutations>()
       .AddTypeExtension<GameGuild.Modules.TestingLab.GraphQL.TestingLabQueries>()
       .AddTypeExtension<GameGuild.Modules.TestingLab.GraphQL.TestingLabMutations>()
       .AddTypeExtension<GameGuild.Modules.Program.GraphQL.ProgramContentQueries>()
       .AddTypeExtension<GameGuild.Modules.Program.GraphQL.ProgramContentMutations>()
       .AddType<UserType>()
       .AddType<CredentialType>()
       .AddType<GameGuild.Modules.Tenant.GraphQL.TenantType>()
       .AddType<GameGuild.Modules.Tenant.GraphQL.TenantPermissionType>()
       .AddType<GameGuild.Modules.UserProfile.GraphQL.UserProfileType>()
       .AddType<GameGuild.Modules.Project.GraphQL.ProjectType>()
       .AddType<GameGuild.Modules.Program.GraphQL.ProgramContentType>()
       .AddType<GameGuild.Modules.TestingLab.GraphQL.TestingRequestType>()
       .AddType<GameGuild.Modules.TestingLab.GraphQL.TestingSessionType>()
       .AddType<GameGuild.Modules.TestingLab.GraphQL.TestingParticipantType>()
       .AddType<GameGuild.Modules.TestingLab.GraphQL.TestingLocationType>()
       .AddType<GameGuild.Modules.Program.GraphQL.ProgramContentType>()
       .ModifyOptions(opt => { opt.RemoveUnreachableTypes = true; })
       .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = builder.Environment.IsDevelopment());

var app = builder.Build();

// Add exception handling middleware (similar to NestJS global filters)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Automatically apply pending migrations and create a database if it doesn't exist
using (var scope = app.Services.CreateScope()) {
  var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
  var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

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
    var seeder = scope.ServiceProvider.GetRequiredService<GameGuild.Common.Services.IDatabaseSeeder>();
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

app.Run();

// Make Program class accessible for testing
public partial class Program { }
