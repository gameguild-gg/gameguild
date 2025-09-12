namespace GameGuild.CQRS;

/// <summary>
/// Represents a void type, since Void is not a valid return type in C#.
/// </summary>
public struct Unit : IEquatable<Unit>, IComparable<Unit>, IComparable {
  /// <summary>
  /// Default and only value of the Unit type.
  /// </summary>
  public static readonly Unit Value = new Unit();

  /// <summary>
  /// Task from a Unit type.
  /// </summary>
  public static readonly Task<Unit> Task = System.Threading.Tasks.Task.FromResult(Value);

  /// <summary>
  /// Compares the current Unit to the provided Unit.
  /// </summary>
  /// <param name="other">The Unit to compare</param>
  /// <returns>0</returns>
  public int CompareTo(Unit other) { return 0; }

  /// <summary>
  /// Compares the current Unit to the provided object.
  /// </summary>
  /// <param name="obj">The object to compare</param>
  /// <returns>0 if object is Unit, throws ArgumentException otherwise</returns>
  /// <exception cref="ArgumentException">Object is not a Unit</exception>
  public int CompareTo(object? obj) {
    if (obj is null) return 1;

    return obj is Unit ? 0 : throw new ArgumentException($"Object must be of type {nameof(Unit)}");
  }

  /// <summary>
  /// Compares the current Unit to the provided Unit.
  /// </summary>
  /// <param name="other">The Unit to compare</param>
  /// <returns>true</returns>
  public readonly bool Equals(Unit other) { return true; }

  /// <summary>
  /// Compares the current Unit to the provided object.
  /// </summary>
  /// <param name="obj">The object to compare</param>
  /// <returns>true if object is Unit, false otherwise</returns>
  public readonly override bool Equals(object? obj) { return obj is Unit; }

  /// <summary>
  /// Returns the hash code for this Unit.
  /// </summary>
  /// <returns>0</returns>
  public override int GetHashCode() { return 0; }

  /// <summary>
  /// Returns a string representation of the Unit.
  /// </summary>
  /// <returns>"()"</returns>
  public override string ToString() { return "()"; }

  /// <summary>
  /// Compares two Unit values.
  /// </summary>
  /// <param name="left">Left Unit</param>
  /// <param name="right">Right Unit</param>
  /// <returns>true</returns>
  public static bool operator ==(Unit left, Unit right) { return true; }

  /// <summary>
  /// Compares two Unit values.
  /// </summary>
  /// <param name="left">Left Unit</param>
  /// <param name="right">Right Unit</param>
  /// <returns>false</returns>
  public static bool operator !=(Unit left, Unit right) { return false; }
}
