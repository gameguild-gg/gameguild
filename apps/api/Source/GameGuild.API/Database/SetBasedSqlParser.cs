namespace GameGuild.API.Database;

/// <summary>
///     Parses the single-statement set-based <c>UPDATE</c>/<c>DELETE</c> SQL shapes that EF Core
///     generates for <c>ExecuteUpdate</c>/<c>ExecuteDelete</c> (and equivalent hand-written
///     <c>ExecuteSqlRaw</c> statements) so the set-based audit interceptor can replay their
///     row-selection predicate inside the same transaction. The parser deliberately accepts only
///     the simple single-table shape — a table reference (optionally schema-qualified, optionally
///     aliased), a <c>SET</c> clause for updates, and an optional top-level <c>WHERE</c> clause —
///     and rejects anything richer (CTEs, joins, <c>RETURNING</c>, multi-statement batches), in
///     which case the caller skips capture for that statement.
/// </summary>
internal static class SetBasedSqlParser
{
    /// <summary>A parsed set-based statement shape. Identifier values are unquoted verbatim.</summary>
    /// <param name="TableIdentifiers">Table reference chain: <c>[schema,] table</c>, unquoted.</param>
    /// <param name="Alias">Table alias when the statement declared one (unquoted), else <c>null</c>.</param>
    /// <param name="IsUpdate"><c>true</c> for <c>UPDATE</c>, <c>false</c> for <c>DELETE</c>.</param>
    /// <param name="WhereClause">Verbatim top-level <c>WHERE</c> expression without the keyword, or <c>null</c> when the statement is unguarded.</param>
    internal sealed record ParsedStatement(
        IReadOnlyList<string> TableIdentifiers,
        string? Alias,
        bool IsUpdate,
        string? WhereClause);

    /// <summary>
    ///     Attempts to parse <paramref name="sql"/> as a capturable set-based statement.
    ///     Returns <c>null</c> for every shape the interceptor does not replay (SELECT, INSERT,
    ///     DDL, advisory-lock helpers, CTEs, statements with RETURNING, multi-statement text).
    /// </summary>
    internal static ParsedStatement? TryParse(string? sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return null;
        }

        var text = sql.Trim();
        if (text.Contains(';'))
        {
            // Multi-statement batches cannot be replayed safely.
            return null;
        }

        var cursor = 0;
        var isUpdate = true;
        if (TryReadKeyword(text, ref cursor, "UPDATE"))
        {
            isUpdate = true;
        }
        else if (TryReadKeyword(text, ref cursor, "DELETE"))
        {
            if (!TryReadKeyword(text, ref cursor, "FROM"))
            {
                return null;
            }

            isUpdate = false;
        }
        else
        {
            return null;
        }

        var tableIdentifiers = new List<string>();
        string? identifier;
        while ((identifier = ReadIdentifier(text, ref cursor)) is not null)
        {
            tableIdentifiers.Add(identifier);
            if (cursor < text.Length && text[cursor] == '.')
            {
                cursor++;
                continue;
            }

            break;
        }

        if (tableIdentifiers.Count is < 1 or > 2)
        {
            // Either nothing parsed or a three-plus-part chain the model cannot match.
            return null;
        }

        SkipWhitespace(text, ref cursor);
        string? alias = null;
        if (TryReadKeyword(text, ref cursor, "AS"))
        {
            alias = ReadIdentifier(text, ref cursor);
            if (alias is null)
            {
                return null;
            }

            SkipWhitespace(text, ref cursor);
        }

        if (isUpdate)
        {
            if (!TryReadKeyword(text, ref cursor, "SET"))
            {
                return null;
            }
        }

        string? whereClause = null;
        var whereIndex = FindTopLevelKeyword(text, cursor, "WHERE");
        if (whereIndex >= 0)
        {
            whereClause = text[(whereIndex + "WHERE".Length)..].Trim();
            if (whereClause.Length == 0)
            {
                return null;
            }
        }

        // Provider-flavoured trailing clauses (for example PostgreSQL RETURNING) belong to the
        // intercepted mutation, not to the replayed row selection: strip them so the
        // compensating SELECT stays valid. The mutation itself is never modified.
        var scanStart = whereIndex >= 0 ? whereIndex + "WHERE".Length : cursor;
        var returningIndex = FindTopLevelKeyword(text, scanStart, "RETURNING");
        if (returningIndex >= 0 && whereClause is not null)
        {
            whereClause = text[scanStart..returningIndex].Trim();
            if (whereClause.Length == 0)
            {
                whereClause = null;
            }
        }

        return new ParsedStatement(tableIdentifiers, alias, isUpdate, whereClause);
    }

    /// <summary>Quotes an identifier for re-embedding into generated SQL (doubles embedded quotes).</summary>
    internal static string Quote(string identifier) => "\"" + identifier.Replace("\"", "\"\"") + "\"";

    private static bool TryReadKeyword(string text, ref int cursor, string keyword)
    {
        SkipWhitespace(text, ref cursor);
        if (cursor + keyword.Length > text.Length
            || string.Compare(text, cursor, keyword, 0, keyword.Length, StringComparison.OrdinalIgnoreCase) != 0)
        {
            return false;
        }

        var after = cursor + keyword.Length;
        if (after < text.Length && IsIdentifierCharacter(char.ToLowerInvariant(text[after])))
        {
            // "UPDATES" must not match the "UPDATE" keyword.
            return false;
        }

        cursor = after;
        SkipWhitespace(text, ref cursor);
        return true;
    }

    private static string? ReadIdentifier(string text, ref int cursor)
    {
        SkipWhitespace(text, ref cursor);
        if (cursor >= text.Length)
        {
            return null;
        }

        if (text[cursor] == '"')
        {
            var end = cursor + 1;
            while (end < text.Length)
            {
                if (text[end] == '"')
                {
                    if (end + 1 < text.Length && text[end + 1] == '"')
                    {
                        end += 2;
                        continue;
                    }

                    break;
                }

                end++;
            }

            if (end >= text.Length)
            {
                return null;
            }

            var value = text[(cursor + 1)..end].Replace("\"\"", "\"");
            cursor = end + 1;
            return value.Length == 0 ? null : value;
        }

        var start = cursor;
        while (cursor < text.Length && IsIdentifierCharacter(char.ToLowerInvariant(text[cursor])))
        {
            cursor++;
        }

        return cursor > start ? text[start..cursor] : null;
    }

    private static void SkipWhitespace(string text, ref int cursor)
    {
        while (cursor < text.Length && char.IsWhiteSpace(text[cursor]))
        {
            cursor++;
        }
    }

    private static bool IsIdentifierCharacter(char candidate) =>
        candidate is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '_' or '$' or '#';

    /// <summary>Finds the first top-level (paren-depth zero, outside identifiers and literals) occurrence of a keyword.</summary>
    private static int FindTopLevelKeyword(string text, int start, string keyword)
    {
        var depth = 0;
        var inIdentifier = false;
        var inLiteral = false;
        for (var index = start; index < text.Length; index++)
        {
            var candidate = text[index];
            if (inIdentifier)
            {
                if (candidate == '"')
                {
                    inIdentifier = false;
                }

                continue;
            }

            if (inLiteral)
            {
                if (candidate == '\'')
                {
                    inLiteral = false;
                }

                continue;
            }

            switch (candidate)
            {
                case '"':
                    inIdentifier = true;
                    break;
                case '\'':
                    inLiteral = true;
                    break;
                case '(':
                    depth++;
                    break;
                case ')':
                    depth--;
                    break;
                default:
                    if (depth == 0
                        && candidate is 'w' or 'W' or 'r' or 'R'
                        && index + keyword.Length <= text.Length
                        && string.Compare(text, index, keyword, 0, keyword.Length, StringComparison.OrdinalIgnoreCase) == 0
                        && (index == 0 || !IsIdentifierCharacter(char.ToLowerInvariant(text[index - 1])))
                        && (index + keyword.Length == text.Length
                            || !IsIdentifierCharacter(char.ToLowerInvariant(text[index + keyword.Length]))))
                    {
                        return index;
                    }

                    break;
            }
        }

        return -1;
    }
}
