using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GameGuild.Learning.Assessments.Grading.Contracts;
using GameGuild.Learning.Courses;
using Microsoft.Extensions.Options;

namespace GameGuild.Learning.Assessments.Grading.Code;

public sealed class CodeGradingWorkerOptions
{
    public const string Section = "CodeGradingWorker";
    public string? NodeExecutable { get; set; }
    public string? ScriptPath { get; set; }
    public string? RuntimeDirectory { get; set; }
    public string? CdnDirectory { get; set; }
    public string? BrowserDirectory { get; set; }
    public int DeadlineSeconds { get; set; } = 300;
}

/// <summary>
/// Credential-free process boundary for the server-owned WebAssembly worker.
/// A bounded wait and one active execution prevent compiler requests from
/// exhausting the API. Worker errors leave the academic transaction uncommitted.
/// </summary>
public sealed class CodeGradingWorker(IOptions<CodeGradingWorkerOptions> configured) : ICodeAssessmentExecutor, IDisposable
{
    private readonly SemaphoreSlim _slot = new(1, 1);

    public async Task<CodeExecutionReceipt> ExecuteAsync(CodingAssignmentContent definition, JsonElement files, CodeToolchainIdentity toolchain,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var options = configured.Value;
        if (string.IsNullOrWhiteSpace(options.NodeExecutable) || string.IsNullOrWhiteSpace(options.ScriptPath) ||
            string.IsNullOrWhiteSpace(options.RuntimeDirectory) || string.IsNullOrWhiteSpace(options.CdnDirectory) ||
            options.DeadlineSeconds is < 1 or > 300)
            throw new InvalidOperationException("The trusted Code grading worker is not configured.");
        if (!await _slot.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("The trusted Code grading worker is busy. Retry this submission.");
        try
        {
            var request = JsonSerializer.SerializeToUtf8Bytes(new
            {
                schemaVersion = 1,
                definition = JsonSerializer.SerializeToElement(definition, CodeAssessmentContracts.ContentJson),
                files,
                toolchain = JsonSerializer.SerializeToElement(toolchain, GradingJson.Options),
            });
            if (request.Length > 12_500_000) throw new InvalidOperationException("Code worker input exceeds its byte budget.");
            var requestHash = Convert.ToHexStringLower(SHA256.HashData(request));
            var start = new ProcessStartInfo
            {
                FileName = Path.GetFullPath(options.NodeExecutable),
                WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(options.ScriptPath))!,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            start.ArgumentList.Add("--max-old-space-size=512");
            start.ArgumentList.Add(Path.GetFullPath(options.ScriptPath));
            start.ArgumentList.Add(Path.GetFullPath(options.RuntimeDirectory));
            start.ArgumentList.Add(Path.GetFullPath(options.CdnDirectory));
            start.Environment.Clear();
            foreach (var key in new[] { "PATH", "SystemRoot", "WINDIR", "TEMP", "TMP", "HOME", "USERPROFILE", "LOCALAPPDATA" })
            {
                if (Environment.GetEnvironmentVariable(key) is { } value) start.Environment[key] = value;
            }
            if (!string.IsNullOrWhiteSpace(options.BrowserDirectory))
                start.Environment["PLAYWRIGHT_BROWSERS_PATH"] = Path.GetFullPath(options.BrowserDirectory);
            using var process = new Process { StartInfo = start };
            if (!process.Start()) throw new InvalidOperationException("Code worker did not start.");
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(options.DeadlineSeconds));
            using var stop = deadline.Token.Register(() => StopOwnedProcess(process));
            try
            {
                var stdout = ReadBoundedAsync(process.StandardOutput.BaseStream, 20_000, deadline);
                var stderr = ReadBoundedAsync(process.StandardError.BaseStream, 20_000, deadline);
                async Task WriteRequestAsync()
                {
                    await process.StandardInput.BaseStream.WriteAsync(request, deadline.Token).ConfigureAwait(false);
                    await process.StandardInput.BaseStream.FlushAsync(deadline.Token).ConfigureAwait(false);
                    process.StandardInput.Close();
                }
                // Observe every pipe task and stop a flooding child immediately.
                // Waiting for exit before observing a full pipe can deadlock.
                await Task.WhenAll(stdout, stderr, WriteRequestAsync(),
                    process.WaitForExitAsync(deadline.Token)).ConfigureAwait(false);
                var output = await stdout.ConfigureAwait(false);
                _ = await stderr.ConfigureAwait(false);
                if (process.ExitCode != 0) throw new InvalidOperationException("The trusted Code grading worker failed. Retry this submission.");
                using var receipt = JsonDocument.Parse(output);
                var root = receipt.RootElement;
                if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 4 ||
                    !root.EnumerateObject().Select(property => property.Name).ToHashSet(StringComparer.Ordinal)
                        .SetEquals(["schemaVersion", "requestHash", "manifestHash", "passed"]) ||
                    root.GetProperty("schemaVersion").GetInt32() != 1 ||
                    root.GetProperty("requestHash").GetString() != requestHash)
                    throw new InvalidOperationException("The Code worker receipt does not match its frozen request.");
                var manifestHash = root.GetProperty("manifestHash").GetString();
                if (manifestHash is null || manifestHash.Length != 64 ||
                    manifestHash.Any(character => !char.IsAsciiHexDigitLower(character)))
                    throw new InvalidOperationException("The Code worker receipt requires an artifact manifest hash.");
                var passed = root.GetProperty("passed").EnumerateArray().Select(value => value.GetBoolean()).ToArray();
                var expectedTests = definition.Tests.Public.Count + definition.Tests.Private.Count;
                if (passed.Length != expectedTests || passed.Length is 0 or > CodeAssessmentContracts.MaxTests)
                    throw new InvalidOperationException("The Code worker test receipt is incomplete.");
                return new CodeExecutionReceipt(passed,
                    Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(output))));
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new InvalidOperationException("The trusted Code grading worker exceeded its deadline. Retry this submission.");
            }
            finally
            {
                StopOwnedProcess(process);
                await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }
        finally { _slot.Release(); }
    }

    internal static async Task<string> ReadBoundedAsync(Stream reader, int maximum,
        CancellationTokenSource deadline)
    {
        var buffer = new byte[1024];
        using var result = new MemoryStream();
        int length;
        while ((length = await reader.ReadAsync(buffer.AsMemory(), deadline.Token).ConfigureAwait(false)) != 0)
        {
            if (result.Length + length > maximum)
            {
                await deadline.CancelAsync().ConfigureAwait(false);
                throw new InvalidOperationException("Code worker output exceeds its byte budget.");
            }
            result.Write(buffer, 0, length);
        }
        return new UTF8Encoding(false, true).GetString(result.ToArray());
    }

    private static void StopOwnedProcess(Process process)
    {
        try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
        catch (InvalidOperationException) { }
    }

    public void Dispose() => _slot.Dispose();
}
