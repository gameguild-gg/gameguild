namespace GameGuild.API.UnitTests.Database;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgreSqlTestCollection
{
    public const string Name = "API unit PostgreSQL";
}
