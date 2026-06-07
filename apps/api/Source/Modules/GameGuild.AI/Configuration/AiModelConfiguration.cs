using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.AI;

public sealed class AiModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AiConversationLogConfiguration());
        modelBuilder.ApplyConfiguration(new AiPromptTemplateConfiguration());
    }
}

public sealed class AiConversationLogConfiguration : IEntityTypeConfiguration<AiConversationLog>
{
    public void Configure(EntityTypeBuilder<AiConversationLog> builder)
    {
        builder.ToTable("ai_conversation_logs");
        builder.HasKey(log => log.Id);

        builder.Property(log => log.Id).ValueGeneratedNever();
        builder.Property(log => log.RequestKind).HasMaxLength(16).IsRequired();
        builder.Property(log => log.Provider).HasMaxLength(32).IsRequired();
        builder.Property(log => log.Model).HasMaxLength(128).IsRequired();
        builder.Property(log => log.RequestText).HasColumnType("text").IsRequired();
        builder.Property(log => log.SystemPrompt).HasColumnType("text");
        builder.Property(log => log.ResponseText).HasColumnType("text");
        builder.Property(log => log.Outcome).HasMaxLength(32).IsRequired();
        builder.Property(log => log.OutcomeCode).HasMaxLength(64);
        builder.Property(log => log.OutcomeReason).HasMaxLength(512);
        builder.Property(log => log.FinishReason).HasMaxLength(64);

        builder.HasQueryFilter(log => log.DeletedAt == null);
        builder.HasIndex(log => new { log.TenantId, log.OccurredAt });
        builder.HasIndex(log => log.UserId);
        builder.HasIndex(log => log.Provider);
        builder.HasIndex(log => log.Outcome);
    }
}

public sealed class AiPromptTemplateConfiguration : IEntityTypeConfiguration<AiPromptTemplate>
{
    public void Configure(EntityTypeBuilder<AiPromptTemplate> builder)
    {
        builder.ToTable("ai_prompt_templates");
        builder.HasKey(template => template.Id);

        builder.Property(template => template.Id).ValueGeneratedNever();
        builder.Property(template => template.Key).HasMaxLength(128).IsRequired();
        builder.Property(template => template.Name).HasMaxLength(256).IsRequired();
        builder.Property(template => template.Description).HasMaxLength(1024);
        builder.Property(template => template.Category).HasMaxLength(128).IsRequired();
        builder.Property(template => template.SystemPrompt).HasColumnType("text");
        builder.Property(template => template.Prompt).HasColumnType("text").IsRequired();

        builder.HasQueryFilter(template => template.DeletedAt == null);
        builder.HasIndex(template => new { template.TenantId, template.Key });
        builder.HasIndex(template => template.Category);
        builder.HasIndex(template => template.IsActive);
    }
}
