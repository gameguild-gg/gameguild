using GameGuild.Modules.Contents;


namespace GameGuild.Modules.Products;

public class UpdateProductInput {
  public required Guid Id { get; set; }

  public string? Name { get; set; }

  public string? ShortDescription { get; set; }

  public string? Description { get; set; }

  public Common.ProductType? Type { get; set; }

  public bool? IsBundle { get; set; }

  public ContentStatus? Status { get; set; }

  public AccessLevel? Visibility { get; set; }
}
