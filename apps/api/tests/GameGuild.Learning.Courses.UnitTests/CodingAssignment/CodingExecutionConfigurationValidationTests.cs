using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.CodingAssignment;

public sealed class CodingExecutionConfigurationValidationTests
{
    [Theory]
    [InlineData("c", "wasi", "clang", null, "c-emception")]
    [InlineData("cpp", "wasi", "clang", null, "cpp-emception")]
    [InlineData("cpp", "sdl3", "emcc", "sdl3", "cpp-emception")]
    [InlineData("cpp", "raylib", "emcc", "raylib", "cpp-emception")]
    [InlineData("cpp", "allegro5", "emcc", "allegro5", "cpp-emception")]
    public void ExplicitConfiguration_AcceptsOnlySupportedEnvironmentCombinations(
        string language, string runtime, string tools, string? bundle, string key)
    {
        var definition = Definition();
        var execution = definition.Execution!;
        definition = definition with
        {
            Environment = new CodingEnvironment { Language = language, Runtime = runtime, Tools = tools, LibBundle = bundle },
            Execution = execution with { Toolchain = execution.Toolchain with { Key = key } },
        };

        AssertValid(definition);
    }

    [Theory]
    [InlineData("cpp")]
    [InlineData("c")]
    [InlineData("sdl-cpp")]
    [InlineData("raylib-cpp")]
    [InlineData("allegro-cpp")]
    public void AbsentExecution_PreservesLegacyLanguageAndMixedTestInterpretation(string language)
    {
        var definition = Definition() with
        {
            Execution = null,
            Environment = new CodingEnvironment { Language = language, Tools = "clang", LibBundle = "historical-bundle" },
            Tests = new TestSuite { Public = [new StandardTest { Stdout = "ok" }, Function()], Private = [] },
        };

        AssertValid(definition);
        var json = JsonSerializer.SerializeToElement(definition);
        json.TryGetProperty("Execution", out _).Should().BeFalse();
        json.GetProperty("Environment").TryGetProperty("Runtime", out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("wasi")]
    [InlineData("sdl3")]
    [InlineData("")]
    [InlineData(" ")]
    public void RuntimeWithoutExecution_IsNotLegacyConfiguration(string runtime)
    {
        var definition = Definition();

        AssertInvalid(definition with { Execution = null, Environment = definition.Environment with { Runtime = runtime } });
    }

    [Theory]
    [InlineData("function")]
    [InlineData("standard-io")]
    public void ExplicitConfiguration_RoundTripsExactNondefaultValuesWithoutFabricatingMemory(string family)
    {
        var json = JsonSerializer.SerializeToElement(Definition(family));
        var execution = json.GetProperty("Execution");
        execution.GetProperty("Limits").GetProperty("TimeLimitMs").GetInt32().Should().Be(1379);
        execution.GetProperty("Limits").GetProperty("OutputLimitBytes").GetInt32().Should().Be(65521);
        execution.GetProperty("Limits").GetProperty("MaxFileCount").GetInt32().Should().Be(7);
        execution.GetProperty("Limits").TryGetProperty("MemoryLimitBytes", out _).Should().BeFalse();
        execution.GetProperty("EntryPoint").GetProperty("Path").GetString().Should().Be("/home/user/src/solution.cpp");
        if (family == "function")
        {
            execution.GetProperty("EntryPoint").GetProperty("Symbol").GetString().Should().Be("sum");
        }
        else
        {
            execution.GetProperty("EntryPoint").TryGetProperty("Symbol", out _).Should().BeFalse();
        }
        execution.GetProperty("Toolchain").GetProperty("Key").GetString().Should().Be("cpp-emception");
        execution.GetProperty("Toolchain").GetProperty("Version").GetString().Should().Be("4.5.2");
        execution.GetProperty("Toolchain").GetProperty("RuntimeAbi").GetString().Should().Be("emception-browser-v1");
        execution.GetProperty("Toolchain").GetProperty("ArtifactDigest").GetString().Should()
            .Be("sha256:405e9f1fdd936ba31e11247fa1f901eaee24c4fcf641e343505df75c3d539b37");

        var restored = json.Deserialize<CodingAssignmentContent>()!;
        AssertValid(restored);
        JsonSerializer.SerializeToElement(restored).GetRawText().Should().Be(json.GetRawText());
    }

    [Theory]
    [InlineData("environment")]
    [InlineData("limits")]
    [InlineData("entry-point")]
    [InlineData("toolchain")]
    public void ExplicitConfiguration_RejectsNullRequiredSectionsWithoutThrowing(string section)
    {
        var definition = Definition();
        var execution = definition.Execution!;
        definition = section switch
        {
            "environment" => definition with { Environment = null! },
            "limits" => definition with { Execution = execution with { Limits = null! } },
            "entry-point" => definition with { Execution = execution with { EntryPoint = null! } },
            "toolchain" => definition with { Execution = execution with { Toolchain = null! } },
            _ => throw new ArgumentOutOfRangeException(nameof(section)),
        };

        AssertInvalid(definition);
    }

    [Theory]
    [InlineData("cpp", null, "clang", null)]
    [InlineData("cpp", "", "clang", null)]
    [InlineData("cpp", "WASI", "clang", null)]
    [InlineData("cpp", "node", "clang", null)]
    [InlineData("CPP", "wasi", "clang", null)]
    [InlineData("sdl-cpp", "sdl3", "emcc", "sdl3")]
    [InlineData("raylib-cpp", "raylib", "emcc", "raylib")]
    [InlineData("allegro-cpp", "allegro5", "emcc", "allegro5")]
    [InlineData("c", "sdl3", "emcc", "sdl3")]
    [InlineData("c", "raylib", "emcc", "raylib")]
    [InlineData("c", "allegro5", "emcc", "allegro5")]
    [InlineData("cpp", "wasi", "emcc", null)]
    [InlineData("cpp", "wasi", "clang", "sdl3")]
    [InlineData("cpp", "wasi", "clang", "")]
    [InlineData("cpp", "sdl3", "clang", "sdl3")]
    [InlineData("cpp", "sdl3", "emcc", null)]
    [InlineData("cpp", "sdl3", "emcc", "raylib")]
    [InlineData("cpp", "raylib", "emcc", "sdl3")]
    [InlineData("cpp", "allegro5", "emcc", "allegro")]
    public void ExplicitConfiguration_RejectsMissingUnsupportedOrMismatchedEnvironment(
        string language, string? runtime, string tools, string? bundle)
    {
        var definition = Definition();
        var execution = definition.Execution!;
        definition = definition with
        {
            Environment = new CodingEnvironment { Language = language, Runtime = runtime, Tools = tools, LibBundle = bundle },
            Execution = execution with
            {
                Toolchain = execution.Toolchain with { Key = language == "c" ? "c-emception" : "cpp-emception" },
            },
        };

        AssertInvalid(definition);
    }

    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(30000, 2000000, 100)]
    public void ExplicitLimits_AcceptInclusiveSupportedBounds(int time, int output, int files)
    {
        var definition = Definition();
        definition = definition with
        {
            Data = new WorkspaceData { Files = new() { ["/home/user/src/solution.cpp"] = new() { Content = "int main(){}" } } },
            Execution = definition.Execution! with
            {
                Limits = new CodingExecutionLimits { TimeLimitMs = time, OutputLimitBytes = output, MaxFileCount = files },
            },
        };

        AssertValid(definition);
    }

    [Theory]
    [InlineData("time", -1)]
    [InlineData("time", 0)]
    [InlineData("time", 30001)]
    [InlineData("time", int.MaxValue)]
    [InlineData("output", -1)]
    [InlineData("output", 0)]
    [InlineData("output", 2000001)]
    [InlineData("output", int.MaxValue)]
    [InlineData("files", -1)]
    [InlineData("files", 0)]
    [InlineData("files", 1)]
    [InlineData("files", 101)]
    public void ExplicitLimits_RejectOutOfRangeOrBelowAuthoredFileCount(string field, int value)
    {
        var definition = Definition();
        var execution = definition.Execution!;
        var limits = field switch
        {
            "time" => execution.Limits with { TimeLimitMs = value },
            "output" => execution.Limits with { OutputLimitBytes = value },
            "files" => execution.Limits with { MaxFileCount = value },
            _ => throw new ArgumentOutOfRangeException(nameof(field)),
        };

        AssertInvalid(definition with { Execution = execution with { Limits = limits } });
    }

    [Theory]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public void ExplicitLimits_CountAllAuthoredFilesIncludingPrivateFiles(int count, bool expectedValid)
    {
        var definition = Definition();
        var execution = definition.Execution!;
        for (var index = definition.Data.Files.Count; index < count; index++)
        {
            definition.Data.Files.Add($"support/{index}.h", new BundleFileMeta { Content = "// support", Modifiable = false });
        }
        definition = definition with
        {
            Execution = execution with { Limits = execution.Limits with { MaxFileCount = 100 } },
        };

        new CodingAssignmentContentValidator().Validate(definition).IsValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData(-1L)]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(134217728L)]
    [InlineData(long.MaxValue)]
    public void ConfiguredMemory_IsAlwaysRejectedAndRemainsRecoverable(long memory)
    {
        var definition = Definition();
        var execution = definition.Execution!;
        definition = definition with
        {
            Execution = execution with { Limits = execution.Limits with { MemoryLimitBytes = memory } },
        };
        var json = JsonSerializer.SerializeToElement(definition);

        AssertInvalid(definition);
        json.GetProperty("Execution").GetProperty("Limits").GetProperty("MemoryLimitBytes").GetInt64().Should().Be(memory);
        json.Deserialize<CodingAssignmentContent>()!.Execution!.Limits.MemoryLimitBytes.Should().Be(memory);
    }

    [Theory]
    [InlineData("key", null)]
    [InlineData("key", "")]
    [InlineData("key", "c-emception")]
    [InlineData("key", "cpp-emception ")]
    [InlineData("version", null)]
    [InlineData("version", "4.4.0")]
    [InlineData("version", "4.5.2 ")]
    [InlineData("abi", null)]
    [InlineData("abi", "emception-browser-v2")]
    [InlineData("digest", null)]
    [InlineData("digest", "405e9f1fdd936ba31e11247fa1f901eaee24c4fcf641e343505df75c3d539b37")]
    [InlineData("digest", "sha256:0000000000000000000000000000000000000000000000000000000000000000")]
    [InlineData("digest", "SHA256:405e9f1fdd936ba31e11247fa1f901eaee24c4fcf641e343505df75c3d539b37")]
    public void ExplicitToolchain_RejectsIncompleteOrNonexactIdentity(string field, string? value)
    {
        var definition = Definition();
        var execution = definition.Execution!;
        var toolchain = field switch
        {
            "key" => execution.Toolchain with { Key = value! },
            "version" => execution.Toolchain with { Version = value! },
            "abi" => execution.Toolchain with { RuntimeAbi = value! },
            "digest" => execution.Toolchain with { ArtifactDigest = value! },
            _ => throw new ArgumentOutOfRangeException(nameof(field)),
        };

        AssertInvalid(definition with { Execution = execution with { Toolchain = toolchain } });
    }

    [Theory]
    [InlineData("src/solution.cpp")]
    [InlineData("/user/src/solution.cpp")]
    [InlineData("/home/user/src/solution.cpp")]
    public void EntryPoint_UsesWorkspaceNormalizationAndAllowsPublicReadonlySource(string path)
    {
        var definition = Definition();
        definition.Data.Files["/home/user/src/solution.cpp"] = definition.Data.Files["/home/user/src/solution.cpp"] with { Modifiable = false };

        AssertValid(definition with { Execution = definition.Execution! with { EntryPoint = new CodingExecutionEntryPoint { Path = path } } });
    }

    [Theory]
    [InlineData("solution.c")]
    [InlineData("solution.cc")]
    [InlineData("solution.cpp")]
    [InlineData("solution.cxx")]
    public void EntryPoint_AcceptsEverySupportedNativeSourceExtension(string path)
    {
        var definition = Definition();
        definition = definition with
        {
            Data = new WorkspaceData { Files = new() { [path] = new() { Content = "int main(){}" } } },
            Execution = definition.Execution! with { EntryPoint = new CodingExecutionEntryPoint { Path = path } },
        };

        AssertValid(definition);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("../solution.cpp")]
    [InlineData("src/../solution.cpp")]
    [InlineData("/home/user/../solution.cpp")]
    [InlineData("/etc/solution.cpp")]
    [InlineData("C:/solution.cpp")]
    [InlineData("src\\solution.cpp")]
    [InlineData("src//solution.cpp")]
    [InlineData("functional_0_test.cpp")]
    public void EntryPoint_RejectsInvalidOrReservedPathsWithoutThrowing(string? path)
    {
        var definition = Definition();

        AssertInvalid(definition with { Execution = definition.Execution! with { EntryPoint = new CodingExecutionEntryPoint { Path = path! } } });
    }

    [Theory]
    [InlineData("private")]
    [InlineData("base64")]
    [InlineData("header")]
    [InlineData("image")]
    [InlineData("missing")]
    public void EntryPoint_MustIdentifyAnAuthoredPublicTextNativeSource(string mutation)
    {
        var definition = Definition();
        const string sourcePath = "/home/user/src/solution.cpp";
        var path = sourcePath;
        var file = definition.Data.Files[sourcePath];
        if (mutation == "private")
        {
            definition.Data.Files[sourcePath] = file with { Visibility = "Private", Modifiable = false };
        }
        if (mutation == "base64")
        {
            definition.Data.Files[sourcePath] = file with { Encoding = "base64", Content = "aW50IG1haW4oKXt9" };
        }
        if (mutation is "header" or "image")
        {
            path = mutation == "header" ? "src/solution.h" : "src/solution.png";
            definition.Data.Files.Remove(sourcePath);
            definition.Data.Files.Add(path, file);
        }
        if (mutation == "missing")
        {
            path = "src/absent.cpp";
        }

        AssertInvalid(definition with { Execution = definition.Execution! with { EntryPoint = new CodingExecutionEntryPoint { Path = path } } });
    }

    [Theory]
    [InlineData("src/solution.cpp")]
    [InlineData("/user/src/solution.cpp")]
    public void ExplicitWorkspace_RejectsAliasedAuthoredPaths(string duplicate)
    {
        var definition = Definition();
        definition.Data.Files.Add(duplicate, new BundleFileMeta { Content = "// alias" });

        AssertInvalid(definition);
    }

    [Theory]
    [InlineData("../unused.h")]
    [InlineData("support/../unused.h")]
    [InlineData("/etc/unused.h")]
    public void ExplicitWorkspace_RejectsTraversalEvenOutsideTheEntryPoint(string path)
    {
        var definition = Definition();
        definition.Data.Files.Add(path, new BundleFileMeta { Content = "// unused", Modifiable = false });

        AssertInvalid(definition);
    }

    [Fact]
    public void FunctionConfiguration_AcceptsIndependentGroupsWithTheSameSignature()
    {
        AssertValid(Definition("function"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("other")]
    [InlineData("ns::sum")]
    public void FunctionConfiguration_RequiresTheExactSignatureSymbol(string? symbol)
    {
        var definition = Definition("function");
        var execution = definition.Execution!;

        AssertInvalid(definition with { Execution = execution with { EntryPoint = execution.EntryPoint with { Symbol = symbol } } });
    }

    [Theory]
    [InlineData("name")]
    [InlineData("return")]
    [InlineData("parameter-name")]
    [InlineData("parameter-type")]
    [InlineData("parameter-order")]
    [InlineData("arity")]
    [InlineData("null-parameter")]
    public void FunctionConfiguration_RejectsIncoherentSignaturesAcrossPublicAndPrivateGroups(string mutation)
    {
        var definition = Definition("function");
        var group = (FunctionalTestGroup)definition.Tests.Private.Single();
        var signature = group.Function;
        var altered = mutation switch
        {
            "name" => signature with { FunctionName = "other" },
            "return" => signature with { ReturnType = new FunctionReturnType { Type = FunctionParameterType.Boolean } },
            "parameter-name" => signature with { Parameters = [signature.Parameters[0] with { Name = "renamed" }, signature.Parameters[1]] },
            "parameter-type" => signature with { Parameters = [signature.Parameters[0] with { Type = FunctionParameterType.Float }, signature.Parameters[1]] },
            "parameter-order" => signature with { Parameters = signature.Parameters.AsEnumerable().Reverse().ToList() },
            "arity" => signature with { Parameters = [signature.Parameters[0]] },
            "null-parameter" => signature with { Parameters = [null!, signature.Parameters[1]] },
            _ => throw new ArgumentOutOfRangeException(nameof(mutation)),
        };
        var testCase = group.Cases.Single();
        if (mutation == "arity")
        {
            testCase = testCase with { Inputs = [testCase.Inputs[0]] };
        }
        if (mutation == "return")
        {
            testCase = testCase with { Expected = new FunctionParameter { Type = FunctionParameterType.Boolean, Content = JsonSerializer.SerializeToElement(true) } };
        }
        if (mutation == "parameter-type")
        {
            testCase = testCase with { Inputs = [new FunctionParameter { Type = FunctionParameterType.Float, Content = JsonSerializer.SerializeToElement(2.5) }, testCase.Inputs[1]] };
        }
        definition.Tests.Private[0] = group with { Function = altered, Cases = [testCase] };

        AssertInvalid(definition);
    }

    [Fact]
    public void FunctionConfiguration_RejectsAnEmptyPlanWithNoSignature()
    {
        AssertInvalid(Definition("function") with { Tests = new TestSuite() });
    }

    [Theory]
    [InlineData("function", "public")]
    [InlineData("function", "private")]
    [InlineData("standard-io", "public")]
    [InlineData("standard-io", "private")]
    public void ExplicitFamily_RejectsTheOtherTestKindInEitherBucketWithoutProblemMetadata(string family, string bucket)
    {
        var definition = Definition(family);
        Test wrong = family == "function" ? new StandardTest { Stdout = "ok" } : Function();
        if (bucket == "public")
        {
            definition.Tests.Public.Add(wrong);
        }
        else
        {
            definition.Tests.Private.Add(wrong);
        }

        AssertInvalid(definition);
    }

    [Theory]
    [InlineData("")]
    [InlineData("main")]
    [InlineData("sum")]
    public void StandardIoConfiguration_RejectsAnyConfiguredSymbol(string symbol)
    {
        var definition = Definition();
        var execution = definition.Execution!;

        AssertInvalid(definition with { Execution = execution with { EntryPoint = execution.EntryPoint with { Symbol = symbol } } });
    }

    private static CodingAssignmentContent Definition(string family = "standard-io") => new()
    {
        ExerciseType = family,
        Environment = new CodingEnvironment { Language = "cpp", Runtime = "wasi", Tools = "clang" },
        Execution = new CodingExecutionConfiguration
        {
            Limits = new CodingExecutionLimits { TimeLimitMs = 1379, OutputLimitBytes = 65521, MaxFileCount = 7 },
            EntryPoint = new CodingExecutionEntryPoint { Path = "/home/user/src/solution.cpp", Symbol = family == "function" ? "sum" : null },
            Toolchain = new CodingExecutionToolchain
            {
                Key = "cpp-emception", Version = "4.5.2", RuntimeAbi = "emception-browser-v1",
                ArtifactDigest = "sha256:405e9f1fdd936ba31e11247fa1f901eaee24c4fcf641e343505df75c3d539b37",
            },
        },
        Data = new WorkspaceData
        {
            Files = new()
            {
                ["/home/user/src/solution.cpp"] = new() { Content = "int sum(int a,int b){return a+b;} int main(){}" },
                ["support/private.h"] = new() { Content = "// private", Visibility = "Private", Modifiable = false },
            },
        },
        Tests = family == "function"
            ? new TestSuite { Public = [Function()], Private = [Function()] }
            : new TestSuite { Public = [new StandardTest { Stdout = "ok" }], Private = [new StandardTest { Stdout = "private" }] },
        Grading = new GradingConfig { MaxScore = 100 },
    };

    private static FunctionalTestGroup Function() => new()
    {
        Function = new TestFunctionData
        {
            FunctionName = "sum",
            Parameters = [new() { Name = "a", Type = FunctionParameterType.Integer }, new() { Name = "b", Type = FunctionParameterType.Integer }],
            ReturnType = new FunctionReturnType { Type = FunctionParameterType.Integer },
        },
        Cases = [new FunctionalTestCase
        {
            Inputs = [new() { Type = FunctionParameterType.Integer, Content = JsonSerializer.SerializeToElement(2) }, new() { Type = FunctionParameterType.Integer, Content = JsonSerializer.SerializeToElement(3) }],
            Expected = new FunctionParameter { Type = FunctionParameterType.Integer, Content = JsonSerializer.SerializeToElement(5) },
        }],
    };

    private static void AssertValid(CodingAssignmentContent definition) =>
        new CodingAssignmentContentValidator().Validate(definition).Errors.Should().BeEmpty();

    private static void AssertInvalid(CodingAssignmentContent definition) =>
        new CodingAssignmentContentValidator().Validate(definition).IsValid.Should().BeFalse();
}
