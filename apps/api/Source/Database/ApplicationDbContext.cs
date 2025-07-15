using System.Reflection;
using GameGuild.Common;
using GameGuild.Modules.Authentication;
using GameGuild.Modules.Certificates;
using GameGuild.Modules.Comments;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Credentials;
using GameGuild.Modules.Feedbacks;
using GameGuild.Modules.Kyc.Models;
using GameGuild.Modules.Localization;
using GameGuild.Modules.Payments;
using GameGuild.Modules.Permissions;
using GameGuild.Modules.Posts.Models;
using GameGuild.Modules.Products;
using GameGuild.Modules.Programs;
using GameGuild.Modules.Projects;
using GameGuild.Modules.Reputations;
using GameGuild.Modules.Resources;
using GameGuild.Modules.Subscriptions.Models;
using GameGuild.Modules.Tags.Models;
using GameGuild.Modules.Tenants;
using GameGuild.Modules.TestingLab;
using GameGuild.Modules.UserProfiles;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;
using Tag = GameGuild.Modules.Tags.Models.Tag;


namespace GameGuild.Database;

// NOTE: do not add fluent api configurations here, they should be in the same file of the entity. On the entity, use notations for simple configurations, and fluent API for complex ones.
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options) {
  // DbSets
  public DbSet<User> Users { get; set; }

  public DbSet<UserProfile> UserProfiles { get; set; }

  public DbSet<Credential> Credentials { get; set; }

  public DbSet<RefreshToken> RefreshTokens { get; set; }

  public DbSet<Tenant> Tenants { get; set; }

  public DbSet<TenantPermission> TenantPermissions { get; set; } = null!;

  public DbSet<TenantDomain> TenantDomains { get; set; } = null!;

  public DbSet<TenantUserGroup> TenantUserGroups { get; set; } = null!;

  public DbSet<TenantUserGroupMembership> TenantUserGroupMemberships { get; set; } = null!;

  // Resource hierarchy DbSet - Required for proper inheritance configuration
  public DbSet<Resource> Resources { get; set; }

  // Resource and Localization DbSets
  public DbSet<Language> Languages { get; set; }

  // Content hierarchy DbSets - Required for TPC inheritance configuration
  public DbSet<ContentLicense> ContentLicenses { get; set; }

  public DbSet<ResourceMetadata> ResourceMetadata { get; set; }

  public DbSet<ContentTypePermission> ContentTypePermissions { get; set; }

  public DbSet<ResourceLocalization> ResourceLocalizations { get; set; }

  // Resource Permission DbSets (Layer 3 of DAC system)
  public DbSet<CommentPermission> CommentPermissions { get; set; }

  public DbSet<ProgramPermission> ProgramPermissions { get; set; }

  public DbSet<ProductPermission> ProductPermissions { get; set; }

  public DbSet<ProjectPermission> ProjectPermissions { get; set; }

  // Reputation Management DbSets
  public DbSet<UserReputation> UserReputations { get; set; }

  public DbSet<UserTenantReputation> UserTenantReputations { get; set; }

  public DbSet<ReputationTier> ReputationTiers { get; set; }

  public DbSet<ReputationAction> ReputationActions { get; set; }

  public DbSet<UserReputationHistory> UserReputationHistory { get; set; }

  // Product Management DbSets
  public DbSet<Product> Products { get; set; }

  public DbSet<ProductPricing> ProductPricings { get; set; }

  public DbSet<ProductProgram> ProductPrograms { get; set; }

  public DbSet<ProductSubscriptionPlan> ProductSubscriptionPlans { get; set; }

  public DbSet<UserProduct> UserProducts { get; set; }

  public DbSet<PromoCode> PromoCodes { get; set; }

  public DbSet<PromoCodeUse> PromoCodeUses { get; set; }

  // Posts Management DbSets
  public DbSet<Modules.Posts.Post> Posts { get; set; }

  public DbSet<Modules.Posts.PostComment> PostComments { get; set; }

  public DbSet<Modules.Posts.PostLike> PostLikes { get; set; }

  public DbSet<Modules.Posts.PostContentReference> PostContentReferences { get; set; }

  public DbSet<PostStatistics> PostStatistics { get; set; }

  public DbSet<PostFollower> PostFollowers { get; set; }

  public DbSet<PostTag> PostTags { get; set; }

  public DbSet<PostTagAssignment> PostTagAssignments { get; set; }

  public DbSet<PostView> PostViews { get; set; }

  // Project Management DbSets
  public DbSet<Project> Projects { get; set; }

  public DbSet<ProjectCollaborator> ProjectCollaborators { get; set; }

  public DbSet<ProjectRelease> ProjectReleases { get; set; }

  public DbSet<ProjectTeam> ProjectTeams { get; set; }

  public DbSet<ProjectFollower> ProjectFollowers { get; set; }

  public DbSet<ProjectFeedback> ProjectFeedbacks { get; set; }

  public DbSet<ProjectJamSubmission> ProjectJamSubmissions { get; set; } // Test Module DbSets

  public DbSet<TestingRequest> TestingRequests { get; set; }

  public DbSet<TestingSession> TestingSessions { get; set; }

  public DbSet<TestingParticipant> TestingParticipants { get; set; }

  public DbSet<TestingFeedback> TestingFeedback { get; set; }

  public DbSet<TestingFeedbackForm> TestingFeedbackForms { get; set; }

  public DbSet<TestingLocation> TestingLocations { get; set; }

  public DbSet<SessionRegistration> SessionRegistrations { get; set; }

  public DbSet<SessionWaitlist> SessionWaitlists { get; set; } // Program Management DbSets

  public DbSet<GameGuild.Modules.Programs.Program> Programs { get; set; }

  public DbSet<ProgramContent> ProgramContents { get; set; }

  public DbSet<ProgramUser> ProgramUsers { get; set; }

  public DbSet<ContentInteraction> ContentInteractions { get; set; }

  public DbSet<ActivityGrade> ActivityGrades { get; set; }

  // Program Enrollment and Progress DbSets
  public DbSet<ProgramEnrollment> ProgramEnrollments { get; set; }

  public DbSet<ContentProgress> ContentProgress { get; set; }

  // Peer Review System DbSets
  public DbSet<PeerReview> PeerReviews { get; set; }

  // Reporting System DbSets
  public DbSet<ContentReport> ContentReports { get; set; }

  // Certificate Management DbSets
  public DbSet<Certificate> Certificates { get; set; }

  public DbSet<UserCertificate> UserCertificates { get; set; }

  public DbSet<CertificateTag> CertificateTags { get; set; }

  public DbSet<CertificateBlockchainAnchor> CertificateBlockchainAnchors { get; set; }

  // Tag Management DbSets
  public DbSet<Tag> Tags { get; set; }

  public DbSet<TagRelationship> TagRelationships { get; set; }

  public DbSet<TagProficiency> TagProficiencies { get; set; }

  // Subscription Management DbSets
  public DbSet<UserSubscription> UserSubscriptions { get; set; }

  // Payment Management DbSets
  public DbSet<Payment> Payments { get; set; }

  public DbSet<PaymentRefund> PaymentRefunds { get; set; }

  public DbSet<DiscountCode> DiscountCodes { get; set; }

  public DbSet<UserFinancialMethod> UserFinancialMethods { get; set; }

  public DbSet<FinancialTransaction> FinancialTransactions { get; set; }

  // KYC Management DbSets
  public DbSet<UserKycVerification> UserKycVerifications { get; set; }

  // Feedback Management DbSets
  public DbSet<ProgramFeedbackSubmission> ProgramFeedbackSubmissions { get; set; }

  public DbSet<ProgramRating> ProgramRatings { get; set; }

  public DbSet<ProgramWishlist> ProgramWishlists { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder) {
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
    foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                                           .Where(t => typeof(ITenantable).IsAssignableFrom(t.ClrType))) {
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
  public override int SaveChanges() {
    UpdateTimestamps();

    return base.SaveChanges();
  }

  /// <summary>
  /// Automatically update timestamps when saving changes asynchronously
  /// </summary>
  public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) {
    UpdateTimestamps();

    return await base.SaveChangesAsync(cancellationToken);
  }

  /// <summary>
  /// Updates CreatedAt and UpdatedAt timestamps for entities that inherit from BaseEntity
  /// Also handles Version incrementing for optimistic concurrency control
  /// </summary>
  private void UpdateTimestamps() {
    var entries = ChangeTracker.Entries()
                               .Where(e => e is { Entity: IEntity, State: EntityState.Added or EntityState.Modified });

    foreach (var entry in entries) {
      var entity = (IEntity)entry.Entity;

      if (entry.State == EntityState.Added) {
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.Version = 1;
      }
      else if (entry.State == EntityState.Modified) {
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
  public ApplicationDbContext IncludeDeleted() {
    ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

    return this;
  }

  /// <summary>
  /// Configure inheritance strategies for the content and resource hierarchies
  /// </summary>
  private static void ConfigureInheritanceStrategies(ModelBuilder modelBuilder) {
    // Configure Table-Per-Concrete-Type (TPC) for ResourceBase inheritance
    // Each concrete entity that inherits from ResourceBase gets its own complete table
    // with all inherited properties included
    modelBuilder.Entity<Resource>().UseTpcMappingStrategy();

    // Additional inheritance configurations can be added here as needed
  }
}
