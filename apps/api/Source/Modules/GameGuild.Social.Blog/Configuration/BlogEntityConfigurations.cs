using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Social.Blog.Configuration;

/// <summary>
/// EF Core configuration for BlogPost.
/// </summary>
public sealed class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
{
    public void Configure(EntityTypeBuilder<BlogPost> builder)
    {
        builder.ToTable("social_blog_posts");
        builder.HasKey(post => post.Id);
        builder.Property(post => post.PrimaryAuthorId).IsRequired();
        builder.Property(post => post.Title).HasMaxLength(200).IsRequired();
        builder.Property(post => post.Slug).HasMaxLength(220).IsRequired();
        builder.Property(post => post.Excerpt).HasMaxLength(500);
        builder.Property(post => post.Content).HasColumnType("text").IsRequired();
        builder.Property(post => post.JsonBody).HasColumnType("jsonb");
        builder.Property(post => post.Format).HasConversion<string>().HasMaxLength(40);
        builder.Property(post => post.MetaTitle).HasMaxLength(200);
        builder.Property(post => post.MetaDescription).HasMaxLength(300);
        builder.Property(post => post.OgImageUrl).HasMaxLength(1000);
        builder.Property(post => post.CanonicalUrlOverride).HasMaxLength(1000);
        builder.Property(post => post.TwitterCard).HasMaxLength(40).IsRequired();
        builder.Property(post => post.StructuredDataOverride).HasColumnType("text");
        builder.Property(post => post.Status).HasConversion<string>().HasMaxLength(40);
        builder.Property(post => post.Revision);
        builder.Ignore(post => post.Tags);
        builder.Property(post => post.TagsJson).HasColumnName("Tags").HasColumnType("jsonb").IsRequired();

        // Slug uniqueness is per primary author — no global slug index.
        builder.HasIndex(post => new { post.PrimaryAuthorId, post.Slug }).IsUnique();
        builder.HasIndex(post => new { post.PrimaryAuthorId, post.Status, post.PublishedAt });
        builder.HasIndex(post => new { post.Status, post.PublishedAt });
    }
}

/// <summary>
/// EF Core configuration for BlogPostAuthor (co-authors).
/// </summary>
public sealed class BlogPostAuthorConfiguration : IEntityTypeConfiguration<BlogPostAuthor>
{
    public void Configure(EntityTypeBuilder<BlogPostAuthor> builder)
    {
        builder.ToTable("social_blog_post_authors");
        builder.HasKey(author => author.Id);
        builder.Property(author => author.UserId).IsRequired();
        builder.HasIndex(author => new { author.BlogPostId, author.UserId }).IsUnique();
        builder.HasIndex(author => author.UserId);
    }
}

/// <summary>
/// EF core configuration for BlogSlugHistory.
/// </summary>
public sealed class BlogSlugHistoryConfiguration : IEntityTypeConfiguration<BlogSlugHistory>
{
    public void Configure(EntityTypeBuilder<BlogSlugHistory> builder)
    {
        builder.ToTable("social_blog_slug_history");
        builder.HasKey(history => history.Id);
        builder.Property(history => history.PreviousSlug).HasMaxLength(220).IsRequired();
        builder.HasIndex(history => new { history.PreviousPrimaryAuthorId, history.PreviousSlug }, "IX_social_blog_slug_history_PreviousPrimaryAuthorId_PreviousSlug").IsUnique();
        builder.HasIndex(history => history.BlogPostId);
    }
}

/// <summary>
/// EF Core configuration for BlogComment.
/// </summary>
public sealed class BlogCommentConfiguration : IEntityTypeConfiguration<BlogComment>
{
    public void Configure(EntityTypeBuilder<BlogComment> builder)
    {
        builder.ToTable("social_blog_comments");
        builder.HasKey(comment => comment.Id);
        builder.Property(comment => comment.Content).HasMaxLength(2000).IsRequired();
        builder.HasIndex(comment => new { comment.BlogPostId, comment.CreatedAt });
        builder.HasIndex(comment => comment.ParentCommentId);
    }
}

/// <summary>
/// EF Core configuration for BlogAiProposal.
/// </summary>
public sealed class BlogAiProposalConfiguration : IEntityTypeConfiguration<BlogAiProposal>
{
    public void Configure(EntityTypeBuilder<BlogAiProposal> builder)
    {
        builder.ToTable("social_blog_ai_proposals");
        builder.Property(proposal => proposal.OriginalContent).HasColumnType("text");
        builder.Property(proposal => proposal.ProposedContent).HasColumnType("text").IsRequired();
        builder.HasIndex(proposal => proposal.RunId).IsUnique();
        builder.HasIndex(proposal => new { proposal.BlogPostId, proposal.Status });
    }
}

/// <summary>
/// EF Core configuration for BlogAiConversation.
/// </summary>
public sealed class BlogAiConversationConfiguration : IEntityTypeConfiguration<BlogAiConversation>
{
    public void Configure(EntityTypeBuilder<BlogAiConversation> builder)
    {
        builder.ToTable("social_blog_ai_conversations");
        builder.HasIndex(conversation => new { conversation.TenantId, conversation.BlogPostId, conversation.AuthorId }).IsUnique();
        builder.HasIndex(conversation => new { conversation.AuthorId, conversation.LastMessageAt });
    }
}

/// <summary>
/// EF Core configuration for BlogAiMessage.
/// </summary>
public sealed class BlogAiMessageConfiguration : IEntityTypeConfiguration<BlogAiMessage>
{
    public void Configure(EntityTypeBuilder<BlogAiMessage> builder)
    {
        builder.ToTable("social_blog_ai_messages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).ValueGeneratedNever();
        builder.Property(message => message.Role).HasMaxLength(16).IsRequired();
        builder.Property(message => message.Content).HasColumnType("text").IsRequired();
        builder.HasIndex(message => new { message.ConversationId, message.CreatedAt });
        builder.HasIndex(message => message.RunId);
    }
}

/// <summary>
/// EF Core configuration for BlogAiRun.
/// </summary>
public sealed class BlogAiRunConfiguration : IEntityTypeConfiguration<BlogAiRun>
{
    public void Configure(EntityTypeBuilder<BlogAiRun> builder)
    {
        builder.ToTable("social_blog_ai_runs");
        builder.Property(run => run.Instruction).HasColumnType("text").IsRequired();
        builder.Property(run => run.Selection).HasColumnType("text");
        builder.Property(run => run.ResponseText).HasColumnType("text");
        builder.Property(run => run.ErrorMessage).HasColumnType("text");
        builder.Property(run => run.IdempotencyKey).HasMaxLength(128).IsRequired();
        builder.Property(run => run.Provider).HasMaxLength(64);
        builder.Property(run => run.Model).HasMaxLength(256);
        builder.Property(run => run.ErrorCode).HasMaxLength(128);
        builder.HasIndex(run => new { run.TenantId, run.ActorId, run.IdempotencyKey }).IsUnique();
        builder.HasIndex(run => new { run.BlogPostId, run.ActorId, run.CreatedAt });
        builder.HasIndex(run => run.Status);
    }
}

/// <summary>
/// EF Core configuration for BlogAiStreamEvent.
/// </summary>
public sealed class BlogAiStreamEventConfiguration : IEntityTypeConfiguration<BlogAiStreamEvent>
{
    public void Configure(EntityTypeBuilder<BlogAiStreamEvent> builder)
    {
        builder.ToTable("social_blog_ai_stream_events");
        builder.HasKey(streamEvent => streamEvent.Id);
        builder.Property(streamEvent => streamEvent.Id).ValueGeneratedNever();
        builder.Property(streamEvent => streamEvent.Type).HasMaxLength(64).IsRequired();
        builder.Property(streamEvent => streamEvent.Status).HasMaxLength(32).IsRequired();
        builder.Property(streamEvent => streamEvent.Delta).HasColumnType("text");
        builder.Property(streamEvent => streamEvent.PayloadJson).HasColumnType("jsonb");
        builder.HasIndex(streamEvent => new { streamEvent.RunId, streamEvent.Sequence }).IsUnique();
    }
}

/// <summary>
/// Registers Social.Blog entities in the composed application model.
/// </summary>
public sealed class BlogModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new BlogPostConfiguration());
        modelBuilder.ApplyConfiguration(new BlogPostAuthorConfiguration());
        modelBuilder.ApplyConfiguration(new BlogSlugHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new BlogCommentConfiguration());
        modelBuilder.ApplyConfiguration(new BlogAiConversationConfiguration());
        modelBuilder.ApplyConfiguration(new BlogAiRunConfiguration());
        modelBuilder.ApplyConfiguration(new BlogAiMessageConfiguration());
        modelBuilder.ApplyConfiguration(new BlogAiProposalConfiguration());
        modelBuilder.ApplyConfiguration(new BlogAiStreamEventConfiguration());
    }
}
