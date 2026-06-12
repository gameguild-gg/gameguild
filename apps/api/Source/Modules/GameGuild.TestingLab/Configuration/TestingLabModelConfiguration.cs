using GameGuild.Identity.Tenants;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.TestingLab;

public sealed class TestingLabModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        ConfigureTestingRequest(modelBuilder);
        ConfigureTestingSession(modelBuilder);
        ConfigureTestingLocation(modelBuilder);
        ConfigureTestingParticipant(modelBuilder);
        ConfigureTestingFeedback(modelBuilder);
        ConfigureTestingFeedbackForm(modelBuilder);
        ConfigureFeedbackQualityRating(modelBuilder);
        ConfigureSessionRegistration(modelBuilder);
        ConfigureSessionWaitlist(modelBuilder);
        ConfigureSessionProject(modelBuilder);
        ConfigureTestingLabSettings(modelBuilder);
    }

    private static void ConfigureTestingRequest(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestingRequest>(builder =>
        {
            builder.ToTable("testing_requests");
            builder.HasKey(request => request.Id);
            builder.Property(request => request.Title).IsRequired().HasMaxLength(255);
            builder.Property(request => request.DownloadUrl).HasMaxLength(1000);
            builder.Property(request => request.InstructionsUrl).HasMaxLength(500);
            builder.Property(request => request.InstructionsType).HasConversion<string>().HasMaxLength(40);
            builder.Property(request => request.Status).HasConversion<string>().HasMaxLength(40);
            builder.Property(request => request.Priority).HasConversion<string>().HasMaxLength(40);
            builder.Property(request => request.Mode).HasConversion<string>().HasMaxLength(40);
            builder.HasOne(request => request.ProjectVersion)
                .WithMany()
                .HasForeignKey(request => request.ProjectVersionId)
                .OnDelete(DeleteBehavior.SetNull);
            builder.HasOne(request => request.CreatedBy)
                .WithMany()
                .HasForeignKey(request => request.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureTestingSession(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestingSession>(builder =>
        {
            builder.ToTable("testing_sessions");
            builder.HasKey(session => session.Id);
            builder.Property(session => session.SessionName).IsRequired().HasMaxLength(255);
            builder.Property(session => session.Status).HasConversion<string>().HasMaxLength(40);
            builder.HasOne(session => session.TestingRequest)
                .WithMany(request => request.Sessions)
                .HasForeignKey(session => session.TestingRequestId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(session => session.Location)
                .WithMany(location => location.Sessions)
                .HasForeignKey(session => session.LocationId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(session => session.Manager)
                .WithMany()
                .HasForeignKey(session => session.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(session => session.CreatedBy)
                .WithMany()
                .HasForeignKey(session => session.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureTestingLocation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestingLocation>(builder =>
        {
            builder.ToTable("testing_locations");
            builder.HasKey(location => location.Id);
            builder.Property(location => location.Name).IsRequired().HasMaxLength(200);
            builder.Property(location => location.Address).HasMaxLength(500);
            builder.Property(location => location.City).HasMaxLength(100);
            builder.Property(location => location.State).HasMaxLength(100);
            builder.Property(location => location.PostalCode).HasMaxLength(20);
            builder.Property(location => location.Country).HasMaxLength(100);
            builder.Property(location => location.VirtualUrl).HasMaxLength(500);
            builder.Property(location => location.ContactEmail).HasMaxLength(255);
            builder.Property(location => location.ContactPhone).HasMaxLength(50);
            builder.Property(location => location.Status).HasConversion<string>().HasMaxLength(40);
            builder.Ignore(location => location.MaxTestersCapacity);
            builder.Ignore(location => location.EquipmentAvailable);
        });
    }

    private static void ConfigureTestingParticipant(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestingParticipant>(builder =>
        {
            builder.ToTable("testing_participants");
            builder.HasKey(participant => participant.Id);
            builder.Property(participant => participant.Status).HasConversion<string>().HasMaxLength(40);
            builder.HasOne(participant => participant.TestingRequest)
                .WithMany(request => request.Participants)
                .HasForeignKey(participant => participant.TestingRequestId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(participant => participant.User)
                .WithMany()
                .HasForeignKey(participant => participant.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureTestingFeedback(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestingFeedback>(builder =>
        {
            builder.ToTable("testing_feedback");
            builder.HasKey(feedback => feedback.Id);
            builder.Property(feedback => feedback.TestingContext).HasConversion<string>().HasMaxLength(40);
            builder.Property(feedback => feedback.QualityRating).HasConversion<string>().HasMaxLength(40);
            builder.Property(feedback => feedback.ReportReason).HasMaxLength(500);
            builder.HasOne(feedback => feedback.TestingRequest)
                .WithMany(request => request.Feedback)
                .HasForeignKey(feedback => feedback.TestingRequestId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(feedback => feedback.FeedbackForm)
                .WithMany(form => form.Feedback)
                .HasForeignKey(feedback => feedback.FeedbackFormId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(feedback => feedback.User)
                .WithMany()
                .HasForeignKey(feedback => feedback.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(feedback => feedback.Session)
                .WithMany(session => session.Feedback)
                .HasForeignKey(feedback => feedback.SessionId)
                .OnDelete(DeleteBehavior.SetNull);
            builder.HasOne(feedback => feedback.ReportedBy)
                .WithMany()
                .HasForeignKey(feedback => feedback.ReportedById)
                .OnDelete(DeleteBehavior.SetNull);
            builder.Ignore(feedback => feedback.ReportedByUserId);
        });
    }

    private static void ConfigureTestingFeedbackForm(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestingFeedbackForm>(builder =>
        {
            builder.ToTable("testing_feedback_forms");
            builder.HasKey(form => form.Id);
            builder.Property(form => form.Name).IsRequired().HasMaxLength(200);
            builder.Property(form => form.FormData).IsRequired();
            builder.Property(form => form.FormType).HasConversion<string>().HasMaxLength(40);
            builder.Property(form => form.Tags).HasMaxLength(500);
            builder.HasOne<TestingRequest>()
                .WithMany(request => request.FeedbackForms)
                .HasForeignKey(form => form.TestingRequestId)
                .OnDelete(DeleteBehavior.SetNull);
            builder.Ignore(form => form.FormSchema);
        });
    }

    private static void ConfigureFeedbackQualityRating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FeedbackQualityRating>(builder =>
        {
            builder.ToTable("feedback_quality_ratings");
            builder.HasKey(rating => rating.Id);
            builder.Property(rating => rating.Reason).HasMaxLength(500);
            builder.HasOne(rating => rating.Feedback)
                .WithMany(feedback => feedback.QualityRatings)
                .HasForeignKey(rating => rating.FeedbackId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(rating => rating.RatedBy)
                .WithMany()
                .HasForeignKey(rating => rating.RatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureSessionRegistration(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SessionRegistration>(builder =>
        {
            builder.ToTable("session_registrations");
            builder.HasKey(registration => registration.Id);
            builder.Property(registration => registration.RegistrationType).HasConversion<string>().HasMaxLength(40);
            builder.Property(registration => registration.Status).HasConversion<string>().HasMaxLength(40);
            builder.Property(registration => registration.AttendanceStatus).HasConversion<string>().HasMaxLength(40);
            builder.HasOne(registration => registration.Session)
                .WithMany(session => session.Registrations)
                .HasForeignKey(registration => registration.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(registration => registration.User)
                .WithMany()
                .HasForeignKey(registration => registration.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Ignore(registration => registration.RegistrationNotes);
            builder.Ignore(registration => registration.AttendedAt);
        });
    }

    private static void ConfigureSessionWaitlist(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SessionWaitlist>(builder =>
        {
            builder.ToTable("session_waitlist");
            builder.HasKey(waitlist => waitlist.Id);
            builder.Property(waitlist => waitlist.RegistrationType).HasConversion<string>().HasMaxLength(40);
            builder.HasOne(waitlist => waitlist.Session)
                .WithMany()
                .HasForeignKey(waitlist => waitlist.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(waitlist => waitlist.User)
                .WithMany()
                .HasForeignKey(waitlist => waitlist.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureSessionProject(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SessionProject>(builder =>
        {
            builder.ToTable("session_projects");
            builder.HasKey(project => project.Id);
            builder.HasOne(project => project.Session)
                .WithMany()
                .HasForeignKey(project => project.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(project => project.Project)
                .WithMany()
                .HasForeignKey(project => project.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(project => project.ProjectVersion)
                .WithMany()
                .HasForeignKey(project => project.ProjectVersionId)
                .OnDelete(DeleteBehavior.SetNull);
            builder.HasOne(project => project.RegisteredBy)
                .WithMany()
                .HasForeignKey(project => project.RegisteredById)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureTestingLabSettings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestingLabSettings>(builder =>
        {
            builder.ToTable("testing_lab_settings");
            builder.HasKey(settings => settings.Id);
            builder.Property(settings => settings.LabName).IsRequired().HasMaxLength(255);
            builder.Property(settings => settings.Description).HasMaxLength(1000);
            builder.Property(settings => settings.Timezone).IsRequired().HasMaxLength(50);
            builder.HasOne(settings => settings.Tenant)
                .WithMany()
                .HasForeignKey(settings => settings.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
