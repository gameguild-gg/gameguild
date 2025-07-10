using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.Users.Inputs;

public class BulkCreateUsersInput {
  [Required] public List<CreateUserInput> Users { get; set; } = new();

  public string? Reason { get; set; }
}
