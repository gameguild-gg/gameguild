namespace GameGuild.API.Database;

/// <summary>
///     Parser tests for the set-based audit capture (issue #166): the interceptor can only
///     replay statements whose shape <see cref="SetBasedSqlParser"/> round-trips, so every
///     accepted shape (schema-qualified quoted identifiers, aliases, guarded WHERE clauses)
///     and every rejected shape (CTEs, RETURNING, multi-statement, SELECT/INSERT/DDL) is
///     pinned here without a database.
/// </summary>
public sealed class SetBasedSqlParserTests
{
    [Theory]
    [InlineData("""
        UPDATE "promo_codes" AS "p" SET "current_uses" = "p"."current_uses" + 1, "updated_at" = @__now_0 WHERE "p"."id" = @__promoCodeId_1 AND "p"."deleted_at" IS NULL AND "p"."is_active" = TRUE AND ("p"."current_uses" < @__maxUses_2 OR @__maxUses_2 IS NULL)
        """, true, "promo_codes", "p")]
    [InlineData("""
        DELETE FROM "user_notifications" AS "u" WHERE "u"."user_id" = @__userId_0 AND "u"."is_archived" = FALSE AND "u"."archived_at" < @__cutoff_1
        """, false, "user_notifications", "u")]
    [InlineData("""
        UPDATE "gameguild"."entity_change_audit_records" SET "tenant_id" = @p0 WHERE "id" = @p1
        """, true, "entity_change_audit_records", null)]
    [InlineData("UPDATE \"gameguild.audit\".\"mutation_value_snapshots\" SET \"actor_id\" = @p0", true, "mutation_value_snapshots", null)]
    [InlineData("DELETE FROM \"users\" WHERE \"users\".\"id\" IN (@p0, @p1)", false, "users", null)]
    [InlineData("UPDATE users SET name = @p0 WHERE id = @p1", true, "users", null)]
    public void ParsesSupportedShapes(string sql, bool expectedUpdate, string expectedTable, string? expectedAlias)
    {
        var statement = SetBasedSqlParser.TryParse(sql);

        Assert.NotNull(statement);
        Assert.Equal(expectedUpdate, statement.IsUpdate);
        Assert.Equal(expectedTable, statement.TableIdentifiers[^1]);
        Assert.Equal(expectedAlias, statement.Alias);
    }

    [Fact]
    public void ParsesWhereClauseVerbatim()
    {
        const string sql = """
            UPDATE "promo_codes" AS "p" SET "current_uses" = "p"."current_uses" + 1 WHERE "p"."id" = @p0 AND ("p"."max_uses" IS NULL OR "p"."current_uses" < "p"."max_uses")
            """;

        var statement = SetBasedSqlParser.TryParse(sql);

        Assert.NotNull(statement);
        Assert.Equal("\"p\".\"id\" = @p0 AND (\"p\".\"max_uses\" IS NULL OR \"p\".\"current_uses\" < \"p\".\"max_uses\")", statement.WhereClause);
    }

    [Fact]
    public void UnguardedStatementHasNoWhereClause()
    {
        var statement = SetBasedSqlParser.TryParse("UPDATE \"outbox_messages\" SET \"delivery_attempts\" = 0");

        Assert.NotNull(statement);
        Assert.Null(statement.WhereClause);
    }

    [Theory]
    [InlineData("SELECT \"Id\" FROM \"Users\"")]
    [InlineData("INSERT INTO \"users\" (\"name\") VALUES (@p0)")]
    [InlineData("CREATE SCHEMA IF NOT EXISTS finance")]
    [InlineData("SELECT pg_advisory_lock(42)")]
    [InlineData("UPDATE \"t\" SET \"a\" = 1; DELETE FROM \"t\"")]
    [InlineData("WITH deleted AS (DELETE FROM \"t\" WHERE \"a\" = 1 RETURNING *) SELECT count(*) FROM deleted")]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectsUnsupportedShapes(string sql)
    {
        Assert.Null(SetBasedSqlParser.TryParse(sql));
    }

    [Fact]
    public void ReturningClauseIsStrippedFromReplayedPredicate()
    {
        var statement = SetBasedSqlParser.TryParse("UPDATE \"t\" SET \"a\" = 1 WHERE \"t\".\"id\" = @p0 RETURNING \"a\"");

        Assert.NotNull(statement);
        Assert.Equal("\"t\".\"id\" = @p0", statement.WhereClause);

        var unguarded = SetBasedSqlParser.TryParse("UPDATE \"t\" SET \"a\" = 1 RETURNING \"a\"");
        Assert.NotNull(unguarded);
        Assert.Null(unguarded.WhereClause);
    }

    [Fact]
    public void ReturningInsideStringLiteralIsNotTreatedAsClause()
    {
        var statement = SetBasedSqlParser.TryParse("UPDATE \"t\" SET \"a\" = @p0 WHERE \"t\".\"note\" = 'RETURNING keep'");

        Assert.NotNull(statement);
        Assert.Equal("\"t\".\"note\" = 'RETURNING keep'", statement.WhereClause);
    }

    [Fact]
    public void KeywordPrefixesDoNotMatchVerbs()
    {
        Assert.Null(SetBasedSqlParser.TryParse("UPDATED_AT column"));
        Assert.Null(SetBasedSqlParser.TryParse("DELETED_AT column"));
    }

    [Fact]
    public void QuoteEscapesEmbeddedQuotes()
    {
        Assert.Equal("\"weird\"\"table\"", SetBasedSqlParser.Quote("weird\"table"));
    }
}
