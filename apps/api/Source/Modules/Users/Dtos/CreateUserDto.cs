using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.Users;

public class CreateUserDto {
  [Required]
  [StringLength(100, MinimumLength = 1)]
  public string Name { get; set; } = string.Empty;

  [Required]
  [EmailAddress]
  [StringLength(255)]
  public string Email { get; set; } = string.Empty;

  public bool IsActive { get; set; } = true;

  [Range(0, double.MaxValue)] public decimal InitialBalance { get; set; } = 0;
}
