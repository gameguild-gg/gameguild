using System.ComponentModel.DataAnnotations;
using MediatR;


namespace GameGuild.Modules.Users;

/// <summary>
/// Command to update user balance
/// </summary>
public class UpdateUserBalanceCommand : IRequest<User>
{
  [Required]
  public Guid UserId { get; set; }

  [Range(0, double.MaxValue)]
  public decimal Balance { get; set; }

  [Range(0, double.MaxValue)]
  public decimal AvailableBalance { get; set; }

  public string? Reason { get; set; }
}
