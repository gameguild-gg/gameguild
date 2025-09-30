namespace GameGuild.Core.Behaviors;

/// <summary>
/// Interface for error information
/// </summary>
public interface IError
{
    string Code { get; }

    string Message { get; }
}
