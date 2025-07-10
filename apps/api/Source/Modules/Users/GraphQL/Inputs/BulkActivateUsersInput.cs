using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.Users.Inputs;

public class BulkActivateUsersInput {
  [Required] public List<Guid> UserIds { get; set; } = new();

  public string? Reason { get; set; }
}
