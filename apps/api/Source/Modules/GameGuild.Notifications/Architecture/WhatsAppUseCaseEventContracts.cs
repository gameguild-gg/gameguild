using GameGuild;
using GameGuild.Notifications;

[assembly: UseCaseEventContract(typeof(CreateWhatsAppBroadcastCommand), "notifications.create-whatsapp-broadcast", NoDomainEventReason = "Campaign authoring is internal; the durable operation event records the mutation without copying campaign content.")]
[assembly: UseCaseEventContract(typeof(ScheduleWhatsAppBroadcastCommand), "notifications.schedule-whatsapp-broadcast", NoDomainEventReason = "Scheduling changes only internal delivery timing; the durable operation event provides administrative observability.")]
[assembly: UseCaseEventContract(typeof(SendWhatsAppBroadcastCommand), "notifications.send-whatsapp-broadcast", NoDomainEventReason = "Send outcomes are tracked per recipient with provider message ids; the durable operation event records the campaign execution without audience phone numbers.")]
[assembly: UseCaseEventContract(typeof(CancelWhatsAppBroadcastCommand), "notifications.cancel-whatsapp-broadcast", NoDomainEventReason = "Cancellation is an administrative state change observed through the durable operation event.")]
