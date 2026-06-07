using GameGuild.CQRS;

namespace GameGuild.Resources.Contents;

public sealed record GenerateContractCommand(
    GenerateContractInput Input,
    Guid CreatedBy) : ICommand<Result<GeneratedContractResult>>;

public sealed record BulkGenerateContractsCommand(
    IReadOnlyList<GenerateContractInput> Inputs,
    Guid CreatedBy,
    bool ContinueOnError = true) : ICommand<BulkGeneratedContractsResult>;

public sealed record BulkGeneratedContractsResult(
    int TotalRequested,
    int Successful,
    int Failed,
    IReadOnlyList<BulkGeneratedContractItemResult> Items)
{
    public bool HasFailures => Failed > 0;
}

public sealed record BulkGeneratedContractItemResult(
    int Index,
    bool Success,
    GeneratedContractResult? Contract,
    Error? Error)
{
    public static BulkGeneratedContractItemResult Succeeded(int index, GeneratedContractResult contract)
        => new(index, true, contract, null);

    public static BulkGeneratedContractItemResult FailedItem(int index, Error error)
        => new(index, false, null, error);
}

public sealed class GenerateContractCommandHandler(IContractGenerationService contractGenerationService)
    : ICommandHandler<GenerateContractCommand, Result<GeneratedContractResult>>
{
    public Task<Result<GeneratedContractResult>> Handle(GenerateContractCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return contractGenerationService.GenerateAsync(request.Input, request.CreatedBy, cancellationToken);
    }
}

public sealed class BulkGenerateContractsCommandHandler(IContractGenerationService contractGenerationService)
    : ICommandHandler<BulkGenerateContractsCommand, BulkGeneratedContractsResult>
{
    public async Task<BulkGeneratedContractsResult> Handle(BulkGenerateContractsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var items = new List<BulkGeneratedContractItemResult>(request.Inputs.Count);
        var successful = 0;
        var failed = 0;

        for (var index = 0; index < request.Inputs.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await contractGenerationService.GenerateAsync(
                request.Inputs[index],
                request.CreatedBy,
                cancellationToken).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                successful++;
                items.Add(BulkGeneratedContractItemResult.Succeeded(index, result.Value));
                continue;
            }

            failed++;
            items.Add(BulkGeneratedContractItemResult.FailedItem(index, result.Error));

            if (!request.ContinueOnError)
            {
                break;
            }
        }

        return new BulkGeneratedContractsResult(
            request.Inputs.Count,
            successful,
            failed,
            items);
    }
}
