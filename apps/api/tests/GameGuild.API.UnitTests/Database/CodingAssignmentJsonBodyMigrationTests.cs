using System.Text.Json;
using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.API.Database.Migrations;
using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql;

namespace GameGuild.API.UnitTests.Database;

[Collection(PostgreSqlTestCollection.Name)]
public sealed class CodingAssignmentJsonBodyMigrationTests
{
    // v1 DTOs serialize PascalCase (per Learning.Courses convention — no naming policy).
    private static readonly JsonSerializerOptions s_v1Default = new() { WriteIndented = false };

    [Fact]
    public async Task Up_BackfillsV2CodingDefinition_AsV1CodingAssignmentContent()
    {
        await using var container = await StartPostgresAsync();
        await using var connection = new NpgsqlConnection(container.ConnectionString);
        await connection.OpenAsync();
        await CreateSchemaAsync(connection);

        var contentId = Guid.NewGuid();

        await InsertAssessmentAsync(
            connection,
            contentId,
            definitionSchemaVersion: 2,
            payload: BuildSampleV2DefinitionJson());

        await ApplyUpAsync(connection);

        // (a) functional BTREE index is created with the expected name — NOT a GIN operator class.
        await AssertIndexIsBtreeAsync(connection, "IX_program_contents_JsonBody_type");

        var v1 = await ReadV1ContentAsync(connection, contentId);

        // (b) CodingAssignmentContent discriminator + version preserved.
        v1.Type.Should().Be("coding-assignment");
        v1.Version.Should().Be(1);

        // (c) Environment mapped from v2 Language. Tools defaults to clang; v2 has no Tools field.
        v1.Environment.Language.Should().Be("cpp");
        v1.Environment.Tools.Should().Be("clang");
        v1.Environment.LibBundle.Should().BeNull();
        v1.Environment.AllowStudentCreateFiles.Should().BeFalse();

        // (d) StdioTestCase mapped to StandardTest with stdin/expectedStdout/expectedStderr/expectedExit.
        var publicTests = v1.Tests.Public;
        publicTests.Should().ContainSingle();
        var publicStdio = publicTests.OfType<StandardTest>().Single();
        publicStdio.Stdin.Should().Be("1 2\n");
        publicStdio.Stdout.Should().Be("3\n");
        publicStdio.Stderr.Should().Be("");
        publicStdio.ExitCode.Should().Be(0);
        publicStdio.Weight.Should().Be(2.0);

        // (e) StdioFileTestCase inlined: file contents read from workspaceConfig.files and
        // dropped into Stdin/Stdout. Hidden=true routed the case to Tests.Private.
        var privateTests = v1.Tests.Private;
        privateTests.Should().ContainSingle();
        var privateStdioFile = privateTests.OfType<StandardTest>().Single();
        privateStdioFile.Stdin.Should().Be("1 2\n");   // from in1.txt
        privateStdioFile.Stdout.Should().Be("3\n");    // from out1.txt
        privateStdioFile.Weight.Should().Be(1.0);

        // (f) Workspace files map to BundleFileMeta with Visibility=Public, Modifiable=true.
        v1.Data.Files.Should().HaveCount(3);
        foreach (var (_, meta) in v1.Data.Files)
        {
            meta.Visibility.Should().Be("Public");
            meta.Modifiable.Should().BeTrue();
        }
        v1.Data.Files["main.cpp"].Content.Should().Be("int main(){}");
        v1.Data.Files["main.cpp"].Encoding.Should().Be("text");

        // (g) Grading mapped from v2 MaxScore/PassingScore.
        v1.Grading.MaxScore.Should().Be(100);
    }

    [Fact]
    public async Task Up_IsIdempotent_RunningTwiceProducesSameStateAsOnce()
    {
        await using var container = await StartPostgresAsync();
        await using var connection = new NpgsqlConnection(container.ConnectionString);
        await connection.OpenAsync();
        await CreateSchemaAsync(connection);

        var contentId = Guid.NewGuid();
        await InsertAssessmentAsync(connection, contentId, 2, BuildSampleV2DefinitionJson());

        await ApplyUpAsync(connection);
        var afterFirstPass = await ReadJsonBodyRawAsync(connection, contentId);

        // Second pass: the CREATE INDEX IF NOT EXISTS + WHERE guard on UPDATE must make this a no-op.
        await ApplyUpAsync(connection);
        var afterSecondPass = await ReadJsonBodyRawAsync(connection, contentId);

        afterSecondPass.Should().Be(afterFirstPass);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task Up_SkipsAssessmentsBelowSchemaVersion2(int definitionSchemaVersion)
    {
        await using var container = await StartPostgresAsync();
        await using var connection = new NpgsqlConnection(container.ConnectionString);
        await connection.OpenAsync();
        await CreateSchemaAsync(connection);

        var contentId = Guid.NewGuid();
        await InsertAssessmentAsync(connection, contentId, definitionSchemaVersion, BuildSampleV2DefinitionJson());

        await ApplyUpAsync(connection);

        var raw = await ReadJsonBodyRawAsync(connection, contentId);
        raw.Should().BeNull("v1/0 DefinitionSchemaVersion rows must be skipped — no backfill");
    }

    [Fact]
    public async Task Up_SkipsAssessmentsWithNullDefinitionSchemaVersion()
    {
        await using var container = await StartPostgresAsync();
        await using var connection = new NpgsqlConnection(container.ConnectionString);
        await connection.OpenAsync();
        await CreateSchemaAsync(connection);

        var contentId = Guid.NewGuid();
        await InsertAssessmentAsync(connection, contentId, definitionSchemaVersion: null, BuildSampleV2DefinitionJson());

        await ApplyUpAsync(connection);

        var raw = await ReadJsonBodyRawAsync(connection, contentId);
        raw.Should().BeNull("NULL DefinitionSchemaVersion must be skipped — no backfill");
    }

    [Fact]
    public async Task Up_SkipsNonCodingDefinitions()
    {
        await using var container = await StartPostgresAsync();
        await using var connection = new NpgsqlConnection(container.ConnectionString);
        await connection.OpenAsync();
        await CreateSchemaAsync(connection);

        var contentId = Guid.NewGuid();
        // Quiz-shaped payload — Kind != 'coding'. Must be skipped.
        var nonCodingPayload = """{"kind":"quiz","maxScore":50,"passingScore":40}""";
        await InsertAssessmentAsync(connection, contentId, 2, nonCodingPayload);

        await ApplyUpAsync(connection);

        var raw = await ReadJsonBodyRawAsync(connection, contentId);
        raw.Should().BeNull("non-coding Kind must be skipped");
    }

    [Fact]
    public async Task Up_PreservesV1ContentThatAlreadyHasType_DoesNotOverwrite()
    {
        await using var container = await StartPostgresAsync();
        await using var connection = new NpgsqlConnection(container.ConnectionString);
        await connection.OpenAsync();
        await CreateSchemaAsync(connection);

        var contentId = Guid.NewGuid();
        await InsertAssessmentAsync(connection, contentId, 2, BuildSampleV2DefinitionJson());

        // Pre-existing JsonBody with a `type` key — must NOT be overwritten by the backfill.
        const string existingJsonBody = """{"type":"lesson","payload":"pre-existing"}""";
        await ExecuteAsync(connection,
            $"UPDATE program_contents SET \"JsonBody\" = '{existingJsonBody.Replace("'", "''")}'::jsonb WHERE \"Id\" = '{contentId}';");

        await ApplyUpAsync(connection);

        var raw = await ReadJsonBodyRawAsync(connection, contentId);
        raw.Should().Contain("pre-existing", "the idempotency guard must preserve JsonBody that already has a type");
    }

    [Fact]
    public async Task Down_DropsIndexWithoutTouchingJsonBody()
    {
        await using var container = await StartPostgresAsync();
        await using var connection = new NpgsqlConnection(container.ConnectionString);
        await connection.OpenAsync();
        await CreateSchemaAsync(connection);

        var contentId = Guid.NewGuid();
        await InsertAssessmentAsync(connection, contentId, 2, BuildSampleV2DefinitionJson());

        await ApplyUpAsync(connection);
        var beforeDown = await ReadJsonBodyRawAsync(connection, contentId);
        beforeDown.Should().NotBeNull();

        await ApplyDownAsync(connection);

        await AssertIndexMissingAsync(connection, "IX_program_contents_JsonBody_type");

        var afterDown = await ReadJsonBodyRawAsync(connection, contentId);
        afterDown.Should().Be(beforeDown, "Down must not undo additive JsonBody writes");
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static string BuildSampleV2DefinitionJson() =>
        """
        {
          "kind": "coding",
          "language": "cpp",
          "workspaceConfig": {
            "files": {
              "main.cpp": {"encoding":"text","content":"int main(){}"},
              "in1.txt": {"encoding":"text","content":"1 2\n"},
              "out1.txt": {"encoding":"text","content":"3\n"}
            }
          },
          "testPlan": {
            "cases": [
              {"kind":"stdio","weight":2.0,"hidden":false,"stdin":"1 2\n","expectedStdout":"3\n","expectedStderr":"","expectedExit":0},
              {"kind":"stdio-file","weight":1.0,"hidden":true,"inFile":"in1.txt","expectedOutFile":"out1.txt"},
              {"kind":"doctest","weight":1.0,"sourceFiles":["doctest.cpp"]}
            ]
          },
          "maxScore": 100,
          "passingScore": 70
        }
        """;

    private static async Task<EconomyPostgreSqlTestDatabase> StartPostgresAsync()
    {
        var container = await EconomyPostgreSqlTestDatabase.CreateAsync("coding_assignment_v1_migration");
        return container;
    }

    private static async Task CreateSchemaAsync(NpgsqlConnection connection)
    {
        // Minimal schema — only the columns our migration touches.
        await ExecuteAsync(connection,
            """
            CREATE TABLE "program_contents" (
                "Id" uuid PRIMARY KEY,
                "JsonBody" jsonb NULL
            );
            """);
        await ExecuteAsync(connection,
            """
            CREATE TABLE "Assessments" (
                "Id" uuid PRIMARY KEY,
                "ContentId" uuid NULL,
                "DefinitionPayload" jsonb NULL,
                "DefinitionSchemaVersion" integer NULL
            );
            """);
    }

    private static async Task InsertAssessmentAsync(
        NpgsqlConnection connection,
        Guid contentId,
        int? definitionSchemaVersion,
        string payload)
    {
        var contentIdLiteral = $"'{contentId}'";
        await ExecuteAsync(connection,
            $"INSERT INTO \"program_contents\" (\"Id\", \"JsonBody\") VALUES ({contentIdLiteral}, NULL);");

        var versionLiteral = definitionSchemaVersion.HasValue
            ? definitionSchemaVersion.Value.ToString()
            : "NULL";
        var escapedPayload = payload.Replace("'", "''");
        await ExecuteAsync(connection,
            $"""
             INSERT INTO "Assessments" ("Id", "ContentId", "DefinitionPayload", "DefinitionSchemaVersion")
             VALUES ('{Guid.NewGuid()}', {contentIdLiteral}, '{escapedPayload}'::jsonb, {versionLiteral});
             """);
    }

    private static async Task ApplyUpAsync(NpgsqlConnection connection)
    {
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        new ExposedCodingAssignmentMigration().BuildUp(builder);
        await ApplyOperationsAsync(connection, builder.Operations);
    }

    private static async Task ApplyDownAsync(NpgsqlConnection connection)
    {
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        new ExposedCodingAssignmentMigration().BuildDown(builder);
        await ApplyOperationsAsync(connection, builder.Operations);
    }

    private static async Task ApplyOperationsAsync(NpgsqlConnection connection, IReadOnlyList<MigrationOperation> operations)
    {
        await using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection.ConnectionString).Options);
        var generator = context.GetService<IMigrationsSqlGenerator>();
        foreach (var command in generator.Generate(operations, null))
        {
            await ExecuteAsync(connection, command.CommandText);
        }
    }

    private static async Task<CodingAssignmentContent> ReadV1ContentAsync(NpgsqlConnection connection, Guid contentId)
    {
        var raw = await ReadJsonBodyRawAsync(connection, contentId);
        raw.Should().NotBeNull("backfill must populate JsonBody for v2 coding assessments");
        return JsonSerializer.Deserialize<CodingAssignmentContent>(raw!, s_v1Default)!;
    }

    private static async Task<string?> ReadJsonBodyRawAsync(NpgsqlConnection connection, Guid contentId)
    {
        await using var command = new NpgsqlCommand(
            """SELECT "JsonBody"::text FROM "program_contents" WHERE "Id" = @id;""",
            connection);
        command.Parameters.AddWithValue("id", contentId);
        await using var reader = await command.ExecuteReaderAsync();
        (await reader.ReadAsync()).Should().BeTrue();
        return reader.IsDBNull(0) ? null : reader.GetString(0);
    }

    private static async Task AssertIndexIsBtreeAsync(NpgsqlConnection connection, string indexName)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT am.amname, pg_get_indexdef(i.indexrelid)
            FROM pg_index i
            JOIN pg_class c ON c.oid = i.indexrelid
            JOIN pg_am am ON am.oid = c.relam
            WHERE c.relname = @indexName;
            """,
            connection);
        command.Parameters.AddWithValue("indexName", indexName);
        await using var reader = await command.ExecuteReaderAsync();
        (await reader.ReadAsync()).Should().BeTrue();
        var amName = reader.GetString(0);
        var indexDef = reader.GetString(1);
        amName.Should().Be("btree", "functional BTREE, not GIN");
        indexDef.Should().Contain("\"JsonBody\"", "index must target the type discriminator expression");
        indexDef.Should().Contain("'type'", "index must target the type discriminator value");
        indexDef.Should().NotContain("gin", "GIN operator classes are explicitly forbidden");
    }

    private static async Task AssertIndexMissingAsync(NpgsqlConnection connection, string indexName)
    {
        await using var command = new NpgsqlCommand(
            "SELECT COUNT(*) FROM pg_class WHERE relname = @indexName AND relkind = 'i';",
            connection);
        command.Parameters.AddWithValue("indexName", indexName);
        var count = (long)(await command.ExecuteScalarAsync() ?? 0);
        count.Should().Be(0, "Down must drop the index");
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private sealed class ExposedCodingAssignmentMigration : AddCodingAssignmentJsonBodyV1
    {
        public void BuildUp(MigrationBuilder migrationBuilder) => Up(migrationBuilder);
        public void BuildDown(MigrationBuilder migrationBuilder) => Down(migrationBuilder);
    }
}
