using GameGuild.CQRS;
using GameGuild.Notifications.Services.WhatsApp;

namespace GameGuild.Notifications;

public sealed record CreateWhatsAppBroadcastCommand(WhatsAppBroadcastCreateRequest Request) : ICommand<Result<WhatsAppBroadcast>>;
public sealed record ScheduleWhatsAppBroadcastCommand(Guid TenantId, Guid BroadcastId, DateTime ScheduledAtUtc) : ICommand<Result<WhatsAppBroadcast>>;
public sealed record SendWhatsAppBroadcastCommand(Guid TenantId, Guid BroadcastId) : ICommand<Result<WhatsAppBroadcastSendReport>>;
public sealed record CancelWhatsAppBroadcastCommand(Guid TenantId, Guid BroadcastId) : ICommand<Result<WhatsAppBroadcast>>;

/// <summary>
///     Broadcast mutation surface (issue #55): thin command handlers delegating to
///     <see cref="IWhatsAppBroadcastService"/> so controllers mutate only through
///     <c>ISender</c> (GGARCH004), mirroring the email delivery mutation commands.
/// </summary>
public sealed class WhatsAppBroadcastMutationCommandHandler(
    IWhatsAppBroadcastService broadcastService) :
    ICommandHandler<CreateWhatsAppBroadcastCommand, Result<WhatsAppBroadcast>>,
    ICommandHandler<ScheduleWhatsAppBroadcastCommand, Result<WhatsAppBroadcast>>,
    ICommandHandler<SendWhatsAppBroadcastCommand, Result<WhatsAppBroadcastSendReport>>,
    ICommandHandler<CancelWhatsAppBroadcastCommand, Result<WhatsAppBroadcast>>
{
    public Task<Result<WhatsAppBroadcast>> Handle(CreateWhatsAppBroadcastCommand command, CancellationToken cancellationToken) =>
        broadcastService.CreateAsync(command.Request, cancellationToken);

    public Task<Result<WhatsAppBroadcast>> Handle(ScheduleWhatsAppBroadcastCommand command, CancellationToken cancellationToken) =>
        broadcastService.ScheduleAsync(command.TenantId, command.BroadcastId, command.ScheduledAtUtc, cancellationToken);

    public Task<Result<WhatsAppBroadcastSendReport>> Handle(SendWhatsAppBroadcastCommand command, CancellationToken cancellationToken) =>
        broadcastService.SendAsync(command.TenantId, command.BroadcastId, cancellationToken);

    public Task<Result<WhatsAppBroadcast>> Handle(CancelWhatsAppBroadcastCommand command, CancellationToken cancellationToken) =>
        broadcastService.CancelAsync(command.TenantId, command.BroadcastId, cancellationToken);
}
