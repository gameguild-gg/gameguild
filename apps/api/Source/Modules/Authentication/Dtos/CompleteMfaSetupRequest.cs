namespace GameGuild.Modules.Authentication.Controllers;

public class CompleteMfaSetupRequest
{
    public string SetupId { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;
}
