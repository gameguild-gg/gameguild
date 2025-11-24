namespace GameGuild.SharedKernel.Configuration;

public class OpenApiServerVariableOptions : BaseOptions
{
    public string Default { get; set; } = string.Empty;

    public string[ ] Enum { get; set; } = Array.Empty<string>();

    public static OpenApiServerVariableOptions CreateDefault() { return new OpenApiServerVariableOptions(); }
}
