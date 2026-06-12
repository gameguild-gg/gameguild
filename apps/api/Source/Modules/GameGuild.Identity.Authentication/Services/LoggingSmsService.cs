using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Development SMS provider that emits verification codes through structured logs.
/// </summary>
public sealed class LoggingSmsService(
    ILogger<LoggingSmsService> logger,
    IOptions<SmsMfaOptions> options) : ISmsService
{
    public Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(options.Value.Enabled);

    public Task SendVerificationCodeAsync(string phoneNumber, string code, CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled)
        {
            throw new InvalidOperationException("SMS MFA delivery is disabled.");
        }

        logger.LogInformation(
            "SMS MFA verification code generated for {PhoneNumberMasked}: {Code}",
            MaskPhoneNumber(phoneNumber),
            code);

        return Task.CompletedTask;
    }

    private static string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length < 4)
        {
            return "****";
        }

        return $"***-***-{phoneNumber[^4..]}";
    }
}
