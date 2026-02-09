namespace GameGuild.Configuration.PresentationLayer.ModelValidation;

public sealed class ModelValidationOptions : BaseOptions
{
    /// <summary>
    ///     The configuration section name for this options type.
    /// </summary>
    public const string SectionName = "ModelValidation";

    public bool ReturnBadRequestOnFailure { get; set; } = true;

    public bool SuppressModelStateInvalidFilter { get; set; } = false;

    public static ModelValidationOptions CreateDefault() { return new ModelValidationOptions(); }
}
