using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.Users.Inputs;

public class BulkDeleteUsersInput 
{
  [Required]
  public List<Guid> UserIds { get; set; } = new();
    
  public bool SoftDelete { get; set; } = true;
  public string? Reason { get; set; }
}