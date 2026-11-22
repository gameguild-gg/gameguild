using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.CodingAssignment;

public sealed class CodingExecutionConfigurationAdditionalValidationTests
{
    public static TheoryData<FunctionParameterType, string, string, string> InvalidScalarValues
    {
        get
        {
            var values = new (FunctionParameterType Type, string Json)[]
            {
                (FunctionParameterType.String, "2"),
                (FunctionParameterType.String, "false"),
                (FunctionParameterType.String, "null"),
                (FunctionParameterType.String, "[]"),
                (FunctionParameterType.Boolean, "\"false\""),
                (FunctionParameterType.Boolean, "0"),
                (FunctionParameterType.Boolean, "null"),
                (FunctionParameterType.Integer, "\"2\""),
                (FunctionParameterType.Integer, "2.5"),
                (FunctionParameterType.Integer, "9007199254740992"),
                (FunctionParameterType.Integer, "-9007199254740992"),
                (FunctionParameterType.Integer, "true"),
                (FunctionParameterType.Integer, "null"),
                (FunctionParameterType.Integer, "{}"),
                (FunctionParameterType.Float, "2.5"),
                (FunctionParameterType.Float, "false"),
                (FunctionParameterType.Float, "null"),
                (FunctionParameterType.Float, "\"NaN\""),
                (FunctionParameterType.Float, "\"Infinity\""),
                (FunctionParameterType.Float, "\"+1\""),
                (FunctionParameterType.Float, "\"01\""),
                (FunctionParameterType.Float, "\" 1\""),
                (FunctionParameterType.Float, "\"1.\""),
            };
            var cases = new TheoryData<FunctionParameterType, string, string, string>();
            foreach (var (type, json) in values)
            {
                foreach (var bucket in new[] { "public", "private" })
                {
                    foreach (var slot in new[] { "input", "expected" })
                    {
                        cases.Add(type, json, bucket, slot);
                    }
                }
            }
            return cases;
        }
    }

    public static TheoryData<double, string, string> InvalidExplicitWeights
    {
        get
        {
            var cases = new TheoryData<double, string, string>();
            foreach (var weight in new[] { 0d, 0.001d, -0.01d, 0.295d, 1.005d, 100_000_000_000_000d,
                double.MaxValue, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            {
                foreach (var family in new[] { "function", "standard-io" })
                {
                    foreach (var bucket in new[] { "public", "private" })
                    {
                        cases.Add(weight, family, bucket);
                    }
                }
            }
            return cases;
        }
    }

    public static TheoryData<string?> InvalidParameterNames => new()
    {
        null, "", " ", "1value", "two words", "value-name", "ns::value", new string('p', 129),
    };

    [Fact]
    public void ExplicitStandardIo_RejectsEmptyPlanWithoutProblemMetadata()
    {
        var definition = Definition() with { Tests = new TestSuite() };

        AssertInvalid(definition);
    }

    [Fact]
    public void LegacyStandardIo_StillAllowsEmptyPlanAndOmitsNewFields()
    {
        var definition = AsLegacy(Definition() with { Tests = new TestSuite() });

        AssertValid(definition);
        var json = JsonSerializer.SerializeToElement(definition);
        json.TryGetProperty("Execution", out _).Should().BeFalse();
        json.GetProperty("Environment").TryGetProperty("Runtime", out _).Should().BeFalse();
    }

    [Theory]
    [InlineData(FunctionParameterType.String, "\"\"")]
    [InlineData(FunctionParameterType.String, "\"2\"")]
    [InlineData(FunctionParameterType.String, "\"false\"")]
    [InlineData(FunctionParameterType.Boolean, "false")]
    [InlineData(FunctionParameterType.Boolean, "true")]
    [InlineData(FunctionParameterType.Integer, "0")]
    [InlineData(FunctionParameterType.Integer, "2")]
    [InlineData(FunctionParameterType.Integer, "-2")]
    [InlineData(FunctionParameterType.Integer, "2.0")]
    [InlineData(FunctionParameterType.Integer, "2e0")]
    [InlineData(FunctionParameterType.Integer, "9007199254740991")]
    [InlineData(FunctionParameterType.Integer, "-9007199254740991")]
    [InlineData(FunctionParameterType.Float, "\"0.25\"")]
    [InlineData(FunctionParameterType.Float, "\"-2.5e+3\"")]
    [InlineData(FunctionParameterType.Float, "\"1e400\"")]
    public void ExplicitFunction_AcceptsExactScalarRepresentationsWithoutConversion(FunctionParameterType type, string json)
    {
        var definition = FunctionDefinition(type, json);

        AssertValid(definition);
        var serialized = JsonSerializer.SerializeToElement(definition);
        AssertScalar(serialized.GetProperty("Tests").GetProperty("Public")[0].GetProperty("Cases")[0]
            .GetProperty("Inputs")[0].GetProperty("Content"), json);
        AssertScalar(serialized.GetProperty("Tests").GetProperty("Private")[0].GetProperty("Cases")[0]
            .GetProperty("Expected").GetProperty("Content"), json);
    }

    [Theory]
    [MemberData(nameof(InvalidScalarValues))]
    public void ExplicitFunction_RejectsInvalidScalarValuesInEitherSlotAndBucket(
        FunctionParameterType type, string json, string bucket, string slot)
    {
        var definition = FunctionDefinition(type, ValidScalarJson(type));
        using var document = JsonDocument.Parse(json);
        var altered = ReplaceCaseValue(definition, bucket, slot, new FunctionParameter
        {
            Type = type,
            Content = document.RootElement.Clone(),
        });

        AssertInvalid(altered);
    }

    [Theory]
    [InlineData("public", "input")]
    [InlineData("public", "expected")]
    [InlineData("private", "input")]
    [InlineData("private", "expected")]
    public void ExplicitFunction_RejectsCaseTypesThatDoNotMatchTheSignature(string bucket, string slot)
    {
        var definition = FunctionDefinition(FunctionParameterType.Integer, "2");
        var altered = ReplaceCaseValue(definition, bucket, slot, new FunctionParameter
        {
            Type = FunctionParameterType.Boolean,
            Content = JsonSerializer.SerializeToElement(false),
        });

        AssertInvalid(altered);
    }

    [Theory]
    [InlineData(FunctionParameterType.String, "2")]
    [InlineData(FunctionParameterType.Boolean, "\"false\"")]
    [InlineData(FunctionParameterType.Integer, "\"2\"")]
    [InlineData(FunctionParameterType.Float, "2.5")]
    public void AbsentExecution_PreservesLegacyScalarInterpretation(FunctionParameterType type, string json)
    {
        var definition = AsLegacy(FunctionDefinition(type, json));

        AssertValid(definition);
        var restored = JsonSerializer.SerializeToElement(definition).Deserialize<CodingAssignmentContent>()!;
        var group = (FunctionalTestGroup)restored.Tests.Public.Single();
        AssertScalar(group.Cases.Single().Inputs.Single().Content, json);
        AssertScalar(group.Cases.Single().Expected.Content, json);
    }

    [Theory]
    [InlineData("Execution")]
    [InlineData("Limits")]
    [InlineData("EntryPoint")]
    [InlineData("Toolchain")]
    public void NewExecutionDtos_RejectUnknownPropertiesOnDefaultDeserialization(string section)
    {
        var document = JsonSerializer.SerializeToNode(Definition())!.AsObject();
        var execution = document["Execution"]!.AsObject();
        var target = section == "Execution" ? execution : execution[section]!.AsObject();
        target["UnknownExecutionOption"] = "must not be silently ignored";

        Action deserialize = () => JsonSerializer.Deserialize<CodingAssignmentContent>(document.ToJsonString());

        deserialize.Should().Throw<JsonException>();
    }

    [Fact]
    public void LegacyDtos_KeepTheirExistingUnknownPropertyHandling()
    {
        var document = JsonSerializer.SerializeToNode(AsLegacy(Definition()))!.AsObject();
        document["HistoricalAnnotation"] = "legacy";
        document["Environment"]!["HistoricalToolOption"] = "legacy";

        var definition = JsonSerializer.Deserialize<CodingAssignmentContent>(document.ToJsonString())!;

        AssertValid(definition);
        definition.Execution.Should().BeNull();
        definition.Environment.Runtime.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(InvalidExplicitWeights))]
    public void PublicationParity_ExplicitRejectsLossyOrUnsafeWeights(double weight, string family, string bucket)
    {
        var definition = WithWeight(family, bucket, weight);

        AssertInvalid(definition);
    }

    [Theory]
    [InlineData(0.01d, "function", "public")]
    [InlineData(0.01d, "function", "private")]
    [InlineData(0.01d, "standard-io", "public")]
    [InlineData(0.01d, "standard-io", "private")]
    [InlineData(0.29d, "function", "public")]
    [InlineData(0.29d, "function", "private")]
    [InlineData(0.29d, "standard-io", "public")]
    [InlineData(0.29d, "standard-io", "private")]
    public void PublicationParity_ExplicitAcceptsExactlyRepresentableWeights(double weight, string family, string bucket)
    {
        var definition = WithWeight(family, bucket, weight);

        AssertValid(definition);
        var target = bucket == "public" ? definition.Tests.Public : definition.Tests.Private;
        target.Single().Weight.Should().Be(weight);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(0.001d)]
    [InlineData(1.005d)]
    [InlineData(100_000_000_000_000d)]
    public void PublicationParity_LegacyKeepsNonnegativeWeightInterpretation(double weight)
    {
        foreach (var family in new[] { "function", "standard-io" })
        {
            foreach (var bucket in new[] { "public", "private" })
            {
                var definition = AsLegacy(WithWeight(family, bucket, weight));

                AssertValid(definition);
                var target = bucket == "public" ? definition.Tests.Public : definition.Tests.Private;
                target.Single().Weight.Should().Be(weight);
            }
        }
    }

    [Theory]
    [MemberData(nameof(InvalidParameterNames))]
    public void PublicationParity_ExplicitRejectsCoherentInvalidParameterNames(string? name)
    {
        var definition = FunctionWithNames("identity", [name]);

        AssertInvalid(definition);
    }

    [Fact]
    public void PublicationParity_ExplicitRejectsCoherentDuplicateParameterNames()
    {
        var definition = FunctionWithNames("identity", ["value", "value"]);

        AssertInvalid(definition);
    }

    [Fact]
    public void PublicationParity_ExplicitRejectsLongFunctionNameEvenWithMatchingEntrySymbol()
    {
        var name = new string('f', 129);
        var definition = FunctionWithNames(name, ["value"]);
        definition.Execution!.EntryPoint.Symbol.Should().Be(name);

        AssertInvalid(definition);
    }

    [Fact]
    public void PublicationParity_ExplicitAcceptsIdentifierLengthBoundary()
    {
        var definition = FunctionWithNames(new string('f', 128), [new string('p', 128)]);

        AssertValid(definition);
    }

    [Fact]
    public void PublicationParity_ExplicitAcceptsDistinctCaseSensitiveParameterNames()
    {
        var definition = FunctionWithNames("identity", ["value", "Value"]);

        AssertValid(definition);
    }

    [Theory]
    [MemberData(nameof(InvalidParameterNames))]
    public void PublicationParity_LegacyKeepsHistoricalParameterNames(string? name)
    {
        var definition = AsLegacy(FunctionWithNames("identity", [name]));

        AssertValid(definition);
    }

    [Theory]
    [InlineData("duplicate-parameters")]
    [InlineData("long-function-name")]
    public void PublicationParity_LegacyKeepsDuplicateParametersAndLongFunctionNames(string mutation)
    {
        var definition = mutation == "duplicate-parameters"
            ? FunctionWithNames("identity", ["value", "value"])
            : FunctionWithNames(new string('f', 129), ["value"]);

        AssertValid(AsLegacy(definition));
    }

    [Theory]
    [InlineData("public")]
    [InlineData("private")]
    public void PublicationParity_ExplicitRejectsNullStdoutAfterWireDeserialization(string bucket)
    {
        var json = JsonSerializer.SerializeToElement(WithStandardTest(bucket, stdout: null));
        var definition = json.Deserialize<CodingAssignmentContent>()!;

        AssertInvalid(definition);
    }

    [Theory]
    [InlineData("public")]
    [InlineData("private")]
    public void PublicationParity_ExplicitAcceptsEmptyExpectedStdout(string bucket)
    {
        var json = JsonSerializer.SerializeToElement(WithStandardTest(bucket, stdout: ""));
        var definition = json.Deserialize<CodingAssignmentContent>()!;

        AssertValid(definition);
        var target = bucket == "public" ? definition.Tests.Public : definition.Tests.Private;
        ((StandardTest)target.Single()).Stdout.Should().BeEmpty();
    }

    [Theory]
    [InlineData("public", null)]
    [InlineData("private", null)]
    [InlineData("public", "")]
    [InlineData("private", "")]
    public void PublicationParity_LegacyKeepsNullAndEmptyStdoutInterpretation(string bucket, string? stdout)
    {
        var json = JsonSerializer.SerializeToElement(AsLegacy(WithStandardTest(bucket, stdout: stdout)));
        var definition = json.Deserialize<CodingAssignmentContent>()!;

        AssertValid(definition);
        var target = bucket == "public" ? definition.Tests.Public : definition.Tests.Private;
        ((StandardTest)target.Single()).Stdout.Should().Be(stdout);
    }

    private static CodingAssignmentContent WithStandardTest(string bucket, string? stdout = "ok")
    {
        var altered = new StandardTest { Stdout = stdout! };
        var unchanged = new StandardTest { Stdout = "unchanged", Weight = 1 };
        return Definition() with
        {
            Tests = new TestSuite
            {
                Public = [bucket == "public" ? altered : unchanged],
                Private = [bucket == "private" ? altered : unchanged],
            },
        };
    }

    private static CodingAssignmentContent WithWeight(string family, string bucket, double weight)
    {
        var definition = family == "function" ? FunctionDefinition(FunctionParameterType.Integer, "2") : WithStandardTest(bucket);
        var target = bucket == "public" ? definition.Tests.Public : definition.Tests.Private;
        target[0] = target[0] with { Weight = weight };
        return definition;
    }

    private static CodingAssignmentContent FunctionWithNames(string name, IReadOnlyList<string?> parameterNames)
    {
        var definition = FunctionDefinition(FunctionParameterType.Integer, "2");
        var execution = definition.Execution!;

        Test Rename(Test test)
        {
            var group = (FunctionalTestGroup)test;
            return group with
            {
                Function = group.Function with
                {
                    FunctionName = name,
                    Parameters = parameterNames.Select(parameter => new FunctionParameterWithName
                    {
                        Name = parameter!, Type = FunctionParameterType.Integer,
                    }).ToList(),
                },
                Cases = group.Cases.Select(testCase => testCase with
                {
                    Inputs = parameterNames.Select(_ => new FunctionParameter
                    {
                        Type = FunctionParameterType.Integer, Content = JsonSerializer.SerializeToElement(2),
                    }).ToArray(),
                }).ToArray(),
            };
        }

        return definition with
        {
            Execution = execution with { EntryPoint = execution.EntryPoint with { Symbol = name } },
            Tests = new TestSuite
            {
                Public = definition.Tests.Public.Select(Rename).ToList(),
                Private = definition.Tests.Private.Select(Rename).ToList(),
            },
        };
    }

    private static CodingAssignmentContent Definition() => new()
    {
        ExerciseType = "standard-io",
        Environment = new CodingEnvironment { Language = "cpp", Runtime = "wasi", Tools = "clang" },
        Execution = new CodingExecutionConfiguration
        {
            Limits = new CodingExecutionLimits { TimeLimitMs = 1379, OutputLimitBytes = 65521, MaxFileCount = 7 },
            EntryPoint = new CodingExecutionEntryPoint { Path = "/home/user/solution.cpp" },
            Toolchain = new CodingExecutionToolchain
            {
                Key = "cpp-emception", Version = "4.5.2", RuntimeAbi = "emception-browser-v1",
                ArtifactDigest = "sha256:405e9f1fdd936ba31e11247fa1f901eaee24c4fcf641e343505df75c3d539b37",
            },
        },
        Data = new WorkspaceData
        {
            Files = new() { ["/home/user/solution.cpp"] = new BundleFileMeta { Content = "int identity(int value){return value;} int main(){}" } },
        },
        Tests = new TestSuite { Public = [new StandardTest { Stdout = "ok" }], Private = [] },
        Grading = new GradingConfig { MaxScore = 100 },
    };

    private static CodingAssignmentContent FunctionDefinition(FunctionParameterType type, string json)
    {
        using var document = JsonDocument.Parse(json);
        var value = new FunctionParameter { Type = type, Content = document.RootElement.Clone() };
        var group = new FunctionalTestGroup
        {
            Function = new TestFunctionData
            {
                FunctionName = "identity",
                Parameters = [new FunctionParameterWithName { Name = "value", Type = type }],
                ReturnType = new FunctionReturnType { Type = type },
            },
            Cases = [new FunctionalTestCase { Inputs = [value], Expected = value }],
        };
        var definition = Definition();
        var execution = definition.Execution!;
        return definition with
        {
            ExerciseType = "function",
            Execution = execution with { EntryPoint = execution.EntryPoint with { Symbol = "identity" } },
            Tests = new TestSuite { Public = [group], Private = [group] },
        };
    }

    private static CodingAssignmentContent ReplaceCaseValue(CodingAssignmentContent definition, string bucket, string slot, FunctionParameter value)
    {
        var tests = definition.Tests with { Public = definition.Tests.Public.ToList(), Private = definition.Tests.Private.ToList() };
        var target = bucket == "public" ? tests.Public : tests.Private;
        var group = (FunctionalTestGroup)target.Single();
        var testCase = group.Cases.Single();
        target[0] = group with
        {
            Cases = [slot == "input" ? testCase with { Inputs = [value] } : testCase with { Expected = value }],
        };
        return definition with { Tests = tests };
    }

    private static CodingAssignmentContent AsLegacy(CodingAssignmentContent definition) => definition with
    {
        Execution = null,
        Environment = definition.Environment with { Runtime = null },
    };

    private static string ValidScalarJson(FunctionParameterType type) => type switch
    {
        FunctionParameterType.String => "\"text\"",
        FunctionParameterType.Boolean => "false",
        FunctionParameterType.Integer => "2",
        FunctionParameterType.Float => "\"2.5\"",
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    private static void AssertScalar(JsonElement actual, string json)
    {
        using var expected = JsonDocument.Parse(json);
        actual.ValueKind.Should().Be(expected.RootElement.ValueKind);
        if (actual.ValueKind == JsonValueKind.String)
        {
            actual.GetString().Should().Be(expected.RootElement.GetString());
        }
        else if (actual.ValueKind == JsonValueKind.Number)
        {
            actual.GetDouble().Should().Be(expected.RootElement.GetDouble());
        }
        else
        {
            actual.GetRawText().Should().Be(json);
        }
    }

    private static void AssertValid(CodingAssignmentContent definition) =>
        new CodingAssignmentContentValidator().Validate(definition).Errors.Should().BeEmpty();

    private static void AssertInvalid(CodingAssignmentContent definition) =>
        new CodingAssignmentContentValidator().Validate(definition).IsValid.Should().BeFalse();
}
