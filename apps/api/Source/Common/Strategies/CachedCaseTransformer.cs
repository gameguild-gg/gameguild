using Microsoft.Extensions.Caching.Memory;


namespace GameGuild.Common;

/// <summary>
/// Base class for cached case transformers providing common caching functionality.
/// </summary>
public abstract class CachedCaseTransformer : ICaseTransformer {
  private static readonly object CacheLock = new();
  private static IMemoryCache? _sharedCache;

  /// <summary>
  /// Gets or creates the shared memory cache instance.
  /// </summary>
  protected static IMemoryCache Cache {
    get {
      if (_sharedCache != null) return _sharedCache;

      lock (CacheLock) {
        if (_sharedCache == null) {
          var cacheOptions = new MemoryCacheOptions {
            SizeLimit = 1000, // Maximum 1000 entries
            CompactionPercentage = 0.25, // Remove 25% of entries when limit is reached
          };

          _sharedCache = new MemoryCache(cacheOptions);
        }
      }

      return _sharedCache;
    }
  }

  /// <summary>
  /// Gets the cache key prefix for this transformer type.
  /// </summary>
  protected abstract string CacheKeyPrefix { get; }

  /// <summary>
  /// Performs the actual transformation logic without caching.
  /// </summary>
  /// <param name="input">The input string to transform.</param>
  /// <param name="options">Transformation options.</param>
  /// <returns>The transformed string.</returns>
  protected abstract string TransformCore(string input, CaseTransformOptions options);

  /// <summary>
  /// Transforms a string according to the specific case strategy.
  /// </summary>
  /// <param name="input">The input string to transform.</param>
  /// <returns>The transformed string.</returns>
  public string Transform(string input) { return Transform(input, CaseTransformOptions.Default); }

  /// <summary>
  /// Transforms a string with additional options.
  /// </summary>
  /// <param name="input">The input string to transform.</param>
  /// <param name="options">Additional transformation options.</param>
  /// <returns>The transformed string.</returns>
  public string Transform(string input, CaseTransformOptions options) {
    if (string.IsNullOrEmpty(input)) return string.Empty;

    options ??= CaseTransformOptions.Default;

    if (!options.UseCache) return TransformCore(input, options);

    var cacheKey = GenerateCacheKey(input, options);

    return Cache.GetOrCreate(
             cacheKey,
             entry => {
               entry.Size = 1; // Each entry counts as 1 towards the size limit
               entry.SlidingExpiration = TimeSpan.FromMinutes(30); // Expire after 30 minutes of inactivity
               entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2); // Absolute expiration after 2 hours

               return TransformCore(input, options);
             }
           ) ??
           string.Empty;
  }

  /// <summary>
  /// Validates if a string is already in the correct format for this case strategy.
  /// </summary>
  /// <param name="input">The string to validate.</param>
  /// <returns>True if the string is in the correct format, false otherwise.</returns>
  public abstract bool IsValidFormat(string input);

  /// <summary>
  /// Transforms multiple strings using the same options.
  /// </summary>
  /// <param name="inputs">The strings to transform.</param>
  /// <param name="options">Transformation options.</param>
  /// <returns>An array of transformed strings.</returns>
  public string[] TransformMany(string[] inputs, CaseTransformOptions? options = null) {
    if (inputs.Length == 0) return [];

    options ??= CaseTransformOptions.Default;
    var result = new string[inputs.Length];

    for (var i = 0; i < inputs.Length; i++) { result[i] = Transform(inputs[i], options); }

    return result;
  }

  /// <summary>
  /// Transforms a type name.
  /// </summary>
  /// <typeparam name="T">The type whose name to transform.</typeparam>
  /// <param name="options">Transformation options.</param>
  /// <returns>The transformed type name.</returns>
  public string TransformType<T>(CaseTransformOptions? options = null) { return Transform(typeof(T).Name, options ?? CaseTransformOptions.Default); }

  /// <summary>
  /// Transforms a type name.
  /// </summary>
  /// <param name="type">The type whose name to transform.</param>
  /// <param name="options">Transformation options.</param>
  /// <returns>The transformed type name.</returns>
  /// <exception cref="ArgumentNullException">Thrown when type is null.</exception>
  public string TransformType(Type type, CaseTransformOptions? options = null) {
    if (type == null) throw new ArgumentNullException(nameof(type));

    return Transform(type.Name, options ?? CaseTransformOptions.Default);
  }

  /// <summary>
  /// Generates a cache key for the given input and options.
  /// </summary>
  /// <param name="input">The input string.</param>
  /// <param name="options">The transformation options.</param>
  /// <returns>A unique cache key.</returns>
  protected virtual string GenerateCacheKey(string input, CaseTransformOptions options) { return $"{CacheKeyPrefix}:{input}:{options.MaxLength}:{options.CustomSeparator}:{options.ForceLowerCase}:{options.TrimWhitespace}"; }

  /// <summary>
  /// Clears the shared cache.
  /// </summary>
  public static void ClearCache() {
    lock (CacheLock) {
      _sharedCache?.Dispose();
      _sharedCache = null;
    }
  }

  /// <summary>
  /// Disposes the shared cache.
  /// </summary>
  public static void Dispose() {
    lock (CacheLock) {
      _sharedCache?.Dispose();
      _sharedCache = null;
    }
  }
}
