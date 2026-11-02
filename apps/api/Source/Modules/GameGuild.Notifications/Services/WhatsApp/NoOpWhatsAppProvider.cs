using GameGuild.CQRS;

namespace GameGuild.Notifications.Services.WhatsApp;

/// <summary>
///     Fail-closed default <see cref="IWhatsAppProvider"/> (issues #54/#55): served while
///     <c>Notifications:WhatsApp:Enabled</c> is false or the environment credentials are
///     absent. Reports itself unconfigured and fails every call with
///     <c>WhatsApp.NotConfigured</c> so the dispatch pipeline retries/deadletters
///     observably and broadcast sends fail explicitly — a runtime misconfiguration can
///     never surface as a silent success (same contract as <c>NoOpPlaidInstantVerification</c>).
/// </summary>
public sealed class NoOpWhatsAppProvider : IWhatsAppProvider
{
    /// <summary>Shared stateless instance for hosts that construct it without settings.</summary>
    public static readonly NoOpWhatsAppProvider Instance = new();

    /// <inheritdoc />
    public bool IsConfigured => false;

    /// <inheritdoc />
    public Task<Result<WhatsAppSendResult>> SendMessageAsync(WhatsAppOutboundMessage message, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Failure<WhatsAppSendResult>(NotConfigured()));

    /// <inheritdoc />
    public Task<Result<WhatsAppSendResult>> SendTemplateAsync(WhatsAppTemplateMessage message, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Failure<WhatsAppSendResult>(NotConfigured()));

    /// <inheritdoc />
    public Task<Result<WhatsAppNumberVerification>> VerifyNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Failure<WhatsAppNumberVerification>(NotConfigured()));

    /// <inheritdoc />
    public Task<Result<WhatsAppMessageStatus>> GetMessageStatusAsync(string providerMessageId, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Failure<WhatsAppMessageStatus>(NotConfigured()));

    private static Error NotConfigured() => Error.Problem(
        "WhatsApp.NotConfigured",
        "WhatsApp delivery is not configured (Notifications:WhatsApp). Set the environment credentials "
        + $"({WhatsAppSettings.AppIdEnvironmentVariable}, {WhatsAppSettings.PhoneNumberIdEnvironmentVariable}, "
        + $"{WhatsAppSettings.AccessTokenEnvironmentVariable}) and enable the channel to activate the "
        + "WhatsApp Business Cloud API integration.");
}
