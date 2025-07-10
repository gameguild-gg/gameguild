namespace GameGuild.Common;

/// <summary>
/// Interface for case transformation strategies.
/// </summary>
public interface ICaseTransformer
{
    /// <summary>
    /// Transforms a string according to the specific case strategy.
    /// </summary>
    /// <param name="input">The input string to transform.</param>
    /// <returns>The transformed string.</returns>
    string Transform(string input);

    /// <summary>
    /// Transforms a string with additional options.
    /// </summary>
    /// <param name="input">The input string to transform.</param>
    /// <param name="options">Additional transformation options.</param>
    /// <returns>The transformed string.</returns>
    string Transform(string input, CaseTransformOptions options);

    /// <summary>
    /// Validates if a string is already in the correct format for this case strategy.
    /// </summary>
    /// <param name="input">The string to validate.</param>
    /// <returns>True if the string is in the correct format, false otherwise.</returns>
    bool IsValidFormat(string input);
}
