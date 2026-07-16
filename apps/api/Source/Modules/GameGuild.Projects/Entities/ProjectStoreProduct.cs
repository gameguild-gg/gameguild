using GameGuild.Commerce.Products;

namespace GameGuild.Projects;

public sealed class ProjectStoreProduct : EntityBase
{
    public Guid ProjectId { get; set; }

    public Guid ProductId { get; set; }

    public Project Project { get; set; } = null!;

    public Product Product { get; set; } = null!;
}
