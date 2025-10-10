using GameGuild.CQRS;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using GameGuild.Core;
using GameGuild.Database;
using GameGuild.Modules.Authentication;

namespace GameGuild.Modules.Users.Queries;

/// <summary>
/// Handler for GetUnifiedVerificationStatusQuery
/// Aggregates email, phone, and 2FA verification status from multiple sources
/// </summary>
public sealed class GetUnifiedVerificationStatusQueryHandler
    : IRequestHandler<GetUnifiedVerificationStatusQuery, Result<UnifiedVerificationStatusDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetUnifiedVerificationStatusQueryHandler> _logger;

    public GetUnifiedVerificationStatusQueryHandler(
        ApplicationDbContext context,
        ILogger<GetUnifiedVerificationStatusQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<UnifiedVerificationStatusDto>> Handle(
        GetUnifiedVerificationStatusQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching unified verification status for user {UserId}", request.UserId);

            // Get user data
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", request.UserId);
                return Result<UnifiedVerificationStatusDto>.Failure("User not found");
            }

            // Check email verification
            // For now, assume email is verified if user has an email address
            // In a real implementation, this would check a verification table or credential status  
            bool isEmailVerified = !string.IsNullOrEmpty(user.Email);
            DateTime? emailVerifiedAt = isEmailVerified ? user.CreatedAt : null;

            // Check phone verification
            // Assume phone is verified if user has a phone number
            bool isPhoneVerified = user.PhoneNumber != null;
            DateTime? phoneVerifiedAt = isPhoneVerified ? user.CreatedAt : null;

            // Check MFA status
            var mfaConfig = await _context.Set<UserMfaConfiguration>()
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == request.UserId, cancellationToken);

            bool isMfaEnabled = mfaConfig?.IsEnabled == true && mfaConfig.IsSetupComplete;
            string? mfaMethod = mfaConfig?.PreferredMethod.ToString();
            DateTime? mfaEnabledAt = mfaConfig?.EnabledAt;
            DateTime? mfaLastUsedAt = mfaConfig?.LastUsedAt;

            // Calculate security score
            int securityScore = UnifiedVerificationStatusDto.CalculateSecurityScore(
                isEmailVerified,
                isPhoneVerified,
                isMfaEnabled);

            // Generate recommendations
            var recommendations = UnifiedVerificationStatusDto.GenerateRecommendations(
                isEmailVerified,
                isPhoneVerified,
                isMfaEnabled);

            var dto = new UnifiedVerificationStatusDto
            {
                UserId = request.UserId,
                IsEmailVerified = isEmailVerified,
                Email = isEmailVerified ? user.Email : null,
                EmailVerifiedAt = emailVerifiedAt,
                IsPhoneVerified = isPhoneVerified,
                PhoneNumber = isPhoneVerified ? user.PhoneNumber?.ToString() : null,
                PhoneVerifiedAt = phoneVerifiedAt,
                IsTwoFactorEnabled = isMfaEnabled,
                MfaMethod = mfaMethod,
                TwoFactorEnabledAt = mfaEnabledAt,
                TwoFactorLastUsedAt = mfaLastUsedAt,
                SecurityScore = securityScore,
                SecurityRecommendations = recommendations
            };

            _logger.LogInformation(
                "Unified verification status retrieved for user {UserId}: Email={EmailVerified}, Phone={PhoneVerified}, MFA={MfaEnabled}, Score={Score}",
                request.UserId, isEmailVerified, isPhoneVerified, isMfaEnabled, securityScore);

            return Result<UnifiedVerificationStatusDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get unified verification status for user {UserId}", request.UserId);
            return Result<UnifiedVerificationStatusDto>.Failure($"Failed to retrieve verification status: {ex.Message}");
        }
    }
}
