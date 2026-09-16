using System.Text.Json.Nodes;
using FluentAssertions;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.Authoring;

public sealed class AuthoringStructuredPatchTests
{
    [Fact]
    public void Apply_LexicalTextReplacement_PreservesUnknownInteractiveNodes()
    {
        const string source = """
            {"root":{"type":"root","version":1,"children":[
              {"type":"paragraph","version":1,"children":[{"type":"text","version":1,"text":"Before","custom":"keep"}]},
              {"type":"game-embed","version":7,"gameId":"game-1","customPayload":{"difficulty":"hard"}}
            ]}}
            """;
        const string patch = """
            {"operations":[{"op":"replace","path":"/root/children/0/children/0/text","value":"After"}]}
            """;

        var result = JsonNode.Parse(AuthoringStructuredPatch.Apply(source, patch, AiProposalKind.LexicalPatch))!;

        result["root"]!["children"]![0]!["children"]![0]!["text"]!.GetValue<string>().Should().Be("After");
        result["root"]!["children"]![0]!["children"]![0]!["custom"]!.GetValue<string>().Should().Be("keep");
        result["root"]!["children"]![1]!["customPayload"]!["difficulty"]!.GetValue<string>().Should().Be("hard");
    }

    [Fact]
    public void Apply_LexicalPatchThatChangesNodeType_IsRejected()
    {
        const string source = """{"root":{"type":"root","version":1,"children":[]}}""";
        const string patch = """{"operations":[{"op":"replace","path":"/root/type","value":"paragraph"}]}""";

        var act = () => AuthoringStructuredPatch.Apply(source, patch, AiProposalKind.LexicalPatch);

        act.Should().Throw<ArgumentException>().WithMessage("*identity and type metadata*");
    }

    [Fact]
    public void Apply_LexicalPatchCannotRemoveUnknownInteractiveNode()
    {
        const string source = """{"root":{"type":"root","version":1,"children":[{"type":"game-embed","version":1,"gameId":"g"}]}}""";
        const string patch = """{"operations":[{"op":"remove","path":"/root/children/0"}]}""";

        var act = () => AuthoringStructuredPatch.Apply(source, patch, AiProposalKind.LexicalPatch);

        act.Should().Throw<ArgumentException>().WithMessage("*interactive or unknown*");
    }

    [Fact]
    public void Apply_QuizPatch_ChangesOnlyAddressedField()
    {
        const string source = """{"items":[{"id":"q1","prompt":"Before","answer":"A","extension":{"rubric":"keep"}}],"grading":{"enabled":true}}""";
        const string patch = """{"operations":[{"op":"replace","path":"/items/0/prompt","value":"After"}]}""";

        var result = JsonNode.Parse(AuthoringStructuredPatch.Apply(source, patch, AiProposalKind.QuizPatch))!;

        result["items"]![0]!["prompt"]!.GetValue<string>().Should().Be("After");
        result["items"]![0]!["extension"]!["rubric"]!.GetValue<string>().Should().Be("keep");
        result["grading"]!["enabled"]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public void Apply_QuizPatch_SupportsObjectArrayAndEscapedPointerOperations()
    {
        const string source = """
            {"object":{"old":1},"items":[1,2],"escaped":{"a/b":{"~key":1}}}
            """;
        const string patch = """
            {"operations":[
              {"op":" add ","path":"/object/new","value":2},
              {"op":"replace","path":"/items/0","value":10},
              {"op":"add","path":"/items/-","value":3},
              {"op":"add","path":"/items/3","value":4},
              {"op":"remove","path":"/object/old"},
              {"op":"remove","path":"/items/1"},
              {"op":"replace","path":"/escaped/a~1b/~0key","value":9}
            ]}
            """;

        var result = JsonNode.Parse(AuthoringStructuredPatch.Apply(source, patch, AiProposalKind.QuizPatch))!;

        result["object"]!["old"].Should().BeNull();
        result["object"]!["new"]!.GetValue<int>().Should().Be(2);
        result["items"]!.AsArray().Select(item => item!.GetValue<int>()).Should().Equal(10, 3, 4);
        result["escaped"]!["a/b"]!["~key"]!.GetValue<int>().Should().Be(9);
    }

    [Fact]
    public void Apply_LexicalPatch_AllowsRemovingKnownAndUnstructuredValues()
    {
        const string source = """
            {"root":{"children":[{"type":"paragraph","children":[]},5,{"type":" ","custom":true},{"custom":true}]}}
            """;
        const string patch = """
            {"operations":[
              {"op":"remove","path":"/root/children/0"},
              {"op":"remove","path":"/root/children/0"},
              {"op":"remove","path":"/root/children/0"},
              {"op":"remove","path":"/root/children/0"}
            ]}
            """;

        var result = JsonNode.Parse(AuthoringStructuredPatch.Apply(source, patch, AiProposalKind.LexicalPatch))!;

        result["root"]!["children"]!.AsArray().Should().BeEmpty();
    }

    [Fact]
    public void Apply_QuizPatch_AllowsRemovingInteractiveObjects()
    {
        const string source = """{"items":[{"type":"game-embed","gameId":"g"}]}""";
        const string patch = """{"operations":[{"op":"remove","path":"/items/0"}]}""";

        var result = JsonNode.Parse(AuthoringStructuredPatch.Apply(source, patch, AiProposalKind.QuizPatch))!;

        result["items"]!.AsArray().Should().BeEmpty();
    }

    [Fact]
    public void Apply_ReplaceAndAddWithoutValues_WriteJsonNull()
    {
        const string source = """{"existing":1}""";
        const string patch = """
            {"operations":[
              {"op":"replace","path":"/existing"},
              {"op":"add","path":"/added"}
            ]}
            """;

        var result = JsonNode.Parse(AuthoringStructuredPatch.Apply(source, patch, AiProposalKind.QuizPatch))!.AsObject();

        result.ContainsKey("existing").Should().BeTrue();
        result["existing"].Should().BeNull();
        result.ContainsKey("added").Should().BeTrue();
        result["added"].Should().BeNull();
    }

    [Theory]
    [InlineData(AiProposalKind.ReplaceDocument)]
    [InlineData(AiProposalKind.MetadataPatch)]
    [InlineData(AiProposalKind.InsertAtCursor)]
    public void Apply_RejectsNonStructuredProposalKinds(AiProposalKind kind)
    {
        var action = () => AuthoringStructuredPatch.Apply("{}", "{}", kind);

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("kind");
    }

    [Theory]
    [InlineData("null", "{\"operations\":[{\"op\":\"add\",\"path\":\"/x\",\"value\":1}]}", "source document")]
    [InlineData("{}", "[]", "JSON object")]
    [InlineData("{}", "{}", "operations array")]
    [InlineData("{}", "{\"operations\":null}", "operations array")]
    [InlineData("{}", "{\"operations\":[]}", "between 1 and 100")]
    [InlineData("{}", "{\"operations\":[1]}", "operation must be an object")]
    [InlineData("{}", "{\"operations\":[{\"path\":\"/x\"}]}", "requires an op")]
    [InlineData("{}", "{\"operations\":[{\"op\":\"add\"}]}", "requires a path")]
    [InlineData("{}", "{\"operations\":[{\"op\":\"add\",\"path\":\"\",\"value\":1}]}", "root is not allowed")]
    [InlineData("{}", "{\"operations\":[{\"op\":\"add\",\"path\":\"x\",\"value\":1}]}", "JSON Pointer")]
    [InlineData("{}", "{\"operations\":[{\"op\":\"move\",\"path\":\"/x\"}]}", "Unsupported")]
    [InlineData("{}", "{\"operations\":[{\"op\":\"add\",\"path\":\"/missing/value\",\"value\":1}]}", "does not exist")]
    [InlineData("{\"value\":1}", "{\"operations\":[{\"op\":\"add\",\"path\":\"/value/child/grandchild\",\"value\":1}]}", "objects and arrays")]
    public void Apply_RejectsMalformedDocumentsAndOperations(string source, string patch, string expectedMessage)
    {
        var action = () => AuthoringStructuredPatch.Apply(source, patch, AiProposalKind.QuizPatch);

        action.Should().Throw<ArgumentException>().WithMessage($"*{expectedMessage}*");
    }

    [Fact]
    public void Apply_RejectsMoreThanOneHundredOperations()
    {
        var operations = string.Join(',', Enumerable.Range(0, 101)
            .Select(index => $$"""{"op":"add","path":"/value{{index}}","value":{{index}}}"""));
        var patch = $$"""{"operations":[{{operations}}]}""";

        var action = () => AuthoringStructuredPatch.Apply("{}", patch, AiProposalKind.QuizPatch);

        action.Should().Throw<ArgumentException>().WithMessage("*between 1 and 100*");
    }

    [Theory]
    [InlineData("type")]
    [InlineData("TYPE")]
    [InlineData("version")]
    [InlineData("key")]
    public void Apply_LexicalPatchRejectsEveryProtectedIdentityProperty(string property)
    {
        const string source = """{"root":{"type":"root","version":1,"key":"root","children":[]}}""";
        var patch = $$"""{"operations":[{"op":"replace","path":"/root/{{property}}","value":"changed"}]}""";

        var action = () => AuthoringStructuredPatch.Apply(source, patch, AiProposalKind.LexicalPatch);

        action.Should().Throw<ArgumentException>().WithMessage("*identity and type metadata*");
    }

    [Theory]
    [InlineData("replace", "{}", "/missing", "existing value")]
    [InlineData("replace", "1", "/value", "existing value")]
    [InlineData("add", "1", "/value", "object or array parent")]
    [InlineData("remove", "{}", "/missing", "existing value")]
    [InlineData("remove", "1", "/value", "object or array parent")]
    public void Apply_RejectsOperationsWithInvalidParents(string op, string source, string path, string expectedMessage)
    {
        var patch = $$"""{"operations":[{"op":"{{op}}","path":"{{path}}","value":1}]}""";

        var action = () => AuthoringStructuredPatch.Apply(source, patch, AiProposalKind.QuizPatch);

        action.Should().Throw<ArgumentException>().WithMessage($"*{expectedMessage}*");
    }

    [Theory]
    [InlineData("x")]
    [InlineData("-1")]
    [InlineData("2")]
    [InlineData("3")]
    public void Apply_RejectsInvalidArrayIndexes(string index)
    {
        const string source = """{"items":[1,2]}""";
        var patch = $$"""{"operations":[{"op":"replace","path":"/items/{{index}}","value":3}]}""";

        var action = () => AuthoringStructuredPatch.Apply(source, patch, AiProposalKind.QuizPatch);

        action.Should().Throw<ArgumentException>().WithMessage("*not a valid array index*");
    }
}
