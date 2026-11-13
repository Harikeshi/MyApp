namespace MyApp.Web.Security;

/// <summary>
/// Выключатель регистрации: если существует файл-флаг, регистрация закрыта (без перезапуска приложения).
/// Закрыть: sudo myapp-registration close. Открыть: sudo myapp-registration open.
/// </summary>
public class RegistrationGate(IConfiguration cfg)
{
    private readonly string _flagFile = cfg["Registration:ClosedFile"] ?? "/etc/myapp/registration.closed";

    public bool IsOpen => !File.Exists(_flagFile);
}

public class RegistrationGateFilter(RegistrationGate gate) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        if (!gate.IsOpen)
            return Results.Json(new { error = "registration_closed" },
                statusCode: StatusCodes.Status503ServiceUnavailable);

        return await next(ctx);
    }
}
