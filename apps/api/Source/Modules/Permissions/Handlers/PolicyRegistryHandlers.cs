using GameGuild.CQRS;
using GameGuild.Modules.Permissions.Abstractions;
using GameGuild.Modules.Permissions.Commands;
using GameGuild.Modules.Permissions.Entities;
using GameGuild.Shared;

namespace GameGuild.Modules.Permissions.Handlers;

public class CreatePolicyBundleHandler : IRequestHandler<CreatePolicyBundleCommand, Result<PolicyBundle>>
{
    private readonly IPolicyRegistryService _registryService;

    public CreatePolicyBundleHandler(IPolicyRegistryService registryService)
    {
        _registryService = registryService;
    }

    public async Task<Result<PolicyBundle>> Handle(CreatePolicyBundleCommand request, CancellationToken cancellationToken)
    {
        var bundle = new PolicyBundle
        {
            Name = request.Name,
            Description = request.Description,
            Version = request.Version,
            BundleType = request.BundleType,
            PolicyData = request.PolicyData,
            Metadata = request.Metadata,
            TenantId = request.TenantId,
            IsGlobal = request.IsGlobal,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveUntil = request.EffectiveUntil,
            CreatedBy = request.CreatedBy
        };
        return await _registryService.CreateBundleAsync(bundle, cancellationToken);
    }
}

public class UpdatePolicyBundleHandler : IRequestHandler<UpdatePolicyBundleCommand, Result<PolicyBundle>>
{
    private readonly IPolicyRegistryService _registryService;

    public UpdatePolicyBundleHandler(IPolicyRegistryService registryService)
    {
        _registryService = registryService;
    }

    public async Task<Result<PolicyBundle>> Handle(UpdatePolicyBundleCommand request, CancellationToken cancellationToken)
    {
        var bundle = new PolicyBundle
        {
            Id = request.BundleId,
            Name = request.Name,
            Description = request.Description,
            PolicyData = request.PolicyData,
            Metadata = request.Metadata,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveUntil = request.EffectiveUntil
        };
        return await _registryService.UpdateBundleAsync(bundle, cancellationToken);
    }
}

public class SignPolicyBundleHandler : IRequestHandler<SignPolicyBundleCommand, Result<PolicyBundle>>
{
    private readonly IPolicyRegistryService _registryService;

    public SignPolicyBundleHandler(IPolicyRegistryService registryService)
    {
        _registryService = registryService;
    }

    public async Task<Result<PolicyBundle>> Handle(SignPolicyBundleCommand request, CancellationToken cancellationToken)
    {
        return await _registryService.SignBundleAsync(request.BundleId, request.PrivateKey, request.SignedBy, cancellationToken);
    }
}

public class VerifyPolicyBundleSignatureHandler : IRequestHandler<VerifyPolicyBundleSignatureQuery, Result<bool>>
{
    private readonly IPolicyRegistryService _registryService;

    public VerifyPolicyBundleSignatureHandler(IPolicyRegistryService registryService)
    {
        _registryService = registryService;
    }

    public async Task<Result<bool>> Handle(VerifyPolicyBundleSignatureQuery request, CancellationToken cancellationToken)
    {
        return await _registryService.VerifyBundleSignatureAsync(request.BundleId, request.PublicKey, cancellationToken);
    }
}

public class ApprovePolicyBundleHandler : IRequestHandler<ApprovePolicyBundleCommand, Result<PolicyBundle>>
{
    private readonly IPolicyRegistryService _registryService;

    public ApprovePolicyBundleHandler(IPolicyRegistryService registryService)
    {
        _registryService = registryService;
    }

    public async Task<Result<PolicyBundle>> Handle(ApprovePolicyBundleCommand request, CancellationToken cancellationToken)
    {
        return await _registryService.ApproveBundleAsync(request.BundleId, request.ApprovedBy, cancellationToken);
    }
}

public class DeployPolicyBundleHandler : IRequestHandler<DeployPolicyBundleCommand, Result<PolicyBundleDeployment>>
{
    private readonly IPolicyRegistryService _registryService;

    public DeployPolicyBundleHandler(IPolicyRegistryService registryService)
    {
        _registryService = registryService;
    }

    public async Task<Result<PolicyBundleDeployment>> Handle(DeployPolicyBundleCommand request, CancellationToken cancellationToken)
    {
        return await _registryService.DeployBundleAsync(request.BundleId, request.TenantId, request.Environment, request.DeployedBy, cancellationToken);
    }
}

public class ActivatePolicyDeploymentHandler : IRequestHandler<ActivatePolicyDeploymentCommand, Result>
{
    private readonly IPolicyRegistryService _registryService;

    public ActivatePolicyDeploymentHandler(IPolicyRegistryService registryService)
    {
        _registryService = registryService;
    }

    public async Task<Result> Handle(ActivatePolicyDeploymentCommand request, CancellationToken cancellationToken)
    {
        return await _registryService.ActivateDeploymentAsync(request.DeploymentId, cancellationToken);
    }
}

public class RollbackPolicyDeploymentHandler : IRequestHandler<RollbackPolicyDeploymentCommand, Result>
{
    private readonly IPolicyRegistryService _registryService;

    public RollbackPolicyDeploymentHandler(IPolicyRegistryService registryService)
    {
        _registryService = registryService;
    }

    public async Task<Result> Handle(RollbackPolicyDeploymentCommand request, CancellationToken cancellationToken)
    {
        return await _registryService.RollbackDeploymentAsync(request.DeploymentId, request.Reason, request.RolledBackBy, cancellationToken);
    }
}

public class GetPolicyBundleHandler : IRequestHandler<GetPolicyBundleQuery, Result<PolicyBundle>>
{
    private readonly IPolicyRegistryService _registryService;

    public GetPolicyBundleHandler(IPolicyRegistryService registryService)
    {
        _registryService = registryService;
    }

    public async Task<Result<PolicyBundle>> Handle(GetPolicyBundleQuery request, CancellationToken cancellationToken)
    {
        return await _registryService.GetBundleAsync(request.BundleId, cancellationToken);
    }
}

public class ListPolicyBundlesHandler : IRequestHandler<ListPolicyBundlesQuery, Result<List<PolicyBundle>>>
{
    private readonly IPolicyRegistryService _registryService;

    public ListPolicyBundlesHandler(IPolicyRegistryService registryService)
    {
        _registryService = registryService;
    }

    public async Task<Result<List<PolicyBundle>>> Handle(ListPolicyBundlesQuery request, CancellationToken cancellationToken)
    {
        return await _registryService.ListBundlesAsync(request.Type, request.Status, cancellationToken);
    }
}

public class GetPolicyDeploymentsHandler : IRequestHandler<GetPolicyDeploymentsQuery, Result<List<PolicyBundleDeployment>>>
{
    private readonly IPolicyRegistryService _registryService;

    public GetPolicyDeploymentsHandler(IPolicyRegistryService registryService)
    {
        _registryService = registryService;
    }

    public async Task<Result<List<PolicyBundleDeployment>>> Handle(GetPolicyDeploymentsQuery request, CancellationToken cancellationToken)
    {
        return await _registryService.GetDeploymentsAsync(request.BundleId, cancellationToken);
    }
}

public class GetPolicyRegistryStatisticsHandler : IRequestHandler<GetPolicyRegistryStatisticsQuery, Result<RegistryStatistics>>
{
    private readonly IPolicyRegistryService _registryService;

    public GetPolicyRegistryStatisticsHandler(IPolicyRegistryService registryService)
    {
        _registryService = registryService;
    }

    public async Task<Result<RegistryStatistics>> Handle(GetPolicyRegistryStatisticsQuery request, CancellationToken cancellationToken)
    {
        return await _registryService.GetStatisticsAsync(cancellationToken);
    }
}
