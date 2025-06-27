using Microsoft.AspNetCore.Mvc.Filters;
using GameGuild.Modules.Auth.Filters;


namespace GameGuild.Tests.Helpers {
  /// <summary>
  /// A test implementation of JwtAuthenticationFilter that bypasses authentication for tests
  /// </summary>
  public class TestJwtAuthenticationFilter(IConfiguration configuration) : JwtAuthenticationFilter(configuration) {
    public override void OnAuthorization(AuthorizationFilterContext context) {
      // Do nothing - bypass all authorization checks for tests
    }
  }
}
