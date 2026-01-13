using GameGuild.Abstractions;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Authorization;
using GameGuild.Products;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GameGuild.API.Database;

/// <summary>
///     Main application database context that implements IApplicationDbContext.
///     Contains all entities from all modules in the application.
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options), IApplicationDbContext
{
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) { return await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false); }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) { return await Database.BeginTransactionAsync(cancellationToken); }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // Apply Tenants module configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Tenant).Assembly, type => type.Namespace?.StartsWith("GameGuild.Tenants") == true);

        // Apply Users module configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(User).Assembly, type => type.Namespace?.StartsWith("GameGuild.Users") == true);

        // Apply Authentication module configurations (excluding AuthUser which has been removed)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RefreshToken).Assembly, type => 
            (type.Namespace?.StartsWith("GameGuild.Authentication") == true || 
             type.Namespace?.StartsWith("GameGuild.Identity.Authentication") == true) && 
            !type.Name.Contains("AuthUser"));

        // Apply Authorization module configurations
        AuthorizationModule.ConfigureAuthorizationModel(modelBuilder);

        // Apply Products module configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Product).Assembly, type => type.Namespace?.StartsWith("GameGuild.Products") == true);

        // NOTE: The following modules are currently disabled. Uncomment when enabling them.
        // // Apply Resources module configurations (using automatic discovery)
        // modelBuilder.ApplyConfigurationsFromAssembly(typeof(ResourceQuota).Assembly, type => type.Namespace?.StartsWith("GameGuild.Resources.Entities") == true);

        // // Apply Payments module configurations - filter out abstract types
        // modelBuilder.ApplyConfigurationsFromAssembly(
        //     typeof(RevenueEvent).Assembly, 
        //     type => type.Namespace?.StartsWith("GameGuild.Payments.Data.Configurations") == true 
        //             && !type.IsAbstract);
        
        // // Explicitly ignore abstract types from Payments module that EF Core might discover
        // var paymentsAssembly = typeof(RevenueEvent).Assembly;
        // var abstractTypesToIgnore = paymentsAssembly.GetTypes()
        //     .Where(t => t.IsAbstract && t.IsClass && t.Namespace?.StartsWith("GameGuild.Payments") == true);
        
        // foreach (var abstractType in abstractTypesToIgnore)
        // {
        //     // Use reflection to call modelBuilder.Ignore<T>() for each abstract type
        //     var ignoreMethod = typeof(ModelBuilder).GetMethod("Ignore", [typeof(Type)]);
        //     if (ignoreMethod != null)
        //     {
        //         ignoreMethod.Invoke(modelBuilder, [abstractType]);
        //     }
        // }

        // // Apply Subscriptions module configurations
        // modelBuilder.ApplyConfigurationsFromAssembly(typeof(GameGuild.Subscriptions.Entities.Subscription).Assembly, type => type.Namespace?.StartsWith("GameGuild.Subscriptions.Data.Configurations") == true);

        // // Apply Programs module configurations
        // modelBuilder.ApplyConfigurationsFromAssembly(typeof(ActivityGrade).Assembly, type => 
        //     type.Namespace?.StartsWith("GameGuild.Modules.Programs") == true && 
        //     typeof(Microsoft.EntityFrameworkCore.IEntityTypeConfiguration<>).IsAssignableFrom(type) == false &&
        //     type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>)));

        base.OnModelCreating(modelBuilder);
    }

    #region Users Module

    public DbSet<User> Users { get => Set<User>(); }

    public DbSet<UserMetadata> UserMetadata { get => Set<UserMetadata>(); }

    public DbSet<UserPreferences> UserPreferences { get => Set<UserPreferences>(); }

    public DbSet<UserProfile> UserProfiles { get => Set<UserProfile>(); }

    public DbSet<UserNotification> UserNotifications { get => Set<UserNotification>(); }

    #endregion

    #region Tenants Module

    public DbSet<Tenant> Tenants { get => Set<Tenant>(); }

    public DbSet<TenantMember> TenantMembers { get => Set<TenantMember>(); }

    public DbSet<TenantDomain> TenantDomains { get => Set<TenantDomain>(); }

    public DbSet<TenantSettings> TenantSettings { get => Set<TenantSettings>(); }

    public DbSet<TenantStatistics> TenantStatistics { get => Set<TenantStatistics>(); }

    public DbSet<UsageTracking> UsageTracking { get => Set<UsageTracking>(); }

    public DbSet<TenantMetadata> TenantMetadata { get => Set<TenantMetadata>(); }

    #endregion

    // NOTE: The following modules are currently disabled. Uncomment when enabling them.

    #region Resources Module (DISABLED)

    // public DbSet<ResourceQuota> ResourceQuotas { get => Set<ResourceQuota>(); }

    // public DbSet<UsageRecord> UsageRecords { get => Set<UsageRecord>(); }

    // public DbSet<CostAllocationReport> CostAllocationReports { get => Set<CostAllocationReport>(); }

    // public DbSet<ResourceUsageTrend> ResourceUsageTrends { get => Set<ResourceUsageTrend>(); }

    // public DbSet<ResourceThrottlingPolicy> ResourceThrottlingPolicies { get => Set<ResourceThrottlingPolicy>(); }

    // public DbSet<UsageRetentionPolicy> UsageRetentionPolicies { get => Set<UsageRetentionPolicy>(); }

    // public DbSet<SlaImpactAnalysis> SlaImpactAnalyses { get => Set<SlaImpactAnalysis>(); }

    #endregion

    #region Audit Module (DISABLED)

    // public DbSet<GameGuild.Audit.AuditLog> AuditLogs { get => Set<GameGuild.Audit.AuditLog>(); }

    #endregion

    #region Authentication Module

    // NOTE: AuthUsers table has been removed. User authentication data is now stored in the Users table.
    // The User entity includes: Username, PasswordHash, IsEmailVerified, LastLoginAt fields.

    public DbSet<AuthenticationAttempt> AuthenticationAttempts { get => Set<AuthenticationAttempt>(); }

    public DbSet<RefreshToken> RefreshTokens { get => Set<RefreshToken>(); }

    public DbSet<UserSession> UserSessions { get => Set<UserSession>(); }

    public DbSet<TrustedDevice> TrustedDevices { get => Set<TrustedDevice>(); }

    public DbSet<MfaAttempt> MfaAttempts { get => Set<MfaAttempt>(); }

    public DbSet<UserMfaConfiguration> UserMfaConfigurations { get => Set<UserMfaConfiguration>(); }

    public DbSet<UserWebAuthnCredential> UserWebAuthnCredentials { get => Set<UserWebAuthnCredential>(); }

    public DbSet<Role> Roles { get => Set<Role>(); }

    public DbSet<UserRole> UserRoles { get => Set<UserRole>(); }

    public DbSet<ServiceAccount> ServiceAccounts { get => Set<ServiceAccount>(); }

    public DbSet<IdentityVerification> IdentityVerifications { get => Set<IdentityVerification>(); }

    public DbSet<BlockchainCertificateAnchor> BlockchainCertificateAnchors { get => Set<BlockchainCertificateAnchor>(); }

    public DbSet<ContentTypePermission> ContentTypePermissions { get => Set<ContentTypePermission>(); }

    #endregion

    #region Authorization Module

    public DbSet<PolicyDefinitionEntity> PolicyDefinitions { get => Set<PolicyDefinitionEntity>(); }

    public DbSet<GameGuild.Identity.Authorization.AbacPolicy> AbacPolicies { get => Set<GameGuild.Identity.Authorization.AbacPolicy>(); }

    public DbSet<GameGuild.Identity.Authorization.ConditionalPolicy> ConditionalPolicies { get => Set<GameGuild.Identity.Authorization.ConditionalPolicy>(); }

    public DbSet<GameGuild.Identity.Authorization.TenantPermission> TenantPermissions { get => Set<GameGuild.Identity.Authorization.TenantPermission>(); }

    public DbSet<JitElevationRequest> JitElevationRequests { get => Set<JitElevationRequest>(); }

    public DbSet<PermissionDelegation> PermissionDelegations { get => Set<PermissionDelegation>(); }

    public DbSet<SoDRule> SoDRules { get => Set<SoDRule>(); }

    public DbSet<SoDViolation> SoDViolations { get => Set<SoDViolation>(); }

    public DbSet<GameGuild.Identity.Authorization.AccessReviewCampaign> AccessReviewCampaigns { get => Set<GameGuild.Identity.Authorization.AccessReviewCampaign>(); }

    public DbSet<GameGuild.Identity.Authorization.AccessReviewItem> AccessReviewItems { get => Set<GameGuild.Identity.Authorization.AccessReviewItem>(); }

    public DbSet<DelegatedAdminScope> DelegatedAdminScopes { get => Set<DelegatedAdminScope>(); }

    public DbSet<DataMaskingRule> DataMaskingRules { get => Set<DataMaskingRule>(); }

    public DbSet<TenantSecurityVersion> TenantSecurityVersions { get => Set<TenantSecurityVersion>(); }

    public DbSet<AccessControlListEntry> AccessControlListEntries { get => Set<AccessControlListEntry>(); }

    #endregion

    #region Products Module

    public DbSet<Product> Products { get => Set<Product>(); }

    public DbSet<ProductPricing> ProductPricings { get => Set<ProductPricing>(); }

    public DbSet<ProductSubscriptionPlan> ProductSubscriptionPlans { get => Set<ProductSubscriptionPlan>(); }

    public DbSet<PricingRule> PricingRules { get => Set<PricingRule>(); }

    public DbSet<PricingTier> PricingTiers { get => Set<PricingTier>(); }

    public DbSet<PromoCode> PromoCodes { get => Set<PromoCode>(); }

    public DbSet<PromoCodeUse> PromoCodeUses { get => Set<PromoCodeUse>(); }

    public DbSet<PromoStackingRule> PromoStackingRules { get => Set<PromoStackingRule>(); }

    public DbSet<UserProduct> UserProducts { get => Set<UserProduct>(); }

    #endregion

    #region Payments Module (DISABLED)

    // public DbSet<RevenueEvent> RevenueEvents { get => Set<RevenueEvent>(); }

    // public DbSet<FinancialLedgerEntry> FinancialLedgerEntries { get => Set<FinancialLedgerEntry>(); }

    // public DbSet<AuditTrail> AuditTrails { get => Set<AuditTrail>(); }

    // public DbSet<UserWallet> UserWallets { get => Set<UserWallet>(); }

    // public DbSet<WalletTransaction> WalletTransactions { get => Set<WalletTransaction>(); }

    // public DbSet<PaymentDispute> PaymentDisputes { get => Set<PaymentDispute>(); }

    // public DbSet<DisputeEvidence> DisputeEvidences { get => Set<DisputeEvidence>(); }

    #endregion

    #region Programs Module (DISABLED)

    // public DbSet<GameGuild.Modules.Programs.Entities.Program> Programs { get => Set<GameGuild.Modules.Programs.Entities.Program>(); }

    // public DbSet<GameGuild.Modules.Programs.Entities.ProgramContent> ProgramContents { get => Set<GameGuild.Modules.Programs.Entities.ProgramContent>(); }

    // public DbSet<GameGuild.Modules.Programs.Entities.ProgramUser> ProgramUsers { get => Set<GameGuild.Modules.Programs.Entities.ProgramUser>(); }

    // public DbSet<GameGuild.Modules.Programs.Entities.ProgramEnrollment> ProgramEnrollments { get => Set<GameGuild.Modules.Programs.Entities.ProgramEnrollment>(); }

    // public DbSet<GameGuild.Modules.Programs.Entities.ContentInteraction> ContentInteractions { get => Set<GameGuild.Modules.Programs.Entities.ContentInteraction>(); }

    // public DbSet<GameGuild.Modules.Programs.Entities.ActivityGrade> ActivityGrades { get => Set<GameGuild.Modules.Programs.Entities.ActivityGrade>(); }

    // public DbSet<GameGuild.Modules.Programs.ContentProgress> ContentProgress { get => Set<GameGuild.Modules.Programs.ContentProgress>(); }

    // public DbSet<GameGuild.Modules.Programs.ProgramRating> ProgramRatings { get => Set<GameGuild.Modules.Programs.ProgramRating>(); }

    // public DbSet<GameGuild.Modules.Programs.Entities.ProgramWishlist> ProgramWishlists { get => Set<GameGuild.Modules.Programs.Entities.ProgramWishlist>(); }

    // public DbSet<GameGuild.Modules.Programs.Entities.ProductProgram> ProductPrograms { get => Set<GameGuild.Modules.Programs.Entities.ProductProgram>(); }

    #endregion
}
