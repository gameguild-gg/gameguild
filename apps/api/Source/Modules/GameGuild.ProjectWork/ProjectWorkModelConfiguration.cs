using GameGuild.Projects;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.ProjectWork;

public sealed class ProjectWorkModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProjectBoard>(builder =>
        {
            builder.ToTable("project_work_boards");
            builder.HasKey(board => board.Id);
            builder.Property(board => board.Name).IsRequired().HasMaxLength(100);
            builder.HasOne<Project>().WithMany().HasForeignKey(board => board.ProjectId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(board => board.ProjectId).IsUnique();
        });
        modelBuilder.Entity<ProjectWorkColumn>(builder =>
        {
            builder.ToTable("project_work_columns");
            builder.HasKey(column => column.Id);
            builder.Property(column => column.Kind).HasConversion<string>().HasMaxLength(30);
            builder.Property(column => column.Name).IsRequired().HasMaxLength(100);
            builder.HasOne(column => column.Board).WithMany(board => board.Columns).HasForeignKey(column => column.BoardId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(column => new { column.BoardId, column.Position }).IsUnique();
        });
        modelBuilder.Entity<ProjectWorkTask>(builder =>
        {
            builder.ToTable("project_work_tasks");
            builder.HasKey(task => task.Id);
            builder.Property(task => task.Title).IsRequired().HasMaxLength(300);
            builder.Property(task => task.Description).HasMaxLength(10000);
            builder.Property(task => task.Status).HasConversion<string>().HasMaxLength(30);
            builder.Property(task => task.Priority).HasConversion<string>().HasMaxLength(30);
            builder.HasOne<Project>().WithMany().HasForeignKey(task => task.ProjectId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(task => task.Column).WithMany(column => column.Tasks).HasForeignKey(task => task.ColumnId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(task => task.Milestone).WithMany(milestone => milestone.Tasks).HasForeignKey(task => task.MilestoneId).OnDelete(DeleteBehavior.SetNull);
            builder.HasOne(task => task.AssigneeUser).WithMany().HasForeignKey(task => task.AssigneeUserId).OnDelete(DeleteBehavior.SetNull);
            builder.HasIndex(task => new { task.ProjectId, task.ColumnId, task.Position });
            builder.HasIndex(task => task.AssigneeUserId);
        });
        modelBuilder.Entity<ProjectMilestone>(builder =>
        {
            builder.ToTable("project_milestones");
            builder.HasKey(milestone => milestone.Id);
            builder.Property(milestone => milestone.Name).IsRequired().HasMaxLength(200);
            builder.Property(milestone => milestone.Description).HasMaxLength(2000);
            builder.HasOne<Project>().WithMany().HasForeignKey(milestone => milestone.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ProjectTaskChecklistItem>(builder =>
        {
            builder.ToTable("project_task_checklist_items");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Text).IsRequired().HasMaxLength(500);
            builder.HasOne(item => item.Task).WithMany(task => task.Checklist).HasForeignKey(item => item.TaskId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ProjectTaskComment>(builder =>
        {
            builder.ToTable("project_task_comments");
            builder.HasKey(comment => comment.Id);
            builder.Property(comment => comment.Body).IsRequired().HasMaxLength(10000);
            builder.HasOne(comment => comment.Task).WithMany(task => task.Comments).HasForeignKey(comment => comment.TaskId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ProjectTaskDependency>(builder =>
        {
            builder.ToTable("project_task_dependencies");
            builder.HasKey(edge => edge.Id);
            builder.HasOne<ProjectWorkTask>().WithMany().HasForeignKey(edge => edge.TaskId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<ProjectWorkTask>().WithMany().HasForeignKey(edge => edge.DependsOnTaskId).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(edge => new { edge.TaskId, edge.DependsOnTaskId }).IsUnique();
        });
        modelBuilder.Entity<ProjectTaskLabel>(builder =>
        {
            builder.ToTable("project_task_labels");
            builder.HasKey(label => label.Id);
            builder.Property(label => label.Name).IsRequired().HasMaxLength(80);
            builder.Property(label => label.Color).IsRequired().HasMaxLength(20);
            builder.HasOne<Project>().WithMany().HasForeignKey(label => label.ProjectId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(label => new { label.ProjectId, label.Name }).IsUnique();
        });
        modelBuilder.Entity<ProjectTaskLabelAssignment>(builder =>
        {
            builder.ToTable("project_task_label_assignments");
            builder.HasKey(link => link.Id);
            builder.HasOne<ProjectWorkTask>().WithMany().HasForeignKey(link => link.TaskId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<ProjectTaskLabel>().WithMany().HasForeignKey(link => link.LabelId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(link => new { link.TaskId, link.LabelId }).IsUnique();
        });
        modelBuilder.Entity<ProjectWorkHistory>(builder =>
        {
            builder.ToTable("project_work_history");
            builder.HasKey(history => history.Id);
            builder.Property(history => history.Action).IsRequired().HasMaxLength(100);
            builder.Property(history => history.ChangesJson).HasMaxLength(10000);
            builder.HasOne<Project>().WithMany().HasForeignKey(history => history.ProjectId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(history => new { history.ProjectId, history.CreatedAt });
        });
    }
}
