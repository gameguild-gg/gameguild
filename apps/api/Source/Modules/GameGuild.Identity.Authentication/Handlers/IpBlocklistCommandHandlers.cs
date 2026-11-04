using System.Net;
using GameGuild.CQRS;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Stores a known-attacker IP blocklist entry (issue #1089). Validation mirrors the
///     admin endpoint contract: single valid IP address, future expiry, default lifetime
///     from <see cref="IpBlocklistOptions"/> when none is supplied.
/// </summary>
public sealed class AddIpBlocklistEntryCommandHandler(
    IIpBlocklistService blocklist,
    IOptions<IpBlocklistOptions> options,
    ILogger<AddIpBlocklistEntryCommandHandler> logger) : ICommandHandler<AddIpBlocklistEntryCommand, IpBlocklistMutationResult>
{
    public async Task<IpBlocklistMutationResult> Handle(AddIpBlocklistEntryCommand request, CancellationToken cancellationToken)
    {
        if (!IPAddress.TryParse(request.IpAddress, out _))
        {
            return new IpBlocklistMutationResult
            {
                Success = false,
                Message = "The entry must be a single valid IPv4 or IPv6 address."
            };
        }

        var settings = options.Value;
        var now = SystemClock.UtcNow;
        var expiresAt = request.ExpiresAt ?? now + settings.DefaultEntryLifetime;
        if (expiresAt <= now)
        {
            return new IpBlocklistMutationResult
            {
                Success = false,
                Message = "The entry expiry must lie in the future."
            };
        }

        await blocklist.AddOrUpdateEntryAsync(new IpBlocklistEntry
        {
            IpAddress = request.IpAddress,
            Reason = request.Reason,
            AddedBy = request.ActingAdmin,
            AddedAt = now,
            ExpiresAt = expiresAt
        }, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("IP blocklist entry stored for {IpAddress}", LogRedaction.MaskIpAddress(request.IpAddress));
        return new IpBlocklistMutationResult { Success = true, Message = "Blocklist entry stored." };
    }
}

/// <summary>
///     Removes an IP blocklist entry (issue #1089).
/// </summary>
public sealed class RemoveIpBlocklistEntryCommandHandler(
    IIpBlocklistService blocklist,
    ILogger<RemoveIpBlocklistEntryCommandHandler> logger) : ICommandHandler<RemoveIpBlocklistEntryCommand, IpBlocklistMutationResult>
{
    public async Task<IpBlocklistMutationResult> Handle(RemoveIpBlocklistEntryCommand request, CancellationToken cancellationToken)
    {
        if (!IPAddress.TryParse(request.IpAddress, out _))
        {
            return new IpBlocklistMutationResult
            {
                Success = false,
                Message = "The entry must be a single valid IPv4 or IPv6 address."
            };
        }

        var removed = await blocklist.RemoveEntryAsync(request.IpAddress, cancellationToken).ConfigureAwait(false);
        if (!removed)
        {
            return new IpBlocklistMutationResult
            {
                Success = false,
                Message = "No blocklist entry exists for that IP address."
            };
        }

        logger.LogInformation("IP blocklist entry removed for {IpAddress}", LogRedaction.MaskIpAddress(request.IpAddress));
        return new IpBlocklistMutationResult { Success = true, Message = "Blocklist entry removed." };
    }
}
