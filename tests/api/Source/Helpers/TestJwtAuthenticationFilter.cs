using GameGuild.Modules.Auth;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;

namespace GameGuild.API.Tests.Helpers {
  /// <summary>
  /// A test implementation of JwtAuthenticationFilter that bypasses authentication for tests
  /// </summary>
  public class TestJwtAuthenticationFilter(IConfiguration configuration) : JwtAuthenticationFilter(configuration) {
    public override void OnAuthorization(AuthorizationFilterContext context) {
      // Do nothing - bypass all authorization checks for tests
    }
  }
}
