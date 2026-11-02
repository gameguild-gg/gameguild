using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace GameGuild.API.UnitTests.Architecture;

/// <summary>
///     Wiring guard G6 — no-op middleware. A middleware whose
///     <c>InvokeAsync</c> body is a handful of statements that neither calls
///     <c>next</c> nor writes anything to the response is registered dead weight:
///     every request through it either hangs (pipeline never continues) or
///     silently falls through. This is a crude Roslyn shape check — deliberately
///     conservative: it only flags bodies with fewer than 4 statements AND no
///     next-invocation AND no response write, so header-only stubs that DO forward
///     (like the ABAC copies tracked by issue #1180) stay out of scope; #1180's
///     semantic emptiness is tracked by its own issue, not by this guard.
/// </summary>
public sealed class StubMiddlewareGuardTests
{
    private const int MinimumBodyStatements = 4;

    [Fact]
    public void NoMiddleware_IsAReturnlessNoOpStub()
    {
        var middlewareFiles = WiringGapSourceTree.SourceFiles
            .Where(file => Path.GetFileName(file.Path).Contains("Middleware", StringComparison.Ordinal))
            .ToList();
        middlewareFiles.Should().HaveCountGreaterThan(10, "the middleware surface is non-trivial; a tiny count means the file scan is broken");

        var flagged = new List<string>();
        var analyzed = 0;
        foreach (var (path, text) in middlewareFiles)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(text, path: path);
            var root = syntaxTree.GetCompilationUnitRoot();

            var namespaceName = root.DescendantNodes()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .Select(declaration => declaration.Name.ToString())
                .FirstOrDefault() ?? string.Empty;

            foreach (var classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                if (!classDeclaration.Identifier.ValueText.Contains("Middleware", StringComparison.Ordinal))
                {
                    continue;
                }

                var invoke = classDeclaration.Members
                    .OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault(method => method.Identifier.ValueText is "InvokeAsync" or "Invoke");
                if (invoke?.Body is null)
                {
                    continue;
                }

                analyzed++;
                var statementCount = invoke.Body.Statements.Count;
                var callsNext = CallsNext(invoke.Body);
                var writesResponse = WritesResponse(invoke.Body);

                if (statementCount < MinimumBodyStatements && !callsNext && !writesResponse)
                {
                    var fullName = string.IsNullOrEmpty(namespaceName)
                        ? classDeclaration.Identifier.ValueText
                        : namespaceName + "." + classDeclaration.Identifier.ValueText;
                    flagged.Add(WiringGapBaseline.Id(
                        WiringGapBaseline.GuardIds.StubMiddleware, $"{fullName}|{statementCount}"));
                }
            }
        }

        analyzed.Should().BeGreaterThan(10, "every middleware Invoke/InvokeAsync body must be analyzable; a tiny count means the Roslyn scan is broken");

        WiringGapBaseline.AssertNoNewGaps(
            WiringGapBaseline.GuardIds.StubMiddleware,
            "middleware InvokeAsync must either continue the pipeline (await next(context)) or produce a response; " +
            "a tiny body that does neither is a registered no-op (hang or silent fall-through)",
            flagged);
        WiringGapBaseline.AssertNoStaleEntries(
            WiringGapBaseline.GuardIds.StubMiddleware, flagged);
    }

    /// <summary>
    ///     True when the body invokes the <c>next</c> delegate (identifier named
    ///     "next" used in an invocation, directly or as an invocation target).
    /// </summary>
    internal static bool CallsNext(BlockSyntax body) =>
        body.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Any(invocation =>
            {
                if (invocation.Expression is IdentifierNameSyntax { Identifier.ValueText: "next" })
                {
                    return true;
                }

                return invocation.Expression is MemberAccessExpressionSyntax memberAccess
                       && memberAccess.Expression is IdentifierNameSyntax { Identifier.ValueText: "next" };
            });

    /// <summary>
    ///     True when the body touches <c>&lt;context&gt;.Response</c> (status, headers,
    ///     body writes) — a middleware producing a response is not a no-op.
    /// </summary>
    internal static bool WritesResponse(BlockSyntax body) =>
        body.DescendantNodes().OfType<MemberAccessExpressionSyntax>()
            .Any(access => access.Name.Identifier.ValueText == "Response");
}
