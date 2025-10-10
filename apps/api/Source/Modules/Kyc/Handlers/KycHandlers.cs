using GameGuild.CQRS;
using GameGuild.Modules.Users;
using GameGuild.Common;
using GameGuild.Modules.Kyc.Commands;
using GameGuild.Modules.Kyc.Models;
using GameGuild.Modules.Kyc.Repositories;
using GameGuild.Modules.Kyc.Services;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Kyc.Handlers;

// Command Handlers
public class SubmitKycVerificationHandler : IRequestHandler<SubmitKycVerificationCommand, Result<UserKycVerification>>
{
    private readonly IKycService _kycService;
    private readonly ILogger<SubmitKycVerificationHandler> _logger;

    public SubmitKycVerificationHandler(IKycService kycService, ILogger<SubmitKycVerificationHandler> logger)
    {
        _kycService = kycService;
        _logger = logger;
    }

    public async Task<Result<UserKycVerification>> Handle(SubmitKycVerificationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            return await _kycService.SubmitVerificationAsync(
                request.UserId,
                request.Provider,
                request.VerificationLevel,
                request.DocumentTypes,
                request.DocumentCountry,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting KYC verification for user {UserId}", request.UserId);
            return Result<UserKycVerification>.Failure($"Failed to submit verification: {ex.Message}");
        }
    }
}

public class UpdateKycVerificationStatusHandler : IRequestHandler<UpdateKycVerificationStatusCommand, Result<UserKycVerification>>
{
    private readonly IKycService _kycService;
    private readonly ILogger<UpdateKycVerificationStatusHandler> _logger;

    public UpdateKycVerificationStatusHandler(IKycService kycService, ILogger<UpdateKycVerificationStatusHandler> logger)
    {
        _kycService = kycService;
        _logger = logger;
    }

    public async Task<Result<UserKycVerification>> Handle(UpdateKycVerificationStatusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            return await _kycService.UpdateVerificationStatusAsync(
                request.VerificationId,
                request.Status,
                request.Notes,
                request.CompletedAt,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating KYC verification status for {VerificationId}", request.VerificationId);
            return Result<UserKycVerification>.Failure($"Failed to update verification status: {ex.Message}");
        }
    }
}

public class UploadKycDocumentHandler : IRequestHandler<UploadKycDocumentCommand, Result<string>>
{
    private readonly IKycService _kycService;
    private readonly ILogger<UploadKycDocumentHandler> _logger;

    public UploadKycDocumentHandler(IKycService kycService, ILogger<UploadKycDocumentHandler> logger)
    {
        _kycService = kycService;
        _logger = logger;
    }

    public async Task<Result<string>> Handle(UploadKycDocumentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            return await _kycService.UploadDocumentAsync(
                request.VerificationId,
                request.DocumentType,
                request.DocumentStream,
                request.FileName,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading KYC document for verification {VerificationId}", request.VerificationId);
            return Result<string>.Failure($"Failed to upload document: {ex.Message}");
        }
    }
}

public class ProcessKycProviderWebhookHandler : IRequestHandler<ProcessKycProviderWebhookCommand, Result<bool>>
{
    private readonly IKycService _kycService;
    private readonly ILogger<ProcessKycProviderWebhookHandler> _logger;

    public ProcessKycProviderWebhookHandler(IKycService kycService, ILogger<ProcessKycProviderWebhookHandler> logger)
    {
        _kycService = kycService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(ProcessKycProviderWebhookCommand request, CancellationToken cancellationToken)
    {
        try
        {
            return await _kycService.ProcessProviderWebhookAsync(
                request.Provider,
                request.ExternalVerificationId,
                request.Status,
                request.ProviderData,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing KYC provider webhook for {ExternalId}", request.ExternalVerificationId);
            return Result<bool>.Failure($"Failed to process webhook: {ex.Message}");
        }
    }
}

public class DeleteKycVerificationHandler : IRequestHandler<DeleteKycVerificationCommand, Result<bool>>
{
    private readonly IKycRepository _repository;
    private readonly ILogger<DeleteKycVerificationHandler> _logger;

    public DeleteKycVerificationHandler(IKycRepository repository, ILogger<DeleteKycVerificationHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(DeleteKycVerificationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _repository.DeleteAsync(request.VerificationId, cancellationToken);
            _logger.LogInformation("KYC verification {VerificationId} deleted", request.VerificationId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting KYC verification {VerificationId}", request.VerificationId);
            return Result<bool>.Failure($"Failed to delete verification: {ex.Message}");
        }
    }
}

// Query Handlers
public class GetKycVerificationByIdHandler : IRequestHandler<GetKycVerificationByIdQuery, Result<UserKycVerification>>
{
    private readonly IKycService _kycService;
    private readonly ILogger<GetKycVerificationByIdHandler> _logger;

    public GetKycVerificationByIdHandler(IKycService kycService, ILogger<GetKycVerificationByIdHandler> logger)
    {
        _kycService = kycService;
        _logger = logger;
    }

    public async Task<Result<UserKycVerification>> Handle(GetKycVerificationByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            return await _kycService.GetVerificationByIdAsync(request.VerificationId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting KYC verification {VerificationId}", request.VerificationId);
            return Result<UserKycVerification>.Failure($"Failed to get verification: {ex.Message}");
        }
    }
}

public class GetKycVerificationsByUserIdHandler : IRequestHandler<GetKycVerificationsByUserIdQuery, Result<List<UserKycVerification>>>
{
    private readonly IKycService _kycService;
    private readonly ILogger<GetKycVerificationsByUserIdHandler> _logger;

    public GetKycVerificationsByUserIdHandler(IKycService kycService, ILogger<GetKycVerificationsByUserIdHandler> logger)
    {
        _kycService = kycService;
        _logger = logger;
    }

    public async Task<Result<List<UserKycVerification>>> Handle(GetKycVerificationsByUserIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            return await _kycService.GetVerificationsByUserIdAsync(request.UserId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting KYC verifications for user {UserId}", request.UserId);
            return Result<List<UserKycVerification>>.Failure($"Failed to get verifications: {ex.Message}");
        }
    }
}

public class GetLatestKycVerificationHandler : IRequestHandler<GetLatestKycVerificationQuery, Result<UserKycVerification?>>
{
    private readonly IKycService _kycService;
    private readonly ILogger<GetLatestKycVerificationHandler> _logger;

    public GetLatestKycVerificationHandler(IKycService kycService, ILogger<GetLatestKycVerificationHandler> logger)
    {
        _kycService = kycService;
        _logger = logger;
    }

    public async Task<Result<UserKycVerification?>> Handle(GetLatestKycVerificationQuery request, CancellationToken cancellationToken)
    {
        try
        {
            return await _kycService.GetLatestVerificationAsync(request.UserId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest KYC verification for user {UserId}", request.UserId);
            return Result<UserKycVerification?>.Failure($"Failed to get latest verification: {ex.Message}");
        }
    }
}

public class IsUserVerifiedHandler : IRequestHandler<IsUserVerifiedQuery, Result<bool>>
{
    private readonly IKycService _kycService;
    private readonly ILogger<IsUserVerifiedHandler> _logger;

    public IsUserVerifiedHandler(IKycService kycService, ILogger<IsUserVerifiedHandler> logger)
    {
        _kycService = kycService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(IsUserVerifiedQuery request, CancellationToken cancellationToken)
    {
        try
        {
            return await _kycService.IsUserVerifiedAsync(request.UserId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user {UserId} is verified", request.UserId);
            return Result<bool>.Failure($"Failed to check verification status: {ex.Message}");
        }
    }
}

public class GetKycVerificationsByStatusHandler : IRequestHandler<GetKycVerificationsByStatusQuery, Result<List<UserKycVerification>>>
{
    private readonly IKycService _kycService;
    private readonly ILogger<GetKycVerificationsByStatusHandler> _logger;

    public GetKycVerificationsByStatusHandler(IKycService kycService, ILogger<GetKycVerificationsByStatusHandler> logger)
    {
        _kycService = kycService;
        _logger = logger;
    }

    public async Task<Result<List<UserKycVerification>>> Handle(GetKycVerificationsByStatusQuery request, CancellationToken cancellationToken)
    {
        try
        {
            return await _kycService.GetVerificationsByStatusAsync(request.Status, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting KYC verifications by status {Status}", request.Status);
            return Result<List<UserKycVerification>>.Failure($"Failed to get verifications: {ex.Message}");
        }
    }
}

public class GetKycComplianceReportHandler : IRequestHandler<GetKycComplianceReportQuery, Result<KycComplianceReportDto>>
{
    private readonly IKycService _kycService;
    private readonly ILogger<GetKycComplianceReportHandler> _logger;

    public GetKycComplianceReportHandler(IKycService kycService, ILogger<GetKycComplianceReportHandler> logger)
    {
        _kycService = kycService;
        _logger = logger;
    }

    public async Task<Result<KycComplianceReportDto>> Handle(GetKycComplianceReportQuery request, CancellationToken cancellationToken)
    {
        try
        {
            return await _kycService.GetComplianceReportAsync(request.StartDate, request.EndDate, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating KYC compliance report");
            return Result<KycComplianceReportDto>.Failure($"Failed to generate report: {ex.Message}");
        }
    }
}
