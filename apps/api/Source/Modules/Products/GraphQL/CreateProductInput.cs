namespace GameGuild.Source.Modules.Products.GraphQL;

public class CreateProductInput {
  public required string Name { get; set; }

  public string? ShortDescription { get; set; }

  public required GameGuild.ProductType Type { get; set; }

  public bool IsBundle { get; set; } = false;

  public Guid? TenantId { get; set; }
}
