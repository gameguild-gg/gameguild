using GameGuild.Common;


// Modern .NET top-level program with fluent configuration
var app = await WebApplication
                .CreateBuilder(args)
                .ConfigureGameGuildApplication()
                .BuildWithPipelineAsync();

await app.RunAsync();

// REMARK: Required for functional and integration tests to work.
// namespace GameGuild {
//   public partial class Program;
// }
