using MyApp.Application.Common;

namespace MyApp.Web.Endpoints;

public static class OutcomeResults
{
    /// <summary>Успех отдаёт endpoint сам (ok), отказ превращается в 404, 403 или 400.</summary>
    public static IResult ToResult<T>(this Outcome<T> outcome, Func<T, IResult> ok) => outcome.Status switch
    {
        OutcomeStatus.Ok => ok(outcome.Value!),
        OutcomeStatus.NotFound => Results.NotFound(),
        OutcomeStatus.Forbidden => Results.Problem(
            detail: outcome.Error ?? "Недостаточно прав.", statusCode: StatusCodes.Status403Forbidden),
        _ => Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["error"] = [outcome.Error ?? "Некорректный запрос."]
        })
    };
}
