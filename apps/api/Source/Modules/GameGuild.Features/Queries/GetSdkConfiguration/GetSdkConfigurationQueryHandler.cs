using GameGuild.CQRS;

namespace GameGuild.Features;

/// <summary>
///     Handler for retrieving SDK configuration
/// </summary>
public sealed class GetSdkConfigurationQueryHandler(IFeatureFlagSdkService sdkService) : IQueryHandler<GetSdkConfigurationQuery, SdkConfiguration>
{
    private readonly IFeatureFlagSdkService _sdkService = sdkService ?? throw new ArgumentNullException(nameof(sdkService));

    public async Task<SdkConfiguration> Handle(GetSdkConfigurationQuery request, CancellationToken cancellationToken)
    {
        // Get SDK configuration
        var sdkConfig = await _sdkService.GenerateSdkConfigurationAsync(request.Environment, request.TenantId, cancellationToken).ConfigureAwait(false);

        return sdkConfig;
    }
}
