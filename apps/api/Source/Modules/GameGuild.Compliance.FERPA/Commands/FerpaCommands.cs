using GameGuild.CQRS;

namespace GameGuild.Compliance.FERPA;

public sealed record RegisterEducationRecordCommand(
    Guid StudentUserId,
    EducationRecordKind RecordKind,
    string ExternalRecordId,
    string Title,
    FerpaRecordProtectionLevel ProtectionLevel = FerpaRecordProtectionLevel.EducationRecord,
    bool IsDirectoryInformation = false,
    Guid? TenantId = null,
    DateTime? RetentionUntil = null,
    string MetadataJson = "{}") : ICommand<FerpaEducationRecordDto>;

public sealed record UpsertDirectoryInformationPolicyCommand(
    Guid? TenantId,
    string AllowedFieldsJson,
    bool OptOutEnabled = true,
    DateTime? AnnualNoticeSentAt = null,
    string? NoticeUrl = null) : ICommand<FerpaDirectoryInformationPolicyDto>;

public sealed record GrantFerpaDisclosureConsentCommand(
    Guid StudentUserId,
    string Recipient,
    string Purpose,
    string Scope,
    DateTime EffectiveFrom,
    Guid? GuardianUserId = null,
    DateTime? ExpiresAt = null) : ICommand<FerpaDisclosureConsentDto>;

public sealed record RevokeFerpaDisclosureConsentCommand(Guid ConsentId) : ICommand<bool>;

public sealed record RecordFerpaDisclosureCommand(
    Guid StudentUserId,
    Guid DisclosedByUserId,
    string Recipient,
    FerpaDisclosureBasis Basis,
    string Purpose,
    string Scope,
    string RecordIdsJson,
    DateTime DisclosedAt) : ICommand<FerpaDisclosureLogDto>;

public sealed record SubmitFerpaInspectionRequestCommand(
    Guid StudentUserId,
    Guid RequestedByUserId,
    DateTime Deadline,
    string? Description = null) : ICommand<FerpaInspectionRequestDto>;

public sealed record CompleteFerpaInspectionRequestCommand(
    Guid RequestId,
    Guid ProcessedByUserId,
    bool Approved,
    string? Notes = null) : ICommand<FerpaInspectionRequestDto>;

public sealed record GetStudentEducationRecordsQuery(Guid StudentUserId) : IQuery<List<FerpaEducationRecordDto>>;
public sealed record GetStudentDirectoryInformationQuery(Guid StudentUserId) : IQuery<List<FerpaEducationRecordDto>>;
public sealed record GetDirectoryInformationPolicyQuery(Guid? TenantId = null) : IQuery<FerpaDirectoryInformationPolicyDto?>;
public sealed record GetStudentFerpaConsentsQuery(Guid StudentUserId) : IQuery<List<FerpaDisclosureConsentDto>>;
public sealed record GetStudentFerpaDisclosureLogsQuery(Guid StudentUserId) : IQuery<List<FerpaDisclosureLogDto>>;
public sealed record GetPendingFerpaInspectionRequestsQuery() : IQuery<List<FerpaInspectionRequestDto>>;

public sealed class RegisterEducationRecordCommandHandler(IFerpaService service) : ICommandHandler<RegisterEducationRecordCommand, FerpaEducationRecordDto>
{
    public Task<FerpaEducationRecordDto> Handle(RegisterEducationRecordCommand request, CancellationToken cancellationToken)
        => service.RegisterEducationRecordAsync(request, cancellationToken);
}

public sealed class UpsertDirectoryInformationPolicyCommandHandler(IFerpaService service) : ICommandHandler<UpsertDirectoryInformationPolicyCommand, FerpaDirectoryInformationPolicyDto>
{
    public Task<FerpaDirectoryInformationPolicyDto> Handle(UpsertDirectoryInformationPolicyCommand request, CancellationToken cancellationToken)
        => service.UpsertDirectoryPolicyAsync(request, cancellationToken);
}

public sealed class GrantFerpaDisclosureConsentCommandHandler(IFerpaService service) : ICommandHandler<GrantFerpaDisclosureConsentCommand, FerpaDisclosureConsentDto>
{
    public Task<FerpaDisclosureConsentDto> Handle(GrantFerpaDisclosureConsentCommand request, CancellationToken cancellationToken)
        => service.GrantDisclosureConsentAsync(request, cancellationToken);
}

public sealed class RevokeFerpaDisclosureConsentCommandHandler(IFerpaService service) : ICommandHandler<RevokeFerpaDisclosureConsentCommand, bool>
{
    public Task<bool> Handle(RevokeFerpaDisclosureConsentCommand request, CancellationToken cancellationToken)
        => service.RevokeDisclosureConsentAsync(request.ConsentId, cancellationToken);
}

public sealed class RecordFerpaDisclosureCommandHandler(IFerpaService service) : ICommandHandler<RecordFerpaDisclosureCommand, FerpaDisclosureLogDto>
{
    public Task<FerpaDisclosureLogDto> Handle(RecordFerpaDisclosureCommand request, CancellationToken cancellationToken)
        => service.RecordDisclosureAsync(request, cancellationToken);
}

public sealed class SubmitFerpaInspectionRequestCommandHandler(IFerpaService service) : ICommandHandler<SubmitFerpaInspectionRequestCommand, FerpaInspectionRequestDto>
{
    public Task<FerpaInspectionRequestDto> Handle(SubmitFerpaInspectionRequestCommand request, CancellationToken cancellationToken)
        => service.SubmitInspectionRequestAsync(request, cancellationToken);
}

public sealed class CompleteFerpaInspectionRequestCommandHandler(IFerpaService service) : ICommandHandler<CompleteFerpaInspectionRequestCommand, FerpaInspectionRequestDto>
{
    public Task<FerpaInspectionRequestDto> Handle(CompleteFerpaInspectionRequestCommand request, CancellationToken cancellationToken)
        => service.CompleteInspectionRequestAsync(request, cancellationToken);
}

public sealed class GetStudentEducationRecordsQueryHandler(IFerpaService service) : IQueryHandler<GetStudentEducationRecordsQuery, List<FerpaEducationRecordDto>>
{
    public Task<List<FerpaEducationRecordDto>> Handle(GetStudentEducationRecordsQuery request, CancellationToken cancellationToken)
        => service.GetStudentRecordsAsync(request.StudentUserId, cancellationToken);
}

public sealed class GetStudentDirectoryInformationQueryHandler(IFerpaService service) : IQueryHandler<GetStudentDirectoryInformationQuery, List<FerpaEducationRecordDto>>
{
    public Task<List<FerpaEducationRecordDto>> Handle(GetStudentDirectoryInformationQuery request, CancellationToken cancellationToken)
        => service.GetDirectoryInformationAsync(request.StudentUserId, cancellationToken);
}

public sealed class GetDirectoryInformationPolicyQueryHandler(IFerpaService service) : IQueryHandler<GetDirectoryInformationPolicyQuery, FerpaDirectoryInformationPolicyDto?>
{
    public Task<FerpaDirectoryInformationPolicyDto?> Handle(GetDirectoryInformationPolicyQuery request, CancellationToken cancellationToken)
        => service.GetDirectoryPolicyAsync(request.TenantId, cancellationToken);
}

public sealed class GetStudentFerpaConsentsQueryHandler(IFerpaService service) : IQueryHandler<GetStudentFerpaConsentsQuery, List<FerpaDisclosureConsentDto>>
{
    public Task<List<FerpaDisclosureConsentDto>> Handle(GetStudentFerpaConsentsQuery request, CancellationToken cancellationToken)
        => service.GetStudentConsentsAsync(request.StudentUserId, cancellationToken);
}

public sealed class GetStudentFerpaDisclosureLogsQueryHandler(IFerpaService service) : IQueryHandler<GetStudentFerpaDisclosureLogsQuery, List<FerpaDisclosureLogDto>>
{
    public Task<List<FerpaDisclosureLogDto>> Handle(GetStudentFerpaDisclosureLogsQuery request, CancellationToken cancellationToken)
        => service.GetDisclosureLogsAsync(request.StudentUserId, cancellationToken);
}

public sealed class GetPendingFerpaInspectionRequestsQueryHandler(IFerpaService service) : IQueryHandler<GetPendingFerpaInspectionRequestsQuery, List<FerpaInspectionRequestDto>>
{
    public Task<List<FerpaInspectionRequestDto>> Handle(GetPendingFerpaInspectionRequestsQuery request, CancellationToken cancellationToken)
        => service.GetPendingInspectionRequestsAsync(cancellationToken);
}
