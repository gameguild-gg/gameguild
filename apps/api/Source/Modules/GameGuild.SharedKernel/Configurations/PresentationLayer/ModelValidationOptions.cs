namespace GameGuild.SharedKernel.Configuration;

public class ModelValidationOptions : BaseOptions
{
    public bool ReturnBadRequestOnFailure { get; set; } = true;

    public bool SuppressModelStateInvalidFilter { get; set; } = false;

    public override void Validate() { base.Validate(); }

    public static ModelValidationOptions CreateDefault() { return new ModelValidationOptions(); }
}
