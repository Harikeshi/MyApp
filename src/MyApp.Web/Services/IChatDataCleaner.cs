namespace MyApp.Web.Services;

/// <summary>Удаление данных пользователя в сервисе AiChat (у него отдельная база и каталог файлов).</summary>
public interface IChatDataCleaner
{
    /// <summary>Удаляет чаты, сообщения и файлы текущего пользователя. true — у него в AiChat ничего не осталось.</summary>
    Task<bool> DeleteAllAsync(CancellationToken ct);
}
