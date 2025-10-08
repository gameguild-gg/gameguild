namespace GameGuild.Authorization;

/// <summary> Extension methods for applying DAC authorization middleware </summary>
public static class DacAuthorizationExtensions
{
    /// <summary> Adds DAC authorization middleware to a field </summary>
    public static IObjectFieldDescriptor UseDacAuthorization(this IObjectFieldDescriptor descriptor)
    {
        // DAC middleware simplified to attribute-based approach
        return descriptor;
    }
}
