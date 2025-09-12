using GameGuild;


var builder = WebApplication.CreateBuilder(args);

builder.AddAppSettings();
builder.Configuration.AddEnvironmentVariables();

// Add services to the container
// Order matters: Infrastructure -> Application -> Presentation.
builder.AddInfrastructureLayer();
builder.AddApplicationLayer();
builder.AddPresentationLayer();

var app = builder.Build();

app.ConfigurePipeline();

await app.RunAsync().ConfigureAwait(false);

// REMARK: Required for functional and integration tests to work.
namespace GameGuild {
  internal partial class Program { };
}
