namespace GameGuild.Identity.Authentication;

/// <summary>
///     Sends SMS verification messages for authentication flows.
/// </summary>
public interface ISmsService
{
    Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default);

    Task SendVerificationCodeAsync(string phoneNumber, string code, CancellationToken cancellationToken = default);
}
