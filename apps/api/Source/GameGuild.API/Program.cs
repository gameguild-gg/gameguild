using GameGuild.Core;

var builder = WebApplication.CreateBuilder(args);

builder.AddAppSettings();
builder.Configuration.AddEnvironmentVariables();

// Add services to the container
// Order matters: Infrastructure -> Application -> Presentation
builder.AddInfrastructureLayer();
builder.AddApplicationLayer();
builder.AddPresentationLayer();

var app = builder.Build();

app.ConfigurePipeline();

// 
// // Configure the HTTP request pipeline
// if (app.Environment.IsDevelopment())
// {
//     app.UseDeveloperExceptionPage();
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }
// else
// {
//     app.UseExceptionHandler("/Error");
//     app.UseHsts();
// }
// 
// app.UseHttpsRedirection();
// app.UseStaticFiles();
// 
// app.UseRouting();
// 
// app.UseCors();
// app.UseAuthentication();
// app.UseAuthorization();
// 
// app.UseRateLimiter();
// 
// app.MapControllers();
// app.MapHealthChecks("/health");

await app.RunAsync().ConfigureAwait(false);

// REMARK: Required for functional and integration tests to work.
namespace GameGuild.API
{
    public class Program { }
}
