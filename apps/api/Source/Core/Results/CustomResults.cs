namespace GameGuild;

public static class CustomResults {
  public static Microsoft.AspNetCore.Http.IResult Problem(Result result) {
    if (result.IsSuccess) throw new InvalidOperationException();

    if (result.Error == null) throw new InvalidOperationException("Error cannot be null for failed result");

    return Results.Problem(title: GetTitle(result.Error), detail: GetDetail(result.Error), type: GetType(), statusCode: GetStatusCode());

    static string GetTitle(Error error) { return error.Code; }

    static string GetDetail(Error error) { return error.Message; }

    static string GetType() { return "https://tools.ietf.org/html/rfc7231#section-6.5.1"; }

    static int GetStatusCode() { return StatusCodes.Status400BadRequest; }
  }
}
