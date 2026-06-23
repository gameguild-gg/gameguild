using FluentAssertions;
using GameGuild.API;

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

    private sealed class FakePostgresException(string sqlState, string message) : Exception(message)
    {
        public string SqlState { get; } = sqlState;
    }
}
