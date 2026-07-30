namespace GameGuild.Configuration.PresentationLayer.ApiVersioning;

/// <summary>
///     Enumeration of API version reading strategies usable by ApiVersioningOptions
/// </summary>
public enum ApiVersionReadingStrategy
{
    UrlSegment,

    QueryString,

    Header,

    MediaType,

    UrlSegmentAndQueryString,

    UrlSegmentAndHeader,

    All
}
