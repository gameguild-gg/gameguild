using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.Users.Inputs;

public class CreateUserInput {
  [Required] [StringLength(100)] public string Name { get; set; } = string.Empty;

  [Required]
  [EmailAddress]
  [StringLength(255)]
  public string Email { get; set; } = string.Empty;

  public bool IsActive { get; set; } = true;
}
