namespace GameGuild.CQRS;

/// <summary> Validation context </summary>
/// <typeparam name="T"> Type being validated </typeparam>
public class ValidationContext<T> {
  /// <summary> Initializes a new instance of the ValidationContext class </summary>
  /// <param name="instance"> Instance to validate </param>
  public ValidationContext(T instance) { Instance = instance; }

  /// <summary> The instance being validated </summary>
  public T Instance { get; }
}
