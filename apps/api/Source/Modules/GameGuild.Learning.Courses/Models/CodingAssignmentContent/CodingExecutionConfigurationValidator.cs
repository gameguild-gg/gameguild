using System.Text.Json;
using System.Text.RegularExpressions;

namespace GameGuild.Learning.Courses;

internal static class CodingExecutionConfigurationValidator
{
    private static readonly Regex s_decimal = new(@"^-?(?:0|[1-9][0-9]*)(?:\.[0-9]+)?(?:[eE][+-]?[0-9]+)?$", RegexOptions.Compiled);
    private static readonly Regex s_identifier = new(@"\A[A-Za-z_][A-Za-z0-9_]{0,127}\z", RegexOptions.Compiled);

    public static bool IsValid(CodingAssignmentContent content)
    {
        if (content.Execution is not { } execution)
        {
            return content.Environment?.Runtime is null;
        }

        var environment = content.Environment;
        var limits = execution.Limits;
        var entryPoint = execution.EntryPoint;
        var toolchain = execution.Toolchain;
        var files = content.Data?.Files;
        if (environment is null || limits is null || entryPoint is null || toolchain is null || files is null
            || content.Tests?.Public is null || content.Tests.Private is null)
        {
            return false;
        }

        if (!HasSupportedEnvironment(environment)
            || limits.TimeLimitMs is <= 0 or > CodingExecutionConfiguration.MaxTimeLimitMs
            || limits.OutputLimitBytes is <= 0 or > CodingExecutionConfiguration.MaxOutputLimitBytes
            || limits.MaxFileCount is <= 0 or > 100 || files.Count > limits.MaxFileCount
            || limits.MemoryLimitBytes is not null
            || toolchain.Key != environment.Language + "-emception"
            || toolchain.Version != CodingExecutionConfiguration.ArtifactVersion
            || toolchain.RuntimeAbi != CodingExecutionConfiguration.RuntimeAbi
            || toolchain.ArtifactDigest != "sha256:" + CodingExecutionConfiguration.ToolchainLockHash)
        {
            return false;
        }

        return HasValidWorkspace(files, entryPoint) && HasCoherentTestPlan(content, entryPoint);
    }

    private static bool HasSupportedEnvironment(CodingEnvironment environment)
    {
        if (environment.Language is not ("c" or "cpp") || (environment.Language == "c" && environment.Runtime != "wasi"))
        {
            return false;
        }

        return environment.Runtime switch
        {
            "wasi" => environment.Tools == "clang" && environment.LibBundle is null,
            "sdl3" or "raylib" or "allegro5" => environment.Tools == "emcc" && environment.LibBundle == environment.Runtime,
            _ => false,
        };
    }

    private static bool HasValidWorkspace(Dictionary<string, BundleFileMeta> files, CodingExecutionEntryPoint entryPoint)
    {
        if (entryPoint.Path is null || Path.GetExtension(entryPoint.Path) is not (".c" or ".cc" or ".cpp" or ".cxx"))
        {
            return false;
        }

        var normalizedFiles = new Dictionary<string, BundleFileMeta>(StringComparer.Ordinal);
        try
        {
            foreach (var (path, file) in files)
            {
                if (file is null || !normalizedFiles.TryAdd(CodingAssignmentPath.Normalize(path), file))
                {
                    return false;
                }
            }

            return normalizedFiles.TryGetValue(CodingAssignmentPath.Normalize(entryPoint.Path), out var entryFile)
                && entryFile is { Visibility: "Public", Encoding: "text", Content: not null };
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool HasCoherentTestPlan(CodingAssignmentContent content, CodingExecutionEntryPoint entryPoint)
    {
        var tests = content.Tests.Public.Concat(content.Tests.Private).ToArray();
        if (tests.Length == 0 || tests.Any(test => test is null || !IsValidWeight(test.Weight)))
        {
            return false;
        }

        if (content.ExerciseType == "standard-io")
        {
            return entryPoint.Symbol is null && tests.All(test => test is StandardTest { Stdout: not null });
        }

        if (content.ExerciseType != "function" || entryPoint.Symbol is null)
        {
            return false;
        }

        TestFunctionData? signature = null;
        foreach (var test in tests)
        {
            if (test is not FunctionalTestGroup group || group.Function is not { } function
                || !IsValidIdentifier(function.FunctionName) || function.FunctionName != entryPoint.Symbol
                || function.ReturnType is null || function.Parameters is null
                || function.Parameters.Any(parameter => parameter is null || !IsValidIdentifier(parameter.Name))
                || function.Parameters.Select(parameter => parameter.Name).Distinct(StringComparer.Ordinal).Count() != function.Parameters.Count
                || !HasValidCases(group, function))
            {
                return false;
            }

            if (signature is not null && (signature.ReturnType.Type != function.ReturnType.Type
                || !signature.Parameters.Select(parameter => (parameter.Name, parameter.Type))
                    .SequenceEqual(function.Parameters.Select(parameter => (parameter.Name, parameter.Type)))))
            {
                return false;
            }

            signature ??= function;
        }

        return true;
    }

    private static bool IsValidWeight(double weight)
    {
        if (!double.IsFinite(weight) || weight <= 0)
        {
            return false;
        }

        var scaled = weight * 100d;
        if (!double.IsFinite(scaled))
        {
            return false;
        }

        var units = Math.Round(scaled, MidpointRounding.AwayFromZero);
        return units > 0 && units <= 9_007_199_254_740_991d && units / 100d == weight;
    }

    private static bool IsValidIdentifier(string? value) => value is not null && s_identifier.IsMatch(value);

    private static bool HasValidCases(FunctionalTestGroup group, TestFunctionData function)
    {
        if (group.Cases is not { Count: > 0 } cases)
        {
            return false;
        }

        foreach (var testCase in cases)
        {
            if (testCase?.Inputs is not { } inputs || inputs.Length != function.Parameters.Count
                || testCase.Expected is not { } expected || expected.Type != function.ReturnType.Type || !IsValidScalar(expected))
            {
                return false;
            }

            for (var index = 0; index < inputs.Length; index++)
            {
                if (inputs[index] is not { } input || input.Type != function.Parameters[index].Type || !IsValidScalar(input))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool IsValidScalar(FunctionParameter value)
    {
        var content = value.Content;
        return value.Type switch
        {
            FunctionParameterType.String => content.ValueKind == JsonValueKind.String,
            FunctionParameterType.Boolean => content.ValueKind is JsonValueKind.True or JsonValueKind.False,
            FunctionParameterType.Integer => content.ValueKind == JsonValueKind.Number && content.TryGetDouble(out var number)
                && double.IsFinite(number) && Math.Abs(number) <= 9_007_199_254_740_991d && Math.Truncate(number) == number,
            FunctionParameterType.Float => content.ValueKind == JsonValueKind.String && content.GetString() is { } text && s_decimal.IsMatch(text),
            _ => false,
        };
    }
}
