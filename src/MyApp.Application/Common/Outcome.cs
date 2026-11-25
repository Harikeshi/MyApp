namespace MyApp.Application.Common;

public enum OutcomeStatus { Ok, NotFound, Forbidden, Invalid }

/// <summary>Результат операции сервиса: успех или понятная причина отказа (слой Web переводит её в HTTP-статус).</summary>
public readonly record struct Outcome<T>(OutcomeStatus Status, T? Value, string? Error)
{
    public bool IsOk => Status == OutcomeStatus.Ok;

    public static Outcome<T> Ok(T value) => new(OutcomeStatus.Ok, value, null);
    public static Outcome<T> NotFound() => new(OutcomeStatus.NotFound, default, null);
    public static Outcome<T> Forbidden(string error) => new(OutcomeStatus.Forbidden, default, error);
    public static Outcome<T> Invalid(string error) => new(OutcomeStatus.Invalid, default, error);
}
