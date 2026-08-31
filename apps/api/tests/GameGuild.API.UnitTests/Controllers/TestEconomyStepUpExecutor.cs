using System.Security.Cryptography;
using System.Text;
using GameGuild.API.Authorization;

namespace GameGuild.API.UnitTests.Controllers;

internal sealed class TestEconomyStepUpExecutor : IEconomyStepUpExecutor
{
    public List<(EconomyStepUpOperation Operation, string Receipt)> Calls { get; } = [];
    public Exception? Failure { get; init; }

    public async Task<TResult> ExecuteAsync<TResult>(
        EconomyStepUpOperation operation,
        string receipt,
        Func<string, CancellationToken, Task<TResult>> protectedAction,
        CancellationToken cancellationToken)
    {
        Calls.Add((operation, receipt));
        if (Failure is not null) throw Failure;
        return await protectedAction(EvidenceHash(receipt), cancellationToken);
    }

    public static string EvidenceHash(string receipt) => Convert.ToHexStringLower(
        SHA256.HashData(Encoding.UTF8.GetBytes(receipt)));
}
