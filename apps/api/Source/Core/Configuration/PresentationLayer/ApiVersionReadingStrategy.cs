namespace GameGuild;

/// <summary> Enumeration of API version reading strategies. </summary>
public enum ApiVersionReadingStrategy {
  /// <summary> Read version from URL segment only (e.g., /v1/users). </summary>
  UrlSegment,

  /// <summary> Read version from query string only (e.g., ?version=1.0). </summary>
  QueryString,

  /// <summary> Read version from header only (e.g., X-Version: 1.0). </summary>
  Header,

  /// <summary> Read version from media type only (e.g., Accept: application/json;ver=1.0). </summary>
  MediaType,

  /// <summary> Read version from URL segment and query string. </summary>
  UrlSegmentAndQueryString,

  /// <summary> Read version from URL segment and header. </summary>
  UrlSegmentAndHeader,

  /// <summary> Read version from all sources (URL segment, query string, header, media type). </summary>
  All,
}
