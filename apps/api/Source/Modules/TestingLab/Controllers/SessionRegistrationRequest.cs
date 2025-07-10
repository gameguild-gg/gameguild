using GameGuild.Modules.TestingLab.Models;


namespace GameGuild.Modules.TestingLab.Controllers;

public class SessionRegistrationRequest {
  public RegistrationType RegistrationType { get; set; }

  public string? Notes { get; set; }
}
