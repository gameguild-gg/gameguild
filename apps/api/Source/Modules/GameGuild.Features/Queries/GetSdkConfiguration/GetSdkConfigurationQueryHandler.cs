using GameGuild.CQRS;
using GameGuild.Features.Abstractions;
using GameGuild.Features.Models;

namespace GameGuild.Features.Queries.Handlers;

/// <summary>
///     Handler for retrieving SDK configuration
/// </summary>
public sealed class GetSdkConfigurationQueryHandler(IFeatureFlagSdkService sdkService) : IQueryHandler<GetSdkConfigurationQuery, SdkConfiguration>
{
    private readonly IFeatureFlagSdkService _sdkService = sdkService ?? throw new ArgumentNullException(nameof(sdkService));

    public async Task<SdkConfiguration> Handle(GetSdkConfigurationQuery request, CancellationToken cancellationToken)
    {
        // Get SDK configuration
        var sdkConfig = await _sdkService.GenerateSdkConfigurationAsync(request.Environment, request.TenantId, cancellationToken);

        return sdkConfig;
    }
}
