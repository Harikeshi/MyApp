namespace MyApp.Application.Abstractions;

public record UserInfo(string Id, string Name);

/// <summary>Справочник пользователей (реализует Identity в Infrastructure). Почта наружу не отдаётся.</summary>
public interface IUserDirectory
{
    /// <summary>Имена по id. Нет пользователя — нет и записи в словаре.</summary>
    Task<IReadOnlyDictionary<string, string>> GetNamesAsync(IReadOnlyCollection<string> ids, CancellationToken ct);

    /// <summary>Поиск по имени (часть имени) или по точному адресу почты.</summary>
    Task<IReadOnlyList<UserInfo>> SearchAsync(string query, int take, CancellationToken ct);

    /// <summary>Какие из id существуют.</summary>
    Task<IReadOnlySet<string>> ExistingIdsAsync(IReadOnlyCollection<string> ids, CancellationToken ct);
}
