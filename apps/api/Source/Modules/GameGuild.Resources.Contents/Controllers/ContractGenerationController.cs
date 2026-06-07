using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Resources.Contents;

[ApiVersion("1.0")]
[Microsoft.AspNetCore.Http.Tags("resources/contents/contracts")]
[Route("v{version:apiVersion}/document-contracts")]
[Authorize]
public sealed class ContractGenerationController(
    ISender sender,
    IActorContextAccessor actorContextAccessor) : BaseApiController
{
    [HttpPost("generate")]
    [ProducesResponseType(typeof(GeneratedContractResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GeneratedContractResponse>> Generate(
        [FromBody] GenerateContractRequest request,
        CancellationToken ct)
    {
        var createdBy = actorContextAccessor.ActorContext.SubjectIdAsGuid;
        if (!createdBy.HasValue)
        {
            return Unauthorized();
        }

        var result = await sender.Send(
            new GenerateContractCommand(ToInput(request), createdBy.Value),
            ct).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return result.Error.Type == ErrorType.NotFound
                ? NotFound(result.Error)
                : BadRequest(result.Error);
        }

        var response = GeneratedContractResponse.FromResult(result.Value);
        return CreatedAtAction(
            nameof(Generate),
            new { version = "1.0", id = response.ContractId },
            response);
    }

    [HttpPost("generate:bulk")]
    [ProducesResponseType(typeof(BulkGeneratedContractsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkGeneratedContractsResponse>> GenerateBulk(
        [FromBody] BulkGenerateContractsRequest request,
        CancellationToken ct)
    {
        var createdBy = actorContextAccessor.ActorContext.SubjectIdAsGuid;
        if (!createdBy.HasValue)
        {
            return Unauthorized();
        }

        if (request.Contracts is null || request.Contracts.Count == 0)
        {
            return BadRequest(Error.Validation(
                "ContractGeneration.BulkEmpty",
                "At least one contract generation request is required."));
        }

        var result = await sender.Send(
            new BulkGenerateContractsCommand(
                request.Contracts.Select(ToInput).ToList(),
                createdBy.Value,
                request.ContinueOnError),
            ct).ConfigureAwait(false);

        return Ok(BulkGeneratedContractsResponse.FromResult(result));
    }

    private static GenerateContractInput ToInput(GenerateContractRequest request)
        => new(
            request.DocumentTemplateId,
            request.EntityType,
            request.EntityId,
            request.Title,
            request.Variables ?? new Dictionary<string, string?>(),
            request.Summary,
            request.Publish,
            request.AllowMissingVariables);
}

public sealed record GenerateContractRequest(
    Guid DocumentTemplateId,
    string EntityType,
    Guid? EntityId,
    string Title,
    IReadOnlyDictionary<string, string?>? Variables = null,
    string? Summary = null,
    bool Publish = false,
    bool AllowMissingVariables = false);

public sealed record BulkGenerateContractsRequest(
    IReadOnlyList<GenerateContractRequest> Contracts,
    bool ContinueOnError = true);

public sealed record GeneratedContractResponse(
    Guid ContractId,
    Guid ContentVersionId,
    Guid DocumentTemplateId,
    string TemplateKey,
    string EntityType,
    Guid EntityId,
    int VersionNumber,
    string Title,
    string? Content,
    IReadOnlyList<string> MissingVariables,
    bool Published,
    DateTime GeneratedAtUtc)
{
    public static GeneratedContractResponse FromResult(GeneratedContractResult result)
        => new(
            result.ContractId,
            result.ContentVersionId,
            result.DocumentTemplateId,
            result.TemplateKey,
            result.EntityType,
            result.EntityId,
            result.VersionNumber,
            result.Title,
            result.Content,
            result.MissingVariables,
            result.Published,
            result.GeneratedAtUtc);
}

public sealed record BulkGeneratedContractsResponse(
    int TotalRequested,
    int Successful,
    int Failed,
    bool HasFailures,
    IReadOnlyList<BulkGeneratedContractItemResponse> Items)
{
    public static BulkGeneratedContractsResponse FromResult(BulkGeneratedContractsResult result)
        => new(
            result.TotalRequested,
            result.Successful,
            result.Failed,
            result.HasFailures,
            result.Items.Select(BulkGeneratedContractItemResponse.FromResult).ToList());
}

public sealed record BulkGeneratedContractItemResponse(
    int Index,
    bool Success,
    GeneratedContractResponse? Contract,
    Error? Error)
{
    public static BulkGeneratedContractItemResponse FromResult(BulkGeneratedContractItemResult item)
        => new(
            item.Index,
            item.Success,
            item.Contract is null ? null : GeneratedContractResponse.FromResult(item.Contract),
            item.Error);
}
