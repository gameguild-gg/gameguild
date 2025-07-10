using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.Users.Inputs;

public class UpdateUserInput {
  [Required] public Guid Id { get; set; }

  [StringLength(100)] public string? Name { get; set; }

  [EmailAddress] [StringLength(255)] public string? Email { get; set; }

  public bool? IsActive { get; set; }
}
