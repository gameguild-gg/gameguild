using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests;

/// <summary>
/// Tests for v1 <see cref="CodingAssignmentContent"/> schema: polymorphic serialization, validator, round-trip.
/// Acceptance criteria (a-d) from .omo/plans/coding-assessment-lifecycle.md todo 1.
/// </summary>
public class CodingAssignmentContentTests
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // ── (a) round-trip: complete content with both test kinds in Public, 1 in Private, mixed visibility ──

    [Fact]
    public void ValidContent_RoundTrips_ThroughJson()
    {
        var content = CreateValid();

        var json = JsonSerializer.Serialize(content, s_jsonOptions);
        var deserialized = JsonSerializer.Deserialize<CodingAssignmentContent>(json, s_jsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.Type.Should().Be("coding-assignment");
        deserialized.Version.Should().Be(1);
        deserialized.Environment.Language.Should().Be("cpp");
        deserialized.Data.Files.Should().HaveCount(2);
        deserialized.Tests.Public.Should().HaveCount(2);
        deserialized.Tests.Private.Should().HaveCount(1);
        deserialized.Grading.MaxScore.Should().Be(100);
    }

    // ── (d) JsonPolymorphic round-trips Test subclasses via the kind discriminator ─────────────────────

    [Fact]
    public void RoundTrip_PreservesTestKindDiscriminator()
    {
        var content = CreateValid();
        var json = JsonSerializer.Serialize(content, s_jsonOptions);

        using var doc = JsonDocument.Parse(json);
        var pub = doc.RootElement.GetProperty("Tests").GetProperty("Public");

        pub[0].GetProperty("kind").GetString().Should().Be("standard");
        pub[1].GetProperty("kind").GetString().Should().Be("functional");
        doc.RootElement.GetProperty("Tests").GetProperty("Private")[0].GetProperty("kind").GetString().Should().Be("standard");
    }

    [Fact]
    public void RoundTrip_DeserializesCorrectTestSubclasses()
    {
        var content = CreateValid();
        var json = JsonSerializer.Serialize(content, s_jsonOptions);
        var deserialized = JsonSerializer.Deserialize<CodingAssignmentContent>(json, s_jsonOptions);

        deserialized!.Tests.Public[0].Should().BeOfType<StandardTest>();
        deserialized.Tests.Public[1].Should().BeOfType<FunctionalTestGroup>();
        deserialized.Tests.Private[0].Should().BeOfType<StandardTest>();
    }

    [Fact]
    public void RoundTrip_FunctionalParameterTypeSerializesAsCamelCaseString()
    {
        var content = CreateValid();
        var json = JsonSerializer.Serialize(content, s_jsonOptions);

        using var doc = JsonDocument.Parse(json);
        var fnParam = doc.RootElement.GetProperty("Tests").GetProperty("Public")[1]
            .GetProperty("Function").GetProperty("Parameters")[0];

        fnParam.GetProperty("Type").GetString().Should().Be("integer");
        fnParam.GetProperty("Name").GetString().Should().Be("a");
    }

    // ── (b) validator accepts minimal valid payload ──────────────────────────────────────────────────

    [Fact]
    public void Validator_Accepts_MinimalValidPayload()
    {
        var errors = Validate(CreateMinimalValid());
        errors.Should().BeEmpty();
    }

    // ── (c) validator rejects: empty Tests.Public + Tests.Private → at_least_one_test ────────────────

    [Fact]
    public void Validator_Rejects_NoTests()
    {
        var content = CreateMinimalValid() with { Tests = new TestSuite() };
        var errors = Validate(content);
        errors.Should().Contain(e => e.ErrorCode == "at_least_one_test");
    }

    // ── (c) Modifiable==true && Visibility=="Private" → private_file_not_modifiable ──────────────────

    [Fact]
    public void Validator_Rejects_PrivateModifiableFile()
    {
        var content = CreateMinimalValid() with
        {
            Data = new WorkspaceData
            {
                Files = new()
                {
                    ["secret.h"] = new BundleFileMeta { Content = "x", Visibility = "Private", Modifiable = true }
                }
            }
        };
        var errors = Validate(content);
        errors.Should().Contain(e => e.ErrorCode == "private_file_not_modifiable");
    }

    // ── (c) Visibility == "Solution" → invalid_visibility_value ──────────────────────────────────────

    [Fact]
    public void Validator_Rejects_SolutionVisibility()
    {
        var content = CreateMinimalValid() with
        {
            Data = new WorkspaceData
            {
                Files = new()
                {
                    ["sol.cpp"] = new BundleFileMeta { Content = "x", Visibility = "Solution", Modifiable = false }
                }
            }
        };
        var errors = Validate(content);
        errors.Should().Contain(e => e.ErrorCode == "invalid_visibility_value");
    }

    // ── (c) FunctionalTestGroup with non-v1 parameter type → functional_param_type_not_supported_v1 ─

    [Fact]
    public void Validator_Rejects_FunctionalNonV1ParameterType()
    {
        var content = CreateMinimalValidWithFunctional("add") with
        {
            Tests = new TestSuite
            {
                Public = new()
                {
                    new FunctionalTestGroup
                    {
                        Function = new TestFunctionData
                        {
                            FunctionName = "add",
                            Parameters = new()
                            {
                                new FunctionParameterWithName
                                {
                                    Name = "a",
                                    Type = (FunctionParameterType)4, // out-of-v1-set
                                    Content = JsonSerializer.SerializeToElement(0)
                                }
                            },
                            ReturnType = new FunctionParameter
                            {
                                Type = FunctionParameterType.Integer,
                                Content = JsonSerializer.SerializeToElement(0)
                            }
                        },
                        Cases = new[]
                        {
                            new FunctionalTestCase
                            {
                                Inputs = new[]
                                {
                                    new FunctionParameter
                                    {
                                        Type = FunctionParameterType.Integer,
                                        Content = JsonSerializer.SerializeToElement(0)
                                    }
                                },
                                Expected = new FunctionParameter
                                {
                                    Type = FunctionParameterType.Integer,
                                    Content = JsonSerializer.SerializeToElement(0)
                                }
                            }
                        }
                    }
                }
            }
        };
        var errors = Validate(content);
        errors.Should().Contain(e => e.ErrorCode == "functional_param_type_not_supported_v1");
    }

    // ── (c) bad FunctionName (add+, ns::add) → invalid_function_name ─────────────────────────────────

    [Theory]
    [InlineData("add+")]
    [InlineData("ns::add")]
    public void Validator_Rejects_BadFunctionName(string name)
    {
        var content = CreateMinimalValidWithFunctional(name);
        var errors = Validate(content);
        errors.Should().Contain(e => e.ErrorCode == "invalid_function_name");
    }

    // ── (c) negative Weight → weight_non_negative ────────────────────────────────────────────────────

    [Fact]
    public void Validator_Rejects_NegativeWeight()
    {
        var content = CreateMinimalValid() with
        {
            Tests = new TestSuite
            {
                Public = new() { new StandardTest { Stdout = "ok", Weight = -1.0 } }
            }
        };
        var errors = Validate(content);
        errors.Should().Contain(e => e.ErrorCode == "weight_non_negative");
    }

    // ── (c) MaxScore <= 0 → max_score_positive ───────────────────────────────────────────────────────

    [Fact]
    public void Validator_Rejects_NonPositiveMaxScore()
    {
        var content = CreateMinimalValid() with { Grading = new GradingConfig { MaxScore = 0 } };
        var errors = Validate(content);
        errors.Should().Contain(e => e.ErrorCode == "max_score_positive");
    }

    // ── (c) bad Language → invalid_language ───────────────────────────────────────────────────────────

    [Fact]
    public void Validator_Rejects_BadLanguage()
    {
        var content = CreateMinimalValid() with
        {
            Environment = new CodingEnvironment { Language = "rust", Tools = "clang" }
        };
        var errors = Validate(content);
        errors.Should().Contain(e => e.ErrorCode == "invalid_language");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────────────────────────

    private static CodingAssignmentContent CreateMinimalValid() => new()
    {
        Environment = new CodingEnvironment { Language = "cpp", Tools = "clang" },
        Data = new WorkspaceData
        {
            Files = new()
            {
                ["main.cpp"] = new BundleFileMeta { Content = "int main(){}", Visibility = "Public", Modifiable = true }
            }
        },
        Tests = new TestSuite
        {
            Public = new() { new StandardTest { Stdout = "ok" } }
        },
        Grading = new GradingConfig { MaxScore = 100 }
    };

    private static CodingAssignmentContent CreateMinimalValidWithFunctional(string functionName) => new()
    {
        Environment = new CodingEnvironment { Language = "cpp", Tools = "clang" },
        Data = new WorkspaceData(),
        Tests = new TestSuite
        {
            Public = new()
            {
                new FunctionalTestGroup
                {
                    Function = new TestFunctionData
                    {
                        FunctionName = functionName,
                        Parameters = new(),
                        ReturnType = new FunctionParameter
                        {
                            Type = FunctionParameterType.Integer,
                            Content = JsonSerializer.SerializeToElement(0)
                        }
                    },
                    Cases = new[]
                    {
                        new FunctionalTestCase
                        {
                            Inputs = Array.Empty<FunctionParameter>(),
                            Expected = new FunctionParameter
                            {
                                Type = FunctionParameterType.Integer,
                                Content = JsonSerializer.SerializeToElement(0)
                            }
                        }
                    }
                }
            }
        },
        Grading = new GradingConfig { MaxScore = 100 }
    };

    private static CodingAssignmentContent CreateValid() => new()
    {
        Environment = new CodingEnvironment { Language = "cpp", Tools = "clang", LibBundle = "sdl3" },
        Data = new WorkspaceData
        {
            Files = new()
            {
                ["main.cpp"] = new BundleFileMeta { Content = "int main(){}", Visibility = "Public", Modifiable = true },
                ["tests/secret.cpp"] = new BundleFileMeta { Content = "ans", Visibility = "Private", Modifiable = false }
            }
        },
        Tests = new TestSuite
        {
            Public = new()
            {
                new StandardTest { Stdout = "ok", Name = "first" },
                new FunctionalTestGroup
                {
                    Function = new TestFunctionData
                    {
                        FunctionName = "add",
                        Parameters = new()
                        {
                            new FunctionParameterWithName
                            {
                                Name = "a",
                                Type = FunctionParameterType.Integer,
                                Content = JsonSerializer.SerializeToElement(0)
                            }
                        },
                        ReturnType = new FunctionParameter
                        {
                            Type = FunctionParameterType.Integer,
                            Content = JsonSerializer.SerializeToElement(0)
                        }
                    },
                    Cases = new[]
                    {
                        new FunctionalTestCase
                        {
                            Inputs = new[]
                            {
                                new FunctionParameter
                                {
                                    Type = FunctionParameterType.Integer,
                                    Content = JsonSerializer.SerializeToElement(0)
                                }
                            },
                            Expected = new FunctionParameter
                            {
                                Type = FunctionParameterType.Integer,
                                Content = JsonSerializer.SerializeToElement(0)
                            }
                        }
                    }
                }
            },
            Private = new()
            {
                new StandardTest { Stdout = "ok2" }
            }
        },
        Grading = new GradingConfig { MaxScore = 100 }
    };

    private static List<ValidationFailure> Validate(CodingAssignmentContent content)
    {
        var validator = new CodingAssignmentContentValidator();
        return validator.Validate(content).Errors;
    }
}
