using Asp.Versioning;
using System.Diagnostics.CodeAnalysis;
using GameGuild.CQRS;
using GameGuild.Identity.Tenants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Compatibility client/customer routes backed by the tenant and subscription models.
/// </summary>
[ApiVersion("1.0")]
[Microsoft.AspNetCore.Http.Tags("clients")]
[Authorize]
public sealed class ClientsController(ISender sender) : BaseApiController
{
    [HttpPost("v{version:apiVersion}/clients")]
    [EndpointSummary("Create a B2B client account")]
    [EndpointDescription("Creates a client account using the canonical tenant creation workflow.")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateClient([FromBody] CreateClientRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);

        if (!TryNormalizeCnpj(body.Cnpj, out var normalizedCnpj))
        {
            return BadRequest(new { error = "Invalid CNPJ" });
        }

        var id = await sender.Send(new CreateTenantCommand(body.Name, body.Slug, body.AdminEmail, body.Description), ct).ConfigureAwait(false);
        var fiscalFields = BuildFiscalFields(normalizedCnpj, body.TaxId, body.FiscalData);
        if (fiscalFields.Count > 0)
        {
            await sender.Send(
                    new UpdateTenantMetadataCommand(
                        id,
                        new UpdateTenantMetadataRequest(
                            new Dictionary<string, object?> { ["fiscal"] = fiscalFields },
                            Tags: null,
                            ExternalReferences: null,
                            BusinessInfo: null,
                            ContactInfo: null,
                            AdminNotes: null)),
                    ct)
                .ConfigureAwait(false);
        }

        return CreatedAtAction(nameof(GetClientById), new { clientId = id }, new { id, body.Name, body.Slug });
    }

    [HttpGet("v{version:apiVersion}/clients")]
    [EndpointSummary("List B2B client accounts")]
    [EndpointDescription("Lists client accounts through the canonical tenant page query.")]
    [ProducesResponseType<PagedResult<Tenant>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClients(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? searchTerm = null,
        CancellationToken ct = default)
    {
        NormalizePaging(ref page, ref pageSize, 500);
        var (isActive, isArchived) = NormalizeTenantStatus(status);

        var result = await sender.Send(new GetTenantsPageQuery(page, pageSize, isActive, isArchived, searchTerm), ct).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpGet("v{version:apiVersion}/clients/{clientId:guid}", Name = "GetClientById")]
    [EndpointSummary("Get a B2B client account")]
    [ProducesResponseType<Tenant>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClientById(Guid clientId, CancellationToken ct)
    {
        var tenant = await sender.Send(new GetTenantByIdQuery(clientId), ct).ConfigureAwait(false);
        return tenant is null ? NotFound() : Ok(tenant);
    }

    [HttpPut("v{version:apiVersion}/clients/{clientId:guid}")]
    [EndpointSummary("Update a B2B client account")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateClientById(Guid clientId, [FromBody] UpdateTenantRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);

        await sender.Send(new UpdateTenantCommand(clientId, body.Name, body.Description), ct).ConfigureAwait(false);
        return NoContent();
    }

    [HttpDelete("v{version:apiVersion}/clients/{clientId:guid}")]
    [EndpointSummary("Archive a B2B client account")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteClientById(Guid clientId, [FromBody] ArchiveRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);

        await sender.Send(new ArchiveTenantCommand(clientId, body.Reason), ct).ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("v{version:apiVersion}/clients/{clientId:guid}/modules")]
    [EndpointSummary("List contracted modules for a B2B client")]
    [EndpointDescription("Returns subscription-backed modules plus tenant feature flags for a client account.")]
    [ProducesResponseType<ClientModulesResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClientModules(
        Guid clientId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] SubscriptionStatus? status = null,
        CancellationToken ct = default)
    {
        NormalizePaging(ref page, ref pageSize, 100);

        var subscriptions = await sender.Send(new GetPagedSubscriptionsQuery(page, pageSize, status, clientId), ct).ConfigureAwait(false);
        var featureFlags = await sender.Send(new GetTenantFeatureFlagsQuery(clientId), ct).ConfigureAwait(false)
            ?? new Dictionary<string, bool>();

        return Ok(new ClientModulesResponse(clientId, subscriptions, featureFlags));
    }

    [HttpPatch("v{version:apiVersion}/clients/{clientId:guid}/modules")]
    [HttpPut("v{version:apiVersion}/clients/{clientId:guid}/modules")]
    [EndpointSummary("Update contracted module toggles for a B2B client")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateClientModules(Guid clientId, [FromBody] UpdateTenantFeatureFlagsRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);

        await sender.Send(new UpdateTenantFeatureFlagsCommand(clientId, body), ct).ConfigureAwait(false);
        return NoContent();
    }

    [ExcludeFromCodeCoverage]
    private static void NormalizePaging(ref int page, ref int pageSize, int maxPageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > maxPageSize) pageSize = maxPageSize;
    }

    private static (bool? IsActive, bool? IsArchived) NormalizeTenantStatus(string? status)
    {
        var normalized = status?.Trim().ToLowerInvariant();
        return normalized switch
        {
            "active" => (true, false),
            "inactive" => (false, false),
            "archived" => (null, true),
            _ => (null, null)
        };
    }

    private static Dictionary<string, object?> BuildFiscalFields(
        string? normalizedCnpj,
        string? taxId,
        Dictionary<string, object?>? fiscalData)
    {
        var fields = fiscalData is null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(fiscalData, StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(normalizedCnpj))
        {
            fields["cnpj"] = normalizedCnpj;
            fields["cnpjFormatted"] = FormatCnpj(normalizedCnpj);
        }

        if (!string.IsNullOrWhiteSpace(taxId))
        {
            fields["taxId"] = taxId.Trim();
        }

        return fields;
    }

    private static bool TryNormalizeCnpj(string? cnpj, out string? normalized)
    {
        normalized = null;
        if (string.IsNullOrWhiteSpace(cnpj))
            return true;

        var digits = new string(cnpj.Where(char.IsDigit).ToArray());
        if (digits.Length != 14 || digits.Distinct().Count() == 1)
            return false;

        if (CalculateCnpjDigit(digits, 12) != digits[12] - '0')
            return false;

        if (CalculateCnpjDigit(digits, 13) != digits[13] - '0')
            return false;

        normalized = digits;
        return true;
    }

    private static int CalculateCnpjDigit(string digits, int length)
    {
        var sum = 0;
        var weight = length - 7;

        for (var index = 0; index < length; index++)
        {
            sum += (digits[index] - '0') * weight--;
            if (weight < 2)
                weight = 9;
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    private static string FormatCnpj(string digits)
        => $"{digits[..2]}.{digits.Substring(2, 3)}.{digits.Substring(5, 3)}/{digits.Substring(8, 4)}-{digits.Substring(12, 2)}";
}

public sealed record CreateClientRequest(
    string Name,
    string Slug,
    string AdminEmail,
    string? Description = null,
    string? Cnpj = null,
    string? TaxId = null,
    Dictionary<string, object?>? FiscalData = null);

public sealed record ClientModulesResponse(
    Guid ClientId,
    PagedResult<Subscription> Subscriptions,
    Dictionary<string, bool> FeatureFlags);
