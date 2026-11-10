using GameGuild;
using GameGuild.API.Core.Compliance;

[assembly: UseCaseEventContract(typeof(RecordUserConsentEndpointCommand), "compliance.consent.user-consent.record", NoDomainEventReason = "Consent grant/revocation is audited durably by the Compliance.Consent module's audit sink.")]
