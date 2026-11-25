namespace MyApp.Application.Accounts;

public record RegisterResult(bool Succeeded, IDictionary<string, string[]> Errors)
{
    public static RegisterResult Success() =>
        new(true, new Dictionary<string, string[]>());

    public static RegisterResult Failure(IDictionary<string, string[]> errors) =>
        new(false, errors);

    public static RegisterResult Failure(string field, string message) =>
        new(false, new Dictionary<string, string[]> { [field] = [message] });
}

public enum PasswordResetRequestResult
{
    Sent,
    NotRegistered,
    NotConfirmed
}

/// <param name="Id">Id пользователя: по нему клиент отличает себя от других (исполнитель задачи, ответственные проекта).</param>
public record ProfileResponse(string Name, string Email, string Id = "");

public record UpdateNameResult(bool Succeeded, ProfileResponse? Profile, string? Error)
{
    public static UpdateNameResult Success(ProfileResponse profile) => new(true, profile, null);

    public static UpdateNameResult Failure(string error) => new(false, null, error);
}

/// <summary>
/// Порт учётных записей. Реализация использует ASP.NET Core Identity и поэтому лежит в Infrastructure.
/// </summary>
public interface IAccountService
{
    Task<RegisterResult> RegisterAsync(string email, string password, string displayName, CancellationToken ct);

    Task<PasswordResetRequestResult> RequestPasswordResetAsync(string email, CancellationToken ct);

    /// <summary>Имя и почта текущего пользователя (null, если пользователь не найден).</summary>
    Task<ProfileResponse?> GetProfileAsync(CancellationToken ct);

    /// <summary>Отзывает refresh-токены текущего пользователя (токен доступа живёт до истечения срока).</summary>
    Task RevokeSessionsAsync(CancellationToken ct);

    /// <summary>Меняет имя текущего пользователя (правила: DisplayNameRules).</summary>
    Task<UpdateNameResult> UpdateDisplayNameAsync(string displayName, CancellationToken ct);

    /// <summary>Верен ли пароль текущего пользователя. Нужен как подтверждение перед удалением аккаунта.</summary>
    Task<bool> VerifyPasswordAsync(string password, CancellationToken ct);

    /// <summary>Удаляет текущего пользователя вместе с его задачами (каскадом в базе). false — удалить не удалось.</summary>
    Task<bool> DeleteAsync(CancellationToken ct);
}
