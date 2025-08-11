namespace GameGuild.Common;

public static class MiddlewareExtensions {
  public static IApplicationBuilder UseRequestContextLogging(this IApplicationBuilder app) {
    app.UseMiddleware<RequestContextLoggingMiddleware>();

    return app;
  }

  // public static IApplicationBuilder UseExceptionHandler(this IApplicationBuilder app) {
  //   app.UseMiddleware<ExceptionHandlingMiddleware>();
  //
  //   return app;
  // }
}
