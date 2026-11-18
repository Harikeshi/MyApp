namespace MyApp.Web.Services;

/// <summary>
/// Вызывает DELETE /api/account/data в AiChat.Api с тем же токеном, с каким пришёл запрос:
/// AiChat сам определяет пользователя по токену, поэтому чужие данные так удалить нельзя.
/// </summary>
public sealed class AiChatDataCleaner(
    HttpClient http,
    IHttpContextAccessor accessor,
    ILogger<AiChatDataCleaner> log) : IChatDataCleaner
{
    public async Task<bool> DeleteAllAsync(CancellationToken ct)
    {
        var authorization = accessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authorization))
            return false;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, "api/account/data");
            request.Headers.TryAddWithoutValidation("Authorization", authorization);

            using var response = await http.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
                return true;

            log.LogError("AiChat ответил {Status} на удаление данных пользователя", (int)response.StatusCode);
            return false;
        }
        catch (HttpRequestException e)
        {
            log.LogError(e, "AiChat недоступен: данные чатов пользователя не удалены");
            return false;
        }
        catch (TaskCanceledException e) when (!ct.IsCancellationRequested)
        {
            log.LogError(e, "AiChat не ответил вовремя на удаление данных пользователя");
            return false;
        }
    }
}
