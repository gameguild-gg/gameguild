using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddLearningExperienceCloseout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "course_discussions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Content = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                    ReplyCount = table.Column<int>(type: "integer", nullable: false),
                    ViewCount = table.Column<int>(type: "integer", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_discussions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "course_likes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(36)", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_likes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "course_reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Content = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    IsVerifiedPurchase = table.Column<bool>(type: "boolean", nullable: false),
                    HelpfulCount = table.Column<int>(type: "integer", nullable: false),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_reviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "course_wishlists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotifyOnSale = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyOnUpdate = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_wishlists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "discussion_replies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscussionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentReplyId = table.Column<Guid>(type: "uuid", nullable: true),
                    Content = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    IsAcceptedAnswer = table.Column<bool>(type: "boolean", nullable: false),
                    UpvoteCount = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discussion_replies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "learning_certificate_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TemplateHtml = table.Column<string>(type: "text", nullable: false),
                    TemplateStyles = table.Column<string>(type: "text", nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_certificate_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "learning_certificates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CertificateNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    RecipientName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    CourseName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevocationReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    VerificationUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DigitalSignature = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_certificates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "learning_cohorts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MaxCapacity = table.Column<int>(type: "integer", nullable: false),
                    CurrentEnrollmentCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IsOpen = table.Column<bool>(type: "boolean", nullable: false),
                    InstructorId = table.Column<Guid>(type: "uuid", nullable: true),
                    MeetingSchedule = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_cohorts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "learning_course_collections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CuratorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Slug = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    CourseCount = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_course_collections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "learning_course_recommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsViewed = table.Column<bool>(type: "boolean", nullable: false),
                    IsDismissed = table.Column<bool>(type: "boolean", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_course_recommendations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "learning_featured_content",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: true),
                    LearningPathId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Subtitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LinkUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TargetAudience = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_featured_content", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "learning_path_enrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearningPathId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Progress = table.Column<int>(type: "integer", nullable: false),
                    CoursesCompleted = table.Column<int>(type: "integer", nullable: false),
                    TotalCourses = table.Column<int>(type: "integer", nullable: false),
                    EnrolledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_path_enrollments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "learning_paths",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Slug = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EstimatedHours = table.Column<int>(type: "integer", nullable: false),
                    Difficulty = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    EnrollmentCount = table.Column<int>(type: "integer", nullable: false),
                    CompletionCount = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_paths", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "learning_search_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Query = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ResultCount = table.Column<int>(type: "integer", nullable: false),
                    ClickedCourseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClickedPosition = table.Column<int>(type: "integer", nullable: true),
                    Filters = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_search_history", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "learning_user_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreferredCategories = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    PreferredDifficulty = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    PreferredDuration = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    LearningGoals = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Skills = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    TotalCoursesCompleted = table.Column<int>(type: "integer", nullable: false),
                    TotalHoursLearned = table.Column<int>(type: "integer", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_user_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "personalized_feed_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(36)", nullable: true),
                    ItemType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: true),
                    DiscussionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewId = table.Column<Guid>(type: "uuid", nullable: true),
                    LearningPathId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelevanceScore = table.Column<double>(type: "double precision", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsViewed = table.Column<bool>(type: "boolean", nullable: false),
                    IsDismissed = table.Column<bool>(type: "boolean", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personalized_feed_items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "learning_path_courses",
                columns: table => new
                {
                    LearningPathId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_path_courses", x => new { x.LearningPathId, x.CourseId });
                    table.ForeignKey(
                        name: "FK_learning_path_courses_learning_paths_LearningPathId",
                        column: x => x.LearningPathId,
                        principalTable: "learning_paths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseDiscussions_AuthorId",
                table: "course_discussions",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseDiscussions_CourseId",
                table: "course_discussions",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseDiscussions_CourseId_ContentId",
                table: "course_discussions",
                columns: new[] { "CourseId", "ContentId" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseDiscussions_CourseId_IsPinned_LastActivityAt",
                table: "course_discussions",
                columns: new[] { "CourseId", "IsPinned", "LastActivityAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseLikes_CourseId",
                table: "course_likes",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseLikes_CourseId_UserId",
                table: "course_likes",
                columns: new[] { "CourseId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseLikes_UserId",
                table: "course_likes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseReviews_CourseId",
                table: "course_reviews",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseReviews_CourseId_IsApproved_IsFeatured",
                table: "course_reviews",
                columns: new[] { "CourseId", "IsApproved", "IsFeatured" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseReviews_CourseId_UserId",
                table: "course_reviews",
                columns: new[] { "CourseId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseReviews_UserId",
                table: "course_reviews",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseWishlists_CourseId_UserId",
                table: "course_wishlists",
                columns: new[] { "CourseId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseWishlists_UserId",
                table: "course_wishlists",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionReplies_AuthorId",
                table: "discussion_replies",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionReplies_DiscussionId",
                table: "discussion_replies",
                column: "DiscussionId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionReplies_DiscussionId_IsAcceptedAnswer",
                table: "discussion_replies",
                columns: new[] { "DiscussionId", "IsAcceptedAnswer" });

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionReplies_ParentReplyId",
                table: "discussion_replies",
                column: "ParentReplyId");

            migrationBuilder.CreateIndex(
                name: "IX_learning_certificate_templates_CourseId",
                table: "learning_certificate_templates",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_learning_certificate_templates_CourseId_IsDefault",
                table: "learning_certificate_templates",
                columns: new[] { "CourseId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_learning_certificate_templates_TenantId_IsActive",
                table: "learning_certificate_templates",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_learning_certificates_CertificateNumber",
                table: "learning_certificates",
                column: "CertificateNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_learning_certificates_CourseId",
                table: "learning_certificates",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_learning_certificates_EnrollmentId",
                table: "learning_certificates",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_learning_certificates_Status",
                table: "learning_certificates",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_learning_certificates_TemplateId",
                table: "learning_certificates",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_learning_certificates_UserId",
                table: "learning_certificates",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_learning_cohorts_CourseId",
                table: "learning_cohorts",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_learning_cohorts_CourseId_Status_IsOpen",
                table: "learning_cohorts",
                columns: new[] { "CourseId", "Status", "IsOpen" });

            migrationBuilder.CreateIndex(
                name: "IX_learning_cohorts_InstructorId",
                table: "learning_cohorts",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_learning_cohorts_TenantId",
                table: "learning_cohorts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_learning_course_collections_CuratorId",
                table: "learning_course_collections",
                column: "CuratorId");

            migrationBuilder.CreateIndex(
                name: "IX_learning_course_collections_TenantId_IsPublished_IsFeatured",
                table: "learning_course_collections",
                columns: new[] { "TenantId", "IsPublished", "IsFeatured" });

            migrationBuilder.CreateIndex(
                name: "IX_learning_course_collections_TenantId_Slug",
                table: "learning_course_collections",
                columns: new[] { "TenantId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_learning_course_recommendations_CourseId",
                table: "learning_course_recommendations",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_learning_course_recommendations_Type",
                table: "learning_course_recommendations",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_learning_course_recommendations_UserId_CourseId",
                table: "learning_course_recommendations",
                columns: new[] { "UserId", "CourseId" });

            migrationBuilder.CreateIndex(
                name: "IX_learning_course_recommendations_UserId_IsDismissed_ExpiresAt",
                table: "learning_course_recommendations",
                columns: new[] { "UserId", "IsDismissed", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_learning_featured_content_CourseId",
                table: "learning_featured_content",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_learning_featured_content_LearningPathId",
                table: "learning_featured_content",
                column: "LearningPathId");

            migrationBuilder.CreateIndex(
                name: "IX_learning_featured_content_TenantId_IsActive_DisplayOrder",
                table: "learning_featured_content",
                columns: new[] { "TenantId", "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_learning_featured_content_Type",
                table: "learning_featured_content",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_learning_path_courses_CourseId",
                table: "learning_path_courses",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_learning_path_courses_LearningPathId_SortOrder",
                table: "learning_path_courses",
                columns: new[] { "LearningPathId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_learning_path_enrollments_LearningPathId_UserId",
                table: "learning_path_enrollments",
                columns: new[] { "LearningPathId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_learning_path_enrollments_Status",
                table: "learning_path_enrollments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_learning_path_enrollments_UserId",
                table: "learning_path_enrollments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_learning_paths_CreatorId",
                table: "learning_paths",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_learning_paths_TenantId_IsPublished_IsFeatured",
                table: "learning_paths",
                columns: new[] { "TenantId", "IsPublished", "IsFeatured" });

            migrationBuilder.CreateIndex(
                name: "IX_learning_paths_TenantId_Slug",
                table: "learning_paths",
                columns: new[] { "TenantId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_learning_search_history_ClickedCourseId",
                table: "learning_search_history",
                column: "ClickedCourseId");

            migrationBuilder.CreateIndex(
                name: "IX_learning_search_history_Query",
                table: "learning_search_history",
                column: "Query");

            migrationBuilder.CreateIndex(
                name: "IX_learning_search_history_TenantId",
                table: "learning_search_history",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_learning_search_history_UserId",
                table: "learning_search_history",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_learning_user_profiles_LastActivityAt",
                table: "learning_user_profiles",
                column: "LastActivityAt");

            migrationBuilder.CreateIndex(
                name: "IX_learning_user_profiles_UserId",
                table: "learning_user_profiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PersonalizedFeedItems_ExpiresAt",
                table: "personalized_feed_items",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalizedFeedItems_UserId",
                table: "personalized_feed_items",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalizedFeedItems_UserId_IsDismissed_ExpiresAt",
                table: "personalized_feed_items",
                columns: new[] { "UserId", "IsDismissed", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalizedFeedItems_UserId_ItemType",
                table: "personalized_feed_items",
                columns: new[] { "UserId", "ItemType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "course_discussions");

            migrationBuilder.DropTable(
                name: "course_likes");

            migrationBuilder.DropTable(
                name: "course_reviews");

            migrationBuilder.DropTable(
                name: "course_wishlists");

            migrationBuilder.DropTable(
                name: "discussion_replies");

            migrationBuilder.DropTable(
                name: "learning_certificate_templates");

            migrationBuilder.DropTable(
                name: "learning_certificates");

            migrationBuilder.DropTable(
                name: "learning_cohorts");

            migrationBuilder.DropTable(
                name: "learning_course_collections");

            migrationBuilder.DropTable(
                name: "learning_course_recommendations");

            migrationBuilder.DropTable(
                name: "learning_featured_content");

            migrationBuilder.DropTable(
                name: "learning_path_courses");

            migrationBuilder.DropTable(
                name: "learning_path_enrollments");

            migrationBuilder.DropTable(
                name: "learning_search_history");

            migrationBuilder.DropTable(
                name: "learning_user_profiles");

            migrationBuilder.DropTable(
                name: "personalized_feed_items");

            migrationBuilder.DropTable(
                name: "learning_paths");
        }
    }
}
