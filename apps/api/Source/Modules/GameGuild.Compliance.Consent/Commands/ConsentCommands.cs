using GameGuild.CQRS;

namespace GameGuild.Compliance.Consent;

// Commands
public sealed record CreateConsentPolicyCommand(string Name, PolicyType PolicyType, bool IsMandatory, string? Description = null) : ICommand<Guid>;
public sealed record PublishPolicyVersionCommand(Guid PolicyId, string VersionNumber, string Content, ContentType ContentType = ContentType.Markdown) : ICommand<PolicyVersionDto>;
public sealed record GrantConsentCommand(Guid UserId, Guid PolicyVersionId, string? IpAddress = null, string? UserAgent = null, string? ConsentMethod = null) : ICommand<UserConsentDto>;
public sealed record RevokeConsentCommand(Guid UserId, Guid PolicyVersionId) : ICommand;
public sealed record SubmitDataSubjectRequestCommand(Guid UserId, DataSubjectRequestType RequestType, string? Description = null) : ICommand<DataSubjectRequestDto>;
public sealed record ProcessDataSubjectRequestCommand(Guid RequestId, Guid ProcessedByUserId, string? Notes = null) : ICommand<DataSubjectRequestDto>;

// Queries
public sealed record GetActivePoliciesQuery(Guid? TenantId = null) : IQuery<List<ConsentPolicyDto>>;
public sealed record GetUserConsentsQuery(Guid UserId) : IQuery<List<UserConsentDto>>;
public sealed record GetPendingDataSubjectRequestsQuery() : IQuery<List<DataSubjectRequestDto>>;

// Command Handlers
public sealed class CreateConsentPolicyCommandHandler(IConsentService consentService) : ICommandHandler<CreateConsentPolicyCommand, Guid>
{
    public async Task<Guid> Handle(CreateConsentPolicyCommand request, CancellationToken cancellationToken)
    {
        var dto = await consentService.CreatePolicyAsync(request.Name, request.PolicyType, request.IsMandatory, request.Description, cancellationToken).ConfigureAwait(false);
        return dto.Id;
    }
}

public sealed class PublishPolicyVersionCommandHandler(IConsentService consentService) : ICommandHandler<PublishPolicyVersionCommand, PolicyVersionDto>
{
    public async Task<PolicyVersionDto> Handle(PublishPolicyVersionCommand request, CancellationToken cancellationToken)
        => await consentService.PublishVersionAsync(request.PolicyId, request.VersionNumber, request.Content, request.ContentType, cancellationToken).ConfigureAwait(false);
}

public sealed class GrantConsentCommandHandler(IConsentService consentService) : ICommandHandler<GrantConsentCommand, UserConsentDto>
{
    public async Task<UserConsentDto> Handle(GrantConsentCommand request, CancellationToken cancellationToken)
        => await consentService.GrantConsentAsync(request.UserId, request.PolicyVersionId, request.IpAddress, request.UserAgent, request.ConsentMethod, cancellationToken).ConfigureAwait(false);
}

public sealed class RevokeConsentCommandHandler(IConsentService consentService) : ICommandHandler<RevokeConsentCommand>
{
    public async Task<Unit> Handle(RevokeConsentCommand request, CancellationToken cancellationToken)
    {
        await consentService.RevokeConsentAsync(request.UserId, request.PolicyVersionId, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

public sealed class SubmitDataSubjectRequestCommandHandler(IConsentService consentService) : ICommandHandler<SubmitDataSubjectRequestCommand, DataSubjectRequestDto>
{
    public async Task<DataSubjectRequestDto> Handle(SubmitDataSubjectRequestCommand request, CancellationToken cancellationToken)
        => await consentService.SubmitDataSubjectRequestAsync(request.UserId, request.RequestType, request.Description, cancellationToken).ConfigureAwait(false);
}

public sealed class ProcessDataSubjectRequestCommandHandler(IConsentService consentService) : ICommandHandler<ProcessDataSubjectRequestCommand, DataSubjectRequestDto>
{
    public async Task<DataSubjectRequestDto> Handle(ProcessDataSubjectRequestCommand request, CancellationToken cancellationToken)
        => await consentService.ProcessDataSubjectRequestAsync(request.RequestId, request.ProcessedByUserId, request.Notes, cancellationToken).ConfigureAwait(false);
}

// Query Handlers
public sealed class GetActivePoliciesQueryHandler(IConsentService consentService) : IQueryHandler<GetActivePoliciesQuery, List<ConsentPolicyDto>>
{
    public async Task<List<ConsentPolicyDto>> Handle(GetActivePoliciesQuery request, CancellationToken cancellationToken)
        => await consentService.GetActivePoliciesAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
}

public sealed class GetUserConsentsQueryHandler(IConsentService consentService) : IQueryHandler<GetUserConsentsQuery, List<UserConsentDto>>
{
    public async Task<List<UserConsentDto>> Handle(GetUserConsentsQuery request, CancellationToken cancellationToken)
        => await consentService.GetUserConsentsAsync(request.UserId, cancellationToken).ConfigureAwait(false);
}

public sealed class GetPendingDataSubjectRequestsQueryHandler(IConsentService consentService) : IQueryHandler<GetPendingDataSubjectRequestsQuery, List<DataSubjectRequestDto>>
{
    public async Task<List<DataSubjectRequestDto>> Handle(GetPendingDataSubjectRequestsQuery request, CancellationToken cancellationToken)
        => await consentService.GetPendingRequestsAsync(cancellationToken).ConfigureAwait(false);
}
