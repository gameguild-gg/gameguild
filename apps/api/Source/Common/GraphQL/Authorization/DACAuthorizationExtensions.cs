namespace GameGuild.Common.Authorization;

/// <summary>
/// Extension methods for applying DAC authorization middleware
/// </summary>
public static class DACAuthorizationExtensions {
  /// <summary>
  /// Adds DAC authorization middleware to a field
  /// </summary>
  public static IObjectFieldDescriptor UseDACAuthorization(this IObjectFieldDescriptor descriptor) { return descriptor.Use<DACAuthorizationMiddleware>(); }
}
