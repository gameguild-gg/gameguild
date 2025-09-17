namespace GameGuild.CQRS;

/// <summary> State for request exception handling </summary>
public enum RequestExceptionHandlerState {
  /// <summary> Continue to next exception handler </summary>
  Continue,

  /// <summary> Stop processing and return response </summary>
  Handled,
}
