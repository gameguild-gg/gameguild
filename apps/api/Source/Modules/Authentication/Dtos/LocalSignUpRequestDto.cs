using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.Authentication.Dtos {
  public class LocalSignUpRequestDto {
    public string? Username { get; set; }

    [Required] [EmailAddress] public string Email { get; set; } = string.Empty;

    [Required] public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Optional tenant ID to use for the sign-up
    /// If not provided, a default tenant may be assigned
    /// </summary>
    public Guid? TenantId { get; set; }
  }
}
