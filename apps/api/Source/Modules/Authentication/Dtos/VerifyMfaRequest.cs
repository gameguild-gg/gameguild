namespace GameGuild.Modules.Authentication.Controllers;

public class VerifyMfaRequest
{
    public Guid UserId { get; set; }

    public string Code { get; set; } = string.Empty;

    public MfaMethod Method { get; set; } = MfaMethod.Totp;
}
