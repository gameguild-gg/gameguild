namespace GameGuild.Core.Behaviors;

/// <summary>
/// Interface to check if a result indicates success
/// </summary>
public interface IResult
{
    bool IsSuccess { get; }

    IError? Error { get; }
}
