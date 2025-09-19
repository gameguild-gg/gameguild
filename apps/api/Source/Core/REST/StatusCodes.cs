namespace GameGuild.Core.REST;

/// <summary>
/// HTTP status code guidelines and semantic meanings
/// </summary>
public static class HttpStatusCodeGuide {
    /// <summary>
    /// 2xx Success - The action was successfully received, understood, and accepted
    /// </summary>
    public static class Success {
        /// <summary>200 OK - Standard response for successful HTTP requests</summary>
        public const int Ok = 200;

        /// <summary>201 Created - Request fulfilled, new resource created</summary>
        public const int Created = 201;

        /// <summary>202 Accepted - Request accepted for processing but not completed</summary>
        public const int Accepted = 202;

        /// <summary>204 No Content - Request processed successfully, no content returned</summary>
        public const int NoContent = 204;
    }

    /// <summary>
    /// 3xx Redirection - Further action must be taken to complete the request
    /// </summary>
    public static class Redirection {
        /// <summary>304 Not Modified - Resource has not been modified since last request</summary>
        public const int NotModified = 304;
    }

    /// <summary>
    /// 4xx Client Error - Request contains bad syntax or cannot be fulfilled
    /// </summary>
    public static class ClientError {
        /// <summary>400 Bad Request - Server cannot process request due to client error</summary>
        public const int BadRequest = 400;

        /// <summary>401 Unauthorized - Authentication required</summary>
        public const int Unauthorized = 401;

        /// <summary>403 Forbidden - Server understood request but refuses to authorize</summary>
        public const int Forbidden = 403;

        /// <summary>404 Not Found - Requested resource not found</summary>
        public const int NotFound = 404;

        /// <summary>405 Method Not Allowed - Request method not supported for resource</summary>
        public const int MethodNotAllowed = 405;

        /// <summary>409 Conflict - Request conflicts with current state of resource</summary>
        public const int Conflict = 409;

        /// <summary>412 Precondition Failed - One or more preconditions failed (e.g., ETag mismatch)</summary>
        public const int PreconditionFailed = 412;

        /// <summary>422 Unprocessable Entity - Request well-formed but semantically incorrect</summary>
        public const int UnprocessableEntity = 422;

        /// <summary>429 Too Many Requests - Rate limit exceeded</summary>
        public const int TooManyRequests = 429;
    }

    /// <summary>
    /// 5xx Server Error - Server failed to fulfill apparently valid request
    /// </summary>
    public static class ServerError {
        /// <summary>500 Internal Server Error - Generic server error</summary>
        public const int InternalServerError = 500;

        /// <summary>501 Not Implemented - Server does not support functionality required</summary>
        public const int NotImplemented = 501;

        /// <summary>502 Bad Gateway - Invalid response from upstream server</summary>
        public const int BadGateway = 502;

        /// <summary>503 Service Unavailable - Server temporarily unavailable</summary>
        public const int ServiceUnavailable = 503;

        /// <summary>504 Gateway Timeout - Upstream server failed to respond in time</summary>
        public const int GatewayTimeout = 504;
    }
}

/// <summary>
/// Helper methods for common REST result patterns
/// </summary>
public static class RestResults {
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
