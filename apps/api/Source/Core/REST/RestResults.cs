namespace GameGuild.Core.REST;

/// <summary>
/// Helper methods for common REST result patterns
/// </summary>
public static class RestResults
{
    /// <summary>Standard 200 OK response</summary>
    public static int Ok => HttpStatusCodeGuide.Success.Ok;

    /// <summary>Standard 201 Created response</summary>
    public static int Created => HttpStatusCodeGuide.Success.Created;

    /// <summary>Standard 204 No Content response</summary>
    public static int NoContent => HttpStatusCodeGuide.Success.NoContent;

    /// <summary>Standard 400 Bad Request response</summary>
    public static int BadRequest => HttpStatusCodeGuide.ClientError.BadRequest;

    /// <summary>Standard 401 Unauthorized response</summary>
    public static int Unauthorized => HttpStatusCodeGuide.ClientError.Unauthorized;

    /// <summary>Standard 404 Not Found response</summary>
    public static int NotFound => HttpStatusCodeGuide.ClientError.NotFound;

    /// <summary>Standard 409 Conflict response</summary>
    public static int Conflict => HttpStatusCodeGuide.ClientError.Conflict;

    /// <summary>Standard 500 Internal Server Error response</summary>
    public static int InternalServerError => HttpStatusCodeGuide.ServerError.InternalServerError;
}
