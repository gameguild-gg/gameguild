using Microsoft.EntityFrameworkCore;
using GameGuild.Common.Data;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Reflection;
using GameGuild.Common.Entities;
using GameGuild.Modules.Auth.Models;
using GameGuild.Modules.Certificate.Models;
using GameGuild.Modules.Comment.Models;
using GameGuild.Modules.Feedback.Models;
using GameGuild.Modules.Kyc.Models;
using GameGuild.Modules.Payment.Models;
using GameGuild.Modules.Product.Models;
using GameGuild.Modules.Program.Models;
using GameGuild.Modules.Project.Models;
using GameGuild.Modules.Reputation.Models;
using GameGuild.Modules.Subscription.Models;
using GameGuild.Modules.Tag.Models;
using GameGuild.Modules.Tenant.Models;
using GameGuild.Modules.TestingLab.Models;
using GameGuild.Modules.User.Models;
using Tag = GameGuild.Modules.Tag.Models.Tag;


namespace GameGuild.Data;

// NOTE: do not add fluent api configurations here, they should be in the same file of the entity. On the entity, use notations for simple configurations, and fluent API for complex ones.
public class ApplicationDbContext : DbContext
{
    private DbSet<User> _users;

    private DbSet<Credential> _credentials;

    private DbSet<RefreshToken> _refreshTokens;

    private DbSet<Tenant> _tenants;

    private DbSet<TenantPermission> _tenantPermissions;

    private DbSet<ResourceBase> _resources;

    private DbSet<Language> _languages;

    private DbSet<ContentLicense> _contentLicenses;

    private DbSet<ResourceMetadata> _resourceMetadata;

    private DbSet<ContentTypePermission> _contentTypePermissions;

    private DbSet<ResourceLocalization> _resourceLocalizations;

    private DbSet<CommentPermission> _commentPermissions;

    private DbSet<ProductPermission> _productPermissions;

    private DbSet<ProjectPermission> _projectPermissions;

    private DbSet<UserReputation> _userReputations;

    private DbSet<UserTenantReputation> _userTenantReputations;

    private DbSet<ReputationTier> _reputationTiers;

    private DbSet<ReputationAction> _reputationActions;

    private DbSet<UserReputationHistory> _userReputationHistory;

    private DbSet<Product> _products;

    private DbSet<ProductPricing> _productPricings;

    private DbSet<ProductProgram> _productPrograms;

    private DbSet<ProductSubscriptionPlan> _productSubscriptionPlans;

    private DbSet<UserProduct> _userProducts;

    private DbSet<PromoCode> _promoCodes;

    private DbSet<PromoCodeUse> _promoCodeUses;

    private DbSet<Project> _projects;

    private DbSet<ProjectCollaborator> _projectCollaborators;

    private DbSet<ProjectRelease> _projectReleases;

    private DbSet<ProjectTeam> _projectTeams;

    private DbSet<ProjectFollower> _projectFollowers;

    private DbSet<ProjectFeedback> _projectFeedbacks;

    private DbSet<ProjectJamSubmission> _projectJamSubmissions;

    private DbSet<TestingRequest> _testingRequests;

    private DbSet<TestingSession> _testingSessions;

    private DbSet<TestingParticipant> _testingParticipants;

    private DbSet<TestingFeedback> _testingFeedback;

    private DbSet<TestingFeedbackForm> _testingFeedbackForms;

    private DbSet<TestingLocation> _testingLocations;

    private DbSet<SessionRegistration> _sessionRegistrations;

    private DbSet<SessionWaitlist> _sessionWaitlists;

    private DbSet<Modules.Program.Models.Program> _programs;

    private DbSet<ProgramContent> _programContents;

    private DbSet<ProgramUser> _programUsers;

    private DbSet<ContentInteraction> _contentInteractions;

    private DbSet<ActivityGrade> _activityGrades;

    private DbSet<Certificate> _certificates;

    private DbSet<UserCertificate> _userCertificates;

    private DbSet<CertificateTag> _certificateTags;

    private DbSet<CertificateBlockchainAnchor> _certificateBlockchainAnchors;

    private DbSet<Tag> _tags;

    private DbSet<TagRelationship> _tagRelationships;

    private DbSet<TagProficiency> _tagProficiencies;

    private DbSet<UserSubscription> _userSubscriptions;

    private DbSet<UserFinancialMethod> _userFinancialMethods;

    private DbSet<FinancialTransaction> _financialTransactions;

    private DbSet<UserKycVerification> _userKycVerifications;

    private DbSet<ProgramFeedbackSubmission> _programFeedbackSubmissions;

    private DbSet<ProgramRating> _programRatings;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // DbSets
    public DbSet<User> Users
    {
        get => _users;
        set => _users = value;
    }

    public DbSet<Credential> Credentials
    {
        get => _credentials;
        set => _credentials = value;
    }

    public DbSet<RefreshToken> RefreshTokens
    {
        get => _refreshTokens;
        set => _refreshTokens = value;
    }

    public DbSet<Tenant> Tenants
    {
        get => _tenants;
        set => _tenants = value;
    }

    public DbSet<TenantPermission> TenantPermissions
    {
        get => _tenantPermissions;
        set => _tenantPermissions = value;
    }

    // Resource hierarchy DbSet - Required for proper inheritance configuration
    public DbSet<ResourceBase> Resources
    {
        get => _resources;
        set => _resources = value;
    }

    // Resource and Localization DbSets
    public DbSet<Language> Languages
    {
        get => _languages;
        set => _languages = value;
    }

    // Content hierarchy DbSets - Required for TPC inheritance configuration
    public DbSet<ContentLicense> ContentLicenses
    {
        get => _contentLicenses;
        set => _contentLicenses = value;
    }

    public DbSet<ResourceMetadata> ResourceMetadata
    {
        get => _resourceMetadata;
        set => _resourceMetadata = value;
    }

    public DbSet<ContentTypePermission> ContentTypePermissions
    {
        get => _contentTypePermissions;
        set => _contentTypePermissions = value;
    }

    public DbSet<ResourceLocalization> ResourceLocalizations
    {
        get => _resourceLocalizations;
        set => _resourceLocalizations = value;
    }

    // Resource Permission DbSets (Layer 3 of DAC system)
    public DbSet<Modules.Comment.Models.CommentPermission> CommentPermissions
    {
        get => _commentPermissions;
        set => _commentPermissions = value;
    }

    public DbSet<Modules.Product.Models.ProductPermission> ProductPermissions
    {
        get => _productPermissions;
        set => _productPermissions = value;
    }

    public DbSet<ProjectPermission> ProjectPermissions
    {
        get => _projectPermissions;
        set => _projectPermissions = value;
    }

    // Reputation Management DbSets
    public DbSet<Modules.Reputation.Models.UserReputation> UserReputations
    {
        get => _userReputations;
        set => _userReputations = value;
    }

    public DbSet<Modules.Reputation.Models.UserTenantReputation> UserTenantReputations
    {
        get => _userTenantReputations;
        set => _userTenantReputations = value;
    }

    public DbSet<Modules.Reputation.Models.ReputationTier> ReputationTiers
    {
        get => _reputationTiers;
        set => _reputationTiers = value;
    }

    public DbSet<Modules.Reputation.Models.ReputationAction> ReputationActions
    {
        get => _reputationActions;
        set => _reputationActions = value;
    }

    public DbSet<Modules.Reputation.Models.UserReputationHistory> UserReputationHistory
    {
        get => _userReputationHistory;
        set => _userReputationHistory = value;
    }

    // Product Management DbSets
    public DbSet<Modules.Product.Models.Product> Products
    {
        get => _products;
        set => _products = value;
    }

    public DbSet<Modules.Product.Models.ProductPricing> ProductPricings
    {
        get => _productPricings;
        set => _productPricings = value;
    }

    public DbSet<Modules.Product.Models.ProductProgram> ProductPrograms
    {
        get => _productPrograms;
        set => _productPrograms = value;
    }

    public DbSet<Modules.Product.Models.ProductSubscriptionPlan> ProductSubscriptionPlans
    {
        get => _productSubscriptionPlans;
        set => _productSubscriptionPlans = value;
    }

    public DbSet<Modules.Product.Models.UserProduct> UserProducts
    {
        get => _userProducts;
        set => _userProducts = value;
    }

    public DbSet<Modules.Product.Models.PromoCode> PromoCodes
    {
        get => _promoCodes;
        set => _promoCodes = value;
    }

    public DbSet<Modules.Product.Models.PromoCodeUse> PromoCodeUses
    {
        get => _promoCodeUses;
        set => _promoCodeUses = value;
    } // Project Management DbSets

    public DbSet<Project> Projects
    {
        get => _projects;
        set => _projects = value;
    }

    public DbSet<ProjectCollaborator> ProjectCollaborators
    {
        get => _projectCollaborators;
        set => _projectCollaborators = value;
    }

    public DbSet<ProjectRelease> ProjectReleases
    {
        get => _projectReleases;
        set => _projectReleases = value;
    }

    public DbSet<ProjectTeam> ProjectTeams
    {
        get => _projectTeams;
        set => _projectTeams = value;
    }

    public DbSet<ProjectFollower> ProjectFollowers
    {
        get => _projectFollowers;
        set => _projectFollowers = value;
    }

    public DbSet<ProjectFeedback> ProjectFeedbacks
    {
        get => _projectFeedbacks;
        set => _projectFeedbacks = value;
    }

    public DbSet<ProjectJamSubmission> ProjectJamSubmissions
    {
        get => _projectJamSubmissions;
        set => _projectJamSubmissions = value;
    } // Test Module DbSets

    public DbSet<Modules.TestingLab.Models.TestingRequest> TestingRequests
    {
        get => _testingRequests;
        set => _testingRequests = value;
    }

    public DbSet<Modules.TestingLab.Models.TestingSession> TestingSessions
    {
        get => _testingSessions;
        set => _testingSessions = value;
    }

    public DbSet<Modules.TestingLab.Models.TestingParticipant> TestingParticipants
    {
        get => _testingParticipants;
        set => _testingParticipants = value;
    }

    public DbSet<Modules.TestingLab.Models.TestingFeedback> TestingFeedback
    {
        get => _testingFeedback;
        set => _testingFeedback = value;
    }

    public DbSet<Modules.TestingLab.Models.TestingFeedbackForm> TestingFeedbackForms
    {
        get => _testingFeedbackForms;
        set => _testingFeedbackForms = value;
    }

    public DbSet<Modules.TestingLab.Models.TestingLocation> TestingLocations
    {
        get => _testingLocations;
        set => _testingLocations = value;
    }

    public DbSet<Modules.TestingLab.Models.SessionRegistration> SessionRegistrations
    {
        get => _sessionRegistrations;
        set => _sessionRegistrations = value;
    }

    public DbSet<Modules.TestingLab.Models.SessionWaitlist> SessionWaitlists
    {
        get => _sessionWaitlists;
        set => _sessionWaitlists = value;
    } // Program Management DbSets

    public DbSet<Modules.Program.Models.Program> Programs
    {
        get => _programs;
        set => _programs = value;
    }

    public DbSet<Modules.Program.Models.ProgramContent> ProgramContents
    {
        get => _programContents;
        set => _programContents = value;
    }

    public DbSet<Modules.Program.Models.ProgramUser> ProgramUsers
    {
        get => _programUsers;
        set => _programUsers = value;
    }

    public DbSet<Modules.Program.Models.ContentInteraction> ContentInteractions
    {
        get => _contentInteractions;
        set => _contentInteractions = value;
    }

    public DbSet<Modules.Program.Models.ActivityGrade> ActivityGrades
    {
        get => _activityGrades;
        set => _activityGrades = value;
    }

    // Certificate Management DbSets
    public DbSet<Modules.Certificate.Models.Certificate> Certificates
    {
        get => _certificates;
        set => _certificates = value;
    }

    public DbSet<Modules.Certificate.Models.UserCertificate> UserCertificates
    {
        get => _userCertificates;
        set => _userCertificates = value;
    }

    public DbSet<Modules.Certificate.Models.CertificateTag> CertificateTags
    {
        get => _certificateTags;
        set => _certificateTags = value;
    }

    public DbSet<Modules.Certificate.Models.CertificateBlockchainAnchor> CertificateBlockchainAnchors
    {
        get => _certificateBlockchainAnchors;
        set => _certificateBlockchainAnchors = value;
    }

    // Tag Management DbSets
    public DbSet<Modules.Tag.Models.Tag> Tags
    {
        get => _tags;
        set => _tags = value;
    }

    public DbSet<Modules.Tag.Models.TagRelationship> TagRelationships
    {
        get => _tagRelationships;
        set => _tagRelationships = value;
    }

    public DbSet<Modules.Tag.Models.TagProficiency> TagProficiencies
    {
        get => _tagProficiencies;
        set => _tagProficiencies = value;
    }

    // Subscription Management DbSets
    public DbSet<Modules.Subscription.Models.UserSubscription> UserSubscriptions
    {
        get => _userSubscriptions;
        set => _userSubscriptions = value;
    }

    // Payment Management DbSets
    public DbSet<Modules.Payment.Models.UserFinancialMethod> UserFinancialMethods
    {
        get => _userFinancialMethods;
        set => _userFinancialMethods = value;
    }

    public DbSet<Modules.Payment.Models.FinancialTransaction> FinancialTransactions
    {
        get => _financialTransactions;
        set => _financialTransactions = value;
    }

    // KYC Management DbSets
    public DbSet<Modules.Kyc.Models.UserKycVerification> UserKycVerifications
    {
        get => _userKycVerifications;
        set => _userKycVerifications = value;
    }

    // Feedback Management DbSets
    public DbSet<Modules.Feedback.Models.ProgramFeedbackSubmission> ProgramFeedbackSubmissions
    {
        get => _programFeedbackSubmissions;
        set => _programFeedbackSubmissions = value;
    }

    public DbSet<Modules.Feedback.Models.ProgramRating> ProgramRatings
    {
        get => _programRatings;
        set => _programRatings = value;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // NOTE: do not add fluent api configurations here, they should be in the same file of the entity. On the entity, use notations for simple configurations, and fluent API for complex ones.

        // Configure ContentTypePermission relationships explicitly to avoid ambiguity
        modelBuilder.Entity<ContentTypePermission>()
            .HasOne(ctp => ctp.User)
            .WithMany()
            .HasForeignKey(ctp => ctp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure TenantPermission relationships explicitly to avoid ambiguity
        modelBuilder.Entity<TenantPermission>()
            .HasOne(tp => tp.User)
            .WithMany()
            .HasForeignKey(tp => tp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure ITenantable entities (this logic needs to stay in OnModelCreating)
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(ITenantable).IsAssignableFrom(t.ClrType)))
        {
            modelBuilder.Entity(entityType.ClrType)
                .HasOne(typeof(Tenant).Name)
                .WithMany()
                .HasForeignKey("TenantId")
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        }

        // Configure base entity properties for all entities
        modelBuilder.ConfigureBaseEntities();

        // Configure soft delete global query filters
        modelBuilder.ConfigureSoftDelete();

        // Configure inheritance strategies for content hierarchy
        ConfigureInheritanceStrategies(modelBuilder);
    }

    /// <summary>
    /// Automatically update timestamps when saving changes
    /// </summary>
    public override int SaveChanges()
    {
        UpdateTimestamps();

        return base.SaveChanges();
    }

    /// <summary>
    /// Automatically update timestamps when saving changes asynchronously
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();

        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Updates CreatedAt and UpdatedAt timestamps for entities that inherit from BaseEntity
    /// Also handles Version incrementing for optimistic concurrency control
    /// </summary>
    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is IEntity && e.State is EntityState.Added or EntityState.Modified);

        foreach (EntityEntry entry in entries)
        {
            var entity = (IEntity)entry.Entity;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;
                entity.Version = 1;
            }
            else if (entry.State == EntityState.Modified)
            {
                // Don't update CreatedAt on modifications
                entry.Property(nameof(IEntity.CreatedAt)).IsModified = false;
                entity.UpdatedAt = DateTime.UtcNow;
                entity.Version++;
            }
        }
    }

    /// <summary>
    /// Include soft-deleted entities in queries
    /// </summary>
    /// <returns>DbContext with soft-deleted entities included</returns>
    public ApplicationDbContext IncludeDeleted()
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

        return this;
    }

    /// <summary>
    /// Configure inheritance strategies for the content and resource hierarchies
    /// </summary>
    private void ConfigureInheritanceStrategies(ModelBuilder modelBuilder)
    {
        // Configure Table-Per-Concrete-Type (TPC) for ResourceBase inheritance
        // Each concrete entity that inherits from ResourceBase gets its own complete table
        // with all inherited properties included
        modelBuilder.Entity<ResourceBase>().UseTpcMappingStrategy();

        // Additional inheritance configurations can be added here as needed
    }
}
