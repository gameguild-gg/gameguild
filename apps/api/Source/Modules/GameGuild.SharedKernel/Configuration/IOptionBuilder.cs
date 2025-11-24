using Microsoft.Extensions.Configuration;

namespace GameGuild.SharedKernel.Configuration;

/// <summary>
///     Contract interface for option builders that create strongly-typed configuration objects from IConfiguration.
///     This interface serves as documentation for the expected method signatures that all option builders should
///     implement.
///     Note: This interface is not meant to be implemented directly by static classes, but serves as a contract
///     specification.
/// </summary>
/// <typeparam name="T">The type of options to build</typeparam>
public interface IOptionBuilder<out T> where T : class, new()
{
  /// <summary>
  ///     Creates an instance of the options type with default values.
  ///     All static option builders should implement: public static T CreateDefault()
  /// </summary>
  /// <returns>A new instance with default configuration</returns>
  T CreateDefault();

  /// <summary>
  ///     Creates an instance of the options type from configuration.
  ///     All static option builders should implement: public static T Create(IConfiguration configuration)
  /// </summary>
  /// <param name="configuration">The configuration to bind from</param>
  /// <returns>A configured instance of the options</returns>
  T Create(IConfiguration configuration);

  /// <summary>
  ///     Creates an instance of the options type from a specific configuration section.
  ///     All static option builders should implement: public static T Create(IConfiguration configuration, string
  ///     sectionName)
  /// </summary>
  /// <param name="configuration">The configuration to bind from</param>
  /// <param name="sectionName">The name of the configuration section to bind</param>
  /// <returns>A configured instance of the options</returns>
  T Create(IConfiguration configuration, string sectionName);
}
