using GameGuild.Modules.Contents.Models;
using ProductTypeEnum = GameGuild.ProductType;


using ProductEntity = GameGuild.Modules.Products.Models.Product;
namespace GameGuild.Modules.Products.GraphQL;

public class UpdateProductInput {
  public required Guid Id { get; set; }

  public string? Name { get; set; }

  public string? ShortDescription { get; set; }

  public string? Description { get; set; }

  public GameGuild.ProductType? Type { get; set; }

  public bool? IsBundle { get; set; }

  public ContentStatus? Status { get; set; }

  public AccessLevel? Visibility { get; set; }
}
